using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultUI : MonoBehaviour {
    [SerializeField] private Text   resultText;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button menuBtn;

    void Start() {
        bool isWin = GameManager.Instance?.State == GameManager.GameState.Win;
        if (resultText != null) resultText.text = isWin ? "MISSION COMPLETE" : "MISSION FAILED";
        if (retryBtn != null) retryBtn.onClick.AddListener(OnRetryClicked);
        if (menuBtn  != null) menuBtn.onClick.AddListener(OnMenuClicked);
    }

    public void OnRetryClicked() => SceneManager.LoadScene("Game");
    public void OnMenuClicked()  => SceneManager.LoadScene("Game");
}
