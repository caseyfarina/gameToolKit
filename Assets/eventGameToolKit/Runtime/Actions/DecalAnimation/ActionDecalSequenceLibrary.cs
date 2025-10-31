using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages multiple ActionDecalSequence components on the same GameObject,
/// allowing students to switch between different facial animation sequences.
/// Perfect for switching between expressions, dialogue states, and reactions.
/// </summary>
public class ActionDecalSequenceLibrary : MonoBehaviour
{
    [Header("Sequence Library")]
    [Tooltip("Available sequences that can be played. Add ActionDecalSequence components to this list.")]
    [SerializeField] private ActionDecalSequence[] sequences = new ActionDecalSequence[0];

    [Header("Playback Settings")]
    [Tooltip("Index of sequence to play on start (-1 = none, 0 = first sequence, etc.)")]
    [SerializeField] private int defaultSequenceIndex = -1;

    [Tooltip("Should the default sequence start playing automatically?")]
    [SerializeField] private bool playOnStart = false;

    [Header("Library Events")]
    /// <summary>
    /// Fires when switching to a new sequence, passes the sequence index
    /// </summary>
    public UnityEvent<int> onSequenceChanged;

    /// <summary>
    /// Fires when the library stops all sequences
    /// </summary>
    public UnityEvent onLibraryStopped;

    // Private state
    private int currentSequenceIndex = -1;

    // Public properties
    public int CurrentSequenceIndex => currentSequenceIndex;
    public int TotalSequences => sequences.Length;
    public bool IsPlaying => currentSequenceIndex >= 0 &&
                              sequences[currentSequenceIndex] != null &&
                              sequences[currentSequenceIndex].IsPlaying;

    private void Awake()
    {
        // Validate sequences
        if (sequences.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] ActionDecalSequenceLibrary: No sequences assigned. Add ActionDecalSequence components to the library.", this);
        }

        // Validate default index
        if (defaultSequenceIndex >= sequences.Length)
        {
            Debug.LogWarning($"[{gameObject.name}] Default sequence index {defaultSequenceIndex} is out of range (0-{sequences.Length - 1}). Resetting to -1.", this);
            defaultSequenceIndex = -1;
        }
    }

    private void Start()
    {
        if (playOnStart && defaultSequenceIndex >= 0 && defaultSequenceIndex < sequences.Length)
        {
            PlaySequence(defaultSequenceIndex);
        }
    }

    /// <summary>
    /// Plays a sequence by index, stopping the currently playing sequence if any
    /// </summary>
    /// <param name="index">Index of the sequence to play (0-based)</param>
    public void PlaySequence(int index)
    {
        if (index < 0 || index >= sequences.Length)
        {
            Debug.LogWarning($"[{gameObject.name}] Sequence index {index} is out of range (0-{sequences.Length - 1}).", this);
            return;
        }

        if (sequences[index] == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Sequence at index {index} is null!", this);
            return;
        }

        // Stop current sequence if different from requested
        if (currentSequenceIndex >= 0 && currentSequenceIndex != index)
        {
            if (sequences[currentSequenceIndex] != null)
            {
                sequences[currentSequenceIndex].Stop();
            }
        }

        // Play new sequence
        currentSequenceIndex = index;
        sequences[index].Play();
        onSequenceChanged?.Invoke(index);
    }

    /// <summary>
    /// Plays a sequence by name (uses the GameObject name of the ActionDecalSequence)
    /// </summary>
    /// <param name="sequenceName">Name to search for</param>
    public void PlaySequenceByName(string sequenceName)
    {
        for (int i = 0; i < sequences.Length; i++)
        {
            if (sequences[i] != null && sequences[i].gameObject.name == sequenceName)
            {
                PlaySequence(i);
                return;
            }
        }

        Debug.LogWarning($"[{gameObject.name}] No sequence found with name '{sequenceName}'.", this);
    }

    /// <summary>
    /// Stops the currently playing sequence
    /// </summary>
    public void StopCurrentSequence()
    {
        if (currentSequenceIndex >= 0 && currentSequenceIndex < sequences.Length)
        {
            if (sequences[currentSequenceIndex] != null)
            {
                sequences[currentSequenceIndex].Stop();
            }

            currentSequenceIndex = -1;
            onLibraryStopped?.Invoke();
        }
    }

    /// <summary>
    /// Pauses the currently playing sequence
    /// </summary>
    public void PauseCurrentSequence()
    {
        if (currentSequenceIndex >= 0 && currentSequenceIndex < sequences.Length)
        {
            if (sequences[currentSequenceIndex] != null)
            {
                sequences[currentSequenceIndex].Pause();
            }
        }
    }

    /// <summary>
    /// Resumes the currently paused sequence
    /// </summary>
    public void ResumeCurrentSequence()
    {
        if (currentSequenceIndex >= 0 && currentSequenceIndex < sequences.Length)
        {
            if (sequences[currentSequenceIndex] != null)
            {
                sequences[currentSequenceIndex].Resume();
            }
        }
    }

    /// <summary>
    /// Plays the next sequence in the library (wraps around to 0 if at the end)
    /// </summary>
    public void PlayNextSequence()
    {
        if (sequences.Length == 0) return;

        int nextIndex = (currentSequenceIndex + 1) % sequences.Length;
        PlaySequence(nextIndex);
    }

    /// <summary>
    /// Plays the previous sequence in the library (wraps around to end if at the beginning)
    /// </summary>
    public void PlayPreviousSequence()
    {
        if (sequences.Length == 0) return;

        int prevIndex = currentSequenceIndex - 1;
        if (prevIndex < 0) prevIndex = sequences.Length - 1;
        PlaySequence(prevIndex);
    }

    /// <summary>
    /// Gets the currently playing sequence (or null if none)
    /// </summary>
    public ActionDecalSequence GetCurrentSequence()
    {
        if (currentSequenceIndex >= 0 && currentSequenceIndex < sequences.Length)
        {
            return sequences[currentSequenceIndex];
        }

        return null;
    }

    /// <summary>
    /// Gets a sequence by index
    /// </summary>
    public ActionDecalSequence GetSequence(int index)
    {
        if (index >= 0 && index < sequences.Length)
        {
            return sequences[index];
        }

        return null;
    }

    /// <summary>
    /// Sets the default sequence index at runtime
    /// </summary>
    public void SetDefaultSequenceIndex(int index)
    {
        if (index >= -1 && index < sequences.Length)
        {
            defaultSequenceIndex = index;
        }
    }

    private void OnDisable()
    {
        // Stop current sequence when disabled
        StopCurrentSequence();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Clamp default sequence index
        if (defaultSequenceIndex >= sequences.Length)
        {
            defaultSequenceIndex = sequences.Length - 1;
        }

        // Check for null sequences
        for (int i = 0; i < sequences.Length; i++)
        {
            if (sequences[i] == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Sequence at index {i} is null. Please assign an ActionDecalSequence component.", this);
            }
        }
    }
#endif
}
