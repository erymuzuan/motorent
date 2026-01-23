using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Accident document management operations.
/// </summary>
public partial class AccidentService
{
    public async Task<List<AccidentDocument>> GetDocumentsAsync(int accidentId)
    {
        var query = this.Context.CreateQuery<AccidentDocument>()
            .Where(d => d.AccidentId == accidentId)
            .OrderByDescending(d => d.UploadedDate);

        var result = await this.Context.LoadAsync(query, 1, 100, false);
        return result.ItemCollection;
    }

    public async Task<SubmitOperation> SaveDocumentAsync(AccidentDocument document, string username)
    {
        if (document.AccidentDocumentId == 0)
        {
            document.UploadedDate = DateTimeOffset.Now;
            document.UploadedBy = username;
        }

        using var session = this.Context.OpenSession(username);
        session.Attach(document);
        return await session.SubmitChanges(document.AccidentDocumentId == 0 ? "DocumentAdded" : "DocumentUpdated");
    }

    public async Task<SubmitOperation> DeleteDocumentAsync(AccidentDocument document, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(document);
        return await session.SubmitChanges("DocumentDeleted");
    }
}
