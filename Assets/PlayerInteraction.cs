using UnityEngine;
using System.Collections.Generic;

public interface IInteractable {
    void OnInteract();
    bool UseSnapping { get; }
    List<Transform> InteractionPositions { get; }
}

public class PlayerInteraction : MonoBehaviour {
    public bool debugMode;
    public KCC player;
    public Camera playerCamera;
    public float interactionDistance = 3f;
    public float snapExitDelay = 0.5f;

    Transform currentSnapPoint;
    float snapExitTimer;

    void Update() {
        LookForInteraction();
        HandleSnapLock();
    }

    void LookForInteraction() {
        if (!player.input.PlayerInputMap.InteractInput.WasPressedThisFrame()) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactionDistance)) return;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable == null) return;

        if (interactable.UseSnapping && interactable.InteractionPositions != null && interactable.InteractionPositions.Count > 0) {
            currentSnapPoint = GetClosestSnapPoint(interactable.InteractionPositions);
            if (currentSnapPoint != null) {
                player.transform.position = currentSnapPoint.position;
                player.transform.rotation = currentSnapPoint.rotation;
                snapExitTimer = 0;
                player.enableMovement = false;
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
                currentSnapPoint = null;
                snapExitTimer = 0;
                player.enableMovement = true;
            }
        } else {
            snapExitTimer = 0;
        }
    }

    Transform GetClosestSnapPoint(List<Transform> points) {
        Transform closest = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = player.transform.position;
        Vector3 forward = player.transform.forward;
        Transform fallback = null;
        float fallbackDist = Mathf.Infinity;

        foreach (Transform t in points) {
            if (t == null) continue;
            Vector3 dir = t.position - currentPos;
            float dist = dir.magnitude;
            if (dist < fallbackDist) {
                fallbackDist = dist;
                fallback = t;
            }
            float facingDot = Vector3.Dot(forward, dir.normalized);
            if (facingDot < 0.3f) continue;
            if (dist < minDist) {
                minDist = dist;
                closest = t;
            }
        }

        return closest != null ? closest : fallback;
    }
}
