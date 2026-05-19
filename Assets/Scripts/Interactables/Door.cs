using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
//  Door  —  unified Door / Window script
//
//  PREFAB SETUP (window)
//  ┌─ Window GameObject  [Door script] [NavMeshObstacle] [NavMeshLink]
//  ├── GlassMesh          → assign to 'breakableMesh'
//  └── Frame              → static, never touched
//
//  NavMeshLink konfiguruj bezpośrednio w prefabie (Start/End Point lub
//  Start/End Transform na poziomie podłogi). Skrypt tylko przełącza
//  activated i nie dotyka żadnych innych pól linku.
//
//  NAVMESH LOGIC (windows):
//    Closed  → Obstacle ON,  Link OFF
//    Open    → Obstacle OFF, Link ON
//    Broken  → Obstacle OFF, Link ON
// ─────────────────────────────────────────────────────────────────────────────
public class Door : MonoBehaviour, IInteractable
{
    // ── Mode ──────────────────────────────────────────────────────
    [Header("Mode")]
    [Tooltip("Enable for windows: adds NavMeshObstacle/Link logic and break behaviour.")]
    public bool isWindow = false;

    // ── State machine ─────────────────────────────────────────────
    public enum OpenableState  { Closed, Open, Locked, Broken }
    public enum DoorActionType { Opening, Closing, Locking, Unlocking, OpeningForce, Breaking, Climbing }

    public OpenableState state = OpenableState.Closed;

    // ── Rotation ──────────────────────────────────────────────────
    [Header("Rotation")]
    public float openAngle    = 90f;
    public float openDuration = 0.5f;

    // ── Breakable mesh ────────────────────────────────────────────
    [Header("Breakable (Window / Forced Door)")]
    [Tooltip("Child GO with the glass/door mesh + collider. Disabled on break.")]
    public GameObject breakableMesh;
    [Tooltip("Optional particle/debris prefab spawned on break.")]
    public GameObject breakFX;

    // ── NavMesh (Window only) ─────────────────────────────────────
    [Header("NavMesh (Window only)")]
    [Tooltip("Enemies within this radius are alerted when glass breaks.")]
    public float enemyAlertRadius = 20f;

    // ── Interaction snapping ──────────────────────────────────────
    [Header("Interaction")]
    [SerializeField] List<Transform> interactionPositions = new();
    [SerializeField] Transform lookAtTarget;

    public bool UseSnapping                     => false;
    public List<Transform> InteractionPositions => interactionPositions;
    public Transform LookAtTarget               => lookAtTarget;

    // ── NavMesh internals ─────────────────────────────────────────
    NavMeshObstacle _obstacle;
    NavMeshLink     _link;

    // ── Rotation internals ────────────────────────────────────────
    Quaternion _closedRot;
    Quaternion _openRot;
    bool       _isAnimating;

    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (!isWindow) return;

        _obstacle = GetComponent<NavMeshObstacle>();
        if (_obstacle != null)
        {
            _obstacle.carving             = true;
            _obstacle.carveOnlyStationary = false;
            _obstacle.enabled             = true;   // starts blocking
        }

        // Pobieramy NavMeshLink z prefaba — nie tworzymy nowego.
        // Skonfiguruj Start/End Point (lub Transform) bezposrednio w Inspectorze.
        _link = GetComponent<NavMeshLink>();
        if (_link != null)
        {
            _link.activated = false;                // starts disabled
        }
        else
        {
            Debug.LogWarning("[Door] '" + name + "' isWindow=true, ale brak komponentu NavMeshLink na tym GameObject. Dodaj go w prefabie.", this);
        }
    }

    void Start()
    {
        _closedRot = transform.localRotation;
        _openRot   = _closedRot * Quaternion.Euler(0f, openAngle, 0f);

        // Sync NavMesh do stanu startowego
        if (isWindow)
            UpdateWindowNavMesh(passable: state == OpenableState.Open || state == OpenableState.Broken);
    }

    // ── IInteractable ─────────────────────────────────────────────
    public void OnInteract() { }

    // ── Context menu ──────────────────────────────────────────────
    public List<DoorAction> GetDoorActions()
    {
        return isWindow ? BuildWindowActions() : BuildDoorActions();
    }

    List<DoorAction> BuildDoorActions()
    {
        var actions = new List<DoorAction>();
        switch (state)
        {
            case OpenableState.Closed:
                actions.Add(new DoorAction { label = "Otworz",           enabled = true, execute = ActionOpen,      type = DoorActionType.Opening      });
                actions.Add(new DoorAction { label = "Zamknij na klucz", enabled = true, execute = ActionLock,      type = DoorActionType.Locking      });
                break;
            case OpenableState.Open:
                actions.Add(new DoorAction { label = "Zamknij",          enabled = true, execute = ActionClose,     type = DoorActionType.Closing      });
                break;
            case OpenableState.Locked:
                actions.Add(new DoorAction { label = "Odblokuj",         enabled = true, execute = ActionUnlock,    type = DoorActionType.Unlocking    });
                actions.Add(new DoorAction { label = "Otworz (wywaz)",   enabled = true, execute = ActionBreakOpen, type = DoorActionType.OpeningForce, duration = 3f });
                break;
        }
        return actions;
    }

    List<DoorAction> BuildWindowActions()
    {
        var actions = new List<DoorAction>();
        switch (state)
        {
            case OpenableState.Closed:
                actions.Add(new DoorAction { label = "Otworz okno",       enabled = true, execute = ActionOpen,  type = DoorActionType.Opening  });
                actions.Add(new DoorAction { label = "Wybij szybe",        enabled = true, execute = ActionBreak, type = DoorActionType.Breaking });
                break;
            case OpenableState.Open:
                actions.Add(new DoorAction { label = "Zamknij okno",       enabled = true, execute = ActionClose, type = DoorActionType.Closing  });
                actions.Add(new DoorAction { label = "Wybij szybe",        enabled = true, execute = ActionBreak, type = DoorActionType.Breaking });
                break;
            case OpenableState.Broken:
                actions.Add(new DoorAction { label = "Przlez przez okno",  enabled = true, execute = ActionClimb, type = DoorActionType.Climbing });
                break;
        }
        return actions;
    }

    // ── Actions ───────────────────────────────────────────────────
    void ActionOpen()
    {
        if (_isAnimating) return;
        state = OpenableState.Open;
        if (isWindow) UpdateWindowNavMesh(passable: true);
        StartCoroutine(RotateTo(_openRot));
    }

    void ActionClose()
    {
        if (_isAnimating) return;
        state = OpenableState.Closed;
        if (isWindow) UpdateWindowNavMesh(passable: false);
        StartCoroutine(RotateTo(_closedRot));
    }

    void ActionLock()
    {
        if (_isAnimating) return;
        state = OpenableState.Locked;
        StartCoroutine(RotateTo(_closedRot));
    }

    void ActionUnlock()
    {
        state = OpenableState.Closed;
    }

    void ActionBreakOpen()
    {
        if (_isAnimating) return;
        state = OpenableState.Open;
        StartCoroutine(RotateTo(_openRot));
    }

    public void ActionBreak()
    {
        if (state == OpenableState.Broken) return;
        state = OpenableState.Broken;

        if (breakFX != null)
            Instantiate(breakFX, transform.position, Quaternion.identity);

        DisableBreakableMesh();

        if (isWindow)
        {
            UpdateWindowNavMesh(passable: true);
            // TODO: NotifyNearbyEnemies();
        }
    }

    void ActionClimb()
    {
        Debug.Log("[Door] Player climbing through " + gameObject.name);
    }

    // ── NavMesh helper ────────────────────────────────────────────
    void UpdateWindowNavMesh(bool passable)
    {
        if (!isWindow) return;
        if (_obstacle != null) _obstacle.enabled = !passable;
        if (_link     != null) _link.activated   =  passable;
    }

    // ── Helpers ───────────────────────────────────────────────────
    void DisableBreakableMesh()
    {
        if (breakableMesh != null)
        {
            breakableMesh.SetActive(false);
            return;
        }

        foreach (Transform child in transform)
        {
            var rend = child.GetComponent<Renderer>();
            var col  = child.GetComponent<Collider>();
            if (rend != null) rend.enabled = false;
            if (col  != null) col.enabled  = false;
        }
    }

    public bool IsPassableByAI => isWindow && (state == OpenableState.Open || state == OpenableState.Broken);

    // ── Rotation coroutine ────────────────────────────────────────
    IEnumerator RotateTo(Quaternion target)
    {
        _isAnimating = true;
        Quaternion start   = transform.localRotation;
        float      elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            transform.localRotation = Quaternion.Lerp(start, target,
                Mathf.SmoothStep(0f, 1f, elapsed / openDuration));
            yield return null;
        }

        transform.localRotation = target;
        _isAnimating = false;
    }
}

public class DoorAction
{
    public string              label;
    public bool                enabled;
    public string              disabledReason;
    public float               duration;
    public System.Action       execute;
    public Door.DoorActionType type;
}