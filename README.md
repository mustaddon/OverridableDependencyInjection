# OverridableDependencyInjection 
[![NuGet version](https://badge.fury.io/nu/OverridableDependencyInjection.svg?104)](https://badge.fury.io/nu/OverridableDependencyInjection)

A .NET extension for `Microsoft.Extensions.DependencyInjection` that lets you substitute a registered service within one `IServiceScope` without changing the root provider or its registrations.

Use it for test doubles, request-specific behavior, or temporary implementations within a unit of work. Overrides can be supplied by implementation type, instance, or factory. Regular services, keyed services, and open generic interfaces are supported.

## Features

- Substitute a service for one scope without rebuilding the root provider
- Keep the original registration available outside that scope
- Override regular, keyed, and open generic interface services
- Choose an implementation type, provide an instance, or create one with a factory
- Integrate with the familiar `ServiceCollection` and `IServiceScope` APIs

## Installation

```bash
dotnet add package OverridableDependencyInjection
```

## Quick start

```csharp
using Microsoft.Extensions.DependencyInjection;

using var provider = new ServiceCollection()
    .AddTransient<IExampleService1, ExampleService1A>()
    .AddTransient<IExampleService2, ExampleService2>()
    .AddOverridability<IExampleService1>()
    .BuildServiceProvider();

using (var scope = provider.CreateScope())
{
    scope.Override<IExampleService1, ExampleService1B>();

    var service = scope.ServiceProvider.GetRequiredService<IExampleService1>();
    Console.WriteLine(service is ExampleService1B); // True
}

// The root provider still resolves the original registration.
Console.WriteLine(provider.GetRequiredService<IExampleService1>() is ExampleService1A); // True
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

Alternatively, provide an instance:

```csharp
scope.Override<IWeatherService>(new FakeWeatherService());
```

Or create the override with a factory. The factory receives the current scope's service provider, so it can resolve scoped dependencies:

```csharp
scope.Override<IWeatherService>(services =>
    new FakeWeatherService(services.GetRequiredService<IWeatherData>()));
```

### Override a service type directly

```csharp
scope.Override(typeof(IWeatherService), typeof(FakeWeatherService));
```

## Open generic support

Open generic interface registrations can also be overridden. Register the open generic service type with `AddOverridability`:

```csharp
var provider = new ServiceCollection()
    .AddTransient(typeof(IRepository<>), typeof(Repository<>))
    .AddOverridability(typeof(IRepository<>))
    .BuildServiceProvider();

using var scope = provider.CreateScope();
scope.Override(typeof(IRepository<>), typeof(FakeRepository<>));
```

## Keyed services

Keyed overrides target the specified key; other keys and unkeyed registrations are not changed:

```csharp
var provider = new ServiceCollection()
    .AddKeyedTransient<IExampleService, ExampleServiceA>("primary")
    .AddOverridability(typeof(IExampleService))
    .BuildServiceProvider();

using var scope = provider.CreateScope();
scope.OverrideKeyed<IExampleService, ExampleServiceB>("primary");
```

## Important behavior

- Register the service first, then call `AddOverridability(...)`, and only then build the provider.
- An override affects resolutions from the scope where it was configured; dispose the scope when the override is no longer needed.
- Other scopes and the root provider continue to use the original registrations.
- Only service types enabled with `AddOverridability` can be overridden.
- Open generic overrides require an open generic interface registration.

## Example application

The repository includes a working demo app:

- [ExampleApp/Program.cs](https://github.com/mustaddon/OverridableDependencyInjection/blob/main/ExampleApp/Program.cs)

## License

MIT
