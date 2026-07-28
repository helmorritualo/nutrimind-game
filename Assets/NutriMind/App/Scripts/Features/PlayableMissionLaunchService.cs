using System;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NutriMind.App.Features
{
    /// <summary>
    /// Records local progress/outbox via <see cref="MockMissionLaunchService"/>,
    /// then loads the playable gameplay scene for supported mission IDs.
    /// </summary>
    public sealed class PlayableMissionLaunchService : IMissionLaunchService
    {
        private readonly MockMissionLaunchService _localRecorder;
        private bool _isLoading;

        public PlayableMissionLaunchService(AppLifetime lifetime)
        {
            if (lifetime == null)
            {
                throw new ArgumentNullException(nameof(lifetime));
            }

            _localRecorder = new MockMissionLaunchService(lifetime);
        }

        public async Task<MissionLaunchResult> LaunchAsync(
            string missionId,
            MissionLaunchKind kind,
            CancellationToken cancellationToken = default)
        {
            if (_isLoading)
            {
                return Failure(
                    missionId,
                    kind,
                    "A mission scene is already loading.");
            }

            if (!MissionGameplaySceneCatalog.TryGet(
                    missionId,
                    out MissionGameplaySceneEntry entry))
            {
                return Failure(
                    missionId,
                    kind,
                    "This mission does not have a playable scene yet.");
            }

            MissionLaunchResult localResult =
                await _localRecorder.LaunchAsync(
                    missionId,
                    kind,
                    cancellationToken).ConfigureAwait(false);

            if (!localResult.Succeeded)
            {
                return localResult;
            }

            cancellationToken.ThrowIfCancellationRequested();

            await UnityMainThread.SwitchToMainAsync(cancellationToken);

            _isLoading = true;

            try
            {
                AsyncOperation operation =
                    SceneManager.LoadSceneAsync(
                        entry.SceneName,
                        LoadSceneMode.Single);

                if (operation == null)
                {
                    return Failure(
                        missionId,
                        kind,
                        "Unity could not start the gameplay scene load.",
                        localResult.EventUuid);
                }

                // Once Unity has started replacing the active scene,
                // allow it to complete even if the Mission Detail presenter
                // is disposed during the scene transition.
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                bool opened =
                    SceneManager.GetActiveScene().name
                    == entry.SceneName;

                if (!opened)
                {
                    return Failure(
                        missionId,
                        kind,
                        "The gameplay scene did not become active.",
                        localResult.EventUuid);
                }

                return new MissionLaunchResult
                {
                    Succeeded = true,
                    Kind = kind,
                    MissionId = entry.MissionId,
                    EventUuid = localResult.EventUuid,
                    GameplaySceneOpened = true,
                    Message = "Mission opened."
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                NutriMindLog.RuntimeWarning(
                    "Mission scene launch failed: "
                    + exception.Message);

                return new MissionLaunchResult
                {
                    Succeeded = false,
                    Kind = kind,
                    MissionId = missionId,
                    EventUuid = localResult.EventUuid,
                    GameplaySceneOpened = false,
                    Message = "The mission scene could not be opened.",
                    Error = AppError.Configuration(
                        "The mission scene could not be opened.")
                };
            }
            finally
            {
                _isLoading = false;
            }
        }

        private static MissionLaunchResult Failure(
            string missionId,
            MissionLaunchKind kind,
            string message,
            string eventUuid = null)
        {
            return new MissionLaunchResult
            {
                Succeeded = false,
                Kind = kind,
                MissionId = string.IsNullOrWhiteSpace(missionId)
                    ? missionId
                    : missionId.Trim(),
                EventUuid = eventUuid,
                GameplaySceneOpened = false,
                Message = message,
                Error = AppError.Configuration(message)
            };
        }
    }
}
