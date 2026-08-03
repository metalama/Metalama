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

## Related documents

- `pipeline.md` for the structure of the pipeline whose state these rules constrain.
- `testing.md` for the test suites and how to run them.
- `compilation-model.md` for what a `CompilationModel` holds, and therefore what a reference to one costs.
