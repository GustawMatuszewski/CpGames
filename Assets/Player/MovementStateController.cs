using UnityEngine;
using UnityEngine.AI;

public class MovementStateController : MonoBehaviour
{
    private Animator animator;
    private Combat combat;
    private CapsuleCollider capsuleCollider;
    private NavMeshAgent navAgent;
    private KCC kcc; // opcjonalny — jeśli jest, czytamy jego state

    [Header("Physics Check")]
    public LayerMask groundMask;
    public float groundCheckDistance = 0.2f;
    private bool isGrounded;
    private bool wasGrounded;
    private int airborneFrames = 0;
    private bool isActuallyAirborne => airborneFrames >= airborneThreshold;
    private const int airborneThreshold = 8;

    private int jumpingFrames = 0;
    private const int fallingDelay = 30;

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

    private Vector3[] velocityBuffer = new Vector3[4];
    private int velocityBufferIndex = 0;

    private int stateChangeCooldown = 0;
    private const int minFramesBetweenChanges = 6;

    public enum mState
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
        animator        = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        lastPosition    = transform.position;
        combat          = GetComponent<Combat>();
        navAgent        = GetComponent<NavMeshAgent>();
        kcc             = GetComponent<KCC>();
    }

    void SetState(mState newState)
    {
        if (newState == currentBaseState) return;
        if (stateChangeCooldown > 0) return;

        currentBaseState = newState;
        stateChangeCooldown = minFramesBetweenChanges;
    }

    string GetDesiredTier()
    {
        if (kcc != null)
        {
            switch (kcc.state)
            {
                case KCC.State.Idle:    return "Idle";
                case KCC.State.Walk:    return "Walk";
                case KCC.State.Run:     return "Run";
                case KCC.State.Sprint:
                case KCC.State.Dash:    return "Sprint";
                case KCC.State.Crouch:  return "Crouch";
                case KCC.State.Prone:   return "Walk";
                default:                return "Idle";
            }
        }

        if (navAgent != null)
        {
            float s = navAgent.velocity.magnitude;
            if (s < 0.1f)                   return "Idle";
            if (s <= WalkSpeed + 0.1f)      return "Walk";
            if (s <= RunSpeed  + 0.1f)      return "Run";
            return "Sprint";
        }

        float speed = new Vector3(velocity.x, 0, velocity.z).magnitude;
        if (speed < 0.1f)                   return "Idle";
        if (speed <= WalkSpeed + 0.1f)      return "Walk";
        if (speed <= RunSpeed  + 0.1f)      return "Run";
        return "Sprint";
    }

    bool GetIsGrounded()
    {
        if (kcc != null)
        {
            return kcc.state != KCC.State.Air;
        }

        float radius = capsuleCollider.radius * 0.9f;
        Vector3 origin = new Vector3(
            capsuleCollider.bounds.center.x,
            capsuleCollider.bounds.min.y + radius + 0.05f,
            capsuleCollider.bounds.center.z
        );

        return Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out _,
            groundCheckDistance + 0.05f,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    void FixedUpdate()
    {
        Vector3 rawVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;

        velocityBuffer[velocityBufferIndex] = rawVelocity;
        velocityBufferIndex = (velocityBufferIndex + 1) % velocityBuffer.Length;
        Vector3 avgVelocity = Vector3.zero;
        foreach (var v in velocityBuffer) avgVelocity += v;
        velocity = avgVelocity / velocityBuffer.Length;

        height = capsuleCollider.bounds.size.y;

        if (stateChangeCooldown > 0) stateChangeCooldown--;

        CheckPhysics();
        if (combat != null && (combat.attackInProgress || combat.combatActive)) return;

        UpdateAnimationState();
    }

    void CheckPhysics()
    {
        isGrounded = GetIsGrounded();

        if (!isGrounded) airborneFrames++;
        else             airborneFrames = 0;

        isStanding  = false;
        isCrouching = false;
        isProne     = false;
        isJumping   = false;

        float margin = 0.1f;
        if (height >= standingHeight - margin)   isStanding  = true;
        else if (height <= proneHeight + margin) isProne     = true;
        else                                     isCrouching = true;
    }

    void UpdateAnimationState()
    {
        string direction = GetDirectionName(new Vector2(velocity.x, velocity.z), transform);
        string tier      = GetDesiredTier();

        if (!isGrounded && velocity.y > 2.0f)
        {
            currentBaseState = mState.Jumping;
            jumpingFrames = 0;
        }
        else if (isActuallyAirborne)
        {
            jumpingFrames++;

            if (currentBaseState == mState.Jumping && jumpingFrames >= fallingDelay && velocity.y < -0.5f)
                currentBaseState = mState.Falling;
            else if (currentBaseState != mState.Jumping && jumpingFrames >= fallingDelay && velocity.y < -2.0f)
                currentBaseState = mState.Falling;
        }
        else // grounded
        {
            jumpingFrames = 0;

            if (tier == "Idle")
            {
                currentBaseState = isCrouching ? mState.Crouch : mState.Idle;
            }
            else if (tier == "Crouch" || isCrouching)
            {
                SetStateByDirection("Crouch", direction);
            }
            else
            {
                SetStateByDirection(tier, direction);
            }
        }

        wasGrounded = isGrounded;
    }

    string GetDirectionName(Vector2 vel, Transform modelTransform)
    {
        if (vel.magnitude < 0.01f) return "Forward";

        Vector3 worldVelocity = new Vector3(vel.x, 0, vel.y);
        Vector3 localVelocity = modelTransform.InverseTransformDirection(worldVelocity);

        float angle = Mathf.Atan2(localVelocity.x, localVelocity.z) * Mathf.Rad2Deg;

        if (angle >  -22.5f && angle <=  22.5f)  return "Forward";
        if (angle >   22.5f && angle <=  67.5f)  return "Forward_Right";
        if (angle >   67.5f && angle <= 112.5f)  return "Right";
        if (angle >  112.5f && angle <= 157.5f)  return "Backward_Right";
        if (angle >  157.5f || angle <= -157.5f) return "Backward";
        if (angle > -157.5f && angle <= -112.5f) return "Backward_Left";
        if (angle > -112.5f && angle <=  -67.5f) return "Left";
        if (angle >  -67.5f && angle <=  -22.5f) return "Forward_Left";

        return "Forward";
    }

    void SetStateByDirection(string type, string dir)
    {
        if (type == "Walk")
        {
            switch (dir)
            {
                case "Forward":        SetState(mState.Walk_Forward); break;
                case "Backward":       SetState(mState.Walk_Backward); break;
                case "Left":           SetState(mState.Walk_Left); break;
                case "Right":          SetState(mState.Walk_Right); break;
                case "Forward_Right":  SetState(mState.Walk_Forward_Right); break;
                case "Forward_Left":   SetState(mState.Walk_Forward_Left); break;
                case "Backward_Right": SetState(mState.Walk_Backward_Right); break;
                case "Backward_Left":  SetState(mState.Walk_Backward_Left); break;
            }
        }
        else if (type == "Run")
        {
            switch (dir)
            {
                case "Forward":        SetState(mState.Run_Forward); break;
                case "Backward":       SetState(mState.Run_Backward); break;
                case "Left":           SetState(mState.Run_Left); break;
                case "Right":          SetState(mState.Run_Right); break;
                case "Forward_Right":  SetState(mState.Run_Forward_Right); break;
                case "Forward_Left":   SetState(mState.Run_Forward_Left); break;
                case "Backward_Right": SetState(mState.Run_Backward_Right); break;
                case "Backward_Left":  SetState(mState.Run_Backward_Left); break;
            }
        }
        else if (type == "Sprint")
        {
            switch (dir)
            {
                case "Forward":        SetState(mState.Sprint_Forward); break;
                case "Backward":       SetState(mState.Sprint_Backward); break;
                case "Left":           SetState(mState.Sprint_Left); break;
                case "Right":          SetState(mState.Sprint_Right); break;
                case "Forward_Right":  SetState(mState.Sprint_Forward_Right); break;
                case "Forward_Left":   SetState(mState.Sprint_Forward_Left); break;
                case "Backward_Right": SetState(mState.Sprint_Backward_Right); break;
                case "Backward_Left":  SetState(mState.Sprint_Backward_Left); break;
            }
        }
        else if (type == "Crouch")
        {
            switch (dir)
            {
                case "Forward":        SetState(mState.Crouch_Forward); break;
                case "Backward":       SetState(mState.Crouch_Backward); break;
                case "Left":           SetState(mState.Crouch_Left); break;
                case "Right":          SetState(mState.Crouch_Right); break;
                case "Forward_Right":  SetState(mState.Crouch_Forward_Right); break;
                case "Forward_Left":   SetState(mState.Crouch_Forward_Left); break;
                case "Backward_Right": SetState(mState.Crouch_Backward_Right); break;
                case "Backward_Left":  SetState(mState.Crouch_Backward_Left); break;
            }
        }
    }
}