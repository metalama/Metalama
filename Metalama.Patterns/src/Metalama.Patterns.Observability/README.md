![Metalama Logo](https://raw.githubusercontent.com/postsharp/Metalama/master/images/metalama-by-postsharp.svg)

## About

The `Metalama.Patterns.Observability` contains an aspect that implements the Observer pattern, i.e. the `INotifyPropertyChanged` interface.

## Key Features

* Supports automatic properties, explicit properties (including field-backed properties).
* Supports child objects, i.e. properties like `string FullName => $"{this.Model.FirstName} {this.Model.LastName}"`.
* Idiomatic. Does not require you to change your coding patterns (unlike the MVVM community toolkit).
* Safe. Reports warnings when a property dependency is not understood.


## Main Types

The primary types in this package are:

* `ObservableAttribute` is a type-level aspect that implements the Observer pattern and the `INotifyPropertyChanged` interface.
* `NotObservableAttribute` is an attribute that waives a property from the `ObservableAttribute` aspect.
* `ConstantAttribute` is an attribute that represents that a method is safed for use from a property made observable by the `ObservableAttribute` aspect.
* `SuppressObservabilityWarningsAttribute` is an attribute that suppresses all warnings reported by the `ObservableAttribute` aspect in the target property.
* `ObservabilityExtensions.ConfigureObservability` is a fabric extension method that allows to configure the `ObservableAttribute` aspect.

## Additional Documentation

* [Conceptual documentation](https://doc.postsharp.net/metalama/patterns/observability)
* [API documentation](https://doc.postsharp.net/metalama/api/metalama-patterns-observability)

## Related Packages

* `Metalama.Patterns.Immutability` implements the immutability concept used by the current package to decide which method calls are supported in property getters.