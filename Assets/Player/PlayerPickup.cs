using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickup : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera used for raycasting and throw direction.")]
    [SerializeField] private Camera cam;

    [Tooltip("Transform representing where the object will be held.")]
    [SerializeField] private Transform holdPoint;

    [Header("Pickup Settings")]
    [Tooltip("Maximum distance for picking up objects.")]
    [SerializeField] private float pickupRange = 3f;

    [Header("Throw Settings")]
    [Tooltip("Base throw force applied to the object.")]
    [SerializeField] private float baseThrowForce = 20f;

    [Tooltip("Minimum throw multiplier regardless of mass.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float minThrowForceMultiplier = 0.2f;

    [Tooltip("Maximum throw multiplier for light objects.")]
    [Range(1f, 5f)]
    [SerializeField] private float maxThrowForceMultiplier = 2f;

    private Rigidbody heldObject;
    private Collider heldCollider;

    private float lastGrabValue = 0f;
    private float lastInteractValue = 0f;

    private PlayerInput actions;

    private void Awake()
    {
        actions = new PlayerInput();
        actions.PlayerInputMap.Enable();
    }

    private void Update()
    {
        float grabValue = actions.PlayerInputMap.GrabInput.ReadValue<float>();
        float interactValue = actions.PlayerInputMap.InteractInput.ReadValue<float>();

        if (grabValue > 0f && lastGrabValue <= 0f)
        {
            if (heldObject == null)
                TryPickup();
            else
                DropItem();
        }

        if (heldObject != null && interactValue > 0f && lastInteractValue <= 0f)
        {
            ThrowItem();
        }

        lastGrabValue = grabValue;
        lastInteractValue = interactValue;
    }

    private void FixedUpdate()
    {
        if (heldObject != null)
        {
            Vector3 toHold = holdPoint.position - heldObject.position;
            heldObject.MovePosition(heldObject.position + toHold);
            heldObject.MoveRotation(holdPoint.rotation);
        }
    }

    private void TryPickup()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            if (hit.collider.CompareTag("CanPickup"))
            {
                Rigidbody rb = hit.collider.attachedRigidbody;
                if (rb != null)
                {
                    heldObject = rb;
                    heldCollider = hit.collider;

                    //ExplosiveObject barrelExplosion = heldObject.GetComponent<ExplosiveObject>();
                    //if (barrelExplosion != null)
                    //    barrelExplosion.isHeld = true;

                    heldObject.isKinematic = true;
                    heldObject.transform.position = holdPoint.position;
                    heldObject.transform.rotation = holdPoint.rotation;
                    heldObject.transform.SetParent(holdPoint, true);
                }
            }
        }
    }

    private void DropItem()
    {
        if (heldObject != null)
        {
            //ExplosiveObject barrelExplosion = heldObject.GetComponent<ExplosiveObject>();
            //if (barrelExplosion != null)
            //    barrelExplosion.isHeld = false;

            heldObject.transform.SetParent(null);
            heldObject.isKinematic = false;

            heldObject = null;
            heldCollider = null;
        }
    }

    private void ThrowItem()
    {
        if (heldObject != null)
        {
            //ExplosiveObject barrelExplosion = heldObject.GetComponent<ExplosiveObject>();
            //if (barrelExplosion != null)
            //    barrelExplosion.isHeld = false;

            heldObject.transform.SetParent(null);
            heldObject.isKinematic = false;

            float mass = Mathf.Max(heldObject.mass, 0.1f); // avoid divide-by-zero
            float massFactor = 1f / mass;

            float throwMultiplier = Mathf.Clamp(massFactor, minThrowForceMultiplier, maxThrowForceMultiplier);
            Vector3 finalForce = cam.transform.forward * baseThrowForce * throwMultiplier;

            heldObject.AddForce(finalForce, ForceMode.VelocityChange);

            heldObject = null;
            heldCollider = null;
        }
    }
}
