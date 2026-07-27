using System;
using System.Collections.Generic;
using System.Linq;
using NutriMind.Core.Utilities;
using SQLite;

namespace NutriMind.Core.Persistence
{
    public sealed class DatabaseMigrator
    {
        private readonly IAppClock _clock;
        private readonly IReadOnlyList<IDatabaseMigration> _migrations;

        public DatabaseMigrator(IAppClock clock, IEnumerable<IDatabaseMigration> migrations = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _migrations = (migrations ?? CreateDefaultMigrations())
                .OrderBy(m => m.Version)
                .ToList();
        }

        public int CurrentVersion { get; private set; }

        public void Migrate(SQLiteConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            EnsureMigrationsTable(connection);
            CurrentVersion = ReadCurrentVersion(connection);

            foreach (IDatabaseMigration migration in _migrations)
            {
                if (migration.Version <= CurrentVersion)
                {
                    continue;
                }

                NutriMindLog.Sqlite(
                    "Applying migration v" + migration.Version + " (" + migration.Name + ").");

                try
                {
                    connection.RunInTransaction(() =>
                    {
                        migration.Apply(connection);
                        connection.Execute(
                            "INSERT INTO schema_migrations (version, name, applied_utc) VALUES (?, ?, ?)",
                            migration.Version,
                            migration.Name,
                            FormatUtc(_clock.UtcNow));
                    });

                    CurrentVersion = migration.Version;
                    NutriMindLog.Sqlite("Migration v" + migration.Version + " applied.");
                }
                catch (Exception exception)
                {
                    NutriMindLog.SqliteError(
                        "Migration v" + migration.Version + " failed; database was not deleted. "
                        + exception.GetType().Name);
                    throw;
                }
            }
        }

        private static void EnsureMigrationsTable(SQLiteConnection connection)
        {
            connection.Execute(@"
CREATE TABLE IF NOT EXISTS schema_migrations (
    version INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    applied_utc TEXT NOT NULL
);");
        }

        private static int ReadCurrentVersion(SQLiteConnection connection)
        {
            return connection.ExecuteScalar<int>(
                "SELECT COALESCE(MAX(version), 0) FROM schema_migrations");
        }

        private static IEnumerable<IDatabaseMigration> CreateDefaultMigrations()
        {
            yield return new Migration001InitialSchema();
            yield return new Migration002IdempotentIdentity();
        }

        private static string FormatUtc(DateTimeOffset utc)
        {
            return utc.ToUniversalTime().ToString("o");
        }
    }
}
