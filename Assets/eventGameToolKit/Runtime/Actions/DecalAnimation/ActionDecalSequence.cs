using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Material frame data structure for decal animation sequences.
/// Each frame holds a material and how long it should be displayed.
/// </summary>
[System.Serializable]
public struct MaterialFrame
{
    [Tooltip("Material to display for this frame")]
    public Material material;

    [Tooltip("How long to display this material (in seconds)")]
    public float duration;
}

/// <summary>
/// Plays a sequence of materials on a URP Decal Projector with custom timing.
/// Perfect for animated facial expressions, texture-based animations, or cycling decals.
/// Students can build custom animation sequences by adding material frames in the Inspector.
/// </summary>
[RequireComponent(typeof(DecalProjector))]
public class ActionDecalSequence : MonoBehaviour
{
    [Header("Material Sequence")]
    [Tooltip("Array of materials and their display durations. Add frames to create your animation sequence.")]
    [SerializeField] private MaterialFrame[] materialFrames = new MaterialFrame[0];

    [Header("Playback Settings")]
    [Tooltip("Start playing the sequence automatically when the scene starts")]
    [SerializeField] private bool playOnStart = false;

    [Tooltip("Loop the sequence continuously (restart from beginning when complete)")]
    [SerializeField] private bool loop = false;

    [Tooltip("Time scale multiplier for the entire sequence (1.0 = normal speed, 0.5 = half speed, 2.0 = double speed)")]
    [SerializeField] [Range(0.1f, 5f)] private float playbackSpeed = 1.0f;

    [Header("Sequence Events")]
    /// <summary>
    /// Fires when the sequence starts playing
    /// </summary>
    public UnityEvent onSequenceStart;

    /// <summary>
    /// Fires when the sequence completes (does not fire if looping)
    /// </summary>
    public UnityEvent onSequenceComplete;

    /// <summary>
    /// Fires when the sequence is paused
    /// </summary>
    public UnityEvent onSequencePause;

    /// <summary>
    /// Fires when the sequence is resumed from pause
    /// </summary>
    public UnityEvent onSequenceResume;

    /// <summary>
    /// Fires when the sequence is stopped
    /// </summary>
    public UnityEvent onSequenceStop;

    /// <summary>
    /// Fires each time a new frame is displayed, passing the frame index as parameter
    /// </summary>
    public UnityEvent<int> onFrameChanged;

    // Private state
    private DecalProjector decalProjector;
    private Coroutine sequenceCoroutine;
    private bool isPlaying = false;
    private bool isPaused = false;
    private int currentFrameIndex = -1;
    private float pauseTimeRemaining = 0f;

    // Public properties
    public bool IsPlaying => isPlaying;
    public bool IsPaused => isPaused;
    public int CurrentFrameIndex => currentFrameIndex;
    public int TotalFrames => materialFrames.Length;

    private void Awake()
    {
        // Get the DecalProjector component
        decalProjector = GetComponent<DecalProjector>();

        if (decalProjector == null)
        {
            Debug.LogError($"[{gameObject.name}] ActionDecalSequence requires a DecalProjector component!", this);
            enabled = false;
            return;
        }

        // Validate sequence
        if (materialFrames.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] ActionDecalSequence: No material frames defined. Add frames in the Inspector.", this);
        }
    }

    private void Start()
    {
        if (playOnStart && materialFrames.Length > 0)
        {
            Play();
        }
    }

    /// <summary>
    /// Starts playing the material sequence from the beginning
    /// </summary>
    public void Play()
    {
        if (materialFrames.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] Cannot play: No material frames defined.", this);
            return;
        }

        // Stop any existing sequence
        if (isPlaying)
        {
            Stop();
        }

        isPlaying = true;
        isPaused = false;
        currentFrameIndex = -1;
        pauseTimeRemaining = 0f;

        onSequenceStart?.Invoke();
        sequenceCoroutine = StartCoroutine(PlaySequence());
    }

    /// <summary>
    /// Stops the sequence and resets to the beginning
    /// </summary>
    public void Stop()
    {
        if (!isPlaying) return;

        isPlaying = false;
        isPaused = false;
        pauseTimeRemaining = 0f;

        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        onSequenceStop?.Invoke();
    }

    /// <summary>
    /// Pauses the sequence at the current frame
    /// </summary>
    public void Pause()
    {
        if (!isPlaying || isPaused) return;

        isPaused = true;
        onSequencePause?.Invoke();
    }

    /// <summary>
    /// Resumes the sequence from where it was paused
    /// </summary>
    public void Resume()
    {
        if (!isPlaying || !isPaused) return;

        isPaused = false;
        onSequenceResume?.Invoke();
    }

    /// <summary>
    /// Jumps to a specific frame in the sequence (0-indexed)
    /// </summary>
    public void JumpToFrame(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= materialFrames.Length)
        {
            Debug.LogWarning($"[{gameObject.name}] Frame index {frameIndex} is out of range (0-{materialFrames.Length - 1}).", this);
            return;
        }

        currentFrameIndex = frameIndex;
        SetCurrentMaterial(currentFrameIndex);
    }

    /// <summary>
    /// Sets the playback speed multiplier at runtime
    /// </summary>
    public void SetPlaybackSpeed(float speed)
    {
        playbackSpeed = Mathf.Clamp(speed, 0.1f, 5f);
    }

    /// <summary>
    /// Sets whether the sequence should loop
    /// </summary>
    public void SetLoop(bool shouldLoop)
    {
        loop = shouldLoop;
    }

    /// <summary>
    /// Adds a new material frame to the end of the sequence at runtime
    /// </summary>
    public void AddFrame(Material material, float duration)
    {
        MaterialFrame newFrame = new MaterialFrame
        {
            material = material,
            duration = duration
        };

        System.Array.Resize(ref materialFrames, materialFrames.Length + 1);
        materialFrames[materialFrames.Length - 1] = newFrame;
    }

    /// <summary>
    /// Clears all material frames from the sequence
    /// </summary>
    public void ClearFrames()
    {
        if (isPlaying)
        {
            Stop();
        }

        materialFrames = new MaterialFrame[0];
        currentFrameIndex = -1;
    }

    private IEnumerator PlaySequence()
    {
        do
        {
            // Play through all frames
            for (int i = 0; i < materialFrames.Length; i++)
            {
                if (!isPlaying) yield break;

                currentFrameIndex = i;
                MaterialFrame frame = materialFrames[i];

                // Set the material
                SetCurrentMaterial(i);

                // Fire frame changed event
                onFrameChanged?.Invoke(currentFrameIndex);

                // Calculate adjusted duration based on playback speed
                float adjustedDuration = frame.duration / playbackSpeed;
                float elapsed = pauseTimeRemaining > 0 ? pauseTimeRemaining : 0f;
                pauseTimeRemaining = 0f;

                // Wait for frame duration (handle pause)
                while (elapsed < adjustedDuration)
                {
                    if (!isPlaying) yield break;

                    if (!isPaused)
                    {
                        elapsed += Time.deltaTime;
                    }
                    else
                    {
                        // Store remaining time when paused
                        pauseTimeRemaining = adjustedDuration - elapsed;
                    }

                    yield return null;
                }
            }

            // Sequence completed one full loop
            if (!loop)
            {
                isPlaying = false;
                onSequenceComplete?.Invoke();
            }

        } while (loop && isPlaying);
    }

    private void SetCurrentMaterial(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= materialFrames.Length)
            return;

        MaterialFrame frame = materialFrames[frameIndex];

        if (frame.material != null && decalProjector != null)
        {
            decalProjector.material = frame.material;
        }
        else if (frame.material == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Frame {frameIndex} has no material assigned!", this);
        }
    }

    private void OnDestroy()
    {
        // Clean up coroutine
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }
    }

    private void OnDisable()
    {
        // Stop sequence when disabled
        if (isPlaying)
        {
            Stop();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Clamp playback speed
        playbackSpeed = Mathf.Clamp(playbackSpeed, 0.1f, 5f);

        // Validate frame durations
        for (int i = 0; i < materialFrames.Length; i++)
        {
            if (materialFrames[i].duration < 0f)
            {
                materialFrames[i].duration = 0.1f;
            }
        }
    }
#endif
}
