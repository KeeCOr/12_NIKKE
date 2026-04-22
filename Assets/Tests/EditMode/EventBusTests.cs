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
