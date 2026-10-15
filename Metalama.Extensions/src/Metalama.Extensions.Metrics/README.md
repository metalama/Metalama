![Metalama by PostSharp](https://raw.githubusercontent.com/metalama/.github/HEAD/images/metalama.svg)

## About

The `Metalama.Extensions.Metrics` package implements a few metrics that can be consumed from Metalama aspects and fabrics.

## Principal Types

* `StatementNumber`: counts the number of statements.
* `SyntaxNodeNumber`: counts the number of Roslyn syntax nodes.

To use a metric in an aspect, use for instance:

```csharp
method.Metrics().Get<StatementNumber>().Value
```

## Documentation

* [Creating and consuming custom metrics](https://doc.metalama.net/conceptual/sdk/custom-metrics)

## AI Coding Assistants

If you use an AI coding assistant such as Claude Code, install the [Metalama AI Skills](https://github.com/metalama/Metalama.AI.Skills) plugin. It gives the assistant access to the Metalama conceptual documentation, the API reference, and code samples.
