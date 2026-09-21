using UnityEngine;

[RequireComponent(typeof(TraceEnemyHealth))]
public sealed class TraceDamagePopupSpawner : MonoBehaviour
{
    private TraceEnemyHealth enemyHealth;

    private void Awake()
    {
        enemyHealth = GetComponent<TraceEnemyHealth>();
    }

    private void OnEnable()
    {
        enemyHealth ??= GetComponent<TraceEnemyHealth>();
        enemyHealth.Damaged += ShowDamage;
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.Damaged -= ShowDamage;
        }
    }

    private void ShowDamage(int damage)
    {
        var popupObject = new GameObject("Damage Popup");
        popupObject.transform.position = transform.position + new Vector3(0f, 0.65f, -0.2f);
        popupObject.AddComponent<TraceDamagePopup>().Initialize(damage);
    }
}
