using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
//  Door  —  unified Door / Window script
//
//  PREFAB SETUP
//  ┌─ Door  [this script]  (+NavMeshObstacle only if isWindow = true)
//  ├── GlassMesh / DoorMesh      → assign to 'breakableMesh' (optional)
//  ├── Frame                     → static, never touched
//  ├── LinkStart                 → assign only for windows
//  └── LinkEnd                   → assign only for windows
//
//  DOOR prefab  : isWindow = false  — NavMeshObstacle NOT required
//  WINDOW prefab: isWindow = true   — add NavMeshObstacle component manually
// ─────────────────────────────────────────────────────────────────────────────
public class Door : MonoBehaviour, IInteractable
{
    // ── Mode ──────────────────────────────────────────────────────
    [Header("Mode")]
    [Tooltip("Enable for windows: adds NavMeshObstacle/Link logic and break behaviour.")]
    public bool isWindow = false;

    // ── State machine ─────────────────────────────────────────────
    //   Doors:   Closed ↔ Open ↔ Locked
    //   Windows: Closed ↔ Open → Broken  (one-way break)
    public enum OpenableState  { Closed, Open, Locked, Broken }
    public enum DoorActionType { Opening, Closing, Locking, Unlocking, OpeningForce, Breaking, Climbing,Deconstructing }

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
    [Header("NavMesh Link (Window only)")]
    [Tooltip("Empty Transform on the outside face of the sill.")]
    public Transform linkStart;
    [Tooltip("Empty Transform on the inside face of the sill.")]
    public Transform linkEnd;
    [Tooltip("Enemies within this radius repath when glass breaks.")]
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
    GameObject      _linkGO;

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
            _obstacle.carving              = true;
            _obstacle.carveOnlyStationary = false;
            _obstacle.enabled             = true;
        }

        _linkGO = new GameObject("DoorNavMeshLink");
        _linkGO.transform.SetParent(transform, worldPositionStays: false);

        _link                = _linkGO.AddComponent<NavMeshLink>();
        _link.startTransform = linkStart;
        _link.endTransform   = linkEnd;
        _link.bidirectional  = true;
        _link.activated      = false;
        _link.autoUpdate     = true;
        _link.width          = 1.2f;
    }

    void Start()
    {
        _closedRot = transform.localRotation;
        _openRot   = _closedRot * Quaternion.Euler(0f, openAngle, 0f);
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
                actions.Add(new DoorAction { label = "Otwórz",           enabled = true, execute = ActionOpen,      type = DoorActionType.Opening      });
                actions.Add(new DoorAction { label = "Zamknij na klucz", enabled = true, execute = ActionLock,      type = DoorActionType.Locking      });
                break;
            case OpenableState.Open:
                actions.Add(new DoorAction { label = "Zamknij",          enabled = true, execute = ActionClose,     type = DoorActionType.Closing      });
                break;
            case OpenableState.Locked:
                actions.Add(new DoorAction { label = "Odblokuj",         enabled = true, execute = ActionUnlock,    type = DoorActionType.Unlocking    });
                actions.Add(new DoorAction { label = "Otwórz (wyważ)",   enabled = true, execute = ActionBreakOpen, type = DoorActionType.OpeningForce, duration = 3f });
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
                actions.Add(new DoorAction { label = "Otwórz okno",       enabled = true, execute = ActionOpen,  type = DoorActionType.Opening  });
                actions.Add(new DoorAction { label = "Wybij szybę",        enabled = true, execute = ActionBreak, type = DoorActionType.Breaking });
                break;
            case OpenableState.Open:
                actions.Add(new DoorAction { label = "Zamknij okno",       enabled = true, execute = ActionClose, type = DoorActionType.Closing  });
                actions.Add(new DoorAction { label = "Wybij szybę",        enabled = true, execute = ActionBreak, type = DoorActionType.Breaking });
                break;
            case OpenableState.Broken:
                actions.Add(new DoorAction { label = "Przeleź przez okno", enabled = true, execute = ActionClimb, type = DoorActionType.Climbing });
                break;
        }
        return actions;
    }

    // ── Actions ───────────────────────────────────────────────────
    void ActionOpen()
    {
        if (_isAnimating) return;
        state = OpenableState.Open;
        StartCoroutine(RotateTo(_openRot));
    }

    void ActionClose()
    {
        if (_isAnimating) return;
        state = OpenableState.Closed;
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
        // DisableBreakableMesh(); well this is for breaking
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
            if (_obstacle != null) _obstacle.enabled = false;
            if (_link     != null) _link.activated   = true;
            // TODO: enemy — NotifyNearbyEnemies();
        }
    }

    void ActionClimb()
    {
        // TODO: hook player vault / climb animation here
        Debug.Log($"[Door] Player climbing through {gameObject.name}");
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
            if (_linkGO != null && child.gameObject == _linkGO) continue;
            var rend = child.GetComponent<Renderer>();
            var col  = child.GetComponent<Collider>();
            if (rend != null) rend.enabled = false;
            if (col  != null) col.enabled  = false;
        }
    }

    // EnemyEntity: GetComponentInParent<Door>() != null → vault traversal
    // TODO: enemy — wire EnemyEntity to use Door instead of Window
    public GameObject LinkGameObject => _linkGO;

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


// ─────────────────────────────────────────────────────────────────────────────
//  DoorAction — unchanged, UI branch compiles as-is
// ─────────────────────────────────────────────────────────────────────────────
public class DoorAction
{
    public string              label;
    public bool                enabled;
    public string              disabledReason;
    public float               duration;
    public System.Action       execute;
    public Door.DoorActionType type;
}