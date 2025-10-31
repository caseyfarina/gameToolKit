using UnityEngine;

/// <summary>
/// Applies continuous attraction force toward a target object with distance-based falloff options.
/// Common use: Gravity wells, magnetic effects, object orbits, tractor beams, or black hole mechanics.
/// </summary>
public class ObjectAttractor : MonoBehaviour
{
    [Header("Attraction Settings")]
    [Tooltip("The target object to attract towards")]
    public Transform targetObject;
    
    [Tooltip("The strength of the attraction force")]
    public float attractionStrength = 10f;
    
    [Tooltip("Minimum distance before attraction stops (prevents jittering)")]
    public float minDistance = 0.1f;
    
    [Tooltip("Maximum distance for attraction (0 = no limit)")]
    public float maxDistance = 0f;
    
    [Header("Force Application")]
    [Tooltip("How the force is applied")]
    public ForceMode forceMode = ForceMode.Force;
    
    [Tooltip("Use square falloff (more realistic physics)")]
    public bool useSquareFalloff = true;
    
    private Rigidbody rb;
    
    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("ObjectAttractor requires a Rigidbody component on " + gameObject.name);
        }
        
        if (targetObject == null)
        {
            Debug.LogWarning("Target object not set for ObjectAttractor on " + gameObject.name);
        }
    }
    
    void FixedUpdate()
    {
        // Only apply attraction if we have valid components and target
        if (rb != null && targetObject != null)
        {
            ApplyAttraction();
        }
    }
    
    void ApplyAttraction()
    {
        // Calculate direction vector from this object to target
        Vector3 direction = targetObject.position - transform.position;
        float distance = direction.magnitude;
        
        // Check if we're within the minimum distance threshold
        if (distance < minDistance)
        {
            return; // Too close, don't apply force to prevent jittering
        }
        
        // Check if we're beyond the maximum distance (if set)
        if (maxDistance > 0f && distance > maxDistance)
        {
            return; // Too far, don't apply attraction
        }
        
        // Normalize the direction vector
        direction = direction.normalized;
        
        // Calculate force magnitude
        float forceMagnitude = attractionStrength;
        
        // Apply square falloff if enabled (inverse square law like gravity)
        if (useSquareFalloff)
        {
            forceMagnitude = attractionStrength / (distance * distance);
        }
        
        // Calculate final force vector
        Vector3 attractionForce = direction * forceMagnitude;
        
        // Apply the force to the rigidbody
        rb.AddForce(attractionForce, forceMode);
    }
    
    // Optional: Draw gizmos in the scene view to visualize attraction
    void OnDrawGizmosSelected()
    {
        if (targetObject != null)
        {
            // Draw line to target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetObject.position);
            
            // Draw min distance sphere
            if (minDistance > 0f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(targetObject.position, minDistance);
            }
            
            // Draw max distance sphere
            if (maxDistance > 0f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(targetObject.position, maxDistance);
            }
        }
    }
}