![Metalama Logo](https://raw.githubusercontent.com/metalama/.github/HEAD/images/metalama.svg)

## About

The `Metalama.Extensions.DependencyInjection` package implements support for dependency injection in Metalama.

## Principal Types

* `[IntroduceDependency]` is an advice attribute that makes it easier for aspect to pull dependencies without knowing which dependency framework is used by the consuming project.
* `[Dependency]` is an aspect that turns a field or property into a dependency, and pulls it according to the rules of the dependency framework used by the consuming project.

## Documentation

* [Conceptual Documentation](https://doc.metalama.net/aspects/dependency-injection)
* [API Documentation](https://doc.metalama.net/api/metalama_extensions_dependencyinjection)

## Related Packages

* [Metalama.Extensions.DependencyInjection.ServiceLocator](https://www.nuget.org/packages/Metalama.Extensions.DependencyInjection.ServiceLocator):  pulls dependencies using a service locator pattern.