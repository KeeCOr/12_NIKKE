using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class FireSystem : MonoBehaviour {
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private BossController boss;

    private IObjectPool<Bullet> _pool;
    private List<MinionController> _activeMinions = new();
    private bool _refreshPending;

    void Awake() {
        if (bulletPrefab == null) {
            Debug.LogError("[FireSystem] bulletPrefab is not assigned. Disabling.", this);
            enabled = false;
            return;
        }
        if (boss == null)
            Debug.LogWarning("[FireSystem] boss is not assigned — bullets will not damage the boss.", this);

        _pool = new ObjectPool<Bullet>(
            createFunc:      () => { var b = Instantiate(bulletPrefab); b.Pool = _pool; return b; },
            actionOnGet:     b => b.gameObject.SetActive(true),
            actionOnRelease: b => b.gameObject.SetActive(false),
            actionOnDestroy: b => Destroy(b.gameObject),
            collectionCheck: false,
            defaultCapacity: 64,
            maxSize: 256
        );
    }

    void OnEnable() {
        GameEvents.OnFireBullet  += HandleFireBullet;
        GameEvents.OnSpawnMinion += HandleSpawnMinion;
    }
    void OnDisable() {
        GameEvents.OnFireBullet  -= HandleFireBullet;
        GameEvents.OnSpawnMinion -= HandleSpawnMinion;
    }

    private void HandleFireBullet(BulletData data) {
        VFXSystem.Instance?.ShowMuzzleFlash(data.origin);
        int pellets = Mathf.Max(1, data.pellets);
        for (int i = 0; i < pellets; i++) {
            // Apply spread for multi-pellet weapons AND single-pellet weapons that have spread > 0 (e.g. AR)
            float spreadAngle = data.spread > 0f
                ? data.angle + Mathf.Deg2Rad * Random.Range(-data.spread / 2f, data.spread / 2f)
                : data.angle;
            var bullet   = _pool.Get();
            var shotData = data;
            shotData.angle = spreadAngle;
            bullet.Initialize(shotData, boss);
        }
    }

    private void HandleSpawnMinion(MinionType type, Vector2 pos) {
        // Debounce: start at most one refresh coroutine per wave (RaiseSpawnMinion fires N times per wave)
        if (!_refreshPending) {
            _refreshPending = true;
            StartCoroutine(RegisterMinionsNextFrame());
        }
    }

    private IEnumerator RegisterMinionsNextFrame() {
        yield return null;
        _refreshPending = false;
        _activeMinions.Clear();
        _activeMinions.AddRange(FindObjectsOfType<MinionController>());
    }
}
