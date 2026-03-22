using UnityEngine;
using UnityEngine.UIElements;

public class GradientElement : VisualElement
{
    public Color startColor = Color.white;
    public Color endColor = Color.black;

    public GradientElement()
    {
        generateVisualContent += DrawGradient;
    }

    void DrawGradient(MeshGenerationContext ctx)
    {
        var rect = contentRect;
        var mesh = ctx.Allocate(4, 6);

        // bottom-left
        mesh.SetNextVertex(new Vertex()
        {
            position = new Vector3(rect.xMin, rect.yMin, Vertex.nearZ),
            tint = startColor
        });

        // bottom-right
        mesh.SetNextVertex(new Vertex()
        {
            position = new Vector3(rect.xMax, rect.yMin, Vertex.nearZ),
            tint = Color.Lerp(startColor, endColor, 0.5f)
        });

        // top-right
        mesh.SetNextVertex(new Vertex()
        {
            position = new Vector3(rect.xMax, rect.yMax, Vertex.nearZ),
            tint = endColor
        });

        // top-left
        mesh.SetNextVertex(new Vertex()
        {
            position = new Vector3(rect.xMin, rect.yMax, Vertex.nearZ),
            tint = Color.Lerp(startColor, endColor, 0.5f)
        });

        mesh.SetNextIndex(0);
        mesh.SetNextIndex(1);
        mesh.SetNextIndex(2);

        mesh.SetNextIndex(2);
        mesh.SetNextIndex(3);
        mesh.SetNextIndex(0);
    }
}