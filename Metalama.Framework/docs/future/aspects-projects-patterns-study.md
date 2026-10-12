# Applying the aspects topology to Metalama.Patterns

This document studies how the libraries of `Metalama.Patterns` would be split into a run-time package and an
aspects package under the designs of [aspects projects](aspects-projects.md) and
[attribute-bound aspects](attribute-bound-aspects.md), and lists the gaps that the split exposes in those
designs. It is a study, not a plan. The file and line references describe `develop/2027.0` in October 2026, with
paths relative to `Metalama.Patterns/src/` unless stated otherwise.

## 1. What the split means for a pattern library

A pattern library today is one assembly that mixes four kinds of types:

1. Custom attributes that the user applies, which are the aspect classes themselves (`[NotNull]` derives from
   `ContractAspect`, `[Observable]` implements `IAspect<INamedType>`), and configuration attributes, which
   implement `IHierarchicalOptionsProvider`.
2. Run-time types that the woven code calls: exception types, helpers, command classes.
3. Compile-time types: templates, options, fabric extension methods, diagnostics, implementation.
4. Types shared by both, marked `[RunTimeOrCompileTime]`: enums used as attribute arguments, configuration
   records that the woven code also instantiates.

The user references the one assembly, applies the attributes, and the business assembly keeps a metadata
dependency on the library and on `Metalama.Framework.dll`, because the attributes derive from framework types.
The second exploration report confirmed that the woven code of Contracts, Memoization, Observability,
Immutability and Wpf never calls a `Metalama.Framework` API: the dependency is metadata only.

Under the aspects topology, each library becomes two packages:

| Package | Content | References | Who references it |
|---|---|---|---|
| `Metalama.Patterns.X.Redist` | The attributes as plain types marked `[RunTimeOrCompileTime]`, the run-time helpers, the shared enums and records. No reference to `Metalama.Framework.dll`. | `Metalama.Framework.Redist`; `Metalama.Framework` privately when built with Metalama (section 4) | The business project, as an ordinary reference; flows to its consumers |
| `Metalama.Patterns.X.Compiler` | `metalama/<tfm>/Metalama.Patterns.X.Aspects.dll`: the aspect classes bound to the attributes with `[AppliedBy]`, the options, the options providers bound to the configuration attributes, the templates, the fabric extension methods; registered by the props of the package, never a compile reference | `Metalama.Patterns.X.Redist` at an exact version; the `Compiler` packages of the libraries its aspects use | The business project, as an ordinary reference that flows to its consumers; the business project references `Metalama.Framework` privately |
| `Metalama.Patterns.X` | `lib/<tfm>/Metalama.Patterns.X.Aspects.dll`, the same assembly as a compile reference | `Metalama.Patterns.X.Redist`, `Metalama.Framework` flowing | A project that writes fabrics or configuration code against the library, or that wants the one-reference experience of today |

The convention is the one of section 6.2 of the aspects projects document: `Redist` for the run-time package,
`Compiler` for the package that the compiler loads, the unsuffixed name for the package that compile-time code
is written against. Caching is the exception for its run-time package: `Metalama.Patterns.Caching` is a product
with real content that is usable without Metalama and is documented as such, so it keeps its name, as do
`Metalama.Patterns.Caching.Backend` and `Metalama.Patterns.Caching.Aspects`, which already has the role of the
unsuffixed package; Caching gains `Metalama.Patterns.Caching.Compiler`. The rule is that a run-time package
takes the `Redist` suffix when its content is only what the aspects need; a run-time package with a life of its
own keeps its name. The word "contracts project" of the aspects projects document collides with
`Metalama.Patterns.Contracts`, the code contracts library, so this document says "run-time package" or
"`Redist` package".

## 2. The libraries today

The inventory comes from the two exploration reports and was spot-checked in the sources.

### 2.1. Caching

Already split, and the most instructive case because the split is incomplete in three ways.

- `Metalama.Patterns.Caching` and `Metalama.Patterns.Caching.Backend` declare that they do not require Metalama,
  and neither package depends on `Metalama.Framework.Redist`, but both assemblies reference `Metalama.Framework.dll`:
  for `[RunTimeOrCompileTime]` on `CacheItemConfiguration`, `ICacheItemConfiguration`, `CacheItemPriority`,
  `CachedMethodConfiguration` and `CachingAssertionFailedException`, for `[RunTime]` on `CachingProfile`, for
  `RunTimeAspectHelper.BufferAsync` and `AsyncEnumerableList<T>` in the async enumerable adapters
  (`Metalama.Patterns.Caching/ValueAdapters/AsyncEnumerableAdapter.cs:6,21`,
  `AsyncEnumeratorAdapter.cs:7,22,26`), and for an internal `ProjectFabric` that calls
  `VerifyNotNullableDeclarations` from Contracts (`Metalama.Patterns.Caching/Fabric.cs:6-15`). The framework
  reaches them through `Metalama.Patterns.Contracts` with `PrivateAssets="all"`, so they are built with Metalama
  and embed a compile-time project of their shared types, and their packages hide the dependency. A consumer of
  the run-time package alone can fail at run time on an async enumerable. This is a packaging defect today and
  is exactly the dependency that `Metalama.Framework.Redist` absorbs.
- The user-facing attributes `[Cache]`, `[InvalidateCache]`, `[CacheKey]` and `[CachingConfiguration]` live in
  the aspects package (`Metalama.Patterns.Caching.Aspects/CacheAttribute.cs:37-39`,
  `CachingConfigurationAttribute.cs:25-28`); only `[NotCacheKey]` is in the run-time package. The business
  assembly therefore references the aspects package, and through it `Metalama.Framework.dll` and
  `Metalama.Extensions.DependencyInjection`.
- `AsyncEnumerableHelper`, a run-time class that the templates call for async enumerables, is in the aspects
  package (`Metalama.Patterns.Caching.Aspects/Helpers/AsyncEnumerableHelper.cs:14-17`).

Configuration has three layers: the attributes, which are options providers (`CachingBaseAttribute`,
`CachingBaseAttribute.cs:26-29,106`); the `ConfigureCaching` fabric extensions
(`Configuration/CachingConfigurationExtensions.cs:19-70`); and run-time profiles (`CachingProfile`), with
compile-time values taking precedence. `CachingOptions` is `[RunTimeOrCompileTime]` and implements the run-time
`ICacheItemConfiguration`, which is the reason the run-time package must expose compile-time-usable types.

### 2.2. Contracts

One assembly. Every contract is an aspect attribute deriving from `ContractBaseAttribute : ContractAspect,
IConditionallyInheritableAspect` (`Metalama.Patterns.Contracts/ContractBaseAttribute.cs:27`), with `Direction`
typed as the framework enum `ContractDirection` (`:67`). `[Invariant]` and `[SuspendInvariants]` are aspects,
`[DoNotCheckInvariants]` is a plain attribute matched by type.

Woven code calls `PostconditionViolationException`, `ContractHelpers`, `EnumDataTypeAttributeHelper`,
`Numeric.NumberComparer`, `InvariantSuspensionCounter` and `SuspendInvariantsCookie`, all free of framework
dependencies except `[Durable]` on `InvariantSuspensionCounter`. `NumericRange` and `NumericBound` are
`[RunTimeOrCompileTime]` and `ICompileTimeSerializable` because `RangeAttribute` stores a `NumericRange` field.

Configuration is `ContractOptions` (`ContractOptions.cs:21-23`) set through `ConfigureContracts` fabric
extensions, plus `ContractTemplates` as the template override hook. There is no configuration attribute. The
only run-time configuration is the settable statics of `ContractHelpers`.

### 2.3. Memoization

One file. `MemoizeAttribute : Attribute, IAspect<IMethod>, IAspect<IProperty>`. Woven code references only
`Interlocked` and `StrongBox<T>`. No configuration.

### 2.4. Observability

One assembly. `[Observable]` is an inheritable aspect; `[NotObservable]` and `[ObservedExpressions]` are plain
attributes, the second one emitted by the woven code on the introduced `OnChildPropertyChanged` methods and
read back by derived classes compiled later (`Implementation/ClassicObservabilityStrategyImpl.cs:337-339,683`);
`[Constant]` and `[SuppressObservabilityWarnings]` are configuration attributes implementing
`IHierarchicalOptionsProvider`. Woven code references `INotifyPropertyChanged` and `EqualityComparer<T>` only.
Options are `ObservabilityOptions`, `ClassicObservabilityStrategyOptions` and `DependencyAnalysisOptions`, all
internal, set through `ConfigureObservability`; the default verbosity comes from the MSBuild property
`InpcDiagnosticCommentVerbosity` through `GetDefaultOptions` (`Configuration/ObservabilityOptions.cs:52-66`).

### 2.5. Immutability

One assembly. `[Immutable(kind)]` is both an inheritable aspect and an options provider; `ImmutabilityKind` is a
`[RunTimeOrCompileTime]` enum. No code is generated; the aspect reports diagnostics. Options are set through
`ConfigureImmutability` with a kind or an `IImmutabilityClassifier`, and a `TransitiveProjectFabric` classifies
`System.Collections.Immutable` for consumers. Observability depends on it at compile time.

### 2.6. Wpf

One assembly, `net472;net10.0-windows`. `[Command]` and `[DependencyProperty]` are both aspects and options
providers. Woven code references the `DelegateCommandFactory` and the `DelegateCommand` family, which are pure
WPF types with internal constructors, and WPF's `DependencyProperty` API. Options are `CommandOptions` and
`DependencyPropertyOptions`, set through `ConfigureCommand` and `ConfigureDependencyProperty`.

## 3. The target topology per library

| Library | Run-time package | Aspects package |
|---|---|---|
| Caching | `Metalama.Patterns.Caching` and `.Backend` as today, minus the framework reference, plus `[Cache]`, `[InvalidateCache]`, `[CacheKey]`, `[CachingConfiguration]` as plain attributes, plus `AsyncEnumerableHelper` | `CacheAspect`, `InvalidateCacheAspect`, `CacheKeyAspect`, `ImplementFormattableAspect`, each bound with `[AppliedBy]`; a `CachingConfigurationProvider` bound to `[CachingConfiguration]`; `CachingOptions`, `ConfigureCaching` |
| Contracts | The contract attributes as plain attributes with their arguments (bounds, pattern, direction, inheritability), `[Invariant]`, `[SuspendInvariants]`, `[DoNotCheckInvariants]`, the exceptions, `ContractHelpers`, `EnumDataTypeAttributeHelper`, `NumberComparer`, `InvariantSuspensionCounter`, `SuspendInvariantsCookie` | One aspect per contract bound with `[AppliedBy]`, taking the attribute in its constructor and computing `NumericRange` from the raw bounds; `CheckInvariantsAspect`; `ContractOptions`, `ContractTemplates`, `ConfigureContracts`, `VerifyNotNullableDeclarations` |
| Memoization | `[Memoize]` | `MemoizeAspect` bound to it |
| Observability | `[Observable]`, `[NotObservable]`, `[ObservedExpressions]`, `[Constant]`, `[SuppressObservabilityWarnings]` | `ObservableAspect` bound to `[Observable]`; providers bound to `[Constant]` and `[SuppressObservabilityWarnings]`; the options, strategies and `ConfigureObservability` |
| Immutability | `[Immutable]`, `ImmutabilityKind` | `ImmutableAspect` bound to it, which also provides the options; `ConfigureImmutability`, the classifier, the transitive fabric |
| Wpf | `[Command]`, `[DependencyProperty]`, `DelegateCommandFactory` and the command classes | `CommandAspect` and `DependencyPropertyAspect` bound to them, each also an options provider; the options, naming conventions, `ConfigureCommand`, `ConfigureDependencyProperty` |

The package names per library follow section 1: `Metalama.Patterns.Contracts.Redist`, `.Compiler` and the
unsuffixed `Metalama.Patterns.Contracts`, and the same for Memoization, Observability, Immutability and Wpf;
Caching keeps `Metalama.Patterns.Caching`, `.Backend` and `.Aspects`, and gains `.Compiler`.

A business project that uses contracts and caching, and wants no Metalama dependency in its package, then has:

```xml
<ItemGroup>
  <PackageReference Include="Metalama.Framework" PrivateAssets="all" />
  <PackageReference Include="Metalama.Patterns.Contracts.Redist" />
  <PackageReference Include="Metalama.Patterns.Caching" />
  <PackageReference Include="Metalama.Patterns.Contracts.Compiler" />
  <PackageReference Include="Metalama.Patterns.Caching.Compiler" />
</ItemGroup>
```

and its package depends on `Metalama.Patterns.Contracts.Redist`, `Metalama.Patterns.Contracts.Compiler`,
`Metalama.Patterns.Caching`, `Metalama.Patterns.Caching.Compiler` and, through them, `Metalama.Framework.Redist`.
The `Compiler` packages flow to its consumers with no content, so that a consumer that builds with Metalama
inherits the contracts with no action. A project that writes fabrics against the libraries references
`Metalama.Patterns.Contracts` and `Metalama.Patterns.Caching.Aspects` instead, and gets what it gets today.

## 4. Configuration

The third exploration report answers the question of whether configuration already works in the aspects
topology. It does for two of the three channels.

**Fabrics.** A `TransitiveProjectFabric` in a referenced compile-time project runs against the referencing
compilation, and its `SetOptions` calls pass the containment check because the originator is the compilation
(`Metalama.Framework.Engine/Queries/Query.cs:473-490`, `Fabrics/FabricManager.cs:46-130`). So the user's own
aspects project configures the business project from a transitive fabric, by namespace and type names, with the
`ConfigureContracts` and `ConfigureCaching` extension methods unchanged. Options types stay compile-time, as they
are. Nothing is missing. In the aspects topology the user's aspects project is where per-solution policy lives,
which is the role the `TransitiveProjectFabric` documentation already gives it.

**MSBuild properties.** `IHierarchicalOptions.GetDefaultOptions` reads them through
`context.Project.TryGetProperty`, which sees the properties exported with `CompilerVisibleProperty`
(`Metalama.Framework/Project/IProject.cs:70-75`). This works unchanged, with one caveat recorded as gap 8.

**Configuration attributes.** These do not work, and cannot, because the mechanism requires the attribute class
to implement `IHierarchicalOptionsProvider` and return `IHierarchicalOptions` instances
(`Metalama.Framework.Engine/HierarchicalOptions/CompilationHierarchicalOptionsSource.cs:48-92`). Both interfaces
are in `Metalama.Framework`; the second is compile-time through `IIncrementalObject` and
`ICompileTimeSerializable`. A run-time package that references only `Metalama.Framework.Redist` cannot
implement them. `[CachingConfiguration]`, `[Constant]` and `[SuppressObservabilityWarnings]` therefore need the
same binding as aspects, recorded as gap 2. Attributes that are both an aspect and a provider, such as
`[Command]` and `[Immutable]`, need nothing more than `[AppliedBy]`: the bound aspect class implements
`IHierarchicalOptionsProvider` as today, and `AspectDriver` applies its options before `BuildAspect`
(`Metalama.Framework.Engine/Aspects/AspectDriver.cs:75-97`).

## 5. Gaps

Each gap names the design document it affects. Gaps 4 and 7 are corrections of statements in the aspects projects
document; the others are additions.

1. **Aspects packages.** The aspects projects document defines the build-only reference for `ProjectReference` only.
   Every pattern library is consumed as a package. The package form needs no new MSBuild concept: an aspects
   package ships its assembly outside `lib`, in `metalama/<tfm>/`, and its `buildTransitive` props add a
   `MetalamaAspectsAssembly` item, exactly as extension packages ship `MetalamaExtensionAssembly` items today
   (`Metalama.Framework/docs/extensibility.md`, `Metalama.Extensions.HtmlWriter/build/*.props`). This is the
   `Compiler` package of section 1; the unsuffixed package carries the same assembly in `lib/` for projects
   that compile against it. The `Compiler` package depends on its `Redist` package at an exact version and on
   the `Compiler` packages of the libraries its aspects use, and not on `Metalama.Framework`; the business
   project references `Metalama.Framework` privately itself. The business project's
   dependency on the `Redist` package must be a direct, non-private reference for the reason given in section
   6.2 of the aspects projects document, and the pack-time check there should be generalized from
   `Metalama.Framework.Redist` to any assembly that the output references and that only a private reference
   provides. Affects the aspects projects document, section 6.1 and 6.2.

2. **Attribute-bound options providers.** `[AppliedBy]` must be accepted on a class implementing
   `IHierarchicalOptionsProvider` as well as on an aspect class. `CompilationHierarchicalOptionsSource` then
   instantiates the provider once per bound attribute, through the same three constructors as for aspects, and
   calls `GetOptions` with the attribute's containing declaration as the target, as it does today for provider
   attributes. The manifest entry of the attribute-bound aspects document gains a kind (aspect or provider).
   `[CachingConfiguration]`, `[Constant]` and `[SuppressObservabilityWarnings]` need this. Affects the
   attribute-bound aspects document, section 4, which now specifies it.

3. **Framework types used as attribute arguments move to the run-time package.** `ContractDirection` is a
   `[RunTimeOrCompileTime]` enum of `Metalama.Framework.Aspects` used as a named argument of every contract
   attribute (`ContractBaseAttribute.cs:67`). A run-time contracts package cannot reference it unless it is in
   `Metalama.Framework.Redist`. The same applies to `[Durable]` and `[ImmutableType]` of
   `Metalama.Framework.Utilities`, which are analyzer annotations placed on run-time types
   (`InvariantSuspensionCounter.cs:17-19`, `CachingBaseAttribute.cs:26`). The content list of section 6.3 of the
   aspects projects document gains these, and a rule: a framework type that a run-time package needs in its own public
   surface belongs to the run-time package. Affects the aspects projects document, section 6.3.

4. **A third path for shared types: a run-time package built with Metalama.** The aspects projects document offers two
   paths for contract types: the code model path, which loads nothing, and the typed path, which loads the
   contracts assembly as a foreign compile-time assembly and requires it to be loadable in a .NET Framework
   host. Caching shows a third path that exists today and is better for Metalama's own run-time packages: the
   run-time package is built with Metalama (`Metalama.Framework` as a private reference, `Metalama.Framework.Redist`
   as the only run-time reference), Metalama extracts its `[RunTimeOrCompileTime]` types into the embedded
   compile-time project, and consumers recompile those sources. Only the shared types are extracted, so the
   package may be as thick as it likes at run time, and nothing is loaded from the run-time assembly in the
   compile-time domain. `CachingOptions : ICacheItemConfiguration` depends on it. Verified against the code:
   the early exit at `AspectPipeline.cs:689-691` never fires for a project with a compile-time project, because
   `BoundAspectClasses` includes the three system aspect classes (`AspectPipeline.cs:333-343`), and
   `ExecuteCoreAsync` embeds the resource whenever the compile-time project is not empty
   (`CompileTimeAspectPipeline.cs:306-309`). The only condition that stops such a package today is the marker
   reference to `Metalama.Framework.dll` in the three checks of section 6.4 of the aspects projects document,
   and that marker becomes `Metalama.Framework.Redist.dll`, which the package references. No further engine
   change is needed. The foreign compile-time assembly kind then serves only libraries that are not built with
   Metalama. Affects the aspects projects document, sections 4.1, 6.4 and 6.5; resolved there.

5. **Serializable state of bound aspects.** Contracts aspects are `IConditionallyInheritableAspect`, `[Observable]`
   and `[Immutable]` are `[Inheritable]`, so their instances are serialized into `Metalama.InheritableAspects.bin`
   for derived types in other projects. A bound aspect that stores the attribute instance it received would need
   the attribute type to be `ICompileTimeSerializable`, which is a compile-time interface a run-time package
   cannot implement. Two answers: the aspect copies the attribute's values into its own fields, which the
   serializer handles for primitive and framework types and which is the recommended rule for the first version;
   or `ICompileTimeSerializable`, which is a marker interface, moves to `Metalama.Framework.Redist`, which is a
   larger decision with consequences for the serializer generator. Affects the attribute-bound aspects document,
   section 2.2.

6. **Run-time helpers that live in aspect assemblies move.** `AsyncEnumerableHelper` in
   `Metalama.Patterns.Caching.Aspects` is called by woven code and must move to the run-time package; the
   aspects projects document's post-linking diagnostic pass would report it. The same pass must accept a reference from
   woven code to any ordinary run-time package, which it does, since it checks for `Metalama.Framework.dll` only.
   The rule for library authors: an aspects package must contain nothing that woven code references. Affects
   the aspects projects document, section 6.6, as a documented rule.

7. **Inheritable aspects between business projects.** Section 6.8 of the aspects projects document proposes to omit
   `Metalama.InheritableAspects.bin` with a warning in a project that does not reference `Metalama.Framework.dll`,
   on the assumption that its consumers are not built with Metalama. The assumption is wrong for a solution
   with several business projects: a `[Observable]` base class in one project and a derived class in another,
   both referencing the same aspects package, must inherit the aspect, and `TransitivePipelineContributorSource`
   can deserialize the manifest because the aspects project's compile-time project is in the consumer's closure. The
   manifest must be written as today, and the warning dropped. The requirement that such a consumer builds with
   the same aspects is carried by the `Metalama.ConsumerRequirements.json` resource and checked by the `Redist`
   targets, specified in section 6.8 of the aspects projects document. Affects the aspects projects document,
   section 6.8; resolved there.

8. **MSBuild-driven defaults through an aspects project reference.** `ObservabilityOptions.GetDefaultOptions`
   reads `InpcDiagnosticCommentVerbosity`, which the consuming project must export with
   `CompilerVisibleProperty`. An aspects package ships that export in its `buildTransitive` props; an aspects
   project reference cannot, because MSBuild imports no props through a project reference. The documentation
   should say that a library configured through MSBuild properties exports them in its package, and that a
   project-referenced aspects project uses a fabric or a `Directory.Build.props`. Affects the documentation only.

9. **Migration of the existing libraries.** The attributes change assembly and base type: `[NotNull]` moves to
   `Metalama.Patterns.Contracts.Redist` and stops deriving from `ContractAspect`, `[Cache]` moves from
   `Metalama.Patterns.Caching.Aspects` to `Metalama.Patterns.Caching`. A type forwarder from the old assembly to
   the new one keeps binary compatibility for the attribute types when their namespace is kept, and
   `ContractDirection` is forwarded from `Metalama.Framework.dll` to `Metalama.Framework.Redist.dll`. The
   unsuffixed package keeps the one-reference experience, so a user of `Metalama.Patterns.Contracts` changes nothing in
   the project file. Source compatibility breaks for a user who derives from `ContractBaseAttribute` or
   `CachingBaseAttribute` to write a custom contract, and a user of `Metalama.Patterns.Caching.Aspects` who wants
   the clean topology adds the private reference. At the framework level, a library that followed
   `distributing.md` adopts the single-assembly layout or flows `Metalama.Framework` again, as section 6.2 of the aspects projects document states, because no package replaces today's `Redist`. The first version of
   each split library is a breaking change and belongs to a major version. Affects the pattern libraries.

10. **`Metalama.Extensions.DependencyInjection`.** `CacheAspect` uses it at compile time to introduce the
    `ICachingService` dependency, and the woven code references no type of that extension, so it is a compile-time
    dependency of the caching aspects package only. The extension's own attributes, `[Dependency]` and
    `[IntroduceDependency]`, are aspects that business code applies, so the extension needs the same split when
    business code uses them directly. Out of scope for this study; recorded so that it is not forgotten.

11. **The existing packaging defect of the caching run-time packages.** `Metalama.Patterns.Caching` and
    `.Backend` reference `Metalama.Framework.dll` and call `RunTimeAspectHelper` without declaring
    `Metalama.Framework.Redist`. Independent of the design, this is a defect today and needs an issue. With the
    design, the dependency becomes `Metalama.Framework.Redist` and is declared.

12. **Vocabulary.** "Contracts project" in the aspects projects document collides with `Metalama.Patterns.Contracts`.
    "`Redist` package" names it after the convention and avoids the collision. The project form was first
    called "amender project", after `IProjectAmender`, and was renamed "aspects project" to match the `Aspects`
    suffix. Affects the aspects projects document throughout, and is a choice to make before the documentation
    is written.

## 6. What this study does not cover

- `Metalama.Patterns.Wpf` under the typed path. Its attributes have string and boolean arguments only, so the
  third path of gap 4 extracts them into the compile-time project without touching WPF; the code model path
  works as well. Nothing specific was found.
- Flashtrace and the logging integration of Caching, which are run-time only and unaffected.
- Licensing of the pattern libraries, which is unchanged: the aspects are consumed in the business project.
- Performance of the pipeline when several aspects packages contribute aspect classes. The closure mechanism is
  the same as for several referenced aspect libraries today.
