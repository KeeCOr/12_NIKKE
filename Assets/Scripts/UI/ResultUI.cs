using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultUI : MonoBehaviour {
    [SerializeField] private Text   resultText;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button menuBtn;
    [SerializeField] private Image  panelBg;
    [SerializeField] private Image  resultBg;

    void Start() {
        bool isWin = GameManager.Instance?.State == GameManager.GameState.Win;

        if (resultText != null) {
            resultText.text  = isWin ? "VICTORY!"  : "DEFEAT...";
            resultText.color = isWin
                ? new Color(1.0f, 0.92f, 0.15f)   // gold
                : new Color(1.0f, 0.30f, 0.25f);  // red
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
