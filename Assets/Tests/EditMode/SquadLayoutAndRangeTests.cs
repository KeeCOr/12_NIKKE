using NUnit.Framework;

public class SquadLayoutAndRangeTests {
    [Test]
    public void SquadSlots_AreWideEnoughForReadableCharacterSprites() {
        for (int i = 1; i < GameConfig.SQUAD_SLOT_X.Length; i++) {
            float spacing = GameConfig.SQUAD_SLOT_X[i] - GameConfig.SQUAD_SLOT_X[i - 1];
            Assert.GreaterOrEqual(spacing, 0.95f);
        }
    }

    [Test]
    public void DefaultWeaponRange_ReachesOpeningBossTarget() {
        Assert.GreaterOrEqual(GameConfig.DEFAULT_MAX_RANGE, 15f);
    }
}
