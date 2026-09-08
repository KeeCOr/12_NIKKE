using UnityEngine;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("SFX")]
    [SerializeField] private AudioClip sfxGunshot;
    [SerializeField] private AudioClip sfxReload;
    [SerializeField] private AudioClip sfxExplosion;
    [SerializeField] private AudioClip sfxBossAttack;
    [SerializeField] private AudioClip sfxBossEnrage;
    [SerializeField] private AudioClip sfxWin;
    [SerializeField] private AudioClip sfxLose;

    void Awake() {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() {
        GameEvents.OnFireBullet     += HandleFireBullet;
        GameEvents.OnReloadComplete += HandleReloadComplete;
        GameEvents.OnBossAttack     += HandleBossAttack;
        GameEvents.OnBossEnraged    += HandleBossEnraged;
        GameEvents.OnBossDefeated   += HandleBossDefeated;
    }

    void OnDisable() {
        GameEvents.OnFireBullet     -= HandleFireBullet;
        GameEvents.OnReloadComplete -= HandleReloadComplete;
        GameEvents.OnBossAttack     -= HandleBossAttack;
        GameEvents.OnBossEnraged    -= HandleBossEnraged;
        GameEvents.OnBossDefeated   -= HandleBossDefeated;
    }

    private void HandleFireBullet(BulletData _)  => PlaySfx(sfxGunshot, RuntimeAudioDirector.CueActionPrimary);
    private void HandleReloadComplete(string _)  => PlaySfx(sfxReload, RuntimeAudioDirector.CueTransition);
    private void HandleBossAttack()              => PlaySfx(sfxBossAttack, RuntimeAudioDirector.CueDangerWarning);
    private void HandleBossEnraged()             => PlaySfx(sfxBossEnrage, RuntimeAudioDirector.CueDangerWarning);
    private void HandleBossDefeated()            => PlaySfx(sfxWin, RuntimeAudioDirector.CueResultSuccess);

    private void PlaySfx(AudioClip clip, string fallbackCue) {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
        else RuntimeAudioDirector.PlayCue(fallbackCue);
    }

    public void PlayBgm(AudioClip clip) {
        if (bgmSource == null || clip == null) { RuntimeAudioDirector.PlayCue(RuntimeAudioDirector.CueBgmLoop); return; }
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }
}
