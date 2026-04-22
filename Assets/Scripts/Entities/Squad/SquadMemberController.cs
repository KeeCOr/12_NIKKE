using UnityEngine;

public class SquadMemberController : MonoBehaviour {
    [SerializeField] public SquadMemberConfigSO config;
    [SerializeField] private SpriteRenderer bodyRenderer;

    public string Id        => config.id;
    public bool   IsAlive   { get; private set; } = true;
    public float  Hp        { get; private set; }
    public float  MaxHp     { get; private set; }
    public bool   IsReloading { get; private set; }
    public int    CurrentAmmo { get; private set; }

    private float  _fireTimer;
    private float  _reloadTimer;
    private AimController _aim;

    public bool IsEchoMarkActive { get; set; }

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
        Vector2 muzzle = (Vector2)transform.position + new Vector2(0.22f, 0.58f);
        float angle    = Mathf.Atan2(aimPos.y - muzzle.y, aimPos.x - muzzle.x);

        float dmg = config.weapon.damage;
        if (config.special == SpecialType.WeakpointBonus && _aim.TargetPartId == "CORE")
            dmg *= config.specialVal;
        if (IsEchoMarkActive) dmg *= 1.2f;

        GameEvents.RaiseFireBullet(new BulletData {
            origin       = muzzle,
            angle        = angle,
            speed        = config.weapon.bulletSpeed * 0.01f, // px→units
            damage       = dmg,
            ownerId      = Id,
            bulletType   = config.weapon.bulletType,
            splashRadius = config.weapon.splashRadius * 0.01f,
            pellets      = config.weapon.pellets,
            spread       = config.weapon.spread,
            targetPartId = _aim.TargetPartId
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
    // pos unused: shockwave applies full damage to all living members regardless of distance

    public float GetHpRatio() => MaxHp > 0f ? Hp / MaxHp : 0f;
}
