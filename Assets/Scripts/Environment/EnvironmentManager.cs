using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using UnityEngine.VFX;

// ─────────────────────────────────────────────────────────────────────────────
//  EnvironmentManager
//
//  Singleton zarządzający:
//   • Czasem gry (godziny + dni)
//   • Systemem pogodowym
//   • Globalnym zasilaniem (isGlobalPowerOn)
//
//  SYSTEM ELEKTRYKI:
//   • Na starcie losowany jest dzień odcięcia prądu (między minPowerCutDay
//     a maxPowerCutDay).
//   • Po przekroczeniu tego dnia prąd zostaje permanentnie wyłączony.
//   • Odpalany jest event OnPowerCut, na który subskrybują się LightSwitch'e.
// ─────────────────────────────────────────────────────────────────────────────
public class EnvironmentManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static EnvironmentManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────
    //  POWER SYSTEM
    // ─────────────────────────────────────────────────────────────
    [Header("Power System")]
    [Tooltip("Czy sieć elektryczna jest aktualnie aktywna?")]
    public bool isGlobalPowerOn = true;

    [Tooltip("Minimalny dzień, w którym może nastąpić odcięcie prądu.")]
    public int minPowerCutDay = 3;

    [Tooltip("Maksymalny dzień, w którym może nastąpić odcięcie prądu.")]
    public int maxPowerCutDay = 10;

    // Wylosowany dzień odcięcia (Read-only w Inspectorze)
    [SerializeField, HideInInspector]
    private int _powerCutDay;

    /// <summary>
    /// Event odpalany jednorazowo w momencie trwałego odcięcia prądu.
    /// LightSwitch subskrybuje się na niego w OnEnable i wypisuje w OnDisable.
    /// </summary>
    public static event System.Action OnPowerCut;

    // ─────────────────────────────────────────────────────────────
    //  TIME SYSTEM
    // ─────────────────────────────────────────────────────────────
    [Header("Time settings")]
    [Range(0, 24)]
    public float currentTime;
    public float timeSpeed = 1f;

    [Header("Current Time")]
    public string currentTimeString;

    /// <summary>Aktualny dzień gry (zaczyna się od 1).</summary>
    public int currentDay { get; private set; } = 1;

    // ─────────────────────────────────────────────────────────────
    //  WEATHER SYSTEM
    // ─────────────────────────────────────────────────────────────
    [Header("Weather System")]
    public List<WeatherPreset> availablePresets;
    public WeatherPreset activePreset;
    private WeatherPreset _targetPreset;

    [Header("Weather Change Settings")]
    public float minTimeBetweenWeatherChange = 120f;
    public float maxTimeBetweenWeatherChange = 300f;
    public float weatherTransitionDuration   = 20f;

    private float _nextWeatherChangeTime;
    private float _transitionProgress = 0f;
    private bool  _isTransitioning    = false;

    // ─────────────────────────────────────────────────────────────
    //  SUN / MOON
    // ─────────────────────────────────────────────────────────────
    [Header("Sun settings")]
    public Light sunLight;
    public float sunPosition  = 1f;
    public float sunIntensity = 1f;
    public AnimationCurve sunIntensityMultiplier;
    public AnimationCurve sunTemperatureCurve;

    public bool isDay    = true;
    public bool sunActive  = true;
    public bool moonActive = true;

    [Header("Moon settings")]
    public Light moonLight;
    public float moonIntensity = 1f;
    public AnimationCurve moonIntensityMultiplier;
    public AnimationCurve moonTemperatureCurve;

    // ─────────────────────────────────────────────────────────────
    //  CLOUDS / FOG
    // ─────────────────────────────────────────────────────────────
    [Header("Cloud settings")]
    public Volume globalVolume;
    private VolumetricClouds _volumetricClouds;

    [Header("Weather parameters (Read-only)")]
    public float currentTemperature;
    [Range(0, 100)] public float currentHumidity;
    public float windSpeed;

    // ─────────────────────────────────────────────────────────────
    //  RAIN VFX
    // ─────────────────────────────────────────────────────────────
    [Header("Rain VFX")]
    public VisualEffect rainVFX;

    [Header("VFX Parameter Names")]
    public string rainSpawnRateName = "SpawnRate";

    [Tooltip("Max particles/s at rainIntensity = 1")]
    public float rainMaxSpawnRate = 200f;

    [Tooltip("Rain won't play at all below this threshold")]
    [Range(0f, 0.1f)]
    public float rainMinIntensityThreshold = 0.02f;

    // ─────────────────────────────────────────────────────────────
    //  WIND
    // ─────────────────────────────────────────────────────────────
    [Header("Wind settings")]
    public Vector2 windDirection       = new Vector2(1f, 0f);
    public float   perceivedWindTemperature;
    public float   windChillFactor = 0.5f;
    public WindZone windZone;

    // ─────────────────────────────────────────────────────────────
    //  FOG
    // ─────────────────────────────────────────────────────────────
    [Header("Fog settings")]
    private Fog _volumetricFog;
    public Color baseFogColor = Color.white;

    // ─────────────────────────────────────────────────────────────
    //  Internals
    // ─────────────────────────────────────────────────────────────
    private VisualEnvironment _visualEnv;

    // ── Public accessors (kompatybilność z poprzednim kodem) ──────
    public bool          IsTransitioning  => _isTransitioning;
    public WeatherPreset TargetPreset     => _targetPreset;
    public float         TransitionProgress => _transitionProgress;

    // ═════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═════════════════════════════════════════════════════════════
    void Awake()
    {
        // Singleton — jeden manager na całą scenę
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[EnvironmentManager] Duplikat Singletona — niszczę nadmiarowy obiekt.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Losujemy dzień odcięcia prądu już w Awake, zanim cokolwiek się odpyta
        _powerCutDay = Random.Range(minPowerCutDay, maxPowerCutDay + 1);
        Debug.Log($"[EnvironmentManager] Prąd zostanie odcięty w dniu: {_powerCutDay}");
    }

    void Start()
    {
        // Clouds
        if (globalVolume.profile.TryGet<VolumetricClouds>(out _volumetricClouds))
        {
            _volumetricClouds.densityMultiplier.overrideState = true;
            _volumetricClouds.bottomAltitude.overrideState    = true;
            _volumetricClouds.shapeFactor.overrideState       = true;
            _volumetricClouds.erosionFactor.overrideState     = true;
            _volumetricClouds.altitudeRange.overrideState     = true;
        }

        // Fog
        if (globalVolume.profile.TryGet<Fog>(out _volumetricFog))
        {
            _volumetricFog.meanFreePath.overrideState = true;
            _volumetricFog.albedo.overrideState       = true;
        }

        // Wind (visual shader)
        if (globalVolume.profile.TryGet<VisualEnvironment>(out _visualEnv))
        {
            _visualEnv.windSpeed.overrideState      = true;
            _visualEnv.windOrientation.overrideState = true;
        }

        UpdateTimeText();
        CheckShadowStatus();
    }

    void Update()
    {
        AdvanceTime();
        UpdateTimeText();
        CalculateWeatherLogic();
        HandleWeatherTransitionTimer();
        ApplyVisuals();
    }

    void OnValidate()
    {
        if (sunLight == null || moonLight == null) return;
        UpdateLight();
        CheckShadowStatus();
        if (activePreset != null) ApplyCloudsAndFog();
    }

    // ═════════════════════════════════════════════════════════════
    //  TIME & DAY
    // ═════════════════════════════════════════════════════════════

    void AdvanceTime()
    {
        currentTime += Time.deltaTime * timeSpeed;

        if (currentTime >= 24f)
        {
            currentTime -= 24f;
            currentDay++;
            OnNewDayStarted();
        }
    }

    /// <summary>
    /// Natychmiastowo przesuwa czas gry o podaną liczbę godzin.
    /// Obsługuje przejście przez próg 24h i inkrementację currentDay.
    /// Po zakończeniu wymusza UpdateLight() i CheckShadowStatus(), żeby
    /// oświetlenie odpowiadało nowej porze tuż po obudzeniu gracza.
    /// Wywoływane przez SleepManager.
    /// </summary>
    /// <param name="hours">Liczba godzin do przeskoczenia (np. 8).</param>
    public void SkipTime(int hours)
    {
        currentTime += hours;
    
        // Obsługa przejścia przez północ (może być więcej niż jedna doba)
        while (currentTime >= 24f)
        {
            currentTime -= 24f;
            currentDay++;
            OnNewDayStarted();   // sprawdza odcięcie prądu, loguje nowy dzień
        }
    
        // Natychmiastowa aktualizacja oświetlenia — gracz budzi się
        // z właściwym słońcem/księżycem, bez czekania na kolejny Update()
        UpdateLight();
        CheckShadowStatus();
    
        Debug.Log($"[EnvironmentManager] SkipTime +{hours}h → {currentTimeString}, dzień {currentDay}");
    }

    /// <summary>
    /// Natychmiastowo przeskakuje do losowego presetu pogody (innego niż aktualny).
    /// Wywoływana przez SleepManager na czarnym ekranie — gracz nic nie widzi,
    /// więc lerp jest zbędny. Po snapie resetuje timer zmiany pogody, żeby
    /// nowa pogoda nie zmieniła się od razu po przebudzeniu.
    ///
    /// Jeśli lista availablePresets ma tylko jeden preset (lub jest pusta),
    /// metoda cicho kończy działanie bez błędu.
    /// </summary>
    public void SnapRandomWeather()
    {
        // Potrzebujemy co najmniej 2 presetów, żeby mieć z czego losować
        if (availablePresets == null || availablePresets.Count < 2) return;
    
        // Losujemy preset inny niż aktualny — identyczna logika co ChangeWeatherPreset(),
        // ale bez uruchamiania przejścia (lerpa)
        WeatherPreset next = activePreset;
        while (next == activePreset)
            next = availablePresets[Random.Range(0, availablePresets.Count)];
    
        // Snap: od razu ustawiamy aktywny preset, zerujemy trwające przejście
        activePreset        = next;
        _targetPreset       = null;
        _isTransitioning    = false;
        _transitionProgress = 0f;
    
        // Wymuszamy natychmiastowe zastosowanie wizualiów chmur/mgły/deszczu
        // (Update zrobi to za chwilę, ale chcemy pewność już w tej klatce)
        ApplyCloudsAndFog();
        ApplyRainVFX();
    
        // Reset timera — gracz budzi się ze stabilną pogodą, nie zmienia się
        // ona od razu w kolejnych sekundach po przebudzeniu
        SetNextWeatherChangeTimer();
    
        Debug.Log($"[EnvironmentManager] SnapRandomWeather → {activePreset.name}");
    }

    /// <summary>Wywoływana raz na początku każdego nowego dnia.</summary>
    void OnNewDayStarted()
    {
        Debug.Log($"[EnvironmentManager] Nowy dzień: {currentDay}");

        // Sprawdzamy czy właśnie nadszedł dzień odcięcia prądu
        if (isGlobalPowerOn && currentDay >= _powerCutDay)
        {
            CutPower();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  POWER
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Trwale odcina prąd na mapie i powiadamia wszystkie włączniki.
    /// Może być wywołana z zewnątrz (cutscenka, skrypt fabularny itp.).
    /// </summary>
    public void CutPower()
    {
        if (!isGlobalPowerOn) return; // już odcięty, nie odpala eventu drugi raz

        isGlobalPowerOn = false;
        Debug.Log($"[EnvironmentManager] PRĄD ODCIĘTY w dniu {currentDay}!");

        // Powiadamiamy wszystkich subskrybentów (LightSwitch)
        OnPowerCut?.Invoke();
    }

    void UpdateTimeText()
    {
        currentTimeString =
            Mathf.Floor(currentTime).ToString("00") + ":" +
            ((currentTime % 1) * 60f).ToString("00");
    }

    // ═════════════════════════════════════════════════════════════
    //  WEATHER
    // ═════════════════════════════════════════════════════════════

    void CalculateWeatherLogic()
    {
        float t = currentTime / 24f;

        if (_isTransitioning && _targetPreset != null)
        {
            currentTemperature = Mathf.Lerp(
                activePreset.temperatureCurve.Evaluate(t),
                _targetPreset.temperatureCurve.Evaluate(t),
                _transitionProgress);

            currentHumidity = Mathf.Lerp(
                activePreset.humidityCurve.Evaluate(t),
                _targetPreset.humidityCurve.Evaluate(t),
                _transitionProgress);

            windSpeed = Mathf.Lerp(
                activePreset.windSpeedCurve.Evaluate(t),
                _targetPreset.windSpeedCurve.Evaluate(t),
                _transitionProgress);
        }
        else
        {
            currentTemperature = activePreset.temperatureCurve.Evaluate(t);
            currentHumidity    = activePreset.humidityCurve.Evaluate(t);
            windSpeed          = activePreset.windSpeedCurve.Evaluate(t);
        }

        perceivedWindTemperature = currentTemperature - (windSpeed * windChillFactor);
    }

    void ApplyVisuals()
    {
        UpdateLight();
        CheckShadowStatus();
        ApplyCloudsAndFog();
        ApplyWind();
        ApplyRainVFX();
    }

    void UpdateLight()
    {
        float sunRotation  = (currentTime / 24f) * 360f;
        sunLight.transform.rotation  = Quaternion.Euler(sunRotation - 90f, sunPosition, 0f);
        moonLight.transform.rotation = Quaternion.Euler(sunRotation + 90f, sunPosition, 0f);

        float t = currentTime / 24f;

        Light sunData  = sunLight.GetComponent<Light>();
        Light moonData = moonLight.GetComponent<Light>();

        if (sunData != null)
        {
            sunData.intensity        = sunIntensity * sunIntensityMultiplier.Evaluate(t);
            sunData.colorTemperature = sunTemperatureCurve.Evaluate(t) * 10000f;
        }

        if (moonData != null)
        {
            moonData.intensity        = moonIntensity * moonIntensityMultiplier.Evaluate(t);
            moonData.colorTemperature = moonTemperatureCurve.Evaluate(t) * 10000f;
        }
    }

    void CheckShadowStatus()
    {
        HDAdditionalLightData sunLightData  = sunLight.GetComponent<HDAdditionalLightData>();
        HDAdditionalLightData moonLightData = moonLight.GetComponent<HDAdditionalLightData>();

        bool isDayTime = currentTime >= 6f && currentTime <= 18f;
        isDay = isDayTime;

        sunLightData.EnableShadows(isDayTime);
        moonLightData.EnableShadows(!isDayTime);

        sunActive  = currentTime >= 5.7f && currentTime <= 18.3f;
        sunLight.gameObject.SetActive(sunActive);

        moonActive = !(currentTime >= 6.3f && currentTime <= 17.7f);
        moonLight.gameObject.SetActive(moonActive);
    }

    void ApplyCloudsAndFog()
    {
        if (activePreset == null) return;
        float t = currentTime / 24f;

        float density       = activePreset.cloudsDensityCurve.Evaluate(t);
        float altitude      = activePreset.cloudsBottomAltitudeCurve.Evaluate(t);
        float fogDensity    = activePreset.fogDensityCurve.Evaluate(t);
        float shapeFactor   = activePreset.shapeFactor;
        float erosionFactor = activePreset.erosionFactor;
        float altRange      = activePreset.AltitudeRange;
        float rainIntensity = activePreset.rainIntensity;
        Color rainFogColor  = activePreset.rainFogColor;

        if (_isTransitioning && _targetPreset != null)
        {
            density       = Mathf.Lerp(density,       _targetPreset.cloudsDensityCurve.Evaluate(t),       _transitionProgress);
            altitude      = Mathf.Lerp(altitude,      _targetPreset.cloudsBottomAltitudeCurve.Evaluate(t), _transitionProgress);
            fogDensity    = Mathf.Lerp(fogDensity,    _targetPreset.fogDensityCurve.Evaluate(t),           _transitionProgress);
            shapeFactor   = Mathf.Lerp(shapeFactor,   _targetPreset.shapeFactor,                           _transitionProgress);
            erosionFactor = Mathf.Lerp(erosionFactor, _targetPreset.erosionFactor,                         _transitionProgress);
            altRange      = Mathf.Lerp(altRange,      _targetPreset.AltitudeRange,                         _transitionProgress);
            rainIntensity = Mathf.Lerp(rainIntensity, _targetPreset.rainIntensity,                         _transitionProgress);
            rainFogColor  = Color.Lerp(rainFogColor,  _targetPreset.rainFogColor,                          _transitionProgress);
        }

        if (_volumetricClouds != null)
        {
            _volumetricClouds.densityMultiplier.value = density;
            _volumetricClouds.bottomAltitude.value    = altitude;
            _volumetricClouds.shapeFactor.value       = shapeFactor;
            _volumetricClouds.erosionFactor.value     = erosionFactor;
            _volumetricClouds.altitudeRange.value     = altRange;
        }

        if (_volumetricFog != null)
        {
            _volumetricFog.meanFreePath.value = fogDensity;
            _volumetricFog.albedo.value       = Color.Lerp(baseFogColor, rainFogColor, rainIntensity);
        }
    }

    void ApplyWind()
    {
        if (windZone != null)
        {
            windZone.windMain = windSpeed;
            Vector3 windDir3D = new Vector3(windDirection.x, 0f, windDirection.y);
            if (windDir3D != Vector3.zero)
                windZone.transform.rotation = Quaternion.LookRotation(windDir3D);
        }

        if (_visualEnv != null)
        {
            _visualEnv.windSpeed.value      = windSpeed * 20f;
            _visualEnv.windOrientation.value = windZone != null
                ? windZone.transform.eulerAngles.y
                : 0f;
        }
    }

    void ApplyRainVFX()
    {
        if (rainVFX == null || activePreset == null) return;

        float intensity = activePreset.rainIntensity;
        if (_isTransitioning && _targetPreset != null)
            intensity = Mathf.Lerp(intensity, _targetPreset.rainIntensity, _transitionProgress);

        bool shouldPlay = intensity >= rainMinIntensityThreshold;

        if (shouldPlay != rainVFX.gameObject.activeSelf)
            rainVFX.gameObject.SetActive(shouldPlay);

        if (shouldPlay)
            rainVFX.SetFloat(rainSpawnRateName, intensity * rainMaxSpawnRate);
    }

    void HandleWeatherTransitionTimer()
    {
        if (!_isTransitioning && Time.time >= _nextWeatherChangeTime && availablePresets.Count > 1)
            ChangeWeatherPreset();

        if (_isTransitioning)
        {
            _transitionProgress += Time.deltaTime / weatherTransitionDuration;

            if (_transitionProgress >= 1f)
            {
                _transitionProgress = 1f;
                _isTransitioning    = false;
                activePreset        = _targetPreset;
                SetNextWeatherChangeTimer();
            }
        }
    }

    void SetNextWeatherChangeTimer()
    {
        _nextWeatherChangeTime = Time.time +
            Random.Range(minTimeBetweenWeatherChange, maxTimeBetweenWeatherChange);
    }

    void ChangeWeatherPreset()
    {
        if (availablePresets.Count <= 1) return;

        WeatherPreset next = activePreset;
        while (next == activePreset)
            next = availablePresets[Random.Range(0, availablePresets.Count)];

        _targetPreset       = next;
        _transitionProgress = 0f;
        _isTransitioning    = true;
    }
}