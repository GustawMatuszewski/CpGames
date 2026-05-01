using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class WeaponItemMassGenerator : EditorWindow
{
    // Zaczynamy od ID 50 zgodnie z proœb¹
    private static int currentID = 50;
    private static string savePath = "Assets/Items/Weapons";

    [MenuItem("Tools/Generuj Wszystkie Bronie")]
    public static void GenerateWeapons()
    {
        currentID = 50; // Reset ID

        // Tworzenie folderów jeœli nie istniej¹
        if (!AssetDatabase.IsValidFolder("Assets/Items")) AssetDatabase.CreateFolder("Assets", "Items");
        if (!AssetDatabase.IsValidFolder(savePath)) AssetDatabase.CreateFolder("Assets/Items", "Weapons");

        // --- BRONIE BIA£E ---
        // Format: (Nazwa pliku, Nazwa w grze, Waga, Wytrzyma³oœæ, Obra¿enia, Typ Materia³u, Typ U¿ycia)

        CreateWeapon("Lom", "£om", 2.0f, 500, 25f, Item.MaterialType.metal, WeaponItem.UseType.twoHanded);
        CreateWeapon("Kij_baseballowy", "Kij baseballowy", 1.0f, 150, 20f, Item.MaterialType.wood, WeaponItem.UseType.twoHanded);
        CreateWeapon("Siekiera_strazacka", "Siekiera stra¿acka", 3.0f, 250, 40f, Item.MaterialType.metal, WeaponItem.UseType.twoHanded);
        CreateWeapon("Mlotek_ciesielski", "M³otek ciesielski", 0.8f, 300, 18f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Noz_kuchenny", "Nó¿ kuchenny", 0.2f, 50, 15f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Srubokret", "Œrubokrêt", 0.1f, 80, 10f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Maczeta", "Maczeta", 0.6f, 150, 35f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Patelnia", "Patelnia", 1.2f, 200, 15f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Klucz_francuski", "Klucz francuski", 1.5f, 400, 22f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Gazrurka", "Gazrurka", 1.8f, 300, 20f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Lopata", "£opata", 2.5f, 200, 25f, Item.MaterialType.metal, WeaponItem.UseType.twoHanded);
        CreateWeapon("Pila_reczna", "Pi³a rêczna", 0.7f, 100, 18f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Noz_mysliwski", "Nó¿ myœliwski", 0.3f, 150, 25f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Tasak_rzeznicki", "Tasak rzeŸnicki", 0.5f, 120, 30f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Kij_golfowy", "Kij golfowy", 0.8f, 80, 20f, Item.MaterialType.metal, WeaponItem.UseType.twoHanded);
        CreateWeapon("Palka_policyjna", "Pa³ka policyjna", 0.6f, 250, 15f, Item.MaterialType.tough, WeaponItem.UseType.oneHanded);
        CreateWeapon("Kilof", "Kilof", 3.5f, 300, 35f, Item.MaterialType.metal, WeaponItem.UseType.twoHanded);
        CreateWeapon("Dluto", "D³uto", 0.2f, 100, 12f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Sekator", "Sekator", 0.4f, 80, 10f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Deska_z_gwozdziami", "Deska z gwoŸdziami", 1.5f, 50, 25f, Item.MaterialType.wood, WeaponItem.UseType.twoHanded);
        CreateWeapon("Kij_z_nozem", "Kij z no¿em", 1.2f, 60, 30f, Item.MaterialType.wood, WeaponItem.UseType.twoHanded);
        CreateWeapon("Tluczek_do_miesa", "T³uczek do miêsa", 0.4f, 150, 12f, Item.MaterialType.wood, WeaponItem.UseType.oneHanded);
        CreateWeapon("Hantel", "Hantel", 5.0f, 500, 25f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Klucz_do_kol", "Klucz do kó³", 1.2f, 350, 22f, Item.MaterialType.metal, WeaponItem.UseType.twoHanded);
        CreateWeapon("Wedka", "Wêdka", 0.5f, 30, 5f, Item.MaterialType.plastic, WeaponItem.UseType.twoHanded);
        CreateWeapon("Widelki_ogrodowe", "Wide³ki ogrodowe", 1.8f, 150, 28f, Item.MaterialType.metal, WeaponItem.UseType.twoHanded);
        CreateWeapon("Nozyce_do_metalu", "No¿yce do metalu", 0.8f, 150, 15f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Metalowy_pret", "Metalowy prêt", 2.0f, 400, 18f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Noz_do_tapet", "Nó¿ do tapet", 0.1f, 20, 12f, Item.MaterialType.plastic, WeaponItem.UseType.oneHanded);
        CreateWeapon("Otwieracz_do_konserw", "Otwieracz do konserw", 0.1f, 100, 5f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Kij_do_krykieta", "Kij do krykieta", 1.5f, 180, 22f, Item.MaterialType.wood, WeaponItem.UseType.twoHanded);
        CreateWeapon("Grabie", "Grabie", 1.5f, 100, 15f, Item.MaterialType.wood, WeaponItem.UseType.twoHanded);
        CreateWeapon("Siekierka", "Siekierka", 1.0f, 200, 30f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Plaski_srubokret", "P³aski œrubokrêt", 0.1f, 80, 10f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);
        CreateWeapon("Zelazko", "¯elazko", 1.5f, 150, 20f, Item.MaterialType.metal, WeaponItem.UseType.oneHanded);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Wygenerowano pomyœlnie wszystkie bronie! Znajdziesz je w folderze: {savePath}");
    }

    private static void CreateWeapon(string fileName, string displayName, float weight, int durability, float damage, Item.MaterialType materialType, WeaponItem.UseType useType)
    {
        // Tworzenie nowej instancji dla obiektu ScriptableObject
        WeaponItem weapon = ScriptableObject.CreateInstance<WeaponItem>();

        // Zmienne z klasy Item
        weapon.itemID = currentID++;
        weapon.itemName = displayName;
        weapon.description = "To jest " + displayName.ToLower() + ". Mo¿esz tego u¿yæ do walki.";
        weapon.itemType = Item.ItemType.weapon; // Ogólny typ przedmiotu
        weapon.materialType = materialType;
        weapon.weight = weight;
        weapon.durability = durability;
        weapon.usesLeft = durability; // Ustawia usesLeft na maksa (tyle ile durability)
        weapon.burnCalories = weight * 5f; // Generuje domyœlne spalanie kalorii na podstawie wagi 

        // Zmienne z klasy WeaponItem
        weapon.weaponType = WeaponItem.WeaponType.closeCombat; // Wszystko z tej listy to broñ bia³a
        weapon.useType = useType;
        weapon.damage = damage;
        // weapon.attacksList - tutaj mo¿na dodaæ logikê jeœli potrzebujesz domyœlnych ataków

        // Zapisywanie do pliku w Unity
        string assetPath = $"{savePath}/{fileName}.asset";
        AssetDatabase.CreateAsset(weapon, assetPath);
    }
}