using UnityEngine;
using UnityEngine.InputSystem;

public class KCC : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInput input;
    [SerializeField] private CapsuleCollider capsule;
    [SerializeField] private Transform cameraTransform;

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

    private Vector3 velocity;
    private bool jumpRequested = false;

    [Header("Additional Modifiers")]
    public bool useGravity = true;
    public bool enableMovement = true;

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
        Hanging
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
        StateController();
        capsule.height = capsuleHeight;

        if (useGravity)
            ApplyGravity();

        RotateWithCamera();

        if(enableMovement)
            ApplyMovement();

        jumpRequested = false;
    }

    void ApplyMovement()
    {
        Vector3 frameMovement = new Vector3(RequestedMovement().x, velocity.y, RequestedMovement().z) * Time.fixedDeltaTime;
        transform.position = CollideAndSlide(transform.position, frameMovement);
    }

    void ApplyGravity()
    {
        if (!isGrounded())
            velocity.y -= gravityStrength * Time.fixedDeltaTime;
        else if (SlopeCheck() <= maxSlopeAngle)
            velocity.y = 0f;
        else
            velocity.y -= gravityStrength * Time.fixedDeltaTime;
    }

    float SlopeCheck()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, capsuleHeight, groundMask))
            return Vector3.Angle(hit.normal, Vector3.up);

        return 0f;
    }

    void RotateWithCamera()
    {
        Vector3 camEuler = cameraTransform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, camEuler.y, 0f);
    }

    void StateController()
    {
        Vector2 moveInput = input.PlayerInputMap.MoveInput.ReadValue<Vector2>();
        bool run = input.PlayerInputMap.RunInput.ReadValue<float>() > 0;
        bool sprint = input.PlayerInputMap.SprintInput.ReadValue<float>() > 0;
        bool crouch = input.PlayerInputMap.CrouchInput.ReadValue<float>() > 0;
        bool prone = input.PlayerInputMap.ProneInput.ReadValue<float>() > 0;

        float prevHeight = capsuleHeight;

        state = State.None;
        if (velocity.y != 0){
            state = State.Air;
            return;
        }

        if (prone){
            state = State.Prone;
            moveSpeed = proneSpeed;
            capsuleHeight = proneHeight;
        }
        else if (crouch){
            state = State.Crouch;
            moveSpeed = crouchSpeed;
            capsuleHeight = crouchHeight;
        }
        else if (sprint && moveInput.y > 0.1f){
            state = State.Sprint;
            moveSpeed = sprintSpeed;
            capsuleHeight = standingHeight;
        }
        else if (run){
            state = State.Run;
            moveSpeed = runSpeed;
            capsuleHeight = standingHeight;
        }
        else if (moveInput != Vector2.zero){
            state = State.Walk;
            moveSpeed = walkSpeed;
            capsuleHeight = standingHeight;
        }
        else{
            state = State.Idle;
            moveSpeed = 0f;
            capsuleHeight = standingHeight;
        }

        if (capsuleHeight > prevHeight)
            transform.position += new Vector3(0, (capsuleHeight - prevHeight)/2f, 0);
    }



    Vector3 RequestedMovement()
    {
        if (input.PlayerInputMap.JumpInput.ReadValue<float>() != 0 && SlopeCheck() <= maxSlopeAngle)
            jumpRequested = true;

        if (isGrounded() && jumpRequested)
            velocity.y = jumpForce;

        Vector2 moveInput = input.PlayerInputMap.MoveInput.ReadValue<Vector2>();
        Vector3 inputDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        inputDir = inputDir.normalized;

        return inputDir * moveSpeed;
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
                if (distance > 0f)
                    position += remainingMovement.normalized * distance;

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

    void OnDrawGizmos()
    {/*
        float halfHeight = capsuleHeight / 2f - capsuleRadius;
        Vector3 bottom = transform.position + Vector3.down * halfHeight;
        Vector3 top = transform.position + Vector3.up * halfHeight;

        Gizmos.color = isGrounded() ? Color.green : Color.red;
        Gizmos.DrawWireSphere(bottom, capsuleRadius);
        Gizmos.DrawWireSphere(top, capsuleRadius);
        */
    }
}
