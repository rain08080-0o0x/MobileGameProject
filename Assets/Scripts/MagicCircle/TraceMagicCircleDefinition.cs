using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TraceMagicCircle", menuName = "Trace Battle/Magic Circle")]
public sealed class TraceMagicCircleDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "Magic Circle";
    [SerializeField, Min(1)] private int basePower = 100;
    [SerializeField, Min(1)] private int drawCount = 3;
    [SerializeField] private List<TraceMagicCircleShape> shapes = new();

    public string DisplayName => displayName;
    public int BasePower => basePower;
    public int DrawCount => drawCount;
    public IReadOnlyList<TraceMagicCircleShape> Shapes => shapes;
}
