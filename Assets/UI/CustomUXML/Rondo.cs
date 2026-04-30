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
        // Nawet jeśli index jest taki sam, pozwalamy przejść dalej, 
        // jeśli chcemy mieć pewność odświeżenia, ale lepiej sprawdzić:
        if (hoverIndex == index) return; 

        hoverIndex = index;
        var children = GetVisibleChildren();

        for (int i = 0; i < children.Count; i++)
        {
            // 1. Resetujemy skale wszystkich dzieci
            float s = (i == index) ? scaleAmount : 1f;
            children[i].style.scale = new StyleScale(new Scale(new Vector2(s, s)));
        
            if (i == index) children[i].BringToFront();
        }

        // 2. KLUCZOWE: Informujemy system, że geometria Painter2D (promienie) musi zostać przeliczona
        MarkDirtyRepaint();
    }

    void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        var painter = mgc.painter2D;
        var children = GetVisibleChildren();
        int count = children.Count;
        if (count == 0) return;

        float maxRadius = Mathf.Min(contentRect.width, contentRect.height) / 2;
        Vector2 center = contentRect.size / 2;
        float angleStep = 360f / count;

        // KROK 1: Rysujemy tła zwykłych segmentów
        for (int i = 0; i < count; i++)
        {
            if (i == hoverIndex) continue;
            DrawSegment(painter, center, i, angleStep, maxRadius, children[i].resolvedStyle.backgroundColor, false);
        }

        // KROK 2: Rysujemy zaznaczony segment na górze
        if (hoverIndex >= 0 && hoverIndex < count)
        {
            DrawSegment(painter, center, hoverIndex, angleStep, maxRadius, children[hoverIndex].resolvedStyle.backgroundColor, true);
        }
    }

    private void DrawSegment(Painter2D painter, Vector2 center, int i, float angleStep, float maxRadius, Color fillColor, bool isHovered)
    {
        // 1. Zmiana promienia (w głąb i na zewnątrz)
       // float currentOuter = isHovered ? maxRadius + 12f : maxRadius;
        //float currentInner = isHovered ? (maxRadius * InnerRadiusRatio) - 6f : (maxRadius * InnerRadiusRatio);
        float currentOuter = maxRadius;
        float currentInner = (maxRadius * InnerRadiusRatio);
        // 2. POWIĘKSZANIE WZDŁUŻ 
        float offsetAngle = isHovered ? 10 : 0f; 
        float startAngle = (i * angleStep - 90f) - offsetAngle; 
        float endAngle = ((i + 1) * angleStep - 90f) + offsetAngle;

        painter.BeginPath();
        painter.Arc(center, currentOuter, startAngle, endAngle, ArcDirection.Clockwise);
        painter.Arc(center, currentInner, endAngle, startAngle, ArcDirection.CounterClockwise);
        painter.ClosePath();

        if (fillColor.a == 0) fillColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        painter.fillColor = fillColor;
        painter.Fill();

        if (SegmentBorderWidth > 0 && SegmentBorderColor.a > 0)
        {
            painter.lineWidth = isHovered ? SegmentBorderWidth + 1f : SegmentBorderWidth;
            painter.strokeColor = SegmentBorderColor;
            painter.Stroke(); // Border rysuje się na wierzchu wypełnienia[cite: 1]
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