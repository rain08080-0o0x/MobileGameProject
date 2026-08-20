public readonly struct TraceShapeAttackResult
{
    public TraceShapeAttackResult(float accuracyPercentage, float drawingSeconds)
    {
        AccuracyPercentage = accuracyPercentage;
        DrawingSeconds = drawingSeconds;
    }

    public float AccuracyPercentage { get; }
    public float DrawingSeconds { get; }
}
