using UnityEngine;
using UnityEngine.UI;

public class BossHpBarUI : MonoBehaviour {
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Text hpText;
    [SerializeField] private Image[] partIcons; // 7, order: HEAD ARM_L ARM_R LEG_L LEG_R CHEST CORE
    [SerializeField] private Color destroyedColor = new Color(0.3f, 0f, 0f);

    private static readonly string[] PART_ORDER = { "HEAD","ARM_L","ARM_R","LEG_L","LEG_R","CHEST","CORE" };

    public void OnBossHpChanged(float hp, float maxHp) {
        if (hpSlider != null) hpSlider.value = hp / maxHp;
        if (hpText   != null) hpText.text = $"BOSS  {Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(maxHp)}";
    }

    public void OnPartDestroyed(string partId) {
        for (int i = 0; i < PART_ORDER.Length; i++) {
            if (PART_ORDER[i] == partId && i < partIcons.Length && partIcons[i] != null)
                partIcons[i].color = destroyedColor;
        }
    }
}
