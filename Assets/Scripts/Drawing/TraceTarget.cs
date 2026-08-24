using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class TraceTarget : MonoBehaviour
{
    [SerializeField] private TracePattern pattern = TracePattern.UprightTriangle;
    [SerializeField, Min(16)] private int pointCount = 128;
    [SerializeField, Min(0.05f)] private float toleranceRadius = 0.42f;
    [SerializeField] private Color lineColor = new(0.58f, 0.62f, 0.66f, 0.85f);

    private readonly List<Vector2> points = new();
    private LineRenderer targetRenderer;

    public IReadOnlyList<Vector2> Points => points;
    public TracePattern Pattern => pattern;
    public bool IsClosed => true;
    public float ToleranceRadius => toleranceRadius;

    private void Awake()
    {
        Refresh();
    }

    public void SetPattern(TracePattern newPattern)
    {
        pattern = newPattern;
        Refresh();
    }

    public void SetVisible(bool visible)
    {
        targetRenderer ??= GetComponent<LineRenderer>();
        targetRenderer.enabled = visible;
    }

    public static List<Vector2> CreatePatternPoints(TracePattern targetPattern, int count)
    {
        return TracePatternPointFactory.Create(targetPattern, count, Vector2.zero, 2.05f);
    }

    private void Refresh()
    {
        targetRenderer ??= GetComponent<LineRenderer>();
        ConfigureRenderer();

        points.Clear();
        points.AddRange(CreatePatternPoints(pattern, pointCount));

        targetRenderer.loop = IsClosed;
        targetRenderer.positionCount = points.Count;
        for (var index = 0; index < points.Count; index++)
        {
            targetRenderer.SetPosition(index, new Vector3(points[index].x, points[index].y, 0f));
        }
    }

    private void ConfigureRenderer()
    {
        targetRenderer.useWorldSpace = true;
        targetRenderer.widthMultiplier = toleranceRadius * 2f;
        targetRenderer.startColor = lineColor;
        targetRenderer.endColor = lineColor;
        targetRenderer.numCapVertices = 12;
        targetRenderer.numCornerVertices = 8;
        targetRenderer.sortingOrder = 0;

        if (targetRenderer.sharedMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            targetRenderer.sharedMaterial = new Material(shader)
            {
                name = "Runtime Trace Target Material",
            };
        }
    }
}
