#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Networking;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;
using UnityEngine;

namespace NutriMind.App.Composition
{
    /// <summary>
    /// Runtime mock developer controls for Editor and Development APK builds.
    /// Never ships in production player builds.
    /// </summary>
    public sealed class DevelopmentMockRuntimeController : MonoBehaviour
    {
        private DevelopmentMockMenu _menu;

        public static DevelopmentMockRuntimeController EnsureOn(GameObject host)
        {
            if (host == null)
            {
                return null;
            }

            DevelopmentMockRuntimeController existing =
                host.GetComponent<DevelopmentMockRuntimeController>();
            if (existing != null)
            {
                return existing;
            }

            return host.AddComponent<DevelopmentMockRuntimeController>();
        }

        private void Awake()
        {
            if (_menu == null)
            {
                _menu = gameObject.GetComponent<DevelopmentMockMenu>();
                if (_menu == null)
                {
                    _menu = gameObject.AddComponent<DevelopmentMockMenu>();
                }
            }
        }

        public MockApiScenario Scenario
        {
            get => AppLifetime.HasInstance && AppLifetime.Instance.MockRuntimeState != null
                ? AppLifetime.Instance.MockRuntimeState.Scenario
                : MockApiScenario.HappyPath;
            set
            {
                if (!AppLifetime.HasInstance || AppLifetime.Instance.MockRuntimeState == null)
                {
                    return;
                }

                AppLifetime.Instance.MockRuntimeState.SetScenario(value);
                NutriMindLog.Runtime("Mock scenario set to " + value + ".");
            }
        }

        public bool IsOnline
        {
            get => AppLifetime.HasInstance
                   && AppLifetime.Instance.Connectivity != null
                   && AppLifetime.Instance.Connectivity.IsOnline;
            set
            {
                if (!AppLifetime.HasInstance || AppLifetime.Instance.MockRuntimeState == null)
                {
                    if (AppLifetime.HasInstance && AppLifetime.Instance.Connectivity != null)
                    {
                        AppLifetime.Instance.Connectivity.SetState(
                            value ? ConnectivityState.Online : ConnectivityState.Offline);
                    }

                    return;
                }

                AppLifetime.Instance.MockRuntimeState.SetConnectivity(
                    value ? ConnectivityState.Online : ConnectivityState.Offline);
                NutriMindLog.Runtime(value ? "Connectivity set Online." : "Connectivity set Offline.");
            }
        }

        public string DatabasePath =>
            AppLifetime.HasInstance && AppLifetime.Instance.Database != null
                ? AppLifetime.Instance.Database.DatabaseFilePath
                : NutriMindDatabase.GetDefaultDatabasePath();

        public int GetOutboxCount()
        {
            if (!AppLifetime.HasInstance || AppLifetime.Instance.OutboxRepository == null)
            {
                return -1;
            }

            AppResult<int> count = AppLifetime.Instance.OutboxRepository.CountByStates(
                OutboxEventState.Pending,
                OutboxEventState.Sending,
                OutboxEventState.Deferred);
            return count.IsSuccess ? count.Value : -1;
        }

        public string[] GetKnownCacheKeys()
        {
            return new[]
            {
                ResourceCacheKeys.Bootstrap,
                ResourceCacheKeys.Profile,
                ResourceCacheKeys.Subjects,
                ResourceCacheKeys.ProgressSummary,
                ResourceCacheKeys.Rewards,
                ResourceCacheKeys.Certificates,
                ResourceCacheKeys.Announcements
            };
        }

        public Task<AppResult> ResetMockServerAsync(CancellationToken cancellationToken = default)
        {
            if (!AppLifetime.HasInstance)
            {
                return Task.FromResult(AppResult.Failure(
                    AppErrorCodes.ClientInternalError,
                    "AppLifetime is not available."));
            }

            return AppLifetime.Instance.ResetMockServerAsync(cancellationToken);
        }

        public Task<AppResult> ResetLocalDatabaseAsync(CancellationToken cancellationToken = default)
        {
            if (!AppLifetime.HasInstance)
            {
                return Task.FromResult(AppResult.Failure(
                    AppErrorCodes.ClientInternalError,
                    "AppLifetime is not available."));
            }

            return AppLifetime.Instance.ResetLocalDatabaseAsync(cancellationToken);
        }

        public Task<AppResult> FullInstallationResetAsync(CancellationToken cancellationToken = default)
        {
            if (!AppLifetime.HasInstance)
            {
                return Task.FromResult(AppResult.Failure(
                    AppErrorCodes.ClientInternalError,
                    "AppLifetime is not available."));
            }

            return AppLifetime.Instance.FullInstallationResetAsync(cancellationToken);
        }

        public void ToggleMenu()
        {
            if (_menu == null)
            {
                _menu = GetComponent<DevelopmentMockMenu>();
            }

            _menu?.ToggleVisible();
        }
    }
}
#endif
