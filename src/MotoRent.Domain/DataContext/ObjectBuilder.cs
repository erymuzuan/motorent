using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MotoRent.Domain.DataContext;

public static class ObjectBuilder
{
    private static IServiceProvider? s_serviceProvider;
    private static IServiceScopeFactory? s_scopeFactory;

    public static void Configure(IServiceProvider serviceProvider)
    {
        s_serviceProvider = serviceProvider;
        s_scopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
    }

    /// <summary>
    /// Gets the service provider from the current HTTP context scope if available,
    /// otherwise returns the root service provider.
    /// </summary>
    private static IServiceProvider GetCurrentScopeProvider()
    {
        if (s_serviceProvider == null)
            throw new InvalidOperationException("ObjectBuilder has not been configured. Call Configure() first.");

        // Try to get from HttpContext scope first (for scoped services in web requests)
        var httpContextAccessor = s_serviceProvider.GetService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor?.HttpContext;
        if (httpContext?.RequestServices != null)
        {
            return httpContext.RequestServices;
        }

        return s_serviceProvider;
    }

    public static T GetObject<T>() where T : class
    {
        var provider = GetCurrentScopeProvider();

        var service = provider.GetService(typeof(T)) as T;
        if (service != null)
            return service;

        throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    public static T? GetObjectOrDefault<T>() where T : class
    {
        if (s_serviceProvider == null)
            return null;

        var provider = GetCurrentScopeProvider();
        return provider.GetService(typeof(T)) as T;
    }
}
