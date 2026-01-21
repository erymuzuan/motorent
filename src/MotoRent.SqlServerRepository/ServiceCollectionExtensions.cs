using Microsoft.Extensions.DependencyInjection;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Helps;

namespace MotoRent.SqlServerRepository;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMotoRentSqlServerRepository(this IServiceCollection services)
    {
        // Register infrastructure services
        services.AddSingleton<IPagingTranslator, Sql2012PagingTranslator>();
        services.AddSingleton<ISqlServerMetadata, SqlServerMetadata>();

        // Register batch persistence service
        services.AddScoped<IPersistence, SqlPersistence>();

        // Register repositories for MotoRent operational entities using SqlJsonRepository
        services.AddSingleton<IRepository<Shop>, SqlJsonRepository<Shop>>();
        services.AddSingleton<IRepository<ShopSchedule>, SqlJsonRepository<ShopSchedule>>();
        services.AddSingleton<IRepository<ServiceLocation>, SqlJsonRepository<ServiceLocation>>();
        services.AddSingleton<IRepository<Renter>, SqlJsonRepository<Renter>>();
        services.AddSingleton<IRepository<Document>, SqlJsonRepository<Document>>();
        services.AddSingleton<IRepository<Vehicle>, SqlJsonRepository<Vehicle>>();
        services.AddSingleton<IRepository<VehicleImage>, SqlJsonRepository<VehicleImage>>();
        services.AddSingleton<IRepository<VehiclePool>, SqlJsonRepository<VehiclePool>>();
        services.AddSingleton<IRepository<Motorbike>, SqlJsonRepository<Motorbike>>();  // Deprecated: Use Vehicle
        services.AddSingleton<IRepository<Rental>, SqlJsonRepository<Rental>>();
        services.AddSingleton<IRepository<Booking>, SqlJsonRepository<Booking>>();
        services.AddSingleton<IRepository<Deposit>, SqlJsonRepository<Deposit>>();
        services.AddSingleton<IRepository<Insurance>, SqlJsonRepository<Insurance>>();
        services.AddSingleton<IRepository<Accessory>, SqlJsonRepository<Accessory>>();
        services.AddSingleton<IRepository<RentalAccessory>, SqlJsonRepository<RentalAccessory>>();
        services.AddSingleton<IRepository<Payment>, SqlJsonRepository<Payment>>();
        services.AddSingleton<IRepository<DamageReport>, SqlJsonRepository<DamageReport>>();
        services.AddSingleton<IRepository<DamagePhoto>, SqlJsonRepository<DamagePhoto>>();
        services.AddSingleton<IRepository<RentalAgreement>, SqlJsonRepository<RentalAgreement>>();
        services.AddSingleton<IRepository<ServiceType>, SqlJsonRepository<ServiceType>>();
        services.AddSingleton<IRepository<MaintenanceSchedule>, SqlJsonRepository<MaintenanceSchedule>>();
        services.AddSingleton<IRepository<MaintenanceAlert>, SqlJsonRepository<MaintenanceAlert>>();
        services.AddSingleton<IRepository<MaintenanceRecord>, SqlJsonRepository<MaintenanceRecord>>();
        // Dynamic pricing entities
        services.AddSingleton<IRepository<PricingRule>, SqlJsonRepository<PricingRule>>();
        // Third-party owner entities
        services.AddSingleton<IRepository<VehicleOwner>, SqlJsonRepository<VehicleOwner>>();
        services.AddSingleton<IRepository<OwnerPayment>, SqlJsonRepository<OwnerPayment>>();
        // Accident entities
        services.AddSingleton<IRepository<Accident>, SqlJsonRepository<Accident>>();
        services.AddSingleton<IRepository<AccidentParty>, SqlJsonRepository<AccidentParty>>();
        services.AddSingleton<IRepository<AccidentDocument>, SqlJsonRepository<AccidentDocument>>();
        services.AddSingleton<IRepository<AccidentCost>, SqlJsonRepository<AccidentCost>>();
        services.AddSingleton<IRepository<AccidentNote>, SqlJsonRepository<AccidentNote>>();
        // Comment entity
        services.AddSingleton<IRepository<Comment>, SqlJsonRepository<Comment>>();
        // Agent entities
        services.AddSingleton<IRepository<Agent>, SqlJsonRepository<Agent>>();
        services.AddSingleton<IRepository<AgentCommission>, SqlJsonRepository<AgentCommission>>();
        services.AddSingleton<IRepository<AgentInvoice>, SqlJsonRepository<AgentInvoice>>();
        // Asset depreciation entities
        services.AddSingleton<IRepository<Asset>, SqlJsonRepository<Asset>>();
        services.AddSingleton<IRepository<DepreciationEntry>, SqlJsonRepository<DepreciationEntry>>();
        services.AddSingleton<IRepository<AssetExpense>, SqlJsonRepository<AssetExpense>>();
        services.AddSingleton<IRepository<AssetLoan>, SqlJsonRepository<AssetLoan>>();
        services.AddSingleton<IRepository<AssetLoanPayment>, SqlJsonRepository<AssetLoanPayment>>();
        // Cashier Till entities
        services.AddSingleton<IRepository<TillSession>, SqlJsonRepository<TillSession>>();
        services.AddSingleton<IRepository<TillTransaction>, SqlJsonRepository<TillTransaction>>();
        services.AddSingleton<IRepository<TillDenominationCount>, SqlJsonRepository<TillDenominationCount>>();
        services.AddSingleton<IRepository<Receipt>, SqlJsonRepository<Receipt>>();
        // Exchange rate entities
        services.AddSingleton<IRepository<ExchangeRate>, SqlJsonRepository<ExchangeRate>>();
        // End of day entities
        services.AddSingleton<IRepository<DailyClose>, SqlJsonRepository<DailyClose>>();
        services.AddSingleton<IRepository<ShortageLog>, SqlJsonRepository<ShortageLog>>();

        return services;
    }
}
