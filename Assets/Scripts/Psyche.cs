using UnityEngine;

public class Psyche : MonoBehaviour
{
    [Header("Psyche Settings")]
    [Tooltip("Current mental state (0 = Broken, 100 = Perfect)")]
    public float mentalHealth = 100f;
    public float baseRecoveryRate = 0.1f;

    [Header("Hunger Influence")]
    public HungerSystem hungerSystem;
    [Tooltip("Threshold below which psyche starts to drop")]
    public float hungerThreshold = 20f;
    [Tooltip("How fast psyche drops when starving")]
    public float psycheDrainRate = 1.5f;

    [Header("Environment Influence")]
    public EnvironmentManager environmentManager;
    [Tooltip("Temperature above which psyche starts to drop")]
    public float maxTemperature = 30f;
    [Tooltip("Temperature below which psyche starts to drop")]
    public float minTemperature = 0f;
    [Tooltip("Humidity above which psyche starts to drop")]
    public float maxHumidity = 80f;
    [Tooltip("How fast psyche drops in bad temperature")]
    public float temperatureDrainRate = 0.5f;
    [Tooltip("How fast psyche drops in high humidity")]
    public float humidityDrainRate = 0.3f;

    [Header("Enemy Detection (FOV)")]
    public string enemyName = "Enemy";
    public float detectionRange = 15f;
    [Range(0f, 360f)]
    public float fieldOfView = 90f;
    public LayerMask obstacleMask;
    public float enemyDrainRate = 1f;

    [Header("Depression Settings")]
    public float depressionThreshold = 25f;
    public float depressionDelay = 120f;
    [Tooltip("How fast psyche drops when depressed")]
    public float depressionDrainRate = 0.2f;

    [Header("Health Influence")]
    public EntityStatus entityStatus;
    [Tooltip("Health below which psyche starts to drop")]
    public float healthThreshold = 50f;
    [Tooltip("How fast psyche drops when badly injured")]
    public float injuryDrainRate = 0.5f;
    [Tooltip("How fast health drops when depressed")]
    public float depressionHealthDrainRate = 0.1f;

    private float lowPsycheTimer = 0f;
    public bool isDepressed = false;
    private bool isEnemyVisible = false;

    void Start()
    {
        if (hungerSystem == null) hungerSystem = GetComponent<HungerSystem>();
        if (environmentManager == null) environmentManager = FindObjectOfType<EnvironmentManager>();
        if (entityStatus == null) entityStatus = GetComponent<EntityStatus>();
    }

    void Update()
    {
        if (hungerSystem == null) return;

        bool isHungerOk = hungerSystem.currentHunger >= hungerThreshold;
        bool isTempOk = true;
        bool isHumidityOk = true;
        isEnemyVisible = CheckEnemyInFOV();

        // --- Temperatura i wilgotnoœæ ---
        if (environmentManager != null)
        {
            float temp = environmentManager.currentTemperature;
            float humidity = environmentManager.currentHumidity;

            if (temp > maxTemperature)
            {
                float heatExcess = (temp - maxTemperature) / maxTemperature;
                mentalHealth -= temperatureDrainRate * heatExcess * Time.deltaTime;
                isTempOk = false;
            }
            else if (temp < minTemperature)
            {
                float coldExcess = (minTemperature - temp) / Mathf.Abs(minTemperature + 1f);
                mentalHealth -= temperatureDrainRate * coldExcess * Time.deltaTime;
                isTempOk = false;
            }

            if (humidity > maxHumidity)
            {
                float humidityExcess = (humidity - maxHumidity) / (100f - maxHumidity);
                mentalHealth -= humidityDrainRate * humidityExcess * Time.deltaTime;
                isHumidityOk = false;
            }
        }

        // --- G³ód ---
        if (!isHungerOk)
        {
            float hungerDeficit = 1f - (hungerSystem.currentHunger / hungerThreshold);
            mentalHealth -= psycheDrainRate * hungerDeficit * Time.deltaTime;
        }

        // --- Przeciwnik w polu widzenia ---
        if (isEnemyVisible)
            mentalHealth -= enemyDrainRate * Time.deltaTime;

        // --- Wp³yw zdrowia na psychikê ---
        if (entityStatus != null)
        {
            // Niskie zdrowie obni¿a psychikê
            if (entityStatus.entityHealth < healthThreshold)
            {
                float healthDeficit = 1f - (entityStatus.entityHealth / healthThreshold);
                mentalHealth -= injuryDrainRate * healthDeficit * Time.deltaTime;
            }
        }

        // Regeneracja TYLKO gdy wszystko jest ok
        if (isHungerOk && isTempOk && isHumidityOk && !isEnemyVisible && mentalHealth < 100f &&
            (entityStatus == null || entityStatus.entityHealth >= healthThreshold))
        {
            mentalHealth += baseRecoveryRate * Time.deltaTime;
        }

        mentalHealth = Mathf.Clamp(mentalHealth, 0f, 100f);

        // --- Depresja ---
        HandleDepressionTimer();

        // --- Wp³yw depresji na zdrowie i EntityStatus ---
        HandleDepressionEffects();

        HandlePsycheEffects();
    }

    void HandleDepressionTimer()
    {
        if (mentalHealth < depressionThreshold)
        {
            lowPsycheTimer += Time.deltaTime;

            if (lowPsycheTimer >= depressionDelay && !isDepressed)
            {
                isDepressed = true;

                // Dodaj nastrój Depressed do EntityStatus
                if (entityStatus != null && !entityStatus.moods.Contains(EntityStatus.Mood.Depressed))
                    entityStatus.moods.Add(EntityStatus.Mood.Depressed);

                Debug.LogWarning("Depression triggered!");
            }
        }
        else
        {
            lowPsycheTimer = 0f;
            if (isDepressed)
            {
                isDepressed = false;

                // Usuñ nastrój Depressed z EntityStatus
                if (entityStatus != null)
                    entityStatus.moods.Remove(EntityStatus.Mood.Depressed);

                Debug.Log("Depression lifted.");
            }
        }
    }

    void HandleDepressionEffects()
    {
        if (!isDepressed) return;

        // Depresja obni¿a psychikê
        mentalHealth -= depressionDrainRate * Time.deltaTime;

        // Depresja obni¿a zdrowie przez EntityStatus
        if (entityStatus != null)
        {
            entityStatus.entityHealth -= depressionHealthDrainRate * Time.deltaTime;
            entityStatus.entityHealth = Mathf.Clamp(entityStatus.entityHealth, 0f, entityStatus.entityMaxHealth);
        }
    }

    bool CheckEnemyInFOV()
    {
        Collider[] allInRange = Physics.OverlapSphere(transform.position, detectionRange);

        foreach (Collider col in allInRange)
        {
            if (col.gameObject == this.gameObject) continue;
            if (!col.gameObject.name.Contains(enemyName)) continue;

            Vector3 directionToEnemy = (col.transform.position - transform.position).normalized;
            float angleToEnemy = Vector3.Angle(transform.forward, directionToEnemy);

            if (angleToEnemy < fieldOfView / 2f)
            {
                float distanceToEnemy = Vector3.Distance(transform.position, col.transform.position);
                if (!Physics.Raycast(transform.position, directionToEnemy, distanceToEnemy, obstacleMask))
                    return true;
            }
        }

        return false;
    }

    void HandlePsycheEffects()
    {
        if (mentalHealth <= 0)
            Debug.LogWarning("Character is mentally broken!");

        if (environmentManager != null)
        {
            if (environmentManager.currentTemperature > maxTemperature)
                Debug.LogWarning("Too hot! Psyche dropping.");
            else if (environmentManager.currentTemperature < minTemperature)
                Debug.LogWarning("Too cold! Psyche dropping.");
            if (environmentManager.currentHumidity > maxHumidity)
                Debug.LogWarning("Too humid! Psyche dropping.");
        }

        if (isEnemyVisible)
            Debug.LogWarning("Enemy spotted! Psyche dropping.");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Vector3 leftFOV = Quaternion.Euler(0, -fieldOfView / 2f, 0) * transform.forward;
        Vector3 rightFOV = Quaternion.Euler(0, fieldOfView / 2f, 0) * transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, leftFOV * detectionRange);
        Gizmos.DrawRay(transform.position, rightFOV * detectionRange);
    }
}