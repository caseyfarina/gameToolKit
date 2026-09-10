using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Makes a 2D character ride a moving platform instead of sliding off it.
/// Put this on the character, not the platform.
///
/// Without it a character stands still while the platform slides out from under them.
/// CharacterController2D assigns its velocity every physics step, so any motion the
/// platform imparts is overwritten before it can take effect. This component sidesteps
/// that by parenting the character to the platform while they are standing on it, which
/// moves the transform directly and cannot be overwritten.
///
/// The platform needs a collider on the Platform Layer. Giving it a Rigidbody2D set to
/// Kinematic is recommended, so Unity does not rebuild the static collider every frame.
///
/// The 2D counterpart of PhysicsPlatformStick.
/// </summary>
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class PhysicsPlatformStick2D : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("How far below the character to look for a platform")]
    [SerializeField] private float groundCheckDistance = 0.15f;

    [Tooltip("Width of the check. Roughly the character's width works well.")]
    [SerializeField] private float groundCheckWidth = 0.6f;

    [Tooltip("How far below the character's centre to start the check")]
    [SerializeField] private float groundCheckOffset = 0.5f;

    [Tooltip("Which layers count as platforms")]
    [SerializeField] private LayerMask platformLayer = ~0;

    [Tooltip("Only ride platforms with this tag. Leave empty to ride any collider on the layer.")]
    [SerializeField] private string platformTag = "movingPlatform";

    [Header("Events")]
    /// <summary>
    /// Fires when the character steps onto a moving platform
    /// </summary>
    public UnityEvent onPlatformEntered;

    /// <summary>
    /// Fires when the character steps off a moving platform
    /// </summary>
    public UnityEvent onPlatformExited;

    private Transform _currentPlatform;
    private Transform _originalParent;

    private void Awake()
    {
        // Remembered so leaving a platform restores the character to wherever it lived in
        // the hierarchy, rather than dumping it at the scene root.
        _originalParent = transform.parent;
    }

    private void FixedUpdate()
    {
        Transform platform = FindPlatformBelow();

        if (platform == _currentPlatform) return;

        if (platform != null) AttachTo(platform);
        else Detach();
    }

    private Transform FindPlatformBelow()
    {
        Vector2 centre = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        Vector2 size = new Vector2(groundCheckWidth, groundCheckDistance);

        Collider2D hit = Physics2D.OverlapBox(centre, size, 0f, platformLayer);
        if (hit == null) return null;

        if (!string.IsNullOrEmpty(platformTag) && !hit.CompareTag(platformTag)) return null;

        return hit.transform;
    }

    private void AttachTo(Transform platform)
    {
        // worldPositionStays: true, so parenting never teleports or rescales the character.
        transform.SetParent(platform, true);
        _currentPlatform = platform;
        onPlatformEntered?.Invoke();
    }

    private void Detach()
    {
        transform.SetParent(_originalParent, true);
        _currentPlatform = null;
        onPlatformExited?.Invoke();
    }

    /// <summary>True while the character is riding a platform.</summary>
    public bool IsOnPlatform => _currentPlatform != null;

    /// <summary>The platform being ridden, or null.</summary>
    public Transform CurrentPlatform => _currentPlatform;

    private void OnDisable()
    {
        // Leaving the character parented to a platform that may be destroyed or disabled
        // would drag it along or destroy it with the platform.
        if (_currentPlatform != null) Detach();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Application.isPlaying && IsOnPlatform ? Color.green : Color.cyan;
        Vector2 centre = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        Gizmos.DrawWireCube(centre, new Vector3(groundCheckWidth, groundCheckDistance, 0f));
    }
}
