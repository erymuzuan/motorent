using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public partial class VehicleOwnerService
{
    public async Task<SubmitOperation> AttachVehicleAsync(
        int vehicleId,
        int ownerId,
        OwnerPaymentModel model,
        decimal rateOrPercent,
        string username)
    {
        var vehicleTask = this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == vehicleId);
        var ownerTask = this.GetOwnerByIdAsync(ownerId);
        await Task.WhenAll(vehicleTask, ownerTask);

        var vehicle = vehicleTask.Result;
        if (vehicle is null)
            return SubmitOperation.CreateFailure("Vehicle not found");

        if (vehicle.VehicleOwnerId is > 0)
            return SubmitOperation.CreateFailure("Vehicle is already assigned to an owner");

        var owner = ownerTask.Result;
        if (owner is null)
            return SubmitOperation.CreateFailure("Owner not found");

        vehicle.VehicleOwnerId = ownerId;
        vehicle.VehicleOwnerName = owner.Name;
        vehicle.OwnerPaymentModel = model;

        if (model == OwnerPaymentModel.DailyRate)
        {
            vehicle.OwnerDailyRate = rateOrPercent;
            vehicle.OwnerRevenueSharePercent = null;
        }
        else
        {
            vehicle.OwnerRevenueSharePercent = rateOrPercent;
            vehicle.OwnerDailyRate = null;
        }

        using var session = this.Context.OpenSession(username);
        session.Attach(vehicle);
        return await session.SubmitChanges("AttachVehicleToOwner");
    }

    public async Task<SubmitOperation> DetachVehicleAsync(int vehicleId, string username)
    {
        var vehicle = await this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == vehicleId);
        if (vehicle is null)
            return SubmitOperation.CreateFailure("Vehicle not found");

        if (vehicle.Status == VehicleStatus.Rented)
            return SubmitOperation.CreateFailure("Cannot detach a vehicle that is currently rented.");

        vehicle.VehicleOwnerId = null;
        vehicle.VehicleOwnerName = null;
        vehicle.OwnerPaymentModel = null;
        vehicle.OwnerDailyRate = null;
        vehicle.OwnerRevenueSharePercent = null;

        using var session = this.Context.OpenSession(username);
        session.Attach(vehicle);
        return await session.SubmitChanges("DetachVehicleFromOwner");
    }
}
