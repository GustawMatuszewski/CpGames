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
    private VisualElement _root; // Dodajemy referencję do roota
    private int _lastIndex = -1; // Śledzimy poprzedni wybór
    private int index;
    private void OnEnable()
    {
        if (UI_doc == null) return;

        _root = UI_doc.rootVisualElement;
        _rondoElement = _root?.Q<Rondo>();

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
    
        _rondoElement.MarkDirtyRepaint();
    }
    public DoorAction SelectRondoAction()
    {
        _rondoElement.style.display = DisplayStyle.None;
        if (index < 0 || _rondoElement == null) return null;
        VisualElement selectedElement = _rondoElement.ElementAt(index);
        return selectedElement?.userData as DoorAction;
       
    }
}