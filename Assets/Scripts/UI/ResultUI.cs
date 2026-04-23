using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultUI : MonoBehaviour {
    [SerializeField] private Text resultText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    void Start() {
        bool isWin = GameManager.Instance?.State == GameManager.GameState.Win;
        if (winPanel  != null) winPanel.SetActive(isWin);
        if (losePanel != null) losePanel.SetActive(!isWin);
        if (resultText != null) resultText.text = isWin ? "MISSION COMPLETE" : "MISSION FAILED";
    }

    public void OnRetryClicked() => SceneManager.LoadScene("Game");
    public void OnMenuClicked()  => SceneManager.LoadScene("Boot");
}
