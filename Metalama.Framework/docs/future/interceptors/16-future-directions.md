# Future directions

> Part of the [call-site interceptors design](README.md). Previous: [15-decisions.md](15-decisions.md) | Next: [appendix-a-evidence.md](appendix-a-evidence.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

## 16. Future directions

This section sketches eleven directions that version 1 does not implement. The product owner states that delegate-based interceptions are closer to PostSharp and useful in practice, so the design must not close them. The sketches of indexers, operators and target-side registration (section [16.5](#165-indexers) to [16.7](#167-target-side-registration)) are API sketches only; the product owner stated that details are not needed now. Sections [16.8](#168-the-proceed-and-packedarguments-sources-interception-without-a-template) to [16.11](#1611-functions-and-locals-that-enclose-a-site) were added by the sixth product-owner batch (PO67). Each sketch gives an example, what the direction needs, and what version 1 must keep open. Decisions PO50 and PO67 confirm that they are out of scope for version 1. Nothing in this section is part of the milestones of section [11](11-delivery-plan.md#11-delivery-plan). One smaller open point has no subsection: templates at the level of the awaiter, which would intercept `GetAwaiter`, `IsCompleted` and `GetResult` through a template-only type such as `AnyAwaiter`, next to the `AnyAwaitable` of section [10.6.9](10b-oss-linker-and-templates.md#1069-anyawaitable).

### 16.1 Composition of several interceptors on one site

Version 1 allows at most one interceptor per site (R7, section [9.5.8](09-premium-engine.md#958-conflict-detection-r7-b7)). The rule is "not composed yet", not a permanent rule. Composition would use the strategy of several overrides of one method: each interceptor is a layer of a synthetic interception slot of the site, ordered by aspect order, and the `meta.Proceed()` of each layer binds to the previous layer.

```csharp
// Source: two aspects intercept File.ReadAllText at the same call site. Logging is ordered after Caching.
var text = File.ReadAllText( path );

// Possible rewrite: the site calls the outermost layer.
var text = ReadAllText_Logging( path );

private static string ReadAllText_Logging( string path )
{
    Console.WriteLine( $"Reading {path}." );

    return ReadAllText_Caching( path );          // meta.Proceed() of the Logging layer: the previous layer.
}

private static string ReadAllText_Caching( string path )
{
    return Cache.GetOrAdd( path, File.ReadAllText );   // meta.Proceed() of the innermost layer: the target.
}
```

What it needs:

- A synthetic slot per site, whose layers the linker resolves with its existing aspect-reference resolution in `Previous` order, as it resolves stacked overrides (ENG26 `Linking\AspectReferenceResolver.cs:88-96, 564-569`).
- A group key that includes the chain of layers below each layer, because the body of a layer depends on the next inner layer.
- A conflict rule that becomes an ordering rule: LAMA1010 remains only for layers whose order is not defined.

What version 1 keeps open:

- The proceed binding is declarative (`ProceedBinding`, section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)), so a later binding kind can target the previous layer instead of the intercepted method.
- R7 is phrased as a rule of version 1 (section [1.6](01-summary.md#16-main-decisions), item 7; interpretation I1).
- Existing-method interceptors call the target directly. They are documented as terminal: in a composition, they can only be the innermost layer.
- `ExtensionContributionOrigin` captures the ordering data of the registering layer: the aspect layer and the pipeline step index (section [10.2.3](10a-oss-bridge-hook-factory.md#1023-contribution-origin)). The factory already derives `AdviceOrderingIndices` from them (section [10.4.7](10a-oss-bridge-hook-factory.md#1047-attribution-and-ordering)).
- The group key is an internal record (section [8.2](08-deduplication-and-naming.md#82-the-key)), so a chain component is an addition.
- Accessor sites already count interceptors per accessor use (RC50), so composition applies per use as well: the layers of the getter of a compound site are independent of the layers of its setter, and the single evaluation of the receiver stays a property of the site rewrite, not of the layers.

### 16.2 Delegate-based handler interceptors

A handler interceptor is ordinary run-time code, not a template. It receives the intercepted call as a proceed delegate and the packed arguments, like `MethodInterceptionAspect.OnInvoke( MethodInterceptionArgs args )` with `args.Proceed()` in PostSharp (OSS27ROOT `Metalama.Migration\src\Metalama.Migration\Aspects\MethodInterceptionAspect.cs:27, 47`; `Aspects\MethodInterceptionArgs.cs:42`).

```csharp
// User code: a run-time handler, compiled and debugged as ordinary code.
public static class RetryHandler
{
    public static TResult Intercept<TReceiver, TArgs, TResult>( in MethodInvocation<TReceiver, TArgs, TResult> invocation )
    {
        for ( var attempt = 1; ; attempt++ )
        {
            try
            {
                return invocation.Proceed();
            }
            catch ( IOException ) when ( attempt < 3 )
            {
            }
        }
    }
}

// Provider: a new factory of InterceptorResult.
return InterceptorResult.Handler( retryHandlerInterceptMethod );

// Call site after the rewrite.
var text = RetryHandler.Intercept(
    new MethodInvocation<FileReader, (string, Encoding), string>(
        reader,
        ( path, encoding ),
        static ( r, args ) => r.Read( args.Item1, args.Item2 ),
        MetalamaInterceptors.Metadata_FileReader_Read ) );

// Run-time library.
public readonly struct MethodInvocation<TReceiver, TArgs, TResult>
{
    public TReceiver Receiver { get; }
    public TArgs Arguments { get; }                        // A ValueTuple; an untyped form could expose object[] for PostSharp compatibility.
    public MethodInvocationMetadata Metadata { get; }      // Cached per site: the intercepted MethodInfo or a lightweight descriptor, and site information.
    public TResult Proceed();                              // Invokes the accessor with Receiver and Arguments.
}
```

A method-reference site uses the wrapper form of section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites): `list.Select( reader.Read )` becomes a delegate that evaluates `reader` once and calls the handler with a new `MethodInvocation` for each invocation.

Why it is useful:

- It is the migration target of `MethodInterceptionAspect`, which Metalama.Migration marks as not supported for external methods today.
- Users write no template and need no knowledge of compile-time code.
- The handler is ordinary code: it can be debugged, tested and shared as a library.

What it needs:

- A run-time library, for example a redistributable assembly `Metalama.Extensions.Interceptors.Runtime`, that defines `MethodInvocation` and `MethodInvocationMetadata`. Its licensing and redistribution terms are an open question (compare the Redist package of PO3).
- The accessor of section [16.4](#164-accessor-mode-for-inaccessible-targets), which the handler receives inside `MethodInvocation`.
- The limitations of the accessor: `ref`, `out` and `in` parameters, ref-struct arguments and ref returns, unless a generated struct replaces the `ValueTuple`.
- Caching of the metadata per site, for example in static readonly fields of the generated static class.

What version 1 keeps open:

- `InterceptorResult` is a sealed class with a private constructor and static factories (section [5.6.1](05b-api-providers-contexts-results.md#561-interceptorresult)). `Handler` is a new factory and a new member of `InterceptorResultKind`, which is an addition.
- `ProceedBinding` is declarative, so a binding that invokes an accessor parameter is an addition (section [16.4](#164-accessor-mode-for-inaccessible-targets)).
- The group key contains the implementation identity (`ImplementationKey`, section [8.2](08-deduplication-and-naming.md#82-the-key)). A handler adds an implementation kind to it.
- The sources `Proceed` and `PackedArguments` of section [16.8](#168-the-proceed-and-packedarguments-sources-interception-without-a-template) give an ordinary method the same data without a `MethodInvocation` value. The two directions overlap, and the product owner defers the choice to the end of M2 (PO67).
- A handler must also receive method-reference sites. The wrapper of section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites) already evaluates the receiver once and creates a delegate that calls another method, which is the shape that a handler needs.
- A handler must also receive accessor sites: a getter handler returns the value, a setter handler receives the value and returns the assigned value, and an add or remove handler receives the handler. The rewrite of compound sites calls the interceptor of each accessor use with the receiver temporary (section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites)), so a handler interceptor is one more kind of callee and needs no new site rewrite.

### 16.3 Other method uses without invocation syntax

C# calls some methods without invocation syntax: `Add` in collection initializers and collection expressions, `GetEnumerator`, `MoveNext` and `Dispose` in `foreach`, `Deconstruct` in deconstruction, and the methods that query clauses call (rules table row 65). They would become new values of `MethodUseKind` (section [5.5.2](05b-api-providers-contexts-results.md#552-methodinterceptioncontext-and-invocationargument)).

```csharp
// Source: the collection initializer calls OrderList.Add twice.
var orders = new OrderList { first, second };

// Possible rewrite with MethodUseKind.CollectionInitializerAdd: the initializer is expanded, because no syntax of an
// initializer can call another method.
var orders = new OrderList();
Add_Interceptor( orders, first );
Add_Interceptor( orders, second );
```

What it needs: a rewrite shape per kind, because most of these uses have no syntax that can name another method, and the analysis of the form that Roslyn exposes for each kind: `GetForEachStatementInfo` for `foreach` (RC `CSharpExtensions.cs:891-904`), an implicit `IInvocationOperation` for each element of a collection initializer (RC `..\Core\Portable\Generated\Operations.Generated.cs:1777-1778`), and `IDeconstructionAssignmentOperation` for deconstruction (same file, line 2248).

What version 1 keeps open: `MethodUseKind` is not a flags enumeration and its documentation states that later versions add values (section [5.3.11](05a-api-registration.md#5311-kinds-of-method-use)). A registration has no kind filter, so a new kind reaches existing providers, which is consistent with the equivalence argument of RC44. The accessor sites that version 1 declines because they have no syntax that can call a method, the setters of object and `with` initializers and the targets of a deconstruction (section [6.2.11](06a-call-site-model.md#6211-accessor-sites)), are the accessor form of this direction and would use the same expansion into statements.

### 16.4 Accessor mode for inaccessible targets

Section [6.5.5](06b-signatures-and-validation.md#655-access-to-private-and-protected-targets) describes the sketch: an interceptor whose placement has no access to a private or protected target receives a static lambda, generated in the calling type, that calls the target with the arguments packed in a `ValueTuple`, and `meta.Proceed()` invokes that lambda. The sketch is not implemented in version 1.

What version 1 keeps open: `ProceedBinding` is declarative, so a later kind can invoke a delegate parameter instead of the target, and the call-site rewrite already appends extra arguments (`CallSiteExtraArgument`, section [10.4.5](10a-oss-bridge-hook-factory.md#1045-call-site-redirections)), which can carry the tuple and the accessor. The handler interceptors of section [16.2](#162-delegate-based-handler-interceptors) use the same accessor.

### 16.5 Indexers

Not planned for version 1. The names of an `InterceptAccessors` registration never match an indexer in version 1. Two API options exist:

```csharp
// Option A: InterceptAccessors with the code-model name of indexers, "this[]" (ENG27 CodeModel\Introductions\Builders\IndexerBuilder.cs:33).
amender.InterceptAccessors( typeof(Matrix), "this[]", MethodKind.PropertySet, new MatrixWriteProvider() );

// Option B: a dedicated verb without names.
amender.InterceptIndexers( typeof(Matrix), MethodKind.PropertySet, new MatrixWriteProvider() );
```

Signatures: `T I( TR receiver, TIndex index )` for a getter and `T I( TR receiver, TIndex index, T value )` for a setter, with one parameter per index parameter. At a compound site, the index arguments are evaluated once, with the receiver, through the temporaries of section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites). The walker records element accesses without an identifier (ENG27 `ReferenceGraph\ReferenceIndexWalker.cs:730-736`), so an indexer registration has no name to filter the index before binding; its binding cost is a decision to take.

### 16.6 Operators

Not planned for version 1, as R2 states. API sketch:

```csharp
amender.InterceptOperators( typeof(Money), [OperatorKind.Addition, OperatorKind.Subtraction], new MoneyOperatorProvider() );
amender.InterceptOperators( t => t.ContainingNamespace.FullName == "Contoso.Units", [OperatorKind.Multiplication], new UnitProvider() );
```

- `IOperatorInterceptorProvider.GetInterceptor( OperatorInterceptionContext context )` returns an `InterceptorResult`.
- `OperatorInterceptionContext` exposes `OperatorKind`, `OperatorMethod` (or `null` for an intrinsic operator such as `int + int`), `OperandTypes`, `ResultType`, `IsChecked`, `IsLifted`, a use kind (expression, compound assignment, increment, implicit conversion, explicit conversion), and the operands as inspection-only `IExpression` values.
- The rewrite is `a + b` into `I( a, b )`, which keeps the left-to-right evaluation. `meta.Proceed()` emits the operator itself, which keeps the lifting and the checked context; the checked context is part of the group key.
- The operator token is the name filter before binding, except for implicit user-defined conversions, which have no token; their cost is a decision to take.
- Limitations and sites never presented: the short-circuit operators `&&` and `||` with user-defined `true` and `false` operators, because the rewrite would evaluate both operands; constant expressions, which the compiler folds; expression trees, which are data; string concatenation, which the compiler lowers to `string.Concat`.

### 16.7 Target-side registration

Not planned for version 1 (PO5). A registration of version 1 starts from the calling side: it names a scope, and a member aspect can register only a scope that is contained in the closest type of its target (RC19). A target-side registration is made on the target, by an aspect applied to the target, and its scope is implicitly the whole project. Earlier versions of this document called it inbound registration (RC57).

```csharp
// An aspect applied to a method intercepts every use of that method in the current project.
public override void BuildAspect( IAspectBuilder<IMethod> builder )
    => builder.InterceptUses( new AuditProvider() );

// The accessor equivalent, on an aspect applied to a property.
public override void BuildAspect( IAspectBuilder<IProperty> builder )
    => builder.InterceptUses( MethodKind.PropertySet, new AuditProvider() );
```

The direction needs a containment rule that differs from RC19, because the scope is the project and not a declaration inside the closest type of the aspect target. Uses in downstream projects would also need the manifest-based transitive route of section [9.9.4](09-premium-engine.md#994-deferred-route-manifest-based-transitive-interceptors). Version 1 keeps it open because the adviser surface rejects a compilation scope from a member aspect instead of accepting it with another meaning, because a registration stores its declaring type and names as data that can be derived from the target, and because the provider interfaces and the contexts do not depend on how a registration selects its sites. The caller-side argument providers of section [16.10](#1610-caller-side-argument-providers) extend target-side registration from members to parameters.

### 16.8 The Proceed and PackedArguments sources: interception without a template

Not planned for version 1 (PO67). `InterceptorArgument` reserves two sources (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)). `Proceed` gives a delegate that calls the intercepted target with the values of the site, and `PackedArguments` gives the arguments of the site as one value, for example an `object?[]` or a `ValueTuple`. With them, an ordinary user method intercepts a call without a template and without code generation by the user:

```csharp
// User code: an ordinary method, compiled and debugged as ordinary code.
public static class Telemetry
{
    public static T Measure<T>( Func<T> proceed, [CallerMemberName] string operation = "" )
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return proceed();
        }
        finally
        {
            Console.WriteLine( $"{operation}: {stopwatch.ElapsedMilliseconds} ms" );
        }
    }
}

// Provider.
return InterceptorResult.ExistingMethod( measureMethod, m => m.Parameters["proceed"].Bind( InterceptorArgument.Proceed ) );

// Site in OrderService.Process.
gross = order.Total( true );

// Rewritten site: the receiver and the arguments are evaluated once, before the delegate is created.
{
    var r0 = order;
    var a0 = true;
    gross = Telemetry.Measure( () => r0.Total( a0 ), "Process" );
}
```

`T` is inferred from the lambda. `operation` binds canonically to `CallerInfo( MemberName )`. The site becomes a block with temporaries, because the lambda must not evaluate the receiver and the arguments again at each call of `proceed`.

Costs:

- A closure and a delegate are allocated for each call.
- The receiver and the arguments are evaluated eagerly into temporaries, before the helper runs.
- `Func` and `Action` have at most 16 parameters, which limits a proceed delegate that receives the packed arguments as parameters.
- `ref`, `out` and `in` parameters and ref-struct values cannot be captured by a lambda, so such sites need another shape or a limitation.
- An async target needs a separate contract, for example `Func<Task<T>>` or `Func<ValueTask<T>>`, and a helper for each.
- Stack traces contain the frames of the helper and of the lambda.

The direction overlaps with the handler interceptors of section [16.2](#162-delegate-based-handler-interceptors), whose `MethodInvocation` carries the receiver, the packed arguments and `Proceed()` in one value. The product owner defers the choice between the two to the end of M2. This direction is the strongest candidate for version 1 if the migration from PostSharp becomes the priority, because the user writes only run-time code.

What version 1 keeps open: `InterceptorArgument` is a sealed class with factories and a `Kind` that can receive members; the argument plan of section [6.4.10](06b-signatures-and-validation.md#6410-rewrite-plan) already evaluates the values of the site into temporaries; the lambda wrapper of method-reference sites already writes a lambda that calls the interceptor (section [5.6.10](05b-api-providers-contexts-results.md#5610-the-callers-instance-and-method-reference-sites)).

### 16.9 Pull strategies at the registration and at the site

Not planned for version 1 (PO64, PO67). Version 1 pulls values only through a `PullAction` that the provider or the `configure` function computes for each site. Two extensions reuse the existing pull-strategy concept:

- A registration-level overload that takes an `IPullStrategy`, so that a dependency-injection framework can reuse its strategy for interceptors. The existing method `IPullStrategy.GetPullAction( IParameter pulledParameter, IHasParameters targetMember )` (FW27 `Advising\IPullStrategy.cs:50`) fits: `pulledParameter` is the added parameter of the interceptor, and `targetMember` is the origin of the site. `IPullStrategy` is `[Durable]` and `ICompileTimeSerializable` (lines 39-41), so a registration can store it.
- A site-level strategy, beside `IPullStrategy`, which receives the whole site:

```csharp
[CompileTime]
[Durable]
public interface ISitePullStrategy
{
    PullAction GetPullAction( IParameter pulledParameter, InterceptionContext site );
}
```

What version 1 keeps open: `InterceptorArgument.Pull` takes a `PullAction`, which both strategies return, and the options of a registration are records that can receive a property without a new overload.

### 16.10 Caller-side argument providers

Not planned for version 1 (PO67, RC68). A caller-side argument provider is a parameter-level aspect, applied to an optional parameter, that provides the value of the parameter at the caller side, at each site that omits the argument:

```csharp
public void Save( Order order, [Now] DateTime timestamp = default, [PullCancellationToken] CancellationToken cancellationToken = default );

// In the aspects, on IAspectBuilder<IParameter>:
builder.ProvideOmittedArgument( PullAction.UseExpression( ExpressionFactory.Parse( "global::System.DateTime.Now" ) ) );
builder.ProvideOmittedArgument( new CancellationTokenPullStrategy() );   // An IPullStrategy.

// A site that omits both arguments, in a method with a parameter ct:
this.Save( order );
// becomes:
this.Save( order, timestamp: global::System.DateTime.Now, cancellationToken: ct );
```

The existing `IPullStrategy.GetPullAction( IParameter pulledParameter, IHasParameters targetMember )` fits unchanged: `pulledParameter` is the parameter of the aspect, and `targetMember` is the origin of the site. `PullAction.None` keeps the declared default. Only sites that omit the argument are rewritten: the site model reports them with `InvocationArgumentKind.DefaultValue`. The rewrite keeps the original target and appends a named argument.

What the direction reuses from version 1:

| Version 1 element | Use by caller-side argument providers |
|---|---|
| Target-side registration (section [16.7](#167-target-side-registration)) | Extended from members to parameters: the aspect on the parameter registers for the uses of its method. |
| The shared index and the name filter (section [10.7.5](10c-oss-reference-graph-design-time.md#1075-shared-index-of-source-references)) | The name of the method filters the index before binding. |
| The site model (section [6.2](06a-call-site-model.md#62-the-call-site-model)) | `InvocationArgumentKind.DefaultValue` identifies the sites that omit the argument. |
| `PullAction` and `InterceptorArgument.Pull` (section [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values)) | The same vocabulary and the same resolution to the origin, with the guard. |
| The binding rules (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)) | The type rules and the evaluation after the arguments of the site. |
| Materialized defaults and the packing of `params` (section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params), rules c and j) | An appended named argument after an expanded `params` argument. |
| The injection-time rewrite (section [10.5](10b-oss-linker-and-templates.md#105-linker-changes)) | The same rewriter and the same completeness check. |
| Rule E16 (section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9)) | The speculative binding of the rewritten call. |
| Phase B (section [9.7.4](09-premium-engine.md#974-phase-b-analyzer)) | The diagnostics of the providers in the IDE. |

Doors that version 1 must keep open:

| Door | Requirement on version 1 |
|---|---|
| A redirection that keeps the original target | The open-source factory accepts `CallSiteRedirectionTarget.Existing( originalMethod )` with extra named arguments (section [10.4.5](10a-oss-bridge-hook-factory.md#1045-call-site-redirections)). |
| Composition with interceptors | Providers run first, as a normalization of the site, and the interceptor then sees the argument as `Computed`. The site model keeps `InvocationArgumentKind.Computed`. Providers are not interceptors, so R7 does not count them. |
| Target-side registration on parameters | The registration model of section [16.7](#167-target-side-registration) must not assume that the target of a target-side registration is a member. |
| Library-defined attributes that apply in consumers | They need the manifest-based transitive route of section [9.9.4](09-premium-engine.md#994-deferred-route-manifest-based-transitive-interceptors). The aspect instance is serializable, so it can rebuild the registration in the consumer. |
| Object creation | A future kind of target, so that `new Order( ... )` sites can be completed as well. |
| Indirect calls | A call through a delegate or through reflection receives the declared default, because no site exists. The documentation states this. |
| `IntroduceParameterAndPull` | It stays out, as for interceptors. |

### 16.11 Functions and locals that enclose a site

Not planned for version 1 (PO65, PO67). In version 1, a pulled parameter always belongs to the origin member (section [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values)). Three options would give providers access to the parameters and locals of the functions that enclose a site.

Option B: the enclosing functions. `InterceptionContext.EnclosingFunctions` lists the functions from the innermost one to the origin member:

```csharp
[CompileTime]
public sealed class InterceptionEnclosingFunction
{
    public InterceptionEnclosingFunctionKind Kind { get; }            // Lambda, AnonymousMethod, LocalFunction, Member.
    public bool IsStatic { get; }
    public bool IsAsync { get; }
    public IMember? Member { get; }                                  // The origin member, for the kind Member.
    public IReadOnlyList<InterceptionVariable> Parameters { get; }
}

[CompileTime]
public sealed class InterceptionVariable
{
    public string Name { get; }
    public IType Type { get; }
    public RefKind RefKind { get; }
    public IParameter? Parameter { get; }                            // The code-model parameter, for a parameter of the origin member.
    public bool IsAccessibleAtSite { get; }
    public IExpression ToExpression();                               // Emits the name; throws when IsAccessibleAtSite is false.
}
```

`IsAccessibleAtSite` applies the rules of the guard: a variable is not accessible when a nearer function declares a parameter or a local with the same name, when a static function lies between the variable and the site, or when a `ref`, `out` or `in` parameter would be captured. The range variables of query expressions are excluded, because a query clause is lowered to a lambda whose parameters are not visible in source. Such a list would let a provider pull the `CancellationToken` of the handler lambda of an ASP.NET Core minimal API, which version 1 cannot reach.

Option C: the locals. The same model, extended to the locals that are in scope at the site. A local is accessible only when it is definitely assigned at the site, which needs the data-flow analysis of Roslyn (`SemanticModel.AnalyzeDataFlow`) for each candidate.

Alias hoisting. The engine would insert `var __ct = ct;` at the start of the body of the origin, and pull `__ct` instead of `ct`. The alias is never shadowed, and a lambda can capture it where it could not capture a `ref` parameter. The value is a snapshot at the start of the body, so an assignment to `ct` later in the body is not seen. Hoisting would turn some limitations of version 1 into rewrites without changing any site that version 1 already rewrites.

The site-level pull strategy of section [16.9](#169-pull-strategies-at-the-registration-and-at-the-site) would receive these models through its `InterceptionContext`.

What version 1 keeps open: `InterceptionContext` is an abstract class that can receive members; `IsInNestedFunction` already reports that a site is inside a nested function; the internal `CallSiteEnclosingContext.Functions` (section [6.2.8](06a-call-site-model.md#628-enclosing-function-and-body-context)) already holds the enclosing functions, innermost first.
