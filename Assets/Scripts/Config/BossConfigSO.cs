using UnityEngine;

[CreateAssetMenu(fileName = "BossConfig", menuName = "SquadVsMonster/Boss Config")]
public class BossConfigSO : ScriptableObject {
    public int   maxHp              = 4500;
    public float stopX              = 4.2f;  // world x where boss stops moving and attacks wall
    public float speed              = 0.26f;
    public float enragedSpeed       = 0.44f;
    public float attackInterval     = 2.5f;
    public float attackDamageWall   = 80f;
    public float shockwaveDamage    = 30f;
    public float shockwaveInterval  = 6f;
    public float enrageHpThreshold  = 0.5f; // 0–1 normalized ratio of maxHp
    public BossPartConfig[] parts;
}
