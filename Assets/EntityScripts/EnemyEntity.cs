using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
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
    }

    void FixedUpdate()
    {
        DetectEntitiesInSphere(transform.position, viewDistance, entityMask, groundMask, entities);
        GameObject visibleTarget = CheckForVisibleTarget();
        if (player != null && CanSeeTarget(player.transform))
        {
            visibleTarget = player.gameObject;
        }

        Vector3? heardNoisePos = null;
        if (visibleTarget == null && enemyState != EntityState.Attack)
        {
            heardNoisePos = CheckForNoise();
        }

        if (visibleTarget != null)
        {
            currentTarget = visibleTarget;
            lastKnownTargetPos = currentTarget.transform.position;
            if (enemyState != EntityState.Attack)
            {
                enemyState = EntityState.Sprint;
            }
            investigateTimer = investigateTime;
        }
        else if (heardNoisePos.HasValue)
        {
            if (enemyState != EntityState.Attack && enemyState != EntityState.Sprint)
            {
                lastKnownTargetPos = heardNoisePos.Value;
                enemyState = EntityState.Search;
                investigateTimer = investigateTime;

                if (debugMode) Debug.Log("Noise heard from: " + lastKnownTargetPos);
            }
        }

        switch (enemyState)
        {
            case EntityState.Patrol:
                PatrolBehavior();
                break;
            case EntityState.Sprint:
                ChaseBehavior(visibleTarget);
                break;
            case EntityState.Search:
                InvestigateBehavior();
                break;
            case EntityState.Attack:
                AttackBehavior();
                break;
        }
    }

    GameObject CheckForVisibleTarget()
    {
        foreach (GameObject entity in entities)
        {
            if (entity == null) continue;
            if (CanSeeTarget(entity.transform))
            {
                return entity;
            }
        }
        return null;
    }

    bool CanSeeTarget(Transform target)
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 targetPos = target.position + Vector3.up;

        Vector3 dirToTarget = targetPos - origin;
        float distance = dirToTarget.magnitude;

        if (distance > viewDistance) return false;

        dirToTarget.Normalize();

        Vector3 flatDir = (target.position - transform.position).normalized;
        flatDir.y = 0;

        if (Vector3.Angle(transform.forward, flatDir) > viewAngle * 0.5f) return false;

        if (Physics.Raycast(origin, dirToTarget, distance, obstacleMask)) return false;

        return true;
    }

    Vector3? CheckForNoise()
    {
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, hearingRadius);

        SoundController loudest = null;
        float maxSpeed = 0f;
        bool foundSound = false;

        for (int i = 0; i < collidersInRange.Length; i++)
        {
            SoundController sc = collidersInRange[i].GetComponent<SoundController>();

            if (sc != null)
            {
                float speed = sc.GetVelocity().magnitude;
                if (speed < minVelocityThreshold) continue;
                if (speed > maxSpeed)
                {
                    maxSpeed = speed;
                    loudest = sc;
                    foundSound = true;
                }
            }
        }

        if (foundSound && loudest != null)
        {
            return loudest.transform.position;
        }

        return null;
    }

    void PatrolBehavior()
    {
        if (isWaiting)
        {
            patrolTimer -= Time.deltaTime;
            if (patrolTimer <= 0f)
            {
                isWaiting = false;
                patrolPoint = GetRandomPatrolPoint();
                TrySetDestination(patrolPoint);
            }
            return;
        }

        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
        {
            isWaiting = true;
            patrolTimer = Random.Range(minPatrolInterval, maxPatrolInterval);
            agent.ResetPath();
        }

        if (debugMode) Debug.Log("Patrol");
    }

    void ChaseBehavior(GameObject visibleTarget)
    {
        if (visibleTarget == null)
        {
            TrySetDestination(lastKnownTargetPos);
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                enemyState = EntityState.Search;
            }
            return;
        }

        lastKnownTargetPos = visibleTarget.transform.position;
        float distanceToTarget = Vector3.Distance(transform.position, lastKnownTargetPos);

        if (distanceToTarget <= agent.stoppingDistance + 0.2f)
        {
            enemyState = EntityState.Attack;
            agent.ResetPath();
        }
        else
        {
            TrySetDestination(lastKnownTargetPos);
        }
    }

    void InvestigateBehavior()
    {
        TrySetDestination(lastKnownTargetPos);
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            investigateTimer -= Time.deltaTime;

            if (investigateTimer <= 0f)
            {
                enemyState = EntityState.Patrol;
            }
        }
        else
        {
            TrySetDestination(lastKnownTargetPos);
        }

        if (debugMode) Debug.Log("Investigate");
    }

    void AttackBehavior()
    {
        if (currentTarget == null)
        {
            enemyState = EntityState.Search;
            combat.combatActive = false;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distanceToTarget >= attackRange * 1.2f)
        {
            enemyState = EntityState.Sprint;
            combat.combatActive = false;
            return;
        }

        RotateTowardsTarget(currentTarget.transform.position);

        // Ustaw szablon ataku jeśli nie ustawiony
        if (combat.currentAttack == null && combat.attackTemplates.Count > 0)
        {
            combat.currentAttack = combat.attackTemplates[0];
        }

        // Wystarczy ustawić combatActive = true — Combat.FixedUpdate
        // automatycznie odpala atak gdy cooldown minął i nie ma ataku w toku.
        // Nie trzeba ręcznie zarządzać canAttack.
        combat.combatActive = true;
    }

    Vector3 GetRandomPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * patrolRange;
        randomDir += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, patrolRange, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position;
    }

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
        if (debugMode)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, viewDistance);

            Vector3 left = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
            Vector3 right = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, left * viewDistance);
            Gizmos.DrawRay(transform.position, right * viewDistance);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, hearingRadius);

            if (enemyState == EntityState.Search || enemyState == EntityState.Sprint)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lastKnownTargetPos, 0.5f);
                Gizmos.DrawLine(transform.position, lastKnownTargetPos);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}