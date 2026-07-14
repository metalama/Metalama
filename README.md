<p align="center">
  <img width="450" src="https://raw.githubusercontent.com/metalama/.github/HEAD/images/metalama.svg" alt="Metalama by PostSharp" />
</p>

[![OpenSSF Best Practices](https://www.bestpractices.dev/projects/10558/badge)](https://www.bestpractices.dev/projects/10558) 
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/metalama/Metalama/badge)](https://scorecard.dev/viewer/?uri=github.com/metalama/Metalama)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Metalama.Compiler)](https://www.nuget.org/packages?q=Metalama&includeComputedFrameworks=true&prerel=true&sortby=totalDownloads-desc)
[![GitHub Release](https://img.shields.io/github/v/release/metalama/Metalama)](https://github.com/metalama/Metalama/releases)


**Metalama is an open-source patterns & architecture toolkit for C#.**

Define your team's patterns once: the compiler writes the repetitive parts at build time and enforces your rules as you type.

## Why Metalama?

- **Write the pattern once, apply it everywhere**: Aspects generate the repetitive code at compile time; the boilerplate never lands in the repo, so it never needs review or maintenance.
- **Enforce architecture as you type**: Dependency rules, naming conventions, and pattern guidelines in plain C#, with real-time IDE feedback long before the pull request.
- **Stay consistent in the AI era**: Hand-written or AI-generated, every line is checked against your rules; a pattern change is one file edit and the whole codebase follows at the next build.

Built on Roslyn by the PostSharp team, who have been doing compiler-level meta-programming in .NET since 2004.

## When to use it?

Metalama is ideal for:

- **Large projects**: Automate repetitive patterns across dozens of entities and hundreds of properties or methods.
- **Large teams**: Align developers on consistent patterns and practices.
- **Long lifecycle projects**: Maintain quality over years of development.

Its main use cases are:

- **Design Patterns**: [Singleton](https://postsharp.net/metalama/applications/classic-singleton), [Memento](https://postsharp.net/metalama/applications/memento), [Factory](https://postsharp.net/metalama/applications/factory), [Builder](https://postsharp.net/metalama/applications/builder), [Decorator](https://postsharp.net/metalama/applications/decorator), [Proxy](https://postsharp.net/metalama/applications/proxy), ...
- **UI Patterns**: [INotifyPropertyChanged](https://postsharp.net/metalama/applications/inotifypropertychanged), [Change Tracking](https://postsharp.net/metalama/applications/change-tracking), [Memoization](https://postsharp.net/metalama/applications/memoization), [Undo/Redo](https://postsharp.net/metalama/applications/undo-redo), [Command](https://postsharp.net/metalama/applications/command), [Dependency Properties](https://postsharp.net/metalama/applications/dependency-property), ...
- **Object Services**: [Cloning](https://postsharp.net/metalama/applications/cloning), [ToString](https://postsharp.net/metalama/applications/tostring), [Comparison](https://postsharp.net/metalama/applications/equatable), ...
- **Defensive Programming**: [Code Contracts](https://postsharp.net/metalama/applications/contracts) (preconditions, post-conditions, invariants)
- **DevOps**: [Logging & Tracing](https://postsharp.net/metalama/applications/logging), [Metrics](https://postsharp.net/metalama/applications/metrics), [Caching](https://postsharp.net/metalama/applications/caching), [Exception Handling](https://postsharp.net/metalama/applications/exception-handling)
- [Architecture Validation](https://postsharp.net/metalama/applications/architecture-verification) 💎
- [Refactoring](https://postsharp.net/metalama/applications/refactoring)
- In general, [Clean Code](https://postsharp.net/metalama/applications/clean-code) and [SOLID & DRY Principles](https://postsharp.net/metalama/applications/solid)


## License

Metalama is vendor-led open source: built and maintained by full-time engineers, funded by commercial licenses.

The core framework, which is the large majority of the codebase, is released under the [MIT license](LICENSE.md). It cannot be taken away, relicensed, or paywalled.

Some optional extensions and IDE tooling are released under a proprietary license and are marked with a diamond 💎 symbol.

## Features

- [Code Generation](https://postsharp.net/metalama/features/code-generation)
- [Code Validation](https://postsharp.net/metalama/features/code-validation)
- [Immediate Editor Feedback](https://postsharp.net/metalama/features/design-time-feedback)
- [Code Fix Toolkit](https://postsharp.net/metalama/features/code-fixes) 💎
- [Ready-to-Use Aspect Libraries](https://postsharp.net/metalama/features/aspect-libraries)
- [Visual Studio Tooling](https://postsharp.net/metalama/features/tooling) 💎
- [Test Frameworks](https://postsharp.net/metalama/features/testing)
- [Debugging of Transformed Code](https://postsharp.net/metalama/features/debugging)
- [Roslyn Extensibility SDK](https://postsharp.net/metalama/features/roslyn)
- [Code Query API](https://postsharp.net/metalama/features/code-query)

## Resources

- [Metalama Website](https://postsharp.net/metalama)
- [Documentation](https://doc.postsharp.net/metalama)
- [Annotated Examples](https://doc.postsharp.net/metalama/examples)
- [Release Notes](https://doc.postsharp.net/metalama/conceptual/release-notes)
- [Builds](https://github.com/metalama/Metalama/releases)
- [Metalama Tools for Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=PostSharpTechnologies.PostSharp)

## Quick Start

1. Add the `Metalama.Framework` package to your project:

    ```powershell
    dotnet add package Metalama.Framework
    ```

2. Optionally, install [Metalama Tools for Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=PostSharpTechnologies.PostSharp). It's free for individuals, non-commercial uses, and companies with up to 3 users.

3. Explore the [Metalama Marketplace](https://postsharp.net/metalama/marketplace) for ready-made aspects or examples.

4. Follow the [Getting Started](https://doc.postsharp.net/metalama/conceptual/getting-started) guide to create your first aspect.

## Contributing

Contributions are accepted through the following channels:

- Share your aspects on the [Metalama Marketplace](https://postsharp.net/metalama/marketplace).
- Contribute aspects to [Metalama.Community](https://github.com/metalama/Metalama.Community).
- Improve the documentation. [Learn how](https://doc.postsharp.net/metalama/contributing/contribute-docs).
- Fix bugs or contribute code. [Learn how](https://doc.postsharp.net/metalama/contributing/contribute-code).

For more details, see [Contributing to Metalama](https://doc.postsharp.net/metalama/contributing).

## Support

- Report issues on GitHub. Follow [these recommendations](https://doc.postsharp.net/metalama/contributing/file-an-issue).
- Ask questions and submit proposals in [GitHub discussions](https://github.com/orgs/metalama/discussions).
- Enterprise support is available. Learn more about [premium support](https://postsharp.net/metalama/premium/enterprise-support). 💎

## Packages

Below is a list of packages originating from this repository:

| Package Name                                                                                          |Description                                                                                           |
|-------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| [Metalama.Framework](https://www.nuget.org/packages/Metalama.Framework/)                             |  This is the public API of the Metalama Framework. It incorporates a reference to Metalama.Compiler, effectively replacing the Roslyn compiler with our custom version.  |
| [Metalama.Testing.UnitTesting](https://www.nuget.org/packages/Metalama.Testing.UnitTesting/)          |  Provides base classes and utilities for unit testing compile-time code.   |
| [Metalama.Testing.AspectTesting](https://www.nuget.org/packages/Metalama.Testing.AspectTesting/)      |  A framework based on xUnit for testing code generation by aspects.   |
| [Metalama.Framework.Redist](https://www.nuget.org/packages/Metalama.Framework.Redist/)               |  Similar to `Metalama.Framework`, but excludes the dependency on `Metalama.Compiler`.   |
| [Metalama.Framework.Sdk](https://www.nuget.org/packages/Metalama.Framework.Sdk/)                     |  Facilitates the use of the Roslyn API from aspects.   |
| [Metalama.Framework.Engine](https://www.nuget.org/packages/Metalama.Framework.Engine/)               |  This is the core implementation of `Metalama.Framework`. Direct referencing of this package is discouraged and unsupported. It's intended to be a dependency for `Metalama.Testing.AspectTesting`.    |
| [Metalama.Framework.CompileTimeContracts](https://www.nuget.org/packages/Metalama.Framework.CompileTimeContracts/) |  Defines the public API between compiled T# templates and `Metalama.Framework.Engine`.  |
| [Metalama.Framework.Introspection](https://www.nuget.org/packages/Metalama.Framework.Introspection/) |  Provides an API to inspect the object model that represents the compilation process of `Metalama.Framework`, such as aspect and advice instances, as well as its results.  |
| [Metalama.Framework.Workspaces](https://www.nuget.org/packages/Metalama.Framework.Workspaces/)       |  A supplementary API to `Metalama.Framework.Introspection`, designed to facilitate the loading of Visual Studio projects and solutions. This package is also useful to inspect projects that don't use Metalama. It is used by `Metalama.LinqPad`.   |
| [Metalama.Tool](https://www.nuget.org/packages/Metalama.Tool/)                                       |  The `metalama` tool for the .NET CLI.   |
| [Metalama.Extensions.DependencyInjection](https://www.nuget.org/packages/Metalama.Extensions.DependencyInjection/) | A framework that allows aspects to consume dependencies from an arbitrary dependency injection framework. |
| [Metalama.Extensions.Metrics](https://www.nuget.org/packages/Metalama.Extensions.Metrics/)           | Implements code metrics that can be consumed by aspects and fabrics. |
| [Metalama.Extensions.Multicast](https://www.nuget.org/packages/Metalama.Extensions.Multicast/)       | Reproduces PostSharp attribute multicasting in Metalama, for teams porting business code from one to the other. |
| [Metalama.Extensions.Architecture](https://www.nuget.org/packages/Metalama.Extensions.Architecture/) | Allows you to validate the source code against architecture rules. |
| [Metalama.Patterns.Caching](https://www.nuget.org/packages/Metalama.Patterns.Caching/)               | Caching framework for Metalama.                                                         |
| [Metalama.Patterns.Caching.Aspects](https://www.nuget.org/packages/Metalama.Patterns.Caching.Aspects/) | Aspects designed for Metalama caching, building upon `Metalama.Patterns.Caching`.                     |
| [Metalama.Patterns.Caching.Backend](https://www.nuget.org/packages/Metalama.Patterns.Caching.Backend/) | Provides an abstraction over caching backends, including an in-memory caching implementation.         |
| [Metalama.Patterns.Contracts](https://www.nuget.org/packages/Metalama.Patterns.Contracts/)           | Code contract aspects like `[NotNull]`, `[Url]` for contract-based programming.                       |
| [Metalama.Patterns.Immutability](https://www.nuget.org/packages/Metalama.Patterns.Immutability/)     | Represents the concept of Immutable Type so that it can be used by other packages like Metalama.Patterns.Observability. |
| [Metalama.Patterns.Memoization](https://www.nuget.org/packages/Metalama.Patterns.Memoization)         | Implements a memoization aspect, i.e., simple, low-overhead caching.                                  |
| [Metalama.Patterns.Observability](https://www.nuget.org/packages/Metalama.Patterns.Observability)     | A Metalama aspect implementing `INotifyPropertyChanged`.                                              |
| [Metalama.Patterns.Wpf](https://www.nuget.org/packages/Metalama.Patterns.Wpf)                         | Aspects that implement WPF dependency properties and commands.                                        |
| [Metalama.LinqPad](https://www.nuget.org/packages/Metalama.LinqPad/)                                 | Provides integration with LINQPad for inspecting projects and solutions.                              |
| [Flashtrace](https://www.nuget.org/packages/Flashtrace)                                               | A structured tracing library used by `Metalama.Patterns.Caching`.                                     |
| [Flashtrace.Formatters](https://www.nuget.org/packages/Flashtrace.Formatters)                         | Object formatters used in caching and logging.                                                        |

## Related Repositories

| Repository                                                                 | License          | Description                                                                 |
| ------------------------------------------------------------------------- | ---------------- | --------------------------------------------------------------------------- |
| [Metalama.Compiler](https://github.com/metalama/Metalama.Compiler)        | MIT              | A [Roslyn](https://github.com/dotnet/roslyn) fork for source code transformations. |
| [PostSharp.Engineering](https://github.com/postsharp/PostSharp.Engineering) | MIT              | A custom multi-repo build and CI framework.                                 |
| [Metalama.Community](https://github.com/metalama/Metalama.Community)     | MIT              | Community-contributed aspects repository.                                   |
| [Metalama.Documentation](https://github.com/metalama/Metalama.Documentation) | MIT              | Source for documentation hosted on [Metalama Docs](https://doc.postsharp.net/metalama/). |
| [Metalama.Samples](https://github.com/metalama/Metalama.Samples)          | MIT              | Illustrative samples available at [Metalama Examples](https://doc.postsharp.net/metalama/examples). |
| [Metalama.Premium](https://github.com/metalama/Metalama.Premium)  💎       | Proprietary      | Extensions available to customers with a commercial license.                      |

## Dependencies

Direct and indirect dependencies, as well as their licensing, are documented in [Third Party Notices](THIRD-PARTY-NOTICES.md).
