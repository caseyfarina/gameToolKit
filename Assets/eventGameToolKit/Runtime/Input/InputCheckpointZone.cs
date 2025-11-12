using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Checkpoint trigger zone that saves player position when entered.
/// Automatically integrates with GameCheckpointManager for scene reload survival.
/// Common use: Platformer checkpoints, racing lap markers, save points, respawn locations.
/// </summary>
[RequireComponent(typeof(Collider))]
public class InputCheckpointZone : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Tag of object that activates checkpoint (usually 'Player')")]
    [SerializeField] private string triggerObjectTag = "Player";

    [Tooltip("Only activate once, then disable")]
    [SerializeField] private bool oneTimeUse = false;

    [Tooltip("Save full game state (score, health) or just position?")]
    [SerializeField] private bool saveFullState = false;

    [Header("Persistence")]
    [Tooltip("Reference to GameCheckpointManager. If null, searches automatically.")]
    [SerializeField] private GameCheckpointManager checkpointPersistence;

    [Header("Visual Feedback")]
    [Tooltip("Object to disable when checkpoint is activated (e.g., particle effect)")]
    [SerializeField] private GameObject visualEffect;

    [Tooltip("Material to change when activated")]
    [SerializeField] private Renderer checkpointRenderer;
    [SerializeField] private Material activatedMaterial;

    [Header("Events")]
    /// <summary>
    /// Fires when the checkpoint is activated by the player entering the zone
    /// </summary>
    public UnityEvent onCheckpointActivated;
    /// <summary>
    /// Fires when checkpoint position is saved, passing the checkpoint's position as a Vector3 parameter
    /// </summary>
    public UnityEvent<Vector3> onCheckpointPositionSaved;  // Passes checkpoint position

    [SerializeField] [HideInInspector] private bool hasBeenActivated = false;
    private Material originalMaterial;
    private Material materialInstance; // Instance to avoid shared material modification

    private void Start()
    {
        // Ensure collider is set to trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"InputCheckpointZone on {gameObject.name}: Collider was not set as trigger. Automatically fixed.");
        }

        // DEBUG: Log initial state
        Debug.Log($"InputCheckpointZone '{gameObject.name}' Start(): hasBeenActivated={hasBeenActivated}, oneTimeUse={oneTimeUse}, collider.enabled={col?.enabled}, collider.isTrigger={col?.isTrigger}");

        // Find checkpoint persistence if not assigned
        if (checkpointPersistence == null)
        {
            // Use singleton instance (works across DontDestroyOnLoad)
            checkpointPersistence = GameCheckpointManager.Instance;

            if (checkpointPersistence == null)
            {
                Debug.LogWarning($"InputCheckpointZone '{gameObject.name}': No GameCheckpointManager found in scene! Add one to enable checkpoint saving.");
            }
            else
            {
                Debug.Log($"InputCheckpointZone '{gameObject.name}': Successfully found GameCheckpointManager singleton");
            }
        }
        else
        {
            Debug.Log($"InputCheckpointZone '{gameObject.name}': Using assigned checkpointPersistence reference");
        }

        // Create material instance to avoid shared material modification
        if (checkpointRenderer != null)
        {
            originalMaterial = checkpointRenderer.sharedMaterial;
            materialInstance = new Material(originalMaterial);
            checkpointRenderer.material = materialInstance;
        }

        // Apply visual feedback if already activated (preserves state across scene reloads)
        if (hasBeenActivated)
        {
            Debug.Log($"InputCheckpointZone '{gameObject.name}': Already activated, applying visual feedback");
            ApplyVisualFeedback();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"InputCheckpointZone '{gameObject.name}' OnTriggerEnter: other.name='{other.name}', other.tag='{other.tag}', looking for tag='{triggerObjectTag}'");

        // Check if correct tag
        if (!other.CompareTag(triggerObjectTag))
        {
            Debug.Log($"InputCheckpointZone '{gameObject.name}': Tag mismatch, ignoring trigger");
            return;
        }

        // Check if already activated (for one-time use)
        if (oneTimeUse && hasBeenActivated)
        {
            Debug.Log($"InputCheckpointZone '{gameObject.name}': Already activated (oneTimeUse=true), ignoring trigger");
            return;
        }

        Debug.Log($"InputCheckpointZone '{gameObject.name}': Triggering ActivateCheckpoint()");
        // Activate checkpoint
        ActivateCheckpoint();
    }

    /// <summary>
    /// Activate this checkpoint (can be called manually via events)
    /// </summary>
    public void ActivateCheckpoint()
    {
        Debug.Log($"InputCheckpointZone '{gameObject.name}' ActivateCheckpoint() called: oneTimeUse={oneTimeUse}, hasBeenActivated={hasBeenActivated}");

        if (oneTimeUse && hasBeenActivated)
        {
            Debug.Log($"InputCheckpointZone '{gameObject.name}': Already activated (oneTimeUse=true), exiting ActivateCheckpoint()");
            return;
        }

        hasBeenActivated = true;
        Debug.Log($"InputCheckpointZone '{gameObject.name}': Set hasBeenActivated=true");

        // Save to persistence system - use CHECKPOINT position, not player position
        if (checkpointPersistence != null)
        {
            Debug.Log($"InputCheckpointZone '{gameObject.name}': checkpointPersistence found, saving position {transform.position}, saveFullState={saveFullState}");

            if (saveFullState)
            {
                checkpointPersistence.SaveCheckpointFullAtPosition(transform.position);
            }
            else
            {
                checkpointPersistence.SaveCheckpointAtPosition(transform.position);
            }

            Debug.Log($"Checkpoint activated at {transform.position}");
        }
        else
        {
            Debug.LogWarning($"InputCheckpointZone '{gameObject.name}': checkpointPersistence is NULL! Cannot save checkpoint!");
        }

        // Visual feedback
        ApplyVisualFeedback();

        // Fire events
        onCheckpointActivated.Invoke();
        onCheckpointPositionSaved.Invoke(transform.position);

        // Disable if one-time use
        if (oneTimeUse)
        {
            Debug.Log($"InputCheckpointZone '{gameObject.name}': oneTimeUse=true, disabling collider");
            GetComponent<Collider>().enabled = false;
        }
    }

    /// <summary>
    /// Apply visual feedback when checkpoint is activated
    /// </summary>
    private void ApplyVisualFeedback()
    {
        // Disable visual effect (like particles)
        if (visualEffect != null)
        {
            visualEffect.SetActive(false);
        }

        // Change material
        if (checkpointRenderer != null && activatedMaterial != null)
        {
            checkpointRenderer.material = activatedMaterial;
        }
    }

    /// <summary>
    /// Reset checkpoint to inactive state (for testing or reusable checkpoints)
    /// </summary>
    public void ResetCheckpoint()
    {
        hasBeenActivated = false;
        GetComponent<Collider>().enabled = true;

        // Restore visual state
        if (visualEffect != null)
        {
            visualEffect.SetActive(true);
        }

        if (checkpointRenderer != null && materialInstance != null && originalMaterial != null)
        {
            // Copy original material properties to instance
            materialInstance.CopyPropertiesFromMaterial(originalMaterial);
        }

        Debug.Log($"Checkpoint at {transform.position} has been reset");
    }

    private void OnDestroy()
    {
        // Clean up material instance
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }

    /// <summary>
    /// Check if this checkpoint has been activated
    /// </summary>
    public bool IsActivated => hasBeenActivated;

    private void OnDrawGizmos()
    {
        // Draw checkpoint zone in editor
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = hasBeenActivated ? Color.green : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
            }
        }

        // Draw checkpoint icon above trigger
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
    }
}
