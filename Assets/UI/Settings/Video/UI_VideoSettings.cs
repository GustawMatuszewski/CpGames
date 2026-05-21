using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UI_VideoSettings : MonoBehaviour
{
    private UIDocument UI_doc => GetComponent<UIDocument>();
    public RawImage rawImage;
    public RenderTexture ditherTexture; 
    void Awake()

    {
        UpdateDitherResolution(Screen.width, Screen.height);
        VisualElement root = UI_doc.rootVisualElement;
        root= root.Q<VisualElement>("VideoSettings");//poprostu ogrania tylko i wylacznie video settings nic wiecej
      
        Resolution[] resolutions = Screen.resolutions;
        List<string> options = new List<string>();
       // List<string> options = resolutions
    //        .Select(res => $"{res.width}x{res.height}")
       //     .Distinct()
        //    .Reverse()
         //   .ToList();
         float screenAspect = (float)Screen.currentResolution.width / Screen.currentResolution.height;
        DropdownField dropdown = root.Q<DropdownField>("Res");
        foreach (var res in resolutions)
        {
            float resAspect = (float)res.width / res.height;
        
            // Sprawdzamy czy rozdzielczość pasuje do proporcji monitora
            if (Mathf.Abs(resAspect - screenAspect) < 0.01f)
            {
                string option = $"{res.width}x{res.height}";
                if (!options.Contains(option))
                {
                    options.Add(option);
                }
            }
        }
        options.Reverse();
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
        UpdateDitherResolution(width, height);
        // Zastosuj zmiany
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
        
        Debug.Log($"Zmieniono rozdzielczość na: {width}x{height}");
    }

    
    private void UpdateDitherResolution(int screenWidth, int screenHeight)
    {
        if (rawImage == null || ditherTexture == null) return;
        var panelSettings = GetComponent<UIDocument>().panelSettings;
        
        // 1. Dopasowanie RawImage do ekranu
        rawImage.rectTransform.sizeDelta = new Vector2(screenWidth, screenHeight);

        // 2. Obliczanie proporcjonalnej wielkości RenderTexture
        //1920 -> 375 oraz 1080 -> 225
        float scaleX = 375f / 1920f;
        float scaleY = 225f / 1080f;

        int targetWidth = Mathf.RoundToInt(screenWidth * scaleX);
        int targetHeight = Mathf.RoundToInt(screenHeight * scaleY);
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc != null && uiDoc.panelSettings != null)
        {
            uiDoc.panelSettings.scale = 1.0f;
        }

        if (ditherTexture.width != targetWidth || ditherTexture.height != targetHeight)
        {
            ditherTexture.Release();
            ditherTexture.width = targetWidth;
            ditherTexture.height = targetHeight;
            ditherTexture.Create();
        
           
        }
    }
}
