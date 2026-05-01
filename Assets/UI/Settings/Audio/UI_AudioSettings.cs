using System;
using UnityEngine;
using UnityEngine.UIElements;


    public class UI_AudioSettings : MonoBehaviour
    {

        private UIDocument UI_doc => GetComponent<UIDocument>();
        private Slider MasterVolume;
        public float audio;
        private void Awake()
        {

            VisualElement root = UI_doc.rootVisualElement;
            root = root.Q<VisualElement>("AudioSettings"); //poprostu ogrania tylko i wylacznie video settings nic wiecej
             MasterVolume = root.Q<Slider>("MasterVolume");
             MasterVolume.highValue = 1;
             MasterVolume.RegisterValueChangedCallback(evt =>
             {
                UpdateMasterVolume();
             });
        }

        private void Start()
        {
            if(!PlayerPrefs.HasKey("MasterVolume"))
            {
                PlayerPrefs.SetFloat("MasterVolume",1);
                Load();
            }
            else
            {
                Load();
            }
        }

        public void UpdateMasterVolume()
        {
            AudioListener.volume = MasterVolume.value;
            UI_Logs.Log("Master Volume: " + MasterVolume.value);
            Save();
        }

        private void Save()
        {
            PlayerPrefs.SetFloat("MasterVolume", MasterVolume.value);
        }
 
        private void Load()
        {
            MasterVolume.value = PlayerPrefs.GetFloat("MasterVolume");
            UpdateMasterVolume();
            audio = PlayerPrefs.GetFloat("MasterVolume");
        }
    }
