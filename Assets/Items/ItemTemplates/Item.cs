using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("Basic Item configurations")]
    public int itemID;
    public string itemName;
    public Sprite icon;
    [TextArea]
    public string description;
    public MonoBehaviour useScript;
    public CraftingRecipe craftingRecipe;

    [Header("Item use configurations")]
    public float weight;
    public int durability;
    public int usesLeft;
    public float burnCalories;

    public enum ItemType
    {
        none, resource, food, tool, weapon, buildingMaterial, medical, storage, clothing, loot
    }
    public ItemType itemType;

    public enum MaterialType
    {
        none, wood, kindling, stone, metal, plastic, tissue, woolen, jeans, nylon, leather, tough, unknown
    }
    public MaterialType materialType;

    public virtual void Use(Item instance, Inventory playerInventory)
    {
        Debug.Log($"[Item] {itemName} has no Use() defined.");
    }
}