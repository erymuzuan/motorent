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

        using (var rentalSession = this.Context.OpenSession(username))
        {
            rentalSession.Attach(rental);
            var rentalResult = await rentalSession.SubmitChanges("RecordPastRental");
            if (!rentalResult.Success)
                return rentalResult;
        }

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

            using var paymentSession = this.Context.OpenSession(username);
            paymentSession.Attach(payment);
            return await paymentSession.SubmitChanges("RecordPastRentalPayment");
        }

        return SubmitOperation.CreateSuccess(1, 0, 0);
    }
}
