// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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