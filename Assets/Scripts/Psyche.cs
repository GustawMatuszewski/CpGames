using UnityEngine;

public class Psyche : MonoBehaviour
{
    [Header("Psyche Settings")]
    [Tooltip("Current mental state (0 = Broken, 100 = Perfect)")]
    public float mentalHealth = 100f;
    public float baseRecoveryRate = 0.1f;

    [Header("Hunger Influence")]
    public HungerSystem hungerSystem;
    public float hungerThreshold = 20f;
    public float psycheDrainRate = 1.5f;

    [Header("Environment Influence")]
    public EnvironmentManager environmentManager;
    public float maxTemperature = 30f;
    public float minTemperature = 0f;
    public float maxHumidity = 80f;
    public float temperatureDrainRate = 0.5f;
    public float humidityDrainRate = 0.3f;

    [Header("Enemy Detection (FOV)")]
    public string enemyName = "Enemy";
    public float detectionRange = 15f;
    [Range(0f, 360f)]
    public float fieldOfView = 90f;
    public LayerMask obstacleMask;
    public float enemyDrainRate = 1f;

    [Header("Health Influence")]
    public EntityStatus entityStatus;
    public float healthThreshold = 50f;
    public float injuryDrainRate = 0.5f;

    [Header("Thirst Influence")]
    public float thirstThreshold = 30f;
    public float thirstDrainRate = 0.8f;

    [Header("Stamina Influence")]
    public float staminaThreshold = 20f;
    public float staminaDrainRate = 0.6f;

    [Header("Depression Settings")]
    public float depressionThreshold = 34f;
    public float deepDepressionThreshold = 24f;
    public float suicidalThreshold = 5f;
    public float depressionDelay = 900f;
    public float deepDepressionExitTime = 1800f;

    [Header("Antidepressants")]
    public bool hasAntidepressants = false;
    public int antidepressantUsesThisWeek = 0;
    private float antidepressantCooldown = 0f;
    private bool antidepressantActive = false;
    private float antidepressantTimer = 0f;

    public enum DepressionState
    {
        None,
        Depression,
        DeepDepression,
        Suicidal,
        Dead
    }
    public DepressionState currentDepressionState = DepressionState.None;

    private float lowPsycheTimer = 0f;
    private float aboveFiftyTimer = 0f;
    private float suicidalDropItemTimer = 0f;
    private float suicidalFreezeTimer = 0f;
    private bool isFrozen = false;
    private float freezeDuration = 0f;

    private float staminaMultiplier = 1f;
    private float regenMultiplier = 1f;
    private float staminaMaxMultiplier = 1f;

    private bool isEnemyVisible = false;

    void Start()
    {
        if (hungerSystem == null) hungerSystem = GetComponent<HungerSystem>();
        if (environmentManager == null) environmentManager = FindObjectOfType<EnvironmentManager>();
        if (entityStatus == null) entityStatus = GetComponent<EntityStatus>();
    }

    void Update()
    {
        if (isFrozen)
        {
            freezeDuration -= Time.deltaTime;
            if (freezeDuration <= 0f)
                isFrozen = false;
            return;
        }

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

        // --- Zdrowie ---
        if (entityStatus != null && entityStatus.entityHealth < healthThreshold)
        {
            float healthDeficit = 1f - (entityStatus.entityHealth / healthThreshold);
            mentalHealth -= injuryDrainRate * healthDeficit * Time.deltaTime;
        }

        // --- Pragnienie ---
        if (entityStatus != null && entityStatus.entityThirst < thirstThreshold)
        {
            float thirstDeficit = 1f - (entityStatus.entityThirst / thirstThreshold);
            mentalHealth -= thirstDrainRate * thirstDeficit * Time.deltaTime;
        }

        // --- Stamina ---
        if (entityStatus != null && entityStatus.entityStamina < staminaThreshold)
        {
            float staminaDeficit = 1f - (entityStatus.entityStamina / staminaThreshold);
            mentalHealth -= staminaDrainRate * staminaDeficit * Time.deltaTime;
        }

        // --- Antydepresanty ---
        if (antidepressantActive)
            HandleAntidepressantEffect();

        if (antidepressantCooldown > 0f)
            antidepressantCooldown -= Time.deltaTime;

        // --- Regeneracja ---
        bool allOk = isHungerOk && isTempOk && isHumidityOk && !isEnemyVisible &&
                     (entityStatus == null || entityStatus.entityHealth >= healthThreshold) &&
                     (entityStatus == null || entityStatus.entityThirst >= thirstThreshold) &&
                     (entityStatus == null || entityStatus.entityStamina >= staminaThreshold);

        if (allOk && mentalHealth >= 83f && mentalHealth < 100f)
            mentalHealth += baseRecoveryRate * Time.deltaTime;
        else if (allOk && mentalHealth >= 50f && mentalHealth < 83f)
            mentalHealth += (baseRecoveryRate * 0.5f) * Time.deltaTime;

        mentalHealth = Mathf.Clamp(mentalHealth, 0f, 100f);

        // --- Stan depresji ---
        UpdateDepressionState();

        // --- Efekty na EntityStatus ---
        ApplyPsycheEffectsToEntity();

        HandlePsycheEffects();
    }

    void UpdateDepressionState()
    {
        if (mentalHealth <= 0f)
        {
            currentDepressionState = DepressionState.Dead;
            return;
        }

        if (mentalHealth >= 1f && mentalHealth <= suicidalThreshold)
        {
            currentDepressionState = DepressionState.Suicidal;
            HandleSuicidalState();
            return;
        }

        if (mentalHealth > suicidalThreshold && mentalHealth <= deepDepressionThreshold)
        {
            currentDepressionState = DepressionState.DeepDepression;

            if (mentalHealth > 50f)
            {
                aboveFiftyTimer += Time.deltaTime;
                if (aboveFiftyTimer >= deepDepressionExitTime)
                {
                    currentDepressionState = DepressionState.None;
                    aboveFiftyTimer = 0f;
                    Debug.Log("Wyszed³eœ z g³êbokiej depresji!");
                }
            }
            else
            {
                aboveFiftyTimer = 0f;
            }
            return;
        }

        if (mentalHealth >= 25f && mentalHealth <= 50f)
        {
            lowPsycheTimer += Time.deltaTime;
            if (lowPsycheTimer >= depressionDelay)
            {
                currentDepressionState = DepressionState.Depression;

                if (entityStatus != null && !entityStatus.moods.Contains(EntityStatus.Mood.Depressed))
                    entityStatus.moods.Add(EntityStatus.Mood.Depressed);
            }
            return;
        }

        if (mentalHealth > 50f && currentDepressionState == DepressionState.Depression)
        {
            lowPsycheTimer = 0f;
            currentDepressionState = DepressionState.None;

            if (entityStatus != null)
                entityStatus.moods.Remove(EntityStatus.Mood.Depressed);
        }

        if (mentalHealth > 50f && currentDepressionState == DepressionState.None)
            lowPsycheTimer = 0f;
    }

    void ApplyPsycheEffectsToEntity()
    {
        if (entityStatus == null) return;

        staminaMultiplier = 1f;
        regenMultiplier = 1f;
        staminaMaxMultiplier = 1f;

        if (mentalHealth > 95f)
        {
            staminaMultiplier = 1.15f;
            regenMultiplier = 1.15f;
            staminaMaxMultiplier = 1.15f;
        }
        else if (mentalHealth >= 83f)
        {
            staminaMultiplier = 1.08f;
            regenMultiplier = 1.08f;
            staminaMaxMultiplier = 1.08f;
        }
        else if (currentDepressionState == DepressionState.Depression)
        {
            staminaMaxMultiplier = 0.9f;
            regenMultiplier = 0.9f;
            staminaMultiplier = 1f / 1.1f;
        }
        else if (currentDepressionState == DepressionState.DeepDepression)
        {
            staminaMaxMultiplier = 0.7f;
            regenMultiplier = 0.6f;
            staminaMultiplier = 0.6f;
        }

        entityStatus.entityMaxStamina = 100f * staminaMaxMultiplier;
        entityStatus.entityStamina = Mathf.Clamp(entityStatus.entityStamina, 0f, entityStatus.entityMaxStamina);
    }

    void HandleSuicidalState()
    {
        suicidalDropItemTimer += Time.deltaTime;
        if (suicidalDropItemTimer >= 180f)
        {
            suicidalDropItemTimer = 0f;
            Debug.LogWarning("Suicidal: dropping held item!");
            // TODO: pod³¹cz do swojego systemu ekwipunku
        }

        suicidalFreezeTimer += Time.deltaTime;
        if (suicidalFreezeTimer >= 600f)
        {
            suicidalFreezeTimer = 0f;
            isFrozen = true;
            freezeDuration = 15f;
            Debug.LogWarning("Suicidal: character frozen for 15 seconds!");
        }
    }

    public void UseAntidepressants()
    {
        if (antidepressantCooldown > 0f)
        {
            Debug.Log("Antydepresanty s¹ na cooldownie!");
            return;
        }

        antidepressantActive = true;
        antidepressantTimer = 0f;

        float baseCooldown = 480f;
        antidepressantCooldown = baseCooldown + (antidepressantUsesThisWeek * 600f);
        antidepressantUsesThisWeek++;

        if (currentDepressionState == DepressionState.None)
            Debug.LogWarning("Antydepresanty bez depresji - psychika bêdzie spadaæ do 25!");

        Debug.Log("U¿yto antydepresantów!");
    }

    void HandleAntidepressantEffect()
    {
        antidepressantTimer += Time.deltaTime;

        float effectDuration = 480f + (antidepressantUsesThisWeek * 600f);

        if (currentDepressionState == DepressionState.None)
        {
            float targetDrop = mentalHealth - 25f;
            mentalHealth -= (targetDrop / 3600f) * Time.deltaTime;
            mentalHealth = Mathf.Max(mentalHealth, 25f);
        }
        else
        {
            mentalHealth += (25f / effectDuration) * Time.deltaTime;
            mentalHealth = Mathf.Clamp(mentalHealth, 0f, 100f);
        }

        if (antidepressantTimer >= effectDuration)
        {
            antidepressantActive = false;
            antidepressantTimer = 0f;
            Debug.Log("Dzia³anie antydepresantów skoñczy³o siê.");
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
        if (currentDepressionState == DepressionState.Dead)
            Debug.LogWarning("Postaæ jest psychicznie martwa!");

        if (currentDepressionState == DepressionState.Suicidal)
            Debug.LogWarning("Postaæ ma myœli samobójcze!");

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