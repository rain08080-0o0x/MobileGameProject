//なぞり終えた画像をこのスクリプトが設置された場所まで移動

using UnityEngine;

public class TraceDestination : MonoBehaviour
{
    //移動先の位置を返す
    public Vector3 TargetPosition => transform.position;

    private void OnDrawGizmos() //...シーンビューに描画する為のコールバック関数(でバック用？)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
