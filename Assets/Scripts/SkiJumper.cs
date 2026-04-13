using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SkiJumper : MonoBehaviour
{
    public static SkiJumper Instance { get; private set; }

    [Header("Sliding")]
    public float initialSpeed      = 5f;
    public float maxSpeed          = 28f;
    public float slideAcceleration = 10f;

    [Header("Jump")]
    public float jumpBoost = 5f;

    public enum JumperState { Waiting, Sliding, Flying, Landed }
    public JumperState CurrentState { get; private set; }

    // ランプ後半（最後の3セグメント）でのみジャンプ可能
    public bool IsInJumpZone => _rampPath != null && _seg >= _rampPath.Length - 4;

    private Rigidbody2D _rb;
    private Vector2[]   _rampPath;
    private Vector2     _takeoffPoint;
    private int         _seg;
    private float       _segT;
    private float       _speed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2[] rampPath, Vector2 takeoffPoint)
    {
        _rampPath     = rampPath;
        _takeoffPoint = takeoffPoint;
        ResetPlayer();
    }

    public void ResetPlayer()
    {
        _seg   = 0;
        _segT  = 0f;
        _speed = 0f;
        CurrentState = JumperState.Waiting;

        _rb.isKinematic     = true;
        _rb.velocity        = Vector2.zero;
        _rb.angularVelocity = 0f;
        _rb.rotation        = 0f;

        if (_rampPath != null && _rampPath.Length > 0)
            transform.position = _rampPath[0];

        transform.rotation = Quaternion.identity;
    }

    private void Update()
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.State != GameManager.GameState.Playing) return;

        bool pressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);

        switch (CurrentState)
        {
            case JumperState.Waiting:
                if (pressed) BeginSlide();
                break;

            case JumperState.Sliding:
                UpdateSlide();
                // ジャンプはランプ後半ゾーンでのみ有効
                if (pressed && IsInJumpZone) DoJump(true);
                break;

            case JumperState.Flying:
                AlignToVelocity();
                break;
        }
    }

    private void BeginSlide()
    {
        CurrentState = JumperState.Sliding;
        _speed = initialSpeed;
    }

    private void UpdateSlide()
    {
        _speed = Mathf.MoveTowards(_speed, maxSpeed, slideAcceleration * Time.deltaTime);
        MoveAlongRamp();
    }

    private void MoveAlongRamp()
    {
        float distLeft = _speed * Time.deltaTime;

        while (distLeft > Mathf.Epsilon)
        {
            if (_seg >= _rampPath.Length - 1)
            {
                transform.position = _rampPath[_rampPath.Length - 1];
                DoJump(false);  // 自動ジャンプ（ペナルティあり）
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

        if (_seg < _rampPath.Length - 1)
        {
            transform.position = Vector2.Lerp(_rampPath[_seg], _rampPath[_seg + 1], _segT);
            Vector2 dir = (_rampPath[_seg + 1] - _rampPath[_seg]).normalized;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }
    }

    private void DoJump(bool manual)
    {
        if (CurrentState == JumperState.Flying || CurrentState == JumperState.Landed) return;

        CurrentState = JumperState.Flying;

        Vector2 rampDir;
        if (_seg < _rampPath.Length - 1)
            rampDir = (_rampPath[_seg + 1] - _rampPath[_seg]).normalized;
        else
            rampDir = (_rampPath[_rampPath.Length - 1] - _rampPath[_rampPath.Length - 2]).normalized;

        _rb.isKinematic  = false;
        _rb.gravityScale = 1.5f;

        if (manual)
        {
            // 通常ジャンプ：フルスピード＋上方向ブースト
            _rb.velocity = rampDir * _speed + Vector2.up * jumpBoost;
        }
        else
        {
            // 自動ジャンプ（ペナルティ）：速度30%・上昇なし
            _rb.velocity = rampDir * (_speed * 0.3f);
        }
    }

    private void AlignToVelocity()
    {
        if (_rb.velocity.sqrMagnitude < 0.1f) return;
        float angle = Mathf.Atan2(_rb.velocity.y, _rb.velocity.x) * Mathf.Rad2Deg;
        float cur   = transform.eulerAngles.z;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(cur, angle, Time.deltaTime * 5f));
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (CurrentState != JumperState.Flying) return;
        if (!col.gameObject.CompareTag("Ground")) return;
        Land(col.contacts[0].point);
    }

    private void Land(Vector2 landingPoint)
    {
        // テイクオフ直後の誤検出を防ぐ最小距離チェック
        if (Vector2.Distance(_takeoffPoint, landingPoint) < 4f) return;

        CurrentState    = JumperState.Landed;
        _rb.velocity    = Vector2.zero;
        _rb.isKinematic = true;

        float distance = Vector2.Distance(_takeoffPoint, landingPoint);
        GameManager.Instance?.EndGame(distance);
    }

    public float GetSpeed() => _speed;
}
