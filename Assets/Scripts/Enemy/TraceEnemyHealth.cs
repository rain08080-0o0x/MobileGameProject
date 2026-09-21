using System;
using UnityEngine;

public sealed class TraceEnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 1000;

    public event Action<int> Damaged;

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
        if (appliedDamage > 0)
        {
            Damaged?.Invoke(appliedDamage);
        }

        return appliedDamage;
    }
}
