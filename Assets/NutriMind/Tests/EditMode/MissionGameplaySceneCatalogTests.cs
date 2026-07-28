using NutriMind.App.Features;
using NUnit.Framework;

namespace NutriMind.Tests.EditMode
{
    public sealed class MissionGameplaySceneCatalogTests
    {
        [Test]
        public void TryGet_FestivalStorybookMission_ResolvesSceneName()
        {
            bool found = MissionGameplaySceneCatalog.TryGet(
                MissionGameplaySceneCatalog.FestivalStorybookMissionId,
                out MissionGameplaySceneEntry entry);

            Assert.That(found, Is.True);
            Assert.That(entry.MissionId, Is.EqualTo("g5_lq_t1_m01"));
            Assert.That(
                entry.SceneName,
                Is.EqualTo("SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue"));
            Assert.That(
                entry.ScenePath,
                Is.EqualTo(MissionGameplaySceneCatalog.FestivalStorybookScenePath));
        }

        [Test]
        public void TryGet_UnknownMissionIds_DoNotResolve()
        {
            Assert.That(
                MissionGameplaySceneCatalog.TryGet("g5_lq_t1_m02", out _),
                Is.False);
            Assert.That(
                MissionGameplaySceneCatalog.TryGet("g5_lq_t1_m03", out _),
                Is.False);
            Assert.That(
                MissionGameplaySceneCatalog.TryGet(null, out _),
                Is.False);
            Assert.That(
                MissionGameplaySceneCatalog.TryGet("  ", out _),
                Is.False);
        }
    }
}
