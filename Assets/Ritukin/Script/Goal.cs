using UnityEngine;

/// <summary>
/// ゴール判定を管理するクラス
/// </summary>
public class Goal : MonoBehaviour
{
    /// <summary>
    /// 経過時間管理
    /// </summary>
    [SerializeField]
    [Tooltip("ElapsedTimeUIが付いているオブジェクトを設定します。")]
    private ElapsedTimeUI elapsedTimeUI;

    /// <summary>
    /// ゴールに何かが入ったとき
    /// </summary>
    /// <param name="other">触れたオブジェクト</param>
    private void OnTriggerEnter(Collider other)
    {
        // Player以外は無視
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // 時計を停止
        elapsedTimeUI.StopTimer();
    }
}