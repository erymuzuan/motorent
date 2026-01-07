using Microsoft.Extensions.DependencyInjection;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMotoRentDataContext(this IServiceCollection services, string connectionString)
    {
        // Register QueryProvider
        services.AddSingleton(new QueryProvider(connectionString));

        // Register repositories for all entity types
        services.AddScoped<IRepository<Shop>, Repository<Shop>>();
        services.AddScoped<IRepository<Renter>, Repository<Renter>>();
        services.AddScoped<IRepository<Document>, Repository<Document>>();
        services.AddScoped<IRepository<Motorbike>, Repository<Motorbike>>();
        services.AddScoped<IRepository<Rental>, Repository<Rental>>();
        services.AddScoped<IRepository<Deposit>, Repository<Deposit>>();
        services.AddScoped<IRepository<Insurance>, Repository<Insurance>>();
        services.AddScoped<IRepository<Accessory>, Repository<Accessory>>();
        services.AddScoped<IRepository<RentalAccessory>, Repository<RentalAccessory>>();
        services.AddScoped<IRepository<Payment>, Repository<Payment>>();
        services.AddScoped<IRepository<DamageReport>, Repository<DamageReport>>();
        services.AddScoped<IRepository<DamagePhoto>, Repository<DamagePhoto>>();
        services.AddScoped<IRepository<RentalAgreement>, Repository<RentalAgreement>>();

        // Register DataContext
        services.AddScoped<RentalDataContext>();

        return services;
    }

    public static void ConfigureObjectBuilder(this IServiceProvider serviceProvider)
    {
        ObjectBuilder.Configure(serviceProvider);
    }
}
