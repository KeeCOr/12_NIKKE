# SquadVsMonster 다음 개선 지시서
최신화: 2026-07-16 KST

## 목표

v1.6.0에서 구현된 result cue line과 squad composition preview가 실제 Unity 화면에서 페르소나에게 빠르게 읽히는지 확인한다. 새 결과 로직을 다시 만들기보다, ResultUI/HUD 시각 스모크와 병목별 시각 피드백을 우선한다.

## 현재 해결된 문제

- CombatAdvisorHUD는 전투 전 조합 화력, 방어, 포지션 프리뷰를 표시한다.
- 전투 중에는 보스 약점, 방벽 위협, 재장전/다운 상태 조언을 표시한다.
- `CombatAdvisorLogic.GetWaveResultSummary`는 승패 원인, 다음 조정, cue line을 만든다.
- `GameManager.LastWaveResultSummary`, `CombatAdvisorUI`, `ResultUI`로 결과 요약 경로가 연결되어 있다.
- current-facing GDD와 업데이트 내역서를 readable UTF-8 한국어로 정리했다.

## 다음 구현/검증 범위

- ResultUI에서 결과 타이틀, 원인, cue line, 다음 조정 텍스트가 모두 보이는지 확인한다.
- win-by-composition, loss-by-survival, loss-by-reload-damage, loss-by-timing 4개 케이스를 스모크 기준으로 둔다.
- 버튼, 결과 텍스트, HUD 오버레이가 같은 depth에서 겹치지 않는지 확인한다.
- 병목별 작은 아이콘 또는 색상 태그를 추가해 텍스트 의존도를 낮춘다.
- 새 캐릭터, 새 가챠 구조, 전체 BM 재설계는 다음 배치 범위가 아니다.

## 검증 기준

- Unity Editor 또는 BatchMode가 가능하면 EditMode 테스트와 Windows 빌드를 실행한다.
- Unity가 잠겨 있거나 라이선스가 막히면, 차단 사유를 명확히 기록하고 성공으로 주장하지 않는다.
- UI 변경이 발생하면 기획서와 업데이트 내역서를 동시에 갱신한다.
- 런타임 코드 변경이 있으면 package/release/Drive/Git 상태를 함께 확인한다.