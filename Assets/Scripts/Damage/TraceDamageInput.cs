using System.Collections.Generic;

public readonly struct TraceDamageInput
{
    public TraceDamageInput(
        TraceMagicCircleDefinition magicCircle,
        IReadOnlyList<float> accuracyPercentages,
        IReadOnlyList<float> drawingSeconds)
    {
        MagicCircle = magicCircle;
        AccuracyPercentages = accuracyPercentages;
        DrawingSeconds = drawingSeconds;
    }

    public TraceMagicCircleDefinition MagicCircle { get; }
    public IReadOnlyList<float> AccuracyPercentages { get; }
    public IReadOnlyList<float> DrawingSeconds { get; }
}
