using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ResourceRepairUtility {
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    [MenuItem("SquadVsMonster/Repair Image Links")]
    public static void RepairImageLinks() {
        RepairSceneSprites();
        RepairMinionPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ResourceRepair] Image links repaired.");
    }

    public static void RepairImageLinksCLI() {
        RepairImageLinks();
    }

    private static void RepairSceneSprites() {
        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        SetSceneSprite("Background", "Assets/Sprites/Background/stage_1.png");
        SetSceneSprite("BossBody", "Assets/Sprites/Enemy/B_1_NM.png");
        SetSceneSprite("Barricade0", "Assets/Sprites/Object/Barricade_1.png");
        SetSceneSprite("Barricade1", "Assets/Sprites/Object/Barricade_1.png");
        SetSceneSprite("Barricade2", "Assets/Sprites/Object/Barricade_1.png");
        SetSceneSprite("Alpha", "Assets/Sprites/Character/character (1).png");
        SetSceneSprite("Bravo", "Assets/Sprites/Character/character (4).png");
        SetSceneSprite("Charlie", "Assets/Sprites/Character/character (5).png");
        SetSceneSprite("Delta", "Assets/Sprites/Character/character (2).png");
        SetSceneSprite("Echo", "Assets/Sprites/Character/character (3).png");

        var square = LoadSprite("Assets/Sprites/square.png");
        SetSceneSprite("Wall", square);
        SetSceneSprite("Ground", square);
        SetSceneSprite("GroundAccentLine", square);
        SetSceneSprite("RoadBlock0", square);
        SetSceneSprite("RoadBlock1", square);
        SetSceneSprite("RoadBlock2", square);

        var terrain = Object.FindObjectOfType<TerrainManager>();
        if (terrain != null) {
            var so = new SerializedObject(terrain);
            SetSpriteArray(so, "wallSprites", square, square, square);
            var barricade = LoadSprite("Assets/Sprites/Object/Barricade_1.png");
            SetSpriteArray(so, "barricadeSprites", barricade, barricade, barricade);
            SetSpriteArray(so, "roadBlockSprites", square, square, square);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(terrain);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RepairMinionPrefabs() {
        SetPrefabSprite("Assets/Prefabs/Runner.prefab", "Assets/Sprites/Enemy/M_1_Runner.png");
        SetPrefabSprite("Assets/Prefabs/Berserker.prefab", "Assets/Sprites/Enemy/M_2_Leader.png");
        SetPrefabSprite("Assets/Prefabs/Spitter.prefab", "Assets/Sprites/Enemy/M_3_Shooter.png");
        SetPrefabSprite("Assets/Prefabs/Bullet.prefab", "Assets/Sprites/circle.png");
    }

    private static void SetSceneSprite(string objectName, string spritePath) {
        SetSceneSprite(objectName, LoadSprite(spritePath));
    }

    private static void SetSceneSprite(string objectName, Sprite sprite) {
        var go = GameObject.Find(objectName);
        if (go == null || sprite == null) return;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        sr.sprite = sprite;
        EditorUtility.SetDirty(sr);
    }

    private static void SetPrefabSprite(string prefabPath, string spritePath) {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var sprite = LoadSprite(spritePath);
        if (prefab == null || sprite == null) return;

        foreach (var sr in prefab.GetComponentsInChildren<SpriteRenderer>(true)) {
            sr.sprite = sprite;
            EditorUtility.SetDirty(sr);
        }
        PrefabUtility.SavePrefabAsset(prefab);
    }

    private static Sprite LoadSprite(string assetPath) {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite
                                || importer.spriteImportMode != SpriteImportMode.Single)) {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void SetSpriteArray(SerializedObject so, string fieldName, params Sprite[] sprites) {
        var prop = so.FindProperty(fieldName);
        if (prop == null) return;
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
