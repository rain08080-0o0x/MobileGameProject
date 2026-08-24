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
    [SerializeField] private TraceMagicCircleShapeUsage usage;

    public TracePattern Pattern => pattern;
    public Vector2 Position => position;
    public float Size => size;
    public TraceMagicCircleShapeUsage Usage => usage;
    public bool IsDisplayed => usage == TraceMagicCircleShapeUsage.Display;
}
