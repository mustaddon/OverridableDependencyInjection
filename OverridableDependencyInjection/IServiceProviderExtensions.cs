using Microsoft.Extensions.DependencyInjection;

namespace OverridableDependencyInjection;

internal static class IServiceProviderExtensions
{
    internal static object GetOverridedService(this IServiceProvider services, Type serviceType, object? serviceKey, object? sourceKey)
    {
        return services.GetService<ScopeOverrides>()?.GetFactory(serviceType, serviceKey)?.Invoke(services)
            ?? services.GetRequiredKeyedService(serviceType, sourceKey);
    }
}