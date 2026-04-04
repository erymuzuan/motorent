using MotoRent.Domain.DataContext;
using MotoRent.Domain.Settings;

namespace MotoRent.Services;

public partial class RentalService(
    RentalDataContext context,
    VehiclePoolService? poolService = null,
    BookingService? bookingService = null,
    AgentCommissionService? commissionService = null,
    ISettingConfig? settingConfig = null,
    TillService? tillService = null)
{
    private RentalDataContext Context { get; } = context;
    private VehiclePoolService? PoolService { get; } = poolService;
    private BookingService? BookingService { get; } = bookingService;
    private AgentCommissionService? CommissionService { get; } = commissionService;
    private ISettingConfig? SettingConfig { get; } = settingConfig;
    private TillService? TillService { get; } = tillService;
}
