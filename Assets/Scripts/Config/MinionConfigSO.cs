using UnityEngine;

[CreateAssetMenu(fileName = "MinionConfig_", menuName = "SquadVsMonster/Minion Config")]
public class MinionConfigSO : ScriptableObject {
    public MinionType type;
    public int   hp             = 60;
    public float speed          = 1.2f;
    public float damage         = 20f;
    public float attackRange    = 0.4f;
    public float attackInterval = 1.5f;
    public float terrainDamage  = 15f;
}
