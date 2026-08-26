using UnityEngine;

public sealed class TraceEnemyView : MonoBehaviour
{
    [SerializeField] private Color normalColor = new(0.88f, 0.18f, 0.2f, 1f);
    [SerializeField] private Color preparingColor = new(1f, 0.48f, 0.12f, 1f);

    private SpriteRenderer enemyRenderer;
    private bool isPreparing;

    public Vector3 AttackTargetPosition => transform.position;

    private void Awake()
    {
        var visualObject = new GameObject("Enemy Visual");
        visualObject.transform.SetParent(transform, false);
        visualObject.transform.localScale = Vector3.one * 0.85f;

        enemyRenderer = visualObject.AddComponent<SpriteRenderer>();
        enemyRenderer.sprite = TraceCircleSpriteFactory.Create(96, "Runtime Enemy Texture");
        enemyRenderer.color = normalColor;
        enemyRenderer.sortingOrder = 3;
    }

    private void Update()
    {
        if (!isPreparing)
        {
            transform.localScale = Vector3.one;
            enemyRenderer.color = normalColor;
            return;
        }

        var pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.09f;
        transform.localScale = Vector3.one * pulse;
        enemyRenderer.color = Color.Lerp(normalColor, preparingColor, (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f);
    }

    public void SetPreparing(bool preparing)
    {
        isPreparing = preparing;
    }

    public void SetVisible(bool visible)
    {
        enemyRenderer.enabled = visible;
    }
}
