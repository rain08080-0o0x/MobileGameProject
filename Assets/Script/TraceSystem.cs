using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class TraceSystem : MonoBehaviour
{
    [Header("Trace Targets")]
    [SerializeField] private List<SpriteRenderer> targetImages = new();

    [Header("Collection Destinations")]
    [SerializeField] private List<TraceDestination> destination = new();
    [SerializeField] private float moveSpeed = 5f;

    [Header("Line Settings")]
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Color lineColor = new(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private float minDistancePoints = 0.5f;

    private readonly List<Vector3> currentPoints = new();
    private readonly List<CompletedStroke> completedStrokes = new();
    private LineRenderer lineRenderer;
    private Camera mainCamera;
    private Material lineMaterial;
    private SpriteRenderer activeImage;
    private float drawingStartedAt;
    private float lastDrawingSeconds;
    private bool isTracing;
    private bool isCollecting;
    private Coroutine collectionCoroutine;

    public event Action<TraceStrokeResult[]> BatchCompleted;

    public bool InputEnabled { get; private set; }
    public int CompletedCount => completedStrokes.Count;
    public int TargetCount => CountConfiguredTargets();
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
        ConfigureRenderer(lineRenderer);
        lineRenderer.positionCount = 0;
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }

    private void Update()
    {
        if (!InputEnabled || isCollecting)
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

    public void BeginRound()
    {
        if (collectionCoroutine != null)
        {
            StopCoroutine(collectionCoroutine);
            collectionCoroutine = null;
        }

        ClearCurrentLine();
        ClearCollectedLines();
        completedStrokes.Clear();
        activeImage = null;
        isTracing = false;
        isCollecting = false;
        lastDrawingSeconds = 0f;

        for (var index = 0; index < targetImages.Count; index++)
        {
            if (targetImages[index] != null)
            {
                targetImages[index].enabled = true;
            }
        }

        InputEnabled = true;
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

    private void StartTracing(Vector2 screenPosition)
    {
        var worldPosition = GetWorldPositionFromScreen(screenPosition);
        if (!TryGetAvailableImage(worldPosition, out activeImage))
        {
            return;
        }

        ClearCurrentLine();
        isTracing = true;
        drawingStartedAt = Time.time;
        lastDrawingSeconds = 0f;
        AddPointIfValid(worldPosition, true);
    }

    private void Trace(Vector2 screenPosition)
    {
        AddPointIfValid(GetWorldPositionFromScreen(screenPosition), false);
    }

    private void EndTracing(Vector2 screenPosition)
    {
        AddPointIfValid(GetWorldPositionFromScreen(screenPosition), true);
        isTracing = false;
        lastDrawingSeconds = Mathf.Max(Time.time - drawingStartedAt, 0.01f);

        if (currentPoints.Count < 2)
        {
            CancelCurrentStroke();
            return;
        }

        var targetIndex = targetImages.IndexOf(activeImage);
        var strokePoints = currentPoints.ToArray();
        var savedRenderer = CreateSavedLine(strokePoints, completedStrokes.Count + 1);
        completedStrokes.Add(new CompletedStroke(
            activeImage,
            destination[targetIndex],
            savedRenderer,
            strokePoints,
            CalculateAccuracy(activeImage, strokePoints),
            lastDrawingSeconds));

        activeImage.enabled = false;
        activeImage = null;
        ClearCurrentLine();

        InputEnabled = false;
        isCollecting = true;
        collectionCoroutine = StartCoroutine(MoveLineToDestination(completedStrokes.Count - 1));
    }

    private void AddPointIfValid(Vector3 worldPosition, bool force)
    {
        if (activeImage == null || !IsOverImage(activeImage, worldPosition))
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

    private IEnumerator MoveLineToDestination(int strokeIndex)
    {
        var stroke = completedStrokes[strokeIndex];
        var bounds = new Bounds(stroke.Points[0], Vector3.zero);
        for (var pointIndex = 1; pointIndex < stroke.Points.Length; pointIndex++)
        {
            bounds.Encapsulate(stroke.Points[pointIndex]);
        }

        var targetCenter = stroke.Destination.TargetPosition;
        targetCenter.z = bounds.center.z;
        var targetOffset = targetCenter - bounds.center;
        var currentOffset = Vector3.zero;

        while (currentOffset != targetOffset)
        {
            var nextOffset = Vector3.MoveTowards(
                currentOffset,
                targetOffset,
                moveSpeed * Time.deltaTime);
            var delta = nextOffset - currentOffset;
            currentOffset = nextOffset;
            for (var pointIndex = 0; pointIndex < stroke.Points.Length; pointIndex++)
            {
                stroke.Points[pointIndex] += delta;
                stroke.Renderer.SetPosition(pointIndex, stroke.Points[pointIndex]);
            }
            yield return null;
        }

        isCollecting = false;
        collectionCoroutine = null;
        if (completedStrokes.Count < CountConfiguredTargets())
        {
            lastDrawingSeconds = 0f;
            InputEnabled = true;
            yield break;
        }

        BatchCompleted?.Invoke(CreateBatchResults());
    }

    private TraceStrokeResult[] CreateBatchResults()
    {
        var results = new TraceStrokeResult[completedStrokes.Count];
        for (var index = 0; index < completedStrokes.Count; index++)
        {
            var stroke = completedStrokes[index];
            var points = new Vector2[stroke.Points.Length];
            for (var pointIndex = 0; pointIndex < stroke.Points.Length; pointIndex++)
            {
                points[pointIndex] = stroke.Points[pointIndex];
            }
            results[index] = new TraceStrokeResult(points, stroke.Accuracy, stroke.DrawingSeconds);
        }
        return results;
    }

    private float CalculateAccuracy(SpriteRenderer image, IReadOnlyList<Vector3> strokePoints)
    {
        var points = new Vector2[strokePoints.Count];
        for (var index = 0; index < strokePoints.Count; index++)
        {
            points[index] = strokePoints[index];
        }
        return TraceScorer.Calculate(
            CreateTargetPoints(image), true, points, Mathf.Max(lineWidth, 0.1f));
    }

    private static List<Vector2> CreateTargetPoints(SpriteRenderer image)
    {
        var bounds = image.sprite.bounds;
        var minimum = bounds.min;
        var maximum = bounds.max;
        var localPoints = image.name.IndexOf("Triangle", StringComparison.OrdinalIgnoreCase) >= 0
            ? new[]
            {
                new Vector3(minimum.x, maximum.y),
                new Vector3(maximum.x, maximum.y),
                new Vector3((minimum.x + maximum.x) * 0.5f, minimum.y),
            }
            : new[]
            {
                new Vector3(minimum.x, maximum.y),
                new Vector3(maximum.x, maximum.y),
                new Vector3(maximum.x, minimum.y),
                new Vector3(minimum.x, minimum.y),
            };

        var result = new List<Vector2>(localPoints.Length);
        for (var index = 0; index < localPoints.Length; index++)
        {
            result.Add(image.transform.TransformPoint(localPoints[index]));
        }
        return result;
    }

    private bool TryGetAvailableImage(Vector3 worldPosition, out SpriteRenderer image)
    {
        for (var index = 0; index < targetImages.Count; index++)
        {
            var candidate = targetImages[index];
            if (candidate != null && !IsCompleted(candidate) && IsOverImage(candidate, worldPosition))
            {
                image = candidate;
                return true;
            }
        }

        image = null;
        return false;
    }

    private bool IsCompleted(SpriteRenderer image)
    {
        for (var index = 0; index < completedStrokes.Count; index++)
        {
            if (completedStrokes[index].Image == image)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsOverImage(SpriteRenderer image, Vector3 worldPosition)
    {
        if (image == null || image.sprite == null)
        {
            return false;
        }

        var localPosition = image.transform.InverseTransformPoint(worldPosition);
        var sprite = image.sprite;
        var rect = sprite.rect;
        var pixelX = localPosition.x * sprite.pixelsPerUnit + sprite.pivot.x;
        var pixelY = localPosition.y * sprite.pixelsPerUnit + sprite.pivot.y;
        if (pixelX < 0f || pixelX >= rect.width || pixelY < 0f || pixelY >= rect.height)
        {
            return false;
        }

        var color = sprite.texture.GetPixel(
            Mathf.FloorToInt(rect.x + pixelX),
            Mathf.FloorToInt(rect.y + pixelY));
        return color.a > 0.1f;
    }

    private Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
    {
        mainCamera ??= Camera.main;
        var screenPoint = new Vector3(
            screenPosition.x, screenPosition.y, -mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(screenPoint);
    }

    private LineRenderer CreateSavedLine(IReadOnlyList<Vector3> points, int number)
    {
        var lineObject = new GameObject($"Collected Trace {number}");
        lineObject.transform.SetParent(transform);
        var savedRenderer = lineObject.AddComponent<LineRenderer>();
        ConfigureRenderer(savedRenderer);
        savedRenderer.positionCount = points.Count;
        for (var index = 0; index < points.Count; index++)
        {
            savedRenderer.SetPosition(index, points[index]);
        }
        return savedRenderer;
    }

    private void ConfigureRenderer(LineRenderer renderer)
    {
        renderer.useWorldSpace = true;
        renderer.sharedMaterial = lineMaterial;
        renderer.startWidth = lineWidth;
        renderer.endWidth = lineWidth;
        renderer.startColor = lineColor;
        renderer.endColor = lineColor;
    }

    private int CountConfiguredTargets()
    {
        var count = 0;
        var maximum = Mathf.Min(targetImages.Count, destination.Count);
        for (var index = 0; index < maximum; index++)
        {
            if (targetImages[index] != null && destination[index] != null)
            {
                count++;
            }
        }
        return count;
    }

    private void CancelCurrentStroke()
    {
        isTracing = false;
        activeImage = null;
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

    private sealed class CompletedStroke
    {
        public CompletedStroke(
            SpriteRenderer image,
            TraceDestination targetDestination,
            LineRenderer renderer,
            Vector3[] points,
            float accuracy,
            float drawingSeconds)
        {
            Image = image;
            Destination = targetDestination;
            Renderer = renderer;
            Points = points;
            Accuracy = accuracy;
            DrawingSeconds = drawingSeconds;
        }

        public SpriteRenderer Image { get; }
        public TraceDestination Destination { get; }
        public LineRenderer Renderer { get; set; }
        public Vector3[] Points { get; }
        public float Accuracy { get; }
        public float DrawingSeconds { get; }
    }
}
