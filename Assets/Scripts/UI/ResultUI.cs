using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultUI : MonoBehaviour {
    [SerializeField] private Text   resultText;
    [SerializeField] private Text   reasonText;
    [SerializeField] private Text   nextAdjustmentText;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button menuBtn;
    [SerializeField] private Image  panelBg;
    [SerializeField] private Image  resultBg;

    void Start() {
        bool isWin = GameManager.Instance?.State == GameManager.GameState.Win;
        WaveResultSummary summary = GameManager.Instance != null
            ? GameManager.Instance.LastWaveResultSummary
            : CombatAdvisorLogic.GetWaveResultSummary(isWin, isWin ? 0f : 1f, isWin ? 0.8f : 0.2f, 0, 0);

        if (resultText != null) {
            resultText.text  = string.IsNullOrEmpty(summary.title) ? (isWin ? "VICTORY!" : "DEFEAT...") : summary.title;
            resultText.color = isWin
                ? new Color(1.0f, 0.92f, 0.15f)   // gold
                : new Color(1.0f, 0.30f, 0.25f);  // red
        }
        if (reasonText != null) {
            reasonText.text = summary.reason;
            reasonText.color = new Color(0.84f, 0.92f, 1.0f, 0.95f);
        }
        if (nextAdjustmentText != null) {
            nextAdjustmentText.text = summary.nextAdjustment;
            nextAdjustmentText.color = isWin
                ? new Color(0.95f, 0.86f, 0.40f, 1f)
                : new Color(1.0f, 0.58f, 0.38f, 1f);
        }
        if (panelBg != null)
            panelBg.color = isWin
                ? new Color(0.04f, 0.12f, 0.04f, 0.97f)
                : new Color(0.14f, 0.03f, 0.03f, 0.97f);
        if (resultBg != null)
            resultBg.color = isWin
                ? new Color(0.04f, 0.05f, 0.10f, 1f)
                : new Color(0.10f, 0.02f, 0.02f, 1f);

        if (retryBtn != null) retryBtn.onClick.AddListener(OnRetryClicked);
        if (menuBtn  != null) menuBtn.onClick.AddListener(OnMenuClicked);
    }

    public void OnRetryClicked() => SceneManager.LoadScene("Game");
    public void OnMenuClicked()  => SceneManager.LoadScene("Game");
}
