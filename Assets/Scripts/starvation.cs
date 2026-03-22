using UnityEngine;
using UnityEngine.UI;

public class HungerSystem : MonoBehaviour
{
    [Header("Hunger Statistics")]
    public float maxHunger = 100f;
    [Range(0f, 100f)]
    public float currentHunger = 100f;
    [Tooltip("Base hunger depletion per second")]
    public float baseDepletionRate = 0.1f;

    [Header("References")]
    public KCC kccScript; 
    public Image hungerVignette;

    [Header("Effort Multipliers")]
    public float idleMultiplier = 1f;   
    public float moveMultiplier = 1.5f;   
    public float fastMultiplier = 2f;   
    public float jumpMultiplier = 2.5f;   

    void Start()
    {
        currentHunger = maxHunger;

        
        if (kccScript == null) kccScript = GetComponent<KCC>();
    }

    void Update()
    {
        float currentMultiplier = GetKCCMultiplier();

        
        if (currentHunger > 0)
        {
            currentHunger -= (baseDepletionRate * currentMultiplier) * Time.deltaTime;
        }

        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        UpdateVisuals();
    }

    float GetKCCMultiplier()
    {
        if (kccScript == null) return idleMultiplier;

        
        switch (kccScript.state)
        {
            case KCC.State.Idle:
                return idleMultiplier;

            case KCC.State.Walk:
            case KCC.State.Crouch:
            case KCC.State.Prone:
                return moveMultiplier;

            case KCC.State.Run:
            case KCC.State.Sprint:
            case KCC.State.Dash:
                return fastMultiplier;

            case KCC.State.Air:
            case KCC.State.Climbing:
            case KCC.State.Hanging:
                return jumpMultiplier;

            default:
                return idleMultiplier;
        }
    }

    void UpdateVisuals()
    {
        if (hungerVignette != null)
        {
            if (currentHunger <= 30f)
            {
                
                float dangerLevel = 1f - (currentHunger / 30f);
                Color c = hungerVignette.color;
                c.a = dangerLevel * 0.6f;
                hungerVignette.color = c;
            }
            else
            {
                Color c = hungerVignette.color;
                c.a = 0f;
                hungerVignette.color = c;
            }
        }
    }
}