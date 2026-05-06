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
    private int     _selectedMember  = -1;
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
            _isDragging    = true;
            _moved         = false;
            _pressWorldPos = worldPos;

            int hit = GetSquadMemberAt(worldPos);
            // Select a different member; selecting the same member waits for click-release
            if (hit >= 0 && hit != _selectedMember)
                SetSelection(hit);
        }

        // ── During drag ───────────────────────────────────────────────────────
        if (_isDragging && pressed) {
            if (Vector2.Distance(worldPos, _pressWorldPos) > 0.12f)
                _moved = true;

            if (_selectedMember >= 0 && _selectedMember < aimControllers.Length) {
                aimControllers[_selectedMember].DragTarget = worldPos;

                // OverlapPointAll so stacked colliders (trigger + body) are all checked
                var cols = Physics2D.OverlapPointAll(worldPos);
                foreach (var col in cols) {
                    var part = col.GetComponent<BossPartController>();
                    if (part != null && part.IsActive && !part.IsDestroyed) {
                        aimControllers[_selectedMember].SetUserTargetPart(part);
                        break;
                    }
                    var minion = col.GetComponent<MinionController>();
                    if (minion != null && minion.IsAlive) {
                        aimControllers[_selectedMember].SetUserTargetMinion(minion);
                        break;
                    }
                }
            }
        }

        // ── Release ───────────────────────────────────────────────────────────
        if (!pressed && _isDragging) {
            _isDragging = false;
            if (_selectedMember >= 0 && _selectedMember < aimControllers.Length)
                aimControllers[_selectedMember].DragTarget = null;

            // Pure click (no movement): deselect if tapping same member or empty area
            if (!_moved) {
                int hit = GetSquadMemberAt(_pressWorldPos);
                if (hit < 0 || hit == _selectedMember)
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
        for (int i = 0; i < squadMembers.Length; i++) {
            if (squadMembers[i] == null) continue;
            if (Vector2.Distance(worldPos, squadMembers[i].transform.position) < 0.4f) return i;
        }
        return -1;
    }
}
