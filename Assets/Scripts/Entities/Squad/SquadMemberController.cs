using UnityEngine;

public class SquadMemberController : MonoBehaviour {
    [SerializeField] public SquadMemberConfigSO config;
    [SerializeField] private SpriteRenderer bodyRenderer;

    public string Id          => config.id;
    public bool   IsAlive     { get; private set; } = true;
    public float  Hp          { get; private set; }
    public float  MaxHp       { get; private set; }
    public bool   IsReloading { get; private set; }
    public int    CurrentAmmo { get; private set; }

    public bool IsEchoMarkActive { get; set; }

    private float  _fireTimer;
    private float  _reloadTimer;
    private AimController _aim;
    private Color  _defaultColor;

    void Awake() {
        if (config == null) {
            Debug.LogError($"[SquadMemberController] config not assigned on {gameObject.name}. Disabling.", this);
            enabled = false;
            return;
        }
        MaxHp       = config.hp;
        Hp          = MaxHp;
        CurrentAmmo = config.weapon.magazineSize;
        _aim        = GetComponent<AimController>();
        _defaultColor = bodyRenderer != null ? bodyRenderer.color : Color.white;
    }

    void OnEnable()  => GameEvents.OnBossShockwave += HandleShockwave;
    void OnDisable() => GameEvents.OnBossShockwave -= HandleShockwave;

    void Update() {
        if (!IsAlive) return;

        if (IsReloading) {
            _reloadTimer -= Time.deltaTime;
            float progress = Mathf.Clamp01(1f - _reloadTimer / config.weapon.reloadTime);
            GameEvents.RaiseReloadProgress(Id, progress);
            if (_reloadTimer <= 0f) FinishReload();
            return;
        }

        _fireTimer -= Time.deltaTime;
        if (_fireTimer <= 0f && CurrentAmmo > 0) TryFire();
        if (CurrentAmmo <= 0 && !IsReloading) StartReload();
    }

    private void TryFire() {
        if (_aim == null || !_aim.HasTarget) return;

        Vector2 aimPos = _aim.AimPosition;
        Bounds _b = (bodyRenderer != null && bodyRenderer.sprite != null)
            ? bodyRenderer.bounds
            : new Bounds(transform.position, new Vector3(0.5f, 1.0f, 0.1f));
        Vector2 muzzle = new Vector2(
            _b.center.x + _b.extents.x * 0.85f,
            _b.center.y + _b.extents.y * 0.30f);
        float   angle  = Mathf.Atan2(aimPos.y - muzzle.y, aimPos.x - muzzle.x);
        float   dist   = Vector2.Distance(muzzle, aimPos);

        // Clamp to effective max range (0 = use global default, so every weapon has a limit)
        float effectiveMaxRange = config.weapon.maxRange > 0f
            ? config.weapon.maxRange
            : GameConfig.DEFAULT_MAX_RANGE;
        if (dist > effectiveMaxRange) return;

        float dmg             = config.weapon.damage;
        float effectiveSpread = config.weapon.spread;
        float effectiveSplash = config.weapon.splashRadius * 0.01f;

        switch (config.special) {
            case SpecialType.WeakpointBonus:
                if (_aim.TargetPartId == "CORE")
                    dmg *= config.specialVal;
                break;

            case SpecialType.WeakpointMark:
                if (!string.IsNullOrEmpty(_aim.TargetPartId))
                    dmg *= config.specialVal;
                break;

            case SpecialType.BurstAccuracy:
                // Bravo AR: tighter base spread, then distance drift re-added
                if (effectiveSpread > 0f)
                    effectiveSpread /= config.specialVal;
                break;

            case SpecialType.RocketSplash:
                effectiveSplash *= config.specialVal;
                break;
        }

        // AR (single-pellet with spread): bullets drift further at range.
        // Extra spread starts growing past 3 wu — at 12 wu it adds ~10 degrees.
        if (config.weapon.bulletType == BulletType.Single && effectiveSpread > 0f)
            effectiveSpread += Mathf.Max(0f, dist - 3f) * 1.2f;

        // Echo mark bonus (applied by external system)
        if (IsEchoMarkActive) dmg *= 1.2f;

        GameEvents.RaiseFireBullet(new BulletData {
            origin       = muzzle,
            angle        = angle,
            speed        = config.weapon.bulletSpeed * 0.01f,
            damage       = dmg,
            ownerId      = Id,
            bulletType   = config.weapon.bulletType,
            splashRadius = effectiveSplash,
            pellets      = config.weapon.pellets,
            spread       = effectiveSpread,
            targetPartId = _aim.TargetPartId,
            maxRange     = effectiveMaxRange
        });

        CurrentAmmo--;
        _fireTimer = config.weapon.fireRate;
        GameEvents.RaiseAmmoChanged(Id, CurrentAmmo, config.weapon.magazineSize);
    }

    private void StartReload() {
        if (config.weapon.reloadTime <= 0f) { FinishReload(); return; }
        IsReloading  = true;
        _reloadTimer = config.weapon.reloadTime;
        GameEvents.RaiseReloadStarted(Id);
    }

    private void FinishReload() {
        IsReloading = false;
        CurrentAmmo = config.weapon.magazineSize;
        GameEvents.RaiseReloadComplete(Id);
        GameEvents.RaiseAmmoChanged(Id, CurrentAmmo, config.weapon.magazineSize);
    }

    public void TakeDamage(float amount) {
        if (!IsAlive) return;
        Hp = Mathf.Max(0f, Hp - amount);
        if (Hp <= 0f) Die();
    }

    private void Die() {
        IsAlive = false;
        if (bodyRenderer != null) bodyRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        GameEvents.RaiseMemberDied(Id);
    }

    private void HandleShockwave(Vector2 pos, float damage) => TakeDamage(damage);

    public float GetHpRatio() => MaxHp > 0f ? Hp / MaxHp : 0f;

    public void SetSelected(bool selected) {
        if (bodyRenderer == null) return;
        bodyRenderer.color = selected
            ? Color.Lerp(_defaultColor, Color.white, 0.45f)
            : _defaultColor;
    }
}
