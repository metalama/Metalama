# InitializationContract — Implementation Stories

Derived from [InitializationContract-spec.md](InitializationContract-spec.md).

## Story 1 — `AddInitializer(OnInitialized)` (§1, §2, §5)

The foundational story. Introduces the public API types and the advice that wires them together.

**Scope:**

- `[OnInitialized]` attribute (declaration, method contract, inheritance rules)
- `InitializationContext` struct with `CallerIntent`, `InitializationSlot`, and `InitializationMetadata`
- `CallerIntent` enum (`None`, `WillInitialize`, `CallInitialize`)
- `InitializationMetadata` base class with `Default` and `Modify` singletons
- `InitializerKind.OnInitialized` enum value (replacing obsolete `AfterObjectInitialization`, `AfterDeserialize`, `AfterMemberwiseClone`, `AfterWith`)
- `AddInitializer(InitializerKind.OnInitialized)` advice with optional `slotFields` parameter
- `OnInitialized` method introduction: if the target type has no `[OnInitialized]` method, introduce one; if it exists, inject template statements
- Slot field wiring: emit `Descend(slots)` calls and `IsHandledBy` guards
- Template parameter convention: `OnInitialized` templates may optionally declare an `InitializationContext` parameter
- Inheritance: when both base and derived types have `[OnInitialized]`, the derived type's method wins; it calls `base.OnInitialized()` to chain

**Dependencies:** None.

---

## Story 2 — Constructor parameter introduction (§4)

Enables constructors to receive `InitializationContext` so they can react to caller intent (e.g., self-invoke `OnInitialized` on `CallInitialize`).

**Scope:**

- New **overload generation mode** for `IntroduceParameter`: generates a constructor overload without the `InitializationContext` parameter that forwards `default` internally, instead of adding an optional parameter with a default value
- Template parameter convention: when an `AddInitializer(InitializerKind.BeforeInstanceConstructor)` template declares an `InitializationContext` parameter, the advice automatically introduces the parameter on the target constructor
- Propagation to child constructors via existing `PullAction.IntroduceParameterAndPull`

**Dependencies:** Story 1.

---

## Story 3 — Linker call-site rewriting (§3)

The Linker transforms construction call sites to supply `InitializationContext` and append `.OnInitialized()` calls.

**Scope:**

- `InitializationContext` parameter supply based on call-site form:
  - `new T(...)` (no object initializer) → pass `CallInitialize` (constructor self-invokes)
  - `new T(...) { ... }` (object initializer) → pass `WillInitialize`, append `.OnInitialized()`
  - `with { ... }` → append `.OnInitialized()` with `InitializationMetadata.Modify`
- Skip rewriting when user code already supplies an `InitializationContext` argument
- Emit a cast when `OnInitialized` return type is not the constructed type
- Contract validation diagnostics (§3.3): validate `[OnInitialized]` method contract from §1.2 and report errors

**Dependencies:** Story 2.

---

## Story 4 — Record support (§6)

Extends Linker rewriting to handle record-specific patterns.

**Scope:**

- Positional records: `IntroduceParameter` handles primary constructor materialization and `Deconstruct` generation
- Copy constructor: compiler-generated, not modified — post-copy initialization handled by `.OnInitialized()` at `with` call sites
- Non-positional records: standard pattern from §3 applies as-is
- `with` expression call sites: append `.OnInitialized(InitializationContext.Create(InitializationMetadata.Modify))`

**Dependencies:** Story 3.

---

## Story 5 — Serialization integration (§7)

Enables serialization frameworks to participate in the `OnInitialized` protocol via custom `InitializationMetadata` subclasses.

**Scope:**

- `ISerializationFramework` interface for framework-specific integration
- `SystemTextJsonInitializationMetadata` subclass (and similar for other frameworks)
- Serialization frameworks call `OnInitialized` after deserialization with their specific metadata

**Dependencies:** Story 1.
