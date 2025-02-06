![Metalama Logo](https://raw.githubusercontent.com/postsharp/Metalama/master/images/metalama-by-postsharp.svg)

The `Metalama.LinqPad` package allows you to load any C# project or solution into LINQPad and exposes its code model to queries. This package contains an optional driver, samples, and dumping methods.

## Documentation

* [Metalama.LinqPad conceptual documentation](https://doc.postsharp.net/metalama/conceptual/introspection/linqpad).
* [Code model API documentation](https://doc.postsharp.net/metalama/api/metalama-framework-code) for APIs like `IDeclaration`, `INamedType`, `IMethod`, ...
* [Introspection API documentation](https://doc.postsharp.net/metalama/api/introspection-api) for APIs like `Workspace`, `Project`, `IIntrospectionAspectInstance`, `IIntrospectionAspectClass`, `IIntrospectionAspectDiagnostic`, ...

## Example

The following query lists all public methods in a project:

```cs
workspace
    .SourceCode
	.Methods
	.Where( m => m.Accessibility == Accessibility.Public && m.DeclaringType.Accessibility == Accessibility.Public )
	.GroupBy( m => m.DeclaringType.FullName )
	.OrderBy( g => g.Key )
```	



## Related Packages

* `Metalama.Framework.Workspaces` allows you to do code queries in arbitrary projects without LinqPad.
* `Metalama.Framework.Instrospection` exposes concepts like like `Workspace`, `Project`, `IIntrospectionAspectInstance`, `IIntrospectionAspectClass`, `IIntrospectionAspectDiagnostic`, ...
* `Metalama.Framework` exposes the basic code model in the `Metalama.Framework.Code` namespace.