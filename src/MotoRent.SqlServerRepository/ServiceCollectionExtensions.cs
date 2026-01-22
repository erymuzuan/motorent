using Microsoft.Extensions.DependencyInjection;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Helps;
using MotoRent.Domain.QueryProviders;

namespace MotoRent.SqlServerRepository;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMotoRentSqlServerRepository(this IServiceCollection services)
    {
        // Register infrastructure services
        services.AddSingleton<IPagingTranslator, Sql2012PagingTranslator>();
        // NOTE: Must be Scoped because these depend on scoped IRequestContext
        services.AddScoped<ISqlServerMetadata, SqlServerMetadata>();
        services.AddScoped<QueryProvider, SqlQueryProvider>();
        services.AddScoped<SqlQueryProvider>();

        // Register batch persistence service
        services.AddScoped<IPersistence, SqlPersistence>();

        // Register repositories for MotoRent operational entities using SqlJsonRepository
        // NOTE: Must be Scoped (not Singleton) because SqlJsonRepository depends on scoped IRequestContext
        services.AddScoped<IRepository<Shop>, SqlJsonRepository<Shop>>();
        services.AddScoped<IRepository<ShopSchedule>, SqlJsonRepository<ShopSchedule>>();
        services.AddScoped<IRepository<ServiceLocation>, SqlJsonRepository<ServiceLocation>>();
        services.AddScoped<IRepository<Renter>, SqlJsonRepository<Renter>>();
        services.AddScoped<IRepository<Document>, SqlJsonRepository<Document>>();
        services.AddScoped<IRepository<Vehicle>, SqlJsonRepository<Vehicle>>();
        services.AddScoped<IRepository<VehicleImage>, SqlJsonRepository<VehicleImage>>();
        services.AddScoped<IRepository<VehiclePool>, SqlJsonRepository<VehiclePool>>();
        services.AddScoped<IRepository<Motorbike>, SqlJsonRepository<Motorbike>>();  // Deprecated: Use Vehicle
        services.AddScoped<IRepository<Rental>, SqlJsonRepository<Rental>>();
        services.AddScoped<IRepository<Booking>, SqlJsonRepository<Booking>>();
        services.AddScoped<IRepository<Deposit>, SqlJsonRepository<Deposit>>();
        services.AddScoped<IRepository<Insurance>, SqlJsonRepository<Insurance>>();
        services.AddScoped<IRepository<Accessory>, SqlJsonRepository<Accessory>>();
        services.AddScoped<IRepository<RentalAccessory>, SqlJsonRepository<RentalAccessory>>();
        services.AddScoped<IRepository<Payment>, SqlJsonRepository<Payment>>();
        services.AddScoped<IRepository<DamageReport>, SqlJsonRepository<DamageReport>>();
        services.AddScoped<IRepository<DamagePhoto>, SqlJsonRepository<DamagePhoto>>();
        services.AddScoped<IRepository<RentalAgreement>, SqlJsonRepository<RentalAgreement>>();
        services.AddScoped<IRepository<ServiceType>, SqlJsonRepository<ServiceType>>();
        services.AddScoped<IRepository<MaintenanceSchedule>, SqlJsonRepository<MaintenanceSchedule>>();
        services.AddScoped<IRepository<MaintenanceAlert>, SqlJsonRepository<MaintenanceAlert>>();
        services.AddScoped<IRepository<MaintenanceRecord>, SqlJsonRepository<MaintenanceRecord>>();
        // Dynamic pricing entities
        services.AddScoped<IRepository<PricingRule>, SqlJsonRepository<PricingRule>>();
        // Third-party owner entities
        services.AddScoped<IRepository<VehicleOwner>, SqlJsonRepository<VehicleOwner>>();
        services.AddScoped<IRepository<OwnerPayment>, SqlJsonRepository<OwnerPayment>>();
        // Accident entities
        services.AddScoped<IRepository<Accident>, SqlJsonRepository<Accident>>();
        services.AddScoped<IRepository<AccidentParty>, SqlJsonRepository<AccidentParty>>();
        services.AddScoped<IRepository<AccidentDocument>, SqlJsonRepository<AccidentDocument>>();
        services.AddScoped<IRepository<AccidentCost>, SqlJsonRepository<AccidentCost>>();
        services.AddScoped<IRepository<AccidentNote>, SqlJsonRepository<AccidentNote>>();
        // Comment entity
        services.AddScoped<IRepository<Comment>, SqlJsonRepository<Comment>>();
        // Agent entities
        services.AddScoped<IRepository<Agent>, SqlJsonRepository<Agent>>();
        services.AddScoped<IRepository<AgentCommission>, SqlJsonRepository<AgentCommission>>();
        services.AddScoped<IRepository<AgentInvoice>, SqlJsonRepository<AgentInvoice>>();
        // Asset depreciation entities
        services.AddScoped<IRepository<Asset>, SqlJsonRepository<Asset>>();
        services.AddScoped<IRepository<DepreciationEntry>, SqlJsonRepository<DepreciationEntry>>();
        services.AddScoped<IRepository<AssetExpense>, SqlJsonRepository<AssetExpense>>();
        services.AddScoped<IRepository<AssetLoan>, SqlJsonRepository<AssetLoan>>();
        services.AddScoped<IRepository<AssetLoanPayment>, SqlJsonRepository<AssetLoanPayment>>();
        // Cashier Till entities
        services.AddScoped<IRepository<TillSession>, SqlJsonRepository<TillSession>>();
        services.AddScoped<IRepository<TillTransaction>, SqlJsonRepository<TillTransaction>>();
        services.AddScoped<IRepository<TillDenominationCount>, SqlJsonRepository<TillDenominationCount>>();
        services.AddScoped<IRepository<Receipt>, SqlJsonRepository<Receipt>>();
        // Exchange rate entities
        services.AddScoped<IRepository<ExchangeRate>, SqlJsonRepository<ExchangeRate>>();
        // End of day entities
        services.AddScoped<IRepository<DailyClose>, SqlJsonRepository<DailyClose>>();
        services.AddScoped<IRepository<ShortageLog>, SqlJsonRepository<ShortageLog>>();

        return services;
    }
}
