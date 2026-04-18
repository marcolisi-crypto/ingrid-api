using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AIReception.Mvc.Data;

public class IngridDmsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IngridDmsDbContext>
{
    public IngridDmsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IngridDmsDbContext>();
        var connectionString = ResolveConnectionString();

        optionsBuilder.UseNpgsql(NormalizeConnectionString(connectionString));
        return new IngridDmsDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var ingridEnvironmentConnection = Environment.GetEnvironmentVariable("INGRID_DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(ingridEnvironmentConnection))
        {
            return ingridEnvironmentConnection;
        }

        var railwayConnection = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(railwayConnection))
        {
            return railwayConnection;
        }

        return "Host=localhost;Port=5432;Database=ingrid_dms;Username=postgres;Password=postgres";
    }

    private static string NormalizeConnectionString(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2, StringSplitOptions.TrimEntries);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "postgres";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var database = uri.AbsolutePath.Trim('/');
        var sslMode = uri.Query.Contains("sslmode=require", StringComparison.OrdinalIgnoreCase)
            ? "Require"
            : "Prefer";

        return $"Host={uri.Host};Port={uri.Port};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true";
    }
}
