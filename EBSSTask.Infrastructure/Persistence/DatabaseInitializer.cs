using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EBSSTask.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    private static readonly string[] ScriptNames =
    [
        "CreateTable_FileProcess.sql",
        "sp_GetBalanceHistorySummary.sql",
        "IX_BalanceHistory_Balance_Date.sql"
    ];

    /// <summary>
    /// Checks database connection validity and safely executes required schema scripts
    /// (tables, stored procedures, indexes) if not already applied.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        var dbContext = scope.ServiceProvider.GetRequiredService<EliteDbContext>();

        logger.LogInformation("Verifying database connectivity...");

        bool canConnect = false;
        try
        {
            canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database connection check failed: {Message}. Scripts will not be executed until connection is valid.", ex.Message);
            return;
        }

        if (!canConnect)
        {
            logger.LogWarning("Database connection is invalid. Please verify the 'DefaultConnection' string in appsettings.json.");
            return;
        }

        logger.LogInformation("Database connection is valid. Applying database scripts if needed...");

        var assembly = typeof(EliteDbContext).Assembly;

        foreach (var scriptFileName in ScriptNames)
        {
            try
            {
                var scriptContent = await LoadScriptContentAsync(assembly, scriptFileName, cancellationToken);
                if (string.IsNullOrWhiteSpace(scriptContent))
                {
                    logger.LogWarning("SQL script '{ScriptFileName}' was not found or is empty.", scriptFileName);
                    continue;
                }

                logger.LogInformation("Processing SQL script: {ScriptFileName}", scriptFileName);

                // Split script into executable batches by SSMS / sqlcmd batch separator 'GO'
                var batches = Regex.Split(scriptContent, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                                   .Where(b => !string.IsNullOrWhiteSpace(b))
                                   .Select(b => b.Trim());

                foreach (var batch in batches)
                {
                    await dbContext.Database.ExecuteSqlRawAsync(batch, cancellationToken);
                }

                logger.LogInformation("Successfully executed SQL script: {ScriptFileName}", scriptFileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while applying SQL script '{ScriptFileName}': {Message}", scriptFileName, ex.Message);
            }
        }

        logger.LogInformation("Database initialization complete.");
    }

    private static async Task<string?> LoadScriptContentAsync(Assembly assembly, string scriptFileName, CancellationToken cancellationToken)
    {
        // 1. Try reading from Embedded Resources
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith(scriptFileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName != null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync(cancellationToken);
            }
        }

        // 2. Fallback to physical file path in base directory or subfolders
        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Persistence", "Scripts", scriptFileName),
            Path.Combine(AppContext.BaseDirectory, "Scripts", scriptFileName),
            Path.Combine(AppContext.BaseDirectory, scriptFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Persistence", "Scripts", scriptFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "EBSSTask.Infrastructure", "Persistence", "Scripts", scriptFileName)
        };

        foreach (var path in candidatePaths)
        {
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path, cancellationToken);
            }
        }

        return null;
    }
}
