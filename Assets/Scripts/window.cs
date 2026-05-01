using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Window : MonoBehaviour, IInteractable
{

    // ── Stan okna ─────────────────────────────────────────────────
    public enum WindowState { Closed, Open, Broken }
    public WindowState state = WindowState.Closed;

    [Header("Rotation")]
    public float openAngle = 90f;
    public float openDuration = 0.5f;

    // ── Snapping ──────────────────────────────────────────────────
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
    public List<DoorAction> GetWindowActions()
    {
        var actions = new List<DoorAction>();

        switch (state)
        {
            case WindowState.Closed:
                actions.Add(new DoorAction
                {
                    label = "Otwórz okno",
                    enabled = true,
                    execute = WindowOpen
                });
                actions.Add(new DoorAction
                {
                    label = "Wybij szybę",
                    enabled = true,
                    duration = 0f,
                    execute = WindowBreak
                });
                break;

            case WindowState.Open:
                actions.Add(new DoorAction
                {
                    label = "Zamknij okno",
                    enabled = true,
                    execute = WindowClose
                });
                actions.Add(new DoorAction
                {
                    label = "Wybij szybę",
                    enabled = true,
                    execute = WindowBreak
                });
                break;

            case WindowState.Broken:
                actions.Add(new DoorAction
                {
                    label = "Przeleź przez okno",
                    enabled = true,
                    execute = WindowClimb
                });
                break;
        }

        return actions;
    }

    // ── Akcje ─────────────────────────────────────────────────────
    void WindowOpen()
    {
        if (isAnimating) return;
        state = WindowState.Open;
        StartCoroutine(RotateTo(openRotation));
    }

    void WindowClose()
    {
        if (isAnimating) return;
        state = WindowState.Closed;
        StartCoroutine(RotateTo(closedRotation));
    }

    void WindowBreak()
    {
        if (state == WindowState.Broken) return;
        state = WindowState.Broken;
        Destroy(gameObject);
    }

    void WindowClimb()
    {
        // Tutaj możesz podpiąć animację gracza / teleport przez okno
        Debug.Log("Gracz przełazi przez okno");
    }

    // ── Animacje ──────────────────────────────────────────────────
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