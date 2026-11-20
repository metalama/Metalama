![Metalama by PostSharp](https://raw.githubusercontent.com/metalama/.github/HEAD/images/metalama.svg)

The `Metalama.LinqPad` package allows you to load any C# project or solution into LINQPad and exposes its code model to queries. This package contains an optional driver, samples, and dumping methods.

## Requirements

The driver targets `net10.0-windows` and loads projects through `Metalama.Framework.Workspaces`, which registers the
MSBuild of a .NET SDK in the LINQPad process. The major version of the runtime that LINQPad runs the query on
therefore decides which SDK can be used: on .NET 10, a .NET 10 SDK must be installed, even if a .NET 11 SDK is
present, because the MSBuild of the .NET 11 SDK targets .NET 11.

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
* `Metalama.Framework.Introspection` exposes concepts like `Workspace`, `Project`, `IIntrospectionAspectInstance`, `IIntrospectionAspectClass`, `IIntrospectionAspectDiagnostic`, ...
* `Metalama.Framework` exposes the basic code model in the `Metalama.Framework.Code` namespace.

## AI Coding Assistants

If you use an AI coding assistant such as Claude Code, install the [Metalama AI Skills](https://github.com/metalama/Metalama.AI.Skills) plugin. It gives the assistant access to the Metalama conceptual documentation, the API reference, and code samples.
