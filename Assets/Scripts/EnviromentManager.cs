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
        UpdateTimeText();
        CheckShadowStatus();
        // Get vol clouds component :P
        if (globalVolume.profile.TryGet<VolumetricClouds>(out var clouds))
        {
            volumetricClouds = clouds;
        }
        // Get fog component
        if (globalVolume.profile.TryGet<Fog>(out var fog))
        {
            volumetricFog = fog;
        }
        //get visual enviroment
        if(globalVolume.profile.TryGet<VisualEnvironment>(out var env))
        {
            visualEnv = env;
            visualEnv.windSpeed.overrideState = true;
            visualEnv.windOrientation.overrideState = true;
        }
    }
    void Update()
    {
        currentTime += Time.deltaTime * timeSpeed;

        if(currentTime >= 24f)
        {
            currentTime = 0f;
        }

        UpdateTimeText();
        UpdateLight();
        CheckShadowStatus();
        UpdateClouds();
        UpdateWeather();
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
    void UpdateLight()
    {
        float sunRotation = (currentTime / 24f) * 360f;
        sunLight.transform.rotation = Quaternion.Euler(sunRotation - 90f, sunPosition, 0f);
        moonLight.transform.rotation = Quaternion.Euler(sunRotation + 90f, sunPosition, 0f);

        float normalizedTime = currentTime/24f;
        float sunIntensityCurve = sunIntensityMultiplier.Evaluate(normalizedTime);
        float moonIntensityCurve = moonIntensityMultiplier.Evaluate(normalizedTime);

        Light sunLightData = sunLight.GetComponent<Light>();
        Light moonLightData = moonLight.GetComponent<Light>();

        if(sunLightData != null)
        {
            sunLightData.intensity = sunIntensity * sunIntensityCurve;
        }
        if(moonLightData != null)
        {
            moonLightData.intensity = moonIntensity * moonIntensityCurve;
        }

        float sunTemperatureMultiplier = sunTemperatureCurve.Evaluate(normalizedTime);
        float moonTemperatureMultiplier = moonTemperatureCurve.Evaluate(normalizedTime);
        Light sunLightComponent = sunLight.GetComponent<Light>();
        Light moonLightComponent = moonLight.GetComponent<Light>();

        if(sunLightComponent != null)
        {
            sunLightComponent.colorTemperature = sunTemperatureMultiplier*10000f;
        }
        if(moonLightComponent != null)
        {
            moonLightComponent.colorTemperature = moonTemperatureMultiplier*10000f;
        }
    }
    void CheckShadowStatus()
    {
        HDAdditionalLightData sunLightData = sunLight.GetComponent<HDAdditionalLightData>();
        HDAdditionalLightData moonLightData = moonLight.GetComponent<HDAdditionalLightData>();

        float currentSunRotation = currentTime;
        if(currentSunRotation >= 6f && currentSunRotation <= 18f)
        {
            sunLightData.EnableShadows(true);
            moonLightData.EnableShadows(false);
            isDay=true;
        }
        else
        {
            sunLightData.EnableShadows(false);
            moonLightData.EnableShadows(true);
            isDay=false;
        }
        
        if(currentSunRotation >= 5.7f && currentSunRotation <= 18.3f)
        {
            sunLight.gameObject.SetActive(true);
            sunActive=true;
        }
        else
        {
            sunLight.gameObject.SetActive(false);
            sunActive=false;
        }

        if(currentSunRotation >= 6.3f && currentSunRotation <= 17.7f)
        {
            moonLight.gameObject.SetActive(false);
            moonActive=false;
        }
        else
        {
            moonLight.gameObject.SetActive(true);
            moonActive=true;
        }
    }
    void UpdateClouds()
    {
        if(volumetricClouds == null) return;

        float normalizedTime = currentTime/24f;
        
        volumetricClouds.densityMultiplier.overrideState = true;
        volumetricClouds.bottomAltitude.overrideState = true;
        volumetricClouds.densityMultiplier.value = cloudsDensityCurve.Evaluate(normalizedTime);
        volumetricClouds.bottomAltitude.value = cloudsAltitudeCurve.Evaluate(normalizedTime);
        
        volumetricClouds.globalWindSpeed.overrideState = true;

        volumetricClouds.globalWindSpeed.value= new WindParameter.WindParamaterValue
        {
            mode = WindParameter.WindOverrideMode.Custom,
            customValue =  windSpeed
        };
    }
    void UpdateWeather()
    {
        float normalizedTime = currentTime/24f;
        //wind temperature and humidity levels taken from curves
        currentTemperature = temperatureCurve.Evaluate(normalizedTime);
        currentHumidity = humidityCurve.Evaluate(normalizedTime);
        windSpeed = windSpeedCurve.Evaluate(normalizedTime);

        if(windSpeed>0)
        {
            perceivedWindTemperature = currentTemperature - (windSpeed * windChillFactor);
        }else
        {
            perceivedWindTemperature = currentTemperature;
        }

        if(windZone != null)
        {
            windZone.windMain = windSpeed;
            //rotate wind zone in wind direction so grass moves correctly
            Vector3 windDir3D = new Vector3(windDirection.x, 0f, windDirection.y);
            if(windDir3D != Vector3.zero)
            {
                windZone.transform.rotation = Quaternion.LookRotation(windDir3D);
            }
        }

        if(volumetricFog != null)
        {
            volumetricFog.meanFreePath.overrideState = true;
            // the distance you see the fog from, ex. 10 fog is close, 1000 is far
            volumetricFog.meanFreePath.value = fogDensityCurve.Evaluate(normalizedTime);
        }
    }
}