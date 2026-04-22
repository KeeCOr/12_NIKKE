using UnityEngine;

public class BossPartController : MonoBehaviour {
    public string PartId      { get; private set; }
    public bool   IsActive    { get; private set; }
    public bool   IsDestroyed { get; private set; }
    public float  MaxHp       { get; private set; }
    public float  Hp          { get; private set; }
    public float  DamageMult  { get; private set; }

    public float DebuffTimer  { get; private set; }
    public bool  DebuffStrong { get; private set; }

    private Collider2D _collider;

    public void Initialize(BossPartConfig cfg) {
        PartId      = cfg.id;
        MaxHp       = cfg.hp;
        Hp          = cfg.hp;
        DamageMult  = cfg.damageMult;
        IsActive    = cfg.activeOnStart;
        IsDestroyed = false;
        _collider   = GetComponent<Collider2D>();
        if (_collider != null) _collider.enabled = IsActive;
    }

    public void Activate() {
        IsActive = true;
        if (_collider != null) _collider.enabled = true;
    }

    public void UpdateDebuff(float delta) {
        if (DebuffTimer > 0f) DebuffTimer = Mathf.Max(0f, DebuffTimer - delta);
    }

    public void ApplyHitDebuff(bool isAlreadyDestroyed) {
        DebuffTimer  = 5f;
        DebuffStrong = isAlreadyDestroyed;
    }

    public float TakeDamage(float amount) {
        if (IsDestroyed) return 0f;
        float applied = Mathf.Min(Hp, amount);
        Hp -= applied;
        if (Hp <= 0f) DestroyPart();
        return applied;
    }

    private void DestroyPart() {
        IsDestroyed = true;
        if (_collider != null) _collider.enabled = false;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.13f, 0f, 0f);
        GameEvents.RaiseBossPartDestroyed(PartId);
    }
}
