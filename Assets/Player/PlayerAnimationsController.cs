using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class PlayerAnimationsController : MonoBehaviour
{
    [Header("References")]
    public MovementStateController MovementStateController;
    public Animator animator;
    [Header("Model Mesh")]
    public Transform modelTransform;

    [Header("Animation Type")]
    public bool Simple_Animations_Changing_Directions = true;

    [Header("Direction Angle")]
    public float Forward_Side_ANGLE = 35.0f;
    public float Side_ANGLE = 55.0f;
    public float Backward_Side_ANGLE = 35.0f;

    [Header("Animation Settings")]
    public float transitionDuration = 0.1f;
    public float rotationSpeed = 15f;
    private MovementStateController.mState lastState = MovementStateController.mState.None;
    private Quaternion targetRotation;

    [Header("Unique Animations")]
    public string anim_Jump = "";

    [Header("Idle Animations")]
    public string anim_Idle = "";
    public string anim_Crouch = "";

    [Header("Crouch Animations (walking velocity)")]
    public string anim_Crouch_Forward = "";
    public string anim_Crouch_Forward_Right = "";
    public string anim_Crouch_Forward_Left = "";
    public string anim_Crouch_Right = "";
    public string anim_Crouch_Left = "";
    public string anim_Crouch_Backward = "";
    public string anim_Crouch_Backward_Right = "";
    public string anim_Crouch_Backward_Left = "";

    [Header("Walk Animations")]
    public string anim_Walk_Forward = "";
    public string anim_Walk_Forward_Right = "";
    public string anim_Walk_Forward_Left = "";
    public string anim_Walk_Right = "";
    public string anim_Walk_Left = "";
    public string anim_Walk_Backward = "";
    public string anim_Walk_Backward_Right = "";
    public string anim_Walk_Backward_Left = "";

    [Header("Run Animations")]
    public string anim_Run_Forward = "";
    public string anim_Run_Forward_Right = "";
    public string anim_Run_Forward_Left = "";
    public string anim_Run_Right = "";
    public string anim_Run_Left = "";
    public string anim_Run_Backward = "";
    public string anim_Run_Backward_Right = "";
    public string anim_Run_Backward_Left = "";

    [Header("Sprint Animations")]
     public string anim_Sprint_Forward = "";
    public string anim_Sprint_Forward_Right = "";
    public string anim_Sprint_Forward_Left = "";
    public string anim_Sprint_Right = "";
    public string anim_Sprint_Left = "";

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (MovementStateController == null) MovementStateController = GetComponent<MovementStateController>();
        if (modelTransform == null) modelTransform = transform;
        targetRotation = modelTransform.localRotation;
    }

    void Update()
    {
        if (MovementStateController == null || animator == null) return;

        MovementStateController.mState currentState = MovementStateController.currentBaseState;
        if (currentState != lastState)
        {
            Debug.Log("New Animation State: " + currentState);
            PlayAnimationForState(currentState);
            SetTargetRotationForState(currentState);
            lastState = currentState;
        }

        if (modelTransform != null)
        {
            modelTransform.localRotation = Quaternion.Slerp(modelTransform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void PlayAnimationForState(MovementStateController.mState state) // It plays animations!!
    {
        if(Simple_Animations_Changing_Directions) // Simple animations
        {
            switch (state)
            {
                case MovementStateController.mState.Idle:
                    SafeCrossFade(anim_Idle); break;

                case MovementStateController.mState.Crouch:
                    SafeCrossFade(anim_Crouch); break;

                case MovementStateController.mState.Jumping:
                    SafeCrossFade(anim_Jump); break;

                case MovementStateController.mState.Crouch_Forward or MovementStateController.mState.Crouch_Forward_Right
                or MovementStateController.mState.Crouch_Forward_Left or MovementStateController.mState.Crouch_Right or MovementStateController.mState.Crouch_Left:
                    SafeCrossFade(anim_Crouch_Forward); break;

                case MovementStateController.mState.Crouch_Backward or MovementStateController.mState.Crouch_Backward_Right
                or MovementStateController.mState.Crouch_Backward_Left:
                    SafeCrossFade(anim_Crouch_Backward); break;

                case MovementStateController.mState.Walk_Forward or MovementStateController.mState.Walk_Forward_Right
                or MovementStateController.mState.Walk_Forward_Left or MovementStateController.mState.Walk_Right or MovementStateController.mState.Walk_Left:
                    SafeCrossFade(anim_Walk_Forward); break;

                case MovementStateController.mState.Walk_Backward or MovementStateController.mState.Walk_Backward_Right
                or MovementStateController.mState.Walk_Backward_Left:
                    SafeCrossFade(anim_Walk_Backward); break;

                case MovementStateController.mState.Run_Forward or MovementStateController.mState.Run_Forward_Right
                or MovementStateController.mState.Run_Forward_Left or MovementStateController.mState.Run_Right or MovementStateController.mState.Run_Left:
                    SafeCrossFade(anim_Run_Forward); break;

                case MovementStateController.mState.Run_Backward or MovementStateController.mState.Run_Backward_Right
                or MovementStateController.mState.Run_Backward_Left:
                    SafeCrossFade(anim_Run_Backward); break;

                case MovementStateController.mState.Sprint_Forward or MovementStateController.mState.Sprint_Forward_Right
                or MovementStateController.mState.Sprint_Forward_Left or MovementStateController.mState.Sprint_Right or MovementStateController.mState.Sprint_Left:
                    SafeCrossFade(anim_Sprint_Forward); break;
            }
        }
        else if(!Simple_Animations_Changing_Directions) // Advenced animations
        {
            switch (state)
            {
                case MovementStateController.mState.Jumping:
                    SafeCrossFade(anim_Jump); break;

                case MovementStateController.mState.Idle:
                    SafeCrossFade(anim_Idle); break;

                case MovementStateController.mState.Crouch:
                    SafeCrossFade(anim_Crouch); break;

                case MovementStateController.mState.Walk_Forward:
                    SafeCrossFade(anim_Walk_Forward); break;

                case MovementStateController.mState.Run_Forward:
                    SafeCrossFade(anim_Run_Forward); break;
            }
        }
    }

    void SetTargetRotationForState(MovementStateController.mState state) // Rotating player if state is equal to smth
    {
        float fRight = Forward_Side_ANGLE; //f = forward, s = side, b = backward
        float fLeft = Forward_Side_ANGLE * -1; 
        float sRight = Side_ANGLE;
        float sLeft = Side_ANGLE * -1;
        float bRight = Backward_Side_ANGLE * -1;
        float bLeft = Backward_Side_ANGLE;
        float targetAngle = 0f;
        switch (state)
        {
            case MovementStateController.mState.Idle:
            case MovementStateController.mState.Crouch:
            case MovementStateController.mState.Crouch_Forward:
            case MovementStateController.mState.Walk_Forward:
            case MovementStateController.mState.Run_Forward:
            case MovementStateController.mState.Sprint_Forward:
            case MovementStateController.mState.Crouch_Backward:
            case MovementStateController.mState.Walk_Backward:
            case MovementStateController.mState.Run_Backward:
            case MovementStateController.mState.Sprint_Backward:
                targetAngle = 0f; 
                break;
                
            case MovementStateController.mState.Walk_Forward_Right or MovementStateController.mState.Run_Forward_Right
            or MovementStateController.mState.Sprint_Forward_Right or MovementStateController.mState.Crouch_Forward_Right:
                targetAngle = fRight;
                break;

            case MovementStateController.mState.Walk_Right or MovementStateController.mState.Run_Right
            or MovementStateController.mState.Sprint_Right or MovementStateController.mState.Crouch_Right:
                targetAngle = sRight;
                break;

            case MovementStateController.mState.Walk_Forward_Left or MovementStateController.mState.Run_Forward_Left
            or MovementStateController.mState.Sprint_Forward_Left or MovementStateController.mState.Crouch_Forward_Left:
                targetAngle = fLeft;
                break;

            case MovementStateController.mState.Walk_Left or MovementStateController.mState.Run_Left
            or MovementStateController.mState.Sprint_Left or MovementStateController.mState.Crouch_Left:
                targetAngle = sLeft;
                break;

            case MovementStateController.mState.Walk_Backward_Right or MovementStateController.mState.Run_Backward_Right
            or MovementStateController.mState.Sprint_Backward_Right or MovementStateController.mState.Crouch_Backward_Right:
                targetAngle = bRight;
                break;

            case MovementStateController.mState.Walk_Backward_Left or MovementStateController.mState.Run_Backward_Left
            or MovementStateController.mState.Sprint_Backward_Left or MovementStateController.mState.Crouch_Backward_Left:
                targetAngle = bLeft;
                break;
        }

        targetRotation = Quaternion.Euler(0, targetAngle, 0);
    }

    void SafeCrossFade(string animName)
    {
        if (string.IsNullOrEmpty(animName)) return;
        animator.CrossFade(animName, transitionDuration);
    }
}
