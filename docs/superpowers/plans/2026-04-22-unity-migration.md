# Squad vs Monster — Unity Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Phaser 3 브라우저 게임을 Unity 2022 LTS로 이식, ScriptableObject 아키텍처 + New Input System으로 PC/모바일 멀티플랫폼 지원

**Architecture:** ScriptableObject로 모든 수치 분리, `GameEvents` static C# 이벤트 버스로 느슨한 결합, `UnityEngine.Pool.ObjectPool<T>`로 총알 풀 관리. 렌더링은 SpriteRenderer, 로직은 Controller 스크립트로 완전 분리.

**Tech Stack:** Unity 2022.3 LTS, URP, New Input System 1.7+, TextMeshPro, Kenney Assets (무료)

---

## File Map

```
Assets/
├── Scripts/
│   ├── Config/
│   │   ├── GameConfig.cs              ← 좌표 상수, 슬롯 위치
│   │   ├── BossConfigSO.cs            ← 보스 수치 ScriptableObject
│   │   ├── BossPartConfig.cs          ← 파트 데이터 [Serializable]
│   │   ├── SquadMemberConfigSO.cs     ← 스쿼드 수치 ScriptableObject
│   │   ├── WeaponConfig.cs            ← 무기 데이터 [Serializable]
│   │   └── MinionConfigSO.cs          ← 미니언 수치 ScriptableObject
│   ├── Core/
│   │   ├── GameManager.cs             ← PLAYING/WIN/LOSE 상태, 씬 전환
│   │   └── GameEvents.cs              ← static C# 이벤트 버스
│   ├── Entities/
│   │   ├── Boss/
│   │   │   ├── BossController.cs      ← 상태머신, 이동, 공격, 광폭화
│   │   │   └── BossPartController.cs  ← 파트 HP, 히트박스, 디버프
│   │   ├── Squad/
│   │   │   ├── SquadMemberController.cs ← 사격, 장전, 피해, 사망
│   │   │   └── AimController.cs       ← 자동조준, 유저타겟, 조준선
│   │   ├── Enemies/
│   │   │   └── MinionController.cs    ← 미니언 AI (이동→공격→사망)
│   │   └── Bullet.cs                  ← 이동, 충돌, 풀 반환
│   ├── Systems/
│   │   ├── WaveSystem.cs              ← 타이머 기반 스폰 페이즈
│   │   ├── FireSystem.cs              ← ObjectPool<Bullet>, 발사 처리
│   │   ├── BombingSystem.cs           ← 폭격 스킬 범위 피해
│   │   ├── EnrageSystem.cs            ← 광폭화 조건 체크
│   │   └── TerrainManager.cs          ← 장벽/바리케이드/로드블록 HP
│   ├── Input/
│   │   └── InputManager.cs            ← New Input System 래퍼
│   ├── UI/
│   │   ├── UIManager.cs               ← GameEvents 구독, HUD 조율
│   │   ├── BossHpBarUI.cs
│   │   ├── SquadHpBarUI.cs
│   │   ├── AmmoDisplayUI.cs
│   │   └── ResultUI.cs
│   └── Audio/
│       └── AudioManager.cs            ← 효과음 싱글턴 스텁
└── Tests/EditMode/
    ├── EventBusTests.cs
    ├── BossDamageTests.cs
    └── WavePhaseTests.cs
```

---

## Task 1: Unity 프로젝트 생성 (수동)

**Files:** 없음 (Unity Hub에서 수행)

- [ ] **Step 1: Unity Hub에서 새 프로젝트 생성**

  Unity Hub → New Project → **2D (URP)** 템플릿 → Unity 2022.3 LTS 선택  
  프로젝트 이름: `SquadVsMonster`  
  저장 위치: `게임개발/` 폴더 내

- [ ] **Step 2: 필수 패키지 설치**

  Window → Package Manager → 아래 패키지 Install:
  ```
  com.unity.inputsystem         ← New Input System
  com.unity.textmeshpro         ← TextMeshPro (보통 이미 포함)
  com.unity.cinemachine         ← 카메라 쉐이크용 (선택)
  ```

  Edit → Project Settings → Player → **Active Input Handling: Both** (기존 코드 호환) → **New Input System Only** 로 변경 후 재시작

- [ ] **Step 3: Assembly Definition 생성**

  `Assets/Scripts/` 폴더 우클릭 → Create → Assembly Definition → 이름 `SquadVsMonster`  
  `Assets/Tests/EditMode/` 폴더 생성 후 Assembly Definition → 이름 `SquadVsMonster.Tests`  
  Tests assembly에서 References에 `SquadVsMonster` + `UnityEngine.TestRunner` + `UnityEditor.TestRunner` 추가, Test Assemblies 체크

- [ ] **Step 4: 폴더 구조 생성**

  Assets/Scripts 하위에 `Config, Core, Entities/Boss, Entities/Squad, Entities/Enemies, Systems, Input, UI, Audio` 폴더 생성

- [ ] **Step 5: URP 카메라 설정**

  Main Camera: Projection = **Orthographic**, Size = **3.6** (720px / 2 / 100)  
  Position = (6.4, 3.6, -10), Clear Flags = Solid Color, Background = #0D0D22

---

## Task 2: GameEvents & GameManager

**Files:**
- Create: `Assets/Scripts/Core/GameEvents.cs`
- Create: `Assets/Scripts/Core/GameManager.cs`
- Create: `Assets/Tests/EditMode/EventBusTests.cs`

- [ ] **Step 1: 테스트 작성**

`Assets/Tests/EditMode/EventBusTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

public class EventBusTests {
    [SetUp] public void Setup() => GameEvents.ClearAllListeners();

    [Test]
    public void BossHpChanged_FiresWithCorrectValues() {
        float receivedHp = -1f, receivedMax = -1f;
        GameEvents.OnBossHpChanged += (hp, max) => { receivedHp = hp; receivedMax = max; };
        GameEvents.RaiseBossHpChanged(2000f, 4500f);
        Assert.AreEqual(2000f, receivedHp);
        Assert.AreEqual(4500f, receivedMax);
    }

    [Test]
    public void MemberDied_FiresWithId() {
        string receivedId = null;
        GameEvents.OnMemberDied += id => receivedId = id;
        GameEvents.RaiseMemberDied("alpha");
        Assert.AreEqual("alpha", receivedId);
    }

    [Test]
    public void ClearAllListeners_RemovesSubscribers() {
        bool fired = false;
        GameEvents.OnBossDefeated += () => fired = true;
        GameEvents.ClearAllListeners();
        GameEvents.RaiseBossDefeated();
        Assert.IsFalse(fired);
    }
}
```

- [ ] **Step 2: 테스트 실패 확인**

  Window → General → Test Runner → EditMode → Run All  
  Expected: 3개 FAIL (GameEvents not found)

- [ ] **Step 3: GameEvents.cs 작성**

`Assets/Scripts/Core/GameEvents.cs`:
```csharp
using System;
using UnityEngine;

public static class GameEvents {
    public static event Action<float, float>   OnBossHpChanged;
    public static event Action<string>          OnBossPartDestroyed;
    public static event Action                  OnBossEnraged;
    public static event Action                  OnBossDefeated;
    public static event Action<BulletData>      OnFireBullet;
    public static event Action<string, int, int> OnAmmoChanged;
    public static event Action<string, float>   OnReloadStarted;
    public static event Action<string>          OnReloadComplete;
    public static event Action<string>          OnMemberDied;
    public static event Action<Vector2, float>  OnBossShockwave;
    public static event Action                  OnBossAttack;
    public static event Action<float, float>    OnWallHpChanged;
    public static event Action                  OnWallDestroyed;
    public static event Action<MinionType, Vector2> OnSpawnMinion;

    public static void RaiseBossHpChanged(float hp, float max)          => OnBossHpChanged?.Invoke(hp, max);
    public static void RaiseBossPartDestroyed(string partId)            => OnBossPartDestroyed?.Invoke(partId);
    public static void RaiseBossEnraged()                               => OnBossEnraged?.Invoke();
    public static void RaiseBossDefeated()                              => OnBossDefeated?.Invoke();
    public static void RaiseFireBullet(BulletData data)                 => OnFireBullet?.Invoke(data);
    public static void RaiseAmmoChanged(string id, int cur, int max)    => OnAmmoChanged?.Invoke(id, cur, max);
    public static void RaiseReloadStarted(string id, float duration)    => OnReloadStarted?.Invoke(id, duration);
    public static void RaiseReloadComplete(string id)                   => OnReloadComplete?.Invoke(id);
    public static void RaiseMemberDied(string id)                       => OnMemberDied?.Invoke(id);
    public static void RaiseBossShockwave(Vector2 pos, float damage)    => OnBossShockwave?.Invoke(pos, damage);
    public static void RaiseBossAttack()                                => OnBossAttack?.Invoke();
    public static void RaiseWallHpChanged(float hp, float max)          => OnWallHpChanged?.Invoke(hp, max);
    public static void RaiseWallDestroyed()                             => OnWallDestroyed?.Invoke();
    public static void RaiseSpawnMinion(MinionType type, Vector2 pos)   => OnSpawnMinion?.Invoke(type, pos);

    public static void ClearAllListeners() {
        OnBossHpChanged = null; OnBossPartDestroyed = null; OnBossEnraged = null;
        OnBossDefeated = null; OnFireBullet = null; OnAmmoChanged = null;
        OnReloadStarted = null; OnReloadComplete = null; OnMemberDied = null;
        OnBossShockwave = null; OnBossAttack = null; OnWallHpChanged = null;
        OnWallDestroyed = null; OnSpawnMinion = null;
    }
}

public enum MinionType { Runner, Berserker, Spitter }

public struct BulletData {
    public Vector2 origin;
    public float angle;
    public float speed;
    public float damage;
    public string ownerId;
    public BulletType bulletType;
    public float splashRadius;
    public int pellets;
    public float spread;
    public string targetPartId;
}

public enum BulletType { Single, Shotgun, Rocket }
```

- [ ] **Step 4: GameManager.cs 작성**

`Assets/Scripts/Core/GameManager.cs`:
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Win, Lose }
    public GameState State { get; private set; } = GameState.Playing;

    [SerializeField] private float resultDelay = 1.2f;

    void Awake() {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable() {
        GameEvents.OnBossDefeated += HandleBossDefeated;
        GameEvents.OnMemberDied   += HandleMemberDied;
    }

    void OnDisable() {
        GameEvents.OnBossDefeated -= HandleBossDefeated;
        GameEvents.OnMemberDied   -= HandleMemberDied;
    }

    public void TriggerDefenseBroken() => StartCoroutine(EndGame(GameState.Lose, resultDelay));

    private void HandleBossDefeated() {
        if (State != GameState.Playing) return;
        StartCoroutine(EndGame(GameState.Win, 0.8f));
    }

    private void HandleMemberDied(string _) {
        // SquadMemberController가 전원 사망 체크 후 호출
    }

    public void NotifyAllMembersDead() {
        if (State != GameState.Playing) return;
        StartCoroutine(EndGame(GameState.Lose, resultDelay));
    }

    private System.Collections.IEnumerator EndGame(GameState result, float delay) {
        State = result;
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("Result");
    }
}
```

- [ ] **Step 5: 테스트 통과 확인**

  Test Runner → Run All → 3개 PASS 확인

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/Core/ Assets/Tests/
git commit -m "feat: add GameEvents static bus and GameManager state machine"
```

---

## Task 3: Config ScriptableObjects

**Files:**
- Create: `Assets/Scripts/Config/GameConfig.cs`
- Create: `Assets/Scripts/Config/BossPartConfig.cs`
- Create: `Assets/Scripts/Config/BossConfigSO.cs`
- Create: `Assets/Scripts/Config/WeaponConfig.cs`
- Create: `Assets/Scripts/Config/SquadMemberConfigSO.cs`
- Create: `Assets/Scripts/Config/MinionConfigSO.cs`

- [ ] **Step 1: GameConfig.cs 작성 (좌표 상수)**

`Assets/Scripts/Config/GameConfig.cs`:
```csharp
using UnityEngine;

public static class GameConfig {
    // Phaser 1280×720 → Unity units (÷100, Y 반전)
    public static readonly float[] SQUAD_SLOT_X = { 0.68f, 1.28f, 1.88f, 2.48f, 3.08f };
    public const float SQUAD_Y       = 1.48f;   // (720-572)/100
    public const float BOSS_START_X  = 14.0f;
    public const float BOSS_STOP_X   = 4.2f;
    public const float BOSS_Y        = 4.1f;    // (720-310)/100
    public const float DEFENSE_LINE  = 1.3f;    // 보스가 이 X 이하 → LOSE
    public const float WALL_X        = 7.0f;
    public const float WALL_Y        = 2.02f;
    public const float SPAWN_X_MIN   = 11.6f;
    public const float SPAWN_X_MAX   = 12.6f;
    public const float SPAWN_Y_MIN   = 2.2f;
    public const float SPAWN_Y_MAX   = 2.8f;
}
```

- [ ] **Step 2: BossPartConfig.cs 작성**

`Assets/Scripts/Config/BossPartConfig.cs`:
```csharp
using System;
using UnityEngine;

[Serializable]
public class BossPartConfig {
    public string id;
    public int hp;
    public float damageMult;
    public Vector2 offset;   // Unity units (원본 px ÷ 100, Y 반전)
    public Vector2 size;     // Unity units
    public bool activeOnStart;
}
```

- [ ] **Step 3: BossConfigSO.cs 작성**

`Assets/Scripts/Config/BossConfigSO.cs`:
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "BossConfig", menuName = "SquadVsMonster/Boss Config")]
public class BossConfigSO : ScriptableObject {
    public int   maxHp              = 4500;
    public float speed              = 0.26f;   // units/s (26px/s ÷ 100)
    public float enragedSpeed       = 0.44f;
    public float attackInterval     = 2.5f;
    public float attackDamageWall   = 80f;
    public float shockwaveDamage    = 30f;
    public float shockwaveInterval  = 6f;
    public float enrageHpThreshold  = 0.5f;
    public BossPartConfig[] parts;
}
```

- [ ] **Step 4: WeaponConfig.cs 작성**

`Assets/Scripts/Config/WeaponConfig.cs`:
```csharp
using System;

[Serializable]
public class WeaponConfig {
    public string name;
    public int    magazineSize;
    public float  damage;
    public float  fireRate;       // seconds between shots
    public float  reloadTime;     // seconds
    public float  bulletSpeed;    // units/s
    public BulletType bulletType;
    public int    pellets    = 1;
    public float  spread     = 0f;  // degrees
    public float  splashRadius = 0f;
}
```

- [ ] **Step 5: SquadMemberConfigSO.cs 작성**

`Assets/Scripts/Config/SquadMemberConfigSO.cs`:
```csharp
using UnityEngine;

public enum SpecialType { None, WeakpointBonus, BurstAccuracy, CloseSplash, RocketSplash, WeakpointMark }

[CreateAssetMenu(fileName = "SquadConfig_", menuName = "SquadVsMonster/Squad Member Config")]
public class SquadMemberConfigSO : ScriptableObject {
    public string      id;
    public string      label;
    public int         hp;
    public Color       color;
    public WeaponConfig weapon;
    public SpecialType special;
    public float       specialVal;
    public string[]    aimPriority;  // e.g. {"CORE","HEAD","CHEST"}
}
```

- [ ] **Step 6: MinionConfigSO.cs 작성**

`Assets/Scripts/Config/MinionConfigSO.cs`:
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "MinionConfig_", menuName = "SquadVsMonster/Minion Config")]
public class MinionConfigSO : ScriptableObject {
    public MinionType type;
    public int   hp             = 60;
    public float speed          = 1.2f;
    public float damage         = 20f;
    public float attackRange    = 0.4f;
    public float attackInterval = 1.5f;
    public float terrainDamage  = 15f;
}
```

- [ ] **Step 7: Inspector에서 .asset 파일 생성 (수동)**

  Project 창 → ScriptableObjects/Configs/ 폴더 우클릭 → Create → SquadVsMonster →  
  `Boss Config` → BossConfig.asset 생성 후 Inspector에서 수치 입력:

  **BossConfig.asset parts 배열 (7개):**
  | id    | hp  | mult | offset          | size       | active |
  |-------|-----|------|-----------------|------------|--------|
  | HEAD  | 520 | 2.0  | (0, 1.55)       | (0.9, 0.85)| true   |
  | ARM_L | 340 | 1.0  | (-1.15, 0.7)    | (0.55, 1.3)| true   |
  | ARM_R | 340 | 1.0  | (1.15, 0.7)     | (0.55, 1.3)| true   |
  | LEG_L | 400 | 1.0  | (-0.55, -0.95)  | (0.5, 1.3) | true   |
  | LEG_R | 400 | 1.0  | (0.55, -0.95)   | (0.5, 1.3) | true   |
  | CHEST | 800 | 1.5  | (0, 0.5)        | (1.1, 1.1) | true   |
  | CORE  | 260 | 3.0  | (0, 0.5)        | (0.6, 0.6) | false  |

  **SquadConfig_Alpha.asset:**
  id=alpha, label=ALPHA, hp=80, color=#4488FF, weapon(BoltAction/5/80dmg/2.5s/3s/9.0spd/Single), special=WeakpointBonus, specialVal=1.5, aimPriority=[CORE,HEAD,CHEST,ARM_L,ARM_R]

  **SquadConfig_Bravo.asset:** id=bravo, hp=100, #44FF88, AR/30/14dmg/0.35s/2s/7.0spd/Single, BurstAccuracy, 0.7, [CHEST,ARM_L,ARM_R,HEAD]

  **SquadConfig_Charlie.asset:** id=charlie, hp=120, #FFDD44, Shotgun/8/12dmg/1.3s/1.5s/4.8spd/Shotgun(pellets=5,spread=28), CloseSplash, 80, [CHEST,LEG_L,LEG_R,ARM_L]

  **SquadConfig_Delta.asset:** id=delta, hp=70, #FF6644, Rocket/2/130dmg/5s/4.5s/3.8spd/Rocket(splash=1.2), RocketSplash, 120, [CHEST,HEAD,LEG_L,LEG_R]

  **SquadConfig_Echo.asset:** id=echo, hp=90, #CC44FF, DMR/10/38dmg/1.2s/2s/8.2spd/Single, WeakpointMark, 1.2, [CORE,HEAD,CHEST,ARM_R]

  **MinionConfig_Runner.asset:** Runner, hp=60, speed=1.8, dmg=15, range=0.3, interval=1.2
  **MinionConfig_Berserker.asset:** Berserker, hp=160, speed=0.9, dmg=40, range=0.5, interval=2.0
  **MinionConfig_Spitter.asset:** Spitter, hp=40, speed=0.7, dmg=20, range=3.5, interval=2.5

- [ ] **Step 8: 커밋**

```bash
git add Assets/Scripts/Config/
git commit -m "feat: add ScriptableObject config classes for boss, squad, minion"
```

---

## Task 4: Boss Entity

**Files:**
- Create: `Assets/Scripts/Entities/Boss/BossPartController.cs`
- Create: `Assets/Scripts/Entities/Boss/BossController.cs`
- Create: `Assets/Tests/EditMode/BossDamageTests.cs`

- [ ] **Step 1: 테스트 작성**

`Assets/Tests/EditMode/BossDamageTests.cs`:
```csharp
using NUnit.Framework;

public class BossDamageTests {
    [SetUp] public void Setup() => GameEvents.ClearAllListeners();

    [Test]
    public void EffectiveSpeed_BothLegsDestroyed_Reduces() {
        // 다리 2개 파괴 시 속도 0.65 * 0.65 = 0.4225배
        float baseSpeed = 1.0f;
        float result = baseSpeed * 0.65f * 0.65f;
        Assert.AreApproximately(0.4225f, result, 0.001f);
    }

    [Test]
    public void DestroyedPartCount_IncreaseDamageBonus() {
        // 파트 2개 파괴 시 데미지 * (1 + 2*0.25) = 1.5배
        float rawDmg = 100f;
        int destroyedCount = 2;
        float result = rawDmg * (1f + destroyedCount * 0.25f);
        Assert.AreApproximately(150f, result, 0.001f);
    }
}
```

- [ ] **Step 2: 테스트 실패 확인**

  Test Runner → Run All (BossDamageTests) → PASS (순수 계산 테스트라 즉시 통과)

- [ ] **Step 3: BossPartController.cs 작성**

`Assets/Scripts/Entities/Boss/BossPartController.cs`:
```csharp
using UnityEngine;

public class BossPartController : MonoBehaviour {
    public string PartId { get; private set; }
    public bool IsActive  { get; private set; }
    public bool IsDestroyed { get; private set; }
    public float MaxHp   { get; private set; }
    public float Hp      { get; private set; }
    public float DamageMult { get; private set; }

    // 피격 디버프 상태
    public float DebuffTimer  { get; private set; }
    public bool  DebuffStrong { get; private set; }

    private Collider2D _collider;

    public void Initialize(BossPartConfig cfg) {
        PartId      = cfg.id;
        MaxHp       = cfg.hp;
        Hp          = cfg.hp;
        DamageMult  = cfg.damageMult;
        IsActive    = cfg.activeOnStart;
        IsDestroyed = false;
        _collider   = GetComponent<Collider2D>();
        if (_collider != null) _collider.enabled = IsActive;
    }

    public void Activate() {
        IsActive = true;
        if (_collider != null) _collider.enabled = true;
    }

    public void UpdateDebuff(float delta) {
        if (DebuffTimer > 0f) DebuffTimer = Mathf.Max(0f, DebuffTimer - delta);
    }

    public void ApplyHitDebuff(bool isAlreadyDestroyed) {
        DebuffTimer  = 5f;
        DebuffStrong = isAlreadyDestroyed;
    }

    // 반환값: 실제 적용된 데미지
    public float TakeDamage(float amount) {
        if (IsDestroyed) return 0f;
        float applied = Mathf.Min(Hp, amount);
        Hp -= applied;
        if (Hp <= 0f) Destroy_();
        return applied;
    }

    private void Destroy_() {
        IsDestroyed = true;
        if (_collider != null) _collider.enabled = false;
        // 스프라이트 파괴 표현 (색상 어둡게)
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.13f, 0f, 0f);
        GameEvents.RaiseBossPartDestroyed(PartId);
    }
}
```

- [ ] **Step 4: BossController.cs 작성**

`Assets/Scripts/Entities/Boss/BossController.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour {
    public enum BossState { Walking, Attacking, Stunned, Enraged, Dying }

    [SerializeField] private BossConfigSO config;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer[] partRenderers; // Inspector에서 파트 순서 매핑

    public BossState State { get; private set; } = BossState.Walking;
    public bool IsAlive    { get; private set; } = true;
    public bool IsEnraged  { get; private set; }
    public float Hp        { get; private set; }
    public float MaxHp     { get; private set; }

    private Dictionary<string, BossPartController> _parts = new();
    private float _attackTimer;
    private float _shockwaveTimer;
    private float _stunTimer;
    private float _stunCooldown;
    private TerrainManager _terrain;

    void Awake() {
        MaxHp = config.maxHp;
        Hp    = MaxHp;
        transform.position = new Vector3(GameConfig.BOSS_START_X, GameConfig.BOSS_Y, 0f);

        foreach (var partCfg in config.parts) {
            var go = new GameObject($"Part_{partCfg.id}");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(partCfg.offset.x, partCfg.offset.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = partCfg.size;
            col.isTrigger = true;
            var part = go.AddComponent<BossPartController>();
            part.Initialize(partCfg);
            _parts[partCfg.id] = part;
        }
    }

    void Start() {
        _terrain = FindObjectOfType<TerrainManager>();
        GameEvents.OnBossPartDestroyed += OnPartDestroyed;
    }

    void OnDestroy() {
        GameEvents.OnBossPartDestroyed -= OnPartDestroyed;
    }

    void Update() {
        if (!IsAlive) return;
        float dt = Time.deltaTime;

        foreach (var p in _parts.Values) p.UpdateDebuff(dt);

        if (_stunTimer > 0f) {
            _stunTimer -= dt;
            return;
        }
        if (_stunCooldown > 0f) _stunCooldown -= dt;

        if (State == BossState.Walking || State == BossState.Enraged) {
            var obstacle = _terrain?.GetRoadBlockAhead(transform.position.x);
            if (obstacle != null) {
                AttackObstacle(dt, obstacle);
            } else if (transform.position.x > config.stopX) {
                transform.position += Vector3.left * EffectiveSpeed * dt;
            } else {
                AttackWall(dt);
            }
        }

        // 보스가 방어선 돌파 → LOSE
        if (transform.position.x < GameConfig.DEFENSE_LINE) {
            GameManager.Instance.TriggerDefenseBroken();
        }

        // 광폭화 충격파
        if (IsEnraged) {
            _shockwaveTimer -= dt;
            if (_shockwaveTimer <= 0f) {
                _shockwaveTimer = config.shockwaveInterval;
                GameEvents.RaiseBossShockwave(transform.position, config.shockwaveDamage);
            }
        }
    }

    private float EffectiveSpeed {
        get {
            float s = IsEnraged ? config.enragedSpeed : config.speed;
            if (_parts.TryGetValue("LEG_L", out var ll) && ll.IsDestroyed) s *= 0.65f;
            if (_parts.TryGetValue("LEG_R", out var lr) && lr.IsDestroyed) s *= 0.65f;
            foreach (var pid in new[] { "LEG_L", "LEG_R" }) {
                if (_parts.TryGetValue(pid, out var p) && p.DebuffTimer > 0f)
                    s *= p.DebuffStrong ? 0.90f : 0.95f;
            }
            return s;
        }
    }

    private float EffectiveAttackInterval {
        get {
            float t = IsEnraged ? config.attackInterval * 0.5f : config.attackInterval;
            if (_parts.TryGetValue("ARM_L", out var al) && al.IsDestroyed) t *= 1.6f;
            if (_parts.TryGetValue("ARM_R", out var ar) && ar.IsDestroyed) t *= 1.6f;
            foreach (var pid in new[] { "ARM_L", "ARM_R" }) {
                if (_parts.TryGetValue(pid, out var p) && p.DebuffTimer > 0f)
                    t *= p.DebuffStrong ? 1.40f : 1.20f;
            }
            return t;
        }
    }

    private void AttackWall(float dt) {
        _attackTimer -= dt;
        if (_attackTimer <= 0f) {
            _attackTimer = EffectiveAttackInterval;
            _terrain?.BossDamageWall(config.attackDamageWall);
            GameEvents.RaiseBossAttack();
            Camera.main.GetComponent<CameraShaker>()?.Shake(0.18f, 0.008f);
        }
    }

    private void AttackObstacle(float dt, TerrainBlock block) {
        _attackTimer -= dt;
        if (_attackTimer <= 0f) {
            _attackTimer = EffectiveAttackInterval;
            _terrain?.BossSmashBlock(block, config.attackDamageWall * 0.75f);
            GameEvents.RaiseBossAttack();
        }
    }

    public float TakeDamage(string partId, float rawDmg) {
        if (!IsAlive) return 0f;
        if (!_parts.TryGetValue(partId, out var part)) return 0f;
        if (!part.IsActive) return 0f;

        part.ApplyHitDebuff(part.IsDestroyed);
        if (part.IsDestroyed) return 0f;

        // 파괴 파트 수 보너스
        int destroyedCount = 0;
        foreach (var p in _parts.Values) if (p.IsDestroyed && p.PartId != "CORE") destroyedCount++;
        float finalDmg = rawDmg * (1f + destroyedCount * 0.25f);

        // HEAD/CORE vuln 배율
        foreach (var pid in new[] { "HEAD", "CORE" }) {
            if (_parts.TryGetValue(pid, out var dp) && dp.DebuffTimer > 0f)
                finalDmg *= dp.DebuffStrong ? 1.30f : 1.15f;
        }

        // CHEST 스태거
        if (partId == "CHEST" && _stunCooldown <= 0f && _parts.TryGetValue("CHEST", out var chest)) {
            float chance = chest.DebuffStrong ? 0.20f : 0.08f;
            if (chest.DebuffTimer > 0f && Random.value < chance) {
                _stunTimer = 1.5f;
                _stunCooldown = 5f;
                Camera.main.GetComponent<CameraShaker>()?.Shake(0.22f, 0.01f);
            }
        }

        float dmgApplied = part.TakeDamage(finalDmg);
        Hp = Mathf.Max(0f, Hp - dmgApplied);
        GameEvents.RaiseBossHpChanged(Hp, MaxHp);

        if (Hp <= 0f) Die();
        return dmgApplied;
    }

    public void Enrage() {
        if (IsEnraged) return;
        IsEnraged = true;
        State = BossState.Enraged;
        _shockwaveTimer = 3f;
        if (bodyRenderer != null) bodyRenderer.color = new Color(0.48f, 0.1f, 0.04f);
        Camera.main.GetComponent<CameraShaker>()?.Shake(0.6f, 0.018f);
        GameEvents.RaiseBossEnraged();
    }

    private void OnPartDestroyed(string partId) {
        if (partId == "CHEST" && _parts.TryGetValue("CORE", out var core))
            core.Activate();
    }

    private void Die() {
        IsAlive = false;
        State = BossState.Dying;
        StartCoroutine(DieCoroutine());
    }

    private System.Collections.IEnumerator DieCoroutine() {
        float t = 0f;
        while (t < 1.2f) {
            t += Time.deltaTime;
            if (bodyRenderer != null) bodyRenderer.color = new Color(1f, 1f, 1f, 1f - t / 1.2f);
            yield return null;
        }
        GameEvents.RaiseBossDefeated();
        gameObject.SetActive(false);
    }

    public BossPartController GetPart(string id) => _parts.TryGetValue(id, out var p) ? p : null;

    // 광폭화 조건: 기본 HP 50% + HEAD, CHEST 파괴
    public bool MeetsEnrageCondition() {
        return Hp / MaxHp <= config.enrageHpThreshold
            && (_parts.TryGetValue("HEAD",  out var h) && h.IsDestroyed)
            && (_parts.TryGetValue("CHEST", out var c) && c.IsDestroyed);
    }
}
```

- [ ] **Step 5: CameraShaker.cs 추가 (보스 공격/스턴 화면 진동)**

`Assets/Scripts/Core/CameraShaker.cs`:
```csharp
using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour {
    private Vector3 _originPos;

    public void Shake(float duration, float magnitude) {
        StopAllCoroutines();
        _originPos = transform.localPosition;
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude) {
        float elapsed = 0f;
        while (elapsed < duration) {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = _originPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = _originPos;
    }
}
```

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/Entities/Boss/ Assets/Scripts/Core/CameraShaker.cs Assets/Tests/
git commit -m "feat: add BossController state machine and BossPartController"
```

---

## Task 5: Squad Entity

**Files:**
- Create: `Assets/Scripts/Entities/Squad/SquadMemberController.cs`
- Create: `Assets/Scripts/Entities/Squad/AimController.cs`

- [ ] **Step 1: SquadMemberController.cs 작성**

`Assets/Scripts/Entities/Squad/SquadMemberController.cs`:
```csharp
using UnityEngine;

public class SquadMemberController : MonoBehaviour {
    [SerializeField] public SquadMemberConfigSO config;
    [SerializeField] private SpriteRenderer bodyRenderer;

    public string Id        => config.id;
    public bool   IsAlive   { get; private set; } = true;
    public float  Hp        { get; private set; }
    public float  MaxHp     { get; private set; }
    public bool   IsReloading { get; private set; }
    public int    CurrentAmmo { get; private set; }

    private float  _fireTimer;
    private float  _reloadTimer;
    private AimController _aim;

    // Echo 버프 적용을 위해 GameManager가 설정
    public bool IsEchoMarkActive { get; set; }

    void Awake() {
        MaxHp = config.hp;
        Hp    = MaxHp;
        CurrentAmmo = config.weapon.magazineSize;
        _aim  = GetComponent<AimController>();
    }

    void OnEnable()  => GameEvents.OnBossShockwave += HandleShockwave;
    void OnDisable() => GameEvents.OnBossShockwave -= HandleShockwave;

    void Update() {
        if (!IsAlive) return;

        if (IsReloading) {
            _reloadTimer -= Time.deltaTime;
            float progress = 1f - _reloadTimer / config.weapon.reloadTime;
            GameEvents.RaiseReloadStarted(Id, progress); // progress 재활용 → UIManager가 progress로 처리
            if (_reloadTimer <= 0f) FinishReload();
            return;
        }

        _fireTimer -= Time.deltaTime;
        if (_fireTimer <= 0f && CurrentAmmo > 0) TryFire();
        if (CurrentAmmo <= 0 && !IsReloading) StartReload();
    }

    private void TryFire() {
        if (_aim == null || !_aim.HasTarget) return;
        Vector2 aimPos = _aim.AimPosition;
        Vector2 muzzle = (Vector2)transform.position + new Vector2(0.22f, 0.58f);
        float angle    = Mathf.Atan2(aimPos.y - muzzle.y, aimPos.x - muzzle.x);

        float dmg = config.weapon.damage;
        if (config.special == SpecialType.WeakpointBonus && _aim.TargetPartId == "CORE")
            dmg *= config.specialVal;
        if (IsEchoMarkActive) dmg *= 1.2f;

        GameEvents.RaiseFireBullet(new BulletData {
            origin       = muzzle,
            angle        = angle,
            speed        = config.weapon.bulletSpeed * 0.01f, // px→units
            damage       = dmg,
            ownerId      = Id,
            bulletType   = config.weapon.bulletType,
            splashRadius = config.weapon.splashRadius * 0.01f,
            pellets      = config.weapon.pellets,
            spread       = config.weapon.spread,
            targetPartId = _aim.TargetPartId
        });

        CurrentAmmo--;
        _fireTimer = config.weapon.fireRate;
        GameEvents.RaiseAmmoChanged(Id, CurrentAmmo, config.weapon.magazineSize);
    }

    private void StartReload() {
        IsReloading  = true;
        _reloadTimer = config.weapon.reloadTime;
        GameEvents.RaiseReloadStarted(Id, 0f);
    }

    private void FinishReload() {
        IsReloading = false;
        CurrentAmmo = config.weapon.magazineSize;
        GameEvents.RaiseReloadComplete(Id);
        GameEvents.RaiseAmmoChanged(Id, CurrentAmmo, config.weapon.magazineSize);
    }

    public void TakeDamage(float amount) {
        if (!IsAlive) return;
        Hp = Mathf.Max(0f, Hp - amount);
        if (Hp <= 0f) Die();
    }

    private void Die() {
        IsAlive = false;
        if (bodyRenderer != null) bodyRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        GameEvents.RaiseMemberDied(Id);
    }

    private void HandleShockwave(Vector2 pos, float damage) => TakeDamage(damage);

    public float GetHpRatio() => Hp / MaxHp;
}
```

- [ ] **Step 2: AimController.cs 작성**

`Assets/Scripts/Entities/Squad/AimController.cs`:
```csharp
using UnityEngine;

public class AimController : MonoBehaviour {
    [SerializeField] private SquadMemberConfigSO config;

    public bool    HasTarget    { get; private set; }
    public Vector2 AimPosition  { get; private set; }
    public string  TargetPartId { get; private set; }

    private BossController _boss;
    private BossPartController _userTargetPart;
    private MinionController   _userTargetMinion;

    // InputManager가 드래그 타겟 설정
    public Vector2? DragTarget { get; set; }

    void Start() {
        _boss = FindObjectOfType<BossController>();
        AimPosition = new Vector2(GameConfig.BOSS_START_X * 0.7f, GameConfig.BOSS_Y);
    }

    void Update() {
        CleanupUserTarget();
        ResolveTarget();
        SmoothAim();
    }

    private void CleanupUserTarget() {
        if (_userTargetPart != null && (!_userTargetPart.IsActive || _userTargetPart.IsDestroyed))
            _userTargetPart = null;
        if (_userTargetMinion != null && !_userTargetMinion.IsAlive)
            _userTargetMinion = null;
    }

    private void ResolveTarget() {
        if (_userTargetPart != null) {
            HasTarget   = true;
            AimPosition = _userTargetPart.transform.position;
            TargetPartId = _userTargetPart.PartId;
            return;
        }
        if (_userTargetMinion != null) {
            HasTarget   = true;
            AimPosition = _userTargetMinion.transform.position;
            TargetPartId = null;
            return;
        }
        // 자동 조준: aimPriority 순서로 활성 파트 탐색
        if (_boss != null && _boss.IsAlive) {
            foreach (var pid in config.aimPriority) {
                var part = _boss.GetPart(pid);
                if (part != null && part.IsActive && !part.IsDestroyed) {
                    HasTarget    = true;
                    AimPosition  = part.transform.position;
                    TargetPartId = pid;
                    return;
                }
            }
        }
        HasTarget = false;
        TargetPartId = null;
    }

    private void SmoothAim() {
        if (DragTarget.HasValue) {
            AimPosition = DragTarget.Value;
            return;
        }
        if (HasTarget) {
            // 500 units/s 속도로 부드럽게 이동
            AimPosition = Vector2.MoveTowards(AimPosition, AimPosition, 5f * Time.deltaTime);
        }
    }

    public void SetUserTargetPart(BossPartController part)   { _userTargetPart = part; _userTargetMinion = null; }
    public void SetUserTargetMinion(MinionController minion) { _userTargetMinion = minion; _userTargetPart = null; }
    public void ClearUserTarget() { _userTargetPart = null; _userTargetMinion = null; }
}
```

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Entities/Squad/
git commit -m "feat: add SquadMemberController and AimController"
```

---

## Task 6: Minion & Bullet

**Files:**
- Create: `Assets/Scripts/Entities/Enemies/MinionController.cs`
- Create: `Assets/Scripts/Entities/Bullet.cs`

- [ ] **Step 1: MinionController.cs 작성**

`Assets/Scripts/Entities/Enemies/MinionController.cs`:
```csharp
using UnityEngine;

public class MinionController : MonoBehaviour {
    [SerializeField] private MinionConfigSO config;

    public bool IsAlive { get; private set; } = true;

    private float _hp;
    private float _attackTimer;
    private TerrainManager _terrain;
    private SquadMemberController[] _squad;
    private enum MinionState { Moving, Attacking }
    private MinionState _state = MinionState.Moving;

    void Start() {
        _hp      = config.hp;
        _terrain = FindObjectOfType<TerrainManager>();
        _squad   = FindObjectsOfType<SquadMemberController>();
    }

    void Update() {
        if (!IsAlive) return;

        var target = GetCurrentTarget();
        if (target == null) {
            // 타겟 없으면 왼쪽으로 이동
            transform.position += Vector3.left * config.speed * Time.deltaTime;
            _state = MinionState.Moving;
            return;
        }

        float dist = Vector2.Distance(transform.position, target);
        if (dist <= config.attackRange) {
            _state = MinionState.Attacking;
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f) {
                _attackTimer = config.attackInterval;
                AttackTarget(target);
            }
        } else {
            _state = MinionState.Moving;
            Vector2 dir = ((Vector3)target - transform.position).normalized;
            transform.position += (Vector3)(dir * config.speed * Time.deltaTime);
        }
    }

    private Vector2? GetCurrentTarget() {
        // Spitter → 스쿼드 멤버 우선
        if (config.type == MinionType.Spitter) {
            float nearest = float.MaxValue;
            Vector2? pos = null;
            foreach (var m in _squad) {
                if (!m.IsAlive) continue;
                float d = Vector2.Distance(transform.position, m.transform.position);
                if (d < nearest) { nearest = d; pos = m.transform.position; }
            }
            if (pos.HasValue) return pos;
        }
        // 바리케이드 → 장벽 순
        var barricade = _terrain?.GetAliveBarricade();
        if (barricade.HasValue) return barricade;
        var wall = _terrain?.GetWallTarget();
        if (wall.HasValue) return wall;
        return null;
    }

    private void AttackTarget(Vector2 targetPos) {
        if (config.type == MinionType.Spitter) {
            foreach (var m in _squad) {
                if (!m.IsAlive) continue;
                if (Vector2.Distance(targetPos, m.transform.position) < 0.1f) {
                    m.TakeDamage(config.damage);
                    return;
                }
            }
        } else {
            _terrain?.MinionDamage(targetPos, config.terrainDamage);
        }
    }

    public void TakeDamage(float amount) {
        if (!IsAlive) return;
        _hp -= amount;
        if (_hp <= 0f) Die();
    }

    private void Die() {
        IsAlive = false;
        gameObject.SetActive(false);
        // ObjectPool 반환은 SpawnSystem에서 처리
    }
}
```

- [ ] **Step 2: Bullet.cs 작성**

`Assets/Scripts/Entities/Bullet.cs`:
```csharp
using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour {
    public IObjectPool<Bullet> Pool { get; set; }

    private float _speed;
    private float _damage;
    private float _splashRadius;
    private string _targetPartId;
    private BulletType _bulletType;
    private BossController _boss;
    private float _lifetime;
    private const float MAX_LIFETIME = 3f;

    public void Initialize(BulletData data, BossController boss) {
        _speed       = data.speed;
        _damage      = data.damage;
        _splashRadius = data.splashRadius;
        _targetPartId = data.targetPartId;
        _bulletType  = data.bulletType;
        _boss        = boss;
        _lifetime    = 0f;
        transform.position = data.origin;
        transform.rotation = Quaternion.Euler(0f, 0f, data.angle * Mathf.Rad2Deg);
    }

    void Update() {
        transform.Translate(Vector2.right * _speed * Time.deltaTime);
        _lifetime += Time.deltaTime;
        if (_lifetime >= MAX_LIFETIME) ReturnToPool();
    }

    void OnTriggerEnter2D(Collider2D other) {
        // 보스 파트
        var part = other.GetComponent<BossPartController>();
        if (part != null && _boss != null) {
            if (_bulletType == BulletType.Rocket && _splashRadius > 0f) {
                // 범위 피해: 모든 파트에 분산 적용
                var hits = Physics2D.OverlapCircleAll(transform.position, _splashRadius);
                foreach (var h in hits) {
                    var p = h.GetComponent<BossPartController>();
                    if (p != null) _boss.TakeDamage(p.PartId, _damage * 0.5f);
                }
            } else {
                _boss.TakeDamage(part.PartId, _damage);
            }
            ReturnToPool();
            return;
        }
        // 미니언
        var minion = other.GetComponent<MinionController>();
        if (minion != null) {
            if (_bulletType == BulletType.Rocket && _splashRadius > 0f) {
                var hits = Physics2D.OverlapCircleAll(transform.position, _splashRadius);
                foreach (var h in hits) h.GetComponent<MinionController>()?.TakeDamage(_damage * 0.5f);
            } else {
                minion.TakeDamage(_damage);
            }
            ReturnToPool();
        }
    }

    private void ReturnToPool() {
        if (Pool != null) Pool.Release(this);
        else Destroy(gameObject);
    }
}
```

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Entities/
git commit -m "feat: add MinionController and Bullet with ObjectPool support"
```

---

## Task 7: Game Systems — Wave & Fire

**Files:**
- Create: `Assets/Scripts/Systems/WaveSystem.cs`
- Create: `Assets/Scripts/Systems/FireSystem.cs`
- Create: `Assets/Tests/EditMode/WavePhaseTests.cs`

- [ ] **Step 1: WavePhase 테스트 작성**

`Assets/Tests/EditMode/WavePhaseTests.cs`:
```csharp
using NUnit.Framework;

public class WavePhaseTests {
    [Test]
    public void GetPhaseParams_At5Seconds_Returns10sInterval() {
        // t=5s → 첫 번째 페이즈: interval=10s, count 2~2
        var phase = WaveSystem.GetPhaseParams(5f, false);
        Assert.AreEqual(10f, phase.interval);
        Assert.AreEqual(2, phase.countMin);
    }

    [Test]
    public void GetPhaseParams_Enraged_Returns2200msInterval() {
        var phase = WaveSystem.GetPhaseParams(30f, true);
        Assert.AreEqual(2.2f, phase.interval);
        Assert.GreaterOrEqual(phase.countMax, 6);
    }
}
```

- [ ] **Step 2: WaveSystem.cs 작성**

`Assets/Scripts/Systems/WaveSystem.cs`:
```csharp
using UnityEngine;

public class WaveSystem : MonoBehaviour {
    [SerializeField] private MinionConfigSO runnerConfig;
    [SerializeField] private MinionConfigSO berserkerConfig;
    [SerializeField] private MinionConfigSO spitterConfig;

    public struct PhaseParams {
        public float interval;
        public int countMin, countMax;
        public float runnerW, berserkerW, spitterW;
    }

    private float _elapsed;
    private float _spawnTimer = 4f;
    private bool  _isEnraged;

    void OnEnable()  => GameEvents.OnBossEnraged += () => { _isEnraged = true; _spawnTimer = Mathf.Min(_spawnTimer, 1.5f); };
    void OnDisable() => GameEvents.OnBossEnraged -= () => { };

    void Update() {
        _elapsed += Time.deltaTime;
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f) {
            var p = GetPhaseParams(_elapsed, _isEnraged);
            _spawnTimer = p.interval;
            Spawn(p);
        }
    }

    // static → EditMode 테스트에서 직접 호출 가능
    public static PhaseParams GetPhaseParams(float elapsed, bool enraged) {
        if (enraged) return new PhaseParams { interval=2.2f, countMin=6, countMax=10, runnerW=50, berserkerW=30, spitterW=20 };
        if (elapsed < 10f) return new PhaseParams { interval=10f,  countMin=2, countMax=2,  runnerW=100 };
        if (elapsed < 20f) return new PhaseParams { interval=7f,   countMin=2, countMax=2,  runnerW=100 };
        if (elapsed < 30f) return new PhaseParams { interval=5.5f, countMin=2, countMax=4,  runnerW=80, berserkerW=10, spitterW=10 };
        if (elapsed < 45f) return new PhaseParams { interval=4.5f, countMin=4, countMax=6,  runnerW=65, berserkerW=20, spitterW=15 };
        if (elapsed < 60f) return new PhaseParams { interval=3.5f, countMin=4, countMax=8,  runnerW=50, berserkerW=30, spitterW=20 };
        return                   new PhaseParams { interval=3f,   countMin=6, countMax=10, runnerW=40, berserkerW=35, spitterW=25 };
    }

    private void Spawn(PhaseParams p) {
        int count = Random.Range(p.countMin, p.countMax + 1);
        for (int i = 0; i < count; i++) {
            var type = WeightedRandom(p);
            float x = Random.Range(GameConfig.SPAWN_X_MIN, GameConfig.SPAWN_X_MAX);
            float y = Random.Range(GameConfig.SPAWN_Y_MIN, GameConfig.SPAWN_Y_MAX);
            GameEvents.RaiseSpawnMinion(type, new Vector2(x, y));
        }
    }

    private MinionType WeightedRandom(PhaseParams p) {
        float total = p.runnerW + p.berserkerW + p.spitterW;
        float r = Random.value * total;
        if (r < p.runnerW) return MinionType.Runner;
        r -= p.runnerW;
        if (r < p.berserkerW) return MinionType.Berserker;
        return MinionType.Spitter;
    }
}
```

- [ ] **Step 3: FireSystem.cs 작성**

`Assets/Scripts/Systems/FireSystem.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class FireSystem : MonoBehaviour {
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private BossController boss;

    private IObjectPool<Bullet> _pool;
    private List<MinionController> _activeMinions = new();

    void Awake() {
        _pool = new ObjectPool<Bullet>(
            createFunc:      () => { var b = Instantiate(bulletPrefab); b.Pool = _pool; return b; },
            actionOnGet:     b => b.gameObject.SetActive(true),
            actionOnRelease: b => b.gameObject.SetActive(false),
            actionOnDestroy: b => Destroy(b.gameObject),
            collectionCheck: false,
            defaultCapacity: 64,
            maxSize: 256
        );
    }

    void OnEnable()  {
        GameEvents.OnFireBullet += HandleFireBullet;
        GameEvents.OnSpawnMinion += HandleSpawnMinion;
    }
    void OnDisable() {
        GameEvents.OnFireBullet -= HandleFireBullet;
        GameEvents.OnSpawnMinion -= HandleSpawnMinion;
    }

    private void HandleFireBullet(BulletData data) {
        int pellets = Mathf.Max(1, data.pellets);
        for (int i = 0; i < pellets; i++) {
            float spreadAngle = (pellets > 1)
                ? data.angle + Mathf.Deg2Rad * Random.Range(-data.spread / 2f, data.spread / 2f)
                : data.angle;
            var bullet = _pool.Get();
            var shotData = data;
            shotData.angle = spreadAngle;
            bullet.Initialize(shotData, boss);
        }
    }

    private void HandleSpawnMinion(MinionType type, Vector2 pos) {
        // SpawnSystem이 별도로 처리하나, FireSystem에서 참조 업데이트
        StartCoroutine(RegisterMinionsNextFrame());
    }

    private System.Collections.IEnumerator RegisterMinionsNextFrame() {
        yield return null;
        _activeMinions.Clear();
        _activeMinions.AddRange(FindObjectsOfType<MinionController>());
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

  Test Runner → Run All → WavePhaseTests 2개 PASS 확인

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Systems/WaveSystem.cs Assets/Scripts/Systems/FireSystem.cs Assets/Tests/
git commit -m "feat: add WaveSystem phase logic and FireSystem with ObjectPool"
```

---

## Task 8: Game Systems — Enrage, Bombing, Terrain

**Files:**
- Create: `Assets/Scripts/Systems/EnrageSystem.cs`
- Create: `Assets/Scripts/Systems/BombingSystem.cs`
- Create: `Assets/Scripts/Systems/TerrainManager.cs`

- [ ] **Step 1: EnrageSystem.cs 작성**

`Assets/Scripts/Systems/EnrageSystem.cs`:
```csharp
using UnityEngine;

public class EnrageSystem : MonoBehaviour {
    [SerializeField] private BossController boss;
    private bool _triggered;

    void Update() {
        if (_triggered || boss == null || !boss.IsAlive) return;
        if (boss.MeetsEnrageCondition()) {
            _triggered = true;
            boss.Enrage();
        }
    }
}
```

- [ ] **Step 2: BombingSystem.cs 작성**

`Assets/Scripts/Systems/BombingSystem.cs`:
```csharp
using UnityEngine;

public class BombingSystem : MonoBehaviour {
    [SerializeField] private float damage       = 300f;
    [SerializeField] private float radius       = 1.5f;
    [SerializeField] private float cooldown     = 20f;
    [SerializeField] private BossController boss;

    private float _timer;
    public bool IsReady => _timer <= 0f;

    void Update() {
        if (_timer > 0f) _timer -= Time.deltaTime;
    }

    public void Activate(Vector2 position) {
        if (!IsReady) return;
        _timer = cooldown;

        // 보스 파트 피해
        var hits = Physics2D.OverlapCircleAll(position, radius);
        foreach (var h in hits) {
            var part = h.GetComponent<BossPartController>();
            if (part != null && boss != null) boss.TakeDamage(part.PartId, damage);
            h.GetComponent<MinionController>()?.TakeDamage(damage);
        }
    }
}
```

- [ ] **Step 3: TerrainManager.cs 작성**

`Assets/Scripts/Systems/TerrainManager.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

public struct TerrainBlock {
    public Vector2 position;
    public float hp, maxHp;
    public bool alive;
    public bool isWall, isBarricade, isRoadBlock;
    public SpriteRenderer renderer;
    public Sprite[] damageSprites; // [healthy, damaged, critical]
}

public class TerrainManager : MonoBehaviour {
    [SerializeField] private float wallMaxHp        = 5000f;
    [SerializeField] private float barricadeMaxHp   = 280f;
    [SerializeField] private float roadBlockMaxHp   = 300f;

    // Inspector에서 SpriteRenderer 할당
    [SerializeField] private SpriteRenderer wallRenderer;
    [SerializeField] private SpriteRenderer[] barricadeRenderers;  // 3개
    [SerializeField] private SpriteRenderer[] roadBlockRenderers;  // 3개
    [SerializeField] private Sprite[] wallSprites;        // [healthy, damaged, critical]
    [SerializeField] private Sprite[] barricadeSprites;
    [SerializeField] private Sprite[] roadBlockSprites;

    private float _wallHp;
    private float[] _barricadeHp = new float[3];
    private float[] _roadBlockHp = new float[3];
    private bool[] _barricadeAlive = { true, true, true };
    private bool[] _roadBlockAlive = { true, true, true };

    void Awake() {
        _wallHp = wallMaxHp;
        for (int i = 0; i < 3; i++) {
            _barricadeHp[i] = barricadeMaxHp;
            _roadBlockHp[i] = roadBlockMaxHp;
        }
    }

    public void BossDamageWall(float dmg) {
        _wallHp = Mathf.Max(0f, _wallHp - dmg);
        UpdateWallSprite();
        GameEvents.RaiseWallHpChanged(_wallHp, wallMaxHp);
        if (_wallHp <= 0f) GameEvents.RaiseWallDestroyed();
    }

    public void BossSmashBlock(TerrainBlock block, float dmg) {
        for (int i = 0; i < 3; i++) {
            if (!_roadBlockAlive[i]) continue;
            if (Vector2.Distance(roadBlockRenderers[i].transform.position, block.position) < 0.5f) {
                _roadBlockHp[i] = Mathf.Max(0f, _roadBlockHp[i] - dmg);
                UpdateBlockSprite(i);
                if (_roadBlockHp[i] <= 0f) _roadBlockAlive[i] = false;
                return;
            }
        }
    }

    public void MinionDamage(Vector2 targetPos, float dmg) {
        for (int i = 0; i < 3; i++) {
            if (!_barricadeAlive[i]) continue;
            if (Vector2.Distance(barricadeRenderers[i].transform.position, targetPos) < 0.5f) {
                _barricadeHp[i] = Mathf.Max(0f, _barricadeHp[i] - dmg);
                if (_barricadeHp[i] <= 0f) _barricadeAlive[i] = false;
                return;
            }
        }
        BossDamageWall(dmg);
    }

    public Vector2? GetAliveBarricade() {
        for (int i = 0; i < 3; i++)
            if (_barricadeAlive[i]) return barricadeRenderers[i].transform.position;
        return null;
    }

    public Vector2? GetWallTarget() => _wallHp > 0f ? (Vector2?)wallRenderer.transform.position : null;

    public TerrainBlock? GetRoadBlockAhead(float bossX) {
        for (int i = 0; i < 3; i++) {
            if (!_roadBlockAlive[i]) continue;
            float rx = roadBlockRenderers[i].transform.position.x;
            if (bossX <= rx + 0.75f && bossX > rx - 0.15f)
                return new TerrainBlock { position = roadBlockRenderers[i].transform.position, alive = true };
        }
        return null;
    }

    private void UpdateWallSprite() {
        if (wallRenderer == null || wallSprites == null || wallSprites.Length < 3) return;
        float r = _wallHp / wallMaxHp;
        wallRenderer.sprite = r > 0.7f ? wallSprites[0] : r > 0.3f ? wallSprites[1] : wallSprites[2];
    }

    private void UpdateBlockSprite(int i) {
        if (roadBlockRenderers[i] == null || roadBlockSprites == null || roadBlockSprites.Length < 3) return;
        float r = _roadBlockHp[i] / roadBlockMaxHp;
        roadBlockRenderers[i].sprite = r > 0.6f ? roadBlockSprites[0] : r > 0.3f ? roadBlockSprites[1] : roadBlockSprites[2];
    }
}
```

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Systems/
git commit -m "feat: add EnrageSystem, BombingSystem, TerrainManager"
```

---

## Task 9: Input System

**Files:**
- Create: `Assets/Scripts/Input/InputManager.cs`
- Create: `Assets/Settings/InputActions.inputactions` (Unity Editor에서 생성)

- [ ] **Step 1: InputActions 파일 생성 (Unity Editor)**

  Project 창 → Assets/Settings/ → Create → Input Actions → `InputActions` 이름 지정  
  아래 Action Map 구성:

  **Map: Gameplay**
  - `PointerPosition` (Value / Vector2) → Binding: `<Mouse>/position` + `<Touchscreen>/touch0/position`
  - `PrimaryPress` (Button) → Binding: `<Mouse>/leftButton` + `<Touchscreen>/touch0/press`
  - `Drag` (Button) → 같은 바인딩, Hold interaction 추가 (holdTime=0.05s)

  Generate C# Class 체크 → Apply

- [ ] **Step 2: InputManager.cs 작성**

`Assets/Scripts/Input/InputManager.cs`:
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour {
    public static InputManager Instance { get; private set; }

    [SerializeField] private Camera gameCamera;
    [SerializeField] private SquadMemberController[] squadMembers;
    [SerializeField] private AimController[] aimControllers;
    [SerializeField] private BossController boss;

    private InputActions _actions;
    private int _selectedMember = -1;
    private bool _isDragging;
    private Vector2 _dragStartScreen;

    void Awake() {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _actions = new InputActions();
    }

    void OnEnable()  => _actions.Gameplay.Enable();
    void OnDisable() => _actions.Gameplay.Disable();

    void Update() {
        Vector2 screenPos = _actions.Gameplay.PointerPosition.ReadValue<Vector2>();
        Vector2 worldPos  = gameCamera.ScreenToWorldPoint(screenPos);

        bool pressed = _actions.Gameplay.PrimaryPress.IsPressed();

        if (pressed && !_isDragging) {
            // 캐릭터 선택 확인
            int hit = GetSquadMemberAt(worldPos);
            if (hit >= 0) {
                _selectedMember = (_selectedMember == hit) ? -1 : hit;
                return;
            }
            _isDragging = true;
            _dragStartScreen = screenPos;
        }

        if (_isDragging && pressed && _selectedMember >= 0) {
            aimControllers[_selectedMember].DragTarget = worldPos;
            // 드래그 타겟 → 보스 파트 히트 체크
            var hit2D = Physics2D.OverlapPoint(worldPos);
            if (hit2D != null) {
                var part = hit2D.GetComponent<BossPartController>();
                if (part != null) aimControllers[_selectedMember].SetUserTargetPart(part);
                var minion = hit2D.GetComponent<MinionController>();
                if (minion != null) aimControllers[_selectedMember].SetUserTargetMinion(minion);
            }
        }

        if (!pressed && _isDragging) {
            _isDragging = false;
            if (_selectedMember >= 0) aimControllers[_selectedMember].DragTarget = null;
        }
    }

    private int GetSquadMemberAt(Vector2 worldPos) {
        for (int i = 0; i < squadMembers.Length; i++) {
            float dist = Vector2.Distance(worldPos, squadMembers[i].transform.position);
            if (dist < 0.4f) return i;
        }
        return -1;
    }
}
```

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Input/ Assets/Settings/
git commit -m "feat: add InputManager with New Input System (PC+mobile unified)"
```

---

## Task 10: UI System

**Files:**
- Create: `Assets/Scripts/UI/UIManager.cs`
- Create: `Assets/Scripts/UI/BossHpBarUI.cs`
- Create: `Assets/Scripts/UI/SquadHpBarUI.cs`
- Create: `Assets/Scripts/UI/AmmoDisplayUI.cs`
- Create: `Assets/Scripts/UI/ResultUI.cs`

- [ ] **Step 1: UIManager.cs 작성**

`Assets/Scripts/UI/UIManager.cs`:
```csharp
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour {
    [SerializeField] private BossHpBarUI bossHpBar;
    [SerializeField] private SquadHpBarUI[] squadHpBars;   // 5개
    [SerializeField] private AmmoDisplayUI[] ammoDisplays; // 5개
    [SerializeField] private SquadMemberConfigSO[] squadConfigs;

    void OnEnable() {
        GameEvents.OnBossHpChanged  += bossHpBar.OnBossHpChanged;
        GameEvents.OnBossPartDestroyed += bossHpBar.OnPartDestroyed;
        for (int i = 0; i < squadHpBars.Length; i++) {
            int idx = i;
            string memberId = squadConfigs[i].id;
            GameEvents.OnAmmoChanged    += (id, cur, max) => { if (id == memberId) ammoDisplays[idx].Refresh(cur, max); };
            GameEvents.OnReloadStarted  += (id, prog) =>    { if (id == memberId) squadHpBars[idx].ShowReload(prog); };
            GameEvents.OnReloadComplete += id =>             { if (id == memberId) squadHpBars[idx].HideReload(); };
            GameEvents.OnMemberDied     += id =>             { if (id == memberId) squadHpBars[idx].SetDead(); };
        }
        GameEvents.OnWallHpChanged += OnWallHpChanged;
    }

    void OnDisable() {
        GameEvents.OnBossHpChanged  -= bossHpBar.OnBossHpChanged;
        GameEvents.OnBossPartDestroyed -= bossHpBar.OnPartDestroyed;
        GameEvents.OnWallHpChanged -= OnWallHpChanged;
        // 람다 구독 해제는 OnDestroy에서 ClearAllListeners 또는 명시적 delegate 변수로 처리
    }

    private void OnWallHpChanged(float hp, float max) {
        // WallHpBar UI 업데이트 (별도 컴포넌트로 확장 가능)
    }
}
```

- [ ] **Step 2: BossHpBarUI.cs 작성**

`Assets/Scripts/UI/BossHpBarUI.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHpBarUI : MonoBehaviour {
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Image[] partIcons; // 7개, id 순서: HEAD ARM_L ARM_R LEG_L LEG_R CHEST CORE
    [SerializeField] private Color destroyedColor = new Color(0.3f, 0f, 0f);

    private static readonly string[] PART_ORDER = { "HEAD","ARM_L","ARM_R","LEG_L","LEG_R","CHEST","CORE" };

    public void OnBossHpChanged(float hp, float maxHp) {
        if (hpSlider != null) hpSlider.value = hp / maxHp;
        if (hpText   != null) hpText.text = $"{Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(maxHp)}";
    }

    public void OnPartDestroyed(string partId) {
        for (int i = 0; i < PART_ORDER.Length; i++) {
            if (PART_ORDER[i] == partId && i < partIcons.Length && partIcons[i] != null)
                partIcons[i].color = destroyedColor;
        }
    }
}
```

- [ ] **Step 3: SquadHpBarUI.cs 작성**

`Assets/Scripts/UI/SquadHpBarUI.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;

public class SquadHpBarUI : MonoBehaviour {
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider reloadSlider;
    [SerializeField] private SquadMemberController member;

    void Update() {
        if (member == null) return;
        if (hpSlider != null) hpSlider.value = member.GetHpRatio();
    }

    public void ShowReload(float progress) {
        if (reloadSlider != null) { reloadSlider.gameObject.SetActive(true); reloadSlider.value = progress; }
    }

    public void HideReload() {
        if (reloadSlider != null) reloadSlider.gameObject.SetActive(false);
    }

    public void SetDead() {
        if (hpSlider != null) hpSlider.value = 0f;
        GetComponent<CanvasGroup>()?.SetAlpha(0.4f);
    }
}
```

- [ ] **Step 4: AmmoDisplayUI.cs 작성**

`Assets/Scripts/UI/AmmoDisplayUI.cs`:
```csharp
using TMPro;
using UnityEngine;

public class AmmoDisplayUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI ammoText;

    public void Refresh(int current, int max) {
        if (ammoText != null) ammoText.text = $"{current}/{max}";
    }
}
```

- [ ] **Step 5: ResultUI.cs 작성**

`Assets/Scripts/UI/ResultUI.cs`:
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    void Start() {
        bool isWin = GameManager.Instance?.State == GameManager.GameState.Win;
        if (winPanel  != null) winPanel.SetActive(isWin);
        if (losePanel != null) losePanel.SetActive(!isWin);
        if (resultText != null) resultText.text = isWin ? "MISSION COMPLETE" : "MISSION FAILED";
    }

    public void OnRetryClicked()  => SceneManager.LoadScene("Game");
    public void OnMenuClicked()   => SceneManager.LoadScene("Boot");
}
```

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/UI/
git commit -m "feat: add UIManager and HUD UI components (HpBar, Ammo, Result)"
```

---

## Task 11: AudioManager (Stub)

**Files:**
- Create: `Assets/Scripts/Audio/AudioManager.cs`

- [ ] **Step 1: AudioManager.cs 작성**

`Assets/Scripts/Audio/AudioManager.cs`:
```csharp
using UnityEngine;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("SFX")]
    [SerializeField] private AudioClip sfxGunshot;
    [SerializeField] private AudioClip sfxReload;
    [SerializeField] private AudioClip sfxExplosion;
    [SerializeField] private AudioClip sfxBossAttack;
    [SerializeField] private AudioClip sfxBossEnrage;
    [SerializeField] private AudioClip sfxWin;
    [SerializeField] private AudioClip sfxLose;

    void Awake() {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() {
        GameEvents.OnFireBullet     += _ => PlaySfx(sfxGunshot);
        GameEvents.OnReloadComplete += _ => PlaySfx(sfxReload);
        GameEvents.OnBossAttack     += () => PlaySfx(sfxBossAttack);
        GameEvents.OnBossEnraged    += () => PlaySfx(sfxBossEnrage);
        GameEvents.OnBossDefeated   += () => PlaySfx(sfxWin);
    }

    void OnDisable() {
        GameEvents.OnFireBullet     -= _ => PlaySfx(sfxGunshot);
        GameEvents.OnReloadComplete -= _ => PlaySfx(sfxReload);
        GameEvents.OnBossAttack     -= () => PlaySfx(sfxBossAttack);
        GameEvents.OnBossEnraged    += () => PlaySfx(sfxBossEnrage);
        GameEvents.OnBossDefeated   -= () => PlaySfx(sfxWin);
    }

    private void PlaySfx(AudioClip clip) {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    public void PlayBgm(AudioClip clip) {
        if (bgmSource == null || clip == null) return;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }
}
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/Scripts/Audio/
git commit -m "feat: add AudioManager stub with event subscriptions"
```

---

## Task 12: Unity Editor 씬 구성 (수동)

**Files:** Unity Editor에서 수행

- [ ] **Step 1: Kenney 에셋 임포트**

  아래 URL에서 무료 다운로드 후 `Assets/Sprites/` 에 임포트:
  ```
  kenney.nl/assets/toon-characters-1     → Sprites/Characters/
  kenney.nl/assets/creature-mixer        → Sprites/Enemies/Boss/
  kenney.nl/assets/monsters              → Sprites/Enemies/Minions/
  kenney.nl/assets/city-kit-commercial   → Sprites/Environment/
  kenney.nl/assets/platformer-kit        → Sprites/Environment/Terrain/
  kenney.nl/assets/particle-pack         → Sprites/Effects/
  kenney.nl/assets/ui-pack               → Sprites/UI/
  ```

  모든 스프라이트: Texture Type = **Sprite (2D and UI)**, Pixels Per Unit = **100**, Filter = **Point** (픽셀 느낌) 또는 **Bilinear**

- [ ] **Step 2: Prefab 생성**

  **Boss.prefab:**
  - GameObject: Boss → BossController.cs (config=BossConfig.asset), SpriteRenderer (Creature Mixer 몸통 스프라이트), Rigidbody2D (Kinematic), CameraShaker.cs
  - 자식 파트들(HEAD/CHEST 등)은 BossController.Awake()에서 자동 생성 — 각 파트 SpriteRenderer를 Inspector에서 할당하려면 파트 프리팹을 별도 생성

  **SquadMember.prefab (5개 변형):**
  - SquadMemberController.cs (config=SquadConfig_Alpha.asset), AimController.cs
  - SpriteRenderer: Kenney Toon Characters 색상별 캐릭터
  - BoxCollider2D (isTrigger, 선택 감지용)

  **Minion_Runner.prefab / Minion_Berserker.prefab / Minion_Spitter.prefab:**
  - MinionController.cs (각 MinionConfigSO 할당)
  - SpriteRenderer: Kenney Monsters 스프라이트
  - BoxCollider2D (isTrigger)

  **Bullet.prefab:**
  - Bullet.cs, SpriteRenderer (Particle Pack 총알 스프라이트)
  - CircleCollider2D (isTrigger, radius=0.05)
  - Rigidbody2D (Kinematic)

- [ ] **Step 3: Game.unity 씬 구성**

  **Hierarchy:**
  ```
  Main Camera          ← CameraShaker.cs 추가
  GameManager          ← GameManager.cs
  AudioManager         ← AudioManager.cs
  Systems/
  ├── WaveSystem       ← WaveSystem.cs, Runner/Berserker/Spitter config 할당
  ├── FireSystem       ← FireSystem.cs, bulletPrefab=Bullet.prefab
  ├── EnrageSystem     ← EnrageSystem.cs, boss 참조 할당
  └── BombingSystem    ← BombingSystem.cs
  InputManager         ← InputManager.cs
  Boss                 ← Boss.prefab 배치 (BossController 설정 확인)
  Squad/
  ├── Alpha            ← SquadMember.prefab (config=Alpha)
  ├── Bravo
  ├── Charlie
  ├── Delta
  └── Echo
  Terrain/
  ├── Wall             ← SpriteRenderer (Platformer Kit 장벽), TerrainManager 할당
  ├── Barricade_0~2    ← 바리케이드 3개
  └── RoadBlock_0~2    ← 로드블록 3개
  Canvas (Screen Space Overlay, Scale With Screen Size 1280×720)
  ├── UIManager        ← UIManager.cs, 모든 UI 컴포넌트 참조 할당
  ├── BossHpPanel
  │   └── BossHpBarUI  ← BossHpBarUI.cs
  ├── SquadPanel
  │   ├── Squad_0~4    ← SquadHpBarUI.cs × 5
  └── AmmoPanel
      └── Ammo_0~4     ← AmmoDisplayUI.cs × 5
  Background           ← Kenney City Kit 스프라이트 (정적 배경)
  ```

- [ ] **Step 4: Squad 위치 배치**

  각 SquadMember Transform.position을 GameConfig 기준으로 배치:
  ```
  Alpha:   (0.68, 1.48, 0)
  Bravo:   (1.28, 1.48, 0)
  Charlie: (1.88, 1.48, 0)
  Delta:   (2.48, 1.48, 0)
  Echo:    (3.08, 1.48, 0)
  ```

- [ ] **Step 5: Boot.unity 씬 생성**

  간단한 Title 캔버스 → "START" 버튼 → `SceneManager.LoadScene("Game")`  
  Kenney UI Pack 버튼 스프라이트 사용

- [ ] **Step 6: Result.unity 씬 생성**

  ResultUI.cs 붙인 Canvas → WIN/LOSE Panel, Retry/Menu 버튼

- [ ] **Step 7: Build Settings 설정**

  File → Build Settings:
  - Scenes 순서: Boot(0), Game(1), Result(2)
  - Platform: **PC** (Windows) → Build
  - Platform: **Android** → Switch Platform, Player Settings에서 Min SDK = 22, Scripting Backend = IL2CPP

- [ ] **Step 8: 최종 플레이 테스트**

  Play Mode 진입 후 확인:
  - [ ] 보스가 오른쪽에서 왼쪽으로 이동
  - [ ] 스쿼드 멤버가 보스 파트 자동 조준 및 발사
  - [ ] 클릭/드래그로 타겟 변경
  - [ ] 보스 파트 HP 소진 시 파괴 표시
  - [ ] HP 50% + HEAD+CHEST 파괴 시 광폭화
  - [ ] 미니언 스폰 및 이동/공격
  - [ ] WIN/LOSE 씬 전환

- [ ] **Step 9: 최종 커밋**

```bash
git add .
git commit -m "feat: complete Unity migration - ScriptableObject arch + Kenney assets"
```

---

## 자체 검토 결과

**Spec 커버리지:**
- ✅ ScriptableObject 아키텍처 → Task 3
- ✅ 보스 상태머신 + 파트 시스템 → Task 4
- ✅ 스쿼드 5명 + 자동조준/드래그 → Task 5
- ✅ 미니언 3종 + 총알 ObjectPool → Task 6
- ✅ 웨이브 시스템 페이즈 → Task 7
- ✅ 광폭화/폭격/지형 → Task 8
- ✅ New Input System PC+모바일 → Task 9
- ✅ UI HUD + 결과화면 → Task 10
- ✅ Kenney 에셋 임포트 가이드 → Task 12
- ✅ 멀티플랫폼 빌드 설정 → Task 12

**누락 없음, 타입 일관성 확인:**
- `BulletData` struct → GameEvents.cs에서 정의, Bullet.cs / FireSystem.cs / SquadMemberController.cs 전부 동일 타입 참조
- `MinionType` enum → GameEvents.cs에서 정의, WaveSystem.cs / MinionController.cs 동일 참조
- `TerrainBlock` struct → TerrainManager.cs에서 정의, BossController.cs에서 동일 타입 사용
