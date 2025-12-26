using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyEntity : BaseEntity
{
    [Header("Vision settings")]
    public float viewDistance = 15f;
    [Range(0, 360)] public float viewAngle = 90f;
    public LayerMask obstacleMask;

    [Header("Hearing settings")]
    public float hearingRadius = 10f;

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
    private GameObject currentTarget;
    private bool isWaiting;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player = FindAnyObjectByType<KCC>();
    }

    void FixedUpdate()
    {
        DetectEntitiesInSphere(transform.position, viewDistance, entityMask, groundMask, entities);
        GameObject visibleTarget = CheckForVisibleTarget();

        if (visibleTarget != null)
        {
            currentTarget = visibleTarget;
            lastKnownTargetPos = currentTarget.transform.position;
            enemyState = EntityState.Sprint;
            investigateTimer = investigateTime;
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
        Vector3 dirToTarget = (target.position - transform.position);
        float distance = dirToTarget.magnitude;

        if (distance > viewDistance) return false;

        dirToTarget.Normalize();

        if (Vector3.Angle(transform.forward, dirToTarget) > viewAngle * 0.5f) return false;

        if (Physics.Raycast(transform.position + Vector3.up, dirToTarget, distance, obstacleMask)) return false;

        return true;
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
    }

    void ChaseBehavior(GameObject visibleTarget)
    {
        if (visibleTarget != null)
        {
            lastKnownTargetPos = visibleTarget.transform.position;
            TrySetDestination(lastKnownTargetPos);
        }
        else
        {
            TrySetDestination(lastKnownTargetPos);
            enemyState = EntityState.Search;
        }
    }

    void InvestigateBehavior()
    {
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

    void OnDrawGizmos()
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
    }
}