# Linker Architecture: Transformation Pipeline

This document describes the internal architecture of the Metalama linker's transformation pipeline — specifically how transformations flow from the advice layer through the injection step to final syntax generation. It focuses on the injection step internals, which are the most complex and least documented part of the linker.

For related topics, see:
- [linker-overview.md](linker-overview.md) — overview of the 3-step pipeline, aspect references, and inlining algorithms
- [linker-inlining.md](linker-inlining.md) — detailed inlining mechanics
- [linker-callsite.md](linker-callsite.md) — call-site advice (OnInitialized) and cross-project propagation

## The Three-Step Pipeline

The `AspectLinker` processes all transformations in three sequential steps:

1. **Injection** (`LinkerInjectionStep`) — indexes transformations, expands templates, and produces the intermediate Roslyn compilation with all introduced/modified members. Outputs `LinkerInjectionStepOutput`.
2. **Analysis** (`LinkerAnalysisStep`) — analyzes aspect references, determines reachability and inlineability of semantics, finds call-site advice targets.
3. **Linking** (`LinkerLinkingStep`) — produces the final compilation by inlining, removing unreachable semantics, and applying substitutions.

This document focuses on **Step 1 (Injection)**, specifically its transformation indexing and constructor rewriting subsystems.

## Transformation Taxonomy

Transformations flowing through the linker implement different interfaces that determine how they are processed:

| Interface | Concrete Examples | Purpose |
|---|---|---|
| `IIntroduceDeclarationTransformation` | `IntroduceConstructorTransformation`, `IntroduceMethodTransformation` | Introduce new declarations into the compilation |
| `IReplaceMemberTransformation` | Implemented by `IntroduceConstructorTransformation` and others | Replace existing member implementations |
| `IOverrideDeclarationTransformation` | Various override transformations | Override methods, properties, events |
| `IInjectMemberTransformation` | Template-expanded transformations (also `IInjectInterfaceTransformation` for interfaces) | Inject code via template expansion |
| `IMemberLevelTransformation` | `IntroduceParameterTransformation`, `ReplaceParameterTransformation`, `IntroduceConstructorInitializerArgumentTransformation` | Modify existing members at the parameter/argument level |
| `IInsertStatementTransformation` | Statement insertion transformations | Insert statements into method/constructor bodies |

**Member-level transformations** are the focus of this document. They modify constructors by adding/replacing parameters and adding/overriding initializer arguments in `: base(...)` or `: this(...)` calls.

## Transformation Indexing Pipeline

### Processing Order

`LinkerInjectionStep.ExecuteAsync` processes all transformations through `IndexTransformationsInSyntaxTree`, which iterates transformations in deterministic order (sorted by `TransformationLinkerOrderComparer`). For each transformation, the following indexing methods are called **in this order**:

1. **`IndexIntroduceDeclarationTransformation`** — register introduced members (first pass, separate loop)
2. **`IndexReplaceTransformation`** — handle replacements (second pass, separate loop; correct because replaced transformation is always in the same syntax tree as the replacing one)
3. **`IndexOverrideTransformation`** — process overrides
4. **`IndexInjectTransformation`** — template expansion, producing `InjectedMember` objects
5. **`IndexMemberLevelTransformation`** — collect parameters and arguments (documented below)
6. **`IndexInsertStatementTransformation`** — collect inserted statements
7. **`IndexNodesWithModifiedAttributes`** — track attribute changes

Steps 3–7 run in the same loop. Steps 1–2 run in separate preceding loops because they have ordering dependencies (introductions must be registered before replacements, and replacements before overrides).

### Finalization

After all transformations are indexed, `TransformationCollection.FinalizeAsync()` calls `MemberLevelTransformations.Sort()` concurrently on all collections, producing immutable arrays of deduplicated, index-ordered parameters and arguments. This must complete before the rewriting phase begins.

## Member-Level Transformations

### The Two-Dictionary Pattern

`TransformationCollection` stores member-level transformations in two concurrent dictionaries:

```
_symbolMemberLevelTransformations       : ConcurrentDictionary<SyntaxNode, MemberLevelTransformations>
_introductionMemberLevelTransformations : ConcurrentDictionary<DeclarationBuilderData, MemberLevelTransformations>
```

**Why two dictionaries?** Source members have a stable identity via their syntax node. Introduced members have no source syntax — their identity is their `DeclarationBuilderData`. The linker must look up transformations for both during rewriting.

### Dispatch: `GetOrAddMemberLevelTransformations`

When a member-level transformation is indexed, the linker resolves which dictionary to use via a pattern-match dispatch:

```
IRef<IDeclaration> →
  ISymbolRef (source code)    → extract primary SyntaxNode → _symbolMemberLevelTransformations
  IIntroducedRef (introduced) → extract BuilderData        → _introductionMemberLevelTransformations
  Redirected declaration      → recursive resolution through CompilationModel
  Otherwise                   → AssertionFailedException (invariant violation)
```

### `IndexMemberLevelTransformation`: The Switch Dispatch

`IndexMemberLevelTransformation` (in `LinkerInjectionStep.cs`) is the single entry point for all member-level transformation indexing. It dispatches on the concrete transformation type:

```
IMemberLevelTransformation →
├── ReplaceParameterTransformation
│   └── Source only → Add to MemberLevelTransformations
│       (Introduced → SKIP; builder data already updated)
├── IntroduceParameterTransformation
│   ├── Introduced → SKIP (parameters already in builder data)
│   └── Source → Add + register; special record primary ctor handling
└── IntroduceConstructorInitializerArgumentTransformation
    └── Always add (applies to both source and introduced constructors)
```

> **NOTE (TODO in source):** Currently supports only constructors without overrides. Needs to be generalized for anything else.

## Source vs Introduced Constructors: The Critical Distinction

This is the most important architectural invariant in the member-level transformation system. Understanding it is essential for adding new parameter or argument transformations.

### Source Constructors

For constructors defined in source code:

1. **Indexing**: `IntroduceParameterTransformation` and `ReplaceParameterTransformation` are added to `MemberLevelTransformations` (via `_symbolMemberLevelTransformations`, keyed by syntax node).
2. **Finalization**: `Sort()` deduplicates parameters and arguments (see below).
3. **Rewriting**: `VisitConstructorDeclarationCore` looks up transformations by syntax node and calls `ApplyMemberLevelTransformations`, which modifies the existing source syntax:
   - `AppendParameters()` inserts new parameter syntax into the existing parameter list
   - `AppendInitializerArguments()` adds/overrides arguments in `: base(...)` / `: this(...)`

### Introduced Constructors

For constructors created by `IntroduceConstructorTransformation`:

1. **Syntax generation**: `IntroduceConstructorTransformation.GetInjectedMembers()` generates a complete `ConstructorDeclarationSyntax` from `ConstructorBuilderData`. Parameters are drawn from the builder's `Parameters` collection, which was already updated by `CompilationModel.AddTransformation()`.
2. **Indexing**: `IntroduceParameterTransformation` and `ReplaceParameterTransformation` are **skipped** during indexing. The guard pattern:

```csharp
if ( transformation.TargetDeclaration is IIntroducedRef ||
     compilationModel.IsRedirected( transformation.TargetDeclaration ) )
{
    break; // Skip — builder data already has the parameter
}
```

3. **Why skip?** The `CompilationModel` processes `IntroduceParameterTransformation` by adding the parameter to the builder's `ParameterBuilderData` collection, and `ReplaceParameterTransformation` by calling `ParameterUpdatableCollection.Replace()`. When the linker later generates syntax from builder data, those parameters are already present. If we also added them to `MemberLevelTransformations`, `AppendParameters()` would produce **duplicate parameters** in the final syntax.

4. **Exception**: `IntroduceConstructorInitializerArgumentTransformation` is **always** indexed (not skipped), because initializer arguments are not part of `ConstructorBuilderData.Parameters` — they modify the `: base(...)` call, which is assembled separately during syntax generation.

5. **Rewriting of injected members**: After the `InjectedMember` is created with its initial syntax (from builder data), the Rewriter checks `TryGetMemberLevelTransformations(BuilderData)` and applies any remaining transformations (typically only initializer arguments).

### Summary Table

| Aspect | Source Constructors | Introduced Constructors |
|---|---|---|
| Dictionary key | `SyntaxNode` | `DeclarationBuilderData` |
| Parameter transformations | Indexed in `MemberLevelTransformations` | **Skipped** (already in builder data) |
| Argument transformations | Indexed in `MemberLevelTransformations` | Indexed in `MemberLevelTransformations` |
| Syntax generation | Existing syntax modified by Rewriter | Fresh syntax from builder data, then arguments applied |
| Lookup during rewriting | `TryGetMemberLevelTransformations(SyntaxNode)` | `TryGetMemberLevelTransformations(BuilderData)` |

## Deduplication: `MemberLevelTransformations.Sort()`

### Lifecycle

1. **Collection phase**: Concurrent `Add()` calls append to `ConcurrentLinkedList<T>` (LIFO order — latest-added first when iterated).
2. **Finalization**: `FinalizeAsync()` calls `Sort()` concurrently on all collections in both dictionaries.
3. **Consumption**: Immutable arrays consumed read-only during rewriting.

### Parameter Deduplication (`IsReplacement`)

`ReplaceParameterTransformation` sets `IsReplacement => true`. During `Sort()`:

1. Collect all parameter transformations from the linked list (LIFO order).
2. Group replacements by `Parameter.Index`, keeping the first (latest-added) per index.
3. Filter out original `IntroduceParameterTransformation` entries at indices where replacements exist.
4. Concatenate remaining originals with replacements.
5. Sort by `Parameter.Index` to produce the final immutable array.

**Result**: At most one parameter transformation per index. Replacements supersede originals.

### Argument Deduplication (`IsOverride`)

`IntroduceConstructorInitializerArgumentTransformation` can have `IsOverride => true`. The same dedup algorithm applies:

1. Group overrides by `ParameterIndex`.
2. Filter out non-override arguments at indices where overrides exist.
3. Sort by `ParameterIndex`.

**Use case**: The `OnConstructed` advice rewrites the `context` argument pulled into a derived `: base(...)` call so it carries the correct `InitializationSlot`.

## Constructor Rewriting Flow

### Source Constructors

```
VisitConstructorDeclarationCore(originalNode)
  │
  ├── VisitConstructorDeclaration(originalNode)          // Standard CSharpSyntaxRewriter visit
  │
  ├── Resolve lookup node (handle partial constructors)
  │   └── Partial definitions: use implementation part's syntax for lookup
  │
  ├── TryGetMemberLevelTransformations(lookupNode)       // _symbolMemberLevelTransformations
  │   └── ApplyMemberLevelTransformations()
  │       ├── AppendParameters(parameterList, sorted parameters)
  │       │   └── Insert before params keyword if present; append otherwise
  │       └── AppendInitializerArguments(initializer, sorted arguments)
  │           └── Override matching: by name first, then by position
  │
  ├── Inject entry/epilogue statements (contracts, initializers)
  │
  └── Rewrite attributes
```

**Partial constructor handling** (Roslyn 5.0+): Partial definitions receive parameters only (no initializer syntax). Partial implementations receive both parameters and initializer arguments.

### Introduced Constructors

```
IntroduceConstructorTransformation.GetInjectedMembers()
  │
  ├── Resolve finalConstructor from CompilationModel    // Builder data already has all parameters
  │
  ├── Generate ParameterList via SyntaxGenerator        // From builder's Parameters collection
  │
  ├── Generate initializer from BuilderData.InitializerArguments
  │
  └── Produce InjectedMember with complete ConstructorDeclarationSyntax

Rewriter.AddInjectionsOnPosition (during syntax tree rewriting)
  │
  ├── Insert InjectedMember at correct position
  │
  └── TryGetMemberLevelTransformations(BuilderData)     // _introductionMemberLevelTransformations
      └── ApplyMemberLevelTransformations()             // Only initializer arguments typically
```

### Primary Constructor Handling

Primary constructors (on records and classes) receive special treatment:

- **`ApplyMemberLevelTransformationsToPrimaryConstructor`**: Modifies the type declaration's parameter list and base list (for `: BaseType(args)` syntax) instead of a constructor body.
- **Non-materialized parameters** (`MaterializeOnRecord == false`): Trigger primary-constructor materialization — the linker emits an explicit body-declared constructor and registers the parameter name so the linker skips the corresponding positional property / `Deconstruct` entry.
- **Materialized parameters** (`MaterializeOnRecord == true`): Tracked separately so `Deconstruct` emission can differentiate the all-non-materialized case from the mixed/all-materialized case.

## Key Files

| File | Role |
|---|---|
| `LinkerInjectionStep.cs` | Main orchestration; `IndexMemberLevelTransformation` dispatches transformations |
| `LinkerInjectionStep.TransformationCollection.cs` | Two dictionaries, `GetOrAddMemberLevelTransformations` dispatch, `FinalizeAsync` |
| `LinkerInjectionStep.MemberLevelTransformations.cs` | `Sort()` dedup logic, `IsReplacement`/`IsOverride` semantics |
| `LinkerInjectionStep.Rewriter.cs` | `VisitConstructorDeclarationCore`, `ApplyMemberLevelTransformations`, `AppendParameters`, `AppendInitializerArguments` |
| `IntroduceParameterTransformation.cs` | Base transformation class, `IsReplacement` virtual property |
| `ReplaceParameterTransformation.cs` | Derived class for parameter type replacement, `IsReplacement => true` |
| `IntroduceConstructorInitializerArgumentTransformation.cs` | Initializer argument transformation, `IsOverride` flag |
| `IntroduceConstructorTransformation.cs` | `GetInjectedMembers()` — generates complete constructor syntax from builder data |
| `CompilationModel.Members.cs` | Processes parameter transformations — adds to / replaces in `ParameterUpdatableCollection` |
