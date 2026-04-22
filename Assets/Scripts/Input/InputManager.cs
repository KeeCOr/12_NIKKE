using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour {
    public static InputManager Instance { get; private set; }

    [SerializeField] private Camera gameCamera;
    [SerializeField] private SquadMemberController[] squadMembers;
    [SerializeField] private AimController[] aimControllers;
    [SerializeField] private BossController boss;

    private InputActions _actions;
    private int _selectedMember = -1;
    private bool _isDragging;
    private Vector2 _dragStartScreen;

    void Awake() {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _actions = new InputActions();
    }

    void OnEnable()  => _actions.Gameplay.Enable();
    void OnDisable() => _actions.Gameplay.Disable();

    void Update() {
        Vector2 screenPos = _actions.Gameplay.PointerPosition.ReadValue<Vector2>();
        Vector2 worldPos  = gameCamera.ScreenToWorldPoint(screenPos);

        bool pressed = _actions.Gameplay.PrimaryPress.IsPressed();

        if (pressed && !_isDragging) {
            int hit = GetSquadMemberAt(worldPos);
            if (hit >= 0) {
                _selectedMember = (_selectedMember == hit) ? -1 : hit;
                return;
            }
            _isDragging = true;
            _dragStartScreen = screenPos;
        }

        if (_isDragging && pressed && _selectedMember >= 0) {
            aimControllers[_selectedMember].DragTarget = worldPos;
            var hit2D = Physics2D.OverlapPoint(worldPos);
            if (hit2D != null) {
                var part = hit2D.GetComponent<BossPartController>();
                if (part != null) aimControllers[_selectedMember].SetUserTargetPart(part);
                var minion = hit2D.GetComponent<MinionController>();
                if (minion != null) aimControllers[_selectedMember].SetUserTargetMinion(minion);
            }
        }

        if (!pressed && _isDragging) {
            _isDragging = false;
            if (_selectedMember >= 0) aimControllers[_selectedMember].DragTarget = null;
        }
    }

    private int GetSquadMemberAt(Vector2 worldPos) {
        for (int i = 0; i < squadMembers.Length; i++) {
            float dist = Vector2.Distance(worldPos, squadMembers[i].transform.position);
            if (dist < 0.4f) return i;
        }
        return -1;
    }
}
