using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;

public class UI_CustomizationSettings : MonoBehaviour
{
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Material ghostObstructedMaterial;
    
    private UIDocument UI_doc => GetComponent<UIDocument>();
    

    private VisualElement buildingGhostColor;
    private VisualElement buildingGhostObstructedColor;

    void Awake()
    {
        VisualElement root = UI_doc.rootVisualElement.Q<VisualElement>("CustomizationSettings");

    
        buildingGhostColor = root.Q<VisualElement>("BuildingGhostColor");
        SetupColorInput(buildingGhostColor, ghostMaterial, "GhostColorKey");


        buildingGhostObstructedColor = root.Q<VisualElement>("BuildingGhostObstructedColor");
        SetupColorInput(buildingGhostObstructedColor, ghostObstructedMaterial, "GhostObstructedKey");
    }

  
    private void SetupColorInput(VisualElement container, Material mat, string saveKey)
    {
        TextField input = container.Q<TextField>("ColorInput");
        input.maxLength = 7;
        input.RegisterValueChangedCallback(evt =>
        {
            UpdateColor(evt, input, mat);
            if (input.value.Length == 7)
            {
                SaveColor(saveKey, input);
            }
        });
    }

    void Start()
    {
        // Ładowanie pierwszego materiału
        TextField inputNormal = buildingGhostColor.Q<TextField>("ColorInput");
        LoadColor("GhostColorKey", inputNormal, ghostMaterial, "#00FF00");

        // Ładowanie drugiego materiału
        TextField inputObstructed = buildingGhostObstructedColor.Q<TextField>("ColorInput");
        LoadColor("GhostObstructedKey", inputObstructed, ghostObstructedMaterial, "#FF0000");
    }



    void UpdateColor(ChangeEvent<string> evt, TextField UInputElement, Material ColorMaterial)
    {
        VisualElement Coloroutput = UInputElement.parent.Q<VisualElement>("ColorPrev");
        string input = evt.newValue;
        string cleanHex = Regex.Replace(input, @"[^0-9a-fA-F]", "").ToUpper();

        if (cleanHex.Length > 6) cleanHex = cleanHex.Substring(0, 6);
        string finalValue = "#" + cleanHex;

        if (finalValue != input)
        {
            UInputElement.SetValueWithoutNotify(finalValue);
            UInputElement.schedule.Execute(() => UInputElement.SelectRange(finalValue.Length, finalValue.Length));
        }

        if (cleanHex.Length == 6 && ColorUtility.TryParseHtmlString(finalValue, out Color c))
        {
            c.a = 0.6f;
            Coloroutput.style.backgroundColor = c;
            if (ColorMaterial != null)
            {
                if (ColorMaterial.HasProperty("_UnlitColor")) ColorMaterial.SetColor("_UnlitColor", c);
                else if (ColorMaterial.HasProperty("_BaseColor")) ColorMaterial.SetColor("_BaseColor", c);
                else ColorMaterial.SetColor("_Color", c);
            }
        }
    }

    private void SaveColor(string key, TextField input)
    {
        PlayerPrefs.SetString(key, input.value);
        PlayerPrefs.Save();
    }

    private void LoadColor(string key, TextField input, Material material, string defaultHex = "#FFFFFF")
    {
        string savedHex = PlayerPrefs.GetString(key, defaultHex);
        input.value = savedHex;

        ChangeEvent<string> evt = ChangeEvent<string>.GetPooled("", savedHex);
        evt.target = input;
        UpdateColor(evt, input, material);
    }
}