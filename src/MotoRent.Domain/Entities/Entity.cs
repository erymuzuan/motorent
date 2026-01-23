using System.Text.Json.Serialization;
using MotoRent.Domain.Core;
using MotoRent.Domain.Helps;

namespace MotoRent.Domain.Entities;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
// MotoRent operational entities
[JsonDerivedType(typeof(Shop), nameof(Shop))]
[JsonDerivedType(typeof(ShopSchedule), nameof(ShopSchedule))]
[JsonDerivedType(typeof(ServiceLocation), nameof(ServiceLocation))]
[JsonDerivedType(typeof(Renter), nameof(Renter))]
[JsonDerivedType(typeof(Document), nameof(Document))]
[JsonDerivedType(typeof(Vehicle), nameof(Vehicle))]
[JsonDerivedType(typeof(VehiclePool), nameof(VehiclePool))]
[JsonDerivedType(typeof(Motorbike), nameof(Motorbike))]  // Deprecated: Use Vehicle instead
[JsonDerivedType(typeof(Rental), nameof(Rental))]
[JsonDerivedType(typeof(Booking), nameof(Booking))]
// Agent booking entities
[JsonDerivedType(typeof(Agent), nameof(Agent))]
[JsonDerivedType(typeof(AgentCommission), nameof(AgentCommission))]
[JsonDerivedType(typeof(AgentInvoice), nameof(AgentInvoice))]
[JsonDerivedType(typeof(Deposit), nameof(Deposit))]
[JsonDerivedType(typeof(Insurance), nameof(Insurance))]
[JsonDerivedType(typeof(Accessory), nameof(Accessory))]
[JsonDerivedType(typeof(RentalAccessory), nameof(RentalAccessory))]
[JsonDerivedType(typeof(Payment), nameof(Payment))]
[JsonDerivedType(typeof(DamageReport), nameof(DamageReport))]
[JsonDerivedType(typeof(DamagePhoto), nameof(DamagePhoto))]
[JsonDerivedType(typeof(RentalAgreement), nameof(RentalAgreement))]
[JsonDerivedType(typeof(VehicleImage), nameof(VehicleImage))]
// Third-party owner entities
[JsonDerivedType(typeof(VehicleOwner), nameof(VehicleOwner))]
[JsonDerivedType(typeof(OwnerPayment), nameof(OwnerPayment))]
// Maintenance entities
[JsonDerivedType(typeof(ServiceType), nameof(ServiceType))]
[JsonDerivedType(typeof(MaintenanceSchedule), nameof(MaintenanceSchedule))]
[JsonDerivedType(typeof(MaintenanceAlert), nameof(MaintenanceAlert))]
// Dynamic pricing entities
[JsonDerivedType(typeof(PricingRule), nameof(PricingRule))]
// Accident entities
[JsonDerivedType(typeof(Accident), nameof(Accident))]
[JsonDerivedType(typeof(AccidentParty), nameof(AccidentParty))]
[JsonDerivedType(typeof(AccidentDocument), nameof(AccidentDocument))]
[JsonDerivedType(typeof(AccidentCost), nameof(AccidentCost))]
[JsonDerivedType(typeof(AccidentNote), nameof(AccidentNote))]
// Core multi-tenant entities
[JsonDerivedType(typeof(Organization), nameof(Organization))]
[JsonDerivedType(typeof(User), nameof(User))]
[JsonDerivedType(typeof(Setting), nameof(Setting))]
[JsonDerivedType(typeof(AccessToken), nameof(AccessToken))]
[JsonDerivedType(typeof(RegistrationInvite), nameof(RegistrationInvite))]
[JsonDerivedType(typeof(LogEntry), nameof(LogEntry))]
// Help and support entities
[JsonDerivedType(typeof(Comment), nameof(Comment))]
[JsonDerivedType(typeof(Follow), nameof(Follow))]
[JsonDerivedType(typeof(SupportRequest), nameof(SupportRequest))]
// Global lookup entities
[JsonDerivedType(typeof(VehicleModel), nameof(VehicleModel))]
// Asset depreciation entities
[JsonDerivedType(typeof(Asset), nameof(Asset))]
[JsonDerivedType(typeof(DepreciationEntry), nameof(DepreciationEntry))]
[JsonDerivedType(typeof(AssetExpense), nameof(AssetExpense))]
[JsonDerivedType(typeof(AssetLoan), nameof(AssetLoan))]
[JsonDerivedType(typeof(AssetLoanPayment), nameof(AssetLoanPayment))]
// Till/Cashier entities
[JsonDerivedType(typeof(TillSession), nameof(TillSession))]
[JsonDerivedType(typeof(TillTransaction), nameof(TillTransaction))]
[JsonDerivedType(typeof(TillDenominationCount), nameof(TillDenominationCount))]
[JsonDerivedType(typeof(Receipt), nameof(Receipt))]
// Exchange rate entities
[JsonDerivedType(typeof(ExchangeRate), nameof(ExchangeRate))]
// End of day entities
[JsonDerivedType(typeof(DailyClose), nameof(DailyClose))]
[JsonDerivedType(typeof(ShortageLog), nameof(ShortageLog))]
public abstract class Entity
{
    public string? WebId { get; set; }

    [JsonIgnore]
    public string? CreatedBy { get; set; }

    [JsonIgnore]
    public DateTimeOffset CreatedTimestamp { get; set; }

    [JsonIgnore]
    public string? ChangedBy { get; set; }

    [JsonIgnore]
    public DateTimeOffset ChangedTimestamp { get; set; }

    public abstract int GetId();
    public abstract void SetId(int value);
}
