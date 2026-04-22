using UnityEngine;

public class AimController : MonoBehaviour {
    // config is sourced from sibling SquadMemberController to avoid duplicate Inspector slot
    private SquadMemberConfigSO _config;

    public bool    HasTarget    { get; private set; }
    public Vector2 AimPosition  { get; private set; }
    public string  TargetPartId { get; private set; }

    private BossController _boss;
    private BossPartController _userTargetPart;
    private MinionController   _userTargetMinion;

    private Vector2 _desiredAimPos; // resolved world target; AimPosition smoothly tracks this

    // InputManager가 드래그 타겟 설정
    public Vector2? DragTarget { get; set; }

    void Start() {
        var owner = GetComponent<SquadMemberController>();
        if (owner == null || owner.config == null) {
            Debug.LogError($"[AimController] SquadMemberController or its config is missing on {gameObject.name}. Disabling.", this);
            enabled = false;
            return;
        }
        _config = owner.config;
        _boss   = FindObjectOfType<BossController>();
        AimPosition = _desiredAimPos = new Vector2(GameConfig.BOSS_START_X * 0.7f, GameConfig.BOSS_Y);
    }

    void Update() {
        CleanupUserTarget();
        ResolveTarget();
        SmoothAim();
    }

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
        // 자동 조준: aimPriority 순서로 활성 파트 탐색
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
            // Keep _desiredAimPos in sync so we return to the tracked target smoothly on drag release
            AimPosition = DragTarget.Value;
            return;
        }
        if (HasTarget) {
            AimPosition = Vector2.MoveTowards(AimPosition, _desiredAimPos, 5f * Time.deltaTime);
        }
    }

    public void SetUserTargetPart(BossPartController part)   { _userTargetPart = part; _userTargetMinion = null; }
    public void SetUserTargetMinion(MinionController minion) { _userTargetMinion = minion; _userTargetPart = null; }
    public void ClearUserTarget() { _userTargetPart = null; _userTargetMinion = null; }
}
