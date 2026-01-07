using System.Text.Json.Serialization;
using MotoRent.Domain.Core;

namespace MotoRent.Domain.Entities;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
// MotoRent operational entities
[JsonDerivedType(typeof(Shop), nameof(Shop))]
[JsonDerivedType(typeof(Renter), nameof(Renter))]
[JsonDerivedType(typeof(Document), nameof(Document))]
[JsonDerivedType(typeof(Motorbike), nameof(Motorbike))]
[JsonDerivedType(typeof(Rental), nameof(Rental))]
[JsonDerivedType(typeof(Deposit), nameof(Deposit))]
[JsonDerivedType(typeof(Insurance), nameof(Insurance))]
[JsonDerivedType(typeof(Accessory), nameof(Accessory))]
[JsonDerivedType(typeof(RentalAccessory), nameof(RentalAccessory))]
[JsonDerivedType(typeof(Payment), nameof(Payment))]
[JsonDerivedType(typeof(DamageReport), nameof(DamageReport))]
[JsonDerivedType(typeof(DamagePhoto), nameof(DamagePhoto))]
[JsonDerivedType(typeof(RentalAgreement), nameof(RentalAgreement))]
// Core multi-tenant entities
[JsonDerivedType(typeof(Organization), nameof(Organization))]
[JsonDerivedType(typeof(User), nameof(User))]
[JsonDerivedType(typeof(Setting), nameof(Setting))]
[JsonDerivedType(typeof(AccessToken), nameof(AccessToken))]
[JsonDerivedType(typeof(RegistrationInvite), nameof(RegistrationInvite))]
[JsonDerivedType(typeof(LogEntry), nameof(LogEntry))]
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
