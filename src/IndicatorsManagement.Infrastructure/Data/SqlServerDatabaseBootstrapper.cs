using Microsoft.Data.SqlClient;

namespace IndicatorsManagement.Infrastructure.Data;

/// <summary>
/// Creates a SQL Server database if it does not exist yet.
/// </summary>
/// <remarks>
/// The application database is created by EF Core (<c>Database.MigrateAsync</c>), but the
/// Hangfire database has no equivalent: <c>Hangfire.SqlServer</c> creates its <em>schema</em>
/// inside an existing database and fails with SQL error 4060 ("Cannot open database ...")
/// when the database itself is absent. Without this bootstrap the API cannot start against
/// a fresh SQL Server instance.
/// </remarks>
public static class SqlServerDatabaseBootstrapper
{
    /// <summary>
    /// Ensures the database named in <paramref name="connectionString"/> exists, creating it
    /// through the <c>master</c> database when necessary.
    /// </summary>
    /// <param name="connectionString">Connection string whose Initial Catalog names the target database.</param>
    /// <exception cref="InvalidOperationException">
    /// The database is unreachable and could not be created — usually a wrong host, wrong
    /// credentials, or a login without permission to create databases.
    /// </exception>
    public static void EnsureDatabaseExists(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var target = new SqlConnectionStringBuilder(connectionString);
        var databaseName = target.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
            return;

        var master = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };

        try
        {
            using var connection = new SqlConnection(master.ConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            // QUOTENAME escapes the identifier. The name is bound as a parameter of the outer
            // batch and is never concatenated into executed text unescaped.
            command.CommandText = """
                IF DB_ID(@databaseName) IS NULL
                BEGIN
                    DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@databaseName);
                    EXEC sp_executesql @sql;
                END
                """;
            command.Parameters.AddWithValue("@databaseName", databaseName);
            command.ExecuteNonQuery();
        }
        catch (SqlException ex)
        {
            // A restricted production login may have no access to master at all. If the target
            // database is already reachable there is nothing to do; otherwise this is fatal.
            if (CanConnect(connectionString))
                return;

            throw new InvalidOperationException(
                $"Database '{databaseName}' does not exist and could not be created on server " +
                $"'{target.DataSource}'. Verify the server is running and that the login has " +
                $"permission to create databases, or create it manually: " +
                $"CREATE DATABASE [{databaseName}]. See Docs/09-development-setup.md.", ex);
        }

        // SqlClient caches login failures per connection string for a blocking period, so any
        // earlier failed attempt would be replayed from the pool even though the database now
        // exists. Clearing the pool guarantees the first real consumer opens a fresh connection.
        using var pooled = new SqlConnection(connectionString);
        SqlConnection.ClearPool(pooled);
    }

    private static bool CanConnect(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }
}
