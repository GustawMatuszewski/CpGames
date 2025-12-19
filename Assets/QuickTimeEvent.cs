using UnityEngine;
using UnityEngine.InputSystem;

public class QuickTimeEvent : MonoBehaviour, IInteractable {
    public void OnInteract(){
        if (eventActive || (interactOnlyOnce && hasFinishedSuccessfully)) return;

        if (playerInteractionPosition != null){
            player.enableMovement = false;
            player.enableClimbing = false;
            player.transform.position = playerInteractionPosition.position;
        }  
        
        ResetEvent();
    }
    
    private bool hasFinishedSuccessfully;

    [Header("DEBUG MODE !!!")]
    public bool debugMode;

    [Header("References")]
    public KCC player;
    public Transform playerInteractionPosition;

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
            if (debugMode) Debug.Log("Event Canceled");
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
            if (debugMode) Debug.Log("Event Succes");
        }

        if (debugMode) Debug.Log("Tap Progress " + currentProcent);
    }

    void PerfectTimingEvent() {
        internalProcent += fillSpeed * Time.fixedDeltaTime;
        currentProcent = Mathf.RoundToInt(internalProcent);

        float inputValue = player.input.PlayerInputMap.InteractInput.ReadValue<float>();

        if (inputValue > 0) {
            if (currentProcent >= (completeProcent - timingWindow) && currentProcent < completeProcent) {
                eventSucces = true;
                hasFinishedSuccessfully = true;
                if (debugMode) Debug.Log("Succesful Timing");
            }
            else {
                eventFail = true;
                if (debugMode) Debug.Log("Shit timing failed");
            }
            eventActive = false;
        }

        if (currentProcent >= completeProcent) {
            currentProcent = completeProcent;
            eventFail = true;
            eventActive = false;
            if (debugMode) Debug.Log("Didnt even try to time it u idiot");
        }

        if (debugMode) Debug.Log("Timing Progress " + currentProcent);
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