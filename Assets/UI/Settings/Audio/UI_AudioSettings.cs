using UnityEngine;
using UnityEngine.UIElements;


    public class UI_AudioSettings : MonoBehaviour
    {

        private UIDocument UI_doc => GetComponent<MenuScript>().UI_doc;

        void Awake()
        {

            VisualElement root = UI_doc.rootVisualElement;
            root = root.Q<VisualElement>(
                "AudioSettings"); //poprostu ogrania tylko i wylacznie video settings nic wiecej

        }

    }
