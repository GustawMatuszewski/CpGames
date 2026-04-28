using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

public class MedicineItemMassGenerator : EditorWindow
{
    // Zaczynamy od 700, aby nie nadpisaæ jedzenia (100+) ani zasobów (500+)
    private static int currentID = 700;

    [MenuItem("Tools/Generuj Medycyne")]
    public static void GenerateItems()
    {
        currentID = 700;

        string path = "Assets/Items/Medicine";
        if (!AssetDatabase.IsValidFolder("Assets/Items")) AssetDatabase.CreateFolder("Assets", "Items");
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets/Items", "Medicine");

        // --- ZASOBY MEDYCZNE (ResourceItem) ---
        // (Nazwa wyœwietlana, Waga, ItemType, MaterialType)
        CreateResource("Opaska uciskowa", 0.1f, Item.ItemType.medical, Item.MaterialType.nylon);
        CreateResource("Gaza medyczna", 0.05f, Item.ItemType.medical, Item.MaterialType.tissue);
        CreateResource("Banda¿ sterylny", 0.1f, Item.ItemType.medical, Item.MaterialType.tissue);
        CreateResource("Alkohol izopropylowy", 0.5f, Item.ItemType.medical, Item.MaterialType.plastic);
        CreateResource("Woda utleniona", 0.3f, Item.ItemType.medical, Item.MaterialType.plastic);
        CreateResource("Maœæ antyseptyczna", 0.1f, Item.ItemType.medical, Item.MaterialType.unknown);
        CreateResource("Ig³a medyczna", 0.01f, Item.ItemType.medical, Item.MaterialType.metal);
        CreateResource("Niæ chirurgiczna", 0.02f, Item.ItemType.medical, Item.MaterialType.nylon);
        CreateResource("Zestaw do szycia ran", 0.2f, Item.ItemType.medical, Item.MaterialType.plastic);
        CreateResource("Jodyna", 0.1f, Item.ItemType.medical, Item.MaterialType.unknown);
        CreateResource("Krople do oczu", 0.05f, Item.ItemType.medical, Item.MaterialType.plastic);
        CreateResource("Maseczka chirurgiczna", 0.02f, Item.ItemType.medical, Item.MaterialType.tissue);
        CreateResource("Rêkawiczki lateksowe", 0.05f, Item.ItemType.medical, Item.MaterialType.plastic);
        CreateResource("Spray do dezynfekcji", 0.3f, Item.ItemType.medical, Item.MaterialType.plastic);
        CreateResource("Plastry opatrunkowe", 0.05f, Item.ItemType.medical, Item.MaterialType.plastic);
        CreateResource("Szyna usztywniaj¹ca", 0.5f, Item.ItemType.medical, Item.MaterialType.wood);
        CreateResource("Ko³nierz ortopedyczny", 0.4f, Item.ItemType.medical, Item.MaterialType.plastic);
        CreateResource("Termometr", 0.1f, Item.ItemType.medical, Item.MaterialType.unknown);
        CreateResource("Strzykawka", 0.05f, Item.ItemType.medical, Item.MaterialType.plastic);
        CreateResource("¯el przeciwbólowy", 0.1f, Item.ItemType.medical, Item.MaterialType.unknown);
        CreateResource("Balsam ³agodz¹cy", 0.1f, Item.ItemType.medical, Item.MaterialType.unknown);
        CreateResource("Pêseta", 0.05f, Item.ItemType.tool, Item.MaterialType.metal);
        CreateResource("No¿yczki ratownicze", 0.15f, Item.ItemType.tool, Item.MaterialType.metal);
        CreateResource("Wata bawe³niana", 0.05f, Item.ItemType.medical, Item.MaterialType.tissue);
        CreateResource("Chusta trójk¹tna", 0.1f, Item.ItemType.medical, Item.MaterialType.tissue);
        CreateResource("Koc termiczny", 0.2f, Item.ItemType.medical, Item.MaterialType.plastic);
        CreateResource("Podk³adki ch³odz¹ce", 0.3f, Item.ItemType.medical, Item.MaterialType.plastic);
        CreateResource("Talk medyczny", 0.2f, Item.ItemType.medical, Item.MaterialType.unknown);

        // --- LEKARSTWA / DO SPO¯YCIA (FoodItem) ---
        // (Nazwa, Waga, Protein, Fats, Carbs, Calories, Nurishment, Hydration, Energy, Stan)
        CreateFood("Antybiotyki", 0.05f, 0f, 0f, 0f, 0f, 0f, -5f, -10f, FoodItem.FoodState.fresh);
        CreateFood("Œrodki przeciwbólowe", 0.05f, 0f, 0f, 0f, 0f, 0f, -2f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Tabletki nasenne", 0.05f, 0f, 0f, 0f, 0f, 0f, -2f, -50f, FoodItem.FoodState.fresh);
        CreateFood("Wêgiel aktywny", 0.05f, 0f, 0f, 0f, 0f, 0f, -10f, 0f, FoodItem.FoodState.fresh);
        CreateFood("Witaminy", 0.05f, 0f, 0f, 2f, 10f, 5f, 0f, 15f, FoodItem.FoodState.fresh);
        CreateFood("P³yn do p³ukania ust", 0.4f, 0f, 0f, 0f, 0f, 0f, 5f, 2f, FoodItem.FoodState.fresh);
        CreateFood("Œrodek na uspokojenie", 0.05f, 0f, 0f, 0f, 0f, 0f, -2f, -20f, FoodItem.FoodState.fresh);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Sukces] Wygenerowano przedmioty medyczne. Ostatnie u¿yte ID: {currentID - 1}");
    }

    private static void CreateResource(string displayName, float weight, Item.ItemType type, Item.MaterialType mat)
    {
        ResourceItem newItem = ScriptableObject.CreateInstance<ResourceItem>();

        newItem.itemID = currentID++;
        newItem.itemName = displayName;
        newItem.weight = weight;
        newItem.itemType = type;
        newItem.materialType = mat;

        string assetName = SanitizeName(displayName);
        string assetPath = $"Assets/Items/Medicine/{assetName}.asset";

        AssetDatabase.CreateAsset(newItem, assetPath);
    }

    private static void CreateFood(string displayName, float weight, float protein, float fats, float carbs, float calories, float nurishment, float hydration, float energy, FoodItem.FoodState state)
    {
        FoodItem newItem = ScriptableObject.CreateInstance<FoodItem>();

        newItem.itemID = currentID++;
        newItem.itemName = displayName;
        newItem.weight = weight;
        newItem.itemType = Item.ItemType.medical; // Leki do spo¿ycia to wci¹¿ kategoria medyczna
        newItem.materialType = Item.MaterialType.unknown;

        newItem.foodState = state;
        newItem.protein = protein;
        newItem.fats = fats;
        newItem.carbs = carbs;
        newItem.calories = calories;
        newItem.nurishment = nurishment;
        newItem.hydration = hydration;
        newItem.eneryBoost = energy; // Zak³adam literówkê w Twoim kodzie "eneryBoost" -> odwzorowane dok³adnie

        string assetName = SanitizeName(displayName);
        string assetPath = $"Assets/Items/Medicine/{assetName}.asset";

        AssetDatabase.CreateAsset(newItem, assetPath);
    }

    private static string SanitizeName(string input)
    {
        string safe = input.ToLower();
        safe = safe.Replace("¹", "a").Replace("æ", "c").Replace("ê", "e")
                   .Replace("³", "l").Replace("ñ", "n").Replace("ó", "o")
                   .Replace("œ", "s").Replace("Ÿ", "z").Replace("¿", "z");
        safe = Regex.Replace(safe, @"[^a-z0-9]", "_");
        safe = Regex.Replace(safe, @"_+", "_");
        safe = safe.Trim('_');

        if (safe.Length > 0)
        {
            safe = char.ToUpper(safe[0]) + safe.Substring(1);
        }
        return safe;
    }
}