using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OverridableDependencyInjection.Tests;

public class LifetimeOverrideTests
{
    /// <summary>
    /// Verifies that open generic proxy registrations preserve their singleton, scoped, and transient lifetimes.
    /// </summary>
    [Theory(DisplayName = "Open generic services should preserve their registered lifetime.")]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void OpenGenericService_PreservesRegisteredLifetime(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        services.Add(new ServiceDescriptor(typeof(IGenericLifetimeService<>), typeof(GenericLifetimeService<>), lifetime));
        services.AddOverridability(typeof(IGenericLifetimeService<>));
        using var provider = services.BuildServiceProvider(validateScopes: true);

        IGenericLifetimeService<int> first;
        using (var firstScope = provider.CreateScope())
        {
            first = firstScope.ServiceProvider.GetRequiredService<IGenericLifetimeService<int>>();
            var second = firstScope.ServiceProvider.GetRequiredService<IGenericLifetimeService<int>>();
            Assert.Equal(lifetime != ServiceLifetime.Transient, ReferenceEquals(first, second));
        }

        using var secondScope = provider.CreateScope();
        var serviceInSecondScope = secondScope.ServiceProvider.GetRequiredService<IGenericLifetimeService<int>>();
        Assert.Equal(lifetime == ServiceLifetime.Singleton, ReferenceEquals(first, serviceInSecondScope));

        if (lifetime == ServiceLifetime.Singleton)
            Assert.Same(first, provider.GetRequiredService<IGenericLifetimeService<int>>());
    }

    /// <summary>
    /// Verifies that each supported registration lifetime can be overridden in one scope without changing another scope.
    /// </summary>
    [Theory(DisplayName = "Services with different lifetimes should be overridable within a single scope.")]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void Override_RespectsOriginalLifetime_AndIsScoped(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.AddSingleton<ILifetimeService, OriginalLifetimeService>();
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped<ILifetimeService, OriginalLifetimeService>();
                break;
            case ServiceLifetime.Transient:
                services.AddTransient<ILifetimeService, OriginalLifetimeService>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }

        services.AddOverridability<ILifetimeService>();
        using var provider = services.BuildServiceProvider(validateScopes: true);

        LifetimeIdentity firstOriginal;
        LifetimeIdentity secondOriginal;

        using (var scope = provider.CreateScope())
        {
            firstOriginal = scope.ServiceProvider.GetRequiredService<ILifetimeService>().Identity;
            secondOriginal = scope.ServiceProvider.GetRequiredService<ILifetimeService>().Identity;

            var shouldReuseInstance = lifetime != ServiceLifetime.Transient;
            Assert.Equal(shouldReuseInstance, firstOriginal.Equals(secondOriginal));

            scope.Override<ILifetimeService, ReplacementLifetimeService>();
            Assert.Equal("Replacement", scope.ServiceProvider.GetRequiredService<ILifetimeService>().Name);
        }

        using var otherScope = provider.CreateScope();
        var unchanged = otherScope.ServiceProvider.GetRequiredService<ILifetimeService>();
        Assert.Equal("Original", unchanged.Name);

        if (lifetime == ServiceLifetime.Singleton)
            Assert.Equal(firstOriginal, unchanged.Identity);
    }

    /// <summary>
    /// Verifies that disposable service instances are released according to their registered lifetime.
    /// </summary>
    [Theory(DisplayName = "Disposable services should be disposed according to their registered lifetime.")]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void DisposableService_IsDisposedAccordingToLifetime(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        services.Add(new ServiceDescriptor(typeof(IDisposableLifetimeService), typeof(DisposableLifetimeService), lifetime));
        services.AddOverridability<IDisposableLifetimeService>();
        var provider = services.BuildServiceProvider(validateScopes: true);
        var instances = new List<IDisposableLifetimeService>();
        var root = lifetime == ServiceLifetime.Scoped ? null 
            : provider.GetRequiredService<IDisposableLifetimeService>();

        // without Override
        using (var firstScope = provider.CreateScope())
        {
            var first = firstScope.ServiceProvider.GetRequiredService<IDisposableLifetimeService>();
            var second = firstScope.ServiceProvider.GetRequiredService<IDisposableLifetimeService>();
            instances.Add(first);
            instances.Add(second);

            Assert.Equal(lifetime == ServiceLifetime.Singleton, first.InstanceId == root?.InstanceId);
            Assert.Equal(lifetime != ServiceLifetime.Transient, first.InstanceId == second.InstanceId);
            Assert.False(first.IsDisposed);
            Assert.False(second.IsDisposed);
        }

        Assert.False(root?.IsDisposed ?? false);
        Assert.All(instances, instance => Assert.Equal(lifetime == ServiceLifetime.Singleton ? 0 : 1, instance.DisposeCount));
        var secondOffset = instances.Count;

        // with Override
        using (var secondScope = provider.CreateScope())
        {
            secondScope.Override<IDisposableLifetimeService, DisposableLifetimeService>();
            var first = secondScope.ServiceProvider.GetRequiredService<IDisposableLifetimeService>();
            var second = secondScope.ServiceProvider.GetRequiredService<IDisposableLifetimeService>();
            instances.Add(first);
            instances.Add(second);

            Assert.NotEqual(first.InstanceId, root?.InstanceId);
            Assert.NotEqual(instances[0].InstanceId, first.InstanceId);
            Assert.Equal(lifetime != ServiceLifetime.Transient, first.InstanceId == second.InstanceId);
            Assert.False(first.IsDisposed);
            Assert.False(second.IsDisposed);
        }

        Assert.False(root?.IsDisposed ?? false);
        Assert.All(instances.Take(secondOffset), instance => Assert.Equal(lifetime == ServiceLifetime.Singleton ? 0 : 1, instance.DisposeCount));
        Assert.All(instances.Skip(secondOffset), instance => Assert.Equal(1, instance.DisposeCount));

        provider.Dispose();

        Assert.Equal(1, root?.DisposeCount ?? 1);
        Assert.All(instances, instance => Assert.Equal(1, instance.DisposeCount));
    }
}

public interface IGenericLifetimeService<T>
{
}

public sealed class GenericLifetimeService<T> : IGenericLifetimeService<T>
{
}

public interface ILifetimeService
{
    string Name { get; }
    LifetimeIdentity Identity { get; }
}

public readonly struct LifetimeIdentity : IEquatable<LifetimeIdentity>
{
    public LifetimeIdentity(Guid value) => Value = value;

    public Guid Value { get; }

    public bool Equals(LifetimeIdentity other) => Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is LifetimeIdentity other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();
}

public sealed class OriginalLifetimeService : ILifetimeService
{
    public string Name => "Original";
    public LifetimeIdentity Identity { get; } = new(Guid.NewGuid());
}

public sealed class ReplacementLifetimeService : ILifetimeService
{
    public string Name => "Replacement";
    public LifetimeIdentity Identity { get; } = new(Guid.NewGuid());
}

public interface IDisposableLifetimeService : IDisposable
{
    Guid InstanceId { get; }
    bool IsDisposed { get; }
    int DisposeCount { get; }
}

public sealed class DisposableLifetimeService : IDisposableLifetimeService
{
    public Guid InstanceId { get; } = Guid.NewGuid();
    public bool IsDisposed => DisposeCount > 0;
    public int DisposeCount { get; private set; }

    public void Dispose() => DisposeCount++;
}
