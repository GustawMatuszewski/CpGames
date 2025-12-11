using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public int itemID;
    public string itemName;
    public Sprite icon;
    [TextArea]
    public string description;

    public enum itemType{
        none,
        resource
    }
    public itemType type;

    public float burnCalories;
    public float foodCalories;
}