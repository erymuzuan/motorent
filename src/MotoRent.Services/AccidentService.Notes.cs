using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Accident note management operations.
/// </summary>
public partial class AccidentService
{
    public async Task<List<AccidentNote>> GetNotesAsync(int accidentId)
    {
        var query = this.Context.CreateQuery<AccidentNote>()
            .Where(n => n.AccidentId == accidentId)
            .OrderByDescending(n => n.AccidentNoteId);

        var result = await this.Context.LoadAsync(query, 1, 100, false);
        return result.ItemCollection;
    }

    public async Task<SubmitOperation> SaveNoteAsync(AccidentNote note, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(note);
        return await session.SubmitChanges(note.AccidentNoteId == 0 ? "NoteAdded" : "NoteUpdated");
    }

    public async Task<SubmitOperation> DeleteNoteAsync(AccidentNote note, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(note);
        return await session.SubmitChanges("NoteDeleted");
    }
}
