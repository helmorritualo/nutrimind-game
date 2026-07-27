using System;
using System.Collections.Generic;
using System.Threading;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;
using UnityEngine;

namespace NutriMind.Core.Networking
{
    /// <summary>
    /// Loads TextAsset fixtures from Resources/NutriMindMock.
    /// Prefers an in-memory preload so mock latency can complete off the main thread
    /// without calling Resources.Load from a background thread.
    /// </summary>
    public sealed class ResourcesMockFixtureSource : IMockFixtureSource
    {
        private static readonly string[] DefaultFixtureNames =
        {
            MockFixtureNames.LoginSuccess,
            MockFixtureNames.Config,
            MockFixtureNames.Bootstrap,
            MockFixtureNames.Profile,
            MockFixtureNames.Settings,
            MockFixtureNames.Subjects,
            MockFixtureNames.Terms,
            MockFixtureNames.Missions,
            MockFixtureNames.MissionDetail,
            MockFixtureNames.ProgressSummary,
            MockFixtureNames.Quizzes,
            MockFixtureNames.QuizDetail,
            MockFixtureNames.QuizResult,
            MockFixtureNames.QuizHistory,
            MockFixtureNames.Rewards,
            MockFixtureNames.Certificates,
            MockFixtureNames.Announcements,
            MockFixtureNames.Leaderboard,
            MockFixtureNames.SyncStatus
        };

        private readonly Dictionary<string, string> _textByFixtureName =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly object _gate = new object();
        private AppError _preloadError;

        public ResourcesMockFixtureSource(bool preloadAll = true)
        {
            if (preloadAll)
            {
                PreloadAll();
            }
        }

        public AppError PreloadError => _preloadError;

        public void PreloadAll()
        {
            if (!UnityMainThread.IsMainThread && SynchronizationContext.Current == null)
            {
                // Still attempt Resources.Load when constructed during Awake/Compose on main thread.
                // If truly off-main with no sync context, record a clear error.
            }

            for (int i = 0; i < DefaultFixtureNames.Length; i++)
            {
                AppResult<string> loaded = LoadTextFromResources(DefaultFixtureNames[i], cache: true);
                if (loaded.IsFailure && _preloadError == null)
                {
                    _preloadError = loaded.Error;
                }
            }
        }

        public AppResult<string> LoadText(string fixtureName)
        {
            string key = NormalizeKey(fixtureName);
            lock (_gate)
            {
                if (_textByFixtureName.TryGetValue(key, out string cached))
                {
                    return AppResult<string>.Success(cached);
                }
            }

            // Cache miss: only safe on the Unity main thread.
            if (!CanCallResourcesLoad())
            {
                string path = MockFixtureNames.ToResourcePath(fixtureName);
                NutriMindLog.MockGatewayError(
                    "Resources.Load attempted off the main thread for '" + path + "'.");
                return AppResult<string>.Failure(
                    AppErrorCodes.ClientInternalError,
                    "Mock fixture load must run on the Unity main thread ('" + path + "').");
            }

            return LoadTextFromResources(fixtureName, cache: true);
        }

        public AppResult<T> LoadJson<T>(string fixtureName) where T : class
        {
            AppResult<string> textResult = LoadText(fixtureName);
            if (textResult.IsFailure)
            {
                return AppResult<T>.Failure(textResult.Error);
            }

            string path = MockFixtureNames.ToResourcePath(fixtureName);
            try
            {
                T parsed = JsonUtility.FromJson<T>(textResult.Value);
                if (parsed == null)
                {
                    NutriMindLog.MockGatewayError("Fixture parse returned null at '" + path + "'.");
                    return AppResult<T>.Failure(
                        AppErrorCodes.FixtureLoadFailed,
                        "Mock fixture parse returned null at Resources path '" + path + "'.");
                }

                return AppResult<T>.Success(parsed);
            }
            catch (Exception exception)
            {
                NutriMindLog.MockGatewayError(
                    "Fixture parse failed at '" + path + "': " + exception.GetType().Name);
                return AppResult<T>.Failure(
                    AppErrorCodes.FixtureLoadFailed,
                    "Mock fixture parse failed at Resources path '" + path + "'.");
            }
        }

        private AppResult<string> LoadTextFromResources(string fixtureName, bool cache)
        {
            string path = MockFixtureNames.ToResourcePath(fixtureName);
            TextAsset asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                NutriMindLog.MockGatewayError("Fixture missing at Resources path '" + path + "'.");
                return AppResult<string>.Failure(
                    AppErrorCodes.FixtureLoadFailed,
                    "Mock fixture not found at Resources path '" + path + "'.");
            }

            if (string.IsNullOrWhiteSpace(asset.text))
            {
                NutriMindLog.MockGatewayError("Fixture empty at Resources path '" + path + "'.");
                return AppResult<string>.Failure(
                    AppErrorCodes.FixtureLoadFailed,
                    "Mock fixture is empty at Resources path '" + path + "'.");
            }

            string text = asset.text;
            if (cache)
            {
                lock (_gate)
                {
                    _textByFixtureName[NormalizeKey(fixtureName)] = text;
                }
            }

            return AppResult<string>.Success(text);
        }

        private static string NormalizeKey(string fixtureName)
        {
            return string.IsNullOrWhiteSpace(fixtureName)
                ? string.Empty
                : fixtureName.Trim().Replace('\\', '/');
        }

        private static bool CanCallResourcesLoad()
        {
            // Prefer the captured Unity main thread id when available.
            if (UnityMainThread.IsMainThread)
            {
                return true;
            }

            // During first Awake/Compose before capture, allow Resources.Load on whatever
            // thread currently owns a synchronization context (normally the main thread).
            return SynchronizationContext.Current != null;
        }
    }
}
