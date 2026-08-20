using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class TraceGlyphAttack : MonoBehaviour
{
    private Vector3[] originalPositions;
    private Vector3 originalCenter;
    private LineRenderer glyphRenderer;

    public void Initialize(IReadOnlyList<Vector2> points, float width, Color color)
    {
        glyphRenderer = GetComponent<LineRenderer>();
        glyphRenderer.useWorldSpace = true;
        glyphRenderer.widthMultiplier = width;
        glyphRenderer.startColor = color;
        glyphRenderer.endColor = color;
        glyphRenderer.numCapVertices = 12;
        glyphRenderer.numCornerVertices = 8;
        glyphRenderer.sortingOrder = 4;
        glyphRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
        {
            name = "Runtime Trace Attack Material",
        };

        originalPositions = new Vector3[points.Count];
        originalCenter = Vector3.zero;
        for (var index = 0; index < points.Count; index++)
        {
            originalPositions[index] = new Vector3(points[index].x, points[index].y, -0.1f);
            originalCenter += originalPositions[index];
        }

        if (originalPositions.Length > 0)
        {
            originalCenter /= originalPositions.Length;
        }

        glyphRenderer.positionCount = originalPositions.Length;
        glyphRenderer.SetPositions(originalPositions);
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        glyphRenderer.enabled = visible;
    }

    public void SetFlightProgress(Vector3 target, float progress)
    {
        var easedProgress = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress), 3f);
        var currentCenter = Vector3.Lerp(originalCenter, target, easedProgress);
        var scale = Mathf.Lerp(1f, 0.12f, easedProgress);

        for (var index = 0; index < originalPositions.Length; index++)
        {
            var offset = (originalPositions[index] - originalCenter) * scale;
            glyphRenderer.SetPosition(index, currentCenter + offset);
        }
    }
}
