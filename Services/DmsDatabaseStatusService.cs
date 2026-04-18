using AIReception.Mvc.Data;
using Microsoft.EntityFrameworkCore;

namespace AIReception.Mvc.Services;

public class DmsDatabaseStatusService
{
    private readonly IDbContextFactory<IngridDmsDbContext> _dbContextFactory;
    private readonly IConfiguration _configuration;

    public DmsDatabaseStatusService(
        IDbContextFactory<IngridDmsDbContext> dbContextFactory,
        IConfiguration configuration)
    {
        _dbContextFactory = dbContextFactory;
        _configuration = configuration;
    }

    public object GetStatus()
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var configuredConnection = ResolveIngridConnectionString();
        var canConnect = false;

        try
        {
            canConnect = dbContext.Database.CanConnect();
        }
        catch
        {
            canConnect = false;
        }

        return new
        {
            provider = dbContext.Database.ProviderName,
            databaseConfigured = !string.IsNullOrWhiteSpace(configuredConnection),
            databaseCanConnect = canConnect,
            usingInMemoryFallback = string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal),
            pendingModelSupport = true
        };
    }

    private string ResolveIngridConnectionString()
    {
        var configuredConnection = _configuration.GetConnectionString("IngridDms");
        if (!string.IsNullOrWhiteSpace(configuredConnection))
        {
            return configuredConnection;
        }

        var ingridEnvironmentConnection = Environment.GetEnvironmentVariable("INGRID_DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(ingridEnvironmentConnection))
        {
            return ingridEnvironmentConnection;
        }

        return Environment.GetEnvironmentVariable("DATABASE_URL") ?? "";
    }
}
