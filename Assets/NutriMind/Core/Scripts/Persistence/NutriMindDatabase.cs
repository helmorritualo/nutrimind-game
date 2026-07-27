using System;
using System.IO;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;
using SQLite;
using UnityEngine;

namespace NutriMind.Core.Persistence
{
    /// <summary>
    /// Owns the gameplay SQLite connection under persistentDataPath/NutriMind/nutrimind.db.
    /// Accepts a custom path for tests. Never auto-deletes the database on migration failure.
    /// </summary>
    public sealed class NutriMindDatabase : IDisposable
    {
        private readonly string _databaseFilePath;
        private readonly IAppClock _clock;
        private readonly object _gate = new object();
        private SQLiteConnection _connection;
        private DatabaseMigrator _migrator;
        private bool _isOpen;
        private bool _disposed;

        public NutriMindDatabase(IAppClock clock = null, string databaseFilePath = null)
        {
            _clock = clock ?? new SystemAppClock();
            _databaseFilePath = string.IsNullOrWhiteSpace(databaseFilePath)
                ? GetDefaultDatabasePath()
                : databaseFilePath;
        }

        public string DatabaseFilePath => _databaseFilePath;

        public bool IsOpen
        {
            get
            {
                lock (_gate)
                {
                    return _isOpen && _connection != null;
                }
            }
        }

        public int SchemaVersion
        {
            get
            {
                lock (_gate)
                {
                    EnsureOpenUnlocked();
                    return _migrator.CurrentVersion;
                }
            }
        }

        public static string GetDefaultDatabasePath()
        {
            return Path.Combine(Application.persistentDataPath, "NutriMind", "nutrimind.db");
        }

        public AppResult Open()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return AppResult.Failure(
                        AppErrorCodes.ClientInternalError,
                        "Database has been disposed.");
                }

                try
                {
                    EnsureOpenUnlocked();
                    return AppResult.Success();
                }
                catch (Exception exception)
                {
                    NutriMindLog.SqliteError(
                        "Open failed: " + exception.GetType().Name + " — " + exception.Message);
                    return AppResult.Failure(AppError.FromException(exception));
                }
            }
        }

        public AppResult RunInTransaction(Action<SQLiteConnection> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (_gate)
            {
                try
                {
                    EnsureOpenUnlocked();
                    _connection.RunInTransaction(() => action(_connection));
                    return AppResult.Success();
                }
                catch (Exception exception)
                {
                    NutriMindLog.SqliteError("Transaction failed: " + exception.GetType().Name);
                    return AppResult.Failure(AppError.FromException(exception));
                }
            }
        }

        public AppResult<T> RunInTransaction<T>(Func<SQLiteConnection, T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (_gate)
            {
                try
                {
                    EnsureOpenUnlocked();
                    T result = default;
                    _connection.RunInTransaction(() => { result = action(_connection); });
                    return AppResult<T>.Success(result);
                }
                catch (Exception exception)
                {
                    NutriMindLog.SqliteError("Transaction failed: " + exception.GetType().Name);
                    return AppResult<T>.Failure(AppError.FromException(exception));
                }
            }
        }

        internal void ExecuteWithConnection(Action<SQLiteConnection> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (_gate)
            {
                EnsureOpenUnlocked();
                action(_connection);
            }
        }

        internal T ExecuteWithConnection<T>(Func<SQLiteConnection, T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (_gate)
            {
                EnsureOpenUnlocked();
                return action(_connection);
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                if (_connection != null)
                {
                    _connection.Dispose();
                    _connection = null;
                }

                _isOpen = false;
            }
        }

        private void EnsureOpenUnlocked()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NutriMindDatabase));
            }

            if (_isOpen && _connection != null)
            {
                return;
            }

            string directory = Path.GetDirectoryName(_databaseFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            NutriMindLog.Sqlite("Opening database at configured path.");
            _connection = new SQLiteConnection(
                _databaseFilePath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

            ApplyPragmas(_connection);
            _migrator = new DatabaseMigrator(_clock);
            _migrator.Migrate(_connection);
            _isOpen = true;
            NutriMindLog.Sqlite("Database ready at schema v" + _migrator.CurrentVersion + ".");
        }

        private static void ApplyPragmas(SQLiteConnection connection)
        {
            connection.Execute("PRAGMA foreign_keys = ON;");
            int foreignKeys = connection.ExecuteScalar<int>("PRAGMA foreign_keys;");
            if (foreignKeys != 1)
            {
                throw new InvalidOperationException("PRAGMA foreign_keys did not enable.");
            }

            string journalMode;
            try
            {
                connection.EnableWriteAheadLogging();
                journalMode = connection.ExecuteScalar<string>("PRAGMA journal_mode;");
            }
            catch (SQLiteException exception)
            {
                NutriMindLog.SqliteWarning(
                    "journal_mode=WAL request failed (" + exception.Message + "); reading current mode.");
                journalMode = connection.ExecuteScalar<string>("PRAGMA journal_mode;");
            }

            if (string.IsNullOrWhiteSpace(journalMode))
            {
                throw new InvalidOperationException("PRAGMA journal_mode returned empty.");
            }

            string normalizedMode = journalMode.Trim().ToUpperInvariant();
            if (normalizedMode == "WAL")
            {
                NutriMindLog.Sqlite("journal_mode=WAL enabled.");
            }
            else
            {
                NutriMindLog.SqliteWarning(
                    "journal_mode=WAL unavailable; using " + journalMode.Trim() + ".");
            }

            connection.Execute("PRAGMA synchronous = NORMAL;");
            connection.BusyTimeout = TimeSpan.FromMilliseconds(5000);
        }
    }
}
