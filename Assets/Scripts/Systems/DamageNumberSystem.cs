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

        var tm = go.AddComponent<TextMesh>();
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.fontSize      = 30;
        tm.characterSize = 0.06f;
        tm.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Render in front of everything else
        var mr = go.GetComponent<MeshRenderer>();
        mr.sortingLayerName = "Default";
        mr.sortingOrder     = 30;

        go.SetActive(false);
        return go.AddComponent<DamageNumber>();
    }
}
