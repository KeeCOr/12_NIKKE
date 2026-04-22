using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour {
    [SerializeField] private BossHpBarUI bossHpBar;
    [SerializeField] private SquadHpBarUI[] squadHpBars;   // 5
    [SerializeField] private AmmoDisplayUI[] ammoDisplays; // 5
    [SerializeField] private SquadMemberConfigSO[] squadConfigs;

    void OnEnable() {
        GameEvents.OnBossHpChanged     += bossHpBar.OnBossHpChanged;
        GameEvents.OnBossPartDestroyed += bossHpBar.OnPartDestroyed;
        for (int i = 0; i < squadHpBars.Length; i++) {
            int idx = i;
            string memberId = squadConfigs[i].id;
            GameEvents.OnAmmoChanged    += (id, cur, max) => { if (id == memberId) ammoDisplays[idx].Refresh(cur, max); };
            GameEvents.OnReloadStarted  += id =>             { if (id == memberId) squadHpBars[idx].ShowReload(0f); };
            GameEvents.OnReloadProgress += (id, prog) =>     { if (id == memberId) squadHpBars[idx].ShowReload(prog); };
            GameEvents.OnReloadComplete += id =>             { if (id == memberId) squadHpBars[idx].HideReload(); };
            GameEvents.OnMemberDied     += id =>             { if (id == memberId) squadHpBars[idx].SetDead(); };
        }
        GameEvents.OnWallHpChanged += OnWallHpChanged;
    }

    void OnDisable() {
        GameEvents.OnBossHpChanged     -= bossHpBar.OnBossHpChanged;
        GameEvents.OnBossPartDestroyed -= bossHpBar.OnPartDestroyed;
        GameEvents.OnWallHpChanged     -= OnWallHpChanged;
        // Lambda subscriptions cleared via GameEvents.ClearAllListeners() on scene unload
    }

    private void OnWallHpChanged(float hp, float max) { }
}
