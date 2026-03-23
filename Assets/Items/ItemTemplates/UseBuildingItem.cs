using UnityEngine;

/// <summary>
/// Attach to the Player (same GameObject as Build).
/// Set this as the `useScript` on a BuildingItem ScriptableObject.
/// When equipped it pushes the Construction prefab into Build and
/// removes one instance of the item from inventory after each placement.
/// </summary>
[RequireComponent(typeof(Build))]
public class UseBuildingItem : MonoBehaviour
{
    // Set by your equip / hotbar system when the player selects the item
    [HideInInspector] public BuildingItem buildingItem;
    [HideInInspector] public Inventory    inventory;

    Build buildComponent;
    bool  isActive;
    bool  prevCanBuild = true;

    // The exact instance that was equipped (so RemoveFromInventory matches by ref)
    Item equippedInstance;

    void Awake()
    {
        buildComponent = GetComponent<Build>();
    }

    // ── Public API called by your hotbar / equip system ──────────────────

    /// <summary>
    /// Call this when the player equips a building item.
    /// Pass the *instance* from inventory.inventory (not the original SO).
    /// </summary>
    public void Activate(Item itemInstance, Inventory inv)
    {
        BuildingItem data = itemInstance as BuildingItem;

        if (data == null || data.constructionPrefab == null)
        {
            Debug.LogWarning("UseBuildingItem: item is not a BuildingItem or has no constructionPrefab.");
            return;
        }

        buildingItem     = data;
        equippedInstance = itemInstance;
        inventory        = inv;

        // Configure Build with this item's construction prefab
        buildComponent.toPlace = data.constructionPrefab;
        buildComponent.enabled = true;

        // Re-spawn the ghost so it reflects the new prefab
        buildComponent.SendMessage("SpawnGhost", SendMessageOptions.DontRequireReceiver);

        prevCanBuild = true;
        isActive     = true;
    }

    /// <summary>Call this when the player unequips / switches item.</summary>
    public void Deactivate()
    {
        isActive         = false;
        equippedInstance = null;
        buildComponent.enabled = false;
    }

    // ── Placement detection & consumption ────────────────────────────────

    void Update()
    {
        if (!isActive || buildingItem == null) return;

        // Build sets canBuild = false on the exact frame it places an object,
        // then flips back to true once the interact key is released.
        // Catching the true->false edge gives us exactly one event per placement.
        bool placedThisFrame = prevCanBuild && !buildComponent.canBuild;
        prevCanBuild = buildComponent.canBuild;

        if (placedThisFrame)
            ConsumeItem();
    }

    void ConsumeItem()
    {
        if (inventory == null || equippedInstance == null) return;

        // Uses Inventory's own method — matches by reference, consistent with your codebase
        inventory.RemoveFromInventory(inventory.inventory, equippedInstance, 1);

        // Check if the player still has another instance of this building item type
        // (inventory holds Instantiate'd copies, so we match by itemID)
        Item nextInstance = inventory.inventory.Find(
            i => i != null && i.itemID == buildingItem.itemID
        );

        if (nextInstance != null)
        {
            // Keep building with the next available instance
            equippedInstance = nextInstance;
        }
        else
        {
            Debug.Log($"[UseBuildingItem] No more {buildingItem.itemName} in inventory. Deactivating.");
            Deactivate();
        }
    }
}