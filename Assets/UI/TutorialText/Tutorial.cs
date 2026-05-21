using System;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

public class Tutorial : MonoBehaviour
{
    private UIDocument UI_doc => GetComponent<UIDocument>();
    private VisualElement root;

    private void Awake()
    {
        root = UI_doc.rootVisualElement;
        Button closeButton = root.Q<Button>("Button");
        
        Time.timeScale = 0f;                   

        closeButton.clicked += () => 
        {
            Time.timeScale = 1f;                     
            Cursor.lockState = CursorLockMode.Locked;   
            Cursor.visible = false;                  
            
            Destroy(gameObject); 
        };
    }
    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }
        }
    }
}