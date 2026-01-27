namespace MotoRent.Client.Services;

public interface IThaiAddressDataProvider
{
    Task<string> GetAddressDataJsonAsync();
}
