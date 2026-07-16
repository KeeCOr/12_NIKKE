# SquadVsMonster 업데이트 내역서

최신화: 2026-07-16 KST

## 2026-07-16 / v1.6.0 Result Cue Line And Advisor Density

- `WaveResultSummary`에 `cueLine`을 추가해 승패 결과가 생존, 화력, 재장전, 타이밍 중 어디에서 갈렸는지 한 줄로 강조한다.
- `ResultUI`는 원인 텍스트 아래 cue line을 함께 표시하고, `CombatAdvisorUI`의 전투 종료 알림은 cue line을 우선 노출한다.
- `CombatAdvisorLogicTests`에 cue line 3개 케이스를 추가해 승리, 생존 붕괴, 재장전 화력 병목을 회귀 검증한다.
- `SquadCompositionPreview`는 전투 전 편성의 화력/방어/포지션 성향을 HUD로 보여주는 기존 개선을 v1.6.0 문서 기준에 포함했다.
- 기획서와 업데이트 내역서를 현재 구현 상태 기준의 readable UTF-8 문서로 다시 정리했다.

## 2026-07-03 / Current GDD Readability And ResultUI Smoke Plan

- current-facing `SquadVsMonster_기획서.md/html`을 readable UTF-8 한국어로 복구했다.
- 문제 정의, 주 페르소나, 핵심 루프, MVP 가설, 레퍼런스 분석, KPI를 다시 정리했다.
- 이미 존재하는 wave result 경로를 명시했다: `CombatAdvisorLogic.GetWaveResultSummary`, `GameManager.LastWaveResultSummary`, `CombatAdvisorUI`, `ResultUI`.
- 다음 작업을 새 결과 로직 구현이 아니라 Unity ResultUI/HUD 시각 스모크로 고정했다.
- 이번 배치는 docs-only이므로 실행파일은 새로 만들지 않았다.

## 2026-07-02 / Persona Recheck

- 스쿼드 자동전투 팬 페르소나 기준으로 결과 요약 모델이 누락된 상태는 아님을 확인했다.
- 남은 리스크는 실제 ResultUI에서 요약 텍스트가 보이고, 버튼과 겹치지 않으며, 목표 해상도에서 읽히는지 확인하는 것이다.

## 2026-07-01 / v1.5.0 Squad Composition Preview

- 편성/전투 진입 전 CombatAdvisorHUD가 조합 단위 화력, 방어, 포지션 프리뷰를 보여주도록 추가했다.
- `CombatAdvisorLogic.GetSquadCompositionPreview`가 무기 DPS, HP, 사거리, 특수 역할, 약점 우선순위를 합산한다.
- `CombatAdvisorUI`는 전투 신호 전에는 프리뷰를 표시하고, 전투 신호 이후에는 기존 보스/분대 상태 조언으로 전환한다.
- 테스트: `CombatAdvisorLogicTests`에 화력 A, 방어 C, 혼합 포지션 3개 케이스를 추가했다.

## 2026-06-30 / v1.4.0 Wave Result Advisory

- 전투 종료 결과 요약 로직을 추가했다.
- 승패 원인을 화력, 생존, 재장전/포지셔닝 축으로 분리해 설명한다.
- CombatAdvisorHUD 종료 알림과 ResultUI가 같은 결과 요약을 사용하도록 연결했다.
- 검증: Unity Editor 라이선스 차단으로 EditMode 테스트/빌드는 미실행, `_temp/logic-check` .NET 검증 PASS.

## 2026-06-30 / Barricade Resource Replacement

- 적용 자산: `Assets/Sprites/Object/Barricade_1.png`
- 적용 방식: `Game.unity`가 참조 중인 GUID를 유지하고 PNG만 교체해 prefab/scene 참조를 보존했다.
- 목표: 반복 노출되는 방벽 리소스의 SF 전장 가독성과 경고 톤을 강화했다.

## 2026-06-24 / Document Split

- 기획서와 업데이트 내역서를 분리했다.
- 기획서는 게임 소개, 핵심 루프, MVP 가설, KPI, UX 원칙 중심으로 유지한다.
- 변경 이력, 구현 로그, 검증 기록은 업데이트 내역서에서 관리한다.