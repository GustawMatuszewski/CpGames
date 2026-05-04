using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class UI_KeyBindsSettings : MonoBehaviour
{

    public InputActionAsset inputActions;
    private UIDocument UI_doc => GetComponent<UIDocument>();
  
    void Awake()
    {

        VisualElement root = UI_doc.rootVisualElement;
        root= root.Q<VisualElement>("KeyBindsSettings");
      
    }
    
    
    
    
    
    

}
