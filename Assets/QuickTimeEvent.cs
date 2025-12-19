using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
public class QuickTimeEvent : MonoBehaviour, IInteractable {
    [Header("Interaction Settings")]
    public bool useSnapping = true;
    public List<Transform> snapPoints = new List<Transform>();
    
    public bool UseSnapping => useSnapping;
    public List<Transform> InteractionPositions => snapPoints;

    public bool IsActive => eventActive;


    public void OnInteract()
    {
        if (eventActive || (interactOnlyOnce && hasFinishedSuccessfully)) return;

        player.enableMovement = false;
        player.enableClimbing = false;

        eventActive = true;
        if (resetOnNextUse)
            ResetEvent();
    }
    
    private bool hasFinishedSuccessfully;

    [Header("DEBUG MODE !!!")]
    public bool debugMode;

    [Header("References")]
    public KCC player;

    public enum EventType {
        None,
        TapEvent,
        PerfectTimingEvent
    }
    public EventType eventType;

    [Header("Event IOs")]
    public bool eventActive;
    public bool eventSucces;
    public bool eventFail;

    [Header("Settings")]
    public bool interactOnlyOnce;
    public bool resetOnNextUse;

    [Header("Tap Event Settings")]
    public int completeProcent = 100;
    public int currentProcent;
    public int decreaseProcent = 1;

    [Header("Perfect Timing Settings")]
    public int timingWindow = 20; 
    public float fillSpeed = 50f;

    private float inactivityTimer;
    private float internalProcent;

    void FixedUpdate() {
        if (!eventActive) return;

        CheckForCancel();

        if (eventActive) {
            switch (eventType) {
                case EventType.TapEvent:
                    TapEvent();
                    break;
                case EventType.PerfectTimingEvent:
                    PerfectTimingEvent();
                    break;
            }
        }

        if (!eventActive){
            player.enableMovement = true;
            player.enableClimbing = true;
        }
    }

    void CheckForCancel(){
        Vector2 moveInput = player.input.PlayerInputMap.MoveInput.ReadValue<Vector2>();
        bool jumpTriggered = player.input.PlayerInputMap.JumpInput.triggered;

        if (moveInput.magnitude > 0.1f || jumpTriggered){
            eventActive = false;
            eventFail = true;
        }
    }

    void TapEvent() {
        float inputValue = player.input.PlayerInputMap.InteractInput.ReadValue<float>();

        if (inputValue > 0) {
            currentProcent += Mathf.RoundToInt(inputValue);
            inactivityTimer = 0;
        }
        else {
            inactivityTimer += Time.fixedDeltaTime;
            if (inactivityTimer >= 1.0f) {
                currentProcent -= decreaseProcent;
            }
        }

        currentProcent = Mathf.Clamp(currentProcent, 0, completeProcent);

        if (currentProcent >= completeProcent) {
            currentProcent = completeProcent;
            eventActive = false;
            eventSucces = true;
            hasFinishedSuccessfully = true;
        }
    }

    void PerfectTimingEvent() {
        internalProcent += fillSpeed * Time.fixedDeltaTime;
        currentProcent = Mathf.RoundToInt(internalProcent);

        float inputValue = player.input.PlayerInputMap.InteractInput.ReadValue<float>();

        if (inputValue > 0) {
            if (currentProcent >= (completeProcent - timingWindow) && currentProcent < completeProcent) {
                eventSucces = true;
                hasFinishedSuccessfully = true;
            }
            else {
                eventFail = true;
            }
            eventActive = false;
        }

        if (currentProcent >= completeProcent) {
            currentProcent = completeProcent;
            eventFail = true;
            eventActive = false;
        }
    }

    public void ResetEvent() {
        internalProcent = 0;
        currentProcent = 0;
        inactivityTimer = 0;
        eventActive = true;
        eventSucces = false;
        eventFail = false;
    }
}