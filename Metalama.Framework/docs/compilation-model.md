# CompilationModel Architecture

This document describes the `CompilationModel` — Metalama's incremental, immutable code model that evolves as aspects apply transformations. It covers how the model is versioned, how declarations are represented and referenced, and how transformations flow through the pipeline.

For the pipeline execution model — stages, aspect layers, declaration depth, parallel type processing, mutable compilation lifecycle — see [pipeline.md](pipeline.md).

## Overview

`CompilationModel` is the central data structure of the Metalama pipeline. It represents the state of a compilation at a specific point in aspect processing — including both original source declarations and aspect-introduced declarations. Each pipeline step produces a new immutable snapshot, creating a chain of progressively richer models.

**Key properties:**
- **Immutable by default**: Once created, a `CompilationModel` cannot be modified. Aspects see a frozen view.
- **Incremental**: New versions are created by applying transformations to a prototype, not by rebuilding from scratch.
- **Unified view**: Source declarations (backed by Roslyn symbols) and introduced declarations (backed by builder data) are accessed through the same interfaces.

**File:** `Metalama.Framework.Engine/CodeModel/CompilationModel.cs` (partial class, split across multiple files)

## Versioning

### Factory Methods

| Method | Purpose |
|---|---|
| `CreateInitialInstance()` | Static factory: first model from Roslyn compilation |
| `WithTransformationsAndAspectInstances()` | New version replaying observable transformations on prototype |
| `WithTransformations()` | New version with transformations only |
| `CreateMutableClone()` | Mutable copy for aspect execution |
| `CreateImmutableClone()` | Freeze a mutable model into an immutable snapshot |

### The Mutable/Immutable Invariant

`CompilationModel` has an `IsMutable` flag that enforces the lifecycle:

- `AddTransformation()` throws `InvalidOperationException` if called on an immutable model
- A `Revision` counter (in `CompilationModel.Members.cs`) is incremented on every `AddTransformation()` call; updatable collections use this for cache invalidation
- Once `IsMutable` is set to `false`, it can never be set back to `true` — only a new clone can be mutable

## Declaration Types

Metalama's code model contains two fundamentally different kinds of declarations:

### Source Declarations (Symbol-Based)

Declarations from source code or referenced assemblies, backed by Roslyn `ISymbol` objects.

- **Base class**: `SymbolBasedDeclaration` (`CodeModel/Source/SymbolBasedDeclaration.cs`)
- **Identity**: Roslyn `ISymbol`
- **Origin**: `DeclarationOriginKind.Source` (current project) or `DeclarationOriginKind.External` (referenced assemblies)
- **Resolution**: Symbol is translated between compilations via `SymbolTranslator`

### Introduced Declarations (Builder-Based)

Declarations created by aspects, backed by `DeclarationBuilderData` objects.

- **Base class**: `IntroducedDeclaration` (`CodeModel/Introductions/Introduced/IntroducedDeclaration.cs`)
- **Identity**: `DeclarationBuilderData` instance
- **Origin**: The `AspectInstance` that introduced the declaration (via `IAspectDeclarationOrigin`)
- **Resolution**: Builder data is looked up in the `DeclarationFactory` cache

### Unified Access

Both declaration types implement the same public interfaces (`IMethod`, `IProperty`, `IConstructor`, etc.). The `CompilationModel`'s updatable collections aggregate both source and introduced declarations, so consumers iterate a single unified collection without knowing the backing type.

## DeclarationBuilder vs DeclarationBuilderData

Introduced declarations use a two-phase pattern: mutable **builder** → immutable **builder data**.

### DeclarationBuilder (Mutable)

**Base class:** `CodeModel/Introductions/Builders/DeclarationBuilder.cs`

Builders are created during aspect execution and allow mutation via property setters. All setters call `CheckNotFrozen()` to enforce the lifecycle.

**Key state:**
- `Attributes` — mutable collection
- `AspectLayerInstance` — the creating aspect
- `IsFrozen` — lifecycle flag

**Concrete examples:** `ConstructorBuilder`, `ParameterBuilder`, `MethodBuilder`, `FieldBuilder`, `PropertyBuilder`

### DeclarationBuilderData (Immutable)

**Base class:** `CodeModel/Introductions/BuilderData/DeclarationBuilderData.cs`

Created from a frozen builder. All properties are immutable (`ImmutableArray<>`, read-only). This is what the `CompilationModel` stores and what the linker reads.

**Key state:**
- `ContainingDeclaration` — `IFullRef<IDeclaration>` (parent)
- `ParentAdvice` �� `AspectLayerInstance` that created it
- `Attributes` — `ImmutableArray<AttributeBuilderData>`

**Concrete examples:** `ConstructorBuilderData`, `ParameterBuilderData`, `MethodBuilderData`, `FieldBuilderData`

### The Freeze Pattern

```
ParameterBuilder (mutable)         ParameterBuilderData (immutable)
  ._name = "service"         →       .Name = "service"
  ._type = ILogger<T>        →       .Type = IFullRef(ILogger<T>)
  ._defaultValue = null       →       .DefaultValue = null
  ._index = 0                →       .Index = 0

                Freeze()
  1. EnsureReferenceCreated()     → Create IntroducedRef<IParameter>
  2. FreezeChildren()             → Freeze attributes
  3. EnsureReferenceInitialized() → Create ParameterBuilderData, attach to ref
```

After `Freeze()`:
- The builder becomes read-only (`IsFrozen = true`)
- The `DeclarationBuilderData` is the authoritative representation
- The `IntroducedRef` points to the builder data

### Example: Constructor Introduction

```
ConstructorBuilder (during aspect execution)
  .Parameters → ParameterBuilderList (mutable, Add/Insert/Remove)
  .InitializerKind → ConstructorInitializerKind.Base
  .InitializerArguments → List<(IExpression, string?)> (mutable)

         ↓ Freeze()

ConstructorBuilderData (stored in CompilationModel)
  .Parameters → ImmutableArray<ParameterBuilderData>
  .InitializerKind → ConstructorInitializerKind.Base
  .InitializerArguments → ImmutableArray<(IExpression, string?)>
  .GetOwnedDeclarations() → Parameters ∪ Attributes
```

## Declaration References

References (`IRef<T>`) provide a stable identity for declarations across compilation versions. They are compilation-independent and can resolve to a declaration in any compilation.

### Reference Hierarchy

```
IRef<T> (public interface)
  │
  ├── IFullRef<T> (engine-internal, compilation-bound)
  │     │
  │     ├── SymbolRef<T> (ISymbolRef)
  │     │     Backed by Roslyn ISymbol
  │     │     Resolves via SymbolTranslator → factory.GetCompilationElement()
  │     │
  │     └── IntroducedRef<T> (IIntroducedRef)
  │           Backed by DeclarationBuilderData (via StrongBox for delayed init)
  │           Resolves via factory.GetDeclaration(builderData)
  │           Has ReplacedDeclaration for implicit→explicit constructor replacement
  │
  └── DeclarationIdRef (durable, serializable string ID)
```

### Key Distinction: ISymbolRef vs IIntroducedRef

This distinction is critical throughout the engine (see [linker-architecture.md](linker-architecture.md) for how the linker dispatches on it):

| | `ISymbolRef` | `IIntroducedRef` |
|---|---|---|
| Backing store | Roslyn `ISymbol` | `DeclarationBuilderData` |
| Identity key | Symbol + target kind | Builder data instance |
| Cross-compilation | Symbol translated via `SymbolTranslator` | Builder data is stable (same instance) |
| Syntax in linker | Has source syntax node | No source syntax; generated from builder data |

### Redirected Declarations

When a declaration is "replaced" (e.g., an implicit constructor replaced by an explicit one, or a field promoted to a property), the `CompilationModel` stores a **redirection** mapping:

```csharp
_redirections: ImmutableDictionary<IRef, DeclarationBuilderData>
```

- `IsRedirected(IRef)` — checks if a reference has been replaced
- `TryGetRedirectedDeclaration(IRef, out DeclarationBuilderData)` — resolves the replacement

The `DeclarationFactory` checks redirections during resolution: if a builder data has been redirected, it recursively resolves to the replacement.

## Updatable Collections

`CompilationModel` stores declarations in **updatable collections** — lazily-populated, copy-on-write containers that aggregate both source and introduced declarations.

### Storage Pattern

```csharp
// CompilationModel.Members.cs — one dictionary per declaration kind
_fields      : ImmutableDictionary<IFullRef<INamedType>, FieldUpdatableCollection>
_methods     : ImmutableDictionary<IFullRef<INamedType>, MethodUpdatableCollection>
_constructors: ImmutableDictionary<IFullRef<INamedType>, ConstructorUpdatableCollection>
_parameters  : ImmutableDictionary<IFullRef<IHasParameters>, ParameterUpdatableCollection>
// ... etc.
```

Each dictionary maps an **owning declaration** (e.g., a named type) to the collection of members it contains.

### Copy-on-Write

When a new `CompilationModel` is created from a prototype, it initially **shares** the prototype's collections. When a collection needs mutation (to add an introduced declaration), `GetMemberCollection()` clones it:

```
GetMemberCollection(ref dictionary, requestMutableCollection: true, declaration)
  │
  ├── Collection doesn't exist → create new, populate from Roslyn symbols
  ├── Collection belongs to THIS compilation → return as-is
  └���─ Collection belongs to PROTOTYPE compilation → Clone(), store in this model
```

### Base Class: `DeclarationUpdatableCollection<T>`

**File:** `CodeModel/UpdatableCollections/DeclarationUpdatableCollection.cs`

- **Lazy population**: `EnsureComplete()` populates from Roslyn symbols on first access
- **Mutation methods**: `AddItem()`, `InsertItem()`, `SetItem()`, `RemoveItem()`
- **Clone()**: `MemberwiseClone()` + copy the internal list — produces an independent snapshot

### ParameterUpdatableCollection

**File:** `CodeModel/UpdatableCollections/ParameterUpdatableCollection.cs`

Special methods for parameter management:
- **`Add(ParameterBuilderData)`** — inserts before `params` parameter if present
- **`Replace(int index, ParameterBuilderData)`** — updates parameter at index (used by `ReplaceParameterTransformation`)
- **`PopulateAllItems()`** — handles four sources: method symbols, indexer symbols, introduced methods, introduced constructors

## How Transformations Are Applied

`AddTransformation()` (in `CompilationModel.Members.cs`) dispatches on the transformation type:

| Transformation Type | Action |
|---|---|
| `IReplaceMemberTransformation` | Add to `_redirections` dictionary |
| `RemoveAttributesTransformation` | Remove attributes from target |
| `IIntroduceDeclarationTransformation` | `AddDeclaration(builderData)` → route to appropriate updatable collection |
| `ReplaceParameterTransformation` | `parameterCollection.Replace(index, newParameterData)` |
| `IntroduceParameterTransformation` | `AddDeclaration(parameterBuilderData)` |
| `IIntroduceInterfaceTransformation` | Update interface index |
| `AddAnnotationTransformation` | Add to annotations dictionary |
| `SetHasImplementationTransformation` | Update implementation flag |

### AddDeclaration Routing

`AddDeclaration(DeclarationBuilderData)` routes builder data to the correct updatable collection based on type:

- `MethodBuilderData` → `MethodUpdatableCollection` of owning type
- `ConstructorBuilderData` → `ConstructorUpdatableCollection` (or `_staticConstructors` for static)
- `FieldBuilderData` → `FieldUpdatableCollection`
- `PropertyBuilderData` → `PropertyUpdatableCollection`
- `ParameterBuilderData` → `ParameterUpdatableCollection` of owning method/constructor
- `NamedTypeBuilderData` → `TypeUpdatableCollection` of parent + top-level collection
- `AttributeBuilderData` → `AttributeUpdatableCollection`

## Declaration Origin

Every declaration exposes its origin via `IDeclaration.Origin`:

| Origin | `DeclarationOriginKind` | Meaning |
|---|---|---|
| Source declaration in current project | `Source` | Defined in source code being compiled |
| Declaration from referenced assembly | `External` | From a NuGet package or project reference |
| Aspect-introduced declaration | via `IAspectDeclarationOrigin` | Created by an aspect; `AspectInstance` identifies which |
| Compiler-generated | `Source` + `IsCompilerGenerated` | Implicit constructors, top-level statement classes, etc. |

Source declarations determine their origin by comparing the symbol's containing assembly with the compilation's assembly. Introduced declarations always return the creating aspect instance.

## Key Files

| File | Role |
|---|---|
| `CompilationModel.cs` | Main class, factory methods, versioning |
| `CompilationModel.Members.cs` | `AddTransformation`, `AddDeclaration`, collection access |
| `Introductions/Builders/DeclarationBuilder.cs` | Mutable builder base class, freeze pattern |
| `Introductions/BuilderData/DeclarationBuilderData.cs` | Immutable builder data base class |
| `Introductions/Builders/ConstructorBuilder.cs` | Concrete builder example |
| `Introductions/BuilderData/ConstructorBuilderData.cs` | Concrete builder data example |
| `Introductions/Introduced/IntroducedDeclaration.cs` | Base class for introduced declaration facades |
| `Source/SymbolBasedDeclaration.cs` | Base class for source declaration facades |
| `References/SymbolRef.cs` | Reference implementation for source declarations |
| `References/IntroducedRef.cs` | Reference implementation for introduced declarations |
| `UpdatableCollections/DeclarationUpdatableCollection.cs` | Copy-on-write collection base |
| `UpdatableCollections/ParameterUpdatableCollection.cs` | Parameter-specific collection with Replace() |
