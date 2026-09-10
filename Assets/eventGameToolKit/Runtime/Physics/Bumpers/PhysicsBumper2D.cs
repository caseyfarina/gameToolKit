using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Bounces sprites away on collision, with animated feedback and a cooldown.
/// The 2D counterpart of PhysicsBumper.
///
/// Set Bumper Tag to choose which objects it reacts to, or leave it empty to bounce
/// anything with a Rigidbody2D.
///
/// Needs a Collider2D that is NOT a trigger, because it reacts to collisions rather than
/// overlaps. The bouncing object needs a Rigidbody2D.
///
/// Common use: Bounce pads, pinball bumpers, launch mechanisms, trampolines.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class PhysicsBumper2D : MonoBehaviour
{
    [Header("Bumper Tag")]
    [Tooltip("Only objects with this tag are bounced. Leave empty to bounce anything with a Rigidbody2D.")]
    [SerializeField] private string bumperTag = "Player";

    [Header("Bumper Settings")]
    [Tooltip("Force applied to the object on collision")]
    [SerializeField] private float bumperForce = 12f;

    [Tooltip("Push along the collision surface, or always straight up?")]
    [SerializeField] private bool useCollisionNormal = true;

    [Tooltip("Extra upward push added to the bounce direction")]
    [Range(0f, 2f)]
    [SerializeField] private float upwardForceMultiplier = 0.5f;

    [Header("Cooldown Settings")]
    [Tooltip("Seconds before the bumper can fire again")]
    [Min(0f)]
    [SerializeField] private float cooldownDuration = 0.5f;

    [Header("Animation Settings")]
    [Tooltip("How the squash-and-stretch plays out over time")]
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Scale at the peak of the animation. 1 on an axis means no scaling there.")]
    [SerializeField] private Vector2 scaleMultiplier = new Vector2(1.4f, 1.4f);

    [Tooltip("How long the animation lasts")]
    [SerializeField] private float animationDuration = 0.35f;

    [Header("Colour Flash")]
    [Tooltip("Flash the sprite's colour when it fires")]
    [SerializeField] private bool useColourFlash = true;

    [Tooltip("Colour at the peak of the flash")]
    [SerializeField] private Color flashColour = Color.white;

    [Header("Events")]
    /// <summary>
    /// Fires when the bumper bounces something
    /// </summary>
    public UnityEvent onBumperTriggered;

    /// <summary>
    /// Fires when the cooldown ends and the bumper can fire again
    /// </summary>
    public UnityEvent onCooldownComplete;

    private SpriteRenderer _sprite;
    private Vector3 _originalScale;
    private Color _originalColour;
    private float _lastTriggerTime = -999f;
    private Sequence _animation;

    private void Start()
    {
        _originalScale = transform.localScale;

        _sprite = GetComponent<SpriteRenderer>();
        if (_sprite != null) _originalColour = _sprite.color;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.isTrigger)
        {
            Debug.LogWarning($"PhysicsBumper2D on '{gameObject.name}': the Collider2D is set to " +
                             "Is Trigger, so collisions never happen and the bumper will never " +
                             "fire. Uncheck Is Trigger.", this);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // An empty tag means "react to anything".
        if (!string.IsNullOrEmpty(bumperTag) && !collision.gameObject.CompareTag(bumperTag)) return;
        if (Time.time < _lastTriggerTime + cooldownDuration) return;

        Vector2 bounce = useCollisionNormal && collision.contactCount > 0
            ? -collision.GetContact(0).normal
            : (Vector2)(collision.transform.position - transform.position).normalized;

        bounce += Vector2.up * upwardForceMultiplier;
        bounce.Normalize();

        // Returns false when the other object has no Rigidbody2D, in which case there is
        // nothing to push and firing the events would be misleading.
        if (!EGTKPhysics.TryAddImpulse(collision.gameObject, bounce * bumperForce)) return;

        _lastTriggerTime = Time.time;
        PlayFeedback();
        onBumperTriggered?.Invoke();
    }

    private void PlayFeedback()
    {
        _animation?.Kill();
        transform.localScale = _originalScale;
        if (_sprite != null) _sprite.color = _originalColour;

        // DOTween.To rather than a module extension: module extensions clash with the
        // package's assembly definition. See CLAUDE.md.
        _animation = DOTween.Sequence();

        Vector3 peak = new Vector3(_originalScale.x * scaleMultiplier.x,
                                   _originalScale.y * scaleMultiplier.y,
                                   _originalScale.z);

        _animation.Append(DOTween.To(() => transform.localScale,
                                     v => transform.localScale = v,
                                     peak, animationDuration * 0.5f).SetEase(animationCurve));
        _animation.Append(DOTween.To(() => transform.localScale,
                                     v => transform.localScale = v,
                                     _originalScale, animationDuration * 0.5f).SetEase(animationCurve));

        if (useColourFlash && _sprite != null)
        {
            _animation.Join(DOTween.To(() => _sprite.color,
                                       c => _sprite.color = c,
                                       flashColour, animationDuration * 0.5f)
                                   .SetLoops(2, LoopType.Yoyo));
        }

        _animation.OnComplete(() => onCooldownComplete?.Invoke());
    }

    /// <summary>
    /// Fires the bumper's animation and event without a collision. Useful for testing.
    /// </summary>
    public void Trigger()
    {
        _lastTriggerTime = Time.time;
        PlayFeedback();
        onBumperTriggered?.Invoke();
    }

    /// <summary>
    /// Makes the bumper ready to fire again straight away.
    /// </summary>
    public void ResetCooldown() => _lastTriggerTime = -999f;

    /// <summary>
    /// Sets how many seconds must pass before the bumper can fire again.
    /// </summary>
    public void SetCooldownDuration(float seconds) => cooldownDuration = Mathf.Max(0f, seconds);

    private void OnDisable()
    {
        _animation?.Kill();
        transform.localScale = _originalScale;
        if (_sprite != null) _sprite.color = _originalColour;
    }
}
