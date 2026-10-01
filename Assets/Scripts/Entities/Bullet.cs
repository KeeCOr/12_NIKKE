using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour {
    public IObjectPool<Bullet> Pool   { get; set; }
    public float               Damage => _damage;

    /// <summary>Remove this bullet from the scene (called by external systems such as BossWeakspot).</summary>
    public void Consume() => ReturnToPool();

    private float _speed;
    private float _damage;
    private float _splashRadius;
    private string _targetPartId;
    private BulletType _bulletType;
    private BossController _boss;
    private float _lifetime;
    private const float MAX_LIFETIME = 3f;

    private static readonly Collider2D[] _splashBuffer = new Collider2D[32];

    private TrailRenderer _trail;
    private float   _maxRange;
    private Vector2 _spawnPos;
    void Awake() { _trail = GetComponent<TrailRenderer>(); }

    public void Initialize(BulletData data, BossController boss) {
        _speed        = data.speed;
        _damage       = data.damage;
        _splashRadius = data.splashRadius;
        _targetPartId = data.targetPartId;
        _bulletType   = data.bulletType;
        _maxRange     = data.maxRange;
        _spawnPos     = data.origin;
        _boss         = boss;
        _lifetime     = 0f;
        transform.position = data.origin;
        transform.rotation = Quaternion.Euler(0f, 0f, data.angle * Mathf.Rad2Deg);
        _trail?.Clear();   // prevent ghost trail from previous pool lifetime
    }

    void Update() {
        transform.Translate(Vector2.right * _speed * Time.deltaTime, Space.Self);
        _lifetime += Time.deltaTime;
        if (_lifetime >= MAX_LIFETIME) { ReturnToPool(); return; }
        if (_maxRange > 0f && Vector2.Distance(transform.position, _spawnPos) >= _maxRange)
            ReturnToPool();
    }

    void OnTriggerEnter2D(Collider2D other) {
        var part = other.GetComponent<BossPartController>();
        if (part != null && _boss != null) {
            _boss.TakeDamage(part.PartId, _damage, transform.position);
            if (_bulletType == BulletType.Rocket && _splashRadius > 0f) {
                VFXSystem.Instance?.ShowExplosion(transform.position);
                int count = Physics2D.OverlapCircleNonAlloc(transform.position, _splashRadius, _splashBuffer);
                for (int i = 0; i < count; i++) {
                    var p = _splashBuffer[i].GetComponent<BossPartController>();
                    if (p != null && p.PartId != part.PartId)
                        _boss.TakeDamage(p.PartId, _damage * 0.5f, transform.position);
                }
            }
            ReturnToPool();
            return;
        }

        var minion = other.GetComponent<MinionController>();
        if (minion != null) {
            minion.TakeDamage(_damage);
            if (_bulletType == BulletType.Rocket && _splashRadius > 0f) {
                VFXSystem.Instance?.ShowExplosion(transform.position);
                int count = Physics2D.OverlapCircleNonAlloc(transform.position, _splashRadius, _splashBuffer);
                for (int i = 0; i < count; i++) {
                    var m = _splashBuffer[i].GetComponent<MinionController>();
                    if (m != null && m != minion) m.TakeDamage(_damage * 0.5f);
                }
            }
            ReturnToPool();
        }
    }

    private void ReturnToPool() {
        // Deactivate before release so the collider can't fire again this physics step
        gameObject.SetActive(false);
        Pool?.Release(this);
    }
}
