using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class MotorbikeService
{
    private readonly RentalDataContext m_context;

    public MotorbikeService(RentalDataContext context)
    {
        m_context = context;
    }

    public async Task<LoadOperation<Motorbike>> GetMotorbikesAsync(
        int shopId,
        string? searchTerm = null,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = m_context.Motorbikes
            .Where(m => m.ShopId == shopId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(m => m.Status == status);
        }

        query = query.OrderByDescending(m => m.MotorbikeId);

        var result = await m_context.LoadAsync(query, page, pageSize, includeTotalRows: true);

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
        return await m_context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == motorbikeId);
    }

    public async Task<SubmitOperation> CreateMotorbikeAsync(Motorbike motorbike, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(motorbike);
        return await session.SubmitChanges("Create");
    }

    public async Task<SubmitOperation> UpdateMotorbikeAsync(Motorbike motorbike, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(motorbike);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeleteMotorbikeAsync(Motorbike motorbike, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Delete(motorbike);
        return await session.SubmitChanges("Delete");
    }

    public async Task<SubmitOperation> UpdateStatusAsync(int motorbikeId, string status, string username)
    {
        var motorbike = await GetMotorbikeByIdAsync(motorbikeId);
        if (motorbike == null)
            return SubmitOperation.CreateFailure("Motorbike not found");

        motorbike.Status = status;

        using var session = m_context.OpenSession(username);
        session.Attach(motorbike);
        return await session.SubmitChanges("StatusUpdate");
    }

    public async Task<Dictionary<string, int>> GetStatusCountsAsync(int shopId)
    {
        var allBikes = await m_context.LoadAsync(
            m_context.Motorbikes.Where(m => m.ShopId == shopId),
            page: 1, size: 1000, includeTotalRows: false);

        return allBikes.ItemCollection
            .GroupBy(m => m.Status ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets available motorbikes for public browsing (tourist portal)
    /// </summary>
    public async Task<IEnumerable<Motorbike>> GetAvailableMotorbikesAsync(int shopId)
    {
        var result = await m_context.LoadAsync(
            m_context.Motorbikes
                .Where(m => m.ShopId == shopId)
                .Where(m => m.Status == "Available"),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection;
    }
}
