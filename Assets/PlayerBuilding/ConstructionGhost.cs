using UnityEngine;
using System.Collections.Generic;

public class ConstructionGhost : MonoBehaviour
{
    Construction frame;
    List<ConstructionSlot> slots = new List<ConstructionSlot>();

    public bool HasFilledSlots
    {
        get
        {
            foreach (ConstructionSlot s in slots)
                if (s.isFilled) return true;
            return false;
        }
    }

    public void Init(Construction frame, Construction variant)
    {
        this.frame = frame;

        // Ghost visual — use variant's Model if set, otherwise variant's own mesh
        GameObject modelSource = variant.Model != null ? variant.Model : variant.gameObject;
        GameObject visual = Instantiate(modelSource, transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        if (frame.ghostMaterial != null)
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
                r.material = frame.ghostMaterial;

        // Ghost mesh must not block slot raycasts
        SetLayerRecursive(visual, LayerMask.NameToLayer("Ignore Raycast"));

        // Get slot positions directly from variant prefab children
        ConstructionSlot[] variantSlots = variant.GetComponentsInChildren<ConstructionSlot>(true);
        Debug.Log($"[Ghost] Found {variantSlots.Length} slots in variant {variant.name}");

        if (variantSlots.Length == 0)
        {
            Debug.LogWarning($"[Ghost] Variant {variant.name} has no ConstructionSlot components in children! Add empty GameObjects with ConstructionSlot to your variant prefab.");
        }

        foreach (ConstructionSlot prefabSlot in variantSlots)
        {
            // Reconstruct world position from variant-local to frame world space
            // prefabSlot.transform.localPosition is relative to variant root
            Vector3 worldPos = frame.transform.TransformPoint(prefabSlot.transform.localPosition);
            Quaternion worldRot = frame.transform.rotation * prefabSlot.transform.localRotation;

            GameObject slotObj = new GameObject("ConstructionSlot");
            slotObj.transform.SetPositionAndRotation(worldPos, worldRot);
            slotObj.transform.SetParent(transform);

            ConstructionSlot slot = slotObj.AddComponent<ConstructionSlot>();
            slot.Init(this, frame.slotUnfilledMaterial, frame.slotFilledMaterial);
            slots.Add(slot);
        }
    }

    public void CheckAllFilled()
    {
        foreach (ConstructionSlot slot in slots)
            if (!slot.isFilled) return;

        frame.OnGhostComplete();
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        if (layer == -1) return; // Layer doesn't exist, skip
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}