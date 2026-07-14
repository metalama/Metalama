![Metalama by PostSharp](https://raw.githubusercontent.com/metalama/.github/HEAD/images/metalama.svg)

**An open-source patterns & architecture toolkit for C#.**

[Metalama](https://postsharp.net/metalama) lets you define your team's patterns once: the compiler writes the repetitive parts at build time and enforces your rules as you type.

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


## License

Metalama is vendor-led open source: built and maintained by full-time engineers, funded by commercial licenses.

The core framework, which is the large majority of the codebase, is released under the MIT license. It cannot be taken away, relicensed, or paywalled.

Some optional extensions and IDE tooling are released under a proprietary license. 💎

## Features

- [Code Generation](https://postsharp.net/metalama/features/code-generation)
- [Code Validation](https://postsharp.net/metalama/features/code-validation)
- [Architecture Validation](https://postsharp.net/metalama/applications/architecture-verification) 💎
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
- [Changelogs](https://github.com/orgs/metalama/discussions/categories/changelog)
- [Release Notes](https://doc.postsharp.net/metalama/conceptual/release-notes)
- [Metalama Tools for Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=PostSharpTechnologies.PostSharp)

## Quick Start

1. Add the `Metalama.Framework` package to your project:

    ```powershell
    dotnet add package Metalama.Framework
    ```

2. Optionally, install [Metalama Tools for Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=PostSharpTechnologies.PostSharp). It's free for individuals, non-commercial uses, and companies with up to 3 users.

3. Explore the [Metalama Marketplace](https://postsharp.net/metalama/marketplace) for ready-made aspects or examples.

4. Follow the [Getting Started](https://doc.postsharp.net/metalama/conceptual/getting-started) guide to create your first aspect.

## Building Metalama from Source

Please check instructions [here](https://doc.postsharp.net/metalama/contributing/build-from-source).

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


## Related Packages

- [Metalama.Extensions.DependencyInjection](https://www.nuget.org/packages/Metalama.Extensions.DependencyInjection): allows you to inject services into your aspects using a dependency injection framework.
- [Metalama.Extensions.Metrics](https://www.nuget.org/packages/Metalama.Extensions.Metrics): allows your aspects or fabrics to rely on code metrics, e.g. number of lines of code.
- [Metalama.Extensions.Validation](https://www.nuget.org/packages/Metalama.Extensions.Validation) 💎: provides an API allowing to validate code and references, usages and dependencies.
- [Metalama.Extensions.Architecture](https://www.nuget.org/packages/Metalama.Extensions.Architecture) 💎: built on `Metalama.Extensions.Validation`, implements concrete rules for architecture verification.
- [Metalama.Extensions.CodeFixes](https://www.nuget.org/packages/Metalama.Extensions.CodeFixes) 💎: allows you to attach code fix suggestions to errors and warnings or to suggest code refactorings.
- [Metalama.Patterns.*](https://www.nuget.org/packages?q=Metalama.Patterns&includeComputedFrameworks=true&prerel=true&sortby=relevance): a set of ready-made, professionally-built aspect libraries built with `Metalama.Framework`, most of them open-source.
