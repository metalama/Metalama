The `Metalama.Extensions.Metrics` package implements a few metrics that can be consumed from Metalama aspects and fabrics.

Currently, the following metrics are implemented:

* `StatementNumber`: counts the number of statements.
* `SyntaxNodeNumber`: counts the number of Roslyn syntax node.

To use a metric in an aspect, use for instance:

```csharp
method.Metrics().Get<StatementNumber>().Value
```
