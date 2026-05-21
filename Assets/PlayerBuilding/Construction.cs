using UnityEngine;
using System.Collections.Generic;
public class Construction : MonoBehaviour, IInteractable
{
    public float placementOffset = 1.5f;
    [Header("Build")]
public List<GameObject> connectors;
public GameObject Model;
public List<Item> itemsList;
public float timeToBuild;
public bool canBeBurnt;
public float tempHealth;
    [Header("Ghost Visuals")]
public Material ghostMaterial;
    [Header("Interaction")]
public List<Transform> snapPoints;
public Transform lookAtPoint;
public bool UseSnapping => false;
public List<Transform> InteractionPositions =>
snapPoints != null && snapPoints.Count > 0
? snapPoints
: new List<Transform> { transform };
public Transform LookAtTarget => lookAtPoint != null ? lookAtPoint : transform;
List<Collider> ownColliders = new List<Collider>();

public void OnInteract()
    {
    }
    public void RestoreColliders()
    {
         foreach (Collider c in ownColliders)
             if (c != null) c.enabled = true;
    }

    public void ActionDeconstruct()
    {
        
        Destroy(gameObject);
    }
}