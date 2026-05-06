# [Design] Squad vs Monster — 기술 설계 문서

> Plan 참조: `docs/01-plan/features/squad-vs-monster.plan.md`

---

## 1. 전체 아키텍처

### 1.1 기술 스택 확정

```
Phaser 3.60+ (CDN)
  ├── Arcade Physics — 총알/잡몹 충돌 (AABB 기반, 경량)
  ├── Scene Plugin — 씬 분리 (Game + UI 동시 실행)
  ├── Tween Manager — HP바 보간, 스케일 이펙트
  ├── Particle Manager — 총구 화염, 피격, 폭발
  └── Input Plugin — Pointer (마우스/터치) 드래그

빌드 없음 → index.html에서 CDN import
```

### 1.2 씬(Scene) 구성

```
BootScene
  └── 에셋 preload (이미지, 오디오)
  └── → MenuScene

MenuScene
  └── 타이틀, 시작 버튼
  └── → GameScene + UIScene (병렬 실행)

GameScene          [메인 게임 로직]
UIScene            [HUD 오버레이 — GameScene 위에 항상 실행]
  └── 두 씬 병렬 실행: scene.launch('UIScene')

ResultScene
  └── WIN / LOSE 화면
  └── 재시작 → GameScene + UIScene
```

### 1.3 GameScene 렌더 레이어 (Depth 값)

| Depth | 레이어 | 포함 요소 |
|-------|--------|-----------|
| 0 | Background | 하늘, 도시 원경 (정적 이미지) |
| 10 | Far Buildings | 원거리 빌딩 실루엣 (시차 스크롤 없음, 고정) |
| 20 | Terrain | 도심 건물, 장벽, 바리케이드 |
| 30 | Minions | 잡몹 3종 |
| 40 | Boss | 보스 본체 + 부위 히트박스 |
| 50 | Bullets | 스쿼드 총알 |
| 60 | MinionBullets | 잡몹 투사체 (Spitter) |
| 70 | Squad | 스쿼드 5명 (화면 하단 고정) |
| 80 | Effects | 파티클, 이펙트 (총구화염, 피격, 폭발) |
| 90 | AimIndicators | 조준선, 크로스헤어 |

---

## 2. 클래스 설계

### 2.1 Entity 클래스 계층

```
Phaser.GameObjects.Container
  ├── Boss                  (보스 본체)
  │     ├── BossPart[]      (7개 부위 히트박스, Zone 사용)
  │     └── BossAnimator    (상태별 애니메이션 제어)
  │
  ├── Minion                (기본 잡몹 클래스)
  │     ├── Runner extends Minion
  │     ├── Berserker extends Minion
  │     └── Spitter extends Minion
  │
  └── SquadMember           (스쿼드 기본 클래스)
        ├── Alpha extends SquadMember   (저격소총)
        ├── Bravo extends SquadMember   (돌격소총)
        ├── Charlie extends SquadMember (산탄총)
        ├── Delta extends SquadMember   (로켓런처)
        └── Echo extends SquadMember    (저격+마킹)

Phaser.GameObjects.Image / Sprite
  ├── Terrain               (지형지물 — Container 불필요)
  │     ├── Building        (도심 건물)
  │     ├── Wall            (장벽)
  │     └── Barricade       (바리케이드)
  │
  └── Bullet                (Phaser.Physics.Arcade.Image 사용)
```

### 2.2 Boss 클래스 상세

```javascript
class Boss extends Phaser.GameObjects.Container {
  // 상태
  hp: number              // 현재 전체 HP
  maxHp: number           // 최대 HP (1500)
  x: number               // 현재 X 위치 (원거리 → 근거리)
  scale: number           // 거리에 따른 스케일 (0.3 ~ 1.2)
  state: BossState        // WALKING | ATTACKING | STUNNED | ENRAGED | DYING
  isEnraged: boolean

  // 컴포넌트
  parts: Map<PartId, BossPart>   // 7개 부위
  animator: BossAnimator

  // 메서드
  update(delta): void
  takeDamage(partId, amount): void
  checkEnrageCondition(): boolean    // HP<=50% AND (HEAD||CHEST destroyed)
  triggerEnrage(): void
  attackWall(): void                 // 장벽 공격 행동
  summonMinions(): void

  // 스케일 계산 (원근감)
  // x=1200(원거리) → scale=0.3, x=200(근거리) → scale=1.2
  updateScale(): void
}

type BossState = 'WALKING' | 'ATTACKING' | 'STUNNED' | 'ENRAGED' | 'DYING'
type PartId = 'HEAD' | 'ARM_L' | 'ARM_R' | 'LEG_L' | 'LEG_R' | 'CHEST' | 'CORE'
```

### 2.3 BossPart 클래스 상세

```javascript
class BossPart {
  id: PartId
  hp: number
  maxHp: number
  damageMultiplier: number    // 1.0 / 1.5 / 2.0 / 3.0
  isDestroyed: boolean
  isActive: boolean           // CORE는 기본 false

  // 부위의 상대 좌표 (보스 스케일에 따라 같이 변함)
  offsetX: number             // 보스 중심으로부터 오프셋
  offsetY: number

  // Arcade Physics Zone (실제 충돌 영역)
  hitZone: Phaser.GameObjects.Zone

  takeDamage(amount: number): number   // 실제 적용 데미지 반환
  destroy(): void                       // 파괴 처리
  getWorldPosition(): { x, y }         // 보스 위치 + 오프셋 계산
}
```

**부위 오프셋 정의 (보스 기준 스케일 1.0 시)**

| 부위 | offsetX | offsetY | hitW | hitH |
|------|---------|---------|------|------|
| HEAD | 0 | -180 | 80 | 80 |
| ARM_L | -120 | -80 | 60 | 120 |
| ARM_R | +120 | -80 | 60 | 120 |
| LEG_L | -60 | +80 | 50 | 130 |
| LEG_R | +60 | +80 | 50 | 130 |
| CHEST | 0 | -60 | 100 | 100 |
| CORE | 0 | -60 | 60 | 60 (CHEST 파괴 후 활성) |

### 2.4 SquadMember 클래스 상세

```javascript
class SquadMember extends Phaser.GameObjects.Container {
  // 스탯
  id: string              // 'alpha' | 'bravo' | 'charlie' | 'delta' | 'echo'
  hp: number
  maxHp: number
  isAlive: boolean
  isReloading: boolean

  // 무기
  weapon: WeaponConfig    // 탄창, 공격력, 사격속도
  currentAmmo: number
  fireTimer: number       // 다음 사격까지 남은 시간(ms)
  reloadTimer: number

  // 조준
  aimTarget: BossPart | Minion | null   // 현재 조준 타겟
  focusTarget: BossPart | null          // 플레이어 지시 집중 조준

  // 위치 (화면 하단 고정)
  slotIndex: number       // 0~4, 좌→우 순서

  // 메서드
  update(delta): void
  autoAim(boss, minions): void    // 자동 조준 로직
  tryFire(): void                  // 발사 시도 (쿨다운 체크)
  fire(target): void               // 실제 발사
  startReload(): void
  takeDamage(amount): void
  die(): void
}

interface WeaponConfig {
  name: string
  magazineSize: number     // 탄창 크기
  damage: number           // 기본 데미지
  fireRate: number         // ms 간격
  reloadTime: number       // ms
  bulletSpeed: number
  bulletType: 'single' | 'shotgun' | 'rocket'
  spreadAngle?: number     // shotgun 산탄 각도
  splashRadius?: number    // rocket 범위
}
```

**WeaponConfig 인스턴스 값**

```javascript
const WEAPONS = {
  alpha: { name:'Bolt-Action', magazineSize:5,  damage:120, fireRate:2500, reloadTime:3000, bulletSpeed:900, bulletType:'single' },
  bravo: { name:'AR',          magazineSize:30, damage:30,  fireRate:150,  reloadTime:2000, bulletSpeed:700, bulletType:'single' },
  charlie:{ name:'Shotgun',    magazineSize:8,  damage:15,  fireRate:1200, reloadTime:1500, bulletSpeed:500, bulletType:'shotgun', spreadAngle:30 },
  delta: { name:'Rocket',      magazineSize:2,  damage:200, fireRate:4000, reloadTime:4000, bulletSpeed:400, bulletType:'rocket',  splashRadius:120 },
  echo:  { name:'DMR',         magazineSize:10, damage:60,  fireRate:1000, reloadTime:2000, bulletSpeed:800, bulletType:'single' }
}
```

### 2.5 Minion 클래스 상세

```javascript
class Minion extends Phaser.Physics.Arcade.Sprite {
  minionType: 'runner' | 'berserker' | 'spitter'
  hp: number
  maxHp: number
  speed: number
  damage: number
  attackTarget: 'barricade' | 'wall' | 'squad' | null
  attackTimer: number

  update(delta): void
  decideTarget(terrain, squad): void   // 행동 우선순위 결정
  moveTowardTarget(): void
  attackTarget(): void
  takeDamage(amount): void
  die(): void
}

// 잡몹 스탯
const MINION_CONFIGS = {
  runner:    { hp:80,  speed:180, damage:15, attackInterval:800 },
  berserker: { hp:200, speed:80,  damage:40, attackInterval:1500 },
  spitter:   { hp:60,  speed:60,  damage:20, attackInterval:2000, range:400 }
}
```

### 2.6 Terrain 클래스

```javascript
class Terrain extends Phaser.GameObjects.Sprite {
  terrainType: 'building' | 'wall' | 'barricade'
  hp: number
  maxHp: number
  isDestroyed: boolean

  // 상태별 텍스처 키 배열
  // [HP>70%: normal, HP30-70%: cracked, HP<30%: critical, HP=0: destroyed]
  stateTextures: string[]

  takeDamage(amount): void
  updateVisual(): void      // HP에 따라 텍스처 변경
  collapse(): void          // 붕괴 애니메이션 + 잔해 생성
  getHpRatio(): number
}
```

---

## 3. 시스템 설계

### 3.1 AimSystem — 조준 입력 처리

```javascript
class AimSystem {
  scene: GameScene
  selectedMember: SquadMember | null    // 현재 선택된 캐릭터
  isDragging: boolean
  dragStartPos: { x, y }

  // 포인터 다운: 캐릭터 아이콘 클릭 → 선택
  onPointerDown(pointer): void

  // 포인터 업: 보스 부위 위에서 손을 떼면 → 집중 조준 설정
  onPointerUp(pointer): void

  // 실시간 드래그: 크로스헤어 + 조준선 업데이트
  onPointerMove(pointer): void

  // 드래그 중 어떤 BossPart 위에 있는지 판정
  getPartUnderPointer(pointer): BossPart | null

  // 집중 조준 설정: 선택 캐릭터 → 특정 부위
  setFocusTarget(member, part): void

  // 집중 조준 해제
  clearFocusTarget(member): void
}
```

**조준 우선순위 로직 (SquadMember.autoAim)**

```
1. focusTarget이 있고 살아있음 → focusTarget 유지
2. 잡몹이 일정 거리 이내(300px) 진입 → 가장 가까운 잡몹 자동 조준
3. focusTarget 없음 → 기본 조준:
   - Alpha/Echo: CORE(활성시) > HEAD > CHEST 순
   - Bravo: 잡몹 우선 > CHEST
   - Charlie: 가장 가까운 잡몹 우선 > LEG
   - Delta: 잡몹 군집 중심 > 현재 부위
```

### 3.2 FireSystem — 사격 및 장전

```javascript
class FireSystem {
  scene: GameScene
  bulletGroup: Phaser.Physics.Arcade.Group   // 오브젝트 풀

  update(delta, squadMembers): void

  // 각 멤버 사격 처리
  processMemberFire(member, delta): void

  // 총알 발사 (bulletType에 따라 분기)
  spawnBullet(member): void
  spawnShotgunBurst(member): void   // 6발 퍼짐
  spawnRocket(member): void         // 범위 폭발

  // 충돌 처리 (Arcade overlap)
  onBulletHitPart(bullet, partZone): void
  onBulletHitMinion(bullet, minion): void
  onRocketExplode(rocket, target): void   // 범위 내 모든 적 피해

  // 장전
  startReload(member): void
  onReloadComplete(member): void
}
```

**총알 풀링 구조**

```javascript
// GameScene.create() 에서
this.bulletPool = this.physics.add.group({
  classType: Bullet,
  maxSize: 200,        // 최대 풀 크기
  runChildUpdate: true
})

class Bullet extends Phaser.Physics.Arcade.Image {
  damage: number
  ownerId: string      // 발사한 캐릭터 ID
  bulletType: string
  splashRadius: number

  fire(x, y, angle, speed, damage): void
  onHit(): void    // 비활성화 → 풀 반환
}
```

### 3.3 WaveSystem — 잡몹 소환

```javascript
class WaveSystem {
  scene: GameScene
  minionPool: Phaser.Physics.Arcade.Group
  spawnTimer: number     // 다음 소환까지 남은 시간
  baseInterval: number   // 기본 소환 주기 (ms)

  // 보스 상태에 따른 소환 주기 조정
  getSpawnInterval(): number
  //  일반: 5000ms, 광폭화: 2500ms

  // 소환 위치: 보스 주변 랜덤 + 측면
  getSpawnPosition(): { x, y }

  // 소환 타입 결정 (가중치)
  // 일반: Runner 60%, Berserker 25%, Spitter 15%
  // 광폭화: Runner 40%, Berserker 35%, Spitter 25%
  pickMinionType(isEnraged): MinionType

  update(delta): void
  spawnMinion(type, x, y): Minion
}
```

### 3.4 EnrageSystem — 광폭화 관리

```javascript
class EnrageSystem {
  scene: GameScene
  boss: Boss
  hasEnraged: boolean   // 한 번만 발동

  checkConditions(): boolean
  //  return boss.hp <= boss.maxHp * 0.5
  //      && (boss.parts.get('HEAD').isDestroyed
  //          || boss.parts.get('CHEST').isDestroyed)

  triggerEnrage(): void
  //  boss.isEnraged = true
  //  boss.speed *= 1.5
  //  boss.attackPower *= 1.8
  //  waveSystem.baseInterval /= 2
  //  playEnrageEffect()   ← 붉은 오라 파티클 + 카메라 흔들림

  playEnrageEffect(): void
  //  scene.cameras.main.shake(500, 0.02)
  //  ParticleSystem.playEnrageAura(boss)
  //  UIScene.showEnrageWarning()

  update(): void   // update마다 조건 체크 (아직 발동 안 됐을 때)
}
```

### 3.5 TerrainSystem — 지형 관리

```javascript
class TerrainSystem {
  scene: GameScene
  buildings: Building[]    // 2~3개 도심 건물
  wall: Wall               // 장벽 1개
  barricades: Barricade[]  // 2개

  // 보스가 건물/장벽 앞에서 공격
  bossDamageBuilding(boss): void
  bossDamageWall(boss): void

  // 잡몹 → 바리케이드 공격
  minionDamageBarricade(minion, barricade): void

  // 건물 붕괴 시 장벽에 연쇄 피해
  onBuildingCollapse(building): void
  //  wall.takeDamage(wall.maxHp * 0.1)
  //  spawnDebris(building.x, building.y)

  // 장벽 파괴 시 효과
  onWallDestroyed(): void
  //  squad.loseDefenseBonus()
  //  UIScene.showWallBreachWarning()

  // 바리케이드 파괴 시 효과
  onBarricadeDestroyed(barricade): void
  //  runners can now target squad directly
}
```

---

## 4. 렌더링 및 시각 설계

### 4.1 45도 쿼터뷰 원근감 구현

보스가 멀리 있을 때 작게, 가까워질수록 크게 보이는 효과:

```javascript
// Boss.updateScale() — GameScene.update()마다 호출
const BOSS_START_X = 1200     // 시작 위치 (화면 오른쪽 바깥)
const BOSS_END_X = 350        // 장벽 앞에 멈추는 위치
const MIN_SCALE = 0.25
const MAX_SCALE = 1.3

updateScale() {
  const progress = 1 - (this.x - BOSS_END_X) / (BOSS_START_X - BOSS_END_X)
  this.scale = MIN_SCALE + (MAX_SCALE - MIN_SCALE) * progress
  // 부위 히트존도 같이 스케일 적용
  this.parts.forEach(part => part.hitZone.setScale(this.scale))
}
```

### 4.2 보스 비주얼 구성 (스프라이트 없을 때 도형 대체)

```
스프라이트 미완성 시 도형 기반 프로토타입:
- 몸통: 직사각형 (회색)
- 머리: 원 (회색)
- 팔: 직사각형 (회색, 기울기)
- 다리: 직사각형 (짙은 회색)
- CORE: 원 (밝은 노란색, CHEST 파괴 후만 표시)
- 부위파괴 후: 빨간색 X 표시 + 해당 부위 어둡게

광폭화 비주얼:
- 보스 전체에 빨간색 outline tint
- 보스 눈 위치에 빨간 파티클 (emitter)
- 주변에 불꽃/오라 파티클
```

### 4.3 스쿼드 비주얼 (45도 쿼터뷰)

```
화면 하단 고정. 좌→우 순서: Alpha, Bravo, Charlie, Delta, Echo
각 캐릭터 간격: 화면 너비 / 6 (양쪽 여백 포함)

캐릭터당 구성:
  ├── 뒷모습 스프라이트 (또는 도형: 상체 직사각형+하체)
  ├── 총기 스프라이트 (오른쪽 어깨 옆)
  ├── 장전 애니메이션 (재장전 중 총기 내리는 모션)
  └── HP 바 (캐릭터 머리 위)

조준선 (AimIndicator):
  - 각 캐릭터 색상으로 구분 (Alpha:파랑, Bravo:초록, Charlie:노랑, Delta:빨강, Echo:보라)
  - 캐릭터 총구 위치 → 현재 조준 타겟 위치
  - 드래그 중: 실선 + 크로스헤어
  - 자동 조준: 반투명 점선
```

### 4.4 이펙트 파티클 정의

```javascript
// 이펙트 목록 및 Phaser Particle Emitter 설정 요약

effects = {
  muzzleFlash: {
    // 총구 화염 — 매 사격마다 짧게
    lifespan: 80, speed: 100, scale: { start:0.4, end:0 },
    quantity: 5, tint: 0xFFAA00
  },
  bulletImpact: {
    // 일반 피격 — 하얀 불꽃
    lifespan: 200, speed: 60, scale: { start:0.3, end:0 },
    quantity: 8, tint: 0xFFFFFF
  },
  weakpointHit: {
    // 약점 피격 — 빨간+노란 폭발
    lifespan: 400, speed: 150, scale: { start:0.6, end:0 },
    quantity: 20, tint: [0xFF0000, 0xFF6600, 0xFFFF00]
  },
  partDestroy: {
    // 부위파괴 — 검은 연기 + 불꽃
    lifespan: 800, speed: 80, scale: { start:0.8, end:0.1 },
    quantity: 30, tint: [0x222222, 0xFF4400, 0xFFAA00]
  },
  enrageAura: {
    // 광폭화 오라 — 연속 방출
    lifespan: 600, speed: 30, scale: { start:0.5, end:0 },
    frequency: 50, tint: [0xFF0000, 0xAA0000]
  },
  rocketExplosion: {
    // 로켓 폭발 — 큰 범위
    lifespan: 600, speed: 200, scale: { start:1.0, end:0 },
    quantity: 40, tint: [0xFF6600, 0xFF0000, 0x888888]
  },
  buildingCollapse: {
    // 건물 붕괴 — 먼지 + 잔해
    lifespan: 1200, speed: 120, scale: { start:1.0, end:0 },
    quantity: 50, tint: [0x888888, 0xAAAAAA, 0x666666]
  }
}
```

---

## 5. UI 설계 (UIScene)

### 5.1 UIScene 레이아웃

```
┌─────────────────────────────────────────────────────────┐
│ [BOSS HP 바 — 상단 중앙]                                 │
│ ████████████████████████████░░░░░░ 1200/1500            │
│                              ↑ 광폭화 경계선 (50%)       │
│                                        [거리: 850m]      │
│                                                         │
│                                                         │
│                         [게임 영역]                      │
│                                                         │
│                                                         │
│─────────────────────────────────────────────────────────│
│  [Alpha]  [Bravo]  [Charlie]  [Delta]  [Echo]           │
│  HP:████  HP:████  HP:████   HP:████  HP:████           │
│  ●●●●●   ●●●●●●●  ●●●●●●●●  ●●      ●●●●●●●●●●        │
│  5/5     28/30    6/8        1/2     9/10               │
│  [장전중                    ]                            │
└─────────────────────────────────────────────────────────┘

부위 HP 표시: 보스 각 부위 위에 소형 HP 바
  - 마우스가 부위 근처 or 해당 부위 조준 중일 때 표시
  - 파괴된 부위: 해골/X 아이콘 표시
```

### 5.2 UI 컴포넌트 목록

```javascript
UIScene 내 컴포넌트:

BossHpBar         // 상단 보스 HP 바 + 광폭화 경계선
BossDistanceText  // "거리: 850m" 텍스트
EnrageWarning     // "ENRAGE!" 경고 텍스트 (빨간색, 깜빡임)
WallBreachWarning // "WALL BREACHED!" 경고

// 하단 스쿼드 HUD — 5개
SquadHUD[5] {
  nameText         // Alpha / Bravo ...
  hpBar            // 개인 HP 바
  ammoDisplay      // 탄약 점(●) 표시
  reloadProgress   // 장전 중 프로그레스 바 (장전 중에만 표시)
  deadOverlay      // 사망 시 회색 처리
  selectedIndicator // 현재 선택된 캐릭터 하이라이트 (흰 테두리)
}

// 부위 HP (GameScene에 배치, depth 95)
PartHpBars[7]     // 보스 부위 위에 떠있는 소형 HP 바
```

### 5.3 이벤트 통신 (씬 간 데이터 교환)

```javascript
// GameScene → UIScene: Phaser EventEmitter 사용
this.events.emit('bossHpChanged', { hp, maxHp })
this.events.emit('squadHpChanged', { memberId, hp, maxHp })
this.events.emit('ammoChanged', { memberId, current, max })
this.events.emit('reloadStarted', { memberId, reloadTime })
this.events.emit('reloadComplete', { memberId })
this.events.emit('enrageTriggered')
this.events.emit('wallDestroyed')
this.events.emit('memberDied', { memberId })

// UIScene에서 GameScene 이벤트 구독
const gameScene = this.scene.get('GameScene')
gameScene.events.on('bossHpChanged', this.updateBossHp, this)
// ... 등
```

---

## 6. 충돌 그룹 설계 (Arcade Physics)

```javascript
// GameScene.create() 에서 설정

// 물리 그룹
squadBullets      = this.physics.add.group()         // 스쿼드 총알
minionBullets     = this.physics.add.group()         // 잡몹 투사체
minionsGroup      = this.physics.add.group()         // 잡몹 스프라이트
squadGroup        = this.physics.add.staticGroup()   // 스쿼드 (고정 위치)
terrainGroup      = this.physics.add.staticGroup()   // 지형지물

// overlap 등록 (충돌 판정)
//  1. 스쿼드 총알 ↔ 보스 부위 히트존
this.physics.add.overlap(squadBullets, bossPartZones, onBulletHitPart)

//  2. 스쿼드 총알 ↔ 잡몹
this.physics.add.overlap(squadBullets, minionsGroup, onBulletHitMinion)

//  3. 잡몹 투사체 ↔ 스쿼드
this.physics.add.overlap(minionBullets, squadGroup, onMinionBulletHitSquad)

//  4. 잡몹 ↔ 바리케이드 (Overlap + 공격 로직은 수동)
//     → 잡몹이 바리케이드 위치에 도달하면 attackTarget() 호출

//  5. 잡몹 ↔ 스쿼드 (바리케이드 파괴 후)
this.physics.add.overlap(minionsGroup, squadGroup, onMinionReachSquad)
```

---

## 7. 데이터 구조 (Config)

### 7.1 BossConfig.js

```javascript
export const BOSS_CONFIG = {
  maxHp: 1500,
  speed: 40,            // 픽셀/초
  enragedSpeed: 60,     // 광폭화 시 속도
  attackDamageWall: 80, // 장벽 공격 데미지
  attackInterval: 2000, // ms
  summonInterval: 5000, // 잡몹 소환 주기

  parts: {
    HEAD:  { hp:250, mult:2.0, offsetX:0,    offsetY:-180, w:80,  h:80  },
    ARM_L: { hp:150, mult:1.0, offsetX:-120, offsetY:-80,  w:60,  h:120 },
    ARM_R: { hp:150, mult:1.0, offsetX:120,  offsetY:-80,  w:60,  h:120 },
    LEG_L: { hp:180, mult:1.0, offsetX:-60,  offsetY:80,   w:50,  h:130 },
    LEG_R: { hp:180, mult:1.0, offsetX:60,   offsetY:80,   w:50,  h:130 },
    CHEST: { hp:350, mult:1.5, offsetX:0,    offsetY:-60,  w:100, h:100 },
    CORE:  { hp:120, mult:3.0, offsetX:0,    offsetY:-60,  w:60,  h:60, active:false }
  },

  enrageCondition: {
    hpThreshold: 0.5,     // HP 50% 이하
    requiredParts: ['HEAD', 'CHEST']  // 하나 이상 파괴 시
  }
}
```

### 7.2 SquadConfig.js

```javascript
export const SQUAD_CONFIG = [
  { id:'alpha',   hp:80,  slotIndex:0, weapon:WEAPONS.alpha,   special:'weakpointBonus',  specialValue:1.5 },
  { id:'bravo',   hp:100, slotIndex:1, weapon:WEAPONS.bravo,   special:'burstAccuracy',   specialValue:0.8 },
  { id:'charlie', hp:120, slotIndex:2, weapon:WEAPONS.charlie, special:'closeSplash',     specialValue:80  },
  { id:'delta',   hp:70,  slotIndex:3, weapon:WEAPONS.delta,   special:'rocketSplash',    specialValue:120 },
  { id:'echo',    hp:90,  slotIndex:4, weapon:WEAPONS.echo,    special:'weakpointMark',   specialValue:1.2 }
]
```

### 7.3 GameState (공유 상태 객체)

```javascript
// GameScene에서 관리, 이벤트로 UIScene에 전달
const gameState = {
  phase: 'NORMAL',         // 'NORMAL' | 'ENRAGED' | 'WIN' | 'LOSE'
  bossHp: 1500,
  bossMaxHp: 1500,
  bossDistance: 1000,      // 화면상 거리 (픽셀 → m 표시)
  destroyedParts: [],      // 파괴된 부위 ID 배열
  wallHp: 1000,
  wallMaxHp: 1000,
  barricadeHp: [300, 300],
  squadStatus: [
    { id:'alpha',   hp:80,  maxHp:80,  ammo:5,  maxAmmo:5,  alive:true, reloading:false },
    // ...
  ]
}
```

---

## 8. 씬별 구현 순서 (Do 단계 가이드)

### Step 1: index.html + Phaser 초기화

```html
<!-- index.html 핵심 구조 -->
<script src="https://cdn.jsdelivr.net/npm/phaser@3.60.0/dist/phaser.min.js"></script>
<script>
const config = {
  type: Phaser.AUTO,
  width: 1280,
  height: 720,
  backgroundColor: '#1a1a2e',
  physics: { default: 'arcade', arcade: { gravity: { y: 0 }, debug: false } },
  scene: [BootScene, MenuScene, GameScene, UIScene, ResultScene]
}
const game = new Phaser.Game(config)
</script>
```

### Step 2: BootScene — 에셋 로드

```javascript
// 프로토타입 단계에서는 실제 이미지 대신 Graphics로 생성
// 나중에 실제 스프라이트로 교체

preload() {
  // 도형으로 임시 텍스처 생성
  this.makeRectTexture('boss_body', 160, 300, 0x666666)
  this.makeCircleTexture('boss_head', 80, 0x888888)
  this.makeRectTexture('squad_char', 50, 80, 0x4444AA)
  // ...
}
```

### Step 3: GameScene 구축 순서

```
1. create()
   ├── 배경 이미지 배치 (depth 0, 10)
   ├── TerrainSystem 초기화 (건물, 장벽, 바리케이드)
   ├── Boss 생성 (x=1200, depth 40)
   ├── Squad 5명 생성 (하단 고정 위치, depth 70)
   ├── 물리 충돌 그룹 설정
   ├── AimSystem 초기화 + 포인터 이벤트 등록
   ├── FireSystem 초기화 (bulletPool 생성)
   ├── WaveSystem 초기화
   ├── EnrageSystem 초기화
   └── UIScene launch

2. update(time, delta)
   ├── Boss.update(delta)          // 이동 + 스케일
   ├── TerrainSystem.update()      // 보스의 건물/장벽 공격
   ├── WaveSystem.update(delta)    // 잡몹 소환
   ├── Minions 각각 update         // AI 이동 + 공격
   ├── FireSystem.update(delta)    // 자동 사격 + 장전
   ├── AimSystem.update()          // 자동 조준 갱신
   ├── EnrageSystem.update()       // 광폭화 조건 체크
   ├── updatePartHitZones()        // 부위 히트존 위치 갱신
   └── checkWinLoseConditions()    // 승패 판정
```

---

## 9. 파일별 구현 책임 요약

| 파일 | 책임 | 주요 메서드 |
|------|------|-------------|
| `scenes/GameScene.js` | 게임 전체 조율 | create, update, checkWinLose |
| `scenes/UIScene.js` | HUD 렌더링 | create, 이벤트 구독, updateHUD |
| `entities/Boss.js` | 보스 이동/공격/상태 | update, takeDamage, triggerEnrage |
| `entities/BossPart.js` | 부위 히트박스/HP | takeDamage, destroy, getWorldPos |
| `entities/SquadMember.js` | 캐릭터 기본 로직 | autoAim, tryFire, takeDamage, reload |
| `entities/Minion.js` | 잡몹 AI | decideTarget, moveToTarget, attack |
| `entities/Bullet.js` | 투사체 풀링 | fire, onHit |
| `entities/Terrain.js` | 지형지물 HP/시각 | takeDamage, updateVisual, collapse |
| `systems/AimSystem.js` | 드래그 조준 입력 | onPointerDown/Up/Move, setFocusTarget |
| `systems/FireSystem.js` | 사격 + 장전 | processMemberFire, spawnBullet, onBulletHit |
| `systems/WaveSystem.js` | 잡몹 소환 | update, spawnMinion, getSpawnInterval |
| `systems/EnrageSystem.js` | 광폭화 판정/발동 | checkConditions, triggerEnrage |
| `systems/TerrainSystem.js` | 지형 공격/파괴 | bossDamageBuilding, onBuildingCollapse |
| `config/BossConfig.js` | 보스 스탯 상수 | BOSS_CONFIG |
| `config/SquadConfig.js` | 스쿼드 스탯 상수 | SQUAD_CONFIG, WEAPONS |
| `config/MinionConfig.js` | 잡몹 스탯 상수 | MINION_CONFIGS |
