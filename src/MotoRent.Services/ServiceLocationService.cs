using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing predefined pick-up/drop-off service locations.
/// </summary>
public class ServiceLocationService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    #region Query Methods

    /// <summary>
    /// Gets all active service locations for a shop.
    /// </summary>
    public async Task<List<ServiceLocation>> GetActiveLocationsAsync(
        int shopId,
        bool? pickupOnly = null,
        bool? dropoffOnly = null)
    {
        var query = this.Context.CreateQuery<ServiceLocation>()
            .Where(l => l.ShopId == shopId && l.IsActive);

        var result = await this.Context.LoadAsync(query, page: 1, size: 100, includeTotalRows: false);

        var locations = result.ItemCollection
            .OrderBy(l => l.DisplayOrder)
            .ThenBy(l => l.Name)
            .ToList();

        if (pickupOnly == true)
            locations = locations.Where(l => l.PickupAvailable).ToList();

        if (dropoffOnly == true)
            locations = locations.Where(l => l.DropoffAvailable).ToList();

        return locations;
    }

    /// <summary>
    /// Gets all service locations for a shop (including inactive).
    /// </summary>
    public async Task<LoadOperation<ServiceLocation>> GetLocationsAsync(
        int shopId,
        string? searchTerm = null,
        ServiceLocationType? locationType = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<ServiceLocation>()
            .Where(l => l.ShopId == shopId);

        if (locationType.HasValue)
            query = query.Where(l => l.LocationType == locationType.Value);

        if (isActive.HasValue)
            query = query.Where(l => l.IsActive == isActive.Value);

        query = query.OrderBy(l => l.DisplayOrder).ThenBy(l => l.Name);

        var result = await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);

        // Apply search term filter in memory
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            result.ItemCollection = result.ItemCollection
                .Where(l =>
                    (l.Name?.ToLowerInvariant().Contains(term) ?? false) ||
                    (l.Address?.ToLowerInvariant().Contains(term) ?? false))
                .ToList();
        }

        return result;
    }

    /// <summary>
    /// Gets locations grouped by type for display.
    /// </summary>
    public async Task<Dictionary<ServiceLocationType, List<ServiceLocation>>> GetLocationsGroupedByTypeAsync(
        int shopId,
        bool activeOnly = true)
    {
        var locations = activeOnly
            ? await this.GetActiveLocationsAsync(shopId)
            : (await this.GetLocationsAsync(shopId, isActive: null, page: 1, pageSize: 100)).ItemCollection;

        return locations
            .GroupBy(l => l.LocationType)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Gets a single location by ID.
    /// </summary>
    public async Task<ServiceLocation?> GetLocationByIdAsync(int locationId)
    {
        return await this.Context.LoadOneAsync<ServiceLocation>(l => l.ServiceLocationId == locationId);
    }

    /// <summary>
    /// Gets locations available for pickup.
    /// </summary>
    public async Task<List<ServiceLocation>> GetPickupLocationsAsync(int shopId)
    {
        return await this.GetActiveLocationsAsync(shopId, pickupOnly: true);
    }

    /// <summary>
    /// Gets locations available for drop-off.
    /// </summary>
    public async Task<List<ServiceLocation>> GetDropoffLocationsAsync(int shopId)
    {
        return await this.GetActiveLocationsAsync(shopId, dropoffOnly: true);
    }

    #endregion

    #region CRUD Operations

    /// <summary>
    /// Creates a new service location.
    /// </summary>
    public async Task<SubmitOperation> CreateLocationAsync(ServiceLocation location, string username)
    {
        // Set display order to end if not specified
        if (location.DisplayOrder == 0)
        {
            var existingCount = await this.GetLocationCountAsync(location.ShopId);
            location.DisplayOrder = existingCount + 1;
        }

        using var session = this.Context.OpenSession(username);
        session.Attach(location);
        return await session.SubmitChanges("Create");
    }

    /// <summary>
    /// Updates an existing service location.
    /// </summary>
    public async Task<SubmitOperation> UpdateLocationAsync(ServiceLocation location, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(location);
        return await session.SubmitChanges("Update");
    }

    /// <summary>
    /// Deletes a service location.
    /// </summary>
    public async Task<SubmitOperation> DeleteLocationAsync(ServiceLocation location, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(location);
        return await session.SubmitChanges("Delete");
    }

    /// <summary>
    /// Sets the active status of a location.
    /// </summary>
    public async Task<SubmitOperation> SetActiveStatusAsync(int locationId, bool isActive, string username)
    {
        var location = await this.GetLocationByIdAsync(locationId);

        if (location == null)
            return SubmitOperation.CreateFailure("Location not found");

        location.IsActive = isActive;
        return await this.UpdateLocationAsync(location, username);
    }

    /// <summary>
    /// Updates the display order of locations.
    /// </summary>
    public async Task<SubmitOperation> UpdateDisplayOrderAsync(
        IEnumerable<(int LocationId, int DisplayOrder)> orders,
        string username)
    {
        using var session = this.Context.OpenSession(username);

        foreach (var (locationId, displayOrder) in orders)
        {
            var location = await this.GetLocationByIdAsync(locationId);

            if (location != null)
            {
                location.DisplayOrder = displayOrder;
                session.Attach(location);
            }
        }

        return await session.SubmitChanges("ReorderLocations");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the count of locations for a shop.
    /// </summary>
    public async Task<int> GetLocationCountAsync(int shopId)
    {
        var query = this.Context.CreateQuery<ServiceLocation>()
            .Where(l => l.ShopId == shopId);
        return await this.Context.GetCountAsync(query);
    }

    /// <summary>
    /// Checks if a location name already exists for a shop.
    /// </summary>
    public async Task<bool> LocationNameExistsAsync(int shopId, string name, int? excludeLocationId = null)
    {
        var existing = await this.Context.LoadOneAsync<ServiceLocation>(
            l => l.ShopId == shopId && l.Name == name);

        if (existing == null)
            return false;

        if (excludeLocationId.HasValue && existing.ServiceLocationId == excludeLocationId.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Creates common default locations for a shop (Airport, etc.).
    /// </summary>
    public async Task<SubmitOperation> CreateDefaultLocationsAsync(
        int shopId,
        string shopLocation,
        string username)
    {
        var defaultLocations = GetDefaultLocationsForArea(shopLocation);

        using var session = this.Context.OpenSession(username);
        var order = 1;

        foreach (var location in defaultLocations)
        {
            location.ShopId = shopId;
            location.DisplayOrder = order++;
            session.Attach(location);
        }

        return await session.SubmitChanges("CreateDefaults");
    }

    /// <summary>
    /// Gets suggested default locations based on area.
    /// </summary>
    private static List<ServiceLocation> GetDefaultLocationsForArea(string area)
    {
        var lowerArea = area?.ToLowerInvariant() ?? "";

        // Phuket locations
        if (lowerArea.Contains("phuket"))
        {
            return
            [
                new ServiceLocation
                {
                    Name = "Phuket International Airport",
                    LocationType = ServiceLocationType.Airport,
                    Address = "Mai Khao, Thalang District, Phuket 83110",
                    PickupFee = 300,
                    DropoffFee = 200,
                    EstimatedTravelMinutes = 45,
                    StaffNotes = "Meet at arrivals hall exit"
                },
                new ServiceLocation
                {
                    Name = "Patong Beach Hotels",
                    LocationType = ServiceLocationType.Hotel,
                    Address = "Patong Beach Area",
                    PickupFee = 150,
                    DropoffFee = 100,
                    EstimatedTravelMinutes = 20
                },
                new ServiceLocation
                {
                    Name = "Kata/Karon Beach Hotels",
                    LocationType = ServiceLocationType.Hotel,
                    Address = "Kata/Karon Beach Area",
                    PickupFee = 200,
                    DropoffFee = 150,
                    EstimatedTravelMinutes = 30
                },
                new ServiceLocation
                {
                    Name = "Rassada Pier",
                    LocationType = ServiceLocationType.FerryTerminal,
                    Address = "Rassada, Mueang Phuket District",
                    PickupFee = 150,
                    DropoffFee = 100,
                    EstimatedTravelMinutes = 25,
                    StaffNotes = "Ferry to Phi Phi Islands"
                }
            ];
        }

        // Krabi locations
        if (lowerArea.Contains("krabi"))
        {
            return
            [
                new ServiceLocation
                {
                    Name = "Krabi International Airport",
                    LocationType = ServiceLocationType.Airport,
                    Address = "Nuea Khlong District, Krabi 81130",
                    PickupFee = 300,
                    DropoffFee = 200,
                    EstimatedTravelMinutes = 30
                },
                new ServiceLocation
                {
                    Name = "Ao Nang Beach Hotels",
                    LocationType = ServiceLocationType.Hotel,
                    Address = "Ao Nang, Mueang Krabi District",
                    PickupFee = 150,
                    DropoffFee = 100,
                    EstimatedTravelMinutes = 20
                },
                new ServiceLocation
                {
                    Name = "Klong Jilad Pier",
                    LocationType = ServiceLocationType.FerryTerminal,
                    Address = "Krabi Town",
                    PickupFee = 100,
                    DropoffFee = 100,
                    EstimatedTravelMinutes = 15
                }
            ];
        }

        // Default generic locations
        return
        [
            new ServiceLocation
            {
                Name = "Airport",
                LocationType = ServiceLocationType.Airport,
                PickupFee = 300,
                DropoffFee = 200,
                EstimatedTravelMinutes = 30
            },
            new ServiceLocation
            {
                Name = "Hotel Delivery",
                LocationType = ServiceLocationType.Hotel,
                PickupFee = 150,
                DropoffFee = 100,
                EstimatedTravelMinutes = 20
            }
        ];
    }

    #endregion
}
