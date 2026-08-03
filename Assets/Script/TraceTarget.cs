using System.Collections.Generic;
using UnityEngine;

public enum TracePattern
{
    SShape,
    Circle,
}

[RequireComponent(typeof(LineRenderer))]
public sealed class TraceTarget : MonoBehaviour
{
    [SerializeField] private TracePattern pattern = TracePattern.SShape;
    [SerializeField, Min(16)] private int pointCount = 128;
    [SerializeField, Min(0.05f)] private float toleranceRadius = 0.42f;
    [SerializeField] private Color lineColor = new(0.58f, 0.62f, 0.66f, 0.85f);

    private readonly List<Vector2> points = new();
    private LineRenderer targetRenderer;

    public IReadOnlyList<Vector2> Points => points;
    public bool IsClosed => pattern == TracePattern.Circle;
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

    public static List<Vector2> CreatePatternPoints(TracePattern targetPattern, int count)
    {
        count = Mathf.Max(16, count);
        var result = new List<Vector2>(count);

        for (var index = 0; index < count; index++)
        {
            var t = targetPattern == TracePattern.Circle
                ? index / (float)count
                : index / (float)(count - 1);

            if (targetPattern == TracePattern.Circle)
            {
                var angle = Mathf.PI * 2f * t + Mathf.PI * 0.5f;
                result.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2.05f);
            }
            else
            {
                var x = 1.55f * Mathf.Sin(Mathf.PI * 2f * t);
                var y = Mathf.Lerp(2.45f, -2.45f, t);
                result.Add(new Vector2(x, y));
            }
        }

        return result;
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
