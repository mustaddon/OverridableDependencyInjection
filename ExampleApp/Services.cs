using Microsoft.Extensions.DependencyInjection;

namespace ExampleApp;


interface IExampleService
{
    string SomeMethod(string a);
}

interface IExampleService1 : IExampleService { }

internal class ExampleService1A : IExampleService1
{
    public string SomeMethod(string a) => $"{GetType().Name}: {a}";
}

internal class ExampleService1B : ExampleService1A { }
internal class ExampleService1C : ExampleService1A { }


interface IExampleService2 : IExampleService { }

internal class ExampleService2(IExampleService1 exampleService1) : IExampleService2
{
    public string SomeMethod(string a) => $"{GetType().Name}: {exampleService1.SomeMethod(a)}";
}






interface IExampleService3<T1> : IExampleService
    where T1 : IExampleService
{

}

internal class ExampleService3A<T1>(T1 exampleService1) : IExampleService3<T1>
    where T1 : IExampleService
{
    public string SomeMethod(string a)
    {
        return $"{GetType().Name}: {exampleService1.SomeMethod(a)}";
    }
}

internal class ExampleService3B<T1>(T1 exampleService1) : ExampleService3A<T1>(exampleService1)
    where T1 : IExampleService
{ }

internal class ExampleService3C<T1>(T1 exampleService1) : ExampleService3A<T1>(exampleService1)
    where T1 : IExampleService
{ }