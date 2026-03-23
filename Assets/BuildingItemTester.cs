using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingItemTester : MonoBehaviour
{
    public Item testItem;
    public Inventory playerInventory;
    public InputActionReference activateAction;

    Item spawnedInstance;
    bool buildingModeActive = false;

    void Start()
    {
        if (testItem == null || playerInventory == null) return;

        playerInventory.Add(testItem, 1);
        spawnedInstance = playerInventory.inventory[playerInventory.inventory.Count - 1];

        Debug.Log($"[Tester] Added {testItem.itemName}");
    }

    void Update()
    {
        if (activateAction == null || !activateAction.action.WasPressedThisFrame()) return;

        if (buildingModeActive)
        {
            Debug.Log("[Tester] Already in building mode.");
            return;
        }

        if (spawnedInstance == null)
        {
            Debug.Log("[Tester] No item left.");
            return;
        }

        buildingModeActive = true;
        spawnedInstance.Use(spawnedInstance, playerInventory);

        // after placement Build disables itself — detect that to reset
        Build build = playerInventory.GetComponentInChildren<Build>();
        if (build == null) build = playerInventory.GetComponentInParent<Build>();
        if (build != null)
            StartCoroutine(WaitForPlacement(build));
    }

    System.Collections.IEnumerator WaitForPlacement(Build build)
    {
        // wait until Build disables itself (happens in PlaceConstruction)
        yield return new WaitUntil(() => !build.enabled);

        Debug.Log("[Tester] Placement done, item removed.");
        spawnedInstance = null;
        buildingModeActive = false;
    }
}