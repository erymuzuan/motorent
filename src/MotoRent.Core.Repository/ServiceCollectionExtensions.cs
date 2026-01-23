using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Helps;

namespace MotoRent.Core.Repository;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Core Repository services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration for connection strings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCoreRepository(this IServiceCollection services, IConfiguration configuration)
    {

        // Register the paging translator
        services.AddSingleton<ICorePagingTranslator, Core2012PagingTranslator>();

        // Register the cache service
        services.AddScoped<ICacheService, HybridCacheService>();

        // Register the query provider
        services.AddScoped<CoreSqlQueryProvider>();

        // Register repositories for Core entities
        services.AddScoped<IRepository<Organization>, CoreSqlJsonRepository<Organization>>();
        services.AddScoped<IRepository<User>, CoreSqlJsonRepository<User>>();
        services.AddScoped<IRepository<Setting>, CoreSqlJsonRepository<Setting>>();
        services.AddScoped<IRepository<AccessToken>, CoreSqlJsonRepository<AccessToken>>();
        services.AddScoped<IRepository<RegistrationInvite>, CoreSqlJsonRepository<RegistrationInvite>>();
        services.AddScoped<IRepository<LogEntry>, CoreSqlJsonRepository<LogEntry>>();
        services.AddScoped<IRepository<SupportRequest>, CoreSqlJsonRepository<SupportRequest>>();
        services.AddScoped<IRepository<VehicleModel>, CoreSqlJsonRepository<VehicleModel>>();

        // Register CoreDataContext (uses DI-injected repositories)
        services.AddScoped<CoreDataContext>();

        return services;
    }
}
