using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

/// <summary>
/// Plays a randomly selected audio clip from a list using PlayOneShot.
/// Supports randomized pitch and volume ranges for natural variation.
/// Common use: impact sounds, footstep variation, collectible pickups, button feedback.
/// </summary>
public class ActionPlaySound : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Array of audio clips to randomly choose from. One clip is picked at random each time Play() is called.")]
    [SerializeField] private AudioClip[] audioClips;

    [Header("Volume")]
    [Tooltip("Minimum volume for each play (0 = silent, 1 = full). A random value between Min and Max is chosen each time.")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeMin = 0.8f;

    [Tooltip("Maximum volume for each play (0 = silent, 1 = full). Set Min and Max to the same value for a fixed volume.")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeMax = 1f;

    [Header("Mixer")]
    [Tooltip("Optional Audio Mixer Group to route this sound through (e.g. SFX, Music, Ambience). Leave empty to use the default output.")]
    [SerializeField] private AudioMixerGroup outputMixerGroup;

    [Header("Pitch")]
    [Tooltip("Minimum pitch multiplier (1 = normal, < 1 = lower, > 1 = higher). A random value between Min and Max is chosen each time.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float pitchMin = 1f;

    [Tooltip("Maximum pitch multiplier. Set Min and Max to 1 for no pitch variation. Try 0.9–1.1 for subtle variation.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float pitchMax = 1f;

    [Header("Events")]
    /// <summary>
    /// Fires each time a clip is successfully played.
    /// </summary>
    public UnityEvent onPlay;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        // Volume is passed directly to PlayOneShot — keep source at 1
        // so the PlayOneShot volume parameter represents the true 0–1 range.
        audioSource.volume = 1f;
        audioSource.outputAudioMixerGroup = outputMixerGroup;
    }

    private void OnValidate()
    {
        volumeMin = Mathf.Min(volumeMin, volumeMax);
        pitchMin  = Mathf.Min(pitchMin,  pitchMax);
    }

    /// <summary>
    /// Plays a randomly selected clip from the array with randomized pitch and volume.
    /// Wire this to any UnityEvent source such as InputKeyPress or InputTriggerZone.
    /// </summary>
    public void Play()
    {
        if (audioClips == null || audioClips.Length == 0)
        {
            Debug.LogWarning($"[ActionPlaySound] '{name}': No audio clips assigned.", this);
            return;
        }

        AudioClip clip = audioClips[Random.Range(0, audioClips.Length)];

        if (clip == null)
        {
            Debug.LogWarning($"[ActionPlaySound] '{name}': Selected clip is null — check for empty slots in the array.", this);
            return;
        }

        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.PlayOneShot(clip, Random.Range(volumeMin, volumeMax));
        onPlay?.Invoke();
    }

    /// <summary>
    /// Sets a fixed volume with no variation (both min and max are assigned the same value).
    /// </summary>
    public void SetVolume(float value)
    {
        volumeMin = volumeMax = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Sets a fixed pitch with no variation (both min and max are assigned the same value).
    /// </summary>
    public void SetPitch(float value)
    {
        pitchMin = pitchMax = Mathf.Clamp(value, 0.1f, 3f);
    }
}
