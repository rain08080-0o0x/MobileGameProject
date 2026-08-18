using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TraceSystem : MonoBehaviour
{
    [Header("画像の参照")]
    [SerializeField] private List<SpriteRenderer> targetImages = new List<SpriteRenderer>(); //なぞる画像

    [Header("移動設定")]
    [SerializeField] private List<TraceDestination> destination = new List<TraceDestination>();

    [Tooltip("移動スピード")]
    [SerializeField] private float moveSpeed = 5.0f;

    [Header("線の設定")]

    [Tooltip("なぞり線の太さ")]
    [SerializeField] private float lineWidth = 0.1f;            //線の太さ

    [Tooltip("なぞり線の色")]
    [SerializeField] private Color lineColor = new(0.1f, 0.1f, 0.1f, 1f);

    [Tooltip("この数値以上なぞらないと線は描かれないヨ")]
    [SerializeField] private float minDistancePoints = 0.5f;   //これ以上動かさないと戦は描かれない

    private LineRenderer lineRenderer;                          //描画のlineRendererコンポーネント
    private List<Vector3> points = new List<Vector3>();         //描画された線の頂点座標リスと　
    private Camera mainCamera;

    private bool isTracing = false;                             //なぞってるか否か
    private Coroutine moveCoroutine;

    private HashSet<SpriteRenderer> tracedImages = new HashSet<SpriteRenderer> ();

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        mainCamera = Camera.main;

        //線の幅設定
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth   = lineWidth;

        //線の色設定
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));       //これをやらないと、インスペクターで決めた色が反映されない
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor   = lineColor;

        lineRenderer.positionCount = 0;
    }

    void Update()
    {
        //タッチ入力処理
        if(Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);    //一本目の指のタッチの取得

            //タッチ開始したら
            if (touch.phase == TouchPhase.Began)
            {
                StartTracing(touch.position);   
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                if(isTracing)
                {
                    Trace(touch.position);
                }
            }
            //タッチを中断したら
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if(isTracing)
                {
                    EndTracing();
                }
            }
        }
        //マウス流力処理
        else
        {
            if(Input.GetMouseButtonDown(0))
            {
                StartTracing(Input.mousePosition);
            }
            //ドラック中
            else if(Input.GetMouseButton(0))
            {
                if(isTracing)
                {
                    Trace(Input.mousePosition);
                }
            }
            else if(Input.GetMouseButtonUp(0))
            {
                if(isTracing)
                {
                    EndTracing();
                }
            }
        }
    }

    //なぞり始めの処理
    private void StartTracing(Vector2 screenPosition)
    {
        if(moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            SaveCurrentLine();
            moveCoroutine = null;
        }

        ClearLine();
        tracedImages.Clear();
        isTracing = true;
        Trace(screenPosition);
    }
    
    //なぞり終わりの処理
    private void EndTracing()
    {
        isTracing = false;

        //離したら画面中央に移動
        if(points.Count > 0)
        {
            TraceDestination currentDestination = GetTraceDestinationForImages();
            moveCoroutine = StartCoroutine(MoveLineToDestination(currentDestination));
        }
    }

    //なぞりの処理
    private void Trace(Vector2 screenPosition)
    {
        Vector3 worldPos = GetWorldPositionFromScreen(screenPosition);

        if(!IsOverImage(worldPos,out SpriteRenderer touchedImage))
        {
            return;
        }

        //触れた画像を記録
        tracedImages.Add(touchedImage);

     
        //指定した距離以上でないと描画されない
        if(points.Count == 0 || Vector3.Distance(points[points.Count - 1],worldPos) > minDistancePoints)
        {
            points.Add(worldPos);

            lineRenderer.positionCount = points.Count;

            lineRenderer.SetPosition(points.Count - 1, worldPos);
        }
    }

    //画像の移動先を取得
    private TraceDestination GetTraceDestinationForImages()
    {
        foreach(var image in tracedImages)
        {
            int index = targetImages.IndexOf(image);

            if(index != -1 && index < destination.Count)
            {
                return destination[index];
            }
        }
        return null;
    }

    //中央へ移動させる処理
    private IEnumerator MoveLineToDestination(TraceDestination targetDestination)
    {
        Bounds bounds = new Bounds(points[0],Vector3.zero);

        foreach(Vector3 p in points)
        {
            bounds.Encapsulate(p);
        }

        Vector3 currentCenter = bounds.center;

        Vector3 targetWorldCenter = (destination != null) ? targetDestination.TargetPosition : Vector3.zero;
        targetWorldCenter.z = currentCenter.z;

        //オブジェクトの中心点から中央までの移動差分の算出
        Vector3 targetOffset = targetWorldCenter - currentCenter;
        Vector3 currentOffset = Vector3.zero;

        while(currentOffset != targetOffset)
        {
            Vector3 nextOffset = Vector3.MoveTowards(currentOffset,targetOffset,moveSpeed * Time.deltaTime);
            Vector3 delta = nextOffset - currentOffset;

            for (int i = 0; i < points.Count; i++)
            {
                points[i] += delta;
                lineRenderer.SetPosition(i, points[i]);
            }
            currentOffset = nextOffset;
            yield return null;
        }
        SaveCurrentLine();
        ClearLine();

        HideGuideImage();
        moveCoroutine = null;
    }

    //なぞり終えた画像を削除
    private void HideGuideImage()
    {
        List<SpriteRenderer> imageRemove = new List<SpriteRenderer>(tracedImages);

        foreach(var image in imageRemove)
        {
            if(image != null)
            {
                int index = targetImages.IndexOf(image);

                if(index != -1)
                {
                    targetImages[index] = null;
                }

                Destroy(image.gameObject);
            }
        }
        tracedImages.Clear();
    }

    //できたものを保存
    private void SaveCurrentLine()
    {
        if(points.Count == 0)
        {
            return;
        }

        GameObject saveLineObj = new GameObject("SavedLine");
        saveLineObj.transform.SetParent(this.transform);

        LineRenderer savedLine = saveLineObj.AddComponent<LineRenderer>();

        savedLine.material = lineRenderer.material;
        savedLine.startWidth = lineRenderer.startWidth;
        savedLine.endWidth = lineRenderer.endWidth;
        savedLine.useWorldSpace = lineRenderer.useWorldSpace;

        savedLine.startColor = lineColor;
        savedLine.endColor = lineColor;

        savedLine.positionCount = points.Count;

        for(int i = 0; i < points.Count;i++)
        {
            savedLine.SetPosition(i,points[i]);
        }
    }

    //座標変換 
    private Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
    {
        Vector3 screenPos = screenPosition;

        screenPos.z = -mainCamera.transform.position.z;

        return mainCamera.ScreenToWorldPoint(screenPos);
    }

    //指定した画像上にあるかどうか
    private bool IsOverImage(Vector3 worldPos,out SpriteRenderer touchedImage)
    {
        touchedImage = null;

        if(targetImages == null || targetImages.Count == 0)
        {
            return false;
        }

       foreach(var image in targetImages)
       {
            if (image == null || image.sprite == null) continue;

            Vector2 localPos = image.transform.InverseTransformPoint(worldPos);
            Sprite sprite = image.sprite;
            Rect rect = sprite.rect;

            //ローカル座標をテクスチャ内のピクセル位置に菅さん
            float pixelX = localPos.x * sprite.pixelsPerUnit + sprite.pivot.x;
            float pixelY = localPos.y * sprite.pixelsPerUnit + sprite.pivot.y;

            //座標が画像内に収まってるか判定
            if(pixelX >= 0 && pixelX < rect.width && pixelY >= 0 && pixelY < rect.height)
            {
                Texture2D texture = sprite.texture;
                Color color = texture.GetPixel((int)(rect.x + pixelX),(int)(rect.y + pixelY));

                if(color.a > 0.1f)
                {
                    touchedImage = image;
                    return true;
                }
            }
       }
       return false;
    }

    //今かかかれている線を削除して初期化
    private void ClearLine()
    {
        points.Clear();
        lineRenderer.positionCount = 0;
    }
}