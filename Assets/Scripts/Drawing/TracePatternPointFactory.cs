using System.Collections.Generic;
using UnityEngine;

public static class TracePatternPointFactory
{
    private const float StarInnerRadiusRatio = 0.45f;

    public static List<Vector2> Create(
        TracePattern pattern,
        int pointCount,
        Vector2 position,
        float size)
    {
        pointCount = Mathf.Max(16, pointCount);
        size = Mathf.Max(0.01f, size);

        if (pattern == TracePattern.Circle)
        {
            return CreateCircle(pointCount, position, size);
        }

        return SampleVertices(CreateVertices(pattern, position, size), pointCount);
    }

    private static List<Vector2> CreateCircle(int pointCount, Vector2 position, float size)
    {
        var result = new List<Vector2>(pointCount);
        for (var index = 0; index < pointCount; index++)
        {
            var angle = Mathf.PI * 2f * index / pointCount + Mathf.PI * 0.5f;
            result.Add(position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size);
        }
        return result;
    }

    private static Vector2[] CreateVertices(TracePattern pattern, Vector2 position, float size)
    {
        return pattern switch
        {
            TracePattern.UprightTriangle => CreateRegularVertices(3, position, size, 90f),
            TracePattern.InvertedTriangle => CreateRegularVertices(3, position, size, -90f),
            TracePattern.Square => CreateRegularVertices(4, position, size, 45f),
            TracePattern.Diamond => CreateRegularVertices(4, position, size, 90f),
            TracePattern.Hexagon => CreateRegularVertices(6, position, size, 90f),
            TracePattern.Star => CreateStarVertices(position, size),
            _ => CreateRegularVertices(3, position, size, 90f),
        };
    }

    private static Vector2[] CreateRegularVertices(
        int vertexCount,
        Vector2 position,
        float size,
        float startAngleDegrees)
    {
        var vertices = new Vector2[vertexCount];
        for (var index = 0; index < vertexCount; index++)
        {
            var angle = (startAngleDegrees - 360f * index / vertexCount) * Mathf.Deg2Rad;
            vertices[index] = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size;
        }
        return vertices;
    }

    private static Vector2[] CreateStarVertices(Vector2 position, float size)
    {
        const int vertexCount = 10;
        var vertices = new Vector2[vertexCount];
        for (var index = 0; index < vertexCount; index++)
        {
            var radius = index % 2 == 0 ? size : size * StarInnerRadiusRatio;
            var angle = (90f - 360f * index / vertexCount) * Mathf.Deg2Rad;
            vertices[index] = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return vertices;
    }

    private static List<Vector2> SampleVertices(IReadOnlyList<Vector2> vertices, int pointCount)
    {
        var result = new List<Vector2>(pointCount);
        for (var index = 0; index < pointCount; index++)
        {
            var edgePosition = index / (float)pointCount * vertices.Count;
            var edgeIndex = Mathf.FloorToInt(edgePosition);
            var edgeProgress = edgePosition - edgeIndex;
            result.Add(Vector2.Lerp(
                vertices[edgeIndex],
                vertices[(edgeIndex + 1) % vertices.Count],
                edgeProgress));
        }
        return result;
    }
}
