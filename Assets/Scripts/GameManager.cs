using UnityEngine;

/// <summary>
/// ゲーム全体の状態管理を行うシングルトン。
/// Start → Playing → Result → Start のフローを制御する。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Start, Playing, Result }

    public GameState State { get; private set; }

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
        ChangeState(GameState.Start);
    }

    private void ChangeState(GameState newState)
    {
        State = newState;
        UIManager.Instance?.UpdateUI(newState);
    }

    /// <summary>ゲーム開始（スタート画面 → プレイ中）</summary>
    public void StartGame()
    {
        SkiJumper.Instance?.ResetPlayer();
        CameraController.Instance?.ResetToInitialPosition();
        ChangeState(GameState.Playing);
    }

    /// <summary>ゲーム終了（プレイ中 → リザルト）</summary>
    public void EndGame(float distance)
    {
        ChangeState(GameState.Result);
        UIManager.Instance?.SetResultDistance(distance);
    }

    /// <summary>タイトルへ戻る（リザルト → スタート画面）</summary>
    public void ReturnToTitle()
    {
        ChangeState(GameState.Start);
    }
}
