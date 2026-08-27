using UnityEngine;

public sealed class TraceBattleHud : MonoBehaviour
{
    [SerializeField] private TraceBattleController battleController;
    [SerializeField] private TracePlayerHealth playerHealth;
    [SerializeField] private TraceEnemyHealth enemyHealth;
    [SerializeField] private TraceEnemyAttackController enemyAttackController;

    private void OnGUI()
    {
        if (battleController == null || playerHealth == null || enemyHealth == null || enemyAttackController == null)
        {
            return;
        }

        var panelWidth = Mathf.Min(640f, Screen.width - 24f);
        GUILayout.BeginArea(new Rect(12f, 12f, panelWidth, 190f), GUI.skin.box);
        GUILayout.Label($"プレイヤーHP  {playerHealth.CurrentHealth} / {playerHealth.MaxHealth}");
        GUILayout.Label($"敵HP  {enemyHealth.CurrentHealth} / {enemyHealth.MaxHealth}");

        switch (battleController.Phase)
        {
            case TraceBattlePhase.MagicCircleSelection:
                GUILayout.Label("魔法陣を選択してください");
                break;
            case TraceBattlePhase.PlayerTurn:
                GUILayout.Label("魔法陣の図形をなぞってください");
                GUILayout.Label(
                    $"完了 {battleController.CurrentPatternIndex} / {battleController.TotalPatternCount}");
                GUILayout.Label($"描画時間 {battleController.CurrentPatternElapsedSeconds:0.00}秒");
                if (battleController.LastShapeResult is { } lastShape)
                {
                    GUILayout.Label($"直前：一致率 {lastShape.AccuracyPercentage:0.0}% / {lastShape.DrawingSeconds:0.00}秒");
                }
                break;
            case TraceBattlePhase.PlayerAttackResolution:
                var result = battleController.DamageResult;
                GUILayout.Label(
                    $"{result.BasePower} × {result.AccuracyMultiplier:0.00} × " +
                    $"{result.CountMultiplier:0.00} × {result.TempoMultiplier:0.00} = {result.FinalDamage}");
                break;
            case TraceBattlePhase.EnemyTurn:
                GUILayout.Label(enemyAttackController.AttackApplied
                    ? $"敵の攻撃！  {enemyAttackController.LastDamage}ダメージ"
                    : $"敵の攻撃まで {Mathf.Max(0f, enemyAttackController.TimeRemaining):0.0}秒");
                break;
            case TraceBattlePhase.Victory:
                GUILayout.Label("敵を倒した！");
                break;
            case TraceBattlePhase.Defeat:
                GUILayout.Label("プレイヤーは倒された…");
                break;
        }

        GUILayout.EndArea();
    }
}
