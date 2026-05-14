using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using Cursor = UnityEngine.Cursor;

public class PauseMenu : MonoBehaviour
{
    private UIDocument UI_doc => GetComponent<UIDocument>();
    VisualElement  root;
    VisualElement  Settings;
    VisualElement  pauseMenu;
    TabView  SettingsTabs;
    public InputActionAsset inputActions;
    
    
    void Awake()
    {
        root = UI_doc.rootVisualElement.Q<VisualElement>("root");
        root.style.display = DisplayStyle.None;
       
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
            ToggleMenu(new InputAction.CallbackContext());
            

        };
        Button Quit = root.Q<Button>("Quit");
        Quit.clickable.clicked += () => 
        { 
            UI_doc.sortingOrder = -999;
            ToggleMenu(new InputAction.CallbackContext());
            UI_GameOver.Instance.GameOver();

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
    
    private void ToggleMenu(InputAction.CallbackContext context)
    {
        if (root.style.display == DisplayStyle.None)
        {
            UI_doc.sortingOrder = 100;
            root.style.display = DisplayStyle.Flex;
            Time.timeScale = 0; // Pauza gry
            Cursor.lockState = CursorLockMode.None; // Pokazuje myszkę
            Cursor.visible = true;
            inputActions.FindActionMap("UI").Disable();
            inputActions.FindActionMap("UI").FindAction("PauseMenu").Enable();
        }
        else
        {
            UI_doc.sortingOrder = -999;
            root.style.display = DisplayStyle.None;
            Time.timeScale = 1; // Wznowienie gry
            Cursor.lockState = CursorLockMode.Locked; // Chowa myszkę
            Cursor.visible = false;
            inputActions.FindActionMap("UI").Enable();
        }
    }
    void OnEnable()
    {
        var action = inputActions.FindActionMap("UI").FindAction("PauseMenu");
        if (action != null)
        {
            action.performed += ToggleMenu; // Używamy minusa, żeby przestać słuchać
        }
        inputActions.Enable();
    }

    void OnDisable()
    {
        var action = inputActions.FindActionMap("UI").FindAction("PauseMenu");
        if (action != null)
        {
            action.performed -= ToggleMenu; // Używamy minusa, żeby przestać słuchać
        }
        inputActions.Disable();
    }
 
}
