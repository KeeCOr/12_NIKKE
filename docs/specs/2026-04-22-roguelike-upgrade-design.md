# Roguelike Upgrade & Stage Loop Design

**Goal:** 보스 처치 후 강화 선택지 3장을 고르고 캐릭터를 재배치해 무한 스테이지를 반복하는 로그라이크 루프 추가

**Architecture:** RuntimeStatLayer — ScriptableObject는 기본값 전용, RunState 싱글톤이 누적 보너스 보관. StageManager가 OnBossDefeated를 가로채 업그레이드→배치→리셋 흐름 오케스트레이션.

**Tech Stack:** Unity 2022.3 LTS, C#, UnityEngine.UI, TextMeshPro, ScriptableObject

---

## 1. 데이터 모델

### 1.1 새 열거형 (GameEvents.cs에 추가)

```csharp
public enum WeaponType { Shotgun, Sniper, MachineGun, Railgun, Melee, HeavyWeapon }
public enum ElementType { Light, Dark, Fire, Ice, Electric }
```

### 1.2 SquadMemberConfigSO 변경

기존 필드 유지, 추가:
```csharp
public WeaponType weaponType;
public ElementType element;
```

### 1.3 WeaponConfig 변경

기존 필드 유지, 추가:
```csharp
public float maxRange;       // 0 = 무제한. 샷건=2.5, 기관총=4.0
public float rangeDropoff;   // 최대사거리에서 남는 데미지 비율 (0~1). 샷건=0.1, 기관총=0.5
public int   penetration;    // 관통 대상 수. 0=없음, 저격=1, 레일건=999
public bool  isMelee;        // true면 총알 없이 근접 공격
public float meleeRadius;    // 근접 공격 감지 반경 (isMelee=true 전용)
```

### 1.4 무기 타입별 기준 스탯

| 무기 | damage | fireRate | magazineSize | reloadTime | pellets | spread | splashRadius | maxRange | rangeDropoff | penetration | isMelee |
|------|--------|----------|--------------|------------|---------|--------|-------------|---------|-------------|------------|---------|
| Shotgun | 18 | 0.9s | 4 | 2.5s | 8 | 20° | 0 | 2.5 | 0.10 | 0 | false |
| Sniper | 120 | 2.2s | 6 | 1.8s | 1 | 0° | 0 | 0 | 1.0 | 1 | false |
| MachineGun | 12 | 0.08s | 40 | 3.5s | 1 | 8° | 0 | 4.0 | 0.50 | 0 | false |
| Railgun | 280 | 4.0s | 2 | 3.0s | 1 | 0° | 0 | 0 | 1.0 | 999 | false |
| Melee | 55 | 0.5s | 0 | 0s | 0 | 0° | 0 | 0 | 1.0 | 0 | true |
| HeavyWeapon | 90 | 2.5s | 3 | 3.0s | 1 | 5° | 1.5 | 0 | 1.0 | 0 | false |

### 1.5 UpgradeCardSO

```csharp
// Assets/Scripts/Config/UpgradeCardSO.cs
public enum UpgradeTargetType { SpecificMember, WeaponType, ElementType, Global }
public enum UpgradeStat { Damage, FireRate, ReloadSpeed, Hp, MagazineSize, BombCharge, Barricade }

[CreateAssetMenu(menuName = "SquadVsMonster/Upgrade Card")]
public class UpgradeCardSO : ScriptableObject {
    public string           title;
    public string           description;
    public Sprite           icon;
    public UpgradeTargetType targetType;
    public string           targetId;      // 멤버 id / "Shotgun" / "Fire" / "" (Global)
    public UpgradeStat      stat;
    public float            value;         // 0.2 = +20%, 50 = 플랫 +50
    public bool             isMultiplier;  // true=배율 곱, false=플랫 덧셈
    public int              maxStacks = 5; // 같은 카드 최대 중첩 횟수
}
```

### 1.6 RunState

```csharp
// Assets/Scripts/Core/RunState.cs
public class RunState : MonoBehaviour {
    public static RunState Instance { get; private set; }

    public int CurrentStage    { get; private set; } = 0;
    public int BombCharges     { get; set; }         = 1;
    public int ExtraBarricades { get; set; }         = 0;

    public struct MemberBonuses {
        public float damageMult;     // 기본 1.0
        public float fireRateMult;   // 기본 1.0 (낮을수록 빠름)
        public float reloadMult;     // 기본 1.0 (낮을수록 빠름)
        public float hpBonus;        // 플랫 추가 HP
        public int   magazineBonus;  // 플랫 추가 탄창
    }

    // key = member id
    public Dictionary<string, MemberBonuses> Bonuses { get; private set; }
    // key = card.name, value = 적용 횟수
    private Dictionary<string, int> _stackCounts;

    public float BossDifficultyMult => Mathf.Pow(1.2f, CurrentStage);

    public void ApplyCard(UpgradeCardSO card);   // 카드 효과를 Bonuses에 반영
    public bool CanStack(UpgradeCardSO card);    // maxStacks 초과 여부
    public void AdvanceStage();                  // CurrentStage++
    public MemberBonuses GetBonus(string memberId);
}
```

---

## 2. 스테이지 루프

### 2.1 게임 흐름

```
[기존] 보스 처치 → GameManager Win → Result 씬
[신규] 보스 처치 → StageManager
                  ├─ 일시정지 (Time.timeScale=0)
                  ├─ UpgradeUI.Show(3장)
                  │    └─ 선택 → RunState.ApplyCard()
                  ├─ DeploymentUI.Show()
                  │    └─ 확인 → 배치 결과 반환
                  └─ ResetStage()
                       ├─ RunState.AdvanceStage()
                       ├─ Boss.InitWithDifficulty(RunState.BossDifficultyMult)
                       ├─ Squad 위치 이동 + HP 복원 (사망자 30% HP 부활)
                       ├─ TerrainManager.ResetForStage(RunState.ExtraBarricades)
                       ├─ BombingSystem.ResetCharges(RunState.BombCharges)
                       ├─ WaveSystem 타이머 리셋
                       └─ Time.timeScale=1

[패배] 전멸·방벽 파괴 → GameManager → Result 씬 (RunState.CurrentStage 표시)
```

### 2.2 StageManager

```csharp
// Assets/Scripts/Systems/StageManager.cs
public class StageManager : MonoBehaviour {
    [SerializeField] private BossController        boss;
    [SerializeField] private SquadMemberController[] squad;
    [SerializeField] private TerrainManager        terrain;
    [SerializeField] private BombingSystem         bombing;
    [SerializeField] private WaveSystem            waves;
    [SerializeField] private UpgradeUI             upgradeUI;
    [SerializeField] private DeploymentUI          deploymentUI;
    [SerializeField] private UpgradeCardSO[]       cardPool;  // Inspector에서 모든 카드 할당
}
```

### 2.3 보스 난이도 스케일링

`BossController`에 `InitWithDifficulty(float mult)` 추가:
- `_runtimeMaxHp  = config.maxHp * mult`
- `_runtimeSpeed  = config.speed * Mathf.Pow(1.05f, stage)`
- `_runtimeDamage = config.attackDamageWall * Mathf.Pow(1.1f, stage)`
- config SO 값은 절대 수정하지 않음

### 2.4 GameManager 변경

`HandleBossDefeated()` — Win 전환 로직 제거 (StageManager가 대신 처리).  
패배 흐름은 그대로 유지.  
Result 씬에 `RunState.CurrentStage` 전달.

---

## 3. 업그레이드 카드 UI

### 3.1 컴포넌트

**UpgradeCardUI.cs** (`Assets/Scripts/UI/UpgradeCardUI.cs`)
```csharp
[SerializeField] Image             icon;
[SerializeField] TextMeshProUGUI   titleText;
[SerializeField] TextMeshProUGUI   descText;

public void Setup(UpgradeCardSO card, Action onSelected);
```

**UpgradeUI.cs** (`Assets/Scripts/UI/UpgradeUI.cs`)
```csharp
[SerializeField] UpgradeCardUI[]   cards;        // 3개
[SerializeField] TextMeshProUGUI   stageLabel;   // "STAGE N CLEAR"

public void Show(UpgradeCardSO[] options, Action<UpgradeCardSO> onPicked);
public void Hide();
```

### 3.2 카드 풀 추출 로직

```
1. cardPool 전체에서 CanStack() = true인 카드만 필터
2. Fisher-Yates 셔플
3. 앞에서 3장 추출 (풀이 3장 미만이면 있는 만큼만 표시)
```

### 3.3 카드 예시 (에셋으로 만들 것)

| 제목 | 대상 | 효과 |
|------|------|------|
| Alpha 집중 훈련 | Member: alpha | Damage ×1.2 |
| 샷건 탄약 개선 | WeaponType: Shotgun | MagazineSize +2 |
| 불꽃 속성 강화 | ElementType: Fire | Damage ×1.15 |
| 폭격 보급 | Global | BombCharge +1 |
| 바리케이드 증설 | Global | Barricade +1 |
| 빠른 손 | WeaponType: Sniper | ReloadSpeed ×0.8 |
| 전기 충격 | ElementType: Electric | FireRate ×0.9 |

---

## 4. 배치 UI

### 4.1 컴포넌트

**DeploymentSlotUI.cs** (`Assets/Scripts/UI/DeploymentSlotUI.cs`)
```csharp
public int    SlotIndex  { get; private set; }
public string AssignedId { get; private set; }   // 배치된 멤버 id, null=비어있음

public void Assign(string memberId);
public void Clear();
```

**DeploymentMemberCard.cs** (`Assets/Scripts/UI/DeploymentMemberCard.cs`)
```csharp
// IBeginDragHandler, IDragHandler, IEndDragHandler 구현
[SerializeField] TextMeshProUGUI nameText;
[SerializeField] Image           portrait;
[SerializeField] Image           hpIndicator;   // 사망 시 회색

public void Setup(SquadMemberController member, bool isDead);
```

**DeploymentUI.cs** (`Assets/Scripts/UI/DeploymentUI.cs`)
```csharp
[SerializeField] DeploymentSlotUI[]      slots;        // 5개
[SerializeField] DeploymentMemberCard[]  memberCards;  // 5개
[SerializeField] Button                  confirmButton;

public void Show(SquadMemberController[] squad, Action<string[]> onConfirmed);
// onConfirmed: 슬롯 순서대로 배치된 memberId 배열 (null = 기본 위치 유지)
public void Hide();
```

### 4.2 배치 규칙

- 슬롯에 카드 드롭 → `DeploymentSlotUI.Assign()` 호출
- 이미 점유된 슬롯에 드롭 → 기존 카드와 교체
- 배치 안 된 슬롯 → 이전 스테이지 위치 유지 (기본 포지션 배열에서 읽음)
- 사망 캐릭터 배치 시 → StageManager가 `30% HP`로 부활 처리
- "배치 완료" → `onConfirmed(slotAssignments)` 호출

### 4.3 월드 좌표 슬롯 기준 포지션

```
슬롯 0: (0.68, 1.48)   슬롯 1: (1.28, 1.48)   슬롯 2: (1.88, 1.48)
슬롯 3: (2.48, 1.48)   슬롯 4: (3.08, 1.48)
```

---

## 5. 기존 파일 수정 요약

| 파일 | 변경 내용 |
|------|----------|
| `GameEvents.cs` | `WeaponType`, `ElementType` 열거형 / `OnStageCleared` 이벤트 추가 |
| `SquadMemberConfigSO.cs` | `weaponType`, `element` 필드 추가 |
| `WeaponConfig.cs` | `maxRange`, `rangeDropoff`, `penetration`, `isMelee`, `meleeRadius` 추가 |
| `GameManager.cs` | `HandleBossDefeated` Win 전환 제거 |
| `BossController.cs` | `InitWithDifficulty(float mult)` 추가, 런타임 스탯 변수 분리 |
| `SquadMemberController.cs` | `ApplyRunStateBonus()` public 메서드 추가 (Awake + StageManager.ResetStage 모두 호출) / `isMelee` 분기 처리 |
| `Bullet.cs` | `penetration` 관통 처리 / `maxRange` 거리 기반 데미지 감소 |
| `BombingSystem.cs` | 다중 충전 지원 (`BombCharges`) |
| `TerrainManager.cs` | `ResetForStage(int extraBarricades)` 추가 |
| `ResultUI.cs` | 최고 스테이지 수 표시 |

---

## 6. 테스트

**RunStateTests.cs**
- `ApplyCard` 후 `damageMult` 누적 정확성 검증
- `maxStacks` 초과 시 `CanStack()` false 반환 검증
- `BossDifficultyMult` 지수 계산 검증 (stage 3 → 1.2^3 = 1.728)

**StageScalingTests.cs**
- `InitWithDifficulty` 후 런타임 HP가 config.maxHp × mult 인지 검증
- SO 원본 값이 변경되지 않았는지 검증
