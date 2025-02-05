![Metalama Logo](https://raw.githubusercontent.com/postsharp/Metalama/master/images/metalama-by-postsharp.svg)

Metalama is a Roslyn-based framework for on-the-fly code generation and validation in C#.

Using Metalama, you can:

* **Reduce boilerplate** by generating it dynamically during compilation. Your source code remains crystal-clear.
* **Verify code** in real time against architecture, patterns, and conventions. No need to wait for code reviews.
* **Provide coding assistance** to your team with customized feedback and suggestions.
* **Do it by your rules.** Automate your own implementation patterns and architecture guidelines.

For more information, see the [Metalama website](https://www.postsharp.net/metalama) or [Metalama documentation](https://doc.postsharp.net/metalama).

## About this package

The `Metalama.Framework` package is the principal package of Metalama. Reference this package if you want to use Metalama in your code.

Referencing this package replaces the compiler with `Metalama.Compiler`, a shallow fork of Roslyn that adds an extension point to allow us to transform the syntax trees. If you need to reference Metalama without replacing the compiler, use the `Metalama.Framework.Redist` package.

## Quick Links

- 🌐 [Metalama Website](https://www.postsharp.net/metalama)
- 📖 [Documentation](https://doc.postsharp.net/metalama)
- 📝 [Annotated Examples](https://doc.postsharp.net/metalama/examples)
- 🎥 [Tutorial Videos](https://doc.postsharp.net/metalama/videos)
- 🐞 [Bug Reports](https://github.com/postsharp/Metalama/issues)
- 💬 [Discussions](https://github.com/postsharp/Metalama/discussions)
- 📜 [Detailed Changelog](https://github.com/orgs/postsharp/discussions/categories/changelog)
- 📢 [Release Notes](https://doc.postsharp.net/metalama/conceptual/aspects/release-notes)
- ✨ [Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=PostSharpTechnologies.PostSharp)


## Related packages

| Package Name                                                                                                       | Description |
|--------------------------------------------------------------------------------------------------------------------|-------------|
| [Metalama.Compiler](https://www.nuget.org/packages/Metalama.Compiler)                                               | Metalama Compiler package. Referencing this package directly is not recommended. A fork of Roslyn. |
| [Metalama.Compiler.Sdk](https://www.nuget.org/packages/Metalama.Compiler.Sdk)                                       | Defines APIs for writing source transformers for Metalama.Compiler. |
| [Metalama.Extensions.Architecture](https://www.nuget.org/packages/Metalama.Extensions.Architecture)                 | Verifies code against architecture rules in Metalama. |
| [Metalama.Extensions.DependencyInjection](https://www.nuget.org/packages/Metalama.Extensions.DependencyInjection)   | Allows aspects to consume dependencies from an arbitrary dependency injection framework. |
| [Metalama.Extensions.DependencyInjection.ServiceLocator](https://www.nuget.org/packages/Metalama.Extensions.DependencyInjection.ServiceLocator) | Locates and pulls dependencies from a global service provider. |
| [Metalama.Extensions.Metrics](https://www.nuget.org/packages/Metalama.Extensions.Metrics)                           | Exposes code metrics to the code model in Metalama. |
| [Metalama.Extensions.Multicast](https://www.nuget.org/packages/Metalama.Extensions.Multicast)                       | Emulates PostSharp multicasting within Metalama. |
| [Metalama.Framework](https://www.nuget.org/packages/Metalama.Framework)                                             | The principal package. |
| [Metalama.Framework.Introspection](https://www.nuget.org/packages/Metalama.Framework.Introspection)                 | Provides introspection of Metalama aspect classes, instances, and diagnostics. |
| [Metalama.Framework.Redist](https://www.nuget.org/packages/Metalama.Framework.Redist)                               | Redistributable components for `Metalama.Framework`. Should be installed as a dependency. |
| [Metalama.Framework.Sdk](https://www.nuget.org/packages/Metalama.Framework.Sdk)                                     | A tool for custom source code modifying extensions using the Roslyn API. |
| [Metalama.Framework.Workspaces](https://www.nuget.org/packages/Metalama.Framework.Workspaces)                       | Allows loading solutions and projects, executing Metalama, and introspection of compilation results. |
| [Metalama.Migration](https://www.nuget.org/packages/Metalama.Migration)                                             | Annotated PostSharp API documenting its Metalama equivalent. |
| [Metalama.Patterns.Caching](https://www.nuget.org/packages/Metalama.Patterns.Caching)                               | A caching front-end that can be plugged into different backends and helps with building the cache key, coping with special return types, and invalidating the cache. |
| [Metalama.Patterns.Caching.Aspects](https://www.nuget.org/packages/Metalama.Patterns.Caching.Aspects)               | A set of aspects that simplify the caching: [Cache] to cache a method result as a function of its parameters, [InvalidateCache] to invalidate the cache, or [CacheKey] to mark a cache key. |
| [Metalama.Patterns.Caching.Backend](https://www.nuget.org/packages/Metalama.Patterns.Caching.Backend)               | Backend components for `Metalama.Patterns.Caching`. |
| [Metalama.Patterns.Caching.Backends.Azure](https://www.nuget.org/packages/Metalama.Patterns.Caching.Backends.Azure) | Synchronizes the invalidation of distributed `Metalama.Patterns.Caching` caches over Azure Service Bus. |
| [Metalama.Patterns.Caching.Backends.Redis](https://www.nuget.org/packages/Metalama.Patterns.Caching.Backends.Redis) | Redis back-end for `Metalama.Patterns.Caching`. Implements both caching and cache invalidation over Redis Pub/Sub. |
| [Metalama.Patterns.Contracts](https://www.nuget.org/packages/Metalama.Patterns.Contracts)                           | Contract-Based Programming (or Design-by-Contract) with custom attributes such as [NotNull] or [Url] thanks to Metalama: pre-conditions, post-conditions and invariants. |
| [Metalama.Patterns.Immutability](https://www.nuget.org/packages/Metalama.Patterns.Immutability)                     | Represents the concept of Immutable Type so that it can be used by other packages like `Metalama.Patterns.Observability`. |
| [Metalama.Patterns.Memoization](https://www.nuget.org/packages/Metalama.Patterns.Memoization)                       | Simple and high-performance caching of properties or parameterless methods with a single [Memo] custom attribute thanks to Metalama. |
| [Metalama.Patterns.Observability](https://www.nuget.org/packages/Metalama.Patterns.Observability)                   | Implements the Observable pattern and the INotifyPropertyChanged interface with a single [Observable] attribute thanks to Metalama. Supports both automatic and explicit properties and child objects. |
| [Metalama.Patterns.Xaml](https://www.nuget.org/packages/Metalama.Patterns.Xaml)                                     | Provides custom observability features for XAML applications. |
| [Metalama.Testing.AspectTesting](https://www.nuget.org/packages/Metalama.Testing.AspectTesting)                     | Test framework for Metalama aspects and fabrics. |
| [Metalama.Testing.UnitTesting](https://www.nuget.org/packages/Metalama.Testing.UnitTesting)                         | A package for unit testing compile-time code. |
| [Metalama.Tool](https://www.nuget.org/packages/Metalama.Tool)                                                       | Command line tool for registering a license key or accessing configuration settings. |
| [Metalama.Community.AutoCancellationToken](https://www.nuget.org/packages/Metalama.Community.AutoCancellationToken) | A Metalama weaver that automatically adds CancellationToken to your method definitions and your method calls. |
| [Metalama.Community.Costura](https://www.nuget.org/packages/Metalama.Community.Costura)                             | A Metalama weaver that embeds dependent assemblies as managed resources. A fork of `Costura.Fody`. |
| [Metalama.Community.Costura.Redist](https://www.nuget.org/packages/Metalama.Community.Costura.Redist)               | Redistributable components for package `Metalama.Community.Costura`. Should only be installed as a dependency. |
| [Metalama.Community.Virtuosity](https://www.nuget.org/packages/Metalama.Community.Virtuosity)                       | A Metalama weaver that makes all methods in a type `virtual`. |


## Change log

We maintain a detailed change log on [GitHub](https://github.com/postsharp/Metalama/discussions/categories/changelog).

## Feedback and support

If you have any feedback regarding Metalama, please [open an issue](https://github.com/postsharp/Metalama/issues/new),
 [start a discussion](https://github.com/postsharp/Metalama/discussions/new) on GitHub, or contact us directly at hello@postsharp.net.

 You can also join our Slack community and ask your technical questions in real time.

 [![Slack](https://img.shields.io/badge/Slack-4A154B?label=Chat%20with%20us%20on&style=flat&logo=slack&logoColor=white)](https://www.postsharp.net/slack)

