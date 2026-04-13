using UnityEngine;

/// <summary>
/// スキージャンパーの動作を管理するコンポーネント。
///
/// 状態遷移:
///   Waiting  → 待機中（スタートを待つ）
///   Sliding  → 滑走中（パスに沿って移動、スピードアップ）
///   Flying   → 飛行中（物理演算による放物線飛行）
///   Landed   → 着地済み
///
/// 操作:
///   Waiting 中: クリック / スペース → 滑走開始
///   Sliding 中: クリック / スペース → ジャンプ
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SkiJumper : MonoBehaviour
{
    public static SkiJumper Instance { get; private set; }

    // ── パラメータ ────────────────────────────────────────────────────────────

    [Header("Sliding")]
    [Tooltip("滑走開始時の初速 (m/s)")]
    public float initialSpeed = 5f;

    [Tooltip("滑走の最高速度 (m/s)")]
    public float maxSpeed = 28f;

    [Tooltip("滑走中の加速度 (m/s²)")]
    public float slideAcceleration = 10f;

    [Header("Jump")]
    [Tooltip("ジャンプ時の追加上方向速度")]
    public float jumpBoost = 5f;

    // ── 状態 ─────────────────────────────────────────────────────────────────

    public enum JumperState { Waiting, Sliding, Flying, Landed }
    public JumperState CurrentState { get; private set; }

    // ── 内部変数 ──────────────────────────────────────────────────────────────

    private Rigidbody2D _rb;

    private Vector2[] _rampPath;     // 滑走パスの頂点列
    private Vector2   _takeoffPoint; // テイクオフ（ジャンプ台先端）座標

    private int   _seg;   // 現在いるセグメントのインデックス
    private float _segT;  // セグメント内の進行度 [0, 1]
    private float _speed; // 現在速度

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>SceneSetup から呼ばれる初期化。パスと離陸点を受け取る。</summary>
    public void Initialize(Vector2[] rampPath, Vector2 takeoffPoint)
    {
        _rampPath     = rampPath;
        _takeoffPoint = takeoffPoint;
        ResetPlayer();
    }

    /// <summary>プレイヤーを初期位置・初期状態にリセットする。</summary>
    public void ResetPlayer()
    {
        _seg   = 0;
        _segT  = 0f;
        _speed = 0f;
        CurrentState = JumperState.Waiting;

        _rb.isKinematic      = true;
        _rb.velocity         = Vector2.zero;
        _rb.angularVelocity  = 0f;
        _rb.rotation         = 0f;

        if (_rampPath != null && _rampPath.Length > 0)
            transform.position = _rampPath[0];

        transform.rotation = Quaternion.identity;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.State != GameManager.GameState.Playing)
            return;

        bool pressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);

        switch (CurrentState)
        {
            case JumperState.Waiting:
                if (pressed) BeginSlide();
                break;

            case JumperState.Sliding:
                UpdateSlide();
                if (pressed) DoJump();
                break;

            case JumperState.Flying:
                AlignToVelocity();
                break;
        }
    }

    // ── 滑走 ─────────────────────────────────────────────────────────────────

    private void BeginSlide()
    {
        CurrentState = JumperState.Sliding;
        _speed = initialSpeed;
    }

    private void UpdateSlide()
    {
        // 徐々に最高速度まで加速
        _speed = Mathf.MoveTowards(_speed, maxSpeed, slideAcceleration * Time.deltaTime);
        MoveAlongRamp();
    }

    /// <summary>パス上を指定距離だけ進める。</summary>
    private void MoveAlongRamp()
    {
        float distLeft = _speed * Time.deltaTime;

        while (distLeft > Mathf.Epsilon)
        {
            // パスの末端に到達 → 自動ジャンプ
            if (_seg >= _rampPath.Length - 1)
            {
                transform.position = _rampPath[_rampPath.Length - 1];
                DoJump();
                return;
            }

            float segLen = Vector2.Distance(_rampPath[_seg], _rampPath[_seg + 1]);
            if (segLen < Mathf.Epsilon) { _seg++; continue; }

            float remaining = segLen * (1f - _segT);

            if (distLeft >= remaining)
            {
                distLeft -= remaining;
                _seg++;
                _segT = 0f;
            }
            else
            {
                _segT += distLeft / segLen;
                distLeft = 0f;
            }
        }

        // 位置・回転を更新
        if (_seg < _rampPath.Length - 1)
        {
            transform.position = Vector2.Lerp(_rampPath[_seg], _rampPath[_seg + 1], _segT);

            Vector2 dir  = (_rampPath[_seg + 1] - _rampPath[_seg]).normalized;
            float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // ── ジャンプ ──────────────────────────────────────────────────────────────

    private void DoJump()
    {
        if (CurrentState == JumperState.Flying || CurrentState == JumperState.Landed)
            return;

        CurrentState = JumperState.Flying;

        // 現在のランプ方向を取得
        Vector2 rampDir;
        if (_seg < _rampPath.Length - 1)
            rampDir = (_rampPath[_seg + 1] - _rampPath[_seg]).normalized;
        else
            rampDir = (_rampPath[_rampPath.Length - 1] - _rampPath[_rampPath.Length - 2]).normalized;

        // 物理演算を有効化し、速度を与える
        _rb.isKinematic = false;
        _rb.gravityScale = 1.5f;
        _rb.velocity = rampDir * _speed + Vector2.up * jumpBoost;
    }

    // ── 飛行 ─────────────────────────────────────────────────────────────────

    /// <summary>飛行中、速度ベクトルの方向に体を向ける。</summary>
    private void AlignToVelocity()
    {
        if (_rb.velocity.sqrMagnitude < 0.1f) return;

        float targetAngle  = Mathf.Atan2(_rb.velocity.y, _rb.velocity.x) * Mathf.Rad2Deg;
        float currentAngle = transform.eulerAngles.z;
        float newAngle     = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * 5f);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    // ── 着地 ─────────────────────────────────────────────────────────────────

    private void OnCollisionEnter2D(Collision2D col)
    {
        // 飛行中のみ着地判定
        if (CurrentState != JumperState.Flying) return;
        if (!col.gameObject.CompareTag("Ground"))  return;

        Land(col.contacts[0].point);
    }

    private void Land(Vector2 landingPoint)
    {
        CurrentState        = JumperState.Landed;
        _rb.velocity        = Vector2.zero;
        _rb.isKinematic     = true;

        // テイクオフ点からの飛距離（ユークリッド距離）
        float distance = Vector2.Distance(_takeoffPoint, landingPoint);

        GameManager.Instance?.EndGame(distance);
    }

    // ── ゲッター ──────────────────────────────────────────────────────────────

    /// <summary>現在の滑走速度を返す（UI 表示用）。</summary>
    public float GetSpeed() => _speed;
}
