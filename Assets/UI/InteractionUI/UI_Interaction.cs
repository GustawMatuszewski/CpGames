using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
[Serializable]
public struct ActionIconMapping
{
    public Door.DoorActionType type;
    public Sprite icon;
}

public class UI_Interaction : MonoBehaviour
{
    private UIDocument UI_doc => GetComponent<UIDocument>();
    public static UI_Interaction Instance;
    [Header("Icon Settings")]
    public List<ActionIconMapping> iconMappings;
    private Dictionary<Door.DoorActionType, Sprite> _iconCache;
    private Rondo _rondoElement;
    private VisualElement rondoParent;
    private VisualElement _root; // Dodajemy referencję do roota
    private int _lastIndex = -1; // Śledzimy poprzedni wybór
    private int index;
    private Label actionLabel;
    private void OnEnable()
    {
        if (UI_doc == null) return;

        _root = UI_doc.rootVisualElement;
        rondoParent=_root?.Q<VisualElement>("RondoBox");
        _rondoElement = rondoParent?.Q<Rondo>();
         actionLabel  = rondoParent.Q<Label>();
        
        if (_root != null && _rondoElement != null)
        {
            // WAŻNE: Rejestrujemy ruch myszy na całym ekranie (Root)
            _root.RegisterCallback<PointerMoveEvent>(OnPointerMoveOnRoot);
        }
    }

    private void OnDisable()
    {
        if (_root != null)
        {
            _root.UnregisterCallback<PointerMoveEvent>(OnPointerMoveOnRoot);
        }
    }

    private void OnPointerMoveOnRoot(PointerMoveEvent evt)
    {
        if (_rondoElement == null) return;

        // Przeliczamy pozycję na lokalną względem Ronda[cite: 3]
        Vector2 localPos = _rondoElement.WorldToLocal(evt.position);
        index = _rondoElement.GetSelectedIndex(localPos);

        // WYWOŁANIE: Musi być poza jakimkolwiek if (index != -1). 
        // Jeśli index to -1, SetSelectedSegment przywróci normalny wygląd.
        _rondoElement.SetSelectedSegment(index, 1.25f);
        if (index < 0 || _rondoElement == null) 
            actionLabel.text="";
        else
        {
            VisualElement selectedElement = _rondoElement.ElementAt(index);
            DoorAction data =selectedElement.userData as DoorAction;
            actionLabel.text =  data.label.ToString();
        }
    }

    private void Awake()
    {
        Instance = this;
        _iconCache = new Dictionary<Door.DoorActionType, Sprite>();
        foreach (var mapping in iconMappings)
        {
            _iconCache[mapping.type] = mapping.icon;
        }
        
        
    }

    public void SendRondoActions(List<DoorAction> actions)
    {
        _rondoElement.style.display = DisplayStyle.Flex;
        actionLabel.style.display = DisplayStyle.Flex;
        _rondoElement.Clear();

        foreach (var action in actions)
        {
            VisualElement temp = new VisualElement();
            temp.name = action.label;
            temp.userData = action;

            // DODAWANIE IKONY
            if (_iconCache.TryGetValue(action.type, out Sprite iconSprite))
            {
                VisualElement iconElement = new VisualElement();
                iconElement.style.backgroundImage = new StyleBackground(iconSprite);
                iconElement.AddToClassList("rondo-icon"); // Stylizuj rozmiar w USS
                temp.Add(iconElement);
            }
            
            _rondoElement.Add(temp);
        }
    
        _rondoElement.UpdateLayout();
        _rondoElement.MarkDirtyRepaint();
    }
    public DoorAction SelectRondoAction()
    {
        _rondoElement.style.display = DisplayStyle.None;
        actionLabel.style.display = DisplayStyle.None;
        if (index < 0 || _rondoElement == null) return null;
        VisualElement selectedElement = _rondoElement.ElementAt(index);
        return selectedElement?.userData as DoorAction;
       
    }
}