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
compilation is current. When what is at hand is the declaration rather than a reference, call
`declaration.ToDurableRef()`: it produces an equal reference and never allocates the compilation-bound one.

**Declare the requirement in the type, do not leave it to the caller.** A field, property or constructor parameter that
must hold a durable reference is typed `IDurableRef<T>`, not `IRef<T>`. The conversion then cannot be forgotten,
because the compiler asks for it at every call site, and a reader of the type learns the constraint from its signature
rather than from a comment. `TransitiveAspectInstance.TargetDeclaration` is the model. Where such a reference crosses a
serializer, keep `IRef<T>` as the wire shape, which is what the serialization framework resolves, and convert on that
boundary.

This is why `SyntaxTreePipelineResult` documents itself as *compilation-independent and cacheable*: the pipeline
re-analyses only the syntax trees that changed and carries the results of every other file forward from an earlier
run, so a compilation-bound reference in one of them pins the version it was computed in for as long as the project
stays open. `InheritableAspectInstance` and `FabricDriver` both convert their target declaration for this reason.

When adding a member to a type that is stored in a `SyntaxTreePipelineResult`, the question to ask is not "is this
convenient to keep" but "does this reach a `Compilation`". Note that a `Microsoft.CodeAnalysis.Diagnostic` does: it
holds a `Location`, which holds its source tree.

**A type is not a declaration, and `ToDurable()` treats it as one.** `ToDurable()` is backed by a
`SerializableDeclarationId`, which names a *declaration*, so converting `Base<int>` yields a reference that resolves
to `Base<T>`: the type arguments are lost, silently, and the result is a usable type rather than an error.
`ToDurableRef()` behaves identically here, deliberately: it takes the declaration route for everything that is a
declaration, and a named type is one. Where the value is an `IType`, and above all where it comes from user code and
its shape is therefore not known, use `DurableRefFactory.FromTypeId( type.GetSerializableTypeId() )` instead.
`Query.CreateBaseTypeResolver` does, and `RefTests.DurableRefToConstructedGenericTypeLosesTheTypeArguments` records
the difference.

Neither identifier can denote a type an aspect introduced, because resolving one goes through the symbol table. Where
such a type can reach the conversion, verify it rather than assume it: converting unconditionally replaces a query
that works with one that throws `SymbolNotFoundException` when it runs, far from the call site.

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
naturally takes to be per-run. Each is a place to check before storing anything in it.

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

The corollary is where this rule is easiest to break by accident, and is worth stating separately.
`UserCodeExecutionContext` is ambient and scoped to one execution of user code, so it is entitled to hold a
compilation, a declaration and a syntax builder: that is its purpose. **The objects created from a context and
outliving it are the ones that must not.** A fabric amender and the queries it owns are such objects.

Two shapes follow. An owner that outlives a request does not store a context but produces one per compilation, which is
what `IQueryOwner.GetUserCodeExecutionContext` exists for; an owner whose own lifetime is a single run, such as an
aspect builder, may return the context it holds, rebound through
`UserCodeExecutionContext.WithCompilationAndDiagnosticAdder`. And a query builder that is handed a declaration converts
it to a durable reference before capturing it in the adder closure, because the closure lives as long as the query.

Building a context on demand has a consequence that is easy to miss, because it is not about memory. The ordinary
constructor fills in the target declaration, the meta API and the syntax builder from the context that happens to be
current, which is right for a context created while its own user code is running and wrong for one created on demand:
what it would inherit is then whatever the calling thread was doing. `IQuery.ToCollection` is public, so a fabric query
can be executed from inside an aspect. Use `UserCodeExecutionContext.CreateWithoutInheritance` in that shape;
`UserCodeExecutionContextTests` holds the distinction.

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

**Recording a retention that is known but not yet fixed** is done with `MemoryLeakAssert.RetainedThrough`, which names
the route as well as asserting that the object is alive. Asserting liveness alone would hold whether the documented
route retains the object or something else does, and would go on holding after the documented route was removed. Naming
the route makes the record fail in both of the directions that matter: when the defect is fixed, which is the signal to
replace the call with `Collected`, and when the object is retained for a different reason than the one written down.

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

## Open items

Recorded here rather than in a pull request, because this is the document the next person to work on design-time memory
reads. Strike an entry when it is closed, and add one rather than leaving a repair half-applied. State for each whether
it was **measured** or **reasoned**, and phrase a reasoned one as a question, so that a reader can tell an observation
from a hypothesis without re-deriving it.

### A transitive aspect instance retains one compilation

Measured. Tracked by [#1797](https://github.com/metalama/Metalama/issues/1797). The remaining route is:

```
TransitiveAspectInstance.Aspect
  -> PullConstructorParameterTransitiveAspect._parameter : IntroducedRef<IParameter>
    -> RefFactory -> CompilationContext -> Compilation
```

The target declarations on that path are durable and typed `IDurableRef<IDeclaration>`, so only the aspect object
remains. Why the parameter reference is not also durable is specific, and worth knowing before trying it again:

- an `IntroducedRef` **is** serializable. It overrides `ToSerializableId`, `FullRef.ToDurable` is built on that, and
  the identifier of a pulled parameter names the constructor signature and the ordinal, which the resolution path
  handles explicitly and `ConstructorParameterIdResolutionTests` covers;
- calling `ToDurable()` on it therefore compiles, and does close this retention;
- but it breaks the cross-project case. `PullParameterTests.CrossProjectIntegration` then fails: the consuming project
  resolves nothing and the transitive aspect silently does not apply. That test runs on `net48` only, so a `net8.0` run
  stays green and hides it.

The identity of an introduced declaration is not settled at the moment the advice runs. Making the reference durable
has to happen later in the pipeline, which is a change of design rather than a change of call.

`TransitiveContributorMemoryLeakTests` holds a control that asserts the compilation is *still* retained. It fails when
this is fixed, which is the signal to replace it with the ordinary assertion.

### Two requirements the framework cannot check

Structural, and open by nature. Both are contracts stated in documentation, because what is stored is opaque to the
code that stores it:

- what an extension puts in an `IDesignTimePipelineResultExtension`;
- what a user lambda captures. `Select`, `SelectMany`, `Where` and `Tag` take delegates written by the user, and the
  query holding them is as durable as its owner. Only the framework's own captures can be fixed.

A DEBUG invariant in `DesignTimeAspectPipelineResult.Update`, walking a stored extension for a non-durable `IFullRef`,
would make the first of these detectable. It has not been written.

## Established as clean

Recorded so that the same suspicions are not re-investigated from the same reading of the same code. Each of these
looks like a leak and is not.

**`WithCancellation` does not accumulate on the task it waits for.** `Task.WhenAny` removes its continuation from the
task that did not win. Measured on a task that is never completed, the number of continuations left is the same small
constant after 10, 100 and 1000 cancelled waits, for both implementations in use: `TaskExtensions.WithCancellation` of
this codebase, and `Microsoft.VisualStudio.Threading.ThreadingTools.WithCancellation`, which
`Metalama.Framework.DesignTime.Rpc` resolves to because that project does not reference the engine.
`WithCancellationMemoryLeakTests` holds the property, which belongs to the runtime and to a third-party library rather
than to this codebase and would otherwise go unnoticed if lost.

**`TransitiveManifestDeserializationCache` is invalidated.** The wholesale replacement of its dictionaries in
`EnsureBoundTo` is not a substitute for missing eviction, it *is* the invalidation, keyed on the identity of the
consuming project's `CompileTimeProject`. That scope is load-bearing: a manifest is deserialized with the consuming
project's service provider so that it binds to that project's compile-time copy of each type
([#1710](https://github.com/metalama/Metalama/issues/1710)), so an entry made against a superseded projection must not
be reused, and a consumer whose compile-time project is unknown is not cached at all.
`TransitiveManifestDeserializationCacheTests` covers this, `ReprojectedConsumer_DropsTheCache` by name. The hash-keyed
overload is also reached only when `CanReuseLiveManifest` is false; a same-version reference reads the producer's live
result and never touches the cache.

**Waits on a task completion source observe their token.** Every call site composes the token with `WithCancellation`
before `WarnIfLongAsync`. Passing the token to that method alone is not enough, and is the mistake to watch for: it
uses the token for its own delay, and returns the original task untouched when warning logging is disabled. Worth
re-checking after any change on that surface, because it is invisible until an analysis process runs for hours with no
client attached:

```bash
grep -rn "Task[.]WarnIfLongAsync" --include=*.cs . | grep -v WithCancellation
```

## What has not been examined

The rules and entries above came from an audit of the extension, fabric, query, RPC and design-time-result surfaces.
The linker, the code-model caches, and the source-generator pipeline beyond what the RPC surface touches were not
examined. Treat the absence of an entry as "not looked at" rather than "clean"; the section above lists what was
positively established.

Two habits are worth keeping, because the defects that escaped this audit escaped it for the same two reasons.

**Measure before recording.** A retention that is easy to reason about is not thereby real. Entries have stood in the
list of open items and been struck once somebody measured them.

**Make a test prove its scenario happened.** A control that passes for the wrong reason proves nothing. Assert that the
run produced what it was supposed to produce, a contributor, a diagnostic, a validator, before asserting anything about
what is retained.

## Related documents

- `pipeline.md` for the structure of the pipeline whose state these rules constrain.
- `testing.md` for the test suites and how to run them.
- `compilation-model.md` for what a `CompilationModel` holds, and therefore what a reference to one costs.
