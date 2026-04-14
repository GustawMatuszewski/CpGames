using UnityEngine;
using System.Collections.Generic;

public class ConstructionSlot : MonoBehaviour, IInteractable
{
    public bool isFilled = false;

    ConstructionGhost ghost;
    Renderer indicator;
    Material unfilledMat;
    Material filledMat;

    public bool UseSnapping => false;
    public List<Transform> InteractionPositions => new List<Transform> { transform };
    public Transform LookAtTarget => transform;

    public void Init(ConstructionGhost ghost, Material unfilled, Material filled)
    {
        this.ghost = ghost;
        unfilledMat = unfilled;
        filledMat = filled;

        BoxCollider col = gameObject.AddComponent<BoxCollider>();
        col.size = Vector3.one * 0.4f;
        col.isTrigger = false;

        // Visible indicator cube
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = Vector3.one * 0.3f;
        Destroy(cube.GetComponent<Collider>()); // collider is on parent

        indicator = cube.GetComponent<Renderer>();
        UpdateVisual();
    }

    public void OnInteract()
    {
        if (isFilled) return;
        Fill();
    }

    public void Fill()
    {
        isFilled = true;
        UpdateVisual();
        ghost?.CheckAllFilled();
    }

    void UpdateVisual()
    {
        if (indicator == null) return;
        if (isFilled && filledMat != null) indicator.material = filledMat;
        else if (!isFilled && unfilledMat != null) indicator.material = unfilledMat;
    }
}