using UnityEngine;

public class DayCycleController : MonoBehaviour
{
    [Header("Time Settings")]
    public float fullCycleTime = 120f;
    public float currentTime;
    public bool cycleEnabled = true;

    [Header("Sun Settings")]
    public Light sun;
    public float dayIntensity = 1.2f;
    public float nightIntensity = 0.05f;
    public Color daySunColor = new Color(1f, 0.95f, 0.8f);
    public Color nightSunColor = new Color(0.15f, 0.2f, 0.4f);

    [Header("Transition Control")]
    [Range(-1f, 1f)] public float sunsetThreshold = -0.1f; 
    [Range(0.01f, 1f)] public float transitionSmoothness = 0.2f;

    [Header("Skybox & Movement")]
    public Material skyboxMaterial;
    public float skyboxRotationSpeed = 0.5f;
    [Range(0f, 1f)] public float nightSkyboxExposure = 0.05f;
    float currentSkyRotation = 0f;

    [Header("Fog & Environment")]
    public bool useFog = true;
    public FogMode fogMode = FogMode.Linear;
    public Color dayFogColor = new Color(0.5f, 0.6f, 0.7f);
    public Color nightFogColor = new Color(0.01f, 0.01f, 0.02f);
    
    [Header("Fog Distance (Linear Mode Only)")]
    public float dayFogStart = 10f;
    public float dayFogEnd = 100f;
    public float nightFogStart = 0f;
    public float nightFogEnd = 30f;

    [Header("Fog Density (Exp/Exp2 Mode Only)")]
    public float dayFogDensity = 0.01f;
    public float nightFogDensity = 0.08f; 

    [Header("Ambient Lighting")]
    public Color dayAmbientColor = new Color(0.5f, 0.5f, 0.5f);
    public Color nightAmbientColor = Color.black;

    void Start()
    {
        RenderSettings.fog = useFog;
        RenderSettings.fogMode = fogMode;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        
        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;
    }

    void Update()
    {
        if (!cycleEnabled) return;

        currentTime += Time.deltaTime;
        if (currentTime >= fullCycleTime) currentTime = 0;

        float timePercent = currentTime / fullCycleTime;
        float sunAngle = timePercent * 360f;

        sun.transform.rotation = Quaternion.Euler(sunAngle - 90f, 45f, 0f);

        float sunRawHeight = Vector3.Dot(sun.transform.forward, Vector3.down);
        float sunHeightFactor = Mathf.Clamp01((sunRawHeight - sunsetThreshold) / transitionSmoothness);
        
        ApplyEnvironment(sunHeightFactor);

        if (skyboxMaterial != null)
        {
            currentSkyRotation += skyboxRotationSpeed * Time.deltaTime;
            skyboxMaterial.SetFloat("_Rotation", currentSkyRotation % 360f);
        }
    }

    void ApplyEnvironment(float height)
    {
        // Light
        sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, height);
        sun.color = Color.Lerp(nightSunColor, daySunColor, height);

        // Fog Color
        RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, height);
        
        // Fog Density/Distance based on Mode
        if (RenderSettings.fogMode == FogMode.Linear)
        {
            RenderSettings.fogStartDistance = Mathf.Lerp(nightFogStart, dayFogStart, height);
            RenderSettings.fogEndDistance = Mathf.Lerp(nightFogEnd, dayFogEnd, height);
        }
        else
        {
            RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, height);
        }

        // Ambient
        RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, height);

        // Skybox
        if (skyboxMaterial != null)
        {
            if (skyboxMaterial.HasProperty("_Exposure"))
                skyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(nightSkyboxExposure, 1.0f, height));
            
            if (skyboxMaterial.HasProperty("_Tint"))
                skyboxMaterial.SetColor("_Tint", Color.Lerp(nightFogColor * 2, Color.gray, height));
        }
    }
}