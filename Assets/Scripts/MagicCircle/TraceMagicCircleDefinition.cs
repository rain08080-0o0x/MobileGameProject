using UnityEngine;

[CreateAssetMenu(fileName = "TraceMagicCircle", menuName = "Trace Battle/Magic Circle")]
public sealed class TraceMagicCircleDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "Magic Circle";
    [SerializeField] private TracePattern pattern = TracePattern.UprightTriangle;
    [SerializeField, Min(1)] private int basePower = 100;
    [SerializeField, Min(1)] private int drawCount = 3;

    public string DisplayName => displayName;
    public TracePattern Pattern => pattern;
    public int BasePower => basePower;
    public int DrawCount => drawCount;
}
