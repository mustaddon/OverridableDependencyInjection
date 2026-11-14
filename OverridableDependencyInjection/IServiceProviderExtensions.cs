using Microsoft.Extensions.DependencyInjection;

namespace OverridableDependencyInjection;

internal static class IServiceProviderExtensions
{
    internal static object GetOverridedService(this IServiceProvider services, Type serviceType, object? serviceKey, object? sourceKey, ServiceLifetime lifetime)
    {
        ScopeOverrides? overrides;
        try
        {
            overrides = services.GetService<ScopeOverrides>();
        }
        catch (InvalidOperationException)
        {
            overrides = null;
        }

        var factory = overrides?.GetFactory(serviceType, serviceKey, lifetime);
        if (factory != null)
            return factory(services);

        return services.GetRequiredKeyedService(serviceType, sourceKey);
    }
}