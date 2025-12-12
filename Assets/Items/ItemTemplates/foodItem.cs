using UnityEngine;

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
    public FoodState foodState;
    public float calories;
    public float nurishment;
    public float hydration;
    public float eneryBoost;

    public enum Effect
    {
        none,
        nausea,
        poisoned,
        ill,
        diareah,
        drunk
    }
}