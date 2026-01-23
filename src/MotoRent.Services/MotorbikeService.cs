using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class MotorbikeService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    public async Task<LoadOperation<Motorbike>> GetMotorbikesAsync(
        string? searchTerm = null,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<Motorbike>();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(m => m.Status == status);
        }

        query = query.OrderByDescending(m => m.MotorbikeId);

        var result = await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);

        // Apply search term filter in memory (for brand/model/license plate)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            result.ItemCollection = result.ItemCollection
                .Where(m =>
                    (m.LicensePlate?.ToLowerInvariant().Contains(term) ?? false) ||
                    (m.Brand?.ToLowerInvariant().Contains(term) ?? false) ||
                    (m.Model?.ToLowerInvariant().Contains(term) ?? false))
                .ToList();
        }

        return result;
    }

    public async Task<Motorbike?> GetMotorbikeByIdAsync(int motorbikeId)
    {
        return await this.Context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == motorbikeId);
    }

    public async Task<SubmitOperation> CreateMotorbikeAsync(Motorbike motorbike, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(motorbike);
        return await session.SubmitChanges("CreateMotorbike");
    }

    public async Task<SubmitOperation> UpdateMotorbikeAsync(Motorbike motorbike, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(motorbike);
        return await session.SubmitChanges("UpdateMotorbike");
    }

    public async Task<SubmitOperation> DeleteMotorbikeAsync(Motorbike motorbike, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(motorbike);
        return await session.SubmitChanges("DeleteMotorbike");
    }

    public async Task<SubmitOperation> UpdateStatusAsync(int motorbikeId, string status, string username)
    {
        var motorbike = await this.GetMotorbikeByIdAsync(motorbikeId);
        if (motorbike == null)
            return SubmitOperation.CreateFailure("Motorbike not found");

        motorbike.Status = status;

        using var session = this.Context.OpenSession(username);
        session.Attach(motorbike);
        return await session.SubmitChanges("StatusUpdate");
    }

    public async Task<Dictionary<string, int>> GetStatusCountsAsync()
    {
        var groupCounts = await this.Context.GetGroupByCountAsync(
            this.Context.CreateQuery<Motorbike>(),
            m => m.Status ?? "Unknown");

        return groupCounts.ToDictionary(g => g.Key, g => g.Count);
    }

    /// <summary>
    /// Gets available motorbikes for public browsing (tourist portal)
    /// </summary>
    public async Task<IEnumerable<Motorbike>> GetAvailableMotorbikesAsync()
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<Motorbike>()
                .Where(m => m.Status == "Available"),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection;
    }
}
