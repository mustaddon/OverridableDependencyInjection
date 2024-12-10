using OverridableDependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class OverridableDIServiceCollectionExtensions
{
    [Obsolete("The method has been renamed. Please use - 'AddOverridability'.")]
    /// <summary>
    /// Adds override capability for <typeparamref name="TService"/>
    /// </summary>
    public static IServiceCollection AddOverridable<TService>(this IServiceCollection services)
    {
        return AddOverridable(services, [typeof(TService)]);
    }

    [Obsolete("The method has been renamed. Please use - 'AddOverridability'.")]
    /// <summary>
    /// Adds override capability for selected types
    /// </summary>
    public static IServiceCollection AddOverridable(this IServiceCollection services, params Type[] serviceTypes)
    {
        ServiceRegistrar.Register(services, serviceTypes);

        return services;
    }

    [Obsolete("The method has been renamed. Please use - 'AddOverridability'.")]
    /// <summary>
    /// Adds override capability for selected types
    /// </summary>
    public static IServiceCollection AddOverridable(this IServiceCollection services, IEnumerable<Type> serviceTypes)
    {
        ServiceRegistrar.Register(services, serviceTypes);

        return services;
    }
}