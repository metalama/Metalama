![Metalama Logo](https://raw.githubusercontent.com/postsharp/Metalama/master/images/metalama-by-postsharp.svg)

The `Metalama.Framework.Implementation` package contains multiple implementation libraries for `Metalama.Framework`. 

Libraries included in this package have the following content:
* `Metalama.Framework.Engine.*` assembly contains the implementation details of `Metalama.Framework` for the latest version of Roslyn. 
* `Metalama.Framework.DesignTime.*` assembly contains design-time implementation logic for the latest version of Roslyn.
* `Metalama.Framework.DesignTime.Contracts` assembly contains Roslyn version-agnostic design-time interface.
* `Metalama.Framework.DesignTime.Rpc` assembly contains design-time communication protocol.
* `Metalama.Framework.CompileTimeContracts`assembly exposes the public API of Metalama to the _transformed_ compile-time user code created with `Metalama.Framework` (e.g. to the compiled code templates).

This package is required by `Metalama.Framework.Workspaces`, `Metalama.Testing.AspectTesting` and `Metalama.Testing.UnitTesting`.

You should normally never reference it directly in your projects.

** All APIs in this package are considered implementation details and are subject to change without notice. **