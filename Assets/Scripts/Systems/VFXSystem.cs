using UnityEngine;

/// <summary>
/// Centralised visual-effect spawner. All effects are fire-and-forget
/// GameObjects that auto-destroy via ParticleSystem stopAction.
/// </summary>
public class VFXSystem : MonoBehaviour {
    public static VFXSystem Instance { get; private set; }

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    void OnDestroy() { if (Instance == this) Instance = null; }

    // ── Public API ──────────────────────────────────────────────────────────

    /// Small sparks when a boss part is struck.
    public void ShowHit(Vector3 pos, Color col) =>
        Burst(pos, col, 7, 0.45f, 2.2f, 0.055f, 0.5f, 0.3f);

    /// Large burst when a minion dies.
    public void ShowMinionDeath(Vector3 pos, Color col) =>
        Burst(pos, col, 18, 0.55f, 3.0f, 0.085f, 0.7f, 0.4f);

    /// Orange explosion for rocket impact.
    public void ShowExplosion(Vector3 pos) {
        Burst(pos, new Color(1.00f, 0.50f, 0.08f), 30, 0.60f, 4.5f, 0.12f, 0.85f, 0.45f);
        Burst(pos, new Color(1.00f, 0.90f, 0.30f), 15, 0.25f, 2.5f, 0.07f, 0.40f, 0.25f);
    }

    /// Quick flash at muzzle when firing.
    public void ShowMuzzleFlash(Vector3 pos) =>
        Burst(pos, new Color(1f, 0.92f, 0.45f), 5, 0.12f, 1.8f, 0.045f, 0.18f, 0.0f);

    /// Shockwave ring particles around the boss.
    public void ShowShockwave(Vector3 pos) =>
        Burst(pos, new Color(0.55f, 0.20f, 1.00f), 22, 0.50f, 3.5f, 0.08f, 0.6f, 0.0f);

    // ── Internal ────────────────────────────────────────────────────────────

    private static Material _mat;
    private static Material SpriteMat() {
        if (_mat != null) return _mat;
        _mat = new Material(Shader.Find("Sprites/Default"));
        return _mat;
    }

    private static void Burst(Vector3 pos, Color col,
            int count, float lifeMin, float speedMax,
            float size, float duration, float gravity) {

        var go = new GameObject("VFX");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();

        var main              = ps.main;
        main.startColor       = new ParticleSystem.MinMaxGradient(
                                    col, new Color(col.r * 0.7f, col.g * 0.7f, col.b * 0.6f));
        main.startSpeed       = new ParticleSystem.MinMaxCurve(speedMax * 0.3f, speedMax);
        main.startSize        = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.4f);
        main.startLifetime    = new ParticleSystem.MinMaxCurve(lifeMin, lifeMin * 1.6f);
        main.startRotation    = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.maxParticles     = count + 4;
        main.loop             = false;
        main.stopAction       = ParticleSystemStopAction.Destroy;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.gravityModifier  = gravity;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape        = ps.shape;
        shape.shapeType  = ParticleSystemShapeType.Circle;
        shape.radius     = 0.07f;

        // Alpha fade over lifetime
        var col2lt = ps.colorOverLifetime;
        col2lt.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(col, 0f), new GradientColorKey(col, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) }
        );
        col2lt.color = g;

        // Size shrink
        var sz2lt = ps.sizeOverLifetime;
        sz2lt.enabled = true;
        sz2lt.size    = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.1f)));

        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.material         = SpriteMat();
        psr.sortingLayerName = "Default";
        psr.sortingOrder     = 15;

        ps.Play();
        Object.Destroy(go, duration + lifeMin * 1.6f + 0.3f);
    }
}
