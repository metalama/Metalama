![Metalama Logo](https://raw.githubusercontent.com/metalama/Metalama/master/images/metalama.svg)

**A Roslyn-based meta-programming framework for code generation, architecture validation and aspect-oriented programming in C#.**

[Metalama](https://metalama.net) lets you write cleaner, more maintainable .NET code by automating repetitive patterns at compile time using aspects, templates, and compile-time logic. It integrates tightly with the Roslyn compiler, providing full control over how your code is built.

## Why Metalama?

- **Write and maintain less code**: Eliminate boilerplate by generating it dynamically during compilation, reducing code lines and bugs by up to 15%.
- **Validate your codebase in real time**: Enforce adherence to design rules and conventions without waiting for code reviews.
- **Excel with large, complex, or legacy codebases**: Metalama shines at scale and does not require architectural changes.

## When to use it?

Metalama is ideal for:

- **Large projects**: Automate repetitive patterns across dozens of entities and hundreds of properties or methods.
- **Large teams**: Align developers on consistent patterns and practices.
- **Long lifecycle projects**: Maintain quality over years of development.

It's main use cases are:

- **Design Patterns**: [Singleton](https://metalama.net/applications/design-patterns/classic-singleton), [Memento](https://metalama.net/applications/design-patterns/memento), [Factory](https://metalama.net/applications/design-patterns/factory), [Builder](https://metalama.net/applications/design-patterns/builder), [Decorator](https://metalama.net/applications/design-patterns/decorator), [Proxy](https://metalama.net/applications/design-patterns/proxy), ...
- **UI Patterns**: [INotifyPropertyChanged](https://metalama.net/applications/ui/inotifypropertychanged), [Change Tracking](https://metalama.net/applications/ui/command), [Memoization](https://metalama.net/applications/ui/memoization), [Undo/Redo](https://metalama.net/applications/ui/undo-redo), [Command](https://metalama.net/applications/ui/command), [Dependency Properties](https://metalama.net/applications/ui/dependency-property), ...
- **Object Services**: Cloning, ToString, Comparison, ...
- **Defensive Programming**: [Code Contracts](https://metalama.net/applications/contracts) (preconditions, post-conditions, invariants)
- **DevOps**: [Logging & Tracing](https://metalama.net/applications/devops/logging), [Metrics](https://metalama.net/applications/devops/metrics), [Caching](https://metalama.net/applications/devops/caching), [Exception Handling](https://metalama.net/applications/devops/exception-handling)


---

## License

Metalama is released under the MIT license.

Some optional extensions and IDE tooling are released under a proprietary license. 💎

---

## Features

- [Code Generation](https://metalama.net/features/core/code-generation)
- [Code Validation](https://metalama.net/features/core/code-validation)
- [Architecture Validation](https://metalama.net/features/core/architecture-verification) 💎
- [Immediate Editor Feedback](https://metalama.net/features/core/architecture-verification)
- [Code Fix Toolkit](https://metalama.net/features/core/code-fixes) 💎
- [Ready-to-Use Aspect Libraries](https://metalama.net/features/productivity/aspect-libraries)
- [Visual Studio Tooling](https://metalama.net/features/productivity/tooling) 💎
- [Test Frameworks](https://metalama.net/features/productivity/testing)
- [Debugging of Transformed Code](https://metalama.net/features/productivity/debugging)
- [Roslyn Extensibility SDK](https://metalama.net/features/extensibility/roslyn)
- [Code Query API](https://metalama.net/features/extensibility/code-query)

---

## Resources

- 🌐 [Metalama Website](https://metalama.net)
- 📖 [Documentation](https://doc.metalama.net)
- 📝 [Annotated Examples](https://doc.metalama.net/examples)
- 📜 [Changelogs](https://github.com/orgs/metalama/discussions/categories/changelog)
- 📢 [Release Notes](https://doc.metalama.net/conceptual/aspects/release-notes)
- ✨ [Metalama Tools for Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=PostSharpTechnologies.PostSharp)

---

## Quick Start

1. Add the `Metalama.Framework` package to your project:

    ```powershell
    dotnet add package Metalama.Framework
    ```

2. Optionally, install [Metalama Tools for Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=PostSharpTechnologies.PostSharp). It's free for individuals, non-commercial uses, and companies with up to 3 users.

3. Explore the [Metalama Marketplace](https://metalama.net/marketplace) for ready-made aspects or examples.

4. Follow the [Getting Started](https://doc.metalama.net/conceptual/getting-started) guide to create your first aspect.

---

## Building Metalama from Source

Please check instructions [here](https://metalama.net/contributing/build-from-source).

---

## Contributing

We welcome contributions! Here's how you can help:

- ⭐ Star this repository or review us on [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=PostSharpTechnologies.PostSharp).
- 📝 Write a blog post or record a video about Metalama.
- 📦 Share your aspects on the [Metalama Marketplace](https://metalama.net/marketplace).
- 🛠️ Contribute to [Metalama.Community](https://github.com/metalama/Metalama.Community).
- 📚 Improve the documentation. [Learn how](https://metalama.net/contributing/contribute-docs).
- 🐛 Fix bugs or contribute code. [Learn how](https://metalama.net/contributing/contribute-code).

For more details, see [Contributing to Metalama](https://metalama.net/contributing).

---

## Support & Community

- Report issues on GitHub. Follow [these recommendations](https://metalama.net/contributing/file-an-issue).
- Join [GitHub discussions](https://github.com/orgs/metalama/discussions) for questions and proposals.
- Enterprise support is available. Learn more about [premium support](https://metalama.net/premium/enterprise-support). 💎

---

## Related Repositories

| Repository                                                                 | License          | Description                                                                 |
| ------------------------------------------------------------------------- | ---------------- | --------------------------------------------------------------------------- |
| [Metalama.Compiler](https://github.com/metalama/Metalama.Compiler)        | MIT              | A [Roslyn](https://github.com/dotnet/roslyn) fork for source code transformations. |
| [Metalama.Premium](https://github.com/metalama/Metalama.Premium) 💎         | Proprietary      | Extensions available to customers with a paid license.                      |
| [PostSharp.Engineering](https://github.com/postsharp/PostSharp.Engineering) | MIT              | A custom multi-repo build and CI framework.                                 |
| [Metalama.Community](https://github.com/metalama/Metalama.Community)     | MIT              | Community-contributed aspects repository.                                   |
| [Metalama.Documentation](https://github.com/metalama/Metalama.Documentation) | MIT              | Source for documentation hosted on [Metalama Docs](https://doc.metalama.net/). |
| [Metalama.Samples](https://github.com/metalama/Metalama.Samples)          | MIT              | Illustrative samples available at [Metalama Examples](https://doc.metalama.net/examples). |
