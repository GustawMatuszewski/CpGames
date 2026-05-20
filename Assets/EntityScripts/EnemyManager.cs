using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [Header("Grouping")]
    [Tooltip("Enemies within this radius share one perception leader.")]
    public float groupRadius = 8f;

    [Tooltip("How often to re-evaluate groups (seconds).")]
    public float regroupInterval = 0.5f;

    private List<EnemyEntity> allEnemies = new List<EnemyEntity>();

    // Read-only access for Window.cs (enemy path invalidation on break)
    // and any other system that needs to iterate enemies without modifying the list.
    public IReadOnlyList<EnemyEntity> AllEnemies => allEnemies;

    // ZMIANA: Kwadrat promienia grupowania – obliczany raz przy starcie,
    // eliminuje pierwiastkowanie w RegroupEnemies() (O(n²) par wrogów).
    private float _groupRadiusSq;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Prekalkulacja kwadratu – robiona raz, używana w każdej iteracji grupowania.
        _groupRadiusSq = groupRadius * groupRadius;
    }

    void Start()
    {
        StartCoroutine(RegroupLoop());
    }

    // NOWE: Jeśli groupRadius zmieni się w runtime (np. przez skrypt lub Inspector),
    // zaktualizuj też kwadrat.
    void OnValidate()
    {
        _groupRadiusSq = groupRadius * groupRadius;
    }

    public void RegisterEnemy(EnemyEntity enemy)
    {
        if (!allEnemies.Contains(enemy)) allEnemies.Add(enemy);
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
        for (int i = 0; i < allEnemies.Count; i++)
        {
            if (allEnemies[i] == null || allEnemies[i].isDead) continue;
            allEnemies[i].isGroupLeader = false;
            allEnemies[i].groupLeader   = null;
        }

        bool[] assigned = new bool[allEnemies.Count];

        for (int i = 0; i < allEnemies.Count; i++)
        {
            if (allEnemies[i] == null || assigned[i]) continue;

            EnemyEntity leader   = allEnemies[i];
            leader.isGroupLeader = true;
            assigned[i]          = true;

            for (int j = i + 1; j < allEnemies.Count; j++)
            {
                if (allEnemies[j] == null || assigned[j]) continue;

                // ZMIANA: sqrMagnitude zamiast Vector3.Distance – eliminuje sqrt
                // przy każdej parze wrogów w pętli O(n²).
                float distSq = (leader.transform.position - allEnemies[j].transform.position).sqrMagnitude;
                if (distSq > _groupRadiusSq) continue;

                allEnemies[j].isGroupLeader = false;
                allEnemies[j].groupLeader   = leader;
                assigned[j]                 = true;
            }
        }
    }

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
}