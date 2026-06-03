using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskManager.Infrastructure.DbContext;

namespace TaskManager.Extensions
{
    public static class DatabaseMigrationExtensions
    {
        public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskManagerDbContext>();

            await BaselineLegacySqliteDatabaseAsync(db);
            await db.Database.MigrateAsync();
        }

        private static async Task BaselineLegacySqliteDatabaseAsync(TaskManagerDbContext db)
        {
            if (!db.Database.IsSqlite())
            {
                return;
            }

            var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pendingMigrations.Count == 0)
            {
                return;
            }

            var allMigrations = db.Database.GetMigrations().ToList();
            if (pendingMigrations.Count != allMigrations.Count)
            {
                return;
            }

            var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
            if (appliedMigrations.Count > 0)
            {
                return;
            }

            await using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();

            if (!await TableExistsAsync(connection, "Tasks"))
            {
                return;
            }

            var productVersion = typeof(Migration).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                .Split('+')[0]
                ?? typeof(Migration).Assembly.GetName().Version?.ToString()
                ?? "8.0.14";

            await db.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL);");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({pendingMigrations[0]}, {productVersion});");
        }

        private static async Task<bool> TableExistsAsync(DbConnection connection, string tableName)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
    }
}