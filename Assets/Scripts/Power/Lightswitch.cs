using UnityEngine;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
//  LightSwitch
//
//  Włącznik ścienny implementujący IInteractable (tak jak Door).
//  Steruje listą podłączonych żarówek (LightBulb).
//
//  ZASADY:
//   • Interakcja (klawisz E) → isSwitchOn = !isSwitchOn
//   • Żarówki świecą TYLKO gdy: isSwitchOn == true ORAZ isGlobalPowerOn == true
//   • Subskrybuje EnvironmentManager.OnPowerCut → natychmiastowe zgaszenie
//
//  PREFAB SETUP
//  ┌─ SwitchObject  [LightSwitch script]
//  └── (w Inspektorze) przypisz referencje do LightBulb na scenie
// ─────────────────────────────────────────────────────────────────────────────
public class LightSwitch : MonoBehaviour, IInteractable
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Switch State")]
    [Tooltip("Aktualny stan włącznika (możesz ustawić startowy stan w Inspektorze).")]
    public bool isSwitchOn = false;

    [Header("Connected Bulbs")]
    [Tooltip("Lista żarówek sterowanych przez ten włącznik.")]
    public List<LightBulb> _connectedBulbs = new List<LightBulb>();

    [Header("Interaction")]
    [SerializeField] private List<Transform> _interactionPositions = new();
    [SerializeField] private Transform       _lookAtTarget;

    // ── IInteractable ─────────────────────────────────────────────
    public bool           UseSnapping          => false;
    public List<Transform> InteractionPositions => _interactionPositions;
    public Transform      LookAtTarget         => _lookAtTarget;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void OnEnable()
    {
        // Subskrybujemy event odcięcia prądu
        EnvironmentManager.OnPowerCut += HandlePowerCut;
    }

    void OnDisable()
    {
        // Zawsze wypisujemy się z eventów w OnDisable — unikamy memory leaków
        EnvironmentManager.OnPowerCut -= HandlePowerCut;
    }

    void Start()
    {
        // Synchronizujemy żarówki ze stanem startowym włącznika
        RefreshBulbs();
    }

    // ─────────────────────────────────────────────────────────────
    //  IInteractable
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Wywoływane przez system interakcji gracza (klawisz E).
    /// Odwraca stan włącznika i odświeża żarówki.
    /// </summary>
    public void OnInteract()
    {
        // Jeśli prąd jest odcięty, włącznik fizycznie nie działa
        if (EnvironmentManager.Instance != null && !EnvironmentManager.Instance.isGlobalPowerOn)
        {
            Debug.Log($"[LightSwitch] '{name}': brak prądu w sieci — włącznik nie reaguje.");
            return;
        }

        isSwitchOn = !isSwitchOn;
        RefreshBulbs();

        Debug.Log($"[LightSwitch] '{name}': stan → {(isSwitchOn ? "ON" : "OFF")}");
    }

    // ─────────────────────────────────────────────────────────────
    //  POWER CUT HANDLER
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Wywoływane przez EnvironmentManager.OnPowerCut.
    /// Gasimy żarówki natychmiast, bez zmiany stanu włącznika
    /// (switch pamięta że jest "ON", ale prądu nie ma).
    /// </summary>
    private void HandlePowerCut()
    {
        Debug.Log($"[LightSwitch] '{name}': prąd odcięty — gaszę żarówki.");
        SetBulbs(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Odświeża stan żarówek na podstawie aktualnego stanu włącznika i sieci.
    /// </summary>
    private void RefreshBulbs()
    {
        bool globalPowerOn = EnvironmentManager.Instance == null || EnvironmentManager.Instance.isGlobalPowerOn;
        bool shouldLight   = isSwitchOn && globalPowerOn;
        SetBulbs(shouldLight);
    }

    /// <summary>Włącza lub wyłącza wszystkie podłączone żarówki.</summary>
    private void SetBulbs(bool on)
    {
        foreach (LightBulb bulb in _connectedBulbs)
        {
            if (bulb != null)
                bulb.SetLight(on);
        }
    }
}