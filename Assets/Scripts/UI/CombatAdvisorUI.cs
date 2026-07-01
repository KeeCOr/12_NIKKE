using UnityEngine;
using UnityEngine.UI;

public class CombatAdvisorUI : MonoBehaviour {
    [SerializeField] private Text bodyText;
    [SerializeField] private Image accentImage;
    [SerializeField] private SquadMemberConfigSO[] squadConfigs;

    private string _bossTip = CombatAdvisorLogic.DefaultBossTip;
    private string _compositionPreview;
    private bool _hasCombatSignal;
    private int _reloadingCount;
    private int _downCount;
    private float _bossHpRatio = 1f;
    private float _wallHpRatio = 1f;
    private float _alertUntil;
    private string _alertText;

    public void Bind(Text text, Image accent) {
        bodyText = text;
        accentImage = accent;
        Refresh();
    }

    public void BindSquadComposition(SquadMemberConfigSO[] configs) {
        squadConfigs = configs;
        _compositionPreview = CombatAdvisorLogic.GetSquadCompositionPreview(squadConfigs).ToHudLine();
        Refresh();
    }

    void OnEnable() {
        GameEvents.OnBossHpChanged += HandleBossHpChanged;
        GameEvents.OnBossPartHpChanged += HandleBossPartHudChanged;
        GameEvents.OnWallHpChanged += HandleWallHpChanged;
        GameEvents.OnReloadStarted += HandleReloadStarted;
        GameEvents.OnReloadComplete += HandleReloadComplete;
        GameEvents.OnMemberDied += HandleMemberDied;
        GameEvents.OnBossEnraged += HandleBossEnraged;
        GameEvents.OnGameEnded += HandleGameEnded;
        if (string.IsNullOrEmpty(_compositionPreview) && squadConfigs != null && squadConfigs.Length > 0) {
            _compositionPreview = CombatAdvisorLogic.GetSquadCompositionPreview(squadConfigs).ToHudLine();
        }
        Refresh();
    }

    void OnDisable() {
        GameEvents.OnBossHpChanged -= HandleBossHpChanged;
        GameEvents.OnBossPartHpChanged -= HandleBossPartHudChanged;
        GameEvents.OnWallHpChanged -= HandleWallHpChanged;
        GameEvents.OnReloadStarted -= HandleReloadStarted;
        GameEvents.OnReloadComplete -= HandleReloadComplete;
        GameEvents.OnMemberDied -= HandleMemberDied;
        GameEvents.OnBossEnraged -= HandleBossEnraged;
        GameEvents.OnGameEnded -= HandleGameEnded;
    }

    void Update() {
        if (!string.IsNullOrEmpty(_alertText) && Time.time > _alertUntil) {
            _alertText = null;
            Refresh();
        }
    }

    private void HandleBossHpChanged(float hp, float maxHp) {
        _hasCombatSignal = true;
        _bossHpRatio = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
    }

    private void HandleWallHpChanged(float hp, float maxHp) {
        _hasCombatSignal = true;
        _wallHpRatio = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
    }

    private void HandleBossPartHudChanged(BossPartHudState[] states) {
        _hasCombatSignal = true;
        _bossTip = CombatAdvisorLogic.GetBossPartTip(states);
        Refresh();
    }

    private void HandleReloadStarted(string memberId) {
        _hasCombatSignal = true;
        _reloadingCount++;
        Refresh();
    }

    private void HandleReloadComplete(string memberId) {
        _hasCombatSignal = true;
        _reloadingCount = Mathf.Max(0, _reloadingCount - 1);
        Refresh();
    }

    private void HandleMemberDied(string memberId) {
        _hasCombatSignal = true;
        _downCount++;
        ShowAlert($"{memberId.ToUpperInvariant()} down - protect the wall!", 3.0f, new Color(1.0f, 0.25f, 0.18f));
    }

    private void HandleBossEnraged() {
        _hasCombatSignal = true;
        ShowAlert("Boss enraged - expect shockwaves!", 3.0f, new Color(1.0f, 0.22f, 0.10f));
    }

    private void HandleGameEnded(bool isWin) {
        _hasCombatSignal = true;
        WaveResultSummary summary = CombatAdvisorLogic.GetWaveResultSummary(isWin, _bossHpRatio, _wallHpRatio, _downCount, _reloadingCount);
        ShowAlert($"{summary.title} {summary.nextAdjustment}", 10f,
            isWin ? new Color(1.0f, 0.85f, 0.18f) : new Color(1.0f, 0.25f, 0.18f));
    }

    private void ShowAlert(string text, float seconds, Color accent) {
        _alertText = text;
        _alertUntil = Time.time + seconds;
        if (accentImage != null) accentImage.color = accent;
        Refresh();
    }

    private void Refresh() {
        if (bodyText == null) return;
        string squadTip = CombatAdvisorLogic.GetSquadTip(_reloadingCount, _downCount);
        if (!string.IsNullOrEmpty(_alertText)) {
            bodyText.text = _alertText;
        } else if (!_hasCombatSignal && !string.IsNullOrEmpty(_compositionPreview)) {
            bodyText.text = _compositionPreview;
        } else {
            bodyText.text = $"{_bossTip}  |  {squadTip}";
        }

        if (accentImage != null && string.IsNullOrEmpty(_alertText)) {
            accentImage.color = _reloadingCount >= 2 || _downCount > 0
                ? new Color(1.0f, 0.55f, 0.10f, 0.90f)
                : new Color(0.35f, 0.70f, 1.0f, 0.90f);
        }
    }
}