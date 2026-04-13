using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [HideInInspector] public GameObject startPanel;
    [HideInInspector] public GameObject gamePanel;
    [HideInInspector] public GameObject resultPanel;

    [HideInInspector] public Button startButton;

    [HideInInspector] public TextMeshProUGUI instructionText;
    [HideInInspector] public TextMeshProUGUI speedText;

    [HideInInspector] public TextMeshProUGUI resultDistanceText;
    [HideInInspector] public Button          retryButton;
    [HideInInspector] public Button          backButton;

    // カメラ切り替えボタン
    [HideInInspector] public Button          camSwitchButton;
    [HideInInspector] public TextMeshProUGUI camSwitchLabel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        startButton?.onClick.AddListener(() => GameManager.Instance?.StartGame());
        retryButton?.onClick.AddListener(() => GameManager.Instance?.StartGame());
        backButton? .onClick.AddListener(() => GameManager.Instance?.ReturnToTitle());
    }

    public void UpdateUI(GameManager.GameState state)
    {
        startPanel? .SetActive(state == GameManager.GameState.Start);
        gamePanel?  .SetActive(state == GameManager.GameState.Playing);
        resultPanel?.SetActive(state == GameManager.GameState.Result);
    }

    public void SetResultDistance(float distance)
    {
        if (resultDistanceText != null)
            resultDistanceText.text = $"Distance: {distance:F1} m";
    }

    private void Update()
    {
        if (GameManager.Instance?.State != GameManager.GameState.Playing) return;

        var jumper = SkiJumper.Instance;
        if (jumper == null) return;

        // 速度表示
        if (speedText != null)
        {
            speedText.text = jumper.CurrentState == SkiJumper.JumperState.Sliding
                ? $"Speed: {jumper.GetSpeed():F1} m/s"
                : string.Empty;
        }

        // 操作ガイド
        if (instructionText != null)
        {
            switch (jumper.CurrentState)
            {
                case SkiJumper.JumperState.Waiting:
                    instructionText.text = "Click / Space : Start sliding";
                    break;
                case SkiJumper.JumperState.Sliding:
                    bool inZone = jumper.IsInJumpZone;
                    instructionText.text = inZone
                        ? "Click / Space : JUMP!"
                        : "Gain speed!  Jump near the takeoff!";
                    break;
                default:
                    instructionText.text = string.Empty;
                    break;
            }
        }

        // カメラボタンのラベル更新
        if (camSwitchLabel != null && CameraController.Instance != null)
        {
            camSwitchLabel.text =
                CameraController.Instance.CurrentMode == CameraController.CameraMode.Side
                ? "FP View"
                : "Side View";
        }
    }
}
