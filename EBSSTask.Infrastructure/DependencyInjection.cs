using EBSSTask.Application.Common.Interfaces;
using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Infrastructure.Persistence;
using EBSSTask.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EBSSTask.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<EliteDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<EliteDbContext>());

        // Background Reporting Infrastructure
        services.AddSingleton<IReportQueue, ChannelReportQueue>();
        services.AddSingleton<IReportStorage, DiskReportStorage>();
        services.AddHostedService<ReportGenerationBackgroundService>();

        return services;
    }
}
