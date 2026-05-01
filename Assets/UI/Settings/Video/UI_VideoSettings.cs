using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class UI_VideoSettings : MonoBehaviour
{
    private UIDocument UI_doc => GetComponent<UIDocument>();
    void Awake()
    {

        VisualElement root = UI_doc.rootVisualElement;
        root= root.Q<VisualElement>("VideoSettings");//poprostu ogrania tylko i wylacznie video settings nic wiecej
        Resolution[] resolutions = Screen.resolutions;
        List<string> options = resolutions
            .Select(res => $"{res.width}x{res.height}")
            .Distinct()
            .Reverse()
            .ToList();
        DropdownField dropdown = root.Q<DropdownField>("Res");
        dropdown.choices = options;
        
        string currentRes = $"{Screen.width}x{Screen.height}";
        if (options.Contains(currentRes))
        {
            dropdown.value = currentRes;
        }
        else if (options.Count > 0)
        {
            dropdown.value = options[0]; // Ustaw najwyższą dostępną
            Debug.LogWarning($"Aktualna rozdzielczość {currentRes} nie jest wspierana przez monitor. Wybrano: {options[0]}");
        }
        dropdown.RegisterValueChangedCallback(evt => {
            ApplyResolution(evt.newValue);
        });
    }
    
    
    
    private void ApplyResolution(string resString)
    {
        // Rozdziel stringa z powrotem na liczby
        string[] dimensions = resString.Split('x');
        int width = int.Parse(dimensions[0]);
        int height = int.Parse(dimensions[1]);

        // Zastosuj zmiany
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
        Debug.Log($"Zmieniono rozdzielczość na: {width}x{height}");
    }

}
