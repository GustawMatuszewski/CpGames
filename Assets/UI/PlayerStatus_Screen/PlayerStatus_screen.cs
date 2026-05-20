using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
public class PlayerStatus_screen : MonoBehaviour
{
    public static PlayerStatus_screen instance;
    private UIDocument UI_doc => GetComponent<UIDocument>();
    private VisualElement root;
    
    // NOWOŚĆ: Referencja tylko do okna ze statusami
    private VisualElement statusContainer;

    void OnEnable()
    {
        EntityStatus.UIUpdateStats += ProcessStats;
    }

    void OnDisable()
    {
        EntityStatus.UIUpdateStats -= ProcessStats;
    }

    private void Awake()
    {
        instance = this;
        root = UI_doc.rootVisualElement;

        // Szukamy kontenera o nazwie "StatusContainer" (ustaw taką nazwę/ID w UI Builderze!)
        statusContainer = root.Q<VisualElement>("root");
        
        if (statusContainer == null)
        {
            Debug.LogError("Nie znaleziono StatusContainer w pliku UXML! Używam roota jako fallbacku.");
            statusContainer = root; // Awaryjnie, żeby nie było błędu
        }
    }

    private void ProcessStats(PlayerStats stats)
    {
        if (this == null || root == null) return;
        
        FieldInfo[] fields = typeof(PlayerStats).GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            // Szukamy pasków wewnątrz statusContainer
            ProgressBar progressBar = statusContainer.Q<VisualElement>(field.Name)?.Q<ProgressBar>();
      
            if (progressBar != null)
            {
                object value = field.GetValue(stats); // Magia refleksji Microsoftu ;)
                float floatValue = Convert.ToSingle(value);
                progressBar.value = floatValue;
            }
        }
        UpdateBarStyles();
    }

    private void UpdateBarStyles()
    {
        // Szukamy pasków wewnątrz kontenera statusu
        List<ProgressBar> bars = statusContainer.Query<VisualElement>(className: "Bar").Children<ProgressBar>().ToList();

        foreach (ProgressBar progressBar in bars)
        {
            float percentage = (progressBar.value / progressBar.highValue) * 100f;
            VisualElement progressFill = progressBar.Q(className: "unity-progress-bar__progress");

            if (progressFill != null)
            {
                if (percentage > 40f)
                {
                    progressFill.style.unityBackgroundImageTintColor = new StyleColor(Color.green);
                }
                else
                {
                    progressFill.style.unityBackgroundImageTintColor = new StyleColor(Color.red);
                }
            }
        }
    }

 
    public void PauseHide(bool toggle)
    {
        if (!toggle)
        {
            statusContainer.style.display = DisplayStyle.Flex;
            statusContainer.pickingMode = PickingMode.Position; 
        }
        else
        {
            statusContainer.style.display = DisplayStyle.None;
            statusContainer.pickingMode = PickingMode.Ignore;
        }

        toggle = !toggle;
    }

    public void DisplayPlayerStatusOnScreen(DisplayItem3D.Hand hand, bool active)
    {
        if (statusContainer == null)
        {
            Debug.LogError("statusContainer nie jest przypisany!");
            return;
        }

        // Modyfikujemy styl KONTENERA, a nie całego ekranu (roota)
        if (hand == DisplayItem3D.Hand.left)
        {
            statusContainer.style.left = 0;
            statusContainer.style.right = StyleKeyword.Null; 
            statusContainer.style.alignSelf = Align.FlexStart; 
            statusContainer.style.marginRight = new StyleLength(Length.Percent(0f));
        }
        else if (hand == DisplayItem3D.Hand.right)
        {
            statusContainer.style.left = StyleKeyword.Null;
            statusContainer.style.right = 0;
            statusContainer.style.alignSelf = Align.FlexEnd; 
            statusContainer.style.marginRight = new StyleLength(Length.Percent(5f));
        }

        if (active)
        {
            statusContainer.style.display = DisplayStyle.Flex;
            UI_doc.sortingOrder = 10; 
            statusContainer.pickingMode = PickingMode.Position; 
        }
        else
        {
            statusContainer.style.display = DisplayStyle.None;
            ///UI_Logs.Log("Psuje UI");
            UI_doc.sortingOrder = -10; 
            statusContainer.pickingMode = PickingMode.Ignore;
        }
    }
}