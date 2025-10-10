using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays a random audio clip from a list when triggered via events.
/// Common use: Impact sounds, button click feedback, random footstep variations, or collectible pickup effects.
/// </summary>
public class ActionPlaySound : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Array of audio clips to randomly choose from")]
    [SerializeField] private AudioClip[] audioClips;
    
    [Tooltip("Volume level for played sounds")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;
    
    private AudioSource audioSource;
    
    private void Start()
    {
        // Create AudioSource component if it doesn't exist
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure the AudioSource for one-shot playback
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
    }
    
    /// <summary>
    /// Public function to play a random sound from the audio clips array
    /// Designed to be called from UnityEvents
    /// </summary>
    public void PlaySound()
    {
        // Check if we have clips and an audio source
        if (audioClips == null || audioClips.Length == 0)
        {
            Debug.LogWarning("No audio clips assigned to ActionPlaySound script!");
            return;
        }
        
        if (audioSource == null)
        {
            Debug.LogWarning("No AudioSource available on ActionPlaySound script!");
            return;
        }
        
        // Choose a random clip from the array
        int randomIndex = Random.Range(0, audioClips.Length);
        AudioClip clipToPlay = audioClips[randomIndex];
        
        // Play the clip if it's valid
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, volume);
        }
        else
        {
            Debug.LogWarning($"Audio clip at index {randomIndex} is null!");
        }
    }
    
    /// <summary>
    /// Update the volume and apply it to the AudioSource
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
    
    /// <summary>
    /// Get the number of audio clips currently loaded
    /// </summary>
    public int GetClipCount()
    {
        return audioClips != null ? audioClips.Length : 0;
    }
}