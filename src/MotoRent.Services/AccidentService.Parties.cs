using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Accident party management operations.
/// </summary>
public partial class AccidentService
{
    public async Task<List<AccidentParty>> GetPartiesAsync(int accidentId)
    {
        var query = this.Context.CreateQuery<AccidentParty>()
            .Where(p => p.AccidentId == accidentId)
            .OrderBy(p => p.AccidentPartyId);

        var result = await this.Context.LoadAsync(query, 1, 100, false);
        return result.ItemCollection;
    }

    public async Task<SubmitOperation> SavePartyAsync(AccidentParty party, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(party);
        return await session.SubmitChanges(party.AccidentPartyId == 0 ? "PartyAdded" : "PartyUpdated");
    }

    public async Task<SubmitOperation> DeletePartyAsync(AccidentParty party, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(party);
        return await session.SubmitChanges("PartyDeleted");
    }
}
