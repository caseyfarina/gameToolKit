using UnityEngine;
using UnityEngine.Events;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Applies repulsion forces to colliding objects with animated visual feedback and cooldown system.
/// Common use: Bounce pads, pinball bumpers, launch mechanisms, or trampoline effects.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PhysicsBumper : MonoBehaviour
{
    [Header("Bumper Settings")]
    [Tooltip("Force applied to the player on collision")]
    [SerializeField] private float bumperForce = 20f;

    [Tooltip("Should the force be relative to collision point or just use up direction?")]
    [SerializeField] private bool useCollisionNormal = true;

    [Tooltip("Additional upward force multiplier")]
    [Range(0f, 2f)]
    [SerializeField] private float upwardForceMultiplier = 0.5f;

    [Header("Cooldown Settings")]
    [Tooltip("Time in seconds before the bumper can be triggered again")]
    [Min(0f)]
    [SerializeField] private float cooldownDuration = 0.5f;

    [Header("Animation Settings")]
    [Tooltip("Animation curve for scaling and emission (defaults to ease out)")]
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("Scale multiplier for each axis (X, Y, Z) during animation. Use 1 for no scaling on that axis")]
    [SerializeField] private Vector3 scaleMultiplier = new Vector3(1.5f, 1.5f, 1.5f);
    
    [Tooltip("How long the animation lasts")]
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Material Settings")]
    [Tooltip("Should the bumper animate its material emission on collision?")]
    [SerializeField] private bool useEmissionAnimation = false;

    [Tooltip("Name of the shader property to animate (e.g., '_EmissionIntensity' for shadergraph)")]
    [SerializeField] private string emissionPropertyName = "_EmissionColor";

    [Tooltip("Color of the emission at peak animation (for standard materials)")]
    [SerializeField] private Color emissionColor = Color.white;

    [Tooltip("Intensity of the emission effect")]
    [SerializeField] private float emissionIntensity = 2f;

    [Header("Events")]
    /// <summary>
    /// Fires when the bumper is triggered by a collision with the player
    /// </summary>
    [SerializeField] private UnityEvent onBumperTriggered;
    /// <summary>
    /// Fires when the bumper's cooldown period completes and it can be triggered again
    /// </summary>
    [SerializeField] private UnityEvent onBumperCooldownComplete;

    private Vector3 originalScale;
    private Material material;
    private bool hasEmission = false;
    private Color baseEmissionColor;
    private float baseEmissionFloat;
    private bool isFloatProperty = false;
    private Coroutine animationCoroutine;
    private float lastTriggerTime = -Mathf.Infinity; // Initialize to allow immediate first use

    private void Start()
    {
        originalScale = transform.localScale;
        
        // Set up default ease out curve if none is provided
        if (animationCurve.keys.Length == 0)
        {
            animationCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 1f),
                new Keyframe(1f, 1f, 0f, 0f)
            );
            animationCurve.preWrapMode = WrapMode.Once;
            animationCurve.postWrapMode = WrapMode.Once;
        }

        // Set up material if emission animation is enabled
        if (useEmissionAnimation)
        {
            SetupMaterial();
        }
    }

    private void SetupMaterial()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create a material instance to avoid modifying the shared material
            material = new Material(renderer.sharedMaterial);
            renderer.material = material;

            // Check if the specified property exists on the material
            if (material.HasProperty(emissionPropertyName))
            {
                hasEmission = true;
                
                // Determine if it's a float or color property by checking if the property exists as float
                if (material.HasProperty(emissionPropertyName))
                {
                    try
                    {
                        // Try to get as float first
                        baseEmissionFloat = material.GetFloat(emissionPropertyName);
                        isFloatProperty = true;
                        Debug.Log($"Found float property '{emissionPropertyName}' with base value: {baseEmissionFloat}");
                    }
                    catch
                    {
                        // If that fails, it's a color property
                        isFloatProperty = false;
                        baseEmissionColor = material.GetColor(emissionPropertyName);
                        Debug.Log($"Found color property '{emissionPropertyName}' with base value: {baseEmissionColor}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Material does not have property '{emissionPropertyName}'. Emission animation will be disabled.");
                useEmissionAnimation = false;
            }
        }
        else
        {
            Debug.LogWarning("No renderer found. Emission animation will be disabled.");
            useEmissionAnimation = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // Check if cooldown has elapsed
        if (!CanTrigger()) return;

        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
        if (playerRb == null) return;

        Vector3 bounceDirection;
        if (useCollisionNormal)
        {
            // Use the collision normal, but inverted to push away from bumper
            // The collision normal points into the surface, we want to push away
            bounceDirection = -collision.contacts[0].normal;
        }
        else
        {
            // Use direction from bumper to player
            bounceDirection = (collision.transform.position - transform.position).normalized;
        }

        // Add upward component
        bounceDirection += Vector3.up * upwardForceMultiplier;
        bounceDirection.Normalize();

        // Apply the force
        playerRb.AddForce(bounceDirection * bumperForce, ForceMode.Impulse);
        
        // Update last trigger time and start animation
        Trigger();

        // Invoke the collision event
        onBumperTriggered?.Invoke();

        // Start cooldown completion check
        StartCoroutine(CheckCooldownCompletion());
    }

    private IEnumerator CheckCooldownCompletion()
    {
        yield return new WaitForSeconds(cooldownDuration);
        onBumperCooldownComplete?.Invoke();
    }

    /// <summary>
    /// Checks if enough time has passed since the last trigger.
    /// </summary>
    public bool CanTrigger()
    {
        return Time.time >= lastTriggerTime + cooldownDuration;
    }

    /// <summary>
    /// Returns the remaining cooldown time in seconds.
    /// </summary>
    public float GetRemainingCooldown()
    {
        float remaining = (lastTriggerTime + cooldownDuration) - Time.time;
        return Mathf.Max(0f, remaining);
    }

    /// <summary>
    /// Manually triggers the bumper animation and starts the cooldown.
    /// </summary>
    public void Trigger()
    {
        lastTriggerTime = Time.time;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateEffects());
    }

    /// <summary>
    /// Resets the cooldown, allowing immediate triggering.
    /// </summary>
    public void ResetCooldown()
    {
        lastTriggerTime = -Mathf.Infinity;
    }

    /// <summary>
    /// Updates the cooldown duration.
    /// </summary>
    public void SetCooldownDuration(float duration)
    {
        cooldownDuration = Mathf.Max(0f, duration);
    }

    private IEnumerator AnimateEffects()
    {
        float elapsedTime = 0f;
        // Calculate target scale for each axis independently
        Vector3 targetScale = new Vector3(
            originalScale.x * scaleMultiplier.x,
            originalScale.y * scaleMultiplier.y,
            originalScale.z * scaleMultiplier.z
        );

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / animationDuration;
            
            // Evaluate animation progress
            float progress = animationCurve.Evaluate(normalizedTime);

            // Animate scale
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);

            // Animate emission if enabled (check useEmissionAnimation every frame)
            if (useEmissionAnimation && hasEmission && material != null)
            {
                if (isFloatProperty)
                {
                    // Animate float property (e.g., EmissionIntensity for shadergraph)
                    // Base should already be 1, so just add the animated intensity
                    float animatedValue = baseEmissionFloat + (emissionIntensity * progress);
                    material.SetFloat(emissionPropertyName, animatedValue);
                }
                else
                {
                    // Animate color property (standard URP emission)
                    Color targetEmission = emissionColor * emissionIntensity;
                    material.SetColor(emissionPropertyName, Color.Lerp(baseEmissionColor, targetEmission, progress));
                }
            }
            
            yield return null;
        }

        // Reset to original values
        transform.localScale = originalScale;
        if (useEmissionAnimation && hasEmission && material != null)
        {
            if (isFloatProperty)
            {
                // Reset to original base value (which should already be 1 for flat look)
                material.SetFloat(emissionPropertyName, baseEmissionFloat);
            }
            else
            {
                material.SetColor(emissionPropertyName, baseEmissionColor);
            }
        }

        animationCoroutine = null;
    }

    private void OnDestroy()
    {
        // Clean up material instance if we created one
        if (material != null)
        {
            Destroy(material);
        }
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw arrow to show bumper force
        float arrowLength = bumperForce * 0.1f;
        Vector3 arrowDirection = useCollisionNormal ? Vector3.up : Vector3.forward;
        arrowDirection += Vector3.up * upwardForceMultiplier;
        arrowDirection.Normalize();

        Handles.color = Color.yellow;
        
        // Draw main arrow
        Vector3 start = transform.position;
        Vector3 end = start + arrowDirection * arrowLength;
        Handles.DrawDottedLine(start, end, 2f);
        
        // Draw arrow head
        float headSize = arrowLength * 0.2f;
        Vector3[] points = new Vector3[4];
        points[0] = end;
        points[1] = end - arrowDirection * headSize + Vector3.right * headSize;
        points[2] = end - arrowDirection * headSize - Vector3.right * headSize;
        points[3] = end;

        Handles.DrawPolyLine(points);

        // Add force, animation, and cooldown info labels
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        
        // In play mode, show cooldown status
        string cooldownInfo = "";
        if (Application.isPlaying)
        {
            float remainingCooldown = GetRemainingCooldown();
            if (remainingCooldown > 0)
            {
                cooldownInfo = $"\nCooldown: {remainingCooldown:F1}s";
            }
        }

        // Add emission info if enabled
        string emissionInfo = useEmissionAnimation ? $"\nEmission Enabled (x{emissionIntensity:F1})" : "";
        
        Handles.Label(end + Vector3.up * 0.5f, 
            $"Force: {bumperForce}\n" +
            $"Scale: ({scaleMultiplier.x:F1}, {scaleMultiplier.y:F1}, {scaleMultiplier.z:F1})\n" +
            $"Duration: {animationDuration:F2}s" +
            emissionInfo +
            cooldownInfo, 
            style);
    }
    #endif
}