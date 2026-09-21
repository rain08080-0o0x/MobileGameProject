using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// “G1‘Ì•ª‚ÌŠî–{î•ñ‚ğŠÇ—‚·‚éƒNƒ‰ƒX
/// </summary>
public class EnemyData : MonoBehaviour
{
    /// <summary>
    /// “G‚ğ¯•Ê‚·‚éID
    /// </summary>
    [SerializeField]
    [Tooltip("“G‚ğ¯•Ê‚·‚é‚½‚ß‚ÌIDB—áFenemy_001")]
    private string enemyId;

    /// <summary>
    /// “G‚Ì–¼‘O
    /// </summary>
    [SerializeField]
    [Tooltip("“G‚Ì•\¦–¼")]
    private string enemyName;

    /// <summary>
    /// Å‘åHP
    /// </summary>
    [SerializeField]
    [Tooltip("“G‚ÌÅ‘å‘Ì—Í")]
    private int maxHP = 100;

    /// <summary>
    /// “G‚ÌŒ©‚½–Ú‚Ég—p‚·‚éSprite
    /// </summary>
    [SerializeField]
    [Tooltip("2D•\¦‚Ég—p‚·‚é“G‰æ‘œ")]
    private Sprite sprite;

    /// <summary>
    /// “G‚ÌPrefab
    /// </summary>
    [SerializeField]
    [Tooltip("“G‚Æ‚µ‚Äg—p‚·‚éPrefab")]
    private GameObject prefab;

    /// <summary>
    /// “G‚ªg—p‚Å‚«‚éUŒ‚ˆê——
    /// </summary>
    [SerializeField]
    [Tooltip("‚±‚Ì“G‚ªg—p‚·‚éUŒ‚ˆê——")]
    private List<EnemyAttackData> attackList = new List<EnemyAttackData>();


    /// <summary>
    /// “GID‚ğæ“¾
    /// </summary>
    public string EnemyId => enemyId;

    /// <summary>
    /// “G–¼‚ğæ“¾
    /// </summary>
    public string EnemyName => enemyName;

    /// <summary>
    /// Å‘åHP‚ğæ“¾
    /// </summary>
    public int MaxHP => maxHP;

    /// <summary>
    /// Sprite‚ğæ“¾
    /// </summary>
    public Sprite EnemySprite => sprite;

    /// <summary>
    /// Prefab‚ğæ“¾
    /// </summary>
    public GameObject EnemyPrefab => prefab;

    /// <summary>
    /// UŒ‚ˆê——‚ğæ“¾
    /// </summary>
    public List<EnemyAttackData> AttackList => attackList;
}