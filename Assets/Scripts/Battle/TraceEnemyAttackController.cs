using System.Collections;
using UnityEngine;

public sealed class TraceEnemyAttackController : MonoBehaviour
{
    private const float CameraShakeDuration = 0.28f;
    private const float CameraShakeMagnitude = 0.18f;

    [SerializeField, Min(0)] private int attackDamage = 100;
    [SerializeField, Min(0f)] private float windupDuration = 1f;
    [SerializeField, Min(0f)] private float resultDuration = 1f;

    public float TimeRemaining { get; private set; }
    public int LastDamage { get; private set; }
    public int LastDamageReduction { get; private set; }
    public bool AttackApplied { get; private set; }

    public IEnumerator Execute(TraceEnemyView enemyView, TracePlayerHealth playerHealth)
    {
        return Execute(enemyView, playerHealth, 0);
    }

    public IEnumerator Execute(
        TraceEnemyView enemyView,
        TracePlayerHealth playerHealth,
        int damageReduction)
    {
        AttackApplied = false;
        LastDamage = 0;
        LastDamageReduction = 0;
        TimeRemaining = windupDuration;
        enemyView.SetPreparing(true);

        while (TimeRemaining > 0f)
        {
            TimeRemaining -= Time.deltaTime;
            yield return null;
        }

        LastDamageReduction = Mathf.Min(attackDamage, Mathf.Max(0, damageReduction));
        LastDamage = playerHealth.TakeDamage(attackDamage - LastDamageReduction);
        AttackApplied = true;
        enemyView.SetPreparing(false);
        yield return ShakeCamera();
        yield return new WaitForSeconds(resultDuration);
    }

    private static IEnumerator ShakeCamera()
    {
        var targetCamera = Camera.main;
        if (targetCamera == null)
        {
            yield break;
        }

        var cameraTransform = targetCamera.transform;
        var originalPosition = cameraTransform.position;
        var elapsed = 0f;
        while (elapsed < CameraShakeDuration)
        {
            elapsed += Time.deltaTime;
            var strength = 1f - Mathf.Clamp01(elapsed / CameraShakeDuration);
            var offset = Random.insideUnitCircle * (CameraShakeMagnitude * strength);
            cameraTransform.position = originalPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        cameraTransform.position = originalPosition;
    }
}
