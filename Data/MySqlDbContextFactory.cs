using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DevTaskHubBackend.Data;

public class MySqlDbContextFactory : IDesignTimeDbContextFactory<MySqlDbContext>
{
    public MySqlDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var isDevelopment = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<MySqlDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var settings = DatabaseSettings.From(configuration, isDevelopment);
        var options = new DbContextOptionsBuilder<MySqlDbContext>()
            .UseMySQL(MySqlConfiguration.BuildConnectionString(configuration, settings, isDevelopment))
            .Options;
        return new MySqlDbContext(options);
    }
}

