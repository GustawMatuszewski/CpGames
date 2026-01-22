using UnityEngine;

public class PlayerAnimationsController : MonoBehaviour
{
    [Header("References")]
    public KCC playerLogic;
    public Animator animator;

    [Header("Current State (Debug)")]
    public KCC.State currentState;

    // To są TYLKO ID parametrów (ich adresy)
    private int stateParamID;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        
        // Pobieramy ID raz, żeby nie szukać po stringu "State" co klatkę
        stateParamID = Animator.StringToHash("State"); 
    }

    void Update()
    {
        if (playerLogic == null || animator == null) return;

        // 1. Pobieramy aktualny stan z logiki KCC
        currentState = playerLogic.state;

        // 2. WYSYŁAMY wartość stanu do Animatora
        // Rzutujemy (int)currentState, co zamieni np. Walk na 3, Run na 4 itd.
        animator.SetInteger(stateParamID, (int)currentState);
        
        
        // DEBUG: Jeśli chcesz widzieć w konsoli czy numer się zmienia:
        Debug.Log("Aktualny numer stanu wysyłany do Animatora: " + (int)currentState);
    }
}