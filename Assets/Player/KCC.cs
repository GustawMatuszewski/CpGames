using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


public class KCC : MonoBehaviour
{
    [Header("Debug MODE!!!")]
    public bool debugMode = false;

    [Header("References")]
    [SerializeField] private PlayerInput input;
    [SerializeField] private CapsuleCollider capsule;
    [SerializeField] private Transform cameraTransform;
    //[SerializeField] private PlayerStatusController stamina;

    [Header("Movement Settings")]
    public float walkSpeed = 5;
    public float runSpeed = 7f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 3;
    public float proneSpeed = 1;
    float moveSpeed;

    public float jumpForce = 8f;
    public float gravityStrength = 20f;
    public float skinWidth = 0.05f;
    public int maxSlideIterations = 5;
    public float maxSlopeAngle = 45;

    [Header("Capsule Settings")]
    public float standingHeight;
    public float proneHeight;
    public float crouchHeight;
    float capsuleHeight = 1.8f;

    public float capsuleRadius = .5f;
    public LayerMask collisionMask;
    public LayerMask groundMask;
    public string ledgeTag;

    [Header ("Climbing Settings")]
    public float forwardCheckDistance = 1.0f;
    public float downCheckDistance = 2.0f;
    public float hangTimer = .4f;

    private Vector3 velocity;
    private bool jumpRequested = false;

    [Header("Additional Modifiers")]
    public bool useGravity = true;
    public bool enableMovement = true;
    public bool enableClimbing = true;

    public enum State
    {
        None,
        Idle,
        Air,
        Walk,
        Run,
        Sprint,
        Crouch,
        Prone,
        Climbing,
        Hanging,
        Attack
    }

    public State state;

    private void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        input = new PlayerInput();
        input.Enable();
        capsule.height = standingHeight;
    }

    void FixedUpdate() {
        StateController();
        capsule.height = capsuleHeight;

        if (useGravity) ApplyGravity();

        RotateWithCamera();

        if(enableMovement) ApplyMovement();

        jumpRequested = false;
    }

    void ApplyMovement() {
        Vector3 frameMovement = new Vector3(RequestedMovement().x, velocity.y, RequestedMovement().z) * Time.fixedDeltaTime;
        transform.position = CollideAndSlide(transform.position, frameMovement);
    }

    void ApplyGravity() {
        if (!isGrounded()) velocity.y -= gravityStrength * Time.fixedDeltaTime;
        else if (SlopeCheck() <= maxSlopeAngle) velocity.y = 0f;
        else velocity.y -= gravityStrength * Time.fixedDeltaTime;
    }

    float SlopeCheck() {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, capsuleHeight, groundMask)) return Vector3.Angle(hit.normal, Vector3.up);
        return 0f;
    }

    void RotateWithCamera() {
        Vector3 camEuler = cameraTransform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, camEuler.y, 0f);
    }

    void StateController() {
        Vector2 moveInput = input.PlayerInputMap.MoveInput.ReadValue<Vector2>();
        bool run = input.PlayerInputMap.RunInput.ReadValue<float>() > 0;
        bool sprint = input.PlayerInputMap.SprintInput.ReadValue<float>() > 0;
        bool crouch = input.PlayerInputMap.CrouchInput.ReadValue<float>() > 0;
        bool prone = input.PlayerInputMap.ProneInput.ReadValue<float>() > 0;
        bool grabLedge = input.PlayerInputMap.InteractInput.ReadValue<float>() > 0;

        float prevHeight = capsuleHeight;
        Vector3 ledgePosition = new Vector3(0,0,0);

        state = State.None;


        if (grabLedge && enableClimbing){
            if (DetectLedge(out ledgePosition)){
                state = State.Climbing;
                moveSpeed = 0;
                capsuleHeight = standingHeight;
                StartCoroutine(ClimbOntoObject(ledgePosition));

            }
        }

        if (!isGrounded()) {
            state = State.Air;
            return;
        }


        
        if (prone) {
            state = State.Prone;
            moveSpeed = proneSpeed;
            capsuleHeight = proneHeight;
        } else if (crouch) {
            state = State.Crouch;
            moveSpeed = crouchSpeed;
            capsuleHeight = crouchHeight;
        } else if (sprint && moveInput.y > 0.1f) {
            state = State.Sprint;
            moveSpeed = sprintSpeed;
            capsuleHeight = standingHeight;
        } else if (run) {
            state = State.Run;
            moveSpeed = runSpeed;
            capsuleHeight = standingHeight;
        } else if (moveInput != Vector2.zero) {
            state = State.Walk;
            moveSpeed = walkSpeed;
            capsuleHeight = standingHeight;
        } else {
            state = State.Idle;
            moveSpeed = 0f;
            capsuleHeight = standingHeight;
        }

        if (capsuleHeight > prevHeight) transform.position += new Vector3(0, (capsuleHeight - prevHeight)/2f, 0);
    }

    Vector3 RequestedMovement() {
        if (input.PlayerInputMap.JumpInput.ReadValue<float>() != 0 && SlopeCheck() <= maxSlopeAngle) jumpRequested = true;
        if (isGrounded() && jumpRequested) velocity.y = jumpForce;

        Vector2 moveInput = input.PlayerInputMap.MoveInput.ReadValue<Vector2>();
        Vector3 inputDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        inputDir = inputDir.normalized;

        return inputDir * moveSpeed;
    }

    Vector3 CollideAndSlide(Vector3 position, Vector3 movement) {
        Vector3 remainingMovement = movement;
        float halfHeight = capsuleHeight / 2f - capsuleRadius;

        for (int i = 0; i < maxSlideIterations; i++) {
            Vector3 bottom = position + Vector3.down * halfHeight;
            Vector3 top = position + Vector3.up * halfHeight;

            if (Physics.CapsuleCast(bottom, top, capsuleRadius, remainingMovement.normalized,
                out RaycastHit hit, remainingMovement.magnitude + skinWidth, collisionMask)) {

                if (debugMode) {
                    DebugCapsuleSweep(position, remainingMovement.normalized * hit.distance, halfHeight, capsuleRadius);
                    Debug.DrawRay(hit.point, hit.normal, Color.magenta, 0.1f);
                }

                float distance = hit.distance - skinWidth;
                if (distance > 0f) position += remainingMovement.normalized * distance;
                remainingMovement = Vector3.ProjectOnPlane(remainingMovement, hit.normal);
            } else {
                if (debugMode)
                    DebugCapsuleSweep(position, remainingMovement, halfHeight, capsuleRadius);

                position += remainingMovement;
                break;
            }
        }

        return position;
    }


    void DebugCapsuleSweep(Vector3 startPos, Vector3 movement, float halfHeight, float radius, float predictDistance = 5f, int steps = 10){
        Vector3 extendedMovement = movement.normalized * Mathf.Max(movement.magnitude, predictDistance);

        for (int s = 0; s <= steps; s++){
            float t = s / (float)steps;
            Vector3 interpPos = Vector3.Lerp(startPos, startPos + extendedMovement, t);
            Vector3 interpBottom = interpPos + Vector3.down * halfHeight;
            Vector3 interpTop = interpPos + Vector3.up * halfHeight;

            Color col = Color.Lerp(Color.yellow, Color.green, t);
            DebugDrawCapsule(interpBottom, interpTop, radius, col);
        }
    }




    bool isGrounded() {
        float halfHeight = capsuleHeight / 2f - capsuleRadius;
        Vector3 bottom = transform.position + Vector3.down * halfHeight;
        Vector3 top = transform.position + Vector3.up * halfHeight;
        float checkDistance = 0.05f;

        return Physics.CapsuleCast(bottom, top, capsuleRadius, Vector3.down, out _, checkDistance + skinWidth, groundMask);
    }

    IEnumerator ClimbOntoObject(Vector3 ledgePos){
        enableMovement = false;
        yield return new WaitForSeconds(hangTimer);
        transform.position = ledgePos + new Vector3(0, capsuleHeight / 2f + 0.06f, 0);
        enableMovement = true;
    }


    bool Hanging() {
        return false;
    }

 bool DetectLedge(out Vector3 ledgePos){
    ledgePos = Vector3.zero;
    LayerMask ledgeMask = collisionMask;

    Vector3 origin = transform.position + Vector3.up * 1.0f;

    // Forward ray
    if (Physics.Raycast(origin, transform.forward, out RaycastHit forwardHit, forwardCheckDistance, ledgeMask)){
        if(debugMode)
            Debug.DrawLine(origin, forwardHit.point, Color.green);

        if (!forwardHit.collider.CompareTag(ledgeTag))
            return false;

        Vector3 downOrigin = forwardHit.point + Vector3.up * 1.5f;

        if (Physics.Raycast(downOrigin, Vector3.down, out RaycastHit downHit, downCheckDistance, ledgeMask)){
            Vector3 ledgeForward = -transform.forward * 0.1f;
            Vector3 capsuleBottom = downHit.point + Vector3.up * capsuleRadius + ledgeForward;
            Vector3 capsuleTop = capsuleBottom + Vector3.up * (capsuleHeight - 2 * capsuleRadius);

            if (debugMode){
                Debug.DrawLine(downOrigin, downHit.point, Color.green);
                DebugDrawCapsule(capsuleBottom, capsuleTop, capsuleRadius, Color.blue);
            }
            if (!Physics.CheckCapsule(capsuleTop, capsuleBottom, capsuleRadius, ledgeMask)){
                ledgePos = downHit.point;
                return true;
            }
        }
    }

    return false;
}

// Helper function to draw a capsule in the Scene view
void DebugDrawCapsule(Vector3 start, Vector3 end, float radius, Color color)
{
    int segments = 16;
    for (int i = 0; i < segments; i++)
    {
        float angle1 = (i / (float)segments) * Mathf.PI * 2;
        float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2;

        Vector3 offset1 = new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
        Vector3 offset2 = new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

        Debug.DrawLine(start + offset1, start + offset2, color);
        Debug.DrawLine(end + offset1, end + offset2, color);
        Debug.DrawLine(start + offset1, end + offset1, color);
    }
}





    void OnDrawGizmos() {/*
        float halfHeight = capsuleHeight / 2f - capsuleRadius;
        Vector3 bottom = transform.position + Vector3.down * halfHeight;
        Vector3 top = transform.position + Vector3.up * halfHeight;

        Gizmos.color = isGrounded() ? Color.green : Color.red;
        Gizmos.DrawWireSphere(bottom, capsuleRadius);
        Gizmos.DrawWireSphere(top, capsuleRadius);
        */
    }
}
