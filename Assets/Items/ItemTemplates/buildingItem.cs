using UnityEngine;

[CreateAssetMenu(fileName = "BuildingItem", menuName = "Inventory/ItemByType/BuildingItem")]
public class BuildingItem : Item
{
    [Header("Building Configuration")]
    public Construction constructionPrefab;

    public override void Use(Item instance, Inventory playerInventory)
    {
        if (constructionPrefab == null)
        {
            Debug.LogWarning($"[BuildingItem] {itemName} has no constructionPrefab assigned!");
            return;
        }

        Build build = playerInventory.GetComponentInChildren<Build>();
        if (build == null) build = playerInventory.GetComponentInParent<Build>();

        if (build == null)
        {
            Debug.LogWarning($"[BuildingItem] No Build component found on player!");
            return;
        }

        build.toPlace = constructionPrefab;
        build.pendingItem = instance;
        build.pendingInventory = playerInventory;
        build.enabled = true;
        build.SendMessage("SpawnGhost", SendMessageOptions.DontRequireReceiver);

        Debug.Log($"[BuildingItem] Building mode ON for {itemName}");
    }
}