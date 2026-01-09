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

    public static T GetObject<T>() where T : class
    {
        if (s_serviceProvider == null)
            throw new InvalidOperationException("ObjectBuilder has not been configured. Call Configure() first.");

        // Try to get from root provider first (for singletons)
        var service = s_serviceProvider.GetService(typeof(T)) as T;
        if (service != null)
            return service;

        // Create a scope for scoped services
        if (s_scopeFactory != null)
        {
            using var scope = s_scopeFactory.CreateScope();
            service = scope.ServiceProvider.GetService(typeof(T)) as T;
            if (service != null)
                return service;
        }

        throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    public static T? GetObjectOrDefault<T>() where T : class
    {
        if (s_serviceProvider == null)
            return null;

        var service = s_serviceProvider.GetService(typeof(T)) as T;
        if (service != null)
            return service;

        if (s_scopeFactory != null)
        {
            using var scope = s_scopeFactory.CreateScope();
            return scope.ServiceProvider.GetService(typeof(T)) as T;
        }

        return null;
    }
}
