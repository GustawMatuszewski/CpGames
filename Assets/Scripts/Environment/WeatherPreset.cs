using UnityEngine;

[CreateAssetMenu(fileName = "WeatherPreset", menuName = "Weather/Preset")]
public class WeatherPreset : ScriptableObject
{
    [Header("General Settings")]
    public string weatherName;

    [Header("Time based curves (0-1)")]
    public AnimationCurve temperatureCurve;
    public AnimationCurve humidityCurve;
    public AnimationCurve windSpeedCurve;
    public AnimationCurve fogDensityCurve;
    public AnimationCurve cloudsDensityCurve;
    public AnimationCurve cloudsBottomAltitudeCurve;
    

    [Header("Clouds appearance")] //These are static for this preset
    [Range(0,1)] public float shapeFactor = 0.9f;
    [Range(0,1)] public float erosionFactor = 0.8f;
    public float AltitudeRange = 2000f;

    [Header("Rain settings")]
    [Range(0,1)] public float rainIntensity;  // 0 = no rain, 1 = heavy rain
    public Color rainFogColor = Color.gray; // The color of the fog when it's raining
    
}
