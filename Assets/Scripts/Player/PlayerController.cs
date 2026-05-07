using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.65f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float sprintSpeedMultiplier = 1.7f;

    [Header("Sprint stamina (seconds)")]
    [SerializeField] private float maxSprintStamina = 6f;
    [SerializeField] private float sprintRegenRate  = 0.4f;
    [SerializeField] private float sprintMinToStart = 0.4f;

    [Header("Interaction")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask absorbableLightLayer;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float walkAnimationSpeed = 1f;
    [SerializeField] private float runAnimationSpeed = 1.15f;
    [SerializeField] private float crouchAnimationSpeed = 1f;

    private static readonly int SpeedHash      = Animator.StringToHash("Speed");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");

    private CharacterController _cc;
    private LightAbsorber _absorber;
    private Camera _cam;
    private Vector3 _velocity;
    private bool _isCrouching;
    private bool _isSprinting;
    private float _sprintStamina;
    private float _currentSpeed;
    private bool _hasSpeedParam;
    private bool _hasCrouchParam;
    private bool _hasSprintParam;

    public float SprintStamina    => _sprintStamina;
    public float MaxSprintStamina => maxSprintStamina;
    public bool  IsSprinting      => _isSprinting;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _absorber = GetComponent<LightAbsorber>();
        _cam = Camera.main;
        animator = AnimatedCharacterVisual.EnsureAnimator(
            gameObject,
            animator,
            AnimatedCharacterVisual.PlayerControllerPath,
            new Color(0.07f, 0.07f, 0.16f));
        CacheAnimatorParameters();
        _sprintStamina = maxSprintStamina;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        HandleSprint();
        HandleMovement();
        HandleCrouch();
        HandleInteract();
    }

    private void HandleSprint()
    {
        bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && !_isCrouching;

        if (_isSprinting)
        {
            _sprintStamina -= Time.deltaTime;
            if (!wantsSprint || _sprintStamina <= 0f)
            {
                _sprintStamina = Mathf.Max(0f, _sprintStamina);
                _isSprinting = false;
            }
        }
        else
        {
            // Реген медленнее, чем расход — общий запас по уровню уменьшается со временем.
            if (_sprintStamina < maxSprintStamina)
                _sprintStamina = Mathf.Min(maxSprintStamina,
                    _sprintStamina + sprintRegenRate * Time.deltaTime);

            if (wantsSprint && _sprintStamina >= sprintMinToStart)
                _isSprinting = true;
        }
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Движение относительно горизонтального направления камеры.
        Vector3 camForward = _cam.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = _cam.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 move = (camForward * v + camRight * h).normalized;
        float speed = moveSpeed
            * (_isCrouching ? crouchSpeedMultiplier : 1f)
            * (_isSprinting ? sprintSpeedMultiplier : 1f);
        _cc.Move(move * speed * Time.deltaTime);

        if (move != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(move), 12f * Time.deltaTime);

        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);

        _currentSpeed = Mathf.Lerp(_currentSpeed, move.magnitude * speed, 10f * Time.deltaTime);
        if (animator != null)
        {
            if (_hasSpeedParam) animator.SetFloat(SpeedHash, _currentSpeed);
            if (_hasCrouchParam) animator.SetBool(IsCrouchingHash, _isCrouching);
            if (_hasSprintParam) animator.SetBool(IsSprintingHash, _isSprinting);
            animator.speed = GetAnimatorPlaybackSpeed(move.sqrMagnitude > 0.001f);
        }
    }

    private float GetAnimatorPlaybackSpeed(bool isMoving)
    {
        if (!isMoving) return 1f;
        if (_isCrouching) return crouchAnimationSpeed;
        return _isSprinting ? runAnimationSpeed : walkAnimationSpeed;
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null) return;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.nameHash == SpeedHash) _hasSpeedParam = true;
            else if (parameter.nameHash == IsCrouchingHash) _hasCrouchParam = true;
            else if (parameter.nameHash == IsSprintingHash) _hasSprintParam = true;
        }
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
            _isCrouching = !_isCrouching;
    }

    private void HandleInteract()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, absorbableLightLayer);
        if (hits.Length == 0) return;

        AbsorbableLight nearest = null;
        float minDist = float.MaxValue;
        foreach (var hit in hits)
        {
            float d = Vector3.Distance(transform.position, hit.transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = hit.GetComponent<AbsorbableLight>();
            }
        }

        if (nearest != null)
            _absorber.ToggleAbsorb(nearest);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
