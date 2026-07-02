# SquadVsMonster Persona Playtest Feedback

Last updated: 2026-07-02 KST

## Persona

- Name: Seo Yuna
- Age: 27
- Preferred genre: squad auto-battle and character-collection combat
- Play context: likes comparing squad composition, role coverage, and wave results before changing the next lineup.

## Persona Expectation

A squad auto-battle fan expects the game to explain why a lineup worked: strongest role, weakest slot, enemy counter, and one next adjustment.

## 2026-07-02 Recheck

- Current strength: the wave result path is already present in code through `CombatAdvisorLogic.GetWaveResultSummary`, `GameManager.LastWaveResultSummary`, `CombatAdvisorUI`, and `ResultUI`.
- Persona confidence: the project should no longer be treated as missing the entire wave-result model.
- Remaining risk: Unity scene/HUD visual smoke still needs to confirm that the result summary is readable in the actual scene and not hidden by layout, font scale, or overlay order.

## Next Smoke Criteria

1. Run one win-by-composition case and confirm the result summary names the winning factor.
2. Run one loss-by-counter case and confirm the summary points to the counter or weak slot.
3. Run one underpower/attrition loss case and confirm the next adjustment is visible without opening another panel.
4. Confirm the summary text is readable on the target viewport and does not overlap result buttons.