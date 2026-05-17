using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;

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

    [Header("Window Traversal")]
    [Tooltip("Speed the enemy moves while vaulting through a window.")]
    public float windowTraversalSpeed = 2.5f;
    [Tooltip("Animator state name to play during the vault. Empty = skip.")]
    public string windowVaultAnimation = "WindowVault";

    [Header("Death Settings")]
    public float disappearDelay = 30f;
    public float fadeDuration = 2f;

    private Vector3 patrolPoint;
    private float   patrolTimer;
    private Vector3 lastKnownTargetPos;
    private float   investigateTimer;
    private bool    isWaiting;
    private Combat  combat;
    private float   attackRange;
    private float   attackRotateSpeed = 5f;
    private bool    _traversingWindow = false;
    private EntityStatus _status;
    private bool    _isDead = false;

    [Header("Group AI (set by EnemyManager)")]
    public bool        isGroupLeader = false;
    public EnemyEntity groupLeader;
    [HideInInspector] public GameObject groupSharedTarget;
    [HideInInspector] public bool       groupTargetDetected;
    public bool hasIndependentVision = false;

    private GameObject cachedVisibleTarget;

    void Start()
    {
        _status = GetComponent<EntityStatus>();
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

        agent.autoTraverseOffMeshLink = false;

        EnemyManager.Instance?.RegisterEnemy(this);
        StartCoroutine(AIPerceptionLoop());
    }

    void OnDestroy()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
    }

    IEnumerator AIPerceptionLoop()
    {
        yield return new WaitForSeconds(Random.Range(0f, aiUpdateInterval));
        while (true)
        {
            if (_status != null && _status.entityHealth <= 0f)
            {
                OnDeath();
                yield break;
            }
            if (!_traversingWindow)
            {
                RunPerception();
                RunStateMachine();
            }
            yield return new WaitForSeconds(aiUpdateInterval);
        }
    }

    void Update()
    {
        if (_isDead) return;
        if (_status != null && _status.entityHealth <= 0f) return;

        if (!_traversingWindow && agent != null && agent.isOnOffMeshLink)
        {
            OffMeshLinkData linkData = agent.currentOffMeshLinkData;
            Door window = FindWindowAtLink(linkData);
            if (window != null)
            {
                StartCoroutine(TraverseWindow(linkData));
                return;
            }
            agent.CompleteOffMeshLink();
        }

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

    Door FindWindowAtLink(OffMeshLinkData linkData)
    {
        // Check both link endpoints — whichever is closer to a passable window wins.
        // IsPassableByAI covers both Open and Broken states.
        Collider[] nearby = Physics.OverlapSphere(linkData.startPos, 1.5f);
        foreach (Collider col in nearby)
        {
            Door w = col.GetComponentInParent<Door>();
            if (w != null && w.IsPassableByAI)
                return w;
        }

        nearby = Physics.OverlapSphere(linkData.endPos, 1.5f);
        foreach (Collider col in nearby)
        {
            Door w = col.GetComponentInParent<Door>();
            if (w != null && w.IsPassableByAI)
                return w;
        }

        return null;
    }

    IEnumerator TraverseWindow(OffMeshLinkData linkData)
    {
        _traversingWindow = true;
        agent.isStopped   = true;

        Vector3 startPos = agent.transform.position;
        Vector3 endPos   = linkData.endPos + Vector3.up * agent.baseOffset;

        Animator anim = GetComponent<Animator>();
        if (anim != null && !string.IsNullOrEmpty(windowVaultAnimation))
            anim.CrossFade(windowVaultAnimation, 0.1f);

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
        agent.CompleteOffMeshLink();
        agent.isStopped   = false;
        _traversingWindow = false;

        InvalidateDestinationCache();

        if (debugMode) Debug.Log("[EnemyEntity] Window traversal complete → " + endPos);
    }

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
            bool isMoving          = agent.velocity.sqrMagnitude > 0.1f;
            bool targetMovedSignif = Vector3.Distance(agent.destination, targetPos) > 2.5f
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

    // ── Death ─────────────────────────────────────────────────────
    void OnDeath()
    {
        if (_isDead) return;
        _isDead = true;

        if (combat != null) combat.combatActive = false;
        EnemyManager.Instance?.UnregisterEnemy(this);

        EnableRagdoll();
        StartCoroutine(DisappearAfterDelay(disappearDelay));
    }

    void EnableRagdoll()
    {
        // Freeze animator in last pose instead of disabling it globally
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.speed = 0f;

        // Stop and disable the nav agent
        agent.isStopped = true;
        agent.velocity  = Vector3.zero;
        agent.enabled   = false;

        // Make sure the root collider is active and NOT a trigger
        // so the rigidbody lands on the floor properly
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled   = true;
            col.isTrigger = false;
        }

        // Add rigidbody if not already present
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic            = false;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Higher drag makes it feel heavier and less like a bouncy box
        rb.linearDamping  = 2f;
        rb.angularDamping = 4f;

        // Freeze Y rotation so it doesn't spin like a top,
        // only tips forward/sideways like a real body would
        rb.constraints = RigidbodyConstraints.FreezeRotationY;

        // Tip it in the direction it was facing with slight randomness
        Vector3 fallDir = (-transform.forward + Vector3.up * 0.2f).normalized;
        fallDir += new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
        rb.AddForce(fallDir * 1.5f, ForceMode.Impulse);

        // Torque only on X and Z so it pitches/rolls, not spins
        Vector3 torque = new Vector3(
            Random.Range(1f, 3f),
            0f,
            Random.Range(-1f, 1f)
        );
        rb.AddTorque(torque, ForceMode.Impulse);
    }

    IEnumerator DisappearAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        List<Renderer> renderers = new List<Renderer>(GetComponentsInChildren<Renderer>());

        // Switch all materials to transparent mode for fading
        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                mat.SetFloat("_Mode", 2);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
        }

        // Fade out
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            foreach (Renderer r in renderers)
                foreach (Material mat in r.materials)
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, alpha);

            yield return null;
        }

        Destroy(gameObject);
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