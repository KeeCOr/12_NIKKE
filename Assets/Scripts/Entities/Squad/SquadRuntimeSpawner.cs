using UnityEngine;

public class SquadRuntimeSpawner : MonoBehaviour {
    [SerializeField] private SquadMemberConfigSO[] squadConfigs;
    [SerializeField] private Sprite[] characterSprites;
    [SerializeField] private Sprite fallbackSprite;

    private readonly string[] _names = { "Alpha", "Bravo", "Charlie", "Delta", "Echo" };
    private readonly bool[] _flipCharacterX = { false, false, true, false, false };
    private readonly Color[] _fallbackColors = {
        new Color(0.20f, 0.50f, 0.90f),
        new Color(0.90f, 0.50f, 0.15f),
        new Color(0.20f, 0.75f, 0.30f),
        new Color(0.75f, 0.20f, 0.85f),
        new Color(0.10f, 0.85f, 0.85f),
    };

    private SquadMemberController[] _members;
    private AimController[] _aimControllers;

    void Awake() {
        SpawnSquad();
        BindInputManager();
        BindSquadBars();
    }

    private void SpawnSquad() {
        int count = Mathf.Min(5, squadConfigs != null ? squadConfigs.Length : 0);
        _members = new SquadMemberController[count];
        _aimControllers = new AimController[count];

        for (int i = 0; i < count; i++) {
            if (squadConfigs[i] == null) continue;

            var go = new GameObject(i < _names.Length ? _names[i] : squadConfigs[i].label);
            go.transform.position = new Vector3(GameConfig.SQUAD_SLOT_X[i], GameConfig.SQUAD_SLOT_Y[i], 0f);
            go.transform.localScale = new Vector3(1.04f, 1.40f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            var sprite = characterSprites != null && i < characterSprites.Length ? characterSprites[i] : null;
            sr.sprite = sprite != null ? sprite : fallbackSprite;
            sr.color = sprite != null ? Color.white : _fallbackColors[i];
            sr.sortingOrder = 8;
            sr.flipX = i < _flipCharacterX.Length && _flipCharacterX[i];
            if (sprite != null) FitSprite(go, sprite, 1.04f, 3.0f);

            var smc = go.AddComponent<SquadMemberController>();
            var aim = go.AddComponent<AimController>();
            smc.Initialize(squadConfigs[i], sr);

            _members[i] = smc;
            _aimControllers[i] = aim;
        }
    }

    private void BindInputManager() {
        var input = FindObjectOfType<InputManager>();
        if (input != null) {
            input.BindSquad(_members, _aimControllers);
        }
    }

    private void BindSquadBars() {
        if (squadConfigs == null) return;
        for (int i = 0; i < squadConfigs.Length && i < _members.Length; i++) {
            if (squadConfigs[i] == null || _members[i] == null) continue;
            var barRoot = GameObject.Find($"Col_{squadConfigs[i].label.ToUpperInvariant()}");
            var bar = barRoot != null ? barRoot.GetComponent<SquadHpBarUI>() : null;
            if (bar != null) {
                bar.Bind(_members[i]);
            }
        }
    }

    private static void FitSprite(GameObject go, Sprite sprite, float targetW, float targetH) {
        if (sprite == null) return;
        var size = sprite.bounds.size;
        if (size.x <= 0f || size.y <= 0f) return;
        float scale = Mathf.Min(targetW / size.x, targetH / size.y);
        go.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
