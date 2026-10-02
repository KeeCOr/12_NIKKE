using UnityEngine;
using UnityEngine.UI;

public class AmmoDisplayUI : MonoBehaviour {
    [SerializeField] private Text ammoText;

    public void Refresh(int current, int max) {
        if (ammoText != null) ammoText.text = $"{current}/{max}";
    }
}
