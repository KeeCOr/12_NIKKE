using TMPro;
using UnityEngine;

public class AmmoDisplayUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI ammoText;

    public void Refresh(int current, int max) {
        if (ammoText != null) ammoText.text = $"{current}/{max}";
    }
}
