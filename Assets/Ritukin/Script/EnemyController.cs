using UnityEngine;

/// <summary>
/// 敵のゲーム中の状態を管理するクラス
/// </summary>
public class EnemyController : MonoBehaviour
{
    /// <summary>
    /// 敵の基本データ
    /// </summary>
    [SerializeField]
    [InspectorName("敵データ")]
    [Tooltip("この敵が使用するEnemyDataです。")]
    private EnemyData enemyData;

    /// <summary>
    /// 現在のHP
    /// </summary>
    [SerializeField]
    [InspectorName("現在HP")]
    [Tooltip("ゲーム中の敵の現在HPです。")]
    private int currentHP;


    /// <summary>
    /// ゲーム開始時の初期化
    /// </summary>
    private void Start()
    {
        // EnemyDataがInspectorで設定されていなければ
        // 同じGameObjectから自動で探す
        if (enemyData == null)
        {
            enemyData = GetComponent<EnemyData>();
        }

        // EnemyDataが見つからなかった場合
        if (enemyData == null)
        {
            Debug.LogError("EnemyDataが設定されていません。");
            return;
        }

        // 最大HPを現在HPに設定
        currentHP = enemyData.MaxHP;

        Debug.Log(
            "敵名：" + enemyData.EnemyName +
            " / HP：" + currentHP
        );
    }


    /// <summary>
    /// 敵にダメージを与える
    /// </summary>
    /// <param name="damage">与えるダメージ量</param>
    public void TakeDamage(int damage)
    {
        // 0以下のダメージは無視
        if (damage <= 0)
        {
            return;
        }

        // HPを減らす
        currentHP -= damage;

        // HPが0未満にならないようにする
        currentHP = Mathf.Max(currentHP, 0);

        Debug.Log(
            enemyData.EnemyName +
            " に " +
            damage +
            " ダメージ！ 現在HP：" +
            currentHP
        );

        // HPが0になったら死亡
        if (currentHP <= 0)
        {
            Die();
        }
    }


    /// <summary>
    /// 敵が倒れた時の処理
    /// </summary>
    private void Die()
    {
        Debug.Log(enemyData.EnemyName + " を倒しました。");

        // 敵を削除
        Destroy(gameObject);
    }


    /// <summary>
    /// Inspectorから20ダメージをテストする
    /// </summary>
    [ContextMenu("テスト：20ダメージを受ける")]
    private void TestTakeDamage()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Play中に実行してください。");
            return;
        }

        TakeDamage(20);
    }
}