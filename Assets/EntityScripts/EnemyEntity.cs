using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering;

public class EnemyEntity : BaseEntity
{
    [Header("Vision settings")]
    public float viewDistance = 15f;
    [Range(0, 360)] public float viewAngle = 90f;
    public LayerMask obstacleMask;

    [Header("Hearing settings")]
    public float hearingRadius = 10f;
    public float minVelocityThreshold = 5f;

    [Header("Patrol settings")]
    public float patrolRange = 12f;
    public float minPatrolInterval = 4f;
    public float maxPatrolInterval = 10f;

    [Header("Investigate")]
    public float investigateTime = 3f;

    [Header("AI Optimization")]
    public float aiUpdateInterval = 0.15f;
    [Header("State")]
    public EntityState enemyState = EntityState.Patrol;

    private Vector3 patrolPoint;
    private float patrolTimer;
    private Vector3 lastKnownTargetPos;
    private float investigateTimer;
    private bool isWaiting;
    private Combat combat;
    private AttackTemplate currentAttack;
    private float attackRange;
    private float attackRotateSpeed = 5f;

        // -----------------------------------------------------------------------
    // OPTIMIZATION: Group AI fields.
    // Leader does all expensive perception; followers just read the result.
    [Header("Group AI (set by EnemyManager)")]
    public bool isGroupLeader = false;
    public EnemyEntity groupLeader;
    [HideInInspector] public GameObject groupSharedTarget;
    [HideInInspector] public bool groupTargetDetected;

    // -----------------------------------------------------------------------
    // PERCEPTION: If true, this follower runs its own full vision check
    // instead of blindly copying the leader's result. Costs more CPU per
    // enemy but means every enemy with this flag can independently spot
    // the player and ping the leader to confirm + decide pathing.
    // Use for elite/tactical enemies. Leave false for dumb grunt types.
    public bool hasIndependentVision = false;
    // -----------------------------------------------------------------------
 
    // -----------------------------------------------------------------------
    // OPTIMIZATION: Cached visible target from last perception tick.
    // Behavior methods read this instead of re-running detection.
    private GameObject cachedVisibleTarget;
    // -----------------------------------------------------------------------

    // BUG FIX: Przeniesione z Awake do Start — Awake odpala się zanim Unity
    // przypisze serializowane pola (jak attackTemplates) do komponentów,
    // które mogły być dodane w tym samym czasie co EnemyEntity.
    // Start gwarantuje że wszystkie Awake() już się wykonały.
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = FindAnyObjectByType<KCC>();
        combat = GetComponent<Combat>();

        if (combat != null && combat.attackTemplates.Count > 0)
        {
            attackRange = combat.attackTemplates[0].range;

            // Jesli range w AttackTemplate jest za male (np. 0.5), NavMesh nigdy
            // nie zatrzyma wroga wystarczajaco blisko i nigdy nie wejdzie w stan Attack.
            if (attackRange < 1.5f)
            {
                Debug.LogWarning("[EnemyEntity] AttackTemplate.range = " + attackRange +
                    " jest za male. Zmien range na >= 1.5 w AttackTemplate. Tymczasowo uzywam 1.5.");
                attackRange = 1.5f;
            }

            agent.stoppingDistance = attackRange * 0.8f;
            Debug.Log("[EnemyEntity] Zasieg ataku: " + attackRange + ", stoppingDistance: " + agent.stoppingDistance);
        }
        else
        {
            Debug.LogError("[EnemyEntity] LISTA ATAKOW JEST PUSTA! Dodaj AttackTemplate do listy Combat na tym obiekcie.");
        }

        // -----------------------------------------------------------------------
        // OPTIMIZATION: Register with EnemyManager so it can assign groups.
        // Stagger each enemy's first AI tick randomly to spread load across frames.
        EnemyManager.Instance?.RegisterEnemy(this);
        StartCoroutine(AIPerceptionLoop());
        // -----------------------------------------------------------------------
    }

        void OnDestroy()
    {
        // -----------------------------------------------------------------------
        EnemyManager.Instance?.UnregisterEnemy(this);
        // -----------------------------------------------------------------------
    }

    // -----------------------------------------------------------------------
    // OPTIMIZATION: Replaces FixedUpdate for all perception logic.
    // Runs every aiUpdateInterval seconds, staggered per enemy.
    // For 8 enemies at 0.15s: ~53 perception ticks/sec total
    // vs old FixedUpdate: 400 ticks/sec (8 × 50Hz). ~7.5× less work.
    IEnumerator AIPerceptionLoop()
    {
        // Stagger startup so enemies don't all tick on frame 1
        yield return new WaitForSeconds(Random.Range(0f, aiUpdateInterval));
 
        while (true)
        {
            RunPerception();
            RunStateMachine();
            yield return new WaitForSeconds(aiUpdateInterval);
        }
    }
    // -----------------------------------------------------------------------
 
    void RunPerception()
{
    if (!isGroupLeader && groupLeader != null)
    {
        // -----------------------------------------------------------------------
        // ORPHAN GUARD: If leader reference is stale, self-promote.
        // EnemyManager will clean this up on next regroup (0.5s).
        if (groupLeader == null)
        {
            isGroupLeader = true;
            return;
        }

        if (!hasIndependentVision)
        {
            groupTargetDetected = groupLeader.groupTargetDetected;
            groupSharedTarget   = groupLeader.groupSharedTarget;
            cachedVisibleTarget = groupSharedTarget;

            // STATE SYNC: Follower mirrors leader's alert state.
            // Without this, followers copy the target data but never act on it —
            // they stay in Patrol while the leader chases.
            if (groupLeader.groupTargetDetected && groupSharedTarget != null)
            {
                currentTarget      = groupSharedTarget;
                lastKnownTargetPos = groupSharedTarget.transform.position;

                // Don't override Attack — if a follower is already in melee, let it finish
                if (enemyState != EntityState.Attack) enemyState = EntityState.Sprint;
            }
            else if (groupLeader.enemyState == EntityState.Search && enemyState != EntityState.Attack && enemyState != EntityState.Sprint)
            {
                // Leader is investigating something — followers sweep with it
                lastKnownTargetPos = groupLeader.lastKnownTargetPos;
                enemyState         = EntityState.Search;
            }

            return;
        }

        // hasIndependentVision == true path:
        // Run cheap FOV dot product only — no OverlapSphere, no full raycast yet.
        // If we spot something, ping the leader to confirm and decide movement.
        // This is the middle ground: aware but not doing the heavy lifting.
        if (player != null)
        {
            Vector3 toPlayer = player.transform.position - transform.position;
            float sqrDist    = toPlayer.sqrMagnitude;

            if (sqrDist <= viewDistance * viewDistance)
            {
                float dot           = Vector3.Dot(transform.forward, toPlayer.normalized);
                float halfAngleCos  = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);

                if (dot >= halfAngleCos)
                {
                    // In our FOV — ping the leader with suspected position.
                    // Leader does the one expensive raycast to confirm.
                    groupLeader.ReceiveSuspicionPing(player.transform.position);
                }
            }
        }

        // Still inherit the leader's confirmed result for our own movement
        groupTargetDetected = groupLeader.groupTargetDetected;
        groupSharedTarget   = groupLeader.groupSharedTarget;
        cachedVisibleTarget = groupSharedTarget;
        return;
        // -----------------------------------------------------------------------
    }

    // ... rest of leader perception unchanged
        // -----------------------------------------------------------------------
 
        // Leader (or solo enemy) does the real work:
        DetectEntitiesInSphere(transform.position, viewDistance, entityMask, groundMask, entities);
 
        GameObject visible = CheckForVisibleTarget();
        if (visible == null && player != null && CanSeeTarget(player.transform))
            visible = player.gameObject;
 
        Vector3? heard = null;
        if (visible == null && enemyState != EntityState.Attack)
            heard = CheckForNoise();
 
        cachedVisibleTarget = visible;
 
        // -----------------------------------------------------------------------
        // OPTIMIZATION: Write shared result so group followers can read it.
        groupTargetDetected = visible != null;
        groupSharedTarget   = visible;
        // -----------------------------------------------------------------------
 
        // Update state based on perception result
        if (visible != null)
        {
            currentTarget        = visible;
            lastKnownTargetPos   = currentTarget.transform.position;
            investigateTimer     = investigateTime;
            if (enemyState != EntityState.Attack)
                enemyState = EntityState.Sprint;
        }
        else if (heard.HasValue)
        {
            if (enemyState != EntityState.Attack && enemyState != EntityState.Sprint)
            {
                lastKnownTargetPos = heard.Value;
                enemyState         = EntityState.Search;
                investigateTimer   = investigateTime;
                if (debugMode) Debug.Log("Noise heard from: " + lastKnownTargetPos);
            }
        }
    }
 
    void RunStateMachine()
    {
        switch (enemyState)
        {
            case EntityState.Patrol:  PatrolBehavior();                   break;
            case EntityState.Sprint:  ChaseBehavior(cachedVisibleTarget); break;
            case EntityState.Search:  InvestigateBehavior();              break;
            case EntityState.Attack:  AttackBehavior();                   break;
        }
    }
 
    // -----------------------------------------------------------------------
    // OPTIMIZATION: Smooth rotation lives in Update() — it needs per-frame
    // precision. Everything else moved to the coroutine.
    void Update()
    {
        if (enemyState == EntityState.Attack && currentTarget != null)
            RotateTowardsTarget(currentTarget.transform.position);
 
        // Patrol wait timer still needs real time
        if (isWaiting)
        {
            patrolTimer -= Time.deltaTime;
            if (patrolTimer <= 0f)
            {
                isWaiting  = false;
                patrolPoint = GetRandomPatrolPoint();
                TrySetDestination(patrolPoint);
            }
        }
 
        // Investigate countdown needs real time
        if (enemyState == EntityState.Search && agent.remainingDistance <= agent.stoppingDistance)
        {
            investigateTimer -= Time.deltaTime;
            if (investigateTimer <= 0f)
                enemyState = EntityState.Patrol;
        }
    }
    // -----------------------------------------------------------------------
 
    GameObject CheckForVisibleTarget()
    {
        foreach (GameObject entity in entities)
        {
            if (entity == null) continue;
            if (entity == this.gameObject) continue;
 
            BaseEntity other = entity.GetComponent<BaseEntity>();
            if (other != null && other.faction == this.faction) continue;
 
            if (CanSeeTarget(entity.transform))
                return entity;
        }
        return null;
    }
 
    bool CanSeeTarget(Transform target)
    {
        Vector3 origin    = transform.position + Vector3.up;
        Vector3 targetPos = target.position + Vector3.up;
 
        Vector3 dirToTarget = targetPos - origin;
        float distance      = dirToTarget.magnitude;
 
        if (distance > viewDistance) return false;
 
        dirToTarget.Normalize();
 
        Vector3 flatDir = (target.position - transform.position).normalized;
        flatDir.y = 0;
 
        if (Vector3.Angle(transform.forward, flatDir) > viewAngle * 0.5f) return false;
        if (Physics.Raycast(origin, dirToTarget, distance, obstacleMask))  return false;
 
        return true;
    }
 
    Vector3? CheckForNoise()
    {
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, hearingRadius);
 
        SoundController loudest = null;
        float maxSpeed  = 0f;
        bool foundSound = false;
 
        for (int i = 0; i < collidersInRange.Length; i++)
        {
            SoundController sc = collidersInRange[i].GetComponent<SoundController>();
            if (sc == null) continue;
 
            float speed = sc.GetVelocity().magnitude;
            if (speed < minVelocityThreshold) continue;
            if (speed > maxSpeed)
            {
                maxSpeed    = speed;
                loudest     = sc;
                foundSound  = true;
            }
        }
 
        return (foundSound && loudest != null) ? loudest.transform.position : (Vector3?)null;
    }
 
    void PatrolBehavior()
    {
        if (isWaiting) return; // timer handled in Update()
 
        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
        {
            isWaiting   = true;
            patrolTimer = Random.Range(minPatrolInterval, maxPatrolInterval);
            agent.ResetPath();
        }
 
        if (debugMode) Debug.Log("Patrol");
    }
 
    void ChaseBehavior(GameObject visibleTarget)
    {
        if (visibleTarget == null)
        {
            agent.isStopped = false;
            TrySetDestination(lastKnownTargetPos);
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                enemyState = EntityState.Search;
            return;
        }

        // Project player to ground
        Vector3 targetPos = visibleTarget.transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 10f, NavMesh.AllAreas))
            targetPos = hit.position;

        lastKnownTargetPos = targetPos;
        float dist = Vector3.Distance(transform.position, lastKnownTargetPos);

        if (dist <= agent.stoppingDistance + 0.2f)
        {
            enemyState = EntityState.Attack;
            agent.isStopped = true;
        }
        else
        {
            agent.isStopped = false;
            
            // ONLY update the destination if the agent is stuck OR the player moved a lot
            // This keeps the steering "clean" for those 20m stretches
            bool isMoving = agent.velocity.sqrMagnitude > 0.1f;
            bool targetMovedSignificantly = Vector3.Distance(agent.destination, targetPos) > 2.5f || Mathf.Abs(agent.destination.y - targetPos.y) > 1.0f;

            if (!isMoving || targetMovedSignificantly)
            {
                TrySetDestination(targetPos);
            }
        }
    }
 
    void InvestigateBehavior()
    {
        // -----------------------------------------------------------------------
        // BUG FIX: Was calling TrySetDestination twice (once in if, once in else).
        // Now called once. Timer countdown moved to Update().
        TrySetDestination(lastKnownTargetPos);
        // -----------------------------------------------------------------------
 
        if (debugMode) Debug.Log("Investigate");
    }
 
    void AttackBehavior()
    {
        if (currentTarget == null)
        {
            enemyState        = EntityState.Search;
            combat.combatActive = false;
            return;
        }
 
        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
 
        if (dist >= attackRange * 1.5f)
        {
            enemyState        = EntityState.Sprint;
            combat.combatActive = false;
            return;
        }
 
        if (combat.currentAttack == null && combat.attackTemplates.Count > 0)
            combat.currentAttack = combat.attackTemplates[0];
 
        combat.combatActive = true;
    }
 
    Vector3 GetRandomPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * patrolRange;
        randomDir += transform.position;
 
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, patrolRange, NavMesh.AllAreas))
            return hit.position;
 
        return transform.position;
    }

    // -----------------------------------------------------------------------
    // Called by followers with hasIndependentVision when their cheap FOV
    // check passes. Leader does one raycast to confirm, then either
    // transitions to Sprint or moves to investigate if wall-blocked.
    public void ReceiveSuspicionPing(Vector3 suspectedPos)
    {
        if (!isGroupLeader) return; // safety — only leader processes this

        Vector3 origin = transform.position + Vector3.up;
        Vector3 dir    = ((suspectedPos + Vector3.up) - origin).normalized;
        float dist     = Vector3.Distance(origin, suspectedPos + Vector3.up);

        if (!Physics.Raycast(origin, dir, dist, obstacleMask))
        {
            // Confirmed — treat as spotted
            groupSharedTarget   = player != null ? player.gameObject : null;
            groupTargetDetected = true;
            lastKnownTargetPos  = suspectedPos;
            if (enemyState != EntityState.Attack)
                enemyState = EntityState.Sprint;
        }
        else
        {
            // Wall blocking leader's view — go investigate the ping
            // Followers follow naturally since they track leader position
            lastKnownTargetPos = suspectedPos;
            enemyState         = EntityState.Search;
            InvalidateDestinationCache();
        }
    }
    // -----------------------------------------------------------------------
 
    void RotateTowardsTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
 
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * attackRotateSpeed);
        }
    }
 
    void OnDrawGizmos()
    {
        if (!debugMode) return;
 
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
 
        Vector3 left  = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0,  viewAngle / 2, 0) * transform.forward;
 
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, left  * viewDistance);
        Gizmos.DrawRay(transform.position, right * viewDistance);
 
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
 
        if (enemyState == EntityState.Search || enemyState == EntityState.Sprint)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownTargetPos, 0.5f);
            Gizmos.DrawLine(transform.position, lastKnownTargetPos);
        }
 
        // -----------------------------------------------------------------------
        // DEBUG: Show which enemy is the group leader (green = leader, white = follower)
        Gizmos.color = isGroupLeader ? Color.green : Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.6f);
        // -----------------------------------------------------------------------
 
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}