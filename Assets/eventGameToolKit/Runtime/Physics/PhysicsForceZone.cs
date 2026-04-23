using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Tracks tagged Rigidbodies inside a trigger collider and applies randomized forces to them
/// on demand or automatically on enter/exit. Each object can be forced only once per stay
/// (deduplication resets when the object leaves and re-enters).
/// Common use: Explosion zones, jump pads, wind tunnels, push traps, physics puzzles, gravity wells.
/// </summary>
[RequireComponent(typeof(Collider))]
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class PhysicsForceZone : MonoBehaviour
{
    public enum ForceSpace { World, Local }
    public enum TargetMode { None, TowardTarget, AwayFromTarget }

    [Header("Tag Filter")]
    [Tooltip("Only Rigidbody objects with this tag will be affected")]
    [SerializeField] private string targetTag = "Player";

    [Header("Target (Optional)")]
    [Tooltip("None: use fixed Force Direction below.\nTowardTarget / AwayFromTarget: direction is computed per object relative to the target.\nIf no target is assigned, the Player-tagged object is used automatically.")]
    [SerializeField] private TargetMode targetMode = TargetMode.None;

    [Tooltip("GameObject to pull toward or push away from. Leave empty to auto-find the Player-tagged object.")]
    [SerializeField] private GameObject directionTarget;

    [Header("Force Direction")]
    [Tooltip("Base direction of the force (does not need to be normalized). Ignored when Target Mode is set.")]
    [SerializeField] private Vector3 forceDirection = Vector3.up;

    [Tooltip("Per-axis random spread added to the base direction. X=2 means ±2 on the X axis.")]
    [SerializeField] private Vector3 randomDirectionOffset = Vector3.zero;

    [Tooltip("World: direction is in world space. Local: direction is relative to this transform. Ignored when Target Mode is set.")]
    [SerializeField] private ForceSpace forceSpace = ForceSpace.World;

    [Header("Force Magnitude")]
    [Tooltip("Minimum force magnitude applied per object")]
    [SerializeField] private float minForce = 5f;

    [Tooltip("Maximum force magnitude applied per object")]
    [SerializeField] private float maxForce = 10f;

    [Tooltip("How the force is applied to the Rigidbody")]
    [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

    [Header("Targeting")]
    [Tooltip("If true, only the first tagged object found in the zone is forced per ApplyForce() call. If false, all tagged objects are forced.")]
    [SerializeField] private bool applyToFirst = false;

    [Header("Deduplication")]
    [Tooltip("Each object can only be forced once per stay. Tracking resets automatically when the object leaves and re-enters the zone. Call ResetTracking() to clear manually.")]
    [SerializeField] private bool oneForcePerStay = true;

    [Header("Auto Apply")]
    [Tooltip("Automatically apply force when a tagged object enters the zone")]
    [SerializeField] private bool applyOnEnter = false;

    [Tooltip("Automatically apply force when a tagged object exits the zone")]
    [SerializeField] private bool applyOnExit = false;

    [Header("Events")]
    /// <summary>
    /// Fires after ApplyForce() is called, passing the number of objects that were forced
    /// </summary>
    public UnityEvent<int> onForceApplied;

    /// <summary>
    /// Fires once per object that receives a force, passing that object's GameObject
    /// </summary>
    public UnityEvent<GameObject> onForceAppliedToObject;

    private readonly List<Rigidbody> objectsInZone = new List<Rigidbody>();
    private readonly HashSet<Rigidbody> forcedThisStay = new HashSet<Rigidbody>();

    private void Start()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
            Debug.LogWarning($"PhysicsForceZone on '{gameObject.name}': Collider is not set to Is Trigger. Objects will not be detected.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        if (!objectsInZone.Contains(rb))
            objectsInZone.Add(rb);

        if (applyOnEnter)
            ApplyForceTo(rb);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        // Clear tracking on exit so re-entry allows force again
        objectsInZone.Remove(rb);
        forcedThisStay.Remove(rb);

        // Exit force applied after clearing — always fires, not blocked by dedup
        if (applyOnExit)
            ForceRigidbody(rb);
    }

    private void OnDestroy()
    {
        objectsInZone.Clear();
        forcedThisStay.Clear();
    }

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Applies force to tagged objects currently inside the zone. Respects applyToFirst and deduplication settings.
    /// </summary>
    public void ApplyForce()
    {
        objectsInZone.RemoveAll(rb => rb == null);

        int count = 0;

        foreach (var rb in objectsInZone)
        {
            bool deduped = oneForcePerStay && forcedThisStay.Contains(rb);
            if (!deduped)
            {
                ApplyForceTo(rb);
                count++;
            }
            if (applyToFirst) break;
        }

        onForceApplied.Invoke(count);
    }

    /// <summary>
    /// Clears deduplication tracking, allowing all objects currently in the zone to be forced again.
    /// </summary>
    public void ResetTracking()
    {
        forcedThisStay.Clear();
    }

    /// <summary>
    /// Returns the number of tagged objects currently inside the zone
    /// </summary>
    public int GetObjectCountInZone() => objectsInZone.Count;

    /// <summary>
    /// Returns true if the given Rigidbody has already been forced during its current stay
    /// </summary>
    public bool HasBeenForced(Rigidbody rb) => forcedThisStay.Contains(rb);

    /// <summary>
    /// Sets the target GameObject used for TowardTarget / AwayFromTarget mode at runtime
    /// </summary>
    public void SetDirectionTarget(GameObject target) => directionTarget = target;

    /// <summary>
    /// Sets the minimum force magnitude
    /// </summary>
    public void SetMinForce(float force) => minForce = Mathf.Max(0f, force);

    /// <summary>
    /// Sets the maximum force magnitude
    /// </summary>
    public void SetMaxForce(float force) => maxForce = Mathf.Max(minForce, force);

    /// <summary>
    /// Sets the base force direction (used when Target Mode is None)
    /// </summary>
    public void SetForceDirection(Vector3 direction) => forceDirection = direction;

    // ──────────────────────────────────────────────
    // Internal force helpers
    // ──────────────────────────────────────────────

    // Dedup-aware: checks and records before applying
    private void ApplyForceTo(Rigidbody rb)
    {
        if (rb == null) return;
        if (oneForcePerStay && forcedThisStay.Contains(rb)) return;

        if (oneForcePerStay) forcedThisStay.Add(rb);

        ForceRigidbody(rb);
    }

    // Raw force application: no dedup check, always fires
    private void ForceRigidbody(Rigidbody rb)
    {
        if (rb == null) return;
        rb.AddForce(GetForceVector(rb), forceMode);
        onForceAppliedToObject.Invoke(rb.gameObject);
    }

    private Vector3 GetForceVector(Rigidbody rb)
    {
        Vector3 dir;

        if (targetMode != TargetMode.None)
        {
            GameObject target = directionTarget;
            if (target == null) target = GameObject.FindWithTag("Player");

            if (target != null)
            {
                dir = (target.transform.position - rb.position).normalized;
                if (targetMode == TargetMode.AwayFromTarget) dir = -dir;
            }
            else
            {
                dir = Vector3.up;
            }
        }
        else
        {
            dir = forceDirection + new Vector3(
                Random.Range(-randomDirectionOffset.x, randomDirectionOffset.x),
                Random.Range(-randomDirectionOffset.y, randomDirectionOffset.y),
                Random.Range(-randomDirectionOffset.z, randomDirectionOffset.z)
            );

            if (forceSpace == ForceSpace.Local)
                dir = transform.TransformDirection(dir);
        }

        if (dir == Vector3.zero) dir = Vector3.up;

        return dir.normalized * Random.Range(minForce, maxForce);
    }

    private void OnValidate()
    {
        minForce = Mathf.Max(0f, minForce);
        maxForce = Mathf.Max(minForce, maxForce);
    }

    // ──────────────────────────────────────────────
    // Editor access
    // ──────────────────────────────────────────────

    public List<Rigidbody> GetObjectsInZone() => objectsInZone;
}
