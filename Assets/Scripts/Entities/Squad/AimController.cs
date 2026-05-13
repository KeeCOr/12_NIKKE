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

    // Cached color states for in-range vs out-of-range
    private Gradient _inRangeGrad;
    private Gradient _outOfRangeGrad;
    private Color    _inRangeCrossColor;
    private Color    _outOfRangeCrossColor;
    private bool     _wasOutOfRange;

    // Base crosshair scale (world units) — multiplied by boss perspective scale each frame
    private float _crosshairBaseScale;
    private float _lineBaseStartWidth;
    private float _lineBaseEndWidth;

    private float EffectiveMaxRange => (_config != null && _config.weapon.maxRange > 0f)
        ? _config.weapon.maxRange : GameConfig.DEFAULT_MAX_RANGE;

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

        // Pre-build both colour states (in-range = character colour, out-of-range = greyscale)
        float lum = col.r * 0.299f + col.g * 0.587f + col.b * 0.114f;
        Color gray = new Color(lum, lum, lum, 1f);

        var alphas = new[] {
            new GradientAlphaKey(0.85f, 0f),
            new GradientAlphaKey(0.50f, 0.6f),
            new GradientAlphaKey(0.30f, 1f)
        };
        _inRangeGrad = new Gradient();
        _inRangeGrad.SetKeys(
            new[] { new GradientColorKey(col,  0f), new GradientColorKey(col,  1f) }, alphas);
        _outOfRangeGrad = new Gradient();
        _outOfRangeGrad.SetKeys(
            new[] { new GradientColorKey(gray, 0f), new GradientColorKey(gray, 1f) }, alphas);

        _inRangeCrossColor = new Color(
            Mathf.Min(1f, col.r * 1.3f + 0.2f),
            Mathf.Min(1f, col.g * 1.3f + 0.2f),
            Mathf.Min(1f, col.b * 1.3f + 0.2f), 1.0f);
        float gb = Mathf.Min(1f, lum * 1.3f + 0.2f);
        _outOfRangeCrossColor = new Color(gb, gb, gb, 1.0f);

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
        _lineBaseStartWidth       = 0.065f * wScale;
        _lineBaseEndWidth         = 0.022f * wScale;
        _aimLine.startWidth       = _lineBaseStartWidth;
        _aimLine.endWidth         = _lineBaseEndWidth;
        _aimLine.sortingLayerName = "Default";
        _aimLine.sortingOrder     = 12;
        _aimLine.material         = new Material(Shader.Find("Sprites/Default"));
        _aimLine.colorGradient    = _inRangeGrad;
        _aimLine.enabled          = false;

        // ── Crosshair ring at target ──────────────────────────────────────────
        var cgo = new GameObject("Crosshair");
        cgo.transform.SetParent(_indicatorRoot.transform, false);
        _crosshairBaseScale = _config.weapon.bulletType == BulletType.Rocket  ? 0.60f
                           : _config.weapon.bulletType == BulletType.Shotgun ? 0.55f
                           : _config.weapon.spread > 5f                      ? 0.45f
                           :                                                    0.38f;
        cgo.transform.localScale = new Vector3(_crosshairBaseScale, _crosshairBaseScale, 1f);

        _crosshair                  = cgo.AddComponent<SpriteRenderer>();
        _crosshair.sprite           = BuildCrosshairSprite();
        _crosshair.color            = _inRangeCrossColor;
        _crosshair.sortingLayerName = "Default";
        _crosshair.sortingOrder     = 12;
        _crosshair.enabled          = false;
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

        // Switch to greyscale when target is out of effective range
        bool outOfRange = Vector2.Distance(new Vector2(muzzle.x, muzzle.y), AimPosition) > EffectiveMaxRange;
        if (outOfRange != _wasOutOfRange) {
            _wasOutOfRange = outOfRange;
            if (_aimLine   != null) _aimLine.colorGradient = outOfRange ? _outOfRangeGrad      : _inRangeGrad;
            if (_crosshair != null) _crosshair.color       = outOfRange ? _outOfRangeCrossColor : _inRangeCrossColor;
        }

        // Scale aim indicator with boss perspective scale so far targets look smaller
        float bossScale = (_boss != null) ? _boss.transform.localScale.x : 1f;
        if (_crosshair != null) {
            float s = _crosshairBaseScale * bossScale;
            _crosshair.transform.localScale = new Vector3(s, s, 1f);
        }
        if (_aimLine != null) {
            _aimLine.startWidth = _lineBaseStartWidth * bossScale;
            _aimLine.endWidth   = _lineBaseEndWidth   * bossScale;
        }

        if (_aimLine != null) {
            _aimLine.enabled = true;
            _aimLine.SetPosition(0, muzzle);
            _aimLine.SetPosition(1, target);
        }
        if (_crosshair != null) {
            _crosshair.enabled            = true;
            _crosshair.transform.position = target;
        }
    }

    // ── Aim logic (unchanged) ─────────────────────────────────────────────────

    private void CleanupUserTarget() {
        if (_userTargetPart != null && !_userTargetPart.IsActive)
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
        if (_boss != null && _boss.IsAlive) {
            if (_config.aimPriority != null) {
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
            // All priority parts destroyed but boss still alive — aim at boss body
            HasTarget      = true;
            _desiredAimPos = _boss.transform.position;
            TargetPartId   = null;
            return;
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
