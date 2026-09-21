using UnityEngine;

public sealed class TraceDamagePopup : MonoBehaviour
{
    private const float DisplayDuration = 0.85f;
    private const float RiseDistance = 0.9f;

    private static readonly Color DamageColor = new(1f, 0.78f, 0.12f, 1f);

    private TextMesh damageText;
    private Vector3 startPosition;
    private float elapsed;

    public void Initialize(int damage)
    {
        startPosition = transform.position;
        transform.localScale = Vector3.one * 0.7f;

        damageText = gameObject.AddComponent<TextMesh>();
        damageText.text = damage.ToString();
        damageText.anchor = TextAnchor.MiddleCenter;
        damageText.alignment = TextAlignment.Center;
        damageText.fontSize = 72;
        damageText.fontStyle = FontStyle.Bold;
        damageText.characterSize = 0.08f;
        damageText.color = DamageColor;

        var textRenderer = damageText.GetComponent<MeshRenderer>();
        textRenderer.sortingOrder = 20;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / DisplayDuration);
        var easedRise = 1f - Mathf.Pow(1f - progress, 2f);
        transform.position = startPosition + Vector3.up * (RiseDistance * easedRise);

        if (progress < 0.2f)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.15f, progress / 0.2f);
        }
        else
        {
            transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, (progress - 0.2f) / 0.8f);
        }

        var color = DamageColor;
        color.a = 1f - Mathf.InverseLerp(0.45f, 1f, progress);
        damageText.color = color;

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
