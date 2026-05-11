using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    [SerializeField] private BossHpBarUI bossHpBar;
    [SerializeField] private SquadHpBarUI[] squadHpBars;
    [SerializeField] private AmmoDisplayUI[] ammoDisplays;
    [SerializeField] private SquadMemberConfigSO[] squadConfigs;

    [SerializeField] private Image endOverlayBg;
    [SerializeField] private Text  endOverlayText;

    private System.Action<string, int, int>[] _ammoHandlers;
    private System.Action<string>[]           _reloadStartHandlers;
    private System.Action<string, float>[]    _reloadProgressHandlers;
    private System.Action<string>[]           _reloadCompleteHandlers;
    private System.Action<string>[]           _memberDiedHandlers;

    void OnEnable() {
        GameEvents.OnGameEnded += ShowEndOverlay;
        if (bossHpBar != null) {
            GameEvents.OnBossHpChanged     += bossHpBar.OnBossHpChanged;
            GameEvents.OnBossPartDestroyed += bossHpBar.OnPartDestroyed;
        }

        int count = 0;
        if (squadHpBars != null && ammoDisplays != null && squadConfigs != null)
            count = Mathf.Min(squadHpBars.Length, Mathf.Min(ammoDisplays.Length, squadConfigs.Length));

        _ammoHandlers           = new System.Action<string, int, int>[count];
        _reloadStartHandlers    = new System.Action<string>[count];
        _reloadProgressHandlers = new System.Action<string, float>[count];
        _reloadCompleteHandlers = new System.Action<string>[count];
        _memberDiedHandlers     = new System.Action<string>[count];

        for (int i = 0; i < count; i++) {
            if (squadConfigs[i] == null) continue;
            int    idx      = i;
            string memberId = squadConfigs[i].id;

            _ammoHandlers[idx]           = (id, cur, max) => { if (id == memberId && ammoDisplays[idx] != null) ammoDisplays[idx].Refresh(cur, max); };
            _reloadStartHandlers[idx]    = id =>             { if (id == memberId && squadHpBars[idx] != null)  squadHpBars[idx].ShowReload(0f); };
            _reloadProgressHandlers[idx] = (id, prog) =>     { if (id == memberId && squadHpBars[idx] != null)  squadHpBars[idx].ShowReload(prog); };
            _reloadCompleteHandlers[idx] = id =>             { if (id == memberId && squadHpBars[idx] != null)  squadHpBars[idx].HideReload(); };
            _memberDiedHandlers[idx]     = id =>             { if (id == memberId && squadHpBars[idx] != null)  squadHpBars[idx].SetDead(); };

            GameEvents.OnAmmoChanged    += _ammoHandlers[idx];
            GameEvents.OnReloadStarted  += _reloadStartHandlers[idx];
            GameEvents.OnReloadProgress += _reloadProgressHandlers[idx];
            GameEvents.OnReloadComplete += _reloadCompleteHandlers[idx];
            GameEvents.OnMemberDied     += _memberDiedHandlers[idx];
        }
    }

    private void ShowEndOverlay(bool isWin) {
        if (endOverlayBg != null) {
            endOverlayBg.gameObject.SetActive(true);
            endOverlayBg.color = isWin
                ? new Color(0.02f, 0.18f, 0.04f, 0.82f)
                : new Color(0.22f, 0.02f, 0.02f, 0.82f);
        }
        if (endOverlayText != null) {
            endOverlayText.gameObject.SetActive(true);
            endOverlayText.text  = isWin ? "VICTORY!" : "DEFEAT...";
            endOverlayText.color = isWin
                ? new Color(1.0f, 0.92f, 0.20f)
                : new Color(1.0f, 0.32f, 0.28f);
        }
    }

    void OnDisable() {
        GameEvents.OnGameEnded -= ShowEndOverlay;
        if (bossHpBar != null) {
            GameEvents.OnBossHpChanged     -= bossHpBar.OnBossHpChanged;
            GameEvents.OnBossPartDestroyed -= bossHpBar.OnPartDestroyed;
        }

        if (_ammoHandlers != null) {
            for (int i = 0; i < _ammoHandlers.Length; i++) {
                if (_ammoHandlers[i]           != null) GameEvents.OnAmmoChanged    -= _ammoHandlers[i];
                if (_reloadStartHandlers[i]    != null) GameEvents.OnReloadStarted  -= _reloadStartHandlers[i];
                if (_reloadProgressHandlers[i] != null) GameEvents.OnReloadProgress -= _reloadProgressHandlers[i];
                if (_reloadCompleteHandlers[i] != null) GameEvents.OnReloadComplete -= _reloadCompleteHandlers[i];
                if (_memberDiedHandlers[i]     != null) GameEvents.OnMemberDied     -= _memberDiedHandlers[i];
            }
        }
    }
}
