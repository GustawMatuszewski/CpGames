using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FirePit : MonoBehaviour
{
    [Header("References")]
    public Item fireStarter;
    public Item starterFuel;

    public Inventory inventory;

    [Header("Fire")]
    public bool isLit = false;
    public bool attemptToLight = false;

    [Header("Fire Conditions")]
    public float windSpeed = 0f;
    public float dampness = 0f;

    [Header("Fire Values")]
    public float fireTemp = 0f;
    public float smoke = 0f;
    public float calories = 0f;
    public float caloriesBurnRate = .01f;

    private float chanceToBeLit = 151;
    private float prepPoints = 0f;
    private float chanceToKeepStarter = 25f;

    void FixedUpdate()
    {
        if (attemptToLight){
            firePreping();
            fireStarting();

            CollectFuelFromInventory();

            if (fireStarter != null){
                float keepRoll = Random.Range(0f, 100f);
                if (keepRoll > chanceToKeepStarter)
                    fireStarter = null;
            }

            if (starterFuel != null){
                calories += starterFuel.burnCalories;
                starterFuel = null;
            }

            attemptToLight = false;
        }

        fireStats();
    }

    void CollectFuelFromInventory()
    {
        if (inventory == null || inventory.inventory.Count == 0)
            return;

        List<Item> toRemove = new List<Item>();

        foreach (Item item in inventory.inventory){
            if (item == null)
                continue;

            if (item.burnCalories > 0){
                calories += item.burnCalories;
                toRemove.Add(item);
            }
            else{
                StartCoroutine(RemoveNonFuelItem(item, 5f));
            }
        }

        foreach (Item item in toRemove){
            inventory.inventory.Remove(item);
        }
    }

    IEnumerator RemoveNonFuelItem(Item item, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (item != null)
            inventory.inventory.Remove(item);
    }

    void fireStarting()
    {
        float windPenalty = windSpeed * 2f;
        float dampPenalty = dampness * 1.5f;

        float roll = Random.Range(0, chanceToBeLit + windPenalty + dampPenalty);

        bool canStartFromExisting = calories > 0;

        if (roll <= prepPoints && fireStarter != null){
            isLit = true;
            return;
        }

        if (roll <= prepPoints && canStartFromExisting && fireStarter != null){
            isLit = true;
        }
    }

    void firePreping()
    {
        if (fireStarter != null)
            prepPoints += fireStarter.burnCalories / 10;

        if (starterFuel != null)
            prepPoints += starterFuel.burnCalories / 40;

        prepPoints -= Random.Range(5f, 10f);
        prepPoints -= windSpeed * 0.5f;
        prepPoints -= dampness * 0.5f;

        prepPoints = Mathf.Clamp(prepPoints, 0f, 100f);
    }

    void fireStats()
    {
        if (isLit){
            float windBurn = caloriesBurnRate + (windSpeed * 0.01f);
            calories -= windBurn;

            fireTemp = 50f + windSpeed * 3f + calories * 0.01f + 10f;

            smoke = dampness * 5f + Random.Range(0f, 2f);
        }
        else{
            fireTemp = 0f;
            smoke = dampness;
        }

        if (calories <= 0)
            isLit = false;
    }
}
