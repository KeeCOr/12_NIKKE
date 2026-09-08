using NUnit.Framework;

public class RuntimeAudioDirectorTests
{
    [Test] public void ClampVolume_BelowRange_ReturnsMin() { Assert.AreEqual(0f, RuntimeAudioDirector.ClampVolume(-1f)); }
    [Test] public void ClampVolume_AboveRange_ReturnsMax() { Assert.AreEqual(1f, RuntimeAudioDirector.ClampVolume(2f)); }
    [Test] public void ClampVolume_NaN_ReturnsMin() { Assert.AreEqual(0f, RuntimeAudioDirector.ClampVolume(float.NaN)); }
    [Test] public void ClampVolume_InRange_ReturnsSameValue() { Assert.AreEqual(0.5f, RuntimeAudioDirector.ClampVolume(0.5f)); }
    [Test] public void ResolveCueName_ExactMatch_ReturnsCue() { Assert.AreEqual(RuntimeAudioDirector.CueBgmLoop, RuntimeAudioDirector.ResolveCueName(RuntimeAudioDirector.CueBgmLoop)); }
    [Test] public void ResolveCueName_CaseInsensitive_ReturnsCue() { Assert.AreEqual(RuntimeAudioDirector.CueUiClick, RuntimeAudioDirector.ResolveCueName(RuntimeAudioDirector.CueUiClick.ToUpperInvariant())); }
    [Test] public void ResolveCueName_WhitespaceTrimmed_ReturnsCue() { Assert.AreEqual(RuntimeAudioDirector.CueActionPrimary, RuntimeAudioDirector.ResolveCueName("  " + RuntimeAudioDirector.CueActionPrimary + "  ")); }
    [Test] public void ResolveCueName_Unknown_ReturnsEmpty() { Assert.AreEqual(string.Empty, RuntimeAudioDirector.ResolveCueName("not_a_real_cue")); }
    [Test] public void ResolveCueName_Null_ReturnsEmpty() { Assert.AreEqual(string.Empty, RuntimeAudioDirector.ResolveCueName(null)); }
    [Test] public void ResolveCueName_DangerWarning_CaseInsensitiveWithWhitespace() { Assert.AreEqual(RuntimeAudioDirector.CueDangerWarning, RuntimeAudioDirector.ResolveCueName("  " + RuntimeAudioDirector.CueDangerWarning.ToUpperInvariant() + "  ")); }
    [Test] public void ResolveCueName_TransitionExactMatch_ReturnsCue() { Assert.AreEqual(RuntimeAudioDirector.CueTransition, RuntimeAudioDirector.ResolveCueName(RuntimeAudioDirector.CueTransition)); }
    [Test] public void ResolveCueName_ResultSuccessAndFailure_ReturnCorrectCues() { Assert.AreEqual(RuntimeAudioDirector.CueResultSuccess, RuntimeAudioDirector.ResolveCueName(RuntimeAudioDirector.CueResultSuccess)); Assert.AreEqual(RuntimeAudioDirector.CueResultFailure, RuntimeAudioDirector.ResolveCueName(RuntimeAudioDirector.CueResultFailure)); }
}
