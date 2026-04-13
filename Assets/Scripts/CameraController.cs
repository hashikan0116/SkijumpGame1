using UnityEngine;

/// <summary>
/// カメラ追従コントローラ。
/// - 待機中・滑走中: 初期位置（ランプ全体が見える位置）に固定
/// - 飛行中・着地後: スキージャンパーを追いかけてスムーズに移動
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Tooltip("追従対象（スキージャンパーの Transform）")]
    public Transform target;

    [Tooltip("追従中のスムーズ係数（大きいほど素早く追従）")]
    public float smoothSpeed = 3.5f;

    [Tooltip("ターゲットに対するカメラオフセット（飛行中）")]
    public Vector3 flightOffset = new Vector3(6f, 2f, -10f);

    // ── 内部変数 ──────────────────────────────────────────────────────────────

    private Vector3 _initialPosition;

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
        _initialPosition = transform.position;
    }

    /// <summary>ゲームリスタート時に呼ばれ、カメラを初期位置へ即座に戻す。</summary>
    public void ResetToInitialPosition()
    {
        transform.position = _initialPosition;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        var jumper = SkiJumper.Instance;
        bool follow = jumper != null &&
                      (jumper.CurrentState == SkiJumper.JumperState.Flying ||
                       jumper.CurrentState == SkiJumper.JumperState.Landed);

        Vector3 desired = follow
            ? new Vector3(target.position.x + flightOffset.x,
                          target.position.y + flightOffset.y,
                          flightOffset.z)
            : _initialPosition;

        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
