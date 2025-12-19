using UnityEngine;
using UnityEngine.InputSystem;

public class QuickTimeEvent : MonoBehaviour {
    public bool debugMode;

    public KCC player;

    public enum EventType {
        None,
        TapEvent,
        PerfectTimingEvent
    }
    public EventType eventType;

    public bool eventActive;
    public bool eventSucces;
    public bool eventFail;

    public int completeProcent = 100;
    public int currentProcent;
    public int decreaseProcent = 1;

    [Header("Perfect Timing Settings")]
    public int timingWindow = 20; 
    public float fillSpeed = 50f;

    private float inactivityTimer;
    private float internalProcent;

    void FixedUpdate() {
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
            eventFail = false;
        }
        else {
            eventSucces = false;
            eventFail = false;
        }

        if (debugMode) Debug.Log(currentProcent);
    }

    void PerfectTimingEvent() {
        internalProcent += fillSpeed * Time.fixedDeltaTime;
        currentProcent = Mathf.RoundToInt(internalProcent);

        float inputValue = player.input.PlayerInputMap.InteractInput.ReadValue<float>();

        if (inputValue > 0) {
            if (currentProcent >= (completeProcent - timingWindow) && currentProcent < completeProcent) {
                eventSucces = true;
                eventFail = false;
            }
            else {
                eventFail = true;
                eventSucces = false;
            }
            eventActive = false;
        }

        if (currentProcent >= completeProcent) {
            currentProcent = completeProcent;
            eventFail = true;
            eventSucces = false;
            eventActive = false;
        }

        if (debugMode) Debug.Log("Timing: " + currentProcent);
    }

    public void ResetEvent() {
        internalProcent = 0;
        currentProcent = 0;
        eventActive = true;
        eventSucces = false;
        eventFail = false;
    }
}