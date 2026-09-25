# User-facing API: template side

> Part of the [call-site interceptors design](README.md). Previous: [05b-api-providers-contexts-results.md](05b-api-providers-contexts-results.md) | Next: [05d-api-samples.md](05d-api-samples.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

### 5.7 Template-side API

#### 5.7.1 meta.Target

`meta.Target` is always the generated interceptor method (baseline B5), including for local-function placements (RC26):

| Placement and receiver mapping (section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)) | `meta.Target.Method` | `meta.Target.Type` | `meta.This` |
|---|---|---|---|
| Any type placement, static (R0, R1, R1x) | The generated static method | The placement type | Throws `CannotUseThisInStaticContext` (ENG27 `Templating\Expressions\ThisInstanceUserReceiver.cs:65-72`) |
| Receiver's type or a base type of it, instance (R2) | The generated instance method | The placement type | The receiver of the intercepted call |
| Calling type or a base type of it, instance (R3, and R0 with an instance method) | The generated instance method | The placement type | The calling object, which is the caller's `this` |
| Calling type, `base` call to a virtual method (R4) | The generated instance method | The placement type | The caller's `this`, which is also the receiver of the `base` call |
| Local function | A method whose `MethodKind` is `LocalFunction` (FW27 `Code\MethodKind.cs:67`) | The calling type | The caller's `this`, when the origin is an instance member of a class |

`meta.This` therefore means the receiver under R2 and R4, and the caller under R3. Its type is the placement type. With `BaseMostAccessibleType()`, the placement type is the base type that the walk selected, so a template can use only the members that are visible in that type (section [6.5.6](06b-signatures-and-validation.md#656-base-most-accessible-type)). `meta.MethodInterception.Receiver` gives the receiver in every shape (section [5.7.3](#573-metamethodinterception-and-metaawaitinterception)), so a template that must work with every placement reads the receiver through it, and not through `meta.This`.

`meta.Target.Parameters` are the parameters of the generated method: the receiver parameter first, when the receiver mapping has one (R1, R1x, R3), then one parameter for each parameter of the intercepted method, with the same names, then the parameters added by the builder. For await interceptors, the first parameter is the awaitable, followed by the added parameters. The receiver parameter is named `receiver` unless the name is taken. When a `configure` function changed the signature, `meta.Target` shows the adjusted signature (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)).

Keeping `meta.Target` on the generated method for local functions makes one template usable with every placement kind. The origin is exposed separately, as `meta.MethodInterception.Origin`. The alternative, where `meta.Target` is the origin, matches template-internal local functions (ENG26 `Templating\TemplateExpansionContext.cs:268, 757`). It was rejected because `meta.Target.Parameters` would then mean the caller's parameters in one placement and the intercepted arguments in another.

`meta.Target.Method.Name` is the final name, because names are allocated before expansion (section [8.7](08-deduplication-and-naming.md#87-names-and-accessibility)). Templates should still not depend on it, because it can change when the code changes.

#### 5.7.2 meta.Proceed()

`meta.Proceed()` invokes the intercepted target (interpretation I1):

| Dispatch | Expansion of `meta.Proceed()` |
|---|---|
| Static method | `C.M( p1, p2 )`; a classic extension method is called in static form |
| Instance, receiver parameter (R1, R1x, R3, and a local function whose receiver is not the caller's `this` in a class) | `receiver.M( p1, p2 )`, which keeps virtual and interface dispatch |
| Instance, receiver is the `this` of the interceptor (R2, and a local function whose receiver is the caller's `this` in a class) | `this.M( p1, p2 )`, which keeps virtual and interface dispatch |
| `base` call to a virtual method (R4) | `base.M( p1, p2 )` |
| Static abstract through a type parameter | `T.M( p1, p2 )` |
| Getter | `receiver.P`, `this.P`, `base.P` or `C.P`, with the receiver of the rows above |
| Setter | `receiver.P = value`, of the property type, whose value is the assigned value; the template writes `return meta.Proceed();` |
| Add or remove accessor | `receiver.E += handler` or `receiver.E -= handler`, of type `void` |
| Accessor of a C# 14 extension property | The static implementation form, `global::E.get_P( receiver )` or `global::E.set_P( receiver, value )`, as the existing invoker writes it (ENG27 `CodeModel\Invokers\FieldOrPropertyInvoker.cs:35-38, 70-101`) |
| Await, mode `Await`, non-async template | `(await awaitable)`, of type `R` |
| Await, mode `Await`, async template, or mode `Awaitable` | `awaitable`, of type `A` |

Additional rules:

- Arguments are passed with their reference kinds. Caller-information values are computed at the original call site and forwarded explicitly (section [6.7](06b-signatures-and-validation.md#67-caller-information-materialization)), so the intercepted method receives the same values as without interception. A `params` collection is passed in normal form, and the parameters added by a `configure` function are not passed (section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params)).
- The proceed call is a plain call. It binds to the final implementation of the intercepted method, as the original call did. When an aspect overrides the intercepted method in the same project, `meta.Proceed()` calls the overridden method.
- In a Default template of a method interceptor, `meta.Proceed()` returns the value of the call without awaiting or buffering it, unless the selector sets `UseAsyncTemplateForAnyAwaitable`. When the selector sets it and the return type of the generated method has a method builder, the Async variant is used when it exists. Otherwise, the Default template is interpreted as async, and `meta.Proceed()` awaits the call. In an Async variant, `meta.Proceed()` and `meta.ProceedAsync()` behave as in async override templates.
- A proceed call written `this.M( p1, p2 )` binds by member lookup in the placement. It is used only under R2, and only when the speculative binding of that call inside the placement gives the intercepted method (condition R2a of section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)). Otherwise, the engine uses R3 or R1, and the receiver is a parameter typed as the target's containing type. A local function is declared inside the origin, so its `this.M( p1, p2 )` binds as the source call does.
- A parameter whose type a `configure` function widened is cast back to the parameter type of the target in the proceed call, for example `M( (string)path )` (section [6.4.9](06b-signatures-and-validation.md#649-proceed-shape)).
- `meta.Proceed()` can be called zero, one or several times in a method interceptor. In an await interceptor, the proceed expression can be emitted at most once, and not inside a loop, a lambda or a local function (LAMA0296, section [7.9](07-await-interception.md#79-template-model)).

#### 5.7.3 meta.MethodInterception and meta.AwaitInterception

Templates read interception facts through two meta extensions, `MethodInterceptionInfo` and `AwaitInterceptionInfo`. The open-source mechanism of section [10.6.6](10b-oss-linker-and-templates.md#1066-meta-extensions-per-expansion-extension-data) attaches them to the expansion of a synthesized method, and `meta.GetExtension<T>()` returns them. The premium package adds two C# 14 static extension properties to the class `meta`, so a template writes `meta.MethodInterception.Receiver` or `meta.AwaitInterception.AwaitableType` (RC40). The extensions expose only facts that are part of the grouping identity, so the generated code is the same for every call site of a group. A group can contain call sites and method-reference sites (section [8.2](08-deduplication-and-naming.md#82-the-key)), so the extensions do not expose the kind of use. A group of accessor interceptors can contain plain accesses and compound sites, so the extensions do not expose the assignment operator, `IsPostfix` or `IsChecked` either.

```csharp
namespace Metalama.Extensions.Interceptors;

/// <summary>
/// Adds the interception accessors to <see cref="meta"/>. The properties can be used only in a template that implements
/// an interceptor. In any other template, they throw <see cref="InvalidOperationException"/>.
/// </summary>
/// <remarks>
/// The accessors require C# 14. With an older language version, call
/// <c>meta.GetExtension&lt;MethodInterceptionInfo&gt;()</c> or <c>meta.GetExtension&lt;AwaitInterceptionInfo&gt;()</c>
/// directly. To test whether the current template implements an interceptor, call
/// <c>meta.TryGetExtension&lt;MethodInterceptionInfo&gt;( out var info )</c>.
/// </remarks>
[CompileTime]
[PublicAPI]
public static class InterceptionMetaExtensions
{
    extension( meta )
    {
        /// <summary>Gets the facts of the invocation that the current template intercepts.</summary>
        public static MethodInterceptionInfo MethodInterception => meta.GetExtension<MethodInterceptionInfo>();

        /// <summary>Gets the facts of the await expression that the current template intercepts.</summary>
        public static AwaitInterceptionInfo AwaitInterception => meta.GetExtension<AwaitInterceptionInfo>();
    }
}

/// <summary>
/// Describes the invocation that the current template intercepts. Templates read it as <c>meta.MethodInterception</c>.
/// </summary>
/// <remarks>
/// The generated method can be shared by several call sites. For this reason, this class exposes only information that
/// is the same for all of them. Pass call-site information as template arguments. The expressions of this class, such as
/// <see cref="Receiver"/>, denote members of the generated method, so templates can emit them. They differ from the
/// inspection-only expressions of <see cref="MethodInterceptionContext"/>, which denote source code of one call site.
/// </remarks>
[CompileTime]
[PublicAPI]
public sealed class MethodInterceptionInfo : IMetaExtension
{
    internal MethodInterceptionInfo( /* filled by the engine */ );

    /// <summary>
    /// Gets the intercepted method, constructed with the type arguments that the generated method uses. For an accessor
    /// interceptor, it is the accessor.
    /// </summary>
    public IMethod Method { get; }

    /// <summary>
    /// Gets the destination of the intercepted sites: the property or the event for an accessor interceptor, and the
    /// definition of <see cref="Method"/> otherwise. It has the meaning of <see cref="MethodInterceptionContext.Destination"/>.
    /// </summary>
    public IMember Destination { get; }

    /// <summary>Gets how <c>meta.Proceed()</c> dispatches to <see cref="Method"/>.</summary>
    public InvocationDispatchKind DispatchKind { get; }

    /// <summary>
    /// Gets an expression that evaluates to the receiver object in every receiver mapping: <c>this</c> when the
    /// receiver is the <c>this</c> of the generated method (an instance method in the receiver's type hierarchy, or a
    /// <c>base</c> call to a virtual method), and the receiver parameter otherwise. The value is <c>null</c> when the
    /// intercepted call has no receiver. A template that reads the receiver through this property works with every
    /// placement.
    /// </summary>
    public IExpression? Receiver { get; }

    /// <summary>Gets the parameter of the generated method that receives the receiver, or <c>null</c> when the receiver is not a parameter.</summary>
    public IParameter? ReceiverParameter { get; }

    /// <summary>Gets the receiver mapping of the generated method. It is the same for every call site of a group.</summary>
    public InterceptorReceiverMapping ReceiverMapping { get; }

    /// <summary>Gets the parameters of the generated method that receive the arguments, in the order of the parameters of <see cref="Method"/>.</summary>
    public IReadOnlyList<IParameter> ArgumentParameters { get; }

    /// <summary>
    /// Gets the origin of the intercepted sites, which is the member that contains the generated local function. This
    /// property is available only when the placement is <see cref="InterceptorPlacement.LocalFunction"/>, because a method
    /// generated in a type can be shared by sites of different origins. In other placements, it throws
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <remarks>
    /// A local function is always declared in the origin of the sites that call it, so every site of its group has the
    /// same origin. The type is <see cref="IMethodBase"/>, because only a method, a constructor, an operator, a finalizer
    /// or an accessor can host a local function.
    /// </remarks>
    public IMethodBase Origin { get; }
}

/// <summary>
/// Describes the await expression that the current template intercepts. Templates read it as <c>meta.AwaitInterception</c>.
/// </summary>
[CompileTime]
[PublicAPI]
public sealed class AwaitInterceptionInfo : IMetaExtension
{
    internal AwaitInterceptionInfo( /* filled by the engine */ );

    /// <summary>Gets the parameter of the generated method that receives the awaitable.</summary>
    public IParameter AwaitableParameter { get; }

    /// <summary>Gets the awaited type <c>A</c>.</summary>
    public IType AwaitableType { get; }

    /// <summary>Gets the result type <c>R</c>, which is the <c>void</c> type when the await produces no value.</summary>
    public IType ResultType { get; }

    /// <summary>Gets the kind of the awaited expression.</summary>
    public AwaitableKind AwaitableKind { get; }

    /// <summary>Gets the mode of the generated interceptor.</summary>
    public AwaitInterceptionMode Mode { get; }

    /// <summary>
    /// Gets an expression that tells whether the awaited operation is already complete, without consuming it:
    /// <c>awaitable.IsCompleted</c> for tasks and value tasks, <c>awaitable.GetAwaiter().IsCompleted</c> for configured
    /// awaitables, the constant <c>false</c> for <c>Task.Yield()</c>, and <c>null</c> for custom awaitables.
    /// The expression is valid only before <c>meta.Proceed()</c>. After the awaitable has been awaited, reading it is
    /// undefined for a <c>ValueTask</c> that is backed by an <c>IValueTaskSource</c>.
    /// </summary>
    public IExpression? IsCompleted { get; }

    /// <summary>Gets the origin of the intercepted awaits, which contains the generated local function. See <see cref="MethodInterceptionInfo.Origin"/>.</summary>
    public IMethodBase Origin { get; }
}
```

Usage in a template:

```csharp
[Template]
private dynamic? LogCall()
{
    Console.WriteLine( $"Calling {meta.MethodInterception.Method.Name}." );

    return meta.Proceed();
}

// The same template, compiled with C# 13 or earlier.
[Template]
private dynamic? LogCallCSharp13()
{
    var info = meta.GetExtension<MethodInterceptionInfo>();
    Console.WriteLine( $"Calling {info.Method.Name}." );

    return meta.Proceed();
}
```

Implementation (PROPOSED). When the premium engine requests a synthesized method, it passes one `MethodInterceptionInfo` or one `AwaitInterceptionInfo` in `SynthesizedMethodTemplate.MetaExtensions` (section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)). The object is built from the representative request of the group, and it holds declarations of the final compilation of the stage. The open-source engine stores the extensions on the `MetaApi` of the expansion, and `meta.GetExtension<T>()` returns the first extension that is an instance of `T` (section [10.6.6](10b-oss-linker-and-templates.md#1066-meta-extensions-per-expansion-extension-data)). An earlier version passed an internal service through `SynthesizedMethodTemplate.ExpansionServices` and read it through `MetalamaExecutionContext.Current.ServiceProvider`, behind static accessor classes named `MethodInterception` and `InterceptedAwait` (RC12). RC40 replaced that design.

Language rules, verified in the Roslyn fork:

- An extension block can extend a static class. The only restriction is that it cannot declare user-defined operators (RC `Errors\ErrorCode.cs:2419`, `ERR_OperatorInExtensionOfStaticClass`; `Symbols\Source\SourceUserDefinedOperatorSymbolBase.cs:81-83`). The block `extension( meta )` has no parameter name, so it can declare only static members.
- The premium API assembly can be compiled with C# 14 while it targets netstandard2.0. Extension blocks need no run-time support. When the target framework does not define `System.Runtime.CompilerServices.ExtensionMarkerAttribute`, the compiler embeds the attribute in the assembly (RC `Emitter\Model\PEAssemblyBuilder.cs:583-589`; `Symbols\Compilation_WellKnownMembers.cs:688-692`). The package must therefore set `LangVersion` 14 or later only for its own build.
- A consumer whose language version is older than 14 can use every other member of the API. Only an access to an extension member reports the language-version error of the extensions feature, because the binder checks the feature at each extension member access (RC `Binder\Binder_Expressions.cs:8288`). Such a consumer calls `meta.GetExtension<T>()` directly. No separate fallback class exists.
- The static class that contains the block, `InterceptionMetaExtensions`, must be in scope. Templates add `using Metalama.Extensions.Interceptors;`, which they already need for the other types of the package.

Template compiler rules, to verify in M2:

- A reference to `meta.MethodInterception` binds to a member of the extension block, whose containing type is not `meta`. `TemplateMemberClassifier.GetMetaMemberKind` therefore returns `MetaMemberKind.None` for it (ENG27 `Templating\TemplateMemberClassifier.cs:107-136`), and the member is classified as an ordinary member of a compile-time type. The existing test of issue #1932 covers an extension block declared in a compile-time type of the same project (TST27 `Metalama.Framework.Tests.AspectTests\Tests\Aspects\CSharp14\ExtensionMembers\ExtensionMembers_CompileTimeExtensionMembers.cs`). M2 adds a test for an extension block on `meta` declared in a referenced compile-time assembly.
- `IParameter` is an `IExpression` (FW27 `Code\IParameter.cs:20`), and a parameter expression is emitted as the parameter name (ENG27 `Templating\Expressions\ParameterExpression.cs:21-22`). `meta.MethodInterception.Receiver.Value` therefore emits the receiver parameter or `this`.

#### 5.7.4 Template signature rules

For invocation templates:

- The return type is `dynamic?`, `void`, or a type compatible with the return type of the generated method under the existing override rules (ENG26 `Advising\TemplateBindingHelper.cs:746-837`).
- Run-time parameters are bound by name among all parameters of the generated method, then by ordinal among the parameters that follow the receiver parameter and precede the added parameters (section [10.6.2](10b-oss-linker-and-templates.md#1062-binder-with-hidden-leading-parameters)). An added parameter binds by name only. The names are those of the adjusted signature when a `configure` function renamed a parameter or added one (sections [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder) and [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values)). A run-time parameter that matches nothing is an error (LAMA1016).
- The receiver cannot be bound by ordinal. Use `meta.MethodInterception.Receiver`, which also covers the mappings in which the receiver is not a parameter.
- A run-time parameter bound to a `params` parameter is declared with the collection type, for example `object[]`, `ReadOnlySpan<int>` or `List<string>`, or with `dynamic`, and without the `params` modifier. The binding compares the types with the override rules, where the template type must convert to the parameter type (ENG27 `Advising\TemplateBindingHelper.cs:566-604, 789-793`), so `object[]` and `dynamic` both bind to `params object[] args`.
- A run-time parameter bound to an optional parameter is declared without a default value. Run-time template parameters declare no default value and no `params` modifier: the signature of the generated method comes from the call site, so they would have no effect, and the engine reports them with LAMA1016. The existing binding uses a default value only as the value of an omitted compile-time argument. For a run-time parameter, the default only moves the argument to the end of the compiled template invocation (ENG27 `Advising\TemplateBindingHelper.cs:913-953`).
- Compile-time parameters and compile-time type parameters receive values from `args`.
- Run-time type parameters are not allowed (LAMA1016), because the generated method can be specialized or lifted to different type parameters.

For accessor templates, the rules of invocation templates apply to the parameters of section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites): a getter template has no run-time parameter except the receiver, which it reads through `meta.MethodInterception.Receiver`; a setter template can bind the value parameter, named `value` by default; an add or remove template can bind the handler parameter, named `handler` by default. A setter template returns `dynamic?` or the property type, and usually ends with `return meta.Proceed();`.

For await templates (decision PO60, RC60):

- The template is referenced by its name, as a `string` or as a `TemplateInvocation`, never as a `MethodTemplateSelector`. A selector that names an alternative template or sets one of its flags is reported with LAMA1014 at an await site.
- The template has no run-time parameters, or one run-time parameter bound to the awaitable, followed by run-time parameters bound by name to the added parameters. The type of the parameter bound to the awaitable must be `dynamic`, `AnyAwaitable<dynamic?>`, or a type to which `A` converts. A parameter typed `AnyAwaitable<dynamic?>` binds to the awaited operand whatever its type (section [10.6.9](10b-oss-linker-and-templates.md#1069-anyawaitable)).
- A non-async template, for example `dynamic? T()`, receives the awaited value: in mode `Await`, `meta.Proceed()` expands to `(await awaitable)`, of type `R`, and the engine makes the interceptor `async` with the task type of section [7.7](07-await-interception.md#77-task-type-of-an-async-interceptor-and-target-frameworks). In mode `Awaitable`, `meta.Proceed()` returns the awaitable.
- An `async` template awaits the value of `meta.Proceed()` or of `meta.ProceedAsync()`, which is the awaitable `A`. The standard form is `async AnyAwaitable<dynamic?> T()`, and the engine chooses the task type of the interceptor, `ValueTask<R>` by default. A template declared `async Task<dynamic?>` or `async ValueTask<dynamic?>` keeps its literal meaning and gives `Task<R>` or `ValueTask<R>` (section [7.9.1](07-await-interception.md#791-accepted-template-shapes), RC67).
- In mode `Awaitable`, the template must not be `async` (LAMA1022).

#### 5.7.5 Rules that keep deduplication correct

The documentation states these rules for template authors:

1. Read call-site information only through template arguments and tags. Both are part of the grouping identity. The accessors expose no call-site information, and `Origin` is available only for local functions, whose group never spans origins.
2. Do not use `meta.Target.Method.Name` in generated code or in diagnostics.
3. Do not keep state in static fields or in the template provider between expansions. A template is expanded once for each group, not once for each call site.
4. In an await template, emit the proceed expression at most once, whether the template is `async` or not. Read `meta.AwaitInterception.IsCompleted` only before `meta.Proceed()`. After the awaitable has been awaited, reading it is undefined for a `ValueTask` that is backed by an `IValueTaskSource`.
5. Do not assume that `meta.AspectInstance` exists. For registrations made by project or namespace fabrics, no aspect instance exists.

#### 5.7.6 Other meta members

| Member | Behavior in interceptor templates |
|---|---|
| `meta.Tags` | Tags of the result, merged with `IAspectBuilder.Tags` of the registering aspect. |
| `meta.AspectInstance` | The registering aspect instance; not available for fabric registrations. |
| `meta.This` | See section [5.7.1](#571-metatarget). |
| `meta.Receiver` | Not supported, because it denotes `this` or an extension receiver of `meta.Target`. Use `meta.MethodInterception.Receiver`. |
| `meta.InvokeTemplate` | Supported. Called templates inherit the meta extensions of their caller, so they can read `meta.MethodInterception` and `meta.AwaitInterception` (section [10.6.6](10b-oss-linker-and-templates.md#1066-meta-extensions-per-expansion-extension-data)). |
| `meta.GetExtension<T>()`, `meta.TryGetExtension<T>(out T?)` | Return `MethodInterceptionInfo` or `AwaitInterceptionInfo`. `GetExtension` throws `InvalidOperationException` when the expansion has no extension of type `T`. |
| `meta.InsertStatement`, `meta.DefineLocalVariable` | Supported, as in other method templates. |
