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
    bool menuOpen = false;
    List<DoorAction> currentActions = new();
    Door currentDoor;
    Window currentWindow;

    // Styl GUI
    GUIStyle menuStyle;
    GUIStyle menuButtonStyle;
    GUIStyle disabledButtonStyle;

    string[] componentsToLock = {
        "CinemachineInputAxisController",
        "CinemachinePanTilt",
        "CinemachineOrbitalFollow",
        "CinemachineRotationHandler"
    };

    // ── Unity ─────────────────────────────────────────────────────
    void OnEnable() 
    { 
        interactAction.Enable(); 
        // Podpinamy się pod moment wykonania interakcji
        interactAction.performed += OnInteractPerformed; 
    }

    void OnDisable() 
    { 
        interactAction.Disable(); 
        interactAction.performed -= OnInteractPerformed; 
    }
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // 1. Najpierw robimy Raycast, żeby wiedzieć na co patrzymy
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance)) return;

        Door door = hit.collider.GetComponentInParent<Door>();
        if (door == null) return;

        // 2. Rozróżniamy Tap od Hold na podstawie ustawień z obrazka
        if (context.interaction is UnityEngine.InputSystem.Interactions.TapInteraction)
        {
            // KLIKNIĘCIE: Wykonaj pierwszą akcję z listy (Default Action)
            var actions = door.GetDoorActions();
            if (actions.Count > 0 && actions[0].enabled) 
            {
                actions[0].execute?.Invoke();
            }
        }
        else if (context.interaction is UnityEngine.InputSystem.Interactions.HoldInteraction)
        {
            // TRZYMANIE: Otwórz menu UI Toolkit (Rondo)
            HandleSnapping(door, hit);
            OpenMenu(door); // Ta metoda wywoła Twoje nowe UI
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
    void OpenMenu(Door door)
    {
        currentDoor = door;
        currentWindow = null;
        currentActions = door.GetDoorActions();
        menuOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OpenMenu(Window window)
    {
        currentWindow = window;
        currentDoor = null;
        currentActions = window.GetWindowActions();
        menuOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseMenu()
    {
        menuOpen = false;
        currentDoor = null;
        currentWindow = null;
        currentActions.Clear();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ExecuteAction(DoorAction action)
    {
        if (!action.enabled) return;
        if (action.duration > 0f)
            Debug.Log($"[Menu] Rozpoczynam: {action.label} ({action.duration}s)");
        action.execute?.Invoke();
        CloseMenu();
    }

    // ── Rysowanie menu (IMGUI) ────────────────────────────────────
    void OnGUI()
    {
        if (!menuOpen || currentActions == null || currentActions.Count == 0) return;

        InitStyles();

        float btnW = 220f;
        float btnH = 36f;
        float padding = 8f;
        float totalH = currentActions.Count * (btnH + padding) + padding;

        float x = Screen.width / 2f - btnW / 2f;
        float y = Screen.height / 2f - totalH / 2f;

        GUI.Box(new Rect(x - 10, y - 10, btnW + 20, totalH + 20), "", menuStyle);

        for (int i = 0; i < currentActions.Count; i++)
        {
            var action = currentActions[i];
            Rect btnRect = new Rect(x, y + i * (btnH + padding), btnW, btnH);

            if (action.enabled)
            {
                if (GUI.Button(btnRect, action.label, menuButtonStyle))
                    ExecuteAction(action);
            }
            else
            {
                GUI.Label(btnRect, new GUIContent(action.label, action.disabledReason), disabledButtonStyle);
            }
        }
    }

    void InitStyles()
    {
        if (menuStyle != null) return;

        menuStyle = new GUIStyle(GUI.skin.box);
        menuStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.85f));

        menuButtonStyle = new GUIStyle(GUI.skin.button);
        menuButtonStyle.fontSize = 14;
        menuButtonStyle.normal.textColor = Color.white;
        menuButtonStyle.hover.textColor = Color.yellow;

        disabledButtonStyle = new GUIStyle(GUI.skin.label);
        disabledButtonStyle.fontSize = 14;
        disabledButtonStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
        disabledButtonStyle.alignment = TextAnchor.MiddleCenter;
    }

    Texture2D MakeTex(int w, int h, Color col)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var tex = new Texture2D(w, h);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
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