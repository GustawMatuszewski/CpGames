using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FoodItemMassGenerator : EditorWindow
{
    // Zmienna do automatycznego przypisywania ID, zaczynamy od 100
    private static int currentID = 100;

    [MenuItem("Tools/Generuj Wszystkie Przedmioty")]
    public static void GenerateItems()
    {
        // Resetujemy ID za ka¿dym razem, gdy odpalamy skrypt, aby unikn¹æ b³êdów przy ponownym generowaniu
        currentID = 100;

        string path = "Assets/Items/Food drink";
        if (!AssetDatabase.IsValidFolder("Assets/Items")) AssetDatabase.CreateFolder("Assets", "Items");
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets/Items", "Food drink");

        // --- JEDZENIE ---
        // (Nazwa, Waga, Protein, Fats, Carbs, Calories, Nurishment, Hydration, Energy, Stan)

        CreateFood("Bochenek_chleba", 0.5f, 9f, 3f, 50f, 250f, 30f, -5f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Ryz_prazony", 0.4f, 7f, 1f, 80f, 350f, 40f, -10f, 10f, FoodItem.FoodState.fresh);
        CreateFood("Ryz_z_miodem", 0.45f, 5f, 1f, 90f, 400f, 45f, -5f, 20f, FoodItem.FoodState.fresh);
        CreateFood("Kabanosy", 0.2f, 25f, 40f, 0f, 450f, 35f, -10f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Kukurydza", 0.3f, 3f, 1f, 20f, 100f, 15f, 10f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Karma_dla_zwierzat", 0.4f, 10f, 5f, 10f, 150f, 15f, 5f, 0f, FoodItem.FoodState.fresh);
        CreateFood("Jablko", 0.15f, 0f, 0f, 20f, 80f, 10f, 20f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Banan", 0.12f, 1f, 0f, 27f, 105f, 15f, 5f, 10f, FoodItem.FoodState.fresh);
        CreateFood("Papryka", 0.2f, 1f, 0f, 6f, 30f, 10f, 15f, 2f, FoodItem.FoodState.fresh);
        CreateFood("Buraki", 0.3f, 2f, 0f, 10f, 45f, 12f, 10f, 3f, FoodItem.FoodState.fresh);
        CreateFood("Czekolada", 0.1f, 5f, 30f, 50f, 550f, 15f, -15f, 15f, FoodItem.FoodState.fresh);
        CreateFood("Whey_protein", 1.0f, 80f, 5f, 10f, 400f, 50f, -20f, 10f, FoodItem.FoodState.fresh);
        CreateFood("Cukier", 1.0f, 0f, 0f, 100f, 400f, 0f, -30f, 25f, FoodItem.FoodState.fresh);
        CreateFood("Maka_ryzowa", 1.0f, 6f, 1f, 80f, 360f, 20f, -40f, 0f, FoodItem.FoodState.fresh);
        CreateFood("Chipsy", 0.15f, 5f, 35f, 50f, 500f, 10f, -20f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Marchewki", 0.2f, 1f, 0f, 10f, 40f, 10f, 15f, 2f, FoodItem.FoodState.fresh);
        CreateFood("Fasola", 0.4f, 20f, 1f, 60f, 340f, 40f, -5f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Brzoskwinie", 0.3f, 1f, 0f, 15f, 60f, 10f, 25f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Ziemniaki", 1.0f, 2f, 0f, 17f, 75f, 20f, 5f, 5f, FoodItem.FoodState.freshRaw);
        CreateFood("Tunczyk", 0.15f, 25f, 1f, 0f, 100f, 30f, -5f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Wedlina", 0.2f, 15f, 10f, 2f, 150f, 20f, -5f, 0f, FoodItem.FoodState.fresh);
        CreateFood("Bekon", 0.15f, 15f, 40f, 1f, 400f, 25f, -15f, 5f, FoodItem.FoodState.freshRaw);
        CreateFood("Salami", 0.2f, 20f, 35f, 2f, 400f, 25f, -15f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Kielbasa", 0.3f, 15f, 25f, 2f, 300f, 30f, -10f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Nuggetsy", 0.25f, 15f, 20f, 15f, 250f, 25f, -10f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Filet_z_kurczaka", 0.5f, 30f, 5f, 0f, 150f, 40f, 0f, 5f, FoodItem.FoodState.freshRaw);
        CreateFood("Parowki", 0.25f, 10f, 20f, 5f, 250f, 20f, -5f, 0f, FoodItem.FoodState.fresh);
        CreateFood("Martwy_szczur", 0.3f, 15f, 5f, 0f, 100f, 5f, 0f, 0f, FoodItem.FoodState.rottenRaw);
        CreateFood("Martwy_karaluch", 0.05f, 2f, 1f, 0f, 10f, 1f, 0f, 0f, FoodItem.FoodState.rottenRaw);
        CreateFood("Martwy_krab", 0.2f, 10f, 1f, 0f, 50f, 5f, 0f, 0f, FoodItem.FoodState.rottenRaw);
        CreateFood("Jajka", 0.3f, 12f, 10f, 1f, 150f, 25f, 0f, 5f, FoodItem.FoodState.freshRaw);
        CreateFood("Dzdzownice", 0.05f, 5f, 1f, 0f, 20f, 2f, 0f, 0f, FoodItem.FoodState.raw);
        CreateFood("Rybiki_cukrowe", 0.02f, 1f, 0f, 0f, 5f, 1f, 0f, 0f, FoodItem.FoodState.raw);
        CreateFood("Arbuzy", 3.0f, 1f, 0f, 8f, 30f, 15f, 50f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Racje_zywnosciowe", 0.5f, 20f, 15f, 50f, 500f, 60f, -5f, 10f, FoodItem.FoodState.fresh);
        CreateFood("Zupki_chinskie", 0.1f, 5f, 15f, 40f, 350f, 15f, -15f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Maslo", 0.2f, 1f, 80f, 1f, 700f, 10f, -5f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Ogorki", 0.2f, 1f, 0f, 3f, 15f, 5f, 25f, 2f, FoodItem.FoodState.fresh);
        CreateFood("Zelki", 0.1f, 5f, 0f, 80f, 350f, 5f, -10f, 15f, FoodItem.FoodState.fresh);
        CreateFood("Proteinowe_batony", 0.1f, 20f, 5f, 20f, 250f, 25f, -5f, 10f, FoodItem.FoodState.fresh);
        CreateFood("Margaryna", 0.25f, 0f, 70f, 0f, 600f, 5f, -5f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Gumy_do_zucia", 0.02f, 0f, 0f, 5f, 10f, 0f, 2f, 2f, FoodItem.FoodState.fresh);
        CreateFood("Kawa_ziarna", 0.25f, 10f, 10f, 40f, 200f, 0f, -20f, 30f, FoodItem.FoodState.freshRaw);
        CreateFood("Suche_jedzenie_dla_zwierzat", 1.0f, 20f, 10f, 30f, 300f, 15f, -20f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Krakersy", 0.15f, 5f, 15f, 60f, 400f, 15f, -15f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Faworki", 0.2f, 5f, 25f, 50f, 450f, 15f, -15f, 10f, FoodItem.FoodState.fresh);
        CreateFood("Donuty", 0.1f, 5f, 20f, 40f, 350f, 15f, -10f, 15f, FoodItem.FoodState.fresh);
        CreateFood("Kajzerki", 0.05f, 4f, 1f, 30f, 150f, 10f, -5f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Zupa_pomidorowa", 0.4f, 5f, 5f, 20f, 150f, 25f, 30f, 5f, FoodItem.FoodState.cooked);
        CreateFood("Miod", 0.5f, 0f, 0f, 80f, 300f, 10f, -10f, 25f, FoodItem.FoodState.fresh);
        CreateFood("Serek_homogenizowany", 0.15f, 10f, 5f, 15f, 150f, 15f, 5f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Lody_na_patyku", 0.1f, 2f, 5f, 20f, 150f, 5f, 5f, 10f, FoodItem.FoodState.fresh);
        CreateFood("Wiadro_lodow", 1.0f, 10f, 30f, 100f, 1000f, 20f, 10f, 20f, FoodItem.FoodState.fresh);
        CreateFood("Sol_i_pieprz", 0.05f, 0f, 0f, 0f, 0f, 0f, -10f, 0f, FoodItem.FoodState.none);
        CreateFood("Dzem_malinowy", 0.3f, 0f, 0f, 60f, 250f, 10f, -5f, 15f, FoodItem.FoodState.fresh);
        CreateFood("Rosol", 0.5f, 10f, 10f, 5f, 150f, 30f, 40f, 10f, FoodItem.FoodState.cooked);
        CreateFood("Nutella", 0.4f, 5f, 30f, 55f, 500f, 15f, -10f, 20f, FoodItem.FoodState.fresh);
        CreateFood("Brokuly", 0.3f, 3f, 0f, 5f, 35f, 15f, 15f, 2f, FoodItem.FoodState.fresh);
        CreateFood("Kisiel", 0.2f, 0f, 0f, 15f, 60f, 5f, 10f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Orzechy", 0.2f, 15f, 50f, 15f, 600f, 20f, -10f, 10f, FoodItem.FoodState.fresh);
        CreateFood("Wafle_kukurydziane", 0.1f, 3f, 1f, 80f, 350f, 15f, -15f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Winogrona", 0.2f, 1f, 0f, 15f, 60f, 10f, 20f, 5f, FoodItem.FoodState.fresh);

        // --- PICIE ---
        CreateFood("Baniak_z_woda_5l", 5.0f, 0f, 0f, 0f, 0f, 0f, 500f, 0f, FoodItem.FoodState.fresh);
        CreateFood("Butelka_z_woda_0_5l", 0.5f, 0f, 0f, 0f, 0f, 0f, 50f, 0f, FoodItem.FoodState.fresh);
        CreateFood("Butelka_z_woda_1l", 1.0f, 0f, 0f, 0f, 0f, 0f, 100f, 0f, FoodItem.FoodState.fresh);
        CreateFood("Butelka_z_woda_1_5l", 1.5f, 0f, 0f, 0f, 0f, 0f, 150f, 0f, FoodItem.FoodState.fresh);
        CreateFood("Butelka_z_woda_2l", 2.0f, 0f, 0f, 0f, 0f, 0f, 200f, 0f, FoodItem.FoodState.fresh);
        CreateFood("Keczup", 0.5f, 1f, 0f, 25f, 100f, 5f, 0f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Ostry_sos", 0.2f, 1f, 0f, 10f, 50f, 2f, -5f, 2f, FoodItem.FoodState.fresh);
        CreateFood("Musztarda", 0.2f, 4f, 3f, 5f, 60f, 5f, -5f, 2f, FoodItem.FoodState.fresh);
        CreateFood("Mleko", 1.0f, 3f, 3f, 5f, 60f, 10f, 80f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Racja_wodna", 0.25f, 0f, 0f, 0f, 0f, 0f, 30f, 0f, FoodItem.FoodState.fresh);
        CreateFood("Coca_cola_z_cukrem", 0.5f, 0f, 0f, 40f, 150f, 5f, 30f, 15f, FoodItem.FoodState.fresh);
        CreateFood("Coca_cola_zero", 0.5f, 0f, 0f, 0f, 0f, 0f, 30f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Olej", 1.0f, 0f, 100f, 0f, 800f, 5f, -10f, 5f, FoodItem.FoodState.fresh);
        CreateFood("Mleko_zageszczone", 0.5f, 8f, 8f, 55f, 320f, 20f, 20f, 15f, FoodItem.FoodState.fresh);
        CreateFood("Kawa_gotowa", 0.3f, 1f, 1f, 5f, 30f, 5f, 25f, 30f, FoodItem.FoodState.fresh);
        CreateFood("Protein_shake_gotowy", 0.5f, 30f, 5f, 10f, 200f, 35f, 40f, 10f, FoodItem.FoodState.fresh);
        CreateFood("Wodka", 0.5f, 0f, 0f, 0f, 200f, 0f, -20f, -10f, FoodItem.FoodState.fresh);
        CreateFood("Piwo", 0.5f, 1f, 0f, 15f, 150f, 5f, 10f, -5f, FoodItem.FoodState.fresh);
        CreateFood("Tequila", 0.5f, 0f, 0f, 0f, 200f, 0f, -20f, -10f, FoodItem.FoodState.fresh);
        CreateFood("Whisky", 0.5f, 0f, 0f, 0f, 200f, 0f, -20f, -10f, FoodItem.FoodState.fresh);
        CreateFood("Likier", 0.5f, 0f, 0f, 30f, 250f, 0f, -15f, 0f, FoodItem.FoodState.fresh);
        CreateFood("Wino", 0.75f, 0f, 0f, 5f, 100f, 2f, -5f, -5f, FoodItem.FoodState.fresh);
        CreateFood("Bimber", 1.0f, 0f, 0f, 0f, 250f, 0f, -30f, -20f, FoodItem.FoodState.fresh);
        CreateFood("Gorzalka", 0.5f, 0f, 0f, 0f, 200f, 0f, -20f, -10f, FoodItem.FoodState.fresh);
        CreateFood("Denaturat", 0.5f, 0f, 0f, 0f, 0f, -50f, -50f, -50f, FoodItem.FoodState.none);
        CreateFood("Shot_mineralow", 0.1f, 0f, 0f, 5f, 20f, 15f, 10f, 20f, FoodItem.FoodState.fresh);
        CreateFood("Fanta", 0.5f, 0f, 0f, 40f, 150f, 5f, 30f, 10f, FoodItem.FoodState.fresh);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Wygenerowano wszystkie 89 przedmiotów z automatycznymi ID (od 100) oraz odpowiednimi opcjami bazowymi!");
    }

    private static void CreateFood(string name, float weight, float p, float f, float c, float cal, float nur, float hyd, float enery, FoodItem.FoodState state)
    {
        FoodItem asset = ScriptableObject.CreateInstance<FoodItem>();
        asset.itemName = name.Replace("_", " ");

        // ---- AUTOMATYCZNE PRZYPISYWANIE OPCJI BAZOWYCH ----

        // 1. Przypisanie i inkrementacja ID
        asset.itemID = currentID++;

        // 2. Automatyczny typ przedmiotu (wszystko tutaj to jedzenie)
        asset.itemType = Item.ItemType.food;

        // 3. Sprytne dobieranie materia³u na podstawie nazwy (domyœlnie none)
        Item.MaterialType matType = Item.MaterialType.none;
        string lowerName = name.ToLower();

        if (lowerName.Contains("butelka") || lowerName.Contains("baniak") || lowerName.Contains("coca_cola") || lowerName.Contains("fanta"))
        {
            matType = Item.MaterialType.plastic;
        }
        else if (lowerName.Contains("szczur") || lowerName.Contains("kurczak") || lowerName.Contains("bekon") || lowerName.Contains("krab") || lowerName.Contains("karaluch"))
        {
            matType = Item.MaterialType.tissue;
        }

        asset.materialType = matType;
        // ---------------------------------------------------

        asset.weight = weight;
        asset.protein = p;
        asset.fats = f;
        asset.carbs = c;
        asset.calories = cal;
        asset.nurishment = nur;
        asset.hydration = hyd;
        asset.eneryBoost = enery;
        asset.foodState = state;
        asset.effects = new List<FoodItem.Effect> { FoodItem.Effect.none };

        string path = $"Assets/Items/Food drink/{name}.asset";
        AssetDatabase.CreateAsset(asset, path);
    }
}