using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TerrainTargetingTests {
    [Test]
    public void GetClosestAliveBarricade_ReturnsNearestAliveBarricade() {
        var terrainGo = new GameObject("TerrainManager_Test");
        var barricadeA = MakeRenderer("BarricadeA", new Vector2(0f, 0f));
        var barricadeB = MakeRenderer("BarricadeB", new Vector2(5f, 0f));
        var barricadeC = MakeRenderer("BarricadeC", new Vector2(10f, 0f));

        try {
            var terrain = terrainGo.AddComponent<TerrainManager>();
            SetPrivateField(terrain, "barricadeRenderers", new[] { barricadeA, barricadeB, barricadeC });

            var target = terrain.GetClosestAliveBarricade(new Vector2(7f, 0f));

            Assert.IsTrue(target.HasValue);
            Assert.That(target.Value.x, Is.EqualTo(5f).Within(0.001f));
            Assert.That(target.Value.y, Is.EqualTo(0f).Within(0.001f));
        } finally {
            Object.DestroyImmediate(terrainGo);
            Object.DestroyImmediate(barricadeA.gameObject);
            Object.DestroyImmediate(barricadeB.gameObject);
            Object.DestroyImmediate(barricadeC.gameObject);
        }
    }

    private static SpriteRenderer MakeRenderer(string name, Vector2 pos) {
        var go = new GameObject(name);
        go.transform.position = pos;
        return go.AddComponent<SpriteRenderer>();
    }

    private static void SetPrivateField(object target, string fieldName, object value) {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing field {fieldName}");
        field.SetValue(target, value);
    }
}
