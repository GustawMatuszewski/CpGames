using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class KCC : MonoBehaviour
{
    [Header("Debug MODE!!!")]
    public bool debugMode = false;

    [Header("References")]
    [SerializeField] public PlayerInput input;
    [SerializeField] private CapsuleCollider capsule;
    [SerializeField] private Transform cameraTransform;

    [Header("Movement Settings")]
    public float walkSpeed = 5;
    public float runSpeed = 7f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 3;
    public float proneSpeed = 1;
    public float dashTime = .1f;
    public float dashSpeed = 20f;
    public float dashCooldown = 0.5f;

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

    [Header("Climbing Settings")]
    public float forwardCheckDistance = 1.0f;
    public float downCheckDistance = 2.0f;
    public float hangTimer = .4f;

    private Vector3 velocity;
    private bool jumpRequested = false;

    [Header("Additional Modifiers")]
    public bool useGravity = true;
    public bool enableMovement = true;
    public bool enableClimbing = true;
    public bool allowAirDash = false;

    float lastSprintTapTime = -1f;
    float doubleTapWindow = 0.3f;
    bool sprintHeldLastFrame = false;

    bool isDashing = false;
    float dashTimer = 0f;
    float dashCooldownTimer = 0f;
    Vector3 dashDirection;

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
        Attack,
        Dash
    }

    public State state;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        input = new PlayerInput();
        input.Enable();
        capsule.height = standingHeight;
    }

    void FixedUpdate()
    {
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.fixedDeltaTime;

        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                dashCooldownTimer = dashCooldown;
            }
        }

        StateController();
        capsule.height = capsuleHeight;

        if (useGravity) ApplyGravity();
        RotateWithCamera();
        if (enableMovement) ApplyMovement();

        jumpRequested = false;
    }

    void ApplyMovement()
    {
        Vector3 frameMovement = new Vector3(RequestedMovement().x, velocity.y, RequestedMovement().z) * Time.fixedDeltaTime;
        transform.position = CollideAndSlide(transform.position, frameMovement);
    }

    void ApplyGravity()
    {
        if (!isGrounded()) velocity.y -= gravityStrength * Time.fixedDeltaTime;
        else if (SlopeCheck() <= maxSlopeAngle) velocity.y = 0f;
        else velocity.y -= gravityStrength * Time.fixedDeltaTime;
    }

    float SlopeCheck()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, capsuleHeight, groundMask)) return Vector3.Angle(hit.normal, Vector3.up);
        return 0f;
    }

    void RotateWithCamera()
    {
        Vector3 camEuler = cameraTransform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, camEuler.y, 0f);
    }

    bool CanFit(float targetHeight)
    {
        float currentHalf = capsuleHeight / 2f;
        float targetHalf = targetHeight / 2f;
        Vector3 feetPos = transform.position - Vector3.up * currentHalf;
        Vector3 newCenter = feetPos + Vector3.up * targetHalf;

        Vector3 point1 = newCenter + Vector3.up * (targetHalf - capsuleRadius);
        Vector3 point2 = newCenter - Vector3.up * (targetHalf - capsuleRadius);

        return !Physics.CheckCapsule(point1, point2, capsuleRadius - 0.01f, collisionMask);
    }

    void StateController()
    {
        Vector2 moveInput = input.PlayerInputMap.MoveInput.ReadValue<Vector2>();
        bool run = input.PlayerInputMap.RunInput.ReadValue<float>() > 0;
        bool sprintHeld = input.PlayerInputMap.SprintInput.ReadValue<float>() > 0;
        bool crouch = input.PlayerInputMap.CrouchInput.ReadValue<float>() > 0;
        bool prone = input.PlayerInputMap.ProneInput.ReadValue<float>() > 0;
        bool grabLedge = input.PlayerInputMap.InteractInput.ReadValue<float>() > 0;

        bool sprintPressed = sprintHeld && !sprintHeldLastFrame;
        sprintHeldLastFrame = sprintHeld;

        if (!prone && !crouch)
        {
            if (!CanFit(standingHeight))
            {
                if (CanFit(crouchHeight)) crouch = true;
                else prone = true;
            }
        }
        else if (!prone && crouch)
        {
            if (!CanFit(crouchHeight)) prone = true;
        }

        if (sprintPressed)
        {
            if (Time.time - lastSprintTapTime <= doubleTapWindow && !isDashing && dashCooldownTimer <= 0f)
            {
                if (allowAirDash || isGrounded()) StartDash();
            }
            lastSprintTapTime = Time.time;
        }

        float prevHeight = capsuleHeight;
        Vector3 ledgePosition = Vector3.zero;

        if (isDashing)
        {
            state = State.Dash;
            moveSpeed = dashSpeed;
            capsuleHeight = standingHeight;
            return;
        }

        if (grabLedge && enableClimbing)
        {
            if (DetectLedge(out ledgePosition))
            {
                state = State.Climbing;
                moveSpeed = 0;
                capsuleHeight = standingHeight;
                StartCoroutine(ClimbOntoObject(ledgePosition));
            }
        }

        if (!isGrounded())
        {
            state = State.Air;
            return;
        }

        if (prone)
        {
            state = State.Prone;
            moveSpeed = proneSpeed;
            capsuleHeight = proneHeight;
        }
        else if (crouch)
        {
            state = State.Crouch;
            moveSpeed = crouchSpeed;
            capsuleHeight = crouchHeight;
        }
        else if (sprintHeld && moveInput.y > 0.1f)
        {
            state = State.Sprint;
            moveSpeed = sprintSpeed;
            capsuleHeight = standingHeight;
        }
        else if (run)
        {
            state = State.Run;
            moveSpeed = runSpeed;
            capsuleHeight = standingHeight;
        }
        else if (moveInput != Vector2.zero)
        {
            state = State.Walk;
            moveSpeed = walkSpeed;
            capsuleHeight = standingHeight;
        }
        else
        {
            state = State.Idle;
            moveSpeed = 0f;
            capsuleHeight = standingHeight;
        }

        if (capsuleHeight > prevHeight) transform.position += new Vector3(0, (capsuleHeight - prevHeight) / 2f, 0);
    }

    Vector3 RequestedMovement()
    {
        if (input.PlayerInputMap.JumpInput.ReadValue<float>() != 0 && SlopeCheck() <= maxSlopeAngle) jumpRequested = true;
        if (isGrounded() && jumpRequested)
        {
            if (state == State.Crouch || state == State.Prone)
            {
                if (CanFit(standingHeight)) velocity.y = jumpForce;
            }
            else
            {
                velocity.y = jumpForce;
            }
        }

        if (isDashing) return dashDirection * dashSpeed;

        Vector2 moveInput = input.PlayerInputMap.MoveInput.ReadValue<Vector2>();
        Vector3 inputDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        inputDir = inputDir.normalized;

        return inputDir * moveSpeed;
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashTime;
        state = State.Dash;

        Vector2 moveInput = input.PlayerInputMap.MoveInput.ReadValue<Vector2>();
        if (moveInput == Vector2.zero) dashDirection = transform.forward;
        else dashDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
    }

    Vector3 CollideAndSlide(Vector3 position, Vector3 movement)
    {
        Vector3 remainingMovement = movement;
        float halfHeight = capsuleHeight / 2f - capsuleRadius;

        for (int i = 0; i < maxSlideIterations; i++)
        {
            Vector3 bottom = position + Vector3.down * halfHeight;
            Vector3 top = position + Vector3.up * halfHeight;

            if (Physics.CapsuleCast(bottom, top, capsuleRadius, remainingMovement.normalized,
                out RaycastHit hit, remainingMovement.magnitude + skinWidth, collisionMask))
            {
                float distance = hit.distance - skinWidth;
                if (distance > 0f) position += remainingMovement.normalized * distance;
                remainingMovement = Vector3.ProjectOnPlane(remainingMovement, hit.normal);
            }
            else
            {
                position += remainingMovement;
                break;
            }
        }

        return position;
    }

    bool isGrounded()
    {
        float halfHeight = capsuleHeight / 2f - capsuleRadius;
        Vector3 bottom = transform.position + Vector3.down * halfHeight;
        Vector3 top = transform.position + Vector3.up * halfHeight;
        float checkDistance = 0.05f;

        return Physics.CapsuleCast(bottom, top, capsuleRadius, Vector3.down, out _, checkDistance + skinWidth, groundMask);
    }

    IEnumerator ClimbOntoObject(Vector3 ledgePos)
    {
        enableMovement = false;
        yield return new WaitForSeconds(hangTimer);
        transform.position = ledgePos + new Vector3(0, capsuleHeight / 2f + 0.06f, 0);
        enableMovement = true;
    }

    bool DetectLedge(out Vector3 ledgePos)
    {
        ledgePos = Vector3.zero;
        LayerMask ledgeMask = collisionMask;

        Vector3 origin = transform.position + Vector3.up * 1.0f;

        if (Physics.Raycast(origin, transform.forward, out RaycastHit forwardHit, forwardCheckDistance, ledgeMask))
        {
            if (!forwardHit.collider.CompareTag(ledgeTag)) return false;

            Vector3 downOrigin = forwardHit.point + Vector3.up * 1.5f;

            if (Physics.Raycast(downOrigin, Vector3.down, out RaycastHit downHit, downCheckDistance, ledgeMask))
            {
                Vector3 ledgeForward = -transform.forward * 0.1f;
                Vector3 capsuleBottom = downHit.point + Vector3.up * capsuleRadius + ledgeForward;
                Vector3 capsuleTop = capsuleBottom + Vector3.up * (capsuleHeight - 2 * capsuleRadius);

                if (!Physics.CheckCapsule(capsuleTop, capsuleBottom, capsuleRadius, ledgeMask))
                {
                    ledgePos = downHit.point;
                    return true;
                }
            }
        }

        return false;
    }


    // theoritically this should fix the leak issue unity sometimes warns about but the issue appears randomly when reloading domain so can't really test it
    private void OnDisable()
    {
        if (input != null)
        {
            input.PlayerInputMap.Disable();
            input.Disable();
        }
    }

    private void OnDestroy()
    {
        if (input != null)
        {
            input.Dispose();
        }
    }
}