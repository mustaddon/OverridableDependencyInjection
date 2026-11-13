# OverridableDependencyInjection 
[![NuGet version](https://badge.fury.io/nu/OverridableDependencyInjection.svg?104)](https://badge.fury.io/nu/OverridableDependencyInjection)

A small extension for `Microsoft.Extensions.DependencyInjection` that lets you override registered services inside a specific scope without changing the root container configuration.

This is useful for tests, request-specific behavior, and temporary substitutions when you need a different implementation for a single unit of work.

## Why use it?

- Override a service only for one `IServiceScope`
- Keep the original registrations intact for the rest of the app
- Works with regular services, keyed services, and open generic interfaces
- Very lightweight integration with `ServiceCollection`

## Installation

```bash
dotnet add package OverridableDependencyInjection
```

## Quick start

```csharp
using Microsoft.Extensions.DependencyInjection;

var provider = new ServiceCollection()
    .AddTransient<IExampleService1, ExampleService1A>()
    .AddTransient<IExampleService2, ExampleService2>()

    // Mark which services may be overridden
    .AddOverridability(typeof(IExampleService1))
    .BuildServiceProvider();

using (var scope = provider.CreateScope())
{
    scope.Override<IExampleService1, ExampleService1B>();

    var service = scope.ServiceProvider.GetRequiredService<IExampleService1>();
    Console.WriteLine(service is ExampleService1B); // True
}
```

## Basic API

### Register overridable services

```csharp
var services = new ServiceCollection();

services.AddTransient<IWeatherService, WeatherService>();
services.AddTransient<IAuditService, AuditService>();
services.AddOverridability(typeof(IWeatherService), typeof(IAuditService));
```

You can also do it with generics:

```csharp
services.AddOverridability<IWeatherService>();
```

### Override inside a scope

```csharp
using var scope = provider.CreateScope();

scope.Override<IWeatherService, FakeWeatherService>();
```

You can also override by instance or factory:

```csharp
scope.Override<IWeatherService>(new FakeWeatherService());
scope.Override<IWeatherService>(_ => new FakeWeatherService());
```

### Override a service type directly

```csharp
scope.Override(typeof(IWeatherService), typeof(FakeWeatherService));
```

## Open generic support

This library also supports open generic interfaces:

```csharp
var provider = new ServiceCollection()
    .AddTransient(typeof(IRepository<>), typeof(Repository<>))
    .AddOverridability(typeof(IRepository<>))
    .BuildServiceProvider();

using var scope = provider.CreateScope();
scope.Override(typeof(IRepository<>), typeof(FakeRepository<>));
```

## Keyed services

Keyed overrides are supported as well:

```csharp
var provider = new ServiceCollection()
    .AddKeyedTransient<IExampleService, ExampleServiceA>("primary")
    .AddOverridability(typeof(IExampleService))
    .BuildServiceProvider();

using var scope = provider.CreateScope();
scope.OverrideKeyed<IExampleService, ExampleServiceB>("primary");
```

## Important behavior

- `AddOverridability(...)` must be called before `BuildServiceProvider()`.
- Overrides are scoped to the lifetime of the `IServiceScope`.
- The original registration remains available outside that scope.
- Only service types registered with `AddOverridability` can be overridden.

## Example application

The repository includes a working demo app:

- [ExampleApp/Program.cs](https://github.com/mustaddon/OverridableDependencyInjection/blob/main/ExampleApp/Program.cs)

## License

MIT
