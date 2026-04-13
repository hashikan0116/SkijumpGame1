using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ゲームシーン全体をランタイムに構築するブートストラッパー。
///
/// 【使い方】
///   1. Unity のシーンにある SceneSetup GameObject にアタッチ
///   2. Play ボタンを押すだけ
///
/// 【3Dモデル差し替え】
///   Assets/Resources/ に "DummyJumper" という名前のプレハブ/モデルを置くと
///   赤箱の代わりに 3D モデルが使われる。
/// </summary>
[DefaultExecutionOrder(-200)]
public class SceneSetup : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  地形定義
    // ═══════════════════════════════════════════════════════════════════════════

    // インラン（助走路）パス：先頭が待機位置、末尾がテイクオフ点
    private static readonly Vector2[] RampPath =
    {
        new Vector2(-22f, 14f),   // 0: 待機位置
        new Vector2(-18f, 12f),   // 1
        new Vector2(-13f,  9f),   // 2
        new Vector2( -8f,  6f),   // 3
        new Vector2( -4f, 3.5f),  // 4  ← ここからジャンプゾーン
        new Vector2( -1f, 2.2f),  // 5
        new Vector2(0.5f, 2.5f),  // 6
        new Vector2(  2f,  3f),   // 7: テイクオフ（ランプ先端）
    };

    // 着地斜面：テイクオフより少し先から開始（直後の誤着地を防ぐ）
    private static readonly Vector2[] LandingPath =
    {
        new Vector2(  6f, 1.5f),
        new Vector2( 12f,  -1f),
        new Vector2( 20f,  -4f),
        new Vector2( 30f,  -7f),
        new Vector2( 42f, -9.5f),
        new Vector2( 56f, -10f),
        new Vector2( 75f, -10f),
    };

    private static readonly Vector2 Takeoff = new Vector2(2f, 3f);

    // ═══════════════════════════════════════════════════════════════════════════
    //  エントリーポイント
    // ═══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        SetupCamera();
        CreateLandingHill();
        CreateRamp();
        CreatePlayer();
        CreateManagers();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  カメラ設定
    // ═══════════════════════════════════════════════════════════════════════════

    private void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        cam.transform.position = new Vector3(-4f, 6f, -10f);
        cam.orthographic       = true;
        cam.orthographicSize   = 14f;
        cam.clearFlags         = CameraClearFlags.SolidColor;
        cam.backgroundColor    = new Color(0.47f, 0.65f, 0.85f);

        var cc = cam.gameObject.AddComponent<CameraController>();
        cc.smoothSpeed  = 3.5f;
        cc.flightOffset = new Vector3(6f, 2f, -10f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  地形生成
    // ═══════════════════════════════════════════════════════════════════════════

    private void CreateLandingHill()
    {
        var go = new GameObject("LandingHill");
        go.tag = "Ground";

        var col    = go.AddComponent<EdgeCollider2D>();
        col.points = LandingPath;
        col.edgeRadius = 0.05f;

        AddLineRenderer(go, LandingPath, 0.3f, new Color(0.88f, 0.94f, 1f), sortOrder: 1);
    }

    private void CreateRamp()
    {
        // 滑走面（ビジュアルのみ）
        var ramp = new GameObject("Ramp");
        AddLineRenderer(ramp, RampPath, 0.25f, Color.white, sortOrder: 2);

        // 鉄塔
        var tower = new GameObject("RampTower");
        AddLineRenderer(tower, new Vector2[]
        {
            new Vector2(-22f, 14f),
            new Vector2(-22f,  0f),
            new Vector2(  2f,  0f),
            new Vector2(  2f,  3f),
        }, 0.2f, new Color(0.65f, 0.65f, 0.65f), sortOrder: 0);

        // 地面ベース
        var ground = new GameObject("GroundBase");
        AddLineRenderer(ground, new Vector2[]
        {
            new Vector2(-25f, 0f),
            new Vector2( 75f, 0f),
        }, 0.2f, new Color(0.88f, 0.94f, 1f), sortOrder: 0);

        // テイクオフマーカー（黄色の円）
        CreateCircleMarker("TakeoffMarker", Takeoff, 0.4f, new Color(1f, 0.85f, 0f));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  プレイヤー生成
    // ═══════════════════════════════════════════════════════════════════════════

    private void CreatePlayer()
    {
        var go = new GameObject("SkiJumper");

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 1.5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation          = RigidbodyInterpolation2D.Interpolate;

        var col       = go.AddComponent<CapsuleCollider2D>();
        col.size      = new Vector2(0.8f, 0.35f);
        col.direction = CapsuleDirection2D.Horizontal;

        // Resources フォルダの DummyJumper を読み込む
        var modelPrefab = Resources.Load<GameObject>("DummyJumper");
        if (modelPrefab != null)
        {
            var model = Instantiate(modelPrefab, go.transform);
            model.transform.localPosition = Vector3.zero;
            // 見た目がおかしい場合は Y 値を 90/-90/0/180 で調整
            model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            // 大きさがおかしい場合は数値を調整
            model.transform.localScale    = Vector3.one * 0.5f;
        }
        else
        {
            Debug.LogWarning("[SceneSetup] DummyJumper が Resources フォルダに見つかりません。赤箱で代替します。");
            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeRectSprite(0.8f, 0.35f);
            sr.color        = new Color(0.9f, 0.15f, 0.15f);
            sr.sortingOrder = 5;
        }

        go.AddComponent<SkiJumper>();
        StartCoroutine(LateInitPlayer(go));
    }

    private IEnumerator LateInitPlayer(GameObject playerGo)
    {
        yield return null;

        var jumper = playerGo.GetComponent<SkiJumper>();
        if (jumper != null)
            jumper.Initialize(RampPath, Takeoff);

        if (Camera.main != null)
        {
            var cc = Camera.main.GetComponent<CameraController>();
            if (cc != null) cc.target = playerGo.transform;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  マネージャー生成
    // ═══════════════════════════════════════════════════════════════════════════

    private void CreateManagers()
    {
        new GameObject("GameManager").AddComponent<GameManager>();
        CreateUI();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UI 生成
    // ═══════════════════════════════════════════════════════════════════════════

    private void CreateUI()
    {
        // EventSystem がなければ作成（ボタンクリックに必要）
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem")
                .AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

        var canvasGo = new GameObject("Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        var ui = canvasGo.AddComponent<UIManager>();

        // ── Start Screen ────────────────────────────────────────────────────
        var sp = MakePanel(canvasGo, "StartPanel", new Color(0f, 0.08f, 0.25f, 0.88f));
        MakeText(sp, "Title", "SKI JUMP",
            new Vector2(0f, 120f), new Vector2(800f, 100f), 60, true, Color.white);
        MakeText(sp, "Sub", "Jump at the right moment!",
            new Vector2(0f, 30f), new Vector2(700f, 50f), 26, false, new Color(0.8f, 0.9f, 1f));
        MakeText(sp, "Hint", "Click / Space to control",
            new Vector2(0f, -30f), new Vector2(700f, 40f), 20, false, new Color(0.7f, 0.8f, 1f));
        ui.startButton = MakeButton(sp, "StartBtn", "GAME START",
            new Vector2(0f, -130f), new Vector2(300f, 70f));
        ui.startPanel = sp;

        // ── Game Screen ─────────────────────────────────────────────────────
        var gp = MakePanel(canvasGo, "GamePanel", Color.clear);
        gp.GetComponent<Image>().raycastTarget = false;

        // 操作ガイド（下中央）
        var instrBg = MakePanel(gp, "InstrBg", new Color(1f, 1f, 1f, 0.72f));
        var instrBgRt = instrBg.GetComponent<RectTransform>();
        instrBgRt.anchorMin        = new Vector2(0.5f, 0.5f);
        instrBgRt.anchorMax        = new Vector2(0.5f, 0.5f);
        instrBgRt.anchoredPosition = new Vector2(0f, -470f);
        instrBgRt.sizeDelta        = new Vector2(760f, 52f);
        ui.instructionText = MakeText(instrBg, "InstrText",
            "Click / Space : Start sliding",
            Vector2.zero, new Vector2(740f, 48f), 22, false, new Color(0.1f, 0.1f, 0.1f));

        // 速度表示（左上）
        ui.speedText = MakeText(gp, "SpeedText", "",
            new Vector2(-700f, 490f), new Vector2(280f, 44f), 22, false, Color.white);

        // カメラ切り替えボタン（左下）
        var camBtnGo = new GameObject("CamSwitchBtn");
        camBtnGo.transform.SetParent(gp.transform, false);
        var cbRt = camBtnGo.AddComponent<RectTransform>();
        cbRt.anchorMin        = new Vector2(0f, 0f);
        cbRt.anchorMax        = new Vector2(0f, 0f);
        cbRt.anchoredPosition = new Vector2(90f, 45f);
        cbRt.sizeDelta        = new Vector2(160f, 50f);
        camBtnGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        var camBtn    = camBtnGo.AddComponent<Button>();
        var camColors = camBtn.colors;
        camColors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        camBtn.colors = camColors;
        camBtn.onClick.AddListener(() => CameraController.Instance?.SwitchMode());
        var camLblGo = new GameObject("Label");
        camLblGo.transform.SetParent(camBtnGo.transform, false);
        var clRt = camLblGo.AddComponent<RectTransform>();
        clRt.anchorMin = Vector2.zero;
        clRt.anchorMax = Vector2.one;
        clRt.offsetMin = clRt.offsetMax = Vector2.zero;
        var camTmp = camLblGo.AddComponent<TextMeshProUGUI>();
        camTmp.text               = "FP View";
        camTmp.fontSize           = 18;
        camTmp.fontStyle          = FontStyles.Bold;
        camTmp.color              = Color.white;
        camTmp.alignment          = TextAlignmentOptions.Center;
        camTmp.enableWordWrapping = false;
        ui.camSwitchButton = camBtn;
        ui.camSwitchLabel  = camTmp;

        ui.gamePanel = gp;

        // ── Result Screen ────────────────────────────────────────────────────
        var rp = MakePanel(canvasGo, "ResultPanel", new Color(0f, 0.08f, 0.25f, 0.88f));
        MakeText(rp, "ResultTitle", "RESULT",
            new Vector2(0f, 150f), new Vector2(500f, 80f), 52, true, Color.white);
        ui.resultDistanceText = MakeText(rp, "DistanceText", "Distance: -- m",
            new Vector2(0f, 30f), new Vector2(600f, 90f), 52, true, new Color(1f, 0.92f, 0.3f));
        ui.retryButton = MakeButton(rp, "RetryBtn", "RETRY",
            new Vector2(-110f, -140f), new Vector2(200f, 65f));
        ui.backButton = MakeButton(rp, "BackBtn", "TITLE",
            new Vector2(110f, -140f), new Vector2(200f, 65f));
        ui.resultPanel = rp;

        gp.SetActive(false);
        rp.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UI ヘルパー
    // ═══════════════════════════════════════════════════════════════════════════

    private GameObject MakePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private TextMeshProUGUI MakeText(GameObject parent, string name, string text,
        Vector2 anchorPos, Vector2 size, int fontSize, bool bold, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchorPos;
        rt.sizeDelta        = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.fontSize           = fontSize;
        tmp.fontStyle          = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color              = color;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode       = TextOverflowModes.Overflow;
        return tmp;
    }

    private Button MakeButton(GameObject parent, string name, string label,
        Vector2 anchorPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchorPos;
        rt.sizeDelta        = size;
        go.AddComponent<Image>().color = new Color(0.12f, 0.50f, 0.88f);
        var btn    = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.2f, 0.62f, 1f);
        colors.pressedColor     = new Color(0.08f, 0.38f, 0.72f);
        btn.colors = colors;

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(go.transform, false);
        var lrt       = lbl.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text               = label;
        tmp.fontSize           = 24;
        tmp.fontStyle          = FontStyles.Bold;
        tmp.color              = Color.white;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        return btn;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ビジュアルヘルパー
    // ═══════════════════════════════════════════════════════════════════════════

    private void AddLineRenderer(GameObject go, Vector2[] points,
        float width, Color color, int sortOrder)
    {
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++)
            lr.SetPosition(i, (Vector3)points[i]);
        lr.startWidth    = width;
        lr.endWidth      = width;
        lr.startColor    = color;
        lr.endColor      = color;
        lr.useWorldSpace = true;
        lr.sortingOrder  = sortOrder;
        lr.material      = MakeLineMaterial();
    }

    private void CreateCircleMarker(string name, Vector2 center, float radius, Color color)
    {
        var go = new GameObject(name);
        var lr = go.AddComponent<LineRenderer>();
        const int segs = 24;
        lr.positionCount = segs + 1;
        for (int i = 0; i <= segs; i++)
        {
            float a = i * 2f * Mathf.PI / segs;
            lr.SetPosition(i, (Vector3)center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius);
        }
        lr.startWidth    = 0.12f;
        lr.endWidth      = 0.12f;
        lr.startColor    = color;
        lr.endColor      = color;
        lr.useWorldSpace = true;
        lr.sortingOrder  = 3;
        lr.material      = MakeLineMaterial();
    }

    private Material MakeLineMaterial()
    {
        string[] candidates = { "Sprites/Default", "Universal Render Pipeline/2D/Sprite-Lit-Default", "Unlit/Color" };
        foreach (var s in candidates)
        {
            var sh = Shader.Find(s);
            if (sh != null) return new Material(sh);
        }
        return new Material(Shader.Find("Standard"));
    }

    private Sprite MakeRectSprite(float worldW, float worldH)
    {
        const float ppu = 20f;
        int pw  = Mathf.Max(1, Mathf.RoundToInt(worldW * ppu));
        int ph  = Mathf.Max(1, Mathf.RoundToInt(worldH * ppu));
        var tex = new Texture2D(pw, ph);
        var pixels = new Color[pw * ph];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, pw, ph), new Vector2(0.5f, 0.5f), ppu);
    }
}
