using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Checkpoint trigger zone that saves player position when entered.
/// Integrates with GameCheckpointManager (or any ISpawnPointProvider) for scene reload survival.
///
/// Works in both 2D and 3D. Give the checkpoint a Collider for a 3D game or a Collider2D
/// for a 2D game; either is set to a trigger automatically.
/// 
/// Common use: Platformer checkpoints, racing lap markers, save points, respawn locations.
/// </summary>
// No [RequireComponent(typeof(Collider))]: that forces a 3D collider, and Unity then
// refuses to add a Collider2D, making 2D checkpoints impossible. Start() validates that
// some trigger collider is present instead.
[HelpURL("https://caseyfarina.github.io/egtk-docs/")]
public class InputCheckpointZone : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Tag of object that activates checkpoint (usually 'Player')")]
    [SerializeField] private string triggerObjectTag = "Player";

    [Tooltip("Only activate once, then disable")]
    [SerializeField] private bool oneTimeUse = false;

    [Tooltip("Save full game state (score, health) or just position?")]
    [SerializeField] private bool saveFullState = false;

    [Header("Spawn Point Configuration")]
    [Tooltip("Offset from checkpoint position where player will respawn")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Tooltip("Use the checkpoint's rotation for respawn orientation")]
    [SerializeField] private bool useCheckpointRotation = false;

    [Header("Persistence")]
    [Tooltip("Reference to GameCheckpointManager. If null, searches automatically.")]
    [SerializeField] private GameCheckpointManager checkpointManager;

    [Header("Visual Feedback")]
    [Tooltip("Object to disable when checkpoint is activated")]
    [SerializeField] private GameObject visualEffect;

    [Tooltip("Renderer to change material when activated")]
    [SerializeField] private Renderer checkpointRenderer;
    
    [Tooltip("Material to apply when activated")]
    [SerializeField] private Material activatedMaterial;

    [Header("Events")]
    /// <summary>
    /// Fires when the checkpoint is activated
    /// </summary>
    public UnityEvent onCheckpointActivated;

    /// <summary>
    /// Fires when checkpoint position is saved, passing the position
    /// </summary>
    public UnityEvent<Vector3> onCheckpointPositionSaved;

    private bool hasBeenActivated = false;
    private Material originalMaterial;
    private Material materialInstance;

    private void Start()
    {
        // Ensure whichever collider is present is a trigger
        Collider col3D = GetComponent<Collider>();
        Collider2D col2D = GetComponent<Collider2D>();

        if (col3D != null && !col3D.isTrigger)
        {
            col3D.isTrigger = true;
            Debug.LogWarning($"InputCheckpointZone '{gameObject.name}': Collider set to trigger automatically.");
        }
        else if (col2D != null && !col2D.isTrigger)
        {
            col2D.isTrigger = true;
            Debug.LogWarning($"InputCheckpointZone '{gameObject.name}': Collider2D set to trigger automatically.");
        }
        else if (col3D == null && col2D == null)
        {
            Debug.LogWarning($"InputCheckpointZone '{gameObject.name}': needs a Collider or Collider2D set to trigger, or it will never activate.", this);
        }

        // Find checkpoint manager if not assigned
        if (checkpointManager == null)
        {
            checkpointManager = GameCheckpointManager.Instance;

            if (checkpointManager == null)
            {
                Debug.LogWarning($"InputCheckpointZone '{gameObject.name}': No GameCheckpointManager found!");
            }
        }

        // Create material instance
        if (checkpointRenderer != null)
        {
            originalMaterial = checkpointRenderer.sharedMaterial;
            materialInstance = new Material(originalMaterial);
            checkpointRenderer.material = materialInstance;
        }

        // Restore visual state if already activated
        if (hasBeenActivated)
        {
            ApplyVisualFeedback();
        }
    }

    private void OnTriggerEnter(Collider other) => HandleEnter(other.tag);
    private void OnTriggerEnter2D(Collider2D other) => HandleEnter(other.tag);

    private void HandleEnter(string otherTag)
    {
        if (otherTag != triggerObjectTag) return;
        if (oneTimeUse && hasBeenActivated) return;

        ActivateCheckpoint();
    }

    /// <summary>
    /// Activate this checkpoint. Can be called manually via UnityEvents.
    /// </summary>
    public void ActivateCheckpoint()
    {
        if (oneTimeUse && hasBeenActivated) return;

        hasBeenActivated = true;

        // Calculate spawn position with offset
        Vector3 spawnPosition = transform.position + transform.TransformDirection(spawnOffset);
        Quaternion spawnRotation = useCheckpointRotation ? transform.rotation : Quaternion.identity;

        // Save to checkpoint manager
        if (checkpointManager != null)
        {
            if (saveFullState)
            {
                checkpointManager.SaveCheckpointFullAtPosition(spawnPosition);
            }
            else
            {
                checkpointManager.SaveCheckpointAtPositionAndRotation(spawnPosition, spawnRotation);
            }

            Debug.Log($"InputCheckpointZone: Saved checkpoint at {spawnPosition}");
        }

        ApplyVisualFeedback();

        onCheckpointActivated.Invoke();
        onCheckpointPositionSaved.Invoke(spawnPosition);

        if (oneTimeUse)
        {
            SetColliderEnabled(false);
        }
    }

    private void ApplyVisualFeedback()
    {
        if (visualEffect != null)
        {
            visualEffect.SetActive(false);
        }

        if (checkpointRenderer != null && activatedMaterial != null)
        {
            checkpointRenderer.material = activatedMaterial;
        }
    }

    /// <summary>
    /// Reset checkpoint to inactive state.
    /// </summary>
    public void ResetCheckpoint()
    {
        hasBeenActivated = false;
        SetColliderEnabled(true);

        if (visualEffect != null)
        {
            visualEffect.SetActive(true);
        }

        if (checkpointRenderer != null && materialInstance != null && originalMaterial != null)
        {
            materialInstance.CopyPropertiesFromMaterial(originalMaterial);
        }

        Debug.Log($"InputCheckpointZone '{gameObject.name}': Reset");
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }

    /// <summary>
    /// Whether this checkpoint has been activated.
    /// </summary>
    public bool IsActivated => hasBeenActivated;

    /// <summary>
    /// The world position where the player will spawn.
    /// </summary>
    public Vector3 SpawnPosition => transform.position + transform.TransformDirection(spawnOffset);

    // Enables or disables whichever collider type this checkpoint uses.
    private void SetColliderEnabled(bool value)
    {
        Collider col3D = GetComponent<Collider>();
        if (col3D != null) col3D.enabled = value;

        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = value;
    }

    private void OnDrawGizmos()
    {
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            Gizmos.color = hasBeenActivated ? Color.green : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Bounds b = col2D.bounds;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireCube(b.center, b.size);
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = hasBeenActivated ? Color.green : Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
                Gizmos.DrawWireCube(box.center, box.size);
            else if (col is SphereCollider sphere)
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            else if (col is CapsuleCollider capsule)
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
        }

        // Draw spawn position
        Gizmos.matrix = Matrix4x4.identity;
        Vector3 spawnPos = transform.position + transform.TransformDirection(spawnOffset);

        // Flag icon
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(spawnPos, spawnPos + Vector3.up * 2f);
        Gizmos.DrawWireSphere(spawnPos + Vector3.up * 2f, 0.3f);

        // Spawn offset indicator
        if (spawnOffset != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(spawnPos, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPos);
        }

        // Rotation indicator
        if (useCheckpointRotation)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(spawnPos, transform.forward * 1.5f);
        }
    }
}
