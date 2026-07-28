using System;

namespace NutriMind.App.Features
{
    public readonly struct MissionGameplaySceneEntry
    {
        public MissionGameplaySceneEntry(
            string missionId,
            string sceneName,
            string scenePath)
        {
            MissionId = missionId;
            SceneName = sceneName;
            ScenePath = scenePath;
        }

        public string MissionId { get; }
        public string SceneName { get; }
        public string ScenePath { get; }
    }

    /// <summary>
    /// Maps playable mission IDs to build-settings scene names.
    /// Only Mission 1 (<c>g5_lq_t1_m01</c>) is currently supported.
    /// </summary>
    public static class MissionGameplaySceneCatalog
    {
        public const string FestivalStorybookMissionId =
            "g5_lq_t1_m01";

        public const string FestivalStorybookSceneName =
            "SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue";

        public const string FestivalStorybookScenePath =
            "Assets/NutriMind/Missions/Grade5/LiteraQuest/"
            + "G5_LQ_T1_M01/Scenes/"
            + "SCN_G5_LQ_T1_M01_TheFestivalStorybookRescue.unity";

        public static bool TryGet(
            string missionId,
            out MissionGameplaySceneEntry entry)
        {
            string normalized = missionId?.Trim();

            if (string.Equals(
                    normalized,
                    FestivalStorybookMissionId,
                    StringComparison.Ordinal))
            {
                entry = new MissionGameplaySceneEntry(
                    FestivalStorybookMissionId,
                    FestivalStorybookSceneName,
                    FestivalStorybookScenePath);

                return true;
            }

            entry = default;
            return false;
        }
    }
}
