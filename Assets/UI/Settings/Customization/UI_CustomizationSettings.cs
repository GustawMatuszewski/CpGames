using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
public class UI_CustomizationSettings : MonoBehaviour
{
    [SerializeField] private Material ghostMaterial;
    private UIDocument UI_doc => GetComponent<MenuScript>().UI_doc;
    VisualElement BuildingGhostColor;
    void Awake()
    {

        VisualElement root = UI_doc.rootVisualElement;
        root= root.Q<VisualElement>("CustomizationSettings");//poprostu ogrania tylko i wylacznie video settings nic wiecej
        BuildingGhostColor = root.Q<VisualElement>("BuildingGhostColor");
        TextField UInputElement = BuildingGhostColor.Q<TextField>("ColorInput");
        VisualElement Coloroutput = BuildingGhostColor.Q<VisualElement>("ColorPrev");
    
        UInputElement.maxLength = 7;
        UInputElement.value = "#000000";
        UInputElement.RegisterValueChangedCallback(evt =>
        {
            UpdateColor(evt, UInputElement);//funkcja odpowiadajac za aktualizacje koloru i kontrole inputa

        });
        if (ColorUtility.TryParseHtmlString(UInputElement.value, out Color c))
        {
            Coloroutput.style.backgroundColor = c;
        }

    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    void UpdateColor(ChangeEvent<string> evt,TextField UInputElement)
    {

 
        VisualElement Coloroutput = BuildingGhostColor.Q<VisualElement>("ColorPrev");
        string input = evt.newValue;
        string cleanHex = Regex.Replace(input, @"[^0-9a-fA-F]", "").ToUpper();

        if (cleanHex.Length > 6)
            cleanHex = cleanHex.Substring(0, 6);
        string finalValue = "#" + cleanHex;
        if (finalValue != input)
        {
            UInputElement.SetValueWithoutNotify(finalValue);
            if (!input.StartsWith("#"))
            {
                UInputElement.schedule.Execute(() => UInputElement.SelectRange(1, 1));
            }
            else
            {
                UInputElement.schedule.Execute(() => UInputElement.SelectRange(finalValue.Length, finalValue.Length));
            }
        }
        if (cleanHex.Length == 6 && ColorUtility.TryParseHtmlString(finalValue, out Color c))
        {
            Coloroutput.style.backgroundColor = c;
            if (ghostMaterial != null)
            {
                //strzelam jak sie nazywa proprety
                if (ghostMaterial.HasProperty("_BaseColor"))
                    ghostMaterial.SetColor("_BaseColor", c);
                else
                    ghostMaterial.SetColor("_Color", c);
            }
        }
    }
}
