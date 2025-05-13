# OverridableDependencyInjection [![NuGet version](https://badge.fury.io/nu/OverridableDependencyInjection.svg?101)](http://badge.fury.io/nu/OverridableDependencyInjection)
Overridable injections for Microsoft.Extensions.DependencyInjection.

### Example

```C#
var provider = new ServiceCollection()
    .AddTransient<IExampleService1, ExampleService1A>()
    
    // REQUIRED: Adds override capability for IExampleService1
    .AddOverridability(typeof(IExampleService1))

    .BuildServiceProvider();



// SCOPE
using (var scope = provider.CreateScope())
{
    // Override the service implementation for this scope
    scope.Override<IExampleService1, ExampleService1B>();

    // Testing the override
    var serviceInstance = scope.ServiceProvider.GetRequiredService<IExampleService1>();

    Console.WriteLine($"IExampleService1 overridden: {serviceInstance is ExampleService1B}");
}
```

[Program.cs](https://github.com/mustaddon/OverridableDependencyInjection/blob/main/ExampleApp/Program.cs)
