using UnityEngine;

public class AimController : MonoBehaviour {
    private SquadMemberConfigSO _config;

    public bool     HasTarget    { get; private set; }
    public Vector2  AimPosition  { get; private set; }
    public string   TargetPartId { get; private set; }
    public Vector2? DragTarget   { get; set; }

    private BossController     _boss;
    private BossPartController _userTargetPart;
    private MinionController   _userTargetMinion;
    private Vector2            _desiredAimPos;

    private SquadMemberController _owner;
    private SpriteRenderer        _bodySr;

    // Visual indicator — lives as a scene-root GO so it inherits no parent scale
    private GameObject    _indicatorRoot;
    private LineRenderer  _aimLine;
    private SpriteRenderer _crosshair;

    private static Sprite _crosshairSprite;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start() {
        _owner = GetComponent<SquadMemberController>();
        if (_owner == null || _owner.config == null) {
            Debug.LogError($"[AimController] SquadMemberController or its config is missing on {gameObject.name}. Disabling.", this);
            enabled = false;
            return;
        }
        _config = _owner.config;
        _bodySr = GetComponent<SpriteRenderer>();
        _boss   = FindObjectOfType<BossController>();
        AimPosition = _desiredAimPos = new Vector2(GameConfig.BOSS_START_X * 0.7f, GameConfig.BOSS_Y);
        BuildAimVisuals();
    }

    void OnDestroy() {
        if (_indicatorRoot != null) Destroy(_indicatorRoot);
    }

    void Update() {
        CleanupUserTarget();
        ResolveTarget();
        SmoothAim();
    }

    void LateUpdate() {
        UpdateAimVisuals();
    }

    // ── Aim visual ────────────────────────────────────────────────────────────

    private void BuildAimVisuals() {
        Color col = _config != null ? _config.color : Color.white;

        // Scene-root GO so transform scale doesn't distort line width or crosshair size
        _indicatorRoot = new GameObject($"AimIndicator_{name}");

        // ── Aim line ──────────────────────────────────────────────────────────
        _aimLine = _indicatorRoot.AddComponent<LineRenderer>();
        _aimLine.useWorldSpace    = true;
        _aimLine.positionCount    = 2;
        float wScale = _config.weapon.bulletType == BulletType.Rocket  ? 1.80f
                     : _config.weapon.bulletType == BulletType.Shotgun ? 1.50f
                     : _config.weapon.spread > 5f                      ? 1.10f
                     :                                                    0.80f;
        _aimLine.startWidth       = 0.030f * wScale;
        _aimLine.endWidth         = 0.008f * wScale;
        _aimLine.sortingLayerName = "Default";
        _aimLine.sortingOrder     = 12;
        _aimLine.material         = new Material(Shader.Find("Sprites/Default"));

        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(col, 0f), new GradientColorKey(col, 1f) },
            new[] { new GradientAlphaKey(1.00f, 0f), new GradientAlphaKey(0.20f, 0.75f), new GradientAlphaKey(0f, 1f) }
        );
        _aimLine.colorGradient = grad;
        _aimLine.enabled       = false;

        // ── Crosshair ring at target ──────────────────────────────────────────
        var cgo = new GameObject("Crosshair");
        cgo.transform.SetParent(_indicatorRoot.transform, false);
        float chScale = _config.weapon.bulletType == BulletType.Rocket  ? 0.32f
                      : _config.weapon.bulletType == BulletType.Shotgun ? 0.30f
                      : _config.weapon.spread > 5f                      ? 0.24f
                      :                                                    0.20f;
        cgo.transform.localScale = new Vector3(chScale, chScale, 1f);

        _crosshair                   = cgo.AddComponent<SpriteRenderer>();
        _crosshair.sprite            = BuildCrosshairSprite();
        _crosshair.color             = new Color(col.r, col.g, col.b, 0.92f);
        _crosshair.sortingLayerName  = "Default";
        _crosshair.sortingOrder      = 12;
        _crosshair.enabled           = false;
    }

    private void UpdateAimVisuals() {
        bool alive = _owner == null || _owner.IsAlive;
        bool show  = HasTarget && alive;

        if (!show) {
            if (_aimLine   != null) _aimLine.enabled   = false;
            if (_crosshair != null) _crosshair.enabled = false;
            return;
        }

        // Muzzle = right side, upper body of the character sprite in world space
        Bounds b = (_bodySr != null && _bodySr.sprite != null)
            ? _bodySr.bounds
            : new Bounds(transform.position, new Vector3(0.5f, 1.0f, 0.1f));

        var muzzle = new Vector3(
            b.center.x + b.extents.x * 0.85f,
            b.center.y + b.extents.y * 0.30f,
            0f);
        var target = new Vector3(AimPosition.x, AimPosition.y, 0f);

        if (_aimLine != null) {
            _aimLine.enabled = true;
            _aimLine.SetPosition(0, muzzle);
            _aimLine.SetPosition(1, target);
        }
        if (_crosshair != null) {
            _crosshair.enabled          = true;
            _crosshair.transform.position = target;
        }
    }

    // ── Aim logic (unchanged) ─────────────────────────────────────────────────

    private void CleanupUserTarget() {
        if (_userTargetPart != null && (!_userTargetPart.IsActive || _userTargetPart.IsDestroyed))
            _userTargetPart = null;
        if (_userTargetMinion != null && !_userTargetMinion.IsAlive)
            _userTargetMinion = null;
    }

    private void ResolveTarget() {
        if (_userTargetPart != null) {
            HasTarget      = true;
            _desiredAimPos = _userTargetPart.transform.position;
            TargetPartId   = _userTargetPart.PartId;
            return;
        }
        if (_userTargetMinion != null) {
            HasTarget      = true;
            _desiredAimPos = _userTargetMinion.transform.position;
            TargetPartId   = null;
            return;
        }
        if (_boss != null && _boss.IsAlive && _config.aimPriority != null) {
            foreach (var pid in _config.aimPriority) {
                var part = _boss.GetPart(pid);
                if (part != null && part.IsActive && !part.IsDestroyed) {
                    HasTarget      = true;
                    _desiredAimPos = part.transform.position;
                    TargetPartId   = pid;
                    return;
                }
            }
        }
        HasTarget    = false;
        TargetPartId = null;
    }

    private void SmoothAim() {
        if (DragTarget.HasValue) {
            AimPosition = DragTarget.Value;
            return;
        }
        if (HasTarget)
            AimPosition = Vector2.MoveTowards(AimPosition, _desiredAimPos, 5f * Time.deltaTime);
    }

    public void SetUserTargetPart(BossPartController part)   { _userTargetPart = part; _userTargetMinion = null; }
    public void SetUserTargetMinion(MinionController minion) { _userTargetMinion = minion; _userTargetPart = null; }
    public void ClearUserTarget() { _userTargetPart = null; _userTargetMinion = null; }

    // ── Crosshair sprite (ring, created once) ─────────────────────────────────

    private static Sprite BuildCrosshairSprite() {
        if (_crosshairSprite != null) return _crosshairSprite;

        const int size = 32;
        float cx    = (size - 1) * 0.5f;
        float cy    = (size - 1) * 0.5f;
        float ringR = size * 0.37f;   // ring radius in pixels
        float ringHW = size * 0.10f;  // half-width of the ring stroke

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float dist  = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float alpha = Mathf.Clamp01(1f - Mathf.Abs(dist - ringR) / ringHW);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        // PPU = size so natural world size = 1 unit; localScale on the GO controls display size
        _crosshairSprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            (float)size);
        return _crosshairSprite;
    }
}
