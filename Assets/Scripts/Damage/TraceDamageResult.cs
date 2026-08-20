public readonly struct TraceDamageResult
{
    public TraceDamageResult(
        int basePower,
        float averageAccuracyPercentage,
        float accuracyMultiplier,
        int completedShapeCount,
        float countMultiplier,
        float averageDrawingSeconds,
        float tempoMultiplier,
        int finalDamage)
    {
        BasePower = basePower;
        AverageAccuracyPercentage = averageAccuracyPercentage;
        AccuracyMultiplier = accuracyMultiplier;
        CompletedShapeCount = completedShapeCount;
        CountMultiplier = countMultiplier;
        AverageDrawingSeconds = averageDrawingSeconds;
        TempoMultiplier = tempoMultiplier;
        FinalDamage = finalDamage;
    }

    public int BasePower { get; }
    public float AverageAccuracyPercentage { get; }
    public float AccuracyMultiplier { get; }
    public int CompletedShapeCount { get; }
    public float CountMultiplier { get; }
    public float AverageDrawingSeconds { get; }
    public float TempoMultiplier { get; }
    public int FinalDamage { get; }
}
