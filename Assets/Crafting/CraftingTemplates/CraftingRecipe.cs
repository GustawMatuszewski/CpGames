using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CraftingRecipe", menuName = "Inventory/CraftingRecipe")]
public class CraftingRecipe : ScriptableObject
{
    public int recipeID;

    public List<Item> itemsList = new List<Item>();
    public Item outcomeItem;
    public enum ToolNeeded
    {
        none,
        chisel,
        knife,
        water,
        saw,
        drill,
        heat,
        hammer
    }
    public ToolNeeded toolNeeded;
}