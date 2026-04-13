using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    public Transform target;
    public float     smoothSpeed  = 3.5f;
    public Vector3   flightOffset = new Vector3(6f, 2f, -10f);

    public enum CameraMode { Side, FirstPerson }
    public CameraMode CurrentMode { get; private set; } = CameraMode.Side;

    private Vector3 _initialPosition;
    private Camera  _cam;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cam = GetComponent<Camera>();
    }

    private void Start()
    {
        _initialPosition = transform.position;
    }

    public void ResetToInitialPosition()
    {
        transform.position = _initialPosition;
        ApplyMode(CameraMode.Side);
    }

    /// <summary>サイド ⇔ 主観視点を切り替える</summary>
    public void SwitchMode()
    {
        ApplyMode(CurrentMode == CameraMode.Side
            ? CameraMode.FirstPerson
            : CameraMode.Side);
    }

    private void ApplyMode(CameraMode mode)
    {
        CurrentMode = mode;
        if (_cam == null) return;

        if (mode == CameraMode.Side)
        {
            _cam.orthographic    = true;
            _cam.orthographicSize = 14f;
        }
        else
        {
            _cam.orthographic  = false;
            _cam.fieldOfView   = 80f;
            _cam.nearClipPlane = 0.1f;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        switch (CurrentMode)
        {
            case CameraMode.Side:        UpdateSideCamera();        break;
            case CameraMode.FirstPerson: UpdateFirstPersonCamera(); break;
        }
    }

    // ── サイドカメラ（元の挙動）──────────────────────────────────────────────

    private void UpdateSideCamera()
    {
        var jumper = SkiJumper.Instance;
        bool follow = jumper != null &&
                      (jumper.CurrentState == SkiJumper.JumperState.Flying ||
                       jumper.CurrentState == SkiJumper.JumperState.Landed);

        Vector3 desired = follow
            ? new Vector3(target.position.x + flightOffset.x,
                          target.position.y + flightOffset.y,
                          flightOffset.z)
            : _initialPosition;

        transform.position = Vector3.Lerp(transform.position, desired,
                                          smoothSpeed * Time.deltaTime);
    }

    // ── 主観視点カメラ ────────────────────────────────────────────────────────

    private void UpdateFirstPersonCamera()
    {
        // 頭部位置（少し前方にオフセットしてモデルが映り込まないように）
        Vector3 head = target.position
                       + Vector3.up   * 0.5f
                       + target.right * 0.4f;
        transform.position = head;

        // 進行方向を向く（2D の Z 軸回転後の +X 方向 = 移動方向）
        Vector3 moveDir = target.right;

        // 真上・真下のときのフォールバック
        if (Mathf.Abs(Vector3.Dot(moveDir.normalized, Vector3.up)) > 0.99f)
            moveDir = Vector3.right;

        transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);
    }
}
