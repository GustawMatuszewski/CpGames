using UnityEngine;
using System.Collections.Generic;

public class ConstructionSlot : MonoBehaviour, IInteractable
{
    public VariantUpgradeManager manager;
    public bool isFilled = false;
    
    public GameObject filledVisual;
    public GameObject unfilledVisual;

    public bool UseSnapping => false;
    public List<Transform> InteractionPositions => null;
    public Transform LookAtTarget => null;

    void Awake()
    {
        if (manager == null)
            manager = GetComponentInParent<VariantUpgradeManager>();
            
        UpdateVisuals();
    }

    public void OnInteract()
    {
        if (isFilled) return;

        Fill();
    }

    public void Fill()
    {
        isFilled = true;
        UpdateVisuals();
        manager?.CheckAllFilled();
    }

    void UpdateVisuals()
    {
        if (unfilledVisual != null) unfilledVisual.SetActive(!isFilled);
        if (filledVisual != null) filledVisual.SetActive(isFilled);
    }
}