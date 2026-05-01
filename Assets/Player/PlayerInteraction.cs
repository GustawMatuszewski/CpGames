using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.Rendering.HighDefinition;

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
    public KCC player;
    public Camera playerCamera;

    [Header("Settings")]
    public float interactionDistance = 3f;
    public float snapExitDelay = 0.5f;

    [Header("Input")]
    public InputAction interactAction;

    [Header("Debug")]
    public bool debugMode = true;

    // ── Stan wewnętrzny ───────────────────────────────────────────
    Transform currentSnapPoint;
    float snapExitTimer;
    CinemachineCamera internalCinemachine;

    // Menu kontekstowe

    List<DoorAction> currentActions = new();
    Door currentDoor;
    Window currentWindow;

 

    string[] componentsToLock = {
        "CinemachineInputAxisController",
        "CinemachinePanTilt",
        "CinemachineOrbitalFollow",
        "CinemachineRotationHandler"
    };
    private bool menuOpen=false;

    // ── Unity ─────────────────────────────────────────────────────
    void OnEnable() 
    { 
        interactAction.Enable(); 
        // Podpinamy się pod moment wykonania interakcji
        interactAction.performed += OnInteractPerformed; 
        interactAction.canceled += OnInteractCanceled;
    }

    void OnDisable() 
    { 
        interactAction.Disable(); 
        interactAction.performed -= OnInteractPerformed; 
        interactAction.canceled -= OnInteractCanceled;
    }
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // 1. Najpierw robimy Raycast, żeby wiedzieć na co patrzymy
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance)) return;

        Door door = hit.collider.GetComponentInParent<Door>();
        if (door == null) return;
        var actions = door.GetDoorActions();
        
        // 2. Rozróżniamy Tap od Hold na podstawie ustawień z obrazka
        if (context.interaction is UnityEngine.InputSystem.Interactions.TapInteraction)
        {
            // KLIKNIĘCIE: Wykonaj pierwszą akcję z listy (Default Action)
            
            if (actions.Count > 0 && actions[0].enabled) 
            {
                actions[0].execute?.Invoke();
            }
        }
        else if (context.interaction is UnityEngine.InputSystem.Interactions.HoldInteraction)
        {
           
            if (actions.Count == 1)
            {
                actions[0].execute?.Invoke();
                
            }
            else
            {
                // TRZYMANIE: Otwórz menu UI Toolkit (Rondo)
                
                HandleSnapping(door, hit);
                OpenMenu(actions); // Ta metoda wywoła Twoje nowe UI
                menuOpen = true;
            }

        }
    }

    void Start()
    {
        if (playerCamera != null)
        {
            internalCinemachine = playerCamera.GetComponent<CinemachineCamera>();
            if (internalCinemachine == null)
                internalCinemachine = playerCamera.GetComponentInParent<CinemachineCamera>();
        }
    }

    void Update()
    {
        HandleSnapLock();//Kiedys to wypierdzziele Wieze w to --- Windforce
    }

    private void OnInteractCanceled(InputAction.CallbackContext context)
    {

        if (menuOpen)
        {
            // Pobieramy zaznaczoną akcję z Twojego UI
            DoorAction selectedAction = UI_Interaction.Instance.SelectRondoAction(); //pobiera akcje i zmayka menu

            if (selectedAction != null && selectedAction.enabled)
            {
                // Wykonujemy zapisaną logikę (np. DoorOpen)
                selectedAction.execute?.Invoke();
                // Debug.Log($"[Hold Exit] Wykonano akcję: {selectedAction.label}");
            }

            Cursor.lockState = CursorLockMode.Locked;
            menuOpen = false;
        }
    }


    void HandleSnapping(IInteractable interactable, RaycastHit hit)
    {
        if (!interactable.UseSnapping || interactable.InteractionPositions == null) return;
        if (interactable.InteractionPositions.Count == 0) return;

        currentSnapPoint = GetClosestSnapPoint(interactable.InteractionPositions, hit.point);
        if (currentSnapPoint == null) return;

        player.transform.position = currentSnapPoint.position;
        player.transform.rotation = currentSnapPoint.rotation;
        snapExitTimer = 0;
        player.enableMovement = false;
        player.enableClimbing = false;

        if (internalCinemachine != null)
        {
            Transform targetToLookAt = interactable.LookAtTarget ?? hit.transform;
            internalCinemachine.LookAt = targetToLookAt;
            if (targetToLookAt != null)
                playerCamera.transform.LookAt(targetToLookAt);
            foreach (string name in componentsToLock)
            {
                var comp = internalCinemachine.GetComponent(name) as Behaviour;
                if (comp != null) comp.enabled = false;
            }
        }
    }

    // ── Menu kontekstowe ──────────────────────────────────────────
    public void OpenMenu(List<DoorAction> actions)
    {
        // Czyścimy starą listę i kopiujemy nową
        currentActions = actions; 
       

        // Przekazujemy do Twojego UI Toolkit (Rondo)
        // Musisz tam mieć metodę, która przyjmuje List<DoorAction>
        UI_Interaction.Instance.SendRondoActions(actions); 

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OpenMenu(Window window)
    {
        currentWindow = window;
        currentDoor = null;
        currentActions = window.GetWindowActions();
   
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }







    // ── Snap exit ─────────────────────────────────────────────────
    void HandleSnapLock()
    {
        if (currentSnapPoint == null) return;

        Vector2 moveInput = player.input.PlayerInputMap.MoveInput.ReadValue<Vector2>();
        bool jumpInput = player.input.PlayerInputMap.JumpInput.triggered;

        if (moveInput.magnitude > 0.1f || jumpInput)
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
        currentSnapPoint = null;
        snapExitTimer = 0;
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
        float minDist = Mathf.Infinity;
        foreach (Transform t in points)
        {
            if (t == null) continue;
            float dist = Vector3.Distance(hitPoint, t.position);
            if (dist < minDist) { minDist = dist; closest = t; }
        }
        return closest;
    }
}