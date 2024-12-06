using DispatchProxyAdvanced;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace OverridableDependencyInjection;

public static class ServiceDescriptorExtensions
{
    public static bool IsOverridableDI(this ServiceDescriptor descriptor)
    {
        if (descriptor.ServiceKey is string serviceKey)
            return serviceKey.StartsWith(ServiceRegistrar.KEY_PREFIX);

        return IsOverridableService(descriptor.ServiceType);
    }

    static bool IsOverridableService(Type type)
    {
        return type == typeof(ScopeOverrides) || (type.GetProxyHandlerParameter()
            ?.GetCustomAttribute<FromKeyedServicesAttribute>()
            ?.Key as string)
            ?.StartsWith(ServiceRegistrar.KEY_PREFIX) == true;
    }
}
