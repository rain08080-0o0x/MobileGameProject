using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[System.Serializable]
public struct TraceImageSetting
{
    [Tooltip("なぞり対象の画像")]
    public SpriteRenderer targetImage;

    [Tooltip("チェックで画面中心へ移動外すと指定した場所に移動")]
    public bool moveToCenter;

    [Tooltip("移動先設定")]
    public TraceDestination destination;
}

// テクスチャのアルファ判定データを高速参照するためのキャッシュ構造
public class AlphaCache
{
    public int width;
    public int height;
    public Rect rect;
    public bool[] alphaMask; // 透明度閾値（0.1f）を超えているかどうかのフラグ配列

    public AlphaCache(Sprite sprite, float threshold = 0.1f)
    {
        Texture2D texture = sprite.texture;
        rect = sprite.rect;
        width = (int)rect.width;
        height = (int)rect.height;
        alphaMask = new bool[width * height];

        // テクスチャから一度だけ全ピクセルを取得してキャッシュ
        Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, width, height);
        for (int i = 0; i < pixels.Length; i++)
        {
            alphaMask[i] = pixels[i].a > threshold;
        }
    }

    public bool IsOpaque(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        return alphaMask[y * width + x];
    }
}

[RequireComponent(typeof(LineRenderer))]
public class TraceSystem : MonoBehaviour
{
    [Header("画像個別設定")]
    [SerializeField] private List<TraceImageSetting> imageSettings = new List<TraceImageSetting>();

    [Tooltip("移動スピード")]
    [SerializeField] private float moveSpeed = 5.0f;

    [Header("線の設定")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Color lineColor = new(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private float minDistancePoints = 0.5f;

    private LineRenderer lineRenderer;
    private readonly List<Vector3> points = new List<Vector3>(128);
    private Camera mainCamera;

    private bool isTracing = false;
    private Coroutine moveCoroutine;

    private readonly HashSet<SpriteRenderer> tracedImages = new HashSet<SpriteRenderer>();

    // 画像ごとのアルファデータとインデックスのキャッシュ構造
    private readonly Dictionary<SpriteRenderer, AlphaCache> alphaCacheMap = new Dictionary<SpriteRenderer, AlphaCache>();
    private readonly Dictionary<SpriteRenderer, int> settingIndexMap = new Dictionary<SpriteRenderer, int>();

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        mainCamera = Camera.main;

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        if (lineMaterial != null)
        {
            lineRenderer.sharedMaterial = lineMaterial;
        }

        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.positionCount = 0;

        // ゲーム開始時にアルファデータを事前読み込み（これ以降GetPixelは不使用）
        BuildAlphaCache();
    }

    private void BuildAlphaCache()
    {
        alphaCacheMap.Clear();
        settingIndexMap.Clear();

        for (int i = 0; i < imageSettings.Count; i++)
        {
            var setting = imageSettings[i];
            if (setting.targetImage != null && setting.targetImage.sprite != null)
            {
                // アルファ配列を作成
                alphaCacheMap[setting.targetImage] = new AlphaCache(setting.targetImage.sprite, 0.1f);
                settingIndexMap[setting.targetImage] = i;
            }
        }
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    StartTracing(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isTracing) Trace(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isTracing) EndTracing();
                    break;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartTracing(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && isTracing)
            {
                Trace(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0) && isTracing)
            {
                EndTracing();
            }
        }
    }

    private void StartTracing(Vector2 screenPosition)
    {
        if (moveCoroutine != null)
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

    private void EndTracing()
    {
        isTracing = false;

        if (points.Count > 0)
        {
            Vector3 targetWorldPosition = GetTargetWorldPosition();
            moveCoroutine = StartCoroutine(MoveLineToDestination(targetWorldPosition));
        }
    }

    private void Trace(Vector2 screenPosition)
    {
        Vector3 worldPos = GetWorldPositionFromScreen(screenPosition);

        if (!IsOverImage(worldPos, out SpriteRenderer touchedImage))
        {
            return;
        }

        tracedImages.Add(touchedImage);

        // 二乗距離計算で平方根（Mathf.Sqrt）の負荷をカット
        float sqrMinDistance = minDistancePoints * minDistancePoints;
        if (points.Count == 0 || (points[points.Count - 1] - worldPos).sqrMagnitude > sqrMinDistance)
        {
            points.Add(worldPos);
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPosition(points.Count - 1, worldPos);
        }
    }

    private Vector3 GetTargetWorldPosition()
    {
        foreach (var setting in imageSettings)
        {
            if (setting.targetImage != null && tracedImages.Contains(setting.targetImage))
            {
                if (setting.moveToCenter) return Vector3.zero;
                if (setting.destination != null) return setting.destination.TargetPosition;
            }
        }
        return Vector3.zero;
    }

    private IEnumerator MoveLineToDestination(Vector3 targetWorldCenter)
    {
        if (points.Count == 0) yield break;

        Bounds bounds = new Bounds(points[0], Vector3.zero);
        for (int i = 1; i < points.Count; i++)
        {
            bounds.Encapsulate(points[i]);
        }

        Vector3 currentCenter = bounds.center;
        targetWorldCenter.z = currentCenter.z;

        Vector3 targetOffset = targetWorldCenter - currentCenter;
        Vector3 currentOffset = Vector3.zero;

        while (currentOffset != targetOffset)
        {
            Vector3 nextOffset = Vector3.MoveTowards(currentOffset, targetOffset, moveSpeed * Time.deltaTime);
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

    private void HideGuideImage()
    {
        foreach (var image in tracedImages)
        {
            if (image == null) continue;

            if (settingIndexMap.TryGetValue(image, out int index))
            {
                var setting = imageSettings[index];
                setting.targetImage = null;
                imageSettings[index] = setting;

                settingIndexMap.Remove(image);
                alphaCacheMap.Remove(image);
            }

            Destroy(image.gameObject);
        }
        tracedImages.Clear();
    }

    private void SaveCurrentLine()
    {
        if (points.Count == 0) return;

        GameObject saveLineObj = new GameObject("SavedLine");
        saveLineObj.transform.SetParent(this.transform);

        LineRenderer savedLine = saveLineObj.AddComponent<LineRenderer>();
        savedLine.sharedMaterial = lineRenderer.sharedMaterial;
        savedLine.startWidth = lineRenderer.startWidth;
        savedLine.endWidth = lineRenderer.endWidth;
        savedLine.useWorldSpace = lineRenderer.useWorldSpace;

        savedLine.startColor = lineColor;
        savedLine.endColor = lineColor;

        savedLine.positionCount = points.Count;
        savedLine.SetPositions(points.ToArray());
    }

    private Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
    {
        return mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z));
    }

    // キャッシュ参照による「完全精度かつ超高速」な判定処理
    private bool IsOverImage(Vector3 worldPos, out SpriteRenderer touchedImage)
    {
        touchedImage = null;

        foreach (var setting in imageSettings)
        {
            SpriteRenderer image = setting.targetImage;
            if (image == null || !alphaCacheMap.TryGetValue(image, out AlphaCache cache)) continue;

            // ローカル座標変換
            Vector2 localPos = image.transform.InverseTransformPoint(worldPos);
            Sprite sprite = image.sprite;

            // ローカル座標からピクセルインデックスの算出
            float pixelX = localPos.x * sprite.pixelsPerUnit + sprite.pivot.x;
            float pixelY = localPos.y * sprite.pixelsPerUnit + sprite.pivot.y;

            int x = (int)pixelX;
            int y = (int)pixelY;

            // キャッシュしたBool配列の高速アクセス（計算コスト O(1)）
            if (cache.IsOpaque(x, y))
            {
                touchedImage = image;
                return true;
            }
        }
        return false;
    }

    private void ClearLine()
    {
        points.Clear();
        lineRenderer.positionCount = 0;
    }
}