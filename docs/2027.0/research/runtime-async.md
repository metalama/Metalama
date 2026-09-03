# Runtime Async ("Runtime Async V2") in .NET 11 — research notes

Research date: **2026-09-03**. Latest public .NET 11 milestone at that date: **Preview 7** (no RC folder exists yet
under `dotnet/core/release-notes/11.0/`). .NET 11 / C# 15 GA is expected **November 2026**.

All statements below were verified against primary sources (dotnet/runtime, dotnet/roslyn, dotnet/core release
notes, dotnet/docs / learn.microsoft.com). Source URLs are given inline. Where two sources disagree, both are
reported and the more recent / more authoritative one is identified.

---

## 1. Executive summary

Runtime async moves the async state machine out of compiler-generated classes and into the CLR. The C# compiler
stops emitting an `IAsyncStateMachine` struct/class plus an `AsyncTaskMethodBuilder`; instead it emits the original
method body, marks the method with the `MethodImplAttributes.Async` (0x2000) metadata flag, and expresses each
`await` either as a *runtime-async call pair* (`call` the awaited method, then `call
System.Runtime.CompilerServices.AsyncHelpers.Await(...)`) or as a call to
`AsyncHelpers.AwaitAwaiter`/`UnsafeAwaitAwaiter`. The JIT/AOT compiler then generates suspension and resumption
code and allocates `Continuation` objects.

For a tool that rewrites **C# source** before the compiler sees it, this is **below the source level**: there is no
new syntax, no new `SyntaxKind`, no new modifier, no new symbol shape, and no new attribute a user writes.
The two places where it becomes visible to a Roslyn-based tool are (a) the semantic model for `await` expressions
(`AwaitExpressionInfo.RuntimeAwaitMethod`, and `GetAwaiterMethod`/`IsCompletedProperty`/`GetResultMethod` becoming
`null`), and (b) `RuntimeCapability.RuntimeAsyncMethods`. Both are additive and both were already shipped in the
.NET 10 / VS 18.x compiler.

For a tool that rewrites **IL** after the compiler, this is a hard break: the IL shape is genuinely new.

---

## 2. Primary sources

| Source | URL |
|---|---|
| ECMA-335 augment spec (authoritative for metadata/IL) | https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md |
| Roslyn design document | https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Runtime%20Async%20Design.md |
| Runtime epic issue | https://github.com/dotnet/runtime/issues/109632 |
| Roslyn test plan issue | https://github.com/dotnet/roslyn/issues/75960 |
| Public API proposal (approved) | https://github.com/dotnet/runtime/issues/114310 |
| Original experiment issue (.NET 9) | https://github.com/dotnet/runtime/issues/94620 |
| Superseded runtimelab "async2" design | https://github.com/dotnet/runtimelab/blob/feature/async2-experiment/docs/design/features/runtime-handled-tasks.md |
| What's new in .NET 11 runtime | https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime |
| .NET 11 Preview 1 runtime notes | https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/runtime.md |
| .NET 11 Preview 2 runtime notes | https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview2/runtime.md |
| .NET 11 Preview 3 runtime notes | https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview3/runtime.md |
| .NET 11 Preview 4 runtime notes | https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview4/runtime.md |
| .NET 11 Preview 5 runtime notes | https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/runtime.md |
| .NET 11 Preview 6 runtime notes | https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/runtime.md |
| .NET 11 Preview 7 runtime notes | https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/runtime.md |
| Debugger support PR | https://github.com/dotnet/runtime/pull/123644 |
| Removal of `DOTNET_RuntimeAsync` knob | https://github.com/dotnet/runtime/pull/125406 |

Key Roslyn source files read (raw.githubusercontent.com, branch `main`):

* `src/Compilers/CSharp/Portable/Lowering/AsyncRewriter/RuntimeAsyncRewriter.cs`
* `src/Compilers/CSharp/Portable/Compiler/MethodCompiler.cs` (lines ~780 and ~1605)
* `src/Compilers/CSharp/Portable/Compilation/CSharpCompilation.cs` (`IsRuntimeAsyncEnabledIn`, lines 346-396)
* `src/Compilers/CSharp/Portable/Binder/Binder_Await.cs`
* `src/Compilers/CSharp/Portable/Compilation/AwaitExpressionInfo.cs`
* `src/Compilers/CSharp/Portable/CodeGen/CodeGenerator.cs`
* `src/Compilers/CSharp/Portable/Symbols/AssemblySymbol.cs`
* `src/Compilers/CSharp/Portable/Symbols/Metadata/PE/PEMethodSymbol.cs`
* `src/Compilers/CSharp/Portable/Symbols/Source/SourceMethodSymbolWithAttributes.cs`
* `src/Compilers/CSharp/Portable/Symbols/Synthesized/SynthesizedEntryPointSymbol.cs`
* `src/Compilers/CSharp/Portable/Lowering/StateMachineRewriter/IteratorAndAsyncCaptureWalker.cs`
* `src/Compilers/CSharp/Portable/Errors/ErrorCode.cs`, `CSharpResources.resx`
* `src/Compilers/Core/Portable/CommandLine/Feature.cs`
* `src/Compilers/Core/Portable/RuntimeCapability.cs`
* `src/Compilers/Core/Portable/Symbols/Attributes/AttributeDescription.cs`
* `docs/Language Feature Status.md`, `docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`

Key runtime source files read:

* `src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/AsyncHelpers.cs`
* `src/coreclr/System.Private.CoreLib/src/System/Runtime/CompilerServices/AsyncHelpers.CoreCLR.cs`
* `src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/MethodImplOptions.cs`
* `src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/RuntimeFeature.cs`
* `src/libraries/System.Runtime/ref/System.Runtime.cs` (shipped reference-assembly surface)
* `src/libraries/Directory.Build.targets` (the `UseRuntimeAsync` / `RuntimeAsyncSupported` properties)

---

## 3. What the compiler emits instead of a state machine

### 3.1 Method-level marking

The C# source

```cs
async Task M()
{
    // ...
}
```

is emitted as

```cs
[MethodImpl(MethodImplOptions.Async)]
Task M()
{
    // ... rewritten body ...
}
```

Verbatim from the Roslyn design document ("General signature transformation"): "The same holds for methods that
return `Task<T>`, `ValueTask`, and `ValueTask<T>`. Any method returning a different `Task`-like type is not
transformed to runtime async form and uses a C#-generated state machine."

The marking is **not a custom attribute in metadata**. `MethodImplAttribute` is a pseudo-custom attribute: the
compiler sets a bit in the `MethodDef` row's `MethodImplAttributes`. From the ECMA augment
(§ II.23.1.11 *Flags for methods [MethodImplAttributes]*):

| Flag | Value | Description |
|---|---|---|
| Async | 0x2000 | Method is an Async Method. |

> "The flag is represented in IL by the `async` keyword. Tools like `ilasm` and `ildasm` recognize this flag."

Roslyn sets it in `SourceMethodSymbolWithAttributes.AddAsyncImplAttributeIfNeeded`:

```cs
protected void AddAsyncImplAttributeIfNeeded(ref System.Reflection.MethodImplAttributes result)
{
    if (this.IsAsync && this.DeclaringCompilation.IsRuntimeAsyncEnabledIn(this))
    {
        // When a method is emitted using runtime async, we add MethodImplAttributes.Async to indicate to the
        // runtime to generate the state machine
        result |= System.Reflection.MethodImplAttributes.Async;
    }
}
```

Consequences:

* `System.Reflection.MethodImplAttributes.Async = 8192` and `System.Runtime.CompilerServices.MethodImplOptions.Async = 0x2000` are new **public** BCL enum members (verified in `src/libraries/System.Runtime/ref/System.Runtime.cs`: `Async = 8192,` inside `enum MethodImplOptions`; approved in dotnet/runtime#114310).
* Because it is a pseudo-custom attribute, `IMethodSymbol.GetAttributes()` and `MethodInfo.GetCustomAttributes()` do **not** show it. It is read through `MethodBase.MethodImplementationFlags`.
* No `[AsyncStateMachine(typeof(...))]` is emitted, because there is no state machine type to reference. (Inference, not a directly quoted statement; the attribute's only constructor takes the state machine `Type`. See open questions.)

### 3.2 Applicability rules (ECMA augment, § I.8.4.5 "Sync and Async Methods")

Verbatim:

> Applicability of `MethodImplOptions.Async`:
> * The `[MethodImpl(MethodImplOptions.Async)]` only has effect when applied to method definitions that return generic or nongeneric variants of Task or ValueTask.
> * The `[MethodImpl(MethodImplOptions.Async)]` only has effect when applied to method definitions with CIL implementation.
> * Async method definitions are only valid inside async-capable assemblies. An async-capable assembly is one which references a corlib containing an `abstract sealed class RuntimeFeature` with a `public const string` field member named `Async`.
> * Combining `MethodImplOptions.Async` with `MethodImplOptions.Synchronized` is invalid.
> * Applying `MethodImplOptions.Async` to methods with a `byref` or `ref-like` return value is invalid.
> * Applying `MethodImplOptions.Async` to vararg methods is invalid.
>
> _[Note: these rules operate before generic substitution, meaning that a method which only meets requirements after substitution would not be considered as valid.]_

**Discrepancy worth noting.** The spec's "async-capable assembly" test names `RuntimeFeature.Async`. That constant
does **not** exist in the shipped BCL: `src/libraries/System.Runtime/ref/System.Runtime.cs` lists
`RuntimeFeature` with `ByRefFields`, `ByRefLikeGenerics`, `CovariantReturnsOfClasses`,
`DefaultImplementationsOfInterfaces`, `NumericIntPtr`, `PortablePdb`, `UnmanagedSignatureCallingConvention`,
`VirtualStaticsInInterfaces` — and no `Async`. Roslyn instead probes for the *type* `AsyncHelpers`:

```cs
// Keep in sync with VB's AssemblySymbol.RuntimeSupportsAsyncMethods
internal bool RuntimeSupportsAsyncMethods
    => GetSpecialType(InternalSpecialType.System_Runtime_CompilerServices_AsyncHelpers) is { TypeKind: TypeKind.Class, IsStatic: true };
```

The Roslyn implementation is the more recent and the one that actually ships; treat the spec's `RuntimeFeature.Async`
clause as stale.

The Roslyn design document adds a further constraint on where `AsyncHelpers` must live:

> "These APIs must be defined in the same assembly that defines `object`, and the assembly cannot reference any
> other assemblies. In terms of CoreFX, this means it must be defined in the `System.Runtime` reference assembly."

### 3.3 Return convention: the IL returns `T`, not `Task<T>`

From the ECMA augment:

> "Async methods also do not have matching return type conventions as sync methods. For sync methods, the stack
> should contain a value convertible to the stated return type before the `ret` instruction. For async methods, the
> stack should be empty in the case of `Task` or `ValueTask`, or the type argument in the case of `Task<T>` or
> `ValueTask<T>`."

Roslyn implements exactly this in `CodeGenerator.LazyReturnTemp`:

```cs
if (_method.IsAsync && _module.Compilation.IsRuntimeAsyncEnabledIn(_method))
{
    // The return type of the method is either Task<T> or ValueTask<T>. The il of the method is
    // actually going to appear to return a T, not the wrapper task type. So we need to
    // translate the return type to the actual type that will be returned.
    var returnType = returnTypeWithAnnotations.Type;
    Debug.Assert(((InternalSpecialType)returnType.OriginalDefinition.ExtendedSpecialType)
        is InternalSpecialType.System_Threading_Tasks_ValueTask_T or InternalSpecialType.System_Threading_Tasks_Task_T);
    returnTypeWithAnnotations = ((NamedTypeSymbol)returnType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
}
```

and in `CodeGenerator.HandleReturn`, where an `async Task` / `async ValueTask` method is treated as returning void:

```cs
Debug.Assert(_method.ReturnsVoid == (_returnTemp == null)
    || (_method.IsAsync
        && _module.Compilation.IsRuntimeAsyncEnabledIn(_method)
        && ((InternalSpecialType)_method.ReturnType.ExtendedSpecialType)
            is InternalSpecialType.System_Threading_Tasks_Task or InternalSpecialType.System_Threading_Tasks_ValueTask));
```

**This is the single most disruptive IL-level fact.** A method whose `MethodDef` signature says it returns
`class [System.Runtime]System.Threading.Tasks.Task`1<int32>` has IL whose `ret` pushes an `int32`, and a method
whose signature says `Task` has IL with a bare `ret`. Any IL verifier, IL rewriter, decompiler, or
`System.Reflection.Metadata`-based analyzer that assumes the return-type convention will reject or corrupt such a
method unless it honours the 0x2000 flag.

Another consequence visible in `CodeGenerator.HandleReturn`: PDB sequence points for the closing brace are now
emitted for runtime-async methods, which previously were excluded along with iterators
(`if (_emitPdbSequencePoints && !_method.IsIterator && (!_method.IsAsync || IsRuntimeAsyncEnabledIn(_method)))`).

### 3.4 The suspension helpers

Declared in the ECMA augment and shipped in the `System.Runtime` reference assembly:

```cs
namespace System.Runtime.CompilerServices
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static partial class AsyncHelpers
    {
        [MethodImpl(MethodImplOptions.Async)] public static void AwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion;
        [MethodImpl(MethodImplOptions.Async)] public static void UnsafeAwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion;

        [MethodImpl(MethodImplOptions.Async)] public static void Await(Task task);
        [MethodImpl(MethodImplOptions.Async)] public static void Await(ValueTask task);
        [MethodImpl(MethodImplOptions.Async)] public static T    Await<T>(Task<T> task);
        [MethodImpl(MethodImplOptions.Async)] public static T    Await<T>(ValueTask<T> task);

        [MethodImpl(MethodImplOptions.Async)] public static void Await(ConfiguredTaskAwaitable configuredAwaitable);
        [MethodImpl(MethodImplOptions.Async)] public static void Await(ConfiguredValueTaskAwaitable configuredAwaitable);
        [MethodImpl(MethodImplOptions.Async)] public static T    Await<T>(ConfiguredTaskAwaitable<T> configuredAwaitable);
        [MethodImpl(MethodImplOptions.Async)] public static T    Await<T>(ConfiguredValueTaskAwaitable<T> configuredAwaitable);

        // Present in the shipped reference assembly, not in the ECMA draft:
        public static void HandleAsyncEntryPoint(Task task);
        public static int  HandleAsyncEntryPoint(Task<int> task);
    }
}
```

Notes from the actual implementation (`AsyncHelpers.cs`, `AsyncHelpers.CoreCLR.cs`):

* The helpers carry `[Intrinsic]`, `[BypassReadyToRun]`, `[StackTraceHidden]` and, for the awaiter overloads,
  `MethodImplOptions.NoInlining | MethodImplOptions.Async`. `[StackTraceHidden]` is why they do not pollute stack traces.
  Source comment: `// "BypassReadyToRun" is until AOT/R2R typesystem has support for MethodImpl.Async` and
  `// Must be NoInlining because we use AsyncSuspend to manufacture an explicit suspension point.`
* `[Experimental("SYSLIB5007")]`, which the Roslyn design document still shows on `AsyncHelpers`, has been **removed**
  from the shipped surface. The reference assembly carries only `[EditorBrowsable(Never)]`. This matches Preview 3,
  which "removes the `[RequiresPreviewFeatures]` gate from its APIs" (dotnet/runtime#124488). The design document is
  stale on this point.
* On platforms where runtime async is not implemented (`#else` branch of `#if CORECLR || NATIVEAOT`), the whole class
  is compiled to stubs that `throw new PlatformNotSupportedException("Runtime Async is not supported on this platform.")`.
* Preview 7 added an internal `AsyncHelpers.TransparentAwait(Task | ValueTask | Task<T> | ValueTask<T>)` family in
  `AsyncHelpers.CoreCLR.cs`, commented `// The next four overloads 'TransparentAwait' are called by the JIT in ...`,
  which the JIT can inline so that awaiting an already-completed task folds into a flag check (dotnet/runtime#130482).

From the ECMA augment on how to call them:

> "These methods are only legal to call inside async methods. The `...AwaitAwaiter` methods will have semantics
> analogous to the current `AsyncTaskMethodBuilder.AwaitOnCompleted/AwaitUnsafeOnCompleted` methods. After calling
> either method, it can be presumed that the task or awaiter has completed. The `Await` methods perform suspension
> like the `...AwaitAwaiter` methods, but are optimized for calling on the return value of a call to an async method.
> To achieve maximum performance, the IL sequence of two `call` instructions -- one to the async method and
> immediately one to the `Await` method -- should be preferred."

### 3.5 Exact IL shapes (from the Roslyn design document)

`await C.M()` where `C.M()` returns `Task`:

```il
call [System.Runtime]System.Threading.Tasks.Task C::M()
call void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
```

`int i = await C.M()` where `C.M()` returns `Task<int>`:

```il
call class [System.Runtime]System.Threading.Tasks.Task`1<int32> C::M()
call int32 [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await<int32>(class [System.Runtime]System.Threading.Tasks.Task`1<int32>)
stloc.0
```

Instance call, awaiting a local, awaiting a delegate invocation, and generic cases all follow the same
`call`-then-`call`-`Await` shape; for example awaiting a `Task`-returning delegate:

```il
IL_001b: callvirt instance class [System.Runtime]System.Threading.Tasks.Task AsyncDelegate::Invoke()
IL_0020: call     void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await(class [System.Runtime]System.Threading.Tasks.Task)
```

`await c` for a custom awaitable whose awaiter implements `ICriticalNotifyCompletion`:

```il
.locals init ([0] class C/Awaiter awaiter)

IL_0000: newobj   instance void C::.ctor()
IL_0005: callvirt instance class C/Awaiter C::GetAwaiter()
IL_000a: stloc.0
IL_000b: ldloc.0
IL_000c: callvirt instance bool C/Awaiter::get_IsCompleted()
IL_0011: brtrue.s IL_0019
IL_0013: ldloc.0
IL_0014: call     void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::UnsafeAwaitAwaiter<class C/Awaiter>(!!0)
IL_0019: ldloc.0
IL_001a: callvirt instance void C/Awaiter::GetResult()
IL_001f: ret
```

`INotifyCompletion`-only awaiters use `AwaitAwaiter<T>` instead. From the design document:
"`ICriticalNotifyCompletion` lowering is always preferred over `INotifyCompletion` lowering, when we statically know
`ICriticalNotifyCompletion` is implemented by the expression."

### 3.6 Helper selection algorithm (verbatim from the Roslyn design document)

> For any `await expr` with where `expr` has type `E`, the compiler will attempt to match it to a helper method in
> `System.Runtime.CompilerServices.AsyncHelpers`. The following algorithm is used:
>
> 1. If `E` has generic arity greater than 1, no match is found and instead move to [await any other type].
> 2. `System.Runtime.CompilerServices.AsyncHelpers` from corelib (the library that defines `System.Object` and has no references) is fetched.
> 3. All methods named `Await` are put into a group called `M`.
> 4. For every `Mi` in `M`:
>    1. If `Mi`'s generic arity does not match `E`, it is removed.
>    2. If `Mi` takes more than 1 parameter (named `P`), it is removed.
>    3. If `Mi` has a generic arity of 0, all of the following must be true, or `Mi` is removed:
>       1. The return type is `System.Void`
>       2. There is an identity or implicit reference conversion from `E` to the type of `P`.
>    4. Otherwise, if `Mi` has a generic arity of 1 with type param `Tm`, all of the following must be true, or `Mi` is removed: [the return type is `Tm`; the generic parameter of `E` is `Te`; `Ti` satisfies any constraints on `Tm`; identity or implicit reference conversion from `E` to the substituted parameter type]
> 5. If only one `Mi` remains, that method is used for the following rewrites. Otherwise, we instead move to [await any other type].
>
> These rules are intended to cover the following types:
> `Task`, or any subtypes of `Task`; `Task<T>`, or any subtypes of `Task<T>`; `ValueTask`; `ValueTask<T>`;
> `ConfiguredTaskAwaitable`; `ConfiguredTaskAwaitable<T>`; `ConfiguredValueTaskAwaitable`;
> `ConfiguredValueTaskAwaitable<T>`; Any future `Task`-like types the runtime would like to intrinsify.

This is implemented in `Binder_Await.cs` (`GetAwaitableExpressionInfo` → local function `tryGetRuntimeAwaitHelper`),
where the algorithm appears verbatim as a comment. Note the asymmetry: the *awaited expression* may be a subtype of
`Task<T>`, but the *enclosing method's own return type* must be exactly one of the four (see §5).

### 3.7 Exception-handler rewriting (unchanged in kind, still required)

Runtime async forbids suspension inside handler blocks, so the compiler still performs the pend-and-rethrow rewrite
it already does for state machines. Verbatim from the design document:

> "Compiler generated async state machines and runtime generated async share some of the same building blocks. Both
> need to have `await`s with in `catch` and `finally` blocks rewritten to pend the exceptions, perform the `await`
> outside of the `catch`/`finally` region, and then have the exceptions restored as necessary."

`await` in a `catch`:

```cs
int pendingCatch = 0;
Exception pendingException;
try { throw new Exception(); }
catch (Exception e) { pendingCatch = 1; pendingException = e; }

if (pendingCatch == 1)
{
    System.Runtime.CompilerServices.AsyncHelpers.Await(C.M());
    throw pendingException;
}
```

`await` in a `finally`:

```cs
Exception pendingException;
try { throw new Exception(); }
catch (Exception e) { pendingException = e; }

System.Runtime.CompilerServices.AsyncHelpers.Await(C.M());

if (pendingException != null) { throw pendingException; }
```

Compound assignments are still spilled to temporaries around the `await` (design document, "Preserving compound
assignments"): `a[C.M2()] += await C.M1();` becomes three temporaries and an explicit `stelem`.

`RuntimeAsyncRewriter.Rewrite` finishes by calling `SpillSequenceSpiller.Rewrite(...)`, the same spiller the state
machine path uses.

### 3.8 Dynamic awaits

Dynamic `await` also uses runtime-async suspension. The compiler keeps the dynamic call sites for the awaited
expression, `GetAwaiter`, `IsCompleted` and `GetResult`, and then:

```cs
System.Runtime.CompilerServices.ICriticalNotifyCompletion critTemp =
    awaiter as System.Runtime.CompilerServices.ICriticalNotifyCompletion;
if (critTemp != null)
    System.Runtime.CompilerServices.AsyncHelpers.UnsafeAwaitAwaiter<ICriticalNotifyCompletion>(critTemp);
else
    System.Runtime.CompilerServices.AsyncHelpers.AwaitAwaiter<INotifyCompletion>((INotifyCompletion)awaiter);
```

The design document states the strategy explicitly and notes "This matches the existing state machine behavior for
dynamic awaits." Dynamic await lowering happens in `RuntimeAsyncRewriter`, not in the binder
(`Binder_Await.cs`: "Runtime async dynamic await lowering is handled in RuntimeAsyncRewriter."). The rewriter
synthesizes its own dynamic call-site container type, named after the current function so that it does not collide
with the container `LocalRewriter` creates for the same method.

### 3.9 Hoisting and local-state rules (ECMA augment)

> "Local variables used across suspension points are considered 'hoisted.' That is, only 'hoisted' local variables
> will have their state preserved after returning from a suspension. By-ref variables may not be hoisted across
> suspension points, and any read of a by-ref variable after a suspension point will produce null. Byref-like structs
> will also not be hoisted across suspension points and will have their default value after a suspension point.
> In the same way, pinning locals may not be 'hoisted' across suspension points and will have `null` value after a
> suspension point."

Roslyn's contribution: `IteratorAndAsyncCaptureWalker.Analyze(..., isRuntimeAsync: true, ...)`. In runtime-async
mode the walker deliberately does *not* hoist ordinary (non-`ref`) parameters, locals and fields, because the runtime
handles them:

```cs
private void CaptureVariable(Symbol variable, SyntaxNode syntax)
{
    if (_isRuntimeAsync)
    {
        switch (variable)
        {
            case ParameterSymbol { RefKind: RefKind.None }:
            case LocalSymbol { RefKind: RefKind.None }:
            case FieldSymbol { RefKind: RefKind.None }:
                // Runtime async only needs to preserve by-ref captures
                return;
        }
    }
    ...
}
```

Also, in debug builds the extra "hoist long-lived locals and parameters" pass is skipped for runtime async
(`if (compilation.Options.OptimizationLevel != OptimizationLevel.Release && !isRuntimeAsync)`), and `this` in a
struct or type-parameter receiver is copied into a synthesized `SynthesizedLocalKind.AwaitByRefSpill` local before
the first `await`. Roslyn comment:

> "This is a struct or a type parameter. We need to replace it with a hoisted local to preserve behavior from
> compiler-generated state machines; `this` is a ref, but results are not observable outside of the method.
> We do this regardless of whether `this` is captured to a ref local, because any usage of `ldarg.0` in these
> scenarios is illegal after the first await."

The runtime's `Continuation` reuse work (Preview 3, dotnet/runtime#125556, #125615) "reuses continuation objects more
aggressively and avoids saving unchanged locals", so the set of values actually spilled to the heap is a JIT decision,
not a compiler one. This is the source of the "locals can be kept on the stack and only spilled if actually live
across an await" property.

### 3.10 Restrictions on async IL (ECMA augment)

Temporary, may be lifted:

* the `tail.` prefix is forbidden;
* the `localloc` instruction is forbidden.

Likely permanent:

* by-ref locals cannot be hoisted across suspension points;
* suspension points may not appear in a handler block (`catch`, `filter`, `finally`, `fault`); they are permitted in the protected `try` block;
* "Only four types will be supported as the return type for 'runtime-async' methods: `System.Threading.Task`, `System.Threading.ValueTask`, `System.Threading.Task<T>`, and `System.Threading.ValueTask<T>`".

### 3.11 Runtime data structures (CoreCLR)

From `src/coreclr/System.Private.CoreLib/src/System/Runtime/CompilerServices/AsyncHelpers.CoreCLR.cs`:

```cs
[Flags]
// Keep in sync with CORINFO_CONTINUATION_FLAGS
internal enum ContinuationFlags
{
    ContinueOnThreadPool = 1 << 0,
    ContinueOnCapturedSynchronizationContext = 1 << 1,
    ContinueOnCapturedTaskScheduler = 1 << 2,
    // This is an await of valueTask.AsTask() ...
    ValueTaskAdaptedToTask = 1 << 3,
    AllContinuationFlags = ContinueOnThreadPool | ContinueOnCapturedSynchronizationContext | ContinueOnCapturedTaskScheduler,
    ExecutionContextIndexFirstBit = 4,    ExecutionContextIndexNumBits = 2,
    ContinuationContextIndexFirstBit = 6, ContinuationContextIndexNumBits = 2,
    ExceptionIndexFirstBit = 8,           ExceptionIndexNumBits = 3,
    // For JIT, the continuation stores space for every possible type of async callee's result.
    ResultIndexFirstBit = 11,             ResultIndexNumBits = 21,
}

// Keep in sync with CORINFO_AsyncResumeInfo in corinfo.h
internal unsafe struct ResumeInfo
{
    public delegate*<Continuation, ref byte, Continuation?> Resume;
    public void* DiagnosticIP;
}

#pragma warning disable CA1852 // "Type can be sealed" -- no it cannot because the runtime constructs subtypes dynamically
internal unsafe class Continuation
{
    public Continuation? Next;
    public ResumeInfo* ResumeInfo;
    public ContinuationFlags Flags;
    public int State;
    // followed by JIT-laid-out data (execution context, continuation context, exception, result)
    // whose offsets are index-encoded in Flags
}
```

Per-thread state:

```cs
[ThreadStatic]
private static RuntimeAsyncAwaitState t_runtimeAsyncAwaitState;
```

holding a sentinel `Continuation`, cached `RuntimeAsyncTaskContinuation` / `ValueTaskSourceContinuation` objects, the
cached `Thread`, and a linked list of `RuntimeAsyncStackState` (which carries `CriticalNotifier`, `Notifier`,
`ValueTaskSourceContinuation`, `TaskContinuation`, an `AwaiterContinuation` function pointer, `AwaiterOffset`,
`LeafExecutionContext` and `LeafSynchronizationContext`).

Important commentary in `ResumeInfo.DiagnosticIP` about how debugging maps back to source:

> "For normal JIT-created continuations this points into the jitted suspension code. For debug codegen the IP
> resolves via an ASYNC native->IL mapping to the IL `AsyncHelpers.Await` (or other async function) call which caused
> the suspension. For optimized codegen the mapping into the root method may be more approximate (e.g. because of
> inlining). For all codegens the offset of DiagnosticsIP matches DiagnosticNativeOffset for the corresponding
> AsyncSuspensionPoint in the debug info."

### 3.12 Two `MethodDesc` variants and thunks

Runtime async introduces, for each affected method, a *sync* (task-returning) entry point and an *async variant*.
From dotnet/runtime#123644 (merged 2026-02-06):

> "Runtime Async methods generate two `MethodDesc` variants with the same metadata token: an async 'thunk'
> (Task-returning adapter) and the actual async method implementation."

Related work:

* Preview 4: "Covariant `Task` → `Task<T>` overrides — when a derived class returns `Task<T>` for a base method that returns `Task`, the runtime now generates a void-returning thunk that bridges the calling convention difference, so virtual dispatch works for both flavors. The same fix landed for NativeAOT" (dotnet/runtime#125900, #126768). Tracking issue: dotnet/runtime#124238.
* Preview 6: "The JIT now compiles a dedicated runtime-async version of a synchronous, task-returning method rather than delegating to it through a thunk" (dotnet/runtime#128384). "The JIT turns the method's tail calls into runtime-async calls and awaits the task that would otherwise have been returned." This means even a *non-`async`* `Task`-returning method can get a JIT-generated async variant.
* Preview 6 bug fix: "Fixed JIT native async compilation incorrectly stripping IL from non-async `Task`-returning methods, which caused failures when those methods were called through async dispatch paths" (dotnet/runtime#129975; follow-ups #129884, #130424 in Preview 7).
* Preview 7: "the internal `RuntimeMethodHandle` for async variants is no longer exposed"; "Ensure we do not try to inline async versions of pinvokes" (dotnet/runtime#129797); "Reenable compiling runtime-async versions of synchronous task-returning methods in crossgen2" (#129474).

### 3.13 Historical note: the superseded "async2" encoding

The original runtimelab experiment
(`dotnet/runtimelab @ feature/async2-experiment`, `docs/design/features/runtime-handled-tasks.md`) used a **completely
different metadata encoding**: a custom modifier on the return type
(`int32 modopt([System.Runtime]System.Threading.Tasks.Task`1)` as "a special encoding of `Task<int>` return type with
additional property of making the method `async2`"), plus a `BindingFlags.Async2Visible` flag to make async variants
visible to reflection. **None of that shipped.** The shipping design is the `MethodImplOptions.Async` flag described
above. Do not use the runtimelab document as a reference for .NET 11 behaviour; it is retained here only to avoid
confusion when it turns up in search results. Its notions of "async variants", runtime-generated thunks between the
`Task`-returning and the async entry points, and `Continuation` objects did carry over conceptually.

---

## 4. Enablement: default, opt-in, or off?

### 4.1 The runtime side is unconditional

* Preview 1: "the CoreCLR support for `RuntimeAsync` is now enabled by default, meaning no environment variables need to be set."
* Preview 4 breaking change: "The `DOTNET_RuntimeAsync` / `UNSUPPORTED_RuntimeAsync` configuration switch is gone. There is no longer a way to disable runtime-async at the runtime level" (dotnet/runtime#125406). The PR description says the knob was removed "since runtime-async is now unconditionally enabled."

So: **the CLR always understands and executes runtime-async IL in .NET 11. There is no runtime config switch and no
`AppContext` switch.**

### 4.2 The compiler side is opt-in, per project, via a compiler feature flag

The only switch is the Roslyn feature flag `runtime-async`, whose value must be exactly the string `"on"`.

`src/Compilers/Core/Portable/CommandLine/Feature.cs`:

```cs
internal static class Feature
{
    ...
    internal const string RuntimeAsync = "runtime-async";
    ...
}
```

`CSharpCompilation.IsRuntimeAsyncEnabledIn` — the single decision point, quoted in full:

```cs
/// <summary>
/// Returns true if this method should be processed with runtime async handling instead
/// of compiler async state machine generation.
/// </summary>
internal bool IsRuntimeAsyncEnabledIn(Symbol? symbol)
{
    if (!Assembly.RuntimeSupportsAsyncMethods)
    {
        return false;
    }

    if (symbol is not MethodSymbol { IsAsync: true } method)
    {
        return false;
    }

    Debug.Assert(ReferenceEquals(method.ContainingAssembly, Assembly));
    Debug.Assert(method.IsDefinition);
    Debug.Assert(method is not Symbols.Metadata.PE.PEMethodSymbol);

    var runtimeAsyncEnabledInMethod = method.RuntimeAsyncMethodGenerationAttributeSetting switch
    {
        ThreeState.True => true,
        ThreeState.False => false,
        _ => Feature(CodeAnalysis.Feature.RuntimeAsync) == "on"
    };

    if (!runtimeAsyncEnabledInMethod)
    {
        return false;
    }

    var methodReturn = method.ReturnType.OriginalDefinition;
    if ((object)methodReturn == LambdaSymbol.ReturnTypeIsBeingInferred)
    {
        // During lambda return type inference we have not yet established whether
        // the return type is Task/ValueTask, so we assume runtime async ...
        return true;
    }

    return ((InternalSpecialType)methodReturn.ExtendedSpecialType) is (
        InternalSpecialType.System_Threading_Tasks_Task or
        InternalSpecialType.System_Threading_Tasks_Task_T or
        InternalSpecialType.System_Threading_Tasks_ValueTask or
        InternalSpecialType.System_Threading_Tasks_ValueTask_T);
}
```

Project-file form (learn.microsoft.com, "What's new in .NET 11 runtime", `ms.date` 2026-08-15, "last updated for
Preview 7"):

```xml
<PropertyGroup>
  <Features>runtime-async=on</Features>
</PropertyGroup>
```

Command line: `csc /features:runtime-async=on ...`.

The same page states plainly: **"Runtime Async is a preview feature. To opt in, add the following property to your
project file"**, and "A `net11.0` project no longer requires `<EnablePreviewFeatures>true</EnablePreviewFeatures>` to
use Runtime Async."

Evolution across previews:

| Milestone | Compiler opt-in required |
|---|---|
| .NET 10 preview | `DOTNET_RuntimeAsync=1` + `EnablePreviewFeatures` + `Features=runtime-async=on` |
| 11 P1 | `<EnablePreviewFeatures>true</EnablePreviewFeatures>` + `<Features>$(Features);runtime-async=on</Features>` |
| 11 P2 | same as P1 |
| 11 P3 | `<Features>runtime-async=on</Features>` only (`[RequiresPreviewFeatures]` gate removed, dotnet/runtime#124488) |
| 11 P4–P7 | `<Features>runtime-async=on</Features>` only |

**There is no `LangVersion` gate.** Runtime async is not a C# language feature; it is a codegen strategy. It has no
entry in `csharplang/proposals`, it is not listed as a C# 15 feature, and it produces no entry in
`docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`. In `docs/Language Feature Status.md` it appears as
"Runtime Async | main | Main feature merged into main in preview | 333fred | jcouv, RikkiGibson" — merged, with no
IDE-impact column entry.

### 4.3 The `UseRuntimeAsync` property is *not* a user-facing SDK property

The Learn page carries this note:

> "The `DOTNET_RuntimeAsync` and `UNSUPPORTED_RuntimeAsync` environment variables that previously controlled
> runtime-async behavior have been removed. To opt out of runtime-async per project, set
> `<UseRuntimeAsync>false</UseRuntimeAsync>` in your project file instead of relying on the environment variable."

This is **misleading**. `UseRuntimeAsync` is defined only in the dotnet/runtime repository's own build, in
`src/libraries/Directory.Build.targets`:

```xml
<!-- Define a shared predicate for platforms that support runtime async. -->
<PropertyGroup Condition="'$(RuntimeAsyncSupported)' == ''">
  <RuntimeAsyncSupported Condition="'$(TargetOS)' != 'browser'
    and '$(TargetOS)' != 'wasi'
    and '$(TargetOS)' != 'android'
    and '$(TargetsAppleMobile)' != 'true'
    and '$(RuntimeFlavor)' != 'Mono'">true</RuntimeAsyncSupported>
</PropertyGroup>

<!-- Enable runtime async for netcoreapp source projects, excluding unsupported platforms and OOB packages. -->
<PropertyGroup Condition="'$(IsNETCoreAppSrc)' == 'true'
  and '$(IsPackable)' != 'true'
  and $([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'net11.0'))
  and '$(UseRuntimeAsync)' != 'false'
  and '$(RuntimeAsyncSupported)' == 'true'">
  <Features>$(Features);runtime-async=on</Features>
</PropertyGroup>
```

Grepping the .NET SDK's own targets (`Microsoft.NET.Sdk.props`, `Microsoft.NET.Sdk.targets`,
`Microsoft.NET.Sdk.CSharp.props`, `Microsoft.NET.Sdk.CSharp.targets`, `Microsoft.NET.Sdk.Common.targets`,
`Microsoft.NET.Sdk.BeforeCommon.targets` in dotnet/sdk `main`) finds **zero** occurrences of `runtime-async` or
`RuntimeAsync`. The SDK does not set the flag for user projects, and setting `UseRuntimeAsync` in a user project has
no effect.

**Conclusion as of Preview 7:** the compiler feature is **off by default for user code** and **on for the .NET
runtime libraries themselves** (which are built inside the dotnet/runtime repository, hence Preview 4's "The runtime
libraries are now built with `runtime-async=on` ... do not contain compiler-generated state machines"). Whether GA
flips the SDK default is not established by any primary source; see open questions. Several secondary sources
("Runtime Async is the default in .NET 11") conflate the runtime libraries' build with user projects.

### 4.4 Platform coverage

* Supported: CoreCLR (JIT); ReadyToRun / crossgen2 (Preview 3, dotnet/runtime#123952, #124203, #125420; inlining unblocked in Preview 4, #125472); NativeAOT (Preview 1 foundation, Preview 3 completion); the CoreCLR interpreter (GC-hole fix #127072); RISC-V64 R2R (fixed in Preview 5, #128066); x86 (frame-pointer fixes #126717, #126915).
* Excluded by the runtime's own build predicate: `browser`, `wasi`, `android`, Apple mobile, and `Mono`.
* The dotnet/runtime epic (#109632) states for .NET 11: "Feature becomes standard; Mono runtime remains unsupported."
* On unsupported platforms `AsyncHelpers` throws `PlatformNotSupportedException`.

### 4.5 Per-method escape hatch

```cs
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method)]
public class RuntimeAsyncMethodGenerationAttribute(bool runtimeAsync) : Attribute();
```

Recognised by Roslyn as
`AttributeDescription("System.Runtime.CompilerServices", "RuntimeAsyncMethodGenerationAttribute", s_signatures_HasThis_Void_Boolean_Only)`,
decoded in `SourceMethodSymbolWithAttributes` into `MethodWellKnownAttributeData.RuntimeAsyncMethodGenerationSetting`
and surfaced as `MethodSymbol.RuntimeAsyncMethodGenerationAttributeSetting` (a `ThreeState`).

It is **not defined in the BCL** (confirmed absent from `src/libraries/System.Runtime/ref/System.Runtime.cs`); the
user must declare it. The design document calls it "an escape hatch for experimentation ... This attribute is not
defined in the BCL ... It may be removed when the feature ships in stable." It overrides the feature flag in both
directions: `true` forces runtime async even without the flag, `false` forces a compiler state machine even with it.

The compiler diagnostic that points at it is `ERR_UnsupportedFeatureInRuntimeAsync` = **CS9328**:

> "Method '{0}' uses a feature that is not supported by runtime async. Opt the method out of runtime async by
> attributing it with 'System.Runtime.CompilerServices.RuntimeAsyncMethodGenerationAttribute(false)'."

### 4.6 The compiler forbids hand-written `MethodImplOptions.Async`

`ERR_MethodImplAttributeAsyncCannotBeUsed` = **CS9330**:

> "'MethodImplAttribute.Async' cannot be manually applied to methods. Mark the method 'async'."
> (resx comment: "'MethodImplAttribute.Async' and 'async' are not localizable.")

From the design document: "The one major exception to this is our handling of the `MethodImplOption.Async`; we do not
let this be applied to user code, and will issue an error if a user tries to do this by hand." This is a real
constraint for a source-generating tool: **never emit `[MethodImpl(MethodImplOptions.Async)]` in generated C# source.**

---

## 5. What still generates a classic compiler state machine

`IsRuntimeAsyncEnabledIn` gates everything, so the exhaustive list follows from it plus the surrounding lowering
pipeline in `MethodCompiler.LowerBodyOrInitializer`:

```cs
BoundStatement bodyWithoutIterators = IteratorRewriter.Rewrite(bodyWithoutLambdas, method, methodOrdinal, ...,
                                                               out IteratorStateMachine iteratorStateMachine);
BoundStatement bodyWithoutAsync;
AsyncStateMachine asyncStateMachine = null;
if (compilationState.Compilation.IsRuntimeAsyncEnabledIn(method))
{
    bodyWithoutAsync = RuntimeAsyncRewriter.Rewrite(bodyWithoutIterators, method, compilationState, methodOrdinal, diagnostics);
}
else
{
    bodyWithoutAsync = AsyncRewriter.Rewrite(bodyWithoutIterators, method, methodOrdinal, ..., out asyncStateMachine);
}
Debug.Assert(iteratorStateMachine is null || asyncStateMachine is null);
stateMachineTypeOpt = (StateMachineTypeSymbol)iteratorStateMachine ?? asyncStateMachine;
```

The identical branch exists in `MethodCompiler.CompileSynthesizedMethods` for lambdas and local functions
(with `const int methodOrdinal = -1`).

A **compiler state machine is still produced** when any of the following holds:

1. **The corlib has no static `AsyncHelpers` class** — targeting `net10.0` or earlier, .NET Framework, or netstandard.
   (`Assembly.RuntimeSupportsAsyncMethods` is false.)
2. **The `runtime-async` feature flag is not `"on"`** — the default for user projects today.
3. **The method's return type is not exactly `Task`, `Task<T>`, `ValueTask` or `ValueTask<T>`.** The check uses
   `method.ReturnType.OriginalDefinition.ExtendedSpecialType`, so this excludes:
   * `async void`;
   * any user type derived from `Task` / `Task<T>` (note the asymmetry with §3.6, where the *awaited* expression may be a subtype);
   * any custom task-like type declared with `[AsyncMethodBuilder(...)]`, and any method carrying a method-level `[AsyncMethodBuilder]` override — these keep `AsyncTaskMethodBuilder`-style lowering entirely (the design document: "Any method returning a different `Task`-like type is not transformed to runtime async form and uses a C#-generated state machine");
   * `IAsyncEnumerable<T>` / `IAsyncEnumerator<T>`.
4. **Async iterators.** `IteratorRewriter` runs first and unconditionally; the enclosing method returns
   `IAsyncEnumerable<T>` / `IAsyncEnumerator<T>`, so rule 3 excludes it. Corroborating evidence: Roslyn's
   `docs/Language Feature Status.md` lists a *separate, still-in-progress* feature row
   "**Runtime Async Streams**" on branch `features/runtime-async-streams` (owner jcouv; champions 333fred,
   RikkiGibson), while plain "Runtime Async" is already merged into `main`. The design document also carries open
   TODOs: "Go over `IAsyncEnumerable` and confirm that the initial rewrite to a `Task`-based method produces code that
   can then be implemented with runtime async, rather than a full compiler state machine" and "TODO: Async iterators
   (returning `IAsyncEnumerable<T>`)".
5. **`[RuntimeAsyncMethodGenerationAttribute(false)]`** on the method.
6. **Methods using a construct runtime async cannot express** (CS9328). The `tail.` prefix and `localloc` are
   forbidden in async IL, and by-ref / `ref struct` / pinned locals cannot live across a suspension point.
7. **Runtime-side opt-outs.** Preview 6: "Methods that are already pooled opt out of runtime-async, avoiding
   redundant work" (dotnet/runtime#128943) — a JIT/VM decision for pooled `ValueTask` methods, not a compiler one.
8. **Mono, browser, wasi, android, Apple mobile** — the runtime does not support it; the runtime's own libraries are
   not built with the flag there, and `AsyncHelpers` throws.

**Async lambdas and async local functions ARE converted** when their (inferred) return type is one of the four.
`IsRuntimeAsyncEnabledIn` explicitly handles `LambdaSymbol.ReturnTypeIsBeingInferred` by optimistically assuming
runtime async during inference and busting the binding cache if the inferred type turns out not to be Task-like.

**Async `Main` changes independently of the flag.** `SynthesizedEntryPointSymbol.AsyncForwardEntryPoint` now prefers
`AsyncHelpers.HandleAsyncEntryPoint(Task)` / `HandleAsyncEntryPoint(Task<int>)` over the historical
`GetAwaiter().GetResult()` whenever the API exists in corlib — i.e. for any `net11.0` compilation, regardless of
`runtime-async`. Roslyn comment: "Try to use the new `HandleAsyncEntryPoint` API if it exists". The field is
documented as "Either a call to AsyncHelpers.HandleAsyncEntryPoint or a call to GetAwaiter().GetResult() on the
user-defined main method."

---

## 6. Stack traces, debugging, and observable behaviour

### 6.1 Live stack traces

The headline change. From the Learn page and the Preview 2 notes, for three nested `async Task` local functions
awaiting each other:

Without `runtime-async` — 13 frames:

```
   at Program.<<Main>$>g__InnerAsync|0_2() in Program.cs:line 24
   at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[TStateMachine](...)
   at Program.<<Main>$>g__InnerAsync|0_2()
   at Program.<<Main>$>g__MiddleAsync|0_1() in Program.cs:line 14
   at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[TStateMachine](...)
   at Program.<<Main>$>g__MiddleAsync|0_1()
   at Program.<<Main>$>g__OuterAsync|0_0() in Program.cs:line 8
   at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[TStateMachine](...)
   at Program.<<Main>$>g__OuterAsync|0_0()
   at Program.<Main>$(String[] args) in Program.cs:line 3
   at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[TStateMachine](...)
   at Program.<Main>$(String[] args)
   at Program.<Main>(String[] args)
```

With `runtime-async` — 5 frames:

```
   at Program.<<Main>$>g__InnerAsync|0_2() in Program.cs:line 24
   at Program.<<Main>$>g__MiddleAsync|0_1() in Program.cs:line 14
   at Program.<<Main>$>g__OuterAsync|0_0() in Program.cs:line 8
   at Program.<Main>$(String[] args) in Program.cs:line 3
   at Program.<Main>(String[] args)
```

Important caveat, quoted from the Learn page:

> "Exception stack traces (from `catch (Exception ex)`) already look the same with or without Runtime Async, because
> existing `ExceptionDispatchInfo` cleanup in compiler-generated code handles that case. The improvement is in what
> you see *during* live execution."

So the difference shows up in `new StackTrace()`, profilers, diagnostic logging and the debugger Call Stack window —
not in `Exception.StackTrace`. Preview 2 also notes "methods are named `$>g__` because they are local functions, not
because they are async". There is no longer a `<M>d__N` state machine type and therefore no `MoveNext` frame.

### 6.2 Debugger

* Preview 2: "Breakpoints now bind correctly inside runtime-async methods, and the debugger can step through `await` boundaries without jumping into compiler-generated infrastructure." Implemented by dotnet/runtime#123644, merged 2026-02-06, "which teaches the debugger to recognize async thunks and map them back to the original source locations."
* That PR adds an `AsyncThunkStubManager`. Breakpoints no longer bind to async thunks unless explicitly targeted by `MethodDesc`, because thunks "lack proper debug metadata". Stepping traces through the thunk to the real implementation: when a step lands on a thunk, the debugger "patches the real target and continues execution instead of stopping in the thunk". Diagnostics-hidden methods (async thunks, IL stubs) are filtered out of debugger stack walks.
* Preview 4 fixed an "x86 runtime-async frame pointer mismatch in `GetSpForDiagnosticReporting` and DBI generic-type resolution" (#126717, #126915), and made the DAC `GetNativeCodeInfo` API "use the async variant for thunk `MethodDesc`s" (#126728).
* Suspension points appear in debug info as `AsyncSuspensionPoint` records with a `DiagnosticNativeOffset` that matches `ResumeInfo.DiagnosticIP` (see §3.11). In debug codegen the mapping resolves back to the IL `AsyncHelpers.Await` call; in optimized codegen the mapping "may be more approximate (e.g. because of inlining)".
* The Roslyn design document still has an open item: "TODO: Clarify with the debugger team where NOPs need to be inserted for debugging/ENC scenarios. We will likely need to insert AwaitYieldPoint and AwaitResumePoints for the scenarios where we emit calls to `AsyncHelpers` async helpers, but can we avoid them for calls in runtime async form?"

### 6.3 Profiling / diagnostics

Preview 7 and the Learn page: "The async profiler now instruments both runtime-async methods and compiler-generated
async state-machine methods, so tools receive one consistent event model regardless of async implementation style"
(dotnet/runtime#129043, with follow-ups #129801, #130297, #130299, #130877 covering pooled `ValueTask` methods and
custom awaiters, and eliminating the per-suspension dispatcher allocation).

### 6.4 `ExecutionContext`, `SynchronizationContext`, `AsyncLocal<T>`

**No semantic change is documented, and the implementation preserves the existing semantics.** Every leaf await
helper calls `state.CaptureContexts()` before suspending:

```cs
public void CaptureContexts()
{
    // CaptureContext is called from leaf await helpers. We either just started a runtime async chain
    // (from a thunk), or we came from DispatchContinuations (on resumption).
    Thread? curThread = CurrentThread;
    Debug.Assert(curThread != null);
    Debug.Assert(StackState != null);
    // Here we get the execution context for presenting to the notifier,
    // not for flowing across suspension to potentially another thread.
    // Therefore we do not need to worry about IsFlowSuppressed
    StackState->LeafExecutionContext = curThread._executionContext;
    StackState->LeafSynchronizationContext = curThread._synchronizationContext;
}
```

and an `AsyncContexts` struct restores both on the way out:

```cs
public void Pop(Thread thread)
{
    // The common case is that these have not changed, so avoid the cost of a write barrier if not needed.
    if (_synchronizationContext != thread._synchronizationContext)
    {
        // Restore changed SynchronizationContext back to previous
        thread._synchronizationContext = _synchronizationContext;
    }

    ExecutionContext? currentExecutionCtx = thread._executionContext;
    if (_executionContext != currentExecutionCtx)
    {
        ExecutionContext.RestoreChangedContextToThread(thread, _executionContext, currentExecutionCtx);
    }
}
```

`ContinuationFlags` encodes `ContinueOnThreadPool`, `ContinueOnCapturedSynchronizationContext` and
`ContinueOnCapturedTaskScheduler`, plus an index into the continuation's data for a stored `ExecutionContext`
(`ExecutionContextIndexFirstBit`), so `ConfigureAwait(false)` and `ConfigureAwait(true)` behave as before, and
`AsyncLocal<T>` flows as before.

The one *performance* change is Preview 6's dotnet/runtime#128323:

> "Async continuations can now opt out of `ExecutionContext` capture and restore. `ExecutionContext` carries ambient
> state — such as `AsyncLocal<T>` values — across `await` points. Every `Task` continuation previously captured a
> snapshot of the context and restored it before running, even when no `AsyncLocal<T>` state was in use and the
> restore was a no-op. The runtime now detects when a continuation has nothing to restore and skips the
> capture/restore cycle entirely. `Task`, `Task<T>`, `ValueTask`, and `ValueTask<T>` all benefit from this change, as
> does the runtime-async implementation path."

Note this optimisation applies to **both** async models, not only runtime async, so it is not a runtime-async
behaviour difference.

Preview 7 additionally fixed a correctness bug in this area: "`await` also now correctly saves and restores async
contexts for `ValueTask`-returning methods; a flag check bug was causing this to be skipped"
(dotnet/runtime#129890).

Thread statics: the runtime-async machinery itself uses `[ThreadStatic] t_runtimeAsyncAwaitState`, but this is
internal bookkeeping. No documented change to how *user* `[ThreadStatic]` fields behave across `await` (they were
never flowed, and still are not).

### 6.5 Performance summary (Preview 5 → 7)

* On-stack replacement: resuming directly into optimized code rather than taking the general OSR transition path; "the transition overhead was around 10-20x, and the sample suspension-heavy benchmark improved from `Took 6357.1 ms` to `Took 457.1 ms`" (dotnet/runtime#127074).
* Continuation reuse when an `IValueTaskSource`-backed `ValueTask` suspends (#127973), removing an allocation.
* Tail-merged suspension points (#128559); cached continuations for callable task thunks (#128320).
* Tiered compilation for async versions (#129985): "Async methods previously bypassed tiering and always ran the tier0 code ... Under load this showed up in traces as a large amount of allocation until everything warmed up."
* JIT inlining of `AsyncHelpers.TransparentAwait` (#130482): a loop awaiting an already-completed `Task` 100,000,000 times went from ~191 ms to ~32 ms.
* Task/ValueTask factory intrinsics (#129810): `Task.FromResult`, `Task.CompletedTask`, `ValueTask.FromResult`, `ValueTask.CompletedTask`, `default(ValueTask)`, `new ValueTask()`, `new ValueTask<T>(T)`; plus Task↔ValueTask adapter unwrapping (#130081).
* Implicit tailcalls re-enabled from async methods (#129255): a dispatcher `return b ? Bar() : Baz();` dropped "from 52 bytes and 19 instructions to 20 bytes and 6 instructions, with both branches becoming `tail.jmp`".
* `await Task.Yield()` no longer allocates the thread-pool box (#130170): 10,000,000 iterations went from ~723 ms to ~534 ms, "matching the compiler-generated state-machine time".
* JIT prolog restriction lifted: "The JIT no longer requires the function prolog to fit in a single instruction group (IG). Complex prologues with many saved registers, large stack allocations, or runtime-async state setup no longer trigger fallback paths."
* Bug fixes worth knowing about: missing GC write barrier when writing an async method return into the continuation object (#126721); GC hole when resuming a continuation via the interpreter (#127072); async resumption stub mishandling `byref` parameters (#130022 / #129999); race leak in runtime-async resumption stubs (Preview 3).

---

## 7. Is anything observable at the source or symbol level?

### 7.1 Syntax: nothing

There is no new syntax node, no new `SyntaxKind`, no new modifier, no new contextual keyword, no grammar change.
`docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md` contains **no runtime-async entry** at all (its
entries are collection-expression safe-context, `InAttribute` requirements, dynamic `&&`/`||` on interfaces,
`nameof(this.)`, `with` parsing in switch arms, pointer types outside `unsafe`, and the `safe` / `closed` / `union`
contextual keywords). A source rewriter that emits ordinary `async`/`await` C# needs no changes.

The Roslyn design document opens with the explicit statement:

> "In general, we try to avoid exposing this feature at the user level; initial binding is almost entirely
> unaffected by runtime async. Exposed symbols do not give direct information about whether they were compiled with
> runtime async, and indeed the compiler has no idea whether a method from a referenced assembly is compiled with
> runtime async or not."

### 7.2 Symbols: nothing new on `IMethodSymbol`

* `IMethodSymbol.IsAsync` is unchanged: `true` for a source method declared `async`, and
  `public override bool IsAsync => false;` unconditionally for `PEMethodSymbol` (metadata methods). Verified in
  `src/Compilers/CSharp/Portable/Symbols/Metadata/PE/PEMethodSymbol.cs`.
* There is no public API exposing `MethodImplAttributes` on `IMethodSymbol`, and `MethodImplAttribute` is a
  pseudo-custom attribute, so it does not appear in `GetAttributes()`. A referenced assembly's runtime-async-ness is
  invisible to the symbol API by design.
* No new well-known attribute is emitted onto user declarations.

### 7.3 Semantic model: two additive surfaces

**(a) `AwaitExpressionInfo.RuntimeAwaitMethod`** — property on
`Microsoft.CodeAnalysis.CSharp.AwaitExpressionInfo`, returned by
`CSharpExtensions.GetAwaitExpressionInfo(SemanticModel, AwaitExpressionSyntax)`. Verbatim XML documentation:

```cs
/// <summary>
/// When runtime async is enabled for this await expression, this represents either:
/// <list type="bullet">
/// <item>
/// A call to <c>System.Runtime.CompilerServices.AsyncHelpers.Await</c>, if this is a
/// supported task type. In such cases, <see cref="GetAwaiterMethod" />,
/// <see cref="IsCompletedProperty" />, and <see cref="GetResultMethod" /> will be
/// <see langword="null" />.
/// </item>
/// <item>
/// A call to <c>System.Runtime.CompilerServices.AsyncHelpers.AwaitAwaiter|UnsafeAwaitAwaiter</c>.
/// In these cases, the other properties may be non-<see langword="null" /> if the
/// the rest of the await expression is successfully bound.
/// </item>
/// </list>
/// </summary>
public IMethodSymbol? RuntimeAwaitMethod { get; }
```

This is the one place where **binding does change**. For `await someTask` in a runtime-async method, the compiler
short-circuits: `GetAwaitableExpressionInfo` tries `tryGetRuntimeAwaitHelper` *first* and returns immediately on a
match, so `GetGetAwaiterMethod` / `GetIsCompletedProperty` / `GetGetResultMethod` are never called, and
`AwaitExpressionInfo.GetAwaiterMethod`, `.IsCompletedProperty` and `.GetResultMethod` are all `null`. Any analyzer
or rewriter that reads those three properties, or that relies on `TaskAwaiter`-related symbols being referenced from
the compilation, must tolerate `null`. `BindAwait` itself now reads:

```cs
TypeSymbol awaitExpressionType = (info.GetResult ?? info.RuntimeAsyncAwaitCall?.Method)?.ReturnType
    ?? (hasErrors ? CreateErrorType() : Compilation.DynamicType);
```

The property is already **shipped** (it is not in `src/Compilers/CSharp/Portable/PublicAPI.Unshipped.txt`), so it
exists in the Roslyn that ships with the .NET 10 SDK / Visual Studio 2026 as well.

`IAwaitOperation` (IOperation) exposes only `Operation`; nothing was added there.

**(b) `RuntimeCapability.RuntimeAsyncMethods`** — new member of the public
`Microsoft.CodeAnalysis.RuntimeCapability` enum, queried through
`Compilation.SupportsRuntimeCapability(RuntimeCapability)`:

```cs
/// <summary>
/// Indicates that this version of the runtime supports generating async state machines.
/// </summary>
RuntimeAsyncMethods = 9,
```

It is backed by `AssemblySymbol.RuntimeSupportsAsyncMethods`, i.e. the presence of a static `AsyncHelpers` class in
corlib. It reports what the *target framework* can do, not whether the flag is on for this compilation.

### 7.4 Diagnostics a source rewriter can trip

| ID | Roslyn `ErrorCode` | Message |
|---|---|---|
| CS9328 | `ERR_UnsupportedFeatureInRuntimeAsync` | "Method '{0}' uses a feature that is not supported by runtime async. Opt the method out of runtime async by attributing it with 'System.Runtime.CompilerServices.RuntimeAsyncMethodGenerationAttribute(false)'." |
| CS9330 | `ERR_MethodImplAttributeAsyncCannotBeUsed` | "'MethodImplAttribute.Async' cannot be manually applied to methods. Mark the method 'async'." |

### 7.5 Practical conclusion for a source-level rewriter

If the tool rewrites C# **source** (or bound trees before lowering) and hands the result to Roslyn, runtime async is
applied *afterwards* by `MethodCompiler` / `RuntimeAsyncRewriter` and requires **no action**. The specific things to
check are:

* `AwaitExpressionInfo.GetAwaiterMethod` / `.IsCompletedProperty` / `.GetResultMethod` may now be `null` — do not
  assume non-null when the compilation has `runtime-async=on`.
* Do not emit `[MethodImpl(MethodImplOptions.Async)]` in generated source (CS9330).
* Do not assume the presence of a `<M>d__N` state machine type, an `[AsyncStateMachine]` attribute, an
  `IAsyncStateMachine` implementation, or an `AsyncTaskMethodBuilder` field when reading back a compiled assembly —
  in particular when reading the .NET 11 framework assemblies, which are all built with the flag.
* If the tool synthesizes an `async` method whose return type is a *custom* task-like, or an async iterator, it will
  keep getting a compiler state machine, so mixed behaviour within one assembly is normal and expected.
* If the tool relies on `Task`/`ValueTask` return-type conventions when reading IL, revisit §3.3.
* Debug-build local hoisting differs (§3.9), so tools that reason about which locals survive an `await` in a debug
  build will see fewer hoisted locals.

---

## 8. Interaction with source generators, IL rewriters, and compiler replacements

### 8.1 Source generators

Transparent. A generator adds `SyntaxTree`s to the compilation; those trees are bound and lowered by the same
compiler with the same `Features` value, so generated `async Task` methods become runtime-async on exactly the same
terms as hand-written ones. Nothing in the generator API is affected. (Unrelated but new in the same wave:
`IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput` /
`IncrementalGeneratorOutputKind.PreCompilation`, gated behind `RSEXPERIMENTAL007`.)

### 8.2 Compiler replacements / bound-tree-level tools

Also transparent, provided the tool produces C# syntax (or bound nodes) and lets Roslyn do the lowering. Runtime
async lives strictly downstream, in `MethodCompiler.LowerBodyOrInitializer`, after closure conversion and iterator
rewriting and before code generation.

The one genuine coupling is that the tool must not itself perform the async lowering. If a tool lowered
`async`/`await` into its own state machine before Roslyn saw it, it would produce a non-`async` method and runtime
async would simply not apply — correct, but forfeiting the feature and producing IL inconsistent with the rest of the
assembly. (In .NET 11 the JIT can still create an async variant of a plain `Task`-returning method, per Preview 6, so
the caller side would still benefit somewhat.)

### 8.3 IL rewriters (post-compile)

**This is where the feature breaks things.** A post-compile IL rewriter (Fody, Mono.Cecil-based tools, ILRepack,
obfuscators, coverage instrumenters, AOP frameworks that weave IL) will encounter, in any assembly compiled with
`runtime-async=on` — including **every .NET 11 runtime library**:

1. `MethodDef` rows with `MethodImplAttributes` bit `0x2000` set. Older metadata libraries do not know this bit; some validate the flags mask and will reject it. `ilasm`/`ildasm` spell it as the `async` keyword.
2. Method bodies whose `ret` type does not match the signature return type (§3.3). Naive IL verification fails; naive "wrap the body and return the original value" weaving produces invalid IL.
3. No `IAsyncStateMachine` type, no `AsyncTaskMethodBuilder` field, no `[AsyncStateMachine]` attribute, no `MoveNext`. Tools that locate the state machine in order to instrument `await` boundaries find nothing.
4. `call`s to `System.Runtime.CompilerServices.AsyncHelpers.*`, which per the ECMA augment "are only legal to call inside async methods". Moving such a `call` into a helper method, or inlining an async body into a non-async method, produces invalid IL.
5. A prohibition on introducing the `tail.` prefix or `localloc` into an async method body.
6. Two `MethodDesc` variants per metadata token at runtime (thunk + async variant), which affects profiler `FunctionID` handling, rejit, and DAC/ICorDebug consumers.

The compiler-side counterpart is `[BypassReadyToRun]` on the helpers and the crossgen2 work in Preview 4; nothing
comparable exists for third-party rewriters. Any such tool needs explicit support for the 0x2000 flag before it can
process .NET 11 framework assemblies or user assemblies built with the flag.

### 8.4 ILAsm / ILDasm / Reflection.Emit

Per the ECMA augment: "The flag is represented in IL by the `async` keyword. Tools like `ilasm` and `ildasm`
recognize this flag." IL assembly/disassembly support was listed as ".NET 10 code complete" in the epic
(dotnet/runtime#109632), along with reflection/introspection support and dynamic-method (`Reflection.Emit`) support.

---

## 9. Timeline of the feature

| Milestone | State |
|---|---|
| .NET 9 | Experiment (`dotnet/runtimelab @ feature/async2-experiment`, dotnet/runtime#94620). Different design: `modopt` return-type encoding, `BindingFlags.Async2Visible`. Superseded. |
| .NET 10 | "Available for local testing and experimentation." Requires `net10.0` + `EnablePreviewFeatures` + `Features=runtime-async=on` + `DOTNET_RuntimeAsync=1`. Runtime + compiler integration, reflection, ilasm/ildasm, `Reflection.Emit` all "code complete". Public API approved (dotnet/runtime#114310); `AsyncHelpers` marked `[Experimental("SYSLIB5007")]`. |
| .NET 11 P1 | CoreCLR support on by default (no env var). NativeAOT foundation. Runtime libraries **not** yet built with it. |
| .NET 11 P2 | Live stack traces; debugger breakpoint/stepping support (dotnet/runtime#123644). |
| .NET 11 P3 | `[RequiresPreviewFeatures]` removed (#124488). R2R and NativeAOT support land (#123952, #124203, #125420). Continuation reuse and unchanged-local elision (#125556, #125615). |
| .NET 11 P4 | **Runtime libraries built with `runtime-async=on`.** `DOTNET_RuntimeAsync`/`UNSUPPORTED_RuntimeAsync` removed (#125406). Covariant `Task`→`Task<T>` thunks (#125900, #126768). crossgen2 inlining unblocked (#125472). |
| .NET 11 P5 | OSR resumption directly into optimized code (#127074); `IValueTaskSource` continuation reuse (#127973); RISC-V64 R2R fix (#128066). |
| .NET 11 P6 | JIT compiles a dedicated async version instead of a thunk (#128384); tail-merged suspension points (#128559); cached continuations (#128320); pooled methods opt out (#128943); `ExecutionContext` capture elision (#128323). |
| .NET 11 P7 | Tiered compilation for async versions (#129985); `TransparentAwait` inlining (#130482); Task/ValueTask factory intrinsics (#129810, #130081); implicit tailcalls (#129255); `Task.Yield()` allocation elision (#130170); uniform async profiler events (#129043). |
| .NET 11 GA (Nov 2026) | Documented at Preview 7 as a **preview feature, opt-in per project via `<Features>runtime-async=on</Features>`**, with the runtime libraries themselves already using it. See open questions. |

---

## 10. Open questions / things I could not establish from primary sources

1. **Will the .NET 11 SDK enable `runtime-async=on` by default for user projects at GA?** As of Preview 7 it does
   not (zero occurrences of `runtime-async` in dotnet/sdk targets), and the Learn page still says "Runtime Async is a
   preview feature. To opt in, ...". Several secondary sources claim it is "the default in .NET 11", which is only
   true of the .NET runtime libraries' own build. Unresolved for GA.
2. **Where exactly is CS9328 (`ERR_UnsupportedFeatureInRuntimeAsync`) reported, and what is the full list of
   constructs that trigger it?** The error code and message exist in `ErrorCode.cs` / `CSharpResources.resx`, but I
   could not locate the reporting site in the files I fetched (not in `MethodCompiler.cs`, `CodeGenerator.cs`,
   `RuntimeAsyncRewriter.cs`, `SpillSequenceSpiller.cs`, `Binder_Await.cs`, `Binder_Statements.cs`,
   `Binder_Expressions.cs`, `LocalRewriter.cs`, `IteratorAndAsyncCaptureWalker.cs`). GitHub code search requires
   authentication and grep.app is behind a bot check.
3. **Is `[AsyncStateMachine]` definitely absent from runtime-async methods?** Very strongly implied (there is no state
   machine type to name, `stateMachineTypeOpt` is null on that path, and the Learn stack-trace sample shows no builder
   frames), but I did not find the emission site in Roslyn to quote directly.
4. **Edit-and-Continue / Hot Reload behaviour.** No primary source describes rude-edit rules for runtime-async
   methods. The Roslyn design document carries an unresolved TODO about NOP placement for "debugging/ENC scenarios",
   and `src/Features/Core/Portable/EditAndContinue/AbstractEditAndContinueAnalyzer.cs` contains no runtime-async
   handling.
5. **Reflection surface of async variants.** Preview 7 says "the internal `RuntimeMethodHandle` for async variants is
   no longer exposed", but I found no positive statement about whether `Type.GetMethods()` can ever return the async
   variant, or how `MethodInfo.Invoke` dispatches. (The superseded runtimelab design had `BindingFlags.Async2Visible`;
   that did not ship.)
6. **Async streams.** The `features/runtime-async-streams` Roslyn branch exists and is tracked under the same test
   plan issue (dotnet/roslyn#75960), but I found no statement about whether any part of it lands in .NET 11 GA or is
   deferred.
7. **`RuntimeFeature.Async`.** The ECMA augment requires it for the "async-capable assembly" test, but it is absent
   from the shipped BCL and Roslyn uses the presence of `AsyncHelpers` instead. Either the spec will be amended or the
   constant will be added; unresolved.
8. **Derived `Task` types after generic substitution.** The spec note says the rules "operate before generic
   substitution"; Roslyn uses `ReturnType.OriginalDefinition.ExtendedSpecialType`, which excludes user types derived
   from `Task<T>`. I did not find a test confirming that case explicitly.
9. **Whether `MethodImplOptions.Async` interacts with `[MethodImpl(MethodImplOptions.NoInlining)]` or
   `AggressiveInlining` in user code compiled with the flag.** The BCL helpers combine `Async | NoInlining`, so the
   combination is at least legal, but no rule is documented for user methods.
