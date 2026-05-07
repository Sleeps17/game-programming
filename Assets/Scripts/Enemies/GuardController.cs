using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(GuardVision))]
public class GuardController : MonoBehaviour
{
    public enum GuardState { Patrol, Alert, Chase }

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointWaitTime = 1.5f;

    [Header("Alert")]
    [SerializeField] private float alertDuration = 3f;
    [SerializeField] private float alertMoveSpeed = 3f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float loseTargetTime = 4f;

    [Header("Speeds")]
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float walkAnimationSpeed = 1f;
    [SerializeField] private float runAnimationSpeed = 1.15f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int LookingHash = Animator.StringToHash("IsLooking");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private const float NavMeshSnapRadius = 25f;

    public GuardState CurrentState { get; private set; }

    private NavMeshAgent _agent;
    private GuardVision _vision;
    private Transform _player;

    private int _waypointIndex;
    private float _stateTimer;
    private bool _waitingAtWaypoint;
    private bool _navMeshWarningLogged;
    private bool _hasSpeedParam;
    private bool _hasLookingParam;
    private bool _hasRunningParam;
    private Vector3 _lastKnownPosition;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _vision = GetComponent<GuardVision>();
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        animator = AnimatedCharacterVisual.EnsureAnimator(
            gameObject,
            animator,
            AnimatedCharacterVisual.GuardControllerPath,
            new Color(0.55f, 0.34f, 0.22f));
        CacheAnimatorParameters();
    }

    private void OnEnable()
    {
        _vision.OnPlayerSpotted += HandlePlayerSpotted;
        _vision.OnPlayerLost    += HandlePlayerLost;
    }

    private void OnDisable()
    {
        _vision.OnPlayerSpotted -= HandlePlayerSpotted;
        _vision.OnPlayerLost    -= HandlePlayerLost;
    }

    private void Start()
    {
        TrySnapToNavMesh();
        EnterPatrol();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Если так и не попали на NavMesh — повторяем попытки, не спамим ошибками.
        if (!_agent.isOnNavMesh)
        {
            TrySnapToNavMesh();
            return;
        }

        switch (CurrentState)
        {
            case GuardState.Patrol: UpdatePatrol(); break;
            case GuardState.Alert:  UpdateAlert();  break;
            case GuardState.Chase:  UpdateChase();  break;
        }

        if (animator != null)
        {
            float speed = _agent.velocity.magnitude;
            bool isLooking = _waitingAtWaypoint ||
                (CurrentState == GuardState.Alert && speed < 0.05f);
            bool isRunning = CurrentState == GuardState.Chase && speed > 0.05f;

            if (_hasSpeedParam) animator.SetFloat(SpeedHash, speed);
            if (_hasLookingParam) animator.SetBool(LookingHash, isLooking);
            if (_hasRunningParam) animator.SetBool(IsRunningHash, isRunning);
            animator.speed = GetAnimatorPlaybackSpeed(speed, isLooking);
        }
    }

    private float GetAnimatorPlaybackSpeed(float speed, bool isLooking)
    {
        if (isLooking || speed < 0.05f) return 1f;
        return CurrentState == GuardState.Chase ? runAnimationSpeed : walkAnimationSpeed;
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null) return;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.nameHash == SpeedHash) _hasSpeedParam = true;
            else if (parameter.nameHash == LookingHash) _hasLookingParam = true;
            else if (parameter.nameHash == IsRunningHash) _hasRunningParam = true;
        }
    }

    // Если позиция спавна не на NavMesh — агент тихо отключается, и SetDestination кидает.
    // Подтягиваем к ближайшей точке NavMesh.
    private void TrySnapToNavMesh()
    {
        if (_agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(transform.position, out var hit, NavMeshSnapRadius, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            _agent.Warp(hit.position);
            _navMeshWarningLogged = false;
        }
        else if (!_navMeshWarningLogged)
        {
            _navMeshWarningLogged = true;
            Debug.LogWarning(
                $"[GuardController] '{name}' has no NavMesh within {NavMeshSnapRadius}m of {transform.position}. " +
                "Check that the level has a baked NavMeshSurface.");
        }
    }


    private void EnterPatrol()
    {
        CurrentState = GuardState.Patrol;
        _agent.speed = patrolSpeed;
        GoToWaypoint(_waypointIndex);
    }

    private void UpdatePatrol()
    {
        if (_waitingAtWaypoint) return;
        if (!_agent.isOnNavMesh || _agent.pathPending) return;

        if (_agent.remainingDistance < 0.3f)
            StartCoroutine(WaitAtWaypoint());
    }

    private IEnumerator WaitAtWaypoint()
    {
        if (waypoints.Length == 0) yield break;
        _waitingAtWaypoint = true;
        yield return new WaitForSeconds(waypointWaitTime);
        _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
        GoToWaypoint(_waypointIndex);
        _waitingAtWaypoint = false;
    }

    private void GoToWaypoint(int index)
    {
        if (waypoints.Length == 0) return;
        if (!_agent.isOnNavMesh) return;
        _agent.SetDestination(waypoints[index].position);
    }


    private void EnterAlert(Vector3 position)
    {
        StopAllCoroutines();
        CurrentState = GuardState.Alert;
        _agent.speed = alertMoveSpeed;
        _lastKnownPosition = position;
        if (_agent.isOnNavMesh) _agent.SetDestination(_lastKnownPosition);
        _stateTimer = alertDuration;
    }

    private void UpdateAlert()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
            EnterPatrol();
    }


    private void EnterChase()
    {
        CurrentState = GuardState.Chase;
        _agent.speed = chaseSpeed;
        _stateTimer = loseTargetTime;
    }

    private void UpdateChase()
    {
        if (_player == null) { EnterPatrol(); return; }

        if (_vision.CanSeePlayer)
        {
            _lastKnownPosition = _player.position;
            if (_agent.isOnNavMesh) _agent.SetDestination(_lastKnownPosition);
            _stateTimer = loseTargetTime;
        }
        else
        {
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
                EnterAlert(_lastKnownPosition);
        }
    }


    private void HandlePlayerSpotted(Vector3 position)
    {
        if (CurrentState != GuardState.Chase)
            EnterChase();
    }

    private void HandlePlayerLost(Vector3 lastPosition)
    {
        if (CurrentState == GuardState.Chase)
            EnterAlert(lastPosition);
    }
}
