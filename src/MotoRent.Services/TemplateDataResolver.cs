using MotoRent.Domain.Entities;
using MotoRent.Domain.Core;
using MotoRent.Services.Core;

namespace MotoRent.Services;

/// <summary>
/// Implementation of ITemplateDataResolver that flattens domain entities for templating.
/// </summary>
public class TemplateDataResolver : ITemplateDataResolver
{
    public Dictionary<string, object?> Resolve(Entity entity, Organization organization, User? staff = null)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // 1. Add Organization details
        this.AddOrganization(data, organization);

        // 2. Add Staff details
        if (staff != null) this.AddStaff(data, staff);

        // 3. Add Entity-specific details
        switch (entity)
        {
            case Booking booking:
                this.AddBooking(data, booking);
                break;
            case Rental rental:
                this.AddRental(data, rental);
                break;
            case Receipt receipt:
                this.AddReceipt(data, receipt);
                break;
        }

        return data;
    }

    private void AddOrganization(Dictionary<string, object?> data, Organization org)
    {
        data["Org.Name"] = org.Name;
        data["Org.Email"] = org.Email;
        data["Org.Phone"] = org.Phone;
        data["Org.Address"] = $"{org.Address.Street}, {org.Address.City}, {org.Address.Province} {org.Address.PostalCode}";
        data["Org.TaxId"] = org.TaxNo;
        data["Org.Website"] = org.WebSite;
    }

    private void AddStaff(Dictionary<string, object?> data, User staff)
    {
        data["Staff.Name"] = staff.FullName;
        data["Staff.UserName"] = staff.UserName;
    }

    private void AddBooking(Dictionary<string, object?> data, Booking booking)
    {
        data["Booking.Id"] = booking.BookingId;
        data["Booking.Ref"] = booking.BookingRef;
        data["Booking.CustomerName"] = booking.CustomerName;
        data["Booking.CustomerPhone"] = booking.CustomerPhone;
        data["Booking.CustomerEmail"] = booking.CustomerEmail;
        data["Booking.StartDate"] = booking.StartDate;
        data["Booking.EndDate"] = booking.EndDate;
        data["Booking.TotalAmount"] = booking.TotalAmount;
        data["Booking.DepositRequired"] = booking.DepositRequired;
        data["Booking.AmountPaid"] = booking.AmountPaid;
        data["Booking.BalanceDue"] = booking.BalanceDue;
        data["Booking.Status"] = booking.Status;
        data["Booking.Notes"] = booking.Notes;
        data["Booking.Days"] = booking.Days;
    }

    private void AddRental(Dictionary<string, object?> data, Rental rental)
    {
        data["Rental.Id"] = rental.RentalId;
        data["Rental.ContractNo"] = rental.RentalId.ToString();
        data["Rental.CustomerName"] = rental.RenterName;
        data["Rental.StartDate"] = rental.StartDate;
        data["Rental.EndDate"] = rental.ExpectedEndDate;
        data["Rental.Status"] = rental.Status;
        data["Rental.TotalAmount"] = rental.TotalAmount;
        data["Rental.VehicleName"] = rental.VehicleName;
        data["Rental.Days"] = rental.RentalDays;
        data["Rental.BalanceDue"] = rental.BalanceDue;
    }

    private void AddReceipt(Dictionary<string, object?> data, Receipt receipt)
    {
        data["Receipt.Id"] = receipt.ReceiptId;
        data["Receipt.No"] = receipt.ReceiptNo;
        data["Receipt.Date"] = receipt.IssuedOn;
        data["Receipt.TotalAmount"] = receipt.GrandTotal;
        data["Receipt.CustomerName"] = receipt.CustomerName;
        data["Receipt.Status"] = receipt.Status;
    }
}
