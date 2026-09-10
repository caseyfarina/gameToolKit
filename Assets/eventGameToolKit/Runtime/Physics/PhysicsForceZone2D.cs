using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Pushes sprites while they are inside a trigger area. The 2D counterpart of
/// PhysicsForceZone.
///
/// Needs a Collider2D with Is Trigger ticked. Objects pushed need a Rigidbody2D.
///
/// Common use: wind, updrafts, water currents, conveyor belts, repulsion fields.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class PhysicsForceZone2D : MonoBehaviour
{
    /// <summary>Whether the direction is fixed in the world or follows this object's rotation.</summary>
    public enum ForceSpace
    {
        /// <summary>Direction stays fixed no matter how the zone is rotated.</summary>
        World,
        /// <summary>Direction rotates with the zone.</summary>
        Local
    }

    /// <summary>How the push direction is decided.</summary>
    public enum TargetMode
    {
        /// <summary>Use the Force Direction set below.</summary>
        None,
        /// <summary>Push each object toward a target, like a magnet.</summary>
        TowardTarget,
        /// <summary>Push each object away from a target, like an explosion.</summary>
        AwayFromTarget
    }

    [Header("Target")]
    [Tooltip("Only objects with this tag are pushed. Leave empty to push anything with a Rigidbody2D.")]
    [SerializeField] private string targetTag = "Player";

    [Header("Direction")]
    [Tooltip("Use a fixed direction, or push toward/away from a target object")]
    [SerializeField] private TargetMode targetMode = TargetMode.None;

    [Tooltip("The object to push toward or away from")]
    [SerializeField] private GameObject directionTarget;

    [Tooltip("Which way to push, when Target Mode is None")]
    [SerializeField] private Vector2 forceDirection = Vector2.up;

    [Tooltip("Fixed in the world, or rotates with this object")]
    [SerializeField] private ForceSpace forceSpace = ForceSpace.World;

    [Header("Strength")]
    [Tooltip("Weakest push. Each object gets a random amount between min and max.")]
    [Min(0f)]
    [SerializeField] private float minForce = 5f;

    [Tooltip("Strongest push")]
    [Min(0f)]
    [SerializeField] private float maxForce = 10f;

    [Tooltip("Impulse gives one sharp shove. Force pushes continuously, like wind.")]
    [SerializeField] private ForceMode2D forceMode = ForceMode2D.Force;

    [Header("Timing")]
    [Tooltip("Push each object only once until it leaves and comes back")]
    [SerializeField] private bool oneForcePerStay = false;

    [Tooltip("Also push the moment an object enters")]
    [SerializeField] private bool applyOnEnter = false;

    [Header("Events")]
    /// <summary>
    /// Fires when force is applied, passing how many objects were pushed
    /// </summary>
    public UnityEvent<int> onForceApplied;

    /// <summary>
    /// Fires once per object pushed, passing that object
    /// </summary>
    public UnityEvent<GameObject> onForceAppliedToObject;

    private readonly List<Rigidbody2D> _occupants = new List<Rigidbody2D>();
    private readonly HashSet<Rigidbody2D> _alreadyPushed = new HashSet<Rigidbody2D>();

    private void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"PhysicsForceZone2D on '{gameObject.name}': the Collider2D needs " +
                             "Is Trigger ticked, or objects will bounce off instead of being " +
                             "pushed through.", this);
        }

        if (maxForce < minForce)
        {
            Debug.LogWarning($"PhysicsForceZone2D on '{gameObject.name}': Max Force is lower than " +
                             "Min Force.", this);
        }
    }

    private bool Matches(GameObject other) =>
        string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Matches(other.gameObject)) return;

        Rigidbody2D body = other.attachedRigidbody;
        if (body == null || _occupants.Contains(body)) return;

        _occupants.Add(body);
        if (applyOnEnter) PushOne(body);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!Matches(other.gameObject)) return;

        Rigidbody2D body = other.attachedRigidbody;
        if (body == null) return;

        _occupants.Remove(body);
        // Cleared on exit so the object can be pushed again next time it enters.
        _alreadyPushed.Remove(body);
    }

    private void FixedUpdate()
    {
        // Driven from FixedUpdate rather than OnTriggerStay2D: physics stops delivering
        // stay callbacks once a body falls asleep, which would make a wind zone quietly
        // stop pushing anything that came to rest inside it.
        _occupants.RemoveAll(b => b == null);
        if (_occupants.Count == 0) return;

        int pushed = 0;
        foreach (Rigidbody2D body in _occupants)
        {
            if (oneForcePerStay && _alreadyPushed.Contains(body)) continue;
            PushOne(body);
            pushed++;
        }

        if (pushed > 0) onForceApplied?.Invoke(pushed);
    }

    private void PushOne(Rigidbody2D body)
    {
        Vector2 direction = ResolveDirection(body.position);
        float strength = Random.Range(minForce, Mathf.Max(minForce, maxForce));

        body.AddForce(direction * strength, forceMode);
        _alreadyPushed.Add(body);
        onForceAppliedToObject?.Invoke(body.gameObject);
    }

    private Vector2 ResolveDirection(Vector2 objectPosition)
    {
        if (targetMode != TargetMode.None && directionTarget != null)
        {
            Vector2 toTarget = (Vector2)directionTarget.transform.position - objectPosition;
            if (toTarget.sqrMagnitude < 0.0001f) return Vector2.up;

            Vector2 normalized = toTarget.normalized;
            return targetMode == TargetMode.TowardTarget ? normalized : -normalized;
        }

        Vector2 direction = forceDirection.sqrMagnitude < 0.0001f ? Vector2.up : forceDirection.normalized;
        return forceSpace == ForceSpace.Local ? (Vector2)(transform.rotation * direction) : direction;
    }

    /// <summary>
    /// Pushes everything currently inside the zone, without waiting for the next physics step.
    /// </summary>
    public void ApplyForce()
    {
        _occupants.RemoveAll(b => b == null);
        foreach (Rigidbody2D body in _occupants) PushOne(body);
        if (_occupants.Count > 0) onForceApplied?.Invoke(_occupants.Count);
    }

    /// <summary>
    /// Lets every object inside be pushed again, when One Force Per Stay is on.
    /// </summary>
    public void ResetTracking() => _alreadyPushed.Clear();

    /// <summary>Sets the object to push toward or away from.</summary>
    public void SetDirectionTarget(GameObject target) => directionTarget = target;

    /// <summary>Sets the weakest push.</summary>
    public void SetMinForce(float force) => minForce = Mathf.Max(0f, force);

    /// <summary>Sets the strongest push.</summary>
    public void SetMaxForce(float force) => maxForce = Mathf.Max(minForce, force);

    private void OnDisable()
    {
        _occupants.Clear();
        _alreadyPushed.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);
        Vector2 dir = ResolveDirection(transform.position);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(dir * 1.5f));
    }
}
