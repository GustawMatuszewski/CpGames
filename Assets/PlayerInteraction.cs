using UnityEngine;

public interface IInteractable {
    void OnInteract();
}

public class PlayerInteraction : MonoBehaviour {
    [Header("DEBUG MODE !!!!")]
    public bool debugMode;

    [Header("References")]
    public KCC player;
    public Camera playerCamera;

    [Header("Interaction Settings")]
    public float interactionDistance = 3f;
    
    void Update(){
        LookForInteraction();
    }

    void LookForInteraction(){
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance)){
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null){
                if (debugMode) Debug.Log("Looking at: " + hit.collider.name);

                if (player.input.PlayerInputMap.InteractInput.triggered){
                    interactable.OnInteract();
                }
            }
        }
    }
}