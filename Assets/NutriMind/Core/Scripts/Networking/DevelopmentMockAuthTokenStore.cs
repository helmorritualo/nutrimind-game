#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NutriMind.Core.Utilities;
using UnityEngine;

namespace NutriMind.Core.Networking
{
    /// <summary>
    /// Development-only Mock auth token persistence for force-close → offline relaunch testing.
    /// Never used for ProductionServer/DevelopmentServer. Not a production-secure token store.
    /// PIN is never written. Token is never stored in SQLite.
    /// </summary>
    public sealed class DevelopmentMockAuthTokenStore : IAuthTokenStore
    {
        private readonly object _gate = new object();
        private readonly string _filePath;
        private string _token;
        private bool _loaded;

        public DevelopmentMockAuthTokenStore(string filePath = null)
        {
            _filePath = string.IsNullOrWhiteSpace(filePath)
                ? GetDefaultPath()
                : filePath;
        }

        public static string GetDefaultPath()
        {
            return Path.Combine(Application.persistentDataPath, "NutriMind", "mock-development-auth.dat");
        }

        public bool HasToken
        {
            get
            {
                EnsureLoaded();
                return !string.IsNullOrEmpty(_token);
            }
        }

        public Task<string> ReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureLoaded();
            return Task.FromResult(_token);
        }

        public Task WriteAsync(string token, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                _loaded = true;
                if (string.IsNullOrWhiteSpace(token))
                {
                    _token = null;
                    TryDeleteFileUnlocked();
                }
                else
                {
                    _token = token.Trim();
                    TryWriteFileUnlocked(_token);
                }
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_gate)
            {
                _loaded = true;
                _token = null;
                TryDeleteFileUnlocked();
            }

            return Task.CompletedTask;
        }

        public static void DeletePersistedFile(string filePath = null)
        {
            string path = string.IsNullOrWhiteSpace(filePath) ? GetDefaultPath() : filePath;
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                NutriMindLog.AuthWarning(
                    "Could not delete mock development auth file: " + exception.GetType().Name);
            }
        }

        private void EnsureLoaded()
        {
            lock (_gate)
            {
                if (_loaded)
                {
                    return;
                }

                _loaded = true;
                try
                {
                    if (!File.Exists(_filePath))
                    {
                        _token = null;
                        return;
                    }

                    string raw = File.ReadAllText(_filePath);
                    _token = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
                }
                catch (Exception exception)
                {
                    NutriMindLog.AuthWarning(
                        "Could not read mock development auth file: " + exception.GetType().Name);
                    _token = null;
                }
            }
        }

        private void TryWriteFileUnlocked(string token)
        {
            try
            {
                string directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_filePath, token);
            }
            catch (Exception exception)
            {
                NutriMindLog.AuthWarning(
                    "Could not write mock development auth file: " + exception.GetType().Name);
            }
        }

        private void TryDeleteFileUnlocked()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
            }
            catch (Exception exception)
            {
                NutriMindLog.AuthWarning(
                    "Could not clear mock development auth file: " + exception.GetType().Name);
            }
        }
    }
}
#endif
