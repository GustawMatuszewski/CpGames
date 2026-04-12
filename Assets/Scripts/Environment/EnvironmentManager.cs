using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Unity.VisualScripting;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Weather System")]
    public List<WeatherPreset> availablePresets; // Add this so you can drag in multiple presets
    public WeatherPreset activePreset;
    private WeatherPreset targetPreset;          // Add this to store where we are transitioning to
    
    [Header("Weather Change Settings")]
    public float minTimeBetweenWeatherChange = 120f;
    public float maxTimeBetweenWeatherChange = 300f;
    public float weatherTransitionDuration = 20f; // How long it takes to blend presets
    
    private float nextWeatherChangeTime;
    private float transitionProgress = 0f;
    private bool isTransitioning = false;
 

    
    [Header("Time settings")]
    [Range(0, 24)]
    public float currentTime;
    public float timeSpeed = 1f;

    [Header("Current Time")]
    public string currentTimeString;

    [Header("Sun settings")]
    public Light sunLight;
    public float sunPosition = 1f;
    public float sunIntensity = 1f;
    public AnimationCurve sunIntensityMultiplier;
    public AnimationCurve sunTemperatureCurve;

    public bool isDay =true;
    public bool sunActive = true;
    public bool moonActive = true;

    [Header("Moon settings")]
    public Light moonLight;
    public float moonIntensity = 1f;
    public AnimationCurve moonIntensityMultiplier;
    public AnimationCurve moonTemperatureCurve;

    [Header("Cloud settings")]
    public Volume globalVolume;
    private VolumetricClouds volumetricClouds;

    [Header("Weather parameters (Read-only)")]
    public float currentTemperature;
    [Range(0,100)] public float currentHumidity;
    public float windSpeed;

    [Header("Wind settings")]
    public Vector2 windDirection = new Vector2(1f, 0f); 
    [Tooltip("The temperature perveived with the wind, can be used for things like making the player feel colder when the wind is strong")]
    public float perceivedWindTemperature; // for player status stuff
    public float windChillFactor = 0.5f; // how much the wind affects the perceived temperature
    [Tooltip("Wind zone is used for plants and other things that react to wind")]
    public WindZone windZone; // for plants and other things that react to wind

    [Header("Fog settings")]
    private Fog volumetricFog;
    public Color baseFogColor = Color.white;


    private VisualEnvironment visualEnv;
    // private WeatherPreset nextPreset;
    // Add anywhere inside EnvironmentManager class
    public bool IsTransitioning => isTransitioning;
    public WeatherPreset TargetPreset => targetPreset;
    public float TransitionProgress => transitionProgress;

    
    void Start()
    {

    if (globalVolume.profile.TryGet<VolumetricClouds>(out volumetricClouds))
    {
        volumetricClouds.densityMultiplier.overrideState = true;
        volumetricClouds.bottomAltitude.overrideState = true;
        volumetricClouds.shapeFactor.overrideState = true;
        volumetricClouds.erosionFactor.overrideState = true;
        volumetricClouds.altitudeRange.overrideState = true;
    }

    if (globalVolume.profile.TryGet<Fog>(out volumetricFog))
    {
        volumetricFog.meanFreePath.overrideState = true; // fog density
        volumetricFog.albedo.overrideState = true; // fog color
    }

    if (globalVolume.profile.TryGet<VisualEnvironment>(out visualEnv))
    {
        visualEnv.windSpeed.overrideState = true;
        visualEnv.windOrientation.overrideState = true;
    }

        UpdateTimeText();
        CheckShadowStatus();
    }
    void Update()
    {
        currentTime += Time.deltaTime * timeSpeed;

        if(currentTime >= 24f)
        {
            currentTime = 0f;
        }

        UpdateTimeText();

        CalculateWeatherLogic();
        HandleWeatherTransitionTimer();

        ApplyVisuals();
    }
    private void OnValidate()
    {
        if(sunLight==null || moonLight == null) return;
        UpdateLight();
        CheckShadowStatus();
        if (activePreset != null) ApplyCloudsAndFog();
    }
    void UpdateTimeText()
    {
        currentTimeString = Mathf.Floor(currentTime).ToString("00") + ":" + ((currentTime%1)*60).ToString("00");
    }
    void CalculateWeatherLogic()
    {
        float normalizedTime = currentTime/24f;

        currentTemperature = activePreset.temperatureCurve.Evaluate(normalizedTime);
        currentHumidity = activePreset.humidityCurve.Evaluate(normalizedTime);
        windSpeed = activePreset.windSpeedCurve.Evaluate(normalizedTime);

        if (isTransitioning && targetPreset != null)
        {
            currentTemperature = Mathf.Lerp(activePreset.temperatureCurve.Evaluate(normalizedTime), targetPreset.temperatureCurve.Evaluate(normalizedTime), transitionProgress);
            currentHumidity = Mathf.Lerp(activePreset.humidityCurve.Evaluate(normalizedTime), targetPreset.humidityCurve.Evaluate(normalizedTime), transitionProgress);
            windSpeed = Mathf.Lerp(activePreset.windSpeedCurve.Evaluate(normalizedTime), targetPreset.windSpeedCurve.Evaluate(normalizedTime), transitionProgress);
        }
        else
        {
            currentTemperature = activePreset.temperatureCurve.Evaluate(normalizedTime);
            currentHumidity = activePreset.humidityCurve.Evaluate(normalizedTime);
            windSpeed = activePreset.windSpeedCurve.Evaluate(normalizedTime);
        }
        
        perceivedWindTemperature = currentTemperature - (windSpeed*windChillFactor);
        
    }

    void ApplyVisuals()
    {
        UpdateLight();
        CheckShadowStatus();
        ApplyCloudsAndFog();
        ApplyWind();
    }

    void UpdateLight()
    {
        float sunRotation = (currentTime / 24f) * 360f;
        sunLight.transform.rotation = Quaternion.Euler(sunRotation - 90f, sunPosition, 0f);
        moonLight.transform.rotation = Quaternion.Euler(sunRotation + 90f, sunPosition, 0f);

        float normalizedTime = currentTime / 24f;
        
        Light sunLightData = sunLight.GetComponent<Light>();
        Light moonLightData = moonLight.GetComponent<Light>();

        if(sunLightData != null)
        {
            sunLightData.intensity = sunIntensity * sunIntensityMultiplier.Evaluate(normalizedTime);
            sunLightData.colorTemperature = sunTemperatureCurve.Evaluate(normalizedTime) * 10000f;
        }
        
        if(moonLightData != null)
        {
            moonLightData.intensity = moonIntensity * moonIntensityMultiplier.Evaluate(normalizedTime);
            moonLightData.colorTemperature = moonTemperatureCurve.Evaluate(normalizedTime) * 10000f;
        }
    }
    void CheckShadowStatus()
    {
        HDAdditionalLightData sunLightData = sunLight.GetComponent<HDAdditionalLightData>();
        HDAdditionalLightData moonLightData = moonLight.GetComponent<HDAdditionalLightData>();

        bool isDayTime = currentTime >= 6f && currentTime <= 18f;
        isDay = isDayTime;

        sunLightData.EnableShadows(isDayTime);
        moonLightData.EnableShadows(!isDayTime);

        sunActive = currentTime >= 5.7f && currentTime <= 18.3f;
        sunLight.gameObject.SetActive(sunActive);

        moonActive = !(currentTime >= 6.3f && currentTime <= 17.7f);
        moonLight.gameObject.SetActive(moonActive);
    }
    void ApplyCloudsAndFog()
{
    if (activePreset == null) return;
    float normalizedTime = currentTime / 24f;

    float density      = activePreset.cloudsDensityCurve.Evaluate(normalizedTime);
    float altitude     = activePreset.cloudsBottomAltitudeCurve.Evaluate(normalizedTime);
    float fogDensity   = activePreset.fogDensityCurve.Evaluate(normalizedTime);
    float shapeFactor  = activePreset.shapeFactor;
    float erosionFactor = activePreset.erosionFactor;
    float altRange     = activePreset.AltitudeRange;
    float rainIntensity = activePreset.rainIntensity;
    Color rainFogColor  = activePreset.rainFogColor;

    if (isTransitioning && targetPreset != null)
    {
        density       = Mathf.Lerp(density, targetPreset.cloudsDensityCurve.Evaluate(normalizedTime), transitionProgress);
        altitude      = Mathf.Lerp(altitude, targetPreset.cloudsBottomAltitudeCurve.Evaluate(normalizedTime), transitionProgress);
        fogDensity    = Mathf.Lerp(fogDensity, targetPreset.fogDensityCurve.Evaluate(normalizedTime), transitionProgress);
        shapeFactor   = Mathf.Lerp(shapeFactor, targetPreset.shapeFactor, transitionProgress);
        erosionFactor = Mathf.Lerp(erosionFactor, targetPreset.erosionFactor, transitionProgress);
        altRange      = Mathf.Lerp(altRange, targetPreset.AltitudeRange, transitionProgress);
        rainIntensity = Mathf.Lerp(rainIntensity, targetPreset.rainIntensity, transitionProgress);
        rainFogColor  = Color.Lerp(rainFogColor, targetPreset.rainFogColor, transitionProgress);
    }

    if (volumetricClouds != null)
    {
        volumetricClouds.densityMultiplier.value = density;
        volumetricClouds.bottomAltitude.value    = altitude;
        volumetricClouds.shapeFactor.value       = shapeFactor;
        volumetricClouds.erosionFactor.value     = erosionFactor;
        volumetricClouds.altitudeRange.value     = altRange;
    }

    if (volumetricFog != null)
    {
        volumetricFog.meanFreePath.value = fogDensity;
        volumetricFog.albedo.value = Color.Lerp(baseFogColor, rainFogColor, rainIntensity);
    }
}
    void ApplyWind()
    {
        if(windZone != null)
        {
            windZone.windMain = windSpeed;
            Vector3 windDir3D = new Vector3(windDirection.x,0f,windDirection.y);
            if(windDir3D != Vector3.zero)
            {
                windZone.transform.rotation = Quaternion.LookRotation(windDir3D);
            }
        }
        
        if(visualEnv != null)
        {
            visualEnv.windSpeed.value = windSpeed * 20f;
            //converting the 2D wind direction to an angle for the shader, where (1,0) is 0 degrees, (0,1) is 90 degrees, (-1,0) is 180 degrees and (0,-1) is 270 degrees
            // float windAngle = Mathf.Atan2(windDirection.y, windDirection.x) * Mathf.Rad2Deg;
            // visualEnv.windOrientation.value = windAngle;
            visualEnv.windOrientation.value = windZone.transform.eulerAngles.y;
        }
    }
    void HandleWeatherTransitionTimer()
    {
        if(!isTransitioning && Time.time >= nextWeatherChangeTime && availablePresets.Count > 1)
        {
            ChangeWeatherPreset();
        }

        if(isTransitioning)
        {
            transitionProgress += Time.deltaTime /weatherTransitionDuration;

            if(transitionProgress>=1f)
            {
                transitionProgress = 1f;
                isTransitioning = false;
                activePreset = targetPreset;
                SetNextWeatherChangeTimer();
            }
        }
    }
    void SetNextWeatherChangeTimer()
    {
        nextWeatherChangeTime = Time.time + Random.Range(minTimeBetweenWeatherChange, maxTimeBetweenWeatherChange);
    }
    void ChangeWeatherPreset()
    {
        if (availablePresets.Count <= 1) return;

        // Pick a random preset that is different from the current one
        WeatherPreset nextPreset = availablePresets[Random.Range(0, availablePresets.Count)];
        while (nextPreset == activePreset)
        {
            nextPreset = availablePresets[Random.Range(0, availablePresets.Count)];
        }

        targetPreset = nextPreset;
        transitionProgress = 0f;
        isTransitioning = true;
    }
}