using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class TraceBattleUI : MonoBehaviour
{
    [SerializeField] private Text playerHealthText;
    [SerializeField] private Text enemyHealthText;
    [SerializeField] private RectTransform playerHealthFill;
    [SerializeField] private RectTransform enemyHealthFill;
    [SerializeField] private ElapsedTimeUI elapsedTimeUI;

    private TracePlayerHealth playerHealth;
    private TraceEnemyHealth enemyHealth;
    private TraceBattleController battleController;
    private TraceSystem traceSystem;
    private HealthDisplay playerDisplay;
    private HealthDisplay enemyDisplay;
    private bool timerStopped;

    public void Initialize(
        TracePlayerHealth inputPlayerHealth,
        TraceEnemyHealth inputEnemyHealth,
        TraceBattleController inputBattleController,
        TraceSystem inputTraceSystem)
    {
        playerHealth = inputPlayerHealth;
        enemyHealth = inputEnemyHealth;
        battleController = inputBattleController;
        traceSystem = inputTraceSystem;
        if (playerHealth == null || enemyHealth == null || battleController == null ||
            traceSystem == null || playerHealthText == null || enemyHealthText == null ||
            elapsedTimeUI == null)
        {
            throw new InvalidOperationException("Battle UI references are not configured.");
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        playerHealthText.font = font;
        enemyHealthText.font = font;
        playerDisplay = new HealthDisplay(playerHealthText, playerHealthFill);
        enemyDisplay = new HealthDisplay(enemyHealthText, enemyHealthFill);
        traceSystem.TracingAvailable += OnTracingAvailable;
        traceSystem.TracingCompleted += OnTracingCompleted;
        Refresh();
    }

    private void OnDestroy()
    {
        if (traceSystem != null)
        {
            traceSystem.TracingAvailable -= OnTracingAvailable;
            traceSystem.TracingCompleted -= OnTracingCompleted;
        }
    }

    private void Update()
    {
        if (playerHealth == null || enemyHealth == null)
        {
            return;
        }

        Refresh();
        if (!timerStopped &&
            (battleController.Phase == TraceBattlePhase.Victory ||
             battleController.Phase == TraceBattlePhase.Defeat))
        {
            timerStopped = true;
            elapsedTimeUI.StopTimer();
        }
    }

    private void Refresh()
    {
        playerDisplay.Set("PLAYER HP", playerHealth.CurrentHealth, playerHealth.MaxHealth);
        enemyDisplay.Set("ENEMY HP", enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
    }

    private void OnTracingAvailable()
    {
        elapsedTimeUI.ResetTimer();
    }

    private void OnTracingCompleted()
    {
        elapsedTimeUI.StopTimer();
    }

    private sealed class HealthDisplay
    {
        private readonly Text label;
        private readonly RectTransform fillRect;

        public HealthDisplay(Text label, RectTransform fillRect)
        {
            this.label = label;
            this.fillRect = fillRect;
        }

        public void Set(string prefix, int current, int maximum)
        {
            label.text = $"{prefix}  {current} / {maximum}";
            if (fillRect != null)
            {
                var anchorMax = fillRect.anchorMax;
                anchorMax.x = maximum > 0 ? Mathf.Clamp01(current / (float)maximum) : 0f;
                fillRect.anchorMax = anchorMax;
            }
        }
    }
}
