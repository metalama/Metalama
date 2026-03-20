![Metalama Logo](https://raw.githubusercontent.com/metalama/.github/HEAD/images/metalama.svg)

## About

The `Metalama.Extensions.DependencyInjection.ServiceLocator` implements an adapter for the _service locator_ dependency injection pattern, in which all services are provided by a global service locator.

When you add this package to a project, it automatically configures itself as the default dependency injection framework for Metalama.

## Principal Types

* `ServiceLocatorDependencyInjectionFramework` is the adapter that implements the service locator pattern for use with the `[IntroduceDependency]` advice attribute.
* `ServiceProviderProvider` is a static class whose `ServiceProvider` property must be set to the application's `IServiceProvider` in order for the service locator to resolve dependencies at run time.

## Documentation

* [Conceptual Documentation](https://doc.metalama.net/conceptual/aspects/dependency-injection)
* [API Documentation](https://doc.metalama.net/api/metalama_extensions_dependencyinjection_servicelocator)

## Related Packages

* [Metalama.Extensions.DependencyInjection](https://www.nuget.org/packages/Metalama.Extensions.DependencyInjection): The parent package.
