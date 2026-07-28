using NUnit.Framework;
using NutriMind.Gameplay.Runtime;
using UnityEditor;
using UnityEngine;

namespace NutriMind.Tests.EditMode.GameplayRuntime
{
    [TestFixture]
    public sealed class MissionContentEvidenceTests
    {
        private const string MissionJsonPath =
            "Assets/NutriMind/Missions/Grade5/LiteraQuest/G5_LQ_T1_M01/Data/g5_lq_t1_m01.json";

        [Test]
        public void MissionJson_LoadsDistinctArea2EvidenceAndQuestions()
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(MissionJsonPath);
            Assert.That(json, Is.Not.Null, "Mission JSON TextAsset missing at " + MissionJsonPath);

            Assert.That(
                MissionContentData.TryLoad(json, out MissionContentData content, out string error),
                Is.True,
                error);

            Assert.That(content.Raw.content_version, Is.EqualTo("1.2.0"));

            Assert.That(
                content.TryGetEvidenceClue(
                    MissionContentIds.ClueOpeningIllustration,
                    out string area1Title,
                    out string area1Body),
                Is.True);
            Assert.That(
                content.TryGetEvidenceClue(
                    MissionContentIds.ClueChildrenGather,
                    out string area2Title,
                    out string area2Body),
                Is.True);

            Assert.That(area2Title, Is.EqualTo("First Banner Marker"));
            Assert.That(area2Body, Does.Contain("FIRST"));
            Assert.That(area2Body, Is.Not.EqualTo(area1Body));
            Assert.That(area2Title, Is.Not.EqualTo(area1Title));

            Assert.That(
                content.Area2.Area.opening_dialogue[0].text,
                Does.Contain("Banner Market Lane"));
            Assert.That(
                content.Area1.Area.opening_dialogue[0].speaker,
                Is.EqualTo("Farmer Lira"));
            Assert.That(
                content.Area2.Area.opening_dialogue[0].speaker,
                Is.EqualTo("Mina"));

            Assert.That(
                content.Area1.Area.questions[0].prompt,
                Does.Contain("Who asks the Pathfinder"));
            Assert.That(
                content.Area2.Area.questions[0].prompt,
                Does.Contain("FIRST event"));
            Assert.That(
                content.Area2.Area.questions[0].prompt,
                Is.Not.EqualTo(content.Area1.Area.questions[0].prompt));
        }
    }
}
