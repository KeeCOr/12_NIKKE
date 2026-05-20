using NUnit.Framework;
using UnityEngine;

public class AimControllerTests {
    [Test]
    public void SetFreeAimTarget_KeepsAimTargetAvailableWithoutEnemyTarget() {
        var go = new GameObject("AimController_Test");
        try {
            var aim = go.AddComponent<AimController>();

            aim.SetFreeAimTarget(new Vector2(20f, 5f));

            Assert.IsTrue(aim.HasTarget);
            Assert.That(aim.AimPosition.x, Is.EqualTo(20f).Within(0.001f));
            Assert.That(aim.AimPosition.y, Is.EqualTo(5f).Within(0.001f));
            Assert.IsNull(aim.TargetPartId);
        } finally {
            Object.DestroyImmediate(go);
        }
    }
}
