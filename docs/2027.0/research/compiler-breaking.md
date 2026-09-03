# C# / Roslyn compiler breaking changes for .NET 11 (C# 15)

Research date: 2026-09-03. .NET 11 / C# 15 are in preview; GA is November 2026.

## Primary sources used

| # | Source | Notes |
|---|---|---|
| S1 | https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2011 | Canonical published list. Page metadata: `updated_at: 2026-08-13`, rendered from roslyn commit `1284a4abf6ee5778539a554f8df12ea0040415ce`. |
| S2 | https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Compiler%20Breaking%20Changes%20-%20DotNet%2011.md | Upstream source of S1. Fetched from `main` on 2026-09-03: **12 `##` sections**, identical set to S1. The two sources do **not** contradict each other. |
| S3 | https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Warnversion%20Warning%20Waves.md | Warning-wave inventory. Highest wave present = **10**. |
| S4 | https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/warning-waves | `ms.date: 2026-04-30`. Text ends at "Warning wave 10 diagnostics were added in C# 14." |
| S5 | https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/LanguageVersion.cs | `CSharp15 = 1500` is the highest concrete member. **No `CSharp16` member exists.** `CurrentVersion => LanguageVersion.CSharp15`. `Latest`/`Default`/`LatestMajor` map to `CSharp15`. `Preview = int.MaxValue - 1`. |
| S6 | https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15 | `updated_at: 2026-08-19`. |
| S7 | https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/union-declaration-errors | Authoritative message text for CS9370-CS9387. |
| S8 | https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/unsafe-code-errors | Authoritative message text for CS9360-CS9363, CS9376. |
| S9 | https://github.com/dotnet/csharplang/blob/main/proposals/unsafe-evolution.md | Unsafe-evolution spec: `safe` modifier, `unsafe(expr)`, `MemorySafetyRulesAttribute`, `RequiresUnsafeAttribute`, compat mode, breaking-changes section. |
| S10 | https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md | Union grammar. |
| S11 | https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/closed-hierarchies.md | `closed` modifier rules. |
| S12 | https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/closed | Contextual-keyword note for `closed`. |
| S13 | https://learn.microsoft.com/dotnet/csharp/language-reference/unsafe-code#the-updated-memory-safety-model-preview | "The updated memory safety model (preview)". |
| S14 | GitHub API commit list for the S2 file | Newest commit `1284a4a`, 2026-08-11, "Add C# 15 language version (#84799)". |

Note: `https://learn.microsoft.com/en-us/dotnet/core/compatibility/11.0` returns **HTTP 404** as of 2026-09-03; there is no published .NET 11 breaking-change index page at that URL yet.

---

## 0. Version-numbering caveat that affects reading the whole document (IMPORTANT)

S1/S2 use two different version labels:

* Entries 1-7, 11, 12 talk about **C# 14 vs C# 15** (`LangVersion` 14 vs 15).
* Entries 8, 9, 10 (all unsafe-evolution) talk about **"C# 16"** and `langversion:16`.

But S5 shows there is **no `LanguageVersion.CSharp16`** in `main`, and `CurrentVersion` is `CSharp15`.
S14 shows the most recent edit to the doc is commit `1284a4a` (2026-08-11) titled *"Add C# 15 language version (#84799)"*.

Reconstruction: before #84799 the C# 15 feature set was reachable only through `<LangVersion>preview</LangVersion>`. When `LanguageVersion.CSharp15` was added as a concrete value, the features that were **not** being finalized for C# 15 (the unsafe-evolution set) were re-labelled "C# 16" in prose, meaning *the next, still-unnamed language version reachable only through `LangVersion=preview`*.

Practical reading for .NET 11 GA:

* Entries 1-7, 11, 12 are **live at LangVersion 15 / `latest` / default** in .NET 11.
* Entries 8, 9, 10 are gated behind `<LangVersion>preview</LangVersion>` and are **expected not to apply** at the .NET 11 GA default. See Open Questions - the Learn reference pages contradict this.

---

## 1. Safe-context of a `Span<T>` / `ReadOnlySpan<T>` collection expression is now *declaration-block*

* **Introduced in**: Visual Studio 2026 version 18.3.
* **Category**: language semantics / ref-safety.
* **Language-version gated?** Not stated in the doc. Presented as an unconditional conformance fix ("the compiler used safe-context *function-member* ... We have now made a change to use *declaration-block* per the specification"). Treat as **not** LangVersion-gated unless verified.
* **Diagnostic**: no new ID; existing ref-safety escape errors (the CS8352 / CS8347 family) now fire where they did not before.
* **Spec clause being honoured** (collection-expressions spec, ref safety section):
  > If the target type is a *span type* `System.Span<T>` or `System.ReadOnlySpan<T>`, the safe-context of the collection expression is the *declaration-block*.

Breaking code:

```cs
scoped Span<int> items1 = default;
scoped Span<int> items2 = default;
foreach (var x in new[] { 1, 2 })
{
    Span<int> items = [x];
    if (x == 1)
        items1 = items; // previously allowed, now an error

    if (x == 2)
        items2 = items; // previously allowed, now an error
}
```

Workaround A - use an array type:

```cs
scoped Span<int> items1 = default;
scoped Span<int> items2 = default;
foreach (var x in new[] { 1, 2 })
{
    int[] items = [x];
    if (x == 1)
        items1 = items; // ok, using 'int[]' conversion to 'Span<int>'

    if (x == 2)
        items2 = items; // ok
}
```

Workaround B - hoist the collection expression to a scope where the assignment is legal:

```cs
scoped Span<int> items1 = default;
scoped Span<int> items2 = default;
Span<int> items = [0];
foreach (var x in new[] { 1, 2 })
{
    items[0] = x;
    if (x == 1)
        items1 = items; // ok

    if (x == 2)
        items2 = items; // ok
}
```

Tracking issue: https://github.com/dotnet/csharplang/issues/9750

---

## 2. Synthesized `ref readonly`-returning delegates now require `System.Runtime.InteropServices.InAttribute`

* **Introduced in**: Visual Studio 2026 version 18.3.
* **Category**: codegen / metadata emission.
* **Diagnostic**: **CS0518** - "Predefined type 'System.Runtime.InteropServices.InAttribute' is not defined or imported".
* **Language-version gated?** Not stated; presented as unconditional.
* **Cause**: the compiler now emits `modreq(InAttribute)` correctly on the return of synthesized delegate types (roslyn commit `64219bb`, "Ensure that `modreq(InAttribute)` is emitted for synthesized `ref readonly`...").

Breaking patterns:

```cs
var d = this.MethodWithRefReadonlyReturn;
```

```cs
var d = ref readonly int () => ref x;
```

* **Workaround**: reference an assembly that defines `System.Runtime.InteropServices.InAttribute`.
* **Who this hits**: minimal reference assemblies, `NoStandardLib` test compilations, and any tooling that builds a `Compilation` from a hand-rolled reference set.

---

## 3. `ref readonly` local functions now require `System.Runtime.InteropServices.InAttribute`

* **Introduced in**: Visual Studio 2026 version 18.3.
* **Category**: codegen / metadata emission.
* **Diagnostic**: **CS0518** (same as #2).
* Breaking pattern:

```cs
void Method()
{
    ...
    ref readonly int local() => ref x;
    ...
}
```

* **Workaround**: reference an assembly defining `System.Runtime.InteropServices.InAttribute`.

---

## 4. `&&` / `||` with an interface-typed left operand and a `dynamic` right operand is now an error

* **Introduced in**: Visual Studio 2026 version 18.3.
* **Category**: language semantics - a runtime failure moved to compile time.
* **Diagnostic**: **CS7083** - "Expression must be implicitly convertible to Boolean or its type 'I1' must not be an interface and must define operator 'false'."
* **Language-version gated?** Not stated; presented as unconditional.
* Previously the code compiled and threw `RuntimeBinderException` at run time, because the runtime binder cannot invoke operators declared on interfaces.

```cs
interface I1
{
    static bool operator true(I1 x) => false;
    static bool operator false(I1 x) => false;
}

class C1 : I1
{
    public static C1 operator &(C1 x, C1 y) => x;
    public static bool operator true(C1 x) => false;
    public static bool operator false(C1 x) => false;
}

void M()
{
    I1 x = new C1();
    dynamic y = new C1();
    _ = x && y; // error CS7083
}
```

Workaround - change the static type of the left operand:

```cs
_ = (C1)x && y;      // Valid - uses operators defined on C1
_ = (dynamic)x && y; // Valid - uses operators defined on C1
```

Tracking issue: https://github.com/dotnet/roslyn/issues/80954

---

## 5. `nameof(this.X)` / `nameof(base.X)` inside an attribute is now disallowed

* **Introduced in**: Visual Studio 2026 version 18.3 **and .NET 10.0.200** (it is also serviced into the .NET 10 band).
* **Category**: language semantics - removal of an unintended permissiveness present since C# 12.
* **Diagnostic**: not named in the doc. The doc says it is "now properly disallowed to match the language specification". Expect the pre-existing "keyword 'this' is not available in the current context" family (CS0026 / CS0027) rather than a new ID - **unverified**.
* **Language-version gated?** Not stated; it is a spec-conformance fix, so likely unconditional.

```cs
class C
{
    string P;
    [System.Obsolete(nameof(this.P))] // now disallowed
    [System.Obsolete(nameof(P))]      // workaround
    void M() { }
}
```

* Implementing PR: https://github.com/dotnet/roslyn/pull/81628
* Tracking issue: https://github.com/dotnet/roslyn/issues/82251

---

## 6. Parsing of `with` inside a *switch-expression-arm*

* **Introduced in**: Visual Studio 2026 version 18.4.
* **Category**: **parser / syntax tree shape**. No diagnostic; the same text produces a *different tree*.
* **Language-version gated?** Not stated. Parse-shape fixes of this kind are normally unconditional. Treat as unconditional pending verification.

Given:

```cs
x switch
{
    (X.Y) when
}
```

* **Before**: `(X.Y)when` was parsed as a *cast-expression* - casting the contextual identifier `when` to the type `(X.Y)`.
* **After**: parsed as a **constant pattern** `(X.Y)` followed by a **`when` clause**.

Consequence: a plain guard such as `(X.Y) when a > b =>` now parses correctly, where before it did not.

Syntax-tree impact: a node that used to come back as `CastExpressionSyntax` (`SyntaxKind.CastExpression`) in this position now comes back as `ConstantPatternSyntax` inside a `SwitchExpressionArmSyntax` with a non-null `WhenClauseSyntax`. Anything that pattern-matches on the shape of switch-expression arms must handle both.

* Issue: https://github.com/dotnet/roslyn/issues/81837
* PR: https://github.com/dotnet/roslyn/pull/81863
* Roslyn commit: `fc2b820`, 2026-01-13, "Fix parsing of parenthesized type in switch arm (#81863)".

---

## 7. `with(...)` as a collection-expression element binds as collection-construction *arguments*

* **Introduced in**: Visual Studio 2026 version 18.4.
* **Category**: language semantics + binding. New C# 15 feature ("collection expression arguments") whose syntax collides with a method call.
* **Language-version gated?** **YES - explicitly.** "when the LangVersion is set to **15 or greater**".
* **Diagnostic**: not a single new ID; the doc's example shows the C# 15 result as "error args not supported for `object[]`".

```cs
object x, y, z = ...;
object[] items;

items = [with(x, y), z];  // C# 14: call to with() method; C# 15: error args not supported for object[]
items = [@with(x, y), z]; // call to with() method
object with(object a, object b) { ... }
```

* **Workaround**: escape the identifier - `@with(...)`.
* **Scope note**: only when `with(...)` is an *element of a collection expression*. The feature itself only accepts `with(...)` as the **first** element (S6).
* Legitimate use of the new feature (S6):

```cs
string[] values = ["one", "two", "three"];
List<string> names = [with(capacity: values.Length * 2), .. values];
HashSet<string> set = [with(StringComparer.OrdinalIgnoreCase), "Hello", "HELLO", "hello"];
```

* Feature spec: https://learn.microsoft.com/dotnet/csharp/language-reference/proposals/csharp-15.0/collection-expression-arguments

---

## 8. Pointer types no longer require an `unsafe` context (overload-resolution break)

* **Introduced in**: Visual Studio 2026 version 18.7.
* **Category**: **overload resolution** + language semantics.
* **Language-version gated?** **YES.** The doc says "In C# 16"; per section 0 this means `<LangVersion>preview</LangVersion>` today. Part of the "unsafe evolution" feature, https://github.com/dotnet/csharplang/issues/9704.
* **Diagnostic**: **CS0121** (ambiguous call) newly reported.

Pointer *types* (`int*`, `delegate*<void>`) become legal outside `unsafe`. Only pointer *indirection* operations still require `unsafe`. Because pointer types are now legal in safe contexts, overload resolution considers candidates it previously excluded:

```cs
using System;

class Program
{
    static void Main()
    {
        M(x => { }); // C# 15: prints "2"; C# 16: error CS0121 (ambiguous)
    }

    static void M(F1 f) { Console.WriteLine(1); }
    static void M(F2 f) { Console.WriteLine(2); }
}

unsafe delegate void F1(int* x);
delegate void F2(int x);
```

Workaround - give the lambda explicit parameter types:

```cs
M((int x) => { }); // Resolves to M(F2)
```

Relaxed operations, i.e. those that no longer need `unsafe` (S13):

1. Declaring a pointer type and taking the address of a variable with `&`.
2. The `fixed` statement that pins a variable.
3. Converting a `stackalloc` expression to a pointer.
4. `sizeof` applied to any unmanaged type.

Operations that still require `unsafe` (S13, S9):

1. Pointer indirection `*p`, pointer member access `p->member`, pointer element access `p[i]`.
2. Function-pointer invocation.
3. Element access on a fixed-size buffer.

New `unsafe(expression)` expression form (requires `AllowUnsafeBlocks`) - usable where an `unsafe` block cannot appear syntactically (field initializer, constructor initializer, `catch` filter):

```cs
class Header
{
    static readonly int Signature = unsafe(ReadSignature());

    static unsafe int ReadSignature()
    {
        int rawValue = 0x1234;
        int* pointer = &rawValue;
        return *pointer;
    }
}
```

From S9: "The `unsafe` context established by an `unsafe_expression` does not extend beyond its closing parenthesis."

---

## 9. `safe` is a contextual keyword

* **Introduced in**: Visual Studio 2026 version 18.9.
* **Category**: **lexing / parsing** - meaning of an existing identifier changes.
* **Language-version gated?** **YES.** Doc says "In C# 16" -> `LangVersion=preview` today. S9: "This new syntax is available under new LangVersion, but regardless of opt-in."
* **Diagnostic**: not named; the break manifests as the type name no longer resolving, or as a parse error.

`safe` becomes a keyword **when placed as a modifier on member declarations**.

```cs
class safe { }

class C
{
    safe M1() => new safe(); // previously `safe` refers to a type, now it is a keyword
    @safe M2() => new safe(); // workaround
}
```

Semantics of the modifier (S9):

* "The `safe` modifier can be applied to all declarations which allow `unsafe` to mark them as *requires-unsafe*." (The quoted sentence reads oddly; per S6 and S13 the intent is that `safe` marks a declaration as **not** *requires-unsafe*.)
* "It is disallowed to apply both the `safe` and `unsafe` modifier on the same declaration."
* "The `safe` modifier only marks the declaration as *not* *requires-unsafe*, it does not introduce a safe context. There is also no `safe` block or expression form."
* In a type with `[StructLayout(LayoutKind.Explicit)]` or `[ExtendedLayout]`, **all instance fields must be marked either `safe` or `unsafe`**; if hidden behind an auto-property or field-like event, the requirement moves to that member.
* S6: "the *requires-unsafe* member model and the assembly opt-in to the updated memory safety rules aren't available yet, so `safe` and `unsafe` currently have no effect on callers."

**Note on the type name `safe`**: `safe` is all-lowercase ASCII, so declaring `class safe { }` already produced **CS8981** (warning wave 7) as a forward-compatibility warning.

---

## 10. `unsafe` required for more members (CS9363)

* **Introduced in**: Visual Studio 2026 version 18.9.
* **Category**: language semantics - a previously missed check is now enforced.
* **Language-version gated?** **YES - explicitly `langversion:16`** (= `preview` today, per section 0).
* **Diagnostic**: **CS9363** - *"'member' must be used in an unsafe context because it has pointers in its signature"* (message text verified in S8).

```cs
var c = new C();
int a = c.M(null); // error always
int b = c[null];   // no error in C# 15, reports CS9363 in C# 16

class C
{
    public unsafe int M(int* x) => 0;
    public unsafe int this[int* x] => 0;
}
```

Workaround - use an `unsafe` block or the new `unsafe` expression:

```cs
int b = unsafe(c[null]);
```

Companion diagnostics introduced by the same feature (all verified in S8):

| ID | Message |
|---|---|
| CS9360 | *This operation may only be used in an unsafe context* |
| CS9361 | *`stackalloc` expression without an initializer inside `SkipLocalsInit` may only be used in an unsafe context* |
| CS9362 | *'member' must be used in an unsafe context because it is marked as '`RequiresUnsafe`' or '`extern`'* |
| CS9363 | *'member' must be used in an unsafe context because it has pointers in its signature* |
| CS9376 | *An unsafe context is required for constructor 'constructor' marked as '`RequiresUnsafe`' or '`extern`' to satisfy the '`new()`' constraint of type parameter 'type parameter' in 'generic type or method'* |

Compat mode (S9), which is what makes CS9363 fire for legacy references:

> "For compat purposes ... members from non-updated modules are considered *requires-unsafe* if containing pointer or function pointer types in parameter or return types (nested in non-pointer types acceptable)."
> "Such compat-mode *requires-unsafe* members require an `unsafe` context to be used even from callers that have not opted into the updated memory-safety rules."

Full list of breaks the spec itself flags for when the updated rules are enabled (S9, "Breaking changes"):

1. `unsafe` on a member now also marks it *requires-unsafe*: callers must be in an `unsafe` context, and overrides cannot be `unsafe` if the base member is safe.
2. `unsafe` on a member or type **no longer introduces an `unsafe` context**; explicit `unsafe` blocks are required around unsafe operations in bodies and initializers.
3. `extern` members and fields in explicit layout require an explicit `unsafe`/`safe` keyword on the declaration.
4. `stackalloc` under certain conditions requires an `unsafe` context.
5. `unsafe` is an **error** on type declarations, static constructors and destructors, because it has no effect there.
6. (S13) Delegates cannot be `unsafe`. A type whose parameterless constructor is `unsafe` does not satisfy the `new()` constraint in declaration positions.

New attributes emitted (S9):

```cs
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Module, Inherited = false)]
    public sealed class MemorySafetyRulesAttribute : Attribute
    {
        public MemorySafetyRulesAttribute(int version) => Version = version;
        public int Version { get; }
    }

    [AttributeUsage(AttributeTargets.Event | AttributeTargets.Method |
        AttributeTargets.Property | AttributeTargets.Constructor,
        AllowMultiple = false, Inherited = false)]
    public sealed class RequiresUnsafeAttribute : Attribute { }
}
```

* `MemorySafetyRulesAttribute` is emitted on the **module**, "filled in with `15` as the language version".
* "It is an error to apply the `MemorySafetyRulesAttribute` or `RequiresUnsafeAttribute` to any symbol explicitly in source."
* Both attribute definitions are **synthesized by the compiler if necessary** per the standard well-known-member rules.
* The compiler ignores `RequiresUnsafeAttribute`-marked members from assemblies using legacy memory-safety rules (compat mode applies instead).

---

## 11. `closed` is a contextual keyword in type-declaration contexts

* **Introduced in**: Visual Studio 2026 version 18.10.
* **Category**: **lexing / parsing** - meaning of an existing identifier changes.
* **Language-version gated?** **YES - C# 15** ("In C# 15, a type or alias declaration named `closed` ... produces CS9380"). This one **is** live at the .NET 11 GA default LangVersion.
* **Diagnostics**:
  * **CS9380** - *"Types and aliases cannot be named 'closed'."* (verified, S7)
  * **CS1519** - the existing "Invalid token in class, record, struct, or interface member declaration" error, produced when `closed` in a member-declaration context is consumed as a modifier and the rest no longer parses.

```cs
class @closed { }

class C
{
    closed oldField;      // C# 14: field of type 'closed'; C# 15: parsed as an incomplete declaration
    @closed currentField; // field of type 'closed'
}
```

Two distinct effects:

1. **Declaring** a type or alias named `closed` without `@` is now an error (CS9380). This is a hard ban on the *declaration*, not just an ambiguity.
2. **Using** `closed` as a type name in a *member declaration* position now parses as a modifier, so the remainder becomes an incomplete declaration, producing CS1519.

Workaround: escape both the declaration and the references with `@`.

Semantics of the modifier (S6, S11, S12):

* Applies to a `class`. A `closed` class can only be derived from within its declaring assembly.
* A `closed` class is **implicitly `abstract`**; it cannot also be `sealed`, `static`, or explicitly `abstract` (CS9381, CS9384).
* Derivation is **not transitive**: a non-`closed` descendant of a `closed` class can still be derived from in other assemblies.
* Makes a `switch` expression over the direct descendants exhaustive without a default arm.
* S12: "`closed` is a contextual keyword. It has special meaning only when it appears as a modifier on a class declaration. You can continue to use `closed` as an identifier in other contexts."
* S11 drawback noted by the spec: "It can be a breaking change to add a `closed` modifier to an existing class, or to add an additional derived class from a closed class."

```cs
public closed record class GateState;
public record class Closed : GateState;
public record class Open(float Percent) : GateState;

string Describe(GateState state) => state switch
{
    Closed => "closed",
    Open(var percent) => $"{percent}% open",
    // No warning: every direct descendant of 'GateState' is handled.
};
```

Related new diagnostics (S7):

| ID | Message |
|---|---|
| CS9380 | *Types and aliases cannot be named 'closed'.* |
| CS9381 | *'type': a closed type cannot be sealed or static* |
| CS9382 | *'type': cannot use a closed type 'type' from another assembly as a base type.* |
| CS9383 | *'type': The type parameter 'parameter' must be referenced in the base type 'type' because the base type is closed.* |
| CS9384 | *'type': a closed type cannot be marked abstract because it is always implicitly abstract.* |

**Note**: `closed` is all-lowercase ASCII, so `class closed { }` already produced **CS8981** (warning wave 7).

---

## 12. `union` is a contextual keyword in type-declaration contexts

* **Introduced in**: Visual Studio 2026 version 18.10.
* **Category**: **lexing / parsing** - meaning of an existing identifier changes.
* **Language-version gated?** **YES - C# 15.** Live at the .NET 11 GA default LangVersion.
* **Diagnostic**: **CS9370** - *"A union declaration must specify at least one case type."* (verified, S7)

```cs
class @union { }

class C
{
    union OldField;      // C# 14: field of type 'union'; C# 15: union declaration
    @union CurrentField; // field of type 'union'
}
```

Mechanism: `union` followed by a type name is now parsed as the **start of a union declaration**. `union OldField;` therefore parses as a union named `OldField` with an *empty* case-type list, which is CS9370 rather than a field declaration.

Note the asymmetry with `closed`: `union` is **not** banned as a type name (there is no equivalent of CS9380 for `union`); only the *use* position breaks.

Workaround: escape with `@`.

Union grammar (S10):

```antlr
union_declaration
    : attributes? struct_modifier* 'partial'? 'union' identifier type_parameter_list?
      '(' case_types ')'  struct_interfaces? type_parameter_constraints_clause*
      ('{' struct_member_declaration* '}' | ';')
    ;
case_types
    : type (',' type)*
    ;
```

Notes from the spec's resolved open questions (S10):

* "A union declaration is a plain struct, not record struct. The `record union ...` isn't supported."
* The base-clause restriction was removed: a union may implement interfaces.
* The restriction "Instance fields, auto-properties or field-like events are not permitted" is **kept** (CS9373).
* Design concern acknowledged in the spec: "Parenthesized lists look too much like primary constructors (despite not having parameter names)."

Example (S6):

```cs
public record class Cat(string Name);
public record class Dog(string Name);
public record class Bird(string Name);

public union Pet(Cat, Dog, Bird);

Pet pet = new Dog("Rex");
string name = pet switch
{
    Dog d => d.Name,
    Cat c => c.Name,
    Bird b => b.Name,
};
```

Runtime support: "The runtime includes the `UnionAttribute` and `IUnion` types beginning with .NET 11 Preview 5. Some features from the proposal specification aren't yet implemented." (S6)

Related new diagnostics (S7):

| ID | Message |
|---|---|
| CS9370 | *A union declaration must specify at least one case type.* |
| CS9371 | *Cannot convert type 'type' to 'object' via an implicit reference or boxing conversion.* |
| CS9373 | *Instance fields, auto-properties or field-like events are not permitted in a 'union' declaration.* |
| CS9374 | *Explicitly declared public constructors with a single parameter are not permitted in a 'union' declaration.* |
| CS9375 | *A constructor declared in a 'union' declaration must have a 'this' initializer that calls a synthesized constructor or an explicitly declared constructor.* |
| CS9385 | *A union type must have at least one union creation member.* |
| CS9386 | *A union member provider type must have an instance 'Value' property of type 'object?' or 'object'. The property must have a public get accessor.* |
| CS9387 | *A 'union' declaration cannot use a union member provider interface.* |

**Note**: `union` is all-lowercase ASCII, so `class union { }` already produced **CS8981** (warning wave 7). The same is true of `closed` and `safe`. CS8981 exists precisely to have warned about these three cases years in advance.

---

## Cross-cutting answers to the assignment's specific questions

### Warning waves - is there a wave 11?

**No.** As of 2026-09-03:

* S3 (roslyn `Warnversion Warning Waves.md`) tops out at **Warning Level 10**, whose only member is **CS9265** - *"Field is never ref-assigned to, and will always have its default value (null reference)"* (added in C# 14).
* S4 (Learn) states: "Warning wave 10 diagnostics were added in C# 14." and lists nothing beyond.

So **no new warning wave and no new wave-gated warning** has been introduced for C# 15 / .NET 11. If the .NET 11 SDK sets `WarningLevel`/`AnalysisLevel` to 11 by tracking the target framework, that level currently carries no new diagnostics.

Complete wave inventory for reference (S3):

* **Wave 10**: CS9265.
* **Wave 8**: CS9123 ("Taking address of local or parameter in async method can create a GC hole"); plus the `EnableGenerateDocumentationFile` helper diagnostic used to enforce IDE0005 on build.
* **Wave 7**: CS8981 (all-lowercase-ASCII type name may become reserved).
* **Wave 6**: CS8826 (partial method declarations have signature differences).
* **Wave 5**: CS7023, CS8073, CS8848, CS8880-CS8887, CS8892, CS8897, CS8898.

### New warnings on by default

None found in S1/S2. Every entry in the .NET 11 compiler breaking-change list is an **error** (new or newly reported), a **parse-shape change**, or an **overload-resolution change**. No entry introduces a warning.

Indirectly, C# 15's `closed` and `union` features change **exhaustiveness** analysis for `switch`, which affects the existing CS8509 ("the switch expression does not handle all possible values") in the *permissive* direction: a switch over a closed hierarchy's direct descendants no longer needs a default arm and no longer warns. Conversely, adding a case type to a `union`, or a direct descendant to a `closed` class, will newly produce exhaustiveness warnings at every `switch` that does not handle it. That is a source-compatibility hazard for library authors, not a compiler break.

### Warnings promoted to errors

None found in S1/S2.

### Nullable analysis

No breaking change to nullable analysis is listed in S1/S2. The only nullable-adjacent statement found is that union types provide "enhanced nullability tracking" (Learn, `builtin-types/union`), which is new analysis for a new construct rather than a change to existing analysis.

### Definite assignment

No breaking change to definite-assignment analysis is listed in S1/S2.

### Overload resolution

One change: **entry 8** (pointer types no longer require an unsafe context) can make previously-inapplicable candidates applicable, producing new **CS0121** ambiguities. Gated on the preview language version.

### Meaning of existing contextual keywords

Three identifiers change meaning:

| Identifier | Gate | Positions affected | Break |
|---|---|---|---|
| `union` | C# 15 (live at GA default) | type-declaration contexts; `union` followed by a type name | field declaration `union X;` reparses as a union declaration, giving CS9370 |
| `closed` | C# 15 (live at GA default) | type/alias **declaration** (hard error) and member-declaration contexts (modifier) | CS9380 on declaring a type/alias named `closed`; CS1519 on using it as a type name in a member declaration |
| `safe` | preview ("C# 16") | modifier position on member declarations | a type named `safe` is no longer resolved there |

All three are mitigated with `@` escaping. All three were pre-announced by CS8981 (warning wave 7).

Also relevant: `with` is **not** a new keyword, but `with(...)` as a collection-expression element changes meaning at LangVersion 15 (entry 7), mitigated with `@with`.

---

## Facts specifically worth carrying into a Roslyn-based rewriter's impact analysis

These are observations about the shape of the change, not about any particular consumer.

1. **Two entries change the parse tree for text that previously parsed successfully**: entry 6 (`(X.Y) when` in a switch arm: `CastExpressionSyntax` becomes `ConstantPatternSyntax` plus `WhenClauseSyntax`) and entry 12 (`union X;`: field declaration becomes union declaration). Entry 11 changes `closed X;` from a field declaration to an incomplete declaration.
2. **New declaration syntax node kinds** arrive with `union` (a `union_declaration` production distinct from `struct` and `record struct`) and with `unsafe(expression)` (a new `unsafe_expression` primary-expression form).
3. **New modifier tokens** in member and type declarations: `closed` (C# 15) and `safe` (preview).
4. **New compiler-synthesized attributes**: `System.Runtime.CompilerServices.MemorySafetyRulesAttribute` (module-level, value `15`) and `System.Runtime.CompilerServices.RequiresUnsafeAttribute` (member-level). Both are synthesized by the compiler when absent, and applying either explicitly in source is an error.
5. **New required well-known type for emit**: `System.Runtime.InteropServices.InAttribute`, now needed for synthesized `ref readonly` delegates and `ref readonly` local functions (entries 2 and 3). Any component that constructs a `CSharpCompilation` from a restricted reference set will hit CS0518.
6. **Ref-safety escape analysis for span-typed collection expressions is stricter** (entry 1), which changes which generated code is accepted, apparently independently of language version.
7. **`unsafe` will eventually stop propagating a lexical context** (S9 breaking-change list item 2): under the updated rules, `unsafe` on a member no longer introduces an unsafe context for its body. Generated code that relies on an enclosing `unsafe` member to license pointer operations inside the body will need explicit `unsafe` blocks or `unsafe(...)` expressions. This is currently preview-gated and requires assembly opt-in.
8. **`extern` members and explicit-layout fields will require an explicit `safe` or `unsafe` modifier** under the updated rules (S9 item 3).

---

## Open questions

1. **Does entry 8/9/10 ("C# 16") apply at the .NET 11 GA default language version?** S1/S2 say "C# 16" and `langversion:16`, but S5 shows no `CSharp16` enum member and `CurrentVersion == CSharp15`, while S9 says `MemorySafetyRulesAttribute` is filled with `15`. Meanwhile S8 (`unsafe-code-errors`) states CS9360-CS9363 apply "Under C# 15's updated memory safety rules" and "The C# 15 compiler tracks unsafe member usage at the call site", and S6 lists "Memory safety" as a C# 15 feature but says it "requires the `preview` language version". S6 (2026-08-19) and S8 are more recent than the "C# 16" text (commit `1284a4a`, 2026-08-11). Not resolvable from published sources; needs a check against a .NET 11 RC SDK.
2. **Is entry 1 (span collection-expression safe-context) language-version gated?** The document does not say. Roslyn commit `a5196d9` (2026-01-22) is titled "Use specific LangVersion in breaking change (#82099)", but which entry it applies to was not determined.
3. **Which exact diagnostic does entry 5 (`nameof(this.)` in attributes) produce?** The document names no ID, and it was not verified against `ErrorCode.cs`.
4. **Are entries 4 and 6 language-version gated?** Not stated in the document.
5. **Whether the .NET 11 SDK raises the default `WarningLevel`/`AnalysisLevel` to 11**, and whether any wave-11 diagnostic is added between now and GA. None exists today.
6. **Whether any further breaking change lands between VS 2026 18.10 and .NET 11 GA.** The list is a live document; the newest entries are dated 18.10 and the newest commit is 2026-08-11.
7. **`https://learn.microsoft.com/en-us/dotnet/core/compatibility/11.0` does not exist yet (HTTP 404)**, so runtime and SDK-side breaking changes for .NET 11 could not be cross-checked from the usual index.
8. **Direct verification of `ErrorCode.cs` failed.** An attempt to read `src/Compilers/CSharp/Portable/Errors/ErrorCode.cs` from `main` returned values inconsistent with the Learn compiler-message pages (it attributed 9363, 9370 and 9380 to unrelated C# 12 inline-array errors). The message texts recorded above come from S7 and S8, which are consistent with S1/S2 and with each other; the raw-source reading is treated as unreliable and was discarded.
