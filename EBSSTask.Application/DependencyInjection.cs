using System.Reflection;
using EBSSTask.Application.Interfaces;
using EBSSTask.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EBSSTask.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBalanceService, BalanceService>();
        services.AddScoped<IBalanceHistoryService, BalanceHistoryService>();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
