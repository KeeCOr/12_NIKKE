using NUnit.Framework;

public class WavePhaseTests {
    [Test]
    public void GetPhaseParams_At5Seconds_Returns10sInterval() {
        var phase = WaveSystem.GetPhaseParams(5f, false);
        Assert.AreEqual(10f, phase.interval);
        Assert.AreEqual(2, phase.countMin);
        Assert.AreEqual(2, phase.countMax);
    }

    [Test]
    public void GetPhaseParams_Enraged_Returns2200msInterval() {
        var phase = WaveSystem.GetPhaseParams(30f, true);
        Assert.AreEqual(2.2f, phase.interval);
        Assert.GreaterOrEqual(phase.countMax, 6);
    }

    [Test]
    public void GetPhaseParams_At10Seconds_EntersSecondPhase() {
        var phase = WaveSystem.GetPhaseParams(10f, false);
        Assert.AreEqual(7f, phase.interval);
    }

    [Test]
    public void GetPhaseParams_At30Seconds_EntersThirdPhase() {
        var phase = WaveSystem.GetPhaseParams(30f, false);
        Assert.AreEqual(5.5f, phase.interval);
        Assert.AreEqual(2, phase.countMin);
    }

    [Test]
    public void GetPhaseParams_At65Seconds_EntersFinalPhase() {
        var phase = WaveSystem.GetPhaseParams(65f, false);
        Assert.AreEqual(3f, phase.interval);
        Assert.AreEqual(6, phase.countMin);
    }

    [Test]
    public void GetPhaseParams_EnragedOverridesTime() {
        // Even at t=5 (first phase), enraged=true should return enraged params
        var phase = WaveSystem.GetPhaseParams(5f, true);
        Assert.AreEqual(2.2f, phase.interval);
        Assert.AreEqual(6, phase.countMin);
    }
}
