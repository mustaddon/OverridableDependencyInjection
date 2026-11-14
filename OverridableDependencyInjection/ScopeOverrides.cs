using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace OverridableDependencyInjection;

internal class ScopeOverrides([FromKeyedServices(ServiceRegistrar.KEY_PREFIX)] HashSet<Type> serviceTypes) : IDisposable
{
    readonly ConcurrentDictionary<object, Func<IServiceProvider, object>?> _overrides = new();
    readonly ConcurrentDictionary<object, bool> _ownedOverrides = new();
    readonly ConcurrentDictionary<object, Lazy<object>> _cachedInstances = new();
    readonly ConcurrentDictionary<object, (Type? Type, ConcurrentBag<Delegate> Factories)> _generics = new();
    readonly ConcurrentBag<IDisposable> _disposables = new();

    public Func<IServiceProvider, object>? GetFactory(Type serviceType, object? serviceKey, ServiceLifetime lifetime)
    {
        var key = GetKey(serviceType, serviceKey);

        if (_overrides.TryGetValue(key, out var factory))
            return ApplyLifetime(key, TrackDisposableResult(key, factory), lifetime);

        if (serviceType.IsGenericType && _generics.TryGetValue(GetKey(serviceType.GetGenericTypeDefinition(), serviceKey), out var generic) && generic.Type != null)
        {
            factory = generic.Type.MakeGenericType(serviceType.GetGenericArguments()).CreateServiceFactory();
            _overrides.AddOrUpdate(key, factory, (k, v) => factory);
            _ownedOverrides.AddOrUpdate(key, true, (k, v) => true);
            generic.Factories.Add(factory);
            return ApplyLifetime(key, TrackDisposableResult(key, factory), lifetime);
        }

        return null;
    }

    public void Override(Type serviceType, object? serviceKey, Func<IServiceProvider, object>? factory, bool ownsResult)
    {
        CheckOverridable(serviceType);

        AddOrUpdate(serviceType, serviceKey, factory, ownsResult);
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

            AddOrUpdate(serviceType, serviceKey, implementationType?.CreateServiceFactory(), true);
        }
    }

    void CheckOverridable(Type serviceType)
    {
        if (!serviceTypes.Contains(serviceType) && !(serviceType.IsGenericType && serviceTypes.Contains(serviceType.GetGenericTypeDefinition())))
            throw new ArgumentException($"'{serviceType}' is not registered as overridable in IServiceCollection.");
    }

    Func<IServiceProvider, object>? ApplyLifetime(object key, Func<IServiceProvider, object>? factory, ServiceLifetime lifetime)
    {
        if (factory == null || lifetime == ServiceLifetime.Transient)
            return factory;

        return services => _cachedInstances.GetOrAdd(key,
            _ => new Lazy<object>(() => factory(services))).Value;
    }

    Func<IServiceProvider, object>? TrackDisposableResult(object key, Func<IServiceProvider, object>? factory)
    {
        if (factory == null || !_ownedOverrides.TryGetValue(key, out var ownsResult) || !ownsResult)
            return factory;

        return services =>
        {
            var instance = factory(services);
            if (instance is IDisposable disposable)
                _disposables.Add(disposable);

            return instance;
        };
    }

    void AddOrUpdate(Type serviceType, object? serviceKey, Func<IServiceProvider, object>? factory, bool ownsResult)
    {
        var key = GetKey(serviceType, serviceKey);
        _cachedInstances.TryRemove(key, out _);
        _overrides.AddOrUpdate(key, factory, (k, v) => factory);
        _ownedOverrides.AddOrUpdate(key, ownsResult, (k, v) => ownsResult);
    }

    void AddOrUpdate(Type serviceType, object? serviceKey, Type? implementationType)
    {
        _generics.AddOrUpdate(GetKey(serviceType, serviceKey), k => (implementationType, []), (k, v) =>
        {
            foreach (var kvp in _overrides.Where(x => v.Factories.Contains(x.Value)))
            {
                _overrides.TryRemove(kvp.Key, out var _);
                _ownedOverrides.TryRemove(kvp.Key, out var _);
                _cachedInstances.TryRemove(kvp.Key, out _);
            }

            return (implementationType, []);
        });
    }

    public void Dispose()
    {
        var disposed = new HashSet<object>(ObjectReferenceComparer.Instance);
        foreach (var instance in _disposables)
        {
            if (disposed.Add(instance))
                instance.Dispose();
        }
    }

    sealed class ObjectReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ObjectReferenceComparer Instance = new();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    static internal object GetKey(Type serviceType, object? serviceKey) => new { serviceType, serviceKey };
}
