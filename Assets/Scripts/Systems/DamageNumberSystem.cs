using System.Collections.Generic;
using UnityEngine;

public class DamageNumberSystem : MonoBehaviour {
    public static DamageNumberSystem Instance { get; private set; }

    private readonly Queue<DamageNumber> _pool = new();
    private const int POOL_SIZE = 30;

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        for (int i = 0; i < POOL_SIZE; i++)
            _pool.Enqueue(CreateDn());
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    public void Show(float damage, bool isCrit, Vector3 worldPos) {
        DamageNumber dn = _pool.Count > 0 ? _pool.Dequeue() : CreateDn();
        dn.gameObject.SetActive(true);
        dn.Play(damage, isCrit, worldPos, () => {
            dn.gameObject.SetActive(false);
            _pool.Enqueue(dn);
        });
    }

    private DamageNumber CreateDn() {
        var go = new GameObject("DmgNum");
        go.transform.SetParent(transform);

        // WorldSpace canvas — reliable rendering in Unity 2022.3+
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode       = RenderMode.WorldSpace;
        canvas.sortingLayerName = "Default";
        canvas.sortingOrder     = 30;

        var rt       = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 40f);
        rt.localScale = Vector3.one * 0.018f;   // 120 × 0.018 ≈ 2.2 world units wide

        var textGo = new GameObject("T");
        textGo.transform.SetParent(go.transform, false);
        var trt       = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        var txt       = textGo.AddComponent<UnityEngine.UI.Text>();
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = 28;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = Color.white;

        go.SetActive(false);
        return go.AddComponent<DamageNumber>();
    }
}
