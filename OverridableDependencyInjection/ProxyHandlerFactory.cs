using DispatchProxyAdvanced;

namespace OverridableDependencyInjection;

internal class ProxyHandlerFactory
{
    public static ProxyHandler Create(IServiceProvider services, Type serviceType, object? serviceKey, object? sourceKey)
    {
        var handler = new ProxyHandler((proxy, method, args) => method.Invoke(
            proxy.GetOrAddState(() => services.GetOverridedService(proxy.GetDeclaringType(), serviceKey, sourceKey)),
            args));

        if (!typeof(IDisposable).IsAssignableFrom(serviceType))
            return handler;

        return (proxy, method, args) =>
        {
            if (method.DeclaringType == typeof(IDisposable))
                return null;

            return handler(proxy, method, args);
        };
    }
}
