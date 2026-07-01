# SquadVsMonster 기획서

문제 정의: 캐릭터 수집형 전투를 좋아하는 플레이어가 조준, 배치, 보스 패턴 대응을 더 직접적으로 느끼고 싶어 한다.

## 게임 소개
스쿼드 배치와 보스 약점 공략을 강조한 라인 전투 게임.

SquadVsMonster의 핵심 매력은 한 번의 선택이 다음 장면의 위험도, 보상, 성장 방향으로 이어지는 구조다. 이 문서는 처음 보는 사람에게 게임의 재미와 현재 방향을 빠르게 소개하기 위한 단일 기획서이며, 세부 변경 이력은 별도 업데이트 내역서에서 관리한다.

## 한 줄 소개
스쿼드 배치와 보스 약점 공략을 강조한 라인 전투 게임.

## 핵심 루프
유저가 현재 전장의 정보를 읽고 선택을 하면 전투/운영 결과가 갱신되고, 그 보상과 손실 때문에 다시 다음 선택을 준비한다.

## 게임 플레이 예시
- 1단계: 플레이어가 SquadVsMonster의 현재 목표, 보유 자원, 즉시 대응해야 할 위험을 확인한다.
- 2단계: 카드, 유닛, 배치, 명령, 이동 중 현재 상황에 맞는 핵심 행동을 선택한다.
- 3단계: 선택 결과가 전투, 운영, 보상, 손실로 즉시 갱신되고 다음 판단의 근거가 된다.
- 4단계: 획득한 보상이나 변화한 상태를 바탕으로 다음 선택을 준비하며 핵심 루프를 반복한다.
- 플레이 감각: 짧은 세션 안에서 상황 파악, 의미 있는 선택, 즉각적인 피드백, 다음 목표 제시가 끊기지 않는 흐름을 지향한다.

## 핵심 재미
- 읽기 쉬운 상황 판단: 지금 위험한 요소와 얻을 수 있는 보상이 한눈에 들어온다.
- 직접적인 선택 피드백: 선택 직후 전투, 점수, 자원, 성장 상태가 변해 손맛을 만든다.
- 누적되는 성장감: 반복 플레이가 단순 재시작이 아니라 다음 전략의 재료로 이어진다.

## 주요 시스템
- 핵심 선택 시스템: 현재 국면에서 가능한 행동을 5개 이하의 명확한 선택지로 제시한다.
- 위험/보상 피드백: 행동 전후의 이득, 손실, 위협 변화를 빠르게 보여준다.
- 성장과 해금: 세션 결과가 능력, 카드, 유닛, 건물, 장비, 스테이지 등 다음 플레이의 선택지를 넓힌다.
- 상태별 UX: 로딩, 빈 상태, 오류, 많은 데이터, 긴 텍스트 상황에서도 레이아웃이 무너지지 않도록 관리한다.
- 실행 안정성: 테스트와 빌드 산출물을 기준으로 현재 플레이 가능한 범위를 계속 확인한다.

## 게임 구성과 규칙 (GDD 통합)
- 통합 기준 문서: `02-design/features/squad-vs-monster.design.md`
- 작성 기준: 16_PokerStrike_GDD처럼 화면 구조, 핵심 시스템, 진행/승패 규칙, UI/HUD, 미결 항목을 한 문서에서 바로 읽을 수 있게 정리한다.

### 화면/플레이 구조
- **1.1 기술 스택 확정** (02-design/features/squad-vs-monster.design.md)
  - Phaser 3.60+ (CDN)
  - ├── Arcade Physics — 총알/잡몹 충돌 (AABB 기반, 경량)
  - ├── Scene Plugin — 씬 분리 (Game + UI 동시 실행)
  - ├── Tween Manager — HP바 보간, 스케일 이펙트
  - ├── Particle Manager — 총구 화염, 피격, 폭발
  - └── Input Plugin — Pointer (마우스/터치) 드래그
  - 빌드 없음 → index.html에서 CDN import
- **1.2 씬(Scene) 구성** (02-design/features/squad-vs-monster.design.md)
  - └── 에셋 preload (이미지, 오디오)
  - └── → MenuScene
  - └── 타이틀, 시작 버튼
  - └── → GameScene + UIScene (병렬 실행)
  - GameScene [메인 게임 로직]
  - UIScene [HUD 오버레이 — GameScene 위에 항상 실행]
  - └── 두 씬 병렬 실행: scene.launch('UIScene')
- **1.3 GameScene 렌더 레이어 (Depth 값)** (02-design/features/squad-vs-monster.design.md)
| Depth | 레이어 | 포함 요소 |
|-------|--------|-----------|
| 0 | Background | 하늘, 도시 원경 (정적 이미지) |
| 10 | Far Buildings | 원거리 빌딩 실루엣 (시차 스크롤 없음, 고정) |
| 20 | Terrain | 도심 건물, 장벽, 바리케이드 |
| 30 | Minions | 잡몹 3종 |
| 40 | Boss | 보스 본체 + 부위 히트박스 |

### 핵심 시스템
- **2.1 Entity 클래스 계층** (02-design/features/squad-vs-monster.design.md)
  - Phaser.GameObjects.Container
  - ├── Boss (보스 본체)
  - │ ├── BossPart[] (7개 부위 히트박스, Zone 사용)
  - │ └── BossAnimator (상태별 애니메이션 제어)
  - ├── Minion (기본 잡몹 클래스)
  - │ ├── Runner extends Minion
  - │ ├── Berserker extends Minion
  - │ └── Spitter extends Minion
  - └── SquadMember (스쿼드 기본 클래스)
  - ├── Alpha extends SquadMember (저격소총)
- **2.2 Boss 클래스 상세** (02-design/features/squad-vs-monster.design.md)
  - 코드 예시는 원본 설계 문서를 참조한다.
  - class Boss extends Phaser.GameObjects.Container {
  - hp: number // 현재 전체 HP
  - maxHp: number // 최대 HP (1500)
  - x: number // 현재 X 위치 (원거리 → 근거리)
  - scale: number // 거리에 따른 스케일 (0.3 ~ 1.2)
  - state: BossState // WALKING | ATTACKING | STUNNED | ENRAGED | DYING
  - isEnraged: boolean
  - parts: Map<PartId, BossPart> // 7개 부위
  - animator: BossAnimator
- **2.5 Minion 클래스 상세** (02-design/features/squad-vs-monster.design.md)
  - 코드 예시는 원본 설계 문서를 참조한다.
  - class Minion extends Phaser.Physics.Arcade.Sprite {
  - minionType: 'runner' | 'berserker' | 'spitter'
  - maxHp: number
  - speed: number
  - damage: number
  - attackTarget: 'barricade' | 'wall' | 'squad' | null
  - attackTimer: number
  - update(delta): void
  - decideTarget(terrain, squad): void // 행동 우선순위 결정
- **2.6 Terrain 클래스** (02-design/features/squad-vs-monster.design.md)
  - 코드 예시는 원본 설계 문서를 참조한다.
  - class Terrain extends Phaser.GameObjects.Sprite {
  - terrainType: 'building' | 'wall' | 'barricade'
  - maxHp: number
  - isDestroyed: boolean
  - // 상태별 텍스처 키 배열
  - // [HP>70%: normal, HP30-70%: cracked, HP<30%: critical, HP=0: destroyed]
  - stateTextures: string[]
  - takeDamage(amount): void
  - updateVisual(): void // HP에 따라 텍스처 변경

### 진행/승패 규칙
- **진행 규칙** (기획서)
  - 한 세션은 상황 확인, 선택, 결과 피드백, 보상 또는 손실 반영, 다음 선택 준비의 흐름으로 닫힌다.
  - 승패나 종료 조건은 실제 구현 상태가 확인될 때 세부 수치와 함께 보강한다.

### UI/HUD/피드백
- **4.4 이펙트 파티클 정의** (02-design/features/squad-vs-monster.design.md)
  - 코드 예시는 원본 설계 문서를 참조한다.
  - // 이펙트 목록 및 Phaser Particle Emitter 설정 요약
  - muzzleFlash: {
  - // 총구 화염 — 매 사격마다 짧게
  - lifespan: 80, speed: 100, scale: { start:0.4, end:0 },
  - quantity: 5, tint: 0xFFAA00
  - bulletImpact: {
- **5.2 UI 컴포넌트 목록** (02-design/features/squad-vs-monster.design.md)
  - 코드 예시는 원본 설계 문서를 참조한다.
  - UIScene 내 컴포넌트:
  - BossHpBar // 상단 보스 HP 바 + 광폭화 경계선
  - BossDistanceText // "거리: 850m" 텍스트
  - EnrageWarning // "ENRAGE!" 경고 텍스트 (빨간색, 깜빡임)
  - WallBreachWarning // "WALL BREACHED!" 경고
  - // 하단 스쿼드 HUD — 5개

### 구현 메모/미결
- **[Design] Squad vs Monster — 기술 설계 문서** (02-design/features/squad-vs-monster.design.md)
  - > Plan 참조: `docs/01-plan/features/squad-vs-monster.plan.md`

## MVP 가설
| 기능 | 검증할 가설 | 검증 방법 |
|------|-------------|-----------|
| 핵심 전투/운영 루프 | 플레이어는 한 판 안에서 선택 결과를 이해하면 다음 판을 자발적으로 시작한다. | 1회 플레이 후 재시작률 60% 이상 |
| 위험/보상 표시 | 위험과 보상이 동시에 보이면 선택 시간이 줄고 납득도가 오른다. | 주요 선택 평균 8초 이내, 결과 불만 피드백 20% 이하 |
| 성장 보상 | 보상이 다음 전략을 바꾸면 반복 플레이 피로가 낮아진다. | 3판 내 서로 다른 빌드 선택률 50% 이상 |

## 레퍼런스 분석
- 장르 기준 레퍼런스는 한 판 시작까지 3단계 이내, 첫 의미 있는 선택까지 30초 이내가 목표다.
- 적용 교훈: 규칙 설명보다 먼저 선택 가능한 상황을 보여주고, 결과 화면에서 다음 판의 개선 포인트를 바로 제안한다.

## 현재 개발 상태 예상 수치
- 완성 목표 대비 구현 체감도: 약 57%
- 첫 세션에서 핵심 루프가 전달될 가능성: 약 63%
- UI/리소스 일관성 체감: 약 53%
- 콘텐츠와 반복 플레이 분량 충족도: 약 53%
- 빌드/실행 안정성 기대치: 약 67%
- 해석 기준: 현재 문서, 최근 산출물 기록, 연결된 예시 이미지 유무를 기준으로 한 사전 추정치이며 실제 플레이 테스트 후 ±15%p 정도 보정이 필요하다.

- 첫 세션 평균 플레이 시간 8분 이상
- 첫 세션 내 2회차 진입률 55% 이상
- 핵심 선택 화면에서 무응답/이탈률 15% 이하

## 현재 구현 상태
- 이 문서는 2026-06-24 기준으로 현재 플레이 방향과 구현 체감 상태를 요약한다.
- 핵심 루프, 조작 원칙, 리소스 적용 현황, 빌드 기준은 프로젝트별 실제 구현과 산출물 기록을 기준으로 계속 보정한다.
- 세부 변경 이력은 별도 업데이트 내역서에서 관리하고, 본 기획서는 처음 보는 사람이 현재 방향을 빠르게 이해하는 공유 문서로 유지한다.
- 새 기능, 밸런스 변경, 리소스 교체, UX 개선이 들어가면 본문과 HTML 문서를 함께 갱신한다.

## 조작과 UX 원칙
- 주요 버튼은 44px 이상으로 유지하고, 화면당 CTA 강조색은 하나만 사용한다.
- 버튼/선택지는 한 번에 5개 이하로 노출해 판단 부담을 줄인다.
- 로딩, 빈 상태, 에러, 많은 데이터, 긴 텍스트 상태를 각각 별도 화면/컴포넌트로 확인한다.
- HUD 동일 레이어 요소는 겹치지 않게 배치하고, 겹침이 필요한 효과는 별도 depth/z-order를 쓴다.

## 적용 리소스
- 런타임에 쓰이는 대표 이미지와 UI 리소스는 프로젝트별 asset/public/Resources 경로를 기준으로 관리한다.
- 새 이미지가 필요할 때는 프로젝트 접두어를 포함한 lowercase kebab-case 파일명을 사용한다.
- 최종 런타임 비주얼은 PNG/WebP 등 비트맵 자산을 우선 사용하고, SVG 또는 코드 드로잉은 문서/임시 참조로만 남긴다.

## 공유용 이미지 미리보기
![SquadVsMonster 공유용 예시 1](archive/SquadVsMonster_gameplay_preview_v1.png)

![SquadVsMonster 공유용 예시 2](SquadVsMonster_01_플레이예시.png)

- Assets/Sprites/Character/character (1).png
- Assets/Sprites/Character/character (2).png
- Assets/Sprites/Character/character (3).png

## 빌드, 테스트, 릴리스
- 프로젝트별 엔진/에디터 빌드 절차를 따른다.
- 문서 전용 갱신에서는 실행 파일을 새로 생성하지 않았다.

## 남은 리스크와 다음 우선순위
- 첫 화면에서 게임의 목표와 다음 행동이 5초 안에 보이는지 확인한다.
- 주요 선택의 결과 예측과 실제 결과가 어긋나는 지점을 플레이 테스트로 수집한다.
- 기획서에 남아 있던 변경 이력성 내용은 업데이트 내역서로 계속 이동해 소개 문서의 밀도를 유지한다.
