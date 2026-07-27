using SQLite;

namespace NutriMind.Core.Persistence
{
    public interface IDatabaseMigration
    {
        int Version { get; }
        string Name { get; }
        void Apply(SQLiteConnection connection);
    }
}
