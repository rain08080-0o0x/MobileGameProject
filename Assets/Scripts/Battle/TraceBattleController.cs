using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class TraceBattleController : MonoBehaviour
{
    private const int AttackBasePower = 100;
    private static readonly Vector3 EnemyPlayerAttackPosition = new(0f, 2.15f, -0.05f);
    private static readonly Vector3 EnemyTurnPosition = new(0f, 0f, -0.05f);

    [Header("Battle References")]
    [SerializeField] private TracePlayerHealth playerHealth;
    [SerializeField] private TraceEnemyAttackController enemyAttackController;
    [SerializeField] private TraceEnemyHealth enemyHealth;
    [SerializeField] private TraceEnemyView enemyView;
    [SerializeField] private TraceDamageSettings damageSettings;

    [Header("Player Attack Presentation")]
    [SerializeField, Min(0.1f)] private float attackFlightDuration = 0.65f;
    [SerializeField, Min(0f)] private float playerAttackResultDuration = 1.4f;

    private readonly List<TraceShapeAttackResult> shapeResults = new();
    private readonly List<TraceGlyphAttack> storedGlyphs = new();
    private TraceSystem traceSystem;
    private bool initialized;

    public TraceBattlePhase Phase { get; private set; }
    public TraceDamageResult DamageResult { get; private set; }
    public int CurrentPatternIndex => traceSystem != null ? traceSystem.CompletedCount : 0;
    public int TotalPatternCount => traceSystem != null ? traceSystem.TargetCount : 0;
    public float CurrentPatternElapsedSeconds =>
        Phase == TraceBattlePhase.PlayerTurn && traceSystem != null
            ? traceSystem.CurrentDrawingSeconds
            : 0f;
    public TraceShapeAttackResult? LastShapeResult =>
        shapeResults.Count > 0 ? shapeResults[^1] : null;

    public void Initialize(TraceSystem inputTraceSystem)
    {
        if (initialized)
        {
            return;
        }

        traceSystem = inputTraceSystem;
        if (traceSystem == null || playerHealth == null || enemyAttackController == null ||
            enemyHealth == null || enemyView == null || damageSettings == null)
        {
            throw new InvalidOperationException("Trace battle references are not configured.");
        }

        initialized = true;
        traceSystem.BatchCompleted += OnBatchCompleted;
        BeginPlayerTurn();
    }

    private void OnDestroy()
    {
        if (traceSystem != null)
        {
            traceSystem.BatchCompleted -= OnBatchCompleted;
        }
    }

    private void BeginPlayerTurn()
    {
        ClearStoredGlyphs();
        shapeResults.Clear();
        DamageResult = default;
        Phase = TraceBattlePhase.PlayerTurn;
        enemyView.SetPreparing(false);
        enemyView.SetVisible(false);
        traceSystem.BeginRound();
    }

    private void OnBatchCompleted(TraceStrokeResult[] strokeResults)
    {
        if (Phase != TraceBattlePhase.PlayerTurn || strokeResults == null || strokeResults.Length == 0)
        {
            return;
        }

        for (var index = 0; index < strokeResults.Length; index++)
        {
            var strokeResult = strokeResults[index];
            shapeResults.Add(new TraceShapeAttackResult(
                strokeResult.Accuracy,
                strokeResult.DrawingSeconds));

            var glyphObject = new GameObject($"Stored Trace {index + 1}");
            glyphObject.transform.SetParent(transform, false);
            var glyph = glyphObject.AddComponent<TraceGlyphAttack>();
            glyph.Initialize(strokeResult.Points, 0.3f, Color.magenta);
            storedGlyphs.Add(glyph);
        }

        EndPlayerTurn();
    }

    private void EndPlayerTurn()
    {
        traceSystem.SetInputEnabled(false);
        var accuracies = new float[shapeResults.Count];
        var durations = new float[shapeResults.Count];
        for (var index = 0; index < shapeResults.Count; index++)
        {
            accuracies[index] = shapeResults[index].AccuracyPercentage;
            durations[index] = shapeResults[index].DrawingSeconds;
        }

        DamageResult = TraceDamageCalculator.CalculateDamage(
            AttackBasePower,
            damageSettings,
            accuracies,
            durations);

        Phase = TraceBattlePhase.PlayerAttackResolution;
        enemyView.transform.position = EnemyPlayerAttackPosition;
        enemyView.SetVisible(true);
        enemyView.SetPreparing(false);
        StartCoroutine(ResolvePlayerAttack());
    }

    private IEnumerator ResolvePlayerAttack()
    {
        for (var index = 0; index < storedGlyphs.Count; index++)
        {
            storedGlyphs[index].SetVisible(true);
        }
        traceSystem.ClearCollectedLines();

        var elapsed = 0f;
        while (elapsed < attackFlightDuration)
        {
            elapsed += Time.deltaTime;
            var progress = elapsed / attackFlightDuration;
            for (var index = 0; index < storedGlyphs.Count; index++)
            {
                storedGlyphs[index].SetFlightProgress(enemyView.AttackTargetPosition, progress);
            }
            yield return null;
        }

        enemyHealth.TakeDamage(DamageResult.FinalDamage);
        ClearStoredGlyphs();
        yield return new WaitForSeconds(playerAttackResultDuration);

        if (enemyHealth.IsDefeated)
        {
            Phase = TraceBattlePhase.Victory;
            yield break;
        }

        yield return RunEnemyTurn();
    }

    private IEnumerator RunEnemyTurn()
    {
        Phase = TraceBattlePhase.EnemyTurn;
        enemyView.transform.position = EnemyTurnPosition;
        enemyView.SetVisible(true);
        yield return enemyAttackController.Execute(enemyView, playerHealth);

        if (playerHealth.IsDefeated)
        {
            Phase = TraceBattlePhase.Defeat;
        }
        else
        {
            BeginPlayerTurn();
        }
    }

    private void ClearStoredGlyphs()
    {
        for (var index = 0; index < storedGlyphs.Count; index++)
        {
            if (storedGlyphs[index] != null)
            {
                Destroy(storedGlyphs[index].gameObject);
            }
        }
        storedGlyphs.Clear();
    }
}
