using System;
using System.IO;
using NutriMind.Core.Persistence;
using NutriMind.Core.Utilities;

namespace NutriMind.Tests.TestData
{
    /// <summary>
    /// Creates temporary NutriMind SQLite databases for EditMode/PlayMode tests.
    /// </summary>
    public sealed class TestDatabaseFactory : IDisposable
    {
        private readonly string _directory;
        private NutriMindDatabase _database;
        private bool _disposed;

        public TestDatabaseFactory(IAppClock clock = null, string fileName = null)
        {
            Clock = clock ?? new FixedMockClock();
            _directory = Path.Combine(
                Path.GetTempPath(),
                "NutriMindTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            DatabaseFilePath = Path.Combine(
                _directory,
                string.IsNullOrWhiteSpace(fileName) ? "nutrimind-test.db" : fileName.Trim());
        }

        public IAppClock Clock { get; }

        public string DatabaseFilePath { get; }

        public string DirectoryPath => _directory;

        public NutriMindDatabase OpenDatabase()
        {
            ThrowIfDisposed();
            _database?.Dispose();
            _database = new NutriMindDatabase(Clock, DatabaseFilePath);
            var open = _database.Open();
            if (open.IsFailure)
            {
                throw new InvalidOperationException(
                    "Failed to open test database: "
                    + (open.Error != null ? open.Error.Code + " — " + open.Error.Message : "unknown"));
            }

            return _database;
        }

        public NutriMindDatabase ReopenDatabase()
        {
            ThrowIfDisposed();
            _database?.Dispose();
            _database = null;
            return OpenDatabase();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _database?.Dispose();
            }
            catch (Exception)
            {
                // ignore dispose errors during cleanup
            }

            _database = null;
            TryDeleteFile(DatabaseFilePath);
            TryDeleteFile(DatabaseFilePath + "-shm");
            TryDeleteFile(DatabaseFilePath + "-wal");
            TryDeleteDirectory(_directory);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TestDatabaseFactory));
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
                // best-effort cleanup
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch (Exception)
            {
                // best-effort cleanup
            }
        }
    }
}
