using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
public struct PlayerStats
{
    public float stamina;         
    public float thirst;      
    public float fats;         
    public float protein;      
    public float carbs;         
    public float calories;             
    public float hunger;           
    public float tiredness;     
    public float psyche;
}
public class PlayerStatus : MonoBehaviour
{
    private UIDocument UI_doc => GetComponent<UIDocument>();
    private VisualElement root;
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
        root = UI_doc.rootVisualElement;
        List<ProgressBar> bars= root.Query<VisualElement>(className: "Bar").Children<ProgressBar>().ToList();
        foreach (ProgressBar progressBar in bars)
        {
            
        }
        
    }
    private void ProcessStats(PlayerStats stats)
    {
        if (this == null) return;
        if (root == null) return;
        FieldInfo[] fields = typeof(PlayerStats).GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {

            ProgressBar progressBar = root.Q<VisualElement>(field.Name).Q<ProgressBar>();;
      
            if (progressBar != null)
            {
                object value = field.GetValue(stats);//zjebane badziewie mincrosoftu wyciaga badziewie 
                float floatValue = Convert.ToSingle(value);//wyciagamy z badziewia wartosc xd
                progressBar.value = floatValue;
               // UI_Logs.Log(field.Name +":"+ floatValue);

            }
        }
        UpdateBarStyles();
    }

        
    

    private void UpdateBarStyles()
    {
        List<ProgressBar> bars = root.Query<VisualElement>(className: "Bar").Children<ProgressBar>().ToList();

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


}
