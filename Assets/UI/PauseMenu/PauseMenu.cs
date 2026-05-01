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
        Button CloseSettingsBtn = Settings.Q<Button>("closeSettings");
        UI_Logs.Log(CloseSettingsBtn);
        CloseSettingsBtn.clickable.clicked += () => HideOptionWindow();
        CloseSettingsBtn.clickable.clicked += () => { UI_Logs.Log("Zamykam Ustawienia");};
        Button ButtonBack = pauseMenu.Q<Button>("BackToMenu");
        ButtonBack.clickable.clicked += () =>
        {
            SceneManager.LoadScene("Main Menu");
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
