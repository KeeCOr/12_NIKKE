# Squad Combat Advisor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a lightweight combat advisor HUD that tells first-time SquadVsMonster players which boss part or squad state matters right now.

**Architecture:** Keep combat logic unchanged. Add a pure `CombatAdvisorLogic` helper for testable message selection, a `CombatAdvisorUI` MonoBehaviour that subscribes to existing `GameEvents`, and wire a compact advisor panel into the existing Canvas via `SceneBuilder`.

**Tech Stack:** Unity 2022.3, C#, Unity UI `Text`/`Image`, existing EditMode tests.

## Global Constraints

- Do not change combat damage, boss AI, wave logic, or input behavior.
- Avoid adding dependencies.
- Build output must still produce `SquadVsMonster_v1.2_portable.exe` and matching `SquadVsMonster_v1.2_portable_Data`.
- Keep the advisor as a single-line tactical hint so it does not crowd the existing HUD.

---

### Task 1: Advisor Logic

**Files:**
- Create: `Assets/Scripts/UI/CombatAdvisorLogic.cs`
- Test: `Assets/Tests/EditMode/CombatAdvisorLogicTests.cs`

**Interfaces:**
- Produces: `CombatAdvisorLogic.GetBossPartTip(BossPartHudState[] states): string`
- Produces: `CombatAdvisorLogic.GetSquadTip(int reloadingCount, int downCount): string`

- [ ] Add tests for CORE active priority, CHEST-before-CORE priority, leg priority, reload warning, and down warning.
- [ ] Implement pure string selection without touching scene objects.

### Task 2: Advisor Runtime UI

**Files:**
- Create: `Assets/Scripts/UI/CombatAdvisorUI.cs`
- Modify: `Assets/Editor/SceneBuilder.cs`

**Interfaces:**
- Consumes: `CombatAdvisorLogic.GetBossPartTip(...)`
- Consumes: `CombatAdvisorLogic.GetSquadTip(...)`
- Produces: `CombatAdvisorUI.Bind(Text bodyText, Image accentImage)`

- [ ] Subscribe to `OnBossPartHpChanged`, `OnReloadStarted`, `OnReloadComplete`, `OnMemberDied`, `OnBossEnraged`, and `OnGameEnded`.
- [ ] Track current boss tip, reloading count, down count, and temporary alert text.
- [ ] Wire a `CombatAdvisorPanel` between wall/wave HUD and squad panel.

### Task 3: Validation And Release Refresh

**Files:**
- Existing build outputs under `Build/`, root, `release/`, and Drive.

- [ ] Run EditMode tests where Unity licensing allows it.
- [ ] Rebuild scenes with `SceneBuilder.Build`.
- [ ] Build Windows player with `BuildScript.BuildWindows`.
- [ ] Copy exe and matching `_Data` folder to root/release portable names.
- [ ] Launch root and release portable exe for 8 seconds and check logs for `corrupt`, `level0`, `Crash`, `exception`.
