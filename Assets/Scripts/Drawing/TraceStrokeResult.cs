using UnityEngine;

public readonly struct TraceStrokeResult
{
    public TraceStrokeResult(Vector2[] points, float accuracy)
        : this(points, accuracy, 0f)
    {
    }

    public TraceStrokeResult(Vector2[] points, float accuracy, float drawingSeconds)
    {
        Points = points;
        Accuracy = accuracy;
        DrawingSeconds = drawingSeconds;
    }

    public Vector2[] Points { get; }
    public float Accuracy { get; }
    public float DrawingSeconds { get; }
}
