using UnityEngine;

public class MinionController : MonoBehaviour {
    [SerializeField] private MinionConfigSO config;

    public bool IsAlive { get; private set; } = true;

    private float _hp;
    private float _attackTimer;
    private TerrainManager _terrain;
    private SquadMemberController[] _squad;
    // _spitterTarget: direct reference avoids fragile position-match in AttackTarget
    private SquadMemberController _spitterTarget;

    void Start() {
        if (config == null) {
            Debug.LogError($"[MinionController] config not assigned on {gameObject.name}. Disabling.", this);
            enabled = false;
            return;
        }
        _hp      = config.hp;
        _terrain = FindObjectOfType<TerrainManager>();
        _squad   = FindObjectsOfType<SquadMemberController>();
    }

    void OnEnable()  => GameEvents.OnMemberDied += RefreshSquad;
    void OnDisable() => GameEvents.OnMemberDied -= RefreshSquad;

    private void RefreshSquad(string _) => _squad = FindObjectsOfType<SquadMemberController>();

    void Update() {
        if (!IsAlive) return;

        bool hasTarget = ResolveTarget(out Vector2 targetPos);
        if (!hasTarget) {
            transform.position += Vector3.left * config.speed * Time.deltaTime;
            return;
        }

        float dist = Vector2.Distance(transform.position, targetPos);
        if (dist <= config.attackRange) {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f) {
                _attackTimer = config.attackInterval;
                AttackResolved();
            }
        } else {
            Vector2 dir = ((Vector3)targetPos - transform.position).normalized;
            transform.position += (Vector3)(dir * config.speed * Time.deltaTime);
        }
    }

    private bool ResolveTarget(out Vector2 pos) {
        if (config.type == MinionType.Spitter) {
            float nearest = float.MaxValue;
            _spitterTarget = null;
            foreach (var m in _squad) {
                if (m == null || !m.IsAlive) continue;
                float d = Vector2.Distance(transform.position, m.transform.position);
                if (d < nearest) { nearest = d; _spitterTarget = m; }
            }
            if (_spitterTarget != null) { pos = _spitterTarget.transform.position; return true; }
        }
        var barricade = _terrain?.GetAliveBarricade();
        if (barricade.HasValue) { pos = barricade.Value; return true; }
        var wall = _terrain?.GetWallTarget();
        if (wall.HasValue) { pos = wall.Value; return true; }
        pos = default;
        return false;
    }

    private void AttackResolved() {
        if (config.type == MinionType.Spitter) {
            if (_spitterTarget != null && _spitterTarget.IsAlive)
                _spitterTarget.TakeDamage(config.damage);
        } else {
            if (ResolveTarget(out Vector2 targetPos))
                _terrain?.MinionDamage(targetPos, config.terrainDamage);
        }
    }

    public void TakeDamage(float amount) {
        if (!IsAlive) return;
        _hp -= amount;
        if (_hp <= 0f) Die();
    }

    private void Die() {
        IsAlive = false;
        Destroy(gameObject);
    }
}
