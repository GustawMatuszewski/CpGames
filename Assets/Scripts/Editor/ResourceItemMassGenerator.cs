using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

public class ResourceItemMassGenerator : EditorWindow
{
    // Zmienna do automatycznego przypisywania ID, zaczynamy od 500
    private static int currentID = 500;

    [MenuItem("Tools/Generuj Wszystkie Zasoby")]
    public static void GenerateItems()
    {
        // Reset ID
        currentID = 500;

        string path = "Assets/Items/Resources";
        if (!AssetDatabase.IsValidFolder("Assets/Items")) AssetDatabase.CreateFolder("Assets", "Items");
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets/Items", "Resources");

        // --- ZASOBY I MATERIA£Y RÓ¯NE ---
        // (Nazwa wyœwietlana, Waga, ItemType, MaterialType)

        CreateResource("Deska", 1.5f, Item.ItemType.buildingMaterial, Item.MaterialType.wood);
        CreateResource("GwóŸdŸ", 0.01f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Wkrêt", 0.01f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Taœma naprawcza (Duct Tape)", 0.3f, Item.ItemType.resource, Item.MaterialType.tough);
        CreateResource("Taœma klej¹ca", 0.1f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Klej do drewna", 0.2f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Klej biurowy", 0.1f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Sznurek", 0.1f, Item.ItemType.resource, Item.MaterialType.nylon);
        CreateResource("Lina", 1.0f, Item.ItemType.resource, Item.MaterialType.tough);
        CreateResource("Lina z przeœcierade³", 1.5f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("Drut", 0.5f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Drut kolczasty", 2.0f, Item.ItemType.construction, Item.MaterialType.metal);
        CreateResource("Z³om metalowy", 1.0f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Blacha metalowa", 2.0f, Item.ItemType.buildingMaterial, Item.MaterialType.metal);
        CreateResource("Ma³a blacha metalowa", 0.5f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Rura metalowa", 1.5f, Item.ItemType.buildingMaterial, Item.MaterialType.metal);
        CreateResource("Metalowy prêt", 1.5f, Item.ItemType.buildingMaterial, Item.MaterialType.metal);
        CreateResource("Z³om elektroniczny", 0.5f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("P³ytka drukowana", 0.1f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Wzmacniacz", 1.0f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Odbiornik radiowy", 0.8f, Item.ItemType.loot, Item.MaterialType.plastic);
        CreateResource("Nadajnik radiowy", 0.8f, Item.ItemType.loot, Item.MaterialType.plastic);
        CreateResource("Tranzystor", 0.01f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Rezystor", 0.01f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Kondensator", 0.01f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Czujnik ruchu", 0.2f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Pilot do zdalnego sterowania", 0.2f, Item.ItemType.loot, Item.MaterialType.plastic);
        CreateResource("¯arówka", 0.05f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("¯arówka czerwona", 0.05f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("¯arówka zielona", 0.05f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("¯arówka niebieska", 0.05f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Wybielacz", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Benzyna (w kanistrze)", 5.0f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Gips", 2.0f, Item.ItemType.buildingMaterial, Item.MaterialType.stone);
        CreateResource("Worek ¿wiru", 15.0f, Item.ItemType.buildingMaterial, Item.MaterialType.stone);
        CreateResource("Worek piasku", 15.0f, Item.ItemType.buildingMaterial, Item.MaterialType.stone);
        CreateResource("Worek ziemi", 10.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Pusty worek na piasek", 0.2f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("Worek na œmieci", 0.05f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Podarte przeœcierad³a", 0.2f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("Brudne szmaty", 0.1f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("Paski jeansu", 0.1f, Item.ItemType.resource, Item.MaterialType.jeans);
        CreateResource("Paski skóry", 0.1f, Item.ItemType.resource, Item.MaterialType.leather);
        CreateResource("Kawa³ek materia³u", 0.1f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("Nici", 0.05f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("W³óczka", 0.1f, Item.ItemType.resource, Item.MaterialType.woolen);
        CreateResource("Guzik", 0.01f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Ig³a do szycia", 0.01f, Item.ItemType.tool, Item.MaterialType.metal);
        CreateResource("Agrafka", 0.01f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Farba bia³a", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Farba czarna", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Farba szara", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Farba czerwona", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Farba niebieska", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Farba ¿ó³ta", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Farba zielona", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Farba br¹zowa", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Farba turkusowa", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Farba pomarañczowa", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Drewno na opa³", 2.0f, Item.ItemType.resource, Item.MaterialType.wood);
        CreateResource("Ga³¹zka", 0.2f, Item.ItemType.resource, Item.MaterialType.kindling);
        CreateResource("Patyk", 0.5f, Item.ItemType.resource, Item.MaterialType.wood);
        CreateResource("Rozpa³ka", 0.1f, Item.ItemType.resource, Item.MaterialType.kindling);
        CreateResource("Kora drzewna", 0.2f, Item.ItemType.resource, Item.MaterialType.kindling);
        CreateResource("Kamieñ", 1.0f, Item.ItemType.resource, Item.MaterialType.stone);
        CreateResource("Ostry kamieñ", 0.8f, Item.ItemType.tool, Item.MaterialType.stone);
        CreateResource("Dziennik", 0.3f, Item.ItemType.loot, Item.MaterialType.tissue);
        CreateResource("Zeszyt", 0.2f, Item.ItemType.loot, Item.MaterialType.tissue);
        CreateResource("Kartka papieru", 0.01f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("Gazeta", 0.1f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("Magazyn", 0.2f, Item.ItemType.loot, Item.MaterialType.tissue);
        CreateResource("Komiks", 0.2f, Item.ItemType.loot, Item.MaterialType.tissue);
        CreateResource("O³ówek", 0.05f, Item.ItemType.tool, Item.MaterialType.wood);
        CreateResource("D³ugopis", 0.05f, Item.ItemType.tool, Item.MaterialType.plastic);
        CreateResource("Kredka czerwona", 0.05f, Item.ItemType.tool, Item.MaterialType.wood);
        CreateResource("Kredka niebieska", 0.05f, Item.ItemType.tool, Item.MaterialType.wood);
        CreateResource("Kredka zielona", 0.05f, Item.ItemType.tool, Item.MaterialType.wood);
        CreateResource("Kredka ¿ó³ta", 0.05f, Item.ItemType.tool, Item.MaterialType.wood);
        CreateResource("Gumka do mazania", 0.05f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Pusta butelka plastikowa", 0.05f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Pusta butelka szklana", 0.2f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Kufel", 0.4f, Item.ItemType.loot, Item.MaterialType.unknown);
        CreateResource("Szklanka", 0.2f, Item.ItemType.loot, Item.MaterialType.unknown);
        CreateResource("S³oik", 0.3f, Item.ItemType.storage, Item.MaterialType.unknown);
        CreateResource("Nakrêtka do s³oika", 0.02f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Puszka aluminiowa", 0.05f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Nasiona broku³a", 0.01f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Nasiona kapusty", 0.01f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Nasiona marchewki", 0.01f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Nasiona ziemniaka", 0.05f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Nasiona pomidora", 0.01f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Nasiona rzodkiewki", 0.01f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Nasiona truskawki", 0.01f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Nawóz NPK", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Myd³o", 0.1f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Papier toaletowy", 0.1f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("Rêcznik papierowy", 0.2f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("Chusteczki higieniczne", 0.05f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("Wata", 0.05f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("Banda¿ materia³owy", 0.1f, Item.ItemType.medical, Item.MaterialType.tissue);
        CreateResource("Banda¿ samoprzylepny", 0.05f, Item.ItemType.medical, Item.MaterialType.plastic);
        CreateResource("Chusteczka nas¹czona alkoholem", 0.01f, Item.ItemType.medical, Item.MaterialType.tissue);
        CreateResource("Pude³ko zapa³ek", 0.05f, Item.ItemType.tool, Item.MaterialType.wood);
        CreateResource("Zapalniczka", 0.05f, Item.ItemType.tool, Item.MaterialType.plastic);
        CreateResource("Papierosy", 0.05f, Item.ItemType.loot, Item.MaterialType.tissue);
        CreateResource("Bateria", 0.1f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Czêœci silnika", 5.0f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Œruba do kó³", 0.2f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Zawiasy do drzwi", 0.2f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Klamka", 0.3f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Guma recepturka", 0.01f, Item.ItemType.resource, Item.MaterialType.nylon);
        CreateResource("Kawa³ek szk³a", 0.1f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Sól (do konserwacji)", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Cukier (do przetworów)", 1.0f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Dro¿d¿e", 0.1f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Proszek do pieczenia", 0.1f, Item.ItemType.resource, Item.MaterialType.unknown);
        CreateResource("Beton w proszku", 25.0f, Item.ItemType.buildingMaterial, Item.MaterialType.stone);
        CreateResource("Kreda", 0.05f, Item.ItemType.resource, Item.MaterialType.stone);
        CreateResource("Szpilka", 0.01f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Pude³ko na naboje (puste)", 0.1f, Item.ItemType.storage, Item.MaterialType.plastic);
        CreateResource("Worki po nawozie", 0.1f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Zatyczki do uszu", 0.01f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Puste pude³ko po zapa³kach", 0.02f, Item.ItemType.resource, Item.MaterialType.tissue);
        CreateResource("Szpula drutu miedzianego", 0.5f, Item.ItemType.resource, Item.MaterialType.metal);
        CreateResource("Przewód elektryczny", 0.5f, Item.ItemType.resource, Item.MaterialType.plastic);
        CreateResource("Akumulator samochodowy (jako magazyn energii)", 15.0f, Item.ItemType.buildingMaterial, Item.MaterialType.metal);
        CreateResource("¯etony do gry", 0.1f, Item.ItemType.loot, Item.MaterialType.plastic);
        CreateResource("Karty do gry", 0.1f, Item.ItemType.loot, Item.MaterialType.tissue);
        CreateResource("Kostki do gry", 0.05f, Item.ItemType.loot, Item.MaterialType.plastic);
        CreateResource("Pude³ko z bi¿uteri¹ (surowiec)", 0.5f, Item.ItemType.loot, Item.MaterialType.unknown);
        CreateResource("Zegarek cyfrowy (do demonta¿u)", 0.1f, Item.ItemType.loot, Item.MaterialType.plastic);
        CreateResource("Zegarek mechaniczny (do demonta¿u)", 0.15f, Item.ItemType.loot, Item.MaterialType.metal);

        // Zapisz zmiany w projekcie
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Sukces] Wygenerowano {currentID - 500} przedmiotów (ID od 500 do {currentID - 1}).");
    }

    private static void CreateResource(string displayName, float weight, Item.ItemType type, Item.MaterialType mat)
    {
        ResourceItem newItem = ScriptableObject.CreateInstance<ResourceItem>();

        // Konfiguracja przedmiotu
        newItem.itemID = currentID++;
        newItem.itemName = displayName;
        newItem.weight = weight;
        newItem.itemType = type;
        newItem.materialType = mat;

        // Tworzymy bezpieczn¹ nazwê pliku (bez polskich znaków, spacji i nawiasów)
        string assetName = SanitizeName(displayName);
        string assetPath = $"Assets/Items/Resources/{assetName}.asset";

        // Tworzenie pliku w Unity
        AssetDatabase.CreateAsset(newItem, assetPath);
    }

    // Pomocnicza funkcja czyszcz¹ca nazwy polskie na proste do nazewnictwa plików
    private static string SanitizeName(string input)
    {
        string safe = input.ToLower();

        // Zastêpowanie polskich znaków
        safe = safe.Replace("¹", "a").Replace("æ", "c").Replace("ê", "e")
                   .Replace("³", "l").Replace("ñ", "n").Replace("ó", "o")
                   .Replace("œ", "s").Replace("Ÿ", "z").Replace("¿", "z");

        // Usuniêcie znaków niealfanumerycznych (zamiana na pod³ogê)
        safe = Regex.Replace(safe, @"[^a-z0-9]", "_");
        // Usuniêcie powielonych pod³óg
        safe = Regex.Replace(safe, @"_+", "_");
        // Uciêcie skrajnych pod³óg
        safe = safe.Trim('_');

        // Zmiana pierwszej litery na wielk¹ (PascalCase)
        if (safe.Length > 0)
        {
            safe = char.ToUpper(safe[0]) + safe.Substring(1);
        }

        return safe;
    }
}