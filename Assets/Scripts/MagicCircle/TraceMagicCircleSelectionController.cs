using System;
using UnityEngine;

public sealed class TraceMagicCircleSelectionController : MonoBehaviour
{
    [SerializeField] private TraceMagicCircleDefinition[] magicCircles;

    public event Action<TraceMagicCircleDefinition> MagicCircleSelected;

    public bool SelectionEnabled { get; private set; }

    public void SetSelectionEnabled(bool enabled)
    {
        SelectionEnabled = enabled;
    }

    public void SelectMagicCircle(TraceMagicCircleDefinition magicCircle)
    {
        if (SelectionEnabled && magicCircle != null)
        {
            MagicCircleSelected?.Invoke(magicCircle);
        }
    }

    private void OnGUI()
    {
        if (!SelectionEnabled || magicCircles == null)
        {
            return;
        }

        var width = Mathf.Min(620f, Screen.width - 32f);
        var height = Mathf.Min(360f, Screen.height - 32f);
        GUILayout.BeginArea(
            new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height),
            GUI.skin.window);
        GUILayout.Label("描く魔法陣を選択");

        for (var index = 0; index < magicCircles.Length; index++)
        {
            var magicCircle = magicCircles[index];
            if (magicCircle == null)
            {
                continue;
            }

            if (GUILayout.Button(
                    $"{GetPatternSymbols(magicCircle)}  {magicCircle.DisplayName}    " +
                    $"基礎威力 {magicCircle.BasePower}    {magicCircle.DrawCount}個描画",
                    GUILayout.Height(64f)))
            {
                SelectMagicCircle(magicCircle);
            }
        }

        GUILayout.EndArea();
    }

    private static string GetPatternSymbols(TraceMagicCircleDefinition magicCircle)
    {
        var symbols = string.Empty;
        for (var index = 0; index < magicCircle.Shapes.Count; index++)
        {
            var shape = magicCircle.Shapes[index];
            if (shape != null && shape.IsDisplayed)
            {
                symbols += GetPatternSymbol(shape.Pattern);
            }
        }
        return symbols;
    }

    private static string GetPatternSymbol(TracePattern pattern)
    {
        return pattern switch
        {
            TracePattern.UprightTriangle => "△",
            TracePattern.InvertedTriangle => "▽",
            TracePattern.Circle => "○",
            TracePattern.Square => "□",
            TracePattern.Diamond => "◇",
            TracePattern.Hexagon => "⬡",
            TracePattern.Star => "☆",
            _ => string.Empty,
        };
    }
}
