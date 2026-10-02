public struct WaveResultSummary {
    public readonly string title;
    public readonly string reason;
    public readonly string nextAdjustment;
    public readonly string cueLine;

    public WaveResultSummary(string title, string reason, string nextAdjustment)
        : this(title, reason, nextAdjustment, string.Empty) { }

    public WaveResultSummary(string title, string reason, string nextAdjustment, string cueLine) {
        this.title = title;
        this.reason = reason;
        this.nextAdjustment = nextAdjustment;
        this.cueLine = cueLine;
    }
}

public struct SquadReasonChip {
    public readonly string key;
    public readonly string label;
    public readonly string reason;

    public SquadReasonChip(string key, string label, string reason) {
        this.key = key;
        this.label = label;
        this.reason = reason;
    }

    public string ToHudSegment() {
        return $"{label}: {reason}";
    }
}

public struct SquadCompositionPreview {
    public readonly string firepowerLabel;
    public readonly string firepowerTip;
    public readonly string defenseLabel;
    public readonly string defenseTip;
    public readonly string rangeLabel;
    public readonly string rangeTip;
    public readonly string positionLabel;
    public readonly string positionTip;
    public readonly int readinessScore;

    public SquadCompositionPreview(
        string firepowerLabel,
        string firepowerTip,
        string defenseLabel,
        string defenseTip,
        string rangeLabel,
        string rangeTip,
        string positionLabel,
        string positionTip,
        int readinessScore = 0
    ) {
        this.firepowerLabel = firepowerLabel;
        this.firepowerTip = firepowerTip;
        this.defenseLabel = defenseLabel;
        this.defenseTip = defenseTip;
        this.rangeLabel = rangeLabel;
        this.rangeTip = rangeTip;
        this.positionLabel = positionLabel;
        this.positionTip = positionTip;
        this.readinessScore = readinessScore;
    }

    public SquadReasonChip[] GetReasonChips() {
        return new[] {
            new SquadReasonChip("firepower", firepowerLabel, firepowerTip),
            new SquadReasonChip("survival", defenseLabel.Replace("Defense", "Survival"), defenseTip),
            new SquadReasonChip("range", rangeLabel, rangeTip),
            new SquadReasonChip("position", positionLabel, positionTip),
        };
    }

    public string ToHudLine() {
        SquadReasonChip[] chips = GetReasonChips();
        return $"Ready {readinessScore}/100  |  {chips[0].ToHudSegment()}  |  {chips[1].ToHudSegment()}  |  {chips[2].ToHudSegment()}  |  {chips[3].ToHudSegment()}";
    }
}

public static class CombatAdvisorLogic {
    public const string DefaultBossTip = "Aim at active parts - break armor first.";
    public const string ReadySquadTip = "Squad ready - drag aim markers onto weak parts.";

    public static string GetBossPartTip(BossPartHudState[] states) {
        if (states == null || states.Length == 0) return DefaultBossTip;

        if (HasActivePart(states, "CORE")) return "CORE exposed - focus all fire!";
        if (HasActivePart(states, "CHEST")) return "Break CHEST to expose CORE.";
        if (HasActivePart(states, "LEG_L") || HasActivePart(states, "LEG_R")) return "Hit LEGS to slow the boss advance.";
        if (HasActivePart(states, "ARM_L") || HasActivePart(states, "ARM_R")) return "Break ARMS to slow boss attacks.";
        if (HasActivePart(states, "HEAD")) return "HEAD is vulnerable - precision shots pay off.";

        return DefaultBossTip;
    }

    public static string GetSquadTip(int reloadingCount, int downCount) {
        if (downCount > 0) {
            string noun = downCount == 1 ? "member" : "members";
            return $"{downCount} squad {noun} down - preserve the wall.";
        }
        if (reloadingCount >= 2) return $"{reloadingCount} members reloading - drag a ready shooter.";
        if (reloadingCount == 1) return "One member reloading - keep another aim line active.";
        return ReadySquadTip;
    }

    public static SquadCompositionPreview GetSquadCompositionPreview(SquadMemberConfigSO[] squadConfigs) {
        if (squadConfigs == null || squadConfigs.Length == 0) {
            return new SquadCompositionPreview(
                "Firepower C", "No squad data: confirm deployed members.",
                "Defense C", "No squad data: survival risk unknown.",
                "Range Unknown", "No weapon range data: confirm deployed members.",
                "Position Unknown", "No aim plan: assign roles before wave start."
            );
        }

        int count = 0;
        int totalHp = 0;
        float totalDps = 0f;
        float minRange = float.MaxValue;
        float maxRange = 0f;
        bool targetsCoreOrChest = false;
        bool hasWeakPartSpecial = false;
        bool hasCloseControl = false;

        for (int i = 0; i < squadConfigs.Length; i++) {
            var config = squadConfigs[i];
            if (config == null) continue;

            count++;
            totalHp += config.hp;
            totalDps += EstimateDps(config.weapon);

            float range = config.weapon != null && config.weapon.maxRange > 0f
                ? config.weapon.maxRange
                : GameConfig.DEFAULT_MAX_RANGE;
            if (range < minRange) minRange = range;
            if (range > maxRange) maxRange = range;

            if (TargetsPart(config, "CORE") || TargetsPart(config, "CHEST")) targetsCoreOrChest = true;
            if (config.special == SpecialType.WeakpointBonus || config.special == SpecialType.BurstAccuracy || config.special == SpecialType.WeakpointMark) {
                hasWeakPartSpecial = true;
            }
            if (config.special == SpecialType.CloseSplash || config.special == SpecialType.RocketSplash) {
                hasCloseControl = true;
            }
        }

        if (count == 0) {
            return new SquadCompositionPreview(
                "Firepower C", "No squad data: confirm deployed members.",
                "Defense C", "No squad data: survival risk unknown.",
                "Range Unknown", "No weapon range data: confirm deployed members.",
                "Position Unknown", "No aim plan: assign roles before wave start."
            );
        }

        float averageDps = totalDps / count;
        float averageHp = (float)totalHp / count;
        string firepowerLabel = averageDps >= 28f ? "Firepower A" : averageDps >= 18f ? "Firepower B" : "Firepower C";
        string defenseLabel = averageHp >= 800f ? "Defense A" : averageHp >= 600f ? "Defense B" : "Defense C";
        bool mixedRange = maxRange - minRange >= 10f;
        string rangeLabel = mixedRange ? "Range Mixed" : maxRange >= 16f ? "Range Long" : "Range Close";
        string positionLabel = mixedRange && targetsCoreOrChest
            ? "Position Split"
            : targetsCoreOrChest
                ? "Position Focus"
                : hasCloseControl
                    ? "Position Screen"
                    : "Position Flexible";

        string firepowerTip = firepowerLabel == "Firepower A" && hasWeakPartSpecial && targetsCoreOrChest
            ? "Weak-part burst squad: open CHEST/CORE early."
            : firepowerLabel == "Firepower C"
                ? "Low damage squad: stagger reloads and save burst for exposed parts."
                : "Balanced damage: rotate aim lines before reloads overlap.";

        string defenseTip = defenseLabel == "Defense C"
            ? "Thin survival pool: keep one aim line on minion cover."
            : defenseLabel == "Defense B"
                ? "Stable line: avoid stacking all members on boss parts."
                : "Durable line: can hold longer boss-part focus windows.";

        string rangeTip = mixedRange
            ? "Mixed ranges: assign front and back lanes before combat."
            : rangeLabel == "Range Long"
                ? "Long range: preserve distance while parts are exposed."
                : "Close range: prepare cover before breach pressure.";

        string positionTip = mixedRange && targetsCoreOrChest
            ? "Mixed range: front slots cover minions while long range marks CORE."
            : targetsCoreOrChest
                ? "Weak-part focus: keep one cover role while CORE shooters commit."
                : hasCloseControl
                    ? "Front screen: let splash users intercept minions near the wall."
                    : "Flexible roles: assign one cover lane before wave start.";

        int readinessScore = 35;
        readinessScore += firepowerLabel == "Firepower A" ? 25 : firepowerLabel == "Firepower B" ? 16 : 8;
        readinessScore += defenseLabel == "Defense A" ? 20 : defenseLabel == "Defense B" ? 13 : 6;
        if (targetsCoreOrChest) readinessScore += 10;
        if (hasWeakPartSpecial || hasCloseControl) readinessScore += 10;
        readinessScore = readinessScore > 100 ? 100 : readinessScore;
        return new SquadCompositionPreview(
            firepowerLabel, firepowerTip,
            defenseLabel, defenseTip,
            rangeLabel, rangeTip,
            positionLabel, positionTip,
            readinessScore
        );
    }

    public static WaveResultSummary GetWaveResultSummary(bool isWin, float bossHpRatio, float wallHpRatio, int downCount, int reloadingCount) {
        bossHpRatio = Clamp01(bossHpRatio);
        wallHpRatio = Clamp01(wallHpRatio);

        if (isWin) {
            if (downCount > 0 || wallHpRatio < 0.45f) {
                return new WaveResultSummary(
                    "Victory - costly hold.",
                    "The boss fell, but survival pressure nearly broke the line.",
                    "Next: keep one member on minion cover before committing skills.",
                    "Cue: win was costly; survival cover is the next bottleneck."
                );
            }

            return new WaveResultSummary(
                "Victory - weak-part focus held.",
                "Role damage stayed online and the wall survived the push.",
                "Next: keep one ready shooter on CORE while reloaders rotate.",
                "Cue: win came from weak-part uptime; keep one CORE shooter ready."
            );
        }

        if (downCount >= 2 || wallHpRatio <= 0.25f) {
            return new WaveResultSummary(
                "Defeat - squad line collapsed.",
                $"{FormatMemberCount(downCount)} fell and the wall dropped into breach range.",
                "Next: split aim lines between minions and boss parts before firing skills.",
                "Cue: survival bottleneck; assign cover role before boss focus."
            );
        }

        if (bossHpRatio >= 0.55f) {
            string reason = reloadingCount >= 2
                ? "The wall held, but boss damage was too low while reloads stacked."
                : "The wall held, but boss damage was too low for the wave timer.";
            string cueLine = reloadingCount >= 2
                ? "Cue: damage bottleneck from stacked reloads; stagger volleys."
                : "Cue: damage bottleneck; retarget CHEST/CORE earlier.";
            return new WaveResultSummary(
                "Defeat - damage window missed.",
                reason,
                "Next: stagger reloaders and retarget CHEST/CORE before the next volley.",
                cueLine
            );
        }

        return new WaveResultSummary(
            "Defeat - final push mistimed.",
            "Boss HP was low, but the squad could not finish before the breach.",
            "Next: save one skill burst for the exposed CORE phase.",
            "Cue: timing bottleneck; reserve burst for exposed CORE."
        );
    }

    private static float EstimateDps(WeaponConfig weapon) {
        if (weapon == null) return 0f;
        float pellets = weapon.pellets > 0 ? weapon.pellets : 1f;
        float burstDamage = weapon.damage * pellets;
        float reload = weapon.reloadTime > 0f ? weapon.reloadTime : 1f;
        float magazine = weapon.magazineSize > 0 ? weapon.magazineSize : 1f;
        float fireRate = weapon.fireRate > 0f ? weapon.fireRate : 1f;
        float cycleSeconds = (magazine / fireRate) + reload;
        return cycleSeconds > 0f ? burstDamage * magazine / cycleSeconds : burstDamage;
    }

    private static bool TargetsPart(SquadMemberConfigSO config, string partId) {
        if (config == null || config.aimPriority == null) return false;
        for (int i = 0; i < config.aimPriority.Length; i++) {
            if (config.aimPriority[i] == partId) return true;
        }
        return false;
    }

    private static bool HasActivePart(BossPartHudState[] states, string id) {
        for (int i = 0; i < states.Length; i++) {
            if (states[i].partId == id && states[i].isActive && !states[i].isDestroyed && states[i].hp > 0f) {
                return true;
            }
        }
        return false;
    }

    private static float Clamp01(float value) {
        if (value < 0f) return 0f;
        if (value > 1f) return 1f;
        return value;
    }

    private static string FormatMemberCount(int count) {
        if (count <= 0) return "No members";
        if (count == 1) return "One member";
        if (count == 2) return "Two members";
        if (count == 3) return "Three members";
        return $"{count} members";
    }
}
