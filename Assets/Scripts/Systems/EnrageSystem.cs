using UnityEngine;

public class EnrageSystem : MonoBehaviour {
    [SerializeField] private BossController boss;
    private bool _triggered;

    void Update() {
        if (_triggered || boss == null || !boss.IsAlive) return;
        if (boss.MeetsEnrageCondition()) {
            _triggered = true;
            boss.Enrage();
        }
    }
}
