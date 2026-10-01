# Await interception

> Part of the [call-site interceptors design](README.md). Previous: [06b-signatures-and-validation.md](06b-signatures-and-validation.md) | Next: [08-deduplication-and-naming.md](08-deduplication-and-naming.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

## 7. Await interception

This section designs the interception of the `await` operator (R2, R11, baseline B8). It keeps interpretations I1 and I2, and uses I6 (meaning of R11), I7 (two modes) and I8 (scope of awaits in lambdas).

Additional evidence for this section comes from the .NET runtime (prefix RT) and from these EXISTING facts:

| Fact | Evidence |
|---|---|
| `AwaitExpressionInfo` exposes `GetAwaiterMethod`, `IsCompletedProperty`, `GetResultMethod`, `IsDynamic` and `RuntimeAwaitMethod`. Under runtime async, the first three are null for task types. | RC `Compilation\AwaitExpressionInfo.cs:15-39` |
| `RuntimeAwaitMethod` was added in Roslyn 5.0.0. The 2027.0 line requires the Roslyn API 5.0.0 or later. This design does not use the member for any decision. | RC `PublicAPI.Shipped.txt:94`; OSS27ROOT `Directory.Packages.props:21` |
| Runtime async is enabled per method, only when the runtime supports it and the method returns `Task`, `Task<T>`, `ValueTask` or `ValueTask<T>`. The binder first tries `AsyncHelpers.Await` and then the awaiter pattern. | RC `Compilation\CSharpCompilation.cs:352-397`; `Binder\Binder_Await.cs:311-330` |
| `AsyncHelpers.Await` has overloads for the task family and for the configured task awaitables, so configured task awaitables also have a null `GetAwaiterMethod` under runtime async. | RT `System\Runtime\CompilerServices\AsyncHelpers.cs:128-377` |
| A non-async template is interpreted as async when the target is async, or when `UseAsyncTemplateForAnyAwaitable` is set and the return type is awaitable with a method builder. | ENG27 `Advising\AdviceFactory.cs:262` |
| Template binding accepts `Task<dynamic>` for any awaitable target and `Task` for any awaitable with a `void` result. | ENG27 `Advising\TemplateBindingHelper.cs:742-837` |
| The engine already assumes that `ValueTask` exists when it overrides an `async void` method, and `ReflectionMapper.GetTypeSymbol(typeof(ValueTask))` throws when the type is missing. | ENG27 `AdviceImpl\Override\OverrideMethodBaseTransformation.cs:70-76`; `CodeModel\Helpers\ReflectionMapper.cs:79, 88` |
| Templates reject `goto` and labels. | ENG27 `Templating\TemplateAnnotator.cs:2640-2650` |

### 7.1 Which await sites are targets

A target is an `AwaitExpressionSyntax` of the source compilation (R6) that satisfies all of the following:

1. It binds without error: `GetTypeInfo(node.Expression).Type` and `GetTypeInfo(node).Type` are not error types.
2. `GetAwaitExpressionInfo(node).IsDynamic` is false.
3. It lies in the scope of at least one await registration. An await registration has no filter by awaitable type or by kind, so every await of the scope that satisfies conditions 1 and 2 is presented, including the awaits of custom awaitables (section [5.3.4](05a-api-registration.md#534-await-registrations), RC58).

Awaits whose awaited type or result type is ref-like or a pointer are presented with `NonInterceptableReason.RefLikeAwait` or `PointerType`.

The rewrite replaces the await node with another await node at the same position. The C# rules that restrict where `await` may appear are positional: no `await` in `lock` statements, `unsafe` contexts, exception filters, or query clauses translated into lambdas. The rewritten node satisfies them as the original did. The only new constraints come from calling the interceptor method, and they concern `this` and captured state (section [7.10](#710-existing-methods-and-placements-for-awaits)).

| Construct | Decision | Reason |
|---|---|---|
| `await e` in an async method, accessor, local function or lambda | Target | Ordinary await expression. |
| `await` in `catch` and `finally` blocks | Target | Allowed since C# 6; the rewritten await is at the same position. |
| `await` in a query expression | Target | The compiler accepts `await` only in query positions that are not translated into lambdas. |
| `await` in an expression-tree lambda | Not applicable | An async lambda cannot be converted to an expression tree. |
| `await` in top-level statements | Target, with static interceptors in type placements only (`InType` of a user type or `GeneratedStaticClass`) | The injection rewriter rewrites global statements (section [10.5.3](10b-oss-linker-and-templates.md#1053-injection-step-and-rewriter)). The implicit `Program` is not an admissible placement, and local functions are not supported there in version 1. |
| `await` in an `async void` method | Target | The await semantics do not depend on the method builder of the calling function. The interceptor is never `async void`. |
| `await` in an async iterator | Target | The awaits in the body are ordinary awaits. |
| `await foreach`, `await using` | Excluded (decision PO43) | There is no `AwaitExpressionSyntax`. The awaits of `MoveNextAsync` and `DisposeAsync` exist only after the compiler lowers the statement. Intercepting them requires replacing the enumerator or the disposable with a wrapper, which changes the pattern-based binding of the statement. Users can intercept the explicit `ConfigureAwait` and `WithCancellation` calls as invocations. |
| Dynamic await | Silent | Late-bound awaiter lookup (RC7). |
| Operand of a type parameter type | Kind `Task` when a class constraint makes the operand a `Task`; otherwise kind `Custom` | The effective base class decides the awaiter. The signature needs lifting or a local-function placement (section [7.10.3](#7103-generic-lifting-and-anonymous-types)). |
| Operand with an extension `GetAwaiter` | Kind `Custom` | The awaiter is found by extension lookup, which constrains the placement (section [7.10.2](#7102-placements)). |
| Runtime async (`GetAwaiterMethod` null) | Target | Classification uses the operand type (section [7.3](#73-awaitable-kinds-and-resumption-classes)). |
| Awaits in code introduced by aspects or produced by source generators | Not scanned | R6. |
| Awaits inside the body of an existing method that serves as the interceptor for that site | LAMA1018 | Self-interception (RC28). |

### 7.2 Site analysis

`AwaitSiteAnalyzer.Analyze` runs once per candidate await node, at compile time in the hook and at design time in `AnalyzeSemanticModel`.

```csharp
namespace Metalama.Extensions.Interceptors.Engine.Awaits;

internal static class AwaitSiteAnalyzer
{
    /// <summary>
    /// Analyzes one await expression of the source compilation. The result holds Roslyn symbols and must not outlive the
    /// current request.
    /// </summary>
    public static AwaitSiteAnalysis Analyze( SemanticModel semanticModel, AwaitExpressionSyntax node, CancellationToken cancellationToken );
}

internal sealed class AwaitSiteAnalysis
{
    public required AwaitExpressionSyntax Node { get; init; }
    public required ITypeSymbol AwaitableType { get; init; }            // A, with nullable annotations.
    public required ITypeSymbol ResultType { get; init; }               // R, System.Void when the await has no value.
    public required AwaitableKind Kind { get; init; }
    public required AwaitResumption Resumption { get; init; }
    public IInvocationOperation? ConfigureAwaitInvocation { get; init; }
    public AwaitConfigurationFlags? ConstantConfiguration { get; init; }
    public IMethodSymbol? AwaitedMethod { get; init; }                  // Normalized: ReducedFrom for classic extension methods.
    public IMethodSymbol? GetAwaiterMethod { get; init; }               // Null under runtime async for task types.
    public bool UsesExtensionGetAwaiter { get; init; }
    public required IMethodSymbol EnclosingFunction { get; init; }      // Method, accessor, local function, lambda or <Main>$.
    public required ISymbol ReferencingSymbol { get; init; }            // The member used for scope matching (I8).
    public bool HasThis { get; init; }
    public NotACallSiteReason? RefusalReason { get; init; }             // BindingError, Dynamic.
    public NonInterceptableReason? Limitation { get; init; }            // RefLikeAwait, PointerType.
    public ImmutableArray<ISymbol> AccessRequirements { get; init; }    // A, R, awaiter type, GetAwaiter, IsCompleted, GetResult.
}
```

Steps:

1. `info = semanticModel.GetAwaitExpressionInfo(node)`. If `info.IsDynamic`, the refusal reason is `Dynamic`.
2. `A = semanticModel.GetTypeInfo(node.Expression, ct).Type`, `R = semanticModel.GetTypeInfo(node, ct).Type`. If either is null or an `IErrorTypeSymbol`, the refusal reason is `BindingError`. If either is ref-like, the limitation is `RefLikeAwait`. If `R` is a pointer, the limitation is `PointerType`.
3. `await = (IAwaitOperation) semanticModel.GetOperation(node, ct)`, `operand = await.Operation`.
4. `function = (IMethodSymbol) semanticModel.GetEnclosingSymbol(node.SpanStart, ct)`. The referencing symbol is found by walking `ContainingSymbol` while the symbol is a lambda or a local function, with the initializer rule of the reference index. `HasThis` is false when the referencing member is static, when any function between the await and the member is static, or when the containing type is a struct.
5. Configuration detection. If `operand` is an `IInvocationOperation` whose `TargetMethod.Name` is `ConfigureAwait`, whose containing type definition is `Task`, `Task<TResult>`, `ValueTask` or `ValueTask<TResult>`, and which has one argument, then `ConfigureAwaitInvocation` is that operation. If its argument has a constant `bool` or `int` value (the value of `ConfigureAwaitOptions`), `ConstantConfiguration` is set. `true` maps to `ContinueOnCapturedContext`, and `false` maps to `None`.
6. Awaited method: `inner = ConfigureAwaitInvocation?.Instance ?? operand`. If `inner` is an `IInvocationOperation`, `AwaitedMethod` is its `TargetMethod`, normalized with `ReducedFrom`.
7. Classification (section [7.3](#73-awaitable-kinds-and-resumption-classes)).
8. `UsesExtensionGetAwaiter = info.GetAwaiterMethod is { IsExtensionMethod: true }`, and the equivalent test for C# 14 extension members.
9. Access requirements: `A`, `R`, `info.GetAwaiterMethod`, its return type, `info.IsCompletedProperty` and `info.GetResultMethod` when they are not null.

The cost is one `GetAwaitExpressionInfo`, two `GetTypeInfo`, one `GetOperation` and one `GetEnclosingSymbol` per await in scope. The design uses only APIs that exist in every supported Roslyn version.

### 7.3 Awaitable kinds and resumption classes

Classification uses `A`, not `GetAwaiterMethod`, because `GetAwaiterMethod` is null for task types under runtime async.

| `A` (after walking base types; for a type parameter, its class constraint) | Kind (`AwaitableKind`) |
|---|---|
| `Task`, `Task<TResult>` or a derived class, when `GetAwaiterMethod` is null or declared by `Task` or `Task<TResult>` | `Task` |
| `ValueTask`, `ValueTask<TResult>` | `ValueTask` |
| `ConfiguredTaskAwaitable`, `ConfiguredTaskAwaitable<TResult>` | `ConfiguredTask` |
| `ConfiguredValueTaskAwaitable`, `ConfiguredValueTaskAwaitable<TResult>` | `ConfiguredValueTask` |
| `YieldAwaitable` | `Yield` |
| Any other type, and any awaiter found through an extension `GetAwaiter` | `Custom` |

The resumption class states where the calling function resumes after the original await:

| Kind and operand shape | `AwaitResumption` |
|---|---|
| `Task`, `ValueTask` | `CapturedContext`: the awaiter resumes on the `SynchronizationContext` or `TaskScheduler` current at the await. |
| `Yield` | `CapturedContext`: `YieldAwaitable` posts to the current context or scheduler (RT `System\Runtime\CompilerServices\YieldAwaitable.cs:73-113`). |
| `ConfiguredTask` or `ConfiguredValueTask`, direct `ConfigureAwait` call with a constant that includes `ContinueOnCapturedContext` | `CapturedContext` |
| Same, constant without `ContinueOnCapturedContext` (`false`, `None`, `ForceYielding`, `SuppressThrowing`) | `AnyContext` |
| Same, non-constant argument, or an operand that is not a direct `ConfigureAwait` call (for example a local of type `ConfiguredTaskAwaitable`) | `Unknown` |
| `Custom` | `Unknown`: the awaiter can resume anywhere. Examples are `JoinableTaskFactory.SwitchToMainThreadAsync()` and `await TaskScheduler.Default` extension awaiters. |

### 7.4 Runtime rules and evaluation of the rewrite shapes

The rewrite introduces a second awaitable between the calling function `M` and the original awaitable `a`. These rules, verified in the runtime source, decide where `M` resumes:

- R-a. An await that captures the context registers a continuation that runs inline when the completing thread has the captured `SynchronizationContext`, and is posted to that context otherwise (RT `System\Threading\Tasks\TaskContinuation.cs:392-410`).
- R-b. An await with `ConfigureAwait(false)` registers the state machine box directly (RT `System\Threading\Tasks\Task.cs:2743-2747`). When the task completes, the box runs inline only if the completing thread has no derived `SynchronizationContext` and uses the default `TaskScheduler`. Otherwise it is queued to the thread pool (RT `Task.cs:3623-3647`; `TaskContinuation.cs:618-632, 774-785`). Runtime async applies the same rule (RT `src\coreclr\System.Private.CoreLib\src\System\Runtime\CompilerServices\AsyncHelpers.CoreCLR.cs:1192-1210`).
- R-c. The synchronous part of an async method runs on the caller's thread, and its changes to `ExecutionContext` and `SynchronizationContext` are undone when it returns (RT `System\Runtime\CompilerServices\AsyncMethodBuilderCore.cs:21-55`).

Notation: original `var x = await e;` in `M`, `e : A`, result `R`, UI synchronization context `S` current at the await, interceptor `I`.

Shape (a), the baseline B8: `await I(e).ConfigureAwait(false)` with `static async ValueTask<R> I(A a) { before; var r = await a; after; return r; }`.

| Original awaitable | Original resumption of `M` | Resumption of `M` with shape (a) | Equivalent |
|---|---|---|---|
| `Task<R>` capturing `S`, completes later | Posted to `S` | `I` resumes on `S` (R-a) and completes its task on the UI thread. `M`'s continuation does not capture (R-b), and the completing thread has `S`, so it is queued to the thread pool. | No |
| `Task<R>` in a task that runs on a custom scheduler `TS` | Runs on `TS` | `I` resumes on `TS`, completes there, and `M` is queued to the thread pool. | No |
| `ConfigureAwait(false)` | Inline on the completing thread, or pool | `I` resumes inline or on the pool, `M` inline | Yes |
| `Task.Yield()` with `S` | Posted to `S` | `I` resumes on `S`, `M` queued to the pool | No |
| Already completed task | Continues synchronously | Continues synchronously | Yes |

Shape (a) breaks the most common case: an unconfigured `await task` in UI code, in legacy ASP.NET, or under any `SynchronizationContext` or custom `TaskScheduler`. Code after the await that touches UI objects then throws a cross-thread exception.

Shape (a'): `await I(e)` without `ConfigureAwait`, with the same `I`.

| Original awaitable | Resumption of `M` with shape (a') | Equivalent |
|---|---|---|
| `Task<R>` with `S` | `M` captures `S` when it suspends. This is the same `S`, because `I` ran synchronously on `M`'s thread (R-c). `I` resumes on `S` and completes there. `M`'s continuation runs inline because the current context is `S` (R-a). | Yes, without an extra post |
| `Task<R>` with `TS` | `I` resumes on `TS`; `M` captured `TS` and runs on `TS`. | Yes |
| `Task.Yield()` | Same as the `Task` case. | Yes |
| `ConfigureAwait(false)` with `S` at the await | `I` resumes on the pool. `M` captured `S` and is posted to `S`. The deadlock protection of library code that calls `.Result` on the UI thread is lost. | No |
| Custom awaiter that switches to another context `T` | `M` resumes on `S`, not on `T`. | No |

Shape (b): `await I(e)` where `I` returns `A` and is `async`. Possible only when `A` has a method builder. For the task family it is shape (a'). It cannot express `ConfiguredTaskAwaitable`, `YieldAwaitable` or custom awaiters (ENG26 `CodeModel\Helpers\AsyncHelper.cs:154-160`).

Shapes (c) and (d): a wrapper awaitable, or awaiter-level hooks, that delegate `GetAwaiter`, `IsCompleted`, `OnCompleted` and `GetResult` to the original awaiter. `M` resumes exactly where the original awaiter decides. C# has no constraint for the awaiter pattern, so a custom awaiter needs a type per awaitable type, and a template with `try` or `finally` around the await cannot be expressed.

| Shape | Context fidelity | Template expressiveness | Extra allocation per suspension | Types needed |
|---|---|---|---|---|
| (a) baseline | Wrong for capturing awaits | Full async template | One state machine box | None |
| (a') | Wrong for `ConfigureAwait(false)` and switching awaiters | Full | One box | None |
| (b) | As (a'), only for awaitables with a builder | Full | One box | None |
| (c), (d) | Exact | Callbacks only | None, or a closure | A wrapper per awaiter family or per awaitable type |

### 7.5 The adaptive rewrite (challenge to B8, adopted)

`T_I` is the return type of the interceptor method. The task family is `Task`, `Task<R>`, `ValueTask` and `ValueTask<R>`.

Rule:

1. When `T_I` equals `A`, or `T_I` is not in the task family, the rewrite is `await I(e)`. The call site awaits what the interceptor returns, so the interceptor decides the resumption context. This covers mode `Awaitable` and existing methods that return non-task awaitables.
2. When `T_I` is in the task family and differs from `A`:
   - `CapturedContext`: `await I(e)`.
   - `AnyContext`: `await I(e).ConfigureAwait(false)`.
   - `Unknown`: the result must set `AwaitRewriteOptions.Resumption`. `CapturedContext` gives `await I(e)`, and `AnyContext` gives `await I(e).ConfigureAwait(false)`. Without a policy, the engine leaves the await unchanged and reports the warning LAMA1020. An implementation that returns `InterceptorResult.Skip` when `context.Resumption` is `Unknown` reports nothing (sample 4).

`Unknown` occurs for the kind `Custom`, and also for the kinds `ConfiguredTask` and `ConfiguredValueTask` when the `ConfigureAwait` argument is not a constant, as in `await t.ConfigureAwait( continueOnCapturedContext )`, and when the operand is a stored configured awaitable (section [7.3](#73-awaitable-kinds-and-resumption-classes)). An await registration has no filter by kind, so all these awaits reach the provider (RC58). Awaits with a non-constant argument are common in library code. LAMA1020 is therefore a warning and not an error, so that a provider that does not skip them does not break the build. The recommended first statement of an await provider returns `InterceptorResult.Skip` when `context.Resumption` is `Unknown` (section [5.3.4](05a-api-registration.md#534-await-registrations)).

Justification per class, with shape (a') and rules R-a to R-c:

- `CapturedContext`. `I` runs synchronously on `M`'s thread and its context changes are undone (R-c). The outer await captures the same context as the original. `M` resumes on that context, inline when the completing thread already has it (R-a). This holds when the template adds awaits before or after `meta.Proceed()`.
- `AnyContext`. `I` resumes where the original configured awaiter resumes, and completes there. That thread is a valid inlining location, so `M` runs inline (R-b). One difference remains: when the original task was already completed but the template suspends after `meta.Proceed()` on a thread with a context, `M` resumes on the thread pool instead of continuing synchronously. Code after `ConfigureAwait(false)` already accepts any thread, so this stays within the contract of the original code.
- `Unknown`. No suffix reproduces an arbitrary awaiter. The engine therefore requires an explicit decision, or mode `Awaitable`.

The suffix is independent of the result cast of section [7.6.1](#761-result-compatibility). When both apply, the rewrite is `(R)( await I( e ).ConfigureAwait( false ) )`. The rules of this section use the return type `T_I` of the interceptor, whatever its result type.

### 7.6 Modes

In mode `Await` (the default), the interceptor method is `async` and returns a task-family type chosen by section [7.7](#77-task-type-of-an-async-interceptor-and-target-frameworks). The original awaitable is awaited inside the interceptor: by the proceed expression of a non-async template, or by an `async` template itself (section [7.9.1](#791-accepted-template-shapes)).

In mode `Awaitable`, the interceptor method is not `async` and returns `T_I`, which is the type passed to `InterceptorResult.WithAwaitRewriteOptions`, or `A`. The call site is `await I(e)` (rule 1). The engine checks that awaiting `T_I` binds and that its result type converts implicitly to `R` (section [7.6.1](#761-result-compatibility), LAMA1021). This mode makes possible:

- changing the resumption on purpose (sample 9);
- exact suspension hooks with a user-written wrapper awaitable (sample 10b, through an existing method);
- replacing the operation, for example `a.WaitAsync(timeout)`. When `A` is `Task<R>`, `T_I` equals `A`, and rule 1 gives `await I(e)`. This is also what rule 2 gives, because every task-family `A` is `CapturedContext`. When `A` is `ValueTask<R>`, the template writes `a.AsTask().WaitAsync(timeout)`, `T_I` differs from `A`, and rule 2 applies. Configured awaitables have no `WaitAsync` method.

This is what R11 means concretely (interpretation I6): any awaitable can be awaited (all kinds of section [7.3](#73-awaitable-kinds-and-resumption-classes)), and any awaitable can be returned by the interceptor (mode `Awaitable` or an existing method). For `async` interceptors, the task type is in the task family: the declared task type of an `async` template, or the type that the engine chooses for a non-async template (RC9, RC60).

Custom task-like types (types with `[AsyncMethodBuilder]` outside the task family) are not accepted as `T_I` of an async interceptor in version 1. The adaptation relies on `ConfigureAwait(bool)` and on the continuation rules of the task family, and the awaiter of a custom task-like type has its own scheduling. Mode `Awaitable` remains available for such types (PO25).

#### 7.6.1 Result compatibility

The awaitable type of the interceptor does not have to be the original awaitable type `A`. The requirement is on the result (decision PO59, RC59). The rule applies to every await interceptor: a synthesized interceptor in both modes, and an existing method (section [7.10.1](#7101-existing-method-interceptors)).

Notation: `R` is the result type of the original await, which is the `void` type when the await has no value. `T_I` is the return type of the interceptor, and `R_I` is the result type obtained by awaiting a value of type `T_I`.

Rules:

1. When `R` is `void`, `R_I` must be `void`.
2. Otherwise, `R_I` must not be `void`, and `R_I` must convert implicitly to `R`: `compilation.ClassifyConversion( R_I, R ).IsImplicit` must hold (RC `CSharpExtensions.cs:438`). An identity conversion is included.
3. `R_I` is identical to `R` when the two types are equal with `SymbolEqualityComparer.IncludeNullability`, which also compares tuple element names and separates `dynamic` from `object` (section [8.3](08-deduplication-and-naming.md#83-canonical-types-and-type-parameters)). The nullable annotations may also differ when `R_I` is not annotated and `R` is annotated. An identical `R_I` needs no cast.
4. When `R_I` is not identical to `R` and the value of the await expression is used, the rewrite is `(R)( await I( e ) )`, so that a `var` declaration and later overload resolution see the original type. `R` must then be nameable at the site. A type that contains an anonymous type is not nameable, so such a site is refused with LAMA1021.
5. When the value is not used, no cast is inserted, because a cast is not a valid statement expression (CS0201). The contexts are those of rule E14 of section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9): an expression statement, a `for` initializer or iterator, and the expression body of a member, lambda or local function that returns `void`. The detection is `IsResultUsed` of the context.
6. The conditional-access restriction of rule E14 does not apply. The cast is placed around the await expression itself, which is never a link of a `?.` chain. When the await expression is the receiver of a member access, the cast is parenthesized, as in `((R)( await I( e ) )).P`.
7. The suffix of section [7.5](#75-the-adaptive-rewrite-challenge-to-b8-adopted) is chosen from `T_I` and is independent of the cast: `(R)( await I( e ).ConfigureAwait( false ) )`.

Violations are reported with LAMA1021 at the site, and the await is left unchanged.

| Original awaitable `A` | Return type `T_I` of the interceptor | `R_I` | Outcome |
|---|---|---|---|
| `Task<T>` | `ValueTask<T>` | `T` | Admitted, without cast. `T_I` differs from `A`, so rule 2 of section [7.5](#75-the-adaptive-rewrite-challenge-to-b8-adopted) decides the suffix. |
| `Task<object>` | `ValueTask<string>` | `string` | Admitted. The rewrite casts to `object` when the value is used. |
| `Task<string>` | `ValueTask<object>` | `object` | LAMA1021: `object` does not convert implicitly to `string`. |
| `Task<long>` | `ValueTask<int>` | `int` | Admitted, with an implicit numeric conversion and a cast to `long`. |
| `Task<int>` | `ValueTask<long>` | `long` | LAMA1021: `long` does not convert implicitly to `int`. |
| `Task<(int A, int B)>` | `ValueTask<(int, int)>` | `(int, int)` | Admitted. The conversion is an identity, but the element names differ, so the rewrite casts to `(int A, int B)` when the value is used, and `(await e).A` still compiles. |
| `Task` | `ValueTask` | `void` | Admitted. |
| `Task` | `ValueTask<int>` | `int` | LAMA1021: the original await has no value. |

Example of the cast:

```csharp
// Original code. GetAsync returns Task<object>, so R is object.
var value = await store.GetAsync( key );
Use( value );

// The interceptor returns ValueTask<string>, so R_I is string. The cast keeps the type of 'value', and
// the overload of Use that the original code selected.
var value = (object) ( await Await_Interceptor( store.GetAsync( key ) ) );
Use( value );

// The value of the await is not used, so no cast is inserted.
await Await_Interceptor( store.GetAsync( key ) );
```

`R_I` comes from different places:

- A non-async template in mode `Await` produces `T_I` = `ValueTask<R>` or `Task<R>` (section [7.7](#77-task-type-of-an-async-interceptor-and-target-frameworks)), so `R_I` is `R`.
- An `async` template declares its task type. With a `dynamic` result, `R_I` is `R`. With another type argument, for example `async ValueTask<string>`, `R_I` is that type (section [7.9.1](#791-accepted-template-shapes)).
- In mode `Awaitable`, `T_I` is the type passed to `WithAwaitRewriteOptions`, or `A`, and `R_I` is the result type of awaiting it.
- For an existing method, `R_I` comes from the speculative binding of section [7.10.1](#7101-existing-method-interceptors).

The cast is call-site data. It is part of `AwaitCallSiteShape.ResultCast` (section [7.11](#711-internal-components)), and not of the key of section [8.2](08-deduplication-and-naming.md#82-the-key). A site whose value is used and a site whose value is discarded therefore share one interceptor.

### 7.7 Task type of an async interceptor and target frameworks

This section applies to a non-async template in mode `Await`, and to an `async` template declared with `AnyAwaitable` or `AnyAwaitable<T>`, for which the engine chooses the task type (RC67). For an `async` template declared with `Task`, `Task<T>`, `ValueTask` or `ValueTask<T>`, `T_I` follows from the declared return type of the template (section [7.9.1](#791-accepted-template-shapes)), and the selector below only checks that `ValueTask` is available when the template declares it (LAMA1023).

In mode `Await`, `T_I` is `ValueTask<R>` (or `ValueTask` for a `void` result) when available, otherwise `Task<R>` (or `Task`). `AwaitInterceptorTaskKind.Task` forces `Task`. `AwaitInterceptorTaskKind.ValueTask` reports LAMA1023 when `ValueTask` is not available. `ValueTask<R>` is preferred because a synchronous completion then allocates nothing.

```csharp
internal sealed class AwaitReturnTypeSelector
{
    /// <summary>Creates a selector for one compilation. The instance is request-scoped and must not be cached statically.</summary>
    public AwaitReturnTypeSelector( Compilation compilation );

    public bool TrySelect(
        ITypeSymbol resultType,
        AwaitInterceptorTaskKind requested,
        [NotNullWhen( true )] out INamedTypeSymbol? returnType,
        out Diagnostic? error );
}
```

Detection, memoized per selector instance:

1. For a non-void `R`: `compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1")`. For a `void` `R`: `"System.Threading.Tasks.ValueTask"`.
2. The type is usable when it is not null, is accessible from the compilation's assembly, and its definition carries an attribute named `AsyncMethodBuilderAttribute`.
3. `GetTypeByMetadataName` returns null when several referenced assemblies define the type (for example a polyfill and `System.Threading.Tasks.Extensions`). The selector then falls back to `Task`.
4. Fallback: `Task<TResult>` or `Task`, which exist on every framework that supports `async Task` methods.

| Target | `R` non-void | `R` void |
|---|---|---|
| .NET Core 2.1 and later | `ValueTask<R>` | `ValueTask` |
| netstandard2.0 or .NET Framework with `System.Threading.Tasks.Extensions` 4.5 or later | `ValueTask<R>` | `ValueTask` |
| Same with `System.Threading.Tasks.Extensions` 4.3 (generic `ValueTask<T>` only) | `ValueTask<R>` | `Task` |
| net471, or netstandard2.0 without the package | `Task<R>` | `Task` |

`ConfigureAwait(false)` exists on `Task` since .NET Framework 4.5, so the adaptation works in every row. The detection runs in the premium engine through `ICompilation.GetRoslynCompilation()` (SDK27 `CodeModel\SymbolExtensions.cs:190`). It must not use `ReflectionMapper.GetTypeSymbol(typeof(ValueTask))`, which throws when the type is missing.

### 7.8 Semantics preserved and changed by mode Await

| Aspect | Original | With the adaptive rewrite |
|---|---|---|
| Evaluation of `e` | Evaluated, then `GetAwaiter` is called | Evaluated as the argument of `I`; the template code before `meta.Proceed()` runs synchronously; then the original `GetAwaiter` is called inside `I` |
| Resumption context of `M` | Chosen by the original awaiter | Same for `CapturedContext` and `AnyContext`; explicit policy or mode `Awaitable` for `Unknown` |
| Template code after `meta.Proceed()` | Not applicable | Runs where the original awaiter resumes. For `CapturedContext` and `AnyContext`, `M` resumes on the same context, except in the `AnyContext` case of section [7.5](#75-the-adaptive-rewrite-challenge-to-b8-adopted) where the template suspends after `meta.Proceed()`. With an explicit `Resumption` policy, `M` resumes where the policy states, which can differ from the context of the template code. |
| Synchronous completion | `M` continues synchronously | Same, if the template does not suspend on its own; no allocation with `ValueTask<R>` |
| Faulted operation | `GetResult` throws the first exception | The same exception object reaches `M` through `I`'s task; its stack trace gains the frames of `I` |
| Several exceptions (`Task.WhenAll`) | `M` sees the first | Same |
| Cancellation | `OperationCanceledException` stored in the task and rethrown | `I`'s builder cancels its task with the same exception object (RT `System\Runtime\CompilerServices\AsyncTaskMethodBuilderT.cs:711-714`), and the outer await rethrows it (RT `TaskAwaiter.cs:139-146`) |
| `AsyncLocal<T>` set by `M` before the await | Visible after the await | Visible in the template and after the await |
| `AsyncLocal<T>` set by the template | Not applicable | Not visible to `M` after the await (R-c); documented |
| Null awaitable | `NullReferenceException` at the await | The template code before `meta.Proceed()` runs, then the exception is thrown inside `I` and rethrown in `M` |
| `ValueTask` single consumption | Awaited once | Awaited once, enforced by LAMA0296 (section [7.9.3](#793-at-most-once-rule)) |
| Allocation on suspension | One state machine box for `M` | One more box for `I` |
| Deadlock behavior with synchronous blocking callers | Depends on the original configuration | Preserved for the original await. A template that adds its own capturing await before `meta.Proceed()` can deadlock where the original could not. The documentation tells template authors to use `ConfigureAwait(false)` on their own awaits. |
| Unobserved exceptions | Observed by the await | A template that never calls `meta.Proceed()` leaves the operation unobserved |

Under runtime async, `await I(e)` binds to `AsyncHelpers.Await(ValueTask<R>)` and `await I(e).ConfigureAwait(false)` to `AsyncHelpers.Await(ConfiguredValueTaskAwaitable<R>)`. The inlining rule is the same, so the analysis holds. The runtime tests of section [12.8](12-test-plan.md#128-premium-runtime-execution-tests-runtime) must still run with runtime async enabled before release.

### 7.9 Template model

#### 7.9.1 Accepted template shapes

Await interceptor templates are method templates, and they are referenced by name (decision PO60, RC60). `meta.Target` is the interceptor method. No `MethodTemplateSelector` is involved: the template method decides the shape through its `async` modifier. The engine reads the modifier from the template symbol. `TemplateMember.GetEffectiveKind` returns `TemplateKind.Async` for a default template whose method is `async` (ENG27 `Advising\TemplateMember.cs:80-122`, with `IsAsyncSafe` of `CodeModel\Helpers\AsyncHelper.cs:19-24`), and the proceed expression receives this effective kind (ENG27 `Templating\TemplateExpansionContext.ProceedUserExpression.cs:33`).

| Mode | Template declaration | Generated method | `meta.Proceed()` |
|---|---|---|---|
| `Await` | Non-async: `[Template] dynamic? T()` | `async ValueTask<R> I( A awaitable )`, or the other task type of section [7.7](#77-task-type-of-an-async-interceptor-and-target-frameworks) | `(await awaitable)`, of type `R` |
| `Await` | Non-async: `[Template] void T()`, only when `R` is `void` | `async ValueTask I( A awaitable )`, or `async Task` (section [7.7](#77-task-type-of-an-async-interceptor-and-target-frameworks)) | The statement `await awaitable` |
| `Await` | `async`, the standard form: `[Template] async AnyAwaitable<dynamic?> T()` | `async ValueTask<R> I( A awaitable )`, or the other task type of section [7.7](#77-task-type-of-an-async-interceptor-and-target-frameworks); `async ValueTask` or `async Task` when `R` is `void` | `awaitable`, of type `A` |
| `Await` | `async`: `[Template] async AnyAwaitable T()`, for a template that never returns a value | `async ValueTask I( A awaitable )` or `async Task`, for a `void` `R` | `awaitable`, of type `A` |
| `Await` | `async`: `[Template] async ValueTask<dynamic?> T()` | `async ValueTask<R> I( A awaitable )` | `awaitable`, of type `A` |
| `Await` | `async`: `[Template] async Task<dynamic?> T()` | `async Task<R> I( A awaitable )` | `awaitable`, of type `A` |
| `Await` | `async`: `[Template] async Task T()` or `async ValueTask T()`, only when `R` is `void` | `async Task I( A awaitable )` or `async ValueTask I( A awaitable )` | `awaitable`, of type `A` |
| `Await` | `async` with a type argument other than `dynamic`, for example `[Template] async ValueTask<string> T()` | `async ValueTask<string> I( A awaitable )`; `R_I` is `string`, which must convert implicitly to `R` (section [7.6.1](#761-result-compatibility)) | `awaitable`, of type `A` |
| `Awaitable` | Non-async: `[Template] dynamic? T()`, or a return type that converts implicitly to `T_I` | `T_I I( A awaitable )`, not `async` | `awaitable`, of type `A` |
| `Awaitable` | `async` | Rejected with LAMA1022 | |

Rules for a non-async template in mode `Await`. The template receives the awaited value from `meta.Proceed()`. The engine makes the interceptor `async`, with the task type of section [7.7](#77-task-type-of-an-async-interceptor-and-target-frameworks): `ValueTask<R>` when available, otherwise `Task<R>`, which `AwaitRewriteOptions.TaskKind` overrides.

Rules for the declared return type of an `async` template:

- `AnyAwaitable<dynamic?>` and `AnyAwaitable` let the engine choose the task type with the rules of section [7.7](#77-task-type-of-an-async-interceptor-and-target-frameworks), and `AwaitRewriteOptions.TaskKind` overrides it, as for a non-async template (RC67). `AnyAwaitable<dynamic?>` gives `R_I = R`. It is the standard form. The non-generic `AnyAwaitable` serves a template that never returns a value, because C# forbids `return value;` in an `async` method whose non-generic task-like type has no result (CS1997). A template declared `AnyAwaitable<dynamic?>` whose `R` is `void` is accepted: the existing expansion turns the returned expression into a statement and emits a bare `return`, as it does for a `Task<dynamic?>` template on a `void` result (ENG27 `Templating\TemplateExpansionContext.cs:393-407`, `CreateReturnStatementVoid` at lines 606-650).
- Otherwise, the definition of the declared type selects the task type of the interceptor, with its literal meaning. `Task` and `Task<TResult>` give `Task` and `Task<R_I>`. `ValueTask` and `ValueTask<TResult>` give `ValueTask` and `ValueTask<R_I>`, and `ValueTask` must be available in the project (LAMA1023).
- A `dynamic` type argument gives `R_I = R`. Another type argument gives `R_I` equal to that type, translated into the run-time compilation as the existing binding translates template types (ENG27 `Advising\TemplateBindingHelper.cs:750-771`), and section [7.6.1](#761-result-compatibility) applies. A non-generic `Task` or `ValueTask` gives a `void` `R_I`, which is valid only when `R` is `void`.
- Any other declared type is refused with LAMA1016: `async void`, whose value cannot be awaited, and a custom task-like type, which version 1 does not accept as `T_I` (section [7.6](#76-modes), PO25).
- For a declared `Task` or `ValueTask` type, `AwaitRewriteOptions.TaskKind` does not apply. A value other than `Default` that names another task type than the declared one is refused with LAMA1016.
- An `async` template cannot be declared with an awaitable type that has no method builder, for example `ConfiguredTaskAwaitable<T>`: C# refuses such an `async` method, so the template does not compile.
- The engine derives the return type of the interceptor from the declared type, so the existing check of the template return type accepts the template: a declared type converts to itself (ENG27 `Advising\TemplateBindingHelper.cs:789`), `Task<dynamic>` is accepted for any awaitable (lines 803-812), and `ValueTask<dynamic>` matches `ValueTask<R>` by the rule on generic definitions (lines 824-830).

Difference with override templates. For an override, `Task<dynamic>` is a wildcard: the binding accepts it for any awaitable target, and the target keeps its own return type (ENG27 `Advising\TemplateBindingHelper.cs:803-812`). An await interceptor has no return type to keep, because the engine creates the method. A declared `Task<dynamic?>` or `ValueTask<dynamic?>` therefore keeps its literal meaning and selects the task type, and `AnyAwaitable<dynamic?>` is the form that lets the engine choose it (RC67). This replaces the rule of RC60, in which the usual `async Task<dynamic?>` template was the only `async` form for any awaitable and gave `Task<R>`. The documentation recommends `AnyAwaitable<dynamic?>` and states the literal meaning of the other forms.

Template compiler. The template compiler has no rule that depends on the declared return type of an `async` template: return types are checked when a template is bound to a target method (ENG27 `Advising\TemplateBindingHelper.cs:742-837`). No existing aspect test declares an `async ValueTask<dynamic?>` template (search of TST27 on 2026-09-25), so M5 adds template tests for `async ValueTask<dynamic?>`, `async ValueTask` and a type argument other than `dynamic`. `AnyAwaitable<dynamic?>` needs the classification rule and the binding rules of section [10.6.9](10b-oss-linker-and-templates.md#1069-anyawaitable), which the milestone MA delivers before M5.

Mode `Awaitable` stays the explicit option for an interceptor that returns another awaitable without awaiting, for example a custom awaitable (R11, section [7.6](#76-modes)). It requires a non-async template, whose `meta.Proceed()` returns the raw awaitable. An `async` template is rejected with LAMA1022, because mode `Awaitable` generates a method that is not `async`.

For the open-source factory, the premium engine passes the template name as `new MethodTemplateSelector( templateName, useAsyncTemplateForAnyAwaitable: true )` in mode `Await`, and as `new MethodTemplateSelector( templateName )` in mode `Awaitable`. The selector names no alternative template, so no variant is selected. The flag makes the helper interpret a non-async template as async when the return type of the interceptor has a method builder (ENG27 `Advising\AdviceFactory.cs:262`), so that the interceptor is `async` (`MustInterpretAsAsyncTemplate`, ENG27 `Advising\TemplateExtensions.cs:113-115`). An `async` template is interpreted as async by its modifier in both cases. The selector is an internal value of the engine and is never received from the user (section [10.6.1](10b-oss-linker-and-templates.md#1061-selection-at-declaration-time)).

#### 7.9.2 Meaning of meta.Proceed() and meta.ProceedAsync()

| Mode and template kind | `meta.Proceed()` | `meta.ProceedAsync()` |
|---|---|---|
| `Await`, non-async template | `(await awaitable)` of type `R`; the statement `await awaitable` when `R` is `void` | Same as `meta.Proceed()` |
| `Await`, async template | `awaitable` of type `A`; the template writes `await meta.Proceed()` | `awaitable` of type `A`; the template writes `await meta.ProceedAsync()` |
| `Awaitable` | `awaitable` of type `A` | `awaitable` of type `A` |

The meaning in an `async` template is the meaning that async override templates already have. When the effective template kind is `Async` and the target is neither `void` nor an async iterator, `ProceedHelper.CreateProceedExpression` returns the invocation without an await, with the return type of the target (ENG27 `Transformations\ProceedHelper.cs:99-113`). A non-async template on an async target receives the awaited value instead (lines 68-97). For an interceptor, the awaitable parameter plays the role of the invocation.

`meta.ProceedAsync().ConfigureAwait(false)` becomes `awaitable.ConfigureAwait(false)`. It is valid only when `A` has `ConfigureAwait(bool)`. For the other kinds, the open-source fix of section [10.6.7](10b-oss-linker-and-templates.md#1067-configureawait-fix) reports LAMA0295 instead of throwing. A `ConfigureAwait` in the template changes only where the template code after the await runs. It does not change where `M` resumes, because the call-site adaptation is independent.

The proceed expression is produced by the declarative open-source bindings `ProceedBinding.AwaitParameter( 0, resultType: R )` in mode `Await` and `ProceedBinding.ReturnParameter( 0 )` in mode `Awaitable` (section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)). The binding does not reuse `ProceedHelper.CreateProceedExpression`, because that helper types the Async form as the method's return type (ENG26 `Transformations\ProceedHelper.cs:111-113`), which would be `ValueTask<R>` instead of `A`.

#### 7.9.3 At-most-once rule

The awaitable represents an operation that has already started. Awaiting it twice does not repeat the operation, and a second await of a `ValueTask` backed by `IValueTaskSource` is undefined behavior. The rule is: in the expanded body, the proceed expression is emitted at most once, and not inside a loop statement, a lambda or a local function of the expanded body. Templates cannot contain `goto` or labels (ENG27 `Templating\TemplateAnnotator.cs:2640-2650`), so this syntactic rule guarantees at most one evaluation per execution. Compile-time loops that emit the proceed expression several times are detected because the count is taken on the emitted syntax.

The check is generic and belongs to the open-source template engine: `SynthesizedMethodTemplate.ProceedMultiplicity = ProceedMultiplicity.AtMostOnce` (section [10.6.5](10b-oss-linker-and-templates.md#1065-proceed-multiplicity)), reported with LAMA0296 (RC27). Two occurrences in exclusive branches (`if` and `else`) are rejected in version 1. The rule does not depend on the mode or on the template kind. In a non-async template in mode `Await`, the counted expression is `(await awaitable)`. In an `async` template and in mode `Awaitable`, it is the awaitable parameter that `meta.Proceed()` returns, which the template awaits or returns. Direct uses of the awaitable parameter through `meta.Target.Parameters[0]` are not counted; the documentation states that they must not await the value.

#### 7.9.4 Interaction with the existing async machinery

- Return statements. `CreateReturnStatement` types `return` with `method.GetAsyncInfoImpl().ResultType` when the template is interpreted as async (ENG27 `Templating\TemplateExpansionContext.cs:393-403`). `T_I` is in the task family in mode `Await`. `AsyncHelper` handles it through its symbol path (ENG26 `CodeModel\Helpers\AsyncHelper.cs:145-171`). When `T_I` mentions an interceptor-owned type parameter (section [7.10.3](#7103-generic-lifting-and-anonymous-types)), the type has no symbol that can be used without mapping, and `AsyncHelper` uses its code-model path (ENG27 `CodeModel\Helpers\AsyncHelper.cs:47-54, 74-111`). That path computes `hasMethodBuilder` only from an `AsyncMethodBuilderAttribute`, and does not treat `Task` and `Task<TResult>` as having a method builder. For `T_I = Task<T>` (the fallback of section [7.7](#77-task-type-of-an-async-interceptor-and-target-frameworks), or `AwaitInterceptorTaskKind.Task`), the Default template would then not be interpreted as async, and `DeclareMethod` would throw (section [10.4.8](10a-oss-bridge-hook-factory.md#1048-validation-performed-by-the-factory)). The open-source fix F18 adds to `TryGetAsyncInfoFromCodeModel` the same `Task` check as `GetAwaitableResultTypeCore` (lines 151-155).
- `AsyncHelper` limits. The premium engine does not use `AsyncHelper` for `A` and `R`. In mode `Awaitable`, a `T_I` whose `GetAwaiter` is an extension method is not recognized by `AsyncHelper`, so `meta.ProceedAsync()` is rejected with LAMA0223 (ENG27 `Templating\TemplateExpansionContext.ProceedUserExpression.cs:46`). `meta.Proceed()` remains available.

### 7.10 Existing methods and placements for awaits

#### 7.10.1 Existing-method interceptors

The engine validates an existing method at the await with speculative binding, which reproduces overload resolution, conversions, type inference and awaitability:

```csharp
internal static class ExistingAwaitInterceptorValidator
{
    /// <summary>
    /// Binds <c>Q.M(operand)</c> and <c>await Q.M(operand)</c> speculatively at the await position, where <c>Q</c> is the
    /// fully qualified placement for a static method or <c>this</c> for an instance method.
    /// </summary>
    public static bool TryValidate(
        SemanticModel semanticModel,
        AwaitSiteAnalysis site,
        IMethodSymbol method,
        ExistingMethodInvocationForm form,
        out ExistingAwaitInterceptorBinding binding,
        out Diagnostic? error );
}

internal enum ExistingMethodInvocationForm
{
    Static,
    ThisInstance
}

internal readonly record struct ExistingAwaitInterceptorBinding(
    IMethodSymbol ConstructedMethod,
    ITypeSymbol ReturnType,
    ITypeSymbol AwaitResultType );
```

Rules:

- The speculative invocation must bind to `method` or to a construction of it. Type arguments are never written explicitly: the only argument is the operand, whose type is known, so C# inference succeeds whenever the method applies.
- The parameter bound to `InterceptorArgument.Awaitable` has `RefKind.None`. By default, it is the single required parameter. The `bind` function of `ExistingMethod` can bind the other parameters, for example a `CancellationToken` parameter to a value pulled from the origin (section [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values)). An unbound caller-information parameter receives the value materialized from the source await (section [6.7](06b-signatures-and-validation.md#67-caller-information-materialization)), an unbound optional parameter its default, and an unbound required parameter gives LAMA1013 (E7). The speculative invocation passes the values of these sources after the operand.
- `GetSpeculativeTypeInfo` of `await Q.M(operand)` gives `R_I`, the result type of awaiting the return value of the method. The method can return another awaitable type than `A`. `R_I` follows the rules of section [7.6.1](#761-result-compatibility), as for a synthesized interceptor: it must convert implicitly to `R`, it must be `void` when `R` is `void`, and the rewrite is `(R)( await I( e ) )` when `R_I` is not identical to `R` and the value of the await expression is used. Violations report LAMA1021. For example, `static ValueTask<T> Traced<T>( Task<T> task )` intercepts an await of `Task<T>`, and `static ValueTask<string> Find( Task<object> task )` intercepts an await of `Task<object>` with a cast to `object` when the value is used.
- The call-site shape follows section [7.5](#75-the-adaptive-rewrite-challenge-to-b8-adopted) with `T_I` the return type of the constructed method.
- Other violations report LAMA1013, and the site is not rewritten.

#### 7.10.2 Placements

| Placement | Valid when | Invocation form at the call site |
|---|---|---|
| Static method in a type of the current compilation, or the generated static class | `A`, `R`, the awaiter type and the awaiter members are accessible from the type; for `UsesExtensionGetAwaiter`, see below | `await global::N.C.I(e)` |
| Static method in the calling type | Always accessible; not in top-level statements | `await I(e)` |
| Instance method in the calling type or a base type | `HasThis`, the calling type is a class, and the await is not in a static lambda or static local function | `await this.I(e)` |
| Local function in the origin | The origin has a source body; the await is not in an initializer or top-level statements; the await is not in a static lambda or static local function | `await I(e)` |

`BaseMostAccessibleType()` is admissible for awaits. The walk of section [6.5.6](06b-signatures-and-validation.md#656-base-most-accessible-type) applies the conditions of the first and third rows to each candidate: `A`, `R`, the awaiter type and the awaiter members must be accessible from the candidate, an extension `GetAwaiter` must bind at the candidate (see below), and an instance interceptor needs `this` at the await.

An await has no receiver, so it follows rule R0 of section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping): the interceptor is static by default, and a `configure` function makes it an instance method of the calling type or of a base type with `IsStatic = false` (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)). The binder reports `ReceiverMapping.None`, and the operand is bound through `InterceptorArgument.Awaitable`. A `configure` function can add parameters, whose values the rewritten await passes as named arguments after the operand: `await I( e, cancellationToken: ct )` (section [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values)). Instance interceptors placed in structs are rejected: a lambda in a struct cannot capture `this` (CS1673), and an async struct method works on a copy of `this`. Async methods cannot have `ref`, `out` or `in` parameters, so CS1628 does not arise for the origin itself. It can arise for an enclosing non-async member when the await is in an async lambda; the engine then reports LAMA1015 for a local-function placement.

Extension `GetAwaiter`. In mode `Await`, the interceptor body contains `await awaitable`. When the original awaiter comes from an extension `GetAwaiter`, the same method must be selected at the interceptor's position, because the using directives of the file of the placement can differ from those of the call site. For a type placement in a user type, the engine binds `default(global::A).GetAwaiter()` with `SemanticModel.GetSpeculativeSymbolInfo` at the open brace of the declaration part into which the factory inserts the member. The bound method must equal `info.GetAwaiterMethod` after `ReducedFrom` normalization. A speculative binding is used, and not `LookupSymbols`, because a lookup returns every accessible candidate without overload resolution. For a local-function placement, the position is the await itself, so the check always succeeds. For `GeneratedStaticClass`, the synthesized tree has no using directives, so the placement is accepted only when the extension method is declared in a type of the global namespace. Otherwise, the placement is rejected with LAMA1015. Mode `Awaitable` never awaits inside the interceptor, so it has no such constraint.

#### 7.10.3 Generic lifting and anonymous types

When `A` or `R` mentions type parameters of the calling context that the placement cannot name, the interceptor becomes generic over them, with copied constraints (section [6.4.6](06b-signatures-and-validation.md#646-generic-specialization-and-lifting)). Inference at the call site always succeeds, because the single argument has type `A`, every lifted type parameter occurs in `A`, and `R` is derived from `A`. Anonymous types in `A` are lifted in the same way. No explicit type arguments are emitted.

### 7.11 Internal components

```csharp
internal sealed class AwaitInterceptionContextImpl : AwaitInterceptionContext { /* wraps AwaitSiteAnalysis and the code model */ }

internal static class AwaitableClassifier
{
    public static AwaitableKind Classify( ITypeSymbol awaitableType, IMethodSymbol? getAwaiterMethod );

    public static AwaitResumption GetResumption( AwaitableKind kind, bool isDirectConfigureAwaitCall, AwaitConfigurationFlags? constantConfiguration );
}

internal readonly record struct AwaitCallSiteShape( bool AppendConfigureAwaitFalse, ITypeSymbol? ResultCast );

internal static class AwaitResumptionAdapter
{
    /// <summary>Applies the rule of section 7.5 and the result compatibility rule of section 7.6.1.</summary>
    public static bool TryGetShape(
        AwaitSiteAnalysis site,
        ITypeSymbol interceptorReturnType,
        ITypeSymbol awaitResultType,
        AwaitResumption? explicitPolicy,
        out AwaitCallSiteShape shape,
        out Diagnostic? error );
}

internal static class AwaitPlacementValidator
{
    public static bool TryValidate( SemanticModel semanticModel, AwaitSiteAnalysis site, InterceptorPlacement placement, out Diagnostic? error );
}

internal sealed class AwaitInterceptionRequestFactory
{
    public AwaitInterceptionRequestFactory( Compilation compilation, AwaitReturnTypeSelector returnTypeSelector );

    /// <summary>
    /// Computes the stage-1 request of one await: the synthesized signature and key, or the validated existing method,
    /// and the call-site shape. It expands no template.
    /// </summary>
    public AwaitInterceptionRequest? Create( AwaitSiteAnalysis site, InterceptorResult result, IDiagnosticAdder diagnostics );
}

internal sealed class AwaitInterceptionRequest
{
    public required AwaitSiteAnalysis Site { get; init; }
    public InterceptorRequestKey? Key { get; init; }                 // Null for an existing method.
    public IMethodSymbol? ExistingMethod { get; init; }
    public required AwaitCallSiteShape Shape { get; init; }
    public required ITypeSymbol ReturnType { get; init; }            // T_I.
    public required AwaitInterceptionMode Mode { get; init; }
}
```

The await request key is the general key of section [8.2](08-deduplication-and-naming.md#82-the-key), with the proceed target `AwaitProceedTargetKey( Mode, A, R, T_I )`. The `ConfigureAwait` suffix and the result cast are call-site data outside the key: `await t.ConfigureAwait(true)` and `await t.ConfigureAwait(false)` on the same `Task<int>` share one interceptor `I(ConfiguredTaskAwaitable<int>)` and differ only at the call site.
