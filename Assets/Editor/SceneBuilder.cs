using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SceneBuilder {
    const string SPRITES = "Assets/Sprites";
    const string CONFIGS = "Assets/Configs";
    const string PREFABS = "Assets/Prefabs";
    const string SCENES  = "Assets/Scenes";

    // ─── Entry Point ──────────────────────────────────────────────────────────
    [MenuItem("SquadVsMonster/Build Game Scene")]
    public static void Build() {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        EnsureFolder(SPRITES);
        EnsureFolder(CONFIGS);
        EnsureFolder(PREFABS);
        EnsureFolder(SCENES);

        // Sprites
        Sprite white = WhiteSprite();

        // Configs
        var runnerCfg    = MakeMinionConfig("Runner",    MinionType.Runner,    60,  1.2f, 20f, 0.4f, 1.5f, 15f);
        var berserkerCfg = MakeMinionConfig("Berserker", MinionType.Berserker, 120, 0.8f, 35f, 0.5f, 1.2f, 20f);
        var spitterCfg   = MakeMinionConfig("Spitter",   MinionType.Spitter,   80,  1.0f, 25f, 4.0f, 2.5f, 10f);
        var bossConfig   = MakeBossConfig();
        var squadCfgs    = MakeSquadConfigs();
        AssetDatabase.SaveAssets();

        // Prefabs
        var bulletPrefab    = MakeBulletPrefab(white);
        var runnerPrefab    = MakeMinionPrefab("Runner",    runnerCfg,    white, new Color(0.70f, 0.60f, 0.40f));
        var berserkerPrefab = MakeMinionPrefab("Berserker", berserkerCfg, white, new Color(0.70f, 0.20f, 0.15f));
        var spitterPrefab   = MakeMinionPrefab("Spitter",   spitterCfg,   white, new Color(0.20f, 0.60f, 0.55f));

        // Scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene(white, bossConfig, squadCfgs,
                   runnerCfg, berserkerCfg, spitterCfg,
                   bulletPrefab, runnerPrefab, berserkerPrefab, spitterPrefab);

        string scenePath = SCENES + "/Game.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AddToBuildSettings(scenePath);
        AssetDatabase.Refresh();

        bool open = EditorUtility.DisplayDialog("SceneBuilder",
            "Game scene built successfully!\n\nAssets/Scenes/Game.unity",
            "Open Scene", "Close");
        if (open) EditorSceneManager.OpenScene(scenePath);
    }

    // ─── Scene Assembly ───────────────────────────────────────────────────────
    static void BuildScene(
        Sprite white, BossConfigSO bossConfig, SquadMemberConfigSO[] squadCfgs,
        MinionConfigSO runnerCfg, MinionConfigSO berserkerCfg, MinionConfigSO spitterCfg,
        GameObject bulletPrefab, GameObject runnerPrefab,
        GameObject berserkerPrefab, GameObject spitterPrefab) {

        // Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic    = true;
        cam.orthographicSize = 3.6f;
        camGo.transform.position = new Vector3(8f, 3f, -10f);
        cam.backgroundColor = new Color(0.10f, 0.12f, 0.10f);
        cam.nearClipPlane   = 0.3f;
        cam.farClipPlane    = 100f;
        camGo.AddComponent<CameraShaker>();

        // Background + Ground
        MkSpriteGo("Background", null, new Vector3(8f, 2.5f, 5f), white,
            new Color(0.12f, 0.16f, 0.10f), new Vector3(16f, 8f, 1f), -10);
        MkSpriteGo("Ground", null, new Vector3(8f, 0.9f, 0f), white,
            new Color(0.30f, 0.24f, 0.16f), new Vector3(16f, 0.6f, 1f), -5);

        // Boss
        var bossGo = new GameObject("Boss");
        bossGo.transform.position  = new Vector3(GameConfig.BOSS_START_X, GameConfig.BOSS_Y, 0f);
        bossGo.transform.localScale = new Vector3(2.0f, 2.5f, 1f);
        var bossSr = bossGo.AddComponent<SpriteRenderer>();
        bossSr.sprite = white;
        bossSr.color  = new Color(0.65f, 0.12f, 0.12f);
        bossSr.sortingOrder = 2;
        var bossCtrl = bossGo.AddComponent<BossController>();
        SetField(bossCtrl, "config",       bossConfig);
        SetField(bossCtrl, "bodyRenderer", bossSr);

        // GameManager
        new GameObject("GameManager").AddComponent<GameManager>();

        // AudioManager
        var amGo   = new GameObject("AudioManager");
        var am     = amGo.AddComponent<AudioManager>();
        var sfxSrc = amGo.AddComponent<AudioSource>();
        var bgmSrc = amGo.AddComponent<AudioSource>();
        sfxSrc.playOnAwake = false;
        bgmSrc.playOnAwake = false;
        bgmSrc.loop = true;
        SetField(am, "sfxSource", sfxSrc);
        SetField(am, "bgmSource", bgmSrc);

        // Terrain
        var terrainParent = new GameObject("Terrain");
        var wallSr = MkSpriteGo("Wall", terrainParent.transform,
            new Vector3(GameConfig.WALL_X, GameConfig.WALL_Y, 0f),
            white, new Color(0.60f, 0.60f, 0.60f), new Vector3(0.3f, 4.0f, 1f), 1
        ).GetComponent<SpriteRenderer>();

        var barSr = new SpriteRenderer[3];
        float[] barX = { 3.8f, 5.0f, 6.2f };
        for (int i = 0; i < 3; i++)
            barSr[i] = MkSpriteGo($"Barricade{i}", terrainParent.transform,
                new Vector3(barX[i], 1.6f, 0f), white,
                new Color(0.80f, 0.65f, 0.20f), new Vector3(0.5f, 1.0f, 1f), 1
            ).GetComponent<SpriteRenderer>();

        var rbSr = new SpriteRenderer[3];
        float[] rbX = { 8.5f, 10.0f, 11.5f };
        for (int i = 0; i < 3; i++)
            rbSr[i] = MkSpriteGo($"RoadBlock{i}", terrainParent.transform,
                new Vector3(rbX[i], 1.9f, 0f), white,
                new Color(0.50f, 0.40f, 0.30f), new Vector3(0.7f, 1.1f, 1f), 1
            ).GetComponent<SpriteRenderer>();

        var tmGo = new GameObject("TerrainManager");
        tmGo.transform.SetParent(terrainParent.transform);
        var tm = tmGo.AddComponent<TerrainManager>();
        SetField(tm, "wallRenderer", wallSr);
        SetArrayField(tm, "barricadeRenderers", ToObj(barSr));
        SetArrayField(tm, "roadBlockRenderers",  ToObj(rbSr));

        // Squad
        var squadParent = new GameObject("Squad");
        Color[] sColors = {
            new Color(0.20f, 0.50f, 0.90f),  // Alpha  – blue
            new Color(0.90f, 0.50f, 0.15f),  // Bravo  – orange
            new Color(0.20f, 0.75f, 0.30f),  // Charlie – green
            new Color(0.75f, 0.20f, 0.85f),  // Delta  – purple
            new Color(0.10f, 0.85f, 0.85f),  // Echo   – cyan
        };
        string[] names = { "Alpha", "Bravo", "Charlie", "Delta", "Echo" };
        float[]  slotX = GameConfig.SQUAD_SLOT_X;

        var smcArr = new SquadMemberController[5];
        var aimArr = new AimController[5];
        for (int i = 0; i < 5; i++) {
            var go = new GameObject(names[i]);
            go.transform.SetParent(squadParent.transform);
            go.transform.position   = new Vector3(slotX[i], GameConfig.SQUAD_Y, 0f);
            go.transform.localScale = new Vector3(0.4f, 0.6f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = white;
            sr.color        = sColors[i];
            sr.sortingOrder = 3;
            var smc = go.AddComponent<SquadMemberController>();
            smc.config = squadCfgs[i];
            SetField(smc, "bodyRenderer", sr);
            aimArr[i] = go.AddComponent<AimController>();
            smcArr[i] = smc;
        }

        // InputManager
        var imGo = new GameObject("InputManager");
        var im   = imGo.AddComponent<InputManager>();
        SetField(im, "gameCamera", cam);
        SetArrayField(im, "squadMembers",   ToObj(smcArr));
        SetArrayField(im, "aimControllers", ToObj(aimArr));
        SetField(im, "boss", bossCtrl);

        // Systems
        var sysParent = new GameObject("Systems");

        var wsGo = new GameObject("WaveSystem");
        wsGo.transform.SetParent(sysParent.transform);
        var ws = wsGo.AddComponent<WaveSystem>();
        SetField(ws, "runnerConfig",    runnerCfg);
        SetField(ws, "berserkerConfig", berserkerCfg);
        SetField(ws, "spitterConfig",   spitterCfg);
        SetField(ws, "runnerPrefab",    runnerPrefab);
        SetField(ws, "berserkerPrefab", berserkerPrefab);
        SetField(ws, "spitterPrefab",   spitterPrefab);

        var fsGo = new GameObject("FireSystem");
        fsGo.transform.SetParent(sysParent.transform);
        var fs = fsGo.AddComponent<FireSystem>();
        SetField(fs, "bulletPrefab", bulletPrefab.GetComponent<Bullet>());
        SetField(fs, "boss",         bossCtrl);

        var esGo = new GameObject("EnrageSystem");
        esGo.transform.SetParent(sysParent.transform);
        var es = esGo.AddComponent<EnrageSystem>();
        SetField(es, "boss", bossCtrl);

        var bsGo = new GameObject("BombingSystem");
        bsGo.transform.SetParent(sysParent.transform);
        var bsys = bsGo.AddComponent<BombingSystem>();
        SetField(bsys, "boss", bossCtrl);

        // UI Canvas
        BuildUI(smcArr, squadCfgs);
    }

    static void BuildUI(SquadMemberController[] smcArr, SquadMemberConfigSO[] squadCfgs) {
        var canvasGo = new GameObject("Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = canvasGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        var bossBarGo = new GameObject("BossHpBar");
        bossBarGo.transform.SetParent(canvasGo.transform, false);
        var bossBarUi = bossBarGo.AddComponent<BossHpBarUI>();

        var squadBarsArr   = new SquadHpBarUI[5];
        var ammoDisplayArr = new AmmoDisplayUI[5];
        for (int i = 0; i < 5; i++) {
            var shGo = new GameObject($"SquadHpBar{i}");
            shGo.transform.SetParent(canvasGo.transform, false);
            shGo.AddComponent<CanvasGroup>();
            var shUi = shGo.AddComponent<SquadHpBarUI>();
            SetField(shUi, "member", smcArr[i]);
            squadBarsArr[i] = shUi;

            var adGo = new GameObject($"AmmoDisplay{i}");
            adGo.transform.SetParent(canvasGo.transform, false);
            ammoDisplayArr[i] = adGo.AddComponent<AmmoDisplayUI>();
        }

        var uiMgr = canvasGo.AddComponent<UIManager>();
        SetField(uiMgr, "bossHpBar",    bossBarUi);
        SetArrayField(uiMgr, "squadHpBars",  ToObj(squadBarsArr));
        SetArrayField(uiMgr, "ammoDisplays", ToObj(ammoDisplayArr));
        SetArrayField(uiMgr, "squadConfigs", ToObj(squadCfgs));
    }

    // ─── Config Factories ─────────────────────────────────────────────────────
    static BossConfigSO MakeBossConfig() {
        var cfg = LoadOrCreate<BossConfigSO>(CONFIGS + "/BossConfig.asset");
        cfg.maxHp             = 4500;
        cfg.stopX             = GameConfig.BOSS_STOP_X;
        cfg.speed             = 0.26f;
        cfg.enragedSpeed      = 0.44f;
        cfg.attackInterval    = 2.5f;
        cfg.attackDamageWall  = 80f;
        cfg.shockwaveDamage   = 30f;
        cfg.shockwaveInterval = 6f;
        cfg.enrageHpThreshold = 0.5f;
        cfg.parts = new BossPartConfig[] {
            new BossPartConfig { id="HEAD",  hp=600,  damageMult=1.5f, offset=new Vector2( 0.00f,  0.85f), size=new Vector2(0.75f,0.55f), activeOnStart=true  },
            new BossPartConfig { id="CHEST", hp=1200, damageMult=1.0f, offset=new Vector2( 0.00f,  0.05f), size=new Vector2(1.00f,0.80f), activeOnStart=true  },
            new BossPartConfig { id="ARM_L", hp=400,  damageMult=0.8f, offset=new Vector2(-0.70f,  0.20f), size=new Vector2(0.40f,0.70f), activeOnStart=true  },
            new BossPartConfig { id="ARM_R", hp=400,  damageMult=0.8f, offset=new Vector2( 0.70f,  0.20f), size=new Vector2(0.40f,0.70f), activeOnStart=true  },
            new BossPartConfig { id="LEG_L", hp=350,  damageMult=0.7f, offset=new Vector2(-0.50f, -0.85f), size=new Vector2(0.40f,0.60f), activeOnStart=true  },
            new BossPartConfig { id="LEG_R", hp=350,  damageMult=0.7f, offset=new Vector2( 0.50f, -0.85f), size=new Vector2(0.40f,0.60f), activeOnStart=true  },
            new BossPartConfig { id="CORE",  hp=800,  damageMult=2.0f, offset=new Vector2( 0.00f,  0.05f), size=new Vector2(0.50f,0.50f), activeOnStart=false },
        };
        EditorUtility.SetDirty(cfg);
        return cfg;
    }

    static SquadMemberConfigSO[] MakeSquadConfigs() {
        string[]  ids    = { "alpha",  "bravo",   "charlie",  "delta",  "echo"    };
        string[]  labels = { "Alpha",  "Bravo",   "Charlie",  "Delta",  "Echo"    };
        int[]     hps    = {  120,      100,        90,         130,      80       };
        Color[]   colors = {
            new Color(0.20f, 0.50f, 0.90f),
            new Color(0.90f, 0.50f, 0.15f),
            new Color(0.20f, 0.75f, 0.30f),
            new Color(0.75f, 0.20f, 0.85f),
            new Color(0.10f, 0.85f, 0.85f),
        };
        WeaponConfig[] weapons = {
            new WeaponConfig { name="M249",      magazineSize=40, damage= 12f, fireRate=0.08f, reloadTime=3.5f, bulletSpeed=1500f, bulletType=BulletType.Single, pellets=1, spread= 8f                     },
            new WeaponConfig { name="KS-23",     magazineSize= 4, damage= 18f, fireRate=0.90f, reloadTime=2.5f, bulletSpeed= 800f, bulletType=BulletType.Shotgun,pellets=8, spread=20f                     },
            new WeaponConfig { name="Barrett",   magazineSize= 6, damage=120f, fireRate=2.20f, reloadTime=1.8f, bulletSpeed=2500f, bulletType=BulletType.Single, pellets=1, spread= 0f                     },
            new WeaponConfig { name="RPG",       magazineSize= 3, damage= 90f, fireRate=2.50f, reloadTime=3.0f, bulletSpeed= 800f, bulletType=BulletType.Rocket, pellets=1, spread= 5f, splashRadius=150f  },
            new WeaponConfig { name="Railgun",   magazineSize= 2, damage=280f, fireRate=4.00f, reloadTime=3.0f, bulletSpeed=3000f, bulletType=BulletType.Single, pellets=1, spread= 0f                     },
        };
        SpecialType[] specials    = { SpecialType.BurstAccuracy, SpecialType.None, SpecialType.WeakpointBonus, SpecialType.RocketSplash, SpecialType.WeakpointMark };
        float[]       specialVals = { 1.3f, 1f, 2.0f, 1.8f, 1.4f };
        string[][]    aims = {
            new[]{ "HEAD","CHEST","ARM_L","ARM_R","LEG_L","LEG_R" },
            new[]{ "HEAD","CHEST","ARM_L","ARM_R"                  },
            new[]{ "CORE","HEAD","CHEST"                           },
            new[]{ "CHEST","HEAD","CORE"                           },
            new[]{ "CORE","HEAD","ARM_L","ARM_R"                   },
        };

        var result = new SquadMemberConfigSO[5];
        for (int i = 0; i < 5; i++) {
            string path = $"{CONFIGS}/SquadConfig_{labels[i]}.asset";
            var cfg = LoadOrCreate<SquadMemberConfigSO>(path);
            cfg.id          = ids[i];
            cfg.label       = labels[i];
            cfg.hp          = hps[i];
            cfg.color       = colors[i];
            cfg.weapon      = weapons[i];
            cfg.special     = specials[i];
            cfg.specialVal  = specialVals[i];
            cfg.aimPriority = aims[i];
            EditorUtility.SetDirty(cfg);
            result[i] = cfg;
        }
        return result;
    }

    static MinionConfigSO MakeMinionConfig(string name, MinionType type,
        int hp, float speed, float damage, float attackRange, float attackInterval, float terrainDamage) {
        var cfg = LoadOrCreate<MinionConfigSO>($"{CONFIGS}/MinionConfig_{name}.asset");
        cfg.type           = type;
        cfg.hp             = hp;
        cfg.speed          = speed;
        cfg.damage         = damage;
        cfg.attackRange    = attackRange;
        cfg.attackInterval = attackInterval;
        cfg.terrainDamage  = terrainDamage;
        EditorUtility.SetDirty(cfg);
        return cfg;
    }

    // ─── Prefab Factories ─────────────────────────────────────────────────────
    static GameObject MakeBulletPrefab(Sprite white) {
        string path = PREFABS + "/Bullet.prefab";
        var tmp = new GameObject("Bullet");
        tmp.transform.localScale = new Vector3(0.10f, 0.10f, 1f);
        var sr = tmp.AddComponent<SpriteRenderer>();
        sr.sprite       = white;
        sr.color        = new Color(1f, 0.95f, 0.30f);
        sr.sortingOrder = 5;
        tmp.AddComponent<Bullet>();
        var rb = tmp.AddComponent<Rigidbody2D>();
        rb.bodyType               = RigidbodyType2D.Kinematic;
        rb.gravityScale           = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = tmp.AddComponent<CircleCollider2D>();
        col.radius    = 0.05f;
        col.isTrigger = true;
        var prefab = PrefabUtility.SaveAsPrefabAsset(tmp, path);
        UnityEngine.Object.DestroyImmediate(tmp);
        return prefab;
    }

    static GameObject MakeMinionPrefab(string name, MinionConfigSO cfg, Sprite white, Color color) {
        string path = $"{PREFABS}/{name}.prefab";
        Vector3 scale = name == "Berserker"
            ? new Vector3(0.55f, 0.60f, 1f)
            : name == "Spitter"
                ? new Vector3(0.40f, 0.50f, 1f)
                : new Vector3(0.45f, 0.55f, 1f);
        var tmp = new GameObject(name);
        tmp.transform.localScale = scale;
        var sr = tmp.AddComponent<SpriteRenderer>();
        sr.sprite       = white;
        sr.color        = color;
        sr.sortingOrder = 2;
        var mc = tmp.AddComponent<MinionController>();
        SetField(mc, "config", cfg);
        var col = tmp.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        var prefab = PrefabUtility.SaveAsPrefabAsset(tmp, path);
        UnityEngine.Object.DestroyImmediate(tmp);
        return prefab;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    static Sprite WhiteSprite() {
        const string assetPath = SPRITES + "/square.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null) return existing;

        var tex = new Texture2D(4, 4);
        var px  = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();

        // Application.dataPath = ".../Assets" → strip "Assets" suffix then append assetPath
        string absPath = Application.dataPath + "/" + assetPath.Substring("Assets/".Length);
        string dir     = Path.GetDirectoryName(absPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(absPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath);

        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti != null) {
            ti.textureType         = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = 100f;
            ti.filterMode          = FilterMode.Point;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var so = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    static GameObject MkSpriteGo(string name, Transform parent, Vector3 pos,
        Sprite sprite, Color color, Vector3 scale, int sortOrder) {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.color        = color;
        sr.sortingOrder = sortOrder;
        return go;
    }

    static void SetField(UnityEngine.Object target, string field, UnityEngine.Object value) {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) {
            Debug.LogWarning($"[SceneBuilder] Field not found: {target.GetType().Name}.{field}");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetArrayField(UnityEngine.Object target, string field, UnityEngine.Object[] values) {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) {
            Debug.LogWarning($"[SceneBuilder] Array field not found: {target.GetType().Name}.{field}");
            return;
        }
        prop.ClearArray();
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static UnityEngine.Object[] ToObj<T>(T[] arr) where T : UnityEngine.Object {
        var result = new UnityEngine.Object[arr.Length];
        for (int i = 0; i < arr.Length; i++) result[i] = arr[i];
        return result;
    }

    static void EnsureFolder(string path) {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        string folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }

    static void AddToBuildSettings(string scenePath) {
        var scenes = EditorBuildSettings.scenes;
        bool exists = Array.Exists(scenes, s => s.path == scenePath);
        if (exists) return;
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
