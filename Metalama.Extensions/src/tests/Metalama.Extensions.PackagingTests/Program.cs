using Metalama.Extensions.DependencyInjection.ServiceLocator;
using Metalama.Extensions.PackagingTests;

class Program
{
    
    public static void Main()
    {
        ServiceProviderProvider.ServiceProvider = () => new ServiceProvider();
        new Program().Test();

    }

    [MyAspect]
    void Test()
    {
        Console.WriteLine("Hello, world.");
    }
}

class ServiceProvider : IServiceProvider
{
    public object? GetService( Type serviceType ) => Console.Out;
}