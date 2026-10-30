using System.Data.Common;
using DevTaskHubBackend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySql.Data.MySqlClient;

namespace DevTaskHubBackend.Data;

public static class MySqlConfiguration
{
    public static IServiceCollection AddMySql(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var settings = DatabaseSettings.From(configuration, environment.IsDevelopment());
        services.AddSingleton(settings);
        services.AddSingleton<EntityWriteInterceptor>();
        services.AddSingleton<ReadCommittedSessionInterceptor>();
        services.AddSingleton(sp => new SlowQueryInterceptor(
            sp.GetRequiredService<ILogger<SlowQueryInterceptor>>(),
            settings.SlowQueryThresholdMs));

        services.AddDbContextPool<MySqlDbContext>((sp, options) =>
        {
            var database = sp.GetRequiredService<DatabaseSettings>();
            options.UseMySQL(
                BuildConnectionString(configuration, database),
                mysql =>
                {
                    mysql.CommandTimeout(database.CommandTimeoutSeconds);
                    mysql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    if (database.RetryCount > 0)
                    {
                        mysql.EnableRetryOnFailure(
                            database.RetryCount,
                            TimeSpan.FromSeconds(database.MaxRetryDelaySeconds),
                            database.TransientErrorNumbers);
                    }
                });

            options.ConfigureWarnings(warnings => warnings.Ignore(
                CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
            options.AddInterceptors(
                sp.GetRequiredService<ReadCommittedSessionInterceptor>(),
                sp.GetRequiredService<EntityWriteInterceptor>(),
                sp.GetRequiredService<SlowQueryInterceptor>());
        }, settings.DbContextPoolSize);

        return services;
    }

    public static string BuildConnectionString(IConfiguration configuration, DatabaseSettings settings)
    {
        var connectionString = configuration.GetConnectionString("MySqlConnection")
            ?? throw new InvalidOperationException("MySqlConnection is not configured.");

        var builder = new MySqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrEmpty(builder.Password))
        {
            throw new InvalidOperationException(
                "MySQL password is missing. Set ConnectionStrings:MySqlConnection in " +
                "development user secrets, or set the ConnectionStrings__MySqlConnection " +
                "environment variable, to a complete connection string including Password.");
        }

        builder.Pooling = true;
        builder.MinimumPoolSize = (uint)settings.MinPoolSize;
        builder.MaximumPoolSize = (uint)settings.MaxPoolSize;
        builder.ConnectionLifeTime = (uint)settings.ConnectionLifetimeSeconds;
        builder.ConnectionTimeout = (uint)settings.ConnectionTimeoutSeconds;
        builder.DefaultCommandTimeout = (uint)settings.CommandTimeoutSeconds;
        builder.ConnectionReset = true;
        builder.AllowUserVariables = true;
        builder.UseAffectedRows = true;
        return builder.ConnectionString;
    }
}

public sealed class DatabaseSettings
{
    public int MinPoolSize { get; init; }
    public int MaxPoolSize { get; init; }
    public int ConnectionLifetimeSeconds { get; init; }
    public int ConnectionTimeoutSeconds { get; init; }
    public int CommandTimeoutSeconds { get; init; }
    public int RetryCount { get; init; }
    public int MaxRetryDelaySeconds { get; init; }
    public int DbContextPoolSize { get; init; }
    public int SlowQueryThresholdMs { get; init; }
    public int[] TransientErrorNumbers { get; init; } = [1205, 1213, 2006, 2013];

    public static DatabaseSettings From(IConfiguration configuration, bool isDevelopment)
    {
        var section = configuration.GetSection("Database");
        var settings = new DatabaseSettings
        {
            MinPoolSize = section.GetValue("MinPoolSize", isDevelopment ? 0 : 5),
            MaxPoolSize = section.GetValue("MaxPoolSize", isDevelopment ? 20 : 50),
            ConnectionLifetimeSeconds = section.GetValue("ConnectionLifetimeSeconds", 180),
            ConnectionTimeoutSeconds = section.GetValue("ConnectionTimeoutSeconds", 15),
            CommandTimeoutSeconds = section.GetValue("CommandTimeoutSeconds", 30),
            RetryCount = section.GetValue("RetryCount", 5),
            MaxRetryDelaySeconds = section.GetValue("MaxRetryDelaySeconds", 8),
            DbContextPoolSize = section.GetValue("DbContextPoolSize", isDevelopment ? 32 : 64),
            SlowQueryThresholdMs = section.GetValue("SlowQueryThresholdMs", 200)
        };

        if (settings.MaxPoolSize is < 1 or > 512
            || settings.MinPoolSize < 0
            || settings.MinPoolSize > settings.MaxPoolSize
            || settings.DbContextPoolSize < settings.MaxPoolSize
            || settings.DbContextPoolSize > 1024
            || settings.ConnectionLifetimeSeconds is < 0 or > 3600
            || settings.ConnectionTimeoutSeconds is < 1 or > 60
            || settings.CommandTimeoutSeconds is < 1 or > 120
            || settings.RetryCount is < 0 or > 10
            || settings.MaxRetryDelaySeconds is < 1 or > 30
            || settings.SlowQueryThresholdMs is < 0 or > 60_000)
        {
            throw new InvalidOperationException("Database settings in appsettings are out of range.");
        }

        return settings;
    }
}

public sealed class EntityWriteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            Normalize(entry);
            if (entry.Entity is BaseEntity row)
            {
                if (entry.State == EntityState.Added)
                {
                    row.CreatedAt = now;
                    row.UpdatedAt = now;
                    continue;
                }

                entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                row.UpdatedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                BumpRowVersion(entry);
            }
        }
    }

    private static void Normalize(EntityEntry entry)
    {
        switch (entry.Entity)
        {
            case User user:
                user.FullName = user.FullName.Trim();
                user.Email = user.Email.Trim().ToLowerInvariant();
                user.PhoneNumber = BlankToNull(user.PhoneNumber);
                user.GitHubUserName = BlankToNull(user.GitHubUserName);
                break;
            case Projects project:
                project.ProjectName = project.ProjectName.Trim();
                project.GitHubOwner = BlankToNull(project.GitHubOwner);
                project.GitHubRepoName = BlankToNull(project.GitHubRepoName);
                break;
            case Tasks task:
                task.Title = task.Title.Trim();
                task.GitHubPRStatus = BlankToNull(task.GitHubPRStatus);
                task.GitHubPRUrl = BlankToNull(task.GitHubPRUrl);
                break;
        }
    }

    private static string? BlankToNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static void BumpRowVersion(EntityEntry entry)
    {
        if (entry.Metadata.FindProperty("RowVersion") is null)
        {
            return;
        }

        var version = entry.Property("RowVersion");
        var current = version.CurrentValue is uint number ? number : 0u;
        version.CurrentValue = current + 1;
    }
}

public sealed class ReadCommittedSessionInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED";
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public sealed class SlowQueryInterceptor : DbCommandInterceptor
{
    private readonly ILogger<SlowQueryInterceptor> _logger;
    private readonly double _thresholdMs;

    public SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger, int thresholdMs)
    {
        _logger = logger;
        _thresholdMs = thresholdMs;
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Log(command, eventData.Duration);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Log(command, eventData.Duration);
        return new ValueTask<DbDataReader>(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Log(command, eventData.Duration);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Log(command, eventData.Duration);
        return new ValueTask<int>(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Log(command, eventData.Duration);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        Log(command, eventData.Duration);
        return new ValueTask<object?>(result);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData) => Log(command, eventData.Duration);

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Log(command, eventData.Duration);
        return Task.CompletedTask;
    }

    private void Log(DbCommand command, TimeSpan duration)
    {
        if (duration.TotalMilliseconds < _thresholdMs)
        {
            return;
        }

        var text = command.CommandText;
        if (text.Length > 400)
        {
            text = text[..400];
        }

        _logger.LogWarning("Slow database command ({ElapsedMs} ms): {CommandText}", (int)duration.TotalMilliseconds, text);
    }
}
