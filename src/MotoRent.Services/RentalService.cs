using MotoRent.Domain.DataContext;

namespace MotoRent.Services;

public partial class RentalService(
    RentalDataContext context,
    VehiclePoolService? poolService = null,
    BookingService? bookingService = null,
    AgentCommissionService? commissionService = null)
{
    private RentalDataContext Context { get; } = context;
    private VehiclePoolService? PoolService { get; } = poolService;
    private BookingService? BookingService { get; } = bookingService;
    private AgentCommissionService? CommissionService { get; } = commissionService;
}
