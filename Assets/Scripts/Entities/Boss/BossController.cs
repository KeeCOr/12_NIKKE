using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour {
    public enum BossState { Walking, Attacking, Stunned, Enraged, Dying }

    [SerializeField] private BossConfigSO config;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer[] partRenderers; // Inspector 파트 순서 매핑

    public BossState State    { get; private set; } = BossState.Walking;
    public bool      IsAlive  { get; private set; } = true;
    public bool      IsEnraged { get; private set; }
    public float     Hp       { get; private set; }
    public float     MaxHp    { get; private set; }

    private readonly Dictionary<string, BossPartController> _parts = new();
    private float _attackTimer;
    private float _shockwaveTimer;
    private float _stunTimer;
    private float _stunCooldown;
    private TerrainManager _terrain;
    private CameraShaker _cameraShaker;

    private static readonly string[] _legIds  = { "LEG_L", "LEG_R" };
    private static readonly string[] _armIds  = { "ARM_L", "ARM_R" };
    private static readonly string[] _vulnIds = { "HEAD", "CORE" };

    void Awake() {
        if (config == null) {
            Debug.LogError("[BossController] config is not assigned!", this);
            enabled = false;
            return;
        }

        MaxHp = config.maxHp;
        Hp    = MaxHp;
        // Boss HP is tracked separately from part HP sums — config.maxHp must match
        // the intended total so they stay in sync as parts take damage.
        _attackTimer = config.attackInterval;
        transform.position = new Vector3(GameConfig.BOSS_START_X, GameConfig.BOSS_Y, 0f);

        foreach (var partCfg in config.parts) {
            var go  = new GameObject($"Part_{partCfg.id}");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(partCfg.offset.x, partCfg.offset.y, 0f);
            var col    = go.AddComponent<BoxCollider2D>();
            col.size   = partCfg.size;
            col.isTrigger = true;
            var part   = go.AddComponent<BossPartController>();
            part.Initialize(partCfg);
            _parts[partCfg.id] = part;
        }

        GameEvents.OnBossPartDestroyed += OnPartDestroyed;
    }

    void Start() {
        _terrain      = FindObjectOfType<TerrainManager>();
        _cameraShaker = Camera.main != null ? Camera.main.GetComponent<CameraShaker>() : null;
    }

    void OnDestroy() {
        GameEvents.OnBossPartDestroyed -= OnPartDestroyed;
    }

    void Update() {
        if (!IsAlive) return;
        float dt = Time.deltaTime;

        foreach (var p in _parts.Values) p.UpdateDebuff(dt);

        if (_stunTimer > 0f) { _stunTimer -= dt; return; }
        if (_stunCooldown > 0f) _stunCooldown -= dt;

        if (State == BossState.Walking || State == BossState.Enraged) {
            var obstacle = _terrain?.GetRoadBlockAhead(transform.position.x);
            if (obstacle.HasValue) {
                AttackObstacle(dt, obstacle.Value);
            } else if (transform.position.x > config.stopX) {
                transform.position += Vector3.left * EffectiveSpeed * dt;
            } else {
                AttackWall(dt);
            }
        }

        // Defense breach → game over (IsAlive guard prevents double TriggerDefenseBroken)
        if (transform.position.x < GameConfig.DEFENSE_LINE && IsAlive) {
            IsAlive = false;
            GameManager.Instance?.TriggerDefenseBroken();
            return;
        }

        if (IsEnraged) {
            _shockwaveTimer -= dt;
            if (_shockwaveTimer <= 0f) {
                _shockwaveTimer = config.shockwaveInterval;
                GameEvents.RaiseBossShockwave(transform.position, config.shockwaveDamage);
            }
        }
    }

    private float EffectiveSpeed {
        get {
            float s = IsEnraged ? config.enragedSpeed : config.speed;
            if (_parts.TryGetValue("LEG_L", out var ll) && ll.IsDestroyed) s *= 0.65f;
            if (_parts.TryGetValue("LEG_R", out var lr) && lr.IsDestroyed) s *= 0.65f;
            if (ll != null && ll.DebuffTimer > 0f) s *= ll.DebuffStrong ? 0.90f : 0.95f;
            if (lr != null && lr.DebuffTimer > 0f) s *= lr.DebuffStrong ? 0.90f : 0.95f;
            return s;
        }
    }

    private float EffectiveAttackInterval {
        get {
            float t = IsEnraged ? config.attackInterval * 0.5f : config.attackInterval;
            if (_parts.TryGetValue("ARM_L", out var al) && al.IsDestroyed) t *= 1.6f;
            if (_parts.TryGetValue("ARM_R", out var ar) && ar.IsDestroyed) t *= 1.6f;
            if (al != null && al.DebuffTimer > 0f) t *= al.DebuffStrong ? 1.40f : 1.20f;
            if (ar != null && ar.DebuffTimer > 0f) t *= ar.DebuffStrong ? 1.40f : 1.20f;
            return t;
        }
    }

    private void AttackWall(float dt) {
        _attackTimer -= dt;
        if (_attackTimer <= 0f) {
            _attackTimer = EffectiveAttackInterval;
            _terrain?.BossDamageWall(config.attackDamageWall);
            GameEvents.RaiseBossAttack();
            _cameraShaker?.Shake(0.18f, 0.008f);
        }
    }

    private void AttackObstacle(float dt, TerrainBlock block) {
        _attackTimer -= dt;
        if (_attackTimer <= 0f) {
            _attackTimer = EffectiveAttackInterval;
            _terrain?.BossSmashBlock(block, config.attackDamageWall * 0.75f);
            GameEvents.RaiseBossAttack();
            _cameraShaker?.Shake(0.13f, 0.006f);
        }
    }

    public float TakeDamage(string partId, float rawDmg) {
        if (!IsAlive) return 0f;
        if (!_parts.TryGetValue(partId, out var part)) return 0f;
        if (!part.IsActive || part.IsDestroyed) return 0f;

        // Apply debuff only on living parts; check IsDestroyed first to avoid writing
        // stale DebuffTimer onto destroyed parts (which would bleed into EffectiveSpeed).
        part.ApplyHitDebuff(false);

        int destroyedCount = 0;
        foreach (var p in _parts.Values)
            if (p.IsDestroyed && p.PartId != "CORE") destroyedCount++;
        float finalDmg = rawDmg * (1f + destroyedCount * 0.25f);

        foreach (var pid in _vulnIds) {
            if (_parts.TryGetValue(pid, out var dp) && dp.DebuffTimer > 0f)
                finalDmg *= dp.DebuffStrong ? 1.30f : 1.15f;
        }

        if (partId == "CHEST" && _stunCooldown <= 0f &&
            _parts.TryGetValue("CHEST", out var chest) && chest.DebuffTimer > 0f) {
            float chance = chest.DebuffStrong ? 0.20f : 0.08f;
            if (Random.value < chance) {
                _stunTimer    = 1.5f;
                _stunCooldown = 5f;
                _cameraShaker?.Shake(0.22f, 0.01f);
            }
        }

        float dmgApplied = part.TakeDamage(finalDmg);
        Hp = Mathf.Max(0f, Hp - dmgApplied);
        GameEvents.RaiseBossHpChanged(Hp, MaxHp);

        if (Hp <= 0f) Die();
        return dmgApplied;
    }

    public void Enrage() {
        if (IsEnraged) return;
        IsEnraged = true;
        State     = BossState.Enraged;
        _shockwaveTimer = 3f;
        if (bodyRenderer != null) bodyRenderer.color = new Color(0.48f, 0.1f, 0.04f); // enraged red
        _cameraShaker?.Shake(0.6f, 0.018f);
        GameEvents.RaiseBossEnraged();
    }

    private void OnPartDestroyed(string partId) {
        if (partId == "CHEST" && _parts.TryGetValue("CORE", out var core))
            core.Activate();
    }

    private void Die() {
        // Die() path: boss HP → 0. Contrast with defense breach path which calls
        // TriggerDefenseBroken() directly without raising OnBossDefeated.
        IsAlive = false;
        State   = BossState.Dying;
        StartCoroutine(DieCoroutine());
    }

    private IEnumerator DieCoroutine() {
        float elapsed = 0f;
        const float duration = 1.2f;
        Color startColor = bodyRenderer != null ? bodyRenderer.color : Color.white;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            if (bodyRenderer != null)
                bodyRenderer.color = new Color(startColor.r, startColor.g, startColor.b,
                    Mathf.Clamp01(1f - elapsed / duration));
            yield return null;
        }
        GameEvents.RaiseBossDefeated();
        gameObject.SetActive(false);
    }

    public BossPartController GetPart(string id) =>
        _parts.TryGetValue(id, out var p) ? p : null;

    // Returns false (no enrage) if HEAD or CHEST are absent from config — check config if enrage never fires.
    public bool MeetsEnrageCondition() =>
        Hp / MaxHp <= config.enrageHpThreshold
        && _parts.TryGetValue("HEAD",  out var h) && h.IsDestroyed
        && _parts.TryGetValue("CHEST", out var c) && c.IsDestroyed;
}
