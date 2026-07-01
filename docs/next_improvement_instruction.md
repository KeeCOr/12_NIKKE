# SquadVsMonster Next Improvement Instruction

Date: 2026-06-24

## Goal
Turn the current biggest project issue into a small, executable improvement batch. This file is intentionally scoped so the next worker can start without rereading the whole workspace audit.

## Instructions
1. Improve squad composition feedback so role differences are visible through damage, survival, and positioning changes.
2. Add a wave result panel that explains why the squad won or failed and suggests one next adjustment.
3. Keep the sci-fi HUD asset tone consistent when replacing buttons, frames, icons, or combat feedback.

## Completion Rules
- Do not include discarded projects in this batch.
- If gameplay, UI, systems, content, controls, build behavior, or project scope changes, update the project planning document and update log before build/release.
- If runtime source changes, run the nearest available validation and then perform the required build/package step from the project instructions.
- If a folder or asset looks ambiguous, document the decision instead of deleting it.

## 2026-06-30 Completion Note
- Current source already includes squad composition feedback through `CombatAdvisorLogic.GetSquadTip`, reload/down-member event tracking, and squad member role differences in weapon/special handling.
- Current source already includes a wave result advisory path: `CombatAdvisorLogic.GetWaveResultSummary`, `GameManager.LastWaveResultSummary`, `CombatAdvisorUI` end alerts, and `ResultUI` title/reason/next-adjustment fields.
- The project GDD already records the sci-fi HUD asset tone and the 2026-06-30 wave result advisory update, so no runtime source changes were made in this pass.

## 2026-07-01 Completion Note
- Implemented squad composition preview for the requested first-priority feedback: firepower, defense, and position tradeoffs are now summarized from squad configs before combat signals arrive.
- Added three EditMode tests covering high firepower weak-part burst, thin survival pool, and mixed range positioning acceptance cases.
- Remaining related idea: a dedicated formation screen can later render the same `SquadCompositionPreview` in separate cards instead of the single-line CombatAdvisorHUD preview.
