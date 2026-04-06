using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MotoRent.Core.Repository;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Helps;
using MotoRent.Domain.QueryProviders;

namespace MotoRent.PostgreSqlRepository;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMotoRentPostgreSqlRepository(this IServiceCollection services)
    {
        // Register infrastructure services
        services.AddSingleton<PgPagingTranslator>();
        // NOTE: Must be Scoped because these depend on scoped IRequestContext
        services.AddScoped<IPgMetadata, PgMetadata>();
        services.AddScoped<DbConnectionInterceptor>();
        services.AddScoped<QueryProvider, PgQueryProvider>();
        services.AddScoped<PgQueryProvider>();

        // Register batch persistence service
        services.AddScoped<IPersistence, PgPersistence>();

        // Register repositories for MotoRent operational entities using PgJsonRepository
        // NOTE: Must be Scoped (not Singleton) because PgJsonRepository depends on scoped IRequestContext
        services.AddScoped<IRepository<Shop>, PgJsonRepository<Shop>>();
        services.AddScoped<IRepository<ShopSchedule>, PgJsonRepository<ShopSchedule>>();
        services.AddScoped<IRepository<ServiceLocation>, PgJsonRepository<ServiceLocation>>();
        services.AddScoped<IRepository<Renter>, PgJsonRepository<Renter>>();
        services.AddScoped<IRepository<Document>, PgJsonRepository<Document>>();
        services.AddScoped<IRepository<Vehicle>, PgJsonRepository<Vehicle>>();
        services.AddScoped<IRepository<VehicleImage>, PgJsonRepository<VehicleImage>>();
        services.AddScoped<IRepository<VehiclePool>, PgJsonRepository<VehiclePool>>();
        services.AddScoped<IRepository<Motorbike>, PgJsonRepository<Motorbike>>();  // Deprecated: Use Vehicle
        services.AddScoped<IRepository<Rental>, PgJsonRepository<Rental>>();
        services.AddScoped<IRepository<Booking>, PgJsonRepository<Booking>>();
        services.AddScoped<IRepository<Deposit>, PgJsonRepository<Deposit>>();
        services.AddScoped<IRepository<Insurance>, PgJsonRepository<Insurance>>();
        services.AddScoped<IRepository<Accessory>, PgJsonRepository<Accessory>>();
        services.AddScoped<IRepository<RentalAccessory>, PgJsonRepository<RentalAccessory>>();
        services.AddScoped<IRepository<Payment>, PgJsonRepository<Payment>>();
        services.AddScoped<IRepository<DamageReport>, PgJsonRepository<DamageReport>>();
        services.AddScoped<IRepository<DamagePhoto>, PgJsonRepository<DamagePhoto>>();
        services.AddScoped<IRepository<RentalAgreement>, PgJsonRepository<RentalAgreement>>();
        services.AddScoped<IRepository<ServiceType>, PgJsonRepository<ServiceType>>();
        services.AddScoped<IRepository<MaintenanceSchedule>, PgJsonRepository<MaintenanceSchedule>>();
        services.AddScoped<IRepository<MaintenanceAlert>, PgJsonRepository<MaintenanceAlert>>();
        services.AddScoped<IRepository<MaintenanceRecord>, PgJsonRepository<MaintenanceRecord>>();
        // Dynamic pricing entities
        services.AddScoped<IRepository<PricingRule>, PgJsonRepository<PricingRule>>();
        // Third-party owner entities
        services.AddScoped<IRepository<VehicleOwner>, PgJsonRepository<VehicleOwner>>();
        services.AddScoped<IRepository<OwnerPayment>, PgJsonRepository<OwnerPayment>>();
        // Accident entities
        services.AddScoped<IRepository<Accident>, PgJsonRepository<Accident>>();
        services.AddScoped<IRepository<AccidentParty>, PgJsonRepository<AccidentParty>>();
        services.AddScoped<IRepository<AccidentDocument>, PgJsonRepository<AccidentDocument>>();
        services.AddScoped<IRepository<AccidentCost>, PgJsonRepository<AccidentCost>>();
        services.AddScoped<IRepository<AccidentNote>, PgJsonRepository<AccidentNote>>();
        // Comment entity
        services.AddScoped<IRepository<Comment>, PgJsonRepository<Comment>>();
        // Agent entities
        services.AddScoped<IRepository<Agent>, PgJsonRepository<Agent>>();
        services.AddScoped<IRepository<AgentCommission>, PgJsonRepository<AgentCommission>>();
        services.AddScoped<IRepository<AgentInvoice>, PgJsonRepository<AgentInvoice>>();
        // Asset depreciation entities
        services.AddScoped<IRepository<Asset>, PgJsonRepository<Asset>>();
        services.AddScoped<IRepository<DepreciationEntry>, PgJsonRepository<DepreciationEntry>>();
        services.AddScoped<IRepository<AssetExpense>, PgJsonRepository<AssetExpense>>();
        services.AddScoped<IRepository<AssetLoan>, PgJsonRepository<AssetLoan>>();
        services.AddScoped<IRepository<AssetLoanPayment>, PgJsonRepository<AssetLoanPayment>>();
        // Cashier Till entities
        services.AddScoped<IRepository<TillSession>, PgJsonRepository<TillSession>>();
        services.AddScoped<IRepository<TillTransaction>, PgJsonRepository<TillTransaction>>();
        services.AddScoped<IRepository<TillDenominationCount>, PgJsonRepository<TillDenominationCount>>();
        services.AddScoped<IRepository<Receipt>, PgJsonRepository<Receipt>>();
        // Document template entity
        services.AddScoped<IRepository<DocumentTemplate>, PgJsonRepository<DocumentTemplate>>();
        // Exchange rate entities
        services.AddScoped<IRepository<ExchangeRate>, PgJsonRepository<ExchangeRate>>();
        services.AddScoped<IRepository<DenominationGroup>, PgJsonRepository<DenominationGroup>>();
        services.AddScoped<IRepository<DenominationRate>, PgJsonRepository<DenominationRate>>();
        services.AddScoped<IRepository<RateDelta>, PgJsonRepository<RateDelta>>();
        // End of day entities
        services.AddScoped<IRepository<DailyClose>, PgJsonRepository<DailyClose>>();
        services.AddScoped<IRepository<ShortageLog>, PgJsonRepository<ShortageLog>>();
        services.AddScoped<IRepository<VehicleInspection>, PgJsonRepository<VehicleInspection>>();
        // Fleet model entities
        services.AddScoped<IRepository<FleetModel>, PgJsonRepository<FleetModel>>();
        services.AddScoped<IRepository<FleetModelImage>, PgJsonRepository<FleetModelImage>>();
        // Traffic fine entities
        services.AddScoped<IRepository<TrafficFine>, PgJsonRepository<TrafficFine>>();

        return services;
    }

    public static IServiceCollection AddCorePostgreSqlRepository(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the paging translator (same instance serves both tenant and core)
        services.AddSingleton<ICorePagingTranslator, PgPagingTranslator>();

        // Register the cache service
        services.AddScoped<ICacheService, HybridCacheService>();

        // Register the Core PostgreSQL query provider
        services.AddScoped<CorePgQueryProvider>();

        // Register repositories for Core entities
        services.AddScoped<IRepository<Organization>, CorePgJsonRepository<Organization>>();
        services.AddScoped<IRepository<User>, CorePgJsonRepository<User>>();
        services.AddScoped<IRepository<Setting>, CorePgJsonRepository<Setting>>();
        services.AddScoped<IRepository<AccessToken>, CorePgJsonRepository<AccessToken>>();
        services.AddScoped<IRepository<RegistrationInvite>, CorePgJsonRepository<RegistrationInvite>>();
        services.AddScoped<IRepository<LogEntry>, CorePgJsonRepository<LogEntry>>();
        services.AddScoped<IRepository<SupportRequest>, CorePgJsonRepository<SupportRequest>>();
        services.AddScoped<IRepository<VehicleModel>, CorePgJsonRepository<VehicleModel>>();
        services.AddScoped<IRepository<SalesLead>, CorePgJsonRepository<SalesLead>>();
        services.AddScoped<IRepository<Feedback>, CorePgJsonRepository<Feedback>>();
        services.AddScoped<IRepository<AiUsageLog>, CorePgJsonRepository<AiUsageLog>>();

        // Register CoreDataContext (uses DI-injected repositories)
        services.AddScoped<CoreDataContext>();

        return services;
    }
}
