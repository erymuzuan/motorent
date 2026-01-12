using System.Reflection;

namespace MotoRent.Worker.Infrastructure;

/// <summary>
/// Discovers subscriber types in loaded assemblies.
/// </summary>
public class Discoverer : IDisposable
{
    /// <summary>
    /// Find all subscriber types.
    /// </summary>
    public IEnumerable<SubscriberMetadata> Find()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var assemblies = Directory.GetFiles(baseDirectory, "*.dll")
            .Where(f => Path.GetFileName(f).StartsWith("MotoRent", StringComparison.OrdinalIgnoreCase));

        foreach (var assemblyPath in assemblies)
        {
            Assembly? assembly;
            try
            {
                assembly = Assembly.LoadFrom(assemblyPath);
            }
            catch
            {
                continue;
            }

            var subscriberTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && IsSubscriber(t));

            foreach (var type in subscriberTypes)
            {
                yield return new SubscriberMetadata
                {
                    Assembly = assembly.FullName,
                    FullName = type.FullName,
                    Type = type,
                    Name = type.Name
                };
            }
        }
    }

    private static bool IsSubscriber(Type type)
    {
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType == typeof(Subscriber))
                return true;

            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Subscriber<>))
                return true;

            baseType = baseType.BaseType;
        }
        return false;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
