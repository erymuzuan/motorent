using Microsoft.Extensions.DependencyInjection;
using MotoRent.Domain.Core;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Helps;

namespace MotoRent.Domain.DataContext;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMotoRentDataContext(this IServiceCollection services, string connectionString)
    {
        // Register QueryProvider
        services.AddSingleton(new QueryProvider(connectionString));

        // Register repositories for MotoRent operational entities (singleton since they're stateless)
        services.AddSingleton<IRepository<Shop>, Repository<Shop>>();
        services.AddSingleton<IRepository<ShopSchedule>, Repository<ShopSchedule>>();
        services.AddSingleton<IRepository<ServiceLocation>, Repository<ServiceLocation>>();
        services.AddSingleton<IRepository<Renter>, Repository<Renter>>();
        services.AddSingleton<IRepository<Document>, Repository<Document>>();
        services.AddSingleton<IRepository<Vehicle>, Repository<Vehicle>>();
        services.AddSingleton<IRepository<VehicleImage>, Repository<VehicleImage>>();
        services.AddSingleton<IRepository<VehiclePool>, Repository<VehiclePool>>();
        services.AddSingleton<IRepository<Motorbike>, Repository<Motorbike>>();  // Deprecated: Use Vehicle
        services.AddSingleton<IRepository<Rental>, Repository<Rental>>();
        services.AddSingleton<IRepository<Deposit>, Repository<Deposit>>();
        services.AddSingleton<IRepository<Insurance>, Repository<Insurance>>();
        services.AddSingleton<IRepository<Accessory>, Repository<Accessory>>();
        services.AddSingleton<IRepository<RentalAccessory>, Repository<RentalAccessory>>();
        services.AddSingleton<IRepository<Payment>, Repository<Payment>>();
        services.AddSingleton<IRepository<DamageReport>, Repository<DamageReport>>();
        services.AddSingleton<IRepository<DamagePhoto>, Repository<DamagePhoto>>();
        services.AddSingleton<IRepository<VehicleImage>, Repository<VehicleImage>>();
        services.AddSingleton<IRepository<RentalAgreement>, Repository<RentalAgreement>>();
        services.AddSingleton<IRepository<ServiceType>, Repository<ServiceType>>();
        services.AddSingleton<IRepository<MaintenanceSchedule>, Repository<MaintenanceSchedule>>();
        // Third-party owner entities
        services.AddSingleton<IRepository<VehicleOwner>, Repository<VehicleOwner>>();
        services.AddSingleton<IRepository<OwnerPayment>, Repository<OwnerPayment>>();
        // Accident entities
        services.AddSingleton<IRepository<Accident>, Repository<Accident>>();
        services.AddSingleton<IRepository<AccidentParty>, Repository<AccidentParty>>();
        services.AddSingleton<IRepository<AccidentDocument>, Repository<AccidentDocument>>();
        services.AddSingleton<IRepository<AccidentCost>, Repository<AccidentCost>>();
        services.AddSingleton<IRepository<AccidentNote>, Repository<AccidentNote>>();
        // Comment entity
        services.AddSingleton<IRepository<Comment>, Repository<Comment>>();
        // GPS tracking entities
        services.AddSingleton<IRepository<GpsTrackingDevice>, Repository<GpsTrackingDevice>>();
        services.AddSingleton<IRepository<GpsPosition>, Repository<GpsPosition>>();
        services.AddSingleton<IRepository<Geofence>, Repository<Geofence>>();
        services.AddSingleton<IRepository<GeofenceAlert>, Repository<GeofenceAlert>>();
        services.AddSingleton<IRepository<TripHistory>, Repository<TripHistory>>();

        // Register repositories for Core entities (uses [Core] schema)
        services.AddSingleton<IRepository<Organization>, CoreRepository<Organization>>();
        services.AddSingleton<IRepository<User>, CoreRepository<User>>();
        services.AddSingleton<IRepository<Setting>, CoreRepository<Setting>>();
        services.AddSingleton<IRepository<AccessToken>, CoreRepository<AccessToken>>();
        services.AddSingleton<IRepository<RegistrationInvite>, CoreRepository<RegistrationInvite>>();
        services.AddSingleton<IRepository<LogEntry>, CoreRepository<LogEntry>>();

        // Register DataContexts
        services.AddScoped<RentalDataContext>();
        services.AddScoped<CoreDataContext>();

        return services;
    }

    public static void ConfigureObjectBuilder(this IServiceProvider serviceProvider)
    {
        ObjectBuilder.Configure(serviceProvider);
    }
}
