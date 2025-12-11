using UnityEngine;

public class InventoryInteractions : MonoBehaviour
{

    [Header("Debug MODE!!!")]
    public bool debugMode = false;

    [Header("References")]
    public PlayerInput input;
    public Inventory playerInventory;
    public Camera playerCamera;

    [Header("Interaction Settings")]
    public float interactDistance = 3f;

//Private shit
    private Inventory focusedInventory;
    private float interactInput;

    void Start(){
        input = new PlayerInput();
        input.Enable();
    }

    void Update(){
        interactInput = input.PlayerInputMap.InteractInput.ReadValue<float>();

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, interactDistance)){
            focusedInventory = hit.collider.GetComponent<Inventory>();

            if(focusedInventory != null){
                if(interactInput > 0){
                    if(focusedInventory.inventory.Count > 0){
                        Item itemToTake = focusedInventory.inventory[0];
                        focusedInventory.RemoveFromInventory(focusedInventory.inventory, itemToTake.itemID, 1);
                        playerInventory.AddToInventory(playerInventory.inventory, 1, itemToTake.itemID);
                    }
                }
            }else{
                focusedInventory = null;
            }
        }
        else{
            focusedInventory = null;
        }
    }
}
