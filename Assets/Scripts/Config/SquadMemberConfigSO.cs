using UnityEngine;

public enum SpecialType { None, WeakpointBonus, BurstAccuracy, CloseSplash, RocketSplash, WeakpointMark }

[CreateAssetMenu(fileName = "SquadConfig_", menuName = "SquadVsMonster/Squad Member Config")]
public class SquadMemberConfigSO : ScriptableObject {
    public string      id;
    public string      label;
    public int         hp;
    public Color       color;
    public WeaponConfig weapon;
    public SpecialType special;
    public float       specialVal;
    public string[]    aimPriority; // valid boss part IDs: HEAD ARM_L ARM_R LEG_L LEG_R CHEST CORE
}
