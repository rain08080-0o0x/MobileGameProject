using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TraceSceneLoader : MonoBehaviour
{
    private enum BattleUiMode
    {
        Numeric,
        Bar,
    }

    [SerializeField] private string drawingSceneName = "TraceSysTest";
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private BattleUiMode battleUiMode = BattleUiMode.Numeric;
    [SerializeField] private string numericUiSceneName = "NumericHudScene";
    [SerializeField] private string barUiSceneName = "BarHudScene";

    private IEnumerator Start()
    {
        yield return SceneManager.LoadSceneAsync(drawingSceneName, LoadSceneMode.Additive);
        yield return SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Additive);
        var uiSceneName = battleUiMode == BattleUiMode.Numeric
            ? numericUiSceneName
            : barUiSceneName;
        yield return SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);

        var traceSystem = FindFirstObjectByType<TraceSystem>();
        var battleController = FindFirstObjectByType<TraceBattleController>();
        var selectionController = FindFirstObjectByType<TraceMagicCircleSelectionController>();
        var playerHealth = FindFirstObjectByType<TracePlayerHealth>();
        var enemyHealth = FindFirstObjectByType<TraceEnemyHealth>();
        var battleUI = FindFirstObjectByType<TraceBattleUI>();
        if (traceSystem == null || battleController == null || playerHealth == null ||
            enemyHealth == null || battleUI == null || selectionController == null)
        {
            throw new InvalidOperationException("Additive battle scenes are missing required components.");
        }

        battleUI.Initialize(playerHealth, enemyHealth, battleController, traceSystem);
        battleController.Initialize(traceSystem, selectionController);
    }
}
