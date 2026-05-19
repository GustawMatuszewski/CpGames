using UnityEngine;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
//  Bed  —  interactable łóżko
//
//  PREFAB SETUP
//  ┌─ Bed GameObject  [Bed script] [Collider]
//  └── (opcjonalnie meshes, particles itp.)
//
//  Implementuje IInteractable — PlayerInteraction wykrywa go przez raycast
//  i wywołuje OnInteract(), które przekazuje kontrolę do SleepManager.
// ─────────────────────────────────────────────────────────────────────────────
public class Bed : MonoBehaviour, IInteractable
{
    // ── Konfiguracja ──────────────────────────────────────────────
    [Header("Sleep Settings")]
    [Tooltip("Ile godzin gry trwa sen na tym łóżku.")]
    public int hoursToSleep = 8;

    // ── IInteractable — snapping (Bed go nie używa) ───────────────
    [Header("Interaction (opcjonalne)")]
    [SerializeField] private List<Transform> interactionPositions = new();
    [SerializeField] private Transform lookAtTarget;

    public bool UseSnapping                     => false;
    public List<Transform> InteractionPositions => interactionPositions;
    public Transform LookAtTarget               => lookAtTarget;

    // ── Cache ─────────────────────────────────────────────────────
    EntityStatus _playerStatus;

    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        // KCC istnieje tylko na graczu — GetComponentInParent nie zadziała
        // (łóżko nie jest dzieckiem gracza), więc szukamy KCC w scenie,
        // a następnie bierzemy EntityStatus z tego samego GameObject.
        KCC kcc = FindAnyObjectByType<KCC>();
        if (kcc != null)
            _playerStatus = kcc.GetComponent<EntityStatus>();

        if (_playerStatus == null)
            Debug.LogWarning("[Bed] Nie znaleziono EntityStatus gracza (przez KCC).", this);
    }

    // ── IInteractable ─────────────────────────────────────────────
    /// <summary>
    /// Wywoływane przez PlayerInteraction przy interakcji z łóżkiem.
    /// Oddelegowuje całą logikę snu do SleepManager, zachowując
    /// separację odpowiedzialności.
    /// </summary>
    public void OnInteract()
    {
        if (SleepManager.Instance == null)
        {
            Debug.LogError("[Bed] Brak SleepManager w scenie!", this);
            return;
        }

        if (_playerStatus == null)
        {
            Debug.LogError("[Bed] Brak EntityStatus gracza — nie można zasnąć.", this);
            return;
        }

        SleepManager.Instance.ExecuteSleep(_playerStatus, hoursToSleep);
    }
}