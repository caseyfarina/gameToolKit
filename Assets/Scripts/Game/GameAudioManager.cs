using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Centralized audio control with mixer integration, music crossfading, and sound effect management.
/// Common use: Background music systems, ambient sound control, menu audio, or cinematic audio transitions.
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    [SerializeField] private string sfxVolumeParameter = "SFXVolume";

    [Header("Music Settings")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float defaultFadeDuration = 2f;

    [Header("Sound Effects")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip[] soundEffects;

    [Header("Events")]
    /// <summary>
    /// Fires when a music track starts playing
    /// </summary>
    public UnityEvent onMusicStarted;
    /// <summary>
    /// Fires when the music is stopped
    /// </summary>
    public UnityEvent onMusicStopped;
    /// <summary>
    /// Fires when a sound effect is played
    /// </summary>
    public UnityEvent onSoundEffectPlayed;

    private Coroutine currentFadeCoroutine;

    public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;
    public bool IsFading => currentFadeCoroutine != null;

    private void Start()
    {
        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        // Create music source if not assigned
        if (musicSource == null)
        {
            GameObject musicObject = new GameObject("Music Source");
            musicObject.transform.SetParent(transform);
            musicSource = musicObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        // Create SFX source if not assigned
        if (sfxSource == null)
        {
            GameObject sfxObject = new GameObject("SFX Source");
            sfxObject.transform.SetParent(transform);
            sfxSource = sfxObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // Assign to mixer groups if mixer is available
        if (audioMixer != null)
        {
            AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("Music");
            if (groups.Length > 0)
                musicSource.outputAudioMixerGroup = groups[0];

            groups = audioMixer.FindMatchingGroups("SFX");
            if (groups.Length > 0)
                sfxSource.outputAudioMixerGroup = groups[0];
        }
    }

    #region Music Control

    /// <summary>
    /// Play music track with optional fade in
    /// </summary>
    public void PlayMusic(AudioClip musicClip, bool fadeIn = true)
    {
        if (musicClip == null) return;

        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        if (fadeIn && musicSource.isPlaying)
        {
            // Crossfade from current track to new track
            currentFadeCoroutine = StartCoroutine(CrossfadeMusic(musicClip, defaultFadeDuration));
        }
        else
        {
            // Direct play
            musicSource.clip = musicClip;
            musicSource.Play();

            if (fadeIn)
            {
                currentFadeCoroutine = StartCoroutine(FadeIn(musicSource, defaultFadeDuration));
            }

            onMusicStarted.Invoke();
        }
    }

    /// <summary>
    /// Stop music with optional fade out
    /// </summary>
    public void StopMusic(bool fadeOut = true)
    {
        if (!musicSource.isPlaying) return;

        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        if (fadeOut)
        {
            currentFadeCoroutine = StartCoroutine(FadeOutAndStop(musicSource, defaultFadeDuration));
        }
        else
        {
            musicSource.Stop();
            onMusicStopped.Invoke();
        }
    }

    /// <summary>
    /// Pause/Resume music
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
            musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (!musicSource.isPlaying && musicSource.clip != null)
            musicSource.UnPause();
    }

    #endregion

    #region Sound Effects

    /// <summary>
    /// Play sound effect by index
    /// </summary>
    public void PlaySoundEffect(int index)
    {
        if (soundEffects != null && index >= 0 && index < soundEffects.Length)
        {
            PlaySoundEffect(soundEffects[index]);
        }
    }

    /// <summary>
    /// Play sound effect by AudioClip
    /// </summary>
    public void PlaySoundEffect(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
            onSoundEffectPlayed.Invoke();
        }
    }

    /// <summary>
    /// Play sound effect with volume control
    /// </summary>
    public void PlaySoundEffect(AudioClip clip, float volume)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
            onSoundEffectPlayed.Invoke();
        }
    }

    #endregion

    #region Volume Control

    /// <summary>
    /// Set master volume (0-1)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        SetMixerVolume(masterVolumeParameter, volume);
    }

    /// <summary>
    /// Set music volume (0-1)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        SetMixerVolume(musicVolumeParameter, volume);
    }

    /// <summary>
    /// Set SFX volume (0-1)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        SetMixerVolume(sfxVolumeParameter, volume);
    }

    private void SetMixerVolume(string parameterName, float volume)
    {
        if (audioMixer != null)
        {
            // Convert 0-1 range to decibel range (-80 to 0)
            float dbValue = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
            audioMixer.SetFloat(parameterName, dbValue);
        }
    }

    #endregion

    #region Fading Coroutines

    private IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
    {
        AudioSource tempSource = CreateTemporaryAudioSource();
        tempSource.clip = newClip;
        tempSource.volume = 0f;
        tempSource.Play();

        float elapsed = 0f;
        float originalVolume = musicSource.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            musicSource.volume = originalVolume * (1f - progress);
            tempSource.volume = originalVolume * progress;

            yield return null;
        }

        // Complete the crossfade
        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.volume = originalVolume;
        musicSource.time = tempSource.time;
        musicSource.Play();

        Destroy(tempSource.gameObject);
        currentFadeCoroutine = null;
        onMusicStarted.Invoke();
    }

    private IEnumerator FadeIn(AudioSource source, float duration)
    {
        float targetVolume = source.volume;
        source.volume = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = targetVolume * (elapsed / duration);
            yield return null;
        }

        source.volume = targetVolume;
        currentFadeCoroutine = null;
    }

    private IEnumerator FadeOutAndStop(AudioSource source, float duration)
    {
        float startVolume = source.volume;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = startVolume * (1f - (elapsed / duration));
            yield return null;
        }

        source.volume = startVolume;
        source.Stop();
        currentFadeCoroutine = null;
        onMusicStopped.Invoke();
    }

    private AudioSource CreateTemporaryAudioSource()
    {
        GameObject tempObject = new GameObject("Temp Audio Source");
        tempObject.transform.SetParent(transform);
        AudioSource tempSource = tempObject.AddComponent<AudioSource>();

        // Copy settings from music source
        tempSource.outputAudioMixerGroup = musicSource.outputAudioMixerGroup;
        tempSource.loop = musicSource.loop;
        tempSource.pitch = musicSource.pitch;

        return tempSource;
    }

    #endregion

    #region Student Helper Methods

    /// <summary>
    /// Simple method for students - play music by name
    /// </summary>
    public void PlayMusicByName(string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip != null)
        {
            PlayMusic(clip);
        }
        else
        {
            Debug.LogWarning($"Could not find audio clip at path: {resourcePath}");
        }
    }

    /// <summary>
    /// Simple method for students - play SFX by name
    /// </summary>
    public void PlaySFXByName(string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        PlaySoundEffect(clip);
    }

    /// <summary>
    /// Stop all audio
    /// </summary>
    public void StopAllAudio()
    {
        StopMusic(false);
        if (sfxSource != null && sfxSource.isPlaying)
            sfxSource.Stop();
    }

    #endregion
}