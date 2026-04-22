using UnityEngine;
using UnityEngine.UI;

public class SquadHpBarUI : MonoBehaviour {
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider reloadSlider;
    [SerializeField] private SquadMemberController member;

    void Update() {
        if (member == null) return;
        if (hpSlider != null) hpSlider.value = member.GetHpRatio();
    }

    public void ShowReload(float progress) {
        if (reloadSlider != null) { reloadSlider.gameObject.SetActive(true); reloadSlider.value = progress; }
    }

    public void HideReload() {
        if (reloadSlider != null) reloadSlider.gameObject.SetActive(false);
    }

    public void SetDead() {
        if (hpSlider != null) hpSlider.value = 0f;
        var cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0.4f;
    }
}
