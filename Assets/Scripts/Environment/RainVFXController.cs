using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.VFX;

public class RainVFXController : MonoBehaviour
{
    [Header("References")]
    public VisualEffect rainVFX;
    public EnvironmentManager envManager;

    [Header("VFX Parameter Names")]
    public string spawnRateName = "SpawnRate";
    
    [Tooltip("Max particles/s at rainIntensity =1")]
    public float maxSpawnRate = 200f;

    [Tooltip("Rain won't play at all below this threshold")]
    [Range(0f, 0.1f)]
    public float minIntensityThreshold = 0.02f;

    private void Update(){
        if (rainVFX == null || envManager == null) return;

        float intensity = GetCurrentRainIntensity();

        bool shouldPlay = intensity >= minIntensityThreshold;

        if (shouldPlay != rainVFX.gameObject.activeSelf)
            rainVFX.gameObject.SetActive(shouldPlay);

        if (shouldPlay)
            rainVFX.SetFloat(spawnRateName, intensity * maxSpawnRate);
    }

    float GetCurrentRainIntensity()
    {
        if (envManager.activePreset == null) return 0f;

        float intensity = envManager.activePreset.rainIntensity;

        // Mirror the transition lerp from EnvironmentManager
        if (envManager.IsTransitioning && envManager.TargetPreset != null)
        {
            intensity = Mathf.Lerp(intensity,
                envManager.TargetPreset.rainIntensity,
                envManager.TransitionProgress);
        }

        return intensity;
    }
}
