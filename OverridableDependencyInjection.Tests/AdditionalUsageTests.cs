using Microsoft.Extensions.DependencyInjection;

namespace OverridableDependencyInjection.Tests;

public class AdditionalUsageTests
{
    /// <summary>
    /// Verifies that every registration for a service type is overridden within the scope.
    /// </summary>
    [Fact(DisplayName = "All registrations for a service type should be overridden inside the scope.")]
    public void Override_MultipleRegistrations_OverridesAllInScope()
    {
        using var provider = new ServiceCollection()
            .AddTransient<IMultiExampleService, FirstMultiExampleService>()
            .AddTransient<IMultiExampleService, SecondMultiExampleService>()
            .AddOverridability<IMultiExampleService>()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.Override<IMultiExampleService, ReplacementMultiExampleService>();

        Assert.Equal(
            new[] { "Replacement", "Replacement" },
            scope.ServiceProvider.GetServices<IMultiExampleService>().Select(service => service.Name));
        Assert.Equal("Second", provider.GetRequiredService<IMultiExampleService>().Name);
    }

    /// <summary>
    /// Verifies that overriding one keyed registration leaves other keys and the unkeyed service unchanged.
    /// </summary>
    [Fact(DisplayName = "A keyed override should not change other keys or the unkeyed service.")]
    public void OverrideKeyed_LeavesOtherKeysAndUnkeyedServiceUnchanged()
    {
        using var provider = new ServiceCollection()
            .AddTransient<IKeyedExampleService>(_ => new KeyedExampleService("Unkeyed"))
            .AddKeyedTransient<IKeyedExampleService>("primary", (_, key) => new KeyedExampleService($"Key-{key}"))
            .AddKeyedTransient<IKeyedExampleService>("secondary", (_, key) => new KeyedExampleService($"Key-{key}"))
            .AddOverridability<IKeyedExampleService>()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.OverrideKeyed<IKeyedExampleService>("primary", new KeyedExampleService("Override-primary"));

        Assert.Equal("Override-primary", scope.ServiceProvider.GetRequiredKeyedService<IKeyedExampleService>("primary").Name);
        Assert.Equal("Key-secondary", scope.ServiceProvider.GetRequiredKeyedService<IKeyedExampleService>("secondary").Name);
        Assert.Equal("Unkeyed", scope.ServiceProvider.GetRequiredService<IKeyedExampleService>().Name);
    }

    /// <summary>
    /// Verifies that a keyed override replaces every registration for that key within the scope only.
    /// </summary>
    [Fact(DisplayName = "A keyed override should replace every registration for its key without changing the root registrations.")]
    public void OverrideKeyed_ReplacesAllRegistrationsForKeyWithinScope()
    {
        using var provider = new ServiceCollection()
            .AddKeyedTransient<IKeyedExampleService, FirstRegisteredKeyedExampleService>("primary")
            .AddKeyedTransient<IKeyedExampleService, SecondRegisteredKeyedExampleService>("primary")
            .AddKeyedTransient<IKeyedExampleService, OtherRegisteredKeyedExampleService>("secondary")
            .AddOverridability<IKeyedExampleService>()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.OverrideKeyed<IKeyedExampleService, ReplacementKeyedExampleService>("primary");

        Assert.Equal(
            new[] { "Replacement", "Replacement" },
            scope.ServiceProvider.GetKeyedServices<IKeyedExampleService>("primary").Select(service => service.Name));
        Assert.Equal(
            new[] { "First primary", "Second primary" },
            provider.GetKeyedServices<IKeyedExampleService>("primary").Select(service => service.Name));
        Assert.Equal("Other secondary", scope.ServiceProvider.GetRequiredKeyedService<IKeyedExampleService>("secondary").Name);
    }

    /// <summary>
    /// Verifies that a keyed factory override replaces a registered keyed service without affecting its registration outside the scope.
    /// </summary>
    [Fact(DisplayName = "A keyed factory override should replace a registered service only for the selected scope and key.")]
    public void OverrideKeyed_FactoryReplacesRegisteredServiceOnlyForSelectedScopeAndKey()
    {
        using var provider = new ServiceCollection()
            .AddKeyedTransient<IKeyedExampleService>("primary", (_, key) => new KeyedExampleService($"Registered-{key}"))
            .AddKeyedTransient<IKeyedExampleService>("secondary", (_, key) => new KeyedExampleService($"Registered-{key}"))
            .AddOverridability<IKeyedExampleService>()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.OverrideKeyed<IKeyedExampleService>("primary", (_, key) => new KeyedExampleService($"Override-{key}"));

        Assert.Equal("Override-primary", scope.ServiceProvider.GetRequiredKeyedService<IKeyedExampleService>("primary").Name);
        Assert.Equal("Registered-secondary", scope.ServiceProvider.GetRequiredKeyedService<IKeyedExampleService>("secondary").Name);
        Assert.Equal("Registered-primary", provider.GetRequiredKeyedService<IKeyedExampleService>("primary").Name);
    }

    /// <summary>
    /// Verifies that regular factory and instance registrations remain available after enabling overrides.
    /// </summary>
    [Fact(DisplayName = "Factory and instance registrations should remain unchanged outside the override scope.")]
    public void AddOverridability_PreservesFactoryAndInstanceRegistrations()
    {
        var factoryCalls = 0;
        var instance = new KeyedExampleService("Registered instance");
        using var factoryProvider = new ServiceCollection()
            .AddTransient<IKeyedExampleService>(_ =>
            {
                factoryCalls++;
                return new KeyedExampleService("Factory registration");
            })
            .AddOverridability<IKeyedExampleService>()
            .BuildServiceProvider();

        Assert.Equal("Factory registration", factoryProvider.GetRequiredService<IKeyedExampleService>().Name);
        Assert.Equal(1, factoryCalls);

        using var instanceProvider = new ServiceCollection()
            .AddSingleton<IKeyedExampleService>(instance)
            .AddOverridability<IKeyedExampleService>()
            .BuildServiceProvider();

        Assert.Same(instance, instanceProvider.GetRequiredService<IKeyedExampleService>());
    }

    /// <summary>
    /// Verifies that a keyed factory registration receives its original key when resolved outside an override scope.
    /// </summary>
    [Fact(DisplayName = "A keyed factory registration should preserve its original key.")]
    public void AddOverridability_PreservesKeyedFactoryRegistration()
    {
        using var provider = new ServiceCollection()
            .AddKeyedTransient<IKeyedExampleService>("registered", (_, key) => new KeyedExampleService($"Factory-{key}"))
            .AddOverridability<IKeyedExampleService>()
            .BuildServiceProvider();

        var service = provider.GetRequiredKeyedService<IKeyedExampleService>("registered");

        Assert.Equal("Factory-registered", service.Name);
    }

    /// <summary>
    /// Verifies that scope-owned factory results are disposed while explicitly supplied instances remain caller-owned.
    /// </summary>
    [Fact(DisplayName = "Factory-created disposable overrides are disposed, but supplied instances remain caller-owned.")]
    public void DisposableOverrides_RespectInstanceOwnership()
    {
        using var provider = new ServiceCollection()
            .AddTransient<IDisposableExampleService, DisposableExampleService>()
            .AddOverridability<IDisposableExampleService>()
            .BuildServiceProvider();
        var factoryResult = new DisposableExampleService();
        var suppliedInstance = new DisposableExampleService();

        using (var scope = provider.CreateScope())
        {
            scope.Override<IDisposableExampleService>(_ => factoryResult);
            Assert.Equal(0, scope.ServiceProvider.GetRequiredService<IDisposableExampleService>().DisposeCount);
        }

        Assert.Equal(1, factoryResult.DisposeCount);

        using (var scope = provider.CreateScope())
        {
            scope.Override<IDisposableExampleService>(suppliedInstance);
            Assert.Equal(0, scope.ServiceProvider.GetRequiredService<IDisposableExampleService>().DisposeCount);
        }

        Assert.Equal(0, suppliedInstance.DisposeCount);
        suppliedInstance.Dispose();
    }

    /// <summary>
    /// Verifies that replacing an override after first resolution changes subsequent results within the same scope.
    /// </summary>
    [Fact(DisplayName = "A later override should replace a previously resolved override in the same scope.")]
    public void Override_ReplacesPreviouslyResolvedFactoryResult()
    {
        using var provider = new ServiceCollection()
            .AddTransient<IExampleService, ExampleService>()
            .AddOverridability<IExampleService>()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.Override<IExampleService>(_ => new ScenarioService("First"));
        Assert.Equal("First", scope.ServiceProvider.GetRequiredService<IExampleService>().Name);

        scope.Override<IExampleService>(_ => new ScenarioService("Second"));
        Assert.Equal("Second", scope.ServiceProvider.GetRequiredService<IExampleService>().Name);
    }

    /// <summary>
    /// Verifies that a closed generic service follows a new open generic implementation after the override is changed.
    /// </summary>
    [Fact(DisplayName = "A changed open generic override should apply to a closed service already resolved in the scope.")]
    public void Override_OpenGeneric_ReplacementAppliesAfterClosedServiceWasResolved()
    {
        using var provider = new ServiceCollection()
            .AddTransient(typeof(IRepository<>), typeof(Repository<>))
            .AddOverridability(typeof(IRepository<>))
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.Override(typeof(IRepository<>), typeof(FakeRepository<>));
        Assert.Equal("Fake-Int32", scope.ServiceProvider.GetRequiredService<IRepository<int>>().Name);

        scope.Override(typeof(IRepository<>), typeof(SecondFakeRepository<>));
        Assert.Equal("SecondFake-Int32", scope.ServiceProvider.GetRequiredService<IRepository<int>>().Name);
    }

    /// <summary>
    /// Verifies that type-based overrides resolve constructor dependencies, including keyed dependencies.
    /// </summary>
    [Fact(DisplayName = "Type-based overrides should resolve keyed constructor dependencies.")]
    public void Override_ByType_ResolvesKeyedConstructorDependency()
    {
        using var provider = new ServiceCollection()
            .AddKeyedSingleton<IConstructorDependency, ConstructorDependency>("constructor")
            .AddTransient<IExampleService, ExampleService>()
            .AddOverridability<IExampleService>()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.Override<IExampleService, KeyedConstructorExampleService>();

        Assert.Equal("Constructor dependency", scope.ServiceProvider.GetRequiredService<IExampleService>().Name);
    }

    /// <summary>
    /// Verifies that invalid registration and override requests fail, and factory exceptions are not hidden.
    /// </summary>
    [Fact(DisplayName = "Invalid override requests and factory failures should be surfaced to the caller.")]
    public void Override_InvalidRequestsAndFactoryErrors_AreSurfaced()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() => services.AddOverridability(typeof(IKeyedExampleService)));

        using var provider = new ServiceCollection()
            .AddTransient<IExampleService, ExampleService>()
            .AddOverridability<IExampleService>()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Throws<ArgumentException>(() => scope.Override(typeof(IExampleService), typeof(string)));
        Assert.Throws<ArgumentException>(() => scope.Override<IUnregisteredExampleService>(new UnregisteredExampleService()));

        scope.Override<IExampleService>(_ => throw new InvalidOperationException("Factory failed."));
        Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IExampleService>());
    }
}

public interface IMultiExampleService
{
    string Name { get; }
}

public sealed class FirstMultiExampleService : IMultiExampleService
{
    public string Name => "First";
}

public sealed class SecondMultiExampleService : IMultiExampleService
{
    public string Name => "Second";
}

public sealed class ReplacementMultiExampleService : IMultiExampleService
{
    public string Name => "Replacement";
}

public interface IKeyedExampleService
{
    string Name { get; }
}

public sealed class KeyedExampleService(string name) : IKeyedExampleService
{
    public string Name { get; } = name;
}

public sealed class FirstRegisteredKeyedExampleService : IKeyedExampleService
{
    public string Name => "First primary";
}

public sealed class SecondRegisteredKeyedExampleService : IKeyedExampleService
{
    public string Name => "Second primary";
}

public sealed class OtherRegisteredKeyedExampleService : IKeyedExampleService
{
    public string Name => "Other secondary";
}

public sealed class ReplacementKeyedExampleService : IKeyedExampleService
{
    public string Name => "Replacement";
}

public interface IDisposableExampleService : IDisposable
{
    int DisposeCount { get; }
}

public sealed class DisposableExampleService : IDisposableExampleService
{
    public int DisposeCount { get; private set; }

    public void Dispose() => DisposeCount++;
}

public sealed class SecondFakeRepository<T> : IRepository<T>
{
    public string Name => $"SecondFake-{typeof(T).Name}";
}

public interface IConstructorDependency
{
    string Value { get; }
}

public sealed class ConstructorDependency : IConstructorDependency
{
    public string Value => "Constructor dependency";
}

public sealed class KeyedConstructorExampleService([FromKeyedServices("constructor")] IConstructorDependency dependency) : IExampleService
{
    public string Name => dependency.Value;
}

public interface IUnregisteredExampleService
{
}

public sealed class UnregisteredExampleService : IUnregisteredExampleService
{
}
