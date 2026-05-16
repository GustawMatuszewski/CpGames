using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

public class UI_GameOver : MonoBehaviour
{
    private UIDocument UI_doc => GetComponent<UIDocument>();
    private VisualElement root;
    public static UI_GameOver Instance;

    [Header("Ustawienia Sterowania")]
    // To pozwoli Ci przeciągnąć konkretną mapę z Twojego pliku .inputactions
    public InputActionAsset UIInput;


    private void Awake()
    {
        Instance = this;
        root = UI_doc.rootVisualElement;
        
        // Ukrywamy menu na starcie
        root.style.display = DisplayStyle.None;
       

        
        // Pobieranie przycisków
        Button newGameButton = root.Q<Button>("NewGame");
        Button statsButton = root.Q<Button>("Stats");
        Button menuButton = root.Q<Button>("Menu");
        Button quitButton = root.Q<Button>("Quit");

        // Obsługa kliknięć
        newGameButton.clicked += () => {
            Time.timeScale = 1f; // Pamiętaj o przywróceniu czasu przed przeładowaniem!
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        };

        statsButton.clicked += () => {
            Debug.Log("Stats clicked");
        };

        menuButton.clicked += () => {
            Time.timeScale = 1f;

            if (LoadingScene.Instance != null)
            {
 
                LoadingScene.Instance.PrepareAndLoad("Main Menu");
            }
            else
            {
                // Fail-safe: jeśli zapomniałeś wrzucić Loadera na scenę Menu
                Debug.LogWarning("LoadingScene Instance nie znaleziona! Ładowanie domyślne.");
                SceneManager.LoadScene("Main Menu");
            }
        };
        
        quitButton.clicked += () => {
            Application.Quit();
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #endif
        };
    }

    public void GameOver()
    {

        root.style.display = DisplayStyle.Flex;
        DisablePlayerControls();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UI_doc.sortingOrder = 100;
    }

    public void DisablePlayerControls()
    {



            UIInput.FindActionMap("UI").Disable();

        
    }

    public void EnablePlayerControls()
    {
        UIInput.FindActionMap("UI").Enable();
    }
}