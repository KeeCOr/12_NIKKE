using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// VFXSystem and DamageNumberSystem referenced by name (same assembly)

public class BossController : MonoBehaviour {
    public enum BossState { Walking, Attacking, Stunned, Enraged, Dying }

    [SerializeField] private BossConfigSO config;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer[] partRenderers;

    public BossState State     { get; private set; } = BossState.Walking;
    public bool      IsAlive   { get; private set; } = true;
    public bool      IsEnraged { get; private set; }
    public float     Hp        { get; private set; }
    public float     MaxHp     { get; private set; }

    private readonly Dictionary<string, BossPartController> _parts = new();
    private float _attackTimer;
    private float _shockwaveTimer;
    private float _stunTimer;
    private float _stunCooldown;
    private bool  _wallDestroyed;
    private TerrainManager _terrain;
    private CameraShaker   _cameraShaker;

    private static readonly string[] _legIds  = { "LEG_L", "LEG_R" };
    private static readonly string[] _armIds  = { "ARM_L", "ARM_R" };
    private static readonly string[] _vulnIds = { "HEAD",  "CORE"  };

    // ── shared white sprite created once at runtime for part renderers ──────
    private static Sprite _sharedSprite;
    private static Sprite SharedSprite() {
        if (_sharedSprite != null) return _sharedSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        _sharedSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return _sharedSprite;
    }

    private static Color PartBaseColor(string id) => id switch {
        "HEAD"  => new Color(1.0f, 0.85f, 0.10f, 0.55f),
        "CHEST" => new Color(1.0f, 0.30f, 0.10f, 0.50f),
        "ARM_L" => new Color(0.25f, 0.55f, 1.0f, 0.45f),
        "ARM_R" => new Color(0.25f, 0.55f, 1.0f, 0.45f),
        "LEG_L" => new Color(0.25f, 0.90f, 0.40f, 0.45f),
        "LEG_R" => new Color(0.25f, 0.90f, 0.40f, 0.45f),
        "CORE"  => new Color(1.0f, 0.20f, 0.90f, 0.00f),  // invisible until activated
        _       => new Color(1f, 1f, 1f, 0.35f),
    };

    void Awake() {
        if (config == null) {
            Debug.LogError("[BossController] config is not assigned!", this);
            enabled = false;
            return;
        }

        MaxHp = config.maxHp;
        Hp    = MaxHp;
        _attackTimer = config.attackInterval;
        transform.position = new Vector3(GameConfig.BOSS_START_X, GameConfig.BOSS_Y, 0f);

        Sprite sp = SharedSprite();
        foreach (var partCfg in config.parts) {
            var go = new GameObject($"Part_{partCfg.id}");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(partCfg.offset.x, partCfg.offset.y, -0.1f);
            // Scale the part so the SpriteRenderer matches the collider area
            go.transform.localScale    = new Vector3(partCfg.size.x, partCfg.size.y, 1f);

            var col      = go.AddComponent<BoxCollider2D>();
            col.size     = Vector2.one;
            col.isTrigger = true;

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sp;
            sr.color        = PartBaseColor(partCfg.id);
            sr.sortingOrder = 6;

            var part = go.AddComponent<BossPartController>();
            part.Initialize(partCfg);
            _parts[partCfg.id] = part;
        }

        GameEvents.OnBossPartDestroyed += OnPartDestroyed;
        GameEvents.OnBossPartBreak     += OnPartBreak;
    }

    void Start() {
        _terrain      = FindObjectOfType<TerrainManager>();
        _cameraShaker = Camera.main != null ? Camera.main.GetComponent<CameraShaker>() : null;
    }

    void OnEnable()  => GameEvents.OnWallDestroyed += HandleWallDestroyed;
    void OnDisable() => GameEvents.OnWallDestroyed -= HandleWallDestroyed;

    void OnDestroy() {
        GameEvents.OnBossPartDestroyed -= OnPartDestroyed;
        GameEvents.OnBossPartBreak     -= OnPartBreak;
    }

    private void HandleWallDestroyed() => _wallDestroyed = true;

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
            } else if (!_wallDestroyed && transform.position.x > config.stopX) {
                // Advance toward wall along diagonal
                AdvanceAlongDiagonal(dt);
            } else if (!_wallDestroyed) {
                // At stopX — keep attacking wall
                AttackWall(dt);
            } else {
                // Wall is destroyed — advance past stopX toward defense line
                AdvanceAlongDiagonal(dt);
            }
        }

        // Defense breach triggers when boss crosses the defense line after wall falls
        if (transform.position.x < GameConfig.DEFENSE_LINE && IsAlive) {
            IsAlive = false;
            GameManager.Instance?.TriggerDefenseBroken();
            return;
        }

        if (IsEnraged) {
            _shockwaveTimer -= dt;
            if (_shockwaveTimer <= 0f) {
                _shockwaveTimer = config.shockwaveInterval;
                VFXSystem.Instance?.ShowShockwave(transform.position);
                GameEvents.RaiseBossShockwave(transform.position, config.shockwaveDamage);
            }
        }
    }

    // Move one frame along the start→stop diagonal, extrapolating past stopX when wall falls
    private void AdvanceAlongDiagonal(float dt) {
        float newX = transform.position.x - EffectiveSpeed * dt;
        float t    = Mathf.InverseLerp(GameConfig.BOSS_START_X, GameConfig.BOSS_STOP_X, newX);
        float newY = Mathf.LerpUnclamped(GameConfig.BOSS_START_Y, GameConfig.BOSS_STOP_Y, t);
        transform.position = new Vector3(newX, newY, transform.position.z);
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

    public float TakeDamage(string partId, float rawDmg, Vector3? hitPos = null) {
        if (!IsAlive) return 0f;
        if (!_parts.TryGetValue(partId, out var part)) return 0f;
        if (!part.IsActive) return 0f;  // CORE inactive until CHEST destroyed — fully blocked

        part.ApplyHitDebuff(part.IsDestroyed);

        int destroyedCount = 0;
        foreach (var p in _parts.Values)
            if (p.IsDestroyed && p.PartId != "CORE") destroyedCount++;

        float finalDmg;
        if (part.IsDestroyed) {
            // Destroyed area: bullet passes through at reduced rate — still worth shooting
            finalDmg = rawDmg * 0.70f;
        } else {
            // Intact part: damageMult is the armor coefficient (< 1 = resistant, > 1 = weak spot)
            // Each already-destroyed part weakens the overall armor by 15%
            finalDmg = rawDmg * part.DamageMult * (1f + destroyedCount * 0.15f);
        }

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

        part.TakeDamage(finalDmg);
        Hp = Mathf.Max(0f, Hp - finalDmg);
        GameEvents.RaiseBossHpChanged(Hp, MaxHp);

        bool isCrit = finalDmg >= rawDmg * 1.25f || partId == "CORE";
        Vector3 numPos = hitPos ?? (part.transform.position + Vector3.up * 0.6f);
        DamageNumberSystem.Instance?.Show(finalDmg, isCrit, numPos);

        if (bodyRenderer != null) StartCoroutine(BodyHitFlash());

        if (Hp <= 0f) Die();
        return finalDmg;
    }

    public void Enrage() {
        if (IsEnraged) return;
        IsEnraged = true;
        State     = BossState.Enraged;
        _shockwaveTimer = 3f;
        if (bodyRenderer != null) bodyRenderer.color = new Color(0.48f, 0.10f, 0.04f);
        _cameraShaker?.Shake(0.6f, 0.018f);
        GameEvents.RaiseBossEnraged();
    }

    private void OnPartDestroyed(string partId) {
        if (partId == "CHEST" && _parts.TryGetValue("CORE", out var core))
            core.Activate();
    }

    private void OnPartBreak(string partId, Vector3 pos) {
        if (!IsAlive) return;
        const float bonusDmg = 100f;
        Hp = Mathf.Max(0f, Hp - bonusDmg);
        GameEvents.RaiseBossHpChanged(Hp, MaxHp);
        VFXSystem.Instance?.ShowPartBreak(pos);
        _cameraShaker?.Shake(0.45f, 0.016f);
        DamageNumberSystem.Instance?.Show(bonusDmg, true, pos + Vector3.up * 0.5f);
        if (Hp <= 0f) Die();
    }

    private void Die() {
        IsAlive = false;
        State   = BossState.Dying;
        StartCoroutine(DieCoroutine());
    }

    private IEnumerator DieCoroutine() {
        float elapsed  = 0f;
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

    private Color _bodyBaseColor;
    private bool  _bodyColorCached;

    private IEnumerator BodyHitFlash() {
        if (!_bodyColorCached) {
            _bodyBaseColor   = bodyRenderer.color;
            _bodyColorCached = true;
        }
        bodyRenderer.color = Color.white;
        yield return new WaitForSeconds(0.06f);
        bodyRenderer.color = _bodyBaseColor;
    }

    public BossPartController GetPart(string id) =>
        _parts.TryGetValue(id, out var p) ? p : null;

    public bool MeetsEnrageCondition() =>
        Hp / MaxHp <= config.enrageHpThreshold
        && _parts.TryGetValue("HEAD",  out var h) && h.IsDestroyed
        && _parts.TryGetValue("CHEST", out var c) && c.IsDestroyed;
}
