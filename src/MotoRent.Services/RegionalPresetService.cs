using MotoRent.Domain.Entities;
using RuleType = MotoRent.Domain.Entities.PricingRuleType;

namespace MotoRent.Services;

/// <summary>
/// Service for providing regional pricing presets for Thai tourism destinations.
/// Each preset contains seasonality patterns and events specific to the region's demographics.
/// </summary>
public class RegionalPresetService
{
    /// <summary>
    /// Regional preset options for dynamic pricing.
    /// </summary>
    public enum RegionalPreset
    {
        None,
        AndamanCoast,    // Phuket, Krabi, Phang Nga
        GulfCoast,       // Koh Samui, Koh Phangan
        SouthernBorder,  // Hat Yai, Songkhla
        Northern,        // Chiang Mai, Chiang Rai
        Eastern,         // Pattaya, Rayong
        Central,         // Bangkok
        Western,         // Hua Hin, Kanchanaburi
        Isaan            // Udon Thani, Khon Kaen
    }

    /// <summary>
    /// Information about a regional preset.
    /// </summary>
    public record PresetInfo(
        RegionalPreset Preset,
        string Name,
        string Description,
        string Category,
        int RulesCount,
        string[] PrimaryDemographics);

    /// <summary>
    /// Gets information about all available presets.
    /// </summary>
    public List<PresetInfo> GetAllPresetInfo() =>
    [
        new(RegionalPreset.None, "Start Blank", "Create rules manually without any preset", "Custom", 0, []),
        new(RegionalPreset.AndamanCoast, "Andaman Coast (Phuket, Krabi)",
            "European/Russian winter escape + Chinese/Indian markets", "Beach Destinations", 58,
            ["Russian", "European", "Chinese", "Australian"]),
        new(RegionalPreset.GulfCoast, "Gulf Coast (Koh Samui, Koh Phangan)",
            "Party island + opposite monsoon pattern", "Beach Destinations", 14,
            ["Backpackers", "Party tourists", "European"]),
        new(RegionalPreset.SouthernBorder, "Southern Border (Hat Yai, Songkhla)",
            "Malaysian weekend visitors + religious festivals", "Border & Regional", 32,
            ["Malaysian", "Singaporean"]),
        new(RegionalPreset.Northern, "Northern (Chiang Mai, Chiang Rai)",
            "Cool season focus + lantern festivals", "Metropolitan", 14,
            ["Chinese", "Japanese", "Korean", "Digital nomads"]),
        new(RegionalPreset.Eastern, "Eastern (Pattaya, Rayong)",
            "Russian + Chinese tourists year-round", "Beach Destinations", 14,
            ["Russian", "Chinese", "European"]),
        new(RegionalPreset.Central, "Central (Bangkok)",
            "Business + leisure tourism", "Metropolitan", 11,
            ["Chinese", "Business travelers"]),
        new(RegionalPreset.Western, "Western (Hua Hin, Kanchanaburi)",
            "Domestic weekenders + Bangkok escapes", "Beach Destinations", 9,
            ["Domestic Thai", "Bangkok residents"]),
        new(RegionalPreset.Isaan, "Isaan (Udon Thani, Khon Kaen)",
            "Domestic Songkran exodus", "Border & Regional", 10,
            ["Domestic Thai", "Bangkok exodus"])
    ];

    /// <summary>
    /// Gets preset info for a specific region.
    /// </summary>
    public PresetInfo GetPresetInfo(RegionalPreset preset) =>
        this.GetAllPresetInfo().First(p => p.Preset == preset);

    /// <summary>
    /// Gets all pricing rules for a regional preset.
    /// </summary>
    public List<PricingRule> GetPresetRules(RegionalPreset preset, int shopId)
    {
        var rules = preset switch
        {
            RegionalPreset.AndamanCoast => GetAndamanCoastRules(shopId),
            RegionalPreset.GulfCoast => GetGulfCoastRules(shopId),
            RegionalPreset.SouthernBorder => GetSouthernBorderRules(shopId),
            RegionalPreset.Northern => GetNorthernRules(shopId),
            RegionalPreset.Eastern => GetEasternRules(shopId),
            RegionalPreset.Central => GetCentralRules(shopId),
            RegionalPreset.Western => GetWesternRules(shopId),
            RegionalPreset.Isaan => GetIsaanRules(shopId),
            _ => []
        };

        // Set ShopId and IsActive for all rules
        foreach (var rule in rules)
        {
            rule.ShopId = shopId;
            rule.IsActive = true;
        }

        return rules;
    }

    // Andaman Coast (Phuket, Krabi) - 58 rules

    private static List<PricingRule> GetAndamanCoastRules(int shopId) =>
    [
        // === SEASONS ===
        new() { Name = "Ultra Peak Season", RuleType = RuleType.Season,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 10),
                Multiplier = 2.2m, IsRecurring = true, RecurringMonth = 12, Priority = 12,
                Description = "Christmas/NY + Russian NY convergence" },

        new() { Name = "High Season", RuleType = RuleType.Season,
                StartDate = new(2025, 11, 1), EndDate = new(2026, 3, 31),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 11, Priority = 10,
                Description = "European/Russian winter escape" },

        new() { Name = "Shoulder Season (Apr)", RuleType = RuleType.Season,
                StartDate = new(2025, 4, 1), EndDate = new(2025, 4, 30),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 4, Priority = 8,
                Description = "Songkran month - still busy" },

        new() { Name = "Shoulder Season (Oct)", RuleType = RuleType.Season,
                StartDate = new(2025, 10, 15), EndDate = new(2025, 10, 31),
                Multiplier = 1.1m, IsRecurring = true, RecurringMonth = 10, Priority = 8,
                Description = "Monsoon ending, tourists returning" },

        new() { Name = "Low Season", RuleType = RuleType.Season,
                StartDate = new(2025, 5, 1), EndDate = new(2025, 10, 14),
                Multiplier = 0.65m, IsRecurring = true, RecurringMonth = 5, Priority = 6,
                Description = "Southwest monsoon" },

        new() { Name = "Deep Low Season", RuleType = RuleType.Season,
                StartDate = new(2025, 6, 1), EndDate = new(2025, 8, 31),
                Multiplier = 0.55m, IsRecurring = true, RecurringMonth = 6, Priority = 4,
                Description = "Wettest months - heavy discounts" },

        // === RUSSIAN TOURISTS ===
        new() { Name = "Russian New Year", RuleType = RuleType.Event,
                StartDate = new(2026, 1, 1), EndDate = new(2026, 1, 14),
                Multiplier = 2.8m, IsRecurring = true, RecurringMonth = 1, Priority = 28,
                Description = "Orthodox Christmas (Jan 7) - PEAK Russian period" },

        new() { Name = "Russian Winter Break", RuleType = RuleType.Event,
                StartDate = new(2026, 1, 15), EndDate = new(2026, 1, 31),
                Multiplier = 2.0m, IsRecurring = true, RecurringMonth = 1, Priority = 22,
                Description = "School holidays continue" },

        new() { Name = "Russian Defender's Day", RuleType = RuleType.Event,
                StartDate = new(2025, 2, 22), EndDate = new(2025, 2, 24),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 2, Priority = 18,
                Description = "Feb 23 holiday + bridge days" },

        new() { Name = "Russian Women's Day", RuleType = RuleType.Event,
                StartDate = new(2025, 3, 7), EndDate = new(2025, 3, 10),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 3, Priority = 18,
                Description = "Mar 8 International Women's Day" },

        new() { Name = "Russian May Holidays", RuleType = RuleType.Event,
                StartDate = new(2025, 5, 1), EndDate = new(2025, 5, 12),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 5, Priority = 20,
                Description = "Labour Day (May 1) + Victory Day (May 9)" },

        new() { Name = "Russia Day", RuleType = RuleType.Event,
                StartDate = new(2025, 6, 11), EndDate = new(2025, 6, 14),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 6, Priority = 15,
                Description = "Jun 12 National Day + bridge" },

        new() { Name = "Russian Summer", RuleType = RuleType.Event,
                StartDate = new(2025, 7, 1), EndDate = new(2025, 8, 31),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 7, Priority = 14,
                Description = "Family holidays despite monsoon" },

        // === UK TOURISTS ===
        new() { Name = "UK Christmas/New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 5),
                Multiplier = 2.5m, IsRecurring = true, RecurringMonth = 12, Priority = 25,
                Description = "Peak British travel period" },

        new() { Name = "UK February Half-Term", RuleType = RuleType.Event,
                StartDate = new(2025, 2, 15), EndDate = new(2025, 2, 23),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 2, Priority = 18,
                Description = "1 week school break" },

        new() { Name = "UK Easter Holidays", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 5), EndDate = new(2025, 4, 21),
                Multiplier = 1.7m, IsRecurring = false, Priority = 20,
                Description = "2 week school break - dates vary yearly" },

        new() { Name = "UK May Half-Term", RuleType = RuleType.Event,
                StartDate = new(2025, 5, 24), EndDate = new(2025, 6, 1),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 5, Priority = 16,
                Description = "Even visits during monsoon start" },

        new() { Name = "UK Summer Holidays", RuleType = RuleType.Event,
                StartDate = new(2025, 7, 20), EndDate = new(2025, 9, 5),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 7, Priority = 14,
                Description = "Family travel despite monsoon" },

        new() { Name = "UK October Half-Term", RuleType = RuleType.Event,
                StartDate = new(2025, 10, 25), EndDate = new(2025, 11, 2),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 10, Priority = 17,
                Description = "Monsoon ending - popular week" },

        // === GERMAN TOURISTS ===
        new() { Name = "German Christmas", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 6),
                Multiplier = 2.3m, IsRecurring = true, RecurringMonth = 12, Priority = 24,
                Description = "Long Christmas/NY tradition" },

        new() { Name = "German Winter Escape", RuleType = RuleType.Event,
                StartDate = new(2025, 2, 1), EndDate = new(2025, 3, 15),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 2, Priority = 16,
                Description = "Ski alternative seekers" },

        new() { Name = "German Easter", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 10), EndDate = new(2025, 4, 27),
                Multiplier = 1.6m, IsRecurring = false, Priority = 18,
                Description = "School holidays - dates vary yearly" },

        new() { Name = "German Autumn Break", RuleType = RuleType.Event,
                StartDate = new(2025, 10, 1), EndDate = new(2025, 10, 20),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 10, Priority = 15,
                Description = "Herbstferien varies by state" },

        // === SCANDINAVIAN TOURISTS ===
        new() { Name = "Nordic Christmas", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 7),
                Multiplier = 2.2m, IsRecurring = true, RecurringMonth = 12, Priority = 23,
                Description = "Escaping dark Nordic winter" },

        new() { Name = "Nordic Winter Escape", RuleType = RuleType.Event,
                StartDate = new(2025, 1, 15), EndDate = new(2025, 3, 15),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 1, Priority = 17,
                Description = "Peak sun-seeking period" },

        new() { Name = "Nordic Easter", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 12), EndDate = new(2025, 4, 21),
                Multiplier = 1.5m, IsRecurring = false, Priority = 17,
                Description = "Påsk holidays - dates vary yearly" },

        new() { Name = "Nordic Autumn Break", RuleType = RuleType.Event,
                StartDate = new(2025, 10, 15), EndDate = new(2025, 11, 5),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 10, Priority = 15,
                Description = "Høstferie/Syysloma" },

        // === FRENCH TOURISTS ===
        new() { Name = "French Christmas", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 5),
                Multiplier = 2.0m, IsRecurring = true, RecurringMonth = 12, Priority = 22,
                Description = "Vacances de Noël" },

        new() { Name = "French February Break", RuleType = RuleType.Event,
                StartDate = new(2025, 2, 8), EndDate = new(2025, 3, 8),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 2, Priority = 17,
                Description = "Zone A/B/C rotation - month long waves" },

        new() { Name = "French Toussaint", RuleType = RuleType.Event,
                StartDate = new(2025, 10, 18), EndDate = new(2025, 11, 3),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 10, Priority = 16,
                Description = "All Saints school break" },

        new() { Name = "French August", RuleType = RuleType.Event,
                StartDate = new(2025, 8, 1), EndDate = new(2025, 8, 20),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 8, Priority = 17,
                Description = "Grandes vacances - monsoon doesn't stop them" },

        // === ITALIAN TOURISTS ===
        new() { Name = "Italian Christmas", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 23), EndDate = new(2026, 1, 6),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 12, Priority = 20,
                Description = "Natale to Epifania" },

        new() { Name = "Ferragosto", RuleType = RuleType.Event,
                StartDate = new(2025, 8, 10), EndDate = new(2025, 8, 20),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 8, Priority = 17,
                Description = "Peak Italian summer holiday" },

        new() { Name = "Italian Easter", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 17), EndDate = new(2025, 4, 22),
                Multiplier = 1.4m, IsRecurring = false, Priority = 16,
                Description = "Pasqua + Pasquetta - dates vary yearly" },

        // === AUSTRALIAN TOURISTS ===
        new() { Name = "Australian Summer", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 15), EndDate = new(2026, 1, 31),
                Multiplier = 2.0m, IsRecurring = true, RecurringMonth = 12, Priority = 22,
                Description = "Peak Aussie family travel" },

        new() { Name = "Australian Easter", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 12), EndDate = new(2025, 4, 27),
                Multiplier = 1.5m, IsRecurring = false, Priority = 17,
                Description = "2 week school break - dates vary yearly" },

        new() { Name = "Australian July Break", RuleType = RuleType.Event,
                StartDate = new(2025, 7, 1), EndDate = new(2025, 7, 15),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 7, Priority = 14,
                Description = "Winter school holidays" },

        new() { Name = "Australian September Break", RuleType = RuleType.Event,
                StartDate = new(2025, 9, 20), EndDate = new(2025, 10, 10),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 9, Priority = 15,
                Description = "Spring school holidays" },

        // === CHINESE TOURISTS ===
        new() { Name = "Chinese New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4),
                Multiplier = 2.2m, IsRecurring = false, Priority = 26,
                Description = "Spring Festival - dates vary (lunar)" },

        new() { Name = "CNY Eve Rush", RuleType = RuleType.Event,
                StartDate = new(2025, 1, 26), EndDate = new(2025, 1, 27),
                Multiplier = 1.8m, IsRecurring = false, Priority = 22,
                Description = "Last minute arrivals" },

        new() { Name = "Chinese Qingming", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 4), EndDate = new(2025, 4, 6),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 4, Priority = 16,
                Description = "Tomb Sweeping Day" },

        new() { Name = "Chinese Labour Day", RuleType = RuleType.Event,
                StartDate = new(2025, 5, 1), EndDate = new(2025, 5, 5),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 5, Priority = 18,
                Description = "5-day Golden Week" },

        new() { Name = "Dragon Boat Festival", RuleType = RuleType.Event,
                StartDate = new(2025, 5, 31), EndDate = new(2025, 6, 2),
                Multiplier = 1.3m, IsRecurring = false, Priority = 14,
                Description = "Duanwu Festival - dates vary (lunar)" },

        new() { Name = "Chinese National Day", RuleType = RuleType.Event,
                StartDate = new(2025, 10, 1), EndDate = new(2025, 10, 7),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 10, Priority = 20,
                Description = "7-day Golden Week" },

        new() { Name = "Mid-Autumn Festival", RuleType = RuleType.Event,
                StartDate = new(2025, 10, 6), EndDate = new(2025, 10, 8),
                Multiplier = 1.4m, IsRecurring = false, Priority = 16,
                Description = "Mooncake Festival - dates vary (lunar)" },

        // === INDIAN TOURISTS ===
        new() { Name = "Diwali", RuleType = RuleType.Event,
                StartDate = new(2025, 10, 20), EndDate = new(2025, 10, 25),
                Multiplier = 1.8m, IsRecurring = false, Priority = 20,
                Description = "Festival of Lights - dates vary" },

        new() { Name = "Indian Wedding Season", RuleType = RuleType.Event,
                StartDate = new(2025, 11, 15), EndDate = new(2026, 2, 15),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 11, Priority = 16,
                Description = "Auspicious wedding dates" },

        new() { Name = "Holi", RuleType = RuleType.Event,
                StartDate = new(2025, 3, 13), EndDate = new(2025, 3, 15),
                Multiplier = 1.3m, IsRecurring = false, Priority = 14,
                Description = "Festival of Colors - dates vary" },

        new() { Name = "Indian Summer Break", RuleType = RuleType.Event,
                StartDate = new(2025, 5, 15), EndDate = new(2025, 6, 30),
                Multiplier = 1.2m, IsRecurring = true, RecurringMonth = 5, Priority = 12,
                Description = "School summer holidays" },

        // === MIDDLE EASTERN TOURISTS ===
        new() { Name = "Eid al-Fitr", RuleType = RuleType.Event,
                StartDate = new(2025, 3, 30), EndDate = new(2025, 4, 5),
                Multiplier = 1.8m, IsRecurring = false, Priority = 20,
                Description = "End of Ramadan - dates shift yearly" },

        new() { Name = "Eid al-Adha", RuleType = RuleType.Event,
                StartDate = new(2025, 6, 6), EndDate = new(2025, 6, 12),
                Multiplier = 1.6m, IsRecurring = false, Priority = 18,
                Description = "Feast of Sacrifice - dates shift yearly" },

        new() { Name = "Gulf Summer Escape", RuleType = RuleType.Event,
                StartDate = new(2025, 7, 1), EndDate = new(2025, 8, 31),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 7, Priority = 17,
                Description = "Escaping 50°C Gulf heat" },

        new() { Name = "Saudi National Day", RuleType = RuleType.Event,
                StartDate = new(2025, 9, 23), EndDate = new(2025, 9, 25),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 9, Priority = 14,
                Description = "Saudi holiday + bridge" },

        new() { Name = "UAE National Day", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 2), EndDate = new(2025, 12, 4),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 12, Priority = 15,
                Description = "Emirates holiday" },

        // === THAI HOLIDAYS ===
        new() { Name = "Songkran Festival", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 13), EndDate = new(2025, 4, 17),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 4, Priority = 20,
                Description = "Extended Thai New Year" },

        new() { Name = "Loy Krathong", RuleType = RuleType.Event,
                StartDate = new(2025, 11, 5), EndDate = new(2025, 11, 6),
                Multiplier = 1.3m, IsRecurring = false, Priority = 14,
                Description = "Lantern festival - dates vary (lunar)" },

        new() { Name = "King's Birthday", RuleType = RuleType.Event,
                StartDate = new(2025, 7, 28), EndDate = new(2025, 7, 28),
                Multiplier = 1.2m, IsRecurring = true, RecurringMonth = 7, Priority = 12,
                Description = "National holiday" }
    ];

    
    // Gulf Coast (Koh Samui) - 14 rules

    private static List<PricingRule> GetGulfCoastRules(int shopId) =>
    [
        // === SEASONS (Opposite monsoon pattern!) ===
        new() { Name = "Peak Season", RuleType = RuleType.Season,
                StartDate = new(2025, 1, 1), EndDate = new(2025, 4, 15),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 1, Priority = 10,
                Description = "Best weather - dry and sunny" },

        new() { Name = "European Summer Peak", RuleType = RuleType.Season,
                StartDate = new(2025, 7, 1), EndDate = new(2025, 9, 15),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 7, Priority = 10,
                Description = "European school holidays" },

        new() { Name = "Shoulder Season", RuleType = RuleType.Season,
                StartDate = new(2025, 4, 16), EndDate = new(2025, 6, 30),
                Multiplier = 1.1m, IsRecurring = true, RecurringMonth = 4, Priority = 8,
                Description = "Transition period - still good weather" },

        new() { Name = "Monsoon Low Season", RuleType = RuleType.Season,
                StartDate = new(2025, 10, 15), EndDate = new(2025, 12, 15),
                Multiplier = 0.6m, IsRecurring = true, RecurringMonth = 10, Priority = 8,
                Description = "Gulf monsoon - heavy rain, ferries may cancel" },

        new() { Name = "Post-Monsoon Recovery", RuleType = RuleType.Season,
                StartDate = new(2025, 12, 16), EndDate = new(2025, 12, 31),
                Multiplier = 1.0m, IsRecurring = true, RecurringMonth = 12, Priority = 8,
                Description = "Weather improving, holiday demand" },

        // === PARTY EVENTS (Koh Phangan) ===
        new() { Name = "Full Moon Party (Jan)", RuleType = RuleType.Event,
                StartDate = new(2025, 1, 13), EndDate = new(2025, 1, 14),
                Multiplier = 2.0m, IsRecurring = false, Priority = 25,
                Description = "Monthly full moon party - update dates yearly" },

        new() { Name = "Full Moon Party (Feb)", RuleType = RuleType.Event,
                StartDate = new(2025, 2, 12), EndDate = new(2025, 2, 13),
                Multiplier = 2.0m, IsRecurring = false, Priority = 25,
                Description = "Monthly full moon party" },

        new() { Name = "Full Moon Party (Mar)", RuleType = RuleType.Event,
                StartDate = new(2025, 3, 14), EndDate = new(2025, 3, 15),
                Multiplier = 2.0m, IsRecurring = false, Priority = 25,
                Description = "Monthly full moon party" },

        new() { Name = "NYE Full Moon Combo", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 29), EndDate = new(2026, 1, 2),
                Multiplier = 2.5m, IsRecurring = true, RecurringMonth = 12, Priority = 28,
                Description = "New Year + Full Moon alignment - MASSIVE" },

        // === STANDARD EVENTS ===
        new() { Name = "Christmas & New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 5),
                Multiplier = 2.0m, IsRecurring = true, RecurringMonth = 12, Priority = 22,
                Description = "Busy despite monsoon tail-end" },

        new() { Name = "Songkran Beach Party", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 12), EndDate = new(2025, 4, 16),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 4, Priority = 20,
                Description = "Extended Songkran celebrations" },

        new() { Name = "Chinese New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4),
                Multiplier = 1.6m, IsRecurring = false, Priority = 18,
                Description = "Chinese tourists - dates vary (lunar)" },

        new() { Name = "Ten Stars Samui Festival", RuleType = RuleType.Event,
                StartDate = new(2025, 9, 1), EndDate = new(2025, 9, 10),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 9, Priority = 15,
                Description = "Local food and culture festival" }
    ];

    
    // Southern Border (Hat Yai) - 32 rules

    private static List<PricingRule> GetSouthernBorderRules(int shopId) =>
    [
        // === BASE WEEKEND PATTERN ===
        new() { Name = "Weekend (Sat)", RuleType = RuleType.DayOfWeek,
                ApplicableDayOfWeek = DayOfWeek.Saturday,
                StartDate = DateOnly.MinValue, EndDate = new(2099, 12, 31),
                Multiplier = 1.15m, Priority = 8,
                Description = "Regular weekend traffic" },

        new() { Name = "Weekend (Sun)", RuleType = RuleType.DayOfWeek,
                ApplicableDayOfWeek = DayOfWeek.Sunday,
                StartDate = DateOnly.MinValue, EndDate = new(2099, 12, 31),
                Multiplier = 1.15m, Priority = 8,
                Description = "Regular weekend traffic" },

        new() { Name = "Friday Premium", RuleType = RuleType.DayOfWeek,
                ApplicableDayOfWeek = DayOfWeek.Friday,
                StartDate = DateOnly.MinValue, EndDate = new(2099, 12, 31),
                Multiplier = 1.2m, Priority = 10,
                Description = "Weekend starters arrive Friday" },

        // === MALAYSIAN SCHOOL HOLIDAYS ===
        new() { Name = "MY School Break (Mar)", RuleType = RuleType.Event,
                StartDate = new(2025, 3, 14), EndDate = new(2025, 3, 23),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 3, Priority = 20,
                Description = "Term 1 mid-break - 10 days" },

        new() { Name = "MY School Break (Jun)", RuleType = RuleType.Event,
                StartDate = new(2025, 5, 24), EndDate = new(2025, 6, 8),
                Multiplier = 1.7m, IsRecurring = true, RecurringMonth = 5, Priority = 22,
                Description = "Term 2 break - MAJOR family travel period" },

        new() { Name = "MY School Break (Aug)", RuleType = RuleType.Event,
                StartDate = new(2025, 8, 16), EndDate = new(2025, 8, 24),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 8, Priority = 18,
                Description = "Term 3 mid-break" },

        new() { Name = "MY Year-End Holiday", RuleType = RuleType.Event,
                StartDate = new(2025, 11, 22), EndDate = new(2026, 1, 1),
                Multiplier = 1.9m, IsRecurring = true, RecurringMonth = 11, Priority = 24,
                Description = "6 weeks - BIGGEST travel period for Hat Yai" },

        // === MAJOR RELIGIOUS FESTIVALS ===
        new() { Name = "Hari Raya Aidilfitri", RuleType = RuleType.Event,
                StartDate = new(2025, 3, 28), EndDate = new(2025, 4, 7),
                Multiplier = 3.0m, IsRecurring = false, Priority = 30,
                Description = "EID - THE BIGGEST EVENT. Dates shift ~11 days/year" },

        new() { Name = "Hari Raya Aidilfitri Eve", RuleType = RuleType.Event,
                StartDate = new(2025, 3, 26), EndDate = new(2025, 3, 27),
                Multiplier = 2.5m, IsRecurring = false, Priority = 28,
                Description = "Pre-Raya rush - last minute shopping" },

        new() { Name = "Chinese New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 3),
                Multiplier = 2.5m, IsRecurring = false, Priority = 28,
                Description = "CNY week - massive Malaysian Chinese crowds" },

        new() { Name = "Chinese New Year Eve", RuleType = RuleType.Event,
                StartDate = new(2025, 1, 26), EndDate = new(2025, 1, 27),
                Multiplier = 2.0m, IsRecurring = false, Priority = 24,
                Description = "Pre-CNY shopping rush" },

        new() { Name = "Hari Raya Haji", RuleType = RuleType.Event,
                StartDate = new(2025, 6, 6), EndDate = new(2025, 6, 10),
                Multiplier = 2.0m, IsRecurring = false, Priority = 24,
                Description = "Eid al-Adha - 4-5 day break" },

        new() { Name = "Deepavali", RuleType = RuleType.Event,
                StartDate = new(2025, 10, 18), EndDate = new(2025, 10, 22),
                Multiplier = 1.8m, IsRecurring = false, Priority = 20,
                Description = "Diwali - Hindu festival of lights" },

        new() { Name = "Thaipusam", RuleType = RuleType.Event,
                StartDate = new(2025, 2, 10), EndDate = new(2025, 2, 12),
                Multiplier = 1.6m, IsRecurring = false, Priority = 18,
                Description = "Hindu pilgrimage festival - big in Penang" },

        // === PUBLIC HOLIDAY LONG WEEKENDS ===
        new() { Name = "New Year Long Weekend", RuleType = RuleType.Event,
                StartDate = new(2024, 12, 31), EndDate = new(2025, 1, 2),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 12, Priority = 20,
                Description = "NYE + New Year holiday" },

        new() { Name = "Federal Territory Day", RuleType = RuleType.Event,
                StartDate = new(2025, 2, 1), EndDate = new(2025, 2, 2),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 2, Priority = 16,
                Description = "KL/Putrajaya residents escape" },

        new() { Name = "Labour Day Weekend", RuleType = RuleType.Event,
                StartDate = new(2025, 5, 1), EndDate = new(2025, 5, 4),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 5, Priority = 17,
                Description = "May Day long weekend" },

        new() { Name = "Agong Birthday", RuleType = RuleType.Event,
                StartDate = new(2025, 6, 7), EndDate = new(2025, 6, 9),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 6, Priority = 17,
                Description = "King's birthday - first Monday of June" },

        new() { Name = "Merdeka Day Weekend", RuleType = RuleType.Event,
                StartDate = new(2025, 8, 30), EndDate = new(2025, 9, 1),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 8, Priority = 18,
                Description = "Malaysian Independence Day" },

        new() { Name = "Malaysia Day Weekend", RuleType = RuleType.Event,
                StartDate = new(2025, 9, 13), EndDate = new(2025, 9, 16),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 9, Priority = 18,
                Description = "Malaysia formation anniversary" },

        new() { Name = "Christmas Weekend", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 24), EndDate = new(2025, 12, 26),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 12, Priority = 18,
                Description = "Christmas + year-end school overlap" },

        // === NORTHERN MALAYSIAN STATE HOLIDAYS ===
        new() { Name = "Penang Heritage Day", RuleType = RuleType.Event,
                StartDate = new(2025, 7, 7), EndDate = new(2025, 7, 7),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 7, Priority = 14,
                Description = "Penang state holiday" },

        new() { Name = "Sultan Kedah Birthday", RuleType = RuleType.Event,
                StartDate = new(2025, 6, 15), EndDate = new(2025, 6, 16),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 6, Priority = 14,
                Description = "Kedah state holiday" },

        // === SINGAPOREAN SPILLOVER ===
        new() { Name = "Singapore National Day", RuleType = RuleType.Event,
                StartDate = new(2025, 8, 9), EndDate = new(2025, 8, 11),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 8, Priority = 17,
                Description = "Singaporean independence long weekend" },

        new() { Name = "SG June Holidays", RuleType = RuleType.Event,
                StartDate = new(2025, 5, 31), EndDate = new(2025, 6, 29),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 5, Priority = 16,
                Description = "Singapore school mid-year break" },

        new() { Name = "SG Year-End", RuleType = RuleType.Event,
                StartDate = new(2025, 11, 15), EndDate = new(2025, 12, 31),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 11, Priority = 18,
                Description = "Singapore school + Christmas season" },

        // === THAI HOLIDAYS (Secondary) ===
        new() { Name = "Songkran", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 13), EndDate = new(2025, 4, 15),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 4, Priority = 16,
                Description = "Thai New Year - some Malaysian visitors too" }
    ];

    
    // Northern (Chiang Mai) - 14 rules

    private static List<PricingRule> GetNorthernRules(int shopId) =>
    [
        // === SEASONS ===
        new() { Name = "Cool Season Peak", RuleType = RuleType.Season,
                StartDate = new(2025, 11, 15), EndDate = new(2026, 2, 15),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 11, Priority = 10,
                Description = "Best weather, festival season" },

        new() { Name = "Hot/Burning Season", RuleType = RuleType.Season,
                StartDate = new(2025, 2, 16), EndDate = new(2025, 4, 30),
                Multiplier = 0.9m, IsRecurring = true, RecurringMonth = 2, Priority = 8,
                Description = "Haze from crop burning - some tourists avoid" },

        new() { Name = "Green/Rainy Season", RuleType = RuleType.Season,
                StartDate = new(2025, 6, 1), EndDate = new(2025, 10, 31),
                Multiplier = 0.75m, IsRecurring = true, RecurringMonth = 6, Priority = 8,
                Description = "Rainy but lush scenery - budget travelers" },

        new() { Name = "Post-Monsoon", RuleType = RuleType.Season,
                StartDate = new(2025, 11, 1), EndDate = new(2025, 11, 14),
                Multiplier = 1.2m, IsRecurring = true, RecurringMonth = 11, Priority = 8,
                Description = "Weather improving, pre-festival" },

        // === MAJOR EVENTS ===
        new() { Name = "Yi Peng Lantern Festival", RuleType = RuleType.Event,
                StartDate = new(2025, 11, 5), EndDate = new(2025, 11, 7),
                Multiplier = 2.5m, IsRecurring = false, Priority = 28,
                Description = "ICONIC - floating lanterns. Dates vary (lunar)" },

        new() { Name = "Songkran (Chiang Mai)", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 12), EndDate = new(2025, 4, 16),
                Multiplier = 2.2m, IsRecurring = true, RecurringMonth = 4, Priority = 25,
                Description = "Famous for best Songkran in Thailand" },

        new() { Name = "Chinese New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4),
                Multiplier = 2.0m, IsRecurring = false, Priority = 22,
                Description = "Large Chinese-Thai population + mainland tourists" },

        new() { Name = "Christmas & New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 5),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 12, Priority = 20,
                Description = "Western tourist peak" },

        // === ASIAN TOURIST EVENTS ===
        new() { Name = "Chinese Golden Week", RuleType = RuleType.Event,
                StartDate = new(2025, 10, 1), EndDate = new(2025, 10, 7),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 10, Priority = 20,
                Description = "Mainland Chinese tourists" },

        new() { Name = "Japanese Golden Week", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 29), EndDate = new(2025, 5, 5),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 4, Priority = 18,
                Description = "Japanese tourists" },

        new() { Name = "Korean Chuseok", RuleType = RuleType.Event,
                StartDate = new(2025, 10, 5), EndDate = new(2025, 10, 8),
                Multiplier = 1.5m, IsRecurring = false, Priority = 16,
                Description = "Korean thanksgiving - dates vary (lunar)" },

        // === LOCAL FESTIVALS ===
        new() { Name = "Flower Festival", RuleType = RuleType.Event,
                StartDate = new(2025, 2, 7), EndDate = new(2025, 2, 9),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 2, Priority = 15,
                Description = "Chiang Mai specialty - first weekend Feb" },

        new() { Name = "Bo Sang Umbrella Fest", RuleType = RuleType.Event,
                StartDate = new(2025, 1, 17), EndDate = new(2025, 1, 19),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 1, Priority = 14,
                Description = "San Kamphaeng - third weekend Jan" }
    ];

    
    // Eastern (Pattaya) - 14 rules

    private static List<PricingRule> GetEasternRules(int shopId) =>
    [
        // === SEASONS ===
        new() { Name = "High Season", RuleType = RuleType.Season,
                StartDate = new(2025, 11, 1), EndDate = new(2026, 3, 31),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 11, Priority = 10,
                Description = "Russian and European winter escape" },

        new() { Name = "Shoulder Season", RuleType = RuleType.Season,
                StartDate = new(2025, 4, 1), EndDate = new(2025, 5, 31),
                Multiplier = 1.2m, IsRecurring = true, RecurringMonth = 4, Priority = 8,
                Description = "Transition period" },

        new() { Name = "Low Season", RuleType = RuleType.Season,
                StartDate = new(2025, 6, 1), EndDate = new(2025, 10, 31),
                Multiplier = 0.7m, IsRecurring = true, RecurringMonth = 6, Priority = 6,
                Description = "Monsoon season" },

        // === RUSSIAN TOURISTS ===
        new() { Name = "Russian New Year", RuleType = RuleType.Event,
                StartDate = new(2026, 1, 1), EndDate = new(2026, 1, 14),
                Multiplier = 2.5m, IsRecurring = true, RecurringMonth = 1, Priority = 28,
                Description = "Orthodox Christmas - major Russian destination" },

        new() { Name = "Russian May Holidays", RuleType = RuleType.Event,
                StartDate = new(2025, 5, 1), EndDate = new(2025, 5, 12),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 5, Priority = 20,
                Description = "Labour + Victory Day cluster" },

        // === CHINESE TOURISTS ===
        new() { Name = "Chinese New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4),
                Multiplier = 2.2m, IsRecurring = false, Priority = 26,
                Description = "Major Chinese tourist destination" },

        new() { Name = "Chinese National Day", RuleType = RuleType.Event,
                StartDate = new(2025, 10, 1), EndDate = new(2025, 10, 7),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 10, Priority = 20,
                Description = "7-day Golden Week" },

        // === EVENTS ===
        new() { Name = "Christmas & New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 5),
                Multiplier = 2.0m, IsRecurring = true, RecurringMonth = 12, Priority = 22,
                Description = "Peak Western tourist period" },

        new() { Name = "Songkran", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 13), EndDate = new(2025, 4, 17),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 4, Priority = 20,
                Description = "Thai New Year - beach parties" },

        new() { Name = "Pattaya Music Festival", RuleType = RuleType.Event,
                StartDate = new(2025, 3, 21), EndDate = new(2025, 3, 23),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 3, Priority = 18,
                Description = "Annual music festival" },

        // === WEEKEND PREMIUM ===
        new() { Name = "Weekend Premium", RuleType = RuleType.DayOfWeek,
                ApplicableDayOfWeek = DayOfWeek.Saturday,
                StartDate = DateOnly.MinValue, EndDate = new(2099, 12, 31),
                Multiplier = 1.1m, Priority = 5,
                Description = "Bangkok weekend escapes" }
    ];

    
    // Central (Bangkok) - 11 rules

    private static List<PricingRule> GetCentralRules(int shopId) =>
    [
        // === SEASONS ===
        new() { Name = "Cool Season", RuleType = RuleType.Season,
                StartDate = new(2025, 11, 1), EndDate = new(2026, 2, 28),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 11, Priority = 10,
                Description = "Best weather for Bangkok" },

        new() { Name = "Hot Season", RuleType = RuleType.Season,
                StartDate = new(2025, 3, 1), EndDate = new(2025, 5, 31),
                Multiplier = 1.0m, IsRecurring = true, RecurringMonth = 3, Priority = 8,
                Description = "Hot but still busy" },

        new() { Name = "Rainy Season", RuleType = RuleType.Season,
                StartDate = new(2025, 6, 1), EndDate = new(2025, 10, 31),
                Multiplier = 0.8m, IsRecurring = true, RecurringMonth = 6, Priority = 6,
                Description = "Monsoon - afternoon showers" },

        // === MAJOR EVENTS ===
        new() { Name = "Chinese New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4),
                Multiplier = 2.0m, IsRecurring = false, Priority = 26,
                Description = "Chinatown celebrations" },

        new() { Name = "Chinese Golden Week", RuleType = RuleType.Event,
                StartDate = new(2025, 10, 1), EndDate = new(2025, 10, 7),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 10, Priority = 20,
                Description = "Mainland Chinese tourists" },

        new() { Name = "Songkran", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 13), EndDate = new(2025, 4, 17),
                Multiplier = 1.6m, IsRecurring = true, RecurringMonth = 4, Priority = 20,
                Description = "Thai New Year - city-wide" },

        new() { Name = "Christmas & New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 5),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 12, Priority = 18,
                Description = "Western tourist peak" },

        new() { Name = "Loy Krathong", RuleType = RuleType.Event,
                StartDate = new(2025, 11, 5), EndDate = new(2025, 11, 6),
                Multiplier = 1.3m, IsRecurring = false, Priority = 14,
                Description = "Lantern festival along Chao Phraya" }
    ];

    
    // Western (Hua Hin) - 9 rules

    private static List<PricingRule> GetWesternRules(int shopId) =>
    [
        // === SEASONS ===
        new() { Name = "Cool Season", RuleType = RuleType.Season,
                StartDate = new(2025, 11, 1), EndDate = new(2026, 2, 28),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 11, Priority = 10,
                Description = "Best weather" },

        new() { Name = "Low Season", RuleType = RuleType.Season,
                StartDate = new(2025, 6, 1), EndDate = new(2025, 10, 31),
                Multiplier = 0.75m, IsRecurring = true, RecurringMonth = 6, Priority = 6,
                Description = "Rainy season" },

        // === WEEKEND PREMIUM (Bangkok escapes) ===
        new() { Name = "Friday Premium", RuleType = RuleType.DayOfWeek,
                ApplicableDayOfWeek = DayOfWeek.Friday,
                StartDate = DateOnly.MinValue, EndDate = new(2099, 12, 31),
                Multiplier = 1.2m, Priority = 10,
                Description = "Bangkok weekend starters" },

        new() { Name = "Weekend Premium", RuleType = RuleType.DayOfWeek,
                ApplicableDayOfWeek = DayOfWeek.Saturday,
                StartDate = DateOnly.MinValue, EndDate = new(2099, 12, 31),
                Multiplier = 1.25m, Priority = 8,
                Description = "Peak Bangkok escape day" },

        // === EVENTS ===
        new() { Name = "Christmas & New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 20), EndDate = new(2026, 1, 5),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 12, Priority = 22,
                Description = "European retirees + Thai elite" },

        new() { Name = "Songkran", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 13), EndDate = new(2025, 4, 15),
                Multiplier = 1.5m, IsRecurring = true, RecurringMonth = 4, Priority = 18,
                Description = "Thai New Year" },

        new() { Name = "Hua Hin Jazz Festival", RuleType = RuleType.Event,
                StartDate = new(2025, 6, 13), EndDate = new(2025, 6, 15),
                Multiplier = 1.4m, IsRecurring = true, RecurringMonth = 6, Priority = 16,
                Description = "Annual jazz on the beach" }
    ];

    
    // Isaan (Udon Thani) - 10 rules

    private static List<PricingRule> GetIsaanRules(int shopId) =>
    [
        // === SEASONS (Domestic focus) ===
        new() { Name = "Cool Season", RuleType = RuleType.Season,
                StartDate = new(2025, 11, 1), EndDate = new(2026, 2, 28),
                Multiplier = 1.2m, IsRecurring = true, RecurringMonth = 11, Priority = 10,
                Description = "Pleasant weather for domestic tourists" },

        // === MAJOR EVENTS ===
        new() { Name = "Songkran Homecoming", RuleType = RuleType.Event,
                StartDate = new(2025, 4, 10), EndDate = new(2025, 4, 17),
                Multiplier = 2.5m, IsRecurring = true, RecurringMonth = 4, Priority = 30,
                Description = "THE biggest event - Bangkok exodus returns home" },

        new() { Name = "Chinese New Year", RuleType = RuleType.Event,
                StartDate = new(2025, 1, 28), EndDate = new(2025, 2, 4),
                Multiplier = 1.5m, IsRecurring = false, Priority = 18,
                Description = "Thai-Chinese population" },

        new() { Name = "New Year Homecoming", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 28), EndDate = new(2026, 1, 3),
                Multiplier = 1.8m, IsRecurring = true, RecurringMonth = 12, Priority = 22,
                Description = "Family reunions" },

        // === LOCAL FESTIVALS ===
        new() { Name = "Udon Thani Red Lotus Sea", RuleType = RuleType.Event,
                StartDate = new(2025, 12, 1), EndDate = new(2026, 2, 28),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 12, Priority = 14,
                Description = "Famous red lotus lake tourism" },

        new() { Name = "Rocket Festival (Bun Bang Fai)", RuleType = RuleType.Event,
                StartDate = new(2025, 5, 10), EndDate = new(2025, 5, 12),
                Multiplier = 1.4m, IsRecurring = false, Priority = 16,
                Description = "Traditional rocket festival - dates vary" },

        new() { Name = "Candle Festival (Ubon)", RuleType = RuleType.Event,
                StartDate = new(2025, 7, 11), EndDate = new(2025, 7, 13),
                Multiplier = 1.3m, IsRecurring = false, Priority = 14,
                Description = "Buddhist Lent candle parade - dates vary" },

        // === LONG WEEKEND PREMIUM ===
        new() { Name = "Long Weekend", RuleType = RuleType.Event,
                StartDate = new(2025, 5, 1), EndDate = new(2025, 5, 5),
                Multiplier = 1.3m, IsRecurring = true, RecurringMonth = 5, Priority = 12,
                Description = "Labour Day + Coronation Day" }
    ];

    }
