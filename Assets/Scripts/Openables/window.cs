// using UnityEngine;
// using UnityEngine.AI;
// using Unity.AI.Navigation;          // NavMeshLink — requires "AI Navigation" package
// using System.Collections;
// using System.Collections.Generic;

// // ── Prefab hierarchy ──────────────────────────────────────────────────────────
// //
// //   Window  [this script + NavMeshObstacle]
// //   ├── GlassMesh        — breakable pane (MeshRenderer + Collider)
// //   │                      assign to the 'glassMesh' field
// //   ├── WindowFrame      — static brick/wood, never touched
// //   ├── LinkStart        — empty Transform, outside face of sill
// //   └── LinkEnd          — empty Transform, inside face of sill
// //
// // NavMeshLink is created at runtime on a child GO called "WindowNavMeshLink".
// // EnemyEntity identifies window links via GetComponentInParent<Window>().
// //
// // ── State machine ─────────────────────────────────────────────────────────────
// //   Intact  → NavMeshObstacle ENABLED (carves navmesh), NavMeshLink DISABLED
// //   Broken  → glassMesh hidden, obstacle DISABLED, NavMeshLink ENABLED
// //             Nearby enemies get InvalidateDestinationCache() so they repath.
// // ─────────────────────────────────────────────────────────────────────────────

// [RequireComponent(typeof(NavMeshObstacle))]
// public class Window : MonoBehaviour, IInteractable
// {
//     // ── State ─────────────────────────────────────────────────────
//     public enum WindowState { Closed, Open, Broken }
//     public WindowState state = WindowState.Closed;

//     // ── Rotation (open / close swing) ────────────────────────────
//     [Header("Rotation")]
//     public float openAngle    = 90f;
//     public float openDuration = 0.5f;

//     // ── References ────────────────────────────────────────────────
//     [Header("Glass")]
//     [Tooltip("Child GO with the glass mesh + collider. Disabled (not destroyed) on break.")]
//     public GameObject glassMesh;

//     [Tooltip("Optional break particle/debris prefab.")]
//     public GameObject breakFX;

//     [Header("NavMesh Link Anchors")]
//     [Tooltip("Empty Transform placed on the outside face of the window sill.")]
//     public Transform linkStart;
//     [Tooltip("Empty Transform placed on the inside face of the window sill.")]
//     public Transform linkEnd;

//     [Header("NavMesh")]
//     [Tooltip("Enemies within this radius have their path cache cleared when window breaks.")]
//     public float enemyAlertRadius = 20f;

//     // ── Interaction snapping ──────────────────────────────────────
//     [Header("Interaction")]
//     [SerializeField] List<Transform> interactionPositions = new();
//     [SerializeField] Transform lookAtTarget;

//     public bool UseSnapping                     => false;
//     public List<Transform> InteractionPositions => interactionPositions;
//     public Transform LookAtTarget               => lookAtTarget;

//     // ── NavMesh components ────────────────────────────────────────
//     private NavMeshObstacle _obstacle;
//     private NavMeshLink     _link;      // Unity.AI.Navigation — NOT the old OffMeshLink
//     private GameObject      _linkGO;    // dedicated child that owns the NavMeshLink

//     // ── Internal rotation state ───────────────────────────────────
//     Quaternion _closedRot;
//     Quaternion _openRot;
//     bool       _isAnimating;

//     // ─────────────────────────────────────────────────────────────
//     void Awake()
//     {
//         // NavMeshObstacle: carves the baked navmesh while window is intact.
//         _obstacle                    = GetComponent<NavMeshObstacle>();
//         _obstacle.carving              = true;
//         _obstacle.carveOnlyStationary = false;
//         _obstacle.enabled            = true;    // ON while intact — hole in navmesh is sealed by geometry

//         // NavMeshLink on its own child GO.
//         // Using a child keeps the link's local-space math isolated from
//         // this transform's rotation (the window swings open/closed).
//         _linkGO                  = new GameObject("WindowNavMeshLink");
//         _linkGO.transform.SetParent(transform, false);

//         _link                = _linkGO.AddComponent<NavMeshLink>();
//         _link.startTransform = linkStart;       // outside sill
//         _link.endTransform   = linkEnd;         // inside sill
//         _link.bidirectional  = true;
//         _link.activated      = false;           // closed until glass breaks
//         _link.autoUpdate     = true;            // re-evaluates if transforms move
//         _link.width          = 1.2f;            // roughly one agent wide
//     }

//     void Start()
//     {
//         _closedRot = transform.localRotation;
//         _openRot   = _closedRot * Quaternion.Euler(0f, openAngle, 0f);
//     }

//     // ── IInteractable ─────────────────────────────────────────────
//     public void OnInteract() { 
//         WindowBreak();
//     }

//     // ── Context menu ──────────────────────────────────────────────
//     public List<DoorAction> GetWindowActions()
//     {
//         var actions = new List<DoorAction>();
//         switch (state)
//         {
//             case WindowState.Closed:
//                 actions.Add(new DoorAction { label = "Otwórz okno", enabled = true, execute = WindowOpen  });
//                 actions.Add(new DoorAction { label = "Wybij szybę", enabled = true, duration = 0f, execute = WindowBreak });
//                 break;
//             case WindowState.Open:
//                 actions.Add(new DoorAction { label = "Zamknij okno", enabled = true, execute = WindowClose });
//                 actions.Add(new DoorAction { label = "Wybij szybę",  enabled = true, execute = WindowBreak });
//                 break;
//             case WindowState.Broken:
//                 actions.Add(new DoorAction { label = "Przeleź przez okno", enabled = true, execute = WindowClimb });
//                 break;
//         }
//         return actions;
//     }

//     // ── Actions ───────────────────────────────────────────────────
//     void WindowOpen()
//     {
//         if (_isAnimating) return;
//         state = WindowState.Open;
//         StartCoroutine(RotateTo(_openRot));
//     }

//     void WindowClose()
//     {
//         if (_isAnimating) return;
//         state = WindowState.Closed;
//         StartCoroutine(RotateTo(_closedRot));
//     }

//     /// <summary>
//     /// Breaks the glass. Safe to call from player interaction or from enemy damage.
//     /// </summary>
//     public void WindowBreak()
//     {
//         if (state == WindowState.Broken) return;
//         state = WindowState.Broken;

//         // 1. Visuals ───────────────────────────────────────────────
//         if (breakFX != null)
//             Instantiate(breakFX, transform.position, Quaternion.identity);

//         if (glassMesh != null)
//         {
//             // Disable, don't destroy — Window root must stay alive for NavMeshLink.
//             glassMesh.SetActive(false);
//         }
//         else
//         {
//             // Fallback: blind-disable all renderers/colliders that aren't the link GO.
//             foreach (Transform child in transform)
//             {
//                 if (child.gameObject == _linkGO) continue;
//                 var rend = child.GetComponent<Renderer>();
//                 var col  = child.GetComponent<Collider>();
//                 if (rend != null) rend.enabled = false;
//                 if (col  != null) col.enabled  = false;
//             }
//         }

//         // 2. NavMesh: open the crossing ────────────────────────────
//         _obstacle.enabled = false;  // stop carving → navmesh restores under opening
//         _link.activated   = true;   // agents can now request traversal here

//         // 3. Enemies repath ────────────────────────────────────────
//         NotifyNearbyEnemies();
//     }

//     void WindowClimb()
//     {
//         // Hook your player vault animation here.
//         Debug.Log("Gracz przełazi przez okno");
//     }

//     // ── Enemy path invalidation ───────────────────────────────────
//     void NotifyNearbyEnemies()
//     {
//         if (EnemyManager.Instance == null) return;
//         foreach (EnemyEntity enemy in EnemyManager.Instance.AllEnemies)
//         {
//             if (enemy == null) continue;
//             if (Vector3.Distance(transform.position, enemy.transform.position) > enemyAlertRadius) continue;
//             enemy.InvalidateDestinationCache();
//         }
//     }

//     // ── Used by EnemyEntity to identify window links ──────────────
//     // EnemyEntity calls: linkGO.GetComponentInParent<Window>()
//     // If non-null → this is a window link → run vault traversal.
//     public GameObject LinkGameObject => _linkGO;

//     // ── Rotation coroutine ────────────────────────────────────────
//     IEnumerator RotateTo(Quaternion target)
//     {
//         _isAnimating = true;
//         Quaternion start   = transform.localRotation;
//         float      elapsed = 0f;
//         while (elapsed < openDuration)
//         {
//             elapsed += Time.deltaTime;
//             transform.localRotation = Quaternion.Lerp(start, target,
//                 Mathf.SmoothStep(0f, 1f, elapsed / openDuration));
//             yield return null;
//         }
//         transform.localRotation = target;
//         _isAnimating = false;
//     }
// }