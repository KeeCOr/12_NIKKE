using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Dockable panel with a one-click rebuild button.
/// Open via  SquadVsMonster > Refresh Panel  or press Ctrl+Alt+R.
/// </summary>
public class RefreshPanel : EditorWindow {

    // ── State ────────────────────────────────────────────────────────────────
    private string _lastResult  = "";
    private bool   _lastSuccess = true;
    private double _finishTime  = -10.0;

    private static readonly Color GreenBtn  = new Color(0.25f, 0.70f, 0.30f);
    private static readonly Color RedText   = new Color(1.0f,  0.35f, 0.30f);
    private static readonly Color GreenText = new Color(0.35f, 0.90f, 0.45f);
    private static readonly Color FadeGray  = new Color(0.55f, 0.55f, 0.55f);

    // ── Open ─────────────────────────────────────────────────────────────────
    [MenuItem("SquadVsMonster/Refresh Panel", false, 0)]
    public static void Open() {
        var w = GetWindow<RefreshPanel>(false, "Refresh", true);
        w.minSize = new Vector2(240f, 160f);
        w.Show();
    }

    // ── GUI ──────────────────────────────────────────────────────────────────
    void OnGUI() {
        EditorGUILayout.Space(10);

        // ── Big rebuild button ────────────────────────────────────────────
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = GreenBtn;
        var btnStyle = new GUIStyle(GUI.skin.button) {
            fontSize  = 16,
            fontStyle = FontStyle.Bold,
        };
        if (GUILayout.Button("REBUILD ALL SCENES", btnStyle, GUILayout.Height(52))) {
            Run();
        }
        GUI.backgroundColor = prevBg;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Ctrl + Alt + R", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(10);

        // ── Shortcut reminder ─────────────────────────────────────────────
        using (new EditorGUILayout.HorizontalScope()) {
            if (GUILayout.Button("Game Scene",   GUILayout.Height(26))) RunGame();
            if (GUILayout.Button("Result Scene", GUILayout.Height(26))) RunResult();
        }

        EditorGUILayout.Space(10);

        // ── Result banner ─────────────────────────────────────────────────
        if (!string.IsNullOrEmpty(_lastResult)) {
            double age = EditorApplication.timeSinceStartup - _finishTime;
            float  t   = Mathf.Clamp01((float)((age - 2.0) / 3.0));  // fade after 2 s over 3 s

            Color textCol = _lastSuccess ? Color.Lerp(GreenText, FadeGray, t)
                                         : Color.Lerp(RedText,   FadeGray, t);
            var resultStyle = new GUIStyle(EditorStyles.boldLabel) {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 13,
                normal    = { textColor = textCol }
            };
            EditorGUILayout.LabelField(_lastResult, resultStyle);

            if (t < 1f) Repaint();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void Run() {
        try {
            SceneBuilder.BuildAll();
            Finish(true, "Build complete");
        } catch (System.Exception e) {
            Finish(false, $"Error: {e.Message}");
        }
    }

    private void RunGame() {
        try {
            SceneBuilder.Build();
            Finish(true, "Game scene built");
        } catch (System.Exception e) {
            Finish(false, $"Error: {e.Message}");
        }
    }

    private void RunResult() {
        try {
            SceneBuilder.BuildResult();
            Finish(true, "Result scene built");
        } catch (System.Exception e) {
            Finish(false, $"Error: {e.Message}");
        }
    }

    private void Finish(bool success, string msg) {
        _lastSuccess = success;
        _lastResult  = msg;
        _finishTime  = EditorApplication.timeSinceStartup;
        Repaint();
    }
}
