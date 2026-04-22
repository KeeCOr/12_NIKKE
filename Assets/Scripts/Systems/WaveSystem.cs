using UnityEngine;

public class WaveSystem : MonoBehaviour {
    [SerializeField] private MinionConfigSO runnerConfig;
    [SerializeField] private MinionConfigSO berserkerConfig;
    [SerializeField] private MinionConfigSO spitterConfig;

    public struct PhaseParams {
        public float interval;
        public int countMin, countMax;
        public float runnerW, berserkerW, spitterW;
    }

    private float _elapsed;
    private float _spawnTimer = 4f; // initial delay before first wave so player can orient
    private bool  _isEnraged;

    // Stored method references — avoids the anonymous-lambda subscribe/unsubscribe leak
    void OnEnable()  => GameEvents.OnBossEnraged += HandleBossEnraged;
    void OnDisable() => GameEvents.OnBossEnraged -= HandleBossEnraged;

    private void HandleBossEnraged() {
        _isEnraged = true;
        _spawnTimer = Mathf.Min(_spawnTimer, 1.5f);
    }

    void Update() {
        _elapsed += Time.deltaTime;
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f) {
            var p = GetPhaseParams(_elapsed, _isEnraged);
            _spawnTimer = p.interval;
            Spawn(p);
        }
    }

    // static → EditMode tests can call directly without a MonoBehaviour instance
    public static PhaseParams GetPhaseParams(float elapsed, bool enraged) {
        if (enraged)        return new PhaseParams { interval=2.2f, countMin=6, countMax=10, runnerW=50, berserkerW=30, spitterW=20 };
        if (elapsed < 10f)  return new PhaseParams { interval=10f,  countMin=2, countMax=2,  runnerW=100 };
        if (elapsed < 20f)  return new PhaseParams { interval=7f,   countMin=2, countMax=2,  runnerW=100 };
        if (elapsed < 30f)  return new PhaseParams { interval=5.5f, countMin=2, countMax=4,  runnerW=80, berserkerW=10, spitterW=10 };
        if (elapsed < 45f)  return new PhaseParams { interval=4.5f, countMin=4, countMax=6,  runnerW=65, berserkerW=20, spitterW=15 };
        if (elapsed < 60f)  return new PhaseParams { interval=3.5f, countMin=4, countMax=8,  runnerW=50, berserkerW=30, spitterW=20 };
        return               new PhaseParams { interval=3f,   countMin=6, countMax=10, runnerW=40, berserkerW=35, spitterW=25 };
    }

    private void Spawn(PhaseParams p) {
        int count = Random.Range(p.countMin, p.countMax + 1);
        for (int i = 0; i < count; i++) {
            var type = WeightedRandom(p);
            float x = Random.Range(GameConfig.SPAWN_X_MIN, GameConfig.SPAWN_X_MAX);
            float y = Random.Range(GameConfig.SPAWN_Y_MIN, GameConfig.SPAWN_Y_MAX);
            GameEvents.RaiseSpawnMinion(type, new Vector2(x, y));
        }
    }

    private MinionType WeightedRandom(PhaseParams p) {
        float total = p.runnerW + p.berserkerW + p.spitterW;
        if (total <= 0f) {
            Debug.LogWarning("[WaveSystem] WeightedRandom: total weight is 0. Defaulting to Runner.");
            return MinionType.Runner;
        }
        float r = Random.value * total;
        if (r < p.runnerW) return MinionType.Runner;
        r -= p.runnerW;
        if (r < p.berserkerW) return MinionType.Berserker;
        return MinionType.Spitter;
    }
}
