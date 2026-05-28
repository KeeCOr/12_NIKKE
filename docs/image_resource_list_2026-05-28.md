# SquadVsMonster 이미지 리소스 목록

## 목적
- 예시 이미지의 어두운 SF 전장/HUD 분위기를 현재 Unity 프로젝트에 적용하기 위한 1차 이미지 리소스 세트.
- 기존 캐릭터, 보스, 미니언 원화는 유지하고 UI 프레임, 스킬 아이콘, 전장 오브젝트를 보강한다.

## 생성 완료 리소스

| 구분 | 파일 | 용도 | 상태 |
| --- | --- | --- | --- |
| HUD/UI 아틀라스 | `Assets/Sprites/UI/Generated/ui_hud_atlas_v1.png` | 보스 HP바, 스쿼드 카드, 상태 패널, 미니맵 프레임, 버튼 프레임, 장식 라인 | 생성 완료 |
| 스킬/상태 아이콘 아틀라스 | `Assets/Sprites/UI/Generated/skill_status_icons_atlas_v1.png` | 폭격, EMP, 중력장, 화염 폭풍, 잠금, 방어, 탄약, 재장전, 약점, 타이머 등 24종 아이콘 | 생성 완료 |
| 전장 오브젝트 아틀라스 | `Assets/Sprites/Object/Generated/battlefield_props_atlas_v1.png` | 금속 바리케이드, 콘크리트 엄폐물, 보급 상자, 기술 파일런, 바닥 패널, 균열/탄흔/파편 데칼 | 생성 완료, 투명 PNG |
| 전장 오브젝트 원본 | `Assets/Sprites/Object/Generated/battlefield_props_atlas_v1_chromakey.png` | 투명 처리 전 원본 보관 | 생성 완료 |

## 게임 적용 리소스

| 구분 | 적용 위치 | 내용 |
| --- | --- | --- |
| HUD 프레임 | `Assets/Scenes/Game.unity` | 보스 HP 패널, 방벽 HP 패널, 웨이브 패널, 폭격 패널 배경에 생성 UI 프레임 적용 |
| 스쿼드 카드 | `Assets/Scenes/Game.unity` | Alpha~Echo 하단 카드 배경을 캐릭터 컬러별 생성 프레임으로 교체 |
| 스킬 버튼 | `Assets/Scenes/Game.unity` | 폭격 버튼에 원형 프레임과 공습 아이콘 적용 |
| 보스 파츠 UI | `Assets/Scenes/Game.unity` | HEAD, ARM, LEG, CHEST, CORE 표시 타일에 생성 아이콘 추가 |
| 전술 상태 UI | `Assets/Scenes/Game.unity` | 우측에 DEFENSE, AUTO AIM, SPEED 상태 스택 추가 |
| 미니맵 UI | `Assets/Scenes/Game.unity` | 우측 하단에 생성 미니맵 프레임과 아군/적 마커 추가 |
| 전장 오브젝트 | `Assets/Scenes/Game.unity` | 바리케이드, 로드블록, 보급 상자, 파일런, 바닥 패널, 균열/탄착 장식 적용 |

## 적용 도구
- `SquadVsMonster/Apply Generated Visuals` 메뉴로 생성 이미지 리소스를 현재 Game 씬에 다시 적용할 수 있다.
- 배치 실행 메서드 `GeneratedVisualApplicator.ApplyGeneratedVisualsCLI`도 제공한다.

## 권장 후속 리소스
- UI 슬라이스용 개별 스프라이트: 위 아틀라스에서 보스 HP 프레임, 카드 프레임, 버튼 프레임을 개별 PNG로 분리.
- 캐릭터 초상화 카드: 하단 스쿼드 카드에 들어갈 5인 초상화 전용 컷.
- 보스 파츠 아이콘: HEAD, ARM, LEG, CHEST, CORE 등 현재 텍스트 기반 파츠 표시를 아이콘화.
- 전투 VFX 시트: 총구 화염, 탄착, 폭발, EMP 링, 쉴드 히트 이펙트.
- 미니맵 마커: 아군, 적, 보스, 방어선, 위험 구역용 작은 마커.

## 생성 프롬프트 방향
- 특정 게임이나 브랜드를 모방하지 않는 오리지널 어두운 SF 전술 슈터 톤.
- 컬러 포인트는 cyan, amber, red, purple, green을 역할별로 분리.
- UI 요소에는 읽을 수 있는 글자, 숫자, 로고를 넣지 않는다.
- 전장 오브젝트는 Unity 2D 배치가 쉽도록 개별 요소 간 패딩을 확보한다.
