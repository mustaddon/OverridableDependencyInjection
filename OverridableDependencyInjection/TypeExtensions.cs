using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace OverridableDependencyInjection;

public static class TypeExtensions
{
    public static Func<IServiceProvider, object> CreateServiceFactory(this Type type)
    {
        if (!type.IsClass)
            throw new ArgumentException($"'{type}' is not Class");

        if (type.IsAbstract)
            throw new ArgumentException($"'{type}' is Abstract");

        var constructor = type
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault()
            ?? throw new ArgumentException($"Constructor '{type}' not found.");

        var parameters = constructor
            .GetParameters()
            .Select(p =>
            {
                var k = (p.GetCustomAttribute(typeof(FromKeyedServicesAttribute)) as FromKeyedServicesAttribute)?.Key;
                var t = p.ParameterType;

                return k == null
                    ? new Func<IServiceProvider, object>(s => s.GetRequiredService(t))
                    : s => s.GetRequiredKeyedService(t, k);
            })
            .ToArray();

        return s => Activator.CreateInstance(type, parameters.Select(x => x(s)).ToArray())!;
    }

}
