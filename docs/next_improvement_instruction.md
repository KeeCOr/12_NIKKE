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

## 2026-09-18 프로젝트별 고유 개선 3개
> Unity 제외 조건: 이번 반영은 설계·씬·검증 명세이며 런타임 구현 완료를 뜻하지 않는다.

1. 편성 카드에 화력·생존·사거리·포지션 장단점 표시
2. 몬스터 숫자·크기·속성에 따른 조합 상성 미리보기
3. 전투 후 가장 기여한 편성 변경과 실패 원인 설명

## 2026-09-21 완료 및 다음 후보

- 소스 완료: `SquadReasonChip`과 `GetReasonChips()`로 화력·생존·사거리·포지션 네 이유를 분리했다.
- 소스 완료: 기존 CombatAdvisor 텍스트 바인딩에서 네 칩을 고정 순서로 표시한다. 씬/메타는 변경하지 않았다.
- 예시 화면: `docs/design-references/2026-09-21-squad-reason-chips.png`
- 독립 검증: .NET 9 로직 검사 통과.
- 미검증: Unity EditMode, 실제 씬 시각 QA, 빌드/패키징.
- 다음 후보: 몬스터 숫자·크기·속성을 입력으로 받아 편성 상성 경고를 전투 전 칩에 결합한다.
