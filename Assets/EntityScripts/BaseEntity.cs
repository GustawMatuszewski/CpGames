using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class BaseEntity : MonoBehaviour
{
    public KCC player;
    public NavMeshAgent agent;
    public NavMeshAgent smallAgentPrefab;
    public List<GameObject> entities = new List<GameObject>();
    public LayerMask groundMask;
    public LayerMask entityMask;
    public bool debugMode = false;
    public float followDistance = 1f;
    protected GameObject currentTarget;

    public EntityStatus status;
    public bool isDead => status != null && status.isDead;

    // -----------------------------------------------------------------------
    // OPTIMIZATION: Cache last destination. TrySetDestination skips
    // CalculatePath entirely if the new target is within this threshold.
    // Prevents 50 path recalculations/sec during chase when target barely moved.
    private Vector3 _lastSetDestination = Vector3.positiveInfinity;
    private const float DEST_RECALC_THRESHOLD = 0.6f;
    // -----------------------------------------------------------------------

    public enum MentalState
    {
        None,
        Neutral,
        Courious,
        Interested,
        Scared,
        Terrified,
        Aggresive,
        SuperAggresive,
        Friendly,
        Hurt
    }

    public enum EntityState
    {
        None,
        Walk,
        Sprint,
        Jump,
        Dash,
        Attack,
        Hit,
        Patrol,
        Search,
        Crawl,
        Prone
    }

    public enum EntityFaction
    {
        Enemy,
        Player,
        Neutral
    }

    public EntityFaction faction = EntityFaction.Neutral;

    public void DetectEntitiesInSphere(Vector3 origin, float radius, LayerMask entityMask, LayerMask groundMask, List<GameObject> entitiesList)
    {
        Collider[] hits = Physics.OverlapSphere(origin, radius, entityMask);
        List<GameObject> currentEntities = new List<GameObject>();

        foreach (Collider col in hits)
        {
            GameObject topParent = GetTopParent(col.gameObject);
            if (topParent == null) continue;
            if (topParent == this.gameObject) continue;

            currentEntities.Add(topParent);
            if (!entitiesList.Contains(topParent))
                entitiesList.Add(topParent);
        }

        for (int i = entitiesList.Count - 1; i >= 0; i--)
        {
            if (!currentEntities.Contains(entitiesList[i]))
                entitiesList.RemoveAt(i);
        }
    }

    GameObject GetTopParent(GameObject obj)
    {
        Transform current = obj.transform;
        GameObject foundEntity = null;

        if (current.GetComponent<EntityStatus>() != null)
            foundEntity = current.gameObject;

        while (current.parent != null)
        {
            current = current.parent;
            if (current.GetComponent<EntityStatus>() != null)
                foundEntity = current.gameObject;
        }

        return foundEntity;
    }

    public bool TrySetDestination(Vector3 target, bool useSmallerCollider = false, bool moveToNearest = true)
    {
        if (agent == null || !agent.isOnNavMesh) return false;

        // 1. If we already have a path and it's calculated...
        if (agent.hasPath && !agent.pathPending)
        {
            float distToTarget = Vector3.Distance(agent.destination, target);
            float yDiff = Mathf.Abs(agent.destination.y - target.y); // NEW: Check height difference

            // If target moved less than 2m AND height barely changed
            if (distToTarget < 2.0f && yDiff < 0.75f) 
                return true;
        }

        // 2. Only calculate a NEW path if the target moved substantially
        float distToLast = Vector3.Distance(target, _lastSetDestination);
        float yDiffLast = Mathf.Abs(target.y - _lastSetDestination.y); // NEW: Check height difference

        if (distToLast < 1.5f && yDiffLast < 0.75f) 
            return true;

        // 2. Only calculate a NEW path if the target moved substantially
        if (Vector3.Distance(target, _lastSetDestination) < 1.5f) // Increased from 0.6f
            return true;

        // Now do the expensive path calculation
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(target, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
            {
                agent.SetPath(path);
                _lastSetDestination = target;
                return true;
            }
        }

        if (useSmallerCollider && smallAgentPrefab != null)
        {
            // -----------------------------------------------------------------------
            // WARNING: Instantiate + Destroy every call is very expensive.
            // If you use this path often, consider pre-creating one temp agent
            // and reusing it, or use a separate NavMeshQuery approach.
            // -----------------------------------------------------------------------
            NavMeshAgent temp = Instantiate(smallAgentPrefab, agent.transform.position, agent.transform.rotation);
            temp.enabled = false;
            temp.radius  = smallAgentPrefab.radius;
            temp.height  = smallAgentPrefab.height;

            if (NavMesh.CalculatePath(temp.transform.position, target, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.radius = temp.radius;
                agent.height = temp.height;
                agent.SetDestination(target);
                Destroy(temp.gameObject);
                // -----------------------------------------------------------------------
                _lastSetDestination = target;
                // -----------------------------------------------------------------------
                return true;
            }

            Destroy(temp.gameObject);
        }

        if (moveToNearest)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(target, out hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                // -----------------------------------------------------------------------
                _lastSetDestination = hit.position;
                // -----------------------------------------------------------------------
                return true;
            }
        }

        return false;
    }

    // -----------------------------------------------------------------------
    // OPTIMIZATION: Call this when you want to force a full path recalculation
    // on the next TrySetDestination call (e.g. after teleport or obstacle change).
    public void InvalidateDestinationCache()
    {
        _lastSetDestination = Vector3.positiveInfinity;
    }
    // -----------------------------------------------------------------------

    public void FollowTarget(GameObject target)
    {
        if (target == null || agent == null)
            return;

        Vector3 targetPos = target.transform.position;
        Vector3 dir       = (transform.position - targetPos).normalized;
        targetPos        += dir * followDistance;

        TrySetDestination(targetPos);

        if (debugMode)
            Debug.Log("Following: " + target.name + " | Target Position: " + targetPos);
    }
}