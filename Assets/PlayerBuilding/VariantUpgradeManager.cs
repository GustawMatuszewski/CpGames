using UnityEngine;
using System.Collections.Generic;

public class VariantUpgradeManager : MonoBehaviour
{
    Construction construction;
    List<ConstructionSlot> activeSlots = new List<ConstructionSlot>();
    List<Collider> disabledColliders = new List<Collider>();
    bool upgradeActive = false;

    void Awake()
    {
        construction = GetComponent<Construction>();
    }

    void Update()
    {
        if (!upgradeActive && construction.selectedVariant != null)
            ActivateUpgrade();
    }

    public void ActivateUpgrade()
    {
        upgradeActive = true;
        Construction variant = construction.selectedVariant;
        if (variant == null) return;

        // Disable construction's own colliders so they don't block slot raycasts
        disabledColliders.Clear();
        foreach (Collider col in GetComponents<Collider>())
        {
            col.enabled = false;
            disabledColliders.Add(col);
        }

        ConstructionSlot[] prefabSlots = variant.GetComponentsInChildren<ConstructionSlot>(true);
        foreach (ConstructionSlot prefabSlot in prefabSlots)
        {
            Vector3 worldPos = transform.TransformPoint(prefabSlot.transform.localPosition);
            Quaternion worldRot = transform.rotation * prefabSlot.transform.localRotation;

            GameObject ghost = Instantiate(prefabSlot.gameObject, worldPos, worldRot);
            ghost.SetActive(true);
            ghost.transform.SetParent(transform, true);

            ConstructionSlot slot = ghost.GetComponent<ConstructionSlot>();
            slot.manager = this;
            slot.Init(construction.slotUnfilledMaterial, construction.slotFilledMaterial);
            activeSlots.Add(slot);
        }

        Debug.Log($"[VariantUpgrade] Spawned {activeSlots.Count} slot ghosts for variant {variant.name}");
    }

    public void CheckAllFilled()
    {
        foreach (ConstructionSlot slot in activeSlots)
            if (!slot.isFilled) return;

        DoUpgrade();
    }

    void DoUpgrade()
    {
        Construction variant = construction.selectedVariant;
        if (variant == null) return;

        // Re-enable colliders before destroying in case something else needs them
        foreach (Collider col in disabledColliders)
            if (col != null) col.enabled = true;

        Debug.Log($"[VariantUpgrade] All slots filled! Replacing with {variant.name}");
        Instantiate(variant.gameObject, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}