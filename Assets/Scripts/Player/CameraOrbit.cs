using UnityEngine;
using Unity.Cinemachine;

// Орбитальная камера: трекпад-двумя-пальцами / ПКМ — поворот, Alt+скролл — зум.
[RequireComponent(typeof(CinemachineFollow))]
public class CameraOrbit : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float yawSpeed       = 220f;
    [SerializeField] private float pitchSpeed     = 160f;
    [SerializeField] private float trackpadYaw    = 25f;
    [SerializeField] private float trackpadPitch  = 18f;
    [SerializeField] private float zoomStep       = 2.5f;

    [Header("Smoothing")]
    [Tooltip("Время (с) догона целевого offset'а. Больше — плавнее, но медленнее реагирует.")]
    [SerializeField] private float smoothTime = 0.12f;

    [Header("Limits")]
    [SerializeField] private float minPitch    = 30f;
    [SerializeField] private float maxPitch    = 70f;
    [SerializeField] private float minDistance = 4f;
    [SerializeField] private float maxDistance = 14f;

    private CinemachineFollow _follow;

    // _* — то, что использует камера сейчас (после сглаживания);
    // _*Target — куда инпут хочет её привести. Каждый кадр current SmoothDamp'ится к target,
    // чтобы резкие "толчки" трекпада превращались в плавное движение.
    private float _yaw;
    private float _pitch;
    private float _distance;

    private float _yawTarget;
    private float _pitchTarget;
    private float _distanceTarget;

    private float _yawVel;
    private float _pitchVel;
    private float _distanceVel;

    private void Awake()
    {
        _follow = GetComponent<CinemachineFollow>();

        var off = _follow.FollowOffset;
        _distance = Mathf.Max(off.magnitude, 0.1f);
        _pitch    = Mathf.Asin(Mathf.Clamp(off.y / _distance, -1f, 1f)) * Mathf.Rad2Deg;
        _yaw      = Mathf.Atan2(off.x, -off.z) * Mathf.Rad2Deg;

        _yawTarget      = _yaw;
        _pitchTarget    = _pitch;
        _distanceTarget = _distance;
    }

    private void LateUpdate()
    {
        if (Input.GetMouseButton(1))
        {
            _yawTarget   += Input.GetAxis("Mouse X") * yawSpeed   * Time.unscaledDeltaTime;
            _pitchTarget -= Input.GetAxis("Mouse Y") * pitchSpeed * Time.unscaledDeltaTime;
        }

        Vector2 scroll = Input.mouseScrollDelta;
        bool zoomMod   = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (scroll.sqrMagnitude > 0.0001f)
        {
            if (zoomMod)
            {
                _distanceTarget -= scroll.y * zoomStep;
            }
            else
            {
                _yawTarget   += scroll.x * trackpadYaw;
                _pitchTarget -= scroll.y * trackpadPitch;
            }
        }

        _pitchTarget    = Mathf.Clamp(_pitchTarget,    minPitch,    maxPitch);
        _distanceTarget = Mathf.Clamp(_distanceTarget, minDistance, maxDistance);

        // unscaledDeltaTime — чтобы орбита продолжала работать на паузе (timeScale = 0).
        float dt = Time.unscaledDeltaTime;
        _yaw      = Mathf.SmoothDampAngle(_yaw,      _yawTarget,      ref _yawVel,      smoothTime, Mathf.Infinity, dt);
        _pitch    = Mathf.SmoothDamp     (_pitch,    _pitchTarget,    ref _pitchVel,    smoothTime, Mathf.Infinity, dt);
        _distance = Mathf.SmoothDamp     (_distance, _distanceTarget, ref _distanceVel, smoothTime, Mathf.Infinity, dt);

        float pr = _pitch * Mathf.Deg2Rad;
        float yr = _yaw   * Mathf.Deg2Rad;
        _follow.FollowOffset = new Vector3(
             Mathf.Sin(yr) * Mathf.Cos(pr),
             Mathf.Sin(pr),
            -Mathf.Cos(yr) * Mathf.Cos(pr)
        ) * _distance;
    }
}
