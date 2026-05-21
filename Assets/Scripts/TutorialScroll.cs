using UnityEngine;
using UnityEngine.UI;

public class TutorialScroll : MonoBehaviour
{
    [Header("Ustawienia")]
    public ScrollRect scrollRect;
    public float scrollSpeed = 0.05f; // Szybkość przewijania
    public float waitAtEnd = 2f;      // Ile sekund poczekać na dole przed wyłączeniem

    private float timer;

    void Start()
    {
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f; // Wymuś start na samej górze
            
        timer = waitAtEnd;
    }

    void Update()
    {
        if (scrollRect == null) return;

        // Przewijanie w dół (1 to góra, 0 to dół)
        if (scrollRect.verticalNormalizedPosition > 0f)
        {
            scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;
        }
        else
        {
            // Osiągnięto dół, odliczamy do wyłączenia
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                CloseTutorial();
            }
        }
    }

    // Możesz to też podpiąć pod przycisk "Pomiń" na ekranie
    public void CloseTutorial()
    {
        gameObject.SetActive(false);
    }
}