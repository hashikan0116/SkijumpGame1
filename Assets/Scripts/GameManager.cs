using UnityEngine;

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

    public void StartGame()
    {
        SkiJumper.Instance?.ResetPlayer();
        CameraController.Instance?.ResetToInitialPosition();
        ChangeState(GameState.Playing);
    }

    public void EndGame(float distance)
    {
        ChangeState(GameState.Result);
        UIManager.Instance?.SetResultDistance(distance);
    }

    public void ReturnToTitle()
    {
        ChangeState(GameState.Start);
    }
}
