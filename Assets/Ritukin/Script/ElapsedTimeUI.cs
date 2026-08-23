using TMPro;
using UnityEngine;

/// <summary>
/// ゲーム開始からの経過時間を表示するクラス
/// </summary>
public class ElapsedTimeUI : MonoBehaviour
{
    /// <summary>
    /// 時間表示用テキスト
    /// </summary>
    [SerializeField]
    [Tooltip("経過時間を表示するTextMeshProUGUIを設定します。")]
    private TextMeshProUGUI timeText;

    /// <summary>
    /// 経過時間
    /// </summary>
    private float elapsedTime = 0.0f;

    /// <summary>
    /// タイマーが動いているか
    /// </summary>
    private bool isRunning = true;

    /// <summary>
    /// 毎フレーム時間を更新
    /// </summary>
    private void Update()
    {
        // タイマー停止中なら何もしない
        if (!isRunning)
        {
            return;
        }

        // 経過時間を増やす
        elapsedTime += Time.deltaTime;

        // 分と秒に変換
        int minutes = Mathf.FloorToInt(elapsedTime / 60.0f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60.0f);

        // TIME 00:00 形式で表示
        timeText.text = $"TIME {minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// タイマーを停止する
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
    }
}