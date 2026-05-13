using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SceneBuilder {
    const string SPRITES = "Assets/Sprites";
    const string CONFIGS = "Assets/Configs";
    const string PREFABS = "Assets/Prefabs";
    const string SCENES  = "Assets/Scenes";

    // ─── Entry Points ─────────────────────────────────────────────────────────
    [MenuItem("SquadVsMonster/Build All Scenes %&r")]   // Ctrl+Alt+R
    public static void BuildAll() {
        Build();  // Build() already calls BuildResult()
    }

    [MenuItem("SquadVsMonster/Build Game Scene")]
    public static void Build() {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EnsureFolder(SPRITES); EnsureFolder(CONFIGS);
        EnsureFolder(PREFABS); EnsureFolder(SCENES);

        Sprite white = WhiteSprite();

        var runnerCfg    = MakeMinionConfig("Runner",    MinionType.Runner,    60,  1.2f, 20f, 0.4f, 1.5f, 15f, 3.00f);
        var berserkerCfg = MakeMinionConfig("Berserker", MinionType.Berserker, 120, 0.8f, 35f, 0.5f, 1.2f, 20f, 3.60f);
        var spitterCfg   = MakeMinionConfig("Spitter",   MinionType.Spitter,   80,  1.0f, 25f, 4.0f, 2.5f, 10f, 2.80f);
        var bossConfig   = MakeBossConfig();
        var squadCfgs    = MakeSquadConfigs();
        var mapConfig    = MakeMapConfig();
        MakeUpgradeCards();
        AssetDatabase.SaveAssets();

        var bulletPrefab    = MakeBulletPrefab(white);
        var runnerPrefab    = MakeMinionPrefab("Runner",    runnerCfg,    white, new Color(1.00f, 0.60f, 0.10f)); // vivid orange
        var berserkerPrefab = MakeMinionPrefab("Berserker", berserkerCfg, white, new Color(0.90f, 0.10f, 0.08f)); // bright red
        var spitterPrefab   = MakeMinionPrefab("Spitter",   spitterCfg,   white, new Color(0.20f, 0.95f, 0.45f)); // vivid green

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildGameScene(white, bossConfig, squadCfgs, mapConfig,
                       runnerCfg, berserkerCfg, spitterCfg,
                       bulletPrefab, runnerPrefab, berserkerPrefab, spitterPrefab);

        string scenePath = SCENES + "/Game.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AddToBuildSettings(scenePath);

        // Always ensure Result scene is also built and registered
        BuildResult();

        AssetDatabase.Refresh();

        bool open = EditorUtility.DisplayDialog("SceneBuilder", "Game scene built!\nAssets/Scenes/Game.unity", "Open", "Close");
        if (open) EditorSceneManager.OpenScene(scenePath);
    }

    [MenuItem("SquadVsMonster/Build Result Scene")]
    public static void BuildResult() {
        EnsureFolder(SCENES);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildResultScene();
        string scenePath = SCENES + "/Result.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AddToBuildSettings(scenePath);
        AssetDatabase.Refresh();
        Debug.Log("[SceneBuilder] Result scene built: " + scenePath);
    }

    // ─── Game Scene ────────────────────────────────────────────────────────────
    static void BuildGameScene(
        Sprite white, BossConfigSO bossConfig, SquadMemberConfigSO[] squadCfgs, MapConfigSO mapConfig,
        MinionConfigSO runnerCfg, MinionConfigSO berserkerCfg, MinionConfigSO spitterCfg,
        GameObject bulletPrefab, GameObject runnerPrefab,
        GameObject berserkerPrefab, GameObject spitterPrefab) {

        // Art sprites (null-safe — falls back to white placeholder if file missing)
        Sprite bgSprite   = LoadArtSprite("Assets/Sprites/Background/stage_1.png");
        Sprite bossSprite = LoadArtSprite("Assets/Sprites/Enemy/B_1_NM.png");
        Sprite barSprite  = LoadArtSprite("Assets/Sprites/Object/Barricade_1.png");
        Sprite[] charSprites = {
            LoadArtSprite("Assets/Sprites/Character/character (1).png"),  // Alpha  – Sniper
            LoadArtSprite("Assets/Sprites/Character/character (4).png"),  // Bravo  – AR
            LoadArtSprite("Assets/Sprites/Character/character (5).png"),  // Charlie – Shotgun
            LoadArtSprite("Assets/Sprites/Character/character (2).png"),  // Delta  – Rocket
            LoadArtSprite("Assets/Sprites/Character/character (3).png"),  // Echo   – DMR
        };

        // EventSystem (required for Button clicks)
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        // Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 4.0f;
        camGo.transform.position = new Vector3(7.5f, 3.0f, -10f);
        cam.backgroundColor  = new Color(0.04f, 0.06f, 0.15f);
        cam.nearClipPlane    = 0.3f;
        cam.farClipPlane     = 100f;
        camGo.AddComponent<CameraShaker>();

        // Background
        if (bgSprite != null) {
            var bgGo = MkSprite("Background", null, new Vector3(7.5f, 2.5f, 5f),
                bgSprite, Color.white, Vector3.one, -10);
            StretchSprite(bgGo, bgSprite, 20f, 12f);  // fill screen, aspect less critical
        } else {
            MkSprite("Background", null, new Vector3(7.5f, 2.5f, 5f), white,
                new Color(0.04f, 0.06f, 0.15f), new Vector3(20f, 12f, 1f), -10);
            BuildBuildings(white);
        }

        // Ground — slightly lighter so it reads against the background
        MkSprite("Ground", null, new Vector3(7.5f, 0.5f, 0f), white,
            new Color(0.28f, 0.24f, 0.20f), new Vector3(20f, 1.5f, 1f), -5);

        // Boss — root stays at scale(1,1,1) so BossController part children
        // use their localPosition offsets as true world-unit offsets.
        // Body sprite lives in a separate child so FitSprite only scales the art.
        var bossGo = new GameObject("Boss");
        bossGo.transform.position   = new Vector3(GameConfig.BOSS_START_X, GameConfig.BOSS_Y, 0f);
        bossGo.transform.localScale = Vector3.one;

        var bossBodyGo = new GameObject("BossBody");
        bossBodyGo.transform.SetParent(bossGo.transform);
        bossBodyGo.transform.localPosition = Vector3.zero;
        var bossSr = bossBodyGo.AddComponent<SpriteRenderer>();
        bossSr.sprite       = bossSprite ?? white;
        bossSr.color        = bossSprite != null ? Color.white : new Color(0.25f, 0.25f, 0.30f);
        bossSr.sortingOrder = 5;
        if (bossSprite != null)
            FitSprite(bossBodyGo, bossSprite, 10.0f, 16.0f);
        else
            bossBodyGo.transform.localScale = new Vector3(9.0f, 5.0f, 1f);

        var bossCtrl = bossGo.AddComponent<BossController>();
        SetField(bossCtrl, "config", bossConfig);
        SetField(bossCtrl, "bodyRenderer", bossSr);

        // Managers
        new GameObject("GameManager").AddComponent<GameManager>();

        var amGo   = new GameObject("AudioManager");
        var am     = amGo.AddComponent<AudioManager>();
        var sfxSrc = amGo.AddComponent<AudioSource>();
        var bgmSrc = amGo.AddComponent<AudioSource>();
        sfxSrc.playOnAwake = false; bgmSrc.playOnAwake = false; bgmSrc.loop = true;
        SetField(am, "sfxSource", sfxSrc);
        SetField(am, "bgmSource", bgmSrc);

        // Terrain
        var terrainParent = new GameObject("Terrain");
        var wallSr = MkSprite("Wall", terrainParent.transform,
            new Vector3(GameConfig.WALL_X, GameConfig.WALL_Y, 0f),
            white, new Color(0.70f, 0.68f, 0.60f), new Vector3(1.0f, 8.0f, 1f), 2
        ).GetComponent<SpriteRenderer>();

        var barSr = new SpriteRenderer[3];
        var barSp = new Sprite[3]; // damage sprites (healthy / damaged / critical)
        float[] barX = { 3.8f, 5.0f, 6.2f };
        // Y values follow the boss diagonal path (upper-right → lower-left)
        float[] barY = { 1.64f, 2.10f, 2.55f };
        for (int i = 0; i < 3; i++) {
            // barSprite is landscape (2528×1696) — stretch to portrait so it reads as a tall barrier
            var barGo = MkSprite($"Barricade{i}", terrainParent.transform,
                new Vector3(barX[i], barY[i], 0f),
                barSprite ?? white,
                barSprite != null ? Color.white : new Color(0.80f, 0.65f, 0.20f),
                new Vector3(1.56f, 2.40f, 1f), 3);
            if (barSprite != null) StretchSprite(barGo, barSprite, 1.56f, 2.40f);
            barSr[i] = barGo.GetComponent<SpriteRenderer>();
        }
        // 3 damage sprites for barricade (tinted at runtime)
        barSp[0] = barSprite ?? white;
        barSp[1] = barSprite ?? white;
        barSp[2] = barSprite ?? white;

        var rbSr = new SpriteRenderer[3];
        float[] rbX = { 8.5f, 10.0f, 11.5f };
        float[] rbY = { 3.42f, 3.99f, 4.56f };
        for (int i = 0; i < 3; i++) {
            rbSr[i] = MkSprite($"RoadBlock{i}", terrainParent.transform,
                new Vector3(rbX[i], rbY[i], 0f), white,
                new Color(0.65f, 0.52f, 0.38f), new Vector3(1.90f, 3.60f, 1f), 1
            ).GetComponent<SpriteRenderer>();
        }

        var wallSprites     = new Sprite[] { white, white, white };
        var roadBlockSprites = new Sprite[] { white, white, white };

        var tmGo = new GameObject("TerrainManager");
        tmGo.transform.SetParent(terrainParent.transform);
        var tm = tmGo.AddComponent<TerrainManager>();
        SetField(tm, "wallRenderer",      wallSr);
        SetArrayField(tm, "barricadeRenderers", ToObj(barSr));
        SetArrayField(tm, "roadBlockRenderers",  ToObj(rbSr));
        SetArrayField(tm, "wallSprites",         ToObj(wallSprites));
        SetArrayField(tm, "barricadeSprites",    ToObj(barSp));
        SetArrayField(tm, "roadBlockSprites",    ToObj(roadBlockSprites));

        // Squad
        var squadParent = new GameObject("Squad");
        Color[] sColors = {
            new Color(0.20f, 0.50f, 0.90f),
            new Color(0.90f, 0.50f, 0.15f),
            new Color(0.20f, 0.75f, 0.30f),
            new Color(0.75f, 0.20f, 0.85f),
            new Color(0.10f, 0.85f, 0.85f),
        };
        string[] names = { "Alpha", "Bravo", "Charlie", "Delta", "Echo" };

        var smcArr = new SquadMemberController[5];
        var aimArr = new AimController[5];
        for (int i = 0; i < 5; i++) {
            var go = new GameObject(names[i]);
            go.transform.SetParent(squadParent.transform);
            go.transform.position   = new Vector3(GameConfig.SQUAD_SLOT_X[i], GameConfig.SQUAD_SLOT_Y[i], 0f);
            go.transform.localScale = new Vector3(1.04f, 1.40f, 1f);  // fallback size (2× scale)
            var sr  = go.AddComponent<SpriteRenderer>();
            var csp = charSprites[i];
            sr.sprite       = csp ?? white;
            sr.color        = csp != null ? Color.white : sColors[i];
            sr.sortingOrder = 8;
            if (csp != null) FitSprite(go, csp, 1.04f, 3.0f);   // portrait sprite → ~1.04 x 1.40 world units
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
        SetField(ws, "mapConfig",       mapConfig);

        var fsGo = new GameObject("FireSystem");
        fsGo.transform.SetParent(sysParent.transform);
        var fs = fsGo.AddComponent<FireSystem>();
        SetField(fs, "bulletPrefab", bulletPrefab.GetComponent<Bullet>());
        SetField(fs, "boss", bossCtrl);

        var esysGo = new GameObject("EnrageSystem");
        esysGo.transform.SetParent(sysParent.transform);
        SetField(esysGo.AddComponent<EnrageSystem>(), "boss", bossCtrl);

        var bsGo = new GameObject("BombingSystem");
        bsGo.transform.SetParent(sysParent.transform);
        var bsys = bsGo.AddComponent<BombingSystem>();
        SetField(bsys, "boss", bossCtrl);

        // Systems
        new GameObject("DamageNumberSystem").AddComponent<DamageNumberSystem>();
        new GameObject("VFXSystem").AddComponent<VFXSystem>();

        // Atmospheric & polish
        BuildAmbientParticles();
        BuildGroundAccentLine(white);

        BuildGameUI(smcArr, squadCfgs, bossCtrl);
    }

    static void BuildBuildings(Sprite white) {
        var parent = new GameObject("CityBuildings");
        Color dark = new Color(0.07f, 0.09f, 0.20f);
        float[][] b = {
            new float[]{ 1.0f, 3.8f, 1.8f, 5.2f },
            new float[]{ 3.5f, 4.4f, 2.2f, 6.0f },
            new float[]{ 6.5f, 3.5f, 1.5f, 4.5f },
            new float[]{ 9.5f, 4.6f, 2.8f, 6.5f },
            new float[]{ 12.0f, 3.8f, 2.0f, 5.0f },
            new float[]{ 14.0f, 3.2f, 1.6f, 4.0f },
        };
        for (int i = 0; i < b.Length; i++)
            MkSprite($"Building{i}", parent.transform,
                new Vector3(b[i][0], b[i][1], 0f), white, dark,
                new Vector3(b[i][2], b[i][3], 1f), -8);
    }

    // ─── Game UI  (modern mobile layout) ──────────────────────────────────────
    //
    //  ┌────────────────────────────────────────────────────┐
    //  │  [BOSS HP BAR + PART ICONS]          [WAVE / TIME] │  ← top
    //  │  [WALL HP]                                         │  ← below boss bar
    //  │                                                    │
    //  │               game world                           │
    //  │                                                    │
    //  │  [SQUAD PANEL ×5]          [BOMB BTN]              │  ← bottom
    //  └────────────────────────────────────────────────────┘

    static void BuildGameUI(SquadMemberController[] smcArr,
                            SquadMemberConfigSO[] squadCfgs,
                            BossController bossCtrl) {
        var canvasGo = new GameObject("Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var cs = canvasGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var bossBarUi    = BuildBossBar(canvasGo.transform);
        BuildWallHpBar(canvasGo.transform);
        BuildWaveInfo(canvasGo.transform);

        var squadBars    = new SquadHpBarUI[5];
        var ammoDisplays = new AmmoDisplayUI[5];
        BuildSquadPanel(canvasGo.transform, smcArr, squadCfgs, squadBars, ammoDisplays);

        BuildBombButton(canvasGo.transform, bossCtrl);
        BuildHintText(canvasGo.transform);

        // ── End-of-game overlay (hidden by default, shown by UIManager on game end) ──
        var overlayBgImg = MkUiImg(canvasGo.transform, "EndOverlayBg",
            new Color(0f, 0f, 0f, 0f), stretch: true);
        overlayBgImg.gameObject.SetActive(false);

        var overlayTextGo = new GameObject("EndOverlayText", typeof(RectTransform));
        overlayTextGo.transform.SetParent(canvasGo.transform, false);
        var overlayRt = overlayTextGo.GetComponent<RectTransform>();
        overlayRt.anchorMin = new Vector2(0f, 0.35f); overlayRt.anchorMax = new Vector2(1f, 0.65f);
        overlayRt.offsetMin = Vector2.zero;            overlayRt.offsetMax = Vector2.zero;
        var overlayText = overlayTextGo.AddComponent<Text>();
        overlayText.text      = "";
        overlayText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        overlayText.fontSize  = 96;
        overlayText.fontStyle = FontStyle.Bold;
        overlayText.alignment = TextAnchor.MiddleCenter;
        overlayText.color     = Color.white;
        overlayTextGo.SetActive(false);

        var uiMgr = canvasGo.AddComponent<UIManager>();
        SetField(uiMgr, "bossHpBar",      bossBarUi);
        SetField(uiMgr, "endOverlayBg",   overlayBgImg);
        SetField(uiMgr, "endOverlayText", overlayText);
        SetArrayField(uiMgr, "squadHpBars",  ToObj(squadBars));
        SetArrayField(uiMgr, "ammoDisplays", ToObj(ammoDisplays));
        SetArrayField(uiMgr, "squadConfigs", ToObj(squadCfgs));
    }

    // ── Boss HP bar ─────────────────────────────────────────────────────────
    static BossHpBarUI BuildBossBar(Transform canvasT) {
        // Top 36px = HP row, bottom 60px = part icons strip, total 130px
        var panel = MkPanel(canvasT, "BossHpBarPanel",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -5f), new Vector2(-20f, 130f));
        MkUiImg(panel.transform, "BossBg", new Color(0.04f, 0.05f, 0.10f, 0.96f), stretch: true);
        // Orange top border
        MkUiImg(panel.transform, "BorderTop", new Color(1f, 0.55f, 0.10f, 0.80f),
            new Vector2(0,1), new Vector2(1,1), new Vector2(0,-2), new Vector2(0,2));
        // Separator between HP row and parts strip
        MkUiImg(panel.transform, "Separator", new Color(0.22f, 0.25f, 0.35f, 0.70f),
            new Vector2(0,0), new Vector2(1,0), new Vector2(0,63), new Vector2(0,1));

        // "BOSS" label — HP row
        MkText(panel.transform, "BossLabel", "BOSS", 26,
            new Color(1f, 0.62f, 0.12f), TextAnchor.MiddleLeft,
            new Vector2(0,0), new Vector2(0,0), new Vector2(8f, 68f), new Vector2(76f, 36f));

        // HP slider — HP row
        var hpSlider = MkSlider(panel.transform, "BossHpSlider",
            new Color(1f, 0.50f, 0.08f), new Color(0.14f, 0.12f, 0.10f),
            new Vector2(0,0), new Vector2(1,0), new Vector2(92f, 68f), new Vector2(-102f, 36f));

        // HP text overlaid on slider (centered)
        var hpText = MkText(panel.transform, "BossHpText", "4500 / 4500",
            15, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0,0), new Vector2(1,0), new Vector2(92f, 68f), new Vector2(-102f, 36f));

        // Part icon strip — 7 color-coded tiles
        string[] pids  = { "HEAD", "ARM L", "ARM R", "LEG L", "LEG R", "CHEST", "CORE" };
        string[] pidsF = { "HEAD", "ARM_L", "ARM_R", "LEG_L", "LEG_R", "CHEST", "CORE" };
        Color[] pColors = {
            new Color(1.0f, 0.85f, 0.10f),   // HEAD  — gold
            new Color(0.30f, 0.60f, 1.0f),   // ARM_L — blue
            new Color(0.30f, 0.60f, 1.0f),   // ARM_R — blue
            new Color(0.25f, 0.90f, 0.40f),  // LEG_L — green
            new Color(0.25f, 0.90f, 0.40f),  // LEG_R — green
            new Color(1.0f, 0.30f, 0.10f),   // CHEST — red
            new Color(1.0f, 0.20f, 0.90f),   // CORE  — magenta
        };

        var partIcons = new Image[7];
        for (int i = 0; i < 7; i++) {
            var iconGo = new GameObject($"PartIcon_{pidsF[i]}");
            iconGo.transform.SetParent(panel.transform, false);
            var rt = iconGo.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(i / 7f, 0f);
            rt.anchorMax        = new Vector2((i + 1) / 7f, 0f);
            rt.pivot            = Vector2.zero;
            rt.anchoredPosition = new Vector2(2f, 4f);
            rt.sizeDelta        = new Vector2(-4f, 57f);

            partIcons[i] = iconGo.AddComponent<Image>();
            partIcons[i].color = new Color(pColors[i].r * 0.22f, pColors[i].g * 0.22f, pColors[i].b * 0.22f, 0.90f);

            // Colored top accent bar per part
            var acGo = new GameObject("Accent");
            acGo.transform.SetParent(iconGo.transform, false);
            var aRt = acGo.AddComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0,1); aRt.anchorMax = new Vector2(1,1);
            aRt.pivot = new Vector2(0.5f,1f);
            aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0,4f);
            acGo.AddComponent<Image>().color = pColors[i];

            MkText(iconGo.transform, "Label", pids[i], 11,
                new Color(Mathf.Min(1f, pColors[i].r + 0.25f), Mathf.Min(1f, pColors[i].g + 0.20f), Mathf.Min(1f, pColors[i].b + 0.20f)),
                TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        var bossBarUi = panel.AddComponent<BossHpBarUI>();
        SetField(bossBarUi, "hpSlider", hpSlider);
        SetField(bossBarUi, "hpText",   hpText);
        SetArrayField(bossBarUi, "partIcons", ToObj(partIcons));
        return bossBarUi;
    }

    // ── Wall HP bar ─────────────────────────────────────────────────────────
    static void BuildWallHpBar(Transform canvasT) {
        // Positioned directly below boss bar (boss bar ends at -135, gap 5 → -140)
        var panel = MkPanel(canvasT, "WallHpPanel",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -140f), new Vector2(290f, 42f));
        MkUiImg(panel.transform, "WallBg", new Color(0.05f, 0.07f, 0.14f, 0.92f), stretch: true);
        // Blue left accent stripe
        MkUiImg(panel.transform, "AccentLeft", new Color(0.35f, 0.62f, 1.0f, 0.90f),
            new Vector2(0,0), new Vector2(0,1), new Vector2(0,0), new Vector2(3f,0));

        MkText(panel.transform, "WallLabel", "WALL", 15,
            new Color(0.55f, 0.78f, 1.0f), TextAnchor.MiddleLeft,
            new Vector2(0,0), new Vector2(0,0), new Vector2(8f, 10f), new Vector2(54f, 22f));

        var wallSlider = MkSlider(panel.transform, "WallHpSlider",
            new Color(0.35f, 0.62f, 0.95f), new Color(0.10f, 0.12f, 0.18f),
            new Vector2(0,0), new Vector2(1,0), new Vector2(66f, 10f), new Vector2(-126f, 22f));

        var wallText = MkText(panel.transform, "WallHpText", "5000",
            14, new Color(0.80f, 0.90f, 1.0f), TextAnchor.MiddleRight,
            new Vector2(1,0), new Vector2(1,0), new Vector2(-8f, 10f), new Vector2(112f, 22f));

        var wallUi = panel.AddComponent<WallHpBarUI>();
        SetField(wallUi, "hpSlider", wallSlider);
        SetField(wallUi, "hpText",   wallText);
    }

    // ── Wave info ────────────────────────────────────────────────────────────
    static void BuildWaveInfo(Transform canvasT) {
        var panel = MkPanel(canvasT, "WavePanel",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-10f, -140f), new Vector2(200f, 42f));
        MkUiImg(panel.transform, "WaveBg", new Color(0.05f, 0.07f, 0.14f, 0.92f), stretch: true);
        // Gold right accent stripe
        MkUiImg(panel.transform, "AccentRight", new Color(1.0f, 0.85f, 0.10f, 0.90f),
            new Vector2(1,0), new Vector2(1,1), new Vector2(-3,0), new Vector2(3f,0));

        var waveText = MkText(panel.transform, "WaveText", "WAVE  1",
            20, new Color(1.0f, 0.92f, 0.30f), TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        panel.AddComponent<WaveInfoUI>();
        SetField(panel.GetComponent<WaveInfoUI>(), "waveText", waveText);
    }

    // ── Squad panel ──────────────────────────────────────────────────────────
    static void BuildSquadPanel(Transform canvasT, SquadMemberController[] smcArr,
        SquadMemberConfigSO[] squadCfgs, SquadHpBarUI[] outBars, AmmoDisplayUI[] outAmmo) {

        string[] mNames  = { "ALPHA", "BRAVO", "CHARLIE", "DELTA", "ECHO" };
        string[] wLabels = { "SNIPER", "AR", "SHOTGUN", "ROCKET", "DMR" };
        Color[]  mColors = {
            new Color(0.20f, 0.50f, 0.90f),
            new Color(0.90f, 0.50f, 0.15f),
            new Color(0.20f, 0.75f, 0.30f),
            new Color(0.75f, 0.20f, 0.85f),
            new Color(0.10f, 0.85f, 0.85f),
        };

        // Column layout (top→bottom):  Name 28px | Weapon 18px | HP bar 24px | Reload 14px | Ammo 22px
        // Margins: top 8px, between items 6px each (5×item + 4×gap + top8 + bot10 = 152)
        float colW  = 120f, gap = 5f;
        float panelW = 5f * colW + 4f * gap + 10f;
        float panelH = 160f;
        float colH   = 152f;

        var panel = MkPanel(canvasT, "SquadPanel",
            Vector2.zero, Vector2.zero, Vector2.zero,
            new Vector2(10f, 10f), new Vector2(panelW, panelH));
        MkUiImg(panel.transform, "SquadBg", new Color(0.04f, 0.05f, 0.10f, 0.93f), stretch: true);
        // Blue-purple top accent
        MkUiImg(panel.transform, "AccentTop", new Color(0.35f, 0.45f, 0.80f, 0.80f),
            new Vector2(0,1), new Vector2(1,1), new Vector2(0,0), new Vector2(0,-3));

        for (int i = 0; i < 5; i++) {
            float colX = i * (colW + gap) + 5f;

            var col = new GameObject($"Col_{mNames[i]}", typeof(RectTransform));
            col.transform.SetParent(panel.transform, false);
            SetRt(col, Vector2.zero, Vector2.zero, new Vector2(colX, 4f), new Vector2(colW, colH));

            // Tinted column bg
            MkUiImg(col.transform, "ColBg",
                new Color(mColors[i].r * 0.12f, mColors[i].g * 0.12f, mColors[i].b * 0.12f, 0.72f),
                stretch: true);

            // Left accent stripe (4px, full height)
            MkUiImg(col.transform, "Stripe",
                new Color(mColors[i].r, mColors[i].g, mColors[i].b, 0.85f),
                new Vector2(0,0), new Vector2(0,1), new Vector2(0,0), new Vector2(4f,0));

            // ── Name (top) — y = 116..144 (28px)
            MkText(col.transform, "NameText", mNames[i], 19, mColors[i], TextAnchor.MiddleCenter,
                new Vector2(0,0), new Vector2(0,0), new Vector2(0f, 116f), new Vector2(colW, 28f));

            // ── Weapon label — y = 92..110 (18px)  gold
            MkText(col.transform, "WeaponText", wLabels[i], 12,
                new Color(1.0f, 0.85f, 0.30f), TextAnchor.MiddleCenter,
                new Vector2(0,0), new Vector2(0,0), new Vector2(0f, 92f), new Vector2(colW, 18f));

            // ── HP slider — y = 62..86 (24px)
            var hpGo = new GameObject("HpSlider", typeof(RectTransform));
            hpGo.transform.SetParent(col.transform, false);
            SetRt(hpGo, Vector2.zero, Vector2.zero, new Vector2(6f, 62f), new Vector2(colW - 12f, 24f));
            var hpSlider = BuildSliderInGo(hpGo, mColors[i], new Color(0.10f, 0.10f, 0.14f));

            // ── Reload slider (hidden) — y = 44..58 (14px)
            var rlGo = new GameObject("ReloadSlider", typeof(RectTransform));
            rlGo.transform.SetParent(col.transform, false);
            SetRt(rlGo, Vector2.zero, Vector2.zero, new Vector2(6f, 44f), new Vector2(colW - 12f, 14f));
            var reloadSlider = BuildSliderInGo(rlGo, new Color(0.25f, 0.65f, 1.0f), new Color(0.10f, 0.10f, 0.14f));
            rlGo.SetActive(false);

            // ── Ammo text — y = 12..34 (22px)
            var ammoGo = new GameObject("AmmoText", typeof(RectTransform));
            ammoGo.transform.SetParent(col.transform, false);
            SetRt(ammoGo, Vector2.zero, Vector2.zero, new Vector2(0f, 12f), new Vector2(colW, 22f));
            var ammoText = ammoGo.AddComponent<Text>();
            ammoText.text = "— / —"; ammoText.fontSize = 17;
            ammoText.color = new Color(0.90f, 0.90f, 0.95f);
            ammoText.alignment = TextAnchor.MiddleCenter;
            ammoText.font = GetFont();

            col.AddComponent<CanvasGroup>();
            var shui = col.AddComponent<SquadHpBarUI>();
            SetField(shui, "hpSlider",     hpSlider);
            SetField(shui, "reloadSlider", reloadSlider);
            SetField(shui, "member",       smcArr[i]);

            var adui = col.AddComponent<AmmoDisplayUI>();
            SetField(adui, "ammoText", ammoText);

            outBars[i]  = shui;
            outAmmo[i]  = adui;
        }
    }

    // ── Bomb button ──────────────────────────────────────────────────────────
    static void BuildBombButton(Transform canvasT, BossController bossCtrl) {
        var panel = MkPanel(canvasT, "ActionPanel",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-10f, 10f), new Vector2(154f, 160f));
        MkUiImg(panel.transform, "ActionBg", new Color(0.04f, 0.05f, 0.10f, 0.93f), stretch: true);
        // Orange top accent
        MkUiImg(panel.transform, "AccentTop", new Color(1f, 0.55f, 0.10f, 0.80f),
            new Vector2(0,1), new Vector2(1,1), new Vector2(0,-3), new Vector2(0,3));

        // "AIR STRIKE" label at top
        MkText(panel.transform, "BombLabel", "AIR STRIKE", 12,
            new Color(1f, 0.72f, 0.22f), TextAnchor.MiddleCenter,
            new Vector2(0,1), new Vector2(1,1), new Vector2(0f,-6f), new Vector2(0f,24f));

        // Cooldown text at bottom
        var cdText = MkText(panel.transform, "CooldownText", "READY",
            13, new Color(1f, 0.92f, 0.55f), TextAnchor.MiddleCenter,
            new Vector2(0,0), new Vector2(1,0), new Vector2(0f, 6f), new Vector2(0f, 22f));

        // Button fills middle area (y=32..y=H-30)
        var btnGo = new GameObject("BombBtn", typeof(RectTransform));
        btnGo.transform.SetParent(panel.transform, false);
        SetRt(btnGo, new Vector2(0.06f, 0f), new Vector2(0.94f, 1f),
            new Vector2(0f, 32f), new Vector2(0f, -62f));

        var baseImg = btnGo.AddComponent<Image>();
        baseImg.color = new Color(0.50f, 0.14f, 0.04f);

        var btn = btnGo.AddComponent<Button>();
        var bColors = btn.colors;
        bColors.normalColor      = new Color(0.50f, 0.14f, 0.04f);
        bColors.highlightedColor = new Color(0.72f, 0.24f, 0.08f);
        bColors.pressedColor     = new Color(0.30f, 0.08f, 0.02f);
        bColors.disabledColor    = new Color(0.22f, 0.22f, 0.26f);
        btn.colors = bColors;
        btn.targetGraphic = baseImg;

        // Cooldown radial overlay
        var cdGo = new GameObject("CooldownOverlay", typeof(RectTransform));
        cdGo.transform.SetParent(btnGo.transform, false);
        SetRt(cdGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var cdImg = cdGo.AddComponent<Image>();
        cdImg.color      = new Color(0f, 0f, 0f, 0.70f);
        cdImg.type       = Image.Type.Filled;
        cdImg.fillMethod = Image.FillMethod.Radial360;
        cdImg.fillAmount = 0f;

        // Bomb icon
        MkText(btnGo.transform, "BombIcon", "💣", 34, Color.white, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var bombUi = panel.AddComponent<BombButtonUI>();
        SetField(bombUi, "button",        btn);
        SetField(bombUi, "cooldownFill",  cdImg);
        SetField(bombUi, "cooldownText",  cdText);
    }

    static void BuildHintText(Transform canvasT) {
        // squad panel is 160px tall + 10px bottom margin + 4px gap = 174px
        MkText(canvasT, "HintText",
            "CLICK to select  ·  DRAG to aim  ·  BOMB clears minions",
            13, new Color(0.68f, 0.74f, 0.88f, 0.60f), TextAnchor.MiddleCenter,
            new Vector2(0.16f, 0f), new Vector2(0.84f, 0f),
            new Vector2(0f, 174f), new Vector2(0f, 18f));
    }

    // ─── Result Scene ─────────────────────────────────────────────────────────
    static void BuildResultScene() {
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor  = new Color(0.04f, 0.05f, 0.10f);

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        // GameManager stub so ResultUI.Instance check works
        new GameObject("GameManager").AddComponent<GameManager>();

        var canvasGo = new GameObject("Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = canvasGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Background (tinted at runtime by ResultUI)
        var resultBgImg = MkUiImg(canvasGo.transform, "ResultBg", new Color(0.04f, 0.05f, 0.10f, 1f), stretch: true).GetComponent<Image>();

        // Center panel (background tinted at runtime by ResultUI)
        var centerPanel = MkPanel(canvasGo.transform, "CenterPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(600f, 320f));
        var panelBgImg = MkUiImg(centerPanel.transform, "PanelBg", new Color(0.08f, 0.09f, 0.16f, 0.95f), stretch: true).GetComponent<Image>();

        // Result text
        var resultText = MkText(centerPanel.transform, "ResultText", "MISSION COMPLETE",
            48, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0,1), new Vector2(1,1), new Vector2(0,-40f), new Vector2(0,60f));

        // Retry button
        var retryGo = BuildResultButton(centerPanel.transform, "RetryBtn", "RETRY",
            new Vector2(0.15f, 0f), new Vector2(0.85f, 0f), new Vector2(0f, 60f), new Vector2(0f, 50f),
            new Color(0.20f, 0.50f, 0.20f));

        // Menu button
        var menuGo = BuildResultButton(centerPanel.transform, "MenuBtn", "MENU",
            new Vector2(0.15f, 0f), new Vector2(0.85f, 0f), new Vector2(0f, 5f), new Vector2(0f, 46f),
            new Color(0.30f, 0.30f, 0.35f));

        var resultUi = canvasGo.AddComponent<ResultUI>();
        SetField(resultUi, "resultText", resultText);
        SetField(resultUi, "retryBtn",   retryGo.GetComponent<Button>());
        SetField(resultUi, "menuBtn",    menuGo.GetComponent<Button>());
        SetField(resultUi, "panelBg",    panelBgImg);
        SetField(resultUi, "resultBg",   resultBgImg);
    }

    static GameObject BuildResultButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color) {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        MkText(go.transform, "Label", label, 22, Color.white, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return go;
    }

    // ─── UI Helpers ───────────────────────────────────────────────────────────
    static Slider BuildSliderInGo(GameObject go, Color fillColor, Color bgColor) {
        var bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(go.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
        bgGo.AddComponent<Image>().color = bgColor;

        var faGo = new GameObject("FillArea");
        faGo.transform.SetParent(go.transform, false);
        var faRt = faGo.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one; faRt.sizeDelta = Vector2.zero;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(faGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.sizeDelta = Vector2.zero;
        fillGo.AddComponent<Image>().color = fillColor;

        var sl = go.AddComponent<Slider>();
        sl.fillRect   = fillRt;
        sl.direction  = Slider.Direction.LeftToRight;
        sl.minValue   = 0f; sl.maxValue = 1f; sl.value = 1f;
        sl.interactable = false;
        return sl;
    }

    static Slider MkSlider(Transform parent, string name, Color fillColor, Color bgColor,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta) {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot     = Vector2.zero;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        return BuildSliderInGo(go, fillColor, bgColor);
    }

    static Text MkText(Transform parent, string name, string content,
        int fontSize, Color color, TextAnchor align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta) {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot     = Vector2.zero;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        var txt = go.AddComponent<Text>();
        txt.text = content; txt.fontSize = fontSize;
        txt.color = color; txt.alignment = align;
        txt.font  = GetFont();
        return txt;
    }

    static Image MkUiImg(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta) {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>(); img.color = color;
        return img;
    }

    static Image MkUiImg(Transform parent, string name, Color color, bool stretch) {
        if (!stretch) return MkUiImg(parent, name, color, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return MkUiImg(parent, name, color, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    static GameObject MkPanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta) {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        return go;
    }

    static void SetRt(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta) {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot     = Vector2.zero;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
    }

    static Font GetFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    // ─── Config Factories ─────────────────────────────────────────────────────
    static BossConfigSO MakeBossConfig() {
        var cfg = LoadOrCreate<BossConfigSO>(CONFIGS + "/BossConfig.asset");
        cfg.maxHp = 4500; cfg.stopX = GameConfig.BOSS_STOP_X;
        cfg.speed = 0.26f; cfg.enragedSpeed = 0.44f;
        cfg.attackInterval = 2.5f; cfg.attackDamageWall = 80f;
        cfg.shockwaveDamage = 30f; cfg.shockwaveInterval = 6f;
        cfg.enrageHpThreshold = 0.5f;
        // Boss canvas 2752×1536 at PPU=100 → 27.52×15.36 nat; FitSprite(8,5) → s=0.2907 → 8.0×4.47 rendered
        // Creature fills ~60%W×70%H → ~4.8×3.1 world units. Parts in world-unit offsets from boss root.
        // damageMult = armor coefficient while intact  (< 1 = resists damage, > 1 = weak spot)
        // After destruction the collider stays live and damage passes through at 70% (see BossController)
        cfg.parts = new BossPartConfig[] {
            new BossPartConfig { id="HEAD",  hp=600,  damageMult=0.50f, offset=new Vector2( 1.50f,  0.80f), size=new Vector2(1.20f,0.90f), activeOnStart=true  },
            new BossPartConfig { id="CHEST", hp=1200, damageMult=0.35f, offset=new Vector2( 0.00f,  0.00f), size=new Vector2(2.20f,1.50f), activeOnStart=true  },
            new BossPartConfig { id="ARM_L", hp=400,  damageMult=0.55f, offset=new Vector2(-2.20f,  0.30f), size=new Vector2(1.00f,1.40f), activeOnStart=true  },
            new BossPartConfig { id="ARM_R", hp=400,  damageMult=0.55f, offset=new Vector2( 2.20f,  0.30f), size=new Vector2(1.00f,1.40f), activeOnStart=true  },
            new BossPartConfig { id="LEG_L", hp=350,  damageMult=0.45f, offset=new Vector2(-1.50f, -0.90f), size=new Vector2(0.90f,1.00f), activeOnStart=true  },
            new BossPartConfig { id="LEG_R", hp=350,  damageMult=0.45f, offset=new Vector2( 1.50f, -0.90f), size=new Vector2(0.90f,1.00f), activeOnStart=true  },
            new BossPartConfig { id="CORE",  hp=800,  damageMult=3.00f, offset=new Vector2( 0.00f,  0.00f), size=new Vector2(1.00f,0.90f), activeOnStart=false },
        };
        EditorUtility.SetDirty(cfg);
        return cfg;
    }

    static SquadMemberConfigSO[] MakeSquadConfigs() {
        string[]  ids    = { "alpha",  "bravo",  "charlie",  "delta",  "echo"   };
        string[]  labels = { "Alpha",  "Bravo",  "Charlie",  "Delta",  "Echo"   };
        int[]     hps    = {  90,       120,       100,        130,      80      };
        Color[]   colors = {
            new Color(0.20f, 0.50f, 0.90f),
            new Color(0.90f, 0.50f, 0.15f),
            new Color(0.20f, 0.75f, 0.30f),
            new Color(0.75f, 0.20f, 0.85f),
            new Color(0.10f, 0.85f, 0.85f),
        };
        // SNIPER / AR / SHOTGUN / ROCKET / DMR
        WeaponConfig[] weapons = {
            // Sniper — no max range; fires at any distance
            new WeaponConfig { name="Barrett", magazineSize= 6, damage=120f, fireRate=2.20f, reloadTime=1.8f, bulletSpeed=2500f, bulletType=BulletType.Single,  pellets=1, spread= 0f, maxRange=  0f },
            // AR — 5-round burst (fast shots), then 1.8 s cooldown; limited to 8 wu
            new WeaponConfig { name="M249",    magazineSize= 5, damage= 15f, fireRate=0.07f, reloadTime=1.8f, bulletSpeed=1500f, bulletType=BulletType.Single,  pellets=1, spread= 8f, maxRange=8.0f },
            // Shotgun — tight spread, 3.5 wu max; won't fire if target out of range
            new WeaponConfig { name="KS-23",   magazineSize= 4, damage= 18f, fireRate=0.90f, reloadTime=2.5f, bulletSpeed= 800f, bulletType=BulletType.Shotgun, pellets=8, spread=20f, maxRange=3.5f },
            // Rocket — splash damage, 11 wu max
            new WeaponConfig { name="RPG",     magazineSize= 3, damage= 90f, fireRate=2.50f, reloadTime=3.0f, bulletSpeed= 800f, bulletType=BulletType.Rocket,  pellets=1, spread= 5f, splashRadius=150f, maxRange=11.0f },
            // DMR — semi-auto precision, 9 wu max
            new WeaponConfig { name="Railgun", magazineSize= 2, damage=280f, fireRate=4.00f, reloadTime=3.0f, bulletSpeed=3000f, bulletType=BulletType.Single,  pellets=1, spread= 0f, maxRange=9.0f },
        };
        SpecialType[] specials    = { SpecialType.WeakpointBonus, SpecialType.BurstAccuracy, SpecialType.None, SpecialType.RocketSplash, SpecialType.WeakpointMark };
        float[]       specialVals = { 2.0f, 1.3f, 1f, 1.8f, 1.4f };
        string[][] aims = {
            new[]{ "CORE","HEAD","CHEST"                           },
            new[]{ "HEAD","CHEST","ARM_L","ARM_R","LEG_L","LEG_R" },
            new[]{ "HEAD","CHEST","ARM_L","ARM_R"                  },
            new[]{ "CHEST","HEAD","CORE"                           },
            new[]{ "CORE","HEAD","ARM_L","ARM_R"                   },
        };

        var result = new SquadMemberConfigSO[5];
        for (int i = 0; i < 5; i++) {
            var cfg = LoadOrCreate<SquadMemberConfigSO>($"{CONFIGS}/SquadConfig_{labels[i]}.asset");
            cfg.id = ids[i]; cfg.label = labels[i]; cfg.hp = hps[i];
            cfg.color = colors[i]; cfg.weapon = weapons[i];
            cfg.special = specials[i]; cfg.specialVal = specialVals[i];
            cfg.aimPriority = aims[i];
            EditorUtility.SetDirty(cfg);
            result[i] = cfg;
        }
        return result;
    }

    // ─── Upgrade Card Assets ──────────────────────────────────────────────────
    static void MakeUpgradeCards() {
        // Per-character damage boost cards
        string[]  ids      = { "alpha",  "bravo",  "charlie",  "delta",  "echo"   };
        string[]  names    = { "Alpha",  "Bravo",  "Charlie",  "Delta",  "Echo"   };
        string[]  weapons  = { "저격소총", "돌격소총", "산탄총",    "로켓",   "DMR"    };

        for (int i = 0; i < ids.Length; i++) {
            // Damage ×1.2 card
            var dmg = LoadOrCreate<UpgradeCardSO>($"{CONFIGS}/UpgradeCard_{names[i]}_Damage.asset");
            dmg.title        = $"{names[i]} 집중 훈련";
            dmg.description  = $"{names[i]}의 {weapons[i]} 공격력 +20%";
            dmg.targetType   = UpgradeTargetType.SpecificMember;
            dmg.targetId     = ids[i];
            dmg.stat         = UpgradeStat.Damage;
            dmg.value        = 0.2f;
            dmg.isMultiplier = true;
            dmg.maxStacks    = 5;
            EditorUtility.SetDirty(dmg);
        }

        // Per-character secondary cards
        (string id, string title, string desc, UpgradeStat stat, float val, bool mult)[] secondary = {
            ("alpha",   "Alpha 장전 훈련",   "Alpha 재장전 속도 +15%",   UpgradeStat.ReloadSpeed, 0.85f, true),
            ("bravo",   "Bravo 탄창 확장",   "Bravo 탄창 +5발",          UpgradeStat.MagazineSize, 5f,   false),
            ("charlie", "Charlie 체력 증강", "Charlie 최대 HP +20",       UpgradeStat.Hp,          20f,  false),
            ("delta",   "Delta 재장전",       "Delta 재장전 속도 +10%",   UpgradeStat.ReloadSpeed, 0.90f, true),
            ("echo",    "Echo 저격 리듬",     "Echo 발사 속도 +15%",      UpgradeStat.FireRate,    0.85f, true),
        };
        foreach (var (id, title, desc, stat, val, mult) in secondary) {
            string capId = char.ToUpper(id[0]) + id.Substring(1);
            var card = LoadOrCreate<UpgradeCardSO>($"{CONFIGS}/UpgradeCard_{capId}_Secondary.asset");
            card.title        = title;
            card.description  = desc;
            card.targetType   = UpgradeTargetType.SpecificMember;
            card.targetId     = id;
            card.stat         = stat;
            card.value        = val;
            card.isMultiplier = mult;
            card.maxStacks    = 3;
            EditorUtility.SetDirty(card);
        }

        // Global cards
        (string title, string desc, UpgradeStat stat, float val, bool mult)[] globals = {
            ("폭격 보급",   "공중 폭격 충전 +1",    UpgradeStat.BombCharge,  1f,  false),
            ("바리케이드 증설", "바리케이드 +1",    UpgradeStat.Barricade,   1f,  false),
            ("분대 강화",   "전원 최대 HP +15",     UpgradeStat.Hp,          15f, false),
        };
        foreach (var (title, desc, stat, val, mult) in globals) {
            string key = title.Replace(" ", "_");
            var card = LoadOrCreate<UpgradeCardSO>($"{CONFIGS}/UpgradeCard_Global_{key}.asset");
            card.title        = title;
            card.description  = desc;
            card.targetType   = UpgradeTargetType.Global;
            card.targetId     = "";
            card.stat         = stat;
            card.value        = val;
            card.isMultiplier = mult;
            card.maxStacks    = 3;
            EditorUtility.SetDirty(card);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SceneBuilder] Upgrade cards created in " + CONFIGS);
    }

    static MapConfigSO MakeMapConfig() {
        var cfg = LoadOrCreate<MapConfigSO>(CONFIGS + "/MapConfig.asset");
        cfg.destX = GameConfig.DEFENSE_LINE;
        cfg.destY = GameConfig.SQUAD_SLOT_Y[0];   // minions target the frontmost slot
        // 3 spawn paths along the diagonal (upper-right → lower-left)
        // Only reset if zones are empty or unset — preserves manual edits
        if (cfg.spawnZones == null || cfg.spawnZones.Length == 0) {
            cfg.spawnZones = new SpawnZone[] {
                new SpawnZone { xMin=12.5f, xMax=13.5f, yMin=4.50f, yMax=5.50f, weight=1f }, // 상단 경로
                new SpawnZone { xMin=11.5f, xMax=12.5f, yMin=3.60f, yMax=4.40f, weight=1f }, // 중단 경로
                new SpawnZone { xMin=12.0f, xMax=13.0f, yMin=5.00f, yMax=5.80f, weight=1f }, // 최상단 경로
            };
        }
        EditorUtility.SetDirty(cfg);
        return cfg;
    }

    static MinionConfigSO MakeMinionConfig(string name, MinionType type,
        int hp, float speed, float damage, float attackRange, float attackInterval, float terrainDamage,
        float spriteHalfHeight = 0f) {
        var cfg = LoadOrCreate<MinionConfigSO>($"{CONFIGS}/MinionConfig_{name}.asset");
        cfg.type = type; cfg.hp = hp; cfg.speed = speed; cfg.damage = damage;
        cfg.attackRange = attackRange; cfg.attackInterval = attackInterval;
        cfg.terrainDamage = terrainDamage; cfg.spriteHalfHeight = spriteHalfHeight;
        EditorUtility.SetDirty(cfg);
        return cfg;
    }

    // ─── Prefab Factories ─────────────────────────────────────────────────────
    static GameObject MakeBulletPrefab(Sprite white) {
        string path   = PREFABS + "/Bullet.prefab";
        Sprite circle = CircleSprite();
        var tmp = new GameObject("Bullet");
        tmp.transform.localScale = new Vector3(0.28f, 0.12f, 1f);
        var sr = tmp.AddComponent<SpriteRenderer>();
        sr.sprite = circle ?? white; sr.color = new Color(1f, 0.95f, 0.35f); sr.sortingOrder = 10;
        tmp.AddComponent<Bullet>();
        var rb = tmp.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = tmp.AddComponent<CircleCollider2D>();
        col.radius = 0.06f; col.isTrigger = true;

        // Tracer trail
        var trail = tmp.AddComponent<TrailRenderer>();
        trail.time              = 0.07f;
        trail.startWidth        = 0.12f;
        trail.endWidth          = 0.0f;
        trail.minVertexDistance = 0.008f;
        trail.autodestruct      = false;
        trail.sortingOrder      = 9;
        trail.startColor        = new Color(1f, 0.95f, 0.50f, 1.0f);
        trail.endColor          = new Color(1f, 0.70f, 0.20f, 0.0f);
        var trailMat = new Material(Shader.Find("Sprites/Default"));
        trail.material = trailMat;

        var prefab = PrefabUtility.SaveAsPrefabAsset(tmp, path);
        UnityEngine.Object.DestroyImmediate(tmp);
        return prefab;
    }

    static GameObject MakeMinionPrefab(string name, MinionConfigSO cfg, Sprite white, Color fallbackColor) {
        string path = $"{PREFABS}/{name}.prefab";

        // Art sprite per minion type (null → procedural color fallback)
        Sprite art = name == "Runner"    ? LoadArtSprite("Assets/Sprites/Enemy/M_1_Runner.png")
                   : name == "Berserker" ? LoadArtSprite("Assets/Sprites/Enemy/M_2_Leader.png")
                   : name == "Spitter"   ? LoadArtSprite("Assets/Sprites/Enemy/M_3_Shooter.png")
                   : null;

        // Max visual extents — 4× scale: Runner 2.00×6.00, Berserker 2.48×7.20, Spitter 1.80×5.60
        float maxW = name == "Berserker" ? 2.48f : name == "Spitter" ? 1.80f : 2.00f;
        float maxH = name == "Berserker" ? 7.20f : name == "Spitter" ? 5.60f : 6.00f;

        var tmp = new GameObject(name);

        // Shadow layer — slightly larger version behind the body
        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(tmp.transform);
        shadow.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        var osr = shadow.AddComponent<SpriteRenderer>();
        osr.sortingOrder = 5;

        // Body renderer on root so HitFlash and MinionController find it via GetComponent
        var sr = tmp.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 7;

        if (art != null) {
            sr.sprite = art;
            sr.color  = Color.white;
            FitSprite(tmp, art, maxW, maxH);

            // Shadow: same art, dark, slightly enlarged for silhouette depth
            var nat = art.bounds.size;
            osr.sprite = art;
            osr.color  = new Color(0f, 0f, 0f, 0.35f);
            shadow.transform.localScale = new Vector3(1.06f, 1.04f, 1f);

            // Collider in local space = natural sprite size → world size = rendered size
            var col = tmp.AddComponent<BoxCollider2D>();
            col.size = nat;
        } else {
            // Procedural fallback: solid-color block with black outline
            Vector3 fbScale = name == "Berserker" ? new Vector3(2.48f, 3.32f, 1f)
                            : name == "Spitter"   ? new Vector3(1.80f, 2.40f, 1f)
                            :                       new Vector3(2.00f, 2.68f, 1f);
            tmp.transform.localScale = fbScale;
            sr.sprite = white; sr.color = fallbackColor;

            osr.sprite = white;
            osr.color  = new Color(0f, 0f, 0f, 0.85f);
            shadow.transform.localScale = new Vector3(1.18f, 1.12f, 1f);

            var col = tmp.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }

        var mc = tmp.AddComponent<MinionController>();
        SetField(mc, "config", cfg);
        tmp.AddComponent<HitFlash>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(tmp, path);
        UnityEngine.Object.DestroyImmediate(tmp);
        return prefab;
    }

    // ─── World Sprite Helper ──────────────────────────────────────────────────
    static GameObject MkSprite(string name, Transform parent, Vector3 pos,
        Sprite sprite, Color color, Vector3 scale, int sortOrder) {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = color; sr.sortingOrder = sortOrder;
        return go;
    }

    // ─── Reflection Helpers ───────────────────────────────────────────────────
    static void SetField(UnityEngine.Object target, string field, UnityEngine.Object value) {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[SceneBuilder] Field not found: {target.GetType().Name}.{field}"); return; }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetArrayField(UnityEngine.Object target, string field, UnityEngine.Object[] values) {
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[SceneBuilder] Array field not found: {target.GetType().Name}.{field}"); return; }
        prop.ClearArray();
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static UnityEngine.Object[] ToObj<T>(T[] arr) where T : UnityEngine.Object {
        var r = new UnityEngine.Object[arr.Length];
        for (int i = 0; i < arr.Length; i++) r[i] = arr[i];
        return r;
    }

    // ─── Asset Helpers ────────────────────────────────────────────────────────

    // Load a PNG/art sprite, configuring it as Sprite type (PPU=100, bilinear) if needed.
    // Returns null if the asset does not exist at assetPath.
    static Sprite LoadArtSprite(string assetPath) {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null) return null;
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti != null && (ti.textureType != TextureImporterType.Sprite
                           || ti.spriteImportMode != SpriteImportMode.Single)) {
            ti.textureType         = TextureImporterType.Sprite;
            ti.spriteImportMode    = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100f;
            ti.filterMode          = FilterMode.Bilinear;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled       = false;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    // Scale uniformly so the sprite fits INSIDE (maxW × maxH) while maintaining aspect ratio.
    static void FitSprite(GameObject go, Sprite sprite, float maxW, float maxH) {
        if (sprite == null) return;
        var nat = sprite.bounds.size;
        if (nat.x <= 0f || nat.y <= 0f) return;
        float s = Mathf.Min(maxW / nat.x, maxH / nat.y);
        go.transform.localScale = new Vector3(s, s, 1f);
    }

    // Stretch sprite to fill exactly (worldW × worldH) — ignores aspect ratio.
    // Use only for backgrounds/terrain where filling the area is more important.
    static void StretchSprite(GameObject go, Sprite sprite, float worldW, float worldH) {
        if (sprite == null) return;
        var nat = sprite.bounds.size;
        if (nat.x <= 0f || nat.y <= 0f) return;
        go.transform.localScale = new Vector3(worldW / nat.x, worldH / nat.y, 1f);
    }

    static Sprite WhiteSprite() {
        const string assetPath = SPRITES + "/square.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null) return existing;

        var tex = new Texture2D(4, 4);
        var px  = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();

        string absPath = Application.dataPath + "/" + assetPath.Substring("Assets/".Length);
        string dir     = Path.GetDirectoryName(absPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(absPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath);

        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti != null) {
            ti.textureType         = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = 4f;   // matches SharedSprite PPU so all objects render at their world-unit scale
            ti.filterMode          = FilterMode.Point;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject {
        var e = AssetDatabase.LoadAssetAtPath<T>(path);
        if (e != null) return e;
        var so = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    static void EnsureFolder(string path) {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        string folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }

    static void AddToBuildSettings(string scenePath) {
        var scenes = EditorBuildSettings.scenes;
        if (Array.Exists(scenes, s => s.path == scenePath)) return;
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // 32×32 anti-aliased circle sprite — used for bullets so they render as round dots
    static Sprite CircleSprite() {
        const string assetPath = SPRITES + "/circle.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null) return existing;

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = (size - 1) * 0.5f, cy = (size - 1) * 0.5f, r = size * 0.5f;
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float dx = x - cx, dy = y - cy;
                float dist  = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((r - dist) / 1.5f);  // 1.5px soft edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        string absPath = Application.dataPath + "/" + assetPath.Substring("Assets/".Length);
        string dir = Path.GetDirectoryName(absPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(absPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath);

        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti != null) {
            ti.textureType         = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = 32f;
            ti.filterMode          = FilterMode.Bilinear;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled       = false;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    // Slow-rising dust motes floating across the background — adds atmosphere without distraction
    static void BuildAmbientParticles() {
        var go = new GameObject("AmbientParticles");
        go.transform.position = new Vector3(7.5f, 3.5f, 1f);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.02f, 0.09f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.90f, 1.0f, 0.10f),
            new Color(0.95f, 0.92f, 0.80f, 0.22f));
        main.maxParticles    = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop            = true;

        var emission = ps.emission;
        emission.rateOverTime = 5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(16f, 6f, 0.1f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.04f,  0.04f);
        vel.y = new ParticleSystem.MinMaxCurve( 0.03f,  0.12f);
        vel.z = new ParticleSystem.MinMaxCurve( 0.00f,  0.00f);  // must match x/y TwoConstants mode

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.22f, 0.15f), new GradientAlphaKey(0.18f, 0.80f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        var curve = new AnimationCurve(
            new Keyframe(0f, 0.2f), new Keyframe(0.15f, 1f),
            new Keyframe(0.85f, 0.9f), new Keyframe(1f, 0.1f));
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sortingOrder = -3;
        psr.material = new Material(Shader.Find("Sprites/Default"));
    }

    // Thin horizontal line drawn at the ground/battlefield boundary for visual grounding
    static void BuildGroundAccentLine(Sprite white) {
        MkSprite("GroundAccentLine", null, new Vector3(7.5f, 1.25f, -0.1f),
            white, new Color(0.80f, 0.72f, 0.55f, 0.55f),
            new Vector3(20f, 0.04f, 1f), -2);
    }
}
