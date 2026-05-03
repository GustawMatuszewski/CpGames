using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Cinemachine;

public interface IInteractable
{
    void OnInteract();
    bool UseSnapping { get; }
    List<Transform> InteractionPositions { get; }
    Transform LookAtTarget { get; }
}

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    public KCC    player;
    public Camera playerCamera;
    public Inventory playerInventory;

    [Header("Settings")]
    public float interactionDistance = 3f;
    public float snapExitDelay       = 0.5f;

    [Header("Input")]
    public InputAction interactAction;

    [Header("Debug")]
    public bool debugMode = true;

    // ── Internal state ─────────────────────────────────────────────
    Transform         currentSnapPoint;
    float             snapExitTimer;
    CinemachineCamera internalCinemachine;

    List<DoorAction> currentActions = new();
    Door currentOpenable;   // replaces Door + Window fields

    bool menuOpen = false;
    Inventory currentOpenChest;
    bool chestIsOpen = false;

    string[] componentsToLock =
    {
        "CinemachineInputAxisController",
        "CinemachinePanTilt",
        "CinemachineOrbitalFollow",
        "CinemachineRotationHandler"
    };

    // ── Unity lifecycle ────────────────────────────────────────────
    void OnEnable()
    {
        interactAction.Enable();
        interactAction.performed += OnInteractPerformed;
        interactAction.canceled  += OnInteractCanceled;
    }

    void OnDisable()
    {
        interactAction.Disable();
        interactAction.performed -= OnInteractPerformed;
        interactAction.canceled  -= OnInteractCanceled;
    }

    void Start()
    {
        if (playerCamera != null)
        {
            internalCinemachine = playerCamera.GetComponent<CinemachineCamera>()
                               ?? playerCamera.GetComponentInParent<CinemachineCamera>();
        }
    }

    void Update()
    {
        HandleSnapLock();//Kiedys to wypierdzziele Wieze w to --- Windforce
        HandleChestAutoClose();
    }

    // ── Input callbacks ────────────────────────────────────────────
    void OnInteractPerformed(InputAction.CallbackContext context)
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance)) return;

        // Chest
        Inventory hitInventory = hit.collider.GetComponentInParent<Inventory>();
        if (hitInventory != null && hitInventory.type != InventoryType.Player)
        {
            if (chestIsOpen && currentOpenChest == hitInventory) { CloseChest(); return; }
            OpenChest(hitInventory);
            return;
        }

        // Door/Window
        Door openable = hit.collider.GetComponentInParent<Door>();
        if (openable == null) return;
        var actions = openable.GetDoorActions();

        if (context.interaction is UnityEngine.InputSystem.Interactions.TapInteraction)
        {
            if (actions.Count > 0 && actions[0].enabled)
                actions[0].execute?.Invoke();
        }
        else if (context.interaction is UnityEngine.InputSystem.Interactions.HoldInteraction)
        {
            if (actions.Count == 1) actions[0].execute?.Invoke();
            else { HandleSnapping(openable, hit); OpenMenu(openable, actions); menuOpen = true; }
        }
    }

    void OnInteractCanceled(InputAction.CallbackContext context)
    {
        if (!menuOpen) return;
        DoorAction selected = UI_Interaction.Instance.SelectRondoAction();
        if (selected != null && selected.enabled)
            selected.execute?.Invoke();

        Cursor.lockState = CursorLockMode.Locked;
        menuOpen = false;
    }
    
    void OpenChest(Inventory chest)
    {
        if (chestIsOpen)
            CloseChest();

        currentOpenChest = chest;
        chestIsOpen = true;

        if (playerInventory != null)
            playerInventory.outsideInventory = chest;

        UI_Script.Instance.SendItemsToChest(chest.inventory);
        UI_Script.Instance.ShowChest();
    }

    void CloseChest()
    {
        if (!chestIsOpen) return;

        UI_Script.Instance.HideInventory();
        UI_Script.Instance.HideChest();

        if (playerInventory != null)
            playerInventory.outsideInventory = null;

        chestIsOpen = false;
        currentOpenChest = null;
    }

    void HandleChestAutoClose()
    {
        if (!chestIsOpen || currentOpenChest == null) return;

        float dist = Vector3.Distance(
            player.transform.position,
            currentOpenChest.transform.position);

        if (dist > interactionDistance * 1.5f)
            CloseChest();
    }


    // ── Menu ───────────────────────────────────────────────────────
    void OpenMenu(Door openable, List<DoorAction> actions)
    {
        currentOpenable = openable;
        currentActions  = actions;

        UI_Interaction.Instance.SendRondoActions(actions);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // ── Snapping ───────────────────────────────────────────────────
    void HandleSnapping(IInteractable interactable, RaycastHit hit)
    {
        if (!interactable.UseSnapping || interactable.InteractionPositions == null) return;
        if (interactable.InteractionPositions.Count == 0) return;

        currentSnapPoint = GetClosestSnapPoint(interactable.InteractionPositions, hit.point);
        if (currentSnapPoint == null) return;

        player.transform.position = currentSnapPoint.position;
        player.transform.rotation = currentSnapPoint.rotation;
        snapExitTimer             = 0;
        player.enableMovement     = false;
        player.enableClimbing     = false;

        if (internalCinemachine != null)
        {
            Transform target = interactable.LookAtTarget ?? hit.transform;
            internalCinemachine.LookAt = target;
            if (target != null) playerCamera.transform.LookAt(target);

            foreach (string name in componentsToLock)
            {
                var comp = internalCinemachine.GetComponent(name) as Behaviour;
                if (comp != null) comp.enabled = false;
            }
        }
    }

    void HandleSnapLock()
    {
        if (currentSnapPoint == null) return;

        Vector2 moveInput = player.input.PlayerInputMap.MoveInput.ReadValue<Vector2>();
        bool    jump      = player.input.PlayerInputMap.JumpInput.triggered;

        if (moveInput.magnitude > 0.1f || jump)
        {
            snapExitTimer += Time.deltaTime;
            if (snapExitTimer >= snapExitDelay) ReleaseSnap();
        }
        else
        {
            snapExitTimer = 0;
        }
    }

    void ReleaseSnap()
    {
        currentSnapPoint      = null;
        snapExitTimer         = 0;
        player.enableMovement = true;
        player.enableClimbing = true;

        if (internalCinemachine != null)
        {
            internalCinemachine.LookAt = null;
            foreach (string name in componentsToLock)
            {
                var comp = internalCinemachine.GetComponent(name) as Behaviour;
                if (comp != null) comp.enabled = true;
            }
        }
    }

    Transform GetClosestSnapPoint(List<Transform> points, Vector3 hitPoint)
    {
        Transform closest = null;
        float     minDist = Mathf.Infinity;
        foreach (Transform t in points)
        {
            if (t == null) continue;
            float d = Vector3.Distance(hitPoint, t.position);
            if (d < minDist) { minDist = d; closest = t; }
        }
        return closest;
    }
}