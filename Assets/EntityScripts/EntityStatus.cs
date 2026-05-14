using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EntityStatus : MonoBehaviour
{
    public enum EntityType { None, Player, Enemy, Neutral }

    public enum Mood
    {
        None, Happy, Excited, Calm, Relaxed, Bored, Focused, Curious,
        Sad, Depressed, Angry, Anxious, Stressed, Lonely, Frustrated,
        Hungry, Thirsty, Tired, Sleepy
    }

    public enum DepressionState { None, Depression, DeepDepression, Suicidal, Dead }

    [Header("Debug Mode!!!!")]
    public bool debugMode;
    public bool test;
    public FoodItem currentItem;

    [Header("References")]
    public Combat combat;
    public EnvironmentManager environmentManager;

    // ── HIT REACTION ────────────────────────────────────────────────────────
    [Header("Hit Reaction")]
    public string hitReactionAnim = "HitReaction"; // nazwa animacji w Animatorze
    public float hitReactionCooldown = 0.3f;       // min czas między kolejnymi hit reaction
    private PlayerAnimationsController animCtrl;
    private float hitReactionTimer = 0f;
    // ────────────────────────────────────────────────────────────────────────

    [Header("Entity Settings")]
    public EntityType entityType;
    public float entityMaxHealth = 100f;
    public float entityMaxHunger = 100f;
    public float entityMaxThirst = 100f;
    public float entityMaxSanity = 100f;
    public float entityMaxTiredness = 100f;
    public float entityMaxStamina = 100f;

    [Header("Modules")]
    public bool useDecay = true;
    public bool useStamina = true;
    public bool useMoods = true;
    public bool usePsyche = true;
    public bool useEnemyDetection = true;
    public bool useDayNight = true;
    public bool useLimbTracker = true;
    public bool useEffects = true;

    [Header("Decay (in-game days to empty/full)")]
    public float daysToEmptyHunger = 2f;
    public float daysToEmptyThirst = 1f;
    public float daysToFullTiredness = 1.5f;
    public float daysToEmptyProtein = 3f;
    public float daysToEmptyFats = 4f;
    public float daysToEmptyCarbs = 2.5f;

    float hungerDecayRate;
    float thirstDecayRate;
    float tirednessGainRate;
    float proteinDecayRate;
    float fatsDecayRate;
    float carbsDecayRate;

    [Header("Day / Night")]
    public float nightTirednessMultiplier = 2.5f;
    public float nightSleepyForceThreshold = 60f;

    [Header("Stamina")]
    public float staminaDrainSprint = 12f;
    public float staminaDrainRun = 5f;
    public float staminaRegenRate = 8f;
    public float staminaRegenDelay = 1.5f;
    public float staminaSprintUnlockThreshold = 20f;

    [Header("Mood Thresholds")]
    public float hungryThreshold = 30f;
    public float thirstyThreshold = 30f;
    public float tiredThreshold = 70f;
    public float sleepyThreshold = 90f;
    public float depressedSanity = 25f;

    [Header("Psyche Settings")]
    public float mentalHealth = 100f;
    public float baseRecoveryRate = 0.1f;
    public float maxTemperature = 30f;
    public float minTemperature = 0f;
    public float maxHumidity = 80f;
    public float temperatureDrainRate = 0.5f;
    public float humidityDrainRate = 0.3f;
    public float psycheHungerDrainRate = 1.5f;
    public float psycheThirstDrainRate = 0.8f;
    public float psycheStaminaDrainRate = 0.6f;
    public float psycheStaminaThreshold = 20f;
    public float injuryDrainRate = 0.5f;
    public float healthThreshold = 50f;
    public float enemyDrainRate = 1f;

    [Header("Enemy Detection (FOV)")]
    public float detectionRange = 15f;
    [Range(0f, 360f)]
    public float fieldOfView = 90f;
    public LayerMask obstacleMask;
    public float fovCheckInterval = 0.3f;

    [Header("Depression Settings")]
    public float depressionThreshold = 34f;
    public float deepDepressionThreshold = 24f;
    public float suicidalThreshold = 5f;
    public float depressionDelay = 900f;
    public float deepDepressionExitTime = 1800f;
    public DepressionState currentDepressionState = DepressionState.None;

    [Header("Antidepressants")]
    public bool hasAntidepressants = false;
    public int antidepressantUsesThisWeek = 0;

    [Header("Entity Outputs")]
    public float entityHealth;
    public float entityHunger;
    public float entityThirst;
    public float entitySanity;
    public float entityTiredness;
    public float entityStamina;
    public float entityBodyTemp;

    public float protein;
    public float fats;
    public float carbs;
    public float calories;

    public float nauseaTime;
    public float poisonTime;
    public float illTime;
    public float diareahTime;
    public float drunkTime;

    public List<FoodItem.Effect> effects;
    public List<Mood> moods;
    protected List<Combat.Limb> limbs;

    public float SpeedMultiplier { get; private set; } = 1f;
    public bool CanSprint { get; private set; } = true;
    public KCC.State currentKCCState { get; private set; } = KCC.State.Idle;
    public bool isDead { get; private set; }

    Coroutine poisonCoroutine;
    Coroutine nauseaCoroutine;
    Coroutine illCoroutine;
    Coroutine diareahCoroutine;
    Coroutine drunkCoroutine;

    float staminaRegenDelayTimer;
    bool staminaExhausted;
    bool isEnemyVisible;
    float fovCheckTimer;

    float lowPsycheTimer;
    float aboveFiftyTimer;
    float suicidalDropItemTimer;
    float suicidalFreezeTimer;
    bool isFrozen;
    float freezeDuration;

    float antidepressantCooldown;
    bool antidepressantActive;
    float antidepressantTimer;

    HashSet<Mood> moodSet = new HashSet<Mood>();

    private void Awake()
    {
        SetDefaults();
        animCtrl = GetComponent<PlayerAnimationsController>();

        // Podpinamy hit reaction pod event onDamageReceived z Combat
        if (combat != null)
            combat.onDamageReceived.AddListener(PlayHitReaction);
    }

    // ── HIT REACTION ────────────────────────────────────────────────────────
    public void PlayHitReaction()
    {
        if (isDead) return;
        if (string.IsNullOrEmpty(hitReactionAnim)) return;

        // Cooldown — żeby animacja nie resetowała się przy każdym ticku
        if (hitReactionTimer > 0f) return;
        hitReactionTimer = hitReactionCooldown;

        if (animCtrl != null)
            animCtrl.PlayCombatAnimation(hitReactionAnim);
    }
    // ────────────────────────────────────────────────────────────────────────

    void FixedUpdate()
    {
        if (isDead) return;

        // Odliczaj cooldown hit reaction
        if (hitReactionTimer > 0f)
            hitReactionTimer -= Time.fixedDeltaTime;

        if (test)
        {
            test = false;
            Consume(currentItem);
        }

        float dt = Time.fixedDeltaTime;

        if (useDecay)    DecayStats(dt);
        if (useStamina)  UpdateStamina(dt);
        if (usePsyche && entityType == EntityType.Player) UpdatePsyche(dt);
        UpdateSpeedModifier();
        if (useMoods)    UpdateMoods();
        if (useLimbTracker) LimbTracker();
        CheckDeath();
    }

    public void ReportState(KCC.State state)
    {
        currentKCCState = state;
    }

    void UpdateStamina(float dt)
    {
        bool isSprinting = currentKCCState == KCC.State.Sprint;
        bool isRunning   = currentKCCState == KCC.State.Run;

        if (isSprinting && entityStamina > 0f)
        {
            entityStamina = Mathf.Clamp(entityStamina - staminaDrainSprint * dt, 0f, entityMaxStamina);
            staminaRegenDelayTimer = staminaRegenDelay;
        }
        else if (isRunning && entityStamina > 0f)
        {
            entityStamina = Mathf.Clamp(entityStamina - staminaDrainRun * dt, 0f, entityMaxStamina);
            staminaRegenDelayTimer = staminaRegenDelay;
        }
        else
        {
            if (staminaRegenDelayTimer > 0f)
                staminaRegenDelayTimer -= dt;
            else if (!moodSet.Contains(Mood.Sleepy))
            {
                float regenScale = Mathf.Clamp01(Mathf.Min(entityHunger / entityMaxHunger, entityThirst / entityMaxThirst) * 2f);
                entityStamina = Mathf.Clamp(entityStamina + staminaRegenRate * regenScale * dt, 0f, entityMaxStamina);
            }
        }

        if (entityStamina <= 0f) staminaExhausted = true;
        if (staminaExhausted && entityStamina >= staminaSprintUnlockThreshold) staminaExhausted = false;

        CanSprint = !staminaExhausted;
    }

    void UpdatePsyche(float dt)
    {
        if (isFrozen)
        {
            freezeDuration -= dt;
            if (freezeDuration <= 0f) isFrozen = false;
            return;
        }

        if (useEnemyDetection)
        {
            fovCheckTimer -= dt;
            if (fovCheckTimer <= 0f)
            {
                isEnemyVisible = CheckEnemyInFOV();
                fovCheckTimer  = fovCheckInterval;
            }
        }

        bool isTempOk     = true;
        bool isHumidityOk = true;

        if (environmentManager != null)
        {
            float temp     = environmentManager.currentTemperature;
            float humidity = environmentManager.currentHumidity;

            if (temp > maxTemperature)
            {
                mentalHealth -= temperatureDrainRate * ((temp - maxTemperature) / maxTemperature) * dt;
                isTempOk = false;
            }
            else if (temp < minTemperature)
            {
                mentalHealth -= temperatureDrainRate * ((minTemperature - temp) / Mathf.Abs(minTemperature + 1f)) * dt;
                isTempOk = false;
            }

            if (humidity > maxHumidity)
            {
                mentalHealth -= humidityDrainRate * ((humidity - maxHumidity) / (100f - maxHumidity)) * dt;
                isHumidityOk = false;
            }
        }

        if (entityHunger < hungryThreshold)
            mentalHealth -= psycheHungerDrainRate * (1f - entityHunger / hungryThreshold) * dt;

        if (useEnemyDetection && isEnemyVisible)
            mentalHealth -= enemyDrainRate * dt;

        if (entityHealth < healthThreshold)
            mentalHealth -= injuryDrainRate * (1f - entityHealth / healthThreshold) * dt;

        if (entityThirst < thirstyThreshold)
            mentalHealth -= psycheThirstDrainRate * (1f - entityThirst / thirstyThreshold) * dt;

        if (entityStamina < psycheStaminaThreshold)
            mentalHealth -= psycheStaminaDrainRate * (1f - entityStamina / psycheStaminaThreshold) * dt;

        if (antidepressantActive) HandleAntidepressantEffect(dt);
        if (antidepressantCooldown > 0f) antidepressantCooldown -= dt;

        bool allOk = isTempOk && isHumidityOk &&
                     (!useEnemyDetection || !isEnemyVisible) &&
                     entityHealth  >= healthThreshold &&
                     entityHunger  >= hungryThreshold &&
                     entityThirst  >= thirstyThreshold &&
                     entityStamina >= psycheStaminaThreshold;

        if (allOk && mentalHealth < 100f)
            mentalHealth += baseRecoveryRate * (mentalHealth >= 83f ? 1f : 0.5f) * dt;

        mentalHealth = Mathf.Clamp(mentalHealth, 0f, 100f);
        entitySanity = mentalHealth;

        UpdateDepressionState(dt);
    }

    void UpdateDepressionState(float dt)
    {
        if (mentalHealth <= 0f) { currentDepressionState = DepressionState.Dead; return; }
        if (mentalHealth <= suicidalThreshold) { currentDepressionState = DepressionState.Suicidal; HandleSuicidalState(dt); return; }
        if (mentalHealth <= deepDepressionThreshold) { currentDepressionState = DepressionState.DeepDepression; aboveFiftyTimer = 0f; return; }
        if (mentalHealth <= depressionThreshold)
        {
            lowPsycheTimer += dt;
            if (lowPsycheTimer >= depressionDelay) currentDepressionState = DepressionState.Depression;
            return;
        }
        if (currentDepressionState == DepressionState.DeepDepression)
        {
            aboveFiftyTimer += dt;
            if (aboveFiftyTimer >= deepDepressionExitTime) { currentDepressionState = DepressionState.None; aboveFiftyTimer = 0f; }
            return;
        }
        lowPsycheTimer = 0f;
        currentDepressionState = DepressionState.None;
    }

    void HandleSuicidalState(float dt)
    {
        suicidalDropItemTimer += dt;
        if (suicidalDropItemTimer >= 180f) { suicidalDropItemTimer = 0f; }
        suicidalFreezeTimer += dt;
        if (suicidalFreezeTimer >= 600f) { suicidalFreezeTimer = 0f; isFrozen = true; freezeDuration = 15f; }
    }

    public void UseAntidepressants()
    {
        if (!hasAntidepressants || antidepressantCooldown > 0f) return;
        antidepressantActive   = true;
        antidepressantTimer    = 0f;
        antidepressantCooldown = 480f + (antidepressantUsesThisWeek * 600f);
        antidepressantUsesThisWeek++;
    }

    void HandleAntidepressantEffect(float dt)
    {
        antidepressantTimer += dt;
        float effectDuration = 480f + (antidepressantUsesThisWeek * 600f);

        if (currentDepressionState == DepressionState.None)
        {
            mentalHealth -= ((mentalHealth - 25f) / 3600f) * dt;
            mentalHealth  = Mathf.Max(mentalHealth, 25f);
        }
        else
        {
            mentalHealth = Mathf.Clamp(mentalHealth + (25f / effectDuration) * dt, 0f, 100f);
        }

        if (antidepressantTimer >= effectDuration) { antidepressantActive = false; antidepressantTimer = 0f; }
    }

    bool CheckEnemyInFOV()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
        float halfFOV   = fieldOfView * 0.5f;
        Vector3 pos     = transform.position;
        Vector3 fwd     = transform.forward;

        foreach (Collider col in hits)
        {
            if (col.gameObject == gameObject) continue;
            EntityStatus otherStatus = col.GetComponentInParent<EntityStatus>();
            if (otherStatus == null || otherStatus.entityType != EntityType.Enemy) continue;
            Vector3 dir = (col.transform.position - pos).normalized;
            if (Vector3.Angle(fwd, dir) < halfFOV)
            {
                float dist = Vector3.Distance(pos, col.transform.position);
                if (!Physics.Raycast(pos, dir, dist, obstacleMask)) return true;
            }
        }
        return false;
    }

    void UpdateSpeedModifier()
    {
        float modifier = 1f;

        if (useDecay)
        {
            if (entityTiredness >= tiredThreshold)
                modifier *= Mathf.Lerp(1f, 0.6f, Mathf.InverseLerp(tiredThreshold, entityMaxTiredness, entityTiredness));
            if (entityHunger <= hungryThreshold)
                modifier *= Mathf.Lerp(1f, 0.85f, Mathf.InverseLerp(hungryThreshold, 0f, entityHunger));
            if (entityThirst <= thirstyThreshold)
                modifier *= Mathf.Lerp(1f, 0.80f, Mathf.InverseLerp(thirstyThreshold, 0f, entityThirst));
        }

        if (useStamina && staminaExhausted) modifier *= 0.75f;

        if (usePsyche && entityType == EntityType.Player)
        {
            if      (currentDepressionState == DepressionState.Depression)     modifier *= 0.9f;
            else if (currentDepressionState == DepressionState.DeepDepression) modifier *= 0.7f;
            else if (currentDepressionState == DepressionState.Suicidal)       modifier *= 0.5f;
        }

        SpeedMultiplier = modifier;
    }

    void DecayStats(float dt)
    {
        float tirednessScale = 1f;
        if (useDayNight && environmentManager != null)
        {
            float s = Mathf.Clamp01((environmentManager.currentTime - 6f) / 2f) * Mathf.Clamp01((18f - environmentManager.currentTime) / 2f);
            tirednessScale = Mathf.Lerp(nightTirednessMultiplier, 1f, s);
        }

        entityHunger = Mathf.Clamp(entityHunger - hungerDecayRate * dt, 0f, entityMaxHunger);
        entityThirst = Mathf.Clamp(entityThirst - thirstDecayRate * dt, 0f, entityMaxThirst);

        float tiredBoost = Mathf.Max(
            entityHunger <= hungryThreshold  ? Mathf.Lerp(1f, 2.5f, Mathf.InverseLerp(hungryThreshold,  0f, entityHunger)) : 1f,
            entityThirst <= thirstyThreshold ? Mathf.Lerp(1f, 3f,   Mathf.InverseLerp(thirstyThreshold, 0f, entityThirst)) : 1f
        );
        entityTiredness = Mathf.Clamp(entityTiredness + tirednessGainRate * tirednessScale * tiredBoost * dt, 0f, entityMaxTiredness);

        protein = Mathf.Clamp(protein - proteinDecayRate * dt, 0f, entityMaxHunger);
        fats    = Mathf.Clamp(fats    - fatsDecayRate    * dt, 0f, entityMaxHunger);
        carbs   = Mathf.Clamp(carbs   - carbsDecayRate   * dt, 0f, entityMaxHunger);

        if (entityHunger <= hungryThreshold)
            entityHealth = Mathf.Clamp(entityHealth - Mathf.Lerp(0.01f, 0.08f, Mathf.InverseLerp(hungryThreshold, 0f, entityHunger)) * dt, 0f, entityMaxHealth);
        if (entityThirst <= thirstyThreshold)
            entityHealth = Mathf.Clamp(entityHealth - Mathf.Lerp(0.02f, 0.15f, Mathf.InverseLerp(thirstyThreshold, 0f, entityThirst)) * dt, 0f, entityMaxHealth);
    }

    void UpdateMoods()
    {
        SetMood(Mood.Hungry,    entityHunger    <= hungryThreshold);
        SetMood(Mood.Thirsty,   entityThirst    <= thirstyThreshold);
        SetMood(Mood.Depressed, entitySanity    <= depressedSanity);
        SetMood(Mood.Stressed,  useEffects && (effects.Contains(FoodItem.Effect.ill) || effects.Contains(FoodItem.Effect.nausea)));

        bool forcedSleepy = useDayNight && environmentManager != null && !environmentManager.isDay && entityTiredness >= nightSleepyForceThreshold;
        SetMood(Mood.Sleepy, entityTiredness >= sleepyThreshold || forcedSleepy);
        SetMood(Mood.Tired,  entityTiredness >= tiredThreshold && !moodSet.Contains(Mood.Sleepy));
    }

    void SetMood(Mood mood, bool condition)
    {
        if (condition) { if (moodSet.Add(mood)) moods.Add(mood); }
        else           { if (moodSet.Remove(mood)) moods.Remove(mood); }
    }

    public void EffectEffects()
    {
        if (!useEffects) return;
        foreach (FoodItem.Effect applyEffects in effects)
        {
            switch (applyEffects)
            {
                case FoodItem.Effect.none: break;
                case FoodItem.Effect.nausea:
                    if (nauseaCoroutine != null) break;
                    nauseaTime += 30f;
                    nauseaCoroutine = StartCoroutine(NauseaRoutine());
                    break;
                case FoodItem.Effect.poisoned:
                    if (poisonCoroutine != null) break;
                    poisonTime += 50f;
                    poisonCoroutine = StartCoroutine(PoisonRoutine());
                    break;
                case FoodItem.Effect.ill:
                    if (illCoroutine != null) break;
                    illTime += 60f;
                    illCoroutine = StartCoroutine(IllRoutine());
                    break;
                case FoodItem.Effect.diareah:
                    if (diareahCoroutine != null) break;
                    diareahTime += 40f;
                    diareahCoroutine = StartCoroutine(DiareahRoutine());
                    break;
                case FoodItem.Effect.drunk:
                    if (drunkCoroutine != null) break;
                    drunkTime += 45f;
                    drunkCoroutine = StartCoroutine(DrunkRoutine());
                    break;
            }
        }
    }

    IEnumerator PoisonRoutine()
    {
        while (poisonTime > 0f) { entityHealth = Mathf.Clamp(entityHealth - 0.05f, 0f, entityMaxHealth); poisonTime -= 1f; yield return new WaitForSeconds(1f); }
        poisonCoroutine = null;
        effects.Remove(FoodItem.Effect.poisoned);
    }

    IEnumerator NauseaRoutine()
    {
        float elapsed = 0f;
        while (nauseaTime > 0f) { nauseaTime -= Time.deltaTime; elapsed += Time.deltaTime; yield return null; }
        nauseaCoroutine = null;
        effects.Remove(FoodItem.Effect.nausea);
    }

    IEnumerator IllRoutine()
    {
        while (illTime > 0f) { entityHealth = Mathf.Clamp(entityHealth - 0.03f, 0f, entityMaxHealth); entityThirst = Mathf.Clamp(entityThirst - 2f, 0f, entityMaxThirst); illTime -= 1f; yield return new WaitForSeconds(1f); }
        illCoroutine = null;
        effects.Remove(FoodItem.Effect.ill);
    }

    IEnumerator DiareahRoutine()
    {
        while (diareahTime > 0f) { entityThirst = Mathf.Clamp(entityThirst - 3f, 0f, entityMaxThirst); entityHunger = Mathf.Clamp(entityHunger - 1f, 0f, entityMaxHunger); diareahTime -= 1f; yield return new WaitForSeconds(1f); }
        diareahCoroutine = null;
        effects.Remove(FoodItem.Effect.diareah);
    }

    IEnumerator DrunkRoutine()
    {
        while (drunkTime > 0f) { mentalHealth = Mathf.Clamp(mentalHealth - 0.2f, 0f, 100f); drunkTime -= 1f; yield return new WaitForSeconds(1f); }
        mentalHealth = Mathf.Clamp(mentalHealth + 10f, 0f, 100f);
        drunkCoroutine = null;
        effects.Remove(FoodItem.Effect.drunk);
    }

    public void Consume(FoodItem itemToBeUsed)
    {
        if (itemToBeUsed == null || isDead) return;
        if (debugMode) Debug.Log("ate: " + itemToBeUsed.name);
        calories     += itemToBeUsed.calories;
        entityHunger  = Mathf.Clamp(entityHunger + itemToBeUsed.nurishment, 0f, entityMaxHunger);
        entityThirst  = Mathf.Clamp(entityThirst  + itemToBeUsed.hydration,  0f, entityMaxThirst);
        protein      += itemToBeUsed.protein;
        carbs        += itemToBeUsed.carbs;
        fats         += itemToBeUsed.fats;
        foreach (FoodItem.Effect itemsEffect in itemToBeUsed.effects)
            if (!effects.Contains(itemsEffect)) effects.Add(itemsEffect);
        EffectEffects();
    }

    public void LimbTracker()
    {
        for (int i = 0; i < limbs.Count; i++)
        {
            Combat.Limb limb = limbs[i];
            bool crippled = limb.severed || limb.limbDamageList.Contains(Combat.Limb.DamageType.Fractured);
            if (crippled)
            {
                if (limb.health > 0f) { limb.health = 0f; if (debugMode) Debug.Log((limb.severed ? "severed" : "fractured") + ": " + limb.name); }
                if (moodSet.Add(Mood.Depressed)) moods.Add(Mood.Depressed);
            }
        }
    }

    void CheckDeath()
    {
        if (!isDead && entityHealth <= 0f)
        {
            isDead       = true;
            entityHealth = 0f;
            UI_GameOver.Instance.GameOver();
            if (debugMode) Debug.Log("[EntityStatus] " + gameObject.name + " died.");
        }
    }

    public void SetDefaults()
    {
        if (combat == null)
        {
            combat = GetComponent<Combat>();
            if (combat == null && debugMode) Debug.LogWarning("no combat: " + gameObject.name);
        }

        if (environmentManager == null)
        {
            environmentManager = Object.FindAnyObjectByType<EnvironmentManager>();
            if (environmentManager == null && debugMode) Debug.LogWarning("no env manager");
        }

        if (environmentManager != null)
        {
            float cycleTime   = 24f / environmentManager.timeSpeed;
            hungerDecayRate   = entityMaxHunger    / (daysToEmptyHunger   * cycleTime);
            thirstDecayRate   = entityMaxThirst    / (daysToEmptyThirst   * cycleTime);
            tirednessGainRate = entityMaxTiredness / (daysToFullTiredness * cycleTime);
            proteinDecayRate  = entityMaxHunger    / (daysToEmptyProtein  * cycleTime);
            fatsDecayRate     = entityMaxHunger    / (daysToEmptyFats     * cycleTime);
            carbsDecayRate    = entityMaxHunger    / (daysToEmptyCarbs    * cycleTime);
        }

        effects = new List<FoodItem.Effect>();
        moods   = new List<Mood>();
        moodSet = new HashSet<Mood>();
        limbs   = combat != null ? combat.ownerHitboxes : new List<Combat.Limb>();

        entityHealth    = entityMaxHealth;
        entityHunger    = entityMaxHunger;
        entityThirst    = entityMaxThirst;
        entitySanity    = entityMaxSanity;
        entityTiredness = 0f;
        entityStamina   = entityMaxStamina;
        entityBodyTemp  = 36.6f;

        protein  = 100f;
        carbs    = 100f;
        fats     = 100f;
        calories = 0f;

        mentalHealth           = 100f;
        currentDepressionState = DepressionState.None;
        isFrozen               = false;
        freezeDuration         = 0f;
        lowPsycheTimer         = 0f;
        aboveFiftyTimer        = 0f;
        suicidalDropItemTimer  = 0f;
        suicidalFreezeTimer    = 0f;
        antidepressantActive   = false;
        antidepressantTimer    = 0f;
        antidepressantCooldown = 0f;
        fovCheckTimer          = 0f;

        isDead           = false;
        staminaExhausted = false;
        SpeedMultiplier  = 1f;
        CanSprint        = true;
    }

    void OnDrawGizmosSelected()
    {
        if (!useEnemyDetection) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, -fieldOfView / 2f, 0) * transform.forward * detectionRange);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0,  fieldOfView / 2f, 0) * transform.forward * detectionRange);
    }
}