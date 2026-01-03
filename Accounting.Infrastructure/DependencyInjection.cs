using Accounting.Application.Common.Abstractions;
using Accounting.Application.Services;
using Accounting.Infrastructure.Persistence;
using Accounting.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config) 
    {
        var conn = config.GetConnectionString("Default")!;
        services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(conn));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddScoped<IInvoiceBalanceService, InvoiceBalanceService>();

        return services;
    }
}
