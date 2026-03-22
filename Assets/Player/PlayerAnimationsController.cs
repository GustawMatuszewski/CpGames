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
    private Coroutine combatCoroutine;
    private Combat combat;

    // BUG FIX: Śledzimy czy byliśmy w trakcie ataku w poprzedniej klatce.
    // Gdy atak się kończy (wasAttacking=true, attacking=false), wymuszamy
    // ponowne odtworzenie animacji ruchu nawet jeśli stan się nie zmienił.
    private bool wasAttacking = false;

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

        combat = GetComponent<Combat>();

        targetRotation = modelTransform.localRotation;
    }

    void Update()
    {
        if (MovementStateController == null || animator == null) return;

        bool attacking = (combat != null && combat.attackInProgress);

        MovementStateController.mState currentState = MovementStateController.currentBaseState;

        if (!attacking)
        {
            // BUG FIX 1: Gdy atak właśnie się skończył (wasAttacking == true),
            // wymuś ponowne odtworzenie animacji ruchu nawet gdy stan nie zmienił się.
            // Bez tego: stan był np. Idle przed atakiem, po ataku nadal Idle,
            // lastState == Idle, więc warunek currentState != lastState był false
            // i animacja ruchu nigdy nie wracała po uderzeniu.
            bool attackJustEnded = wasAttacking && !attacking;

            // BUG FIX 2: Sprawdź flagę stateRefreshRequired z MovementStateController.
            // Teren może powodować że stan wraca do tego samego (np. Run→blokada cooldown→Run),
            // ale animacja wymaga odświeżenia. Flaga stateRefreshRequired sygnalizuje to.
            bool forceRefresh = MovementStateController.stateRefreshRequired;

            if (currentState != lastState || attackJustEnded || forceRefresh)
            {
                PlayAnimationForState(currentState);
                SetTargetRotationForState(currentState);
                lastState = currentState;
                // Skonsumuj flagę
                MovementStateController.stateRefreshRequired = false;
            }
        }
        else
        {
            // BUG FIX: Nie resetujemy lastState do None — to powodowało buga gdzie
            // po ataku stan był taki sam jak przed (np. Idle→Idle), warunek
            // currentState != lastState był false i animacja idle nigdy nie wracała.
            // Zamiast tego używamy flagi wasAttacking żeby wykryć koniec ataku.
        }

        wasAttacking = attacking;

        if (modelTransform != null)
        {
            modelTransform.localRotation = Quaternion.Slerp(modelTransform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    public void PlayCombatAnimation(string animName)
    {
        if (string.IsNullOrEmpty(animName))
        {
            Debug.LogError("!!! PRÓBA ODPALENIA PUSTEJ ANIMACJI W COMBAT !!!");
            return;
        }

        Debug.Log("<color=red>GRAJ ATAK: </color>" + animName);

        animator.Play(animName);
    }

    void PlayAnimationForState(MovementStateController.mState state)
    {
        if (Simple_Animations_Changing_Directions)
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
        else
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

    void SetTargetRotationForState(MovementStateController.mState state)
    {
        float fRight = Forward_Side_ANGLE;
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