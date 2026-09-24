using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class ImageTraceSystem : MonoBehaviour, ITraceDrawingSource
{
    [SerializeField] private List<SpriteRenderer> targetImages = new();
    [SerializeField] private List<TraceDestination> destinations = new();
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField, Min(0.01f)] private float lineWidth = 0.3f;
    [SerializeField] private Color lineColor = new(0.89f, 0f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float minDistancePoints = 0.05f;

    private readonly List<Vector3> currentPoints = new();
    private readonly List<CompletedStroke> completedStrokes = new();
    private LineRenderer lineRenderer;
    private Material lineMaterial;
    private Camera mainCamera;
    private SpriteRenderer activeImage;
    private Coroutine collectionCoroutine;
    private float drawingStartedAt;
    private float lastDrawingSeconds;
    private bool isTracing;
    private bool isCollecting;

    public event Action<TraceStrokeResult[]> BatchCompleted;
    public event Action<TraceStrokeResult> StrokeScored;
    public event Action TracingAvailable;
    public event Action TracingCompleted;

    public int CompletedCount => completedStrokes.Count;
    public int TargetCount => Mathf.Min(targetImages.Count, destinations.Count);
    public float CurrentDrawingSeconds =>
        isTracing ? Mathf.Max(0f, Time.time - drawingStartedAt) : lastDrawingSeconds;
    public bool InputEnabled { get; private set; }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        mainCamera = Camera.main;
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
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
            else if (isTracing &&
                     (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
            {
                Trace(touch.position);
            }
            else if (isTracing &&
                     (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                EndTracing(touch.position);
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            StartTracing(Input.mousePosition);
        }
        else if (isTracing && Input.GetMouseButton(0))
        {
            Trace(Input.mousePosition);
        }
        else if (isTracing && Input.GetMouseButtonUp(0))
        {
            EndTracing(Input.mousePosition);
        }
    }

    public void BeginRound()
    {
        if (TargetCount == 0)
        {
            throw new InvalidOperationException("Image trace targets are not configured.");
        }

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

        for (var index = 0; index < TargetCount; index++)
        {
            targetImages[index].enabled = true;
        }

        InputEnabled = true;
        TracingAvailable?.Invoke();
    }

    public void SetInputEnabled(bool enabled)
    {
        InputEnabled = enabled;
        if (!enabled && isTracing)
        {
            isTracing = false;
            activeImage = null;
            ClearCurrentLine();
        }
    }

    public void ClearCollectedLines()
    {
        for (var index = 0; index < completedStrokes.Count; index++)
        {
            var renderer = completedStrokes[index].Renderer;
            if (renderer != null)
            {
                Destroy(renderer.gameObject);
                completedStrokes[index].Renderer = null;
            }
        }
    }

    private void StartTracing(Vector2 screenPosition)
    {
        var worldPosition = ScreenToWorld(screenPosition);
        activeImage = GetImageAt(worldPosition);
        if (activeImage == null)
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
        AddPoint(ScreenToWorld(screenPosition), false);
    }

    private void EndTracing(Vector2 screenPosition)
    {
        AddPoint(ScreenToWorld(screenPosition), true);
        isTracing = false;
        lastDrawingSeconds = Mathf.Max(Time.time - drawingStartedAt, 0.01f);
        if (currentPoints.Count < 2)
        {
            activeImage = null;
            ClearCurrentLine();
            return;
        }

        var imageIndex = targetImages.IndexOf(activeImage);
        var strokePoints = currentPoints.ToArray();
        var accuracy = CalculateAccuracy(activeImage, strokePoints);
        var savedRenderer = CreateSavedLine(strokePoints);
        completedStrokes.Add(new CompletedStroke(
            activeImage, destinations[imageIndex], savedRenderer,
            strokePoints, accuracy, lastDrawingSeconds));

        activeImage.enabled = false;
        activeImage = null;
        ClearCurrentLine();
        InputEnabled = false;
        StrokeScored?.Invoke(new TraceStrokeResult(ToVector2(strokePoints), accuracy, lastDrawingSeconds));
        TracingCompleted?.Invoke();
        isCollecting = true;
        collectionCoroutine = StartCoroutine(CollectStroke(completedStrokes.Count - 1));
    }

    private IEnumerator CollectStroke(int strokeIndex)
    {
        var stroke = completedStrokes[strokeIndex];
        var bounds = new Bounds(stroke.Points[0], Vector3.zero);
        for (var index = 1; index < stroke.Points.Length; index++)
        {
            bounds.Encapsulate(stroke.Points[index]);
        }

        var targetCenter = stroke.Destination.TargetPosition;
        targetCenter.z = bounds.center.z;
        var targetOffset = targetCenter - bounds.center;
        var currentOffset = Vector3.zero;
        while (currentOffset != targetOffset)
        {
            var nextOffset = Vector3.MoveTowards(
                currentOffset, targetOffset, moveSpeed * Time.deltaTime);
            var delta = nextOffset - currentOffset;
            currentOffset = nextOffset;
            for (var index = 0; index < stroke.Points.Length; index++)
            {
                stroke.Points[index] += delta;
                stroke.Renderer.SetPosition(index, stroke.Points[index]);
            }
            yield return null;
        }

        isCollecting = false;
        collectionCoroutine = null;
        if (completedStrokes.Count < TargetCount)
        {
            lastDrawingSeconds = 0f;
            InputEnabled = true;
            TracingAvailable?.Invoke();
            yield break;
        }

        var results = new TraceStrokeResult[completedStrokes.Count];
        for (var index = 0; index < completedStrokes.Count; index++)
        {
            var completed = completedStrokes[index];
            results[index] = new TraceStrokeResult(
                ToVector2(completed.Points), completed.Accuracy, completed.DrawingSeconds);
        }
        BatchCompleted?.Invoke(results);
    }

    private void AddPoint(Vector3 worldPosition, bool force)
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

    private SpriteRenderer GetImageAt(Vector3 worldPosition)
    {
        for (var index = 0; index < TargetCount; index++)
        {
            var image = targetImages[index];
            if (image != null && image.enabled && destinations[index] != null &&
                IsOverImage(image, worldPosition))
            {
                return image;
            }
        }
        return null;
    }

    private static bool IsOverImage(SpriteRenderer image, Vector3 worldPosition)
    {
        if (image == null || image.sprite == null)
        {
            return false;
        }

        var sprite = image.sprite;
        var local = image.transform.InverseTransformPoint(worldPosition);
        var pixelX = Mathf.FloorToInt(local.x * sprite.pixelsPerUnit + sprite.pivot.x);
        var pixelY = Mathf.FloorToInt(local.y * sprite.pixelsPerUnit + sprite.pivot.y);
        if (pixelX < 0 || pixelY < 0 || pixelX >= sprite.rect.width ||
            pixelY >= sprite.rect.height)
        {
            return false;
        }

        return sprite.texture.GetPixel(
            Mathf.FloorToInt(sprite.rect.x) + pixelX,
            Mathf.FloorToInt(sprite.rect.y) + pixelY).a > 0.1f;
    }

    private float CalculateAccuracy(SpriteRenderer image, IReadOnlyList<Vector3> strokePoints)
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

        var targetPoints = new Vector2[localPoints.Length];
        for (var index = 0; index < localPoints.Length; index++)
        {
            targetPoints[index] = image.transform.TransformPoint(localPoints[index]);
        }
        return TraceScorer.Calculate(targetPoints, true, ToVector2(strokePoints),
            Mathf.Max(lineWidth, 0.1f));
    }

    private LineRenderer CreateSavedLine(IReadOnlyList<Vector3> points)
    {
        var lineObject = new GameObject($"Collected Image Trace {completedStrokes.Count + 1}");
        lineObject.transform.SetParent(transform, false);
        var renderer = lineObject.AddComponent<LineRenderer>();
        ConfigureRenderer(renderer);
        renderer.positionCount = points.Count;
        for (var index = 0; index < points.Count; index++)
        {
            renderer.SetPosition(index, points[index]);
        }
        return renderer;
    }

    private void ConfigureRenderer(LineRenderer renderer)
    {
        renderer.useWorldSpace = true;
        renderer.sharedMaterial = lineMaterial;
        renderer.startWidth = lineWidth;
        renderer.endWidth = lineWidth;
        renderer.startColor = lineColor;
        renderer.endColor = lineColor;
        renderer.sortingOrder = 1;
    }

    private void ClearCurrentLine()
    {
        currentPoints.Clear();
        lineRenderer.positionCount = 0;
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        mainCamera ??= Camera.main;
        return mainCamera.ScreenToWorldPoint(new Vector3(
            screenPosition.x, screenPosition.y, -mainCamera.transform.position.z));
    }

    private static Vector2[] ToVector2(IReadOnlyList<Vector3> points)
    {
        var result = new Vector2[points.Count];
        for (var index = 0; index < points.Count; index++)
        {
            result[index] = points[index];
        }
        return result;
    }

    private sealed class CompletedStroke
    {
        public CompletedStroke(SpriteRenderer image, TraceDestination destination,
            LineRenderer renderer, Vector3[] points, float accuracy, float drawingSeconds)
        {
            Image = image;
            Destination = destination;
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
