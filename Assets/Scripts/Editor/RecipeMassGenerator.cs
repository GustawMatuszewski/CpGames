using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class RecipeMassGenerator : EditorWindow
{
    private static string savePath = "Assets/Items/Recipes";

    private struct RecipeDefinition
    {
        public string OutcomeName;
        public string[] IngredientNames;
        public CraftingRecipe.ToolNeeded Tool;

        public RecipeDefinition(string outcome, CraftingRecipe.ToolNeeded tool, params string[] ingredients)
        {
            OutcomeName = outcome;
            Tool = tool;
            IngredientNames = ingredients;
        }
    }

    [MenuItem("Tools/Generate Crafting Recipes")]
    public static void GenerateRecipes()
    {
        if (!AssetDatabase.IsValidFolder(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        string[] guids = AssetDatabase.FindAssets("t:Item");
        Dictionary<string, Item> allItems = new Dictionary<string, Item>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Item item = AssetDatabase.LoadAssetAtPath<Item>(path);
            if (item != null && !allItems.ContainsKey(item.name))
                allItems.Add(item.name, item);
        }

        // LISTA PRZEPISÓW - DOPASOWANA DO TWOICH NAZW PLIKÓW
        List<RecipeDefinition> recipes = new List<RecipeDefinition>
        {
            // --- BUDYNKI ---
            new RecipeDefinition("PlankFence", CraftingRecipe.ToolNeeded.hammer, "Plank", "Nail"),
            new RecipeDefinition("PlankFloor", CraftingRecipe.ToolNeeded.hammer, "Plank", "Nail"),
            new RecipeDefinition("PlankLadder", CraftingRecipe.ToolNeeded.hammer, "Plank", "Nail"),
            new RecipeDefinition("PlankStairs", CraftingRecipe.ToolNeeded.hammer, "Plank", "Nail"),
            new RecipeDefinition("PlankWall", CraftingRecipe.ToolNeeded.hammer, "Plank", "Nail"),
            new RecipeDefinition("PlankWallWindowed", CraftingRecipe.ToolNeeded.hammer, "Plank", "Nail"),

            // --- JEDZENIE I PICIE ---
            new RecipeDefinition("ChickenNuggets", CraftingRecipe.ToolNeeded.heat, "Sausage"), // uproszczone przetwarzanie
            new RecipeDefinition("TomatoSoupCan", CraftingRecipe.ToolNeeded.heat, "CornCan", "TomatoSoupCan"), // mieszanie składników
            new RecipeDefinition("WaterBottle1L", CraftingRecipe.ToolNeeded.none, "EmptyPlasticBottle"), // napełnianie (założenie: z kranu/źródła)
            new RecipeDefinition("SaltAndPepper", CraftingRecipe.ToolNeeded.none, "Nuts"), // rozdrabnianie (opcjonalnie moździerz)

            // --- MEDYCYNA ---
            new RecipeDefinition("Medbag", CraftingRecipe.ToolNeeded.none, "Bandage", "Syringe", "Tweezers"),
            new RecipeDefinition("Bandage", CraftingRecipe.ToolNeeded.scissors, "FabricScrap"),

            // --- NARZĘDZIA I ZASOBY (CRAFTING) ---
            new RecipeDefinition("Nailsbox", CraftingRecipe.ToolNeeded.none, "Nail", "Nail", "Nail"),
            new RecipeDefinition("DuctTape", CraftingRecipe.ToolNeeded.none, "FabricScrap", "OfficeGlue"),
            new RecipeDefinition("Plank", CraftingRecipe.ToolNeeded.saw, "Stick"),
            new RecipeDefinition("DenimStrips", CraftingRecipe.ToolNeeded.scissors, "FabricScrap"),
            new RecipeDefinition("LeatherStrips", CraftingRecipe.ToolNeeded.knife, "FabricScrap"), // założenie przetworzenia skóry
            new RecipeDefinition("Twine", CraftingRecipe.ToolNeeded.none, "Thread", "Thread"),
            new RecipeDefinition("GlassJarEmpty", CraftingRecipe.ToolNeeded.none, "EmptyGlassBottle", "JarLid"),

            // --- BROŃ ---
            new RecipeDefinition("Axe", CraftingRecipe.ToolNeeded.hammer, "Stick", "ElectronicScrap"), // prowizoryczna głowica
            new RecipeDefinition("BaseballBat", CraftingRecipe.ToolNeeded.none, "Plank", "Nail"),
            new RecipeDefinition("Bayonet", CraftingRecipe.ToolNeeded.heat, "HuntingKnife", "DuctTape"),
            new RecipeDefinition("Butcher'sCleaver", CraftingRecipe.ToolNeeded.heat, "ElectronicScrap"),
            new RecipeDefinition("Crowbar", CraftingRecipe.ToolNeeded.heat, "ElectronicScrap"),
            new RecipeDefinition("FireAxe", CraftingRecipe.ToolNeeded.hammer, "Stick", "ElectronicScrap", "DuctTape"),
            new RecipeDefinition("HuntingKnife", CraftingRecipe.ToolNeeded.heat, "ElectronicScrap"),
            new RecipeDefinition("KitchenKnife", CraftingRecipe.ToolNeeded.heat, "ElectronicScrap"),
            new RecipeDefinition("Machete", CraftingRecipe.ToolNeeded.heat, "ElectronicScrap", "DuctTape"),
            new RecipeDefinition("Screwdriver", CraftingRecipe.ToolNeeded.none, "ElectronicScrap"),
            new RecipeDefinition("TwoHandedAxe", CraftingRecipe.ToolNeeded.hammer, "Plank", "ElectronicScrap", "DuctTape")
        };

        int createdCount = 0;

        foreach (var def in recipes)
        {
            if (!allItems.TryGetValue(def.OutcomeName, out Item outcome))
            {
                Debug.LogWarning($"[BRAK ITEMU] Nie znaleziono wyniku: {def.OutcomeName}");
                continue;
            }

            List<Item> ingredients = new List<Item>();
            bool fail = false;
            foreach (string ingName in def.IngredientNames)
            {
                if (allItems.TryGetValue(ingName, out Item ing))
                    ingredients.Add(ing);
                else
                {
                    Debug.LogWarning($"[BRAK SKŁADNIKA] W przepisie na {def.OutcomeName} brakuje: {ingName}");
                    fail = true;
                    break;
                }
            }

            if (fail) continue;

            CraftingRecipe asset = ScriptableObject.CreateInstance<CraftingRecipe>();
            asset.recipeID = recipes.IndexOf(def) + 100;
            asset.outcomeItem = outcome;
            asset.itemsList = ingredients;
            asset.toolNeeded = def.Tool;

            AssetDatabase.CreateAsset(asset, $"{savePath}/Recipe_{def.OutcomeName}_{createdCount}.asset");
            createdCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=green>Sukces! Utworzono {createdCount} receptur.</color>");
    }
}