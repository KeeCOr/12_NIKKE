using UnityEngine;
using UnityEngine.UI;

public class WaveInfoUI : MonoBehaviour {
    [SerializeField] private Text waveText;

    private float _elapsed;
    private bool  _enraged;

    void OnEnable()  => GameEvents.OnBossEnraged += HandleEnrage;
    void OnDisable() => GameEvents.OnBossEnraged -= HandleEnrage;

    private void HandleEnrage() => _enraged = true;

    void Update() {
        if (waveText == null) return;
        if (_enraged) { waveText.text = "! ENRAGED !"; return; }
        _elapsed += Time.deltaTime;
        int phase = _elapsed < 10f ? 1
                  : _elapsed < 20f ? 2
                  : _elapsed < 30f ? 3
                  : _elapsed < 45f ? 4
                  : _elapsed < 60f ? 5 : 6;
        waveText.text = $"WAVE  {phase}";
    }
}
