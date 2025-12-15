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

    //Tu trzeba dodac 3 rzeczy 
    //public Animacja animacja wyjecia
    //public Animacja animacja uzyca
    //public Animacja animacja schowania

    public MonoBehaviour useScript;
    //to nie prefab jakos inaczej public Prefab itemModel;
    
    public CraftingRecipe craftingRecipe;

    [Header("Item use configurations ")]
    public float weight;
    public int durability;
    public int usesLeft;
    
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
        tissue,
        woolen,
        jeans,
        nylon,
        leather,
        tough,
        unknown
    }
    public MaterialType materialType;

    

}