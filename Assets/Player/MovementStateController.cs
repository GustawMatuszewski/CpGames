using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.ProBuilder.MeshOperations;

public class MovementStateController : MonoBehaviour
{
    private Animator animator;
    private CapsuleCollider capsuleCollider;

    [Header("Physics Check")]
    public LayerMask groundMask; // " (Ground) "
    public float groundCheckDistance = 0.2f;
    private bool isGrounded;
    private bool wasGrounded;
    
    [Header("Settings")]
    public float WalkSpeed = 0f;
    public float RunSpeed = 0f;
    public float SprintSpeed = 0f;
    public float CrouchSpeed = 0f;
    public float ProneSpeed = 0f;

    [Header("Current Status")]
    public bool isStanding = false;
    public bool isCrouching = false;
    public bool isJumping = false;
    public bool isProne = false;
    public float standingHeight = 0f;
    public float crouchHeight = 0f;
    public float proneHeight = 0f;
    public mState currentBaseState = mState.None;
    
    private float height;
    private Vector3 lastPosition;
    private Vector3 velocity;   

    public enum mState // States for animations!!
    {
        None, Jumping, Falling,
        Idle,
        Walk_Forward, Walk_Backward, Walk_Right, Walk_Left,
        Walk_Forward_Right, Walk_Forward_Left, Walk_Backward_Right, Walk_Backward_Left, 
        Run_Forward, Run_Backward, Run_Right, Run_Left,
        Run_Forward_Right, Run_Forward_Left, Run_Backward_Right, Run_Backward_Left,
        Sprint_Forward, Sprint_Backward, Sprint_Right, Sprint_Left,
        Sprint_Forward_Right, Sprint_Forward_Left, Sprint_Backward_Right, Sprint_Backward_Left,
        Crouch, Crouch_Forward, Crouch_Backward, Crouch_Right, Crouch_Left,
        Crouch_Forward_Right, Crouch_Forward_Left, Crouch_Backward_Right, Crouch_Backward_Left
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider>(); 
        lastPosition = transform.position; 
    }

    void FixedUpdate()
    { 
        velocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;

        height = capsuleCollider.bounds.size.y;
        
        CheckPhysics();
        UpdateAnimationState();
    }

    void CheckPhysics()
    {
        Vector3 spherePosition = new Vector3(capsuleCollider.bounds.center.x, capsuleCollider.bounds.min.y + 0.05f, capsuleCollider.bounds.center.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckDistance, groundMask);

        isStanding = false;
        isCrouching = false;
        isProne = false;
        isJumping = false;

        float margin = 0.1f;
        if (height >= standingHeight - margin) isStanding = true;
        else if (height <= proneHeight + margin) isProne = true;
        else isCrouching = true;
    }

    void UpdateAnimationState()
{
    Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
    float speed = horizontalVelocity.magnitude;
    Vector2 velocity2D = new Vector2(velocity.x, velocity.z);
    string direction = GetDirectionName(velocity2D, transform);

    if (wasGrounded && !isGrounded)
    {
        if (velocity.y >= 0) currentBaseState = mState.Jumping;
        else currentBaseState = mState.Falling;
    }
    else if (!isGrounded)
    {
        if (velocity.y > 0.05f) currentBaseState = mState.Jumping;
        else if (velocity.y < -0.05f) currentBaseState = mState.Falling;
    }
    else
    {
        if (speed < 0.1f)
        {
            if (isCrouching) currentBaseState = mState.Crouch;
            else currentBaseState = mState.Idle;
        }
        else if (isCrouching) SetStateByDirection("Crouch", direction);
        else if (speed <= WalkSpeed + 0.01f) SetStateByDirection("Walk", direction);
        else if (speed <= RunSpeed + 0.01f) SetStateByDirection("Run", direction);
        else SetStateByDirection("Sprint", direction);
    }
    wasGrounded = isGrounded;
}

    void SetStateByDirection(string type, string dir)
    {
        if (type == "Walk") {
            switch (dir)
            {
                case "Forward": currentBaseState = mState.Walk_Forward; break;
                case "Backward": currentBaseState = mState.Walk_Backward; break;
                case "Left": currentBaseState = mState.Walk_Left; break;
                case "Right": currentBaseState = mState.Walk_Right; break;
                case "Forward_Right": currentBaseState = mState.Walk_Forward_Right; break;
                case "Forward_Left": currentBaseState = mState.Walk_Forward_Left; break;
                case "Backward_Right": currentBaseState = mState.Walk_Backward_Right; break;
                case "Backward_Left": currentBaseState = mState.Walk_Backward_Left; break;
            }
        }
        else if (type == "Run") {
            switch (dir)
            {
                case "Forward": currentBaseState = mState.Run_Forward; break;
                case "Backward": currentBaseState = mState.Run_Backward; break;
                case "Left": currentBaseState = mState.Run_Left; break;
                case "Right": currentBaseState = mState.Run_Right; break;
                case "Forward_Right": currentBaseState = mState.Run_Forward_Right; break;
                case "Forward_Left": currentBaseState = mState.Run_Forward_Left; break;
                case "Backward_Right": currentBaseState = mState.Run_Backward_Right; break;
                case "Backward_Left": currentBaseState = mState.Run_Backward_Left; break;
            }
        }
        else if (type == "Sprint") {
            switch (dir)
            {
                case "Forward": currentBaseState = mState.Sprint_Forward; break;
                case "Backward": currentBaseState = mState.Sprint_Backward; break;
                case "Left": currentBaseState = mState.Sprint_Left; break;
                case "Right": currentBaseState = mState.Sprint_Right; break;
                case "Forward_Right": currentBaseState = mState.Sprint_Forward_Right; break;
                case "Forward_Left": currentBaseState = mState.Sprint_Forward_Left; break;
                case "Backward_Right": currentBaseState = mState.Sprint_Backward_Right; break;
                case "Backward_Left": currentBaseState = mState.Sprint_Backward_Left; break;
            }
        }
        else if (type == "Crouch") {
            switch (dir)
            {
                case "Forward": currentBaseState = mState.Crouch_Forward; break;
                case "Backward": currentBaseState = mState.Crouch_Backward; break;
                case "Left": currentBaseState = mState.Crouch_Left; break;
                case "Right": currentBaseState = mState.Crouch_Right; break;
                case "Forward_Right": currentBaseState = mState.Crouch_Forward_Right; break;
                case "Forward_Left": currentBaseState = mState.Crouch_Forward_Left; break;
                case "Backward_Right": currentBaseState = mState.Crouch_Backward_Right; break;
                case "Backward_Left": currentBaseState = mState.Crouch_Backward_Left; break;
            }
        }
    }

    string GetDirectionName(Vector2 velocity, Transform modelTransform) // Direction
    {
        if (velocity.magnitude < 0.01f) return "Forward"; 

        Vector3 worldVelocity = new Vector3(velocity.x, 0, velocity.y);
        Vector3 localVelocity = modelTransform.InverseTransformDirection(worldVelocity);

        float angle = Mathf.Atan2(localVelocity.x, localVelocity.z) * Mathf.Rad2Deg;
        
        if (angle > -22.5f && angle <= 22.5f)    return "Forward";
        if (angle > 22.5f && angle <= 67.5f)     return "Forward_Right";
        if (angle > 67.5f && angle <= 112.5f)    return "Right";
        if (angle > 112.5f && angle <= 157.5f)   return "Backward_Right";
        if (angle > 157.5f || angle <= -157.5f)  return "Backward"; 
        if (angle > -157.5f && angle <= -112.5f) return "Backward_Left";
        if (angle > -112.5f && angle <= -67.5f)  return "Left";
        if (angle > -67.5f && angle <= -22.5f)   return "Forward_Left";
        
        return "Forward";
    }
}