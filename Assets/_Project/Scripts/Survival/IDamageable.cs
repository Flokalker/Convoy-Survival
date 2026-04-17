using UnityEngine;

public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(float amount, GameObject source = null);
}
