using System.Collections.Generic;
using UnityEngine;

public static class TraceScorer
{
    private const float CoverageWeight = 0.6f;
    private const float PrecisionWeight = 0.4f;

    public static float Calculate(
        IReadOnlyList<Vector2> targetPoints,
        bool targetIsClosed,
        IReadOnlyList<Vector2> strokePoints,
        float toleranceRadius)
    {
        if (targetPoints == null || targetPoints.Count < 2 ||
            strokePoints == null || strokePoints.Count < 2 ||
            toleranceRadius <= 0f)
        {
            return 0f;
        }

        var sampleSpacing = Mathf.Max(0.025f, toleranceRadius * 0.2f);
        var targetSamples = Resample(targetPoints, targetIsClosed, sampleSpacing);
        var strokeSamples = Resample(strokePoints, false, sampleSpacing);

        var coverage = AverageCloseness(targetSamples, strokePoints, false, toleranceRadius);
        var precision = AverageCloseness(strokeSamples, targetPoints, targetIsClosed, toleranceRadius);

        return Mathf.Clamp01(coverage * CoverageWeight + precision * PrecisionWeight) * 100f;
    }

    private static float AverageCloseness(
        IReadOnlyList<Vector2> samples,
        IReadOnlyList<Vector2> comparisonLine,
        bool comparisonIsClosed,
        float toleranceRadius)
    {
        if (samples.Count == 0)
        {
            return 0f;
        }

        var total = 0f;
        for (var index = 0; index < samples.Count; index++)
        {
            var distance = DistanceToPolyline(samples[index], comparisonLine, comparisonIsClosed);
            total += Mathf.Clamp01(1f - distance / toleranceRadius);
        }

        return total / samples.Count;
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
            minimum = Mathf.Min(minimum, DistanceToSegment(point, start, end));
        }

        return minimum;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        var segment = end - start;
        var lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= Mathf.Epsilon)
        {
            return Vector2.Distance(point, start);
        }

        var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return Vector2.Distance(point, start + segment * t);
    }

    private static List<Vector2> Resample(
        IReadOnlyList<Vector2> source,
        bool isClosed,
        float spacing)
    {
        var result = new List<Vector2> { source[0] };
        var previous = source[0];
        var distanceSinceSample = 0f;
        var segmentCount = isClosed ? source.Count : source.Count - 1;

        for (var index = 0; index < segmentCount; index++)
        {
            var segmentStart = index == 0 ? previous : source[index];
            var segmentEnd = source[(index + 1) % source.Count];
            var segment = segmentEnd - segmentStart;
            var segmentLength = segment.magnitude;

            if (segmentLength <= Mathf.Epsilon)
            {
                continue;
            }

            var direction = segment / segmentLength;
            var travelled = 0f;

            while (distanceSinceSample + segmentLength - travelled >= spacing)
            {
                var step = spacing - distanceSinceSample;
                travelled += step;
                var sample = segmentStart + direction * travelled;
                result.Add(sample);
                distanceSinceSample = 0f;
            }

            distanceSinceSample += segmentLength - travelled;
            previous = segmentEnd;
        }

        if (!isClosed && Vector2.Distance(result[^1], source[^1]) > spacing * 0.25f)
        {
            result.Add(source[^1]);
        }

        return result;
    }
}
