namespace MotoRent.Domain.Lookups;

/// <summary>
/// Malaysian states and federal territories for address selection.
/// </summary>
public static class MalaysianStates
{
    public static readonly StateInfo[] All =
    [
        // States
        new("JHR", "Johor"),
        new("KDH", "Kedah"),
        new("KTN", "Kelantan"),
        new("MLK", "Melaka"),
        new("NSN", "Negeri Sembilan"),
        new("PHG", "Pahang"),
        new("PNG", "Penang"),
        new("PRK", "Perak"),
        new("PLS", "Perlis"),
        new("SBH", "Sabah"),
        new("SWK", "Sarawak"),
        new("SGR", "Selangor"),
        new("TRG", "Terengganu"),
        // Federal Territories
        new("KUL", "Kuala Lumpur"),
        new("PJY", "Putrajaya"),
        new("LBN", "Labuan"),
    ];

    /// <summary>
    /// Tourist regions with typical rental market characteristics.
    /// </summary>
    public static readonly RegionInfo[] TouristRegions =
    [
        new("Langkawi", "KDH", "Duty-free island, European/Middle Eastern tourists"),
        new("Penang", "PNG", "Heritage city, digital nomads, Chinese tourists"),
        new("Melaka", "MLK", "UNESCO heritage, Singaporean weekenders"),
        new("Johor Bahru", "JHR", "Singapore border spillover"),
        new("Cameron Highlands", "PHG", "Hill station escapes"),
        new("Perhentian Islands", "TRG", "Diving islands"),
        new("Tioman Island", "PHG", "Diving islands"),
    ];

    public record StateInfo(string Code, string Name);
    public record RegionInfo(string Name, string StateCode, string Description);
}
