using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Win, Lose }
    public GameState State { get; private set; } = GameState.Playing;
    public WaveResultSummary LastWaveResultSummary { get; private set; }

    [SerializeField] private float resultDelay = 1.2f;
    [SerializeField] private string resultSceneName = "Result";

    private int _deadMemberCount;
    private int _totalMemberCount;
    private int _reloadingCount;
    private float _bossHpRatio = 1f;
    private float _wallHpRatio = 1f;
    private bool _endingGame;

    void Awake() {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() {
        State            = GameState.Playing;
        _deadMemberCount = 0;
        _reloadingCount  = 0;
        _bossHpRatio     = 1f;
        _wallHpRatio     = 1f;
        _endingGame      = false;
        LastWaveResultSummary = CombatAdvisorLogic.GetWaveResultSummary(false, _bossHpRatio, _wallHpRatio, _deadMemberCount, _reloadingCount);
        RegisterSquadSize(FindObjectsOfType<SquadMemberController>().Length);
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    void OnEnable() {
        GameEvents.OnBossDefeated += HandleBossDefeated;
        GameEvents.OnMemberDied   += HandleMemberDied;
        GameEvents.OnBossHpChanged += HandleBossHpChanged;
        GameEvents.OnWallHpChanged += HandleWallHpChanged;
        GameEvents.OnReloadStarted += HandleReloadStarted;
        GameEvents.OnReloadComplete += HandleReloadComplete;
    }

    void OnDisable() {
        GameEvents.OnBossDefeated -= HandleBossDefeated;
        GameEvents.OnMemberDied   -= HandleMemberDied;
        GameEvents.OnBossHpChanged -= HandleBossHpChanged;
        GameEvents.OnWallHpChanged -= HandleWallHpChanged;
        GameEvents.OnReloadStarted -= HandleReloadStarted;
        GameEvents.OnReloadComplete -= HandleReloadComplete;
    }

    public void RegisterSquadSize(int count) {
        _totalMemberCount = count;
        _deadMemberCount  = 0;
    }

    public void TriggerDefenseBroken() {
        if (_endingGame) return;
        StartCoroutine(EndGame(GameState.Lose, resultDelay));
    }

    private void HandleBossDefeated() {
        if (State != GameState.Playing || _endingGame) return;
        StartCoroutine(EndGame(GameState.Win, 0.8f));
    }

    private void HandleMemberDied(string _) {
        _deadMemberCount++;
        if (_totalMemberCount > 0 && _deadMemberCount >= _totalMemberCount)
            NotifyAllMembersDead();
    }

    private void HandleBossHpChanged(float hp, float maxHp) {
        _bossHpRatio = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
    }

    private void HandleWallHpChanged(float hp, float maxHp) {
        _wallHpRatio = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
    }

    private void HandleReloadStarted(string memberId) {
        _reloadingCount++;
    }

    private void HandleReloadComplete(string memberId) {
        _reloadingCount = Mathf.Max(0, _reloadingCount - 1);
    }

    public void NotifyAllMembersDead() {
        if (State != GameState.Playing || _endingGame) return;
        StartCoroutine(EndGame(GameState.Lose, resultDelay));
    }

    private System.Collections.IEnumerator EndGame(GameState result, float delay) {
        _endingGame = true;
        State = result;
        LastWaveResultSummary = CombatAdvisorLogic.GetWaveResultSummary(result == GameState.Win, _bossHpRatio, _wallHpRatio, _deadMemberCount, _reloadingCount);
        GameEvents.RaiseGameEnded(result == GameState.Win);
        yield return new WaitForSeconds(delay);
        if (!string.IsNullOrEmpty(resultSceneName) && Application.CanStreamedLevelBeLoaded(resultSceneName)) {
            SceneManager.LoadScene(resultSceneName);
        }
    }
}
