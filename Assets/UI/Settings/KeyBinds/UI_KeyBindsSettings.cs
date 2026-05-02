using UnityEngine;
using UnityEngine.UIElements;

public class UI_KeyBindsSettings : MonoBehaviour
{

  
    private UIDocument UI_doc => GetComponent<UIDocument>();
  
    void Awake()
    {

        VisualElement root = UI_doc.rootVisualElement;
        root= root.Q<VisualElement>("KeyBindsSettings");
      
    }
    
    
    
    
    
    

}
