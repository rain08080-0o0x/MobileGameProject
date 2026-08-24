using UnityEditor;
using UnityEngine;

public sealed class TraceMagicCircleEditorWindow : EditorWindow
{
    private const float PaletteWidth = 150f;
    private const float InspectorWidth = 280f;
    private const float PreviewExtent = 5f;

    private static readonly TracePattern[] PalettePatterns =
    {
        TracePattern.Circle,
        TracePattern.UprightTriangle,
        TracePattern.InvertedTriangle,
        TracePattern.Square,
        TracePattern.Diamond,
        TracePattern.Hexagon,
        TracePattern.Star,
    };

    private TraceMagicCircleDefinition definition;
    private SerializedObject serializedDefinition;
    private SerializedProperty displayNameProperty;
    private SerializedProperty basePowerProperty;
    private SerializedProperty drawCountProperty;
    private SerializedProperty shapesProperty;
    private Vector2 shapeListScroll;
    private int selectedShapeIndex = -1;

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

        EditorGUILayout.HelpBox(
            "水色: ゲーム中に表示 / 灰色: 保持のみ / 黄色: 選択中",
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
        EditorGUILayout.PropertyField(
            selectedShape.FindPropertyRelative("position"),
            new GUIContent("位置"));
        EditorGUILayout.PropertyField(
            selectedShape.FindPropertyRelative("size"),
            new GUIContent("大きさ"));

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
        var points = TracePatternPointFactory.Create(
            shape.Pattern,
            128,
            shape.Position,
            shape.Size);
        var previewPoints = new Vector3[points.Count + 1];
        for (var index = 0; index < points.Count; index++)
        {
            previewPoints[index] = WorldToPreview(rect, points[index]);
        }
        previewPoints[^1] = previewPoints[0];

        Handles.color = color;
        Handles.DrawAAPolyLine(width, previewPoints);
    }

    private static Vector2 WorldToPreview(Rect rect, Vector2 point)
    {
        var scale = Mathf.Min(rect.width, rect.height) / (PreviewExtent * 2f);
        return new Vector2(
            rect.center.x + point.x * scale,
            rect.center.y - point.y * scale);
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
            _ => pattern.ToString(),
        };
    }
}
