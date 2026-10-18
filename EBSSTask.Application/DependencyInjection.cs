using System.Reflection;
using EBSSTask.Application.Interfaces;
using EBSSTask.Application.Reports.DataSources;
using EBSSTask.Application.Reports.Interfaces;
using EBSSTask.Application.Reports.Services;
using EBSSTask.Application.Reports.Writers;
using EBSSTask.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EBSSTask.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Domain entity query services
        services.AddScoped<IBalanceService, BalanceService>();
        services.AddScoped<IBalanceHistoryService, BalanceHistoryService>();

        // Report Data Sources
        services.AddScoped<IReportDataSource, BalanceHistoryReportDataSource>();

        // Report Format Writers
        services.AddSingleton<IReportWriter, ClosedXmlReportWriter>();
        services.AddSingleton<IReportWriter, QuestPdfReportWriter>();
        services.AddSingleton<IDirectExportService, DirectExportService>();

        // Report Orchestration & Processing
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IReportProcessor, ReportProcessor>();

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
