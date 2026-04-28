using UnityEngine;

public class UI_Logs_Controller : MonoBehaviour
{
    [SerializeField] private bool showLogs = true;

    void OnValidate()
    {
        
        UI_Logs.UIDebugMode = showLogs;
    }

    void Awake()
    {
        
        UI_Logs.UIDebugMode = showLogs;
    }
}