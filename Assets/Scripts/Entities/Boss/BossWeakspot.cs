using System.Collections;
using UnityEngine;

/// <summary>
/// Temporary high-value target that appears on the boss when a part is destroyed.
/// Deals bonus damage on hit; expires after <see cref="Duration"/> seconds.
/// </summary>
public class BossWeakspot : MonoBehaviour {
    public const float Duration      = 8f;    // seconds before it fades away
    public const float DamageMult    = 2.5f;  // hit damage multiplier applied on top of base
    public const float PulseSpeed    = 3.5f;  // ring pulse frequency

    public BossController Boss { get; private set; }

    private SpriteRenderer _sr;
    private Collider2D     _col;
    private float          _elapsed;
    private Color          _baseColor;

    // ── Factory ──────────────────────────────────────────────────────────────

    public static BossWeakspot Spawn(BossController boss, Vector3 worldPos) {
        var go = new GameObject("Weakspot");
        go.transform.position = worldPos;
        go.transform.SetParent(boss.transform);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius    = 0.45f;
        col.isTrigger = true;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = BuildRingSprite();
        sr.color        = new Color(1f, 1f, 0.2f, 0.90f);  // bright yellow
        sr.sortingOrder = 14;
        go.transform.localScale = Vector3.one * 1.1f;

        var ws = go.AddComponent<BossWeakspot>();
        ws.Boss       = boss;
        ws._sr        = sr;
        ws._col       = col;
        ws._baseColor = sr.color;
        return ws;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Update() {
        _elapsed += Time.deltaTime;
        if (_elapsed >= Duration) { Expire(); return; }

        // Pulsing scale + alpha
        float t     = _elapsed / Duration;
        float pulse = 0.85f + 0.15f * Mathf.Sin(_elapsed * PulseSpeed * Mathf.PI * 2f);
        transform.localScale = Vector3.one * (1.1f * pulse);

        float alpha = Mathf.Lerp(0.90f, 0.10f, t);   // fade out over time
        if (_sr != null) _sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
    }

    void OnTriggerEnter2D(Collider2D other) {
        var bullet = other.GetComponent<Bullet>();
        if (bullet == null || Boss == null || !Boss.IsAlive) return;

        // Consume the bullet first so it doesn't also damage a boss part this frame
        bullet.Consume();
        // Bonus damage delivered directly to boss HP (bypasses part armor)
        Boss.ApplyWeakspotBonus(bullet.Damage * DamageMult, transform.position);
        VFXSystem.Instance?.ShowPartBreak(transform.position);
        Expire();
    }

    private void Expire() {
        if (_col != null) _col.enabled = false;
        Destroy(gameObject);
    }

    // ── Ring sprite (shared) ──────────────────────────────────────────────────

    private static Sprite _ringSprite;
    private static Sprite BuildRingSprite() {
        if (_ringSprite != null) return _ringSprite;

        const int size = 32;
        float cx = (size - 1) * 0.5f, cy = (size - 1) * 0.5f;
        float outerR = size * 0.46f, innerR = size * 0.30f;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float a = 0f;
                if (d >= innerR && d <= outerR)
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - (innerR + outerR) * 0.5f) / ((outerR - innerR) * 0.5f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        _ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), (float)size);
        return _ringSprite;
    }
}
