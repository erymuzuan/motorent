namespace MotoRent.Client.Services;

public static class PlaceholderDefinitions
{
    public static readonly Dictionary<string, string[]> Groups = new()
    {
        ["Organization"] =
        [
            "Org.Name", "Org.Email", "Org.Phone", "Org.Address", "Org.TaxId", "Org.Website"
        ],
        ["Staff"] =
        [
            "Staff.Name", "Staff.UserName"
        ],
        ["Booking"] =
        [
            "Booking.Id", "Booking.Ref", "Booking.CustomerName", "Booking.CustomerPhone",
            "Booking.CustomerEmail", "Booking.CustomerPassport", "Booking.CustomerNationality",
            "Booking.HotelName", "Booking.StartDate", "Booking.EndDate",
            "Booking.TotalAmount", "Booking.DepositRequired", "Booking.AmountPaid",
            "Booking.BalanceDue", "Booking.Status", "Booking.Notes", "Booking.Days"
        ],
        ["Rental"] =
        [
            "Rental.Id", "Rental.ContractNo", "Rental.CustomerName",
            "Rental.StartDate", "Rental.EndDate", "Rental.Status",
            "Rental.TotalAmount", "Rental.VehicleName", "Rental.Days", "Rental.BalanceDue"
        ],
        ["Receipt"] =
        [
            "Receipt.Id", "Receipt.No", "Receipt.Date",
            "Receipt.TotalAmount", "Receipt.CustomerName", "Receipt.Status"
        ]
    };

    public static string GetCategoryIcon(string category) => category switch
    {
        "Organization" => "ti-building",
        "Staff" => "ti-user",
        "Booking" => "ti-calendar-event",
        "Rental" => "ti-motorbike",
        "Receipt" => "ti-receipt",
        _ => "ti-tag"
    };
}
