namespace MotoRent.Domain.Entities;

/// <summary>
/// Constants for agent types.
/// </summary>
public static class AgentType
{
    public const string TourGuide = "TourGuide";
    public const string Hotel = "Hotel";
    public const string TravelAgency = "TravelAgency";
    public const string Concierge = "Concierge";
    public const string Resort = "Resort";
    public const string TransportCompany = "TransportCompany";
    public const string Other = "Other";

    public static readonly string[] All =
    [
        TourGuide,
        Hotel,
        TravelAgency,
        Concierge,
        Resort,
        TransportCompany,
        Other
    ];
}
