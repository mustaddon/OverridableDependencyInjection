using Microsoft.Extensions.DependencyInjection;

namespace OverridableDependencyInjection.Tests;

public class OverrideUsageScenarioTests
{
    /// <summary>
    /// Verifies that an instance override returns the exact supplied instance.
    /// </summary>
    [Fact(DisplayName = "Instance override should return the supplied instance.")]
    public void Override_Instance_ReturnsSuppliedInstance()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var instance = new ScenarioService("Instance");

        scope.Override<IExampleService>(instance);

        Assert.Same(instance, scope.ServiceProvider.GetRequiredService<IExampleService>());
    }

    /// <summary>
    /// Verifies that an override factory can resolve dependencies from the current scope.
    /// </summary>
    [Fact(DisplayName = "Override factory should resolve dependencies from the current scope.")]
    public void Override_Factory_ResolvesScopedDependencies()
    {
        using var provider = new ServiceCollection()
            .AddScoped<IMessageSource>(_ => new MessageSource("Scoped message"))
            .AddTransient<IExampleService, ExampleService>()
            .AddOverridability<IExampleService>()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.Override<IExampleService>(services =>
            new ScenarioService(services.GetRequiredService<IMessageSource>().Message));

        var service = scope.ServiceProvider.GetRequiredService<IExampleService>();
        Assert.Equal("Scoped message", service.Name);
    }

    /// <summary>
    /// Verifies that the non-generic Type overload creates the requested implementation.
    /// </summary>
    [Fact(DisplayName = "Type-based override should create the requested implementation.")]
    public void Override_ByType_CreatesRequestedImplementation()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();

        scope.Override(typeof(IExampleService), typeof(OverriddenExampleService));

        var service = scope.ServiceProvider.GetRequiredService<IExampleService>();
        Assert.Equal("Overridden", service.Name);
    }

    /// <summary>
    /// Verifies that separate scopes can use different instances without affecting each other or the root provider.
    /// </summary>
    [Fact(DisplayName = "Overrides in separate scopes should remain isolated.")]
    public void Override_SeparateScopes_AreIsolated()
    {
        using var provider = CreateProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        firstScope.Override<IExampleService>(new ScenarioService("First"));
        secondScope.Override<IExampleService>(new ScenarioService("Second"));

        Assert.Equal("First", firstScope.ServiceProvider.GetRequiredService<IExampleService>().Name);
        Assert.Equal("Second", secondScope.ServiceProvider.GetRequiredService<IExampleService>().Name);
        Assert.Equal("Original", provider.GetRequiredService<IExampleService>().Name);
    }

    /// <summary>
    /// Verifies that a keyed factory receives the key used to resolve the service.
    /// </summary>
    [Fact(DisplayName = "Keyed override factory should receive the requested service key.")]
    public void OverrideKeyed_Factory_ReceivesServiceKey()
    {
        using var provider = new ServiceCollection()
            .AddKeyedTransient<IExampleService, ExampleService>("primary")
            .AddOverridability<IExampleService>()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.OverrideKeyed<IExampleService>("primary", (_, key) => new ScenarioService($"Override-{key}"));

        var service = scope.ServiceProvider.GetRequiredKeyedService<IExampleService>("primary");
        Assert.Equal("Override-primary", service.Name);
    }

    /// <summary>
    /// Verifies that a closed generic service can be overridden independently by its closed implementation type.
    /// </summary>
    [Fact(DisplayName = "Closed generic service should accept a closed implementation override.")]
    public void Override_ClosedGenericService_UsesClosedImplementation()
    {
        using var provider = new ServiceCollection()
            .AddTransient(typeof(IRepository<>), typeof(Repository<>))
            .AddOverridability(typeof(IRepository<>))
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.Override(typeof(IRepository<int>), typeof(FakeRepository<int>));

        Assert.Equal("Fake-Int32", scope.ServiceProvider.GetRequiredService<IRepository<int>>().Name);
        Assert.Equal("Real-String", scope.ServiceProvider.GetRequiredService<IRepository<string>>().Name);
    }

    /// <summary>
    /// Verifies that disposing a scope disposes the implementation behind an open generic proxy.
    /// </summary>
    [Fact(DisplayName = "Disposing a scope should dispose the implementation behind an open generic proxy.")]
    public void OpenGenericDisposableService_IsDisposedWithScope()
    {
        using var provider = new ServiceCollection()
            .AddTransient(typeof(IDisposableRepository<>), typeof(DisposableRepository<>))
            .AddOverridability(typeof(IDisposableRepository<>))
            .BuildServiceProvider();
        IDisposableRepository<int> service;

        using (var scope = provider.CreateScope())
        {
            service = scope.ServiceProvider.GetRequiredService<IDisposableRepository<int>>();
            Assert.False(service.IsDisposed);
        }

        Assert.True(service.IsDisposed);
    }

    static ServiceProvider CreateProvider()
        => new ServiceCollection()
            .AddTransient<IExampleService, ExampleService>()
            .AddOverridability<IExampleService>()
            .BuildServiceProvider();

    }

    public sealed class ScenarioService(string name) : IExampleService
    {
        public string Name { get; } = name;
    }

    public interface IMessageSource
{
    string Message { get; }
}

public sealed class MessageSource(string message) : IMessageSource
{
    public string Message { get; } = message;
}

public interface IDisposableRepository<T> : IDisposable
{
    bool IsDisposed { get; }
}

public sealed class DisposableRepository<T> : IDisposableRepository<T>
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}
