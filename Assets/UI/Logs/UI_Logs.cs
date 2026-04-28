using UnityEngine;

public static class UI_Logs
{
    public static bool UIDebugMode = true;

    // --- STANDARDOWY LOG ---
    public static void Log<T>(T message)
    {
        if (!UIDebugMode) return;
        ExecuteLog(message, "white", "Log", LogType.Log);
    }

    // --- OSTRZEŻENIE (Warning) ---
    public static void Warning<T>(T message)
    {
        if (!UIDebugMode) return;
        ExecuteLog(message, "orange", "Warning", LogType.Warning);
    }

    // --- BŁĄD (Error) ---
    public static void Error<T>(T message)
    {
        if (!UIDebugMode) return;
        ExecuteLog(message, "red", "Error", LogType.Error);
    }

    // Prywatna metoda pomocnicza, żeby nie powtarzać kodu (DRY - Don't Repeat Yourself)
    private static void ExecuteLog<T>(T message, string color, string label, LogType type)
    {
        if (message == null)
        {
            Debug.LogError($"<color=red><b>[{label}]</b> Message is NULL</color>");
            return;
        }

        string prefix = $"<color={color}><b>[{label}]</b></color>";

        // Logowanie obiektów Unity
        if (message is Object unityObject && unityObject != null)
        {
            string msg = $"{prefix}: {unityObject.name}";
            SendToUnityConsole(msg, type, unityObject);
            return;
        }

        // Logowanie danych JSON lub tekstu
        string json = JsonUtility.ToJson(message, true);
        if (!string.IsNullOrEmpty(json) && json != "{}")
        {
            SendToUnityConsole($"{prefix} (Data):\n{json}", type);
        }
        else
        {
            SendToUnityConsole($"{prefix}: {message}", type);
        }
    }

    private static void SendToUnityConsole(string message, LogType type, Object context = null)
    {
        switch (type)
        {
            case LogType.Log: Debug.Log(message, context); break;
            case LogType.Warning: Debug.LogWarning(message, context); break;
            case LogType.Error: Debug.LogError(message, context); break;
        }
    }
}