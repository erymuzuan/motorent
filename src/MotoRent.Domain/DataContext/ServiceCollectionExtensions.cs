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
        services.AddSingleton<IRepository<Booking>, Repository<Booking>>();
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
        services.AddSingleton<IRepository<MaintenanceAlert>, Repository<MaintenanceAlert>>();
        services.AddSingleton<IRepository<MaintenanceRecord>, Repository<MaintenanceRecord>>();
        // Dynamic pricing entities
        services.AddSingleton<IRepository<PricingRule>, Repository<PricingRule>>();
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
        // Agent entities
        services.AddSingleton<IRepository<Agent>, Repository<Agent>>();
        services.AddSingleton<IRepository<AgentCommission>, Repository<AgentCommission>>();
        services.AddSingleton<IRepository<AgentInvoice>, Repository<AgentInvoice>>();
        // Asset depreciation entities
        services.AddSingleton<IRepository<Asset>, Repository<Asset>>();
        services.AddSingleton<IRepository<DepreciationEntry>, Repository<DepreciationEntry>>();
        services.AddSingleton<IRepository<AssetExpense>, Repository<AssetExpense>>();
        services.AddSingleton<IRepository<AssetLoan>, Repository<AssetLoan>>();
        services.AddSingleton<IRepository<AssetLoanPayment>, Repository<AssetLoanPayment>>();

        // Note: Core entity repositories (Organization, User, Setting, etc.) are registered
        // via AddCoreRepository() from MotoRent.Core.Repository project

        // Register RentalDataContext (for tenant-specific entities)
        services.AddScoped<RentalDataContext>();

        return services;
    }

    public static void ConfigureObjectBuilder(this IServiceProvider serviceProvider)
    {
        ObjectBuilder.Configure(serviceProvider);
    }
}
