using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RuntimeAudioDirector : MonoBehaviour
{
    public const string PrefBgmVolume = "RAD_BgmVolume";
    public const string PrefSfxVolume = "RAD_SfxVolume";
    public const string PrefMuted = "RAD_Muted";
    public const string CueBgmLoop = "bgm_loop";
    public const string CueUiClick = "ui_click";
    public const string CueActionPrimary = "action_primary";
    public const string CueDangerWarning = "danger_warning";
    public const string CueTransition = "transition";
    public const string CueResultSuccess = "result_success";
    public const string CueResultFailure = "result_failure";
    private static readonly string[] ClipNames = { CueBgmLoop, CueUiClick, CueActionPrimary, CueDangerWarning, CueTransition, CueResultSuccess, CueResultFailure };
    private static RuntimeAudioDirector _instance;
    private AudioSource _bgmSource;
    private AudioSource _sfxSource;
    private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, int> _sfxPlayCounts = new Dictionary<string, int>();
    private float _bgmVolume = 0.28f;
    private float _sfxVolume = 0.70f;
    private bool _muted;
    private bool _initialSceneSkipped;
    private bool _isDucking;
    private float _duckEndTime = -1f;
    private const float DuckDurationSeconds = 0.7f;
    private const float DuckVolumeMultiplier = 0.5f;
    private const float PitchMin = 0.97f;
    private const float PitchMax = 1.03f;
    private const int PitchSteps = 7;
    private const float InputDebounceSeconds = 0.15f;
    private float _lastInputTime = -999f;
    public static float BgmVolume => _instance != null ? _instance._bgmVolume : 0.28f;
    public static float SfxVolume => _instance != null ? _instance._sfxVolume : 0.70f;
    public static bool Muted => _instance != null && _instance._muted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("RuntimeAudioDirector");
        _instance = go.AddComponent<RuntimeAudioDirector>();
        DontDestroyOnLoad(go);
        _instance.Initialize();
    }

    private void Initialize()
    {
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop = false;
        _sfxSource.playOnAwake = false;
        LoadClips();
        LoadPrefs();
        ApplyVolumes();
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayBgm();
    }

    private void OnDestroy()
    {
        if (_instance == this) { SceneManager.sceneLoaded -= OnSceneLoaded; _instance = null; }
    }

    private void LoadClips()
    {
        foreach (var name in ClipNames)
        {
            var clip = Resources.Load<AudioClip>("Audio/generated/" + name);
            if (clip != null) _clips[name] = clip;
            else Debug.LogWarning("RuntimeAudioDirector: missing clip '" + name + "' in Resources/Audio/generated");
        }
    }

    private void LoadPrefs()
    {
        _bgmVolume = ClampVolume(PlayerPrefs.GetFloat(PrefBgmVolume, 0.28f));
        _sfxVolume = ClampVolume(PlayerPrefs.GetFloat(PrefSfxVolume, 0.70f));
        _muted = PlayerPrefs.GetInt(PrefMuted, 0) != 0;
    }

    private void ApplyVolumes()
    {
        _bgmSource.volume = _muted ? 0f : (_isDucking ? _bgmVolume * DuckVolumeMultiplier : _bgmVolume);
        _sfxSource.volume = _muted ? 0f : _sfxVolume;
    }

    private void PlayBgm()
    {
        if (_clips.TryGetValue(CueBgmLoop, out var clip)) { _bgmSource.clip = clip; _bgmSource.Play(); }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_initialSceneSkipped) { _initialSceneSkipped = true; return; }
        PlayCueInternal(CueTransition);
    }

    private void Update()
    {
        if (_isDucking && Time.unscaledTime >= _duckEndTime)
        {
            _isDucking = false;
            ApplyVolumes();
        }
        bool triggered = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
        if (!triggered && Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++) if (Input.GetTouch(i).phase == TouchPhase.Began) { triggered = true; break; }
        }
        if (!triggered) return;
        float now = Time.unscaledTime;
        if (now - _lastInputTime < InputDebounceSeconds) return;
        _lastInputTime = now;
        string cue = (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) ? CueActionPrimary : CueUiClick;
        PlayCueInternal(cue);
    }

    public static void PlayCue(string requestedName)
    {
        if (_instance == null) return;
        _instance.PlayCueInternal(ResolveCueName(requestedName));
    }

    private void PlayCueInternal(string cueName)
    {
        if (string.IsNullOrEmpty(cueName)) return;
        if (cueName == CueBgmLoop) { PlayBgm(); return; }
        if (_clips.TryGetValue(cueName, out var clip) && clip != null)
        {
            _sfxSource.pitch = ComputeDeterministicPitch(cueName);
            _sfxSource.PlayOneShot(clip, _muted ? 0f : _sfxVolume);
            _sfxSource.pitch = 1f;
        }
        if (IsDuckingCue(cueName)) StartDuck();
    }

    private float ComputeDeterministicPitch(string cueName)
    {
        _sfxPlayCounts.TryGetValue(cueName, out int count);
        _sfxPlayCounts[cueName] = count + 1;
        float step = (PitchMax - PitchMin) / (PitchSteps - 1);
        return PitchMin + (count % PitchSteps) * step;
    }

    private static bool IsDuckingCue(string cueName) => cueName == CueDangerWarning || cueName == CueResultSuccess || cueName == CueResultFailure;

    private void StartDuck()
    {
        _isDucking = true;
        _duckEndTime = Time.unscaledTime + DuckDurationSeconds;
        ApplyVolumes();
    }

    public static void SetBgmVolume(float volume)
    {
        if (_instance == null) return;
        _instance._bgmVolume = ClampVolume(volume); _instance.ApplyVolumes();
        PlayerPrefs.SetFloat(PrefBgmVolume, _instance._bgmVolume); PlayerPrefs.Save();
    }

    public static void SetSfxVolume(float volume)
    {
        if (_instance == null) return;
        _instance._sfxVolume = ClampVolume(volume); _instance.ApplyVolumes();
        PlayerPrefs.SetFloat(PrefSfxVolume, _instance._sfxVolume); PlayerPrefs.Save();
    }

    public static void SetMuted(bool muted)
    {
        if (_instance == null) return;
        _instance._muted = muted; _instance.ApplyVolumes();
        PlayerPrefs.SetInt(PrefMuted, muted ? 1 : 0); PlayerPrefs.Save();
    }

    public static void PlayDangerWarning() => PlayCue(CueDangerWarning);
    public static void PlayResultSuccess() => PlayCue(CueResultSuccess);
    public static void PlayResultFailure() => PlayCue(CueResultFailure);
    public static float ClampVolume(float volume) { if (float.IsNaN(volume)) return 0f; return Mathf.Clamp01(volume); }
    public static string ResolveCueName(string requestedName)
    {
        if (string.IsNullOrEmpty(requestedName)) return string.Empty;
        string trimmed = requestedName.Trim();
        foreach (var known in ClipNames) if (string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase)) return known;
        return string.Empty;
    }
}
