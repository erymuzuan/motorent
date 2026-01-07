namespace MotoRent.Domain.DataContext;

public static class ObjectBuilder
{
    private static IServiceProvider? s_serviceProvider;

    public static void Configure(IServiceProvider serviceProvider)
    {
        s_serviceProvider = serviceProvider;
    }

    public static T GetObject<T>() where T : class
    {
        if (s_serviceProvider == null)
            throw new InvalidOperationException("ObjectBuilder has not been configured. Call Configure() first.");

        return s_serviceProvider.GetService(typeof(T)) as T
            ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    public static T? GetObjectOrDefault<T>() where T : class
    {
        if (s_serviceProvider == null)
            return null;

        return s_serviceProvider.GetService(typeof(T)) as T;
    }
}
