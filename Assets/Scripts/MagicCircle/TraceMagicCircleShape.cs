using System;
using UnityEngine;

public enum TraceMagicCircleShapeUsage
{
    Display,
    StoredOnly,
}

[Serializable]
public sealed class TraceMagicCircleShape
{
    [SerializeField] private TracePattern pattern = TracePattern.Circle;
    [SerializeField] private Vector2 position;
    [SerializeField, Min(0.01f)] private float size = 2.05f;
    [SerializeField, Min(0.1f)] private float rectangleAspectRatio = 1.5f;
    [SerializeField] private Vector2 lineStart = new(-1f, 0f);
    [SerializeField] private Vector2 lineEnd = new(1f, 0f);
    [SerializeField] private TraceMagicCircleShapeUsage usage;

    public TracePattern Pattern => pattern;
    public Vector2 Position => position;
    public float Size => size;
    public float RectangleAspectRatio => Mathf.Max(0.1f, rectangleAspectRatio);
    public Vector2 LineStart => lineStart;
    public Vector2 LineEnd => lineEnd;
    public TraceMagicCircleShapeUsage Usage => usage;
    public bool IsDisplayed => usage == TraceMagicCircleShapeUsage.Display;
}
