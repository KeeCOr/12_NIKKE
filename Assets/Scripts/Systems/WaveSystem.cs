using UnityEngine;

public class WaveSystem : MonoBehaviour {
    [SerializeField] private MinionConfigSO runnerConfig;
    [SerializeField] private MinionConfigSO berserkerConfig;
    [SerializeField] private MinionConfigSO spitterConfig;

    [SerializeField] private GameObject runnerPrefab;
    [SerializeField] private GameObject berserkerPrefab;
    [SerializeField] private GameObject spitterPrefab;

    [SerializeField] private MapConfigSO mapConfig;

    public struct PhaseParams {
        public float interval;
        public int countMin, countMax;
        public float runnerW, berserkerW, spitterW;
    }

    private float _elapsed;
    private float _spawnTimer = 1.5f; // short delay so first runner appears quickly
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
        if (enraged)        return new PhaseParams { interval=2.0f,  countMin=5,  countMax=9,  runnerW=50, berserkerW=30, spitterW=20 };
        if (elapsed <  5f)  return new PhaseParams { interval=4.0f,  countMin=1,  countMax=1,  runnerW=100 };
        if (elapsed < 12f)  return new PhaseParams { interval=5.5f,  countMin=1,  countMax=2,  runnerW=100 };
        if (elapsed < 22f)  return new PhaseParams { interval=5.0f,  countMin=2,  countMax=3,  runnerW=90, berserkerW=10 };
        if (elapsed < 32f)  return new PhaseParams { interval=4.5f,  countMin=2,  countMax=4,  runnerW=75, berserkerW=15, spitterW=10 };
        if (elapsed < 45f)  return new PhaseParams { interval=3.5f,  countMin=4,  countMax=6,  runnerW=60, berserkerW=25, spitterW=15 };
        if (elapsed < 60f)  return new PhaseParams { interval=3.0f,  countMin=4,  countMax=8,  runnerW=50, berserkerW=30, spitterW=20 };
        return               new PhaseParams { interval=2.5f,  countMin=6,  countMax=10, runnerW=40, berserkerW=35, spitterW=25 };
    }

    private void Spawn(PhaseParams p) {
        int count = Random.Range(p.countMin, p.countMax + 1);
        for (int i = 0; i < count; i++) {
            var type = WeightedRandom(p);
            Vector2 pos = mapConfig != null
                ? mapConfig.RandomSpawnPoint()
                : new Vector2(
                    Random.Range(GameConfig.SPAWN_X_MIN, GameConfig.SPAWN_X_MAX),
                    Random.Range(GameConfig.SPAWN_Y_MIN, GameConfig.SPAWN_Y_MAX));
            SpawnMinion(type, pos);
            GameEvents.RaiseSpawnMinion(type, pos);
        }
    }

    private void SpawnMinion(MinionType type, Vector2 pos) {
        GameObject prefab = type switch {
            MinionType.Runner    => runnerPrefab,
            MinionType.Berserker => berserkerPrefab,
            MinionType.Spitter   => spitterPrefab,
            _ => runnerPrefab
        };
        if (prefab == null) {
            Debug.LogWarning($"[WaveSystem] Prefab for {type} not assigned.");
            return;
        }

        // Offset Y so feet land at pos.y (transform.position = sprite center by default)
        MinionConfigSO cfg = type switch {
            MinionType.Runner    => runnerConfig,
            MinionType.Berserker => berserkerConfig,
            MinionType.Spitter   => spitterConfig,
            _ => null
        };
        float yOff = cfg != null ? cfg.spriteHalfHeight : 0f;
        var spawnPos = new Vector3(pos.x, pos.y + yOff, 0f);

        var go = Instantiate(prefab, spawnPos, Quaternion.identity);
        if (mapConfig != null) {
            var mc = go.GetComponent<MinionController>();
            if (mc != null) mc.mapConfig = mapConfig;
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
