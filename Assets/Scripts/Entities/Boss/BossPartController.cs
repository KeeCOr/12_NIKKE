using System.Collections;
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

    private Collider2D      _collider;
    private SpriteRenderer  _renderer;
    private Color           _activeColor;

    public void Initialize(BossPartConfig cfg) {
        PartId     = cfg.id;
        MaxHp      = cfg.hp;
        Hp         = cfg.hp;
        DamageMult = cfg.damageMult;
        IsActive   = cfg.activeOnStart;
        IsDestroyed = false;

        _collider = GetComponent<Collider2D>();
        _renderer = GetComponent<SpriteRenderer>();

        if (_collider != null) _collider.enabled = IsActive;

        if (_renderer != null) {
            _activeColor = _renderer.color;
            // CORE starts invisible until CHEST is destroyed
            if (!cfg.activeOnStart)
                _renderer.color = new Color(_activeColor.r, _activeColor.g, _activeColor.b, 0f);
        }
    }

    public void Activate() {
        IsActive = true;
        if (_collider != null) _collider.enabled = true;
        if (_renderer != null)
            _renderer.color = new Color(_activeColor.r, _activeColor.g, _activeColor.b, 0.70f);
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
        UpdateDamageVisual();
        StartCoroutine(HitFlashCoroutine());
        VFXSystem.Instance?.ShowHit(transform.position, _activeColor);
        if (Hp <= 0f) DestroyPart();
        return applied;
    }

    private IEnumerator HitFlashCoroutine() {
        if (_renderer == null) yield break;
        _renderer.color = Color.white;
        yield return new WaitForSeconds(0.07f);
        UpdateDamageVisual();   // re-apply correct damage-tinted color
    }

    private void UpdateDamageVisual() {
        if (_renderer == null || IsDestroyed) return;
        float ratio = MaxHp > 0f ? Hp / MaxHp : 0f;
        Color damaged = new Color(0.4f, 0.05f, 0.05f, _activeColor.a);
        _renderer.color = Color.Lerp(damaged, _activeColor, ratio);
    }

    private void DestroyPart() {
        IsDestroyed = true;
        if (_collider != null) _collider.enabled = false;
        if (_renderer != null) _renderer.color   = new Color(0.10f, 0.02f, 0.02f, 0.60f);
        GameEvents.RaiseBossPartDestroyed(PartId);
    }
}
