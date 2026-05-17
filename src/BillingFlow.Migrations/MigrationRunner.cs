// File: src/BillingFlow.Migrations/MigrationRunner.cs
using System;
using System.Data;

using FluentMigrator.Runner;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Migrations;

/// <summary>
/// Orchestrates the database creation and schema migration process.
/// </summary>
public class MigrationRunner(
    IConfiguration configuration,
    IMigrationRunner runner,
    ILogger<MigrationRunner> logger,
    IHostEnvironment environment)
{
    public void Run()
    {
        // Execute database provisioning only in Development environment
        if (environment.IsDevelopment())
        {
            EnsureDatabaseExists();
        }
        else
        {
            logger.LogInformation("Environment is {EnvironmentName}. Skipping database provisioning. Assuming database exists.", environment.EnvironmentName);
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing.");

        logger.LogInformation("Acquiring distributed lock for migrations...");

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        // Start a transaction to hold the application lock
        using var transaction = connection.BeginTransaction();

        if (AcquireLock(connection, transaction))
        {
            try
            {
                logger.LogInformation("Starting FluentMigrator execution...");

                // This will apply all pending migrations found in the assembly
                runner.MigrateUp();

                logger.LogInformation("Database schema is up to date.");

                // Commit ONLY on success. This will also automatically release the app lock.
                transaction.Commit();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during migration. Rolling back transaction.");
                // Rollback on failure. This will also automatically release the app lock.
                transaction.Rollback();
                throw; // Rethrow to let the host exit with an error code
            }
        }
        else
        {
            // If we can't acquire the lock, another instance is likely holding it for too long.
            throw new Exception("Could not acquire SQL application lock for migrations. Another migrator instance might be stuck.");
        }
    }

    private bool AcquireLock(SqlConnection connection, SqlTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "sp_getapplock";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("Resource", "BillingFlow_Migration_Lock");
        command.Parameters.AddWithValue("LockMode", "Exclusive");
        command.Parameters.AddWithValue("LockOwner", "Transaction");
        command.Parameters.AddWithValue("LockTimeout", 30000); // Wait up to 30 seconds for other migrators to finish

        var returnParameter = command.Parameters.Add("RetVal", SqlDbType.Int);
        returnParameter.Direction = ParameterDirection.ReturnValue;

        command.ExecuteNonQuery();

        var result = (int)returnParameter.Value;
        // 0 or 1 means success. < 0 means timeout or error.
        return result >= 0;
    }

    private void EnsureDatabaseExists()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing from configuration.");

        // Parse the connection string to safely extract the database name
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("Database name (InitialCatalog) is missing from the connection string.");
        }

        // Temporarily change connection to the 'master' database to execute server-level commands
        builder.InitialCatalog = "master";
        var masterConnectionString = builder.ConnectionString;

        logger.LogInformation("Checking if database '{DatabaseName}' exists...", databaseName);

        using var connection = new SqlConnection(masterConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT db_id('{databaseName}')";
        var result = command.ExecuteScalar();

        // If DB_ID returns null, the database does not exist
        if (result == DBNull.Value || result == null)
        {
            logger.LogInformation("Database '{DatabaseName}' does not exist. Creating it now...", databaseName);

            command.CommandText = $"CREATE DATABASE [{databaseName}]";
            command.ExecuteNonQuery();

            logger.LogInformation("Database '{DatabaseName}' created successfully.", databaseName);
        }
        else
        {
            logger.LogInformation("Database '{DatabaseName}' already exists. Skipping creation.", databaseName);
        }
    }
}
