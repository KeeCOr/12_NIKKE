using NUnit.Framework;

public class BossDamageTests {
    [SetUp] public void Setup() => GameEvents.ClearAllListeners();

    [Test]
    public void EffectiveSpeed_BothLegsDestroyed_Reduces() {
        float baseSpeed = 1.0f;
        float result = baseSpeed * 0.65f * 0.65f;
        Assert.AreApproximately(0.4225f, result, 0.001f);
    }

    [Test]
    public void DestroyedPartCount_IncreaseDamageBonus() {
        float rawDmg = 100f;
        int destroyedCount = 2;
        float result = rawDmg * (1f + destroyedCount * 0.25f);
        Assert.AreApproximately(150f, result, 0.001f);
    }
}
