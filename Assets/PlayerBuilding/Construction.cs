using UnityEngine;
using System.Collections.Generic;
public class Construction : MonoBehaviour, IInteractable
{
    [Header("Build")]
    public List<GameObject> connectors;
    public GameObject Model;
    public List<Item> itemsList;
    public float timeToBuild;
    public bool canBeBurnt;
    public float tempHealth;
    [Header("Variants")]
    public List<Construction> variants;
    [Header("Ghost Visuals")]
    public Material ghostMaterial;
    public Material slotUnfilledMaterial;
    public Material slotFilledMaterial;
    [Header("Interaction")]
    public List<Transform> snapPoints;
    public Transform lookAtPoint;
    public bool UseSnapping => false;
    public List<Transform> InteractionPositions =>
        snapPoints != null && snapPoints.Count > 0
            ? snapPoints
            : new List<Transform> { transform };
    public Transform LookAtTarget => lookAtPoint != null ? lookAtPoint : transform;
    int variantIndex = -1;
    ConstructionGhost activeGhost;
    List<Collider> ownColliders = new List<Collider>();
    public void OnInteract()
    {
        if (variants == null || variants.Count == 0)
        {
            Debug.LogWarning("[Construction] No variants assigned!");
            return;
        }
        // Don't allow re-cycling once player has started filling slots
        if (activeGhost != null && activeGhost.HasFilledSlots) return;
        // Destroy previous ghost if cycling
        if (activeGhost != null)
            Destroy(activeGhost.gameObject);
        variantIndex = (variantIndex + 1) % variants.Count;
        Construction chosenVariant = variants[variantIndex];
        Debug.Log($"[Construction] Selected variant: {chosenVariant.name}");
        // Disable own colliders so raycasts reach slots
        ownColliders.Clear();
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
            ownColliders.Add(c);
        }
        GameObject ghostObj = new GameObject($"Ghost_{chosenVariant.name}");
        ghostObj.transform.SetPositionAndRotation(transform.position, transform.rotation);
        ghostObj.transform.SetParent(transform);
        activeGhost = ghostObj.AddComponent<ConstructionGhost>();
        activeGhost.Init(this, chosenVariant);
    }
    public void OnGhostComplete()
    {
        foreach (Collider c in ownColliders)
            if (c != null) c.enabled = true;
        Instantiate(variants[variantIndex].gameObject, transform.position, transform.rotation);
        Destroy(gameObject);
    }
    public void RestoreColliders()
    {
        foreach (Collider c in ownColliders)
            if (c != null) c.enabled = true;
    }
}