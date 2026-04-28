using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class UI_VideoSettings : MonoBehaviour
{

    private UIDocument UI_doc => GetComponent<MenuScript>().UI_doc;
    void Awake()
    {

        VisualElement root = UI_doc.rootVisualElement;
        root= root.Q<VisualElement>("VideoSettings");//poprostu ogrania tylko i wylacznie video settings nic wiecej
        
    }

}
