using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class TraceMagicCircleEditorWindow : EditorWindow
{
    private const float PaletteWidth = 150f;
    private const float InspectorWidth = 280f;
    private const float PreviewExtent = 5f;
    private const float HandleSize = 10f;
    private const float HitDistance = 10f;
    private const int PreviewControlHint = 184739;

    private static readonly TracePattern[] PalettePatterns =
    {
        TracePattern.Circle,
        TracePattern.UprightTriangle,
        TracePattern.InvertedTriangle,
        TracePattern.LeftTriangle,
        TracePattern.RightTriangle,
        TracePattern.Square,
        TracePattern.Rectangle,
        TracePattern.Diamond,
        TracePattern.Hexagon,
        TracePattern.HorizontalHexagon,
        TracePattern.Star,
        TracePattern.Line,
        TracePattern.Polyline,
    };

    private enum PreviewDragMode
    {
        None,
        Move,
        Scale,
        RectangleAspectRatio,
        LineStart,
        LineEnd,
        PolylinePoint,
    }

    private TraceMagicCircleDefinition definition;
    private SerializedObject serializedDefinition;
    private SerializedProperty displayNameProperty;
    private SerializedProperty basePowerProperty;
    private SerializedProperty drawCountProperty;
    private SerializedProperty shapesProperty;
    private Vector2 shapeListScroll;
    private int selectedShapeIndex = -1;
    private PreviewDragMode previewDragMode;
    private Vector2 dragStartWorld;
    private Vector2 dragInitialPosition;
    private Vector2 dragInitialLineStart;
    private Vector2 dragInitialLineEnd;
    private readonly List<Vector2> dragInitialPolylinePoints = new();
    private Vector2 dragInitialPolylineCenter;
    private float dragInitialPolylineHandleDistance;
    private int activePolylinePointIndex = -1;
    private float dragInitialSize;

    [MenuItem("Tools/魔法陣エディタ")]
    private static void Open()
    {
        var window = GetWindow<TraceMagicCircleEditorWindow>();
        window.titleContent = new GUIContent("魔法陣エディタ");
        window.minSize = new Vector2(820f, 520f);
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnGUI()
    {
        EnsureSerializedObject();
        DrawAssetToolbar();
        if (definition == null)
        {
            EditorGUILayout.HelpBox(
                "編集する魔法陣アセットを選択するか、新規作成してください。",
                MessageType.Info);
            return;
        }

        EnsureSerializedObject();
        serializedDefinition.Update();

        EditorGUILayout.BeginHorizontal();
        DrawPalette();
        DrawPreview();
        DrawShapeInspector();
        EditorGUILayout.EndHorizontal();

        if (serializedDefinition.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(definition);
            Repaint();
        }
    }

    private void DrawAssetToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUI.BeginChangeCheck();
        var selectedDefinition = (TraceMagicCircleDefinition)EditorGUILayout.ObjectField(
            definition,
            typeof(TraceMagicCircleDefinition),
            false,
            GUILayout.ExpandWidth(true));
        if (EditorGUI.EndChangeCheck())
        {
            SetDefinition(selectedDefinition);
        }

        if (GUILayout.Button("新規作成", EditorStyles.toolbarButton, GUILayout.Width(80f)))
        {
            CreateDefinitionAsset();
        }
        using (new EditorGUI.DisabledScope(definition == null))
        {
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                serializedDefinition.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(definition);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (definition == null)
        {
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(displayNameProperty, new GUIContent("表示名"));
        EditorGUILayout.PropertyField(basePowerProperty, new GUIContent("基礎威力"), GUILayout.Width(180f));
        EditorGUILayout.PropertyField(drawCountProperty, new GUIContent("描画回数"), GUILayout.Width(180f));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4f);
    }

    private void DrawPalette()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(PaletteWidth));
        EditorGUILayout.LabelField("図形", EditorStyles.boldLabel);
        for (var index = 0; index < PalettePatterns.Length; index++)
        {
            var pattern = PalettePatterns[index];
            if (GUILayout.Button(GetPatternName(pattern), GUILayout.Height(34f)))
            {
                AddShape(pattern);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPreview()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("プレビュー", EditorStyles.boldLabel);
        var previewRect = GUILayoutUtility.GetRect(
            300f,
            10000f,
            380f,
            10000f,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true));

        EditorGUI.DrawRect(previewRect, new Color(0.055f, 0.08f, 0.12f));
        DrawGrid(previewRect);

        for (var index = 0; index < definition.Shapes.Count; index++)
        {
            var shape = definition.Shapes[index];
            if (shape == null)
            {
                continue;
            }

            var color = index == selectedShapeIndex
                ? new Color(1f, 0.78f, 0.25f)
                : shape.IsDisplayed
                    ? new Color(0.18f, 0.89f, 0.9f)
                    : new Color(0.5f, 0.54f, 0.58f);
            DrawShape(previewRect, shape, color, index == selectedShapeIndex ? 3f : 2f);
        }

        DrawSelectedShapeHandles(previewRect);
        ProcessPreviewInput(previewRect);

        EditorGUILayout.HelpBox(
            "輪郭クリック: 選択 / 黄: 移動 / 緑: 拡縮 / 青: 長方形比率 / 桃: 直線端点・折れ線頂点\n" +
            "水色線: ゲーム中に表示 / 灰色線: 保持のみ / 黄色線: 選択中",
            MessageType.None);
        EditorGUILayout.EndVertical();
    }

    private void DrawShapeInspector()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(InspectorWidth));
        EditorGUILayout.LabelField("使用図形", EditorStyles.boldLabel);

        shapeListScroll = EditorGUILayout.BeginScrollView(shapeListScroll, GUILayout.Height(220f));
        for (var index = 0; index < shapesProperty.arraySize; index++)
        {
            var shapeProperty = shapesProperty.GetArrayElementAtIndex(index);
            var pattern = (TracePattern)shapeProperty.FindPropertyRelative("pattern").enumValueIndex;
            var usage = (TraceMagicCircleShapeUsage)shapeProperty.FindPropertyRelative("usage").enumValueIndex;
            var label = $"{index + 1}. {GetPatternName(pattern)}";
            if (usage == TraceMagicCircleShapeUsage.StoredOnly)
            {
                label += "（保持のみ）";
            }

            if (GUILayout.Toggle(selectedShapeIndex == index, label, "Button"))
            {
                selectedShapeIndex = index;
            }
        }
        EditorGUILayout.EndScrollView();

        if (!HasSelectedShape())
        {
            EditorGUILayout.HelpBox("図形を追加または選択してください。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("選択中の図形", EditorStyles.boldLabel);
        var selectedShape = shapesProperty.GetArrayElementAtIndex(selectedShapeIndex);
        EditorGUILayout.PropertyField(
            selectedShape.FindPropertyRelative("pattern"),
            new GUIContent("種類"));

        var selectedPattern = (TracePattern)selectedShape
            .FindPropertyRelative("pattern")
            .enumValueIndex;
        if (selectedPattern == TracePattern.Line)
        {
            EditorGUILayout.PropertyField(
                selectedShape.FindPropertyRelative("lineStart"),
                new GUIContent("始点"));
            EditorGUILayout.PropertyField(
                selectedShape.FindPropertyRelative("lineEnd"),
                new GUIContent("終点"));
        }
        else if (selectedPattern == TracePattern.Polyline)
        {
            DrawPolylineInspector(selectedShape.FindPropertyRelative("polylinePoints"));
        }
        else
        {
            EditorGUILayout.PropertyField(
                selectedShape.FindPropertyRelative("position"),
                new GUIContent("位置"));
            EditorGUILayout.PropertyField(
                selectedShape.FindPropertyRelative("size"),
                new GUIContent("大きさ"));

            if (selectedPattern == TracePattern.Rectangle)
            {
                EditorGUILayout.PropertyField(
                    selectedShape.FindPropertyRelative("rectangleAspectRatio"),
                    new GUIContent("縦横比（横 ÷ 高さ）"));
            }
        }

        var usageProperty = selectedShape.FindPropertyRelative("usage");
        usageProperty.enumValueIndex = EditorGUILayout.Popup(
            "用途",
            usageProperty.enumValueIndex,
            new[] { "ゲーム中に表示", "保持のみ" });

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("選択中の図形を削除"))
        {
            DeleteSelectedShape();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPolylineInspector(SerializedProperty pointsProperty)
    {
        EditorGUILayout.LabelField("頂点", EditorStyles.boldLabel);
        var removeIndex = -1;
        for (var index = 0; index < pointsProperty.arraySize; index++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(
                pointsProperty.GetArrayElementAtIndex(index),
                new GUIContent($"頂点 {index + 1}"));
            using (new EditorGUI.DisabledScope(pointsProperty.arraySize <= 2))
            {
                if (GUILayout.Button("削除", GUILayout.Width(48f)))
                {
                    removeIndex = index;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
        {
            Undo.RecordObject(definition, "折れ線の頂点を削除");
            pointsProperty.DeleteArrayElementAtIndex(removeIndex);
        }

        if (GUILayout.Button("頂点を追加"))
        {
            Undo.RecordObject(definition, "折れ線に頂点を追加");
            if (pointsProperty.arraySize < 2)
            {
                SetDefaultPolylinePoints(pointsProperty);
            }
            else
            {
                var lastIndex = pointsProperty.arraySize - 1;
                var lastPoint = pointsProperty.GetArrayElementAtIndex(lastIndex).vector2Value;
                var previousPoint = pointsProperty.GetArrayElementAtIndex(lastIndex - 1).vector2Value;
                var direction = lastPoint - previousPoint;
                if (direction.sqrMagnitude <= Mathf.Epsilon)
                {
                    direction = Vector2.right;
                }

                pointsProperty.InsertArrayElementAtIndex(pointsProperty.arraySize);
                pointsProperty.GetArrayElementAtIndex(pointsProperty.arraySize - 1).vector2Value =
                    lastPoint + direction;
            }
        }
    }

    private void DrawGrid(Rect rect)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.08f);
        for (var coordinate = -4; coordinate <= 4; coordinate++)
        {
            var horizontalStart = WorldToPreview(rect, new Vector2(-PreviewExtent, coordinate));
            var horizontalEnd = WorldToPreview(rect, new Vector2(PreviewExtent, coordinate));
            Handles.DrawLine(horizontalStart, horizontalEnd);

            var verticalStart = WorldToPreview(rect, new Vector2(coordinate, -PreviewExtent));
            var verticalEnd = WorldToPreview(rect, new Vector2(coordinate, PreviewExtent));
            Handles.DrawLine(verticalStart, verticalEnd);
        }
    }

    private void DrawShape(Rect rect, TraceMagicCircleShape shape, Color color, float width)
    {
        var points = CreateShapePoints(shape);
        var isClosed = TracePatternPointFactory.IsClosed(shape.Pattern);
        var previewPoints = new Vector3[points.Count + (isClosed ? 1 : 0)];
        for (var index = 0; index < points.Count; index++)
        {
            previewPoints[index] = WorldToPreview(rect, points[index]);
        }
        if (isClosed)
        {
            previewPoints[^1] = previewPoints[0];
        }

        Handles.color = color;
        Handles.DrawAAPolyLine(width, previewPoints);
    }

    private static List<Vector2> CreateShapePoints(
        TraceMagicCircleShape shape)
    {
        return TracePatternPointFactory.Create(
            shape.Pattern,
            128,
            shape.Position,
            shape.Size,
            shape.RectangleAspectRatio,
            shape.LineStart,
            shape.LineEnd,
            shape.PolylinePoints);
    }

    private static Vector2 WorldToPreview(Rect rect, Vector2 point)
    {
        var scale = Mathf.Min(rect.width, rect.height) / (PreviewExtent * 2f);
        return new Vector2(
            rect.center.x + point.x * scale,
            rect.center.y - point.y * scale);
    }

    private static Vector2 PreviewToWorld(Rect rect, Vector2 point)
    {
        var scale = Mathf.Min(rect.width, rect.height) / (PreviewExtent * 2f);
        return new Vector2(
            (point.x - rect.center.x) / scale,
            (rect.center.y - point.y) / scale);
    }

    private void DrawSelectedShapeHandles(Rect rect)
    {
        if (!HasSelectedShape() || selectedShapeIndex >= definition.Shapes.Count)
        {
            return;
        }

        var shape = definition.Shapes[selectedShapeIndex];
        if (shape == null)
        {
            return;
        }

        if (shape.Pattern == TracePattern.Line)
        {
            var center = (shape.LineStart + shape.LineEnd) * 0.5f;
            DrawHandle(rect, center, new Color(1f, 0.78f, 0.25f));
            DrawHandle(rect, shape.LineStart, new Color(1f, 0.35f, 0.65f));
            DrawHandle(rect, shape.LineEnd, new Color(1f, 0.35f, 0.65f));
            return;
        }

        if (shape.Pattern == TracePattern.Polyline)
        {
            var points = shape.PolylinePoints;
            if (points == null || points.Count < 2)
            {
                return;
            }

            var center = GetPolylineCenter(points);
            var radius = GetPolylineRadius(points, center);
            DrawHandle(rect, center, new Color(1f, 0.78f, 0.25f));
            DrawHandle(
                rect,
                center + Vector2.right * (radius + 0.35f),
                new Color(0.3f, 0.95f, 0.45f));
            for (var index = 0; index < points.Count; index++)
            {
                DrawHandle(rect, points[index], new Color(1f, 0.35f, 0.65f));
            }
            return;
        }

        DrawHandle(rect, shape.Position, new Color(1f, 0.78f, 0.25f));
        var scaleHandle = shape.Pattern == TracePattern.Rectangle
            ? shape.Position + Vector2.up * shape.Size
            : shape.Position + Vector2.right * shape.Size;
        DrawHandle(rect, scaleHandle, new Color(0.3f, 0.95f, 0.45f));

        if (shape.Pattern == TracePattern.Rectangle)
        {
            var ratioHandle = shape.Position +
                Vector2.right * shape.Size * shape.RectangleAspectRatio;
            DrawHandle(rect, ratioHandle, new Color(0.25f, 0.65f, 1f));
        }
    }

    private static void DrawHandle(Rect rect, Vector2 worldPosition, Color color)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        var previewPosition = WorldToPreview(rect, worldPosition);
        var outerRect = new Rect(
            previewPosition.x - HandleSize * 0.5f,
            previewPosition.y - HandleSize * 0.5f,
            HandleSize,
            HandleSize);
        EditorGUI.DrawRect(outerRect, Color.black);
        EditorGUI.DrawRect(new Rect(
            outerRect.x + 2f,
            outerRect.y + 2f,
            outerRect.width - 4f,
            outerRect.height - 4f), color);
    }

    private void ProcessPreviewInput(Rect rect)
    {
        var currentEvent = Event.current;
        var controlId = GUIUtility.GetControlID(PreviewControlHint, FocusType.Passive, rect);

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 &&
            rect.Contains(currentEvent.mousePosition))
        {
            if (TryGetDragMode(rect, currentEvent.mousePosition, out var dragMode))
            {
                BeginPreviewDrag(
                    dragMode,
                    PreviewToWorld(rect, currentEvent.mousePosition),
                    controlId);
            }
            else
            {
                selectedShapeIndex = FindShapeAtPosition(rect, currentEvent.mousePosition);
                Repaint();
            }

            currentEvent.Use();
            return;
        }

        if (GUIUtility.hotControl != controlId)
        {
            return;
        }

        if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)
        {
            ApplyPreviewDrag(PreviewToWorld(rect, currentEvent.mousePosition));
            currentEvent.Use();
        }
        else if (currentEvent.rawType == EventType.MouseUp)
        {
            previewDragMode = PreviewDragMode.None;
            activePolylinePointIndex = -1;
            GUIUtility.hotControl = 0;
            currentEvent.Use();
        }
    }

    private bool TryGetDragMode(Rect rect, Vector2 mousePosition, out PreviewDragMode dragMode)
    {
        dragMode = PreviewDragMode.None;
        if (!HasSelectedShape() || selectedShapeIndex >= definition.Shapes.Count)
        {
            return false;
        }

        var shape = definition.Shapes[selectedShapeIndex];
        if (shape == null)
        {
            return false;
        }

        if (shape.Pattern == TracePattern.Line)
        {
            if (IsNearHandle(rect, mousePosition, shape.LineStart))
            {
                dragMode = PreviewDragMode.LineStart;
                return true;
            }
            if (IsNearHandle(rect, mousePosition, shape.LineEnd))
            {
                dragMode = PreviewDragMode.LineEnd;
                return true;
            }

            var center = (shape.LineStart + shape.LineEnd) * 0.5f;
            if (IsNearHandle(rect, mousePosition, center))
            {
                dragMode = PreviewDragMode.Move;
                return true;
            }
            return false;
        }

        if (shape.Pattern == TracePattern.Polyline)
        {
            var points = shape.PolylinePoints;
            if (points == null || points.Count < 2)
            {
                return false;
            }

            for (var index = 0; index < points.Count; index++)
            {
                if (IsNearHandle(rect, mousePosition, points[index]))
                {
                    activePolylinePointIndex = index;
                    dragMode = PreviewDragMode.PolylinePoint;
                    return true;
                }
            }

            var center = GetPolylineCenter(points);
            var radius = GetPolylineRadius(points, center);
            var polylineScaleHandle = center + Vector2.right * (radius + 0.35f);
            if (IsNearHandle(rect, mousePosition, polylineScaleHandle))
            {
                dragMode = PreviewDragMode.Scale;
                return true;
            }
            if (IsNearHandle(rect, mousePosition, center))
            {
                dragMode = PreviewDragMode.Move;
                return true;
            }
            return false;
        }

        var scaleHandle = shape.Pattern == TracePattern.Rectangle
            ? shape.Position + Vector2.up * shape.Size
            : shape.Position + Vector2.right * shape.Size;
        if (IsNearHandle(rect, mousePosition, scaleHandle))
        {
            dragMode = PreviewDragMode.Scale;
            return true;
        }

        if (shape.Pattern == TracePattern.Rectangle)
        {
            var ratioHandle = shape.Position +
                Vector2.right * shape.Size * shape.RectangleAspectRatio;
            if (IsNearHandle(rect, mousePosition, ratioHandle))
            {
                dragMode = PreviewDragMode.RectangleAspectRatio;
                return true;
            }
        }

        if (IsNearHandle(rect, mousePosition, shape.Position))
        {
            dragMode = PreviewDragMode.Move;
            return true;
        }
        return false;
    }

    private static bool IsNearHandle(Rect rect, Vector2 mousePosition, Vector2 worldPosition)
    {
        return Vector2.Distance(mousePosition, WorldToPreview(rect, worldPosition)) <= HitDistance;
    }

    private void BeginPreviewDrag(
        PreviewDragMode dragMode,
        Vector2 mouseWorldPosition,
        int controlId)
    {
        serializedDefinition.ApplyModifiedProperties();
        Undo.RecordObject(definition, "魔法陣の図形を直接編集");
        serializedDefinition.Update();

        var selectedShape = shapesProperty.GetArrayElementAtIndex(selectedShapeIndex);
        previewDragMode = dragMode;
        dragStartWorld = mouseWorldPosition;
        dragInitialPosition = selectedShape.FindPropertyRelative("position").vector2Value;
        dragInitialSize = selectedShape.FindPropertyRelative("size").floatValue;
        dragInitialLineStart = selectedShape.FindPropertyRelative("lineStart").vector2Value;
        dragInitialLineEnd = selectedShape.FindPropertyRelative("lineEnd").vector2Value;
        dragInitialPolylinePoints.Clear();
        var polylinePoints = selectedShape.FindPropertyRelative("polylinePoints");
        for (var index = 0; index < polylinePoints.arraySize; index++)
        {
            dragInitialPolylinePoints.Add(polylinePoints.GetArrayElementAtIndex(index).vector2Value);
        }
        dragInitialPolylineCenter = GetPolylineCenter(dragInitialPolylinePoints);
        dragInitialPolylineHandleDistance = Mathf.Max(
            0.01f,
            Vector2.Distance(mouseWorldPosition, dragInitialPolylineCenter));
        GUIUtility.hotControl = controlId;
        GUIUtility.keyboardControl = 0;
    }

    private void ApplyPreviewDrag(Vector2 mouseWorldPosition)
    {
        if (!HasSelectedShape())
        {
            return;
        }

        serializedDefinition.Update();
        var selectedShape = shapesProperty.GetArrayElementAtIndex(selectedShapeIndex);
        var delta = mouseWorldPosition - dragStartWorld;
        var pattern = (TracePattern)selectedShape.FindPropertyRelative("pattern").enumValueIndex;

        switch (previewDragMode)
        {
            case PreviewDragMode.Move:
                if (pattern == TracePattern.Line)
                {
                    selectedShape.FindPropertyRelative("lineStart").vector2Value =
                        dragInitialLineStart + delta;
                    selectedShape.FindPropertyRelative("lineEnd").vector2Value =
                        dragInitialLineEnd + delta;
                }
                else if (pattern == TracePattern.Polyline)
                {
                    SetPolylinePoints(
                        selectedShape.FindPropertyRelative("polylinePoints"),
                        dragInitialPolylinePoints,
                        delta,
                        1f);
                }
                else
                {
                    selectedShape.FindPropertyRelative("position").vector2Value =
                        dragInitialPosition + delta;
                }
                break;
            case PreviewDragMode.Scale:
                if (pattern == TracePattern.Polyline)
                {
                    var scale = Mathf.Max(
                        0.01f,
                        Vector2.Distance(mouseWorldPosition, dragInitialPolylineCenter) /
                        dragInitialPolylineHandleDistance);
                    SetPolylinePoints(
                        selectedShape.FindPropertyRelative("polylinePoints"),
                        dragInitialPolylinePoints,
                        Vector2.zero,
                        scale);
                    break;
                }
                var size = pattern == TracePattern.Rectangle
                    ? Mathf.Abs(mouseWorldPosition.y - dragInitialPosition.y)
                    : Vector2.Distance(mouseWorldPosition, dragInitialPosition);
                selectedShape.FindPropertyRelative("size").floatValue = Mathf.Max(0.01f, size);
                break;
            case PreviewDragMode.RectangleAspectRatio:
                var halfWidth = Mathf.Abs(mouseWorldPosition.x - dragInitialPosition.x);
                selectedShape.FindPropertyRelative("rectangleAspectRatio").floatValue =
                    Mathf.Max(0.1f, halfWidth / Mathf.Max(0.01f, dragInitialSize));
                break;
            case PreviewDragMode.LineStart:
                selectedShape.FindPropertyRelative("lineStart").vector2Value = mouseWorldPosition;
                break;
            case PreviewDragMode.LineEnd:
                selectedShape.FindPropertyRelative("lineEnd").vector2Value = mouseWorldPosition;
                break;
            case PreviewDragMode.PolylinePoint:
                var points = selectedShape.FindPropertyRelative("polylinePoints");
                if (activePolylinePointIndex >= 0 &&
                    activePolylinePointIndex < points.arraySize)
                {
                    points.GetArrayElementAtIndex(activePolylinePointIndex).vector2Value =
                        mouseWorldPosition;
                }
                break;
        }

        serializedDefinition.ApplyModifiedProperties();
        EditorUtility.SetDirty(definition);
        Repaint();
    }

    private int FindShapeAtPosition(Rect rect, Vector2 mousePosition)
    {
        for (var shapeIndex = definition.Shapes.Count - 1; shapeIndex >= 0; shapeIndex--)
        {
            var shape = definition.Shapes[shapeIndex];
            if (shape == null)
            {
                continue;
            }

            var points = CreateShapePoints(shape);
            var segmentCount = TracePatternPointFactory.IsClosed(shape.Pattern)
                ? points.Count
                : points.Count - 1;
            for (var pointIndex = 0; pointIndex < segmentCount; pointIndex++)
            {
                var start = WorldToPreview(rect, points[pointIndex]);
                var end = WorldToPreview(rect, points[(pointIndex + 1) % points.Count]);
                if (DistanceToSegment(mousePosition, start, end) <= HitDistance)
                {
                    return shapeIndex;
                }
            }
        }
        return -1;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        var segment = end - start;
        var lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= Mathf.Epsilon)
        {
            return Vector2.Distance(point, start);
        }

        var progress = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return Vector2.Distance(point, start + segment * progress);
    }

    private void AddShape(TracePattern pattern)
    {
        Undo.RecordObject(definition, "魔法陣に図形を追加");
        var newIndex = shapesProperty.arraySize;
        shapesProperty.InsertArrayElementAtIndex(newIndex);
        var shape = shapesProperty.GetArrayElementAtIndex(newIndex);
        shape.FindPropertyRelative("pattern").enumValueIndex = (int)pattern;
        shape.FindPropertyRelative("position").vector2Value = Vector2.zero;
        shape.FindPropertyRelative("size").floatValue = 2.05f;
        shape.FindPropertyRelative("rectangleAspectRatio").floatValue = 1.5f;
        shape.FindPropertyRelative("lineStart").vector2Value = new Vector2(-1f, 0f);
        shape.FindPropertyRelative("lineEnd").vector2Value = new Vector2(1f, 0f);
        SetDefaultPolylinePoints(shape.FindPropertyRelative("polylinePoints"));
        shape.FindPropertyRelative("usage").enumValueIndex = (int)TraceMagicCircleShapeUsage.Display;
        selectedShapeIndex = newIndex;
        serializedDefinition.ApplyModifiedProperties();
        EditorUtility.SetDirty(definition);
    }

    private void DeleteSelectedShape()
    {
        Undo.RecordObject(definition, "魔法陣から図形を削除");
        shapesProperty.DeleteArrayElementAtIndex(selectedShapeIndex);
        selectedShapeIndex = Mathf.Min(selectedShapeIndex, shapesProperty.arraySize - 1);
        serializedDefinition.ApplyModifiedProperties();
        EditorUtility.SetDirty(definition);
    }

    private bool HasSelectedShape()
    {
        return selectedShapeIndex >= 0 && selectedShapeIndex < shapesProperty.arraySize;
    }

    private void SetDefinition(TraceMagicCircleDefinition selectedDefinition)
    {
        definition = selectedDefinition;
        serializedDefinition = null;
        selectedShapeIndex = -1;
        activePolylinePointIndex = -1;
        EnsureSerializedObject();
    }

    private void EnsureSerializedObject()
    {
        if (definition == null || serializedDefinition != null && serializedDefinition.targetObject == definition)
        {
            return;
        }

        serializedDefinition = new SerializedObject(definition);
        displayNameProperty = serializedDefinition.FindProperty("displayName");
        basePowerProperty = serializedDefinition.FindProperty("basePower");
        drawCountProperty = serializedDefinition.FindProperty("drawCount");
        shapesProperty = serializedDefinition.FindProperty("shapes");
    }

    private void CreateDefinitionAsset()
    {
        var path = EditorUtility.SaveFilePanelInProject(
            "魔法陣を新規作成",
            "TraceMagicCircle",
            "asset",
            "保存先を選択してください。");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var asset = CreateInstance<TraceMagicCircleDefinition>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
        SetDefinition(asset);
    }

    private void OnUndoRedo()
    {
        if (definition != null)
        {
            serializedDefinition = null;
            EnsureSerializedObject();
            selectedShapeIndex = Mathf.Min(selectedShapeIndex, shapesProperty.arraySize - 1);
        }
        Repaint();
    }

    private static string GetPatternName(TracePattern pattern)
    {
        return pattern switch
        {
            TracePattern.Circle => "○ 円形",
            TracePattern.UprightTriangle => "△ 三角形",
            TracePattern.InvertedTriangle => "▽ 逆三角形",
            TracePattern.Square => "□ 四角形",
            TracePattern.Diamond => "◇ 菱形",
            TracePattern.Hexagon => "⬡ 六角形",
            TracePattern.Star => "☆ 星型",
            TracePattern.Rectangle => "▭ 長方形",
            TracePattern.LeftTriangle => "◁ 左向き三角形",
            TracePattern.RightTriangle => "▷ 右向き三角形",
            TracePattern.HorizontalHexagon => "⬡ 横六角形",
            TracePattern.Line => "／ 直線",
            TracePattern.Polyline => "⌁ 折れ線",
            _ => pattern.ToString(),
        };
    }

    private static Vector2 GetPolylineCenter(IReadOnlyList<Vector2> points)
    {
        if (points == null || points.Count == 0)
        {
            return Vector2.zero;
        }

        var center = Vector2.zero;
        for (var index = 0; index < points.Count; index++)
        {
            center += points[index];
        }
        return center / points.Count;
    }

    private static float GetPolylineRadius(IReadOnlyList<Vector2> points, Vector2 center)
    {
        var radius = 0f;
        for (var index = 0; index < points.Count; index++)
        {
            radius = Mathf.Max(radius, Vector2.Distance(center, points[index]));
        }
        return radius;
    }

    private static void SetPolylinePoints(
        SerializedProperty pointsProperty,
        IReadOnlyList<Vector2> initialPoints,
        Vector2 offset,
        float scale)
    {
        var center = GetPolylineCenter(initialPoints);
        pointsProperty.arraySize = initialPoints.Count;
        for (var index = 0; index < initialPoints.Count; index++)
        {
            pointsProperty.GetArrayElementAtIndex(index).vector2Value =
                center + (initialPoints[index] - center) * scale + offset;
        }
    }

    private static void SetDefaultPolylinePoints(SerializedProperty pointsProperty)
    {
        pointsProperty.arraySize = 3;
        pointsProperty.GetArrayElementAtIndex(0).vector2Value = new Vector2(-1f, 0f);
        pointsProperty.GetArrayElementAtIndex(1).vector2Value = new Vector2(0f, 1f);
        pointsProperty.GetArrayElementAtIndex(2).vector2Value = new Vector2(1f, 0f);
    }
}
