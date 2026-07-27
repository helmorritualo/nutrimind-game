using System;
using System.Collections.Generic;
using System.Linq;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;
using SQLite;

namespace NutriMind.Core.Persistence
{
    public sealed class SqliteInstallationRepository : IInstallationRepository
    {
        private readonly NutriMindDatabase _database;
        private readonly IIdGenerator _idGenerator;
        private readonly IAppClock _clock;

        public SqliteInstallationRepository(
            NutriMindDatabase database,
            IIdGenerator idGenerator,
            IAppClock clock)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public AppResult<string> GetOrCreateDeviceId()
        {
            try
            {
                string deviceId = _database.ExecuteWithConnection(connection =>
                {
                    InstallationStateRecord existing = connection.Find<InstallationStateRecord>(
                        InstallationStateRecord.SingletonKey);
                    if (existing != null && !string.IsNullOrWhiteSpace(existing.DeviceId))
                    {
                        return existing.DeviceId;
                    }

                    string created = _idGenerator.NewUuid();
                    connection.InsertOrReplace(new InstallationStateRecord
                    {
                        SingletonKeyValue = InstallationStateRecord.SingletonKey,
                        DeviceId = created,
                        CreatedUtc = FormatUtc(_clock.UtcNow)
                    });
                    NutriMindLog.Sqlite("Created installation device_id.");
                    return created;
                });

                return AppResult<string>.Success(deviceId);
            }
            catch (Exception exception)
            {
                NutriMindLog.SqliteError("GetOrCreateDeviceId failed: " + exception.GetType().Name);
                return AppResult<string>.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<InstallationStateRecord> GetInstallationState()
        {
            try
            {
                InstallationStateRecord record = _database.ExecuteWithConnection(connection =>
                    connection.Find<InstallationStateRecord>(InstallationStateRecord.SingletonKey));
                return AppResult<InstallationStateRecord>.Success(record);
            }
            catch (Exception exception)
            {
                return AppResult<InstallationStateRecord>.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<string> RegenerateDeviceIdForFullInstallReset()
        {
            try
            {
                string deviceId = _database.ExecuteWithConnection(connection =>
                {
                    string created = _idGenerator.NewUuid();
                    connection.InsertOrReplace(new InstallationStateRecord
                    {
                        SingletonKeyValue = InstallationStateRecord.SingletonKey,
                        DeviceId = created,
                        CreatedUtc = FormatUtc(_clock.UtcNow)
                    });
                    NutriMindLog.Sqlite("Regenerated installation device_id after full-install reset.");
                    return created;
                });

                return AppResult<string>.Success(deviceId);
            }
            catch (Exception exception)
            {
                return AppResult<string>.Failure(AppError.FromException(exception));
            }
        }

        private static string FormatUtc(DateTimeOffset utc) => utc.ToUniversalTime().ToString("o");
    }

    public sealed class SqliteLocalSessionRepository : ILocalSessionRepository
    {
        private readonly NutriMindDatabase _database;

        public SqliteLocalSessionRepository(NutriMindDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public AppResult UpsertSession(LocalSessionRecord session)
        {
            if (session == null || string.IsNullOrWhiteSpace(session.StudentId))
            {
                return AppResult.Failure(AppErrorCodes.ValidationFailed, "Session student_id is required.");
            }

            try
            {
                _database.ExecuteWithConnection(connection =>
                {
                    connection.Execute("DELETE FROM local_session;");
                    connection.Insert(session);
                });
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                NutriMindLog.SqliteError("UpsertSession failed: " + exception.GetType().Name);
                return AppResult.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<LocalSessionRecord> GetSession()
        {
            try
            {
                LocalSessionRecord session = _database.ExecuteWithConnection(connection =>
                    connection.Table<LocalSessionRecord>().FirstOrDefault());
                return AppResult<LocalSessionRecord>.Success(session);
            }
            catch (Exception exception)
            {
                return AppResult<LocalSessionRecord>.Failure(AppError.FromException(exception));
            }
        }

        public AppResult ClearSession()
        {
            try
            {
                _database.ExecuteWithConnection(connection =>
                    connection.Execute("DELETE FROM local_session;"));
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }
    }

    public sealed class SqliteResourceCacheRepository : IResourceCacheRepository
    {
        private readonly NutriMindDatabase _database;

        public SqliteResourceCacheRepository(NutriMindDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public AppResult Upsert(ResourceCacheRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.CacheKey))
            {
                return AppResult.Failure(AppErrorCodes.ValidationFailed, "cache_key is required.");
            }

            if (string.Equals(record.CacheKey, "leaderboard", StringComparison.OrdinalIgnoreCase)
                || record.CacheKey.StartsWith("leaderboard", StringComparison.OrdinalIgnoreCase))
            {
                return AppResult.Failure(AppErrorCodes.ValidationFailed, "Leaderboard must not be cached.");
            }

            try
            {
                _database.ExecuteWithConnection(connection => connection.InsertOrReplace(record));
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }

        public AppResult<ResourceCacheRecord> Get(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return AppResult<ResourceCacheRecord>.Failure(
                    AppErrorCodes.ValidationFailed,
                    "cache_key is required.");
            }

            try
            {
                ResourceCacheRecord record = _database.ExecuteWithConnection(connection =>
                    connection.Find<ResourceCacheRecord>(cacheKey));
                return AppResult<ResourceCacheRecord>.Success(record);
            }
            catch (Exception exception)
            {
                return AppResult<ResourceCacheRecord>.Failure(AppError.FromException(exception));
            }
        }

        public AppResult Delete(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return AppResult.Failure(AppErrorCodes.ValidationFailed, "cache_key is required.");
            }

            try
            {
                _database.ExecuteWithConnection(connection =>
                    connection.Execute("DELETE FROM resource_cache WHERE cache_key = ?", cacheKey));
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }

        public AppResult ClearAll()
        {
            try
            {
                _database.ExecuteWithConnection(connection =>
                    connection.Execute("DELETE FROM resource_cache;"));
                return AppResult.Success();
            }
            catch (Exception exception)
            {
                return AppResult.Failure(AppError.FromException(exception));
            }
        }
    }
}
