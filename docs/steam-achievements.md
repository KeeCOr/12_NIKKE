# SquadVsMonster — Steam Achievements

---

## Stats

| API Name | Type | Description |
|----------|------|-------------|
| `STAT_RUNS_COMPLETED` | INT | Total runs completed |
| `STAT_BOSSES_DEFEATED` | INT | Total bosses defeated |
| `STAT_WAVES_CLEARED` | INT | Total waves cleared |
| `STAT_SQUAD_ADJUSTMENTS` | INT | Total squad lineup changes made |
| `STAT_WEAK_POINTS_HIT` | INT | Total boss weak points exploited |

---

## Achievements

| API Name | EN Name | KO Name | How to Unlock |
|----------|---------|---------|---------------|
| `ACH_FIRST_RUN` | First Formation | 첫 번째 편성 | Complete your first run |
| `ACH_FIRST_BOSS` | Boss Slayer | 보스 처치자 | Defeat your first boss |
| `ACH_WAVE_5` | Wave Runner | 웨이브 돌파자 | Clear 5 waves in a run |
| `ACH_WAVE_10` | Endurance Test | 지구력 테스트 | Clear 10 waves in a run |
| `ACH_FIRST_WEAKPOINT` | Weak Spot Reader | 약점 분석가 | Exploit a boss weak point |
| `ACH_COMP_ANALYSIS` | Tactical Preview | 전술 예습 | Check your composition preview before every battle in a run |
| `ACH_LEARN_FROM_LOSS` | Lesson Learned | 패배에서 배움 | Read the result cue after a loss and adjust squad for the next run |
| `ACH_FIREPOWER_RUN` | All-In Offense | 전력 공격 | Win a run with a firepower-majority squad |
| `ACH_DEFENSE_RUN` | Fortress Squad | 요새 분대 | Win a run with a defense-majority squad |
| `ACH_RANGE_MIX` | Mixed Arms | 혼합 전력 | Win a run with equal firepower and range roles |
| `ACH_BOSS_10` | Boss Rush Veteran | 보스 러시 베테랑 | Defeat 10 bosses total |
| `ACH_PERFECT_RUN` | Flawless Command | 완벽한 지휘 | Win a run without any squad member being eliminated |
| `ACH_SPEED_CLEAR` | Precision Strike | 정밀 타격 | Clear a wave in under 30 seconds |
| `ACH_FIFTY_WAVES` | Century Mark | 웨이브 마스터 | Clear 50 waves total across all runs |

---

## Implementation Notes

- Steam API: `ISteamUserStats`
- `ACH_COMP_ANALYSIS` requires tracking whether preview was checked before each battle — persist a flag per battle
- `ACH_LEARN_FROM_LOSS` triggers when result cue is read + squad is modified + new run begins
- `ACH_PERFECT_RUN` checks squad elimination count at run end
- `ACH_SPEED_CLEAR` measures wave duration from wave start to wave end signal
- All achievements unlockable in offline single-player
- Replace App ID 480 with real Steamworks App ID before submission
