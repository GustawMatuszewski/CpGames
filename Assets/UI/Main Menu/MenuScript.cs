using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    private UIDocument UI_doc => GetComponent<UIDocument>();
    VisualElement  root;
    VisualElement  Settings;
    TabView  SettingsTabs;
 
  
    void Awake()
    {
        root = UI_doc.rootVisualElement;
        root.Q<VisualElement>("OptionsMenu").style.display = DisplayStyle.Flex;
        Settings= root.Q<VisualElement>("Settings");
        SettingsTabs = Settings.Q<TabView>("SettingsTabs");
        Button ButtonPlay = root.Q<Button>("PlayButton");
        
        ButtonPlay.clickable.clicked += () =>
        {
            // Sprawdzamy, czy Loader już istnieje (powinien być na scenie Menu)
            if (LoadingScene.Instance != null)
            {
                // 1. Ustawiamy nazwę mapy w loaderze
                // 2. Metoda PrepareAndLoad sama wywoła SceneManager.LoadScene("Loading")
                LoadingScene.Instance.PrepareAndLoad("GrayBoxedMap");
            }
            else
            {
                // Fail-safe: jeśli zapomniałeś wrzucić Loadera na scenę Menu
                Debug.LogWarning("LoadingScene Instance nie znaleziona! Ładowanie domyślne.");
                SceneManager.LoadScene("Loading");
            }

            Time.timeScale = 1; 
        };
        Button ButtonOptionsMenu = root.Q<Button>("OptionsMenu");
        ButtonOptionsMenu.clickable.clicked += () => toggleOptionsMenu();

        root.Query<Button>(className: "closeSettings").ForEach(btn => 
        {
            btn.clicked += () => { Settings.style.display = DisplayStyle.None; };
        });
        SettingsTabs.activeTabChanged += (Tab old, Tab newTab)=>UpdateTabs(old,newTab);
    }
    bool OptionsMenuOpen = false;
    void toggleOptionsMenu()
    {
        if (!OptionsMenuOpen)
        {
            Settings.style.display = DisplayStyle.Flex;
            UI_Logs.Log("OptionsMenuOpen"+Settings.style.display);
        }
        else
        {
            Settings.style.display = DisplayStyle.None;
            UI_Logs.Log("OptionsMenuClosed"+Settings.style.display);
        }
        OptionsMenuOpen = !OptionsMenuOpen;
    }

    void UpdateTabs(Tab old, Tab newTab)
    {
        //newTab.Q<Label>("settingsTitleLabel").text = newTab.label;

    }


}