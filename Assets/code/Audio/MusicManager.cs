using UnityEngine;

/// <summary>
/// Global background music manager that persists across scene loads.
/// - Singleton (MusicManager.Instance)
/// - Ensures there is exactly one AudioSource playing the BGM
/// - Does NOT restart when changing scenes
/// - Safe to place this prefab in any/all scenes (duplicates auto-destroyed)
/// </summary>
[DisallowMultipleComponent]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Settings")]
    [Tooltip("Music clip to play. Assign your .mp3/.wav here in the prefab/scene.")]
    [SerializeField] private AudioClip defaultMusic;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.6f; // base volume (target when not fading)

    [Tooltip("If true, music starts automatically when the app starts (only on the first instance).")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Loop background music.")]
    [SerializeField] private bool loop = true;

    private AudioSource _source;
    private Coroutine _fadeCo;

    private void Awake()
    {
        // Singleton guard
        if (Instance != null && Instance != this)
        {
            // Another instance already exists and is playing - destroy this duplicate
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure AudioSource
        _source = GetComponent<AudioSource>();
        if (_source == null)
        {
            _source = gameObject.AddComponent<AudioSource>();
        }

        // Configure AudioSource
        _source.playOnAwake = false; // script controls playback
        _source.loop = loop;
        _source.volume = volume;
        if (_source.clip == null)
        {
            _source.clip = defaultMusic; // allow override via AudioSource if you prefer
        }

        // Only start if asked and not already playing
        if (playOnStart && _source.clip != null && !_source.isPlaying)
        {
            _source.Play();
        }
    }

    private void OnValidate()
    {
        // Keep runtime AudioSource in sync when edited in inspector (during play mode)
        if (_source != null)
        {
            _source.volume = Mathf.Clamp01(volume);
            _source.loop = loop;
        }
    }

    // Public API
    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (_source != null) _source.volume = volume;
    }

    public float GetVolume() => volume;

    public void Play()
    {
        if (_source == null) return;
        if (_source.clip == null) return;
        if (_source.isPlaying) return;
        _source.Play();
    }

    public void Stop()
    {
        if (_source == null) return;
        _source.Stop();
    }

    public void Pause() { if (_source != null) _source.Pause(); }
    public void UnPause() { if (_source != null) _source.UnPause(); }

    /// <summary>
    /// Reset playback position to the beginning (keeps current play state by default).
    /// If resume is true and the source was not playing, it will start playing.
    /// </summary>
    public void ResetToStart(bool resume = true)
    {
        if (_source == null || _source.clip == null) return;
        _source.time = 0f; // reset position to start
        if (resume && !_source.isPlaying) _source.Play();
    }

    /// <summary>
    /// Optionally switch to another music clip at runtime without restarting on scene loads.
    /// </summary>
    public void SetMusic(AudioClip newClip, bool autoplay = true)
    {
        if (_source == null) return;
        if (newClip == null) return;
        bool wasPlaying = _source.isPlaying;
        _source.clip = newClip;
        _source.loop = loop;
        if (autoplay || wasPlaying)
        {
            _source.Play();
        }
    }

    // Fade helpers -------------------------------------------------------
    public void FadeTo(float targetVolume, float duration)
    {
        targetVolume = Mathf.Clamp01(targetVolume);
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeRoutine(targetVolume, duration));
    }

    public void FadeOut(float duration) => FadeTo(0f, duration);
    public void FadeIn(float duration) => FadeTo(volume, duration);

    private System.Collections.IEnumerator FadeRoutine(float target, float duration)
    {
        if (_source == null) yield break;
        float start = _source.volume;
        float t = 0f;
        // Use unscaled time so it works when Time.timeScale == 0
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            _source.volume = Mathf.Lerp(start, target, k);
            yield return null;
        }
        _source.volume = target;
        _fadeCo = null;
    }
}

