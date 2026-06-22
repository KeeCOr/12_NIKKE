using UnityEngine;
using UnityEngine.UI;

public class BossHpBarUI : MonoBehaviour {
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Text hpText;
    [SerializeField] private Image[] partIcons; // 7, order: HEAD ARM_L ARM_R LEG_L LEG_R CHEST CORE
    [SerializeField] private Slider[] partHpSliders;
    [SerializeField] private Text[] partHpTexts;
    [SerializeField] private Color healthyColor = new Color(0.95f, 0.65f, 0.18f);
    [SerializeField] private Color inactiveColor = new Color(0.18f, 0.18f, 0.22f, 0.65f);
    [SerializeField] private Color destroyedColor = new Color(0.3f, 0f, 0f);

    private static readonly string[] PART_ORDER = { "HEAD","ARM_L","ARM_R","LEG_L","LEG_R","CHEST","CORE" };

    public void OnBossHpChanged(float hp, float maxHp) {
        if (hpSlider != null) hpSlider.value = maxHp > 0f ? hp / maxHp : 0f;
        if (hpText   != null) hpText.text = $"BOSS  {Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(maxHp)}";
    }

    public void OnBossPartHpChanged(BossPartHudState[] states) {
        if (states == null) return;
        foreach (var state in states) {
            int index = IndexOfPart(state.partId);
            if (index < 0) continue;

            if (partHpSliders != null && index < partHpSliders.Length && partHpSliders[index] != null) {
                partHpSliders[index].gameObject.SetActive(state.isActive || state.isDestroyed);
                partHpSliders[index].value = state.HpRatio;
            }

            if (partHpTexts != null && index < partHpTexts.Length && partHpTexts[index] != null)
                partHpTexts[index].text = FormatPartHp(state);

            if (partIcons != null && index < partIcons.Length && partIcons[index] != null)
                partIcons[index].color = state.isDestroyed ? destroyedColor : state.isActive ? healthyColor : inactiveColor;
        }
    }

    public void OnPartDestroyed(string partId) {
        int index = IndexOfPart(partId);
        if (index >= 0 && partIcons != null && index < partIcons.Length && partIcons[index] != null)
            partIcons[index].color = destroyedColor;
    }

    private static string FormatPartHp(BossPartHudState state) {
        if (!state.isActive && !state.isDestroyed) return "OFF";
        if (state.isDestroyed) return "BREAK";
        return $"{Mathf.CeilToInt(state.HpRatio * 100f)}%";
    }

    private static int IndexOfPart(string partId) {
        for (int i = 0; i < PART_ORDER.Length; i++)
            if (PART_ORDER[i] == partId) return i;
        return -1;
    }
}
