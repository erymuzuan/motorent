namespace MotoRent.Domain.Helps;

public interface ISerialNumber
{
    string? No { get; set; }
    string Prefix { get; }
}

public interface ISerialNumberGenerator
{
    Task<string> SetSerialNumberAsync(ISerialNumber item, int? suggested = null);
}
