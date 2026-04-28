using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCraftingRecipe", menuName = "Inventory/CraftingRecipe")]
public class CraftingRecipe : ScriptableObject
{
    public int recipeID;
    public List<Item> itemsList = new List<Item>(); // Składniki
    public Item outcomeItem; // Wynik

    public enum ToolNeeded
    {
        none,
        chisel,
        knife,
        water,
        saw,
        drill,
        heat,
        hammer,
        screwdriver, // Dodano
        scissors    // Dodano
    }
    public ToolNeeded toolNeeded;
}