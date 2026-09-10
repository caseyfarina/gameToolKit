using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// How a component decides whether it is operating in 2D or 3D.
/// </summary>
public enum PhysicsMode
{
    /// <summary>Detect from the colliders and bodies present on the object.</summary>
    Auto,
    /// <summary>Force 2D physics regardless of what is attached.</summary>
    TwoD,
    /// <summary>Force 3D physics regardless of what is attached.</summary>
    ThreeD
}

/// <summary>
/// Internal helper that owns all 2D-versus-3D physics decisions for the toolkit.
/// Students never see or use this class.
///
/// Unity's 2D and 3D physics are separate engines that never interact, so every
/// component resolves its own dimension independently. Keeping the rules here means
/// they cannot drift across the components that call them.
/// </summary>
public static class EGTKPhysics
{
    // Instance IDs already warned about, so a warning fires once per object rather
    // than every frame.
    private static readonly HashSet<int> WarnedObjects = new HashSet<int>();

    /// <summary>
    /// True when the object should use 2D physics. Honors an explicit mode override;
    /// otherwise detects from the colliders and bodies attached to the object.
    ///
    /// Detection cannot be ambiguous: Unity itself refuses to put 2D and 3D physics
    /// components on the same GameObject, in either order, for both colliders and
    /// rigidbodies. Verified on 6000.3.7f1.
    ///
    /// An object with no collider and no body reports 3D, because that is what the
    /// toolkit did before 2D support existed. A 2D component on a bare GameObject
    /// therefore needs an explicit <see cref="PhysicsMode.TwoD"/> override.
    /// </summary>
    public static bool Is2D(GameObject go, PhysicsMode mode = PhysicsMode.Auto)
    {
        if (mode == PhysicsMode.TwoD) return true;
        if (mode == PhysicsMode.ThreeD) return false;
        if (go == null) return false;

        return go.GetComponent<Collider2D>() != null
               || go.GetComponent<Rigidbody2D>() != null;
    }

    /// <summary>
    /// Applies an instantaneous impulse to whichever body type the target has.
    /// Returns false when the target has no Rigidbody or Rigidbody2D.
    /// </summary>
    public static bool TryAddImpulse(GameObject target, Vector3 force)
    {
        if (target == null) return false;

        Rigidbody2D body2D = target.GetComponent<Rigidbody2D>();
        if (body2D != null)
        {
            body2D.AddForce(force, ForceMode2D.Impulse);
            return true;
        }

        Rigidbody body3D = target.GetComponent<Rigidbody>();
        if (body3D != null)
        {
            body3D.AddForce(force, ForceMode.Impulse);
            return true;
        }

        WarnOnce(target, "has no Rigidbody or Rigidbody2D, so no force was applied. " +
                         "A 3D component cannot push a 2D object, or vice versa.");
        return false;
    }

    /// <summary>
    /// Applies a continuous force to whichever body type the target has.
    /// Returns false when the target has no Rigidbody or Rigidbody2D.
    /// </summary>
    public static bool TryAddForce(GameObject target, Vector3 force)
    {
        if (target == null) return false;

        Rigidbody2D body2D = target.GetComponent<Rigidbody2D>();
        if (body2D != null)
        {
            body2D.AddForce(force, ForceMode2D.Force);
            return true;
        }

        Rigidbody body3D = target.GetComponent<Rigidbody>();
        if (body3D != null)
        {
            body3D.AddForce(force, ForceMode.Force);
            return true;
        }

        WarnOnce(target, "has no Rigidbody or Rigidbody2D, so no force was applied.");
        return false;
    }

    /// <summary>
    /// Stops a target's motion, using whichever body type it has.
    /// Returns false when the target has no Rigidbody or Rigidbody2D.
    /// </summary>
    public static bool TryStopMotion(GameObject target)
    {
        if (target == null) return false;

        Rigidbody2D body2D = target.GetComponent<Rigidbody2D>();
        if (body2D != null)
        {
            body2D.linearVelocity = Vector2.zero;
            body2D.angularVelocity = 0f;
            return true;
        }

        Rigidbody body3D = target.GetComponent<Rigidbody>();
        if (body3D != null)
        {
            body3D.linearVelocity = Vector3.zero;
            body3D.angularVelocity = Vector3.zero;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the GameObject under a screen point, using the matching physics engine.
    /// Returns null when nothing is hit.
    /// </summary>
    public static GameObject PickAtScreenPoint(Vector2 screenPos, float maxDistance,
                                               LayerMask mask, bool is2D, Camera camera = null)
    {
        // Callers that expose their own camera field pass it in. Defaulting to Camera.main
        // silently ignored those, so a component pointed at a second camera picked from the
        // wrong viewpoint, or from nothing at all when there was no MainCamera in the scene.
        Camera cam = camera != null ? camera : Camera.main;
        if (cam == null) return null;

        if (is2D)
        {
            // The z component is the distance from the camera to the plane being picked.
            // Orthographic cameras ignore it, but a perspective camera with z = 0 collapses
            // the result onto the camera's own position, so 2D picking silently misses.
            Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y,
                                              Mathf.Abs(cam.transform.position.z));
            Vector2 worldPoint = cam.ScreenToWorldPoint(screenPoint);
            Collider2D hit2D = Physics2D.OverlapPoint(worldPoint, mask);
            return hit2D != null ? hit2D.gameObject : null;
        }

        Ray ray = cam.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit, maxDistance, mask,
                               QueryTriggerInteraction.Collide)
            ? hit.collider.gameObject
            : null;
    }

    private static void WarnOnce(GameObject go, string message)
    {
        int id = go.GetInstanceID();
        if (!WarnedObjects.Add(id)) return;
        Debug.LogWarning($"[EGTK] '{go.name}' {message}", go);
    }
}
