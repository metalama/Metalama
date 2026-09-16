![Metalama by PostSharp](https://raw.githubusercontent.com/metalama/.github/HEAD/images/metalama.svg)

The `Metalama.Backstage.Core` package is not meant to be directly referenced in user projects.

It contains the services of Metalama.Backstage that do not depend on a product: dependency injection, diagnostics, the file system, named locks, temporary files, configuration, telemetry, licensing and the user interface. The product-specific values and implementations, and the umbrella initialization API, are in the [Metalama.Backstage](https://www.nuget.org/packages/Metalama.Backstage) package, which depends on this one.
