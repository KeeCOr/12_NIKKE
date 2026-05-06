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
    private int    _selectedMember = -1;
    private bool   _isDragging;

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

        if (pressed && !_isDragging) {
            int hit = GetSquadMemberAt(worldPos);
            if (hit >= 0) {
                int next = (_selectedMember == hit) ? -1 : hit;
                SetSelection(next);
                return;
            }
            _isDragging = true;
        }

        if (_isDragging && pressed && _selectedMember >= 0 && _selectedMember < aimControllers.Length) {
            aimControllers[_selectedMember].DragTarget = worldPos;
            var hit2D = Physics2D.OverlapPoint(worldPos);
            if (hit2D != null) {
                var part   = hit2D.GetComponent<BossPartController>();
                if (part   != null) aimControllers[_selectedMember].SetUserTargetPart(part);
                var minion = hit2D.GetComponent<MinionController>();
                if (minion != null) aimControllers[_selectedMember].SetUserTargetMinion(minion);
            }
        }

        if (!pressed && _isDragging) {
            _isDragging = false;
            if (_selectedMember >= 0 && _selectedMember < aimControllers.Length)
                aimControllers[_selectedMember].DragTarget = null;
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
