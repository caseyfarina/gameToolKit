using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// Centralized music and ambient track control with crossfading and global volume management.
/// One-shot sound effects are handled by ActionPlaySound — this manager only owns long-running
/// looping audio (music + ambient) and the shared SFX volume scalar that ActionPlaySound can opt into.
/// Common use: Background music systems, ambient sound control, menu audio, or cinematic audio transitions.
/// </summary>
[HelpURL("https://caseyfarina.github.io/egtk-docs/audio.html")]
public class GameAudioManager : MonoBehaviour
{
    [Header("Starting Music")]
    [Tooltip("Clip to play automatically when the scene starts. Leave empty to start silent.")]
    [SerializeField] private AudioClip startingMusicClip;
    [Tooltip("Play the Starting Music Clip automatically on scene start.")]
    [SerializeField] private bool playOnStart = false;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;
    [Tooltip("Shared SFX volume scalar. ActionPlaySound components with 'Route Through Audio Manager' enabled multiply their playback volume by this value.")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float ambientVolume = 1f;

    [Header("Music Settings")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float defaultFadeDuration = 2f;

    [Header("Ambient Settings")]
    [SerializeField] private AudioSource ambientSource;

    [Header("Events")]
    /// <summary>Fires when a music track starts playing</summary>
    public UnityEvent onMusicStarted;
    /// <summary>Fires when the music is stopped</summary>
    public UnityEvent onMusicStopped;
    /// <summary>Fires when an ambient track starts playing</summary>
    public UnityEvent onAmbientStarted;
    /// <summary>Fires when the ambient track is stopped</summary>
    public UnityEvent onAmbientStopped;
    /// <summary>Fires when a gradual volume change completes</summary>
    public UnityEvent onVolumeChangeComplete;

    // DOTween references for cleanup and interruption
    private Tween musicFadeTween;
    private Tween musicVolumeTween;
    private Tween sfxVolumeTween;
    private Tween masterVolumeTween;
    private Tween ambientFadeTween;
    private Tween ambientVolumeTween;

    // Temp sources created during crossfades
    private AudioSource tempMusicSource;
    private AudioSource tempAmbientSource;

    // Intended target volumes — used so interrupted fades don't corrupt the target
    private float _intendedMusicVolume;
    private float _intendedAmbientVolume;

    // Pause state — Unity's isPlaying returns false when paused, so we track it ourselves
    private bool _musicIsPaused = false;
    private bool _ambientIsPaused = false;

    public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;
    public bool IsAmbientPlaying => ambientSource != null && ambientSource.isPlaying;
    public bool IsFading => (musicFadeTween != null && musicFadeTween.IsActive() && musicFadeTween.IsPlaying()) ||
                            (ambientFadeTween != null && ambientFadeTween.IsActive() && ambientFadeTween.IsPlaying());

    /// <summary>
    /// Shared SFX volume scalar (0–1) that ActionPlaySound components can opt into via 'Route Through Audio Manager'.
    /// </summary>
    public float SFXVolume => sfxVolume;

    private void Start()
    {
        _intendedMusicVolume = musicVolume;
        _intendedAmbientVolume = ambientVolume;

        SetupAudioSources();

        AudioListener.volume = masterVolume;

        if (playOnStart && startingMusicClip != null)
            PlayMusic(startingMusicClip, true);
    }

    private void SetupAudioSources()
    {
        if (musicSource == null)
        {
            GameObject obj = new GameObject("Music Source");
            obj.transform.SetParent(transform);
            musicSource = obj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        musicSource.volume = musicVolume;

        if (ambientSource == null)
        {
            GameObject obj = new GameObject("Ambient Source");
            obj.transform.SetParent(transform);
            ambientSource = obj.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
        }
        ambientSource.volume = ambientVolume;
    }

    #region Music Control

    /// <summary>
    /// Play a music track with fade in. Use this overload in UnityEvents.
    /// </summary>
    public void PlayMusic(AudioClip musicClip) => PlayMusic(musicClip, true);

    /// <summary>
    /// Play music track with optional fade in
    /// </summary>
    public void PlayMusic(AudioClip musicClip, bool fadeIn)
    {
        if (musicClip == null) return;

        _musicIsPaused = false;
        musicFadeTween?.Kill();

        if (fadeIn && musicSource.isPlaying)
        {
            CrossfadeMusic(musicClip, defaultFadeDuration);
        }
        else
        {
            musicSource.clip = musicClip;

            if (fadeIn)
            {
                musicSource.volume = 0f;
                musicSource.Play();

                musicFadeTween = FadeAudioSource(musicSource, _intendedMusicVolume, defaultFadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() => onMusicStarted?.Invoke());
            }
            else
            {
                musicSource.volume = _intendedMusicVolume;
                musicSource.Play();
                onMusicStarted?.Invoke();
            }
        }
    }

    /// <summary>
    /// Stop music with a fade out. Use this overload in UnityEvents.
    /// </summary>
    public void StopMusic() => StopMusic(true);

    /// <summary>
    /// Stop music with optional fade out
    /// </summary>
    public void StopMusic(bool fadeOut)
    {
        if (!musicSource.isPlaying && !_musicIsPaused) return;

        if (_musicIsPaused)
        {
            _musicIsPaused = false;
            musicSource.Stop();
            onMusicStopped?.Invoke();
            return;
        }

        musicFadeTween?.Kill();

        if (fadeOut)
        {
            musicFadeTween = FadeAudioSource(musicSource, 0f, defaultFadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    musicSource.Stop();
                    musicSource.volume = _intendedMusicVolume;
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
    /// Pause music playback
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
            _musicIsPaused = true;
        }
    }

    /// <summary>
    /// Resume paused music
    /// </summary>
    public void ResumeMusic()
    {
        if (_musicIsPaused && musicSource.clip != null)
        {
            musicSource.UnPause();
            _musicIsPaused = false;
        }
    }

    #endregion

    #region Ambient Control

    /// <summary>
    /// Play an ambient track with fade in. Use this overload in UnityEvents.
    /// </summary>
    public void PlayAmbient(AudioClip ambientClip) => PlayAmbient(ambientClip, true);

    /// <summary>
    /// Play ambient track with optional fade in. Crossfades if ambient is already playing.
    /// </summary>
    public void PlayAmbient(AudioClip ambientClip, bool fadeIn)
    {
        if (ambientClip == null) return;

        _ambientIsPaused = false;
        ambientFadeTween?.Kill();

        if (fadeIn && ambientSource.isPlaying)
        {
            CrossfadeAmbient(ambientClip, defaultFadeDuration);
        }
        else
        {
            ambientSource.clip = ambientClip;

            if (fadeIn)
            {
                ambientSource.volume = 0f;
                ambientSource.Play();

                ambientFadeTween = FadeAudioSource(ambientSource, _intendedAmbientVolume, defaultFadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() => onAmbientStarted?.Invoke());
            }
            else
            {
                ambientSource.volume = _intendedAmbientVolume;
                ambientSource.Play();
                onAmbientStarted?.Invoke();
            }
        }
    }

    /// <summary>
    /// Stop ambient with a fade out. Use this overload in UnityEvents.
    /// </summary>
    public void StopAmbient() => StopAmbient(true);

    /// <summary>
    /// Stop ambient with optional fade out
    /// </summary>
    public void StopAmbient(bool fadeOut)
    {
        if (!ambientSource.isPlaying && !_ambientIsPaused) return;

        if (_ambientIsPaused)
        {
            _ambientIsPaused = false;
            ambientSource.Stop();
            onAmbientStopped?.Invoke();
            return;
        }

        ambientFadeTween?.Kill();

        if (fadeOut)
        {
            ambientFadeTween = FadeAudioSource(ambientSource, 0f, defaultFadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    ambientSource.Stop();
                    ambientSource.volume = _intendedAmbientVolume;
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
        {
            ambientSource.Pause();
            _ambientIsPaused = true;
        }
    }

    /// <summary>
    /// Resume ambient audio
    /// </summary>
    public void ResumeAmbient()
    {
        if (_ambientIsPaused && ambientSource.clip != null)
        {
            ambientSource.UnPause();
            _ambientIsPaused = false;
        }
    }

    private void CrossfadeAmbient(AudioClip newClip, float duration)
    {
        if (tempAmbientSource != null)
        {
            Destroy(tempAmbientSource.gameObject);
            tempAmbientSource = null;
        }

        tempAmbientSource = CreateTemporaryAmbientSource();
        tempAmbientSource.clip = newClip;
        tempAmbientSource.volume = 0f;
        tempAmbientSource.Play();

        float targetVolume = _intendedAmbientVolume;

        ambientFadeTween = DOTween.Sequence()
            .Append(FadeAudioSource(ambientSource, 0f, duration))
            .Join(FadeAudioSource(tempAmbientSource, targetVolume, duration))
            .SetUpdate(true)
            .OnComplete(() =>
            {
                ambientSource.Stop();
                ambientSource.clip = newClip;
                ambientSource.volume = targetVolume;
                ambientSource.time = tempAmbientSource.time;
                ambientSource.Play();

                Destroy(tempAmbientSource.gameObject);
                tempAmbientSource = null;
                onAmbientStarted?.Invoke();
            });
    }

    private AudioSource CreateTemporaryAmbientSource()
    {
        GameObject tempObject = new GameObject("Temp Ambient Source");
        tempObject.transform.SetParent(transform);
        AudioSource tempSource = tempObject.AddComponent<AudioSource>();
        tempSource.loop = ambientSource.loop;
        tempSource.pitch = ambientSource.pitch;
        return tempSource;
    }

    #endregion

    #region Volume Control

    /// <summary>
    /// Set master volume instantly (0–1). Affects all audio in the scene via AudioListener.volume.
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Set music volume instantly (0–1)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        _intendedMusicVolume = Mathf.Clamp01(volume);
        if (musicSource != null) musicSource.volume = _intendedMusicVolume;
    }

    /// <summary>
    /// Set the shared SFX volume scalar (0–1). Affects ActionPlaySound components with 'Route Through Audio Manager' enabled.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Set ambient volume instantly (0–1)
    /// </summary>
    public void SetAmbientVolume(float volume)
    {
        _intendedAmbientVolume = Mathf.Clamp01(volume);
        if (ambientSource != null) ambientSource.volume = _intendedAmbientVolume;
    }

    #endregion

    #region Gradual Volume Control

    /// <summary>
    /// Gradually fade master volume to target (0–1). Uses default fade duration.
    /// </summary>
    public void SetMasterVolumeGradual(float targetVolume)
    {
        SetMasterVolumeGradual(targetVolume, defaultFadeDuration);
    }

    /// <summary>
    /// Gradually fade master volume to target (0–1) over a custom duration.
    /// </summary>
    public void SetMasterVolumeGradual(float targetVolume, float duration)
    {
        masterVolumeTween?.Kill();
        masterVolumeTween = DOTween.To(
            () => AudioListener.volume,
            x => AudioListener.volume = x,
            Mathf.Clamp01(targetVolume),
            duration
        ).SetUpdate(true)
         .OnComplete(() => onVolumeChangeComplete?.Invoke());
    }

    /// <summary>
    /// Gradually fade music volume to target (0–1). Uses default fade duration.
    /// </summary>
    public void SetMusicVolumeGradual(float targetVolume)
    {
        SetMusicVolumeGradual(targetVolume, defaultFadeDuration);
    }

    /// <summary>
    /// Gradually fade music volume to target (0–1) over a custom duration.
    /// </summary>
    public void SetMusicVolumeGradual(float targetVolume, float duration)
    {
        if (musicSource == null) return;

        _intendedMusicVolume = Mathf.Clamp01(targetVolume);
        musicVolumeTween?.Kill();
        musicVolumeTween = FadeAudioSource(musicSource, _intendedMusicVolume, duration)
            .SetUpdate(true)
            .OnComplete(() => onVolumeChangeComplete?.Invoke());
    }

    /// <summary>
    /// Gradually fade the shared SFX volume scalar to target (0–1). Uses default fade duration.
    /// </summary>
    public void SetSFXVolumeGradual(float targetVolume)
    {
        SetSFXVolumeGradual(targetVolume, defaultFadeDuration);
    }

    /// <summary>
    /// Gradually fade the shared SFX volume scalar to target (0–1) over a custom duration.
    /// </summary>
    public void SetSFXVolumeGradual(float targetVolume, float duration)
    {
        sfxVolumeTween?.Kill();
        sfxVolumeTween = DOTween.To(
            () => sfxVolume,
            x => sfxVolume = x,
            Mathf.Clamp01(targetVolume),
            duration
        ).SetUpdate(true)
         .OnComplete(() => onVolumeChangeComplete?.Invoke());
    }

    /// <summary>
    /// Gradually fade ambient volume to target (0–1). Uses default fade duration.
    /// </summary>
    public void SetAmbientVolumeGradual(float targetVolume)
    {
        SetAmbientVolumeGradual(targetVolume, defaultFadeDuration);
    }

    /// <summary>
    /// Gradually fade ambient volume to target (0–1) over a custom duration.
    /// </summary>
    public void SetAmbientVolumeGradual(float targetVolume, float duration)
    {
        if (ambientSource == null) return;

        _intendedAmbientVolume = Mathf.Clamp01(targetVolume);
        ambientVolumeTween?.Kill();
        ambientVolumeTween = FadeAudioSource(ambientSource, _intendedAmbientVolume, duration)
            .SetUpdate(true)
            .OnComplete(() => onVolumeChangeComplete?.Invoke());
    }

    #endregion

    #region Music Crossfading

    private void CrossfadeMusic(AudioClip newClip, float duration)
    {
        if (tempMusicSource != null)
        {
            Destroy(tempMusicSource.gameObject);
            tempMusicSource = null;
        }

        tempMusicSource = CreateTemporaryAudioSource();
        tempMusicSource.clip = newClip;
        tempMusicSource.volume = 0f;
        tempMusicSource.Play();

        float targetVolume = _intendedMusicVolume;

        musicFadeTween = DOTween.Sequence()
            .Append(FadeAudioSource(musicSource, 0f, duration))
            .Join(FadeAudioSource(tempMusicSource, targetVolume, duration))
            .SetUpdate(true)
            .OnComplete(() =>
            {
                musicSource.Stop();
                musicSource.clip = newClip;
                musicSource.volume = targetVolume;
                musicSource.time = tempMusicSource.time;
                musicSource.Play();

                Destroy(tempMusicSource.gameObject);
                tempMusicSource = null;
                onMusicStarted?.Invoke();
            });
    }

    private AudioSource CreateTemporaryAudioSource()
    {
        GameObject tempObject = new GameObject("Temp Audio Source");
        tempObject.transform.SetParent(transform);
        AudioSource tempSource = tempObject.AddComponent<AudioSource>();
        tempSource.loop = musicSource.loop;
        tempSource.pitch = musicSource.pitch;
        return tempSource;
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        musicFadeTween?.Kill();
        musicVolumeTween?.Kill();
        sfxVolumeTween?.Kill();
        masterVolumeTween?.Kill();
        ambientFadeTween?.Kill();
        ambientVolumeTween?.Kill();

        if (tempMusicSource != null)
            Destroy(tempMusicSource.gameObject);
        if (tempAmbientSource != null)
            Destroy(tempAmbientSource.gameObject);
    }

    #endregion

    #region Helpers

    private Tween FadeAudioSource(AudioSource source, float targetVolume, float duration)
    {
        return DOTween.To(() => source.volume, x => source.volume = x, targetVolume, duration);
    }

    /// <summary>
    /// Stop music and ambient immediately with no fade
    /// </summary>
    public void StopAllAudio()
    {
        StopMusic(false);
        StopAmbient(false);
    }

    #endregion
}
