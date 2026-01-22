using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

public interface IInteractable {
    void OnInteract();
    bool UseSnapping { get; }
    List<Transform> InteractionPositions { get; }
    Transform LookAtTarget { get; }
}

public class PlayerInteraction : MonoBehaviour {
    public bool debugMode;
    public KCC player;
    public Camera playerCamera;
    public float interactionDistance = 3f;
    public float snapExitDelay = 0.5f;

    Transform currentSnapPoint;
    float snapExitTimer;
    CinemachineCamera internalCinemachine;

    // List of component names to disable when locked (New Cinemachine v3)
    string[] componentsToLock = {
        "CinemachineInputAxisController",
        "CinemachinePanTilt",
        "CinemachineOrbitalFollow",
        "CinemachineRotationHandler"
    };

    void Start() {
        if (playerCamera != null) {
            // Try to find the component on the camera or its parent
            internalCinemachine = playerCamera.GetComponent<CinemachineCamera>();
            if (internalCinemachine == null) internalCinemachine = playerCamera.GetComponentInParent<CinemachineCamera>();
        }
    }

    void Update() {
        LookForInteraction();
        HandleSnapLock();
    }

    void LookForInteraction() {
        if (!player.input.PlayerInputMap.InteractInput.WasPressedThisFrame()) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance)) return;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable == null) return;

        if (interactable.UseSnapping && interactable.InteractionPositions?.Count > 0) {
            currentSnapPoint = GetClosestSnapPoint(interactable.InteractionPositions, hit.point);
            if (currentSnapPoint != null) {
                // 1. Snap Player Body Position & Rotation
                player.transform.position = currentSnapPoint.position;
                player.transform.rotation = currentSnapPoint.rotation;
                
                snapExitTimer = 0;
                player.enableMovement = false;
                player.enableClimbing = false;

                if (internalCinemachine != null) {
                    // 2. Determine what to look at
                    // Priority: Explicit LookAtTarget > The object we hit > The snap point forward
                    Transform targetToLookAt = interactable.LookAtTarget;
                    if (targetToLookAt == null) targetToLookAt = hit.transform;

                    internalCinemachine.LookAt = targetToLookAt;

                    // 3. FORCE the camera to face the target immediately
                    // (We must do this manually because we are about to disable the components that usually do it)
                    if (targetToLookAt != null) {
                        playerCamera.transform.LookAt(targetToLookAt);
                    }

                    // 4. Disable all input/rotation components to FREEZE it there
                    foreach (string name in componentsToLock) {
                        var comp = internalCinemachine.GetComponent(name) as Behaviour;
                        if (comp != null) comp.enabled = false;
                    }
                }
            }
        }

        interactable.OnInteract();
    }

    void HandleSnapLock() {
        if (currentSnapPoint == null) return;

        Vector2 moveInput = player.input.PlayerInputMap.MoveInput.ReadValue<Vector2>();
        bool jumpInput = player.input.PlayerInputMap.JumpInput.triggered;

        if (moveInput.magnitude > 0.1f || jumpInput) {
            snapExitTimer += Time.deltaTime;
            if (snapExitTimer >= snapExitDelay) {
                ReleaseSnap();
            }
        } else {
            snapExitTimer = 0;
        }
    }

    void ReleaseSnap() {
        currentSnapPoint = null;
        snapExitTimer = 0;
        player.enableMovement = true;
        player.enableClimbing = true;

        if (internalCinemachine != null) {
            internalCinemachine.LookAt = null;

            // Re-enable all input/rotation components
            foreach (string name in componentsToLock) {
                var comp = internalCinemachine.GetComponent(name) as Behaviour;
                if (comp != null) comp.enabled = true;
            }
        }
    }

    Transform GetClosestSnapPoint(List<Transform> points, Vector3 hitPoint) {
        Transform closest = null;
        float minDist = Mathf.Infinity;
        foreach (Transform t in points) {
            if (t == null) continue;
            float dist = Vector3.Distance(hitPoint, t.position);
            if (dist < minDist) {
                minDist = dist;
                closest = t;
            }
        }
        return closest;
    }
}