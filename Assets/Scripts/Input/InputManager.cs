using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InputManager : MonoBehaviour {
    public static InputManager Instance { get; private set; }

    [SerializeField] private Camera gameCamera;
    [SerializeField] private SquadMemberController[] squadMembers;
    [SerializeField] private AimController[] aimControllers;
    [SerializeField] private BossController boss;

#if ENABLE_INPUT_SYSTEM
    private InputAction _pointerPosition;
    private InputAction _primaryPress;
#endif
    private int     _selectedMember     = -1;
    private int     _prevSelectedMember = -1; // selection state before the current press
    private bool    _isDragging;
    private bool    _moved;           // true once pointer has moved > threshold after press
    private Vector2 _pressWorldPos;

    void Awake() {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
#if ENABLE_INPUT_SYSTEM
        _pointerPosition = new InputAction("PointerPosition", InputActionType.Value, "<Mouse>/position");
        _pointerPosition.AddBinding("<Touchscreen>/touch0/position");
        _primaryPress    = new InputAction("PrimaryPress",    InputActionType.Button, "<Mouse>/leftButton");
        _primaryPress.AddBinding("<Touchscreen>/touch0/press");
#endif
    }

    void Start() {
        // Self-heal: if aimControllers wasn't wired by SceneBuilder, find them from squadMembers
        bool needsHeal = aimControllers == null || aimControllers.Length == 0;
        if (!needsHeal) {
            for (int i = 0; i < aimControllers.Length; i++)
                if (aimControllers[i] == null) { needsHeal = true; break; }
        }
        if (needsHeal && squadMembers != null && squadMembers.Length > 0) {
            aimControllers = new AimController[squadMembers.Length];
            for (int i = 0; i < squadMembers.Length; i++)
                if (squadMembers[i] != null)
                    aimControllers[i] = squadMembers[i].GetComponent<AimController>();
            Debug.Log("[InputManager] aimControllers auto-populated from squadMembers.");
        }
    }

    void OnEnable() {
#if ENABLE_INPUT_SYSTEM
        _pointerPosition?.Enable();
        _primaryPress?.Enable();
#endif
    }

    void OnDisable() {
#if ENABLE_INPUT_SYSTEM
        _pointerPosition?.Disable();
        _primaryPress?.Disable();
#endif
    }

    void Update() {
#if ENABLE_INPUT_SYSTEM
        Vector2 screenPos = _pointerPosition.ReadValue<Vector2>();
        bool pressed      = _primaryPress.IsPressed();
#else
        Vector2 screenPos = Input.mousePosition;
        bool pressed      = Input.GetMouseButton(0);
#endif
        Vector2 worldPos = gameCamera != null
            ? (Vector2)gameCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -gameCamera.transform.position.z))
            : screenPos;

        // ── Press start ───────────────────────────────────────────────────────
        if (pressed && !_isDragging) {
            _isDragging          = true;
            _moved               = false;
            _pressWorldPos       = worldPos;
            _prevSelectedMember  = _selectedMember; // snapshot before any selection change

            int hit = GetSquadMemberAt(worldPos);
            if (hit >= 0 && hit != _selectedMember) {
                SetSelection(hit);
            } else if (hit < 0) {
                // Click on crosshair indicator → select that character and start dragging immediately
                int aimHit = GetAimIndicatorAt(worldPos);
                if (aimHit >= 0 && aimHit != _selectedMember)
                    SetSelection(aimHit);
            }
        }

        // ── During drag ───────────────────────────────────────────────────────
        if (_isDragging && pressed) {
            if (Vector2.Distance(worldPos, _pressWorldPos) > 0.12f)
                _moved = true;

            if (_selectedMember >= 0 && _selectedMember < aimControllers.Length) {
                aimControllers[_selectedMember].DragTarget = worldPos;

                // OverlapPointAll so stacked colliders (trigger + body) are all checked
                var cols = Physics2D.OverlapPointAll(worldPos);
                bool hitTarget = false;
                foreach (var col in cols) {
                    var part = col.GetComponent<BossPartController>();
                    if (part != null && part.IsActive && !part.IsDestroyed) {
                        aimControllers[_selectedMember].SetUserTargetPart(part);
                        hitTarget = true;
                        break;
                    }
                    var minion = col.GetComponent<MinionController>();
                    if (minion != null && minion.IsAlive) {
                        aimControllers[_selectedMember].SetUserTargetMinion(minion);
                        hitTarget = true;
                        break;
                    }
                }
                // Dragging over empty space — release snap so aim follows the drag freely
                if (!hitTarget)
                    aimControllers[_selectedMember].ClearUserTarget();
            }
        }

        // ── Release ───────────────────────────────────────────────────────────
        if (!pressed && _isDragging) {
            _isDragging = false;
            if (_selectedMember >= 0 && _selectedMember < aimControllers.Length)
                aimControllers[_selectedMember].DragTarget = null;

            // Pure click (no drag movement): toggle off only when tapping the already-selected member.
            // Tapping empty space does NOT deselect — that would break drag workflows.
            if (!_moved) {
                int hit = GetSquadMemberAt(_pressWorldPos);
                if (hit >= 0 && hit == _prevSelectedMember)
                    SetSelection(-1);
            }
        }
    }

    private void SetSelection(int next) {
        // Deselect previous
        if (_selectedMember >= 0 && _selectedMember < squadMembers.Length
            && squadMembers[_selectedMember] != null)
            squadMembers[_selectedMember].SetSelected(false);

        _selectedMember = next;

        // Highlight new selection
        if (_selectedMember >= 0 && _selectedMember < squadMembers.Length
            && squadMembers[_selectedMember] != null)
            squadMembers[_selectedMember].SetSelected(true);
    }

    private int GetSquadMemberAt(Vector2 worldPos) {
        int   best = -1;
        float bestDist = 0.55f;  // radius threshold
        for (int i = 0; i < squadMembers.Length; i++) {
            if (squadMembers[i] == null) continue;
            float d = Vector2.Distance(worldPos, squadMembers[i].transform.position);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    // Returns index of the character whose crosshair indicator is nearest to worldPos
    private int GetAimIndicatorAt(Vector2 worldPos) {
        if (aimControllers == null) return -1;
        int   best = -1;
        float bestDist = 0.45f;  // click radius around crosshair ring
        for (int i = 0; i < aimControllers.Length; i++) {
            if (aimControllers[i] == null) continue;
            if (squadMembers != null && i < squadMembers.Length
                && (squadMembers[i] == null || !squadMembers[i].IsAlive)) continue;
            float d = Vector2.Distance(worldPos, aimControllers[i].AimPosition);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }
}
