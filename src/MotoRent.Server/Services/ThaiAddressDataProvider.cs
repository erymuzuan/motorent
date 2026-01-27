using MotoRent.Client.Services;

namespace MotoRent.Server.Services;

public class ThaiAddressDataProvider : IThaiAddressDataProvider
{
    private readonly IWebHostEnvironment m_env;
    private static string? s_cachedJson;

    public ThaiAddressDataProvider(IWebHostEnvironment env)
    {
        m_env = env;
    }

    public async Task<string> GetAddressDataJsonAsync()
    {
        if (s_cachedJson != null)
            return s_cachedJson;

        var filePath = Path.Combine(m_env.WebRootPath, "data", "thailand-addresses.json");
        s_cachedJson = await File.ReadAllTextAsync(filePath);
        return s_cachedJson;
    }
}
