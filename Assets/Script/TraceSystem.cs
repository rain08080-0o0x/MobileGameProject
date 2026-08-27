using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class TraceSystem : MonoBehaviour
{
    [Header("Trace Target Settings")]
    [SerializeField, Min(16)] private int targetPointCount = 128;
    [SerializeField] private Color targetColor = new(0.58f, 0.62f, 0.66f, 0.85f);

    [Header("Line Settings")]
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Color lineColor = new(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private float minDistancePoints = 0.045f;

    private readonly List<Vector3> currentPoints = new();
    private readonly List<TraceTargetEntry> targets = new();
    private readonly List<CompletedStroke> completedStrokes = new();
    private TraceMagicCircleDefinition magicCircle;
    private LineRenderer lineRenderer;
    private Camera mainCamera;
    private Material lineMaterial;
    private TraceTargetEntry activeTarget;
    private float drawingStartedAt;
    private float lastDrawingSeconds;
    private bool isTracing;

    public event Action<TraceStrokeResult[]> BatchCompleted;
    public event Action<TraceStrokeResult> StrokeScored;
    public event Action TracingAvailable;
    public event Action TracingCompleted;

    public bool InputEnabled { get; private set; }
    public int CompletedCount => completedStrokes.Count;
    public int TargetCount => targets.Count;
    public float CurrentDrawingSeconds =>
        isTracing ? Mathf.Max(0f, Time.time - drawingStartedAt) : lastDrawingSeconds;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        mainCamera = Camera.main;
        lineMaterial = new Material(Shader.Find("Sprites/Default"))
        {
            name = "Runtime Trace Material",
        };
        ConfigureRenderer(lineRenderer, lineColor, 1);
        lineRenderer.positionCount = 0;
    }

    private void OnDestroy()
    {
        ClearTargetRenderers();
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }

    private void Update()
    {
        if (!InputEnabled)
        {
            return;
        }

        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                StartTracing(touch.position);
            }
            else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && isTracing)
            {
                Trace(touch.position);
            }
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && isTracing)
            {
                EndTracing(touch.position);
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            StartTracing(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isTracing)
        {
            Trace(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isTracing)
        {
            EndTracing(Input.mousePosition);
        }
    }

    public void SetMagicCircle(TraceMagicCircleDefinition definition)
    {
        SetInputEnabled(false);
        ClearCollectedLines();
        completedStrokes.Clear();
        magicCircle = definition;
        BuildTargets();
    }

    public void BeginRound()
    {
        if (magicCircle == null || targets.Count == 0)
        {
            throw new InvalidOperationException("A magic circle with displayed shapes is required.");
        }

        ClearCurrentLine();
        ClearCollectedLines();
        completedStrokes.Clear();
        activeTarget = null;
        isTracing = false;
        lastDrawingSeconds = 0f;

        for (var index = 0; index < targets.Count; index++)
        {
            targets[index].Renderer.enabled = true;
        }

        InputEnabled = true;
        TracingAvailable?.Invoke();
    }

    public void SetInputEnabled(bool enabled)
    {
        InputEnabled = enabled;
        if (!enabled && isTracing)
        {
            CancelCurrentStroke();
        }
    }

    public void ClearCollectedLines()
    {
        for (var index = 0; index < completedStrokes.Count; index++)
        {
            var stroke = completedStrokes[index];
            if (stroke.Renderer != null)
            {
                Destroy(stroke.Renderer.gameObject);
                stroke.Renderer = null;
            }
        }
    }

    private void BuildTargets()
    {
        ClearTargetRenderers();
        if (magicCircle == null)
        {
            return;
        }

        for (var index = 0; index < magicCircle.Shapes.Count; index++)
        {
            var shape = magicCircle.Shapes[index];
            if (shape == null || !shape.IsDisplayed)
            {
                continue;
            }

            var points = TracePatternPointFactory.Create(
                shape.Pattern,
                targetPointCount,
                shape.Position,
                shape.Size,
                shape.RectangleAspectRatio,
                shape.LineStart,
                shape.LineEnd);
            var targetObject = new GameObject($"Trace Target {targets.Count + 1}");
            targetObject.transform.SetParent(transform, false);
            var targetRenderer = targetObject.AddComponent<LineRenderer>();
            ConfigureRenderer(targetRenderer, targetColor, 0);
            targetRenderer.loop = TracePatternPointFactory.IsClosed(shape.Pattern);
            targetRenderer.positionCount = points.Count;
            for (var pointIndex = 0; pointIndex < points.Count; pointIndex++)
            {
                targetRenderer.SetPosition(
                    pointIndex,
                    new Vector3(points[pointIndex].x, points[pointIndex].y, 0f));
            }
            targetRenderer.enabled = false;
            targets.Add(new TraceTargetEntry(
                points,
                TracePatternPointFactory.IsClosed(shape.Pattern),
                targetRenderer));
        }
    }

    private void ClearTargetRenderers()
    {
        for (var index = 0; index < targets.Count; index++)
        {
            if (targets[index].Renderer != null)
            {
                Destroy(targets[index].Renderer.gameObject);
            }
        }
        targets.Clear();
    }

    private void StartTracing(Vector2 screenPosition)
    {
        var worldPosition = GetWorldPositionFromScreen(screenPosition);
        if (!TryGetAvailableTarget(worldPosition, out activeTarget))
        {
            return;
        }

        ClearCurrentLine();
        isTracing = true;
        drawingStartedAt = Time.time;
        lastDrawingSeconds = 0f;
        AddPoint(worldPosition, true);
    }

    private void Trace(Vector2 screenPosition)
    {
        AddPoint(GetWorldPositionFromScreen(screenPosition), false);
    }

    private void EndTracing(Vector2 screenPosition)
    {
        AddPoint(GetWorldPositionFromScreen(screenPosition), true);
        isTracing = false;
        lastDrawingSeconds = Mathf.Max(Time.time - drawingStartedAt, 0.01f);

        if (currentPoints.Count < 2)
        {
            CancelCurrentStroke();
            return;
        }

        var strokePoints = currentPoints.ToArray();
        var savedRenderer = CreateSavedLine(strokePoints, completedStrokes.Count + 1);
        var completedStroke = new CompletedStroke(
            activeTarget,
            savedRenderer,
            strokePoints,
            CalculateAccuracy(activeTarget, strokePoints),
            lastDrawingSeconds);
        completedStrokes.Add(completedStroke);

        activeTarget.Renderer.enabled = false;
        activeTarget = null;
        ClearCurrentLine();
        InputEnabled = false;
        StrokeScored?.Invoke(CreateStrokeResult(completedStroke));
        TracingCompleted?.Invoke();

        if (completedStrokes.Count < targets.Count)
        {
            lastDrawingSeconds = 0f;
            InputEnabled = true;
            TracingAvailable?.Invoke();
            return;
        }

        BatchCompleted?.Invoke(CreateBatchResults());
    }

    private void AddPoint(Vector3 worldPosition, bool force)
    {
        if (activeTarget == null)
        {
            return;
        }

        if (!force && currentPoints.Count > 0 &&
            Vector3.Distance(currentPoints[^1], worldPosition) <= minDistancePoints)
        {
            return;
        }

        currentPoints.Add(worldPosition);
        lineRenderer.positionCount = currentPoints.Count;
        lineRenderer.SetPosition(currentPoints.Count - 1, worldPosition);
    }

    private bool TryGetAvailableTarget(Vector2 worldPosition, out TraceTargetEntry target)
    {
        target = null;
        var hitRadius = Mathf.Max(lineWidth * 1.5f, 0.2f);
        var nearestDistance = float.PositiveInfinity;

        for (var index = 0; index < targets.Count; index++)
        {
            var candidate = targets[index];
            if (IsCompleted(candidate))
            {
                continue;
            }

            var distance = DistanceToPolyline(worldPosition, candidate.Points, candidate.IsClosed);
            if (distance <= hitRadius && distance < nearestDistance)
            {
                target = candidate;
                nearestDistance = distance;
            }
        }
        return target != null;
    }

    private bool IsCompleted(TraceTargetEntry target)
    {
        for (var index = 0; index < completedStrokes.Count; index++)
        {
            if (completedStrokes[index].Target == target)
            {
                return true;
            }
        }
        return false;
    }

    private float CalculateAccuracy(
        TraceTargetEntry target,
        IReadOnlyList<Vector3> strokePoints)
    {
        var points = new Vector2[strokePoints.Count];
        for (var index = 0; index < strokePoints.Count; index++)
        {
            points[index] = strokePoints[index];
        }
        return TraceScorer.Calculate(
            target.Points,
            target.IsClosed,
            points,
            Mathf.Max(lineWidth, 0.1f));
    }

    private static float DistanceToPolyline(
        Vector2 point,
        IReadOnlyList<Vector2> line,
        bool isClosed)
    {
        var minimum = float.PositiveInfinity;
        var segmentCount = isClosed ? line.Count : line.Count - 1;
        for (var index = 0; index < segmentCount; index++)
        {
            var start = line[index];
            var end = line[(index + 1) % line.Count];
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            var progress = lengthSquared <= Mathf.Epsilon
                ? 0f
                : Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            minimum = Mathf.Min(minimum, Vector2.Distance(point, start + segment * progress));
        }
        return minimum;
    }

    private TraceStrokeResult[] CreateBatchResults()
    {
        var results = new TraceStrokeResult[completedStrokes.Count];
        for (var index = 0; index < completedStrokes.Count; index++)
        {
            results[index] = CreateStrokeResult(completedStrokes[index]);
        }
        return results;
    }

    private static TraceStrokeResult CreateStrokeResult(CompletedStroke stroke)
    {
        var points = new Vector2[stroke.Points.Length];
        for (var index = 0; index < stroke.Points.Length; index++)
        {
            points[index] = stroke.Points[index];
        }
        return new TraceStrokeResult(points, stroke.Accuracy, stroke.DrawingSeconds);
    }

    private Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
    {
        mainCamera ??= Camera.main;
        var screenPoint = new Vector3(
            screenPosition.x,
            screenPosition.y,
            -mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(screenPoint);
    }

    private LineRenderer CreateSavedLine(IReadOnlyList<Vector3> points, int number)
    {
        var lineObject = new GameObject($"Collected Trace {number}");
        lineObject.transform.SetParent(transform);
        var savedRenderer = lineObject.AddComponent<LineRenderer>();
        ConfigureRenderer(savedRenderer, lineColor, 1);
        savedRenderer.positionCount = points.Count;
        for (var index = 0; index < points.Count; index++)
        {
            savedRenderer.SetPosition(index, points[index]);
        }
        return savedRenderer;
    }

    private void ConfigureRenderer(LineRenderer renderer, Color color, int sortingOrder)
    {
        renderer.useWorldSpace = true;
        renderer.sharedMaterial = lineMaterial;
        renderer.startWidth = lineWidth;
        renderer.endWidth = lineWidth;
        renderer.startColor = color;
        renderer.endColor = color;
        renderer.numCapVertices = 8;
        renderer.numCornerVertices = 8;
        renderer.sortingOrder = sortingOrder;
    }

    private void CancelCurrentStroke()
    {
        isTracing = false;
        activeTarget = null;
        lastDrawingSeconds = 0f;
        ClearCurrentLine();
    }

    private void ClearCurrentLine()
    {
        currentPoints.Clear();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }

    private sealed class TraceTargetEntry
    {
        public TraceTargetEntry(
            IReadOnlyList<Vector2> points,
            bool isClosed,
            LineRenderer renderer)
        {
            Points = points;
            IsClosed = isClosed;
            Renderer = renderer;
        }

        public IReadOnlyList<Vector2> Points { get; }
        public bool IsClosed { get; }
        public LineRenderer Renderer { get; }
    }

    private sealed class CompletedStroke
    {
        public CompletedStroke(
            TraceTargetEntry target,
            LineRenderer renderer,
            Vector3[] points,
            float accuracy,
            float drawingSeconds)
        {
            Target = target;
            Renderer = renderer;
            Points = points;
            Accuracy = accuracy;
            DrawingSeconds = drawingSeconds;
        }

        public TraceTargetEntry Target { get; }
        public LineRenderer Renderer { get; set; }
        public Vector3[] Points { get; }
        public float Accuracy { get; }
        public float DrawingSeconds { get; }
    }
}
