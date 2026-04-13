using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI（スタート画面 / ゲーム画面 / リザルト画面）の表示切り替えと
/// テキスト更新を担当するシングルトン。
/// 各パネルの参照は SceneSetup から設定される。
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ── パネル参照（SceneSetup が代入） ──────────────────────────────────────

    [HideInInspector] public GameObject startPanel;
    [HideInInspector] public GameObject gamePanel;
    [HideInInspector] public GameObject resultPanel;

    // スタート画面
    [HideInInspector] public Button startButton;

    // ゲーム画面
    [HideInInspector] public TextMeshProUGUI instructionText;
    [HideInInspector] public TextMeshProUGUI speedText;

    // リザルト画面
    [HideInInspector] public TextMeshProUGUI resultDistanceText;
    [HideInInspector] public Button retryButton;
    [HideInInspector] public Button backButton;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // ボタンイベント登録
        startButton?.onClick.AddListener(() => GameManager.Instance?.StartGame());
        retryButton?.onClick.AddListener(() => GameManager.Instance?.StartGame());
        backButton? .onClick.AddListener(() => GameManager.Instance?.ReturnToTitle());
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>ゲーム状態に合わせてパネルの表示を切り替える。</summary>
    public void UpdateUI(GameManager.GameState state)
    {
        startPanel? .SetActive(state == GameManager.GameState.Start);
        gamePanel?  .SetActive(state == GameManager.GameState.Playing);
        resultPanel?.SetActive(state == GameManager.GameState.Result);
    }

    /// <summary>リザルト画面の飛距離テキストを更新する。</summary>
    public void SetResultDistance(float distance)
    {
        if (resultDistanceText != null)
            resultDistanceText.text = $"飛距離: {distance:F1} m";
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (GameManager.Instance?.State != GameManager.GameState.Playing) return;

        var jumper = SkiJumper.Instance;
        if (jumper == null) return;

        // 速度テキスト更新
        if (speedText != null)
        {
            speedText.text = jumper.CurrentState == SkiJumper.JumperState.Sliding
                ? $"速度: {jumper.GetSpeed():F1} m/s"
                : string.Empty;
        }

        // 操作ガイドテキスト更新
        if (instructionText != null)
        {
            switch (jumper.CurrentState)
            {
                case SkiJumper.JumperState.Waiting:
                    instructionText.text = "クリック / スペースキー : 滑走スタート";
                    break;
                case SkiJumper.JumperState.Sliding:
                    instructionText.text = "クリック / スペースキー : ジャンプ！";
                    break;
                default:
                    instructionText.text = string.Empty;
                    break;
            }
        }
    }
}
