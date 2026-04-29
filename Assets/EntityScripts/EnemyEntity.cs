using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

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

    // ── Window traversal ──────────────────────────────────────────
    [Header("Window Traversal")]
    [Tooltip("Speed the enemy moves while vaulting through a window.")]
    public float windowTraversalSpeed = 2.5f;
    [Tooltip("Animator state name to play during the vault. Empty = skip.")]
    public string windowVaultAnimation = "WindowVault";

    // ── Private ───────────────────────────────────────────────────
    private Vector3 patrolPoint;
    private float   patrolTimer;
    private Vector3 lastKnownTargetPos;
    private float   investigateTimer;
    private bool    isWaiting;
    private Combat  combat;
    private float   attackRange;
    private float   attackRotateSpeed = 5f;
    private bool    _traversingWindow = false;

    // ── Group AI ──────────────────────────────────────────────────
    [Header("Group AI (set by EnemyManager)")]
    public bool        isGroupLeader = false;
    public EnemyEntity groupLeader;
    [HideInInspector] public GameObject groupSharedTarget;
    [HideInInspector] public bool       groupTargetDetected;
    public bool hasIndependentVision = false;

    private GameObject cachedVisibleTarget;

    // ─────────────────────────────────────────────────────────────
    void Start()
    {
        agent  = GetComponent<NavMeshAgent>();
        player = FindAnyObjectByType<KCC>();
        combat = GetComponent<Combat>();

        if (combat != null && combat.attackTemplates.Count > 0)
        {
            attackRange = combat.attackTemplates[0].range;
            if (attackRange < 1.5f)
            {
                Debug.LogWarning("[EnemyEntity] AttackTemplate.range " + attackRange +
                    " too small — using 1.5. Fix in AttackTemplate.");
                attackRange = 1.5f;
            }
            agent.stoppingDistance = attackRange * 0.8f;
        }
        else
        {
            Debug.LogError("[EnemyEntity] No AttackTemplates found on " + name);
        }

        // IMPORTANT: disable auto-traversal so we can run our own coroutine
        // for window vaulting. Without this, Unity just glides the agent
        // across the link with no animation or speed control.
        agent.autoTraverseOffMeshLink = false;

        EnemyManager.Instance?.RegisterEnemy(this);
        StartCoroutine(AIPerceptionLoop());
    }

    void OnDestroy()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
    }

    // ── AI loop (coroutine, staggered) ────────────────────────────
    IEnumerator AIPerceptionLoop()
    {
        yield return new WaitForSeconds(Random.Range(0f, aiUpdateInterval));
        while (true)
        {
            if (!_traversingWindow)
            {
                RunPerception();
                RunStateMachine();
            }
            yield return new WaitForSeconds(aiUpdateInterval);
        }
    }

    // ── Update: per-frame work + link detection ───────────────────
    void Update()
    {
        // ── Window link detection ──────────────────────────────────
        // agent.isOnOffMeshLink is true for BOTH old OffMeshLinks AND
        // NavMeshLink — that property name is kept for API compatibility.
        // We identify it as a window by checking for a Window component
        // in the parent hierarchy of the link's GameObject.
        if (!_traversingWindow && agent != null && agent.isOnOffMeshLink)
        {
            OffMeshLinkData linkData = agent.currentOffMeshLinkData;

            // linkData.offMeshLink is only populated for the legacy OffMeshLink.
            // For NavMeshLink, linkData.offMeshLink is NULL — we use the
            // nearest position to find our Window GO instead.
            Window window = FindWindowAtLink(linkData);

            if (window != null)
            {
                StartCoroutine(TraverseWindow(linkData));
                return;
            }

            // Not a window link — let the agent handle it normally.
            // (autoTraverseOffMeshLink = false means we must complete it manually
            //  for non-window links too, otherwise the agent gets stuck.)
            agent.CompleteOffMeshLink();
        }

        // ── Normal per-frame ───────────────────────────────────────
        if (enemyState == EntityState.Attack && currentTarget != null)
            RotateTowardsTarget(currentTarget.transform.position);

        if (isWaiting)
        {
            patrolTimer -= Time.deltaTime;
            if (patrolTimer <= 0f)
            {
                isWaiting   = false;
                patrolPoint = GetRandomPatrolPoint();
                TrySetDestination(patrolPoint);
            }
        }

        if (enemyState == EntityState.Search && agent.remainingDistance <= agent.stoppingDistance)
        {
            investigateTimer -= Time.deltaTime;
            if (investigateTimer <= 0f)
                enemyState = EntityState.Patrol;
        }
    }

    // ── Window link identification ────────────────────────────────
    // NavMeshLink does not expose itself through OffMeshLinkData.offMeshLink.
    // Instead we do an OverlapSphere at the link's start position and look
    // for a GO that has a Window component anywhere in its hierarchy.
    // Radius 1.5f is tight enough to avoid false positives at normal door spacing.
    Window FindWindowAtLink(OffMeshLinkData linkData)
    {
        // Check start position of the link
        Collider[] nearby = Physics.OverlapSphere(linkData.startPos, 1.5f);
        foreach (Collider col in nearby)
        {
            Window w = col.GetComponentInParent<Window>();
            if (w != null && w.state == Window.WindowState.Broken)
                return w;
        }

        // Also check end position (bidirectional: enemy may approach from inside)
        nearby = Physics.OverlapSphere(linkData.endPos, 1.5f);
        foreach (Collider col in nearby)
        {
            Window w = col.GetComponentInParent<Window>();
            if (w != null && w.state == Window.WindowState.Broken)
                return w;
        }

        return null;
    }

    // ── Window vault coroutine ────────────────────────────────────
    IEnumerator TraverseWindow(OffMeshLinkData linkData)
    {
        _traversingWindow = true;
        agent.isStopped   = true;       // we drive movement, not the agent

        Vector3 startPos = agent.transform.position;
        Vector3 endPos   = linkData.endPos + Vector3.up * agent.baseOffset;

        // Play vault animation if configured
        Animator anim = GetComponent<Animator>();
        if (anim != null && !string.IsNullOrEmpty(windowVaultAnimation))
            anim.CrossFade(windowVaultAnimation, 0.1f);

        // Smooth lerp across the opening
        float distance = Vector3.Distance(startPos, endPos);
        float duration = Mathf.Max(distance / Mathf.Max(windowTraversalSpeed, 0.1f), 0.15f);
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            agent.transform.position = Vector3.Lerp(startPos, endPos, t);

            Vector3 dir = (endPos - startPos).normalized;
            if (dir != Vector3.zero)
                agent.transform.rotation = Quaternion.Slerp(
                    agent.transform.rotation,
                    Quaternion.LookRotation(dir),
                    Time.deltaTime * 12f);

            yield return null;
        }

        agent.transform.position = endPos;

        // Hand control back to the NavMeshAgent on the far side
        agent.CompleteOffMeshLink();
        agent.isStopped   = false;
        _traversingWindow = false;

        // Old path was computed from outside — invalidate so next
        // TrySetDestination does a clean CalculatePath from inside.
        InvalidateDestinationCache();

        if (debugMode) Debug.Log("[EnemyEntity] Window traversal complete → " + endPos);
    }

    // ── Perception ────────────────────────────────────────────────
    void RunPerception()
    {
        if (!isGroupLeader && groupLeader != null)
        {
            if (groupLeader == null) { isGroupLeader = true; return; }

            if (!hasIndependentVision)
            {
                groupTargetDetected = groupLeader.groupTargetDetected;
                groupSharedTarget   = groupLeader.groupSharedTarget;
                cachedVisibleTarget = groupSharedTarget;

                if (groupLeader.groupTargetDetected && groupSharedTarget != null)
                {
                    currentTarget      = groupSharedTarget;
                    lastKnownTargetPos = groupSharedTarget.transform.position;
                    if (enemyState != EntityState.Attack) enemyState = EntityState.Sprint;
                }
                else if (groupLeader.enemyState == EntityState.Search
                         && enemyState != EntityState.Attack
                         && enemyState != EntityState.Sprint)
                {
                    lastKnownTargetPos = groupLeader.lastKnownTargetPos;
                    enemyState         = EntityState.Search;
                }
                return;
            }

            // Independent vision follower: cheap FOV dot-product only
            if (player != null)
            {
                Vector3 toPlayer = player.transform.position - transform.position;
                if (toPlayer.sqrMagnitude <= viewDistance * viewDistance)
                {
                    float dot          = Vector3.Dot(transform.forward, toPlayer.normalized);
                    float halfAngleCos = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
                    if (dot >= halfAngleCos)
                        groupLeader.ReceiveSuspicionPing(player.transform.position);
                }
            }

            groupTargetDetected = groupLeader.groupTargetDetected;
            groupSharedTarget   = groupLeader.groupSharedTarget;
            cachedVisibleTarget = groupSharedTarget;
            return;
        }

        // ── Leader / solo full perception ─────────────────────────
        DetectEntitiesInSphere(transform.position, viewDistance, entityMask, groundMask, entities);

        GameObject visible = CheckForVisibleTarget();
        if (visible == null && player != null && CanSeeTarget(player.transform))
            visible = player.gameObject;

        Vector3? heard = null;
        if (visible == null && enemyState != EntityState.Attack)
            heard = CheckForNoise();

        cachedVisibleTarget = visible;
        groupTargetDetected = visible != null;
        groupSharedTarget   = visible;

        if (visible != null)
        {
            currentTarget      = visible;
            lastKnownTargetPos = currentTarget.transform.position;
            investigateTimer   = investigateTime;
            if (enemyState != EntityState.Attack) enemyState = EntityState.Sprint;
        }
        else if (heard.HasValue && enemyState != EntityState.Attack && enemyState != EntityState.Sprint)
        {
            lastKnownTargetPos = heard.Value;
            enemyState         = EntityState.Search;
            investigateTimer   = investigateTime;
            if (debugMode) Debug.Log("Noise heard at: " + lastKnownTargetPos);
        }
    }

    // ── State machine ─────────────────────────────────────────────
    void RunStateMachine()
    {
        switch (enemyState)
        {
            case EntityState.Patrol: PatrolBehavior();                    break;
            case EntityState.Sprint: ChaseBehavior(cachedVisibleTarget);  break;
            case EntityState.Search: InvestigateBehavior();               break;
            case EntityState.Attack: AttackBehavior();                    break;
        }
    }

    void PatrolBehavior()
    {
        if (isWaiting) return;
        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
        {
            isWaiting   = true;
            patrolTimer = Random.Range(minPatrolInterval, maxPatrolInterval);
            agent.ResetPath();
        }
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

        Vector3 targetPos = visibleTarget.transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 10f, NavMesh.AllAreas))
            targetPos = hit.position;

        lastKnownTargetPos = targetPos;
        float dist = Vector3.Distance(transform.position, lastKnownTargetPos);

        if (dist <= agent.stoppingDistance + 0.2f)
        {
            enemyState      = EntityState.Attack;
            agent.isStopped = true;
        }
        else
        {
            agent.isStopped = false;
            bool isMoving             = agent.velocity.sqrMagnitude > 0.1f;
            bool targetMovedSignif    = Vector3.Distance(agent.destination, targetPos) > 2.5f
                                     || Mathf.Abs(agent.destination.y - targetPos.y) > 1.0f;
            if (!isMoving || targetMovedSignif)
                TrySetDestination(targetPos);
        }
    }

    void InvestigateBehavior()
    {
        TrySetDestination(lastKnownTargetPos);
    }

    void AttackBehavior()
    {
        if (currentTarget == null)
        {
            enemyState          = EntityState.Search;
            combat.combatActive = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (dist >= attackRange * 1.5f)
        {
            enemyState          = EntityState.Sprint;
            combat.combatActive = false;
            return;
        }

        if (combat.currentAttack == null && combat.attackTemplates.Count > 0)
            combat.currentAttack = combat.attackTemplates[0];
        combat.combatActive = true;
    }

    Vector3 GetRandomPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * patrolRange + transform.position;
        NavMeshHit hit;
        return NavMesh.SamplePosition(randomDir, out hit, patrolRange, NavMesh.AllAreas)
            ? hit.position : transform.position;
    }

    public void ReceiveSuspicionPing(Vector3 suspectedPos)
    {
        if (!isGroupLeader) return;

        Vector3 origin = transform.position + Vector3.up;
        Vector3 dir    = ((suspectedPos + Vector3.up) - origin).normalized;
        float   dist   = Vector3.Distance(origin, suspectedPos + Vector3.up);

        if (!Physics.Raycast(origin, dir, dist, obstacleMask))
        {
            groupSharedTarget   = player != null ? player.gameObject : null;
            groupTargetDetected = true;
            lastKnownTargetPos  = suspectedPos;
            if (enemyState != EntityState.Attack) enemyState = EntityState.Sprint;
        }
        else
        {
            lastKnownTargetPos = suspectedPos;
            enemyState         = EntityState.Search;
            InvalidateDestinationCache();
        }
    }

    void RotateTowardsTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction), Time.deltaTime * attackRotateSpeed);
    }

    // ── Perception helpers ────────────────────────────────────────
    GameObject CheckForVisibleTarget()
    {
        foreach (GameObject entity in entities)
        {
            if (entity == null || entity == gameObject) continue;
            BaseEntity other = entity.GetComponent<BaseEntity>();
            if (other != null && other.faction == faction) continue;
            if (CanSeeTarget(entity.transform)) return entity;
        }
        return null;
    }

    bool CanSeeTarget(Transform target)
    {
        Vector3 origin    = transform.position + Vector3.up;
        Vector3 targetPos = target.position + Vector3.up;
        Vector3 dir       = targetPos - origin;
        float   distance  = dir.magnitude;

        if (distance > viewDistance) return false;

        Vector3 flatDir = (target.position - transform.position).normalized;
        flatDir.y = 0;
        if (Vector3.Angle(transform.forward, flatDir) > viewAngle * 0.5f) return false;
        if (Physics.Raycast(origin, dir.normalized, distance, obstacleMask)) return false;

        return true;
    }

    Vector3? CheckForNoise()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, hearingRadius);
        SoundController loudest = null;
        float maxSpeed = 0f;

        foreach (Collider col in cols)
        {
            SoundController sc = col.GetComponent<SoundController>();
            if (sc == null) continue;
            float speed = sc.GetVelocity().magnitude;
            if (speed < minVelocityThreshold || speed <= maxSpeed) continue;
            maxSpeed = speed;
            loudest  = sc;
        }

        return loudest != null ? loudest.transform.position : (Vector3?)null;
    }

    // ── Gizmos ────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (!debugMode) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward * viewDistance);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0,  viewAngle / 2, 0) * transform.forward * viewDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        if (enemyState == EntityState.Search || enemyState == EntityState.Sprint)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownTargetPos, 0.5f);
            Gizmos.DrawLine(transform.position, lastKnownTargetPos);
        }

        Gizmos.color = isGroupLeader ? Color.green : Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.6f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}