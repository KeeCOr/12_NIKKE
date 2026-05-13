using UnityEngine;

public enum UpgradeTargetType { SpecificMember, WeaponType, Global }
public enum UpgradeStat { Damage, FireRate, ReloadSpeed, Hp, MagazineSize, BombCharge, Barricade }

[CreateAssetMenu(fileName = "UpgradeCard_", menuName = "SquadVsMonster/Upgrade Card")]
public class UpgradeCardSO : ScriptableObject {
    public string           title;
    public string           description;
    public Sprite           icon;
    public UpgradeTargetType targetType;
    public string           targetId;      // member id ("alpha"…) or weapon name ("Shotgun") or "" for Global
    public UpgradeStat      stat;
    public float            value;         // 0.2 = +20 % or flat +20 depending on isMultiplier
    public bool             isMultiplier;  // true = multiply, false = flat add
    public int              maxStacks = 5;
}
