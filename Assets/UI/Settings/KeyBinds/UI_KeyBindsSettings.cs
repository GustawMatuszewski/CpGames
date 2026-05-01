using UnityEngine;
using UnityEngine.UIElements;

public class UI_KeyBindsSettings : MonoBehaviour
{

    [SerializeField] private Material ghostMaterial;
    private UIDocument UI_doc => GetComponent<UIDocument>();
  
    void Awake()
    {

        VisualElement root = UI_doc.rootVisualElement;
        root= root.Q<VisualElement>("KeyBindsSettings");//poprostu ogrania tylko i wylacznie video settings nic wiecej
      
    }
    
    
    
    
    
    

}
