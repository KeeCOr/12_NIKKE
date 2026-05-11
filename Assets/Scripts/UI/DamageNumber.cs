using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DamageNumber : MonoBehaviour {
    private Text    _text;
    private Action  _onDone;
    private Vector3 _baseScale;

    void Awake() {
        _text      = GetComponentInChildren<Text>();
        _baseScale = transform.localScale;
    }

    public void Play(float damage, bool isCrit, Vector3 worldPos, Action onDone) {
        float jitter = UnityEngine.Random.Range(-0.20f, 0.20f);
        transform.position = worldPos + new Vector3(jitter, 0f, -0.5f);
        _onDone = onDone;

        int rounded = Mathf.RoundToInt(damage);
        if (isCrit) {
            _text.text     = $"{rounded}!";
            _text.fontSize = 38;
            _text.color    = new Color(1f, 0.22f, 0.04f, 1f);
        } else {
            _text.text     = rounded.ToString();
            _text.fontSize = 28;
            _text.color    = new Color(1f, 0.95f, 0.65f, 1f);
        }

        StopAllCoroutines();
        StartCoroutine(Animate(isCrit));
    }

    private IEnumerator Animate(bool isCrit) {
        float   dur      = isCrit ? 1.0f : 0.70f;
        float   riseAmt  = isCrit ? 1.6f : 1.1f;
        Vector3 start    = transform.position;
        Color   col      = _text.color;
        float   t        = 0f;

        // brief pop scale for crits
        if (isCrit) {
            transform.localScale = _baseScale * 1.4f;
            yield return null;
            transform.localScale = _baseScale;
        }

        while (t < dur) {
            t += Time.deltaTime;
            float frac = t / dur;
            transform.position = start + Vector3.up * (riseAmt * frac);
            col.a = Mathf.Clamp01(1f - frac * frac);   // quadratic fade
            _text.color = col;
            yield return null;
        }

        gameObject.SetActive(false);
        _onDone?.Invoke();
    }
}
