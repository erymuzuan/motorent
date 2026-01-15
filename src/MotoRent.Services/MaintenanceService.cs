using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class MaintenanceService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    // Warning thresholds
    private const int c_warningDays = 7;
    private const int c_warningKm = 200;

    #region ServiceType CRUD

    public async Task<LoadOperation<ServiceType>> GetServiceTypesAsync(bool activeOnly = true)
    {
        var query = this.Context.CreateQuery<ServiceType>();

        if (activeOnly)
            query = query.Where(st => st.IsActive);

        query = query.OrderBy(st => st.SortOrder);

        return await this.Context.LoadAsync(query, page: 1, size: 100, includeTotalRows: false);
    }

    public async Task<ServiceType?> GetServiceTypeByIdAsync(int serviceTypeId)
    {
        return await this.Context.LoadOneAsync<ServiceType>(st => st.ServiceTypeId == serviceTypeId);
    }

    public async Task<SubmitOperation> CreateServiceTypeAsync(ServiceType serviceType, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(serviceType);
        return await session.SubmitChanges("CreateServiceType");
    }

    public async Task<SubmitOperation> UpdateServiceTypeAsync(ServiceType serviceType, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(serviceType);
        return await session.SubmitChanges("UpdateServiceType");
    }

    public async Task<SubmitOperation> DeleteServiceTypeAsync(ServiceType serviceType, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(serviceType);
        return await session.SubmitChanges("DeleteServiceType");
    }

    public async Task<SubmitOperation> CreateDefaultServiceTypesAsync(string username)
    {
        var defaults = new List<ServiceType>
        {
            new() { Name = "Oil Change", Description = "Engine oil and filter replacement", DaysInterval = 30, KmInterval = 3000, SortOrder = 1, IsActive = true },
            new() { Name = "Brake Check", Description = "Brake pads, discs, and fluid inspection", DaysInterval = 60, KmInterval = 5000, SortOrder = 2, IsActive = true },
            new() { Name = "Tire Inspection", Description = "Tire pressure, wear, and condition check", DaysInterval = 90, KmInterval = 8000, SortOrder = 3, IsActive = true },
            new() { Name = "General Service", Description = "Full vehicle inspection and maintenance", DaysInterval = 180, KmInterval = 15000, SortOrder = 4, IsActive = true }
        };

        using var session = this.Context.OpenSession(username);
        foreach (var st in defaults)
            session.Attach(st);
        return await session.SubmitChanges("CreateDefaultServiceTypes");
    }

    #endregion

    #region MaintenanceSchedule Operations

    public async Task<List<MaintenanceSchedule>> GetSchedulesForMotorbikeAsync(int motorbikeId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<MaintenanceSchedule>().Where(ms => ms.MotorbikeId == motorbikeId),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    public async Task<List<MaintenanceScheduleWithStatus>> GetSchedulesWithStatusForMotorbikeAsync(
        int motorbikeId, int currentMileage, DateTimeOffset today)
    {
        var schedules = await this.GetSchedulesForMotorbikeAsync(motorbikeId);
        return schedules.Select(s => this.CreateScheduleWithStatus(s, currentMileage, today)).ToList();
    }

    public async Task<SubmitOperation> RecordServiceAsync(RecordServiceRequest request, string username)
    {
        // Load or create the maintenance schedule for this service type
        var schedule = await this.Context.LoadOneAsync<MaintenanceSchedule>(
            ms => ms.MotorbikeId == request.MotorbikeId && ms.ServiceTypeId == request.ServiceTypeId);

        var serviceType = await this.Context.LoadOneAsync<ServiceType>(
            st => st.ServiceTypeId == request.ServiceTypeId);

        if (serviceType == null)
            return SubmitOperation.CreateFailure("Service type not found");

        var motorbike = await this.Context.LoadOneAsync<Motorbike>(
            m => m.MotorbikeId == request.MotorbikeId);

        if (motorbike == null)
            return SubmitOperation.CreateFailure("Motorbike not found");

        using var session = this.Context.OpenSession(username);

        if (schedule == null)
        {
            schedule = new MaintenanceSchedule
            {
                MotorbikeId = request.MotorbikeId,
                ServiceTypeId = request.ServiceTypeId,
                ServiceTypeName = serviceType.Name,
                MotorbikeName = $"{motorbike.Brand} {motorbike.Model} ({motorbike.LicensePlate})"
            };
        }

        // Update schedule with service info
        schedule.LastServiceDate = request.ServiceDate;
        schedule.LastServiceMileage = request.ServiceMileage;
        schedule.LastServiceBy = request.PerformedBy;
        schedule.LastServiceNotes = request.Notes;

        // Calculate next due
        schedule.NextDueDate = request.ServiceDate.AddDays(serviceType.DaysInterval);
        schedule.NextDueMileage = request.ServiceMileage + serviceType.KmInterval;

        session.Attach(schedule);

        // Update motorbike's last service date (for quick reference)
        motorbike.LastServiceDate = request.ServiceDate;
        session.Attach(motorbike);

        return await session.SubmitChanges("RecordService");
    }

    public async Task<SubmitOperation> InitializeSchedulesForMotorbikeAsync(int motorbikeId, string username)
    {
        // Get all active service types
        var serviceTypesResult = await this.GetServiceTypesAsync(activeOnly: true);
        var motorbike = await this.Context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == motorbikeId);

        if (motorbike == null)
            return SubmitOperation.CreateFailure("Motorbike not found");

        using var session = this.Context.OpenSession(username);
        int created = 0;

        foreach (var st in serviceTypesResult.ItemCollection)
        {
            // Check if schedule already exists
            var existing = await this.Context.LoadOneAsync<MaintenanceSchedule>(
                ms => ms.MotorbikeId == motorbikeId && ms.ServiceTypeId == st.ServiceTypeId);

            if (existing == null)
            {
                var schedule = new MaintenanceSchedule
                {
                    MotorbikeId = motorbikeId,
                    ServiceTypeId = st.ServiceTypeId,
                    ServiceTypeName = st.Name,
                    MotorbikeName = $"{motorbike.Brand} {motorbike.Model} ({motorbike.LicensePlate})",
                    // No last service - bike is new or service history unknown
                    NextDueDate = DateTimeOffset.Now.AddDays(st.DaysInterval),
                    NextDueMileage = motorbike.Mileage + st.KmInterval
                };
                session.Attach(schedule);
                created++;
            }
        }

        if (created == 0)
            return SubmitOperation.CreateSuccess(0, 0, 0);

        return await session.SubmitChanges("InitializeMaintenanceSchedules");
    }

    #endregion

    #region Status Calculation

    public MaintenanceStatus CalculateStatus(
        DateTimeOffset? nextDueDate, int? nextDueMileage,
        int currentMileage, DateTimeOffset today)
    {
        bool dateOverdue = nextDueDate.HasValue && today > nextDueDate.Value;
        bool mileageOverdue = nextDueMileage.HasValue && currentMileage > nextDueMileage.Value;

        if (dateOverdue || mileageOverdue)
            return MaintenanceStatus.Overdue;

        bool dateDueSoon = nextDueDate.HasValue &&
            today.AddDays(c_warningDays) >= nextDueDate.Value;
        bool mileageDueSoon = nextDueMileage.HasValue &&
            currentMileage + c_warningKm >= nextDueMileage.Value;

        if (dateDueSoon || mileageDueSoon)
            return MaintenanceStatus.DueSoon;

        return MaintenanceStatus.Ok;
    }

    public MaintenanceScheduleWithStatus CreateScheduleWithStatus(
        MaintenanceSchedule schedule, int currentMileage, DateTimeOffset today)
    {
        var status = this.CalculateStatus(
            schedule.NextDueDate, schedule.NextDueMileage,
            currentMileage, today);

        return new MaintenanceScheduleWithStatus
        {
            Schedule = schedule,
            Status = status,
            CurrentMileage = currentMileage
        };
    }

    #endregion

    #region Dashboard Queries

    public async Task<List<MaintenanceAlertItem>> GetMaintenanceAlertsAsync(
        DateTimeOffset today, int limit = 10)
    {
        // Get all motorbikes (excluding those already in maintenance)
        var bikesResult = await this.Context.LoadAsync(
            this.Context.CreateQuery<Motorbike>().Where(m => m.Status != "Maintenance"),
            page: 1, size: 1000, includeTotalRows: false);

        var alerts = new List<MaintenanceAlertItem>();

        foreach (var bike in bikesResult.ItemCollection)
        {
            var schedules = await this.GetSchedulesWithStatusForMotorbikeAsync(
                bike.MotorbikeId, bike.Mileage, today);

            foreach (var schedule in schedules.Where(s => s.Status != MaintenanceStatus.Ok))
            {
                alerts.Add(new MaintenanceAlertItem
                {
                    MotorbikeId = bike.MotorbikeId,
                    MotorbikeName = $"{bike.Brand} {bike.Model}",
                    LicensePlate = bike.LicensePlate,
                    ServiceTypeName = schedule.Schedule.ServiceTypeName ?? "Unknown",
                    Status = schedule.Status,
                    NextDueDate = schedule.Schedule.NextDueDate,
                    NextDueMileage = schedule.Schedule.NextDueMileage,
                    CurrentMileage = bike.Mileage
                });
            }
        }

        return alerts
            .OrderBy(a => a.Status == MaintenanceStatus.Overdue ? 0 : 1)
            .ThenBy(a => a.NextDueDate)
            .Take(limit)
            .ToList();
    }

    public async Task<MotorbikeMaintenanceSummary> GetMotorbikeMaintenanceSummaryAsync(
        int motorbikeId, DateTimeOffset today)
    {
        var motorbike = await this.Context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == motorbikeId);
        if (motorbike == null)
            return new MotorbikeMaintenanceSummary { MotorbikeId = motorbikeId };

        var schedules = await this.GetSchedulesWithStatusForMotorbikeAsync(
            motorbikeId, motorbike.Mileage, today);

        return new MotorbikeMaintenanceSummary
        {
            MotorbikeId = motorbikeId,
            OverallStatus = schedules.Any(s => s.Status == MaintenanceStatus.Overdue)
                ? MaintenanceStatus.Overdue
                : schedules.Any(s => s.Status == MaintenanceStatus.DueSoon)
                    ? MaintenanceStatus.DueSoon
                    : MaintenanceStatus.Ok,
            Schedules = schedules,
            OverdueCount = schedules.Count(s => s.Status == MaintenanceStatus.Overdue),
            DueSoonCount = schedules.Count(s => s.Status == MaintenanceStatus.DueSoon)
        };
    }

    public async Task<MaintenanceSummary> GetMaintenanceSummaryAsync(DateTimeOffset today)
    {
        var bikesResult = await this.Context.LoadAsync(
            this.Context.CreateQuery<Motorbike>(),
            page: 1, size: 1000, includeTotalRows: false);

        int overdueBikes = 0, dueSoonBikes = 0, okBikes = 0;

        foreach (var bike in bikesResult.ItemCollection)
        {
            var summary = await this.GetMotorbikeMaintenanceSummaryAsync(bike.MotorbikeId, today);
            switch (summary.OverallStatus)
            {
                case MaintenanceStatus.Overdue: overdueBikes++; break;
                case MaintenanceStatus.DueSoon: dueSoonBikes++; break;
                default: okBikes++; break;
            }
        }

        return new MaintenanceSummary
        {
            OverdueBikes = overdueBikes,
            DueSoonBikes = dueSoonBikes,
            OkBikes = okBikes,
            TotalBikes = bikesResult.ItemCollection.Count
        };
    }

    #endregion
}

#region DTOs

public class RecordServiceRequest
{
    public int MotorbikeId { get; set; }
    public int ServiceTypeId { get; set; }
    public DateTimeOffset ServiceDate { get; set; }
    public int ServiceMileage { get; set; }
    public string? PerformedBy { get; set; }
    public string? Notes { get; set; }
}

public class MaintenanceScheduleWithStatus
{
    public MaintenanceSchedule Schedule { get; set; } = null!;
    public MaintenanceStatus Status { get; set; }
    public int CurrentMileage { get; set; }
}

public class MaintenanceAlertItem
{
    public int MotorbikeId { get; set; }
    public string MotorbikeName { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string ServiceTypeName { get; set; } = string.Empty;
    public MaintenanceStatus Status { get; set; }
    public DateTimeOffset? NextDueDate { get; set; }
    public int? NextDueMileage { get; set; }
    public int CurrentMileage { get; set; }
}

public class MotorbikeMaintenanceSummary
{
    public int MotorbikeId { get; set; }
    public MaintenanceStatus OverallStatus { get; set; }
    public List<MaintenanceScheduleWithStatus> Schedules { get; set; } = [];
    public int OverdueCount { get; set; }
    public int DueSoonCount { get; set; }
}

public class MaintenanceSummary
{
    public int OverdueBikes { get; set; }
    public int DueSoonBikes { get; set; }
    public int OkBikes { get; set; }
    public int TotalBikes { get; set; }
}

#endregion
