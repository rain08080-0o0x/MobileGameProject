using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TraceMagicCircleRenderer : MonoBehaviour
{
    [SerializeField] private TraceMagicCircleDefinition definition;
    [SerializeField, Min(16)] private int pointCount = 128;
    [SerializeField, Min(0.01f)] private float lineWidth = 0.12f;
    [SerializeField] private Color lineColor = Color.white;
    [SerializeField] private int sortingOrder;

    private readonly List<GameObject> renderedShapes = new();
    private Material lineMaterial;

    public TraceMagicCircleDefinition Definition => definition;

    private void Awake()
    {
        Refresh();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            Refresh();
        }
    }

    private void OnDestroy()
    {
        ClearRenderedShapes();
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }

    public void SetDefinition(TraceMagicCircleDefinition newDefinition)
    {
        definition = newDefinition;
        Refresh();
    }

    public void Refresh()
    {
        ClearRenderedShapes();
        if (definition == null)
        {
            return;
        }

        lineMaterial ??= new Material(Shader.Find("Sprites/Default"))
        {
            name = "Runtime Magic Circle Material",
        };

        for (var index = 0; index < definition.Shapes.Count; index++)
        {
            var shape = definition.Shapes[index];
            if (shape == null || !shape.IsDisplayed)
            {
                continue;
            }

            CreateShapeRenderer(shape, index);
        }
    }

    private void CreateShapeRenderer(TraceMagicCircleShape shape, int index)
    {
        var shapeObject = new GameObject($"Magic Circle Shape {index + 1}");
        shapeObject.transform.SetParent(transform, false);
        renderedShapes.Add(shapeObject);

        var renderer = shapeObject.AddComponent<LineRenderer>();
        renderer.useWorldSpace = false;
        renderer.loop = TracePatternPointFactory.IsClosed(shape.Pattern);
        renderer.widthMultiplier = lineWidth;
        renderer.startColor = lineColor;
        renderer.endColor = lineColor;
        renderer.numCapVertices = 8;
        renderer.numCornerVertices = 8;
        renderer.sortingOrder = sortingOrder;
        renderer.sharedMaterial = lineMaterial;

        var points = TracePatternPointFactory.Create(
            shape.Pattern,
            pointCount,
            shape.Position,
            shape.Size,
            shape.RectangleAspectRatio,
            shape.LineStart,
            shape.LineEnd);
        renderer.positionCount = points.Count;
        for (var pointIndex = 0; pointIndex < points.Count; pointIndex++)
        {
            renderer.SetPosition(pointIndex, new Vector3(points[pointIndex].x, points[pointIndex].y, 0f));
        }
    }

    private void ClearRenderedShapes()
    {
        for (var index = 0; index < renderedShapes.Count; index++)
        {
            if (renderedShapes[index] != null)
            {
                Destroy(renderedShapes[index]);
            }
        }
        renderedShapes.Clear();
    }
}
