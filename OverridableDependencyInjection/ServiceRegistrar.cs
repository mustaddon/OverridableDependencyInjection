using DispatchProxyAdvanced;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Emit;

namespace OverridableDependencyInjection;

internal static class ServiceRegistrar
{

    internal const string KEY_PREFIX = "_overridable_";


    
    public static void Register(IServiceCollection services, IEnumerable<Type> serviceTypes)
    {
        var notFound = TryRegister(services, serviceTypes);

        foreach (var type in notFound)
        {
            if (type.IsInterface && type.IsGenericType && !type.IsGenericTypeDefinition
                && !TryRegister(services, [type.GetGenericTypeDefinition()]).Any())
                continue;

            throw new ArgumentException($"'{type}' is not registered in IServiceCollection. Please register the service before adding overridability.");
        }
    }

    static IEnumerable<Type> TryRegister(IServiceCollection services, IEnumerable<Type> serviceTypes)
    {
        var overrides = services
            .FirstOrDefault(x => object.Equals(x.ServiceKey, KEY_PREFIX) && x.ServiceType == typeof(HashSet<Type>))
            ?.KeyedImplementationInstance as HashSet<Type>;

        if (overrides == null)
        {
            services.Insert(0, new(typeof(HashSet<Type>), KEY_PREFIX, overrides = []));
            services.AddScoped<ScopeOverrides>();
        }

        var newTypes = new HashSet<Type>(serviceTypes.Where(x => !overrides.Contains(x)));

        if (newTypes.Count == 0)
            return [];

        for (var i = services.Count - 1; i >= 0; i--)
        {
            var descriptor = services[i];

            if (!newTypes.Contains(descriptor.ServiceType) || descriptor.IsOverridableDI())
                continue;

            var serviceKey = string.Concat(KEY_PREFIX, Guid.NewGuid().ToString("N"));

            services.RemoveAt(i);
            services.Insert(i, CreateProxyDescriptor(services, descriptor, serviceKey));
            services.Add(CreateSourceDescriptor(descriptor, serviceKey));

            overrides.Add(descriptor.ServiceType);
        }

        return newTypes.Where(x => !overrides.Contains(x));
    }

    static ServiceDescriptor CreateProxyDescriptor(IServiceCollection services, ServiceDescriptor descriptor, object serviceKey)
    {
        if (descriptor.ServiceType.IsGenericType && descriptor.ServiceType.IsGenericTypeDefinition)
            return GenericProxyDescriptor(services, descriptor, serviceKey);

        return new ServiceDescriptor(
            descriptor.ServiceType,
            descriptor.ServiceKey,
            (s, k) => s.GetOverridedService(descriptor.ServiceType, descriptor.ServiceKey, serviceKey),
            ServiceLifetime.Transient);
    }

    static ServiceDescriptor GenericProxyDescriptor(IServiceCollection services, ServiceDescriptor descriptor, object serviceKey)
    {
        if (!descriptor.ServiceType.IsInterface)
            throw new ArgumentException($"'{descriptor.ServiceType}' is not interface (overridable open-generic service must be an interface).");

        var proxyType = ProxyFactory.CreateType(descriptor.ServiceType,
            new CustomAttributeBuilder(typeof(FromKeyedServicesAttribute).GetConstructor([typeof(object)])!, [serviceKey]));

        services.AddKeyedTransient(serviceKey, (s, k) => ProxyHandlerFactory.Create(s, descriptor.ServiceType, descriptor.ServiceKey, serviceKey));

        return new ServiceDescriptor(
            descriptor.ServiceType,
            descriptor.ServiceKey,
            proxyType,
            descriptor.Lifetime);
    }

    static ServiceDescriptor CreateSourceDescriptor(ServiceDescriptor descriptor, object serviceKey)
    {
        if (descriptor.IsKeyedService)
            return KeyedSourceDescriptor(descriptor, serviceKey);

        if (descriptor.ImplementationType != null)
            return new ServiceDescriptor(
                descriptor.ServiceType,
                serviceKey,
                descriptor.ImplementationType,
                descriptor.Lifetime);

        if (descriptor.ImplementationFactory != null)
            return new ServiceDescriptor(
                descriptor.ServiceType,
                serviceKey,
                (s, k) => descriptor.ImplementationFactory(s),
                descriptor.Lifetime);

        return new ServiceDescriptor(
            descriptor.ServiceType,
            serviceKey,
            descriptor.ImplementationInstance!);
    }

    static ServiceDescriptor KeyedSourceDescriptor(ServiceDescriptor descriptor, object serviceKey)
    {
        if (descriptor.KeyedImplementationType != null)
            return new ServiceDescriptor(
                descriptor.ServiceType,
                serviceKey,
                descriptor.KeyedImplementationType,
                descriptor.Lifetime);

        if (descriptor.KeyedImplementationFactory != null)
            return new ServiceDescriptor(
                descriptor.ServiceType,
                serviceKey,
                (s, k) => descriptor.KeyedImplementationFactory(s, descriptor.ServiceKey),
                descriptor.Lifetime);

        return new ServiceDescriptor(
            descriptor.ServiceType,
            serviceKey,
            descriptor.KeyedImplementationInstance!);
    }
}
