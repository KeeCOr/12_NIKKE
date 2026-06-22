using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class BossDamageTests {
    [SetUp] public void Setup() => GameEvents.ClearAllListeners();

    [TearDown]
    public void TearDown() => GameEvents.ClearAllListeners();

    [Test]
    public void EffectiveSpeed_BothLegsDestroyed_Reduces() {
        float baseSpeed = 1.0f;
        float result = baseSpeed * 0.65f * 0.65f;
        Assert.That(result, Is.EqualTo(0.4225f).Within(0.001f));
    }

    [Test]
    public void DestroyedPartCount_IncreaseDamageBonus() {
        float rawDmg = 100f;
        int destroyedCount = 2;
        float result = rawDmg * (1f + destroyedCount * 0.25f);
        Assert.That(result, Is.EqualTo(150f).Within(0.001f));
    }

    [Test]
    public void MeetsEnrageCondition_HeadDestroyedAtHalfHp_ReturnsTrue() {
        var bossGo = MakeBoss();
        try {
            var boss = bossGo.GetComponent<BossController>();

            boss.TakeDamage("HEAD", 250f);
            boss.TakeDamage("ARM_L", 300f);

            Assert.LessOrEqual(boss.Hp / boss.MaxHp, 0.5f);
            Assert.IsTrue(boss.GetPart("HEAD").IsDestroyed);
            Assert.IsTrue(boss.MeetsEnrageCondition());
        } finally {
            Object.DestroyImmediate(bossGo);
        }
    }

    [Test]
    public void MeetsEnrageCondition_ChestDestroyedAtHalfHp_ReturnsTrue() {
        var bossGo = MakeBoss();
        try {
            var boss = bossGo.GetComponent<BossController>();

            boss.TakeDamage("CHEST", 250f);
            boss.TakeDamage("ARM_L", 300f);

            Assert.LessOrEqual(boss.Hp / boss.MaxHp, 0.5f);
            Assert.IsTrue(boss.GetPart("CHEST").IsDestroyed);
            Assert.IsTrue(boss.MeetsEnrageCondition());
        } finally {
            Object.DestroyImmediate(bossGo);
        }
    }

    [Test]
    public void MeetsEnrageCondition_NoKeyPartDestroyedAtHalfHp_ReturnsFalse() {
        var bossGo = MakeBoss();
        try {
            var boss = bossGo.GetComponent<BossController>();

            boss.TakeDamage("ARM_L", 500f);

            Assert.LessOrEqual(boss.Hp / boss.MaxHp, 0.5f);
            Assert.IsFalse(boss.MeetsEnrageCondition());
        } finally {
            Object.DestroyImmediate(bossGo);
        }
    }


    [Test]
    public void TakeDamage_RaisesPartHudStateWithDamagedPartRatio() {
        BossPartHudState[] received = null;
        GameEvents.OnBossPartHpChanged += states => received = states;
        var bossGo = MakeBoss();
        try {
            var boss = bossGo.GetComponent<BossController>();

            boss.TakeDamage("HEAD", 25f);

            var head = FindState(received, "HEAD");
            Assert.NotNull(received);
            Assert.That(head.HpRatio, Is.EqualTo(0.75f).Within(0.001f));
            Assert.IsTrue(head.isActive);
            Assert.IsFalse(head.isDestroyed);
        } finally {
            Object.DestroyImmediate(bossGo);
        }
    }

    [Test]
    public void ChestDestroyed_RaisesCoreActivePartHudState() {
        BossPartHudState[] received = null;
        GameEvents.OnBossPartHpChanged += states => received = states;
        var bossGo = MakeBoss();
        try {
            var boss = bossGo.GetComponent<BossController>();

            boss.TakeDamage("CHEST", 250f);

            var chest = FindState(received, "CHEST");
            var core = FindState(received, "CORE");
            Assert.IsTrue(chest.isDestroyed);
            Assert.IsTrue(core.isActive);
            Assert.IsFalse(core.isDestroyed);
        } finally {
            Object.DestroyImmediate(bossGo);
        }
    }

    private static BossPartHudState FindState(BossPartHudState[] states, string partId) {
        Assert.NotNull(states);
        foreach (var state in states)
            if (state.partId == partId) return state;
        Assert.Fail($"Missing part HUD state: {partId}");
        return default;
    }
    private static GameObject MakeBoss() {
        var go = new GameObject("BossUnderTest");
        var boss = go.AddComponent<BossController>();
        var config = ScriptableObject.CreateInstance<BossConfigSO>();
        config.maxHp = 1000;
        config.enrageHpThreshold = 0.5f;
        config.parts = new[] {
            Part("HEAD", 100, 1f, true),
            Part("CHEST", 100, 1f, true),
            Part("ARM_L", 1000, 1f, true),
            Part("CORE", 100, 1f, false)
        };

        typeof(BossController)
            .GetField("config", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(boss, config);
        boss.enabled = true;
        typeof(BossController)
            .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(boss, null);

        return go;
    }

    private static BossPartConfig Part(string id, int hp, float damageMult, bool activeOnStart) =>
        new BossPartConfig {
            id = id,
            hp = hp,
            damageMult = damageMult,
            offset = Vector2.zero,
            size = Vector2.one,
            activeOnStart = activeOnStart
        };
}


