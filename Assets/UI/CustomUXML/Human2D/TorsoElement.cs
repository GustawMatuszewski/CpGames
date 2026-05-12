using UnityEngine;
using UnityEngine.UIElements;
[UxmlElement]
public partial class TorsoElement : VisualElement
{
    [UxmlAttribute("fill-color")]
    private Color fillColor { get; set; } = Color.white;

    public  TorsoElement()
    {
        // Rejestrujemy callback rysowania
        generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        var painter = mgc.painter2D;
        painter.BeginPath();
        painter.fillColor = fillColor;

        // 1. START: Lewy bark (Początek)
        painter.MoveTo(new Vector2(20, 20));

        // 2. GÓRA (Lekki łuk barków)
        painter.BezierCurveTo(new Vector2(40, 5), new Vector2(60, 5), new Vector2(80, 20));

        // 3. PRAWY BOK (Wypukły)
        // Aby linia była wypukła, punkty kontrolne X muszą być WIĘKSZE niż punkty na końcach.
        // Skoro bark jest na x=80, a biodro na x=85, dajemy uchwyty na x=90 lub x=95.
        painter.BezierCurveTo(
            new Vector2(92, 40), // Uchwyt 1: Wyciąga linię mocno w prawo
            new Vector2(95, 60), // Uchwyt 2: Utrzymuje wypukłość nad biodrem
            new Vector2(85, 185)  // KONIEC: Prawe biodro
        );

        // 4. DÓŁ (Pachwiny - kształt V ze szkicu)
        painter.BezierCurveTo(new Vector2(65, 215), new Vector2(35, 215), new Vector2(15, 185));

        // 5. LEWY BOK (Wypukły - odbicie lustrzane)
        // Tu punkty kontrolne X muszą być MNIEJSZE niż punkty końcowe (bardziej w lewo).
        // Skoro biodro jest na x=15, a bark na x=20, dajemy uchwyty na x=5 lub x=8.
        painter.BezierCurveTo(
            new Vector2(5, 60),  // Uchwyt 1: Wyciąga linię w lewo
            new Vector2(8, 40),  // Uchwyt 2: Podtrzymuje łuk przy barku
            new Vector2(20, 20)  // POWRÓT: Lewy bark
        );

        painter.Fill();
    
        // Obrys (Stroke) pomaga zobaczyć, czy krzywa idzie tak, jak chcesz
        painter.strokeColor = Color.black;
        painter.lineWidth = 1.5f;
        painter.Stroke();

        painter.ClosePath();
    }
}
