<p align="center">
<img width="450" src="https://github.com/postsharp/Metalama/raw/master/images/metalama-by-postsharp.svg" alt="Metalama logo" />
</p>


# Metalama.Framework

This repository houses the core framework implementation of [Metalama](https://github.com/postsharp/Metalama).

> [!WARNING]
> This repository operates under a [source-available commercial license](LICENSE.md). While you can refer to the source code for reference and troubleshooting purposes, modifications and builds from this source code are prohibited. To modify and build from the source code, a separate Source Subscription must be acquired.

## Packages

Below are the NuGet packages associated with this repository:

| Package                                                                                                            |  Description   |
| ------------------------------------------------------------------------------------------------------------------ | -------------- |
| [Metalama.Framework](https://www.nuget.org/packages/Metalama.Framework/)                                           |  This is the public API of the Metalama Framework. It incorporates a reference to Metalama.Compiler, effectively replacing the Roslyn compiler with our custom version.  |
| [Metalama.Testing.UnitTesting](https://www.nuget.org/packages/Metalama.Testing.UnitTesting/)                           |  Provides base classes and utilities for unit testing compile-time code.   |
| [Metalama.Testing.AspectTesting](https://www.nuget.org/packages/Metalama.Testing.AspectTesting/)                     |  A framework based on xUnit for testing code generation by aspects.   |
| [Metalama.Framework.Redist](https://www.nuget.org/packages/Metalama.Framework.Redist/)                             |  Similar to `Metalama.Framework`, but excludes the dependency on `Metalama.Compiler`.   |
| [Metalama.Framework.Sdk](https://www.nuget.org/packages/Metalama.Framework.Sdk/)                                   |  Facilitates the use of the Roslyn API from aspects.   |
| [Metalama.Framework.Engine](https://www.nuget.org/packages/Metalama.Framework.Engine/)                             |  This is the core implementation of `Metalama.Framework`. Direct referencing of this package is discouraged and unsupported. It's intended to be a dependency for `Metalama.Testing.AspectTesting`.    |
| [Metalama.Framework.CompileTimeContracts](https://www.nuget.org/packages/Metalama.Framework.CompileTimeContracts/) |  Defines the public API between compiled T# templates and `Metalama.Framework.Engine`.  |
| [Metalama.Framework.Introspection](https://www.nuget.org/packages/Metalama.Framework.Introspection/)               |  Provides an API to inspect the object model that represents the compilation process of `Metalama.Framework`, such as aspect and advice instances, as well as its results.  |
| [Metalama.Framework.Workspaces](https://www.nuget.org/packages/Metalama.Framework.Workspaces/)                     |  A supplementary API to `Metalama.Framework.Introspection`, designed to facilitate the loading of Visual Studio projects and solutions. This package is also useful to inspect projects that don't use Metalama. It is used by `Metalama.LinqPad`.   |
| [Metalama.Tool](https://www.nuget.org/packages/Metalama.Tool/)                                                     |  The `metalama` tool for the .NET CLI.   |
