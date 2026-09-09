using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 2D player controller for sprites. Choose Platformer for side-on games with gravity
/// and jumping, or Top-Down for overhead games that move freely in all directions.
///
/// Needs a Rigidbody2D and a Collider2D on the same GameObject, and a PlayerInput
/// component using the EGTK input actions. For Platformer games, set Ground Layer to
/// the layer your floors are on, or the character will never be able to jump.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class CharacterController2D : MonoBehaviour, ITeleportableCharacter
{
    /// <summary>Side-on with gravity, or overhead with free movement.</summary>
    public enum MovementStyle
    {
        /// <summary>Side-on view with gravity and jumping.</summary>
        Platformer,
        /// <summary>Overhead view, moves freely in all directions, no gravity.</summary>
        TopDown
    }

    [Header("Movement Style")]
    [Tooltip("Platformer uses gravity and jumping. Top-Down moves freely in all directions.")]
    public MovementStyle movementStyle = MovementStyle.Platformer;

    [Header("Movement Settings")]
    [Tooltip("Maximum movement speed in units per second")]
    [SerializeField] private float moveSpeed = 8f;
    [Tooltip("How quickly the character reaches full speed")]
    [SerializeField] private float acceleration = 60f;
    [Tooltip("How quickly the character slows to a stop")]
    [SerializeField] private float deceleration = 60f;

    [Header("Platformer Settings")]
    [Tooltip("Peak height of a jump, in world units")]
    [SerializeField] private float jumpHeight = 3.5f;
    [Tooltip("Seconds after walking off a ledge during which a jump still works")]
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("Seconds before landing that a jump press is remembered")]
    [SerializeField] private float jumpBufferTime = 0.1f;
    [Tooltip("Multiplies Unity's gravity. Higher values feel snappier and less floaty.")]
    [SerializeField] private float gravityScale = 3f;
    [Tooltip("Fastest the character may fall, in units per second")]
    [SerializeField] private float maxFallSpeed = 20f;
    [Tooltip("Which layers count as ground. Must be set, or jumping never works.")]
    [SerializeField] private LayerMask groundLayer = 1;
    [Tooltip("Size of the box used to check for ground beneath the character")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.45f, 0.12f);
    [Tooltip("How far below the character's centre to check for ground")]
    [SerializeField] private float groundCheckOffset = 0.5f;

    [Header("Top-Down Settings")]
    [Tooltip("Flip the sprite to face the direction of travel")]
    [SerializeField] private bool faceMovementDirection = true;

    [Header("Animation")]
    [Tooltip("Optional Animator driven with Speed, Grounded and VerticalVelocity parameters")]
    [SerializeField] private Animator characterAnimator;
    [Tooltip("Flip the sprite horizontally based on movement direction")]
    [SerializeField] private bool flipSpriteOnDirection = true;

    [Header("Events")]
    /// <summary>Fires when the character touches the ground after being airborne.</summary>
    public UnityEvent onGrounded;
    /// <summary>Fires the moment a jump begins.</summary>
    public UnityEvent onJump;
    /// <summary>Fires when the character lands after falling.</summary>
    public UnityEvent onLanding;
    /// <summary>Fires when the character starts moving from a standstill.</summary>
    public UnityEvent onStartMoving;
    /// <summary>Fires when the character comes to a stop.</summary>
    public UnityEvent onStopMoving;
    /// <summary>Fires after the character is teleported, passing the destination.</summary>
    public UnityEvent<Vector3> onTeleport;
    /// <summary>Fires when a spawn point positions the character, passing the position.</summary>
    public UnityEvent<Vector3> onSpawnPointUsed;

    private Rigidbody2D _body;
    private SpriteRenderer _sprite;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private bool _wasGrounded;
    private bool _wasMoving;
    private float _coyoteCounter;
    private float _jumpBufferCounter;

    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDVerticalVelocity;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.freezeRotation = true;
        _sprite = GetComponentInChildren<SpriteRenderer>();

        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDVerticalVelocity = Animator.StringToHash("VerticalVelocity");

        ApplyStyleToBody();

        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning($"CharacterController2D on '{gameObject.name}' has no Collider2D, " +
                             "so it will fall through everything. Add one (Capsule Collider 2D " +
                             "works well for characters).", this);
        }
    }

    private void ApplyStyleToBody()
    {
        _body.gravityScale = movementStyle == MovementStyle.Platformer ? gravityScale : 0f;
    }

    // Keeps the body in step when the style is switched in the Inspector during play.
    private void OnValidate()
    {
        if (Application.isPlaying && _body != null) ApplyStyleToBody();
    }

    /// <summary>Reads movement input. Called automatically by PlayerInput.</summary>
    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();

    /// <summary>Reads jump input. Called automatically by PlayerInput.</summary>
    public void OnJump(InputValue value)
    {
        if (value.isPressed) _jumpBufferCounter = jumpBufferTime;
    }

    private void Update()
    {
        if (movementStyle == MovementStyle.Platformer) UpdateGroundState();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        // Re-applied every step rather than only in Awake: movementStyle is a public
        // field, so switching it from a script or in the Inspector during play would
        // otherwise leave gravity set for the previous style — a Top-Down character
        // that still falls, or a platformer that floats.
        ApplyStyleToBody();

        if (movementStyle == MovementStyle.Platformer) MovePlatformer();
        else MoveTopDown();
    }

    private void UpdateGroundState()
    {
        Vector2 checkCentre = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        _isGrounded = Physics2D.OverlapBox(checkCentre, groundCheckSize, 0f, groundLayer) != null;

        if (_isGrounded && !_wasGrounded)
        {
            onGrounded?.Invoke();
            onLanding?.Invoke();
        }
        _wasGrounded = _isGrounded;

        // Clamped rather than counting down forever: an unclamped timer drifts to a
        // large negative float over a long session, which is meaningless and makes the
        // values hard to read while debugging.
        _coyoteCounter = _isGrounded
            ? coyoteTime
            : Mathf.Max(_coyoteCounter - Time.deltaTime, 0f);
        _jumpBufferCounter = Mathf.Max(_jumpBufferCounter - Time.deltaTime, 0f);
    }

    private void MovePlatformer()
    {
        float targetSpeed = _moveInput.x * moveSpeed;
        float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        float newX = Mathf.MoveTowards(_body.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
        float newY = Mathf.Max(_body.linearVelocity.y, -maxFallSpeed);

        if (_jumpBufferCounter > 0f && _coyoteCounter > 0f)
        {
            // v = sqrt(2 * g * h) gives the launch speed for a target peak height.
            newY = Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(Physics2D.gravity.y) * gravityScale);
            _jumpBufferCounter = 0f;
            _coyoteCounter = 0f;
            onJump?.Invoke();
        }

        _body.linearVelocity = new Vector2(newX, newY);
        UpdateFacing(_moveInput.x);
        ReportMovementState(Mathf.Abs(newX) > 0.1f);
    }

    private void MoveTopDown()
    {
        Vector2 target = Vector2.ClampMagnitude(_moveInput, 1f) * moveSpeed;
        float rate = target.sqrMagnitude > 0.01f ? acceleration : deceleration;
        _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, target,
                                                   rate * Time.fixedDeltaTime);

        if (faceMovementDirection) UpdateFacing(_moveInput.x);
        ReportMovementState(_body.linearVelocity.sqrMagnitude > 0.01f);
    }

    private void UpdateFacing(float horizontal)
    {
        if (!flipSpriteOnDirection || _sprite == null) return;
        if (Mathf.Abs(horizontal) < 0.01f) return;
        _sprite.flipX = horizontal < 0f;
    }

    private void ReportMovementState(bool isMoving)
    {
        if (isMoving && !_wasMoving) onStartMoving?.Invoke();
        else if (!isMoving && _wasMoving) onStopMoving?.Invoke();
        _wasMoving = isMoving;
    }

    private void UpdateAnimator()
    {
        if (characterAnimator == null) return;
        characterAnimator.SetFloat(_animIDSpeed, Mathf.Abs(_body.linearVelocity.x));
        characterAnimator.SetBool(_animIDGrounded, _isGrounded);
        characterAnimator.SetFloat(_animIDVerticalVelocity, _body.linearVelocity.y);
    }

    /// <summary>True while the character is standing on ground. Platformer style only.</summary>
    public bool IsGrounded => _isGrounded;

    /// <summary>Moves the character to a world position and stops its motion.</summary>
    public void TeleportTo(Vector3 position)
    {
        _body.linearVelocity = Vector2.zero;
        transform.position = position;
        onTeleport?.Invoke(position);
    }

    /// <summary>Moves the character to a spawn point position.</summary>
    public void MoveToSpawnPoint(Vector3 position)
    {
        TeleportTo(position);
        onSpawnPointUsed?.Invoke(position);
    }

    /// <summary>Sets the maximum movement speed.</summary>
    public void SetMoveSpeed(float newSpeed) => moveSpeed = newSpeed;

    /// <summary>Sets the jump height in world units.</summary>
    public void SetJumpHeight(float newHeight) => jumpHeight = newHeight;

    private void OnDrawGizmosSelected()
    {
        if (movementStyle != MovementStyle.Platformer) return;
        Gizmos.color = Application.isPlaying && _isGrounded ? Color.green : Color.yellow;
        Vector2 centre = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        Gizmos.DrawWireCube(centre, groundCheckSize);
    }
}
