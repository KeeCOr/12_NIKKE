using NUnit.Framework;
using UnityEngine;

public class EventBusTests {
    [SetUp]    public void Setup()    => GameEvents.ClearAllListeners();
    [TearDown] public void TearDown() => GameEvents.ClearAllListeners();

    [Test]
    public void BossHpChanged_FiresWithCorrectValues() {
        float receivedHp = -1f, receivedMax = -1f;
        GameEvents.OnBossHpChanged += (hp, max) => { receivedHp = hp; receivedMax = max; };
        GameEvents.RaiseBossHpChanged(2000f, 4500f);
        Assert.AreEqual(2000f, receivedHp);
        Assert.AreEqual(4500f, receivedMax);
    }


    [Test]
    public void BossPartHpChanged_FiresSnapshots() {
        BossPartHudState[] received = null;
        var snapshots = new[] {
            new BossPartHudState("HEAD", 75f, 100f, true, false),
            new BossPartHudState("CORE", 0f, 100f, false, false)
        };

        GameEvents.OnBossPartHpChanged += states => received = states;
        GameEvents.RaiseBossPartHpChanged(snapshots);

        Assert.NotNull(received);
        Assert.AreEqual(2, received.Length);
        Assert.AreEqual("HEAD", received[0].partId);
        Assert.That(received[0].HpRatio, Is.EqualTo(0.75f).Within(0.001f));
        Assert.IsFalse(received[1].isActive);
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

