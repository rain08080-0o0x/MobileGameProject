using UnityEngine;

/// <summary>
/// 敵の1つの攻撃情報を管理するデータクラス
/// </summary>
[System.Serializable]
public class EnemyAttackData
{
    /// <summary>
    /// 攻撃名
    /// </summary>
    [SerializeField]
    [Tooltip("攻撃の名前")]
    private string attackName;

    /// <summary>
    /// 基礎ダメージ
    /// </summary>
    [SerializeField]
    [Tooltip("この攻撃が与える基本ダメージ")]
    private int baseDamage = 10;

    /// <summary>
    /// 防御時になぞる図形
    /// </summary>
    [SerializeField]
    [Tooltip("防御時になぞる図形の画像を設定")]
    private Sprite defenseShape;

    /// <summary>
    /// 攻撃演出
    /// </summary>
    [SerializeField]
    [Tooltip("攻撃時に表示する演出Prefabを設定")]
    private GameObject attackEffect;

    /// <summary>
    /// この攻撃が選ばれる確率
    /// </summary>
    [SerializeField]
    [InspectorName("攻撃を選ぶ確率")]
    [Range(0.0f, 100.0f)]
    [Tooltip("この攻撃が選ばれる確率です。")]
    private float selectionProbability = 100.0f;


    /// <summary>
    /// 攻撃名を取得
    /// </summary>
    public string AttackName => attackName;

    /// <summary>
    /// 基礎ダメージを取得
    /// </summary>
    public int BaseDamage => baseDamage;

    /// <summary>
    /// 防御図形を取得
    /// </summary>
    public Sprite DefenseShape => defenseShape;

    /// <summary>
    /// 攻撃演出を取得
    /// </summary>
    public GameObject AttackEffect => attackEffect;

    /// <summary>
    /// 選択確率を取得
    /// </summary>
    public float SelectionProbability => selectionProbability;
}