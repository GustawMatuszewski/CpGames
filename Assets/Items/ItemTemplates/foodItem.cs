using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "FoodItem", menuName = "Inventory/ItemByType/FoodItem")]
public class FoodItem : Item
{
    public enum FoodState
    {
        none,
        fresh,
        stale,
        slightlyRotten,
        rotten,
        freshCooked,
        cooked,
        staleCooked,
        slightlyRottenCooked,
        rottenCooked,
        freshRaw,
        raw,
        staleRaw,
        slightlyRottenRaw,
        rottenRaw
    }

    public enum Effect
    {
        none,
        nausea,
        poisoned,
        ill,
        diareah,
        drunk
    }

    public FoodState foodState;
    public List<Effect> effects;
    public float protein;
    public float fats;
    public float carbs;
    public float calories;
    public float nurishment;
    public float hydration;
    public float eneryBoost;

    public override void Use(Item instance, Inventory playerInventory)
    {
        EntityStatus entity = playerInventory.GetComponentInChildren<EntityStatus>();
        if (entity == null) entity = playerInventory.GetComponentInParent<EntityStatus>();

        if (entity == null)
        {
            Debug.LogWarning($"[FoodItem] No EntityStatus found on player!");
            return;
        }

        if (entity.isDead)
        {
            Debug.LogWarning($"[FoodItem] Cannot consume {itemName} — entity is dead.");
            return;
        }

        entity.Consume(this);

        Debug.Log($"[FoodItem] {itemName} consumed by {playerInventory.gameObject.name}");
    }
}