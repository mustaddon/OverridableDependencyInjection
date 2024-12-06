using OverridableDependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class OverridableDIServiceScopeExtensions
{
    public static IServiceScope Override<TService>(this IServiceScope scope, TService instance)
        where TService : class
    {
        return Override(scope, typeof(TService), null, s => instance);
    }

    public static IServiceScope Override<TService, TImplementation>(this IServiceScope scope)
        where TService : class
        where TImplementation : class, TService
    {
        return Override(scope, typeof(TService), null, typeof(TImplementation));
    }

    public static IServiceScope Override<TService>(this IServiceScope scope, Func<IServiceProvider, TService> factory)
        where TService : class
    {
        return Override(scope, typeof(TService), null, factory);
    }

    public static IServiceScope Override(this IServiceScope scope, Type serviceType, Func<IServiceProvider, object> factory)
    {
        return Override(scope, serviceType, null, factory);
    }

    public static IServiceScope Override(this IServiceScope scope, Type serviceType)
    {
        return Override(scope, serviceType, null, serviceType);
    }

    public static IServiceScope Override(this IServiceScope scope, Type serviceType, Type implementationType)
    {
        return Override(scope, serviceType, null, implementationType);
    }




    public static IServiceScope OverrideKeyed<TService>(this IServiceScope scope, object? serviceKey, TService instance)
        where TService : class
    {
        return Override(scope, typeof(TService), serviceKey, s => instance);
    }

    public static IServiceScope OverrideKeyed<TService, TImplementation>(this IServiceScope scope, object? serviceKey)
        where TService : class
        where TImplementation : class, TService
    {
        return Override(scope, typeof(TService), serviceKey, typeof(TImplementation));
    }

    public static IServiceScope OverrideKeyed<TService>(this IServiceScope scope, object? serviceKey, Func<IServiceProvider, object?, TService> factory)
        where TService : class
    {
        return OverrideKeyed(scope, typeof(TService), serviceKey, factory);
    }

    public static IServiceScope OverrideKeyed(this IServiceScope scope, Type serviceType, object? serviceKey, Func<IServiceProvider, object?, object> factory)
    {
        var factoryKeyed = factory == null ? null
            : new Func<IServiceProvider, object>(s => factory(s, serviceKey));

        return Override(scope, serviceType, serviceKey, factoryKeyed);
    }

    public static IServiceScope OverrideKeyed(this IServiceScope scope, Type serviceType, object? serviceKey)
    {
        return Override(scope, serviceType, serviceKey, serviceType);
    }

    public static IServiceScope OverrideKeyed(this IServiceScope scope, Type serviceType, object? serviceKey, Type implementationType)
    {
        return Override(scope, serviceType, serviceKey, implementationType);
    }




    static IServiceScope Override(this IServiceScope scope, Type serviceType, object? serviceKey, Func<IServiceProvider, object>? factory)
    {
        scope.ServiceProvider
            .GetRequiredService<ScopeOverrides>()
            .Override(serviceType, serviceKey, factory);

        return scope;
    }

    static IServiceScope Override(this IServiceScope scope, Type serviceType, object? serviceKey, Type implementationType)
    {
        scope.ServiceProvider
            .GetRequiredService<ScopeOverrides>()
            .Override(serviceType, serviceKey, implementationType);

        return scope;
    }
}