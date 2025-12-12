using UnityEngine;

[CreateAssetMenu(fileName = "FoodItem", menuName = "Inventory/ItemByType/FoodItem")]
public class FoodItem : Item
{
    public enum foodState{
        none,
        fresh,
        stale,
        slightlyRotten,
        rotten
    } 
    public float foodCalories;
}