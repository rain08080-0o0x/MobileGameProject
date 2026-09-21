using UnityEngine;

[CreateAssetMenu(fileName = "TraceDamageSettings", menuName = "Trace Battle/Damage Settings")]
public sealed class TraceDamageSettings : ScriptableObject
{
    [Header("Accuracy Correction")]
    [SerializeField, Min(0f)] private float accuracyPercentScale = 0.01f;

    [Header("Draw Count Correction")]
    [SerializeField, Min(0f)] private float countMultiplierPerShape = 1f;

    [Header("Tempo Correction")]
    [SerializeField, Min(0f)] private float fastTempoMaxSeconds = 3f;
    [SerializeField, Min(0f)] private float fastTempoMultiplier = 1f;
    [SerializeField, Min(0f)] private float normalTempoMaxSeconds = 5f;
    [SerializeField, Min(0f)] private float normalTempoMultiplier = 0.8f;
    [SerializeField, Min(0f)] private float slowTempoMultiplier = 0.5f;

    public float AccuracyPercentScale => accuracyPercentScale;
    public float CountMultiplierPerShape => countMultiplierPerShape;
    public float FastTempoMaxSeconds => fastTempoMaxSeconds;
    public float FastTempoMultiplier => fastTempoMultiplier;
    public float NormalTempoMaxSeconds => normalTempoMaxSeconds;
    public float NormalTempoMultiplier => normalTempoMultiplier;
    public float SlowTempoMultiplier => slowTempoMultiplier;
}
