using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TraceTarget))]
public sealed class TraceDrawingController : MonoBehaviour
{
    private const float MinimumPointDistance = 0.045f;
    private const float TopUiHeight = 190f;

    [SerializeField] private Color backgroundColor = new(0.055f, 0.12f, 0.2f, 1f);
    [SerializeField] private Color strokeColor = new(0.18f, 0.89f, 0.9f, 1f);
    [SerializeField] private Color cursorColor = new(1f, 0.82f, 0.4f, 0.35f);

    private readonly List<Vector2> strokePoints = new();

    private Camera drawingCamera;
    private TraceTarget traceTarget;
    private LineRenderer strokeRenderer;
    private GameObject cursorObject;
    private bool isDrawing;

    public event Action<TraceStrokeResult> StrokeCompleted;

    public bool InputEnabled { get; private set; }
    public Color StrokeColor => strokeColor;
    public float StrokeWidth => traceTarget.ToleranceRadius * 2f;

    private void Awake()
    {
        drawingCamera = Camera.main;
        traceTarget = GetComponent<TraceTarget>();

        ConfigureCamera();
        CreateStrokeRenderer();
        CreateCursor();
        PreparePattern(TracePattern.UprightTriangle);
        SetInputEnabled(false);
    }

    private void Update()
    {
        if (!InputEnabled || !TryReadPointer(out var screenPosition, out var began, out var held, out var ended))
        {
            return;
        }

        if (began)
        {
            if (screenPosition.y >= Screen.height - TopUiHeight)
            {
                return;
            }

            BeginStroke(ScreenToWorld(screenPosition));
        }

        if (!isDrawing)
        {
            return;
        }

        var worldPosition = ScreenToWorld(screenPosition);
        if (held)
        {
            AddStrokePoint(worldPosition, false);
            cursorObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, -0.2f);
        }

        if (ended)
        {
            AddStrokePoint(worldPosition, true);
            FinishStroke();
        }
    }

    public void PreparePattern(TracePattern pattern)
    {
        traceTarget.SetPattern(pattern);
        traceTarget.SetVisible(true);
        ClearCurrentStroke();
    }

    public void SetInputEnabled(bool enabled)
    {
        InputEnabled = enabled;
        if (!enabled)
        {
            ClearCurrentStroke();
        }
    }

    public void SetTargetVisible(bool visible)
    {
        traceTarget.SetVisible(visible);
    }

    private void ConfigureCamera()
    {
        if (drawingCamera == null)
        {
            drawingCamera = FindFirstObjectByType<Camera>();
        }

        drawingCamera.orthographic = true;
        drawingCamera.orthographicSize = 3.65f;
        drawingCamera.clearFlags = CameraClearFlags.SolidColor;
        drawingCamera.backgroundColor = backgroundColor;
    }

    private void CreateStrokeRenderer()
    {
        var strokeObject = new GameObject("Player Stroke");
        strokeObject.transform.SetParent(transform, false);

        strokeRenderer = strokeObject.AddComponent<LineRenderer>();
        strokeRenderer.useWorldSpace = true;
        strokeRenderer.widthMultiplier = StrokeWidth;
        strokeRenderer.startColor = strokeColor;
        strokeRenderer.endColor = strokeColor;
        strokeRenderer.numCapVertices = 12;
        strokeRenderer.numCornerVertices = 8;
        strokeRenderer.sortingOrder = 1;
        strokeRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
        {
            name = "Runtime Player Stroke Material",
        };
    }

    private void CreateCursor()
    {
        cursorObject = new GameObject("Trace Range Cursor");
        cursorObject.transform.SetParent(transform, false);

        var spriteRenderer = cursorObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = TraceCircleSpriteFactory.Create(64, "Runtime Trace Cursor Texture");
        spriteRenderer.color = cursorColor;
        spriteRenderer.sortingOrder = 2;

        cursorObject.transform.localScale = new Vector3(StrokeWidth, StrokeWidth, 1f);
        cursorObject.SetActive(false);
    }

    private void BeginStroke(Vector2 worldPosition)
    {
        ClearCurrentStroke();
        isDrawing = true;
        cursorObject.SetActive(true);
        AddStrokePoint(worldPosition, true);
    }

    private void AddStrokePoint(Vector2 worldPosition, bool force)
    {
        if (!force && strokePoints.Count > 0 &&
            Vector2.Distance(strokePoints[^1], worldPosition) < MinimumPointDistance)
        {
            return;
        }

        strokePoints.Add(worldPosition);
        strokeRenderer.positionCount = strokePoints.Count;
        strokeRenderer.SetPosition(
            strokePoints.Count - 1,
            new Vector3(worldPosition.x, worldPosition.y, -0.1f));
    }

    private void FinishStroke()
    {
        isDrawing = false;
        cursorObject.SetActive(false);

        var accuracy = TraceScorer.Calculate(
            traceTarget.Points,
            traceTarget.IsClosed,
            strokePoints,
            traceTarget.ToleranceRadius);
        var result = new TraceStrokeResult(strokePoints.ToArray(), accuracy);
        StrokeCompleted?.Invoke(result);
    }

    private void ClearCurrentStroke()
    {
        isDrawing = false;
        strokePoints.Clear();
        if (strokeRenderer != null)
        {
            strokeRenderer.positionCount = 0;
        }

        if (cursorObject != null)
        {
            cursorObject.SetActive(false);
        }
    }

    private Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        var world = drawingCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, -drawingCamera.transform.position.z));
        return new Vector2(world.x, world.y);
    }

    private static bool TryReadPointer(
        out Vector2 screenPosition,
        out bool began,
        out bool held,
        out bool ended)
    {
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            screenPosition = touch.position;
            began = touch.phase == TouchPhase.Began;
            held = touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            return true;
        }

        screenPosition = Input.mousePosition;
        began = Input.GetMouseButtonDown(0);
        held = Input.GetMouseButton(0);
        ended = Input.GetMouseButtonUp(0);
        return began || held || ended;
    }
}
