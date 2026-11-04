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
    public void WaveResultSummaryCueHighlightsCleanWinRolePlan() {
        WaveResultSummary summary = CombatAdvisorLogic.GetWaveResultSummary(true, 0.08f, 0.78f, 0, 1);

        Assert.AreEqual("Cue: win came from weak-part uptime; keep one CORE shooter ready.", summary.cueLine);
    }

    [Test]
    public void WaveResultSummaryExplainsSurvivalFailureFirst() {
        WaveResultSummary summary = CombatAdvisorLogic.GetWaveResultSummary(false, 0.34f, 0.16f, 2, 0);

        Assert.AreEqual("Defeat - squad line collapsed.", summary.title);
        Assert.AreEqual("Two members fell and the wall dropped into breach range.", summary.reason);
        Assert.AreEqual("Next: split aim lines between minions and boss parts before firing skills.", summary.nextAdjustment);
    }

    [Test]
    public void WaveResultSummaryCueHighlightsSurvivalBottleneck() {
        WaveResultSummary summary = CombatAdvisorLogic.GetWaveResultSummary(false, 0.34f, 0.16f, 2, 0);

        Assert.AreEqual("Cue: survival bottleneck; assign cover role before boss focus.", summary.cueLine);
    }

    [Test]
    public void WaveResultSummaryCallsOutLowDamageWhenWallIsHealthy() {
        WaveResultSummary summary = CombatAdvisorLogic.GetWaveResultSummary(false, 0.72f, 0.66f, 0, 3);

        Assert.AreEqual("Defeat - damage window missed.", summary.title);
        Assert.AreEqual("The wall held, but boss damage was too low while reloads stacked.", summary.reason);
        Assert.AreEqual("Next: stagger reloaders and retarget CHEST/CORE before the next volley.", summary.nextAdjustment);
    }

    [Test]
    public void WaveResultSummaryCueHighlightsReloadDamageBottleneck() {
        WaveResultSummary summary = CombatAdvisorLogic.GetWaveResultSummary(false, 0.72f, 0.66f, 0, 3);

        Assert.AreEqual("Cue: damage bottleneck from stacked reloads; stagger volleys.", summary.cueLine);
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

        Assert.AreEqual("Position Split", preview.positionLabel);
        Assert.AreEqual("Mixed range: front slots cover minions while long range marks CORE.", preview.positionTip);
    }

    [Test]
    public void SquadCompositionPreviewBuildsFourScanFriendlyReasonChips() {
        var preview = CombatAdvisorLogic.GetSquadCompositionPreview(new[] {
            MakeMember("Close", 800, 16f, 2.0f, 5, 1.5f, 7f, SpecialType.CloseSplash, 1.0f, "ARM_L"),
            MakeMember("Long", 760, 15f, 2.5f, 5, 1.5f, 22f, SpecialType.WeakpointMark, 1.0f, "CORE"),
        });

        SquadReasonChip[] chips = preview.GetReasonChips();

        Assert.AreEqual(4, chips.Length);
        Assert.AreEqual("firepower", chips[0].key);
        Assert.AreEqual("survival", chips[1].key);
        Assert.AreEqual("range", chips[2].key);
        Assert.AreEqual("Range Mixed", chips[2].label);
        Assert.AreEqual("position", chips[3].key);
        Assert.AreEqual("Position Split", chips[3].label);
        StringAssert.Contains("Ready", preview.ToHudLine());
    }

    [Test]
    public void SquadCompositionHudLineShowsChipLabelsAndOneWeakestTip() {
        var preview = CombatAdvisorLogic.GetSquadCompositionPreview(new[] {
            MakeMember("Glass", 420, 20f, 2.0f, 4, 2.2f, 18f, SpecialType.WeakpointBonus, 1.2f, "CORE"),
            MakeMember("Scout", 450, 10f, 4.0f, 8, 1.6f, 10f, SpecialType.None, 0f, "HEAD"),
        });

        string line = preview.ToHudLine();

        StringAssert.Contains("Survival C", line);
        StringAssert.Contains(preview.defenseTip, line);
        StringAssert.DoesNotContain(preview.rangeTip, line);
    }

    [Test]
    public void WavePressureAdviceKeepsSquadOnPartsDuringLightRunnerPhase() {
        string advice = CombatAdvisorLogic.GetWavePressureAdvice(WaveSystem.GetPhaseParams(0f, false), RealLikeSquad(), true);

        Assert.AreEqual("Next: Light 1-1/4s - all aim lines on boss parts.", advice);
    }

    [Test]
    public void WavePressureAdviceAssignsSplashMemberToHeavyBerserkerPhase() {
        string advice = CombatAdvisorLogic.GetWavePressureAdvice(WaveSystem.GetPhaseParams(50f, false), RealLikeSquad(), false);

        Assert.AreEqual("Now: Heavy 4-8/3s (Berserker 30%) - Delta splash screens wall, rest on parts.", advice);
    }

    [Test]
    public void WavePressureAdviceAsksForTwoCoverLinesWhenSwarmHasNoSplash() {
        var squad = new[] {
            MakeMember("Alpha", 90, 120f, 1f, 1, 2f, 0f, SpecialType.WeakpointBonus, 2f, "CORE"),
            MakeMember("Charlie", 100, 18f, 4f, 20, 2f, 12f, SpecialType.None, 1f, "HEAD"),
        };

        string advice = CombatAdvisorLogic.GetWavePressureAdvice(WaveSystem.GetPhaseParams(70f, false), squad, true);

        Assert.AreEqual("Next: Swarm 6-10/2.5s (Berserker 35%) - no splash: Charlie + one more on minions.", advice);
    }

    [Test]
    public void WavePressureAdviceWithoutSquadDataStillReportsPressure() {
        string advice = CombatAdvisorLogic.GetWavePressureAdvice(WaveSystem.GetPhaseParams(30f, false), null, true);

        Assert.AreEqual("Next: Rising 2-4/4.5s (Berserker 15%) - assign a cover lane before it lands.", advice);
    }

    [Test]
    public void WavePressureAdviceDoesNotAssignFallenSplashMember() {
        var squad = RealLikeSquad();
        squad[1].id = "delta";
        var down = new System.Collections.Generic.HashSet<string> { "delta" };
        string advice = CombatAdvisorLogic.GetWavePressureAdvice(WaveSystem.GetPhaseParams(50f, false), squad, false, down);
        StringAssert.DoesNotContain("Delta splash", advice);
        StringAssert.Contains("no splash", advice);
    }

    [Test]
    public void WavePressureAdviceChecksWeaponRadiusInsteadOfSpecialName() {
        var squad = RealLikeSquad();
        squad[1].weapon.splashRadius = 0f;
        string advice = CombatAdvisorLogic.GetWavePressureAdvice(WaveSystem.GetPhaseParams(50f, false), squad, false);
        StringAssert.Contains("no splash", advice);
    }

    [Test]
    public void AdvisorKeepsCompositionVisibleAndRefreshesFallenRoleBehindAlert() {
        var panel = new UnityEngine.GameObject("Advisor test");
        var textObject = new UnityEngine.GameObject("Advisor text", typeof(UnityEngine.RectTransform));
        var squad = RealLikeSquad();
        squad[1].id = "delta";
        try {
            var text = textObject.AddComponent<UnityEngine.UI.Text>();
            var advisor = panel.AddComponent<CombatAdvisorUI>();
            advisor.Bind(text, null);
            advisor.BindSquadComposition(squad);
            // EditMode does not run the player lifecycle for this component automatically.
            InvokeAdvisor(advisor, "OnDisable");
            InvokeAdvisor(advisor, "OnEnable");
            GameEvents.RaiseWavePressureChanged(WaveSystem.GetPhaseParams(50f, false), false);
            StringAssert.StartsWith("Firepower", text.text);
            StringAssert.Contains("\nNow: Heavy", text.text);
            StringAssert.Contains("Delta splash", text.text);
            GameEvents.RaiseMemberDied("delta");
            StringAssert.Contains("DELTA down", text.text);
            typeof(CombatAdvisorUI).GetField("_alertUntil", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(advisor, -1f);
            InvokeAdvisor(advisor, "Update");
            StringAssert.StartsWith("Firepower", text.text);
            StringAssert.Contains("no splash", text.text);
            StringAssert.DoesNotContain("Delta splash", text.text);
        } finally {
            var advisor = panel.GetComponent<CombatAdvisorUI>();
            if (advisor != null) InvokeAdvisor(advisor, "OnDisable");
            UnityEngine.Object.DestroyImmediate(panel);
            UnityEngine.Object.DestroyImmediate(textObject);
            foreach (var member in squad) UnityEngine.Object.DestroyImmediate(member);
        }
    }

    private static void InvokeAdvisor(CombatAdvisorUI advisor, string method) {
        typeof(CombatAdvisorUI).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(advisor, null);
    }

    private static SquadMemberConfigSO[] RealLikeSquad() {
        var squad = new[] {
            MakeMember("Alpha", 90, 120f, 1f, 1, 2f, 0f, SpecialType.WeakpointBonus, 2f, "CORE", "HEAD"),
            MakeMember("Delta", 130, 90f, 1f, 2, 2f, 15f, SpecialType.RocketSplash, 1.8f, "CHEST", "CORE"),
            MakeMember("Echo", 80, 280f, 1f, 1, 2f, 16f, SpecialType.WeakpointMark, 1.4f, "CORE"),
        };
        squad[1].weapon.splashRadius = 150f;
        return squad;
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
