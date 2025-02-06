## About

The `Flashtrace.Formatters` package is designed to solve the problem of formatting objects into strings, with a focus on performance and extensibility.

## Key Features

* High performance: rendering into preallocated buffers without short-term memory allocation.
* Run-time extensibility. You can add formatters for third-party types.
* Multi-role support: an object can render itself differently depending on the context, such as logging or inclusion in a cache key.

## Main Types

* [IFormattable](https://doc.postsharp.net/metalama/api/flashtrace-formatters-iformattable-1) is an interface that can be implemented by any object that must be able to format itself.
* [Formatter](https://doc.postsharp.net/metalama/api/flashtrace-formatters-formatter-1) is an abstract class that can format a third-party object, where `IFormattable` cannot be implemented.
* [FormatterRepository](https://doc.postsharp.net/metalama/api/flashtrace-formatters-formatterrepository) is a collection of `Formatter` objects.
* [UnsafeStringBuilder](https://doc.postsharp.net/metalama/api/flashtrace-formatters-unsafestringbuilder) operates similarly to `StringBuilder`, but writes into a preallocated buffer.

## Additional Documentation

* [API Documentation](https://doc.postsharp.net/metalama/api/flashtrace-formatters)

## Related Packages

* `Metalama.Patterns.Caching` is currently the only package that utilizes `Flashtrace.Formatters`.
