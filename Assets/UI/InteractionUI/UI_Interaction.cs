using UnityEngine;
using UnityEngine.UIElements;

public class UI_Interaction : MonoBehaviour
{
    public UIDocument UI_doc;
    private Rondo _rondoElement;
    private VisualElement _root; // Dodajemy referencję do roota
    private int _lastIndex = -1; // Śledzimy poprzedni wybór

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
        int index = _rondoElement.GetSelectedIndex(localPos);

        // WYWOŁANIE: Musi być poza jakimkolwiek if (index != -1). 
        // Jeśli index to -1, SetSelectedSegment przywróci normalny wygląd.[cite: 1, 2]
        _rondoElement.SetSelectedSegment(index, 1.25f);
    }
    
}