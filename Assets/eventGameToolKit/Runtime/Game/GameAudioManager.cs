using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using DG.Tweening;

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

    [Header("Ambient Settings")]
    [SerializeField] private AudioSource ambientSource;

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
    /// <summary>
    /// Fires when an ambient track starts playing
    /// </summary>
    public UnityEvent onAmbientStarted;
    /// <summary>
    /// Fires when the ambient track is stopped
    /// </summary>
    public UnityEvent onAmbientStopped;
    /// <summary>
    /// Fires when a gradual volume change completes
    /// </summary>
    public UnityEvent onVolumeChangeComplete;

    // DOTween references for cleanup and interruption
    private Tween musicFadeTween;
    private Tween musicVolumeTween;
    private Tween sfxVolumeTween;
    private Tween masterVolumeTween;
    private Tween ambientFadeTween;
    private Tween ambientVolumeTween;

    public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;
    public bool IsAmbientPlaying => ambientSource != null && ambientSource.isPlaying;
    public bool IsFading => (musicFadeTween != null && musicFadeTween.IsActive() && musicFadeTween.IsPlaying()) ||
                            (ambientFadeTween != null && ambientFadeTween.IsActive() && ambientFadeTween.IsPlaying());

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

        // Create ambient source if not assigned
        if (ambientSource == null)
        {
            GameObject ambientObject = new GameObject("Ambient Source");
            ambientObject.transform.SetParent(transform);
            ambientSource = ambientObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
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

            groups = audioMixer.FindMatchingGroups("Ambient");
            if (groups.Length > 0)
                ambientSource.outputAudioMixerGroup = groups[0];
        }
    }

    #region Music Control

    /// <summary>
    /// Play music track with optional fade in
    /// </summary>
    public void PlayMusic(AudioClip musicClip, bool fadeIn = true)
    {
        if (musicClip == null) return;

        // Kill any existing fade
        musicFadeTween?.Kill();

        if (fadeIn && musicSource.isPlaying)
        {
            // Crossfade from current track to new track
            CrossfadeMusic(musicClip, defaultFadeDuration);
        }
        else
        {
            // Direct play
            musicSource.clip = musicClip;

            if (fadeIn)
            {
                float targetVolume = musicSource.volume;
                musicSource.volume = 0f;
                musicSource.Play();

                musicFadeTween = FadeAudioSource(musicSource, targetVolume, defaultFadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() => onMusicStarted?.Invoke());
            }
            else
            {
                musicSource.Play();
                onMusicStarted?.Invoke();
            }
        }
    }

    /// <summary>
    /// Stop music with optional fade out
    /// </summary>
    public void StopMusic(bool fadeOut = true)
    {
        if (!musicSource.isPlaying) return;

        // Kill any existing fade
        musicFadeTween?.Kill();

        if (fadeOut)
        {
            float startVolume = musicSource.volume;
            musicFadeTween = FadeAudioSource(musicSource, 0f, defaultFadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    musicSource.Stop();
                    musicSource.volume = startVolume; // Restore volume for next play
                    onMusicStopped?.Invoke();
                });
        }
        else
        {
            musicSource.Stop();
            onMusicStopped?.Invoke();
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

    #region Ambient Control

    /// <summary>
    /// Play ambient track with optional fade in. Crossfades if ambient is already playing.
    /// </summary>
    public void PlayAmbient(AudioClip ambientClip, bool fadeIn = true)
    {
        if (ambientClip == null) return;

        // Kill any existing fade
        ambientFadeTween?.Kill();

        if (fadeIn && ambientSource.isPlaying)
        {
            // Crossfade from current ambient to new ambient
            CrossfadeAmbient(ambientClip, defaultFadeDuration);
        }
        else
        {
            // Direct play
            ambientSource.clip = ambientClip;

            if (fadeIn)
            {
                float targetVolume = ambientSource.volume;
                ambientSource.volume = 0f;
                ambientSource.Play();

                ambientFadeTween = FadeAudioSource(ambientSource, targetVolume, defaultFadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() => onAmbientStarted?.Invoke());
            }
            else
            {
                ambientSource.Play();
                onAmbientStarted?.Invoke();
            }
        }
    }

    /// <summary>
    /// Stop ambient with optional fade out
    /// </summary>
    public void StopAmbient(bool fadeOut = true)
    {
        if (!ambientSource.isPlaying) return;

        // Kill any existing fade
        ambientFadeTween?.Kill();

        if (fadeOut)
        {
            float startVolume = ambientSource.volume;
            ambientFadeTween = FadeAudioSource(ambientSource, 0f, defaultFadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    ambientSource.Stop();
                    ambientSource.volume = startVolume; // Restore volume for next play
                    onAmbientStopped?.Invoke();
                });
        }
        else
        {
            ambientSource.Stop();
            onAmbientStopped?.Invoke();
        }
    }

    /// <summary>
    /// Pause ambient audio
    /// </summary>
    public void PauseAmbient()
    {
        if (ambientSource.isPlaying)
            ambientSource.Pause();
    }

    /// <summary>
    /// Resume ambient audio
    /// </summary>
    public void ResumeAmbient()
    {
        if (!ambientSource.isPlaying && ambientSource.clip != null)
            ambientSource.UnPause();
    }

    private void CrossfadeAmbient(AudioClip newClip, float duration)
    {
        AudioSource tempSource = CreateTemporaryAmbientSource();
        tempSource.clip = newClip;
        tempSource.volume = 0f;
        tempSource.Play();

        float originalVolume = ambientSource.volume;

        // Create a sequence for the crossfade
        ambientFadeTween = DOTween.Sequence()
            .Append(FadeAudioSource(ambientSource, 0f, duration))
            .Join(FadeAudioSource(tempSource, originalVolume, duration))
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // Complete the crossfade - swap to main source
                ambientSource.Stop();
                ambientSource.clip = newClip;
                ambientSource.volume = originalVolume;
                ambientSource.time = tempSource.time;
                ambientSource.Play();

                Destroy(tempSource.gameObject);
                onAmbientStarted?.Invoke();
            });
    }

    private AudioSource CreateTemporaryAmbientSource()
    {
        GameObject tempObject = new GameObject("Temp Ambient Source");
        tempObject.transform.SetParent(transform);
        AudioSource tempSource = tempObject.AddComponent<AudioSource>();

        // Copy settings from ambient source
        tempSource.outputAudioMixerGroup = ambientSource.outputAudioMixerGroup;
        tempSource.loop = ambientSource.loop;
        tempSource.pitch = ambientSource.pitch;

        return tempSource;

        
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

    #region Gradual Volume Control

    /// <summary>
    /// Gradually change music volume over time (0-1 range). Uses default fade duration.
    /// </summary>
    public void SetMusicVolumeGradual(float targetVolume)
    {
        SetMusicVolumeGradual(targetVolume, defaultFadeDuration);
    }

    /// <summary>
    /// Gradually change music volume over time (0-1 range) with custom duration.
    /// </summary>
    public void SetMusicVolumeGradual(float targetVolume, float duration)
    {
        if (musicSource == null) return;

        musicVolumeTween?.Kill();
        musicVolumeTween = FadeAudioSource(musicSource, Mathf.Clamp01(targetVolume), duration)
            .SetUpdate(true)
            .OnComplete(() => onVolumeChangeComplete?.Invoke());
    }

    /// <summary>
    /// Gradually change SFX volume over time (0-1 range). Uses default fade duration.
    /// </summary>
    public void SetSFXVolumeGradual(float targetVolume)
    {
        SetSFXVolumeGradual(targetVolume, defaultFadeDuration);
    }

    /// <summary>
    /// Gradually change SFX volume over time (0-1 range) with custom duration.
    /// </summary>
    public void SetSFXVolumeGradual(float targetVolume, float duration)
    {
        if (sfxSource == null) return;

        sfxVolumeTween?.Kill();
        sfxVolumeTween = FadeAudioSource(sfxSource, Mathf.Clamp01(targetVolume), duration)
            .SetUpdate(true)
            .OnComplete(() => onVolumeChangeComplete?.Invoke());
    }

    /// <summary>
    /// Gradually change ambient volume over time (0-1 range). Uses default fade duration.
    /// </summary>
    public void SetAmbientVolumeGradual(float targetVolume)
    {
        SetAmbientVolumeGradual(targetVolume, defaultFadeDuration);
    }

    /// <summary>
    /// Gradually change ambient volume over time (0-1 range) with custom duration.
    /// </summary>
    public void SetAmbientVolumeGradual(float targetVolume, float duration)
    {
        if (ambientSource == null) return;

        ambientVolumeTween?.Kill();
        ambientVolumeTween = FadeAudioSource(ambientSource, Mathf.Clamp01(targetVolume), duration)
            .SetUpdate(true)
            .OnComplete(() => onVolumeChangeComplete?.Invoke());
    }

    /// <summary>
    /// Gradually change master volume over time (0-1 range). Uses default fade duration.
    /// Requires AudioMixer to be assigned.
    /// </summary>
    public void SetMasterVolumeGradual(float targetVolume)
    {
        SetMasterVolumeGradual(targetVolume, defaultFadeDuration);
    }

    /// <summary>
    /// Gradually change master volume over time (0-1 range) with custom duration.
    /// Requires AudioMixer to be assigned.
    /// </summary>
    public void SetMasterVolumeGradual(float targetVolume, float duration)
    {
        if (audioMixer == null) return;

        masterVolumeTween?.Kill();

        // Get current mixer volume
        float currentDb;
        if (!audioMixer.GetFloat(masterVolumeParameter, out currentDb))
        {
            currentDb = 0f;
        }
        float currentVolume = Mathf.Pow(10f, currentDb / 20f);

        float clampedTarget = Mathf.Clamp01(targetVolume);

        masterVolumeTween = DOTween.To(
            () => currentVolume,
            x =>
            {
                float dbValue = x > 0 ? 20f * Mathf.Log10(x) : -80f;
                audioMixer.SetFloat(masterVolumeParameter, dbValue);
            },
            clampedTarget,
            duration
        ).SetUpdate(true)
         .OnComplete(() => onVolumeChangeComplete?.Invoke());
    }

    #endregion

    #region Music Crossfading

    private void CrossfadeMusic(AudioClip newClip, float duration)
    {
        AudioSource tempSource = CreateTemporaryAudioSource();
        tempSource.clip = newClip;
        tempSource.volume = 0f;
        tempSource.Play();

        float originalVolume = musicSource.volume;

        // Create a sequence for the crossfade
        musicFadeTween = DOTween.Sequence()
            .Append(FadeAudioSource(musicSource, 0f, duration))
            .Join(FadeAudioSource(tempSource, originalVolume, duration))
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // Complete the crossfade - swap to main source
                musicSource.Stop();
                musicSource.clip = newClip;
                musicSource.volume = originalVolume;
                musicSource.time = tempSource.time;
                musicSource.Play();

                Destroy(tempSource.gameObject);
                onMusicStarted?.Invoke();
            });
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

    #region Cleanup

    private void OnDestroy()
    {
        // Clean up all DOTween tweens
        musicFadeTween?.Kill();
        musicVolumeTween?.Kill();
        sfxVolumeTween?.Kill();
        masterVolumeTween?.Kill();
        ambientFadeTween?.Kill();
        ambientVolumeTween?.Kill();
    }

    #endregion

    #region DOTween Helpers

    /// <summary>
    /// Fade AudioSource volume (DOTween FREE compatible - no Pro required)
    /// </summary>
    private Tween FadeAudioSource(AudioSource source, float targetVolume, float duration)
    {
        return DOTween.To(() => source.volume, x => source.volume = x, targetVolume, duration);
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
    /// Simple method for students - play ambient by name
    /// </summary>
    public void PlayAmbientByName(string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip != null)
        {
            PlayAmbient(clip);
        }
        else
        {
            Debug.LogWarning($"Could not find audio clip at path: {resourcePath}");
        }
    }

    /// <summary>
    /// Stop all audio
    /// </summary>
    public void StopAllAudio()
    {
        StopMusic(false);
        StopAmbient(false);
        if (sfxSource != null && sfxSource.isPlaying)
            sfxSource.Stop();
    }

    #endregion
}