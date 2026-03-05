using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Unity.VisualScripting;

public class DayAndNightCycle : MonoBehaviour
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

    
    void Start()
    {
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
        UpdateLight();
        CheckShadowStatus();
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
}