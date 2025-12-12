using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public int itemID;
    public string itemName;
    public Sprite icon;
    [TextArea]
    public string description;

    public float weight;
    public int durability;
    public int usesLeft;
    public bool craftable;
    public CraftingRecipe craftingRecipe;
    public float burnCalories;
    public enum ItemType
    {
        none,
        resource,
        food,
        tool,
        weapon,
        buildingMaterial,
        medical,
        storage,
        clothing,
        loot
    }
    public ItemType itemType;

    public enum MaterialType
    {
        none,
        wood,
        kindling,
        stone,
        metal,
        plastic,
        cloth,
        tissue,
        unknown
    }
    public MaterialType materialType;

    

}