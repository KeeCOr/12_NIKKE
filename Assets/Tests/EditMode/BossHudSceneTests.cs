using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class BossHudSceneTests {
    [Test]
    public void GameScene_BossHudWiresAllPartHpControls() {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");

        var hud = Object.FindObjectOfType<BossHpBarUI>(true);
        Assert.NotNull(hud, "Game scene must contain BossHpBarUI.");

        var sliders = GetPrivateArray<Slider>(hud, "partHpSliders");
        var texts = GetPrivateArray<Text>(hud, "partHpTexts");

        Assert.That(sliders, Has.Length.EqualTo(7));
        Assert.That(texts, Has.Length.EqualTo(7));
        for (int i = 0; i < 7; i++) {
            Assert.NotNull(sliders[i], $"Missing boss part HP slider at index {i}.");
            Assert.NotNull(texts[i], $"Missing boss part HP text at index {i}.");
        }
    }

    private static T[] GetPrivateArray<T>(BossHpBarUI hud, string fieldName) where T : Object {
        return (T[])typeof(BossHpBarUI)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(hud);
    }
}
