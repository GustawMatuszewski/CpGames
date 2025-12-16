using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EntityStatus : MonoBehaviour
{
    public enum EntityType
    {
        None,
        Player,
        Enemy,
        Neutral
    }

    public enum MentalState
    {
        None,
        Happy,
        Excited,
        Calm,
        Relaxed,
        Bored,
        Focused,
        Curious,
        Sad,
        Depressed,
        Angry,
        Anxious,
        Stressed,
        Lonely,
        Frustrated,
        Hungry,
        Thirsty,
        Tired,
        Sleepy
    }

    public bool debugMode;
    public bool test;
    public FoodItem currentItem;

    public EntityType entityType;

    public float entityHealth;
    public float entityHunger;
    public float entityThirst;
    public float entitySanity;
    public float entityTiredness;
    public float entityStamina;
    public float entityBodyTemp;

    public float entityMaxHealth = 100f;
    public float entityMaxHunger = 100f;
    public float entityMaxThirst = 100f;
    public float entityMaxSanity = 100f;
    public float entityMaxTiredness = 100f;
    public float entityMaxStamina = 100f;

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
    public List<MentalState> mentalStates;
    public List<MentalStateEffect> mentalEffects;

    Coroutine poisonCoroutine;
    Coroutine nauseaCoroutine;
    Coroutine illCoroutine;
    Coroutine diareahCoroutine;
    Coroutine drunkCoroutine;

    [System.Serializable]
    public class MentalStateEffect
    {
        public MentalState state;
        public float duration;
    }

    private void Awake(){
        SetDefaults();
    }

    void FixedUpdate(){
        if(test){
            test = false;
            Consume(currentItem);
            EffectEffects();
            
        }
        ApplyMentalEffects();
        UpdateMentalStatModifiers();
    }

    public void EffectEffects(){
        foreach(FoodItem.Effect applyEffects in effects){
            switch (applyEffects){
                case FoodItem.Effect.none:
                    break;

                case FoodItem.Effect.nausea:
                    if(nauseaCoroutine != null) break;
                        nauseaTime += 30f;
                        if(debugMode)
                            Debug.Log("Effect: Nausea");
                        StartCoroutine(NauseaRoutine());
                    break;

                case FoodItem.Effect.poisoned:
                    if(poisonCoroutine != null) break;
                        poisonTime += 50f;
                        if(debugMode)
                            Debug.Log("Effect: Poison");
                        StartCoroutine(PoisonRoutine());
                    break;
            }
        }
    }

    IEnumerator PoisonRoutine(){
        while(poisonTime > 0f){
            entityHealth -= 0.05f;
            poisonTime -= 1f;
            yield return new WaitForSeconds(1f);
        }

        poisonCoroutine = null;
        effects.Remove(FoodItem.Effect.poisoned);
    }

    IEnumerator NauseaRoutine(){
        float elapsed = 0f;

        while(nauseaTime > 0f){
            float t = Mathf.Clamp01(elapsed / nauseaTime);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            float offset = Mathf.Sin(Time.time * 2f) * smooth;

            nauseaTime -= Time.deltaTime;
            elapsed += Time.deltaTime;

            yield return null;
        }

        nauseaCoroutine = null;
        effects.Remove(FoodItem.Effect.nausea);
    }

    public void Consume(FoodItem itemToBeUsed){
        if(debugMode)
            Debug.Log("Consumed: " + itemToBeUsed.name);
        
        calories += itemToBeUsed.calories;
        entityHunger = CalculateStat(entityHunger, itemToBeUsed.nurishment, 1.0f, entityMaxHunger);
        entityThirst = CalculateStat(entityThirst, itemToBeUsed.hydration, 1.0f, entityMaxThirst);

        protein = CalcMacro(protein, itemToBeUsed.protein);
        carbs = CalcMacro(carbs, itemToBeUsed.carbs);
        fats = CalcMacro(fats, itemToBeUsed.fats);

        foreach(FoodItem.Effect itemsEffect in itemToBeUsed.effects){
            if(!effects.Contains(itemsEffect))
                effects.Add(itemsEffect);
        }
    }

    public void ApplyMentalEffects(){
        foreach(MentalStateEffect stateEffect in mentalEffects){
            stateEffect.duration -= Time.deltaTime;
            if(debugMode && stateEffect.duration > 0)
                Debug.Log("Mental State: " + stateEffect.state + " Duration: " + stateEffect.duration);
        }

        mentalEffects.RemoveAll(x => x.duration <= 0f);
    }

    public void AddMentalState(MentalState state, float duration){
        MentalStateEffect existing = mentalEffects.Find(x => x.state == state);
        if(existing != null){
            existing.duration = duration;
        } else {
            mentalEffects.Add(new MentalStateEffect(){ state = state, duration = duration });
        }

        if(debugMode)
            Debug.Log("Added Mental State: " + state + " Duration: " + duration);
    }

    void UpdateMentalStatModifiers(){
        float staminaModifier = 0f;
        float sanityModifier = 0f;

        foreach(MentalStateEffect stateEffect in mentalEffects){
            switch(stateEffect.state){
                case MentalState.Bored:
                    staminaModifier -= 20f;
                    break;
                case MentalState.Stressed:
                    staminaModifier += 15f;
                    sanityModifier -= 20f;
                    break;
                case MentalState.Happy:
                    sanityModifier += 10f;
                    break;
                case MentalState.Depressed:
                    staminaModifier -= 15f;
                    sanityModifier -= 25f;
                    break;
            }
        }

        entityMaxStamina = Mathf.Clamp(0f + staminaModifier, 0f, 130f);
        entitySanity = Mathf.Clamp(entitySanity + sanityModifier * Time.deltaTime, 0f, entityMaxSanity);
    }

    public float CalculateStat(float current, float change, float multiplier, float max)
    {
        current = Mathf.Clamp(current + change * multiplier, 0f, max);
        return current;
    }

    public float CalcMacro(float macro, float val){
        return macro + val;
    }

    public void SetDefaults(){
        effects = new List<FoodItem.Effect>();
        mentalStates = new List<MentalState>();
        mentalEffects = new List<MentalStateEffect>();

        entityHealth = entityMaxHealth;
        entityHunger = entityMaxHunger;
        entityThirst = entityMaxThirst;
        entitySanity = entityMaxSanity;
        entityTiredness = 0f;
        entityStamina = entityMaxStamina;
        entityBodyTemp = 36.6f;

        protein = 100f;
        carbs = 100f;
        fats = 100f;
    }
}
