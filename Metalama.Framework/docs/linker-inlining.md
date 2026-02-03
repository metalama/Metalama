# Linker Inlining System

This document describes the inlining mechanism in the Metalama linker, which optimizes aspect-generated code by embedding method bodies directly into their callers, eliminating unnecessary method call overhead.

## Overview

When Metalama weaves aspects into code, it creates "aspect references" - calls from override methods to either the original method or the next override in the chain. The linker's inlining system analyzes these references and, when possible, replaces them with the actual method body, producing cleaner and more efficient code.

## Aspect References: Production vs Test Representation

Aspect references are the core mechanism that tells the linker how to connect override methods to their targets. The **same underlying mechanism** is used in both production and tests, but the **syntax representation differs**.

### Production Code (Aspect Templates)

In production, aspect templates use `meta.Proceed()` to invoke the next layer in the override chain:

```csharp
// Aspect template
public class LoggingAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine("Before");
        var result = meta.Proceed();  // Calls the next override or original method
        Console.WriteLine("After");
        return result;
    }
}
```

When the template is expanded, the `meta.Proceed()` call becomes a member access expression (like `this.Method()`) **annotated with an `AspectReferenceSpecification`** stored as a Roslyn `SyntaxAnnotation`:

```csharp
// Expanded template (intermediate code) - conceptually:
private int Method_Override()
{
    Console.WriteLine("Before");
    var result = this.Method();  // ← This node has AspectReferenceAnnotation attached
    Console.WriteLine("After");
    return result;
}
```

The annotation is attached using `WithAspectReferenceAnnotation()` and contains:
- `AspectLayerId` - Which aspect created this reference
- `AspectReferenceOrder` - Whether to call `Previous`, `Base`, `Current`, or `Final` semantic
- `AspectReferenceTargetKind` - `Self`, `PropertyGetAccessor`, `PropertySetAccessor`, etc.
- `AspectReferenceFlags` - Including `Inlineable` flag

The annotation is serialized to a string format: `"AspectName:LayerName$Order$TargetKind$Flags"`

> **Source:** See `AspectReferenceSpecification.ToString()` in `Metalama.Framework.Engine/Aspects/AspectReferenceSpecification.cs` and `WithAspectReferenceAnnotation()` in `Metalama.Framework.Engine/Aspects/AspectReferenceAnnotationExtensions.cs`

### Linker Tests (DSL Syntax)

Linker tests use a **test-specific DSL** that bypasses the template expansion system and directly specifies the intermediate representation that the linker processes. This allows precise control over the linker's input for testing.

#### Pseudo Attributes

Pseudo attributes simulate what aspects do in production. They are processed by `TestTypeRewriter` and create the same transformations that real aspects would.

> **Source:** Attribute definitions in `tests/Metalama.Framework.Tests.LinkerTests/Tests/_Helpers/Pseudo*.cs`, processing logic in `tests/Metalama.Framework.Tests.LinkerTests/Runner/LinkerTestInputBuilder.TestTypeRewriter.cs`

| Attribute | Purpose | Production Equivalent |
|-----------|---------|----------------------|
| `[PseudoOverride(target, aspectName)]` | Creates an override of `target` by `aspectName` | `builder.Override(target, template)` |
| `[PseudoIntroduction(aspectName)]` | Introduces a new member by `aspectName` | `builder.Introduce(...)` |
| `[PseudoNotInlineable]` | Marks a member as not inlineable | Internal linker flag |
| `[PseudoNotDiscardable]` | Marks a member as not discardable | Internal linker flag |
| `[PseudoLayerOrder(aspectName)]` | Controls aspect ordering | `[AspectOrder]` attribute |

**`[PseudoOverride]` in detail:**
- First argument: `nameof(TargetMember)` - the member being overridden
- Second argument: Aspect name (e.g., `"TestAspect"`)
- Optional third argument: Layer name for multi-layer aspects
- The decorated method becomes the override body (like a template's `OverrideMethod()`)
- The method is removed from the output; its body replaces the original

#### Link DSL

Inside pseudo-override bodies, the `Link(...)` function creates aspect references:

```csharp
// Linker test syntax (Tests/_Helpers/Api.cs)
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

[PseudoOverride(nameof(Foo), "TestAspect")]
private int Foo_Override()
{
    Console.WriteLine("Before");
    int x;
    x = Link(This.Foo, Inline)();  // Test DSL for aspect reference
    Console.WriteLine("After");
    return x;
}
```

The `TestMethodBodyRewriter` transforms this DSL into the same `AspectReferenceAnnotation` used in production.

> **Source:** DSL definitions in `tests/Metalama.Framework.Tests.LinkerTests/Tests/_Helpers/Api.cs`, transformation logic in `tests/Metalama.Framework.Tests.LinkerTests/Runner/LinkerTestInputBuilder.TestMethodBodyRewriter.cs:61-167`

| DSL Element | Meaning |
|------------|---------|
| `Link(...)` | Creates an aspect reference |
| `This.Foo` | Reference to `this.Foo` member (transforms to `this.Foo`) |
| `Static.Foo` | Reference to static member (transforms to just `Foo`) |
| `Local.Foo` | Reference to local variable/parameter (transforms to just `Foo`) |
| `Cast<T>(x)` | Cast expression (transforms to `(T)x`) |
| `Inline` | Sets `AspectReferenceFlags.Inlineable` |
| `Base` | Sets `AspectReferenceOrder.Base` |
| `Previous` | Sets `AspectReferenceOrder.Previous` (default) |
| `Current` | Sets `AspectReferenceOrder.Current` |
| `Final` | Sets `AspectReferenceOrder.Final` |
| `.get` / `.set` | Sets property accessor target kind |
| `.add` / `.remove` / `.raise` | Sets event accessor target kind |

**Important:** The DSL is **only for testing**. Production code never sees `Link()` syntax - it uses `meta.Proceed()` which the template expansion system converts to annotated member access expressions.

#### Complete Test Example

Here's how a linker test with multiple overrides works.

> **Source:** `tests/Metalama.Framework.Tests.LinkerTests/Tests/Methods/Overrides/Jump/ReturnsVoid_FJ_NJ.cs` (input) and `.t.cs` (expected output)

**Input (ReturnsVoid_FJ_NJ.cs):**
```csharp
internal class Target
{
    private void Foo(int x)
    {
        Console.WriteLine("Original");
    }

    [PseudoOverride(nameof(Foo), "TestAspect1")]  // First override
    private void Foo_Override1(int x)
    {
        Console.WriteLine("Before1");
        if (x == 0) { return; }  // Early return - causes "Forced Jump" (FJ)
        Link(This.Foo, Inline)(x);
        Console.WriteLine("After1");
    }

    [PseudoOverride(nameof(Foo), "TestAspect2")]  // Second override
    private void Foo_Override2(int x)
    {
        Console.WriteLine("Before2");
        Link(This.Foo, Inline)(x);  // No early return - "No Jump" (NJ)
        Console.WriteLine("After2");
    }
}
```

**Output (ReturnsVoid_FJ_NJ.t.cs):**
```csharp
internal class Target
{
    private void Foo(int x)
    {
        // TestAspect2's body (outermost)
        Console.WriteLine("Before2");
        // TestAspect1's body inlined (with goto for early return)
        Console.WriteLine("Before1");
        if (x == 0)
        {
            goto __aspect_return_1;  // Early return becomes goto
        }
        // Original body inlined
        Console.WriteLine("Original");
        Console.WriteLine("After1");
    __aspect_return_1:
        Console.WriteLine("After2");
    }
}
```

The test file naming convention `_FJ_NJ` indicates the control flow patterns:
- `FJ` = Forced Jump (has early return, needs goto label)
- `NJ` = No Jump (no early return, simple inlining)

### The Common Ground: ResolvedAspectReference

Both representations ultimately create the same data structure for the linker to process:

```csharp
// ResolvedAspectReference.cs - Used by the linker regardless of origin
internal sealed class ResolvedAspectReference
{
    public IntermediateSymbolSemantic<IMethodSymbol> ContainingSemantic { get; }
    public ISymbol OriginalSymbol { get; }
    public IntermediateSymbolSemantic ResolvedSemantic { get; }
    public SyntaxNode RootNode { get; }              // The syntax node to replace
    public AspectReferenceTargetKind TargetKind { get; }
    public bool IsInlineable { get; }               // From AspectReferenceFlags.Inlineable
    // ...
}
```

The `AspectReferenceResolver` walks the syntax tree, finds nodes with `AspectReferenceAnnotation`, and creates `ResolvedAspectReference` objects that the inlining system then processes.

> **Source:** `Metalama.Framework.Engine/Linking/ResolvedAspectReference.cs` and `Metalama.Framework.Engine/Linking/AspectReferenceResolver.cs`

### Example Transformation

**Production aspect:**
```csharp
public override dynamic? OverrideMethod()
{
    Console.WriteLine("Before");
    return meta.Proceed();
}
```

**Equivalent linker test:**
```csharp
[PseudoOverride(nameof(Foo), "TestAspect")]
private int Foo_Override()
{
    Console.WriteLine("Before");
    return Link(This.Foo, Inline)();
}
```

**Both become (after annotation):**
```csharp
private int Foo_Override()
{
    Console.WriteLine("Before");
    return this.Foo();  // With AspectReferenceAnnotation: "TestAspect$Previous$Self$Inlineable"
}
```

**After inlining (final code):**
```csharp
private int Foo()
{
    Console.WriteLine("Before");
    Console.WriteLine("Original");  // Inlined from original Foo
    return 42;                      // Inlined from original Foo
}
```

## Architecture

### Key Components

All components are in `Metalama.Framework.Engine/Linking/`:

```
LinkerAnalysisStep (partial class, split across files)
├── LinkerAnalysisStep.InlineabilityAnalyzer.cs  # Determines what CAN be inlined
├── LinkerAnalysisStep.InliningAlgorithm.cs      # Decides what WILL be inlined
├── LinkerAnalysisStep.SemanticBodyAnalyzer.cs   # Analyzes control flow
└── LinkerAnalysisStep.SubstitutionGenerator.cs  # Creates syntax transformations

Inlining/ (subfolder)
├── Inliner.cs               # Abstract base class for all inliners
├── InlinerProvider.cs       # Selects appropriate inliner for a reference
├── InliningSpecification.cs # Full specification for one inlining operation
├── InliningAnalysisContext.cs # Tracks state during analysis (IDs, labels)
├── MethodInliner.cs         # Base for method inliners
├── MethodAssignmentInliner.cs, MethodReturnStatementInliner.cs, ...
├── PropertyInliner.cs, PropertyGetReturnInliner.cs, ...
├── EventInliner.cs, EventAddAssignmentInliner.cs, ...
└── ConstructorInliner.cs
```

## Control Flow: Decision Process

The inlining decision happens in four stages:

### Stage 1: Identify Inlineable Semantics

`InlineabilityAnalyzer.GetInlineableSemanticsAsync()` identifies which method/property/event semantics *can potentially* be inlined.

> **Source:** `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.InlineabilityAnalyzer.cs`, method `GetInlineableSemanticsAsync()`

**A semantic is inlineable if:**
- Not marked with `NotInlineable` flag
- Not a redirection target
- Not a `Final` semantic (final methods are entry points, never inlined)
- Not a `Base` semantic pointing to an actual base member
- For methods: Referenced exactly once
- For properties: At most one getter OR one setter reference (not both)
- For events: At most one add OR one remove reference, not an event field

**Why "exactly once"?** The single-reference constraint ensures a **one-to-zero-or-one mapping** between source code and generated code. This is critical for **debugging**: if a method body were inlined into multiple locations, a breakpoint in the source would hit at multiple places in the generated code, breaking the debugging experience. By requiring exactly one reference, each source location maps to at most one generated location.

```csharp
// LinkerAnalysisStep.InlineabilityAnalyzer.cs:92-145
bool IsInlineable(IntermediateSymbolSemantic semantic)
{
    if (semantic.Symbol.GetDeclarationFlags().HasFlagFast(AspectLinkerDeclarationFlags.NotInlineable))
        return false;  // Explicitly marked non-inlineable

    if (redirectionTargets.Contains(semantic))
        return false;  // Redirection targets cannot be inlined

    if (semantic.Kind is IntermediateSymbolSemanticKind.Final)
        return false;  // Final semantics are entry points

    if (semantic.Kind is IntermediateSymbolSemanticKind.Base
        && (semantic.Symbol.IsOverride || semantic.Symbol.TryGetHiddenSymbol(...)))
        return false;  // Base semantics pointing to actual base members

    // ... symbol-specific checks (methods, properties, events)
}
```

### Stage 2: Identify Inlineable References

`InlineabilityAnalyzer.GetInlineableReferencesAsync()` examines each aspect reference to an inlineable semantic.

> **Source:** `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.InlineabilityAnalyzer.cs`, method `GetInlineableReferencesAsync()`

**A reference is inlineable if:**
- Containing semantic is not marked `NotInliningDestination`
- Reference has `AspectReferenceFlags.Inlineable` flag set (see below)
- Both symbols are in the same type (no cross-type inlining)
- The syntax is an expression (or uses `ImplicitLastOverrideReferenceInliner`)
- An appropriate `Inliner` can be found via `InlinerProvider`

**How the `Inlineable` flag is set:**

The `AspectReferenceFlags.Inlineable` flag is set during template expansion when creating the aspect reference annotation. Different code paths set it differently:

| Code Path | Sets `Inlineable`? | Description |
|-----------|-------------------|-------------|
| `meta.Proceed()` | **Yes** | Standard proceed calls are always inlineable |
| `LinkerAspectReferenceSyntaxProvider` | **Yes** | Linker-generated references (constructors, operators, etc.) |
| `Invoker.cs` (direct member calls) | **No** | `meta.Target.Method()` uses `AspectReferenceFlags.CustomReceiver` (no `Inlineable` flag) |

> **Source:** `Inlineable` flag set in `Metalama.Framework.Engine/Transformations/ProceedHelper.cs:272` and `Metalama.Framework.Engine/Linking/LinkerAspectReferenceSyntaxProvider.cs` (multiple locations). Non-inlineable references created in `Metalama.Framework.Engine/CodeModel/Invokers/Invoker.cs:112` (uses `None` when target is null, `CustomReceiver` when target is specified - neither includes `Inlineable`).

```csharp
// LinkerAnalysisStep.InlineabilityAnalyzer.cs:282-321
bool IsInlineable(ResolvedAspectReference reference, out Inliner? inliner)
{
    // Containing semantic must allow inlining
    if (reference.ContainingSemantic.Symbol.GetDeclarationFlags()
        .HasFlagFast(AspectLinkerDeclarationFlags.NotInliningDestination))
    {
        inliner = null;
        return false;
    }

    // Reference must have Inlineable flag set
    if (!reference.IsInlineable)
    {
        inliner = null;
        return false;
    }

    // Same-type constraint
    if (!SymbolEqualityComparer.Default.Equals(
            reference.ContainingSemantic.Symbol.ContainingType,
            reference.ResolvedSemantic.Symbol.ContainingType))
    {
        inliner = null;
        return false;
    }

    // Non-expression syntax uses special inliner
    if (reference.SymbolSourceNode is not ExpressionSyntax)
    {
        inliner = ImplicitLastOverrideReferenceInliner.Instance;
        return true;
    }

    // Find matching inliner for expression syntax
    return this._inlinerProvider.TryGetInliner(reference, semanticModel, out inliner);
}
```

### Stage 3: Determine Actually Inlined Semantics

`InlineabilityAnalyzer.GetInlinedSemanticsAsync()` finalizes which semantics will be inlined.

> **Source:** `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.InlineabilityAnalyzer.cs`, method `GetInlinedSemanticsAsync()`

**A semantic is actually inlined if:**
- It is inlineable AND
- ALL references to it are inlineable (no non-inlineable references exist)

This ensures a method is only inlined if every call site supports inlining.

### Stage 4: Generate Inlining Specifications

`InliningAlgorithm.RunAsync()` traverses non-inlined semantics and creates `InliningSpecification` objects for each inlineable reference.

> **Source:** `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.InliningAlgorithm.cs`, method `RunAsync()`

Key decisions made here:
- **Simple vs Complex inlining** (see below)
- **Return variable allocation** (for complex inlining)
- **Return label allocation** (for early returns)

## Data Flow: Types and Transformations

### Core Data Types

#### InliningSpecification
The complete specification for one inlining operation.

> **Source:** `Metalama.Framework.Engine/Linking/Inlining/InliningSpecification.cs`

```csharp
// InliningSpecification.cs
internal sealed class InliningSpecification
{
    // WHERE the body is inlined into
    public IntermediateSymbolSemantic<IMethodSymbol> DestinationSemantic { get; }

    // Unique identifier for this inlining within destination
    public InliningContextIdentifier ContextIdentifier { get; }

    // The aspect reference being inlined
    public ResolvedAspectReference AspectReference { get; }

    // Which inliner performs the transformation
    public Inliner Inliner { get; }

    // The syntax node to replace
    public SyntaxNode ReplacedNode { get; }

    // Inlining strategy
    public bool UseSimpleInlining { get; }

    // For complex inlining: return variable handling
    public bool DeclareReturnVariable { get; }
    public string? ReturnVariableIdentifier { get; }

    // For early returns: label to jump to
    public string? ReturnLabelIdentifier { get; }

    // Source method to inline
    public IntermediateSymbolSemantic<IMethodSymbol> TargetSemantic { get; }
}
```

#### InliningAnalysisContext
Tracks state during recursive inlining analysis.

> **Source:** `Metalama.Framework.Engine/Linking/Inlining/InliningAnalysisContext.cs`

```csharp
// InliningAnalysisContext.cs
internal sealed class InliningAnalysisContext
{
    public bool UsingSimpleInlining { get; }        // Current inlining mode
    public string? ReturnVariableIdentifier { get; } // Variable for return values
    public InliningId Id { get; }                    // Unique ID for this inlining
    public InliningId? ParentId { get; }             // Parent's ID (for nested inlining)

    // Allocates labels like "__aspect_return_1", "__aspect_return_2", etc.
    public string AllocateReturnLabel();

    // Recursion helpers
    public InliningAnalysisContext Recurse();
    public InliningAnalysisContext RecurseWithSimpleInlining();
    public InliningAnalysisContext RecurseWithComplexInlining(string? returnVariableIdentifier);
}
```

#### ReturnStatementProperties
Analyzed properties of return statements in the target body.

> **Source:** `Metalama.Framework.Engine/Linking/ReturnStatementProperties.cs`

```csharp
// ReturnStatementProperties.cs
internal sealed class ReturnStatementProperties
{
    // True if control would naturally flow to method exit after rewriting
    public bool FlowsToExitIfRewritten { get; }

    // True if inside switch and needs break instead of removal
    public bool ReplaceWithBreakIfOmitted { get; }
}
```

### Transformation Pipeline

```
1. AspectReference in source
       ↓
2. InlinerProvider.TryGetInliner() selects appropriate Inliner
       ↓
3. InliningAlgorithm creates InliningSpecification
       ↓
4. SubstitutionGenerator creates:
   - InliningSubstitution (for the reference itself)
   - ReturnStatementSubstitution (for each return in inlined body)
       ↓
5. Substitutions applied during rewriting phase
       ↓
6. Final transformed code
```

## Inlining Strategies: Tail Position vs Mid-Body

The linker uses two inlining strategies based on **where the inlined code appears** in the containing method. The key question is: *what happens to `return` statements in the inlined body?*

### The Problem

When we inline a method body, we must handle its `return` statements correctly:

```csharp
// Original method to be inlined
private int Original(int x)
{
    if (x == 0) return 42;  // ← What happens to this return?
    return x;
}
```

If we inline this into `return meta.Proceed();`, the returns are fine - they exit the method as expected.

But if we inline into the **middle** of a method:
```csharp
var result = meta.Proceed();  // Inline here
Console.WriteLine("After");   // ← This code must still run!
return result;
```

A naive inline would be **wrong** - the `return 42;` would skip `Console.WriteLine("After")`.

### Tail-Position Inlining (Simple)

Used when the inlined body appears at a **tail position** - where its return statements naturally exit the method.

> **Source:** `LinkerAnalysisStep.InliningAlgorithm.cs`, property `UsingSimpleInlining` in `InliningAnalysisContext`

**Tail positions:**
- Replacing a `return` statement (e.g., `return meta.Proceed();`)
- Replacing an initializer (`EqualsValueClause`)
- Inlining into a `Final` semantic (the outermost entry point)

**Transformation:** Return statements are preserved or converted to simple assignments.

**Example test:** `tests/Metalama.Framework.Tests.LinkerTests/Tests/Methods/Inliners/MethodReturn.cs`

```csharp
// Template
return meta.Proceed();  // Tail position: return statement

// Original method
private int Original() { return 42; }

// After inlining - return is preserved as-is
private int Foo()
{
    return 42;
}
```

### Mid-Body Inlining (with Control Flow Transformation)

Used when the inlined body appears in the **middle** of a method, with code that must execute afterward. Return statements must be transformed to avoid incorrectly exiting the method.

> **Source:** `InliningAnalysisContext.AllocateReturnLabel()`, `Substitution/ReturnStatementSubstitution.cs`

**Transformation required:**
- **Return variable**: Captures the return value (e.g., `int __aspect_retval_1;`)
- **Return label**: A `goto` target after the inlined block (e.g., `__aspect_return_1:`)
- **Return → goto**: Each `return expr;` becomes `{ __aspect_retval_1 = expr; goto __aspect_return_1; }`

**Example test:** `tests/Metalama.Framework.Tests.LinkerTests/Tests/Methods/Overrides/Jump/ReturnsInt_FJ.cs`

```csharp
// Original method (to be inlined)
private int Original(int x)
{
    if (x == 0) return 42;  // Early return
    return x;
}

// Template (NOT tail position - has code after Proceed)
var result = meta.Proceed();
Console.WriteLine("After");
return result;

// After inlining - returns become goto
private int Foo(int x)
{
    int result;
    // Inlined body with transformed returns:
    if (x == 0)
    {
        result = 42;
        goto __aspect_return_1;  // return 42; → assign + goto
    }
    result = x;
    goto __aspect_return_1;      // return x; → assign + goto
__aspect_return_1:               // Label: jump target
    Console.WriteLine("After");  // Code after Proceed() still runs
    return result;
}
```

**Why "goto"?** The `goto` ensures that:
1. The return value is captured in the variable
2. Control jumps past the inlined block to the label
3. Code after `meta.Proceed()` executes correctly
4. The method returns the captured value at the end

### Flow Analysis

The `BodyAnalyzer` performs control flow analysis to determine which return statements need transformation.

> **Source:** `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs`

```csharp
// LinkerAnalysisStep.SemanticBodyAnalyzer.cs
// Uses Roslyn's AnalyzeControlFlow to find:
// 1. All return statements in the body
// 2. Whether each return "flows to exit" if rewritten
// 3. Whether a return is inside a switch (needs break, not removal)
```

The key property is `FlowsToExitIfRewritten` (defined in `ReturnStatementProperties.cs`):
- **True**: The return is at the natural end of a block - can be removed
- **False**: The return has code after it - needs `goto` to the return label

## Available Inliners

Each inliner handles a specific **syntax pattern**. The linker tries each inliner in order until one matches.

> **Source:** `Metalama.Framework.Engine/Linking/Inlining/InlinerProvider.cs`

### Selection Process

For each aspect reference, `InlinerProvider.TryGetInliner()` checks:
1. `IsValidForTargetSymbol()` - Does the target symbol type match? (method, property, event, constructor)
2. `IsValidForContainingSymbol()` - Is the containing context valid?
3. `CanInline()` - Does the **exact syntax pattern** match?

### Method Inliners

All synchronous method inliners require:
- Target is `IMethodSymbol` (not constructor, not property/event accessor)
- Target is **not async** and **not an iterator** (yield return)
- **Canonical invocation**: Arguments must match parameters exactly (same symbols, same order)

For async methods, see [Async Method Inliners](#async-method-inliners) below.

> **Source:** `Metalama.Framework.Engine/Linking/Inlining/MethodInliner.cs:22-40` for `IsCanonicalInvocation()`

| Inliner | Syntax Pattern | Return Variable | Example |
|---------|---------------|-----------------|---------|
| `MethodReturnStatementInliner` | `return Method();` | None (tail position) | `return meta.Proceed();` |
| `MethodCastReturnStatementInliner` | `return (T)Method();` | None (tail position) | `return (int)meta.Proceed();` |
| `MethodAssignmentInliner` | `local = Method();` | Uses existing local | `x = meta.Proceed();` |
| `MethodLocalDeclarationInliner` | `T local = Method();` | Uses declared local | `var x = meta.Proceed();` |
| `MethodInvocationInliner` | `Method();` | None (void) | `meta.Proceed();` |
| `MethodDiscardInliner` | `_ = Method();` | None (discarded) | `_ = meta.Proceed();` |

**Why canonical invocation matters:**

```csharp
// ✅ Canonical - can inline (arguments are the parameters)
void Override(int x, string y) {
    meta.Proceed();  // Calls original with same x, y
}

// ❌ Not canonical - cannot inline (argument differs from parameter)
void Override(int x, string y) {
    // Cannot inline: would need to substitute "42" for "x" in the body
    Link(This.Method)(42, y);
}
```

> **Source:** Each inliner in `Metalama.Framework.Engine/Linking/Inlining/Method*Inliner.cs`

### Property Inliners

Property inliners handle getter and setter accessor bodies.

| Inliner | Syntax Pattern | Context | Example |
|---------|---------------|---------|---------|
| `PropertyGetReturnInliner` | `return Property;` | Getter, tail position | `return meta.Proceed();` |
| `PropertyGetCastReturnInliner` | `return (T)Property;` | Getter, tail position | `return (int)meta.Proceed();` |
| `PropertyGetAssignmentInliner` | `local = Property;` | Getter | `x = meta.Proceed();` |
| `PropertyGetLocalDeclarationInliner` | `T local = Property;` | Getter | `var x = meta.Proceed();` |
| `PropertySetValueAssignmentInliner` | `Property = value;` | Setter only | `meta.Proceed() = value;` (conceptually) |

**Setter constraint:** `PropertySetValueAssignmentInliner` only matches when the right-hand side is exactly the identifier `value` (the implicit setter parameter).

> **Source:** `Metalama.Framework.Engine/Linking/Inlining/Property*Inliner.cs`

### Event Inliners

Event inliners handle add and remove accessor bodies.

| Inliner | Syntax Pattern | Context | Example |
|---------|---------------|---------|---------|
| `EventAddAssignmentInliner` | `Event += value;` | Add accessor | `meta.Proceed() += value;` (conceptually) |
| `EventRemoveAssignmentInliner` | `Event -= value;` | Remove accessor | `meta.Proceed() -= value;` (conceptually) |

**Constraint:** Both require the right-hand side to be exactly the identifier `value` (the implicit accessor parameter).

> **Source:** `Metalama.Framework.Engine/Linking/Inlining/Event*Inliner.cs`

### Constructor Inliner

Handles constructor chaining scenarios.

| Inliner | Syntax Pattern | Example |
|---------|---------------|---------|
| `ConstructorInliner` | `Ctor(new T(args));` | Constructor body calling base/this |

**Constraints:**
- Both containing and target must be constructors
- Uses `ObjectCreationExpressionSyntax` as the single argument
- Arguments in object creation must match constructor parameters

> **Source:** `Metalama.Framework.Engine/Linking/Inlining/ConstructorInliner.cs`

### Async Method Inliners

Async method inliners handle `await` expressions that call aspect references. They support both parenthesized and non-parenthesized await expressions (since `meta.ProceedAsync()` generates parenthesized expressions for `Task<T>`/`ValueTask<T>`).

All async method inliners require:
- Target is `IMethodSymbol` with `IsAsync: true`
- Target is **not** a `Base` semantic (introductions with empty bodies cannot be inlined)
- **Canonical invocation**: Arguments must match parameters exactly

> **Source:** `Metalama.Framework.Engine/Linking/Inlining/AsyncMethodInliner.cs` (base class)

| Inliner | Syntax Pattern | Return Variable | Example |
|---------|---------------|-----------------|---------|
| `AwaitReturnStatementInliner` | `return await Method();` | None (tail position) | `return await meta.ProceedAsync();` |
| `AwaitCastReturnStatementInliner` | `return (T)await Method();` | None (tail position) | `return (int)await meta.ProceedAsync();` |
| `AwaitAssignmentInliner` | `local = await Method();` | Uses existing local | `x = await meta.ProceedAsync();` |
| `AwaitLocalDeclarationInliner` | `var local = await Method();` | Uses declared local | `var x = await meta.ProceedAsync();` |
| `AwaitExpressionStatementInliner` | `await Method();` | None (void) | `await meta.ProceedAsync();` |
| `AwaitDiscardInliner` | `_ = await Method();` | None (discarded) | `_ = await meta.ProceedAsync();` |

**Parenthesized patterns:** All async inliners also handle parenthesized variants like `return (await Method());` and `x = (await Method());`, since template expansion wraps await expressions in parentheses for `Task<T>` and `ValueTask<T>` return types.

> **Source:** Each inliner in `Metalama.Framework.Engine/Linking/Inlining/Await*Inliner.cs`

### Patterns NOT Supported (Inlining Prevented)

The following patterns **cannot be inlined** because no inliner matches:

```csharp
// ❌ Method call as part of larger expression
var x = meta.Proceed() + 1;

// ❌ Method call in conditional
if (meta.Proceed()) { }

// ❌ Method call as argument to another method
Console.WriteLine(meta.Proceed());

// ❌ Non-canonical invocation (arguments don't match parameters)
meta.Proceed(42);  // When parameter is not 42

// ❌ Iterator methods (yield return)
IEnumerable<int> Method() { yield return meta.Proceed(); }

// ❌ Async iterator methods (IAsyncEnumerable)
async IAsyncEnumerable<int> Method() { yield return await meta.ProceedAsync(); }

// ❌ async void methods (use special handling via __LinkerInjectionHelpers__)
async void Method() { await meta.ProceedAsync(); }

// ❌ Property setter with non-value assignment
this.Property = someOtherValue;  // Must be exactly "value"

// ❌ Await with ConfigureAwait or ContinueWith
return await meta.ProceedAsync().ConfigureAwait(false);
```

When no inliner matches, the aspect reference becomes a **regular method call** instead of being inlined.

## Substitution System

Substitutions transform the syntax tree during rewriting.

### InliningSubstitution

Replaces the aspect reference with the inlined body.

> **Source:** `Metalama.Framework.Engine/Linking/Substitution/InliningSubstitution.cs`

```csharp
// InliningSubstitution.cs
public override SyntaxNode Substitute(SyntaxNode currentNode, SubstitutionContext context)
{
    var statements = new List<StatementSyntax>();

    // 1. Declare return variable if needed (complex inlining)
    if (this._specification.DeclareReturnVariable)
    {
        statements.Add(LocalDeclarationStatement(...));
    }

    // 2. Get the substituted body of the target method
    var substitutedBody = context.RewritingDriver.GetSubstitutedBody(
        this._specification.TargetSemantic,
        context.WithInliningContext(this._specification.ContextIdentifier));

    // 3. Let the inliner transform the body
    var inlinedBody = this._specification.Inliner.Inline(..., substitutedBody);
    statements.Add(inlinedBody);

    // 4. Add return label if needed (complex inlining with early returns)
    if (this._specification.ReturnLabelIdentifier != null)
    {
        statements.Add(LabeledStatement(returnLabel, EmptyStatement()));
    }

    return syntaxGenerator.FormattedBlock(statements);
}
```

### ReturnStatementSubstitution

Transforms return statements in inlined bodies.

> **Source:** `Metalama.Framework.Engine/Linking/Substitution/ReturnStatementSubstitution.cs`

```csharp
// ReturnStatementSubstitution.cs
public override SyntaxNode Substitute(SyntaxNode currentNode, SubstitutionContext context)
{
    switch (currentNode)
    {
        case ReturnStatementSyntax returnStatement:
            if (this._returnLabelIdentifier != null)
            {
                // Complex inlining with early return
                if (returnStatement.Expression != null)
                {
                    // return expr; → { result = expr; goto __aspect_return_N; }
                    return FormattedBlock(
                        CreateAssignmentStatement(returnStatement.Expression),
                        CreateGotoStatement());
                }
                else
                {
                    // return; → goto __aspect_return_N;
                    return CreateGotoStatement();
                }
            }
            else
            {
                // Simple inlining: just assign to result variable
                if (returnStatement.Expression != null)
                {
                    // return expr; → result = expr;
                    return CreateAssignmentStatement(returnStatement.Expression);
                }
                else
                {
                    // return; → ; (empty statement, or break if in switch)
                    return this._replaceByBreakIfOmitted
                        ? BreakStatement()
                        : EmptyStatement();
                }
            }
    }
}
```

## Test Examples

### Linker Tests (DSL Syntax)

Located in: `Metalama.Framework/src/tests/Metalama.Framework.Tests.LinkerTests/Tests/`

These tests use the test-specific DSL (`Link(...)`) to explicitly specify aspect references. The DSL is transformed into `AspectReferenceAnnotation` by `TestMethodBodyRewriter`.

**Methods/Inliners/MethodAssignment.cs** - Assignment inlining:
```csharp
// Input (linker test DSL)
x = Link(This.Foo, Inline)();

// Output: body inlined, return value assigned to x
```

**Methods/Inliners/MethodReturn.cs** - Return statement inlining:
```csharp
// Input (linker test DSL)
return Link(This.Foo, Inline)();

// Output: body inlined, return preserved
```

### Complex Inlining Tests (with control flow)

**Methods/Overrides/Jump/ReturnsInt_FJ.cs** - Early return handling:
```csharp
// Input: Method with conditional early return
private int Foo(int x)
{
    if (x == 0) return 42;  // Early return
    return x;
}

// Override calling with Inline (linker test DSL)
result = Link(This.Foo, Inline)(x);

// Output: Uses goto for early return
if (x == 0)
{
    result = 42;
    goto __aspect_return_1;
}
result = x;
goto __aspect_return_1;
__aspect_return_1:
```

**Methods/Overrides/Jump/ReturnsVoid_FJ_NJ.cs** - Multiple overrides with different return patterns.

### Negative Tests (inlining prevented)

**Methods/Inliners/MethodAssignment_NotExactArguments.cs** - Non-canonical invocation:
```csharp
// Inlining fails: arguments don't match parameters (linker test DSL)
x = Link(This.Foo, Inline)(42);  // Hardcoded argument, not parameter
```

### Aspect Tests (Production Syntax)

Located in: `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/`

These tests use real aspects with `meta.Proceed()`. The template expansion system creates the same `AspectReferenceAnnotation` that linker tests create explicitly.

**Example: Tests/Formatting/Output/OverrideNoInlining.cs** - Multiple `meta.Proceed()` prevents inlining:
```csharp
// Input (aspect template)
public override dynamic? OverrideMethod()
{
    Console.WriteLine("Generated code.");
    try
    {
        meta.Proceed();         // First call
        return meta.Proceed();  // Second call - prevents inlining (referenced twice)
    }
    catch (Exception)
    {
        Console.WriteLine("Oops!");
        throw;
    }
}
```

When a semantic is referenced more than once, it cannot be inlined. This constraint ensures a **one-to-zero-or-one mapping** between source code and generated code, which is essential for debugging - a breakpoint in source code should correspond to at most one location in the output.

## Source Code References

All paths are relative to `Metalama.Framework/src/`.

### Core Linker Implementation

| File | Purpose |
|------|---------|
| `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.InlineabilityAnalyzer.cs` | Determines which semantics and references can be inlined |
| `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.InliningAlgorithm.cs` | Decides what will be inlined, generates `InliningSpecification` |
| `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs` | Analyzes control flow, determines `FlowsToExitIfRewritten` |
| `Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SubstitutionGenerator.cs` | Creates substitutions from inlining specifications |
| `Metalama.Framework.Engine/Linking/ResolvedAspectReference.cs` | Data structure representing a resolved aspect reference |
| `Metalama.Framework.Engine/Linking/AspectReferenceResolver.cs` | Walks syntax tree, creates `ResolvedAspectReference` from annotations |

### Inlining Infrastructure

| File | Purpose |
|------|---------|
| `Metalama.Framework.Engine/Linking/Inlining/Inliner.cs` | Abstract base class for all inliners |
| `Metalama.Framework.Engine/Linking/Inlining/InlinerProvider.cs` | Maintains list of inliners, selects appropriate one |
| `Metalama.Framework.Engine/Linking/Inlining/InliningSpecification.cs` | Complete specification for one inlining operation |
| `Metalama.Framework.Engine/Linking/Inlining/InliningAnalysisContext.cs` | Tracks state during analysis (IDs, return labels) |
| `Metalama.Framework.Engine/Linking/Inlining/MethodInliner.cs` | Base class for synchronous method inliners |
| `Metalama.Framework.Engine/Linking/Inlining/MethodAssignmentInliner.cs` | Handles `x = Link(..., Inline)()` pattern |
| `Metalama.Framework.Engine/Linking/Inlining/MethodReturnStatementInliner.cs` | Handles `return Link(..., Inline)()` pattern |
| `Metalama.Framework.Engine/Linking/Inlining/AsyncMethodInliner.cs` | Base class for async method inliners |
| `Metalama.Framework.Engine/Linking/Inlining/AwaitReturnStatementInliner.cs` | Handles `return await Link(..., Inline)()` pattern |
| `Metalama.Framework.Engine/Linking/Inlining/AwaitAssignmentInliner.cs` | Handles `x = await Link(..., Inline)()` pattern |
| `Metalama.Framework.Engine/Linking/Inlining/AwaitLocalDeclarationInliner.cs` | Handles `var x = await Link(..., Inline)()` pattern |
| `Metalama.Framework.Engine/Linking/Inlining/AwaitExpressionStatementInliner.cs` | Handles `await Link(..., Inline)()` pattern |
| `Metalama.Framework.Engine/Linking/Inlining/AwaitDiscardInliner.cs` | Handles `_ = await Link(..., Inline)()` pattern |
| `Metalama.Framework.Engine/Linking/Inlining/AwaitCastReturnStatementInliner.cs` | Handles `return (T)await Link(..., Inline)()` pattern |
| `Metalama.Framework.Engine/Linking/Inlining/PropertyGetReturnInliner.cs` | Handles `return Link(..., Inline)` for properties |
| `Metalama.Framework.Engine/Linking/Inlining/ImplicitLastOverrideReferenceInliner.cs` | Special inliner for non-expression aspect references |

### Substitutions

| File | Purpose |
|------|---------|
| `Metalama.Framework.Engine/Linking/Substitution/InliningSubstitution.cs` | Replaces aspect reference with inlined body |
| `Metalama.Framework.Engine/Linking/Substitution/ReturnStatementSubstitution.cs` | Transforms return statements in inlined bodies |

### Supporting Types

| File | Purpose |
|------|---------|
| `Metalama.Framework.Engine/Linking/InliningContextIdentifier.cs` | Unique identifier for inlining context |
| `Metalama.Framework.Engine/Linking/ReturnStatementProperties.cs` | Properties of return statements (`FlowsToExitIfRewritten`) |

### Aspect Reference System (Production)

| File | Purpose |
|------|---------|
| `Metalama.Framework.Engine/Aspects/AspectReferenceSpecification.cs` | Annotation data: `AspectLayerId`, `Order`, `TargetKind`, `Flags` |
| `Metalama.Framework.Engine/Aspects/AspectReferenceAnnotationExtensions.cs` | `WithAspectReferenceAnnotation()` extension method |
| `Metalama.Framework.Engine/Aspects/AspectReferenceOrder.cs` | Enum: `Previous`, `Base`, `Current`, `Final` |
| `Metalama.Framework.Engine/Aspects/AspectReferenceTargetKind.cs` | Enum: `Self`, `PropertyGetAccessor`, etc. |
| `Metalama.Framework.Engine/Aspects/AspectReferenceFlags.cs` | Enum: `None`, `Inlineable`, `CustomReceiver` |
| `Metalama.Framework.Engine/CodeModel/Invokers/MethodInvoker.cs` | Creates annotated invocations from templates (see `CreateInvocationExpression`) |

### Linker Test Infrastructure

| File | Purpose |
|------|---------|
| `tests/Metalama.Framework.Tests.LinkerTests/Tests/_Helpers/Api.cs` | DSL definitions: `Link`, `This`, `Static`, `Local`, `Cast`, `Inline`, `Base`, etc. |
| `tests/Metalama.Framework.Tests.LinkerTests/Tests/_Helpers/PseudoOverride.cs` | `[PseudoOverride]` attribute definition |
| `tests/Metalama.Framework.Tests.LinkerTests/Tests/_Helpers/PseudoIntroduction.cs` | `[PseudoIntroduction]` attribute definition |
| `tests/Metalama.Framework.Tests.LinkerTests/Tests/_Helpers/PseudoNotInlineable.cs` | `[PseudoNotInlineable]` attribute definition |
| `tests/Metalama.Framework.Tests.LinkerTests/Runner/LinkerTestInputBuilder.TestMethodBodyRewriter.cs` | Transforms `Link(...)` DSL to `AspectReferenceAnnotation` |
| `tests/Metalama.Framework.Tests.LinkerTests/Runner/LinkerTestInputBuilder.TestTypeRewriter.cs` | Processes `[PseudoOverride]`, `[PseudoIntroduction]` |

### Key Test Files

| Test File | What it demonstrates |
|-----------|---------------------|
| `tests/Metalama.Framework.Tests.LinkerTests/Tests/Methods/Inliners/MethodAssignment.cs` | Simple assignment inlining |
| `tests/Metalama.Framework.Tests.LinkerTests/Tests/Methods/Inliners/MethodReturn.cs` | Return statement inlining |
| `tests/Metalama.Framework.Tests.LinkerTests/Tests/Methods/Inliners/MethodAssignment_NotExactArguments.cs` | Inlining prevented (non-canonical call) |
| `tests/Metalama.Framework.Tests.LinkerTests/Tests/Methods/Overrides/Jump/ReturnsInt_FJ.cs` | Complex inlining with early return (`goto`) |
| `tests/Metalama.Framework.Tests.LinkerTests/Tests/Methods/Overrides/Jump/ReturnsVoid_FJ_NJ.cs` | Multiple overrides with mixed control flow |
| `tests/Metalama.Framework.Tests.LinkerTests/Tests/Properties/Inliners/` | Property getter/setter inlining tests |
| `tests/Metalama.Framework.Tests.LinkerTests/Tests/Events/Inliners/` | Event add/remove inlining tests |
| `tests/Metalama.Framework.Tests.AspectTests/Tests/Formatting/Output/OverrideNoInlining.cs` | Production aspect with multiple `meta.Proceed()` |
