using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PauseMenu : MonoBehaviour
{
    private UIDocument UI_doc => GetComponent<UIDocument>();
    VisualElement  root;
    VisualElement  Settings;
    VisualElement  pauseMenu;
    TabView  SettingsTabs;
    
    
    void Awake()
    {
        root = UI_doc.rootVisualElement;
        
        Settings= root.Q<VisualElement>("Settings");
        pauseMenu = root.Q<VisualElement>("PauseMenu");
        SettingsTabs = Settings.Q<TabView>("SettingsTabs");
        Button SettingsBtn = pauseMenu.Q<Button>("Options");
        SettingsBtn.clickable.clicked += () => ShowOptionWindow();
        root.Query<Button>(className: "closeSettings").ForEach(btn => 
        {
            btn.clicked += () => { Settings.style.display = DisplayStyle.None; };
        });
        Button ButtonBack = pauseMenu.Q<Button>("BackToMenu");
        ButtonBack.clickable.clicked += () =>
        {
            SceneManager.LoadScene("Main Menu");
        };
        Button Resume = root.Q<Button>("Resume");
        Resume.clickable.clicked += () => 
        { 
            UI_doc.sortingOrder = -999;
            root.style.display = DisplayStyle.None;
            
        };
    }

    void HideOptionWindow()
    {
            
           
                Settings.style.display = DisplayStyle.None;
                UI_Logs.Log("OptionsMenuClosed"+Settings.style.display);

    }
    

    void ShowOptionWindow()
    {
        Settings.style.display = DisplayStyle.Flex;
        UI_Logs.Log("OptionsMenuOpen"+Settings.style.display);
    }

    void Update()
    {
        
    }
}
