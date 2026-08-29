using UnityEngine;

// カメラの描画範囲を決まった縦横比に固定する。
// 画面がそれより横長なら左右に、縦長なら上下に黒帯（レターボックス）が入る。
// これで UI の位置がウィンドウサイズによってズレなくなる。
[RequireComponent(typeof(Camera))]
public class AspectRatioKeeper : MonoBehaviour
{
    [Header("固定したい縦横比")]
    // Canvas の Reference Resolution と揃えておくと UI が作ったとおりに出る
    public float targetWidth = 1920.0f;
    public float targetHeight = 1300.0f;

    [Header("黒帯")]
    // 帯の部分を塗りつぶす専用カメラを自動で用意する
    public bool createLetterboxCamera = true;
    public Color letterboxColor = Color.black;

    [Header("同じ範囲に揃えるカメラ")]
    // UI 用のオーバーレイカメラなど。URP のオーバーレイカメラは描画自体は
    // ベースカメラの範囲に従うが、CanvasScaler が参照する pixelRect は
    // 自分の rect から計算されるので、ここで同じ値を入れておく必要がある。
    public Camera[] extraCameras;

    private Camera targetCamera;
    private Camera letterboxCamera;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    void OnEnable()
    {
        SetupLetterboxCamera();
        Apply();
    }

    void OnDisable()
    {
        // 元の全画面表示に戻しておく
        Rect full = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

        if (targetCamera != null)
        {
            targetCamera.rect = full;
        }

        if (extraCameras != null)
        {
            for (int i = 0; i < extraCameras.Length; i++)
            {
                if (extraCameras[i] == null) continue;
                extraCameras[i].rect = full;
            }
        }
    }

    void Update()
    {
        // ウィンドウサイズが変わったときだけ計算し直す
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            Apply();
        }
    }

    void Apply()
    {
        if (targetCamera == null) return;
        if (targetWidth <= 0.0f || targetHeight <= 0.0f) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float targetAspect = targetWidth / targetHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scale = windowAspect / targetAspect;

        Rect rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

        if (scale < 1.0f)
        {
            // 画面のほうが縦長 → 上下に帯
            rect.height = scale;
            rect.y = (1.0f - scale) * 0.5f;
        }
        else
        {
            // 画面のほうが横長 → 左右に帯
            float width = 1.0f / scale;
            rect.width = width;
            rect.x = (1.0f - width) * 0.5f;
        }

        targetCamera.rect = rect;

        if (extraCameras != null)
        {
            for (int i = 0; i < extraCameras.Length; i++)
            {
                if (extraCameras[i] == null) continue;
                if (extraCameras[i] == letterboxCamera) continue;
                extraCameras[i].rect = rect;
            }
        }
    }

    // 帯の部分は本体カメラが描かないので、後ろで全画面を塗るカメラを置く
    void SetupLetterboxCamera()
    {
        if (createLetterboxCamera == false) return;
        if (letterboxCamera != null) return;

        GameObject go = new GameObject("LetterboxCamera");
        go.transform.SetParent(transform, false);

        letterboxCamera = go.AddComponent<Camera>();
        letterboxCamera.clearFlags = CameraClearFlags.SolidColor;
        letterboxCamera.backgroundColor = letterboxColor;
        letterboxCamera.cullingMask = 0;
        letterboxCamera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
        // 本体カメラより先に描かせる
        letterboxCamera.depth = targetCamera.depth - 100.0f;
        letterboxCamera.allowHDR = false;
        letterboxCamera.allowMSAA = false;
    }
}
