using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TraceTarget))]
public sealed class TraceDrawingController : MonoBehaviour
{
    private const float MinimumPointDistance = 0.045f;
    private const float TopUiHeight = 150f;

    [SerializeField] private Color backgroundColor = new(0.055f, 0.12f, 0.2f, 1f);
    [SerializeField] private Color strokeColor = new(0.18f, 0.89f, 0.9f, 1f);
    [SerializeField] private Color cursorColor = new(1f, 0.82f, 0.4f, 0.35f);
    [SerializeField, Min(0.02f)] private float strokeWidth = 0.16f;

    private readonly List<Vector2> strokePoints = new();

    private Camera drawingCamera;
    private TraceTarget traceTarget;
    private LineRenderer strokeRenderer;
    private GameObject cursorObject;
    private bool isDrawing;
    private bool roundComplete;
    private float resultPercentage;

    private void Awake()
    {
        drawingCamera = Camera.main;
        traceTarget = GetComponent<TraceTarget>();

        ConfigureCamera();
        CreateStrokeRenderer();
        CreateCursor();
        SelectPattern(TracePattern.SShape);
    }

    private void Update()
    {
        if (roundComplete || !TryReadPointer(out var screenPosition, out var began, out var held, out var ended))
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

    private void OnGUI()
    {
        var panelWidth = Mathf.Min(520f, Screen.width - 24f);
        var panelHeight = roundComplete ? 142f : 106f;

        GUILayout.BeginArea(new Rect(12f, 12f, panelWidth, panelHeight), GUI.skin.box);
        GUILayout.Label("お手本を一筆でなぞってください（指を離すと判定）");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("S字", GUILayout.Height(32f)))
        {
            SelectPattern(TracePattern.SShape);
        }

        if (GUILayout.Button("円形", GUILayout.Height(32f)))
        {
            SelectPattern(TracePattern.Circle);
        }
        GUILayout.EndHorizontal();

        if (roundComplete)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"一致率  {Mathf.RoundToInt(resultPercentage)}%", GUILayout.Height(30f));
            if (GUILayout.Button("もう一度", GUILayout.Width(120f), GUILayout.Height(30f)))
            {
                ResetRound();
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndArea();
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
        strokeRenderer.widthMultiplier = strokeWidth;
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
        spriteRenderer.sprite = CreateCircleSprite(64);
        spriteRenderer.color = cursorColor;
        spriteRenderer.sortingOrder = 2;

        var diameter = traceTarget.ToleranceRadius * 2f;
        cursorObject.transform.localScale = new Vector3(diameter, diameter, 1f);
        cursorObject.SetActive(false);
    }

    private static Sprite CreateCircleSprite(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Trace Cursor Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var pixels = new Color[size * size];
        var center = (size - 1) * 0.5f;
        var radius = size * 0.5f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                var alpha = Mathf.Clamp01(radius - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void BeginStroke(Vector2 worldPosition)
    {
        strokePoints.Clear();
        strokeRenderer.positionCount = 0;
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
        roundComplete = true;
        cursorObject.SetActive(false);
        resultPercentage = TraceScorer.Calculate(
            traceTarget.Points,
            traceTarget.IsClosed,
            strokePoints,
            traceTarget.ToleranceRadius);
    }

    private void SelectPattern(TracePattern pattern)
    {
        traceTarget.SetPattern(pattern);
        ResetRound();
    }

    private void ResetRound()
    {
        isDrawing = false;
        roundComplete = false;
        resultPercentage = 0f;
        strokePoints.Clear();
        strokeRenderer.positionCount = 0;
        cursorObject.SetActive(false);
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
