using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  LightBulb
//
//  Steruje pojedynczą żarówką:
//   • Włącza / wyłącza komponent Light
//   • Przełącza emisję HDR na materiale MeshRenderer (żarzenie się)
//
//  PREFAB SETUP
//  ┌─ BulbObject  [LightBulb script] [MeshRenderer z materiałem emisyjnym]
//  └── (optional) Point Light  → przypisz do 'lightSource'
//
//  Materiał żarówki musi mieć włączoną emisję ("Emission" checkbox w HDRP Lit).
//  Shader property to "_EmissiveColor" w HDRP (lub "_EmissionColor" w URP/Built-in).
// ─────────────────────────────────────────────────────────────────────────────
[RequireComponent(typeof(MeshRenderer))]
public class LightBulb : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Light Source")]
    [Tooltip("Komponent Light tej żarówki (może być na dziecku prefaba).")]
    public Light _lightSource;

    [Header("Emission (materiał żarówki)")]
    [Tooltip("Kolor i intensywność emisji HDR gdy żarówka jest WŁĄCZONA.")]
    public Color _emissionColorOn  = new Color(1f, 0.95f, 0.8f, 1f) * 3f; // HDR, lekko ciepły

    [Tooltip("Kolor emisji gdy żarówka jest WYŁĄCZONA (zazwyczaj czarny = brak emisji).")]
    public Color _emissionColorOff = Color.black;

    [Tooltip("Nazwa shadera property dla emisji. HDRP Lit: '_EmissiveColor'. URP/Built-in: '_EmissionColor'.")]
    public string _emissionPropertyName = "_EmissiveColor";

    // ─────────────────────────────────────────────────────────────
    //  PRYWATNE
    // ─────────────────────────────────────────────────────────────

    private MeshRenderer _meshRenderer;
    private Material     _materialInstance; // instancja — żeby nie modyfikować shared material
    private bool         _isOn = false;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();

        // Tworzymy instancję materiału żeby nie psuć współdzielonego assetu
        if (_meshRenderer != null)
            _materialInstance = _meshRenderer.material; // Unity automatycznie instancjonuje

        // Jeśli nie przypisano Light ręcznie, szukamy na tym samym GO i w dzieciach
        if (_lightSource == null)
            _lightSource = GetComponentInChildren<Light>();
    }

    void Start()
    {
        // Ustawiamy stan startowy (domyślnie wyłączona)
        ApplyState(_isOn);
    }

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────

    /// <summary>Włącza lub wyłącza żarówkę.</summary>
    public void SetLight(bool on)
    {
        if (_isOn == on) return; // bez zbędnej pracy
        _isOn = on;
        ApplyState(_isOn);
    }

    /// <summary>Aktualny stan żarówki.</summary>
    public bool IsOn => _isOn;

    // ─────────────────────────────────────────────────────────────
    //  PRYWATNE HELPERS
    // ─────────────────────────────────────────────────────────────

    void ApplyState(bool on)
    {
        // 1. Komponent Light
        if (_lightSource != null)
            _lightSource.enabled = on;

        // 2. Emisja materiału
        if (_materialInstance != null)
        {
            Color targetColor = on ? _emissionColorOn : _emissionColorOff;
            _materialInstance.SetColor(_emissionPropertyName, targetColor);

            // W HDRP kluczowe jest odświeżenie GI (jeśli korzystasz z baked GI)
            if (on)
                DynamicGI.SetEmissive(_meshRenderer, targetColor);
            else
                DynamicGI.SetEmissive(_meshRenderer, Color.black);
        }
    }

    // Cleanup — materialInstance jest automatycznie zarządzana przez Unity
    // ale dla pewności przy Destroy zwróćmy ją
    void OnDestroy()
    {
        if (_materialInstance != null)
            Destroy(_materialInstance);
    }
}