using DispatchProxyAdvanced;
using Microsoft.Extensions.DependencyInjection;

namespace OverridableDependencyInjection;

internal class ProxyHandlerFactory
{
    public static ProxyHandler Create(IServiceProvider services, Type serviceType, object? serviceKey, object? sourceKey, ServiceLifetime lifetime)
    {
        var handler = new ProxyHandler((proxy, method, args) => method.Invoke(
            proxy.GetOrAddState(() => services.GetOverridedService(proxy.GetDeclaringType(), serviceKey, sourceKey, lifetime)),
            args));

        if (!typeof(IDisposable).IsAssignableFrom(serviceType))
            return handler;

        return (proxy, method, args) =>
        {
            if (method.Name == nameof(IDisposable.Dispose) && method.ReturnType == typeof(void) && method.GetParameters().Length == 0)
                return null;

            return handler(proxy, method, args);
        };
    }
}
