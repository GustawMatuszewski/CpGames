using UnityEngine;

[CreateAssetMenu(fileName = "ClothingItem", menuName = "Inventory/ItemByType/ClothingItem")]
public class ClothingItem : Item
{
    public enum ClothingType
    {
        none,
        shoes,
        boots,
        socks,
        underwear,
        shirt,
        hoodie,
        jacket,
        armor,
        coverall
    }

    [Header("Clothing Configuration, material type above")]
    public ClothingType clothingType = ClothingType.none;
    public float scratchResistance;
    public float biteResistance;
}