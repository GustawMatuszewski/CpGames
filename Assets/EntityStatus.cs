using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EntityStatus : MonoBehaviour
{
    public enum EntityType
    {
        None,
        Player,
        Enemy,
        Neutral
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

    public List<FoodItem.Effect> effects;
    private void Awake(){
        SetDefaults();
    }

    void FixedUpdate(){
        if(test){
            test=false;
            Consume(currentItem);
            
        }
    }

    public void EffectEffects(){
        
    }

    public void Consume(FoodItem itemToBeUsed){
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
    public float CalculateStat(float current, float change, float multiplier, float max)
    {
        current = Mathf.Clamp(current + change * multiplier, 0f, max);
        return current;
    }


    public float CalcMacro(float macro, float val){
        return macro + val;
    }

    public void SetDefaults(){
        entityHealth = entityMaxHealth;
        entityHunger = entityMaxHunger;
        entityThirst = entityMaxThirst;
        entitySanity = entityMaxSanity;
        entityTiredness = 0f;
        entityStamina = entityMaxStamina;

        protein = 100f;
        carbs = 100f;
        fats = 100f;
    }
}
