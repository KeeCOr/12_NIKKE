using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class GeneratedVisualApplicator {
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string ResultScenePath = "Assets/Scenes/Result.unity";
    const string UiPath = "Assets/Sprites/UI/Generated/Slices/";
    const string ButtonPath = "Assets/Sprites/UI/Generated/ButtonSlices/";
    const string ObjPath = "Assets/Sprites/Object/Generated/Slices/";

    [MenuItem("SquadVsMonster/Apply Generated Visuals")]
    public static void ApplyGeneratedVisualsMenu() {
        ApplyGeneratedVisuals();
    }

    public static void ApplyGeneratedVisualsCLI() {
        ApplyGeneratedVisuals();
    }

    static void ApplyGeneratedVisuals() {
        ImportGeneratedSprites();
        EditorSceneManager.OpenScene(ScenePath);

        ApplyHudVisuals();
        ApplyWorldVisuals();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        EditorSceneManager.OpenScene(ResultScenePath);
        ApplyResultVisuals();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        AssetDatabase.SaveAssets();
        Debug.Log("[GeneratedVisualApplicator] Applied generated UI and battlefield visuals.");
    }

    static void ImportGeneratedSprites() {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] {
            "Assets/Sprites/UI/Generated",
            "Assets/Sprites/Object/Generated"
        })) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single) {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (Mathf.Abs(importer.spritePixelsPerUnit - 100f) > 0.001f) {
                importer.spritePixelsPerUnit = 100f;
                changed = true;
            }
            if (!importer.alphaIsTransparency) {
                importer.alphaIsTransparency = true;
                changed = true;
            }
            if (changed) importer.SaveAndReimport();
        }
    }

    static void ApplyHudVisuals() {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        var root = canvas.transform;

        SetImageSprite(Find(root, "BossHpBarPanel/BossBg"), Button("hp_red_long"), new Color(1f, 1f, 1f, 0.98f));
        SetImageSprite(Find(root, "WallHpPanel/WallBg"), Button("info_blue_shield"), Color.white);
        SetImageSprite(Find(root, "WavePanel/WaveBg"), Button("info_gold_resource"), Color.white);
        SetImageSprite(Find(root, "ActionPanel/ActionBg"), Button("cmd_gold_normal"), Color.white);
        SetImageSprite(Find(root, "ActionPanel/BombBtn"), Button("round_gold"), Color.white);

        Transform bombIcon = Find(root, "ActionPanel/BombBtn/BombIcon");
        if (bombIcon != null) bombIcon.gameObject.SetActive(false);
        EnsureIcon(Find(root, "ActionPanel/BombBtn"), "GeneratedAirstrikeIcon", Ui("icon_airstrike"),
            new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), Color.white);

        string[] memberNames = { "ALPHA", "BRAVO", "CHARLIE", "DELTA", "ECHO" };
        string[] cardSprites = { "squad_card_blue", "squad_card_purple", "squad_card_green", "squad_card_red", "squad_card_gold" };
        for (int i = 0; i < memberNames.Length; i++) {
            Transform col = Find(root, $"SquadPanel/Col_{memberNames[i]}");
            if (col == null) continue;
            SetImageSprite(Find(col, "ColBg"), Button(CardFrameForIndex(i)), Color.white);
            EnsureIcon(col, "GeneratedWeaponIcon", WeaponIconForIndex(i), new Vector2(0.06f, 0.48f), new Vector2(0.30f, 0.72f), Color.white);
        }

        var partIcons = new Dictionary<string, string> {
            { "PartIcon_HEAD", "icon_target" },
            { "PartIcon_ARM_L", "icon_arm" },
            { "PartIcon_ARM_R", "icon_arm" },
            { "PartIcon_LEG_L", "icon_leg" },
            { "PartIcon_LEG_R", "icon_leg" },
            { "PartIcon_CHEST", "icon_warning" },
            { "PartIcon_CORE", "icon_core" },
        };
        foreach (var entry in partIcons) {
            Transform part = FindChildRecursive(root, entry.Key);
            if (part == null) continue;
            SetImageSprite(part, Button(PartFrameForKey(entry.Key)), new Color(1f, 1f, 1f, 0.90f));
            EnsureIcon(part, "GeneratedPartIcon", Ui(entry.Value), new Vector2(0.26f, 0.30f), new Vector2(0.74f, 0.82f), Color.white);
            Transform label = Find(part, "Label");
            if (label != null) {
                var rt = label.GetComponent<RectTransform>();
                if (rt != null) {
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(1f, 0.35f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }
        }

        BuildMiniMap(root);
        BuildStatusStack(root);
    }

    static Sprite WeaponIconForIndex(int index) {
        string[] names = { "icon_target", "icon_reload", "icon_splash", "icon_rocket", "icon_rail" };
        return Ui(names[Mathf.Clamp(index, 0, names.Length - 1)]);
    }

    static string CardFrameForIndex(int index) {
        string[] names = { "cmd_blue_normal", "cmd_purple_normal", "cmd_green_normal", "cmd_red_danger", "cmd_gold_normal" };
        return names[Mathf.Clamp(index, 0, names.Length - 1)];
    }

    static string PartFrameForKey(string key) {
        if (key.Contains("CORE")) return "part_purple_02";
        if (key.Contains("CHEST")) return "part_red_02";
        if (key.Contains("LEG")) return "part_green_02";
        if (key.Contains("ARM")) return "part_blue_02";
        return "part_gold_02";
    }

    static void BuildMiniMap(Transform canvas) {
        Transform old = Find(canvas, "GeneratedMiniMapPanel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var panel = new GameObject("GeneratedMiniMapPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas, false);
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-10f, 180f), new Vector2(154f, 154f));
        SetImageSprite(panel.transform, Ui("minimap_frame"), Color.white);

        AddMarker(panel.transform, "AllyMarker1", new Vector2(-36f, -30f), new Color(0.2f, 0.75f, 1f));
        AddMarker(panel.transform, "AllyMarker2", new Vector2(-12f, -44f), new Color(0.2f, 0.75f, 1f));
        AddMarker(panel.transform, "BossMarker", new Vector2(34f, 28f), new Color(1f, 0.18f, 0.12f));
        AddMarker(panel.transform, "EnemyMarker1", new Vector2(20f, 14f), new Color(1f, 0.18f, 0.12f));
        AddMarker(panel.transform, "EnemyMarker2", new Vector2(48f, -2f), new Color(1f, 0.18f, 0.12f));
    }

    static void BuildStatusStack(Transform canvas) {
        Transform old = Find(canvas, "GeneratedStatusStack");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var stack = new GameObject("GeneratedStatusStack", typeof(RectTransform));
        stack.transform.SetParent(canvas, false);
        SetRect(stack.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-10f, -192f), new Vector2(200f, 164f));

        AddStatusRow(stack.transform, "DefenseRow", "DEFENSE", Button("info_blue_shield"), Ui("icon_shield"), 0);
        AddStatusRow(stack.transform, "AutoAimRow", "AUTO AIM", Button("info_red_target"), Ui("icon_auto_aim"), 1);
        AddStatusRow(stack.transform, "SpeedRow", "SPEED", Button("info_gold_resource"), Ui("icon_speed"), 2);
    }

    static void ApplyResultVisuals() {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        var root = canvas.transform;

        SetImageSprite(Find(root, "CenterPanel/PanelBg"), Button("cmd_blue_glow"), new Color(1f, 1f, 1f, 0.96f));
        SetImageSprite(Find(root, "CenterPanel/RetryBtn"), Button("cmd_gold_glow"), Color.white);
        SetImageSprite(Find(root, "CenterPanel/MenuBtn"), Button("cmd_blue_normal"), Color.white);

        EnsureIcon(Find(root, "CenterPanel/RetryBtn"), "GeneratedRetryCorner", Button("corner_gold"),
            new Vector2(0.02f, 0.18f), new Vector2(0.16f, 0.82f), Color.white);
        EnsureIcon(Find(root, "CenterPanel/MenuBtn"), "GeneratedMenuCorner", Button("corner_blue"),
            new Vector2(0.02f, 0.18f), new Vector2(0.16f, 0.82f), Color.white);
    }

    static void AddStatusRow(Transform parent, string name, string label, Sprite panelSprite, Sprite icon, int index) {
        var row = new GameObject(name, typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        SetRect(row.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -index * 54f), new Vector2(0f, 48f));
        SetImageSprite(row.transform, panelSprite, Color.white);
        EnsureIcon(row.transform, "Icon", icon, new Vector2(0.06f, 0.18f), new Vector2(0.26f, 0.82f), Color.white);
        var text = new GameObject("Label", typeof(RectTransform), typeof(Text));
        text.transform.SetParent(row.transform, false);
        SetRect(text.GetComponent<RectTransform>(), new Vector2(0.30f, 0f), Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var txt = text.GetComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 16;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.color = new Color(0.82f, 0.92f, 1f);
    }

    static void AddMarker(Transform parent, string name, Vector2 pos, Color color) {
        var marker = new GameObject(name, typeof(RectTransform), typeof(Image));
        marker.transform.SetParent(parent, false);
        SetRect(marker.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            pos, new Vector2(10f, 10f));
        var img = marker.GetComponent<Image>();
        img.color = color;
    }

    static void ApplyWorldVisuals() {
        Sprite[] barricades = { Obj("barricade_tech_01"), Obj("barricade_tech_02"), Obj("barricade_tech_03") };
        for (int i = 0; i < barricades.Length; i++) {
            GameObject go = GameObject.Find($"Barricade{i}");
            SetWorldSprite(go, barricades[i], 1.85f, 0.95f);
        }

        Sprite[] roadBlocks = { Obj("barricade_concrete_01"), Obj("barricade_broken_01"), Obj("rubble_01") };
        for (int i = 0; i < roadBlocks.Length; i++) {
            GameObject go = GameObject.Find($"RoadBlock{i}");
            SetWorldSprite(go, roadBlocks[i], 1.35f, 0.75f);
        }

        var terrain = GameObject.Find("Terrain");
        if (terrain != null) {
            Transform old = terrain.transform.Find("GeneratedVisualProps");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var parent = new GameObject("GeneratedVisualProps");
            parent.transform.SetParent(terrain.transform, false);
            AddProp(parent.transform, "SupplyCrateA", Obj("crate_supply_01"), new Vector3(4.8f, 1.92f, -0.05f), 0.70f, 0.45f, 1);
            AddProp(parent.transform, "SupplyCrateB", Obj("crate_supply_02"), new Vector3(6.2f, 2.76f, -0.05f), 0.62f, 0.44f, 1);
            AddProp(parent.transform, "BluePylon", Obj("tech_pylon_blue"), new Vector3(7.9f, 3.42f, -0.05f), 0.42f, 0.92f, 1);
            AddProp(parent.transform, "RoadPlateA", Obj("floor_plate_01"), new Vector3(5.2f, 1.18f, 0.04f), 0.90f, 0.50f, 0);
            AddProp(parent.transform, "RoadPlateB", Obj("floor_plate_02"), new Vector3(9.7f, 3.70f, 0.04f), 0.95f, 0.52f, 0);
            AddProp(parent.transform, "CrackDecal", Obj("crack_decal_01"), new Vector3(8.9f, 2.60f, 0.05f), 1.25f, 0.55f, 0);
            AddProp(parent.transform, "ImpactSpark", Obj("impact_spark_01"), new Vector3(10.8f, 2.95f, -0.05f), 0.55f, 0.55f, 2);
        }

        var tm = Object.FindObjectOfType<TerrainManager>();
        if (tm != null) {
            var so = new SerializedObject(tm);
            SetSpriteArray(so, "barricadeSprites", barricades);
            SetSpriteArray(so, "roadBlockSprites", roadBlocks);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tm);
        }
    }

    static void AddProp(Transform parent, string name, Sprite sprite, Vector3 pos, float width, float height, int order) {
        if (sprite == null) return;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = order;
        FitWorldSprite(go, sprite, width, height);
    }

    static void SetWorldSprite(GameObject go, Sprite sprite, float width, float height) {
        if (go == null || sprite == null) return;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        sr.sprite = sprite;
        sr.color = Color.white;
        FitWorldSprite(go, sprite, width, height);
        EditorUtility.SetDirty(go);
    }

    static void FitWorldSprite(GameObject go, Sprite sprite, float width, float height) {
        Vector2 size = sprite.bounds.size;
        if (size.x <= 0f || size.y <= 0f) return;
        float scale = Mathf.Min(width / size.x, height / size.y);
        go.transform.localScale = new Vector3(scale, scale, 1f);
    }

    static void SetSpriteArray(SerializedObject so, string propertyName, Sprite[] sprites) {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null) return;
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    static void EnsureIcon(Transform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Color color) {
        if (parent == null || sprite == null) return;
        Transform icon = parent.Find(name);
        if (icon == null) {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            icon = go.transform;
        }
        var rt = icon.GetComponent<RectTransform>();
        SetRect(rt, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        SetImageSprite(icon, sprite, color);
    }

    static void SetImageSprite(Transform target, Sprite sprite, Color color) {
        if (target == null || sprite == null) return;
        var img = target.GetComponent<Image>();
        if (img == null) img = target.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        EditorUtility.SetDirty(img);
    }

    static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta) {
        if (rt == null) return;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    static Transform Find(Transform root, string path) {
        return root != null ? root.Find(path) : null;
    }

    static Transform FindChildRecursive(Transform root, string name) {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++) {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static Sprite Ui(string name) {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{UiPath}{name}.png");
    }

    static Sprite Button(string name) {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{ButtonPath}{name}.png");
    }

    static Sprite Obj(string name) {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{ObjPath}{name}.png");
    }
}
