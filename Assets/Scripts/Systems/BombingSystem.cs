using UnityEngine;

public class BombingSystem : MonoBehaviour {
    [SerializeField] private float damage   = 300f;
    [SerializeField] private float radius   = 1.5f;
    [SerializeField] private float cooldown = 20f;
    [SerializeField] private BossController boss;

    private float _timer;
    public bool IsReady => _timer <= 0f;

    void Update() {
        if (_timer > 0f) _timer -= Time.deltaTime;
    }

    public void Activate(Vector2 position) {
        if (!IsReady) return;
        _timer = cooldown;

        var hits = Physics2D.OverlapCircleAll(position, radius);
        foreach (var h in hits) {
            var part = h.GetComponent<BossPartController>();
            if (part != null && boss != null) boss.TakeDamage(part.PartId, damage);
            h.GetComponent<MinionController>()?.TakeDamage(damage);
        }
    }
}
