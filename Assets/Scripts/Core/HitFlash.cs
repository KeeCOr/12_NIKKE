using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to any GameObject with a SpriteRenderer.
/// Call Flash() to briefly tint the sprite white, then restore the original color.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class HitFlash : MonoBehaviour {
    [SerializeField] private float flashDuration = 0.075f;

    private SpriteRenderer _sr;
    private Color          _baseColor;
    private Coroutine      _co;

    void Awake() {
        _sr        = GetComponent<SpriteRenderer>();
        _baseColor = _sr.color;
    }

    public void Flash() {
        if (_sr == null) return;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(DoFlash());
    }

    /// Call this when the object's color legitimately changes (e.g. damage tint),
    /// so subsequent flashes restore the new color rather than the stale cached one.
    public void RefreshBaseColor() {
        if (_sr != null) _baseColor = _sr.color;
    }

    private IEnumerator DoFlash() {
        _sr.color = Color.white;
        yield return new WaitForSeconds(flashDuration);
        _sr.color = _baseColor;
        _co = null;
    }
}
