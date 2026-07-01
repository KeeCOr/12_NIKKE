using NUnit.Framework;

public class CombatAdvisorLogicTests {
    [Test]
    public void BossTipPrioritizesCoreWhenItIsActive() {
        var states = new[] {
            new BossPartHudState("HEAD", 10f, 100f, true, false),
            new BossPartHudState("CORE", 80f, 100f, true, false),
            new BossPartHudState("CHEST", 0f, 100f, true, true),
        };

        Assert.AreEqual("CORE exposed - focus all fire!", CombatAdvisorLogic.GetBossPartTip(states));
    }

    [Test]
    public void BossTipPointsToChestBeforeCoreIsActive() {
        var states = new[] {
            new BossPartHudState("HEAD", 10f, 100f, true, false),
            new BossPartHudState("CORE", 100f, 100f, false, false),
            new BossPartHudState("CHEST", 80f, 100f, true, false),
        };

        Assert.AreEqual("Break CHEST to expose CORE.", CombatAdvisorLogic.GetBossPartTip(states));
    }

    [Test]
    public void BossTipFallsBackToLegsForSlowdown() {
        var states = new[] {
            new BossPartHudState("HEAD", 90f, 100f, true, false),
            new BossPartHudState("LEG_L", 100f, 100f, true, false),
            new BossPartHudState("LEG_R", 100f, 100f, true, false),
            new BossPartHudState("CHEST", 100f, 100f, false, false),
        };

        Assert.AreEqual("Hit LEGS to slow the boss advance.", CombatAdvisorLogic.GetBossPartTip(states));
    }

    [Test]
    public void SquadTipWarnsWhenMultipleMembersReload() {
        Assert.AreEqual("3 members reloading - drag a ready shooter.", CombatAdvisorLogic.GetSquadTip(3, 0));
    }

    [Test]
    public void SquadTipWarnsWhenMemberIsDown() {
        Assert.AreEqual("1 squad member down - preserve the wall.", CombatAdvisorLogic.GetSquadTip(0, 1));
    }

    [Test]
    public void WaveResultSummaryRewardsCleanWeakPartFocus() {
        WaveResultSummary summary = CombatAdvisorLogic.GetWaveResultSummary(true, 0.08f, 0.78f, 0, 1);

        Assert.AreEqual("Victory - weak-part focus held.", summary.title);
        Assert.AreEqual("Role damage stayed online and the wall survived the push.", summary.reason);
        Assert.AreEqual("Next: keep one ready shooter on CORE while reloaders rotate.", summary.nextAdjustment);
    }

    [Test]
    public void WaveResultSummaryExplainsSurvivalFailureFirst() {
        WaveResultSummary summary = CombatAdvisorLogic.GetWaveResultSummary(false, 0.34f, 0.16f, 2, 0);

        Assert.AreEqual("Defeat - squad line collapsed.", summary.title);
        Assert.AreEqual("Two members fell and the wall dropped into breach range.", summary.reason);
        Assert.AreEqual("Next: split aim lines between minions and boss parts before firing skills.", summary.nextAdjustment);
    }

    [Test]
    public void WaveResultSummaryCallsOutLowDamageWhenWallIsHealthy() {
        WaveResultSummary summary = CombatAdvisorLogic.GetWaveResultSummary(false, 0.72f, 0.66f, 0, 3);

        Assert.AreEqual("Defeat - damage window missed.", summary.title);
        Assert.AreEqual("The wall held, but boss damage was too low while reloads stacked.", summary.reason);
        Assert.AreEqual("Next: stagger reloaders and retarget CHEST/CORE before the next volley.", summary.nextAdjustment);
    }

    [Test]
    public void SquadCompositionPreviewHighlightsHighFirepowerWeakPartBurst() {
        var preview = CombatAdvisorLogic.GetSquadCompositionPreview(new[] {
            MakeMember("Alpha", 900, 18f, 3.0f, 6, 1.2f, 20f, SpecialType.WeakpointBonus, 1.5f, "CORE"),
            MakeMember("Bravo", 850, 12f, 5.0f, 8, 1.6f, 14f, SpecialType.BurstAccuracy, 0.3f, "CHEST"),
            MakeMember("Charlie", 950, 10f, 4.0f, 10, 1.8f, 12f, SpecialType.RocketSplash, 1.0f, "ARM_L"),
        });

        Assert.AreEqual("Firepower A", preview.firepowerLabel);
        Assert.AreEqual("Weak-part burst squad: open CHEST/CORE early.", preview.firepowerTip);
    }

    [Test]
    public void SquadCompositionPreviewWarnsWhenSurvivalPoolIsThin() {
        var preview = CombatAdvisorLogic.GetSquadCompositionPreview(new[] {
            MakeMember("Glass", 420, 20f, 2.0f, 4, 2.2f, 18f, SpecialType.WeakpointBonus, 1.2f, "CORE"),
            MakeMember("Scout", 450, 10f, 4.0f, 8, 1.6f, 10f, SpecialType.None, 0f, "HEAD"),
        });

        Assert.AreEqual("Defense C", preview.defenseLabel);
        Assert.AreEqual("Thin survival pool: keep one aim line on minion cover.", preview.defenseTip);
    }

    [Test]
    public void SquadCompositionPreviewCallsOutMixedRangePositioning() {
        var preview = CombatAdvisorLogic.GetSquadCompositionPreview(new[] {
            MakeMember("Close", 800, 16f, 2.0f, 5, 1.5f, 7f, SpecialType.CloseSplash, 1.0f, "ARM_L"),
            MakeMember("Long", 760, 15f, 2.5f, 5, 1.5f, 22f, SpecialType.WeakpointMark, 1.0f, "CORE"),
        });

        Assert.AreEqual("Position Mixed", preview.positionLabel);
        Assert.AreEqual("Mixed range: front slots cover minions while long range marks CORE.", preview.positionTip);
    }

    private static SquadMemberConfigSO MakeMember(
        string label,
        int hp,
        float damage,
        float fireRate,
        int magazine,
        float reload,
        float range,
        SpecialType special,
        float specialVal,
        params string[] aimPriority
    ) {
        var config = UnityEngine.ScriptableObject.CreateInstance<SquadMemberConfigSO>();
        config.label = label;
        config.hp = hp;
        config.weapon = new WeaponConfig {
            damage = damage,
            fireRate = fireRate,
            magazineSize = magazine,
            reloadTime = reload,
            maxRange = range,
        };
        config.special = special;
        config.specialVal = specialVal;
        config.aimPriority = aimPriority;
        return config;
    }
}