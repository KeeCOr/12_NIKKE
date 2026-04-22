using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour {
    private Vector3 _originPos;

    void Awake() => _originPos = transform.localPosition;

    public void Shake(float duration, float magnitude) {
        StopAllCoroutines();
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude) {
        float elapsed = 0f;
        while (elapsed < duration) {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = _originPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = _originPos;
    }
}
