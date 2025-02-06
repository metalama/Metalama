![Metalama Logo](https://raw.githubusercontent.com/postsharp/Metalama/master/images/metalama-by-postsharp.svg)

## About

The `Metalama.Patterns.Contracts` package implements the concept of contract-based programming, encompassing preconditions, postconditions, and invariants.

## Key Features

* Implementation of preconditions and postconditions through a wide range of numeric or string-based contracts.
* Verification of invariants.

## Main Types

Below is a list of available contract attributes for your selection:

### Nullability Contracts

* `[NotNull]` verifies that the assigned value is not `null`.
* `[Required]` contract verifies that the value is not `null` and requires the string to be non-empty.

### String Contracts

* `[NotEmpty]` requires the string to be non-empty. Note that this contract does not validate the string against being null. If you want to prohibit both null and empty strings, use `[Required]`.
* `[CreditCard]` validates that the string is a valid credit card number.
* `[Email]`, `[Phone]`, and `[Url]` validate strings against well-known regular expressions.
* `[Regex]` validates a string against a custom regular expression.
* `[StringLength]` validates that the length of a string falls within a specified range.

### Enum Contracts

* `[EnumDataType]` contract can validate values of type `string`, `object`, or of any integer type. It throws an exception if the value is not valid for the given `enum` type.

### Numeric Contracts

The following contracts can be used to verify that a value falls within a specified range:

* `[LessThan]` verifies that the value is less than or equal to the specified maximum.
* `[GreaterThan]` verifies that the value is greater than or equal to the specified minimum.
* `[Negative]` verifies that the value is less than or equal to zero.
* `[Positive]` verifies that the value is greater than or equal to zero.
* `[Range]` verifies that the value is greater than or equal to a specified minimum and less than or equal to a specified maximum.
* `[StrictlyLessThan]` verifies that the value is strictly less than the specified maximum.
* `[StrictlyGreaterThan]` verifies that the value is strictly greater than the specified minimum.
* `[StrictlyNegative]` verifies that the value is strictly less than zero.
* `[StrictlyPositive]` verifies that the value is strictly greater than zero.
* `[StrictRange]` verifies that the value is strictly greater than a specified minimum and strictly less than a specified maximum.

### Collections Contracts

* `[NotEmpty]` contract can be used on any collection, including arrays or immutable arrays. It requires the collection or the array to contain at least one element.

### Invariants

* `[Invariant]` causes the target method (a parameterless `void` method) to be invoked after each public method or property setter.
* `[DoNotCheckInvariants]` exempts the target method from enforcing invariants, but does _not_ exempt any method or property setter used by this method.
* `[SuspendInvariant]` exempts the target method from enforcing invariants, _including_ any method or property setter used by this method.

## Additional Documentation

* Conceptual documentation:
    * [Preconditions and postconditions](https://doc.postsharp.net/metalama/patterns/contracts/adding-contracts)
    * [Invariants](https://doc.postsharp.net/metalama/patterns/contracts/invariants)
* [API documentation](https://doc.postsharp.net/metalama/api/metalama-patterns-contracts)
