using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public partial class RentalService
{
    public async Task<SubmitOperation> CreateHistoricalRentalAsync(Rental rental, string username)
    {
        rental.IsHistoricalEntry = true;
        rental.Status = "Completed";
        rental.ActualEndDate ??= rental.ExpectedEndDate;

        using var session = this.Context.OpenSession(username);
        session.Attach(rental);

        if (rental.PaidAmount is > 0)
        {
            var payment = new Payment
            {
                RentalId = rental.RentalId,
                Amount = rental.PaidAmount.Value,
                PaymentMethod = rental.PaymentMethod ?? "Cash",
                PaymentType = "Rental",
                PaidOn = rental.ActualEndDate ?? rental.ExpectedEndDate,
                Status = "Completed",
                Notes = "Recorded via past-rental entry"
            };
            session.Attach(payment);
        }

        return await session.SubmitChanges("RecordPastRental");
    }
}
