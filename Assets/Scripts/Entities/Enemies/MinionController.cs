using UnityEngine;

public class MinionController : MonoBehaviour {
    [SerializeField] private MinionConfigSO config;
    public MapConfigSO mapConfig;  // set by WaveSystem at spawn time

    public bool IsAlive { get; private set; } = true;

    private const float SEP_RADIUS   = 0.75f;  // 분리 감지 반경 (world units)
    private const float SEP_STRENGTH = 1.2f;   // 분리 강도

    private static readonly Collider2D[] _sepBuf = new Collider2D[16];

    private float   _hp;
    private float   _attackTimer;
    private Vector3 _baseScale;
    private TerrainManager _terrain;
    private SquadMemberController[] _squad;
    private SquadMemberController _spitterTarget;
    private HitFlash  _hitFlash;
    private Color     _bodyColor;

    void Start() {
        if (config == null) {
            Debug.LogError($"[MinionController] config not assigned on {gameObject.name}. Disabling.", this);
            enabled = false;
            return;
        }
        _hp        = config.hp;
        _baseScale = transform.localScale;
        _terrain   = FindObjectOfType<TerrainManager>();
        _squad     = FindObjectsOfType<SquadMemberController>();
        _hitFlash  = GetComponent<HitFlash>();
        var sr     = GetComponent<SpriteRenderer>();
        _bodyColor = sr != null ? sr.color : Color.white;
    }

    void OnEnable()  => GameEvents.OnMemberDied += RefreshSquad;
    void OnDisable() => GameEvents.OnMemberDied -= RefreshSquad;

    private void RefreshSquad(string _) => _squad = FindObjectsOfType<SquadMemberController>();

    void Update() {
        if (!IsAlive) return;

        // Perspective scale: small when far (upper-right), full size near squad
        float tx = Mathf.Clamp01(Mathf.InverseLerp(GameConfig.BOSS_START_X, GameConfig.DEFENSE_LINE, transform.position.x));
        transform.localScale = _baseScale * Mathf.Lerp(0.30f, 1.0f, tx);

        // Separation: push away from nearby minions to reduce overlap
        ApplySeparation();

        bool hasTarget = ResolveTarget(out Vector2 targetPos);
        if (!hasTarget) {
            // No terrain or squad target — continue along the diagonal path (upper-right → lower-left)
            Vector2 diagDir = mapConfig != null
                ? mapConfig.DiagonalDirection()
                : new Vector2(-1f, -0.378f).normalized;
            transform.position += (Vector3)(diagDir * config.speed * Time.deltaTime);
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

    private void ApplySeparation() {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, SEP_RADIUS, _sepBuf);
        Vector2 push = Vector2.zero;
        for (int i = 0; i < count; i++) {
            var col = _sepBuf[i];
            if (col == null || col.gameObject == gameObject) continue;
            if (col.GetComponent<MinionController>() == null) continue;
            Vector2 away = (Vector2)transform.position - (Vector2)col.transform.position;
            float dist = away.magnitude;
            if (dist < 0.001f) {
                push += UnityEngine.Random.insideUnitCircle.normalized * SEP_STRENGTH;
            } else {
                push += away.normalized * (SEP_STRENGTH * (1f - dist / SEP_RADIUS));
            }
        }
        if (push != Vector2.zero)
            transform.position += (Vector3)(push * Time.deltaTime);
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
        // All terrain destroyed — head directly for any alive squad member
        foreach (var m in _squad) {
            if (m != null && m.IsAlive) { pos = m.transform.position; return true; }
        }
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
        DamageNumberSystem.Instance?.Show(amount, false,
            transform.position + Vector3.up * 0.5f);
        _hitFlash?.Flash();
        _hp -= amount;
        if (_hp <= 0f) Die();
    }

    private void Die() {
        IsAlive = false;
        VFXSystem.Instance?.ShowMinionDeath(transform.position, _bodyColor);
        Destroy(gameObject);
    }
}
