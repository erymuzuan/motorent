using MotoRent.Domain.Entities;
using MotoRent.Domain.Core;

namespace MotoRent.Services.Core;

/// <summary>
/// Service to resolve domain entities into a flattened dictionary for template token replacement.
/// </summary>
public interface ITemplateDataResolver
{
    /// <summary>
    /// Resolves an entity and context into a dictionary of tokens.
    /// </summary>
    /// <param name="entity">The primary entity (Booking, Rental, or Receipt).</param>
    /// <param name="organization">Current organization context.</param>
    /// <param name="staff">Current staff user context (optional).</param>
    /// <returns>Flattened dictionary where keys are token names (e.g., "Customer.Name").</returns>
    Dictionary<string, object?> Resolve(Entity entity, Organization organization, User? staff = null);
}
