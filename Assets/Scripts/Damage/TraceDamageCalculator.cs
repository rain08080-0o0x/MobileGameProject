using System;
using System.Collections.Generic;
using UnityEngine;

public static class TraceDamageCalculator
{
    public static TraceDamageResult CalculateDamage(
        TraceMagicCircleDefinition magicCircle,
        TraceDamageSettings settings,
        IReadOnlyList<float> accuracyPercentages,
        IReadOnlyList<float> drawingSeconds)
    {
        if (magicCircle == null)
        {
            throw new ArgumentNullException(nameof(magicCircle));
        }

        return CalculateDamage(magicCircle.BasePower, settings, accuracyPercentages, drawingSeconds);
    }

    public static TraceDamageResult CalculateDamage(TraceDamageInput input, TraceDamageSettings settings)
    {
        if (input.MagicCircle == null)
        {
            throw new ArgumentNullException(nameof(input), "Magic circle is required.");
        }

        return CalculateDamage(
            input.MagicCircle.BasePower,
            settings,
            input.AccuracyPercentages,
            input.DrawingSeconds);
    }

    public static TraceDamageResult CalculateDamage(
        int basePower,
        TraceDamageSettings settings,
        IReadOnlyList<float> accuracyPercentages,
        IReadOnlyList<float> drawingSeconds)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (accuracyPercentages == null)
        {
            throw new ArgumentNullException(nameof(accuracyPercentages));
        }

        if (drawingSeconds == null)
        {
            throw new ArgumentNullException(nameof(drawingSeconds));
        }

        if (accuracyPercentages.Count != drawingSeconds.Count)
        {
            throw new ArgumentException("Accuracy and drawing-time counts must match.");
        }

        if (accuracyPercentages.Count == 0)
        {
            return new TraceDamageResult(basePower, 0f, 0f, 0, 0f, 0f, 0f, 0);
        }

        var accuracyTotal = 0f;
        var drawingTimeTotal = 0f;
        for (var index = 0; index < accuracyPercentages.Count; index++)
        {
            accuracyTotal += Mathf.Clamp(accuracyPercentages[index], 0f, 100f);
            drawingTimeTotal += Mathf.Max(0f, drawingSeconds[index]);
        }

        var completedShapeCount = accuracyPercentages.Count;
        var averageAccuracy = accuracyTotal / completedShapeCount;
        var averageDrawingSeconds = drawingTimeTotal / completedShapeCount;
        var accuracyMultiplier = averageAccuracy * settings.AccuracyPercentScale;
        var countMultiplier = completedShapeCount * settings.CountMultiplierPerShape;
        var tempoMultiplier = ResolveTempoMultiplier(averageDrawingSeconds, settings);
        var damage = basePower * accuracyMultiplier * countMultiplier * tempoMultiplier;

        return new TraceDamageResult(
            basePower,
            averageAccuracy,
            accuracyMultiplier,
            completedShapeCount,
            countMultiplier,
            averageDrawingSeconds,
            tempoMultiplier,
            Mathf.Max(0, Mathf.RoundToInt(damage)));
    }

    private static float ResolveTempoMultiplier(float averageDrawingSeconds, TraceDamageSettings settings)
    {
        if (averageDrawingSeconds <= settings.FastTempoMaxSeconds)
        {
            return settings.FastTempoMultiplier;
        }

        if (averageDrawingSeconds <= settings.NormalTempoMaxSeconds)
        {
            return settings.NormalTempoMultiplier;
        }

        return settings.SlowTempoMultiplier;
    }
}
