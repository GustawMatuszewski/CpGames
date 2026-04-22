using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// -----------------------------------------------------------------------
// NEW FILE: EnemyManager
//
// Place ONE instance of this on a GameObject in your scene (empty object
// called "EnemyManager" is fine). It groups nearby enemies so only one
// leader per group does expensive perception (OverlapSphere + raycasts).
// Followers just read the leader's result at zero cost.
//
// How it integrates:
//   - EnemyEntity.Start() calls EnemyManager.Instance.RegisterEnemy(this)
//   - EnemyEntity.OnDestroy() calls EnemyManager.Instance.UnregisterEnemy(this)
//   - EnemyManager assigns isGroupLeader and groupLeader on each enemy
//   - EnemyEntity.RunPerception() checks isGroupLeader to decide who does work
// -----------------------------------------------------------------------
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Grouping")]
    [Tooltip("Enemies within this radius share one perception leader")]
    public float groupRadius = 8f;

    [Tooltip("How often to re-evaluate groups (seconds). 0.5 is fine.")]
    public float regroupInterval = 0.5f;

    private List<EnemyEntity> allEnemies = new List<EnemyEntity>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(RegroupLoop());
    }

    public void RegisterEnemy(EnemyEntity enemy)
    {
        if (!allEnemies.Contains(enemy))
            allEnemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyEntity enemy)
    {
        allEnemies.Remove(enemy);
    }

    IEnumerator RegroupLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(regroupInterval);
            RegroupEnemies();
        }
    }

    void RegroupEnemies()
    {
        // Reset everyone
        for (int i = 0; i < allEnemies.Count; i++)
        {
            if (allEnemies[i] == null) continue;
            allEnemies[i].isGroupLeader = false;
            allEnemies[i].groupLeader   = null;
        }

        bool[] assigned = new bool[allEnemies.Count];

        for (int i = 0; i < allEnemies.Count; i++)
        {
            if (allEnemies[i] == null || assigned[i]) continue;

            // This enemy becomes the group leader
            EnemyEntity leader   = allEnemies[i];
            leader.isGroupLeader = true;
            assigned[i]          = true;

            // Find all nearby enemies and assign them to this leader
            for (int j = i + 1; j < allEnemies.Count; j++)
            {
                if (allEnemies[j] == null || assigned[j]) continue;

                float dist = Vector3.Distance(leader.transform.position, allEnemies[j].transform.position);
                if (dist <= groupRadius)
                {
                    allEnemies[j].isGroupLeader = false;
                    allEnemies[j].groupLeader   = leader;
                    assigned[j]                 = true;
                }
            }
        }
        
    }

    // -----------------------------------------------------------------------
    // DEBUG: Draw group boundaries in Scene view
    void OnDrawGizmos()
    {
        if (allEnemies == null) return;
        foreach (var e in allEnemies)
        {
            if (e == null || !e.isGroupLeader) continue;
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            Gizmos.DrawSphere(e.transform.position, groupRadius);
        }
    }
    // -----------------------------------------------------------------------
}