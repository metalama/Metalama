![Metalama Logo](https://raw.githubusercontent.com/postsharp/Metalama/master/images/metalama-by-postsharp.svg)

## About

The `Metalama.Patterns.Immutability` implements the concept of immutable type, i.e. types whose value cannot be changed after they have been created. Other packages, such as `Metalama.Patterns.Observability`, can then rely on this concept to achieve a better understanding of the code.

## Key Features

* You can mark types as immutable using the `[Immutable]` custom attribute or the `ConfigreImmutability` fabric extension warning.
* Concept of _shallow_ or _deep_ immutability.
* The `[Immutable]` aspect reports warnings if some fields or properties are not mutable.


## Main Types

The primary types in this package are:

* `ImmutabilityKind` represents the kind of immutability of a type, i.e. `None`, `Shallow` or `Deep`.
* `ImmutableAttribute` is an aspect that marks a type as immutable and reports warnings if some fields or properties are mutable.
* `ImmutabilityExtensions.GetImmutability` is an extension of `INamedType` to get the `ImmutabilityKind` of a given type.
* `ImmutabilityConfigurationExtensions.ConfigureImmutability` is a fabric extension method to programmatically set the `ImmutabilityKind` of types.
* `IImmutabilityClassifier` is an abstraction whose implementations can be passed to `ImmutabilityConfigurationExtensions.ConfigureImmutability` to dynamically return the `ImmutabilityKind` of a kind. It is useful when the `ImmutabilityKind` depends on type arguments.

## Additional Documentation

* [Conceptual documentation](https://doc.postsharp.net/metalama/patterns/immutability)
* [API documentation](https://doc.postsharp.net/metalama/api/metalama-patterns-immutability)

## Related Packages

* `Metalama.Patterns.Observability` relies on `Metalama.Patterns.Immutability` to decide which method calls are supported in property getters.


