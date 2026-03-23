using UnityEngine;
using System.Collections.Generic;

public class crafting : MonoBehaviour
{
    public static crafting Instance;

    [Header("DEBUG MODE!!!")]
    public bool debugMode;

    [Header("References")]
    public Inventory inventory;
    public CraftingRecipeDatabase recipeDatabase;

    [Header("Settings")]
    public bool enableCrafting = true;
    public bool craft;

    public List<Item> lastCraftResult { get; private set; } = new List<Item>();
    public bool lastCraftSuccess { get; private set; } = false;

    private void Awake() => Instance = this;

    void Update(){
        if(craft){
            craft = false;
            List<Item> itemsOnTable = UI_Script.Instance.GetItemsOnTable();
            List<Item> result = StartCraft(itemsOnTable);
            UI_Script.Instance.ClearTable();
            UI_Script.Instance.SpawnItemsOnTable(result);
        }
    }

    public List<Item> StartCraft(List<Item> itemsOnTable){
        lastCraftSuccess = false;
        lastCraftResult.Clear();

        if(!enableCrafting){
            if(debugMode) Debug.Log("Crafting jest wylaczone.");
            lastCraftResult = new List<Item>(itemsOnTable);
            return lastCraftResult;
        }

        if(itemsOnTable == null || itemsOnTable.Count == 0){
            if(debugMode) Debug.Log("Stol jest pusty.");
            lastCraftResult = new List<Item>();
            return lastCraftResult;
        }

        CraftingRecipe matched = FindMatchingRecipe(itemsOnTable);

        if(matched == null){
            if(debugMode) Debug.Log("Brak receptury dla: " + string.Join(", ", itemsOnTable.ConvertAll(i => i.itemName)));
            lastCraftResult = new List<Item>(itemsOnTable);
            return lastCraftResult;
        }

        if(matched.toolNeeded != CraftingRecipe.ToolNeeded.none){
            bool toolFound = inventory.inventory.Exists(i => i.itemType == Item.ItemType.tool);
            if(!toolFound){
                if(debugMode) Debug.Log("Brak narzedzia: " + matched.toolNeeded);
                lastCraftResult = new List<Item>(itemsOnTable);
                return lastCraftResult;
            }
        }

        foreach(Item needed in matched.itemsList)
            inventory.RemoveFromInventory(inventory.inventory, needed, 1);

        inventory.AddToInventory(inventory.inventory, 1, matched.outcomeItem);

        lastCraftSuccess = true;
        lastCraftResult = new List<Item> { matched.outcomeItem };

        if(debugMode) Debug.Log("Crafted " + matched.outcomeItem.itemName);
        return lastCraftResult;
    }

    public CraftingRecipe PreviewRecipe(List<Item> itemsOnTable) => FindMatchingRecipe(itemsOnTable);

    public List<CraftingRecipe> GetAllRecipes(){
        if(recipeDatabase == null) return new List<CraftingRecipe>();
        return recipeDatabase.allCraftingRecipes;
    }

    public bool CanCraftRecipe(CraftingRecipe recipe){
        if(recipe == null) return false;
        foreach(Item needed in recipe.itemsList)
            if(!inventory.HasItem(needed)) return false;
        return true;
    }

    CraftingRecipe FindMatchingRecipe(List<Item> itemsOnTable){
        if(recipeDatabase == null){
            Debug.LogError("Brak CraftingRecipeDatabase!");
            return null;
        }
        foreach(CraftingRecipe recipe in recipeDatabase.allCraftingRecipes)
            if(RecipeMatches(recipe, itemsOnTable)) return recipe;
        return null;
    }

    bool RecipeMatches(CraftingRecipe recipe, List<Item> itemsOnTable){
        if(debugMode) Debug.Log("Sprawdzam: " + recipe.name + " wymaga: " + recipe.itemsList.Count + " na stole: " + itemsOnTable.Count);
        if(recipe.itemsList.Count != itemsOnTable.Count) return false;
        List<Item> remaining = new List<Item>(itemsOnTable);
        foreach(Item needed in recipe.itemsList){
            Item found = remaining.Find(i => i.itemName == needed.itemName);
            if(debugMode) Debug.Log("Szukam: " + needed.itemName + " znaleziono: " + (found != null));
            if(found == null) return false;
            remaining.Remove(found);
        }
        return true;
    }
}