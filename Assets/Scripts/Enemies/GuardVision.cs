using System;
using UnityEngine;

// Конусное зрение охранника + дочерний фонарь. RegisteredLight ставится на фонарь, не на этот объект.
public class GuardVision : MonoBehaviour
{
    [Header("Vision Cone")]
    [SerializeField] private float viewAngle = 60f;
    [SerializeField] private float viewRange = 10f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask playerMask;

    [Header("Flashlight")]
    [SerializeField] private Light flashlight;

    [Header("Alert Colors")]
    [SerializeField] private Color normalColor  = Color.white;
    [SerializeField] private Color suspiciousColor = Color.yellow;
    [SerializeField] private Color alertColor   = Color.red;

    public bool CanSeePlayer { get; private set; }

    public event Action<Vector3> OnPlayerSpotted;
    public event Action<Vector3> OnPlayerLost;

    private Transform _player;
    private bool _wasSeenLastFrame;

    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (flashlight == null)
            flashlight = GetComponentInChildren<Light>();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        CanSeePlayer = CheckLineOfSight();

        if (CanSeePlayer && !_wasSeenLastFrame)
            OnPlayerSpotted?.Invoke(_player.position);
        else if (!CanSeePlayer && _wasSeenLastFrame)
            OnPlayerLost?.Invoke(_player != null ? _player.position : transform.position);

        _wasSeenLastFrame = CanSeePlayer;

        UpdateFlashlightColor();
    }

    private bool CheckLineOfSight()
    {
        if (_player == null) return false;

        Vector3 toPlayer = _player.position - transform.position;
        if (toPlayer.magnitude > viewRange) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > viewAngle * 0.5f) return false;

        return !Physics.Raycast(transform.position, toPlayer.normalized, toPlayer.magnitude, obstacleMask);
    }

    private void UpdateFlashlightColor()
    {
        if (flashlight == null) return;
        flashlight.color = CanSeePlayer ? alertColor : normalColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, viewRange);

        Vector3 leftBound  = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightBound = Quaternion.Euler(0,  viewAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftBound  * viewRange);
        Gizmos.DrawRay(transform.position, rightBound * viewRange);
    }
}
