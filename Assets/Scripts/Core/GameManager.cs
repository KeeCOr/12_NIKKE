using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Win, Lose }
    public GameState State { get; private set; } = GameState.Playing;

    [SerializeField] private float resultDelay = 1.2f;
    [SerializeField] private string resultSceneName = "Result";

    private int _deadMemberCount;
    private int _totalMemberCount;
    private bool _endingGame;

    void Awake() {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() {
        State            = GameState.Playing;
        _deadMemberCount = 0;
        _endingGame      = false;
        RegisterSquadSize(FindObjectsOfType<SquadMemberController>().Length);
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    void OnEnable() {
        GameEvents.OnBossDefeated += HandleBossDefeated;
        GameEvents.OnMemberDied   += HandleMemberDied;
    }

    void OnDisable() {
        GameEvents.OnBossDefeated -= HandleBossDefeated;
        GameEvents.OnMemberDied   -= HandleMemberDied;
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

    public void NotifyAllMembersDead() {
        if (State != GameState.Playing || _endingGame) return;
        StartCoroutine(EndGame(GameState.Lose, resultDelay));
    }

    private System.Collections.IEnumerator EndGame(GameState result, float delay) {
        _endingGame = true;
        State = result;
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(resultSceneName);
    }
}
