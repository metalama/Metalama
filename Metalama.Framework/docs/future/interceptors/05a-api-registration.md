# User-facing API: overview and registration

> Part of the [call-site interceptors design](README.md). Previous: [04-mechanism.md](04-mechanism.md) | Next: [05b-api-providers-contexts-results.md](05b-api-providers-contexts-results.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

## 5. User-facing API of the premium package

All types of this section are PROPOSED and live in the namespace `Metalama.Extensions.Interceptors` of the assembly `Metalama.Extensions.Interceptors` (package `Metalama.Extensions.Interceptors.Redist`). The API uses no type of another premium package.

> Superseded in part by the row "Fluent registration API" of section [15.0](15-decisions.md#150-decisions-of-2026-10-02) (2026-10-08, metalama/Metalama#2141): `InterceptMethods` takes one delegate, `Func<IMethodInterceptionBuilder, IMethodInterception>`, which describes the interception as a chain of immutable values. `MethodSelector`, `IMethodInterceptorProvider`, `MethodInterceptorProvider`, `InterceptorResult`, `InterceptorResultKind`, `MethodInterceptionOptions`, `MethodMatching`, `InterceptionScopeOptions`, `IInterceptorBuilder` and `IMethodInterceptorBuilder` are no longer public. Some of them remain internal types of the package. The sections below describe `InterceptMethods` with the fluent API. Where the accessor and await verbs still name these types, the text describes their earlier design, which their milestones revise.

### 5.1 The API at a glance

| Concern | Types and members |
|---|---|
| Registration verbs | `InterceptMethods` (ordinary methods and extension methods), `InterceptAccessors` (property and event accessors, with a mandatory `MethodKind` accessor kind), `InterceptAwaits` |
| Fabric and query surface | `InterceptionQueryExtensions` (extension methods on `IQuery<T>` and `ITaggedQuery<T,TTag>`) |
| Aspect surface | `InterceptionAdviserExtensions` (extension methods on `IAdviser<T>`, plus `ITypeAmender` overloads) |
| Method registration chain | `IMethodInterceptionBuilder` (call-site options and type selection), `ITypeSelection` and `ITypeFilterSelection` (method selection), `IMethodSelection` (filter, matching of overrides and interface implementations, and result), `IMethodResultFactory` and `IMethodSiteResultFactory` (results), `IMethodInterception` and `ITemplateInterception` (complete interceptions and the options of a synthesized method) |
| Target selection | Methods: `Type( Type )` or `Type( INamedType )`, matched by definition, then `Methods( params string[] names )` or `AllMethods()`; or `Types( [Durable] Func<INamedType, bool> )` over declaring type definitions, then `Methods( names )`. The `Where` method filters the selected method definitions. Accessors: a declaring type or a type predicate, plus a mandatory list of member names. Awaits: no target selection; the scope selects the await expressions, and the provider skips the ones that it does not want (section [5.3.4](#534-await-registrations)). A method registration has no filter by kind of use (section [5.3.11](#5311-kinds-of-method-use)). |
| Registration options | Methods: `IncludingNestedTypes` and `ExcludingLambdas` on the root of the chain, and `IncludingInterfaceImplementations` and `ExcludingOverrides` after the method selection. Accessors and awaits, earlier design: `MethodInterceptionOptions`, `AwaitInterceptionOptions`, `InterceptionScopeOptions` |
| Per-site decision | Methods: the delegate given to `IMethodSelection.ForEachSite`, which returns a result created by `IMethodSiteResultFactory`. Awaits: `IAwaitInterceptorProvider`, with a method `GetInterceptor` |
| Contexts | `InterceptionContext` (with `Origin` and `IsInNestedFunction`), `MethodInterceptionContext` (with `Kind`, `ConvertedType`, `IsEventSubscription`, `Receiver`, `Destination`, `AssignmentOperator`, `IsPostfix` and `IsChecked`), `AwaitInterceptionContext` (with `Operand`), `InvocationArgument` (with `Expression`), `AwaitConfiguration` |
| Context enumerations | `MethodUseKind` (`Call`, `DelegateCreation`, `FunctionPointer`), `InvocationArgumentKind`, `InvocationDispatchKind`, `InvocationReceiverKind`, `EnclosingCodeKind`, `NonInterceptableReason`, `AwaitResumption`, `AwaitConfigurationFlags`, `AwaitableKind` (the kind of one awaited expression, not a flags enumeration); the existing `OperatorKind`, with the new member `NullCoalescingAssignment` |
| Result model | Methods: `IMethodInterception`, created by `Skip`, `RedirectToExistingMethod` and `RedirectToSynthesizedMethod`, and `ITemplateInterception` with `WithArgs`, `WithTags`, `WithTemplateProvider`, `WithPlacement`, `WithGranularity` and `Configure`. Awaits, earlier design: `InterceptorResult`, `AwaitRewriteOptions`, `AwaitInterceptionMode`, `AwaitInterceptorTaskKind` |
| Placement model | `InterceptorPlacement` (`CallingType`, `InType`, `BaseMostAccessibleType`, `GeneratedStaticClass`, `LocalFunction`), `InterceptorPlacementKind` |
| Parameter binding | `InterceptorArgument` (the source of the value of one interceptor parameter: `Receiver`, `Argument`, `Value`, `Awaitable`, `CallerInstance`, `CallerInfo`, `Pull`), `CallerInfoKind`, `IInterceptorMethodBinder` (for existing methods), `IInterceptorParameterBinder`, `IInterceptorParameterBinderList`, `InterceptorReceiverMapping` |
| Signature builder | `IInterceptorMethodBuilder`, passed to the delegate of `ITemplateInterception.Configure`, with `Method`, `ReceiverMapping`, `Context`, `SetArgument` and `SetTypeArgument`. The earlier `IInterceptorBuilder`, `IInterceptorParameterBuilder`, `IInterceptorParameterBuilderList` and `InterceptorDefaultMode` are not introduced. |
| Template side | `meta.MethodInterception` and `meta.AwaitInterception` (C# 14 static extension properties of `meta`, declared in `InterceptionMetaExtensions`), which return the meta extensions `MethodInterceptionInfo` and `AwaitInterceptionInfo` |
| Internal seam to the engine | `IInterceptionRegistrationService`, `InterceptionRegistration`, `InterceptorDefinition` (internal, visible to the engine assemblies) |

EXISTING public types reused without change: `TemplateInvocation` (FW27 `Aspects\TemplateInvocation.cs:35`), `MethodTemplateSelector` (FW27 `Advising\MethodTemplateSelector.cs:58`; it receives `[Durable]`, section [10.1](10a-oss-bridge-hook-factory.md#101-overview), because it contains only strings and Boolean values and is stored in durable registrations; it is used by method and accessor interceptors only, and await interceptors take a template name, RC60), `TemplateProvider` (FW27 `Aspects\TemplateProvider.cs:36`), `ScopedDiagnosticSink` (FW27 `Diagnostics\ScopedDiagnosticSink.cs:35`), `IAspectState` (FW27 `Aspects\IAspectState.cs:29`), `SourceReference` (FW27 `Code\SourceReference.cs:25`), `IDurableRef<T>` and `ToDurableRef` (FW27 `Code\IDurableRef.cs`; `Code\RefExtensions.cs:193`), `DurableAttribute` and `ImmutableTypeAttribute` (FW27 `Utilities`), `IExpression` (FW27 `Code\IExpression.cs:46`), `ISourceExpression` (FW27 `Code\ISourceExpression.cs:10`), and `PullAction` (FW27 `Advising\PullAction.cs:40`), whose factories `UseExpression`, `UseExistingParameter`, `UseConstant` and `None` give the pulled values of `InterceptorArgument.Pull`.

PROPOSED open-source public types used by this API (section [10](10a-oss-bridge-hook-factory.md#10-open-source-extension-points)): `IMetaExtension`, `meta.GetExtension<T>()` and `meta.TryGetExtension<T>(out T?)` in `Metalama.Framework` (section [10.6.6](10b-oss-linker-and-templates.md#1066-meta-extensions-per-expansion-extension-data)), `OperatorKind.NullCoalescingAssignment` in `Metalama.Framework` (section [10.1](10a-oss-bridge-hook-factory.md#101-overview)), `AnyAwaitable` and `AnyAwaitable<T>` in `Metalama.Framework.Aspects` (section [10.6.9](10b-oss-linker-and-templates.md#1069-anyawaitable)), the public kind and parameter of `PullAction` (section [10.9](10c-oss-reference-graph-design-time.md#109-small-public-helpers-b2g)), and `SourceExpressionExtensions.GetSourceSyntax` in `Metalama.Framework.Sdk` (section [10.9](10c-oss-reference-graph-design-time.md#109-small-public-helpers-b2g)). EXISTING public types reused for accessors: `MethodKind` (FW27 `Code\MethodKind.cs:29-49`) and `OperatorKind` (FW27 `Code\OperatorKind.cs:16`).

### 5.2 Seam between the public API and the engine

EXISTING precedent: the Validation API resolves an internal `IProjectService` from the project service provider (P27 `Metalama.Extensions.Validation\ReferenceValidationQueryExtensions.cs:36-37`), and the engine registers the implementation in `Initialize` (P27 `Metalama.Extensions.Validation.Engine\ValidationPipelineExtension.cs:28-30`).

PROPOSED:

```csharp
namespace Metalama.Extensions.Interceptors;

/// <summary>
/// Registers interceptors on behalf of the public extension methods. The engine assembly implements this service and
/// registers it in <c>PipelineExtension.Initialize</c>.
/// </summary>
internal interface IInterceptionRegistrationService : IProjectService
{
    /// <summary>Registers an interceptor whose scopes are the declarations selected by a query.</summary>
    void Register<TScope>( IQuery<TScope> query, InterceptionRegistration registration )
        where TScope : class, IDeclaration;

    /// <summary>Registers an interceptor whose scopes are the declarations selected by a tagged query.</summary>
    void Register<TScope, TTag>( ITaggedQuery<TScope, TTag> query, InterceptionRegistration registration )
        where TScope : class, IDeclaration;

    /// <summary>Registers an interceptor whose scope is the target of an adviser.</summary>
    void Register<TScope>( IAdviser<TScope> adviser, InterceptionRegistration registration )
        where TScope : class, IDeclaration;
}

internal enum InterceptionKind
{
    Method,
    Accessor,
    Await
}

/// <summary>
/// Selects type definitions: one definition stored as a durable reference, or a durable predicate over definitions.
/// </summary>
[Durable]
internal sealed class TypeSelector
{
    /// <summary>Gets the selected definition, or <c>null</c> when the selector is a predicate.</summary>
    public IDurableRef<INamedType>? Definition { get; }

    /// <summary>Gets the predicate over type definitions, or <c>null</c> when the selector is a definition.</summary>
    public Func<INamedType, bool>? Predicate { get; }

    /// <summary>Resolves <paramref name="type"/> with <c>TypeFactory.GetNamedType</c> and stores its definition.</summary>
    public static TypeSelector FromType( Type type );

    public static TypeSelector FromNamedType( INamedType type );

    public static TypeSelector FromPredicate( [Durable] Func<INamedType, bool> predicate );
}

/// <summary>
/// Describes one registration: what is intercepted, how the interceptor is obtained, and the options.
/// </summary>
/// <remarks>
/// The public extension methods validate the names and resolve the declaring type before they create the registration.
/// </remarks>
[Durable]
internal sealed class InterceptionRegistration
{
    public InterceptionKind Kind { get; }

    /// <summary>
    /// Gets the declaring types of a member registration. The value is <c>null</c> for an await registration, which has no
    /// target selection.
    /// </summary>
    public TypeSelector? Types { get; }

    /// <summary>Gets the member names of a member registration: at least one name. The array is empty for an await registration.</summary>
    public ImmutableArray<string> Names { get; }

    /// <summary>Gets the accessor kind of an accessor registration, or <c>null</c>.</summary>
    public MethodKind? AccessorKind { get; }

    public MethodInterceptionOptions? MethodInterceptionOptions { get; }

    public AwaitInterceptionOptions? AwaitInterceptionOptions { get; }

    public InterceptorDefinition Interceptor { get; }

    public static InterceptionRegistration ForMethods( TypeSelector declaringTypes, ImmutableArray<string> names, InterceptorDefinition interceptor, MethodInterceptionOptions options );

    public static InterceptionRegistration ForAccessors( TypeSelector declaringTypes, ImmutableArray<string> names, MethodKind accessorKind, InterceptorDefinition interceptor, MethodInterceptionOptions options );

    public static InterceptionRegistration ForAwaits( InterceptorDefinition interceptor, AwaitInterceptionOptions options );
}

/// <summary>
/// Describes how the interceptor of a registration is obtained. The engine consumes the concrete subclasses.
/// </summary>
[Durable]
internal abstract class InterceptorDefinition
{
    /// <summary>Wraps an <see cref="IMethodInterceptorProvider"/> or <see cref="IAwaitInterceptorProvider"/> instance.</summary>
    public static InterceptorDefinition FromProvider( object provider );

    /// <summary>Wraps a delegate, which can be a lambda. The durability analyzer checks it at the <c>[Durable]</c> parameter of the public method.</summary>
    public static InterceptorDefinition FromDelegate( [Durable] Delegate getInterceptor );

    /// <summary>Wraps a factory that the engine invokes once for each selected scope. The second argument is the query tag.</summary>
    public static InterceptorDefinition FromFactory( [Durable] Func<IDeclaration, object?, object> factory );

    /// <summary>
    /// Wraps a template result that is identical for every call site. The shorthand carries no template arguments and no
    /// tags, because the durability analyzer classifies anonymous types as not durable (LAMA0872).
    /// </summary>
    public static InterceptorDefinition FromTemplate(
        in MethodTemplateSelector template,
        InterceptorPlacement placement,
        [Durable] Action<IInterceptorBuilder>? configure );

    /// <summary>
    /// Wraps the template result of an await shorthand. An await template is referenced by its name only (RC60).
    /// </summary>
    public static InterceptorDefinition FromAwaitTemplate(
        string templateName,
        InterceptorPlacement placement,
        [Durable] Action<IInterceptorBuilder>? configure,
        AwaitRewriteOptions? rewriteOptions );

    /// <summary>
    /// Wraps an existing method that is identical for every call site, stored as a durable reference, and the optional
    /// function that binds its parameters (section 5.6.8).
    /// </summary>
    public static InterceptorDefinition FromExistingMethod( IMethod method, [Durable] Action<IInterceptorMethodBinder>? bind );
}
```

The code above is the internal model of the earlier design. Since the row "Fluent registration API" of section [15.0](15-decisions.md#150-decisions-of-2026-10-02), the internal registration of a method is created from the chain that the registration delegate returns. The internal helper `InterceptionApiHelper.CreateRegistration` executes the delegate under an `InterceptionChainTracker`, checks that no created value was discarded, and converts the returned chain to an `InterceptionRegistration`. That registration holds an internal `MethodSelector`, an `InterceptorDefinition` that wraps an internal `IMethodInterceptorProvider`, and the internal `MethodInterceptionOptions`. The engine consumes these internal types unchanged.

The public extension methods resolve the service with `GetService`, not `GetRequiredService`. When the service is missing, they throw `InvalidOperationException` with a message that names the package `Metalama.Extensions.Interceptors`. This happens when a library references only the Redist package and its transitive fabric runs in a consumer that does not load the engine (section [9.9.2](09-premium-engine.md#992-licensing-implications)).

### 5.3 Registration

#### 5.3.1 Three verbs

The API has three verbs: `InterceptMethods`, `InterceptAccessors` and `InterceptAwaits`. `InterceptMethods` intercepts ordinary methods, classic extension methods and C# 14 extension methods. `InterceptAccessors` intercepts the accessors of properties and events (section [5.3.13](#5313-accessors)). `InterceptMethods` never matches an accessor. The two member verbs share the context. In the earlier design, they also share the provider interface, the options and the result model; `InterceptMethods` now uses the fluent registration API, and `InterceptAccessors` keeps the earlier design until M2 revises it. The API has no single `Intercept` verb, for these reasons:

- Members and awaits need different target selections, options and contexts. A member registration has a declaring type, names and a matching policy, and its context describes the kind of use. An await registration has no target selection, and its context describes the awaitable, the resumption and the `ConfigureAwait` call (section [5.3.4](#534-await-registrations)).
- A single verb with delegate overloads is ambiguous in C#. In the earlier design, where both providers returned the same result type, the lambda `ctx => ...` converted to the delegate types of both overloads, which raised CS0121.
- A single verb with interface overloads is ambiguous for a class that implements both interfaces.
- The verb names the target, not the syntax that uses it. `InterceptMethods` intercepts every use of a method: a call, or a method group converted to a delegate or to a function pointer. The kind of use is a property of the context, `MethodInterceptionContext.Kind`, and not an option of the registration (section [5.3.11](#5311-kinds-of-method-use), RC44). An earlier version of this design named the verb `InterceptInvocations`. That name excluded delegate creation at the verb level, although delegate creation produces the same interceptor method and the same template (decision PO44).
- An accessor registration needs an accessor kind, which a method registration does not have. A separate verb makes the kind a required parameter instead of an option that most method registrations would ignore.
- The verbs are plural, because one registration covers a set of targets.

#### 5.3.2 Scope semantics

The scope is the calling side (R2). It is a property of the registration. Each registration has one or more scope declarations:

- On the query surface, each declaration that the query selects.
- On the adviser surface, `adviser.Target`.

A site is in the scope when its origin is contained in the scope declaration. The origin is the referencing declaration of the reference index. It is the enclosing member for code in lambdas and local functions, the accessor for accessor bodies and for the expression body of a property or an indexer, the property for property initializers, the field for field initializers, the type for primary-constructor base arguments, and the entry point for top-level statements (ENG26 `ReferenceGraph\ReferenceIndexWalker.cs:154-162, 320-356, 462-474, 551-567`). This rule applies to awaits as well (interpretation I8). The origin is a property of the site, and the provider reads it as `InterceptionContext.Origin` (section [5.5.1](05b-api-providers-contexts-results.md#551-interceptioncontext)).

| Scope kind | Code in scope |
|---|---|
| `ICompilation` | All source code of the current project (interpretation I4). |
| `INamespace` | All types of the namespace and of its descendant namespaces, as `IDeclaration.IsContainedIn` defines it (FW26 `Code\DeclarationExtensions.cs:33-95`). |
| `INamedType` | All members of the type, including field and property initializers, constructor initializers and primary-constructor base arguments. Nested types are excluded unless the chain calls the `IncludingNestedTypes` method. |
| `IMember` | The body of the member, its accessors, its initializer and its constructor initializer. |

Code in lambdas and local functions is in scope unless the chain calls the `ExcludingLambdas` method.

The following code is never in scope: compile-time code; code introduced by aspects (R6, interpretation I5); code produced by source generators, which runs after Metalama (section [3.8](03-background.md#38-roslyn-fork-and-compiler-order)); and, by default, files that Roslyn classifies as generated code (PO12). Code of a member that aspects override is in scope, because it is source code. Superseded by the rewritten decision PO12 (2026-10-05): files classified as generated code are in scope like any other source file, and `IncludeGeneratedFiles` does not exist.

Nested types are excluded by default for a concrete reason (RC18). An aspect applied to every type of a namespace through `amender.SelectTypes()`, whose `includeNestedTypes` parameter defaults to `true` (FW27 `Fabrics\IQuery{T}.cs:83`), creates one aspect instance on the outer type and one on the nested type. If both scopes contained the nested code, each call site in the nested type would receive two interceptors from two different sources, which is an error under R7.

#### 5.3.3 Target selection for members

The target is part of the chain of the registration, not a property of the per-site delegate. One per-site method can therefore serve several targets.

A method registration selects its targets with a declaring type and member names (decision PO52, RC46). Section [5.3.12](#5312-target-selection-options-considered) compares this choice with the other options that were considered. Since the row "Fluent registration API" of section [15.0](15-decisions.md#150-decisions-of-2026-10-02), the selection is a stage of the chain that the registration delegate returns. `InterceptAccessors` keeps the positional shapes of its earlier design, with the accessor kind after the names (section [5.3.13](#5313-accessors)).

```csharp
// One declaring type, given as a System.Type, and a list of names.
b => b.Type( typeof(File) ).Methods( nameof(File.ReadAllText), nameof(File.WriteAllText) ) /* result */

// One declaring type, given as a code-model type.
b => b.Type( namedType ).Methods( names ) /* result */

// A durable predicate over declaring type definitions. The names are mandatory.
b => b.Types( t => t.ContainingNamespace.FullName == "Contoso.Legacy" ).Methods( names ) /* result */

// Every ordinary method that one type declares.
b => b.Type( typeof(Order) ).AllMethods() /* result */
```

The chain then chooses the result: an existing method, a synthesized method, or a decision per site (section [5.4](05b-api-providers-contexts-results.md#54-interceptor-provider-interfaces)).

```csharp
amender.InterceptMethods( b => b.Type( typeof(Guid) ).Methods( nameof(Guid.NewGuid) ).ForEachSite( InterceptSystemCall ) );

amender.InterceptMethods( b => b.Type( typeof(File) ).Methods( nameof(File.ReadAllText), nameof(File.WriteAllText) ).ForEachSite( InterceptFileAccess ) );

// Every type of a namespace. A project fabric cannot enumerate the types of the compilation, so a type predicate is the
// way to select several declaring types.
amender.InterceptMethods( b => b
    .Types( t => t.ContainingNamespace.FullName.StartsWith( "Contoso.Legacy", StringComparison.Ordinal ) )
    .Methods( "Save", "Load" )
    .ForEachSite( InterceptLegacyStorage ) );
```

Rules for the declaring type:

- A `Type` is resolved with `TypeFactory.GetNamedType` (FW27 `Code\TypeFactory.cs:62`) when the registration is made, and the registration stores the definition as a durable reference. An `INamedType` is stored in the same way. `typeof(List<>)` matches every construction of `List<T>`. A constructed type such as `typeof(List<int>)` is reduced to its definition, so it also matches every construction. A delegate given to the `ForEachSite` method that cares about one construction tests `site.Context.InterceptedMethod.DeclaringType` and returns `site.Skip()`.
- The declaring type of a site is the type that declares the member to which the C# compiler binds the site, taken as its definition. It is not the static type of the receiver. For example, `fileStream.CopyTo( other )` binds to `Stream.CopyTo`, so a registration on `FileStream` does not match it, and a registration on `Stream` does. Matching is static: it does not consider the run-time type of the receiver.
- The `Types` method takes a `[Durable] Func<INamedType, bool>`. The engine evaluates it after a name matched and the site was bound, once per distinct declaring type definition, and memoizes the result per compilation. The predicate receives a type definition of the scanned compilation. It must be deterministic and thread-safe. The `[Durable]` parameter lets the durability analyzer reject a lambda that captures a declaration, a symbol or another compilation-bound object (LAMA0878). An exception thrown by the predicate is reported once, and the type then counts as not matching (section [9.5.5](09-premium-engine.md#955-target-matching)).
- The predicate is the only way to select several declaring types, for example the types of a namespace, as above (`INamedType.ContainingNamespace`, FW27 `Code\INamedType.cs:79`). Project and namespace fabrics run once per pipeline configuration (section [3.1](03-background.md#31-pipeline-stages-and-extension-hooks)) and cannot enumerate the types of the compilation. There is deliberately no dedicated namespace method and no method that takes several types.

Rules for the names:

- At least one name is required. A call of the `Methods` method without a name throws `ArgumentException`, and so does a name that is not a valid C# identifier. There is no registration without names. The `AllMethods` method lists the names of the ordinary methods that the type declares, static and instance, when the chain is created; it does not select operators, conversions, explicit interface implementations, accessors, constructors, finalizers or inherited methods. A predicate over types cannot be combined with all methods, because the shared index needs method names.
- The names are always the pre-binding filter of the shared index (section [9.5.3](09-premium-engine.md#953-registration-index-and-index-requirements)): the index binds only the member bodies that contain one of the names.
- All overloads of a selected name are selected. The `Where` method keeps the selected method definitions that a `[Durable] Func<IMethod, bool>` predicate accepts, for instance one overload. The predicate is invoked once for each method definition that has a selected name, and several calls combine their predicates. A delegate given to the `ForEachSite` method can also test the constructed method through the context, for example `site.Context.InterceptedMethod.Parameters`, and return `site.Skip()`. Sample 5 shows the `Where` method.
- A generic method is matched by its bare name, without type arguments, for example `Select` for `Select<int>( ... )`. A classic extension method has the same name in its reduced form and in its static form, because the index normalizes the reduced form (section [10.7.3](10c-oss-reference-graph-design-time.md#1073-reducedfrom-normalization)). An override and an implicit interface implementation have the name of the method that they override or implement, so the names are compatible with the default matching and with the `ExcludingOverrides` and `IncludingInterfaceImplementations` methods. An explicit interface implementation can only be called through the interface method, whose name the registration states.

Other matching rules:

- Generic methods and members of generic types are matched through their definitions. `MethodInterceptionContext.InterceptedMethod` gives the constructed method at a site.
- By default, the engine also tests the members that the bound member overrides, directly or indirectly. After the `ExcludingOverrides` method, it does not. After the `IncludingInterfaceImplementations` method, it also tests the interface members that the bound member implements, implicitly or explicitly. For each member walked, in this order (the bound member, then the overridden members from the nearest, then the implemented interface members), the engine tests the declaring type, or evaluates the type predicate on it. The site matches when the name matches and one member passes. The first member that passes is `MethodInterceptionContext.MatchedMethod`.
- `InterceptMethods` matches only ordinary methods, classic extension methods and C# 14 extension methods. It never matches an accessor, an operator, a constructor, a finalizer or a local function. A name that the declaring type uses only for a property or an event is reported with the warning LAMA1008, with the advice to use `InterceptAccessors` (section [9.4.7](09-premium-engine.md#947-registration-time-checks)).
- Several registrations of the same source that overlap on a site count once for that site, under the one-source rule of section [9.5.8](09-premium-engine.md#958-conflict-detection-r7-b7). Superseded by the decision "Conflicts per registration and event unsubscriptions" of section 15.0 (2026-10-06): every registration is evaluated, and two results that are not skips are LAMA1010. This happens, for example, when an aspect registers a base type and a derived type with the default matching of overrides, or registers a type predicate and a declaring type that it also accepts. The source is the registering aspect instance or fabric instance.

```csharp
namespace Metalama.Extensions.Interceptors;

/// <summary>
/// The stage of a method interception at which the intercepted methods are selected. It filters the methods, decides whether the calls bound
/// to their overrides and to their interface implementations match, and then chooses how the calls are intercepted.
/// </summary>
/// <remarks>
/// By default, a call matches a selected method when it is bound to that method or to an override of it.
/// </remarks>
[CompileTime]
[InternalImplement]
[PublicAPI]
public interface IMethodSelection : IMethodResultFactory
{
    /// <summary>Keeps only the selected methods that a predicate accepts, for instance one overload of a method name.</summary>
    [Pure]
    IMethodSelection Where( [Durable] Func<IMethod, bool> predicate );

    /// <summary>Also matches the calls bound to a method that implements a selected interface method, implicitly or explicitly.</summary>
    [Pure]
    IMethodSelection IncludingInterfaceImplementations();

    /// <summary>Does not match the calls bound to an override of a selected method. Only the calls bound to the selected method itself match.</summary>
    [Pure]
    IMethodSelection ExcludingOverrides();

    /// <summary>Decides the interception of each call site with a delegate, for instance to skip some call sites or to choose a template per site.</summary>
    [Pure]
    IMethodInterception ForEachSite( [Durable] Func<IMethodSiteResultFactory, IMethodInterception> intercept );
}
```

The methods `ExcludingOverrides` and `IncludingInterfaceImplementations` replace the flags enumeration `MethodMatching` of the earlier design, which was named `InterceptedMethodMatching` before that. `MethodMatching.Overrides` is the default chain. `Exact` is `ExcludingOverrides()`. `OverridesAndInterfaceImplementations` is `IncludingInterfaceImplementations()`. `InterfaceImplementations` alone is `ExcludingOverrides().IncludingInterfaceImplementations()`.

A matching policy is necessary. The compiler binds `derived.M()` to the override declared by the static type (RC `CodeGen\EmitExpression.cs:1999-2008`), and it binds `stream.Dispose()` to the class method rather than to `IDisposable.Dispose`. By default, overrides match (RC17, decision PO6): a registration on `Stream.Write` then intercepts `fileStream.Write(...)`, which is the same logical method with virtual dispatch. Interface implementations are opt-in, because a call bound to a class method is not obviously a call to the interface. Name filtering in the index still works with every matching option, because an override or an implicit implementation is called with the same simple name, and an explicit implementation can only be called through the interface method, whose name the registration states.

Targets are matched by definition. Durable references identify definitions, and the reference index normalizes references to `OriginalDefinition` (ENG26 `ReferenceGraph\ReferenceIndexBuilder.cs:20-21`). A delegate given to the `ForEachSite` method that cares about a specific construction filters it and returns `site.Skip()`.

#### 5.3.4 Await registrations

The await verb is not part of the fluent registration API of the row "Fluent registration API" of section [15.0](15-decisions.md#150-decisions-of-2026-10-02). This section keeps its earlier design, with `IAwaitInterceptorProvider` and the earlier result model, until milestone M5 revises it.

An await registration has no target selection (decision PO58, RC58). Its scope is its only selection: every await expression of the scope that section [7.1](07-await-interception.md#71-which-await-sites-are-targets) accepts reaches the interceptor provider, whatever its awaitable type, including the awaits of custom awaitables. The verb has one shape on every surface (sections [5.3.6](#536-fabric-and-query-surface) to [5.3.8](#538-itypeamender-overloads)):

```csharp
InterceptAwaits( <provider form>, AwaitInterceptionOptions? options = null )
```

The provider form is an interceptor provider object, a delegate, a template shorthand, or a factory (section [5.3.6](#536-fabric-and-query-surface)). The template shorthand takes a template name, never a `MethodTemplateSelector` (section [5.7.4](05c-api-templates.md#574-template-signature-rules), RC60).

```csharp
// Every await of the scope is presented to a provider object.
amender.InterceptAwaits( new AwaitTracer() );

// The same, with a delegate of the aspect.
builder.InterceptAwaits( this.MeasureAwait );

// A template shorthand. It cannot skip, so it suits a scope whose awaits all have a known resumption.
builder.InterceptAwaits( nameof(this.TraceAwait), InterceptorPlacement.CallingType() );
```

The provider selects the awaits. It reads the context (section [5.5.3](05b-api-providers-contexts-results.md#553-awaitinterceptioncontext)) and returns `InterceptorResult.Skip` for the awaits that it does not intercept. The recommended first statement of an await provider is the test of the resumption:

```csharp
private InterceptorResult MeasureAwait( AwaitInterceptionContext context )
{
    if ( context.Resumption == AwaitResumption.Unknown )
    {
        return InterceptorResult.Skip;
    }

    // Keep the awaits of Task and Task<TResult>, configured or not.
    var awaitable = context.Configuration?.UnconfiguredType ?? context.AwaitableType;

    if ( !awaitable.Is( typeof(Task) ) )
    {
        return InterceptorResult.Skip;
    }

    return InterceptorResult.Template( nameof(this.Measure), InterceptorPlacement.CallingType() );
}
```

The resumption is `Unknown` for the awaits of custom awaitables, for a `ConfigureAwait` call whose argument is not a constant, and for a stored configured awaitable (section [7.3](07-await-interception.md#73-awaitable-kinds-and-resumption-classes)). The engine cannot preserve the resumption context of such an await automatically. When the provider returns a result for it and sets no resumption policy, the engine keeps the behavior of section [7.5](07-await-interception.md#75-the-adaptive-rewrite-challenge-to-b8-adopted): it leaves the await unchanged and reports the warning LAMA1020. The first statement above avoids these warnings. It replaces the default exclusion of custom awaitables that earlier versions implemented with the flags enumeration `AwaitableKinds`.

The context gives the provider every fact that a type filter used:

- `AwaitableType` is the type of the operand, with nullable annotations, and `AwaitableKind` is its classification (section [7.3](07-await-interception.md#73-awaitable-kinds-and-resumption-classes)).
- `Configuration` describes a direct `ConfigureAwait` call, and `Configuration.UnconfiguredType` is the type on which it is called. It replaces the look-through rule of earlier versions, which existed only because a registration filtered by type.
- `AwaitedMethod` is the method whose result is awaited, after the removal of a `ConfigureAwait` call, and `Operand` is the awaited expression, for inspection only.
- `ResultType` and `IsResultUsed` describe the value of the await expression.

The awaited type is generally not relevant to the decision, so the filtering is the job of the provider. A registration on a type matched by definition, so `typeof(Task)` did not match `Task<int>`, and it needed the look-through rule to match configured awaits. A test in the provider states the intent directly.

The cost of binding does not change. An await reference has no identifier, so the name filter of the index never applied to awaits: the `await` keyword is the syntactic filter, and the index records every `AwaitExpressionSyntax` of the walked bodies (section [10.7.4](10c-oss-reference-graph-design-time.md#1074-referencekindsawait)). An await registration binds every body of its scope that contains an await expression, as before (section [9.6](09-premium-engine.md#96-cost-model-and-shared-binding)). The only change is that the site analysis of section [7.2](07-await-interception.md#72-site-analysis) and one provider call now run for each await of the scope, including the awaits that a type filter discarded before the analysis. Both run on bodies that the index has already bound.

```csharp
/// <summary>
/// Kinds of awaited expressions. The classification is defined in the documentation of await interception.
/// </summary>
[CompileTime]
public enum AwaitableKind
{
    Task,
    ValueTask,
    ConfiguredTask,
    ConfiguredValueTask,
    Yield,
    Custom
}

/// <summary>
/// Options of a registration made with <c>InterceptAwaits</c>.
/// </summary>
/// <remarks>
/// <para>
/// Only <c>await</c> expressions are intercepted. The <c>await foreach</c> and <c>await using</c> statements are not
/// intercepted. They contain no await expression: the awaits of <c>MoveNextAsync</c> and <c>DisposeAsync</c> exist
/// only after the compiler lowers the statement. Await expressions whose operand is <c>dynamic</c> are not intercepted
/// either.
/// </para>
/// <para>
/// The registration has no filter by awaitable type or by awaitable kind. Every await expression of the scope is
/// presented to the interceptor provider, which returns <see cref="InterceptorResult.Skip"/> for the await expressions
/// that it does not intercept. When <see cref="AwaitInterceptionContext.Resumption"/> is
/// <see cref="AwaitResumption.Unknown"/> and the interceptor returns a task of another type than the awaited expression,
/// the await expression is rewritten only when the result sets <see cref="AwaitRewriteOptions.Resumption"/>. Otherwise
/// the engine leaves it unchanged and reports the warning LAMA1020.
/// </para>
/// <para>
/// This type describes which code of the scope is searched. <see cref="AwaitRewriteOptions"/> describes the interceptor
/// method that a result generates and the rewrite of the await expression (section 5.6.2).
/// </para>
/// </remarks>
[CompileTime]
[PublicAPI]
[Durable]
[ImmutableType]
public sealed record AwaitInterceptionOptions
{
    /// <summary>Gets the options that determine which code of each scope declaration is in scope.</summary>
    public InterceptionScopeOptions Scope { get; init; } = InterceptionScopeOptions.Default;
}
```

`AwaitInterceptionOptions` keeps only the scope options. It stays a record and is not replaced by `InterceptionScopeOptions`, for two reasons. A later option can be added to a record without a new overload of every surface, which a flags enumeration cannot do for an option that is not a flag. The await verb then also has the same parameter shape as the member verbs, whose options record contains `Scope` too.

The kind of one awaited expression is the non-flags enumeration `AwaitableKind`, as the kind of use of a method is the non-flags `MethodUseKind` (RC44). Earlier versions used the flags enumeration `AwaitableKinds` both as the registration filter and, with exactly one flag set, as the kind of one expression (review finding API-09). Without the filter, a flags enumeration has no purpose.

The following members of earlier versions are removed (RC58): the overloads that take a `Type`, an `INamedType` or a `[Durable] Func<INamedType, bool>` before the provider form, the distinction between these overloads and the overload for every await, `AwaitableKinds` with its members `Default` and `All`, `AwaitInterceptionOptions.Kinds`, and `AwaitInterceptionOptions.LookThroughConfigureAwait`.

#### 5.3.5 Scope options

> Superseded for `InterceptMethods` by the row "Fluent registration API" of section [15.0](15-decisions.md#150-decisions-of-2026-10-02) (2026-10-08): the call-site options are the methods `IncludingNestedTypes` and `ExcludingLambdas`, which come first on the root of the chain, for example `b => b.IncludingNestedTypes().ExcludingLambdas().Type( typeof(File) )`. `InterceptionScopeOptions` is internal. `IncludeGeneratedFiles` does not exist (decision PO12, rewritten on 2026-10-05). The enumeration below remains the earlier design of the accessor and await verbs.

```csharp
/// <summary>Options that determine which code of a scope declaration is intercepted.</summary>
[CompileTime]
[Flags]
public enum InterceptionScopeOptions
{
    /// <summary>Nested types of a type scope are excluded. Lambdas and local functions are included.</summary>
    Default = 0,

    /// <summary>
    /// When the scope is a type, the code of its nested types, at any depth, is also in scope.
    /// This option has no effect for other kinds of scope.
    /// </summary>
    IncludeNestedTypes = 1,

    /// <summary>Code inside lambdas, anonymous methods and local functions is not in scope.</summary>
    ExcludeLambdasAndLocalFunctions = 2,

    /// <summary>Code in files that Roslyn classifies as generated code is also in scope.</summary>
    IncludeGeneratedFiles = 4
}
```

A flags enumeration follows the precedent of `ReferenceValidationOptions` (P27 `Metalama.Extensions.Validation\ReferenceValidationQueryExtensions.cs:34`). `IncludeGeneratedFiles` exists because of decision PO12. The value is the `Scope` property of `MethodInterceptionOptions` and of `AwaitInterceptionOptions`. An earlier version of this design passed it as a separate parameter of each registration method.

#### 5.3.6 Fabric and query surface

> Superseded in part by the row "Fluent registration API" of section [15.0](15-decisions.md#150-decisions-of-2026-10-02) (2026-10-08, metalama/Metalama#2141): `InterceptMethods` has one overload on `IQuery<T>` and one on `ITaggedQuery<T, TTag>`, each of which takes a `Func<IMethodInterceptionBuilder, IMethodInterception>`. The overloads that took a `MethodSelector` and an `IMethodInterceptorProvider`, and the factory and tagged-factory overloads, are removed. The code below gives the new method overloads. The accessor and await overloads keep their earlier design.

Fabrics call these methods on their amender, because `IAmender<T>` derives from `IQuery<T>` (FW27 `Fabrics\IAmender.cs:63`). Aspects can call them on `IAspectBuilder<T>.Outbound` (FW27 `Aspects\IAspectBuilder.cs:240`).

```csharp
namespace Metalama.Extensions.Interceptors;

/// <summary>
/// Provides extension methods that register interceptors through a query. Each declaration selected by the query is a scope: the uses of
/// the selected methods located in its code are intercepted.
/// </summary>
/// <remarks>
/// <para>
/// Registration only records the interception. Sites are found later, in the source compilation, after all aspects and fabrics have run.
/// Sites in code introduced by aspects are never intercepted.
/// </para>
/// <para>
/// At most one interceptor can apply to a site. When several registrations match the same site, each one is evaluated, and it is an error
/// when more than one of them returns a result other than <see cref="IMethodSiteResultFactory.Skip"/>.
/// </para>
/// <para>
/// The selected methods are the same for every scope declaration. A result that depends on the scope declaration is chosen per site with
/// <see cref="IMethodSelection.ForEachSite"/>, which reads <see cref="InterceptionContext.ScopeDeclaration"/> and, for a tagged query,
/// <see cref="InterceptionContext.ScopeTag"/>.
/// </para>
/// </remarks>
/// <seealso href="@intercepting-call-sites"/>
[CompileTime]
[PublicAPI]
public static class InterceptionQueryExtensions
{
    /// <summary>
    /// Registers the interception of the uses of methods in the declarations selected by a query, described by a delegate.
    /// </summary>
    /// <param name="query">A query that selects the scope declarations.</param>
    /// <param name="build">A delegate that receives an <see cref="IMethodInterceptionBuilder"/>, selects the intercepted methods, chooses how the
    /// calls are intercepted, and returns the resulting <see cref="IMethodInterception"/>. It is invoked once, before this method returns.</param>
    /// <exception cref="ArgumentNullException"><paramref name="build"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The delegate returned <c>null</c>, or a value that does not describe a complete interception.</exception>
    /// <exception cref="InvalidOperationException">The delegate discarded a value that it created, or the project that executes the registration
    /// does not reference the <c>Metalama.Extensions.Interceptors</c> package.</exception>
    public static void InterceptMethods<TScope>( this IQuery<TScope> query, Func<IMethodInterceptionBuilder, IMethodInterception> build )
        where TScope : class, IDeclaration
        => InterceptionApiHelper.GetService( query.Project ).Register( query, InterceptionApiHelper.CreateRegistration( build ) );

    /// <summary>
    /// Registers the interception of the uses of methods in the declarations selected by a tagged query, described by a delegate. The tag of
    /// each declaration is available to <see cref="IMethodSelection.ForEachSite"/> through <see cref="InterceptionContext.ScopeTag"/>.
    /// </summary>
    public static void InterceptMethods<TScope, TTag>( this ITaggedQuery<TScope, TTag> query, Func<IMethodInterceptionBuilder, IMethodInterception> build )
        where TScope : class, IDeclaration
        => InterceptionApiHelper.GetService( query.Project ).Register( query, InterceptionApiHelper.CreateRegistration( build ) );

    // Accessors (section 5.3.13), earlier design. Every overload of the earlier methods group has a twin named
    // InterceptAccessors, with four target selections, and with a parameter MethodKind accessorKind after the names.
    // For example:

    /// <summary>
    /// Registers an interceptor provider for the uses of one accessor of the properties or events that have one of the given
    /// names and that are declared by a given type.
    /// </summary>
    /// <param name="names">The names of the properties or events. At least one name is required.</param>
    /// <param name="accessorKind">The intercepted accessor: <see cref="MethodKind.PropertyGet"/>,
    /// <see cref="MethodKind.PropertySet"/> (which includes <c>init</c> accessors), <see cref="MethodKind.EventAdd"/> or
    /// <see cref="MethodKind.EventRemove"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="accessorKind"/> is another value, including
    /// <see cref="MethodKind.EventRaise"/>.</exception>
    public static void InterceptAccessors<TScope>(
        this IQuery<TScope> query,
        Type declaringType,
        IReadOnlyList<string> names,
        MethodKind accessorKind,
        IMethodInterceptorProvider provider,
        MethodInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    // Awaits. An await registration has no target selection: every await expression of the scope is presented to the
    // provider (section 5.3.4). This part keeps the earlier design, until milestone M5 revises it.

    /// <summary>Registers an interceptor provider for the await expressions of the scope.</summary>
    /// <param name="provider">The interceptor provider, which is invoked once for each await expression of the scope and
    /// returns <see cref="InterceptorResult.Skip"/> for the await expressions that it does not intercept.</param>
    /// <param name="options">The scope options. When the value is <c>null</c>, the default values of
    /// <see cref="AwaitInterceptionOptions"/> apply.</param>
    public static void InterceptAwaits<TScope>(
        this IQuery<TScope> query,
        IAwaitInterceptorProvider provider,
        AwaitInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    /// <summary>Registers an interceptor provider, given as a delegate, for the await expressions of the scope.</summary>
    public static void InterceptAwaits<TScope>(
        this IQuery<TScope> query,
        [Durable] Func<AwaitInterceptionContext, InterceptorResult> getInterceptor,
        AwaitInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    /// <summary>Intercepts the await expressions of the scope with a method generated from a template.</summary>
    /// <param name="template">The name of the template. An await template is referenced by its name only. The template
    /// method decides whether the generated method awaits the original awaitable itself: an <c>async</c> template awaits
    /// the value of <c>meta.Proceed()</c>, and a non-async template receives the awaited value from <c>meta.Proceed()</c>.</param>
    /// <param name="configure">A function that adjusts the generated method. For await interceptors, the name, the
    /// accessibility and the static or instance choice can be adjusted, and parameters can be added.</param>
    /// <param name="rewriteOptions">The options of the generated interceptor method and of the rewrite (section 5.6.2).</param>
    public static void InterceptAwaits<TScope>(
        this IQuery<TScope> query,
        string template,
        InterceptorPlacement placement,
        [Durable] Action<IInterceptorBuilder>? configure = null,
        AwaitRewriteOptions? rewriteOptions = null,
        AwaitInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    public static void InterceptAwaits<TScope, TProvider>(
        this IQuery<TScope> query,
        [Durable] Func<TScope, TProvider> createProvider,
        AwaitInterceptionOptions? options = null )
        where TScope : class, IDeclaration
        where TProvider : class, IAwaitInterceptorProvider;

    public static void InterceptAwaits<TScope, TTag, TProvider>(
        this ITaggedQuery<TScope, TTag> query,
        [Durable] Func<TScope, TTag, TProvider> createProvider,
        AwaitInterceptionOptions? options = null )
        where TScope : class, IDeclaration
        where TProvider : class, IAwaitInterceptorProvider;

    private static ImmutableArray<string> ValidateNames( IReadOnlyList<string> names );

    private static IInterceptionRegistrationService GetService( IQuery query )
        => query.Project.ServiceProvider.GetService<IInterceptionRegistrationService>()
           ?? throw new InvalidOperationException(
               "Interceptors require the Metalama.Extensions.Interceptors package in the project that executes this code." );
}
```

Notes on the parameters:

- The method overloads have no factory and no tagged-factory shape. The earlier design copied these shapes from `ValidateInboundReferences` (P27 `Metalama.Extensions.Validation\ReferenceValidationQueryExtensions.cs:64-79`). The engine finds the call sites by the name of the called method, so the selected methods must be fixed for each registration and cannot depend on the scope declaration. A result that depends on the scope declaration is chosen in the delegate given to the `ForEachSite` method, which reads `InterceptionContext.ScopeDeclaration` and, for a tagged query, `InterceptionContext.ScopeTag`. The await factories keep the earlier shapes, in which the tag is consumed at creation time.
- `[Durable]` on delegate parameters follows the existing precedents `SuppressionDefinition.WithFilter( [Durable] Func<...> )` (FW27 `Diagnostics\SuppressionDefinition.cs:85`) and `EligibilityExtensions.MustSatisfy( [Durable] Predicate<T>, ... )` (FW27 `Eligibility\EligibilityExtensions.cs:419-422`). The durability analyzer then checks each argument, including what a lambda captures. The delegate given to `InterceptMethods` is not `[Durable]`, because it runs once, immediately. The delegates that it gives to the `Types`, `Where`, `ForEachSite` and `Configure` methods are stored in the registration and evaluated in later compilations, so these parameters carry `[Durable]`, and so do the `args` of `RedirectToExistingMethod` and the parameters of `WithArgs`, `WithTags` and `WithTemplateProvider`.
- The declaring type given as a `Type` or an `INamedType` is stored as a durable reference to its definition (section [5.2](#52-seam-between-the-public-api-and-the-engine)). The names are stored as strings.
- The options of a method registration are methods of the chain, so that a later option adds a method to one stage instead of a parameter to every overload. The options of the earlier accessor and await design are records with default values, for the same reason, and `null` stands for the default values, because a default parameter value must be a compile-time constant.
- A synthesized method chosen on the chain takes its template arguments, tags, template provider, placement, granularity and `Configure` delegates from the methods of `ITemplateInterception`. The same methods apply to a result created in the delegate given to the `ForEachSite` method. The `args` object of `RedirectToExistingMethod` binds the parameters of an existing method by name (row "Explicit binding through arguments" of section [15.0](15-decisions.md#150-decisions-of-2026-10-02)).
- `AwaitRewriteOptions` is `[Durable]` and contains no code-model type. The type returned by an interceptor in mode `Awaitable` is passed to `InterceptorResult.WithAwaitRewriteOptions` by an await interceptor provider, in the earlier design of the await verb (section [5.6.2](05b-api-providers-contexts-results.md#562-await-rewrite-options)). The record was named `AwaitInterceptorOptions` in earlier versions. The third product-owner batch renamed it, because the name was too close to `AwaitInterceptionOptions`, which selects await expressions (RC53).

Each surface has one `InterceptMethods` overload for each receiver type, so the method verb has no overload resolution question. The stages of the chain are distinct interfaces, and each method of a stage returns the interface of the next stage, so the compiler rejects a chain that skips a stage or chooses two results. The await verb has one overload per provider form, and a string, a provider interface, a delegate over `AwaitInterceptionContext` and a factory delegate over `TScope` do not convert to each other. The await template shorthand takes a `string`, so no conversion to a selector happens for awaits (RC60). `MethodKind` is an enumeration, so the accessor kind of `InterceptAccessors` does not compete with a provider form.

#### 5.3.7 Aspect surface through IAdviser

> Superseded in part by the row "Fluent registration API" of section [15.0](15-decisions.md#150-decisions-of-2026-10-02) (2026-10-08, metalama/Metalama#2141): `InterceptMethods` has one overload on `IAdviser<T>`, which takes a `Func<IMethodInterceptionBuilder, IMethodInterception>`. The code below gives the new method overload. The accessor and await overloads keep their earlier design.

```csharp
namespace Metalama.Extensions.Interceptors;

/// <summary>
/// Provides extension methods that register interceptors from an aspect or a type fabric. The scope is the target of the
/// adviser: a compilation, a namespace, a type or a member.
/// </summary>
/// <remarks>
/// <para>
/// These methods can be called only during <see cref="IAspect{T}.BuildAspect"/> or during the amend method of a fabric.
/// They return nothing, because interception happens after all aspects have run. When the aspect ends with an error or is
/// skipped, its interceptors are discarded.
/// </para>
/// <para>
/// The scope must be contained in the closest type of the aspect target. When the aspect target is a namespace, the
/// scope must be contained in that namespace. When the aspect target is the compilation, any scope of the current
/// project is accepted.
/// </para>
/// <para>
/// A synthesized method whose template provider is not set with <see cref="ITemplateInterception.WithTemplateProvider"/> resolves its
/// template with the current template provider of the adviser. It therefore honors
/// <see cref="AdviserExtensions.WithTemplateProvider{TDeclaration}(IAdviser{TDeclaration}, ITemplateProvider)"/>.
/// </para>
/// </remarks>
[CompileTime]
[PublicAPI]
public static class InterceptionAdviserExtensions
{
    /// <summary>
    /// Registers the interception of the uses of methods, described by a delegate.
    /// </summary>
    /// <param name="adviser">An adviser whose target is the scope declaration.</param>
    /// <param name="build">A delegate that receives an <see cref="IMethodInterceptionBuilder"/>, selects the intercepted methods, chooses how the
    /// calls are intercepted, and returns the resulting <see cref="IMethodInterception"/>, for instance
    /// <c>b =&gt; b.Type( typeof(Console) ).Methods( "WriteLine" ).RedirectToSynthesizedMethod( nameof(this.Log) )</c>. It is invoked once,
    /// before this method returns.</param>
    public static void InterceptMethods<TScope>( this IAdviser<TScope> adviser, Func<IMethodInterceptionBuilder, IMethodInterception> build )
        where TScope : class, IDeclaration
        => InterceptionApiHelper.GetService( adviser.Compilation.Project ).Register( adviser, InterceptionApiHelper.CreateRegistration( build ) );

    // Accessors, earlier design: each overload of the earlier methods group has a twin named InterceptAccessors with a
    // parameter MethodKind accessorKind after the names (section 5.3.13).

    // Awaits, earlier design: three provider forms (provider object, delegate, template shorthand), with no target selection
    // (section 5.3.4).

    public static void InterceptAwaits<TScope>(
        this IAdviser<TScope> adviser,
        IAwaitInterceptorProvider provider,
        AwaitInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    public static void InterceptAwaits<TScope>(
        this IAdviser<TScope> adviser,
        [Durable] Func<AwaitInterceptionContext, InterceptorResult> getInterceptor,
        AwaitInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    public static void InterceptAwaits<TScope>(
        this IAdviser<TScope> adviser,
        string template,
        InterceptorPlacement placement,
        [Durable] Action<IInterceptorBuilder>? configure = null,
        AwaitRewriteOptions? rewriteOptions = null,
        AwaitInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    // The ITypeAmender overloads of section 5.3.8 are declared here.

    private static IInterceptionRegistrationService GetService( IAdviser adviser )
        => adviser.Compilation.Project.ServiceProvider.GetService<IInterceptionRegistrationService>()
           ?? throw new InvalidOperationException(
               "Interceptors require the Metalama.Extensions.Interceptors package in the project that executes this code." );
}
```

No factory overload exists on `IAdviser<T>`, because an adviser has a single target. The methods return `void`, like `AddAspect` and `AddAnnotation` (FW27 `Aspects\AdviserExtensions.cs:1910-1918, 2035-2044`). A premium result type cannot implement `IAdviceResult`, which is `[InternalImplement]` (FW26 `Advising\IAdviceResult.cs:57-58`). The engine implementation obtains the owner, the aspect target and the current template provider through the open-source adviser bridge (section [10.2](10a-oss-bridge-hook-factory.md#102-adviser-bridge-b2a)). The target selection needs no context of the owner: a declaring type, names and a type predicate mean the same thing on every surface, and an await registration has no target selection.

#### 5.3.8 ITypeAmender overloads

> Superseded in part by the row "Fluent registration API" of section [15.0](15-decisions.md#150-decisions-of-2026-10-02) (2026-10-08, metalama/Metalama#2141): `InterceptMethods` has one `ITypeAmender` overload, which takes the registration delegate and routes to the query path. The accessor and await overloads keep their earlier design.

EXISTING: `ITypeAmender` implements both `IAmender<INamedType>`, which is an `IQuery<INamedType>`, and `IAdviser<INamedType>` (FW27 `Fabrics\ITypeAmender.cs:34`). A call such as `amender.InterceptMethods( b => b.Type( typeof(File) ).Methods( "ReadAllText" ).ForEachSite( Intercept ) )` would find two candidates with equally good receiver conversions (CS0121). The framework solves the same problem for `AddAspect` with `ITypeAmender` overloads (FW27 `Aspects\AdviserExtensions.cs:1944-1967`).

PROPOSED: one overload for each shape that exists on both surfaces. `InterceptMethods` has one overload. In the earlier design, `InterceptAccessors` has four provider forms and four target selections (16 overloads), and `InterceptAwaits` has three provider forms and no target selection (3 overloads). Earlier versions had 16 overloads of `InterceptMethods`, four type selections for awaits and 44 overloads in total (RC58). The overloads route to the query path, so the fabric is the owner and the default template provider.

```csharp
    /// <summary>
    /// Registers the interception of the uses of methods, described by a delegate. The scope declaration is the type of the fabric.
    /// </summary>
    /// <remarks>
    /// This overload resolves the ambiguity between the <see cref="IAdviser{T}"/> and <see cref="IQuery{T}"/> overloads
    /// for <see cref="ITypeAmender"/>, which implements both interfaces. The registration is made through the query.
    /// </remarks>
    public static void InterceptMethods( this ITypeAmender amender, Func<IMethodInterceptionBuilder, IMethodInterception> build )
        => InterceptionQueryExtensions.InterceptMethods<INamedType>( amender, build );

    // Earlier design: sixteen overloads named InterceptAccessors, with MethodKind accessorKind after the names.

    public static void InterceptAwaits( this ITypeAmender amender, IAwaitInterceptorProvider provider, AwaitInterceptionOptions? options = null );

    public static void InterceptAwaits( this ITypeAmender amender, [Durable] Func<AwaitInterceptionContext, InterceptorResult> getInterceptor, AwaitInterceptionOptions? options = null );

    public static void InterceptAwaits( this ITypeAmender amender, string template, InterceptorPlacement placement, [Durable] Action<IInterceptorBuilder>? configure = null, AwaitRewriteOptions? rewriteOptions = null, AwaitInterceptionOptions? options = null );
```

The await factory overloads need no `ITypeAmender` variant, because only the query surface declares them.

#### 5.3.9 Registration rules

Timing:

- Registration is accepted only while the owner is active: during `BuildAspect`, `AmendProject`, `AmendNamespace` or `AmendType`. A registration through an adviser after `BuildAspect` returns throws `InvalidOperationException`, because the engine calls `ThrowIfDisposed` on the adviser context (section [10.2](10a-oss-bridge-hook-factory.md#102-adviser-bridge-b2a)). A registration through `builder.Outbound` after `BuildAspect` is silently lost today, because `AspectBuilderState.AddContributor` adds to a list that `ToResult` has already read (ENG27 `Aspects\AspectBuilderState.cs:78-101`). Open-source fix F12 makes it throw.
- A call from inside an interceptor provider, from a delegate given to the `ForEachSite` or `Configure` method, or from a template throws `InvalidOperationException`.
- The delegate given to `InterceptMethods` runs once, before `InterceptMethods` returns. The delegates that it passes to the `Types`, `Where`, `ForEachSite` and `Configure` methods are stored with the registration and run later, in the engine.
- Interceptors registered by an aspect whose outcome is Error or Ignore are discarded (ENG26 `Pipeline\ExecuteAspectLayerPipelineStep.cs:137-147`). This is documented behavior, not a diagnostic.
- An aspect that runs in a later high-level stage, after a low-level weaver, cannot register interceptors. The engine reports LAMA1007.

Scope validation (reported by the engine):

- Query surface. The rule of `IQueryImpl.InvokeAsync` applies: a selected declaration must be contained in the closest type of the query root, or in the root namespace when the query root is a namespace. A compilation root accepts any declaration (ENG27 `Queries\Query.cs:473-493`). Violations are reported with LAMA1000.
- Adviser surface. The scope must be contained in the containing declaration of the aspect target, computed as `Query.InvokeAsync` computes it: the compilation, the namespace, or the closest named type of the aspect target (RC19). The check uses the aspect target, not the current target of the adviser, because `AdviceFactory.WithDeclaration` and `AspectBuilder.With` do not validate containment, and the self-comparison of FW27 `Aspects\AdviserExtensions.cs:2109` always passes. Violations are reported with LAMA1000.
- On both surfaces, a scope must be a compilation, a namespace, a named type or a member declared in the source of the current project (LAMA1001), and must not be introduced by an aspect (LAMA1002).

Argument validation (synchronous exceptions):

| Condition | Exception |
|---|---|
| A required argument is null, including the registration delegate, a declaring type, a type predicate, a method predicate, a name, a template name and a delegate given to the `ForEachSite` or `Configure` method. | `ArgumentNullException` |
| The list of names is empty, or `AllMethods` selects no method that can be intercepted. | `ArgumentException` |
| A name is not a valid C# identifier. | `ArgumentException` |
| The accessor kind of `InterceptAccessors` is not `PropertyGet`, `PropertySet`, `EventAdd` or `EventRemove`. | `ArgumentException` |
| A `Type` cannot be resolved in the current compilation by `TypeFactory.GetNamedType`. | The exception of `TypeFactory.GetNamedType`, which propagates to the caller. |
| The registration delegate returns `null`, or a value that does not select the intercepted methods and choose a result. | `ArgumentException` |
| The registration delegate creates a value that is not part of the returned chain, or creates more than 64 values. | `InvalidOperationException` |
| Registration outside the active period of the owner. | `InvalidOperationException` |

The type predicates and the method predicates are not invoked during the registration call. An exception thrown later by a type predicate is reported by the engine (section [9.5.5](09-premium-engine.md#955-target-matching)).

Delegate rules (RC33). Every stored delegate may be a method of the aspect or fabric, or a lambda expression: the delegate given to the `ForEachSite` method, the delegates given to the `Configure` method, the type predicates given to the `Types` method, the method predicates given to the `Where` method, and, in the earlier design of the accessor and await verbs, the `getInterceptor` delegate, the factories and the `configure` function of the template shorthands. Their parameters carry `[Durable]`, so the durability analyzer checks what a lambda captures at the point where it is written (LAMA0878). An earlier version of this design required an `interceptMethod` delegate to point to a uniquely named method of the owner type, so that a manifest could rebuild it by name, as transitive validators do (P27 `Metalama.Extensions.Validation.Engine\TransitiveValidatorInstance.cs:85-88`). Registrations are never serialized in version 1 (section [5.4](05b-api-providers-contexts-results.md#54-interceptor-provider-interfaces)), so the rule is removed.

#### 5.3.10 Default template provider

A synthesized method, or a template shorthand of the earlier await design, can omit the template provider. The provider is then chosen in this order:

1. The provider given explicitly: the `WithTemplateProvider` method of `ITemplateInterception`, `TemplateInvocation.TemplateProvider`, or the `templateProvider` parameter of the earlier await design.
2. In the earlier await design only, the provider object, when the interceptor provider is a class instance (not a delegate) that implements `ITemplateProvider`. A method registration has no provider object, so this rule does not apply to it.
3. The default provider of the registration. On the adviser surface, this is the current template provider of the adviser, so `WithTemplateProvider` is honored (FW27 `Aspects\AdviserExtensions.cs:2052-2073`). On the query surface, this is the aspect or the fabric that owns the query. Both `IAspect` and `Fabric` implement `ITemplateProvider` (FW27 `Aspects\IAspect.cs:48`; `Fabrics\Fabric.cs:45`).

Rule 2 excludes delegates, because the target of a delegate is the aspect, and rule 2 would otherwise ignore `WithTemplateProvider`.

#### 5.3.11 Kinds of method use

A method registration selects methods, not syntax. Every use of a selected method in the scope is presented to the provider, whatever its kind. The registration has no filter by kind of use. The context gives the kind of the current site in `MethodInterceptionContext.Kind`, of the non-flags enumeration `MethodUseKind` (section [5.5.2](05b-api-providers-contexts-results.md#552-methodinterceptioncontext-and-invocationargument)).

| `MethodUseKind` | Site | Examples | Rewritten site |
|---|---|---|---|
| `Call` | An `InvocationExpressionSyntax` bound to an `IInvocationOperation`. | `x.M( a )`, `E.M( x, a )`, `a?.M( x )` | A call of the interceptor (section [6.3](06a-call-site-model.md#63-rules-table)). |
| `DelegateCreation` | A method group converted to a delegate: an `IMethodReferenceOperation` whose parent is an `IDelegateCreationOperation` (section [3.3](03-background.md#33-reference-index)). | `list.Select( Transform )`, `new Action( M )`, `Action a = M;`, `var d = M;`, `button.Click += this.OnClick;`, `button.Click -= this.OnClick;` | A method group of the interceptor, for example `list.Select( MetalamaInterceptors.Transform_Interceptor )`, or a wrapper that evaluates the receiver once (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)). |
| `FunctionPointer` | A method group converted to a function pointer: an `IMethodReferenceOperation` whose parent is an `IAddressOfOperation`. C# converts only static methods to function pointers. | `delegate*<int, int> square = &Square;` | `&MetalamaInterceptors.Square_Interceptor` (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)). |

The reason for the absence of a filter is equivalence (RC44, which reverses the earlier decision PO44). A filter with the default value `Call` would make equivalent C# programs compile to programs that are not equivalent:

```csharp
// Both statements call Transform once per element.
var a = list.Select( x => Transform( x ) );   // The call inside the lambda is a call site.
var b = list.Select( Transform );             // The method group is a method-reference site.
```

> Superseded in part by the decision "Provider factories, event subscriptions as a use kind, and the call-site member" of section 15.0 (2026-10-06): a method group added to an event has the use kind `EventSubscription`, and a method group removed from an event has the use kind `EventUnsubscription`, instead of `DelegateCreation` (the decision "Conflicts per registration and event unsubscriptions" of section 15.0 (2026-10-06)), and `ConvertedType` and `IsEventSubscription` are removed with `IMethodReference`.

A registration that intercepts `Transform` intercepts both statements, so both programs keep the same behavior. The default is therefore the sound behavior, and a delegate given to the `ForEachSite` method skips a kind of use that it does not want:

```csharp
amender.InterceptMethods( b => b
    .Type( typeof(Transforms) ).Methods( nameof(Transforms.Transform) )
    .ForEachSite(
        site =>

            // This registration must not change the identity of event handlers, so it leaves subscriptions unchanged.
            site.Context.UseKind is MethodUseKind.EventSubscription or MethodUseKind.EventUnsubscription
                ? site.Skip()
                : site.RedirectToSynthesizedMethod( "LogTransform" ).WithPlacement( InterceptorPlacement.GeneratedStaticClass() ) ) );
```

Rules:

- The interceptor method, the template, `meta.Proceed()` and the group key are the same for every kind. A group can contain call sites and method-reference sites when their signature is identical and method-group convertible (section [8.2](08-deduplication-and-naming.md#82-the-key)).
- The members of the context that describe a call have no meaning for a method-reference site: `Arguments` is empty, `IsResultUsed` and `IsConditionalAccess` are `false`, and the receiver passing mode does not apply. `Receiver` is the receiver of the method group, which C# evaluates when the delegate is created, or `null` when the method is static or the receiver is implicit. `ConvertedType` is the delegate type or the function pointer type. `IsEventSubscription` is `true` for the right operand of `+=` and `-=` on an event.
- Method groups in expression trees, in `nameof`, method groups that are not converted to a delegate or to a function pointer, method groups of local functions and of the `Invoke` method of a delegate type, and method groups in compile-time code are never presented (section [6.2.10](06a-call-site-model.md#6210-method-reference-sites)).
- A method group of the interceptor must keep the method-group conversion and the natural function type of the site unchanged. Only the canonical binding of an unchanged signature, with the default mode `Declared`, can therefore stay a method group. Any other binding, and any other adjustment of the signature than the name, the accessibility and the receiver mapping, makes the site use a lambda wrapper, which is the program that the equivalent lambda form produces (section [5.6.10](05b-api-providers-contexts-results.md#5610-the-callers-instance-and-method-reference-sites), RC69). An event subscription or unsubscription that would need a wrapper gets the limitation `DelegateEqualityRequired` (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)).

Behavior changes that the documentation states, and that a provider avoids by returning `Skip` (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites) gives the details):

| Topic | Change |
|---|---|
| Reflection on the delegate | `Delegate.Method` returns the interceptor method instead of the target. The interceptor copies the attributes of the target that reflection-based frameworks read, such as the parameter attributes that ASP.NET minimal APIs read (section [6.4.5](06b-signatures-and-validation.md#645-attributes)). The parameter names are the names of the target. |
| Delegate equality | A delegate created in the scope and a delegate created outside the scope do not compare equal. An event handler added with `+=` in the scope is not removed by a `-=` outside the scope, and the reverse. A wrapper creates a new delegate each time, so a wrapper is never used for event subscriptions: such a site gets the limitation `DelegateEqualityRequired`. |
| Null receiver | The original conversion throws when the delegate is created. Under R1x, an extension-method delegate accepts a null receiver, so the exception moves to the first invocation of the delegate. |
| Allocation | A method group and a rewritten method group allocate one delegate. A wrapper allocates a closure and a delegate. |

Later versions can add values to `MethodUseKind`, for example the method uses without invocation syntax of section [16.3](16-future-directions.md#163-other-method-uses-without-invocation-syntax). A new kind is presented to existing registrations, because the equivalence argument applies to it. The documentation of `MethodUseKind` states this, and a provider that depends on the kind tests for the kinds that it supports and skips the others.

Accessor sites (section [5.3.13](#5313-accessors)) have the kind `MethodUseKind.Call`. C# cannot convert an accessor to a delegate or to a function pointer, so an accessor has no other kind of use. `InterceptMethods` never matches an accessor, and `InterceptAccessors` never matches an ordinary method.

#### 5.3.12 Target selection: options considered

The product-owner reviews of [Appendix D](appendix-d-product-owner-reviews.md#appendix-d-product-owner-reviews-of-2026-09-24-and-2026-09-25) compared seven ways to select the targets of a registration. The first review chose option 4, reference predicates. The third batch chose option 7, a declaring type plus member names, and withdrew option 4 together with the extraction of `Metalama.Extensions.References` that it required (RC46, RC47). The table uses these criteria:

- Familiarity: whether Metalama users already know the construct.
- Name filter before binding: whether the engine can obtain the simple names of the targets without binding, so that the index binds only the bodies that contain one of the names.
- Cost of broad rules: what a rule without a name costs, for example "every method of a namespace".
- Caching: how the engine avoids repeated evaluation of user code.
- Open-source change: what the open-source repository must change.
- Breaking change: which existing public API breaks.
- Durability: whether the selection can be kept across compilations at design time.
- Serializability: whether the selection could be written to a manifest later.
- Sharing with validators: whether the same construct serves validators and interceptors.

| Criterion | 1. Selector class `InterceptedMethods` (original design) | 2. `IQuery<IMethod>` evaluated forward, rooted by a lambda | 3. `IQuery<IMethod>` evaluated backward as a membership test | 4. Reference predicates (earlier choice, considered, not adopted) | 5. Durable lambda over members plus explicit names | 6. `Expression<Func<IMethod, bool>>` | 7. Declaring type plus member names (chosen) |
|---|---|---|---|---|---|---|---|
| Familiarity | New type. | Known from fabrics. | Known from fabrics. | Known from Architecture rules. | Plain C#. | Plain C#, with restrictions. | Plain C#: a type and names, or a lambda over types. |
| Name filter before binding | Yes, from `Named` and `Method`. | Only after the query is evaluated over referenced types, which enumerates external members. | Only for operators that state names; the query engine does not expose its operators today. | Yes, from `MemberName` and `Method`, through an analysis of the predicate tree. | Yes, from the explicit names. | Yes, from constant comparisons that the engine recognizes. | Always: the names are mandatory. |
| Cost of broad rules | Every invocation in scope is bound. | The forward evaluation enumerates every method of every referenced type that the root selects. | Every invocation in scope is bound, and the membership test is cheap. | Every invocation in scope is bound; a hidden diagnostic reports the cost. | Every invocation in scope is bound. | Every invocation in scope is bound. | No registration binds every body: a broad rule is a type predicate with names. |
| Caching | Predicate result per definition. | The query result is a set, computed once per run. | Needs a cache per query level, and a query engine that keeps its operators as data. | Result per declaration at the granularity of the predicate. | Result per definition. | Result per definition. | Type predicate result per type definition. |
| Open-source change | None. | Queries rooted outside the current compilation. | The query engine must keep operators as data and evaluate them backward. | None; the shared index of section [10.7.5](10c-oss-reference-graph-design-time.md#1075-shared-index-of-source-references) is separate. | None. | None. | None. |
| Premium change | None. | None. | None. | Extraction of `Metalama.Extensions.References`: the predicates move out of Architecture, `ReferenceEnd`, `ReferenceEndRole` and `ReferenceGranularity` move out of Validation, and a base class `ReferenceContext` is added. | None. | None. | None. |
| Breaking change | None. | None. | None. | The predicates move to a new namespace, and `IsMatchCore` and `ValidatedRole` change. | None. | None. | None. |
| Durability | Yes, with durable references. | The query holds a compilation-bound root unless it is rebuilt per run. | Same as option 2. | Yes, predicates hold durable references. | Yes, checked by the durability analyzer. | Yes. | Yes: a durable reference, strings, and a `[Durable]` lambda. |
| Serializability | Not built in. | No. | No. | Yes, `ReferencePredicate` is `ICompileTimeSerializable` (P27 `Metalama.Extensions.Architecture\Predicates\ReferencePredicate.cs:25`). | No. | No. | The type and the names are; the type predicate is not. |
| Sharing with validators | No. | No. | No. | The same language, and predicates state their own index requirements. | No. | No. | Through the shared index only: the names become requirements of the index. |
| Other | A second vocabulary next to Architecture. | Evaluation direction does not match the question that the engine asks at a call site. | Large change to a core engine component. | The move breaks user predicates (CS0115), and the fluent predicate language is large for a registration that needs a type and names. | Names and lambda can disagree. | Rejected: expression trees forbid `?.`, pattern matching and throw expressions. | No predicate over members: the provider filters signatures and returns `Skip`. |

The findings of the first review, which chose option 4:

1. Binding dominates the cost of a scan, and Roslyn binds a whole member body at a time. The only saving that is possible before binding is the simple name of the invoked method. Every option that can state names reaches the same binding cost, and every option without names binds every body in scope.
2. Granularity only memoizes the evaluation of the predicate, which is cheap compared with binding. It does not reduce binding.
3. Serializability is not needed in version 1, because registrations are project-local (section [5.4](05b-api-providers-contexts-results.md#54-interceptor-provider-interfaces)).
4. The shared scan pass with validators (section [10.7.5](10c-oss-reference-graph-design-time.md#1075-shared-index-of-source-references)) made the predicate language look like the natural choice, because a predicate can state its own index requirements.

The reasons of the third batch for option 7:

1. The API is simpler: a type and names, with a lambda over types for the rare case of several types.
2. The names always give the pre-binding filter, so no registration binds every body in scope, and the hidden cost hint LAMA1009 is no longer needed.
3. The provider already receives the constructed method and filters signatures, so a predicate over members duplicates what the provider does.
4. The shared index does not need a shared predicate language: every extension states its requirements as `ReferenceIndexerRequirements`. Without predicates, the extraction of `Metalama.Extensions.References` and its breaking changes have no purpose (RC47).

The row "Fluent registration API" of section [15.0](15-decisions.md#150-decisions-of-2026-10-02) (2026-10-08) added the `Where` method, a `[Durable]` predicate over the method definitions that the names select. It does not replace the names, which remain the filter of the index. It lets a registration keep one overload without a delegate per site, which reason 3 above required.

#### 5.3.13 Accessors

The accessor verb is not part of the fluent registration API of the row "Fluent registration API" of section [15.0](15-decisions.md#150-decisions-of-2026-10-02). This section keeps its earlier design, with `IMethodInterceptorProvider`, `MethodInterceptionOptions` and `MethodMatching`, until milestone M2 revises it.

`InterceptAccessors` intercepts the uses of one accessor of properties and events (decision PO53, RC48). It has the four target selections of the earlier design of section [5.3.3](#533-target-selection-for-members) and every provider form of the earlier design of `InterceptMethods`, and it takes a mandatory `MethodKind accessorKind` after the names:

```csharp
InterceptAccessors( Type declaringType, IReadOnlyList<string> names, MethodKind accessorKind, <provider form>, MethodInterceptionOptions? options = null )
```

Rules of the verb:

- `accessorKind` accepts `MethodKind.PropertyGet`, `PropertySet`, `EventAdd` and `EventRemove` (FW27 `Code\MethodKind.cs:29-44`). `EventRaise` and every other value throw `ArgumentException`. C# declares no raise accessor, and the only raise accessors come from other languages.
- An `init` accessor has the kind `PropertySet`. Its sites are presented with the limitation `InitOnlySetter` (section [6.2.11](06a-call-site-model.md#6211-accessor-sites)).
- The names select properties and events. Indexers are a future direction (section [16.5](16-future-directions.md#165-indexers)). A name that the declaring type uses only for a method is reported with LAMA1008, with the advice to use `InterceptMethods`.
- The interceptor provider is an `IMethodInterceptorProvider`, and the context is a `MethodInterceptionContext` (section [5.5.2](05b-api-providers-contexts-results.md#552-methodinterceptioncontext-and-invocationargument)). `InterceptedMethod` is the accessor, `Destination` is the property or the event, and `Kind` is `MethodUseKind.Call`.
- The matching policies of `MethodMatching` apply to the property or the event, as for methods: with `Overrides`, a use of an overriding property matches a registration on the overridden property.

A mandatory accessor kind does not contradict the principle of RC44, which forbids a segregation by kind of use. RC44 concerns two syntaxes that use the same method, a call and a method group, which must behave the same, because they are equivalent programs. `get_P` and `set_P` are two different methods with two different signatures. Choosing the accessor is choosing the target, as choosing a name is. For the same reason, one registration has exactly one signature shape: a getter, a setter, or an add or remove accessor. The template shorthands therefore work: one template implements the interceptors of one registration.

```csharp
// Log every write of Order.Status and every read of Order.Total in the project.
amender.InterceptAccessors( typeof(Order), nameof(Order.Status), MethodKind.PropertySet, nameof(this.LogWrite), InterceptorPlacement.GeneratedStaticClass() );
amender.InterceptAccessors( typeof(Order), nameof(Order.Total), MethodKind.PropertyGet, nameof(this.LogRead), InterceptorPlacement.GeneratedStaticClass() );
```

Sites. Section [6.2.11](06a-call-site-model.md#6211-accessor-sites) gives the detection rules, and section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites) the signatures and the rewrites.

| Site | Example | Accessor uses |
|---|---|---|
| Read | `x = r.P`, `F( r.P )`, `C.P` | get |
| Write, value not used | `r.P = v;` | set |
| Write, value used | `a = r.P = v`, `F( r.P = v )` | set; the setter interceptor returns the assigned value (RC49) |
| Compound assignment | `r.P += v`, `r.P <<= 2` | get and set |
| Increment and decrement | `r.P++`, `++r.P`, `r.P--`, `--r.P` | get and set |
| Null-coalescing assignment | `r.P ??= v` | get, and set when the value read is null |
| Conditional access | `r?.P`, `r?.P = v` (C# 14 null-conditional assignment) | get; set |
| Event subscription | `r.E += h`, `r.E -= h` | add; remove |
| Static members | `C.P`, `C.P = v`, `C.E += h` | as above, without receiver |
| Receivers | `this.P`, `P`, `base.P`, `other.P` | as above; `base.P` of a virtual property follows rule R4 |
| C# 14 extension properties | `r.Name` where `Name` is declared in an extension block | as above |

A compound site is two interceptions, a get and a set (RC50). The getter use comes from a `PropertyGet` registration and the setter use from a `PropertySet` registration, which can be different registrations of different sources. Each use is evaluated, skipped and checked for conflicts on its own, and the one-interceptor rule R7 counts per accessor use. When only one use is intercepted, the other accessor stays a plain access in the rewritten code. The receiver is evaluated once, as in the original code (section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites), [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call)).

Limitations (presented with `NonInterceptableReason`, section [6.2.11](06a-call-site-model.md#6211-accessor-sites)): object and `with` initializers, deconstruction targets, `init` accessors, ref-returning properties, C# 14 user-defined instance compound assignment and increment operators, and receivers that need a temporary that C# cannot express at the site. Never presented: the assignment of a getter-only auto-property in its own constructor, which writes the backing field; the use of a field-like event as a value inside its declaring type, which reads the field; `nameof`; expression trees.
