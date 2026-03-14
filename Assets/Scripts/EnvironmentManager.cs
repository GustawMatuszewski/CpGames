using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Unity.VisualScripting;

public class EnvironmentManager : MonoBehaviour
{
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
    public AnimationCurve cloudsDensityCurve;
    public AnimationCurve cloudsAltitudeCurve;
    private VolumetricClouds volumetricClouds;

    [Header("Weather parameters")]
    public float currentTemperature;
    public AnimationCurve temperatureCurve;

    [Range(0,100)]
    public float currentHumidity;
    public AnimationCurve humidityCurve;

    [Header("Wind settings")]
    public float windSpeed;
    public Vector2 windDirection = new Vector2(1f, 0f);
    public AnimationCurve windSpeedCurve;
    [Tooltip("The temperature perveived with the wind, can be used for things like making the player feel colder when the wind is strong")]
    public float perceivedWindTemperature; // for player status stuff
    public float windChillFactor = 0.5f; // how much the wind affects the perceived temperature
    [Tooltip("Wind zone is used for plants and other things that react to wind")]
    public WindZone windZone; // for plants and other things that react to wind

    [Header("Fog settings")]
    public AnimationCurve fogDensityCurve;
    private Fog volumetricFog;


    private VisualEnvironment visualEnv;

    
    void Start()
    {

    if (globalVolume.profile.TryGet<VolumetricClouds>(out volumetricClouds))
    {
        volumetricClouds.densityMultiplier.overrideState = true;
        volumetricClouds.bottomAltitude.overrideState = true;
    }

    if (globalVolume.profile.TryGet<Fog>(out volumetricFog))
    {
        volumetricFog.meanFreePath.overrideState = true;
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

        ApplyVisuals();
    }
    private void OnValidate()
    {
        UpdateLight();
        CheckShadowStatus();
    }
    void UpdateTimeText()
    {
        currentTimeString = Mathf.Floor(currentTime).ToString("00") + ":" + ((currentTime%1)*60).ToString("00");
    }
    void CalculateWeatherLogic()
    {
        float normalizedTime = currentTime/24f;

        currentTemperature = temperatureCurve.Evaluate(normalizedTime);
        currentHumidity = humidityCurve.Evaluate(normalizedTime);
        windSpeed = windSpeedCurve.Evaluate(normalizedTime);

        if (windSpeed > 0)
        {
            perceivedWindTemperature = currentTemperature - (windSpeed*windChillFactor);
        }
        else
        {
            perceivedWindTemperature = currentTemperature;
        }
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
        float normalizedTime = currentTime/24f;
        if(volumetricClouds != null)
        {
            volumetricClouds.densityMultiplier.value = cloudsDensityCurve.Evaluate(normalizedTime);
            volumetricClouds.bottomAltitude.value = cloudsAltitudeCurve.Evaluate(normalizedTime);
        }

        if(volumetricFog != null)
        {
            volumetricFog.meanFreePath.value = fogDensityCurve.Evaluate(normalizedTime);
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
            visualEnv.windSpeed.value = windSpeed;
            //converting the 2D wind direction to an angle for the shader, where (1,0) is 0 degrees, (0,1) is 90 degrees, (-1,0) is 180 degrees and (0,-1) is 270 degrees
            float windAngle = Mathf.Atan2(windDirection.x, windDirection.y) * Mathf.Rad2Deg;
            visualEnv.windOrientation.value = windAngle;
        }
    }
}