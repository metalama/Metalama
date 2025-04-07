![Metalama Logo](https://raw.githubusercontent.com/metalama/Metalama/master/images/metalama.svg)

## About

The `Metalama.Extensions.Metrics` package implements a few metrics that can be consumed from Metalama aspects and fabrics.

## Principal Types

* `StatementNumber`: counts the number of statements.
* `SyntaxNodeNumber`: counts the number of Roslyn syntax node.

To use a metric in an aspect, use for instance:

```csharp
method.Metrics().Get<StatementNumber>().Value
```

## Documentation

* [Creating and consuming custom metrics](https://doc.metalama.net/conceptual/sdk/custom-metrics)