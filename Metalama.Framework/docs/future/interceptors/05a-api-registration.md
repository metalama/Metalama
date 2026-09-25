# User-facing API: overview and registration

> Part of the [call-site interceptors design](README.md). Previous: [04-mechanism.md](04-mechanism.md) | Next: [05b-api-providers-contexts-results.md](05b-api-providers-contexts-results.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

## 5. User-facing API of the premium package

All types of this section are PROPOSED and live in the namespace `Metalama.Extensions.Interceptors` of the assembly `Metalama.Extensions.Interceptors` (package `Metalama.Extensions.Interceptors.Redist`). The API uses no type of another premium package.

### 5.1 The API at a glance

| Concern | Types and members |
|---|---|
| Registration verbs | `InterceptMethods` (ordinary methods and extension methods), `InterceptAccessors` (property and event accessors, with a mandatory `MethodKind` accessor kind), `InterceptAwaits` |
| Fabric and query surface | `InterceptionQueryExtensions` (extension methods on `IQuery<T>` and `ITaggedQuery<T,TTag>`) |
| Aspect surface | `InterceptionAdviserExtensions` (extension methods on `IAdviser<T>`, plus `ITypeAmender` overloads) |
| Target selection | Members: a declaring type (`Type` or `INamedType`, matched by definition) or a `[Durable] Func<INamedType, bool>` over declaring type definitions, plus a mandatory list of member names (`IReadOnlyList<string>`, or one `string`). Awaits: no target selection; the scope selects the await expressions, and the provider skips the ones that it does not want (section [5.3.4](#534-await-registrations)). The enumeration `MethodMatching`. A method registration has no filter by kind of use (section [5.3.11](#5311-kinds-of-method-use)). |
| Registration options | `MethodInterceptionOptions` (for both member verbs), `AwaitInterceptionOptions`, `InterceptionScopeOptions` |
| Interceptor provider interfaces | `IMethodInterceptorProvider` (for both member verbs), `IAwaitInterceptorProvider`, each with a method `GetInterceptor` |
| Contexts | `InterceptionContext` (with `Origin` and `IsInNestedFunction`), `MethodInterceptionContext` (with `Kind`, `ConvertedType`, `IsEventSubscription`, `Receiver`, `Destination`, `AssignmentOperator`, `IsPostfix` and `IsChecked`), `AwaitInterceptionContext` (with `Operand`), `InvocationArgument` (with `Expression`), `AwaitConfiguration` |
| Context enumerations | `MethodUseKind` (`Call`, `DelegateCreation`, `FunctionPointer`), `InvocationArgumentKind`, `InvocationDispatchKind`, `InvocationReceiverKind`, `EnclosingCodeKind`, `NonInterceptableReason`, `AwaitResumption`, `AwaitConfigurationFlags`, `AwaitableKind` (the kind of one awaited expression, not a flags enumeration); the existing `OperatorKind`, with the new member `NullCoalescingAssignment` |
| Result model | `InterceptorResult`, `InterceptorResultKind`, `AwaitRewriteOptions`, `AwaitInterceptionMode`, `AwaitInterceptorTaskKind` |
| Placement model | `InterceptorPlacement` (`CallingType`, `InType`, `BaseMostAccessibleType`, `GeneratedStaticClass`, `LocalFunction`), `InterceptorPlacementKind` |
| Parameter binding | `InterceptorArgument` (the source of the value of one interceptor parameter: `Receiver`, `Argument`, `Value`, `Awaitable`, `CallerInstance`, `CallerInfo`, `Pull`), `CallerInfoKind`, `IInterceptorMethodBinder` (for existing methods), `IInterceptorParameterBinder`, `IInterceptorParameterBinderList`, `InterceptorReceiverMapping` |
| Signature builder | `IInterceptorBuilder` (for template results, derived from `IInterceptorMethodBinder`), `IInterceptorParameterBuilder`, `IInterceptorParameterBuilderList`, `InterceptorDefaultMode` |
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

The public extension methods resolve the service with `GetService`, not `GetRequiredService`. When the service is missing, they throw `InvalidOperationException` with a message that names the package `Metalama.Extensions.Interceptors`. This happens when a library references only the Redist package and its transitive fabric runs in a consumer that does not load the engine (section [9.9.2](09-premium-engine.md#992-licensing-implications)).

### 5.3 Registration

#### 5.3.1 Three verbs

The API has three verbs: `InterceptMethods`, `InterceptAccessors` and `InterceptAwaits`. `InterceptMethods` intercepts ordinary methods, classic extension methods and C# 14 extension methods. `InterceptAccessors` intercepts the accessors of properties and events (section [5.3.13](#5313-accessors)). `InterceptMethods` never matches an accessor. The two member verbs share the provider interface, the context, the options and the result model. The API has no single `Intercept` verb, for these reasons:

- Members and awaits need different target selections, options and contexts. A member registration has a declaring type, names and a matching policy, and its context describes the kind of use. An await registration has no target selection, and its context describes the awaitable, the resumption and the `ConfigureAwait` call (section [5.3.4](#534-await-registrations)).
- A single verb with delegate overloads is ambiguous in C#. The lambda `ctx => ...` converts to both `Func<MethodInterceptionContext, InterceptorResult>` and `Func<AwaitInterceptionContext, InterceptorResult>`, which raises CS0121.
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
| `INamedType` | All members of the type, including field and property initializers, constructor initializers and primary-constructor base arguments. Nested types are excluded unless `InterceptionScopeOptions.IncludeNestedTypes` is set. |
| `IMember` | The body of the member, its accessors, its initializer and its constructor initializer. |

Code in lambdas and local functions is in scope unless `InterceptionScopeOptions.ExcludeLambdasAndLocalFunctions` is set.

The following code is never in scope: compile-time code; code introduced by aspects (R6, interpretation I5); code produced by source generators, which runs after Metalama (section [3.8](03-background.md#38-roslyn-fork-and-compiler-order)); and, by default, files that Roslyn classifies as generated code (PO12). Code of a member that aspects override is in scope, because it is source code.

Nested types are excluded by default for a concrete reason (RC18). An aspect applied to every type of a namespace through `amender.SelectTypes()`, whose `includeNestedTypes` parameter defaults to `true` (FW27 `Fabrics\IQuery{T}.cs:83`), creates one aspect instance on the outer type and one on the nested type. If both scopes contained the nested code, each call site in the nested type would receive two interceptors from two different sources, which is an error under R7.

#### 5.3.3 Target selection for members

The target is a registration parameter, not a property of the provider object. One provider class can therefore serve several targets.

A member registration selects its targets with a declaring type and a list of member names (decision PO52, RC46). Section [5.3.12](#5312-target-selection-options-considered) compares this choice with the other options that were considered. The shapes are the same for `InterceptMethods` and `InterceptAccessors`, which adds the accessor kind after the names (section [5.3.13](#5313-accessors)):

```csharp
// One declaring type, given as a System.Type, and a list of names.
InterceptMethods( Type declaringType, IReadOnlyList<string> names, <provider form>, MethodInterceptionOptions? options = null )

// One declaring type, given as a code-model type.
InterceptMethods( INamedType declaringType, IReadOnlyList<string> names, <provider form>, MethodInterceptionOptions? options = null )

// A durable predicate over declaring type definitions.
InterceptMethods( [Durable] Func<INamedType, bool> declaringTypes, IReadOnlyList<string> names, <provider form>, MethodInterceptionOptions? options = null )

// Convenience: one declaring type and one name.
InterceptMethods( Type declaringType, string name, <provider form>, MethodInterceptionOptions? options = null )
```

The provider form is an interceptor provider object, a delegate, an existing method, a template shorthand, or a factory (section [5.3.6](#536-fabric-and-query-surface)).

```csharp
amender.InterceptMethods( typeof(Guid), nameof(Guid.NewGuid), new SystemHookProvider() );

amender.InterceptMethods( typeof(File), [nameof(File.ReadAllText), nameof(File.WriteAllText)], new FileAccessProvider() );

// Every type of a namespace. A project fabric cannot enumerate the types of the compilation, so a type predicate is the
// way to select several declaring types.
amender.InterceptMethods(
    t => t.ContainingNamespace.FullName.StartsWith( "Contoso.Legacy", StringComparison.Ordinal ),
    ["Save", "Load"],
    new LegacyStorageProvider() );
```

Rules for the declaring type:

- A `Type` is resolved with `TypeFactory.GetNamedType` (FW27 `Code\TypeFactory.cs:62`) when the registration is made, and the registration stores the definition as a durable reference. An `INamedType` is stored in the same way. `typeof(List<>)` matches every construction of `List<T>`. A constructed type such as `typeof(List<int>)` is reduced to its definition, so it also matches every construction. A provider that cares about one construction tests `context.InterceptedMethod.DeclaringType` and returns `InterceptorResult.Skip`.
- The declaring type of a site is the type that declares the member to which the C# compiler binds the site, taken as its definition. It is not the static type of the receiver. For example, `fileStream.CopyTo( other )` binds to `Stream.CopyTo`, so a registration on `FileStream` does not match it, and a registration on `Stream` does. Matching is static: it does not consider the run-time type of the receiver.
- The predicate overload takes a `[Durable] Func<INamedType, bool>`. The engine evaluates it after a name matched and the site was bound, once per distinct declaring type definition, and memoizes the result per compilation. The predicate receives a type definition of the scanned compilation. It must be deterministic and thread-safe. The `[Durable]` parameter lets the durability analyzer reject a lambda that captures a declaration, a symbol or another compilation-bound object (LAMA0878). An exception thrown by the predicate is reported once, and the type then counts as not matching (section [9.5.5](09-premium-engine.md#955-target-matching)).
- The predicate is the only way to select several declaring types, for example the types of a namespace, as above (`INamedType.ContainingNamespace`, FW27 `Code\INamedType.cs:79`). Project and namespace fabrics run once per pipeline configuration (section [3.1](03-background.md#31-pipeline-stages-and-extension-hooks)) and cannot enumerate the types of the compilation. There is deliberately no dedicated namespace overload and no overload that takes several types.

Rules for the names:

- At least one name is required. An empty list throws `ArgumentException`, and so does a name that is not a valid C# identifier. There is no registration without names.
- The names are always the pre-binding filter of the shared index (section [9.5.3](09-premium-engine.md#953-registration-index-and-index-requirements)): the index binds only the member bodies that contain one of the names.
- All overloads of a matched name match. The registration does not filter by signature. The provider tests the signature through the context, for example `context.InterceptedMethod.Parameters`, and returns `InterceptorResult.Skip` for the overloads that it does not want. Sample 5 shows this.
- A generic method is matched by its bare name, without type arguments, for example `Select` for `Select<int>( ... )`. A classic extension method has the same name in its reduced form and in its static form, because the index normalizes the reduced form (section [10.7.3](10c-oss-reference-graph-design-time.md#1073-reducedfrom-normalization)). An override and an implicit interface implementation have the name of the method that they override or implement, so the names are compatible with every `MethodMatching` policy. An explicit interface implementation can only be called through the interface method, whose name the registration states.

Other matching rules:

- Generic methods and members of generic types are matched through their definitions. `MethodInterceptionContext.InterceptedMethod` gives the constructed method at a site.
- With `MethodMatching.Overrides`, the engine also tests the members that the bound member overrides, directly or indirectly. With `MethodMatching.InterfaceImplementations`, it also tests the interface members that the bound member implements, implicitly or explicitly. For each member walked, in this order (the bound member, then the overridden members from the nearest, then the implemented interface members), the engine tests the declaring type, or evaluates the type predicate on it. The site matches when the name matches and one member passes. The first member that passes is `MethodInterceptionContext.MatchedMethod`.
- `InterceptMethods` matches only ordinary methods, classic extension methods and C# 14 extension methods. It never matches an accessor, an operator, a constructor, a finalizer or a local function. A name that the declaring type uses only for a property or an event is reported with the warning LAMA1008, with the advice to use `InterceptAccessors` (section [9.4.7](09-premium-engine.md#947-registration-time-checks)).
- Several registrations of the same source that overlap on a site count once for that site, under the one-source rule of section [9.5.8](09-premium-engine.md#958-conflict-detection-r7-b7). This happens, for example, when an aspect registers a base type and a derived type with `MethodMatching.Overrides`, or registers a type predicate and a declaring type that it also accepts. The source is the registering aspect instance or fabric instance.

```csharp
namespace Metalama.Extensions.Interceptors;

/// <summary>
/// Options of a registration made with <c>InterceptMethods</c> or <c>InterceptAccessors</c>.
/// </summary>
/// <remarks>
/// The object contains no declaration, so it can be kept across compilations.
/// </remarks>
[CompileTime]
[PublicAPI]
[Durable]
[ImmutableType]
public sealed record MethodInterceptionOptions
{
    /// <summary>
    /// Gets the policy for calls that the C# compiler binds to an override or to an interface implementation.
    /// The default value is <see cref="MethodMatching.Overrides"/>.
    /// </summary>
    public MethodMatching Matching { get; init; } = MethodMatching.Overrides;

    /// <summary>Gets the options that determine which code of each scope declaration is in scope.</summary>
    public InterceptionScopeOptions Scope { get; init; } = InterceptionScopeOptions.Default;
}

/// <summary>
/// Determines whether a site that the C# compiler binds to an override or to an interface implementation matches a
/// member that the registration selects by its declaring type and its name.
/// </summary>
[CompileTime]
[Flags]
public enum MethodMatching
{
    /// <summary>A site matches only when the registration selects the member to which it is bound.</summary>
    Exact = 0,

    /// <summary>A site also matches when the registration selects a member that the bound member overrides, directly or indirectly.</summary>
    Overrides = 1,

    /// <summary>A site also matches when the registration selects an interface member that the bound member implements, implicitly or explicitly.</summary>
    InterfaceImplementations = 2,

    /// <summary>Combines <see cref="Overrides"/> and <see cref="InterfaceImplementations"/>.</summary>
    OverridesAndInterfaceImplementations = Overrides | InterfaceImplementations
}
```

The enumeration `MethodMatching` was named `InterceptedMethodMatching` in an earlier version of this design. Its members are unchanged.

A matching policy is necessary. The compiler binds `derived.M()` to the override declared by the static type (RC `CodeGen\EmitExpression.cs:1999-2008`), and it binds `stream.Dispose()` to the class method rather than to `IDisposable.Dispose`. The default is `Overrides` (RC17, decision PO6): a registration on `Stream.Write` then intercepts `fileStream.Write(...)`, which is the same logical method with virtual dispatch. Interface implementations are opt-in, because a call bound to a class method is not obviously a call to the interface. Name filtering in the index still works under every policy, because an override or an implicit implementation is called with the same simple name, and an explicit implementation can only be called through the interface method, whose name the registration states.

Targets are matched by definition. Durable references identify definitions, and the reference index normalizes references to `OriginalDefinition` (ENG26 `ReferenceGraph\ReferenceIndexBuilder.cs:20-21`). A provider that cares about a specific construction filters it and returns `InterceptorResult.Skip`.

#### 5.3.4 Await registrations

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

Fabrics call these methods on their amender, because `IAmender<T>` derives from `IQuery<T>` (FW27 `Fabrics\IAmender.cs:63`). Aspects can call them on `IAspectBuilder<T>.Outbound` (FW27 `Aspects\IAspectBuilder.cs:240`).

```csharp
namespace Metalama.Extensions.Interceptors;

/// <summary>
/// Provides extension methods that register interceptors. Each declaration selected by the query is a scope: the
/// invocations, method references, property and event accesses, and await expressions located in its code are presented
/// to the interceptor provider.
/// </summary>
/// <remarks>
/// <para>
/// Registration only records the interceptor. Call sites are found later, in the source compilation, after all aspects
/// and fabrics have run. Call sites in code introduced by aspects are never intercepted.
/// </para>
/// <para>
/// At most one interceptor can apply to a call site. When several registrations match the same call site, each
/// interceptor is evaluated, and it is an error when more than one of them returns a result other than
/// <see cref="InterceptorResult.Skip"/>.
/// </para>
/// </remarks>
/// <seealso href="@intercepting-call-sites"/>
[CompileTime]
[PublicAPI]
public static class InterceptionQueryExtensions
{
    // Methods. Every overload of this group exists with four target selections, which have the same documentation:
    // (Type declaringType, IReadOnlyList<string> names), (INamedType declaringType, IReadOnlyList<string> names),
    // ([Durable] Func<INamedType, bool> declaringTypes, IReadOnlyList<string> names), and (Type declaringType, string name).
    // Only the first selection is written out below.

    /// <summary>
    /// Registers an interceptor provider for the uses of the methods that have one of the given names and that are declared
    /// by a given type, in the code of each selected declaration.
    /// </summary>
    /// <param name="query">A query that selects the scope declarations.</param>
    /// <param name="declaringType">The type that declares the intercepted methods. It is matched by definition, so
    /// <c>typeof(List&lt;&gt;)</c> selects the methods of every construction of <c>List&lt;T&gt;</c>.</param>
    /// <param name="names">The names of the intercepted methods. Every overload of each name is selected. At least one name
    /// is required. The provider tests the signature and returns <see cref="InterceptorResult.Skip"/> for the overloads
    /// that it does not intercept.</param>
    /// <param name="provider">The interceptor provider, which is invoked once for each matching site and returns the
    /// interceptor of that site.</param>
    /// <param name="options">The matching policy and the scope options. When the value is <c>null</c>,
    /// the default values of <see cref="MethodInterceptionOptions"/> apply.</param>
    /// <exception cref="ArgumentException"><paramref name="names"/> is empty or contains a string that is not a valid C# identifier.</exception>
    public static void InterceptMethods<TScope>(
        this IQuery<TScope> query,
        Type declaringType,
        IReadOnlyList<string> names,
        IMethodInterceptorProvider provider,
        MethodInterceptionOptions? options = null )
        where TScope : class, IDeclaration
        => GetService( query ).Register(
            query,
            InterceptionRegistration.ForMethods(
                TypeSelector.FromType( declaringType ),
                ValidateNames( names ),
                InterceptorDefinition.FromProvider( provider ),
                options ?? new MethodInterceptionOptions() ) );

    /// <summary>
    /// Registers an interceptor provider for the uses of the methods that have one of the given names and whose declaring
    /// type satisfies a predicate.
    /// </summary>
    /// <param name="declaringTypes">A predicate over type definitions. The engine evaluates it once per distinct declaring
    /// type of a site whose name matches, and memoizes the result. It must be deterministic and thread-safe, and it must not
    /// capture declarations or other objects of a compilation. The durability analyzer checks what a lambda expression
    /// captures.</param>
    public static void InterceptMethods<TScope>(
        this IQuery<TScope> query,
        [Durable] Func<INamedType, bool> declaringTypes,
        IReadOnlyList<string> names,
        IMethodInterceptorProvider provider,
        MethodInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    /// <summary>Registers an interceptor provider, given as a delegate, for the uses of the selected methods.</summary>
    /// <param name="getInterceptor">A method of the aspect or fabric, or a lambda expression. The durability analyzer
    /// checks what a lambda expression captures.</param>
    public static void InterceptMethods<TScope>(
        this IQuery<TScope> query,
        Type declaringType,
        IReadOnlyList<string> names,
        [Durable] Func<MethodInterceptionContext, InterceptorResult> getInterceptor,
        MethodInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    /// <summary>Replaces the uses of the selected methods with uses of an existing method.</summary>
    /// <param name="replacementMethod">The existing method. See <see cref="InterceptorResult.ExistingMethod"/> for the rules.</param>
    /// <param name="bind">A function that binds the parameters of the existing method for each site (see
    /// <see cref="IInterceptorMethodBinder"/>), or <c>null</c> for the canonical binding. The durability analyzer checks
    /// what a lambda expression captures, because the registration stores the function.</param>
    public static void InterceptMethods<TScope>(
        this IQuery<TScope> query,
        Type declaringType,
        IReadOnlyList<string> names,
        IMethod replacementMethod,
        [Durable] Action<IInterceptorMethodBinder>? bind = null,
        MethodInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    /// <summary>
    /// Intercepts the uses of the selected methods with a method generated from a template. This is equivalent to an
    /// interceptor that always returns <see cref="InterceptorResult.Template(in MethodTemplateSelector, InterceptorPlacement, Action{IInterceptorBuilder}?, object?, object?, TemplateProvider)"/>.
    /// </summary>
    /// <param name="template">The name of the template, or a <see cref="MethodTemplateSelector"/>. The template is resolved
    /// on the aspect or fabric that owns the query.</param>
    /// <param name="placement">The declaration in which the interceptor method is generated.</param>
    /// <param name="configure">A function that adjusts the generated signature for each site, for example its name
    /// (see <see cref="IInterceptorBuilder"/>), or <c>null</c>. The durability analyzer checks what a lambda
    /// expression captures, because the registration stores the function.</param>
    public static void InterceptMethods<TScope>(
        this IQuery<TScope> query,
        Type declaringType,
        IReadOnlyList<string> names,
        in MethodTemplateSelector template,
        InterceptorPlacement placement,
        [Durable] Action<IInterceptorBuilder>? configure = null,
        MethodInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    /// <summary>Registers an interceptor provider created by a factory for each selected declaration.</summary>
    public static void InterceptMethods<TScope, TProvider>(
        this IQuery<TScope> query,
        Type declaringType,
        IReadOnlyList<string> names,
        [Durable] Func<TScope, TProvider> createProvider,
        MethodInterceptionOptions? options = null )
        where TScope : class, IDeclaration
        where TProvider : class, IMethodInterceptorProvider;

    /// <summary>
    /// Registers an interceptor provider created by a factory for each selected declaration. The factory receives the tag
    /// of the declaration. The tag is consumed by the factory and is not exposed to the provider.
    /// </summary>
    public static void InterceptMethods<TScope, TTag, TProvider>(
        this ITaggedQuery<TScope, TTag> query,
        Type declaringType,
        IReadOnlyList<string> names,
        [Durable] Func<TScope, TTag, TProvider> createProvider,
        MethodInterceptionOptions? options = null )
        where TScope : class, IDeclaration
        where TProvider : class, IMethodInterceptorProvider;

    // Accessors (section 5.3.13). Every overload of the methods group has a twin named InterceptAccessors, with the same
    // four target selections, and with a parameter MethodKind accessorKind after the names. For example:

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
    // provider (section 5.3.4). The factory overloads have the same documentation as the method overloads.

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

- The factory and tagged-factory shapes are copied from `ValidateInboundReferences` (P27 `Metalama.Extensions.Validation\ReferenceValidationQueryExtensions.cs:64-79`). The tag is consumed at creation time, so the context needs no `Tag` property.
- `[Durable]` on delegate parameters follows the existing precedents `SuppressionDefinition.WithFilter( [Durable] Func<...> )` (FW27 `Diagnostics\SuppressionDefinition.cs:85`) and `EligibilityExtensions.MustSatisfy( [Durable] Predicate<T>, ... )` (FW27 `Eligibility\EligibilityExtensions.cs:419-422`). The durability analyzer then checks each argument, including what a lambda captures. The type predicates of the member verbs are stored in the registration and evaluated in later compilations, so they carry `[Durable]` too.
- The declaring type given as a `Type` or an `INamedType` is stored as a durable reference to its definition (section [5.2](#52-seam-between-the-public-api-and-the-engine)). The names are stored as strings.
- The options are records with default values, so that a later option does not add a parameter to every overload. `null` stands for the default values, because a default parameter value must be a compile-time constant.
- The template shorthands take an optional `configure` function instead of a name, and the `IMethod` shorthands take an optional `bind` function (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)). The registration stores the functions, so the parameters carry `[Durable]`, like the `getInterceptor` delegate. The functions themselves store nothing: they run once per call site in stage 1.
- The template shorthands have no `args` and no `tags` parameter. Registrations are stored across compilations at design time, so these values would have to be `[Durable]`. The durability analyzer classifies anonymous types as not durable and would report LAMA0872 for `args: new { ... }`, which is the usual form of template arguments. A template that needs arguments or tags is returned by an interceptor provider, given as an object or a delegate, through `InterceptorResult.Template`, whose `args` and `tags` are not `[Durable]` because a result is consumed within one run.
- `AwaitRewriteOptions` is `[Durable]` and contains no code-model type. The type returned by an interceptor in mode `Awaitable` is passed to `InterceptorResult.WithAwaitRewriteOptions` by an interceptor provider (section [5.6.1](05b-api-providers-contexts-results.md#561-interceptorresult)). The record was named `AwaitInterceptorOptions` in earlier versions. The third product-owner batch renamed it, because the name was too close to `AwaitInterceptionOptions`, which selects await expressions (RC53).

Overload resolution was checked shape by shape. The first parameter after the receiver is a `System.Type`, an `INamedType` or a delegate over `INamedType`, and these three types have no conversion between them, except the `null` literal, which is not a valid declaring type and which C# reports as ambiguous (CS0121). A string does not convert to `IReadOnlyList<string>`, so the overload with one name and the overload with a list never compete; a string array and a collection expression convert only to the list. A string converts to `MethodTemplateSelector` through its implicit operator (FW27 `Advising\MethodTemplateSelector.cs:174`), and does not convert to a provider interface, to `IMethod` or to a delegate. The await template shorthand takes a `string`, so no conversion to a selector happens for awaits (RC60). A method group whose parameter is a context type does not convert to `Func<TScope, TProvider>`, because `TScope` is a declaration type fixed by the receiver. `IMethod` is `[InternalImplement]`, so no user object implements both `IMethod` and a provider interface. `MethodKind` is an enumeration, so the accessor kind of `InterceptAccessors` does not compete with a provider form. The await verb has one overload per provider form, and a string, a provider interface, a delegate over `AwaitInterceptionContext` and a factory delegate over `TScope` do not convert to each other.

#### 5.3.7 Aspect surface through IAdviser

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
/// Template shorthands resolve templates with the current template provider of the adviser. They therefore honor
/// <see cref="AdviserExtensions.WithTemplateProvider{TDeclaration}(IAdviser{TDeclaration}, ITemplateProvider)"/>.
/// </para>
/// <para>
/// Each method of this class has the same parameters, and the same XML documentation, as the corresponding method of
/// <see cref="InterceptionQueryExtensions"/>. The documentation is omitted below for brevity only.
/// </para>
/// </remarks>
[CompileTime]
[PublicAPI]
public static class InterceptionAdviserExtensions
{
    // Methods: four provider forms (provider object, delegate, existing method, template shorthand), each with the four
    // target selections of section 5.3.3. Only the first selection is written out.

    public static void InterceptMethods<TScope>(
        this IAdviser<TScope> adviser,
        Type declaringType,
        IReadOnlyList<string> names,
        IMethodInterceptorProvider provider,
        MethodInterceptionOptions? options = null )
        where TScope : class, IDeclaration
        => GetService( adviser ).Register(
            adviser,
            InterceptionRegistration.ForMethods(
                TypeSelector.FromType( declaringType ),
                ValidateNames( names ),
                InterceptorDefinition.FromProvider( provider ),
                options ?? new MethodInterceptionOptions() ) );

    public static void InterceptMethods<TScope>(
        this IAdviser<TScope> adviser,
        Type declaringType,
        IReadOnlyList<string> names,
        [Durable] Func<MethodInterceptionContext, InterceptorResult> getInterceptor,
        MethodInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    public static void InterceptMethods<TScope>(
        this IAdviser<TScope> adviser,
        Type declaringType,
        IReadOnlyList<string> names,
        IMethod replacementMethod,
        [Durable] Action<IInterceptorMethodBinder>? bind = null,
        MethodInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    public static void InterceptMethods<TScope>(
        this IAdviser<TScope> adviser,
        Type declaringType,
        IReadOnlyList<string> names,
        in MethodTemplateSelector template,
        InterceptorPlacement placement,
        [Durable] Action<IInterceptorBuilder>? configure = null,
        MethodInterceptionOptions? options = null )
        where TScope : class, IDeclaration;

    // Accessors: each overload of the methods group has a twin named InterceptAccessors with a parameter
    // MethodKind accessorKind after the names (section 5.3.13).

    // Awaits: three provider forms (provider object, delegate, template shorthand), with no target selection (section 5.3.4).

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

EXISTING: `ITypeAmender` implements both `IAmender<INamedType>`, which is an `IQuery<INamedType>`, and `IAdviser<INamedType>` (FW27 `Fabrics\ITypeAmender.cs:34`). A call such as `amender.InterceptMethods( typeof(File), ["ReadAllText"], provider )` would find two candidates with equally good receiver conversions (CS0121). The framework solves the same problem for `AddAspect` with `ITypeAmender` overloads (FW27 `Aspects\AdviserExtensions.cs:1944-1967`).

PROPOSED: one overload for each shape that exists on both surfaces. `InterceptMethods` and `InterceptAccessors` have four provider forms and four target selections each (32 overloads), and `InterceptAwaits` has three provider forms and no target selection (3 overloads), which gives 35 overloads. Earlier versions had four type selections for awaits and 44 overloads (RC58). They route to the query path, so the fabric is the owner and the default template provider.

```csharp
    /// <remarks>
    /// This overload resolves the ambiguity between the <see cref="IAdviser{T}"/> and <see cref="IQuery{T}"/> overloads
    /// for <see cref="ITypeAmender"/>, which implements both interfaces.
    /// </remarks>
    public static void InterceptMethods( this ITypeAmender amender, Type declaringType, IReadOnlyList<string> names, IMethodInterceptorProvider provider, MethodInterceptionOptions? options = null )
        => ((IQuery<INamedType>) amender).InterceptMethods( declaringType, names, provider, options );

    public static void InterceptMethods( this ITypeAmender amender, Type declaringType, IReadOnlyList<string> names, [Durable] Func<MethodInterceptionContext, InterceptorResult> getInterceptor, MethodInterceptionOptions? options = null );

    public static void InterceptMethods( this ITypeAmender amender, Type declaringType, IReadOnlyList<string> names, IMethod replacementMethod, [Durable] Action<IInterceptorMethodBinder>? bind = null, MethodInterceptionOptions? options = null );

    public static void InterceptMethods( this ITypeAmender amender, Type declaringType, IReadOnlyList<string> names, in MethodTemplateSelector template, InterceptorPlacement placement, [Durable] Action<IInterceptorBuilder>? configure = null, MethodInterceptionOptions? options = null );

    // The same four forms with (INamedType, IReadOnlyList<string>), ([Durable] Func<INamedType, bool>, IReadOnlyList<string>)
    // and (Type, string); and the same sixteen overloads named InterceptAccessors, with MethodKind accessorKind after the names.

    public static void InterceptAwaits( this ITypeAmender amender, IAwaitInterceptorProvider provider, AwaitInterceptionOptions? options = null );

    public static void InterceptAwaits( this ITypeAmender amender, [Durable] Func<AwaitInterceptionContext, InterceptorResult> getInterceptor, AwaitInterceptionOptions? options = null );

    public static void InterceptAwaits( this ITypeAmender amender, string template, InterceptorPlacement placement, [Durable] Action<IInterceptorBuilder>? configure = null, AwaitRewriteOptions? rewriteOptions = null, AwaitInterceptionOptions? options = null );
```

The factory overloads need no `ITypeAmender` variant, because only the query surface declares them.

#### 5.3.9 Registration rules

Timing:

- Registration is accepted only while the owner is active: during `BuildAspect`, `AmendProject`, `AmendNamespace` or `AmendType`. A registration through an adviser after `BuildAspect` returns throws `InvalidOperationException`, because the engine calls `ThrowIfDisposed` on the adviser context (section [10.2](10a-oss-bridge-hook-factory.md#102-adviser-bridge-b2a)). A registration through `builder.Outbound` after `BuildAspect` is silently lost today, because `AspectBuilderState.AddContributor` adds to a list that `ToResult` has already read (ENG27 `Aspects\AspectBuilderState.cs:78-101`). Open-source fix F12 makes it throw.
- A call from inside an interceptor provider or from a template throws `InvalidOperationException`.
- Interceptors registered by an aspect whose outcome is Error or Ignore are discarded (ENG26 `Pipeline\ExecuteAspectLayerPipelineStep.cs:137-147`). This is documented behavior, not a diagnostic.
- An aspect that runs in a later high-level stage, after a low-level weaver, cannot register interceptors. The engine reports LAMA1007.

Scope validation (reported by the engine):

- Query surface. The rule of `IQueryImpl.InvokeAsync` applies: a selected declaration must be contained in the closest type of the query root, or in the root namespace when the query root is a namespace. A compilation root accepts any declaration (ENG27 `Queries\Query.cs:473-493`). Violations are reported with LAMA1000.
- Adviser surface. The scope must be contained in the containing declaration of the aspect target, computed as `Query.InvokeAsync` computes it: the compilation, the namespace, or the closest named type of the aspect target (RC19). The check uses the aspect target, not the current target of the adviser, because `AdviceFactory.WithDeclaration` and `AspectBuilder.With` do not validate containment, and the self-comparison of FW27 `Aspects\AdviserExtensions.cs:2109` always passes. Violations are reported with LAMA1000.
- On both surfaces, a scope must be a compilation, a namespace, a named type or a member declared in the source of the current project (LAMA1001), and must not be introduced by an aspect (LAMA1002).

Argument validation (synchronous exceptions):

| Condition | Exception |
|---|---|
| A required argument is null, including a declaring type, a type predicate, a name and a template name. | `ArgumentNullException` |
| The list of names is empty. | `ArgumentException` |
| A name is not a valid C# identifier. | `ArgumentException` |
| The accessor kind of `InterceptAccessors` is not `PropertyGet`, `PropertySet`, `EventAdd` or `EventRemove`. | `ArgumentException` |
| A `Type` cannot be resolved in the current compilation by `TypeFactory.GetNamedType`. | The exception of `TypeFactory.GetNamedType`, which propagates to the caller. |
| Registration outside the active period of the owner. | `InvalidOperationException` |

The type predicates are not invoked during the registration call. An exception thrown later by a type predicate is reported by the engine (section [9.5.5](09-premium-engine.md#955-target-matching)).

Delegate rules (RC33). Every delegate may be a method of the aspect or fabric, or a lambda expression: the `getInterceptor` delegate, the factories, the `configure` function of the template shorthands, and the type predicates of the member verbs. Their parameters carry `[Durable]`, so the durability analyzer checks what a lambda captures at the point where it is written (LAMA0878). An earlier version of this design required an `interceptMethod` delegate to point to a uniquely named method of the owner type, so that a manifest could rebuild it by name, as transitive validators do (P27 `Metalama.Extensions.Validation.Engine\TransitiveValidatorInstance.cs:85-88`). Registrations are never serialized in version 1 (section [5.4](05b-api-providers-contexts-results.md#54-interceptor-provider-interfaces)), so the rule is removed.

#### 5.3.10 Default template provider

A template result or a template shorthand can omit the template provider. The provider is then chosen in this order:

1. The provider given explicitly in the result (`TemplateInvocation.TemplateProvider` or the `templateProvider` parameter).
2. The provider object, when the interceptor provider is a class instance (not a delegate) that implements `ITemplateProvider`.
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

A registration that intercepts `Transform` intercepts both statements, so both programs keep the same behavior. The default is therefore the sound behavior, and a provider that does not want a kind of use skips it:

```csharp
internal sealed class TransformProvider : IMethodInterceptorProvider
{
    public InterceptorResult GetInterceptor( MethodInterceptionContext context )
    {
        // This provider must not change the identity of event handlers, so it leaves subscriptions unchanged.
        if ( context.IsEventSubscription )
        {
            return InterceptorResult.Skip;
        }

        return InterceptorResult.Template( "LogTransform", InterceptorPlacement.GeneratedStaticClass() );
    }
}
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

#### 5.3.13 Accessors

`InterceptAccessors` intercepts the uses of one accessor of properties and events (decision PO53, RC48). It has the four target selections of section [5.3.3](#533-target-selection-for-members) and every provider form of `InterceptMethods`, and it takes a mandatory `MethodKind accessorKind` after the names:

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
