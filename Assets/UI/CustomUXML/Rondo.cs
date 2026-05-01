using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

[UxmlElement]
public partial class Rondo : VisualElement
{
    [UxmlAttribute("inner-radius-ratio")]
    private float _innerRadiusRatio = 0.6f;

    [UxmlAttribute("segment-border-width")]
    private float _segmentBorderWidth = 2f;

    [UxmlAttribute("segment-border-color")]
    private Color _segmentBorderColor = Color.black;

    private int hoverIndex = -1;
    // Wewnątrz klasy Rondo dodaj:
    private List<float> _segmentAnimationWeights = new List<float>();
    private const int AnimTimeMs = 200; // Czas trwania animacji w milisekundach
    public float InnerRadiusRatio 
    { 
        get => _innerRadiusRatio; 
        set { _innerRadiusRatio = Mathf.Clamp01(value); MarkDirtyRepaint(); UpdateLayout(); } 
    }

    public float SegmentBorderWidth 
    { 
        get => _segmentBorderWidth; 
        set { _segmentBorderWidth = Mathf.Max(0, value); MarkDirtyRepaint(); } 
    }

    public Color SegmentBorderColor 
    { 
        get => _segmentBorderColor; 
        set { _segmentBorderColor = value; MarkDirtyRepaint(); } 
    }

    public Rondo()
    {
        generateVisualContent += OnGenerateVisualContent;
        RegisterCallback<GeometryChangedEvent>(evt => UpdateLayout());
        RegisterCallback<AttachToPanelEvent>(evt => UpdateLayout());

        // WYMUSZENIE RESETU: Gdy mysz opuści cały obszar Ronda
        RegisterCallback<PointerLeaveEvent>(evt => {
            SetSelectedSegment(-1, 1f); 
        });
    }

    public void SetSelectedSegment(int index, float scaleAmount)
    {
        if (hoverIndex == index) return;
        hoverIndex = index;

        var children = GetVisibleChildren();
        while (_segmentAnimationWeights.Count < children.Count) 
            _segmentAnimationWeights.Add(0f);

        for (int i = 0; i < children.Count; i++)
        {
            int currentIndex = i;
            float targetWeight = (i == index) ? 1f : 0f;
            float startWeight = _segmentAnimationWeights[i];

            // LOGIKA SKALI: Tylko sąsiedzi
            float targetIconScale = 1.0f;
            if (index != -1) {
                bool isNeighbor = (i == (index + 1) % children.Count) || 
                                  (i == (index - 1 + children.Count) % children.Count);
            
                if (i == index) targetIconScale = 1.0f; // Wybrany zostaje normalny
                else if (isNeighbor) targetIconScale = 0.5f; // Sąsiedzi maleją[cite: 5]
                else targetIconScale = 1.0f; // Reszta bez zmian
            }

            // Animacja skali ikon
            children[i].experimental.animation.Scale(targetIconScale, AnimTimeMs);

            // Animacja tła i pozycji
            this.experimental.animation.Start(startWeight, targetWeight, AnimTimeMs, (element, val) => {
                _segmentAnimationWeights[currentIndex] = val;
            
                // KLUCZOWE: W każdej klatce animacji przeliczamy układ, 
                // aby ikony płynnie reagowały na zmiany wag
                UpdateIconLayout();
                MarkDirtyRepaint();
            });
        }
    }
    private void UpdateIconLayout()
    {
        var children = GetVisibleChildren();
        int count = children.Count;
        if (count == 0 || contentRect.width <= 0) return;

        // Upewniamy się, że wagi są gotowe
        while (_segmentAnimationWeights.Count < count) _segmentAnimationWeights.Add(0f);

        float radius = Mathf.Min(contentRect.width, contentRect.height) / 2;
        float placementRadius = radius * (1 + InnerRadiusRatio) / 2;
        Vector2 center = contentRect.size / 2;
        float angleStepRad = (2.0f * Mathf.PI) / count;

        for (int i = 0; i < count; i++)
        {
            var child = children[i];
    
            // Pobieramy wagę animacji lewego i prawego sąsiada
            float w_prev = _segmentAnimationWeights[(i - 1 + count) % count];
            float w_next = _segmentAnimationWeights[(i + 1) % count];

            // WAŻNE: maxOffsetDegrees MUSI być takie samo jak w DrawSegment (tutaj 20 stopni)
            float maxOffsetDegrees = 20f; 

            // Różnica wag sąsiadów wpływa na przesunięcie wizualnego środka tego elementu
            float shiftDegrees = (w_prev * maxOffsetDegrees - w_next * maxOffsetDegrees) / 2f;
            float shiftRadians = shiftDegrees * Mathf.Deg2Rad;

            // OBLICZANIE NOWEGO ŚRODKA: Podstawowy kąt + przesunięcie wymuszone przez sąsiada
            float midAngle = (i * angleStepRad) + (angleStepRad / 2f) - (Mathf.PI / 2f) + shiftRadians;
    
            float targetX = center.x + Mathf.Cos(midAngle) * placementRadius;
            float targetY = center.y + Mathf.Sin(midAngle) * placementRadius;

            child.style.position = Position.Absolute;
            child.style.left = targetX;
            child.style.top = targetY;
        
            // Zapewniamy wyśrodkowanie samej ikonki (pivot na środek grafiki)
            child.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
        }
    }
    void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        var painter = mgc.painter2D;
        var children = GetVisibleChildren();
        int count = children.Count;
        if (count == 0) return;

        // Upewniamy się, że mamy wagi dla wszystkich
        while (_segmentAnimationWeights.Count < count) _segmentAnimationWeights.Add(0f);

        float maxRadius = Mathf.Min(contentRect.width, contentRect.height) / 2;
        Vector2 center = contentRect.size / 2;
        float angleStep = 360f / count;

        // Rysujemy najpierw te, które mają mniejszą wagę (są w tle)
        // Aby to zrobić idealnie, można posortować indeksy po wadze, 
        // ale zazwyczaj wystarczy narysować wszystko prócz hoverIndex, a on na końcu.
        for (int i = 0; i < count; i++)
        {
            if (i == hoverIndex) continue;
            DrawSegment(painter, center, i, angleStep, maxRadius, children[i].resolvedStyle.backgroundColor, _segmentAnimationWeights[i]);
        }

        if (hoverIndex >= 0 && hoverIndex < count)
        {
            DrawSegment(painter, center, hoverIndex, angleStep, maxRadius, children[hoverIndex].resolvedStyle.backgroundColor, _segmentAnimationWeights[hoverIndex]);
        }
    }

    // Zmieniamy sygnaturę metody na przyjmowanie wagi
    private void DrawSegment(Painter2D painter, Vector2 center, int i, float angleStep, float maxRadius, Color fillColor, float weight)
    {
        float currentOuter = maxRadius;
        float currentInner = (maxRadius * InnerRadiusRatio);
    
        // PŁYNNE POWIĘKSZANIE WZDŁUŻ: Interpolacja od 0 do 10 stopni
        float offsetAngle = Mathf.Lerp(0f, 20f, weight); 
    
        float startAngle = (i * angleStep - 90f) - offsetAngle; 
        float endAngle = ((i + 1) * angleStep - 90f) + offsetAngle;

        painter.BeginPath();
        painter.Arc(center, currentOuter, startAngle, endAngle, ArcDirection.Clockwise);
        painter.Arc(center, currentInner, endAngle, startAngle, ArcDirection.CounterClockwise);
        painter.ClosePath();

        if (fillColor.a == 0) fillColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    
        // Opcjonalnie: możemy też animować kolor (rozjaśnianie wybranego)
        painter.fillColor = Color.Lerp(fillColor, fillColor * 1.2f, weight);
        painter.Fill();

        if (SegmentBorderWidth > 0)
        {
            painter.lineWidth = Mathf.Lerp(SegmentBorderWidth, SegmentBorderWidth + 1f, weight);
            painter.strokeColor = SegmentBorderColor;
            painter.Stroke();
        }
    }

    // ... Reszta metod UpdateLayout, GetVisibleChildren, GetSelectedIndex (bez zmian względem poprzedniego poprawnego kodu) ...
    public void UpdateLayout() { /* Tak jak w Twoim pliku */
        var children = GetVisibleChildren();
        int count = children.Count;
        if (count == 0 || contentRect.width <= 0) return;
        float radius = Mathf.Min(contentRect.width, contentRect.height) / 2;
        float placementRadius = radius * (1+InnerRadiusRatio) / 2;
        Vector2 center = contentRect.size / 2;
        float angleStep = (2.0f * Mathf.PI) / count;
        for (int i = 0; i < count; i++) {
            var child = children[i];
            float midAngle = (i * angleStep) + (angleStep / 2f) - (Mathf.PI / 2f);
            float targetX = center.x + Mathf.Cos(midAngle) * placementRadius;
            float targetY = center.y + Mathf.Sin(midAngle) * placementRadius;
            child.style.position = Position.Absolute;
            child.style.left = targetX;
            child.style.top = targetY;
            child.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
        }
        MarkDirtyRepaint();
        UpdateIconLayout();
    }
    private List<VisualElement> GetVisibleChildren() {
        List<VisualElement> visible = new List<VisualElement>();
        foreach (var c in hierarchy.Children()) if (c.visible) visible.Add(c);
        return visible;
    }
    public int GetSelectedIndex(Vector2 localMousePosition) {
        var children = GetVisibleChildren();
        int count = children.Count;
        if (count == 0) return -1;
        Vector2 center = contentRect.size / 2;
        Vector2 direction = localMousePosition - center;
        float distance = direction.magnitude;
        float maxRadius = Mathf.Min(contentRect.width, contentRect.height) / 2;
        float minRadius = maxRadius * InnerRadiusRatio;
        if (distance < minRadius) return -1;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        if (angle < 0) angle += 360f;
        float angleStep = 360f / count;
        int index = Mathf.FloorToInt(angle / angleStep);
        return Mathf.Clamp(index, 0, count - 1);
    }
}