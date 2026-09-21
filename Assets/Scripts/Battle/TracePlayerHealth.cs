using UnityEngine;

public sealed class TracePlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 500;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDefeated => CurrentHealth <= 0;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public int TakeDamage(int damage)
    {
        var appliedDamage = Mathf.Min(CurrentHealth, Mathf.Max(0, damage));
        CurrentHealth -= appliedDamage;
        return appliedDamage;
    }
}
