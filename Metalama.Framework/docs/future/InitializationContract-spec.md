# Specification: `InitializationContext` and `IInitializable`

## Motivation

C# `init`-only properties and `required` members enable object initializer syntax for immutable types, but provide no mechanism to validate or compute derived values after all properties have been set.
Constructor-based validation is bypassed by `with` expressions on records.
This feature fills that gap without requiring compiler changes, via a library and the Metalama Linker.

See also the related C# language discussion: https://github.com/dotnet/csharplang/discussions/6591

A second motivation is the **telescoping constructor or initializer problem**: in an inheritance hierarchy, each layer may need to perform initialization logic (validation, change tracking, notification setup, etc.) but only once the entire object — including all derived layers — is fully initialized.
Without a post-initialization hook, each base class either runs its logic too early (before derived properties are set) or duplicates it at every layer.
`IInitializable` with `InitializationSlot`-based coordination solves this by letting each layer declare its behavior and skip it if a derived layer has already guaranteed it will run.

---

## 1. `IInitializable` Interface

`IInitializable` applies to classes, records, structs, and record structs.

### 1.1 Declaration

```csharp
public interface IInitializable
{
    void Initialize(InitializationContext context = default);
}

public static class InitializableExtensions
{
    public static T WithInitialize<T>(this T obj, InitializationMetadata? metadata = null)
        where T : IInitializable
    {
        obj.Initialize(InitializationContext.Create(metadata ?? InitializationMetadata.Default));
        return obj;
    }
}
```

### 1.2 Method Contract

The `IInitializable` interface enforces the method signature: `void` return type and a single `InitializationContext` parameter.

The only additional constraint is on virtuality:

| Constraint | Rule |
|---|---|
| Virtuality | `Initialize` must be `public virtual` (or `override`) on non-sealed classes implementing `IInitializable` |
| Count | Only **one** `Initialize` method per type. Multiple aspects inject their templates into the same method. |

### 1.3 Inheritance

- `Initialize` on a base class implementing `IInitializable` should be declared `virtual`
- A derived class **may** `override` it, but may also declare a separate `Initialize` via a new aspect — both will be executed
- The code model emits the `override` automatically, calling `base.Initialize(context.Descend(...))` before derived logic; if the author declares the override manually the code model leaves it untouched

> **Source generator ordering caveat:** When multiple source generators each add `Initialize` logic to the same type (via partial classes), they must coordinate to ensure correct execution order.
> Metalama solves this with its aspect ordering specification, but no equivalent coordination mechanism exists for independent source generators.
> This is an inherent limitation of the source generator model.

### 1.4 Invocation Responsibility

The method is **not** called automatically by the runtime. It is the responsibility of either:

- The caller, after an object initializer, using `InitializableExtensions.WithInitialize(expr)`
- A code generator transforming call sites (such as the Metalama Linker — see §3)

### 1.5 Examples

**Simple class:**
```csharp
public class Range : IInitializable
{
    public required int Min { get; init; }
    public required int Max { get; init; }

    public virtual void Initialize(InitializationContext context)
    {
        if (Max < Min)
            throw new InvalidOperationException("Max must not be less than Min.");
    }
}
```

**Derived class:**
```csharp
public class NamedRange : Range
{
    public required string Name { get; init; }

    public override void Initialize(InitializationContext context)
    {
        base.Initialize(context.Descend());
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Name must not be empty.");
    }
}
```

**Caller — constructor pattern (instrumented call site, after Linker rewriting):**
```csharp
var r = InitializableExtensions.WithInitialize(new Range(1, 12, InitializationContext.WillInitialize));
```

**Caller — object initializer pattern (instrumented call site, after Linker rewriting):**
```csharp
var r = InitializableExtensions.WithInitialize(
    new Range(InitializationContext.WillInitialize) { Min = 1, Max = 12 });
```

> **Note:** `CallInitialize` (constructor self-invocation of `Initialize` as an epilogue) is deferred to a future story.
> Currently, all call sites wrap with `WithInitialize(expr)` and pass `WillInitialize` to the constructor.

---

## 2. `InitializationContext`

### 2.1 Purpose

A single type used both as a constructor parameter and as the `Initialize` parameter.
It carries:

- The caller's intent regarding `Initialize` (`CallerIntent`) — allows constructor code to choose between eager and lazy initialization strategies, and to self-invoke `Initialize` when the caller requests it via `CallInitialize`
- Which aspect behaviors are guaranteed to run in a derived type (`InitializationSlot` bitmask)
- Optional metadata (`Metadata`) — an extensible object carrying the reason for initialization and any additional context. Only meaningful when passed to `Initialize`.

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

> **Expected usage:** Slots are needed only by aspect types that address the telescoping constructor problem — i.e., aspects whose `Initialize` behavior must be skipped when a derived type guarantees it will handle the same concern.
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
    /// A context signaling that the caller will call <c>Initialize</c> after construction
    /// (e.g. after an object initializer). The constructor should not self-invoke.
    /// </summary>
    public static InitializationContext WillInitialize { get; }
        = new(CallerIntent.WillInitialize);

    /// <summary>
    /// A context signaling that the constructor should self-invoke <c>Initialize</c>
    /// at the end of its body. Used at call sites without object initializers, where
    /// the source transformer can guarantee all properties are set by the constructor.
    /// </summary>
    public static InitializationContext CallInitialize { get; }
        = new(CallerIntent.CallInitialize);

    /// <summary>
    /// A context for <c>with</c> expressions or clone operations. Initialize should
    /// revalidate invariants and reinitialize derived state.
    /// </summary>
    public static InitializationContext Modify { get; }
        = new(CallerIntent.None, 0u, InitializationMetadata.Modify);

    /// <summary>
    /// Creates a context with the given metadata. Used when calling Initialize directly
    /// (not via a constructor), e.g. after deserialization or with-expression.
    /// </summary>
    public static InitializationContext Create(InitializationMetadata metadata)
        => new(CallerIntent.None, 0u, metadata);

    /// <summary>The caller's intent regarding Initialize invocation.</summary>
    public CallerIntent Intent => _intent;

    /// <summary>
    /// Whether Initialize will be called (either by the caller or self-invoked
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
    /// (preserving the promise that Initialize will be called) and preserves
    /// <see cref="Metadata"/> from the original context.
    /// Used when a derived Initialize calls base.Initialize to propagate
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
    WillInitialize  = 1,   // caller will call Initialize after construction
    CallInitialize  = 2,   // constructor should self-invoke Initialize at end of body
}
```

| Value | Meaning | Used when |
|---|---|---|
| `None` | No guarantee that `Initialize` will be called | Non-instrumented callers (generated overload) |
| `WillInitialize` | Caller will call `WithInitialize(expr)` after construction | Call sites with object initializers |
| `CallInitialize` | Constructor should self-invoke `Initialize` at the end of its body | Call sites without object initializers |

The source transformer chooses between `WillInitialize` and `CallInitialize` based on whether the call site uses an object initializer.
For `CallInitialize`, the constructor body ends with:

```csharp
if (context.Intent == CallerIntent.CallInitialize)
    Initialize();
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
    /// <c>Initialize</c> should revalidate invariants and reinitialize derived state.
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

- **`WillInitialize`** (instrumented caller): the constructor does **not** self-invoke `Initialize` — the caller will call `WithInitialize(expr)` after the object initializer (if any) completes. This is currently the only intent used by the Linker for all `new` expression call sites.
- **`CallInitialize`** (deferred — future optimization): the constructor self-invokes `Initialize` at the end of its body, since all properties are set by the constructor. This is not yet implemented by the Linker.
- **`None`** (non-instrumented caller): `Initialize` will not be called automatically

> **Note on `WillCallOnInitialized`:** This property returns `true` when `Intent` is either `WillInitialize` or `CallInitialize`.
> It is a **promise** that `Initialize` will be called — either by the instrumented code at the call site, or by the outermost constructor itself.
> The constructor can rely on this promise to defer work to `Initialize` instead of performing it eagerly.
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
            Initialize();
    }

    public Range(int min, int max, InitializationContext context = default)
        : this(context.WillCallOnInitialized
            ? InitializationContext.WillInitialize
            : default) // preserve promise without triggering self-invocation in chained ctor
    {
        Min = min;
        Max = max;
        if (context.Intent == CallerIntent.CallInitialize)
            Initialize();
    }
}
```

> **Note:** When constructors chain via `: this(...)`, the chained constructor must not receive the original context directly (to prevent double self-invocation of `Initialize`).
> Instead, the chaining expression passes `WillInitialize` when the outermost context promises that `Initialize` will be called (`WillCallOnInitialized == true`), and `default` otherwise (non-instrumented callers, where no promise exists).
> Only the outermost constructor — the one the caller invoked — checks `CallInitialize`.
> The code model handles this automatically when introducing the `InitializationContext` parameter.

---

## 3. Metalama Linker Behavior

The Metalama Linker analyzes all object construction call sites and transforms them according to the following rules.

### 3.1 `InitializationContext` Parameter Supply

At any instrumented construction call site where the constructor has an `InitializationContext` parameter, the Linker chooses the appropriate `CallerIntent` based on the call site form.

**If the user code already supplies an `InitializationContext` argument, the Linker does not modify the call** — the user has explicitly chosen the intent and takes responsibility for calling `Initialize` if needed.
The rules below apply only when no argument is provided:

| Call site form | Pass to constructor | Wrap with `WithInitialize(expr)` |
|---|---|---|
| `new T(...)` (no object initializer) | `WillInitialize` | Yes |
| `new T(...) { ... }` (object/collection initializer) | `WillInitialize` | Yes |
| `with { ... }` expression | N/A (copy ctor is unmodified) | Yes, with `InitializationMetadata.Modify` |

For all `new` expressions, the Linker passes `WillInitialize` (if the constructor accepts `InitializationContext`) and wraps with `WithInitialize(expr)` at the call site.
For `with` expressions, the copy constructor is compiler-generated and cannot be modified — the Linker only wraps with `InitializableExtensions.WithInitialize(expr, InitializationMetadata.Modify)` on the cloned instance.

Collection initializers (`new T { item1, item2 }`) are treated identically to object initializers — there is no special handling for them.

> **Note:** `CallInitialize` (constructor self-invocation as an epilogue) is deferred to a future story. Currently, all call sites uniformly wrap with `WithInitialize(expr)`.

The compatibility overload (§4) is the only case where `default` is passed — and that is because the caller is non-instrumented, not because of call site form.

### 3.2 `Initialize` Invocation

At any construction call site where the type implements `IInitializable`, the Linker rewrites the construction expression according to the call site form.

The examples below assume the constructor has an `InitializationContext` parameter (introduced by an aspect via `IntroduceParameter` — see §4). When no such parameter exists, the Linker only wraps with `WithInitialize(expr)` without injecting a constructor argument (see last example).

**Constructor-only (no object initializer, with `InitializationContext` parameter):**
```csharp
// Original
var r = new Range(1, 12);

// Rewritten — wraps with WithInitialize, passes WillInitialize to constructor
var r = InitializableExtensions.WithInitialize(
    new Range(1, 12, context: InitializationContext.WillInitialize));
```

**Object initializer (with `InitializationContext` parameter):**
```csharp
// Original
var r = new Range { Min = 1, Max = 12 };

// Rewritten — wraps with WithInitialize, passes WillInitialize to constructor
var r = InitializableExtensions.WithInitialize(
    new Range(context: InitializationContext.WillInitialize) { Min = 1, Max = 12 });
```

**`with` expression — `Metadata = Modify`:**
```csharp
// Original
var r2 = r1 with { Max = 15 };

// Rewritten
var r2 = InitializableExtensions.WithInitialize(
    r1 with { Max = 15 }, InitializationMetadata.Modify);
```

**Without `InitializationContext` constructor parameter:**
```csharp
// Original
var r = new Range { Min = 1, Max = 12 };

// Rewritten — only WithInitialize wrapper appended
var r = InitializableExtensions.WithInitialize(
    new Range { Min = 1, Max = 12 });
```

### 3.3 Diagnostics

The Linker itself does not report diagnostics for `IInitializable`. Validation of the `Initialize` method contract (virtuality on non-sealed classes) is performed by the `AddInitializer` advice layer — see §5.3.

The Linker silently rewrites call sites for any type implementing `IInitializable`, regardless of whether `Initialize` is virtual. If a user hand-authors a non-virtual `Initialize` without an aspect, that is the user's choice and no diagnostic is emitted.

---

## 4. Constructor Parameter Introduction for `InitializationContext`

When an `AddInitializer(InitializerKind.AfterObjectInitializer)` or `AddInitializer(InitializerKind.AfterLastInstanceConstructor)` advice is applied, the aspect needs a way to pass `InitializationContext` into the constructor chain. This is achieved using `IntroduceParameter` to add the parameter to every target constructor.

### 4.1 Two Strategies

1. **Optional parameter** — call `IntroduceParameter(…, TypedConstant defaultValue, …)` with an initialized `defaultValue`. Simple, but **breaks binary compatibility** because optional parameters are baked into caller IL.

2. **Required parameter + auto-generated forwarding constructor** — call `IntroduceParameter` without a `defaultValue`, combined with an `IConstructorOverloadingStrategy` and a user-supplied `IPullStrategy` that uses `IsAspectGeneratedForwarder()` to branch. The framework appends a **required** parameter to the target, pulls it into every chained constructor, and — for every mutated constructor that the overloading strategy selects — generates a *forwarding constructor*: a compile-time stub that keeps the pre-mutation signature and, via `: this(...)`, chains to the mutated ctor with a strategy-supplied expression for the new parameter. Binary compat is preserved because original signatures still exist on the type.

### 4.2 API Shape

The existing `IntroduceParameter` methods are amended in place:

```csharp
IIntroductionAdviceResult<IParameter> IntroduceParameter(
    IConstructor constructor,
    string parameterName,
    IType parameterType,
    TypedConstant defaultValue = default,                                      // now optional
    IPullStrategy? pullStrategy = null,
    ImmutableArray<AttributeConstruction> attributes = default,
    IConstructorOverloadingStrategy? overloadingStrategy = null );             // new
```

- **`defaultValue = default`** (i.e., `TypedConstant.IsInitialized == false`) means "no C# default value; the parameter is required".
- **`overloadingStrategy`** is a user-supplied (or stock) strategy that the framework calls for every mutated constructor to decide whether to emit a forwarding stub. The stock `ConstructorOverloadingStrategy.ForwardSourceConstructors` matches all source-origin constructors; custom strategies can refine this.
- **`pullStrategy`** still controls what happens at chain-call sites. Aspect authors either use one of the `PullStrategy.*` factory methods or write their own `IPullStrategy` implementation. A custom strategy can call `IsAspectGeneratedForwarder()` on the `targetMember` argument and branch: when `true`, return `UseExpression(forwardingExpression)`; otherwise return `IntroduceParameterAndPull(...)` (or whatever suits the regular cascade). The stock `PullStrategy.IntroduceParameterAndPull(...)` already handles the forwarder case by substituting a `UseExpression` action carrying the configured default value. Both strategies must implement `ICompileTimeSerializable` to work across project boundaries.

The strategy interface is a single method:

```csharp
public interface IConstructorOverloadingStrategy : ICompileTimeSerializable
{
    bool ShouldGenerateForwarder( IConstructor mutatedConstructor, IParameter introducedParameter );
}
```

Aspects **must not enumerate derived types up-front** to decide which constructors to preserve: that would be a hard violation of the "aspects never look at derived types" rule. The strategy is called per mutated constructor, including derived-class constructors reached by the cross-project pull walk, so the decision is always local.

### 4.3 Framework Behavior

For each constructor `C` the advice mutates (the target and every transitively pulled constructor, per the configured `IPullStrategy`):

1. A new parameter is appended to `C`. If `defaultValue.IsInitialized == false`, the parameter has **no** C# default value (required).
2. If `C` chains to another constructor via `:this(...)` / `:base(...)` that also gets pulled, the chain call is updated so the newly introduced parameter is forwarded by name.
3. **Forwarder emission** (runs after mutation if `overloadingStrategy.ShouldGenerateForwarder(C, introducedParam) == true`): the framework ensures **exactly one** forwarding constructor exists for `C`.
   - If no forwarder exists yet, one is created with `C`'s pre-mutation signature (derived by filtering `C.Parameters` to `Origin.Kind == Source` — aspect-introduced parameters are excluded). Its body is `: this(<existing params by name>, <forwardingExpression for new param>) { }`. It is marked with `AspectGeneratedForwardingConstructorAttribute`.
   - If a forwarder already exists (because an earlier aspect already preserved `C`), the framework **extends the existing forwarder in place** by appending the new forwarded argument to its `:this(...)` call. It does **not** create a second forwarder. Invariant: at most one forwarder per preserved constructor, growing monotonically with each successive advice.

The forwarding expression is obtained by invoking the user-supplied `IPullStrategy.GetPullAction(introducedParameter, forwarder)`. For forwarders, only `UseExpression`, `UseConstant`, and `UseExistingParameter` are valid; `DoNotPull` and `IntroduceParameterAndPull` emit `LAMA0536`.

**Diagnostics:**
- `LAMA0520` (existing) — static constructor target.
- `LAMA0530` (existing) — parameter name already exists on the target or a pulled constructor.
- `LAMA0536` (new) — pull strategy returned `DoNotPull` or `IntroduceParameterAndPull` for an aspect-generated forwarding constructor.

### 4.4 Cross-project behavior

Both the pull strategy and the overloading strategy are `ICompileTimeSerializable`, so they are persisted into the transitive aspect metadata and re-hydrated in referencing projects. When project B derives from a base type in project A and A's aspect introduced a parameter via `ForwardSourceConstructors`:

1. B's aspect pipeline runs `PullConstructorParameterTransitiveAspect`, which rebuilds a `PullConstructorParameterAdvice` carrying both strategies.
2. The pull walk visits each derived constructor in B. Roslyn's symbol model resolves `: base(...)` to the **forwarder** emitted in A's IL (because it matches the source arity). The pull-walk predicate therefore "sees through" the forwarder: if the resolved ctor is marked `AspectGeneratedForwardingConstructorAttribute` and its parameters are a type+refkind prefix of the mutated ctor's parameters, it is treated as chaining to the mutated ctor. Safe because Metalama only ever appends parameters (never inserts).
3. The parameter is pulled into B's derived ctors and the `base(...)` call is updated to pass the new argument — naturally resolving to the mutated (non-forwarder) ctor.
4. The overloading strategy then runs on B's own mutated derived ctors and may emit forwarders there too, cascading the binary-compat guarantee across the hierarchy.

### 4.5 Example

**Original:**
```csharp
public class Range
{
    public Range(int min, int max) { Min = min; Max = max; }
    public int Min { get; } public int Max { get; }
}
```

**Aspect:**
```csharp
public sealed class InitContextPullStrategy : IPullStrategy
{
    public PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember )
    {
        if ( targetMember.IsAspectGeneratedForwarder() )
            return PullAction.UseExpression( ExpressionFactory.Parse( "default" ) );

        return PullAction.IntroduceParameterAndPull(
            pulledParameter.Name, pulledParameter.Type, parameterDefaultValue: null );
    }
}

// In BuildAspect:
foreach ( var ctor in builder.Target.Constructors )
{
    builder.With( ctor ).IntroduceParameter(
        "context",
        typeof( InitializationContext ),
        pullStrategy: new InitContextPullStrategy(),
        overloadingStrategy: ConstructorOverloadingStrategy.ForwardSourceConstructors );
}
```

**Result:**
```csharp
public class Range
{
    public Range(int min, int max, InitializationContext context) { Min = min; Max = max; }

    [AspectGeneratedForwardingConstructor]
    public Range(int min, int max) : this(min, max, default) { }

    public int Min { get; } public int Max { get; }
}
```

### 4.6 Non-Instrumented Callers

Non-instrumented callers (external assemblies, reflection, `Activator.CreateInstance`, `new()` generic constraint, DI containers) continue to see and call the original signatures, which resolve to the forwarding constructors. For `InitializationContract`, this means `Initialize` is still not invoked automatically for non-instrumented callers.

> **Warning: Non-instrumented caller limitation:** Non-instrumented callers will **not** have `Initialize` called automatically — neither via the constructor nor after object initializers. This is an inherent limitation of the design: without Linker call-site rewriting, there is no safe point to invoke `Initialize`. The forwarding constructor intentionally does *not* self-invoke `Initialize` to avoid premature firing before `init` properties are set.
>
> **Mitigation strategies to consider:**
> - Emit a Roslyn analyzer diagnostic when a non-instrumented call site constructs a type implementing `IInitializable` without calling `WithInitialize(expr)` afterward
> - Document the limitation prominently in the API documentation for `IInitializable`
> - Consider a runtime assertion (e.g., `Debug.Assert`) that detects first property access after construction without `Initialize` having been called

### 4.7 Positional Records

For positional records, `IntroduceParameter` already handles primary constructor materialization: the parameter is appended to the primary constructor, and a `Deconstruct` method matching the original parameter list is auto-generated to preserve backward compatibility. When combined with an overloading strategy that asks for a forwarder, a secondary forwarding constructor is generated alongside the modified primary.

---

## 5. `AddInitializer` with `InitializerKind.AfterObjectInitializer`

### 5.1 Overview

`AddInitializer(InitializerKind.AfterObjectInitializer)` injects statement templates into the `Initialize` method of `IInitializable`.
It is a pure code model transformation with no direct link to the Linker — the Linker independently detects the type's `IInitializable` implementation.

An aspect may optionally declare one or more `InitializationSlot` fields and pass references to them via `AddInitializer`.
This enables cross-layer coordination — the code model uses the slot fields to emit the correct `Descend(slots)` call, and the template uses `IsHandledBy` to skip behavior already guaranteed by a derived type.

### 5.2 Signature

This extends the existing `AddInitializer` API with a new `InitializerKind.AfterObjectInitializer` value and an optional `slotFields` parameter:

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
This parameter is meaningful when `kind` is `InitializerKind.AfterObjectInitializer` or `AfterLastInstanceConstructor`.

**Template parameter:** Templates used with `InitializerKind.AfterObjectInitializer` or `AfterLastInstanceConstructor` may optionally declare an `InitializationContext` parameter in the template. This controls whether the template body can access slots and metadata — it does not affect the introduced method's signature, which always includes `InitializationContext context = default` (see §5.3).
When the template declares the parameter, the code model maps it from the enclosing method's parameter.

### 5.3 Method Introduction

When the first `AddInitializer(InitializerKind.AfterObjectInitializer)` advice is applied to a type with no existing `Initialize` method, the code model introduces one:

- `public virtual void Initialize(InitializationContext context = default)` on the declaring type
- Body contains injected templates in aspect order
- The advice also introduces the `IInitializable` interface on the target type if not already implemented

When the target type already has a hand-authored `Initialize` method, the advice validates the virtuality contract: if the method is not `public virtual` (or `override`) on a non-sealed class, the advice reports **LAMA0550** (Error). This validation only applies when an aspect uses `AddInitializer` — hand-authored `IInitializable` without aspects is not validated (see §3.3).

Subsequent advice appends templates to the same method body.

**Templates are always `void`-returning statement blocks** — they never emit `return`.

**The `InitializationContext` parameter is always present on the introduced method**, even if no template currently declares it. Templates may optionally declare an `InitializationContext` parameter to access slots and metadata, but this is independent of the method signature. This is mandatory because:
- **User code**: a hand-authored `Initialize` method may want to inspect the context (e.g., check `Metadata`, `IsHandledBy`) regardless of what aspects are applied
- **Multi-aspect**: multiple aspects may contribute templates to the same `Initialize` method, and a later aspect cannot retroactively add a parameter to an already-introduced method
- **Cross-project inheritance**: the base class method may already be compiled without the parameter, making it impossible for a derived project to fix the signature

The same rule applies to the `OnConstructed` method introduced by `AfterLastInstanceConstructor`.

### 5.4 Inheritance

When the code model introduces `Initialize` on a derived type whose base already implements `IInitializable`:

- `base.Initialize(context.Descend(slot1 | slot2 | ...))` is emitted as the **first statement**, passing the union of all slots declared by aspects applied to the derived type
- The override is `void` — straightforward override of the base method
- If the derived type already has a hand-authored `Initialize`, templates are appended without modifying the existing `base.Initialize(...)` call

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
        var slotField = builder.Target.Compilation.Factory.GetTypeByReflectionType(typeof(ChangeTrackingAspect))
            .Fields.OfName(nameof(Slot)).Single();

        builder.AddInitializer(nameof(Template), InitializerKind.AfterObjectInitializer,
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
        var slotField = builder.Target.Compilation.Factory.GetTypeByReflectionType(typeof(ValidationAspect))
            .Fields.OfName(nameof(Slot)).Single();

        builder.AddInitializer(nameof(Template), InitializerKind.AfterObjectInitializer,
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
// Base — introduced by code model, implements IInitializable
public virtual void Initialize(InitializationContext context)
{
    if (!context.IsHandledBy(ChangeTrackingAspect.Slot))  // ChangeTrackingAspect
        _changeTracker = new ChangeTracker(this);

    if (!context.IsHandledBy(ValidationAspect.Slot))      // ValidationAspect
        Validator.Validate(this);
}

// Derived — override introduced by code model
// ValidationAspect.Slot passed to Descend() — base will skip validation
// ChangeTrackingAspect not applied to NamedRange — base will run it
public override void Initialize(InitializationContext context)
{
    base.Initialize(context.Descend(ValidationAspect.Slot));

    if (!context.IsHandledBy(ValidationAspect.Slot))      // ValidationAspect
        Validator.Validate(this);
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

This is complementary to `BeforeInstanceConstructor` (which injects at the **beginning**, after `:base()`) and `InitializerKind.AfterObjectInitializer` (which introduces a separate `Initialize` method called after object initializers complete).

### 6.2 Semantics

| Aspect | `BeforeInstanceConstructor` | `AfterLastInstanceConstructor` | `InitializerKind.AfterObjectInitializer` |
|---|---|---|---|
| **Where** | Beginning of constructor body (after `:base()`) | Separate `OnConstructed` method, called at end of constructor | Separate `Initialize` method, after object initializers |
| **Which constructors** | All except `:this()` chains | All except `:this()` chains (via `OnConstructed` call) | N/A (called after object initializers) |
| **When** | Before user constructor code | After user constructor code | After object initializers and `init` properties |
| **Object initializer awareness** | No | No | Yes |
| **Introduced method** | None | `OnConstructed` | `Initialize` |

### 6.3 `OnConstructed` Method

The introduced method follows the same pattern as `Initialize`:

```csharp
public virtual void OnConstructed(InitializationContext context = default)
{
    // base.OnConstructed(context.Descend(...)) — if base has OnConstructed
    // template statements injected here
}
```

- `void` return type
- `virtual` on non-sealed classes, non-virtual on sealed classes and structs
- `override` when a base type already has `OnConstructed`
- **No `[OnConstructed]` attribute** — unlike `IInitializable`, this is a system concept, not a user concept. The method name is hardcoded by the advice. Users do not hand-author `OnConstructed` methods.
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

### 6.7 Interaction with `Initialize`

When both `AfterLastInstanceConstructor` and `InitializerKind.AfterObjectInitializer` are used on the same type:

- `OnConstructed` runs at the end of the constructor body, **before** `Initialize`
- `Initialize` runs after object initializers (if any) complete

Execution order for the full lifecycle:
1. Constructor body (including `BeforeInstanceConstructor` + user code)
2. `OnConstructed(context)` — at end of constructor
3. Object initializer (`init` properties) — if present
4. `WithInitialize(expr)` — after object initializers

---

## 7. Record Support

### 7.1 Scope

`class` and `struct` records are fully supported, including positional records.
Note that structs can be default-constructed (`default(T)`), bypassing all constructors.
This is an inherent limitation of value types in .NET and is not specific to this feature.

### 7.2 Positional Records — Copy Constructor

The copy constructor for positional records is compiler-generated and is **not modified** by the Linker.
It remains a pure field copy.
Post-copy initialization is handled entirely by wrapping with `InitializableExtensions.WithInitialize(expr, InitializationMetadata.Modify)` at `with` expression call sites (see §7.4).

Implementations of `Initialize` must support being called with `Metadata = Modify` on an instance that was created via the copy constructor.
This means `Initialize` may be called multiple times over the lifetime of an object graph (once at initial construction, and once after each `with` expression).

### 7.3 Non-Positional Records

No special treatment — the author controls the copy constructor and the standard pattern from §3 applies as-is (call-site rewriting by the Linker works identically).

### 7.4 `with` Expression Call Sites

The Linker rewrites `with` expressions to call `Initialize` via `WithInitialize` with `InitializationMetadata.Modify` on the cloned instance.
The copy constructor itself is not modified — it performs its standard field copy, and `Initialize` runs afterward to revalidate or recompute derived state:

```csharp
// Original
var r2 = r1 with { Max = 15 };

// Rewritten
var r2 = InitializableExtensions.WithInitialize(
    r1 with { Max = 15 }, InitializationMetadata.Modify);
```

`Initialize` implementations must handle `Metadata = Modify` correctly — for example, revalidating invariants against the new property values, or reinitializing derived state such as caches or change trackers.
Since `with` can be called repeatedly, `Initialize` must be safe to call multiple times.

---

## 8. Serialization and Cloning

### 8.1 `ISerializationFramework`

Serialization and cloning are treated as the same concern — in both cases the object is fully populated by an external mechanism and `Initialize` must be called afterward.
The `InitializationMetadata` passed to `Initialize` carries the distinction if needed.

```csharp
public interface ISerializationFramework
{
    void ImplementPostInitialization( INamedType type, IMethod initializeMethod );
}
```

The implementor introduces or injects into whatever hook the framework provides (`[OnDeserialized]`, `[ProtoAfterDeserialization]`, `MemberwiseClone` override, etc.) and calls `Initialize` with the appropriate context:

```csharp
// Post-deserialization (System.Text.Json)
instance.Initialize(InitializationContext.Create(SystemTextJsonInitializationMetadata.Default));

// Post-clone
instance.Initialize(InitializationContext.Create(InitializationMetadata.Modify));
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

## 9. Case Study: Automatic Parent-Child Wiring

### 9.1 Problem

A common pattern in domain models is a parent-child relationship where each child holds a back-reference to its parent.
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

`IInitializable` solves this by running `Initialize` after all `init` properties are assigned.

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
            InitializerKind.AfterObjectInitializer,
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

    // Introduced or authored — receives back-reference from parent's Initialize
    public TreeNode? Parent { get; set; }
}
```

### 9.5 After Transformation

```csharp
public class TreeNode : IChildObject<TreeNode>, IInitializable
{
    public required string Name { get; init; }

    [Child]
    public required IReadOnlyList<TreeNode> Children { get; init; }

    public TreeNode? Parent { get; set; }

    public virtual void Initialize(InitializationContext context)
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
    }
}
```

### 9.6 Call Sites

**instrumented (after Linker rewriting):**
```csharp
var tree = InitializableExtensions.WithInitialize(
    new TreeNode(InitializationContext.WillInitialize)
    {
        Name = "root",
        Children = new[]
        {
            InitializableExtensions.WithInitialize(
                new TreeNode(InitializationContext.WillInitialize)
                {
                    Name = "left",
                    Children = Array.Empty<TreeNode>()
                }),
            InitializableExtensions.WithInitialize(
                new TreeNode(InitializationContext.WillInitialize)
                {
                    Name = "right",
                    Children = Array.Empty<TreeNode>()
                })
        }
    });

// tree.Children[0].Parent == tree  ✓
// tree.Children[1].Parent == tree  ✓
```

### 9.7 `with` Expression — Re-wiring on Modify

When a `with` expression clones a node, the clone's children still reference the old parent.
The Linker wraps with `WithInitialize(expr, InitializationMetadata.Modify)` which re-wires them:

```csharp
// Original
var renamed = tree with { Name = "renamed-root" };

// Rewritten by Linker
var renamed = InitializableExtensions.WithInitialize(
    tree with { Name = "renamed-root" }, InitializationMetadata.Modify);

// renamed.Children[0].Parent == renamed  ✓  (re-wired by Initialize)
```

> **Design note:** After re-wiring, the original `tree.Children[0].Parent` now points to `renamed`, not `tree` — the children are shared references.
> If the original parent must remain valid, the `Initialize(Modify)` implementation should deep-clone the children instead of re-wiring.
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
`IInitializable` provides the right hook — it runs `Initialize` after all properties are set.

In a class hierarchy, freezing must happen **once** at the most-derived level.
If a base class freezes in its `Initialize`, it blocks the derived class from completing its own setup.
`InitializationSlot` coordination solves this: each layer defers freezing if a derived layer guarantees it will freeze.

### 10.2 Aspect

```csharp
[Inheritable]
class FreezableAspect : TypeAspect
{
    public static readonly InitializationSlot Slot = InitializationSlot.Allocate();

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var slotField = builder.Target.Compilation.Factory.GetTypeByReflectionType(typeof(FreezableAspect))
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

        // Add the freeze logic to Initialize
        builder.AddInitializer(
            nameof(FreezeTemplate),
            InitializerKind.AfterObjectInitializer,
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
public class Shape : IInitializable
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

    public virtual void Initialize(InitializationContext context)
    {
        if (!context.IsHandledBy(FreezableAspect.Slot))
            Freeze();
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

    public override void Initialize(InitializationContext context)
    {
        // Derived passes FreezableAspect.Slot → base skips freezing
        base.Initialize(context.Descend(FreezableAspect.Slot));

        if (!context.IsHandledBy(FreezableAspect.Slot))
            Freeze();
    }
}
```

### 10.5 Why the Slot Matters

Without `InitializationSlot` coordination, the execution would be:

1. `LabeledShape.Initialize(context)` calls `base.Initialize(context.Descend())`
2. `Shape.Initialize` calls `Freeze()` — dispatches to `LabeledShape.Freeze()` (virtual) which wraps `Tags`, then chains to `base.Freeze()` which wraps `Vertices` and sets `IsFrozen = true`
3. Back in `LabeledShape.Initialize` — calls `Freeze()` again, which tries to wrap `Tags` via the setter — throws because `IsFrozen` is already `true`

With the slot:

1. `LabeledShape.Initialize(context)` calls `base.Initialize(context.Descend(FreezableAspect.Slot))`
2. `Shape.Initialize` sees `IsHandledBy(FreezableAspect.Slot) == true` — skips `Freeze()`
3. Back in `LabeledShape.Initialize` — calls `Freeze()` which wraps `Tags`, chains to `base.Freeze()` which wraps `Vertices` and sets `IsFrozen = true` — all setters succeed because the object is still unfrozen during the cascade

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
var shape = InitializableExtensions.WithInitialize(
    new LabeledShape(InitializationContext.WillInitialize)
    {
        Color = "Red",
        Vertices = new List<Point> { new(0, 0), new(1, 0), new(1, 1) },
        Label = "Triangle",
        Tags = new List<string> { "geometry", "demo" }
    });

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
        // Non-instrumented caller — Initialize will NOT be called.
        // Option A: log a warning that the object will remain mutable
        // Option B: throw to prevent non-instrumented construction
        throw new InvalidOperationException(
            "Shape must be constructed through an instrumented call site " +
            "to guarantee freeze-after-init semantics. " +
            "Alternatively, call Initialize() manually after setting all properties.");
    }
}
```

This makes the contract explicit: if the caller cannot guarantee that `Initialize` will run, the type refuses construction rather than silently producing an unfrozen instance.
Non-instrumented callers that understand the pattern can still construct the object by calling `Initialize()` manually after setting all properties.

---

## 11. Case Study: Computed Derived Properties

> **Note:** This pattern is often better addressed with `[Memo]` (lazy memoization).
> This case study demonstrates how `IInitializable` can solve it when eager computation is preferred.

### 11.1 Problem

A scientific type has `required init` properties and read-only derived values that must be computed from them.
Without a post-initialization hook, the author must either:

- Compute on every access via `=>` (performance cost on repeated reads)
- Use `private set` instead of true read-only (sacrifices immutability guarantees)
- Duplicate computation in every `init` accessor (fragile, error-prone)

### 11.2 User Code

```csharp
public class Isotope : IInitializable
{
    public required double HalfLifeSeconds { get; init; }
    public required int AtomicNumber { get; init; }
    public required int MassNumber { get; init; }

    // Derived — computed once after all properties are set
    public double DecayConstant { get; private set; }
    public double MeanLifetime { get; private set; }

    public virtual void Initialize(InitializationContext context = default)
    {
        DecayConstant = Math.Log(2) / HalfLifeSeconds;
        MeanLifetime = 1.0 / DecayConstant;
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

    public override void Initialize(InitializationContext context = default)
    {
        base.Initialize(context.Descend());
        Activity = DecayConstant * InitialAtoms;
    }
}
```

### 11.3 Call Sites

```csharp
// Object initializer — Linker wraps with WithInitialize(expr)
var sample = new RadioactiveSample
{
    HalfLifeSeconds = 1.808e11,
    AtomicNumber = 6,
    MassNumber = 14,
    InitialAtoms = 1e24
};
// After rewriting:
// var sample = InitializableExtensions.WithInitialize(
//     new RadioactiveSample(InitializationContext.WillInitialize)
//     { HalfLifeSeconds = 1.808e11, AtomicNumber = 6, MassNumber = 14, InitialAtoms = 1e24 });

// All derived values are already computed
Console.WriteLine(sample.DecayConstant);  // from base
Console.WriteLine(sample.Activity);       // from derived (= DecayConstant * InitialAtoms)
```

### 11.4 `with` Expression

```csharp
// Changing a source property recomputes all derived values through the chain
var adjusted = sample with { InitialAtoms = 5e23 };
// After rewriting:
// var adjusted = InitializableExtensions.WithInitialize(
//     sample with { InitialAtoms = 5e23 }, InitializationMetadata.Modify);

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
        var slotField = builder.Target.Compilation.Factory.GetTypeByReflectionType(typeof(TrackableAspect)).Fields.OfName(nameof(Slot)).Single();
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
            InitializerKind.AfterObjectInitializer,
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
| 8 | `WithInitialize(expr)` called by Linker | `Initialized` |
