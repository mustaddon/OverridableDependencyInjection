using Microsoft.Extensions.DependencyInjection;
using OverridableDependencyInjection;

namespace OverridableDependencyInjection.Tests;

public class ScopeOverrideTests
{
    /// <summary>
    /// Ensures the override applies only within the current scope and does not mutate the root container.
    /// </summary>
    [Fact(DisplayName = "Scoped override should affect only the current scope and leave the root service unchanged.")]
    public void Override_IsScoped_AndLeavesRootContainerUnchanged()
    {
        var provider = new ServiceCollection()
            .AddTransient<IExampleService, ExampleService>()
            .AddOverridability(typeof(IExampleService))
            .BuildServiceProvider();

        Assert.Equal("Original", provider.GetRequiredService<IExampleService>().Name);

        using (var scope = provider.CreateScope())
        {
            scope.Override<IExampleService, OverriddenExampleService>();

            var service = scope.ServiceProvider.GetRequiredService<IExampleService>();
            Assert.Equal("Overridden", service.Name);
        }

        Assert.Equal("Original", provider.GetRequiredService<IExampleService>().Name);
    }

    /// <summary>
    /// Verifies that a keyed override is applied only within the current scope and resolves the keyed registration correctly.
    /// </summary>
    [Fact(DisplayName = "Keyed override should resolve the overridden registration inside the current scope.")]
    public void OverrideKeyed_UsesScopedKeyedRegistration()
    {
        var provider = new ServiceCollection()
            .AddKeyedTransient<IExampleService, ExampleService>("primary")
            .AddOverridability(typeof(IExampleService))
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        scope.OverrideKeyed<IExampleService, OverriddenExampleService>("primary");

        var service = scope.ServiceProvider.GetRequiredKeyedService<IExampleService>("primary");
        Assert.Equal("Overridden", service.Name);
    }

    /// <summary>
    /// Confirms that open generic registrations can also be overridden in a scope and return the replacement implementation.
    /// </summary>
    [Fact(DisplayName = "Open generic override should return the replacement implementation for the requested generic service.")]
    public void Override_OpenGeneric_ServiceType()
    {
        var provider = new ServiceCollection()
            .AddTransient(typeof(IRepository<>), typeof(Repository<>))
            .AddOverridability(typeof(IRepository<>))
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        scope.Override(typeof(IRepository<>), typeof(FakeRepository<>));

        var fake = scope.ServiceProvider.GetRequiredService<IRepository<int>>();
        Assert.Equal("Fake-Int32", fake.Name);

        var real = provider.GetRequiredService<IRepository<int>>();
        Assert.Equal("Real-Int32", real.Name);
    }
}

public interface IExampleService
{
    string Name { get; }
}

public sealed class ExampleService : IExampleService
{
    public string Name => "Original";
}

public sealed class OverriddenExampleService : IExampleService
{
    public string Name => "Overridden";
}

public interface IRepository<T>
{
    string Name { get; }
}

public sealed class Repository<T> : IRepository<T>
{
    public string Name => $"Real-{typeof(T).Name}";
}

public sealed class FakeRepository<T> : IRepository<T>
{
    public string Name => $"Fake-{typeof(T).Name}";
}
