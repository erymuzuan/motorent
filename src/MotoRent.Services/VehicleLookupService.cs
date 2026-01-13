using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing global vehicle make/model lookups.
/// Uses CoreDataContext as these are shared across all tenants.
/// </summary>
public class VehicleLookupService(CoreDataContext context)
{
    private CoreDataContext Context { get; } = context;

    #region Query Operations

    /// <summary>
    /// Gets distinct make names, optionally filtered by vehicle type.
    /// </summary>
    public async Task<List<string>> GetMakesAsync(VehicleType? vehicleType = null)
    {
        var query = this.Context.VehicleModels
            .Where(m => m.IsActive);

        var result = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);

        var makes = result.ItemCollection
            .Where(m => vehicleType == null || m.VehicleType == vehicleType)
            .Select(m => m.Make)
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        return makes;
    }

    /// <summary>
    /// Gets all active models for a specific make.
    /// </summary>
    public async Task<List<VehicleModel>> GetModelsAsync(string make)
    {
        var query = this.Context.VehicleModels
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder);

        var result = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);

        return result.ItemCollection
            .Where(m => string.Equals(m.Make, make, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Gets all active models for a specific vehicle type.
    /// </summary>
    public async Task<List<VehicleModel>> GetModelsByTypeAsync(VehicleType vehicleType)
    {
        var query = this.Context.VehicleModels
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder);

        var result = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);

        return result.ItemCollection
            .Where(m => m.VehicleType == vehicleType)
            .ToList();
    }

    /// <summary>
    /// Gets all vehicle models with pagination.
    /// </summary>
    public async Task<LoadOperation<VehicleModel>> GetAllModelsAsync(
        string? searchTerm = null,
        VehicleType? vehicleType = null,
        string? make = null,
        bool includeInactive = false,
        int page = 1,
        int pageSize = 50)
    {
        var query = this.Context.VehicleModels.OrderBy(m => m.DisplayOrder);

        var result = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: true);

        var filtered = result.ItemCollection.AsEnumerable();

        if (!includeInactive)
            filtered = filtered.Where(m => m.IsActive);

        if (vehicleType.HasValue)
            filtered = filtered.Where(m => m.VehicleType == vehicleType.Value);

        if (!string.IsNullOrWhiteSpace(make))
            filtered = filtered.Where(m => string.Equals(m.Make, make, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            filtered = filtered.Where(m =>
                m.Make.ToLowerInvariant().Contains(term) ||
                m.Model.ToLowerInvariant().Contains(term) ||
                m.Aliases.Any(a => a.ToLowerInvariant().Contains(term)));
        }

        var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new LoadOperation<VehicleModel>
        {
            ItemCollection = items,
            TotalRows = filtered.Count()
        };
    }

    /// <summary>
    /// Gets a vehicle model by ID.
    /// </summary>
    public async Task<VehicleModel?> GetModelByIdAsync(int vehicleModelId)
    {
        return await this.Context.LoadOneAsync<VehicleModel>(m => m.VehicleModelId == vehicleModelId);
    }

    /// <summary>
    /// Searches for vehicle models by text (for autocomplete).
    /// </summary>
    public async Task<List<VehicleModel>> SearchAsync(string searchTerm, VehicleType? vehicleType = null, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return [];

        var query = this.Context.VehicleModels
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder);

        var result = await this.Context.LoadAsync(query, page: 1, size: 500, includeTotalRows: false);

        var term = searchTerm.ToLowerInvariant();
        var filtered = result.ItemCollection
            .Where(m => vehicleType == null || m.VehicleType == vehicleType)
            .Where(m =>
                m.Make.ToLowerInvariant().Contains(term) ||
                m.Model.ToLowerInvariant().Contains(term) ||
                m.DisplayName.ToLowerInvariant().Contains(term) ||
                m.Aliases.Any(a => a.ToLowerInvariant().Contains(term)))
            .Take(maxResults)
            .ToList();

        return filtered;
    }

    #endregion

    #region Matching Operations

    /// <summary>
    /// Matches a make+model combination against the lookup database.
    /// Uses fuzzy matching with aliases.
    /// </summary>
    public async Task<VehicleModel?> MatchAsync(string? make, string? model)
    {
        if (string.IsNullOrWhiteSpace(make) || string.IsNullOrWhiteSpace(model))
            return null;

        var query = this.Context.VehicleModels
            .Where(m => m.IsActive);

        var result = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);

        var normalizedMake = NormalizeForMatching(make);
        var normalizedModel = NormalizeForMatching(model);

        // Try exact match first
        var exactMatch = result.ItemCollection.FirstOrDefault(m =>
            NormalizeForMatching(m.Make) == normalizedMake &&
            NormalizeForMatching(m.Model) == normalizedModel);

        if (exactMatch != null)
            return exactMatch;

        // Try matching with aliases
        var aliasMatch = result.ItemCollection.FirstOrDefault(m =>
            (NormalizeForMatching(m.Make) == normalizedMake ||
             m.Aliases.Any(a => NormalizeForMatching(a) == normalizedMake)) &&
            (NormalizeForMatching(m.Model) == normalizedModel ||
             m.Aliases.Any(a => NormalizeForMatching(a) == normalizedModel)));

        if (aliasMatch != null)
            return aliasMatch;

        // Try partial match (model contains the search term)
        var partialMatch = result.ItemCollection.FirstOrDefault(m =>
            NormalizeForMatching(m.Make) == normalizedMake &&
            (m.Model.ToLowerInvariant().Contains(model.ToLowerInvariant()) ||
             m.Aliases.Any(a => a.ToLowerInvariant().Contains(model.ToLowerInvariant()))));

        return partialMatch;
    }

    /// <summary>
    /// Matches a make name against the lookup database.
    /// Returns the first matching make name found.
    /// </summary>
    public async Task<string?> MatchMakeAsync(string? make)
    {
        if (string.IsNullOrWhiteSpace(make))
            return null;

        var query = this.Context.VehicleModels
            .Where(m => m.IsActive);

        var result = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);

        var normalizedMake = NormalizeForMatching(make);

        // Find the first model with matching make
        var match = result.ItemCollection.FirstOrDefault(m =>
            NormalizeForMatching(m.Make) == normalizedMake);

        return match?.Make;
    }

    private static string NormalizeForMatching(string value)
    {
        return value
            .ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");
    }

    #endregion

    #region CRUD Operations (Super Admin)

    /// <summary>
    /// Creates a new vehicle model in the lookup database.
    /// </summary>
    public async Task<SubmitOperation> CreateModelAsync(VehicleModel vehicleModel, string username)
    {
        // Check for duplicate make+model
        var existing = await this.MatchAsync(vehicleModel.Make, vehicleModel.Model);
        if (existing != null)
            return SubmitOperation.CreateFailure($"Vehicle model '{vehicleModel.Make} {vehicleModel.Model}' already exists");

        using var session = this.Context.OpenSession(username);
        session.Attach(vehicleModel);
        return await session.SubmitChanges("Create");
    }

    /// <summary>
    /// Updates a vehicle model in the lookup database.
    /// </summary>
    public async Task<SubmitOperation> UpdateModelAsync(VehicleModel vehicleModel, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(vehicleModel);
        return await session.SubmitChanges("Update");
    }

    /// <summary>
    /// Deactivates a vehicle model (soft delete).
    /// </summary>
    public async Task<SubmitOperation> DeactivateModelAsync(int vehicleModelId, string username)
    {
        var model = await this.GetModelByIdAsync(vehicleModelId);
        if (model == null)
            return SubmitOperation.CreateFailure("Vehicle model not found");

        model.IsActive = false;

        using var session = this.Context.OpenSession(username);
        session.Attach(model);
        return await session.SubmitChanges("Deactivate");
    }

    /// <summary>
    /// Activates a vehicle model.
    /// </summary>
    public async Task<SubmitOperation> ActivateModelAsync(int vehicleModelId, string username)
    {
        var model = await this.GetModelByIdAsync(vehicleModelId);
        if (model == null)
            return SubmitOperation.CreateFailure("Vehicle model not found");

        model.IsActive = true;

        using var session = this.Context.OpenSession(username);
        session.Attach(model);
        return await session.SubmitChanges("Activate");
    }

    #endregion
}
