using UnityEngine;
using UnityEngine.UI;

public class WallHpBarUI : MonoBehaviour {
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Text   hpText;

    void OnEnable()  => GameEvents.OnWallHpChanged += Refresh;
    void OnDisable() => GameEvents.OnWallHpChanged -= Refresh;

    private void Refresh(float hp, float max) {
        if (hpSlider != null) hpSlider.value = Mathf.Clamp01(hp / max);
        if (hpText   != null) hpText.text    = $"WALL  {Mathf.CeilToInt(hp)}";
    }
}
