﻿using OverridableDependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class OverridableDIServiceCollectionExtensions
{
    /// <summary>
    /// Adds override capability for <typeparamref name="TService"/>
    /// </summary>
    public static IServiceCollection AddOverridability<TService>(this IServiceCollection services)
    {
        return AddOverridability(services, [typeof(TService)]);
    }

    /// <summary>
    /// Adds override capability for selected types
    /// </summary>
    public static IServiceCollection AddOverridability(this IServiceCollection services, params Type[] serviceTypes)
    {
        ServiceRegistrar.Register(services, serviceTypes);

        return services;
    }

    /// <summary>
    /// Adds override capability for selected types
    /// </summary>
    public static IServiceCollection AddOverridability(this IServiceCollection services, IEnumerable<Type> serviceTypes)
    {
        ServiceRegistrar.Register(services, serviceTypes);

        return services;
    }
}