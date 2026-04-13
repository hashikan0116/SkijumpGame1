using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ゲームシーン全体をランタイムに構築するブートストラッパー。
///
/// 【使い方】
///   1. Unity エディタでシーンを開く（SampleScene など）
///   2. ヒエラルキーに空の GameObject "SceneSetup" を作成
///   3. このスクリプトをアタッチ
///   4. Play ボタンで実行 → すべての地形・プレイヤー・UI が生成される
///
/// 【座標系】
///   1 Unity単位 ≈ 1 m として設計。
///   ランプ先端（テイクオフ）は原点付近 (2, 3) に配置。
///   良いジャンプで約 30〜55 m の飛距離が出るようなスケール。
/// </summary>
[DefaultExecutionOrder(-200)]
public class SceneSetup : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  地形定義
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// インラン（助走路）のパス頂点列。
    /// 先頭が待機位置、末尾がテイクオフ点。
    /// </summary>
    private static readonly Vector2[] RampPath =
    {
        new Vector2(-22f, 14f),  // 0: 待機位置（スタート）
        new Vector2(-18f, 12f),  // 1
        new Vector2(-13f,  9f),  // 2
        new Vector2( -8f,  6f),  // 3
        new Vector2( -4f, 3.5f), // 4
        new Vector2( -1f, 2.2f), // 5: 谷部（テーブル入口）
        new Vector2(0.5f, 2.5f), // 6: テーブル
        new Vector2(  2f,  3f),  // 7: テイクオフ（ランプ先端）
    };

    /// <summary>
    /// 着地斜面のパス頂点列（テイクオフ点から平地まで）。
    /// "Ground" タグで物理コライダー付き。
    /// </summary>
    private static readonly Vector2[] LandingPath =
    {
        new Vector2(  2f,   3f),
        new Vector2(  6f, 1.5f),
        new Vector2( 12f,  -1f),
        new Vector2( 20f,  -4f),
        new Vector2( 30f,  -7f),
        new Vector2( 42f, -9.5f),
        new Vector2( 56f, -10f),
        new Vector2( 75f, -10f),  // 平地（アウトラン）
    };

    // テイクオフ点 = ランプパスの末端
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
        CreateManagers();  // GameManager + UIManager + UI
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  カメラ設定
    // ═══════════════════════════════════════════════════════════════════════════

    private void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // ランプ全体が見渡せる初期位置
        cam.transform.position = new Vector3(-4f, 6f, -10f);
        cam.orthographic       = true;
        cam.orthographicSize   = 14f;

        // 空色の背景
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.47f, 0.65f, 0.85f);

        // CameraController をアタッチ
        var cc = cam.gameObject.AddComponent<CameraController>();
        cc.smoothSpeed   = 3.5f;
        cc.flightOffset  = new Vector3(6f, 2f, -10f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  地形生成
    // ═══════════════════════════════════════════════════════════════════════════

    private void CreateLandingHill()
    {
        var go  = new GameObject("LandingHill");
        go.tag  = "Ground";

        // 物理コライダー（プレイヤーが着地するために必要）
        var col    = go.AddComponent<EdgeCollider2D>();
        col.points = LandingPath;
        col.edgeRadius = 0.05f;

        // ビジュアル：雪の白
        AddLineRenderer(go, LandingPath, 0.3f, new Color(0.88f, 0.94f, 1f), sortOrder: 1);
    }

    private void CreateRamp()
    {
        // ── 滑走面（ビジュアルのみ。物理は不要＝パス移動で制御） ──
        var ramp = new GameObject("Ramp");
        AddLineRenderer(ramp, RampPath, 0.25f, Color.white, sortOrder: 2);

        // ── ランプ鉄塔（ビジュアルのみ） ──
        var tower = new GameObject("RampTower");
        AddLineRenderer(tower,
            new Vector2[]
            {
                new Vector2(-22f, 14f),
                new Vector2(-22f,  0f),
                new Vector2(  2f,  0f),
                new Vector2(  2f,  3f),
            },
            0.2f, new Color(0.65f, 0.65f, 0.65f), sortOrder: 0);

        // ── 地面ベースライン（ビジュアルのみ） ──
        var ground = new GameObject("GroundBase");
        AddLineRenderer(ground,
            new Vector2[]
            {
                new Vector2(-25f, 0f),
                new Vector2( 75f, 0f),
            },
            0.2f, new Color(0.88f, 0.94f, 1f), sortOrder: 0);

        // ── テイクオフマーカー（黄色の円） ──
        CreateCircleMarker("TakeoffMarker", Takeoff, 0.4f, new Color(1f, 0.85f, 0f));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  プレイヤー生成
    // ═══════════════════════════════════════════════════════════════════════════

    private void CreatePlayer()
    {
        var go = new GameObject("SkiJumper");

        // Rigidbody2D（飛行フェーズで使用）
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale          = 1.5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation          = RigidbodyInterpolation2D.Interpolate;

        // コライダー（横向きカプセル ≈ スキージャンパーの体）
        var col       = go.AddComponent<CapsuleCollider2D>();
        col.size      = new Vector2(0.8f, 0.35f);
        col.direction = CapsuleDirection2D.Horizontal;

        // ビジュアル（赤い矩形スプライト）
        var sr         = go.AddComponent<SpriteRenderer>();
        sr.sprite      = MakeRectSprite(0.8f, 0.35f);
        sr.color       = new Color(0.9f, 0.15f, 0.15f);
        sr.sortingOrder = 5;

        // SkiJumper スクリプトをアタッチ
        go.AddComponent<SkiJumper>();

        // Initialize はすべての Awake() が終わった後に行う
        StartCoroutine(LateInitPlayer(go));
    }

    private IEnumerator LateInitPlayer(GameObject playerGo)
    {
        yield return null;  // 1フレーム待つ

        var jumper = playerGo.GetComponent<SkiJumper>();
        if (jumper != null)
            jumper.Initialize(RampPath, Takeoff);

        // カメラのターゲットをプレイヤーに設定
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
        CreateUI();  // UIManager は Canvas に追加
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UI 生成
    // ═══════════════════════════════════════════════════════════════════════════

    private void CreateUI()
    {
        // ── Canvas ──────────────────────────────────────────────────────────
        var canvasGo  = new GameObject("Canvas");
        var canvas    = canvasGo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // UIManager を Canvas に追加
        var ui = canvasGo.AddComponent<UIManager>();

        // ── スタート画面 ─────────────────────────────────────────────────────
        var startPanel = MakePanel(canvasGo, "StartPanel", new Color(0f, 0.08f, 0.25f, 0.88f));
        {
            MakeText(startPanel, "Title",
                "スキージャンプ",
                anchorPos: new Vector2(0f, 120f), size: new Vector2(800f, 100f),
                fontSize: 60, bold: true, color: Color.white);

            MakeText(startPanel, "Sub",
                "タイミングよくジャンプしろ！",
                anchorPos: new Vector2(0f, 30f), size: new Vector2(700f, 50f),
                fontSize: 26, bold: false, color: new Color(0.8f, 0.9f, 1f));

            MakeText(startPanel, "Hint",
                "クリック / スペースキー で操作",
                anchorPos: new Vector2(0f, -30f), size: new Vector2(700f, 40f),
                fontSize: 20, bold: false, color: new Color(0.7f, 0.8f, 1f));

            ui.startButton = MakeButton(startPanel, "StartBtn", "ゲームスタート",
                anchorPos: new Vector2(0f, -130f), size: new Vector2(300f, 70f));
        }
        ui.startPanel = startPanel;

        // ── ゲーム画面 ───────────────────────────────────────────────────────
        var gamePanel = MakePanel(canvasGo, "GamePanel", Color.clear);
        gamePanel.GetComponent<Image>().raycastTarget = false;
        {
            // 操作ガイド（画面下中央）
            var instrBg = MakePanel(gamePanel, "InstrBg", new Color(1f, 1f, 1f, 0.72f));
            instrBg.GetComponent<RectTransform>().sizeDelta      = new Vector2(760f, 52f);
            instrBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -470f);
            instrBg.GetComponent<RectTransform>().anchorMin       = new Vector2(0.5f, 0.5f);
            instrBg.GetComponent<RectTransform>().anchorMax       = new Vector2(0.5f, 0.5f);

            ui.instructionText = MakeText(instrBg, "InstrText",
                "クリック / スペースキー : 滑走スタート",
                anchorPos: Vector2.zero, size: new Vector2(740f, 48f),
                fontSize: 22, bold: false, color: new Color(0.1f, 0.1f, 0.1f));

            // 速度表示（画面左上）
            ui.speedText = MakeText(gamePanel, "SpeedText",
                "",
                anchorPos: new Vector2(-700f, 490f), size: new Vector2(280f, 44f),
                fontSize: 22, bold: false, color: Color.white);
        }
        ui.gamePanel = gamePanel;

        // ── リザルト画面 ──────────────────────────────────────────────────────
        var resultPanel = MakePanel(canvasGo, "ResultPanel", new Color(0f, 0.08f, 0.25f, 0.88f));
        {
            MakeText(resultPanel, "ResultTitle",
                "リザルト",
                anchorPos: new Vector2(0f, 150f), size: new Vector2(500f, 80f),
                fontSize: 52, bold: true, color: Color.white);

            ui.resultDistanceText = MakeText(resultPanel, "DistanceText",
                "飛距離: -- m",
                anchorPos: new Vector2(0f, 30f), size: new Vector2(600f, 90f),
                fontSize: 52, bold: true, color: new Color(1f, 0.92f, 0.3f));

            MakeText(resultPanel, "RatingText",
                "",  // 将来的にランク表示などに使用可能
                anchorPos: new Vector2(0f, -50f), size: new Vector2(500f, 50f),
                fontSize: 28, bold: false, color: new Color(0.8f, 0.9f, 1f));

            ui.retryButton = MakeButton(resultPanel, "RetryBtn", "もう一度",
                anchorPos: new Vector2(-110f, -140f), size: new Vector2(200f, 65f));

            ui.backButton = MakeButton(resultPanel, "BackBtn", "タイトルへ",
                anchorPos: new Vector2(110f, -140f), size: new Vector2(200f, 65f));
        }
        ui.resultPanel = resultPanel;

        // 初期表示設定（スタート画面のみ表示）
        gamePanel  .SetActive(false);
        resultPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UI ヘルパー
    // ═══════════════════════════════════════════════════════════════════════════

    private GameObject MakePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt        = go.AddComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;

        go.AddComponent<Image>().color = color;
        return go;
    }

    private TextMeshProUGUI MakeText(
        GameObject parent, string name, string text,
        Vector2 anchorPos, Vector2 size,
        int fontSize, bool bold, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchorPos;
        rt.sizeDelta        = size;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text              = text;
        tmp.fontSize          = fontSize;
        tmp.fontStyle         = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color             = color;
        tmp.alignment         = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode      = TextOverflowModes.Overflow;
        return tmp;
    }

    private Button MakeButton(
        GameObject parent, string name, string label,
        Vector2 anchorPos, Vector2 size)
    {
        // ── ボタン本体 ──
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchorPos;
        rt.sizeDelta        = size;

        go.AddComponent<Image>().color = new Color(0.12f, 0.50f, 0.88f);

        var btn   = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.2f, 0.62f, 1f);
        colors.pressedColor     = new Color(0.08f, 0.38f, 0.72f);
        btn.colors = colors;

        // ── ラベル ──
        var lblGo = new GameObject("Label");
        lblGo.transform.SetParent(go.transform, false);

        var lrt       = lblGo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        var tmp = lblGo.AddComponent<TextMeshProUGUI>();
        tmp.text              = label;
        tmp.fontSize          = 24;
        tmp.fontStyle         = FontStyles.Bold;
        tmp.color             = Color.white;
        tmp.alignment         = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        return btn;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ビジュアルヘルパー
    // ═══════════════════════════════════════════════════════════════════════════

    private void AddLineRenderer(
        GameObject go, Vector2[] points,
        float width, Color color, int sortOrder)
    {
        var lr         = go.AddComponent<LineRenderer>();
        lr.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++)
            lr.SetPosition(i, (Vector3)points[i]);

        lr.startWidth    = width;
        lr.endWidth      = width;
        lr.startColor    = color;
        lr.endColor      = color;
        lr.useWorldSpace = true;
        lr.sortingOrder  = sortOrder;

        // URP でも動作するシェーダーを順番に試す
        lr.material = MakeLineMaterial();
    }

    private void CreateCircleMarker(string name, Vector2 center, float radius, Color color)
    {
        var go  = new GameObject(name);
        var lr  = go.AddComponent<LineRenderer>();

        const int segs = 24;
        lr.positionCount = segs + 1;
        lr.loop          = false;

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

    /// <summary>Built-in RP / URP どちらでも使えるマテリアルを返す。</summary>
    private Material MakeLineMaterial()
    {
        // URP 環境では "Sprites/Default" が使えないことがあるため順番に探す
        string[] candidates =
        {
            "Sprites/Default",
            "Universal Render Pipeline/2D/Sprite-Lit-Default",
            "Unlit/Color",
        };
        foreach (var s in candidates)
        {
            var sh = Shader.Find(s);
            if (sh != null) return new Material(sh);
        }
        return new Material(Shader.Find("Standard"));
    }

    /// <summary>単色の矩形スプライトを生成する（プレイヤービジュアル用）。</summary>
    private Sprite MakeRectSprite(float worldW, float worldH)
    {
        const float ppu = 20f;
        int pw = Mathf.Max(1, Mathf.RoundToInt(worldW * ppu));
        int ph = Mathf.Max(1, Mathf.RoundToInt(worldH * ppu));

        var tex    = new Texture2D(pw, ph);
        var pixels = new Color[pw * ph];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, pw, ph),
                             new Vector2(0.5f, 0.5f), ppu);
    }
}
