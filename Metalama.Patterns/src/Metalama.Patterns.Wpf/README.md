![Metalama Logo](https://raw.githubusercontent.com/postsharp/Metalama/master/images/metalama-by-postsharp.svg)

## About

The `Metalama.Patterns.Wpf` contains aspects that implement WPF dependency properties and commands.

## Key Features

*  Generates WPF **dependency properties** from C# automatic properties:
  * Integrates with `Metalama.Patterns.Contracts`.
  * Supports custom property-changing and property-changed methods.
  * Turns property initializer values into default values.
* Generates WPF **commands** from C# methods :
  * Supports a `CanExecute` method or property.
  * Integrates with `Metalama.Patterns.Observability` to support `INotifyPropertyChanged`.
* Supports custom naming conventions.

## Main Types

The primary types in this package are:

* `DependencyPropertyAttribute` is an aspect that generates a WPF dependency property from a plain automatic property.
* `CommandAttribute` is an aspect that exposes generates a WPF command property from a plain C# method.
* `DependencyPropertyExtensions.ConfigureDependencyProperty` is a fabric extension method that configures the `DependencyPropertyAttribute` aspect.
* `CommandExtensions.ConfigureCommand` is a fabric extension method that configures the `CommandAttribute` aspect.

## Additional Documentation

* Conceptual documentation:
  * [Command](https://doc.postsharp.net/metalama/patterns/wpf/command)
  * [Dependency Property](https://doc.postsharp.net/metalama/patterns/wpf/dependency-property)
* [API documentation](https://doc.postsharp.net/metalama/api/metalama-patterns-wpf)

## Related Packages

* `Metalama.Patterns.Contracts` provides contract attributes such as `[NotNull]` or `[Url]`. They integrate with the `DependencyPropertyAttribute` aspect.
* `Metalama.Patterns.Observability` can implement `INotifyPropertyChanged` for the `CanExecute` property supported by the `CommandAttribute` aspect.