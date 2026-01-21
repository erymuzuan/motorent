using Microsoft.Extensions.DependencyInjection;
using MotoRent.Domain.Helps;
using MotoRent.Domain.Messaging;
using MotoRent.Domain.QueryProviders;

namespace MotoRent.Domain.DataContext;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMotoRentDataContext(this IServiceCollection services, string connectionString)
    {
        // Note: Infrastructure services (IPagingTranslator, ISqlServerMetadata) and
        // IRepository<T> implementations are registered via AddMotoRentSqlServerRepository()
        // from MotoRent.SqlServerRepository project

        // Note: Core entity repositories (Organization, User, Setting, etc.) are registered
        // via AddCoreRepository() from MotoRent.Core.Repository project

        // Register RentalDataContext with full dependency injection
        services.AddScoped<RentalDataContext>(sp =>
        {
            var queryProvider = ObjectBuilder.GetObject<QueryProvider>();
            var messageBroker = sp.GetService<IMessageBroker>();
            var requestContext = sp.GetService<Core.IRequestContext>();
            var persistence = sp.GetService<IPersistence>();

            return new RentalDataContext(
                queryProvider,
                messageBroker,
                requestContext?.GetAccountNo(),
                persistence);
        });

        return services;
    }

    public static void ConfigureObjectBuilder(this IServiceProvider serviceProvider)
    {
        ObjectBuilder.Configure(serviceProvider);
    }
}
