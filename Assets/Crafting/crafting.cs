using System.Collections.Generic;
using UnityEngine;

public class crafting : MonoBehaviour
{
    public static crafting Instance;
    [Header("DEBUG MODE!!!")]
    public bool debugMode;

    [Header("References")]
    public Inventory inventory;
    public ItemDatabase craftedDatabase; // Referencja do bazy danych
    [Header("Settings")]
    public bool enableCrafting;
    public bool craft;
    public Item outcomeItem;

    private List<Item> neededItems = new List<Item>();

    private void Awake()
    {
        Instance = this;
    }

    void Update(){
        if (craft){
            if (CanCraft() && enableCrafting)
                Craft();

            craft = false;
        }
    }

    bool CanCraft(){
        if (outcomeItem == null || outcomeItem.craftingRecipe == null)
            return false;
        CraftingRecipe recipe = outcomeItem.craftingRecipe;

        neededItems.Clear();
        foreach (var ingredient in recipe.itemsList)
            neededItems.Add(ingredient);

        foreach (var requiredItem in neededItems){
            if (!inventory.HasItem(requiredItem)){
                if (debugMode)
                    Debug.Log("Missing " + requiredItem.itemName);
                return false;
            }
        }

        if (recipe.toolNeeded != CraftingRecipe.ToolNeeded.none){
            bool toolFound = inventory.inventory.Exists(
                invItem => invItem.itemType == Item.ItemType.tool// && invItem.itemName.ToLower() == recipe.toolNeeded.ToString().ToLower()
            );

            if (!toolFound){
                if (debugMode)
                    Debug.Log("Missing tool " + recipe.toolNeeded);
                return false;
            }
        }

        return true;
    }
    private void RegisterCraftedItem(Item item)
    {
        
        if (item == null || craftedDatabase == null)
        {
            if (debugMode && craftedDatabase == null)
                Debug.LogWarning("Baza danych 'craftedDatabase' nie jest przypisana!");
            return;
        }
        if (!craftedDatabase.allItems.Contains(item))
        { 
            craftedDatabase.allItems.Add(item);

        }
    }
    public void Craft(){
        CraftingRecipe recipe = outcomeItem.craftingRecipe;

        foreach (var requiredItem in recipe.itemsList)
            inventory.RemoveFromInventory(inventory.inventory, requiredItem, 1);

        inventory.AddToInventory(inventory.inventory, 1, recipe.outcomeItem);
        RegisterCraftedItem(recipe.outcomeItem);//badziewie do UI do listy avalible craftable items
        if (debugMode)
            Debug.Log("Crafted " + recipe.outcomeItem.itemName);
    }
}
