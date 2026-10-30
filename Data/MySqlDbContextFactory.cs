using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DevTaskHubBackend.Data;

public class MySqlDbContextFactory : IDesignTimeDbContextFactory<MySqlDbContext>
{
    public MySqlDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<MySqlDbContextFactory>()
            .AddEnvironmentVariables()
            .Build();
        var settings = DatabaseSettings.From(configuration, isDevelopment: true);
        var options = new DbContextOptionsBuilder<MySqlDbContext>()
            .UseMySQL(MySqlConfiguration.BuildConnectionString(configuration, settings))
            .Options;
        return new MySqlDbContext(options);
    }
}

