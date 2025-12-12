using UnityEngine;

[CreateAssetMenu(fileName = "WeaponItem", menuName = "Inventory/ItemByType/WeaponItem")]
public class WeaponItem : Item
{
    public enum WeaponType
    {
        none,
        ranged,
        closeCombat
    }

    public enum DamageType
    {
        none,
        blunt,
        slice,
        penetrate
    }

    public enum UseType
    {
        none,
        oneHanded,
        twoHanded
    }

    [Header("Weapon Configuration")]
    public WeaponType weaponType = WeaponType.none;
    public DamageType damageType = DamageType.none;
    public UseType useType = UseType.none;

    public float damage;

}