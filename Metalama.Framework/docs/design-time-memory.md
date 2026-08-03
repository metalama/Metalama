# Design-Time Memory: What May Hold a Compilation

This document describes the rules that keep the design-time code from accumulating memory, and the mechanisms the
codebase provides to follow them. It exists because a violation of these rules does not fail a test, does not throw,
and does not degrade anything measurably on a small project. It surfaces days later as a report that Visual Studio
consumes several gigabytes after a few hours of editing, on a solution nobody on the team has.

Read this before adding a field, a cache, a background task or an event handler to `Metalama.Framework.DesignTime`,
and before storing anything derived from the code model in an object that outlives a single request.

## Why design time is different

At compile time the process handles one compilation and exits. Retaining it costs nothing.

At design time the process is the Roslyn out-of-process analysis service, which lives for as long as the solution is
open, and Roslyn hands us a **new** `Compilation` on essentially every keystroke. It releases the previous one as
soon as the components it hosts do. Whatever we still reference, it cannot release.

The unit of the leak is therefore not a byte or a node, it is a version of the project. A single retained
`Compilation` pins:

- every `SyntaxTree` of the project, and therefore the full text and the full green node tree of every file;
- the symbol tables built from it, and the metadata symbol tables of every referenced assembly;
- its `SemanticModel` cache, and the `CompilationContext` and `RefFactory` caches Metalama built on top of it.

Tens of megabytes on a medium project. Retain one per keystroke and the arithmetic explains the reports exactly.

## The rule

> An object that outlives a single request must not hold a strong reference to a `Compilation`, a `SyntaxTree`, a
> `SemanticModel`, an `ISymbol`, a `CompilationModel`, a `PartialCompilation`, or anything that transitively reaches
> one, with the single exception of the most recent version of the project.

The exception is what makes the design-time pipeline possible at all: the pipeline serves the results of the version
it has most recently analysed, so it must keep that version. One version. Not two, and above all not one per edit.

It is useful to classify every field by the lifetime of the object that declares it.

| Scope of the declaring object | May hold a compilation-bound object? |
|---|---|
| A single request (locals, parameters, a `PartialCompilation` being processed) | Yes, freely. |
| The current version of a project (`PipelineState.ProjectVersion`) | Yes, exactly one version. |
| A project (`DesignTimeAspectPipeline`, a `ProjectSourceGenerator`) | No. |
| The pipeline configuration (`AspectPipelineConfiguration` and everything it reaches) | No: it is long-lived by design, reused across keystrokes. |
| An ambient `UserCodeExecutionContext` | Yes: it is scoped to one execution of user code. |
| A per-file result (`SyntaxTreePipelineResult` and everything it holds) | No: it survives every run in which its file is not dirty. |
| The process (a static field, an `IGlobalService`, an analyzer or generator instance) | No. |

The last row deserves emphasis: Roslyn keeps **one instance** of a `DiagnosticAnalyzer` or an `IIncrementalGenerator`
alive for the lifetime of the process. Any instance field on such a type that accumulates is a process-lifetime leak.

## The mechanisms

### Key results by path, not by instance

`DesignTimeAspectPipelineResult.SyntaxTreeResults` is an `ImmutableDictionary<string, SyntaxTreePipelineResult>` keyed
by **file path**. The number of entries is therefore bounded by the number of files, and a new version of a file
replaces the entry of the old one rather than adding to it. A dictionary keyed by `SyntaxTree` would be bounded by
the number of edits instead.

The same reasoning applies to `ProjectVersion.SyntaxTrees` and to the dependency graph, which store paths.

### Make persisted references durable

A reference obtained from the code model is bound to the compilation it came from: it holds the `ISymbol` and it
holds the `RefFactory`, which reaches `CompilationContext.Compilation`. Such a reference is correct and fast inside a
request, and is a retention as soon as it is stored.

`IRef<T>.ToDurable()` converts it to an `IDurableRef<T>`, backed by a `SerializableDeclarationId` or a
`SerializableTypeId`, which reaches nothing. Resolve it later with `GetTarget( compilation )` against whichever
compilation is current.

This is why `SyntaxTreePipelineResult` documents itself as *compilation-independent and cacheable*: the pipeline
re-analyses only the syntax trees that changed and carries the results of every other file forward from an earlier
run, so a compilation-bound reference in one of them pins the version it was computed in for as long as the project
stays open. `InheritableAspectInstance` and `FabricDriver` both convert their target declaration for this reason.

When adding a member to a type that is stored in a `SyntaxTreePipelineResult`, the question to ask is not "is this
convenient to keep" but "does this reach a `Compilation`". Note that a `Microsoft.CodeAnalysis.Diagnostic` does: it
holds a `Location`, which holds its source tree.

### Understand `ConditionalWeakTable` before relying on it

`WeakCache<TKey, TValue>` wraps a `ConditionalWeakTable`, and the diff subsystem is built on it. The semantics are
ephemeron semantics, and they are easy to get subtly wrong:

- The table does **not** keep its keys alive. A compilation used only as a key is collectable.
- The table keeps a value alive **only while its key is alive**.
- A value may reference **its own key** freely. That is not a leak, and the garbage collector resolves it correctly.
  `ProjectVersionProvider`'s cache relies on this: the `ChangeList` stored for a compilation holds a `ProjectVersion`
  whose `Compilation` is that same compilation.
- A value that references a **different** object which is itself a key elsewhere extends that object's lifetime, and
  the reasoning repeats from there. A value that reaches an *older* compilation therefore keeps the whole history
  alive for as long as the newest entry is alive. This is the failure mode to look for in any cache keyed by
  `Compilation`.

### Hold "last known" pointers weakly, and say so

Where a component wants the previous version only as an optimisation, it holds it weakly and tolerates its absence.
The codebase marks such members with a `Dangerous` suffix, which is a warning to the reader that the value may be
gone and that the caller is responsible for establishing that it is not:

- `ProjectVersionProvider.Implementation._lastCompilationPerProject` is a
  `Dictionary<ProjectKey, WeakReference<Compilation>>`.
- `CompilationChanges.OldProjectVersionDangerous` and `ReferencedProjectChange.OldCompilationDangerous` are backed by
  `WeakReference`.

Prefer this over a strong reference whenever the value is a cache rather than a result.

### Do not let a background task outlive what it captured

This is the trap that costs the most, because the captured object is invisible in the source: a lambda that mentions
`compilation` produces a closure object holding it, and whatever holds the delegate holds the compilation.

Two rules follow.

**A queue of pending work must remove an entry on every path that ends the work, including cancellation.** The
canonical mistake is `Task.Run( action, cancellationToken )`: when the token is already signalled at the moment the
thread pool would invoke the delegate, the runtime **does not invoke the delegate at all** and the task goes straight
to the canceled state. Any bookkeeping written inside that delegate, such as a `finally` that removes the entry from
a dictionary, never runs. Observe the token from **inside** the delegate instead, so that the delegate always runs
and its `finally` always executes. `TaskBag.Enqueue` does this.

**A `TaskCompletionSource` that is never completed retains the continuation of its awaiter**, and that continuation is
the suspended state machine of the awaiting method, holding every parameter and local of that method. So a wait must
be endable: register the cancellation for the whole duration of the `await`, not merely for the statement that
publishes the source, and use `TrySetCanceled` rather than `SetCanceled` because the source may already have been
completed by the event being waited for.

### Unsubscribe, or subscribe weakly

An event handler holds its target. `AnalysisProcessEventHub` exposes its high-traffic events through
`WeakEvent<T>` and `AsyncWeakEvent<T>`, which hold subscribers weakly and compact their list. Where a plain event is
used instead, the subscriber must unsubscribe in `Dispose`, and the event must carry only values, such as a
`ProjectKey`, rather than compilation-bound objects.

## What the pipeline stores for longer than one request

Three collections outlive a request without being obviously long-lived, because they belong to objects a reader
naturally takes to be per-run. Each of them is a place where the rule has been broken.

**The pipeline configuration.** `AspectPipelineConfiguration` is built once, from whichever version of the project was
current at the time, and `PipelineState` then reuses it for every subsequent version, discarding it only when the
compile-time code changes. **That long life is deliberate and is not the defect.** Rebuilding the configuration on
every keystroke would mean recompiling the compile-time assemblies each time, which is far too slow to be done between
one keystroke and the next; the reuse is what makes the design-time pipeline viable and it must stay.

What follows from it is a constraint, not a licence: because the configuration is long-lived, **it must not pin a
compilation**. For a session in which the user edits run-time code alone, anything it holds that is bound to a
compilation retains the *first* version of the session, the oldest rather than the most recent, and holds it in
addition to the current one. The exception in the rule above does not cover that. Everything reachable from the
configuration is therefore subject to the rule, including `FabricsContributors`, which reaches the amender of every
static fabric and, through the amender, the queries it owns. `FabricMemoryLeakTests` guards this.

The corollary is worth stating separately, because it is where the rule is easiest to break by accident.
`UserCodeExecutionContext` is ambient and scoped to one execution of user code, so it is entitled to hold a
compilation, a declaration and a syntax builder: that is its purpose. **The objects created from a context and outliving
it are the ones that must not.** An amender stored a whole context, and a query built by `SelectTypesDerivedFrom`
captured the `INamedType` it was given. Both are durable, both are reachable from the configuration, and both were
fixed by keeping a durable reference and resolving it against the compilation of each run, which
`UserCodeExecutionContext.WithCompilationAndDiagnosticAdder` was already doing for everything else.

**The per-file results.** `SyntaxTreePipelineResult` describes itself as compilation-independent and cacheable, and
the pipeline relies on that: `DesignTimeAspectPipelineResult.Update` carries forward the entry of every file the run
did not analyse, and `SplitResultsByTree` goes further, discarding a freshly produced item whose file is not dirty so
that the item produced earlier survives. A file the user never edits therefore keeps, for the whole session, whatever
its first analysis produced. Two kinds of member of that result reach the code model and must be checked:

- the extensions, which are opaque to the engine: an extension chooses what its contributor carries, and nothing in
  `IDesignTimePipelineResultExtension` or `ITransitivePipelineContributor.ToDesignTime` says that it must be durable.
  It must. `ExtensionContributorMemoryLeakTests` states this as a matched pair, one contributor holding a durable
  reference and one holding the reference the code model returns.
- the diagnostics. A `DiagnosticDefinition` formats lazily, so the arguments passed to `WithArguments` are stored in
  the descriptor of the diagnostic and are held for as long as the diagnostic is. An argument that is an
  `IDeclaration` reaches its `CompilationModel`. Passing the declaration is the natural way to write the message, and
  it is what a validator does on every reference it rejects; passing `declaration.Name`, or another value, is what
  keeps the diagnostic durable. `DiagnosticArgumentMemoryLeakTests` states this, also as a matched pair.

**A `TaskCompletionSource` that no path completes.** Awaiting one retains the awaiting frame, and with it every
argument and local the frame captured. On the design-time RPC surface those captures are Roslyn objects: a service is
initialized only when a client attaches to its endpoint, and every method of every server-side service awaits that
initialization before doing anything, including before checking whether there is any client to serve. When no client
ever attaches, which is the ordinary state of the analysis process when the Visual Studio extension is absent, each
such call parks forever. Passing a cancellation token is not by itself enough: it has to be *observed*, which means
composing it with `WithCancellation` rather than handing it to a helper that only uses it for a timeout.
`RpcServiceCancellationTests` states the property for the wait itself, and separately for what the parked frame holds.

## Testing it

`Metalama.Framework.Tests.UnitTests/DesignTime/Pipeline/MemoryLeaks/` contains the suite that guards these rules.
Add to it whenever a change touches something the rules cover.

The shape of a test is: build a project, run the design-time pipeline, apply a number of edits to **run-time** code,
force a full collection, and assert on the liveness of weak references to the versions that were superseded.

Three parts of the harness matter more than the tests themselves.

- **`MemoryLeakAssert`** reports, on failure, the chain of fields that retains the object, computed by
  `RetentionPathFinder` from the objects a design-time host keeps alive. An assertion that says only "something
  retains this compilation" is nearly useless; one that names the field is a fix.
- **`RetentionPathFinder` applies ephemeron marking**, exactly as described above: it follows the value of a
  conditional-weak-table entry only once the key of that entry has been proven reachable by strong references, and it
  never reports a key as retained through the table. Without that rule it reports paths that are circular, or paths
  along which the collector is in fact free to reclaim the object.
- **`MemoryLeakAssertSelfTests`** plants a retention deliberately and requires the assertions to catch it and name the
  field. A suite of liveness tests that all pass is indistinguishable from a suite whose assertions never fire, so
  this positive control is what gives the rest of the suite its value.

Two practical points when writing such a test.

**Never let a compilation reach a local variable of the test method.** A debug build keeps every local alive until the
end of the method that declares it, so a single `var compilation = ...` in the test body defeats the assertion.
Confine every strong reference to a helper marked `[MethodImpl( MethodImplOptions.NoInlining )]` that returns only a
`WeakReference`. `DesignTimeEditingSimulator` is built around this constraint and never hands a compilation back.

**A single `GC.Collect()` is not enough.** An object with a finalizer is reclaimed only by a later collection, so
`GarbageCollectionHelper.Collect` performs several blocking, compacting rounds with `WaitForPendingFinalizers`
between them.

Finally, note what a passing test does **not** prove. Both of the largest defects found in
[#1793](https://github.com/metalama/Metalama/issues/1793) required *cancellation* to trigger. A scenario that submits
one version at a time and waits for each to be analysed never cancels anything and shows no growth at all, which is
why the growth was so hard to reproduce outside a real editing session. When testing a component that cancels
superseded work, the test must cancel too.

## Known residuals

What follows is deliberately not fixed, or not fixed everywhere. It is recorded here rather than left in a pull
request description, because the next person to work on design-time memory will read this document and not that.
Please keep the list honest: strike an entry when it is closed, and add one rather than leaving a repair half-applied.

### Uncancellable waits on a task completion source

Closed. All three call sites now compose the token with `WithCancellation` before `WarnIfLongAsync`, which is the
defect: that method uses the token for its own delay and returns the original task untouched when warning logging is
disabled, so passing it there alone leaves the wait uncancellable by either path. `RpcServiceProviderServerEndpoint`
also completes `_hubRegistrationTask` when the service hub endpoint cannot be obtained, because registration is
attempted once and nothing else would ever complete it. The awaiters of `ClientEndpoint` are deliberately left waiting
in that situation, because a later connection attempt can legitimately complete them; making the wait cancellable is
the whole of what was needed there.

This is worth re-checking after any change on that surface, because the mistake is easy to repeat and invisible:

```bash
grep -rn "Task\.WarnIfLongAsync" --include=*.cs . | grep -v WithCancellation
```

### `WithCancellation` leaves a small object behind per cancelled wait

The fix above releases the awaiting frame, which is what matters, because that frame is what holds the Roslyn objects.
It does not detach the `Task.WhenAny` continuation from the source that is never completed, so a cancelled wait leaves
a promise of about a hundred bytes attached to it. That is unbounded over a session in which no client ever connects,
though it holds nothing bound to a compilation. The shape that avoids it is a collection of waiters an entry can be
removed from, as in the `DesignTimeAspectPipelineFactory` fix for
[#1793](https://github.com/metalama/Metalama/issues/1793). This was reasoned about rather than measured.

### `TransitiveAspectInstance` retains the compilation it was produced in

Bounded to one version, and tracked by [#1797](https://github.com/metalama/Metalama/issues/1797). Its target
declaration closes with `ToDurable()` exactly as the inheritable aspect instances did. The aspect object is the harder
half: it carries an `IntroducedRef` to a declaration the pipeline introduced, which has no source identity to be
durable against, so it needs a design change rather than a repair.

### `TransitiveManifestDeserializationCache` has no eviction

`_manifests` and `_manifestsByHash` (`Metalama.Framework.Engine/Aspects/TransitiveManifestDeserializationCache.cs:42`
and `:43`) grow without bound and are reset only wholesale, in `EnsureBoundTo`. The values are compilation-neutral
deserialized manifests, so this is uncapped managed memory rather than a retained compilation, which is why it is not
treated as a leak by the rules above. It is reached only on a minority reference configuration, namely a cross-version
reference or one whose live manifest cannot be reused.

### Requirements that cannot be enforced

Two of the rules in this document are contracts that the framework states and cannot check, because what is stored is
opaque to it.

- What an extension puts in an `IDesignTimePipelineResultExtension`. The one violation known at the time of writing,
  `TypeEqualityPredicate` in Metalama.Premium, is fixed; another extension can break it silently.
- What a user lambda captures. `Select`, `SelectMany`, `Where` and `Tag` take delegates written by the user, and the
  query that holds them is as durable as the amender that owns it. Only the framework's own captures, such as the one
  in `SelectTypesDerivedFrom`, could be fixed.

An assertion in `DesignTimeAspectPipelineResult.Update`, in DEBUG only, walking a stored extension for a non-durable
`IFullRef`, would turn the first of these into something detectable. It has not been written.

### What the audit did not cover

The sweep behind [#1799](https://github.com/metalama/Metalama/issues/1799) examined the extension, fabric, query, RPC
and design-time-result surfaces. It did not examine the linker, the code-model caches, or the source-generator pipeline
beyond what the RPC surface touched. It also missed the diagnostic-argument defect that the same issue fixes, which was
found by accident when it broke an unrelated control test, and which was probably the most consequential of the set.
Treat the absence of an entry here as "not looked at" rather than "clean", except for the list under **Checked and
clean** in the issue.

## Related documents

- `pipeline.md` for the structure of the pipeline whose state these rules constrain.
- `testing.md` for the test suites and how to run them.
- `compilation-model.md` for what a `CompilationModel` holds, and therefore what a reference to one costs.
