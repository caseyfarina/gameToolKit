using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// Data structure for state-specific sound configuration
/// </summary>
[System.Serializable]
public struct StateSoundData
{
    [Tooltip("Audio clip to play for this state")]
    public AudioClip clip;

    [Tooltip("Volume for this sound (0-1)")]
    [Range(0f, 1f)]
    public float volume;
}

/// <summary>
/// Configurable multi-state switch with visual and audio feedback that fires events on state changes.
/// Common use: Combination locks, lever puzzles, dial interfaces, multi-position switches, or puzzle sequences.
/// </summary>
public class PuzzleSwitch : MonoBehaviour
{
    [Header("Switch Configuration")]
    [Tooltip("Unique identifier for this switch (used by checker components)")]
    [SerializeField] private string switchID = "Switch1";

    [Tooltip("How many states this switch has (minimum 2)")]
    [SerializeField] private int numberOfStates = 2;

    [Tooltip("Current state index (0-based)")]
    [SerializeField] private int currentState = 0;

    [Tooltip("If true, cycles through states. If false, can be set to specific state via events")]
    [SerializeField] private bool cycleStates = true;

    [Header("Visual Feedback")]
    [Tooltip("Materials for each state (optional, must match numberOfStates)")]
    [SerializeField] private Material[] stateMaterials;

    [Tooltip("Renderer to apply material changes (optional)")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("Optional: Rotation per state (Y-axis degrees)")]
    [SerializeField] private float[] stateRotations;

    [Tooltip("Duration for rotation animation between states (0 = instant)")]
    [SerializeField] private float rotationDuration = 0.3f;

    [Tooltip("Easing curve for rotation animation")]
    [SerializeField] private Ease rotationEase = Ease.OutQuad;

    [Header("Audio")]
    [Tooltip("Enable audio feedback for state changes")]
    [SerializeField] private bool enableAudio = false;

    [Tooltip("AudioSource component to play sounds (required if audio enabled)")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Default sound played when switch changes state")]
    [SerializeField] private AudioClip defaultStateChangeSound;

    [Tooltip("Default volume for state change sound (0-1)")]
    [SerializeField] [Range(0f, 1f)] private float defaultVolume = 1f;

    [Tooltip("Sounds for each specific state (overrides default if set)")]
    [SerializeField] private StateSoundData[] stateSounds;

    [Header("Events")]
    [Tooltip("Fires when state changes, passes new state index")]
    /// <summary>
    /// Fires when the switch state changes, passing the new state index as an int parameter
    /// </summary>
    public UnityEvent<int> onStateChanged = new UnityEvent<int>();

    [Tooltip("Fires on any activation/interaction")]
    /// <summary>
    /// Fires whenever the switch is activated or interacted with
    /// </summary>
    public UnityEvent onActivated = new UnityEvent();

    [Tooltip("Fires when transitioning to state 0")]
    /// <summary>
    /// Fires when the switch transitions to state 0
    /// </summary>
    public UnityEvent onState0 = new UnityEvent();

    [Tooltip("Fires when transitioning to state 1")]
    /// <summary>
    /// Fires when the switch transitions to state 1
    /// </summary>
    public UnityEvent onState1 = new UnityEvent();

    [Tooltip("Fires when transitioning to state 2")]
    /// <summary>
    /// Fires when the switch transitions to state 2
    /// </summary>
    public UnityEvent onState2 = new UnityEvent();

    [Tooltip("Fires when transitioning to state 3")]
    /// <summary>
    /// Fires when the switch transitions to state 3
    /// </summary>
    public UnityEvent onState3 = new UnityEvent();

    [Tooltip("Fires when transitioning to state 4")]
    /// <summary>
    /// Fires when the switch transitions to state 4
    /// </summary>
    public UnityEvent onState4 = new UnityEvent();

    // Internal state
    private Tweener rotationTween;

    void Start()
    {
        // Validate configuration
        if (numberOfStates < 2)
        {
            Debug.LogWarning($"PuzzleSwitch '{switchID}': numberOfStates must be at least 2. Setting to 2.", this);
            numberOfStates = 2;
        }

        // Clamp current state to valid range
        currentState = Mathf.Clamp(currentState, 0, numberOfStates - 1);

        // Auto-find renderer if not set
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        // Validate audio setup
        if (enableAudio && audioSource == null)
        {
            Debug.LogWarning($"PuzzleSwitch '{switchID}': Audio is enabled but no AudioSource is assigned.", this);
        }

        // Apply initial visual state
        UpdateVisuals();
    }

    /// <summary>
    /// Activates the switch, cycling to next state or toggling
    /// </summary>
    public void Activate()
    {
        if (cycleStates)
        {
            NextState();
        }
        else
        {
            // Toggle between 0 and 1 for binary switches
            if (numberOfStates == 2)
            {
                SetState(currentState == 0 ? 1 : 0);
            }
            else
            {
                NextState();
            }
        }

        onActivated.Invoke();
    }

    /// <summary>
    /// Cycles to the next state (wraps around)
    /// </summary>
    public void NextState()
    {
        SetState((currentState + 1) % numberOfStates);
    }

    /// <summary>
    /// Cycles to the previous state (wraps around)
    /// </summary>
    public void PreviousState()
    {
        int newState = currentState - 1;
        if (newState < 0)
            newState = numberOfStates - 1;
        SetState(newState);
    }

    /// <summary>
    /// Sets the switch to a specific state
    /// </summary>
    /// <param name="state">Target state index (0-based)</param>
    public void SetState(int state)
    {
        if (state < 0 || state >= numberOfStates)
        {
            Debug.LogWarning($"PuzzleSwitch '{switchID}': Attempted to set invalid state {state}. Valid range: 0-{numberOfStates - 1}", this);
            return;
        }

        if (currentState == state)
            return; // No change

        currentState = state;
        UpdateVisuals();
        PlayAudio();
        FireEvents();
    }

    /// <summary>
    /// Gets the current state index
    /// </summary>
    public int GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Gets the unique identifier for this switch
    /// </summary>
    public string GetSwitchID()
    {
        return switchID;
    }

    /// <summary>
    /// Resets switch to state 0
    /// </summary>
    public void ResetToInitialState()
    {
        SetState(0);
    }

    /// <summary>
    /// Updates visual feedback based on current state
    /// </summary>
    private void UpdateVisuals()
    {
        // Apply material if available
        if (targetRenderer != null && stateMaterials != null && currentState < stateMaterials.Length && stateMaterials[currentState] != null)
        {
            targetRenderer.material = stateMaterials[currentState];
        }

        // Apply rotation if specified
        if (stateRotations != null && currentState < stateRotations.Length)
        {
            // Kill existing rotation tween
            rotationTween?.Kill();

            Quaternion targetRotation = Quaternion.Euler(0, stateRotations[currentState], 0);

            // Animate rotation with DOTween if duration > 0
            if (rotationDuration > 0)
            {
                rotationTween = transform.DORotate(
                    new Vector3(0, stateRotations[currentState], 0),
                    rotationDuration
                ).SetEase(rotationEase);
            }
            else
            {
                // Instant rotation
                transform.rotation = targetRotation;
            }
        }
    }

    /// <summary>
    /// Plays audio feedback for state change
    /// </summary>
    private void PlayAudio()
    {
        if (!enableAudio || audioSource == null)
            return;

        // Play state-specific sound if available
        if (stateSounds != null && currentState < stateSounds.Length && stateSounds[currentState].clip != null)
        {
            audioSource.PlayOneShot(stateSounds[currentState].clip, stateSounds[currentState].volume);
        }
        // Fallback to default state change sound
        else if (defaultStateChangeSound != null)
        {
            audioSource.PlayOneShot(defaultStateChangeSound, defaultVolume);
        }
    }

    /// <summary>
    /// Fires all relevant events for state change
    /// </summary>
    private void FireEvents()
    {
        // Always fire state changed event
        onStateChanged.Invoke(currentState);

        // Fire state-specific events
        switch (currentState)
        {
            case 0: onState0.Invoke(); break;
            case 1: onState1.Invoke(); break;
            case 2: onState2.Invoke(); break;
            case 3: onState3.Invoke(); break;
            case 4: onState4.Invoke(); break;
        }
    }

    // Gizmo visualization in editor
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            // Show switch ID and state count
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Show switch information
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    void OnDestroy()
    {
        // Clean up DOTween
        rotationTween?.Kill();
    }
}
