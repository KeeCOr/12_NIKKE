using UnityEngine;
using UnityEngine.UI;

public class BombButtonUI : MonoBehaviour {
    [SerializeField] private Button button;
    [SerializeField] private Image  cooldownFill;
    [SerializeField] private Text   cooldownText;

    private BombingSystem  _bombing;
    private BossController _boss;

    void Start() {
        _bombing = FindObjectOfType<BombingSystem>();
        _boss    = FindObjectOfType<BossController>();
        if (button != null) button.onClick.AddListener(OnBombPressed);
    }

    void Update() {
        if (_bombing == null) return;
        bool  ready = _bombing.IsReady;
        float ratio = _bombing.CooldownRatio;

        if (button       != null) button.interactable  = ready;
        if (cooldownFill != null) cooldownFill.fillAmount = ratio;
        if (cooldownText != null)
            cooldownText.text = ready ? "READY" : Mathf.CeilToInt(_bombing.RemainingTime).ToString();
    }

    private void OnBombPressed() {
        if (_boss != null && _bombing != null && _boss.IsAlive)
            _bombing.Activate(_boss.transform.position);
    }
}
