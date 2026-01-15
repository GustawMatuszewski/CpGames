using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CraftingRecipeDatabase", menuName = "Inventory/CraftingRecipeDatabase")]
public class CraftingRecipeDatabase : ScriptableObject
{
    public List<CraftingRecipe> allCraftingRecipes = new List<CraftingRecipe>();
}
