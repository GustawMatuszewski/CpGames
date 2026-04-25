using System.Collections.Generic;
using UnityEngine;

public partial class crafting : MonoBehaviour
{
    public static crafting Instance;
    [Header("DEBUG MODE!!!")]
    public bool debugMode;

    [Header("References")]
    public Inventory inventory;
    public ItemDatabase allItemsDatabase; // Baza wszystkich możliwych receptur
    public ItemDatabase craftedDatabase; // Baza przedmiotów już stworzonych (do UI)

    [Header("Settings")]
    public bool enableCrafting;
    public bool craft; // Trigger do craftingu
    public Item outcomeItem; // Ręcznie wybrany przedmiot (opcjonalny)

    private List<Item> neededItems = new List<Item>();

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (craft)
        {
            HandleCraftingLogic();
            craft = false;
        }
    }

    private void HandleCraftingLogic()
    {
        // 1. Jeśli przypisałeś konkretny przedmiot w inspektorze - sprawdź tylko jego
        if (outcomeItem != null)
        {
            if (CanCraft(outcomeItem))
                Craft(outcomeItem);
            return;
        }

        // 2. Jeśli outcomeItem jest pusty - przeszukaj całą bazę danych
        if (allItemsDatabase != null)
        {
            foreach (Item item in allItemsDatabase.allItems)
            {
                if (CanCraft(item))
                {
                    Craft(item);
                    // Opcjonalnie: break; jeśli chcesz stworzyć tylko pierwszy pasujący przedmiot
                     break; 
                }
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("Brak przypisanej bazy 'allItemsDatabase'!");
        }
    }

    // Teraz metoda przyjmuje parametr, co pozwala jej sprawdzać dowolny przedmiot
    bool CanCraft(Item itemToCraft)
    {
        if (itemToCraft == null || itemToCraft.craftingRecipe == null)
            return false;

        CraftingRecipe recipe = itemToCraft.craftingRecipe;

        // Sprawdzanie składników
        neededItems.Clear();
        foreach (var ingredient in recipe.itemsList)
            neededItems.Add(ingredient);

        foreach (var requiredItem in neededItems)
        {
            if (!inventory.HasItem(requiredItem))
            {
                if (debugMode && outcomeItem != null) // Loguj błędy tylko przy konkretnym wyborze
                    Debug.Log("Missing " + requiredItem.itemName + " for " + itemToCraft.itemName);
                return false;
            }
        }

        // Sprawdzanie narzędzi
        if (recipe.toolNeeded != CraftingRecipe.ToolNeeded.none)
        {
            bool toolFound = inventory.inventory.Exists(
                invItem => invItem.itemType == Item.ItemType.tool
            );

            if (!toolFound)
            {
                if (debugMode)
                    Debug.Log("Missing tool " + recipe.toolNeeded);
                return false;
            }
        }

        return true;
    }

    public void Craft(Item itemToCraft)
    {
        CraftingRecipe recipe = itemToCraft.craftingRecipe;

        // Usuwanie składników
        foreach (var requiredItem in recipe.itemsList)
            inventory.RemoveFromInventory(inventory.inventory, requiredItem, 1);

        // Dodawanie wyniku
        inventory.AddToInventory(inventory.inventory, 1, recipe.outcomeItem);
        
        RegisterCraftedItem(recipe.outcomeItem);

        if (debugMode)
            Debug.Log("Crafted " + recipe.outcomeItem.itemName);
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
}











//  using System.Collections.Generic;

// using UnityEngine;


// public class crafting : MonoBehaviour

// {

// public static crafting Instance;

// [Header("DEBUG MODE!!!")]

// public bool debugMode;


// [Header("References")]

// public Inventory inventory;

// public ItemDatabase craftedDatabase; // Referencja do bazy danych

// [Header("Settings")]

// public bool enableCrafting;

// public bool craft;

// public Item outcomeItem;


// private List<Item> neededItems = new List<Item>();


// private void Awake()

// {

// Instance = this;

// }


// void Update(){

// if (craft){

// if (CanCraft())

// Craft();


// craft = false;

// }

// }


// bool CanCraft(){

// if (outcomeItem == null || outcomeItem.craftingRecipe == null)

// return false;

// CraftingRecipe recipe = outcomeItem.craftingRecipe;


// neededItems.Clear();

// foreach (var ingredient in recipe.itemsList)

// neededItems.Add(ingredient);


// foreach (var requiredItem in neededItems){

// if (!inventory.HasItem(requiredItem)){

// if (debugMode)

// Debug.Log("Missing " + requiredItem.itemName);

// return false;

// }

// }


// if (recipe.toolNeeded != CraftingRecipe.ToolNeeded.none){

// bool toolFound = inventory.inventory.Exists(

// invItem => invItem.itemType == Item.ItemType.tool// && invItem.itemName.ToLower() == recipe.toolNeeded.ToString().ToLower()

// );


// if (!toolFound){

// if (debugMode)

// Debug.Log("Missing tool " + recipe.toolNeeded);

// return false;

// }

// }


// return true;

// }

// private void RegisterCraftedItem(Item item)

// {

// if (item == null || craftedDatabase == null)

// {

// if (debugMode && craftedDatabase == null)

// Debug.LogWarning("Baza danych 'craftedDatabase' nie jest przypisana!");

// return;

// }

// if (!craftedDatabase.allItems.Contains(item))

// {

// craftedDatabase.allItems.Add(item);


// }

// }

// public void Craft(){

// CraftingRecipe recipe = outcomeItem.craftingRecipe;


// foreach (var requiredItem in recipe.itemsList)

// inventory.RemoveFromInventory(inventory.inventory, requiredItem, 1);


// inventory.AddToInventory(inventory.inventory, 1, recipe.outcomeItem);

// RegisterCraftedItem(recipe.outcomeItem);//badziewie do UI do listy avalible craftable items

// if (debugMode)

// Debug.Log("Crafted " + recipe.outcomeItem.itemName);

// }

// }



