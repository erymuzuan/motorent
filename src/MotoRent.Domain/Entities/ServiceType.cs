namespace MotoRent.Domain.Entities;

public class ServiceType : Entity
{
    public int ServiceTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DaysInterval { get; set; }

    public int KmInterval { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public List<VehicleType> ApplicableVehicleTypes { get; set; } = [];

    public List<string> ApplicableModels { get; set; } = [];

    public override int GetId() => this.ServiceTypeId;
    public override void SetId(int value) => this.ServiceTypeId = value;
}
