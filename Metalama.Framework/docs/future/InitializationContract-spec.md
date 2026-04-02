# Specification: `InitializationContext` and `[OnInitialized]`

## Motivation

C# `init`-only properties and `required` members enable object initializer syntax for immutable types, but provide no mechanism to validate or compute derived values after all properties have been set.
Constructor-based validation is bypassed by `with` expressions on records.
This feature fills that gap without requiring compiler changes, via a library and the Metalama Linker.

See also the related C# language discussion: https://github.com/dotnet/csharplang/discussions/6591

A second motivation is the **telescoping constructor or initializer problem**: in an inheritance hierarchy, each layer may need to perform initialization logic (validation, change tracking, notification setup, etc.) but only once the entire object — including all derived layers — is fully initialized.
Without a post-initialization hook, each base class either runs its logic too early (before derived properties are set) or duplicates it at every layer.
`[OnInitialized]` with `InitializationSlot`-based coordination solves this by letting each layer declare its behavior and skip it if a derived layer has already guaranteed it will run.

---

## 1. `[OnInitialized]` Attribute

`[OnInitialized]` applies to classes, records, structs, and record structs.

### 1.1 Declaration

```csharp
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnInitializedAttribute : Attribute
{
    /// <summary>
    /// Controls the execution order when multiple [OnInitialized] methods exist on a type
    /// (e.g., from partial classes or source generators). Lower values run first.
    /// </summary>
    public int Order { get; set; }
}
```

### 1.2 Method Contract

A method marked `[OnInitialized]` must satisfy the following constraints.
Any implementation that processes `[OnInitialized]` (the Metalama Linker, a reflection-based framework, etc.) should validate these and report diagnostics (see §3.3 for Metalama diagnostics):

| Constraint | Rule |
|---|---|
| Return type | Must be the declaring type or any base type (not `void`); if a base type, the caller must cast |
| Parameters | Either parameterless, or a single `InitializationContext context = default` |
| Count | Multiple `[OnInitialized]` methods are allowed per type (see ordering rules below) |
| Inheritance | A derived class override should use a **covariant return type**; if it returns a base type instead, the caller must cast |

### 1.3 Execution Order

When multiple `[OnInitialized]` methods exist (on the same type or across an inheritance hierarchy), they are executed in the following order:

1. **`Order` property** (ascending) — lower values run first
2. **Inheritance depth** (base class first) — among methods with the same `Order`, methods declared on base types run before methods on derived types
3. **Method name** (alphabetical) — tie-breaker for methods on the same type with the same `Order`

All `[OnInitialized]` methods in the hierarchy are executed — a derived type's method does **not** hide or replace a base type's method.
Each method receives the `InitializationContext` with slots accumulated via `Descend` from previously executed methods.

### 1.4 Inheritance

- `[OnInitialized]` on a base class method should be declared `virtual`
- A derived class **may** `override` it with its own concrete return type (covariant return, C# 9+), but may also declare a separate `[OnInitialized]` method — both will be executed
- The code model emits the `override` automatically with the covariant return type, calling `base.OnInitialized(context.Descend(...))` before derived logic; if the author declares the override manually the code model leaves it untouched

> **Source generator ordering caveat:** When multiple source generators each add `[OnInitialized]` methods to the same type (via partial classes), they must coordinate on `Order` values to ensure correct execution order.
> Metalama solves this with its aspect ordering specification, but no equivalent coordination mechanism exists for independent source generators.
> This is an inherent limitation of the source generator model.

### 1.5 Invocation Responsibility

The method is **not** called automatically by the runtime. It is the responsibility of either:

- The caller, after an object initializer, using the fluent return value
- A code generator transforming call sites (such as the Metalama Linker — see §3)

### 1.6 Examples

**Simple class:**
```csharp
public class Range
{
    public required int Min { get; init; }
    public required int Max { get; init; }

    [OnInitialized]
    public virtual Range OnInitialized(InitializationContext context = default)
    {
        if (Max < Min)
            throw new InvalidOperationException("Max must not be less than Min.");
        return this;
    }
}
```

**Derived class:**
```csharp
public class NamedRange : Range
{
    public required string Name { get; init; }

    [OnInitialized]
    public override NamedRange OnInitialized(InitializationContext context = default)
    {
        base.OnInitialized(context.Descend());
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Name must not be empty.");
        return this;
    }
}
```

**Caller — constructor pattern (instrumented call site, after Linker rewriting):**
```csharp
var r = new Range(1, 12, InitializationContext.CallInitialize);
// Constructor self-invokes OnInitialized — no explicit .OnInitialized() needed
```

**Caller — object initializer pattern (instrumented call site, after Linker rewriting):**
```csharp
var r = new Range(InitializationContext.WillInitialize) { Min = 1, Max = 12 }
    .OnInitialized();
```

---

## 2. `InitializationContext`

### 2.1 Purpose

A single type used both as a constructor parameter and as the `OnInitialized` parameter.
It carries:

- The caller's intent regarding `OnInitialized` (`CallerIntent`) — allows constructor code to choose between eager and lazy initialization strategies, and to self-invoke `OnInitialized` when the caller requests it via `CallInitialize`
- Which aspect behaviors are guaranteed to run in a derived type (`InitializationSlot` bitmask)
- Optional metadata (`Metadata`) — an extensible object carrying the reason for initialization and any additional context. Only meaningful when passed to `OnInitialized`.

### 2.2 Layout

The struct contains three fields that fit in 16 bytes on x64 (no wasted padding):

| Field | Type | Size | Notes |
|---|---|---|---|
| `_intent` | `CallerIntent` (byte) | 1 byte | See §2.5 |
| `_slots` | `uint` | 4 bytes | Bitmask of aspect behaviors guaranteed by a derived type (32 slots) |
| `_metadata` | `InitializationMetadata?` | 8 bytes | Extensible metadata, typically a singleton (see §2.6) |

The 16-byte size is acceptable: `InitializationContext` is passed on object construction paths where heap allocation already dominates.
The metadata field is typically a singleton reference, so it adds no GC pressure.

### 2.3 `InitializationSlot`

Each aspect **type** (not instance) that needs cross-layer coordination allocates a single slot at class initialization time.
A slot is a strongly-typed bitmask supporting the `|` operator.

> **Expected usage:** Slots are needed only by aspect types that address the telescoping constructor problem — i.e., aspects whose `OnInitialized` behavior must be skipped when a derived type guarantees it will handle the same concern.
> In practice, very few aspect types need this (likely fewer than half a dozen in a typical application).
> The 32-slot limit is therefore generous.
> Aspects that do not use cross-layer coordination (like the `ParentChildAspect` in §9) do not allocate a slot at all.

```csharp
public readonly struct InitializationSlot
{
    private readonly uint _mask;

    internal InitializationSlot(uint mask) => _mask = mask;

    /// <summary>Combines two slots into one.</summary>
    public static InitializationSlot operator |(InitializationSlot a, InitializationSlot b)
        => new(a._mask | b._mask);

    internal uint Mask => _mask;

    private static int _nextBit = 0;

    /// <summary>
    /// Allocates a new slot. Maximum 32 slots per AppDomain.
    /// Throws if the maximum is exceeded.
    /// </summary>
    public static InitializationSlot Allocate()
    {
        int bit = Interlocked.Increment(ref _nextBit) - 1;
        if (bit >= 32)
            throw new InvalidOperationException(
                "Cannot allocate an InitializationSlot: " +
                "the maximum of 32 slots has been exceeded.");
        return new InitializationSlot(1u << bit);
    }
}
```

Typical aspect usage:

```csharp
class ValidationAspect : TypeAspect
{
    public static readonly InitializationSlot Slot = InitializationSlot.Allocate();
}
```

### 2.4 API

```csharp
public readonly struct InitializationContext
{
    private readonly CallerIntent _intent;
    private readonly uint _slots;
    private readonly InitializationMetadata? _metadata;

    private InitializationContext(
        CallerIntent intent,
        uint slots = 0u,
        InitializationMetadata? metadata = null)
    {
        _intent = intent;
        _slots = slots;
        _metadata = metadata;
    }

    /// <summary>
    /// The default context — Intent = None, no slots, no metadata.
    /// </summary>
    public static InitializationContext Default { get; } = new(CallerIntent.None);

    /// <summary>
    /// A context signaling that the caller will call <c>OnInitialized</c> after construction
    /// (e.g. after an object initializer). The constructor should not self-invoke.
    /// </summary>
    public static InitializationContext WillInitialize { get; }
        = new(CallerIntent.WillInitialize);

    /// <summary>
    /// A context signaling that the constructor should self-invoke <c>OnInitialized</c>
    /// at the end of its body. Used at call sites without object initializers, where
    /// the source transformer can guarantee all properties are set by the constructor.
    /// </summary>
    public static InitializationContext CallInitialize { get; }
        = new(CallerIntent.CallInitialize);

    /// <summary>
    /// Creates a context with the given metadata. Used when calling OnInitialized directly
    /// (not via a constructor), e.g. after deserialization or with-expression.
    /// </summary>
    public static InitializationContext Create(InitializationMetadata metadata)
        => new(CallerIntent.None, 0u, metadata);

    /// <summary>The caller's intent regarding OnInitialized invocation.</summary>
    public CallerIntent Intent => _intent;

    /// <summary>
    /// Whether OnInitialized will be called (either by the caller or self-invoked
    /// by the constructor). True when Intent is WillInitialize or CallInitialize.
    /// </summary>
    public bool WillCallOnInitialized => _intent != CallerIntent.None;

    /// <summary>
    /// Optional metadata describing the initialization context. Typically a singleton.
    /// Returns <c>null</c> for default construction (equivalent to
    /// <see cref="InitializationMetadata.Default"/>).
    /// </summary>
    public InitializationMetadata? Metadata => _metadata;

    /// <summary>
    /// Returns whether the given aspect behavior is guaranteed by a derived type.
    /// </summary>
    public bool IsHandledBy(InitializationSlot slot) => (_slots & slot.Mask) != 0;

    /// <summary>
    /// Returns a copy with the given slots added to the handled set.
    /// Normalizes <see cref="Intent"/> to <see cref="CallerIntent.WillInitialize"/>
    /// (preserving the promise that OnInitialized will be called) and preserves
    /// <see cref="Metadata"/> from the original context.
    /// Used when a derived OnInitialized calls base.OnInitialized to propagate
    /// which aspect behaviors the derived type guarantees.
    /// </summary>
    public InitializationContext Descend(InitializationSlot slots = default)
        => new(CallerIntent.WillInitialize, _slots | slots.Mask, _metadata);
}
```

`default(InitializationContext)` is valid — `Intent = None`, no slots, `Metadata = null`.

### 2.5 `CallerIntent`

```csharp
public enum CallerIntent : byte  // not [Flags] — values are mutually exclusive
{
    None            = 0,   // non-instrumented caller, no guarantees
    WillInitialize  = 1,   // caller will call OnInitialized after construction
    CallInitialize  = 2,   // constructor should self-invoke OnInitialized at end of body
}
```

| Value | Meaning | Used when |
|---|---|---|
| `None` | No guarantee that `OnInitialized` will be called | Non-instrumented callers (generated overload) |
| `WillInitialize` | Caller will call `.OnInitialized()` after construction | Call sites with object initializers |
| `CallInitialize` | Constructor should self-invoke `OnInitialized` at the end of its body | Call sites without object initializers |

The source transformer chooses between `WillInitialize` and `CallInitialize` based on whether the call site uses an object initializer.
For `CallInitialize`, the constructor body ends with:

```csharp
if (context.Intent == CallerIntent.CallInitialize)
    OnInitialized();
```

### 2.6 `InitializationMetadata`

```csharp
/// <summary>
/// Base class for metadata attached to an <see cref="InitializationContext"/>.
/// Subclass to carry extension-specific context (e.g., deserialization framework info).
/// Instances should be singletons where possible to avoid allocation.
/// </summary>
public class InitializationMetadata
{
    protected InitializationMetadata() { }

    /// <summary>
    /// Normal construction. This is the implicit metadata when
    /// <see cref="InitializationContext.Metadata"/> is <c>null</c>.
    /// </summary>
    public static InitializationMetadata Default { get; } = new();

    /// <summary>
    /// The object was created via a <c>with</c> expression or clone operation.
    /// <c>OnInitialized</c> should revalidate invariants and reinitialize derived state.
    /// </summary>
    public static InitializationMetadata Modify { get; } = new();

}
```

`Default` and `Modify` are the two system-supported scenarios:

| Instance | Meaning | Passed by |
|---|---|---|
| `Default` (or `null`) | Normal construction | Implicit — no metadata needed |
| `Modify` | `with` expression or clone | Source transformer at `with` call sites |

Extension packages (e.g., serialization frameworks) can subclass `InitializationMetadata` to carry additional context:

```csharp
// Example: System.Text.Json deserialization extension
public class SystemTextJsonInitializationMetadata : InitializationMetadata
{
    public static SystemTextJsonInitializationMetadata Default { get; } = new();

    public JsonSerializerOptions? Options { get; init; }
}
```

### 2.7 Constructor Usage Pattern

Constructors accept an `InitializationContext` parameter so the caller (or a source transformer acting on its behalf) can pass the appropriate `CallerIntent`:

- **`CallInitialize`** (no object initializer): the constructor self-invokes `OnInitialized` at the end of its body, since all properties are set by the constructor
- **`WillInitialize`** (object initializer): the constructor does **not** self-invoke — the caller will call `.OnInitialized()` after the object initializer completes
- **`None`** (non-instrumented caller): `OnInitialized` will not be called automatically

> **Note on `WillCallOnInitialized`:** This property returns `true` when `Intent` is either `WillInitialize` or `CallInitialize`.
> It is a **promise** that `OnInitialized` will be called — either by the instrumented code at the call site, or by the outermost constructor itself.
> The constructor can rely on this promise to defer work to `OnInitialized` instead of performing it eagerly.
>
> When `false` (non-instrumented caller), no such promise exists.
> The constructor may choose to perform eager initialization, accept that validation will be skipped, or throw to prevent non-instrumented construction of types that require post-initialization.

```csharp
public class Range
{
    public Range(InitializationContext context = default)
    {
        // Only self-invoke if this is the outermost constructor (not chained via :this)
        if (context.Intent == CallerIntent.CallInitialize)
            OnInitialized();
    }

    public Range(int min, int max, InitializationContext context = default)
        : this(context.WillCallOnInitialized
            ? InitializationContext.WillInitialize
            : default) // preserve promise without triggering self-invocation in chained ctor
    {
        Min = min;
        Max = max;
        if (context.Intent == CallerIntent.CallInitialize)
            OnInitialized();
    }
}
```

> **Note:** When constructors chain via `: this(...)`, the chained constructor must not receive the original context directly (to prevent double self-invocation of `OnInitialized`).
> Instead, the chaining expression passes `WillInitialize` when the outermost context promises that `OnInitialized` will be called (`WillCallOnInitialized == true`), and `default` otherwise (non-instrumented callers, where no promise exists).
> Only the outermost constructor — the one the caller invoked — checks `CallInitialize`.
> The code model handles this automatically when introducing the `InitializationContext` parameter.

---

## 3. Metalama Linker Behavior

The Metalama Linker analyzes all object construction call sites and transforms them according to the following rules.

### 3.1 `InitializationContext` Parameter Supply

At any instrumented construction call site where the constructor has an `InitializationContext` parameter, the Linker chooses the appropriate `CallerIntent` based on the call site form.

**If the user code already supplies an `InitializationContext` argument, the Linker does not modify the call** — the user has explicitly chosen the intent and takes responsibility for calling `OnInitialized` if needed.
The rules below apply only when no argument is provided:

| Call site form | Pass to constructor | Append `.OnInitialized()` |
|---|---|---|
| `new T(...)` (no object initializer) | `CallInitialize` | No — constructor self-invokes |
| `new T(...) { ... }` (object/collection initializer) | `WillInitialize` | Yes |
| `with { ... }` expression | N/A (copy ctor is unmodified) | Yes, with `Metadata = Modify` |

For `new` expressions **without** object initializers, the Linker passes `CallInitialize` — the constructor self-invokes `OnInitialized` at the end of its body, producing a compact call site.
For `new` expressions **with** object initializers, the Linker passes `WillInitialize` and appends `.OnInitialized()`, since `init` properties are set after the constructor returns.
For `with` expressions, the copy constructor is compiler-generated and cannot be modified — the Linker only appends `.OnInitialized()` with `InitializationMetadata.Modify` on the cloned instance.

Collection initializers (`new T { item1, item2 }`) are treated identically to object initializers — there is no special handling for them.

The compatibility overload (§4) is the only case where `default` is passed — and that is because the caller is non-instrumented, not because of call site form.

### 3.2 `OnInitialized` Invocation

At any construction call site where the type has `[OnInitialized]`, the Linker rewrites the construction expression according to the call site form:

**Constructor-only (no object initializer) — `CallInitialize`:**
```csharp
// Original
var r = new Range(1, 12);

// Rewritten — compact, constructor self-invokes OnInitialized
var r = new Range(1, 12, InitializationContext.CallInitialize);
```

**Object initializer — `WillInitialize`:**
```csharp
// Original
var r = new Range { Min = 1, Max = 12 };

// Rewritten — caller invokes OnInitialized after init properties are set
var r = new Range(InitializationContext.WillInitialize) { Min = 1, Max = 12 }
    .OnInitialized();
```

**`with` expression — `Metadata = Modify`:**
```csharp
// Original
var r2 = r1 with { Max = 15 };

// Rewritten
var r2 = (r1 with { Max = 15 })
    .OnInitialized(InitializationContext.Create(InitializationMetadata.Modify));
```

**Without `InitializationContext` constructor parameter:**
```csharp
// Original
var r = new Range { Min = 1, Max = 12 };

// Rewritten — only .OnInitialized() appended
var r = new Range { Min = 1, Max = 12 }.OnInitialized();
```

**Return type cast:** When the `OnInitialized` method is declared on a base type and its return type is not the constructed type, the Linker must emit a cast.
For example, if `OnInitialized` is declared on `Shape` returning `Shape`, but the call site constructs a `Circle`:

```csharp
// OnInitialized returns Shape, but we need Circle
var c = (Circle) new Circle(InitializationContext.WillInitialize) { Radius = 5 }
    .OnInitialized();
```

### 3.3 Diagnostics

The Linker is responsible for validating the `[OnInitialized]` method contract defined in §1.2 and reporting diagnostics when violations are detected.

> **Remark:** Diagnostic codes below are provisional. Before release they must be replaced by `LAMAXXXX` codes allocated in free slots in the Metalama diagnostic registry.

| Code | Severity | Condition |
|---|---|---|
| `OI001` | Warning | `[OnInitialized]` method return type is not the declaring type (caller will need to cast) |
| `OI002` | Error | `[OnInitialized]` method has parameters other than `InitializationContext` |
| `OI003` | Error | `[OnInitialized]` method return type is `void` |
| `OI004` | Warning | `[OnInitialized]` method is not `virtual` on a non-sealed class (prevents derived types from overriding) |

---

## 4. Constructor Parameter Introduction

The `InitializationContext` parameter can be present on a constructor in two ways:

1. **User code** declares the parameter manually on the constructor.
2. **Aspect code** causes the parameter to be introduced automatically. When an `AddInitializer(InitializerKind.OnInitialized)` or `AddInitializer(InitializerKind.AfterLastInstanceConstructor)` advice is applied, the `InitializationContext` parameter is automatically introduced on the target constructor using the existing `IntroduceParameter` advice API with `IPullStrategy`.

When introduced by an aspect, the parameter is added using the existing `IntroduceParameter` API, not Linker-time code generation.
This is architecturally consistent with how Metalama introduces other constructor parameters.

The parameter is introduced using a new **overload generation mode**: instead of adding an optional parameter with a default value, `IntroduceParameter` generates a separate constructor overload **without** the `InitializationContext` parameter that forwards `default` internally.

This gives a cleaner contract than optional parameters — non-instrumented callers cannot pass arbitrary `InitializationContext` values; they always get `default`.

**Overload generation mode behavior:**

- The `InitializationContext` parameter on the original constructor has **no default value**
- A new constructor overload is generated **without** the `InitializationContext` parameter, preserving all other parameters
- The generated overload passes `default` — **not** `InitializationContext.WillInitialize`
- The generated overload does **not** call `OnInitialized`

For positional records, `IntroduceParameter` already handles primary constructor materialization: the parameter is appended to the primary constructor, and a `Deconstruct` method matching the original parameter list is auto-generated to preserve backward compatibility.

**Pull strategy:** Child constructors (via `: base(...)`) propagate the parameter using `PullAction.IntroduceParameterAndPull`, which works across project boundaries.

**Rationale:** The generated overload cannot know whether the caller will set additional properties via an object initializer after the constructor returns.
Object initializers run *after* the constructor body — so calling `OnInitialized` inside the constructor would fire before those properties are set, which is exactly the problem this feature is designed to prevent.

> **⚠ Non-instrumented caller limitation:** Non-instrumented callers (external assemblies, reflection, `Activator.CreateInstance`) will **not** have `OnInitialized` called automatically — neither via the constructor nor after object initializers.
> This is an inherent limitation of the design: without Linker call-site rewriting, there is no safe point to invoke `OnInitialized`.
> The generated overload intentionally does *not* self-invoke `OnInitialized` to avoid premature firing before `init` properties are set.
>
> **Mitigation strategies to consider:**
> - Emit a Roslyn analyzer diagnostic when a non-instrumented call site constructs a type with `[OnInitialized]` without calling `.OnInitialized()` afterward
> - Document the limitation prominently in the API documentation for `[OnInitialized]`
> - Consider a runtime assertion (e.g., `Debug.Assert`) that detects first property access after construction without `OnInitialized` having been called

**Example — original authored code:**
```csharp
public Range(int min, int max, InitializationContext context = default) { ... }
```

**After `IntroduceParameter` with overload generation mode:**
```csharp
// Original — default value removed
public Range(int min, int max, InitializationContext context) { ... }

// Generated overload for non-instrumented callers
// Passes default — OnInitialized will NOT be called automatically.
public Range(int min, int max)
    : this(min, max, default)
{
}
```

---

## 5. `AddInitializer` with `InitializerKind.OnInitialized`

### 5.1 Overview

`AddInitializer(InitializerKind.OnInitialized)` injects statement templates into an `[OnInitialized]` method.
It is a pure code model transformation with no direct link to the Linker — the Linker independently detects the resulting `[OnInitialized]` method by attribute.

An aspect may optionally declare one or more `InitializationSlot` fields and pass references to them via `AddInitializer`.
This enables cross-layer coordination — the code model uses the slot fields to emit the correct `Descend(slots)` call, and the template uses `IsHandledBy` to skip behavior already guaranteed by a derived type.

### 5.2 Signature

This extends the existing `AddInitializer` API with a new `InitializerKind.OnInitialized` value and an optional `slotFields` parameter:

```csharp
IAddInitializerAdviceResult AddInitializer(
    string template,
    InitializerKind kind,
    object? tags = null,
    object? args = null,
    IEnumerable<IField>? slotFields = null);
```

`slotFields` are references to `public static readonly InitializationSlot` fields on the aspect type.
Passing `null` or omitting the argument means the template always runs regardless of derived type behavior.
This parameter is meaningful when `kind` is `InitializerKind.OnInitialized` or `AfterLastInstanceConstructor`.

**Template parameter:** Templates used with `InitializerKind.OnInitialized` or `AfterLastInstanceConstructor` may optionally declare an `InitializationContext` parameter in the template. This controls whether the template body can access slots and metadata — it does not affect the introduced method's signature, which always includes `InitializationContext context = default` (see §5.3).
When the template declares the parameter, the code model maps it from the enclosing method's parameter.

### 5.3 Method Introduction

When the first `AddInitializer(InitializerKind.OnInitialized)` advice is applied to a type with no existing `[OnInitialized]` method, the code model introduces one:

- Decorated with `[OnInitialized]`
- `public virtual T OnInitialized(InitializationContext context = default)` where `T` is the declaring type
- Body contains injected templates in aspect order, followed by `return this`

Subsequent advice appends templates before `return this`.

**Templates are always `void`-returning statement blocks** — they never emit `return`.
`return this` is owned by the introduced method frame and is always the final statement.

**The `InitializationContext` parameter is always present on the introduced method**, even if no template currently declares it. Templates may optionally declare an `InitializationContext` parameter to access slots and metadata, but this is independent of the method signature. This is mandatory because:
- **User code**: a hand-authored `[OnInitialized]` method may want to inspect the context (e.g., check `Metadata`, `IsHandledBy`) regardless of what aspects are applied
- **Multi-aspect**: multiple aspects may contribute templates to the same `OnInitialized` method, and a later aspect cannot retroactively add a parameter to an already-introduced method
- **Cross-project inheritance**: the base class method may already be compiled without the parameter, making it impossible for a derived project to fix the signature

The same rule applies to the `OnConstructed` method introduced by `AfterLastInstanceConstructor`.

### 5.4 Inheritance

When the code model introduces `OnInitialized` on a derived type whose base already has `[OnInitialized]`:

- `base.OnInitialized(context.Descend(slot1 | slot2 | ...))` is emitted as the **first statement**, passing the union of all slots declared by aspects applied to the derived type
- The return type is the derived type (covariant)
- If the derived type already has a hand-authored `[OnInitialized]`, templates are appended without modifying the existing `base.OnInitialized(...)` call

> **Slot contract obligation:** When a derived type passes a slot to `Descend()`, it signals that the base should skip the behavior associated with that slot.
> The derived type's template is then responsible for handling that behavior for **all** members, including inherited ones.

### 5.5 Example

```csharp
// Aspect A — change tracking
class ChangeTrackingAspect : TypeAspect
{
    public static readonly InitializationSlot Slot = InitializationSlot.Allocate();

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var slotField = TypeFactory.GetNamedType(typeof(ChangeTrackingAspect))
            .Fields.OfName(nameof(Slot)).Single();

        builder.AddInitializer(nameof(Template), InitializerKind.OnInitialized,
            slotFields: new[] { slotField });
    }

    [Template]
    void Template(InitializationContext context)
    {
        if (!context.IsHandledBy(ChangeTrackingAspect.Slot))
            _changeTracker = new ChangeTracker(this);
    }
}

// Aspect B — validation
class ValidationAspect : TypeAspect
{
    public static readonly InitializationSlot Slot = InitializationSlot.Allocate();

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var slotField = TypeFactory.GetNamedType(typeof(ValidationAspect))
            .Fields.OfName(nameof(Slot)).Single();

        builder.AddInitializer(nameof(Template), InitializerKind.OnInitialized,
            slotFields: new[] { slotField });
    }

    [Template]
    void Template(InitializationContext context)
    {
        if (!context.IsHandledBy(ValidationAspect.Slot))
            Validator.Validate(this);
    }
}

// Base class — both aspects applied
[ChangeTracking, Validation]
public record class Range(int Min, int Max) { }

// Derived class — only Validation applied
[Validation]
public record class NamedRange(int Min, int Max, string Name) : Range(Min, Max) { }
```

After transformation:

```csharp
// Base — introduced by code model
[OnInitialized]
public virtual Range OnInitialized(InitializationContext context = default)
{
    if (!context.IsHandledBy(ChangeTrackingAspect.Slot))  // ChangeTrackingAspect
        _changeTracker = new ChangeTracker(this);

    if (!context.IsHandledBy(ValidationAspect.Slot))      // ValidationAspect
        Validator.Validate(this);

    return this;
}

// Derived — override introduced by code model
// ValidationAspect.Slot passed to Descend() — base will skip validation
// ChangeTrackingAspect not applied to NamedRange — base will run it
[OnInitialized]
public override NamedRange OnInitialized(InitializationContext context = default)
{
    base.OnInitialized(context.Descend(ValidationAspect.Slot));

    if (!context.IsHandledBy(ValidationAspect.Slot))      // ValidationAspect
        Validator.Validate(this);

    return this;
}
```

---

## 6. `AddInitializer` with `InitializerKind.AfterLastInstanceConstructor`

### 6.1 Overview

`AddInitializer(InitializerKind.AfterLastInstanceConstructor)` injects template statements that run at the **end** of constructors, after all user constructor code.
Unlike `BeforeInstanceConstructor` (which injects directly into constructor bodies), this kind:

1. **Introduces a virtual method** `OnConstructed(InitializationContext context = default)` on the target type
2. **Injects the template body** into `OnConstructed`
3. **Pulls an `InitializationContext` parameter** into constructors (via the constructor parameter introduction mechanism / `IPullStrategy`)
4. **Adds a call to `OnConstructed`** at the end of each constructor, but only in the most-derived layer (coordinated via an internal `InitializationSlot`)

This is complementary to `BeforeInstanceConstructor` (which injects at the **beginning**, after `:base()`) and `OnInitialized` (which introduces a separate method called after object initializers complete).

### 6.2 Semantics

| Aspect | `BeforeInstanceConstructor` | `AfterLastInstanceConstructor` | `OnInitialized` |
|---|---|---|---|
| **Where** | Beginning of constructor body (after `:base()`) | Separate `OnConstructed` method, called at end of constructor | Separate `OnInitialized` method, after object initializers |
| **Which constructors** | All except `:this()` chains | All except `:this()` chains (via `OnConstructed` call) | N/A (called after object initializers) |
| **When** | Before user constructor code | After user constructor code | After object initializers and `init` properties |
| **Object initializer awareness** | No | No | Yes |
| **Introduced method** | None | `OnConstructed` | `OnInitialized` |

### 6.3 `OnConstructed` Method

The introduced method follows the same pattern as `OnInitialized`:

```csharp
public virtual T OnConstructed(InitializationContext context = default)
{
    // base.OnConstructed(context.Descend(...)) — if base has OnConstructed
    // template statements injected here
    return this;
}
```

- Return type is the declaring type (covariant)
- `virtual` on non-sealed classes, non-virtual on sealed classes and structs
- `override` when a base type already has `OnConstructed`
- **No `[OnConstructed]` attribute** — unlike `[OnInitialized]`, this is a system concept, not a user concept. The method name is hardcoded by the advice. Users do not hand-author `OnConstructed` methods.
- **No Linker involvement** — the call to `OnConstructed` is wired by extending the existing constructor parameter append/pull mechanism (transitive aspect manifest) with an option to emit the `OnConstructed` call. This reuses the infrastructure already implemented for `InitializationContext` parameter propagation (§4).

### 6.4 `InitializationSlot` Coordination

The feature internally allocates an `InitializationSlot` to ensure that only the **most-derived** constructor in an inheritance chain calls `OnConstructed`.

In a hierarchy where both `Base` and `Derived` use `AfterLastInstanceConstructor`:
- `Derived`'s constructor passes `context.Descend(slot)` to the base constructor
- `Base`'s constructor sees `IsHandledBy(slot) == true` and skips the `OnConstructed` call
- Only `Derived`'s constructor (the most-derived) invokes `OnConstructed`

### 6.5 Constructor Wiring

The `OnConstructed` call is wired by extending the existing constructor parameter append/pull mechanism. When the `InitializationContext` parameter is pulled into a constructor (via the transitive aspect manifest), the pull strategy is configured with an additional option to emit the `OnConstructed(context)` call at the end of the constructor body. This keeps the wiring logic co-located with the existing parameter propagation infrastructure rather than introducing a separate code generation path.

Constructors may contain early `return` statements. The implementation rewrites all `return;` statements into `goto __end;` and appends a labeled block at the end of the constructor body:

```csharp
__end:
OnConstructed(context);
```

This ensures `OnConstructed` is called on all exit paths without the overhead of `try/finally`.

Execution order within a constructor body:
1. `: base(...)` call
2. `BeforeInstanceConstructor` statements (in aspect order)
3. User constructor code
4. `OnConstructed(context)` call (only in most-derived constructor — coordinated via `InitializationSlot`)

### 6.6 Use Cases

- Initialization logic that must run after the constructor body has finished setting fields (e.g., registering the object in a lookup table, starting a timer)
- Logic that depends on constructor body code having run, but does not need to wait for object initializers

### 6.7 Interaction with `OnInitialized`

When both `AfterLastInstanceConstructor` and `OnInitialized` are used on the same type:

- `OnConstructed` runs at the end of the constructor body, **before** `OnInitialized`
- `OnInitialized` runs after object initializers (if any) complete

Execution order for the full lifecycle:
1. Constructor body (including `BeforeInstanceConstructor` + user code)
2. `OnConstructed(context)` — at end of constructor
3. Object initializer (`init` properties) — if present
4. `OnInitialized(context)` — after object initializers

---

## 7. Record Support

### 7.1 Scope

`class` and `struct` records are fully supported, including positional records.
Note that structs can be default-constructed (`default(T)`), bypassing all constructors.
This is an inherent limitation of value types in .NET and is not specific to this feature.

### 7.2 Positional Records — Copy Constructor

The copy constructor for positional records is compiler-generated and is **not modified** by the Linker.
It remains a pure field copy.
Post-copy initialization is handled entirely by appending `.OnInitialized()` with `InitializationMetadata.Modify` at `with` expression call sites (see §7.4).

Implementations of `OnInitialized` must support being called with `Metadata = Modify` on an instance that was created via the copy constructor.
This means `OnInitialized` may be called multiple times over the lifetime of an object graph (once at initial construction, and once after each `with` expression).

### 7.3 Non-Positional Records

No special treatment — the author controls the copy constructor and the standard pattern from §3 applies as-is (call-site rewriting by the Linker works identically).

### 7.4 `with` Expression Call Sites

The Linker rewrites `with` expressions to call `OnInitialized` with `InitializationMetadata.Modify` on the cloned instance.
The copy constructor itself is not modified — it performs its standard field copy, and `OnInitialized` runs afterward to revalidate or recompute derived state:

```csharp
// Original
var r2 = r1 with { Max = 15 };

// Rewritten
var r2 = (r1 with { Max = 15 })
    .OnInitialized(InitializationContext.Create(InitializationMetadata.Modify));
```

`OnInitialized` implementations must handle `Metadata = Modify` correctly — for example, revalidating invariants against the new property values, or reinitializing derived state such as caches or change trackers.
Since `with` can be called repeatedly, `OnInitialized` must be safe to call multiple times.

---

## 8. Serialization and Cloning

### 8.1 `ISerializationFramework`

Serialization and cloning are treated as the same concern — in both cases the object is fully populated by an external mechanism and `OnInitialized` must be called afterward.
The `InitializationMetadata` passed to `OnInitialized` carries the distinction if needed.

```csharp
public interface ISerializationFramework
{
    void ImplementPostInitialization( INamedType type, IMethod onInitializedMethod );
}
```

The implementor introduces or injects into whatever hook the framework provides (`[OnDeserialized]`, `[ProtoAfterDeserialization]`, `MemberwiseClone` override, etc.) and calls `OnInitialized` with the appropriate context:

```csharp
// Post-deserialization (System.Text.Json)
instance.OnInitialized(InitializationContext.Create(SystemTextJsonInitializationMetadata.Default));

// Post-clone
instance.OnInitialized(InitializationContext.Create(InitializationMetadata.Modify));
```

> **Remark:** The mechanism by which `ISerializationFramework` implementations are discovered and called is not yet specified.
> This is intentionally left for a later design iteration.

### 8.2 Planned Implementations

| Package | Framework |
|---|---|
| `Metalama.Extensions.Serialization.SystemTextJson` | `System.Text.Json` |
| `Metalama.Extensions.Serialization.NewtonsoftJson` | `Newtonsoft.Json` |
| `Metalama.Extensions.Serialization.ProtobufNet` | `protobuf-net` |

---

## 9. Case Study: Automatic Parent–Child Wiring

### 9.1 Problem

A common pattern in domain models is a parent–child relationship where each child holds a back-reference to its parent.
With `init`-only properties and object initializers, the parent cannot wire children in the constructor because the child properties may not yet be set:

```csharp
// The parent property CANNOT be set in the constructor — Children is not assigned yet
var tree = new TreeNode
{
    Name = "root",
    Children = new[]
    {
        new TreeNode { Name = "left" },
        new TreeNode { Name = "right" }
    }
};
// tree.Children[0].Parent is null — nobody wired it
```

`[OnInitialized]` solves this by running after all `init` properties are assigned.

### 9.2 Contracts

```csharp
/// <summary>
/// Implemented by child objects that maintain a back-reference to their parent.
/// </summary>
public interface IChildObject<TParent> where TParent : class
{
    TParent? Parent { get; set; }
}

/// <summary>
/// Marks a field or property whose value(s) should have their Parent set to this instance.
/// Supported member types: T, IEnumerable<T>, where T : IChildObject<TParent>.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ChildAttribute : Attribute { }
```

### 9.3 Aspect

```csharp
[Inheritable]
class ParentChildAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.AddInitializer(
            nameof(WireChildren),
            InitializerKind.OnInitialized,
            args: new { TParent = builder.Target });
    }

    [Template]
    void WireChildren<[CompileTime] TParent>(InitializationContext context) where TParent : class
    {
        // Compile-time loop: the template expands to one block per [Child] member
        foreach (var member in meta.Target.Type.FieldsAndProperties
            .Where(m => m.Attributes.Any(typeof(ChildAttribute))))
        {
            if (member.Type.Is(typeof(IEnumerable<>)))
            {
                // Collection child — wire each element
                var collection = member.Value;
                if (collection != null)
                {
                    foreach (var child in (IEnumerable)collection!)
                    {
                        ((IChildObject<TParent>)child).Parent = (TParent)(object)meta.This;
                    }
                }
            }
            else
            {
                // Scalar child — wire directly
                var child = member.Value;
                if (child != null)
                {
                    ((IChildObject<TParent>)child!).Parent = (TParent)(object)meta.This;
                }
            }
        }
    }
}
```

No `InitializationSlot` is needed here: each layer in an inheritance hierarchy wires only its own declared `[Child]` members, so there is no cross-layer coordination.

### 9.4 User Code

```csharp
[ParentChild]
public class TreeNode : IChildObject<TreeNode>
{
    public required string Name { get; init; }

    [Child]
    public required IReadOnlyList<TreeNode> Children { get; init; }

    // Introduced or authored — receives back-reference from parent's OnInitialized
    public TreeNode? Parent { get; set; }
}
```

### 9.5 After Transformation

```csharp
public class TreeNode : IChildObject<TreeNode>
{
    public required string Name { get; init; }

    [Child]
    public required IReadOnlyList<TreeNode> Children { get; init; }

    public TreeNode? Parent { get; set; }

    [OnInitialized]
    public virtual TreeNode OnInitialized(InitializationContext context = default)
    {
        // Wiring for [Child] Children
        var collection = this.Children;
        if (collection != null)
        {
            foreach (var child in collection)
            {
                ((IChildObject<TreeNode>)child).Parent = this;
            }
        }

        return this;
    }
}
```

### 9.6 Call Sites

**instrumented (after Linker rewriting):**
```csharp
var tree = new TreeNode(InitializationContext.WillInitialize)
{
    Name = "root",
    Children = new[]
    {
        new TreeNode(InitializationContext.WillInitialize)
        {
            Name = "left",
            Children = Array.Empty<TreeNode>()
        }.OnInitialized(),
        new TreeNode(InitializationContext.WillInitialize)
        {
            Name = "right",
            Children = Array.Empty<TreeNode>()
        }.OnInitialized()
    }
}.OnInitialized();

// tree.Children[0].Parent == tree  ✓
// tree.Children[1].Parent == tree  ✓
```

### 9.7 `with` Expression — Re-wiring on Modify

When a `with` expression clones a node, the clone's children still reference the old parent.
The Linker appends `.OnInitialized(Modify)` which re-wires them:

```csharp
// Original
var renamed = tree with { Name = "renamed-root" };

// Rewritten by Linker
var renamed = (tree with { Name = "renamed-root" })
    .OnInitialized(InitializationContext.Create(InitializationMetadata.Modify));

// renamed.Children[0].Parent == renamed  ✓  (re-wired by OnInitialized)
```

> **Design note:** After re-wiring, the original `tree.Children[0].Parent` now points to `renamed`, not `tree` — the children are shared references.
> If the original parent must remain valid, the `OnInitialized(Modify)` implementation should deep-clone the children instead of re-wiring.
> The aspect can inspect `context.Metadata` to choose the strategy:
>
> ```csharp
> if (context.Metadata == InitializationMetadata.Modify)
> {
>     // Deep-clone children to avoid aliasing the original parent's children
>     Children = Children.Select(c => c with { }).ToList();
> }
> // Then wire parent on the (possibly cloned) children
> ```
>
> Whether to re-wire or deep-clone is a domain-specific decision.
> The aspect should either pick one strategy or expose a configuration option.

---

## 10. Case Study: Freezable Objects

### 10.1 Problem

Some domain models need objects that are mutable during a setup phase — properties with regular `set` accessors, mutable `IList<T>` collections — but become immutable once fully initialized.
The "freeze" pattern achieves this by flipping a flag after construction, causing subsequent mutations to throw.

This cannot be done in the constructor: the object must remain mutable while properties are being assigned (including via object initializers, builder patterns, or deserialization).
`[OnInitialized]` provides the right hook — it runs after all properties are set.

In a class hierarchy, freezing must happen **once** at the most-derived level.
If a base class freezes in its `OnInitialized`, it blocks the derived class from completing its own setup.
`InitializationSlot` coordination solves this: each layer defers freezing if a derived layer guarantees it will freeze.

### 10.2 Aspect

```csharp
[Inheritable]
class FreezableAspect : TypeAspect
{
    public static readonly InitializationSlot Slot = InitializationSlot.Allocate();

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var slotField = TypeFactory.GetNamedType(typeof(FreezableAspect))
            .Fields.OfName(nameof(Slot)).Single();

        // Introduce IsFrozen property and Freeze method — Ignore if already introduced
        // by a base aspect layer; Override to extend with derived-level logic
        builder.IntroduceProperty(nameof(IsFrozen),
            whenExists: OverrideStrategy.Ignore);
        builder.IntroduceMethod(nameof(Freeze),
            whenExists: OverrideStrategy.Override);

        // Override mutable property setters to guard against mutation after freeze
        foreach (var property in builder.Target.Properties
            .Where(p => p.Writeability == Writeability.All
                     && !p.IsStatic
                     && p.DeclaringType == builder.Target))
        {
            builder.With(property).Override(nameof(PropertyTemplate));
        }

        // Add the freeze logic to OnInitialized
        builder.AddInitializer(
            nameof(FreezeTemplate),
            InitializerKind.OnInitialized,
            slotFields: new[] { slotField });
    }

    [Template]
    public bool IsFrozen { get; private set; }

    /// <summary>
    /// Freezes the object: wraps mutable collections declared on this type in read-only
    /// wrappers, then chains to base. The base-most Freeze() sets the frozen flag.
    /// </summary>
    [Template]
    protected virtual void Freeze()
    {
        // Wrap mutable list properties declared on this type
        foreach (var member in meta.Target.Type.FieldsAndProperties
            .Where(m => !m.IsStatic
                     && m.Type.Is(typeof(IList<>))
                     && m.DeclaringType == meta.Target.Type)
        {
            var list = member.Value;
            if (list != null)
            {
                member.Value = new ReadOnlyCollection(list);
            }
        }

        if (meta.Target.Method.IsOverride)
        {
            // Chain to base Freeze() — base sets the flag
            meta.Proceed();
        }
        else
        {
            // Base-most type — set the flag
            IsFrozen = true;
        }
    }

    [Template]
    dynamic? PropertyTemplate
    {
        get => meta.Proceed();
        set
        {
            if (IsFrozen)
                throw new InvalidOperationException(
                    $"Cannot set '{meta.Target.Property.Name}': " +
                    $"the {meta.Target.Type.Name} instance is frozen.");
            meta.Proceed();
        }
    }

    [Template]
    void FreezeTemplate(InitializationContext context)
    {
        if (!context.IsHandledBy(FreezableAspect.Slot))
        {
            Freeze();
        }
    }
}
```

### 10.3 User Code

```csharp
[Freezable]
public class Shape
{
    public string Color { get; set; } = "Black";
    public IList<Point> Vertices { get; set; } = new List<Point>();
}

[Freezable]
public class LabeledShape : Shape
{
    public string Label { get; set; } = "";
    public double FontSize { get; set; } = 12.0;
    public IList<string> Tags { get; set; } = new List<string>();
}
```

### 10.4 After Transformation

```csharp
public class Shape
{
    public bool IsFrozen { get; private set; }

    protected virtual void Freeze()
    {
        Vertices = new ReadOnlyCollection<Point>(Vertices);
        IsFrozen = true;
    }

    public string Color
    {
        get;
        set
        {
            if (IsFrozen)
                throw new InvalidOperationException(
                    "Cannot set 'Color': the Shape instance is frozen.");
            field = value;
        }
    } = "Black";

    public IList<Point> Vertices
    {
        get;
        set
        {
            if (IsFrozen)
                throw new InvalidOperationException(
                    "Cannot set 'Vertices': the Shape instance is frozen.");
            field = value;
        }
    } = new List<Point>();

    [OnInitialized]
    public virtual Shape OnInitialized(InitializationContext context = default)
    {
        if (!context.IsHandledBy(FreezableAspect.Slot))
            Freeze();

        return this;
    }
}

public class LabeledShape : Shape
{
    public string Label
    {
        get;
        set
        {
            if (IsFrozen)
                throw new InvalidOperationException(
                    "Cannot set 'Label': the LabeledShape instance is frozen.");
            field = value;
        }
    } = "";

    public double FontSize
    {
        get;
        set
        {
            if (IsFrozen)
                throw new InvalidOperationException(
                    "Cannot set 'FontSize': the LabeledShape instance is frozen.");
            field = value;
        }
    } = 12.0;

    public IList<string> Tags
    {
        get;
        set
        {
            if (IsFrozen)
                throw new InvalidOperationException(
                    "Cannot set 'Tags': the LabeledShape instance is frozen.");
            field = value;
        }
    } = new List<string>();

    protected override void Freeze()
    {
        Tags = new ReadOnlyCollection<string>(Tags);
        base.Freeze();  // chains down — base sets IsFrozen = true
    }

    [OnInitialized]
    public override LabeledShape OnInitialized(InitializationContext context = default)
    {
        // Derived passes FreezableAspect.Slot → base skips freezing
        base.OnInitialized(context.Descend(FreezableAspect.Slot));

        if (!context.IsHandledBy(FreezableAspect.Slot))
            Freeze();

        return this;
    }
}
```

### 10.5 Why the Slot Matters

Without `InitializationSlot` coordination, the execution would be:

1. `LabeledShape.OnInitialized(context)` calls `base.OnInitialized(context.Descend())`
2. `Shape.OnInitialized` calls `Freeze()` — dispatches to `LabeledShape.Freeze()` (virtual) which wraps `Tags`, then chains to `base.Freeze()` which wraps `Vertices` and sets `IsFrozen = true`
3. Back in `LabeledShape.OnInitialized` — calls `Freeze()` again, which tries to wrap `Tags` via the setter — throws because `IsFrozen` is already `true`

With the slot:

1. `LabeledShape.OnInitialized(context)` calls `base.OnInitialized(context.Descend(FreezableAspect.Slot))`
2. `Shape.OnInitialized` sees `IsHandledBy(FreezableAspect.Slot) == true` — skips `Freeze()`
3. Back in `LabeledShape.OnInitialized` — calls `Freeze()` which wraps `Tags`, chains to `base.Freeze()` which wraps `Vertices` and sets `IsFrozen = true` — all setters succeed because the object is still unfrozen during the cascade

### 10.6 Call Site

```csharp
// User writes:
var shape = new LabeledShape
{
    Color = "Red",
    Vertices = new List<Point> { new(0, 0), new(1, 0), new(1, 1) },
    Label = "Triangle",
    Tags = new List<string> { "geometry", "demo" }
};

// After Linker rewriting:
var shape = new LabeledShape(InitializationContext.WillInitialize)
{
    Color = "Red",
    Vertices = new List<Point> { new(0, 0), new(1, 0), new(1, 1) },
    Label = "Triangle",
    Tags = new List<string> { "geometry", "demo" }
}.OnInitialized();

shape.Color = "Blue";  // throws InvalidOperationException: instance is frozen
shape.Tags.Add("new");  // throws NotSupportedException: collection is read-only
```

### 10.7 `WillCallOnInitialized` in Constructors

The `WillCallOnInitialized` flag lets the constructor distinguish instrumented from non-instrumented callers.
A freezable type can use this for a defensive strategy:

```csharp
public Shape(InitializationContext context = default)
{
    if (!context.WillCallOnInitialized)
    {
        // Non-instrumented caller — OnInitialized will NOT be called.
        // Option A: log a warning that the object will remain mutable
        // Option B: throw to prevent non-instrumented construction
        throw new InvalidOperationException(
            "Shape must be constructed through an instrumented call site " +
            "to guarantee freeze-after-init semantics. " +
            "Alternatively, call .OnInitialized() manually after setting all properties.");
    }
}
```

This makes the contract explicit: if the caller cannot guarantee that `OnInitialized` will run, the type refuses construction rather than silently producing an unfrozen instance.
Non-instrumented callers that understand the pattern can still construct the object by calling `.OnInitialized()` manually after setting all properties.

---

## 11. Case Study: Computed Derived Properties

> **Note:** This pattern is often better addressed with `[Memo]` (lazy memoization).
> This case study demonstrates how `[OnInitialized]` can solve it when eager computation is preferred.

### 11.1 Problem

A scientific type has `required init` properties and read-only derived values that must be computed from them.
Without a post-initialization hook, the author must either:

- Compute on every access via `=>` (performance cost on repeated reads)
- Use `private set` instead of true read-only (sacrifices immutability guarantees)
- Duplicate computation in every `init` accessor (fragile, error-prone)

### 11.2 User Code

```csharp
public class Isotope
{
    public required double HalfLifeSeconds { get; init; }
    public required int AtomicNumber { get; init; }
    public required int MassNumber { get; init; }

    // Derived — computed once after all properties are set
    public double DecayConstant { get; private set; }
    public double MeanLifetime { get; private set; }

    [OnInitialized]
    public virtual Isotope OnInitialized(InitializationContext context = default)
    {
        DecayConstant = Math.Log(2) / HalfLifeSeconds;
        MeanLifetime = 1.0 / DecayConstant;
        return this;
    }
}

/// <summary>
/// A sample of a radioactive isotope with a known initial number of atoms.
/// Activity (decays per second) depends on DecayConstant (computed by base)
/// and InitialAtoms — so it must be computed after both are available.
/// </summary>
public class RadioactiveSample : Isotope
{
    public required double InitialAtoms { get; init; }

    // Derived — depends on DecayConstant from base + InitialAtoms from this type
    public double Activity { get; private set; }

    [OnInitialized]
    public override RadioactiveSample OnInitialized(InitializationContext context = default)
    {
        base.OnInitialized(context.Descend());
        Activity = DecayConstant * InitialAtoms;
        return this;
    }
}
```

### 11.3 Call Sites

```csharp
// Object initializer — Linker appends .OnInitialized()
var sample = new RadioactiveSample
{
    HalfLifeSeconds = 1.808e11,
    AtomicNumber = 6,
    MassNumber = 14,
    InitialAtoms = 1e24
};
// After rewriting:
// var sample = new RadioactiveSample(InitializationContext.WillInitialize)
//     { HalfLifeSeconds = 1.808e11, AtomicNumber = 6, MassNumber = 14, InitialAtoms = 1e24 }
//     .OnInitialized();

// All derived values are already computed
Console.WriteLine(sample.DecayConstant);  // from base
Console.WriteLine(sample.Activity);       // from derived (= DecayConstant * InitialAtoms)
```

### 11.4 `with` Expression

```csharp
// Changing a source property recomputes all derived values through the chain
var adjusted = sample with { InitialAtoms = 5e23 };
// After rewriting:
// var adjusted = (sample with { InitialAtoms = 5e23 })
//     .OnInitialized(InitializationContext.Create(InitializationMetadata.Modify));

// Activity reflects the new InitialAtoms; DecayConstant is unchanged
Console.WriteLine(adjusted.Activity);
```

---

## 12. Case Study: Object Lifecycle Tracking

### 12.1 Problem

A diagnostic or framework layer needs to track every instance of certain types through their lifecycle: `Constructing` (constructor is running), `Constructed` (constructor finished, but object initializers may not have run), and `Initialized` (fully ready).

This cannot be solved with default field values — each transition requires active registration in a static registry. And in an inheritance hierarchy, the `Constructed` status must only be set by the most-derived constructor: if `Base`'s constructor marks `Constructed` while `Derived`'s constructor body is still running, the registry reports incorrect state.

### 12.2 Infrastructure

```csharp
public enum ObjectStatus
{
    Constructing,
    Constructed,
    Initialized
}

public static class ObjectTracker
{
    private static readonly ConditionalWeakTable<object, StrongBox<ObjectStatus>> _registry = new();

    public static void Register(object instance, ObjectStatus status)
    {
        if (_registry.TryGetValue(instance, out var box))
            box.Value = status;
        else
            _registry.AddOrUpdate(instance, new StrongBox<ObjectStatus>(status));
    }

    public static ObjectStatus GetStatus(object instance)
        => _registry.TryGetValue(instance, out var box) ? box.Value : throw new InvalidOperationException("Not tracked");
}
```

### 12.3 Aspect

```csharp
[Inheritable]
public class TrackableAspect : TypeAspect
{
    public static readonly InitializationSlot Slot = InitializationSlot.Allocate();

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var slotField = TypeFactory.GetNamedType(typeof(TrackableAspect)).Fields.OfName(nameof(Slot)).Single();
        var slotFields = new[] { slotField };

        // Register as Constructing at the start of the base-most constructor only.
        // Since BeforeInstanceConstructor does not support slot coordination,
        // we detect the base-most constructor by checking whether the aspect
        // has been applied to the base type.
        var isBasemost = !builder.Target.BaseType!.Enhancements().HasAspect<TrackableAspect>();

        if (isBasemost)
        {
            builder.AddInitializer(
                nameof(OnConstructing),
                InitializerKind.BeforeInstanceConstructor);
        }

        // Register as Constructed at the end of the most-derived constructor
        builder.AddInitializer(
            nameof(OnConstructed),
            InitializerKind.AfterLastInstanceConstructor,
            slotFields: slotFields);

        // Register as Initialized after object initializers complete
        builder.AddInitializer(
            nameof(OnFullyInitialized),
            InitializerKind.OnInitialized,
            slotFields: slotFields);
    }

    [Template]
    private void OnConstructing()
    {
        ObjectTracker.Register(this, ObjectStatus.Constructing);
    }

    [Template]
    private void OnConstructed(InitializationContext context)
    {
        if (!context.IsHandledBy(Slot))
        {
            ObjectTracker.Register(this, ObjectStatus.Constructed);
        }
    }

    [Template]
    private void OnFullyInitialized(InitializationContext context)
    {
        if (!context.IsHandledBy(Slot))
        {
            ObjectTracker.Register(this, ObjectStatus.Initialized);
        }
    }
}
```

> **Detecting the base-most constructor:** Since `BeforeInstanceConstructor` does not support slot-based coordination (slots work in the "most-derived wins" direction, not "base-most wins"), the aspect detects whether it is the base-most layer by checking `HasAspect<TrackableAspect>()` on the base type. This strategy works because any class knows its base type with certainty (the base type is fixed and cannot be modified by aspects), whereas a base type does not know its derived types. This is the opposite of the "most-derived wins" problem, where `InitializationSlot` coordination is needed precisely because the base type cannot know at compile time whether a derived type will handle the concern. Other aspects may use different detection mechanisms depending on their needs — for example, checking for the presence of a specific method, interface implementation, or custom attribute on the base type.

### 12.4 User Code

```csharp
[Trackable]
public class Shape
{
    public string Color { get; init; } = "Red";

    public Shape()
    {
        // ObjectTracker.GetStatus(this) == Constructing here
    }
}

[Trackable]
public class LabeledShape : Shape
{
    public required string Label { get; init; }

    public LabeledShape() : base()
    {
        // ObjectTracker.GetStatus(this) == Constructing here
        // (Base constructor registered Constructing, but did NOT mark Constructed)
    }
}
```

### 12.5 Lifecycle Trace

```
new LabeledShape { Label = "A", Color = "Blue" }
```

| Step | What happens | Status |
|------|-------------|--------|
| 1 | `Shape` constructor entered — `BeforeInstanceConstructor` registers `Constructing` (base-most: `Shape` has no base with `[Trackable]`) | `Constructing` |
| 2 | `Shape` constructor body runs | `Constructing` |
| 3 | `Shape` constructor ends — `OnConstructed` **skipped** (slot: derived will handle) | `Constructing` |
| 4 | `LabeledShape` constructor entered — no `BeforeInstanceConstructor` (not base-most: `Shape` has `[Trackable]`) | `Constructing` |
| 5 | `LabeledShape` constructor body runs | `Constructing` |
| 6 | `LabeledShape` constructor ends — `OnConstructed` called (most-derived) | `Constructed` |
| 7 | Object initializer: `Label = "A"`, `Color = "Blue"` | `Constructed` |
| 8 | `.OnInitialized()` called by Linker | `Initialized` |
