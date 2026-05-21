using System.Collections.Generic;
using UnityEngine;

public class ZombieDeathTracker : MonoBehaviour
{
    
    public static ZombieDeathTracker Instance { get; private set; }

    [SerializeField] private List<ZombieStatsTrackerType> allDaysStats = new List<ZombieStatsTrackerType>();

    public List<ZombieStatsTrackerType> AllDaysStats => allDaysStats;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        EntityStatus.OnZombieDeath += RecordZombieDeath;
    }

    private void OnDisable()
    {
        EntityStatus.OnZombieDeath -= RecordZombieDeath;
    }

    private void RecordZombieDeath(int day)
    {
        ZombieStatsTrackerType todayStats = allDaysStats.Find(stats => stats.dayNumber == day);
        if (todayStats == null)
        {
            todayStats = new ZombieStatsTrackerType(day);
            allDaysStats.Add(todayStats);
        }

        todayStats.zombieKills++;
        Debug.Log($"[Dzień {todayStats.dayNumber}] Zabito zombie! Suma dzisiaj: {todayStats.zombieKills}");
    }
}

[System.Serializable]
public class ZombieStatsTrackerType
{
    public int dayNumber;
    public int zombieKills;
 
    public ZombieStatsTrackerType(int day)
    {
        dayNumber = day;
        zombieKills = 0;
    }
}