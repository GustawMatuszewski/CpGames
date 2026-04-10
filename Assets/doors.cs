using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Door : MonoBehaviour, IInteractable
{

    // ── Stan drzwi ──────────────────────────────────────────────
    public enum DoorState { Closed, Open, Locked }
    public DoorState state = DoorState.Closed;

    // ── Ustawienia obrotu ─────────────────────────────────────────
    [Header("Rotation")]
    public float openAngle = 90f;
    public float openDuration = 0.5f;

    // ── Snapping ─────────────────────────────────────────────────
    [Header("Interaction")]
    [SerializeField] List<Transform> interactionPositions = new();
    [SerializeField] Transform lookAtTarget;

    public bool UseSnapping => false;
    public List<Transform> InteractionPositions => interactionPositions;
    public Transform LookAtTarget => lookAtTarget;

    // ── Stan wewnętrzny ───────────────────────────────────────────
    Quaternion closedRotation;
    Quaternion openRotation;
    bool isAnimating = false;

    void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    // ── IInteractable ─────────────────────────────────────────────
    public void OnInteract() { }

    // ── Menu kontekstowe ──────────────────────────────────────────
    public List<DoorAction> GetDoorActions()
    {
        var actions = new List<DoorAction>();

        switch (state)
        {
            case DoorState.Closed:
                actions.Add(new DoorAction
                {
                    label = "Otwórz",
                    enabled = true,
                    execute = DoorOpen
                });
                actions.Add(new DoorAction
                {
                    label = "Zamknij na klucz",
                    enabled = true,
                    execute = DoorLock
                });
                break;

            case DoorState.Open:
                actions.Add(new DoorAction
                {
                    label = "Zamknij",
                    enabled = true,
                    execute = DoorClose
                });
                break;

            case DoorState.Locked:
                actions.Add(new DoorAction
                {
                    label = "Otwórz (wyważ)",
                    enabled = true,
                    duration = 3f,
                    execute = DoorOpen
                });
                actions.Add(new DoorAction
                {
                    label = "Odblokuj",
                    enabled = true,
                    execute = DoorUnlock
                });
                break;
        }

        return actions;
    }

    // ── Akcje ─────────────────────────────────────────────────────
    void DoorOpen()
    {
        if (isAnimating) return;
        state = DoorState.Open;
        StartCoroutine(RotateTo(openRotation));
    }

    void DoorClose()
    {
        if (isAnimating) return;
        state = DoorState.Closed;
        StartCoroutine(RotateTo(closedRotation));
    }

    void DoorLock()
    {
        if (isAnimating) return;
        state = DoorState.Locked;
        StartCoroutine(RotateTo(closedRotation));
    }

    void DoorUnlock()
    {
        state = DoorState.Closed;
    }

    // ── Animacja obrotu ───────────────────────────────────────────
    IEnumerator RotateTo(Quaternion target)
    {
        isAnimating = true;
        Quaternion start = transform.localRotation;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
            transform.localRotation = Quaternion.Lerp(start, target, t);
            yield return null;
        }

        transform.localRotation = target;
        isAnimating = false;
    }
}

// ── Struktura akcji ───────────────────────────────────────────────
public class DoorAction
{
    public string label;
    public bool enabled;
    public string disabledReason;
    public float duration;
    public System.Action execute;
}