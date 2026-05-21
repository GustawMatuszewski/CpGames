using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections.Generic; 
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
        InitPlayerStats();
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

        statsButton.clicked += () =>
        {
            DisplayAndUpdatePStats();
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
        PlayerStatus_screen.instance.PauseHide(true);
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

    private VisualElement playerStatsContainer;
    private void closePlayerStats()
    {
        playerStatsContainer.style.display = DisplayStyle.None;
        
    }

private void DisplayAndUpdatePStats()
    {
        playerStatsContainer.style.display = DisplayStyle.Flex;
        statsScrollView.Clear();//usuwam poprzednie słupki

        if (ZombieDeathTracker.Instance == null) return;

        List<ZombieStatsTrackerType> statsData = ZombieDeathTracker.Instance.AllDaysStats;
        int maxKills = 1;//maxymalna wysokosc słupka
        foreach (var data in statsData)
        {
            if (data.zombieKills > maxKills) maxKills = data.zombieKills;
        }
        foreach (var data in statsData)
        {
            VisualElement column = new VisualElement();
            column.style.width = 70;
            column.style.height = Length.Percent(100);
            column.style.marginRight = 20;
            column.style.justifyContent = Justify.FlexEnd; 
            column.style.alignItems = Align.Center;
            VisualElement barContainer = new VisualElement();
            barContainer.style.width = Length.Percent(100);
            barContainer.style.height = Length.Percent(80); 
            barContainer.style.justifyContent = Justify.FlexEnd;

          
            VisualElement bar = new VisualElement();
            bar.style.width = Length.Percent(80);
            
           
            float killPercentage = ((float)data.zombieKills / maxKills) * 100f;
            bar.style.height = Length.Percent(Mathf.Clamp(killPercentage, 5f, 100f));
            
          
            bar.style.backgroundColor = new StyleColor((Color)new Color32(180, 40, 40, 255));
            bar.style.borderTopLeftRadius = 8;
            bar.style.borderTopRightRadius = 8;

            // Liczba zabójstw liczba
            Label killsLabel = new Label(data.zombieKills.ToString());
            killsLabel.style.color = Color.white;
            killsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            killsLabel.style.marginBottom = 4;
            bar.Add(killsLabel);

            
            Label dayLabel = new Label($"Dzień {data.dayNumber}");// Podpis dnia na samym dole kolumny
            dayLabel.style.color = new StyleColor((Color)new Color32(200, 200, 200, 255));
            dayLabel.style.marginTop = 8;
            dayLabel.style.unityFontStyleAndWeight = FontStyle.Normal;

            
            barContainer.Add(bar);
            column.Add(barContainer);
            column.Add(dayLabel);

           
            statsScrollView.Add(column);
        }
    }
    private ScrollView statsScrollView;
   private void InitPlayerStats()
    {
        playerStatsContainer = new VisualElement();
        playerStatsContainer.name = "PlayerStats";
        playerStatsContainer.style.display = DisplayStyle.None;
        playerStatsContainer.style.position = Position.Absolute;
        playerStatsContainer.style.left = Length.Percent(50);
        playerStatsContainer.style.top = Length.Percent(50);
        playerStatsContainer.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50), 0);
        playerStatsContainer.style.width = Length.Percent(80);
        playerStatsContainer.style.height = Length.Percent(80 / 1.618f);
        playerStatsContainer.style.backgroundColor = new StyleColor(new Color32(40, 40, 40, 255));
        playerStatsContainer.style.borderBottomLeftRadius = 30;
        playerStatsContainer.style.borderBottomRightRadius = 30;
        playerStatsContainer.style.borderTopLeftRadius = 30;
        playerStatsContainer.style.borderTopRightRadius = 30;
        playerStatsContainer.style.borderBottomColor = new StyleColor(new Color32(60, 60, 60, 255));
        playerStatsContainer.style.paddingBottom = 40;
        playerStatsContainer.style.paddingTop = 40;
        playerStatsContainer.style.paddingLeft = 40;
        playerStatsContainer.style.paddingRight = 40;

        // Tytuł okna statystyk
        Label titleLabel = new Label("ZABÓJSTWA ZOMBIE PER DZIEŃ");
        titleLabel.style.color = Color.white;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.fontSize = 20;
        titleLabel.style.marginBottom = 20;
        playerStatsContainer.Add(titleLabel);

        // INICJALIZACJA SCROLLVIEW (Przewijanie poziome)
        statsScrollView = new ScrollView(ScrollViewMode.Horizontal);
        statsScrollView.style.flexGrow = 1;
        statsScrollView.style.marginTop = 10;
        statsScrollView.style.marginBottom = 10;
        playerStatsContainer.Add(statsScrollView);

        Button closeButton = new Button();
        closeButton.name = "CloseStatsButton";
        closeButton.text = "X"; 
        closeButton.style.display = DisplayStyle.Flex;
        closeButton.style.position = Position.Absolute;
        closeButton.style.right = 0;
        closeButton.style.top = 0;
        closeButton.style.translate = new Translate(Length.Percent(50), Length.Percent(-50), 0);
        closeButton.style.width = 48;
        closeButton.style.height = 48;
        closeButton.style.backgroundColor = new StyleColor(new Color32(150, 40, 40, 255));
        closeButton.style.color = Color.white;
        closeButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        closeButton.style.borderTopLeftRadius = 24;
        closeButton.style.borderTopRightRadius = 24;
        closeButton.style.borderBottomLeftRadius = 24;
        closeButton.style.borderBottomRightRadius = 24;
        
        closeButton.clickable.clicked += () => { closePlayerStats(); };
        playerStatsContainer.Add(closeButton);
        root.Add(playerStatsContainer);
    }

    public void EnablePlayerControls()
    {
        UIInput.FindActionMap("UI").Enable();
    }
    
}