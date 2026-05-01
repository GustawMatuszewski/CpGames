using UnityEngine;
using UnityEngine.UIElements;

public class UI_GeneralSettings : MonoBehaviour
{
    private UIDocument UI_doc => GetComponent<UIDocument>();
    void Awake()
    {

        VisualElement root = UI_doc.rootVisualElement;
        root= root.Q<VisualElement>("GeneralSettings");//poprostu ogrania tylko i wylacznie video settings nic wiecej
        
    }


}
