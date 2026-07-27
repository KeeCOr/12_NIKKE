# Next Improvement Instruction

## Scope
SquadVsMonster formation screen only. Do not edit Unity scenes or meta files unless Unity Editor is available and EditMode tests can run.

## Goal
Add pre-battle composition reason chips for Firepower, Survival, Range, and Position so the player understands why a squad change matters before combat.

## Safe implementation path
1. Add a test in `Assets/Tests/EditMode/CombatAdvisorLogicTests.cs` for four chips generated from `CombatAdvisorLogic.GetSquadCompositionPreview`.
2. Extend `Assets/Scripts/UI/CombatAdvisorLogic.cs` with a chip string array or small chip DTO.
3. Bind the chips through `CombatAdvisorUI` only if the existing generated scene already has safe text slots. Otherwise keep this as logic-only until Unity scene editing is available.
4. Verify with Unity EditMode tests.

## Blocker this batch
Unity batchmode could not run because no valid Unity Editor license was available.
