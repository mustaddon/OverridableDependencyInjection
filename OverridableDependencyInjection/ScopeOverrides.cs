using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace OverridableDependencyInjection;

internal class ScopeOverrides([FromKeyedServices(ServiceRegistrar.KEY_PREFIX)] HashSet<Type> serviceTypes)
{
    readonly ConcurrentDictionary<object, Func<IServiceProvider, object>?> _overrides = new();
    readonly ConcurrentDictionary<object, (Type? Type, ConcurrentBag<Delegate> Factories)> _generics = new();

    public Func<IServiceProvider, object>? GetFactory(Type serviceType, object? serviceKey)
    {
        var key = GetKey(serviceType, serviceKey);

        if (_overrides.TryGetValue(key, out var factory))
            return factory;

        if (serviceType.IsGenericType && _generics.TryGetValue(GetKey(serviceType.GetGenericTypeDefinition(), serviceKey), out var generic) && generic.Type != null)
        {
            factory = generic.Type.MakeGenericType(serviceType.GetGenericArguments()).CreateServiceFactory();
            _overrides.AddOrUpdate(key, factory, (k, v) => factory);
            generic.Factories.Add(factory);
            return factory;
        }

        return null;
    }

    public void Override(Type serviceType, object? serviceKey, Func<IServiceProvider, object>? factory)
    {
        CheckOverridable(serviceType);

        AddOrUpdate(serviceType, serviceKey, factory);
    }

    public void Override(Type serviceType, object? serviceKey, Type? implementationType)
    {
        CheckOverridable(serviceType);

        if (serviceType.IsGenericType && serviceType.IsGenericTypeDefinition)
        {
            if (implementationType != null && implementationType.IsAbstract)
                throw new ArgumentException($"'{implementationType}' is abstract.");

            AddOrUpdate(serviceType, serviceKey, implementationType);
        }
        else
        {
            if (!serviceType.IsAssignableFrom(implementationType))
                throw new ArgumentException($"'{implementationType}' is not assignable to '{serviceType}'.");

            AddOrUpdate(serviceType, serviceKey, implementationType?.CreateServiceFactory());
        }
    }

    void CheckOverridable(Type serviceType)
    {
        if (!serviceTypes.Contains(serviceType) && !(serviceType.IsGenericType && serviceTypes.Contains(serviceType.GetGenericTypeDefinition())))
            throw new ArgumentException($"'{serviceType}' is not registered as overridable in IServiceCollection.");
    }

    void AddOrUpdate(Type serviceType, object? serviceKey, Func<IServiceProvider, object>? factory)
        => _overrides.AddOrUpdate(GetKey(serviceType, serviceKey), factory, (k, v) => factory);

    void AddOrUpdate(Type serviceType, object? serviceKey, Type? implementationType)
    {
        _generics.AddOrUpdate(GetKey(serviceType, serviceKey), k => (implementationType, []), (k, v) =>
        {
            foreach (var kvp in _overrides.Where(x => v.Factories.Contains(x.Value)))
                _overrides.TryRemove(kvp.Key, out var _);

            return (implementationType, []);
        });
    }

    static internal object GetKey(Type serviceType, object? serviceKey) => new { serviceType, serviceKey };
}
