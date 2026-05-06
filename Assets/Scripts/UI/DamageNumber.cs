using System;
using System.Collections;
using UnityEngine;

public class DamageNumber : MonoBehaviour {
    private TextMesh _mesh;
    private Action   _onDone;

    void Awake() {
        _mesh = GetComponent<TextMesh>();
    }

    public void Play(float damage, bool isCrit, Vector3 worldPos, Action onDone) {
        float jitter = UnityEngine.Random.Range(-0.20f, 0.20f);
        transform.position = worldPos + new Vector3(jitter, 0f, -0.5f);
        _onDone = onDone;

        int rounded = Mathf.RoundToInt(damage);
        if (isCrit) {
            _mesh.text      = $"{rounded}!";
            _mesh.fontSize  = 42;
            _mesh.color     = new Color(1f, 0.22f, 0.04f, 1f);
            _mesh.characterSize = 0.07f;
        } else {
            _mesh.text      = rounded.ToString();
            _mesh.fontSize  = 30;
            _mesh.color     = new Color(1f, 0.95f, 0.65f, 1f);
            _mesh.characterSize = 0.06f;
        }

        StopAllCoroutines();
        StartCoroutine(Animate(isCrit));
    }

    private IEnumerator Animate(bool isCrit) {
        float dur      = isCrit ? 1.0f : 0.70f;
        float riseAmt  = isCrit ? 1.6f : 1.1f;
        Vector3 start  = transform.position;
        Color   col    = _mesh.color;
        float   t      = 0f;

        // brief pop scale for crits
        if (isCrit) {
            transform.localScale = new Vector3(1.4f, 1.4f, 1f);
            yield return null;
            transform.localScale = Vector3.one;
        }

        while (t < dur) {
            t += Time.deltaTime;
            float frac = t / dur;
            transform.position = start + Vector3.up * (riseAmt * frac);
            col.a = Mathf.Clamp01(1f - frac * frac);   // quadratic fade
            _mesh.color = col;
            yield return null;
        }

        gameObject.SetActive(false);
        _onDone?.Invoke();
    }
}
