using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// Physics-based player controller for rolling ball movement with camera-relative controls.
/// Common use: Rolling ball games, marble platformers, physics-based puzzles, or Katamari-style games.
/// </summary>
public class PhysicsPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveForce = 10f;
    [SerializeField] private float maxVelocity = 20f;
    [SerializeField] private bool enableAirControl = false;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // The Header has been moved here, as it can only be applied to fields/properties.
    [Header("Events")]
    public UnityEvent OnJumpEvent;
    public UnityEvent OnIdleEvent;
    [SerializeField] private float idleTimeThreshold = 5f;

    private Rigidbody rb;
    private Camera mainCamera;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool jumpRequested;
    private float idleTimer;

    // Public Properties for force values
    public float MoveForce
    {
        get { return moveForce; }
        set { moveForce = value; }
    }

    public float JumpForce
    {
        get { return jumpForce; }
        set { jumpForce = value; }
    }


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component missing from this GameObject. Please add one.", this);
            enabled = false;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found. Please ensure a camera is tagged as 'MainCamera'.");
        }
    }

    public void Update()
    {
        CheckIdleState();
    }


    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        idleTimer = 0f;
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded)
        {
            jumpRequested = true;
        }
        idleTimer = 0f;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        CheckGrounded();
        HandleMovement();
        HandleJump();
    }

    private void CheckIdleState()
    {
        if (moveInput == Vector2.zero && rb.linearVelocity.sqrMagnitude < 0.01f)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleTimeThreshold)
            {
                OnIdleEvent.Invoke();
                idleTimer = 0f;
            }
        }
        else
        {
            idleTimer = 0f;
        }
    }

    private void CheckGrounded()
    {
        isGrounded = Physics.CheckSphere(
            transform.position - new Vector3(0f, 0.1f, 0f),
            groundCheckRadius,
            groundLayer
        );
    }

    private void HandleMovement()
    {
        if (!isGrounded && !enableAirControl)
        {
            return;
        }

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        if (inputDirection != Vector3.zero)
        {
            Vector3 cameraForward = mainCamera != null ? mainCamera.transform.forward : Vector3.forward;
            Vector3 cameraRight = mainCamera != null ? mainCamera.transform.right : Vector3.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraRight * inputDirection.x + cameraForward * inputDirection.z);

            if (rb.linearVelocity.magnitude < maxVelocity)
            {
                rb.AddForce(moveDirection * moveForce, ForceMode.Force);
            }
        }
    }

    private void HandleJump()
    {
        if (jumpRequested)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequested = false;

            OnJumpEvent.Invoke();
        }
    }

    /// <summary>
    /// Enables or disables the ability to control movement while in the air
    /// </summary>
    public void EnableAirControl(bool value)
    {
        enableAirControl = value;
    }

    // Optional: Visualize the ground check radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position - new Vector3(0f, 0.1f, 0f),
            groundCheckRadius
        );
    }
}