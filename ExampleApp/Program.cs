using ExampleApp;
using Microsoft.Extensions.DependencyInjection;

var provider = new ServiceCollection()
    .AddTransient<IExampleService1, ExampleService1A>()
    .AddTransient<IExampleService2, ExampleService2>()
    .AddTransient(typeof(IExampleService3<>), typeof(ExampleService3A<>))

    // REQUIRED: Adds override capability for selected types
    .AddOverridability(typeof(IExampleService1), typeof(IExampleService3<>))

    .BuildServiceProvider();




// SCOPE 1: without overrides
using (var scope = provider.CreateScope())
{
    Console.WriteLine(scope.ServiceProvider
        .GetRequiredService<IExampleService1>()
        .SomeMethod("test 1A"));

    Console.WriteLine(scope.ServiceProvider
        .GetRequiredService<IExampleService2>()
        .SomeMethod("test 2A"));

    Console.WriteLine(scope.ServiceProvider
        .GetRequiredService<IExampleService3<IExampleService1>>()
        .SomeMethod("test 3A"));
}

Console.WriteLine();


// SCOPE 2: with overrides
using (var scope = provider.CreateScope())
{
    scope.Override<IExampleService1>(new ExampleService1B());
    scope.Override(typeof(IExampleService3<>), typeof(ExampleService3B<>));


    Console.WriteLine(scope.ServiceProvider
        .GetRequiredService<IExampleService1>()
        .SomeMethod("test 1B"));

    Console.WriteLine(scope.ServiceProvider
        .GetRequiredService<IExampleService2>()
        .SomeMethod("test 2B"));

    Console.WriteLine(scope.ServiceProvider
        .GetRequiredService<IExampleService3<IExampleService1>>()
        .SomeMethod("test 3B"));
}

Console.WriteLine();


// SCOPE 3: with overrides
using (var scope = provider.CreateScope())
{
    scope.Override<IExampleService1>(new ExampleService1C());
    scope.Override<IExampleService3<IExampleService1>, ExampleService3C<IExampleService1>>();


    Console.WriteLine(scope.ServiceProvider
        .GetRequiredService<IExampleService1>()
        .SomeMethod("test 1C"));

    Console.WriteLine(scope.ServiceProvider
        .GetRequiredService<IExampleService2>()
        .SomeMethod("test 2C"));

    Console.WriteLine(scope.ServiceProvider
        .GetRequiredService<IExampleService3<IExampleService1>>()
        .SomeMethod("test 3C"));
}