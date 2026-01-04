using System.Collections;
using UnityEngine;

public class DayCycleController : MonoBehaviour
{
    [Header("Time Settings")]
    public float fullCycleTime = 120f;
    public float dayTime = 60f;
    public float currentTime;
    public bool cycleEnabled = true;

    [Header("Sun Settings")]
    public Light sun;
    public float dayIntensity = 1.2f;
    public float nightIntensity = 0f;
    public Color daySunColor = new Color(1f, 0.95f, 0.8f);
    public Color nightSunColor = new Color(0.15f, 0.2f, 0.4f);

    [Header("Transition Control")]
    [Range(-0.5f, 0.5f)] public float sunsetThreshold = -0.1f; 
    [Range(0.01f, 1f)] public float transitionSmoothness = 0.2f;

    [Header("Skybox & Movement")]
    public Material skyboxMaterial;
    public float skyboxRotationSpeed = 0.5f;
    [Range(0f, 1f)] public float nightSkyboxExposure = 0.05f;
    float currentRotation = 0f;

    [Header("Fog & Environment")]
    public bool useFog = true;
    public float dayFogDensity = 0.01f;
    public float nightFogDensity = 0.08f; 
    public Color dayFogColor = new Color(0.5f, 0.6f, 0.7f);
    public Color nightFogColor = new Color(0.01f, 0.01f, 0.02f);

    [Header("Ambient Lighting")]
    public Color dayAmbientColor = new Color(0.5f, 0.5f, 0.5f);
    public Color nightAmbientColor = Color.black;

    float nightTime;
    bool isDay = true;

    void Start()
    {
        nightTime = fullCycleTime - dayTime;
        
        // Krytyczne ustawienia dla uniknięcia "dziwnej" mgły
        RenderSettings.fog = useFog;
        RenderSettings.fogMode = FogMode.ExponentialSquared; // Najbardziej naturalna
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        
        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;

        StartCoroutine(TimeCycle());
    }

    IEnumerator TimeCycle()
    {
        while (true)
        {
            if (cycleEnabled)
            {
                float maxTime = isDay ? dayTime : nightTime;
                currentTime += Time.deltaTime;
                float t = currentTime / maxTime;

                float startRot = isDay ? -10f : 170f;
                float endRot = isDay ? 170f : 350f;
                float currentRot = Mathf.Lerp(startRot, endRot, t);
                sun.transform.rotation = Quaternion.Euler(currentRot, 45f, 0f);

                float sunRawHeight = Vector3.Dot(sun.transform.forward, Vector3.down);
                float sunHeight = Mathf.Clamp01((sunRawHeight - sunsetThreshold) / transitionSmoothness);
                
                ApplyEnvironment(sunHeight);

                if (skyboxMaterial != null)
                {
                    currentRotation += skyboxRotationSpeed * Time.deltaTime;
                    skyboxMaterial.SetFloat("_Rotation", currentRotation % 360f);
                }

                if (currentTime >= maxTime)
                {
                    currentTime = 0f;
                    isDay = !isDay;
                }
            }
            yield return null;
        }
    }

    void ApplyEnvironment(float height)
    {
        sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, height);
        sun.color = Color.Lerp(nightSunColor, daySunColor, height);

        // Synchronizacja mgły i ambientu
        Color currentFogColor = Color.Lerp(nightFogColor, dayFogColor, height);
        RenderSettings.fogColor = currentFogColor;
        RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, height);

        // Ustawienie Ambient na kolor mgły pomaga scalić horyzont
        RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, height);

        if (skyboxMaterial != null && skyboxMaterial.HasProperty("_Exposure"))
        {
            skyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(nightSkyboxExposure, 1.0f, height));
            
            // Opcjonalnie: ściemnianie Tint Skyboxa do koloru mgły, aby nie było "prześwitów"
            if (skyboxMaterial.HasProperty("_Tint"))
                skyboxMaterial.SetColor("_Tint", Color.Lerp(nightFogColor * 2, Color.gray, height));
        }
    }
}