using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration config) 
        {
            var conn = config.GetConnectionString("Default")!;
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(conn));
            return services;
        }
    }
}
