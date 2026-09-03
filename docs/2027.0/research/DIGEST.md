# .NET 11 / C# 15 / Roslyn 5.12 — Consolidated Digest

Consolidated 2026-09-03 from seventeen primary-source research notes. Target: **.NET 11 GA, 2026-11-10**
(Standard Term Support, two years, end of support 2028-11-09). Latest public milestone at consolidation
time: **.NET 11 Preview 7** (`11.0.100-preview.7.26381.103`, released 2026-08-11). No RC had shipped.

Identifiers have the form `N11-<AREA>-<nn>`. Areas: `LANG` (C# 15 language features), `ROSLYN` (Roslyn API
and grammar), `BREAK` (compiler breaking changes), `BCL` (.NET 11 runtime and base class library),
`ASYNC` (runtime async), `SDK` (SDK, MSBuild, NuGet), `IDE` (design-time hosts).
Resolved contradictions are numbered `RES-nn`; open questions `OQ-nn`.

## Version anchors (from `eng/Versions.props` and `eng/Version.Details.xml`)

| Component | Value at GA | Evidence |
|---|---|---|
| .NET SDK | `11.0.100` | `dotnet/sdk` `main`: `VersionMajor=11`, `VersionMinor=0`, `VersionSDKMinor=1` |
| Runtime / ref packs | `11.0.0` | `dotnet/core` release notes |
| Roslyn | **5.12** (assembly version `5.12.0.0`) | `dotnet/roslyn` `main` `MajorVersion=5 MinorVersion=12`; `dotnet/sdk` `release/11.0.1xx` pins `Microsoft.Net.Compilers.Toolset` `5.12.0-1.26451.112` |
| MSBuild | **18.12** (`AssemblyVersion` still frozen at `15.1.0.0`) | `dotnet/msbuild` `main` `VersionPrefix 18.12.0` |
| NuGet client | 7.12 *(inferred: NuGet minor tracks VS minor)* | `NuGet-7.9.md` plus the pattern |
| Visual Studio | **2027 = version 18.12** *(inferred)* | monthly 18.x cadence from 18.0 = Nov 2025; Roslyn `RazorVsixVersionPrefix 18.12.1` |
| C# | **15**, default for `net11.0` | `_MaxAvailableLangVersion` raised to `15.0` in Roslyn `main` `Microsoft.CSharp.Core.targets` |

Roslyn ↔ Visual Studio maps **5.N ↔ 18.N**, exact on every branch that exists. SDK feature bands map to VS
as 10.0.1xx/18.0, 10.0.2xx/18.3, 10.0.3xx/18.6, 10.0.4xx/18.9 — every third VS minor. Published Roslyn
NuGet versions follow the same cadence: 5.0.0, 5.3.0, 5.6.0, 5.9.0.

---

## 1. C# 15 language features

### The authoritative gate

`src/Compilers/CSharp/Portable/Errors/MessageID.cs`, `RequiredVersion(MessageID)`, is the single
authoritative statement of which features are C# 15 and which are preview-only:

```csharp
// C# preview features.
case MessageID.IDS_FeatureUnsafeEvolution:
    return LanguageVersion.Preview;

// C# 15.0 features.
case MessageID.IDS_FeatureCollectionExpressionArguments:
case MessageID.IDS_FeatureUnions:
case MessageID.IDS_FeatureStaticMembersInInterfaces:
case MessageID.IDS_FeatureClosedClasses:          // semantic check
case MessageID.IDS_FeatureLabeledBreakContinue:
case MessageID.IDS_FeatureExtensionIndexers:
    return LanguageVersion.CSharp15;
```

`LanguageVersion.CSharp15 = 1500`. `LanguageVersionFacts.CurrentVersion => CSharp15`;
`MapSpecifiedToEffectiveVersion(Latest | Default | LatestMajor) => CSharp15`;
`CSharpNext = LanguageVersion.Preview`. **There is no `LanguageVersion.CSharp16`**; the string `"16"` does
not parse. New error `ERR_FeatureNotAvailableInVersion15 = CS9399`.

MessageID values (`MessageBase + n`): `IDS_FeatureCollectionExpressionArguments` 12858,
`IDS_FeatureUnsafeEvolution` 12859, `IDS_FeatureUnions` 12860, `IDS_FeatureStaticMembersInInterfaces` 12861,
`IDS_FeatureClosedClasses` 12862, `IDS_FeatureExtensionIndexers` 12863,
`IDS_FeatureLabeledBreakContinue` 12864.

Sources: `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Errors/MessageID.cs`,
`.../LanguageVersion.cs`, <https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md>,
<https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15>.

---

### N11-LANG-01 — Collection expression arguments (`with(...)`)

**Grammar.** A third concrete collection element:

```antlr
collection_element : expression_element | spread_element | with_element ;
with_element       : 'with' argument_list ;
```

```csharp
string[] values = ["one", "two", "three"];
List<string> names = [with(capacity: values.Length * 2), .. values];
HashSet<string> set = [with(StringComparer.OrdinalIgnoreCase), "Hello", "HELLO", "hello"];

// List<T> candidates: List<T>(), List<T>(IEnumerable<T>), List<T>(int capacity)
List<int> l;
l = [with(capacity: 3), 1, 2]; // new List<int>(capacity: 3)
l = [with([1, 2]), 3];         // new List<int>(IEnumerable<int> collection)
l = [with(default)];           // error: ambiguous constructor
```

**Disambiguation.** If a collection element lexically starts with the token sequence `with` `(`, it is
**always** a `with_element`. To call a method literally named `with`, escape it: `@with(...)`.

**Placement.** A `with_element` must be the first element, else `CS9354`. Arguments may not be `dynamic`
(`CS9356`, LDM-2025-01-22). `__arglist` is specified as unsupported (LDM-2025-04-14) but the binder passes
`allowArglist: true`; unverified in practice.

**Conversion rule change.** For a struct/class implementing `System.Collections.IEnumerable` the C# 12
"has an applicable parameterless constructor" clause becomes: (a) no `with_element` and a parameterless
constructor accessible at the collection expression, **or** (b) a `with_element` and at least one
accessible constructor. Only the *presence* of the `with_element` affects convertibility; the arguments are
ignored for conversion and for type inference (LDM-2025-03-17), so
`Print([with(comparer: null), 1, 2, 3])` remains ambiguous between `Print<T>(List<T>)` and
`Print<T>(HashSet<T>)`. A type with no parameterless constructor is now target-typable from a collection
expression carrying `with(...)`, but still cannot be a `params` parameter type.

**Arrays and spans reject `with()` entirely, even empty** (LDM-2025-05-12):
`Span<int> a = [with(), 1, 2, 3];` and `int[] b = [with(length: 1), 3];` are errors.

**Interface target types.** `IEnumerable<E>` / `IReadOnlyCollection<E>` / `IReadOnlyList<E>` → `()` only;
`ICollection<E>` / `IList<E>` → `List<E>()` and `List<E>(int)`, the compiler constructing a `List<E>`.
`CS9357` covers a non-empty `with()` on a read-only interface. The dictionary-interface rows in the speclet
belong to **dictionary expressions** and are not in C# 15.

**`CollectionBuilderAttribute` relaxation.** A create method must now have the `ReadOnlySpan<E>` parameter
**last**, passed by value; parameters may precede it; **multiple create methods are supported**; overload
resolution runs over *projection methods* (the create method minus its last parameter) against the
`with(...)` argument list. The method must be named per the attribute, declared directly on the builder
type, `static`, accessible, and of matching arity; methods on base types or interfaces are ignored; the
builder type must be a non-generic class or struct. LDM-2025-03-12 put the span parameter last so it can be
`params`.

```csharp
[CollectionBuilder(typeof(MyBuilder), "Create")]
class MyCollection<T> { }
class MyBuilder
{
    public static MyCollection<T> Create<T>(ReadOnlySpan<T> elements);
    public static MyCollection<T> Create<T>(IEqualityComparer<T> comparer, ReadOnlySpan<T> elements);
}
MyCollection<string> c1 = [with(GetComparer()), "1", "2"];
// IEqualityComparer<string> _tmp1 = GetComparer();
// ReadOnlySpan<string> _tmp2 = ["1", "2"];
// c1 = MyBuilder.Create<string>(_tmp1, _tmp2);
```

**Evaluation order.** Elements left to right; within `with(...)` the arguments left to right; each element
and argument evaluated exactly once. Because `with(...)` is first, the arguments are evaluated **before**
the elements — an explicit design goal.

**Ref safety.** For a create method, the collection expression's safe-context is that of an invocation
whose arguments are the `with()` arguments followed by the collection expression itself as the final
`ReadOnlySpan<E>` argument. For a constructor call on a `ref struct` type, the safe-context is the
narrowest of `new C(a₁ … aₙ)` and the element expressions, and *method arguments must match* is applied as
if the whole thing were `new C(a₁ … aₙ) { e₁ … eₙ }` (expression elements as collection element
initializers, spread elements as if `C` had `Add(SpreadType spread)`).

**Parsing is unconditional.** `LanguageParser.ParseCollectionElement` has **no `LanguageVersion` check**; it
produces a `WithElementSyntax` for `with (` at the start of any collection element regardless of
`LangVersion`, and `Binder.BindCollectionExpression` reports the feature-availability diagnostic. See
OQ-02.

**Status at GA:** stable, `LanguageVersion.CSharp15`, shipped in .NET 11 Preview 1; Roslyn test plan closed
2026-01-21.

Sources: <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/collection-expression-arguments.md>,
<https://github.com/dotnet/csharplang/issues/8887>, <https://github.com/dotnet/roslyn/issues/80613>,
<https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15>.

---

### N11-LANG-02 — Union types (`union` contextual keyword)

**Grammar.**

```antlr
union_declaration
    : attributes? struct_modifier* 'partial'? 'union' identifier type_parameter_list?
      '(' case_types ')' struct_interfaces? type_parameter_constraints_clause*
      ('{' struct_member_declaration* '}' | ';')
    ;
case_types : type (',' type)* ;
```

```csharp
public record class Cat(string Name);
public record class Dog(string Name);
public record class Bird(string Name);
public union Pet(Cat, Dog, Bird);

public union OneOrMore<T>(T, IEnumerable<T>)
{
    public IEnumerable<T> AsEnumerable() => Value switch
    {
        IEnumerable<T> list => list,
        T value => [value],
    };
}

public record class None();
public record class Some<T>(T value);
public union Option<T>(None, Some<T>);

union S1(int, bool) : I1 { }        // interfaces allowed in the base list
partial union S1(int, bool) { }     // case-type list on exactly one part
partial union S1 { }
```

**What a union is.** A union of *types*, not a discriminated or tagged union. **The compiler does not
generate the case types**; they are pre-existing types named in the parameter list, and there are no
synthesised nested case classes. Case types may be classes, structs, interfaces, type parameters, nullable
types and other unions; overlapping and nested unions are allowed. Each case type must be implicitly
convertible to `object` by a reference or boxing conversion, else `CS9371`.

**Modifiers.** Accessibility, `partial`, `readonly`, `unsafe`, `safe`, and `new` when nested. **`ref` is
forbidden** (unlike a plain struct); being `TypeKind.Struct` it also forbids `closed`, `abstract`, `sealed`
and `static` (it is implicitly sealed). **`record union` is not supported** ("A union declaration is a
plain struct, not record struct"). Nested, generic and partial unions are supported, as are constraint
clauses. Mixed-kind partials give `CS0261`, whose message was updated to include "all unions".
`union` parses as a type declaration **only when `LangVersion >= 15`** (`IsEnabledRecordOrUnionKeyword`),
exactly like `record`, so the parse tree is language-version dependent.

**Body restrictions.** No instance fields, auto-properties or field-like events (`CS9373`); no explicitly
declared **public** single-parameter constructors (`CS9374`); an explicit constructor must chain through
`this(...)` to a synthesised or explicitly declared constructor (`CS9375`); at least one case type
(`CS9370`); a `union` declaration may not use a union member provider interface (`CS9387`). Preview 6
relaxed the constructor rule to permit a **non-public** single-parameter constructor (roslyn #83788).
Otherwise a union body accepts everything a struct body accepts. `__arglist` in the case-type list is
`CS1669`; anything before or after the bare type inside a case-type entry is `ERR_UnexpectedToken`.

**Lowering.**

```csharp
public union Pet(Cat, Dog) { ... }
// lowers to
[Union] public struct Pet : IUnion
{
    public Pet(Cat value) => Value = value;
    public Pet(Dog value) => Value = value;
    public object? Value { get; }
    ... // original body
}
```

Emitted IL: `sequential ansi sealed beforefieldinit`, `extends System.ValueType`, implements `IUnion`
**implicitly** (`get_Value` is `public final … newslot virtual`; listing `IUnion` explicitly in the base
list does not duplicate it); exactly one private `initonly object <Value>k__BackingField` carrying
`[CompilerGenerated]` and `[DebuggerBrowsable(Never)]`; one `public [CompilerGenerated]` constructor per
case type with **value-type cases boxed on entry**; a get-only `Value` property (no setter emitted).
`[UnionAttribute]` is synthesised onto the type unless the user already wrote it
(`ShouldApplyUnionAttribute() => IsUnionDeclaration && !HasUnionAttribute`). Constructor member offsets are
assigned in decreasing order so the emitted constructor order matches the case-type order.

**Adopting an existing type: the "basic union pattern".** Any class or struct carrying
`[System.Runtime.CompilerServices.UnionAttribute]` and following the pattern is a union type. Required,
all public: one or more **union creation members** (each public constructor with a single by-value or `in`
parameter; its parameter type is a case type), and a **`Value` property** `public object? Value { get; }`
(or `object`), optionally with an `init`/`set` of any accessibility that the compiler ignores. Optional
**non-boxing access pattern**: `public bool HasValue { get; }` (true iff `Value` is not null) and
`public bool TryGetValue(out TCase value)` per case type (the out-parameter type is the *underlying* type
when the case type is a nullable value type). Each is independently optional and neither is a fallback for
the other. Missing members give `CS9385` / `CS9386`.

**Union member provider.** If the union type *directly contains* a public nested interface literally named
`IUnionMembers` and implements it, union members are found **only** there, and creation members take the
form of public static `Create` methods with a single parameter and a return type identity-convertible to
the union type.

```csharp
public record class Result<T> : Result<T>.IUnionMembers
{
    object? _value;
    public interface IUnionMembers
    {
        public static Result<T> Create(T value) => new() { _value = value };
        public static Result<T> Create(Exception value) => new() { _value = value };
        public object? Value { get; }
    }
    object? IUnionMembers.Value => _value;
}
```

Unchecked well-formedness assumptions: *soundness*, *stability*, *creation equivalence*,
*access pattern consistency*. **A type parameter is never a union type, even when constrained to one.**
`IUnion<TUnion>` was proposed and then removed.

**Conversions.** There is an implicit *union conversion* to union type `U` from `E` when a standard
implicit conversion exists from `E` to some `C` that is the parameter type of a union creation member of
`U`; if `U` is a struct, also to `U?`. A union conversion is **not itself a standard implicit conversion**
and cannot chain into a user-defined conversion or another union conversion. There are no explicit union
conversions. It is executed by calling the creation member (`Pet pet = dog;` becomes
`Pet pet = new Pet(dog);`; `Result<string> r = "Hello";` becomes
`Result<string>.IUnionMembers.Create("Hello")`).

Priority against user-defined conversions: an implicit user-defined operator **beats** a union conversion;
under an explicit cast an explicit user-defined operator beats it; without a cast a union conversion beats
an inapplicable explicit user-defined operator. Ambiguity between two equally applicable case types is an
error. Nullable conversions are supported (`S1? x = someInt;`); **lifted union conversions are not**
(`static S1 Test1(int? x) => x;` is `CS0029`). Conversion from a base type or an interface type is allowed
even though a user-defined conversion from a base type is illegal. Expression trees may not contain a union
conversion (`CS9369`). Default parameter values cannot use one (`CS1750`); an explicit cast that resolves
to a union conversion does work (`s = (S)100;`).

**Exhaustiveness.** A `switch` expression over a union is exhaustive when it handles all case types; no
discard or `var` arm is required. If the null state of `Value` is "maybe null", an unhandled `null`
produces a warning even when the switch is otherwise exhaustive.

**Nullability.** The default null state of `Value` is "maybe null" if the default null state of **any** case
type is "maybe null", otherwise "not null". (The earlier rule keyed on "none of the case types are
nullable" was removed; analysis now uses the `Value` property's annotations.) Creating a union from a case
type gives `Value` the incoming value's null state. `HasValue` / `TryGetValue` narrow `Value` to "not null"
on the `true` branch. Post-condition attributes such as `[NotNull]` do not affect this.

**Status at GA:** stable, `LanguageVersion.CSharp15`. Compiler support first in Preview 2, IDE Preview 3,
declarations and patterns Preview 5, runtime support types in-box Preview 5/6. learn.microsoft.com
(ms.date 2026-08-14) cautions "Some features from the proposal specification aren't yet implemented"
without enumerating which. See OQ-05.

Sources: <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md>,
<https://github.com/dotnet/csharplang/issues/9662>, <https://github.com/dotnet/roslyn/issues/81074>,
<https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union>.

---

### N11-LANG-03 — Union pattern matching ("unwrapping")

**Semantics per the current spec (csharplang PR #10302, 2026-08-18).** When the pattern input is a union
type or a `Nullable<union struct>`, most patterns are applied to `Value`, not to the union value.

| Pattern | Unwraps to `Value`? | Notes |
|---|---|---|
| `var` (all forms) | **No** | captures the union itself |
| discard `_` | **No** | |
| list pattern | **No** | |
| property pattern without a type `{ … }` | **No** | applied to the union instance |
| positional pattern without a type `( … )` | **No** | applied to the union instance |
| type pattern `T` | **Yes** | equivalent to `{ Value: T }`; output is `Value` narrowed to `T` |
| declaration pattern `T x` | **Yes** | equivalent to `T and var x` |
| property pattern with a type `T { … }` | **Yes** | equivalent to `T and { … }` |
| positional pattern with a type `T ( … )` | **Yes** | equivalent to `T and ( … )` |
| constant pattern, non-null | **Yes** | `result is 1` ⇒ `result != null && result.Value is 1` |
| constant pattern `null` | **Yes**, special | see below |
| relational pattern | **Yes** | `result is > 1` ⇒ `result != null && result.Value is > 1` |
| `not` | **No** | applies to its input; output is its input |
| `and` / `or` | per-branch | in `and` the right pattern's input is the left pattern's *output*; in `or` both branches take the same input and the output is the input |

The `is`-type operator applied to a union has the same meaning as a type pattern.

**Pattern compatibility.** A union's `Value` is pattern compatible with `type` when at least one case type
is; otherwise `CS9372` `ERR_UnionMatchingWrongPattern`.

```csharp
union Pet(Cat, Dog);
Pet? p = new Cat(...);
p is Pet   // error: p.Value.Value is not pattern compatible with Pet
p is Cat   // true; output value is (Cat)p.Value.Value

record Cat(...) : ICat;
union Pet(Cat, Dog) : IPet;
Pet p = new Cat(...);
p is IPet ip   // error: p.Value is not pattern compatible with IPet
p is ICat c    // true; c is (ICat)p.Value

record Cat(string Name);
Pet p = new Cat(Name: "Fido");
p is { Name: "Fido" }     // error: Pet has no 'Name'; applied to p
p is { Value: Cat }       // true; applied to p
p is Pet { Value: Cat }   // error
p is Cat { Name: "Fido" } // true; applied to p.Value
p is {}                   // true; applied to p, always true for a struct union

union Pet(Cat, Dog) { public void Deconstruct(out object value) { value = this.Value; } }
p is ("Fido")     // false: applied to p
p is (Cat)        // true: applied to p
p is Pet (Cat)    // error
p is Cat ("Fido") // true; applied to p.Value
```

```csharp
GetPet() switch
{
    var pet and not null    => ..., // 'var pet' applies to Pet; 'not'/'null' to its Value
    not null and var value  => ..., // 'not' applies to Pet; 'var value' still captures Pet
    var pet and Dog         => ...,
    Dog and { Name: "Dog" } => ..., // 'Dog' changes the incoming value for the right branch
}
```

**Null matching.** Struct union: `s is null` ⇒ `s.Value == null`. Class union: `u is null` ⇒
`u == null || u.Value == null`. `Nullable<struct union>`: `n is null` ⇒
`!n.HasValue || n.GetValueOrDefault().Value == null`. The output value of the `null` constant pattern is
its input value.

**Codegen preference (guaranteed minimum).** (1) For a pattern implying a check for type `T`, if a
`TryGetValue(out S)` exists and there is an identity, implicit reference or implicit boxing conversion from
`T` to `S`, call it. Only those three conversion kinds are considered; a non-boxing conversion is preferred
and ties are broken in an implementation-defined manner; unsuitable, `Obsolete` or `Experimental` APIs are
silently ignored with no diagnostic. (2) Otherwise for a null check, use `HasValue` if present.
(3) Otherwise apply the pattern to `Value`.

**List patterns** originally always failed over unions because `Value` is `object`; the resolution is that
extension blocks can enable list-pattern scenarios for union types by adding the missing APIs for `object`.

**Status at GA: MOVING TARGET — see OQ-01 and RES-02.**

Sources: <https://github.com/dotnet/csharplang/pull/10302>,
<https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md>,
<https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/csharp.md>.
---

### N11-LANG-04 — Closed class hierarchies (`closed` contextual keyword)

**Syntax.** `closed` is a contextual keyword **modifier on classes only** (including `record class`). There
is **no new syntax node**; it appears in `TypeDeclarationSyntax.Modifiers`.

```csharp
// Assembly 1
public closed record class GateState;
public record class Closed : GateState;
public record class Open(float Percent) : GateState;

// Assembly 2
public record class Locked : GateState; // ERROR CS9382

string Describe(GateState state) => state switch
{
    Closed => "closed",
    Open(var percent) => $"{percent}% open",
    // No warning: every direct descendant is handled.
};
```

**Rules.**
- A `closed` class is implicitly `abstract`; it cannot combine with `sealed`, `static` or an explicit
  `abstract` (`CS9381`, `CS9384`). `DeclarationModifiers.Closed` is granted only for `TypeKind.Class`
  (and `Submission`), so `closed` on an interface, enum, delegate, struct, union, method, property, event
  or field is `CS0106`. Interfaces are listed as a possible future extension.
- Direct derivation outside the declaring **module** is `CS9382`. (`ITypeSymbol.IsClosed`'s XML doc says
  "outside its containing module"; the speclet says assembly *and* module.)
- **Derivation is not transitive**: a non-closed descendant of a closed class remains open. Mark
  intermediate descendants `closed` to extend exhaustiveness downward.
- **Type-parameter restriction**: if a generic class directly derives from a closed class, all of its type
  parameters must be used in the base class specification (`CS9383`). `class D1<U> : C<U>` ok;
  `class D2<V> : C<V[]>` ok; `class D3<W> : C<int>` error. Rationale: to guarantee a single generic
  instantiation of the derived type per instantiation of the closed base.
- **Exhaustiveness**: handling all direct descendants exhausts the closed class, so listing the closed base
  afterwards is an *unreachable case* error. Impossible generic instantiations need no case. A subtype that
  is inaccessible, constraint-violating or unspeakable at the use site **defeats** exhaustiveness (the
  switch warns). Subtype constraints are not analysed precisely. An empty switch over a closed class with
  no subtypes is **not** exhaustive (a deliberate quirk). A type parameter constrained to a closed class is
  treated like the closed class (added in Preview 7), whether the type parameter is on the method or on the
  containing type; listing the closed base as an extra arm is then a subsumption error. A switch over
  `JobStatus?` must also handle `null`.
- **Interface convertibility restriction**: if a closed class has a *sealed hierarchy* (every class in the
  expanded hierarchy is sealed or itself closed), an explicit reference conversion to an interface that no
  member of the hierarchy implements is an error, mirroring the existing rule for sealed classes.
- The speclet notes the source-compatibility hazard: "It can be a breaking change to add a `closed`
  modifier to an existing class, or to add an additional derived class from a closed class."

**Subtype determination.** For a closed type `C` with original definition `C₀`, each subtype declaration
`S₀` whose base type has original definition `C₀` is examined for a construction `S` whose base type is
`C`; if one exists, `S` is in the set of subtypes. On the source side Roslyn implements this as a
**whole-source-module walk of the global namespace**, cached in `_lazyClosedSubtypeCandidates`. On the
metadata side `PENamedTypeSymbol.CandidateClosedSubtypeDefinitions` scans **every `TypeDefinition` row of
the module**.

**Metadata.** The type is emitted `abstract` and carries
`[System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = { … })]`; **every** constructor
carries `[CompilerFeatureRequired("ClosedClasses")]`. Unlike required members, **no companion
`ObsoleteAttribute` is emitted**. Multiple `CompilerFeatureRequired` attributes stack:

```csharp
class C1
{
    [Obsolete("Types with required members are not supported in this version of your compiler")]
    [CompilerFeatureRequired("RequiredMembers")]
    [CompilerFeatureRequired("ClosedClasses")]
    public C1() { }
}
```

A missing `CompilerFeatureRequiredAttribute` is `CS0656`. A down-level compiler reports
`'C1.C1()' requires compiler feature "ClosedClasses"`.
`CompilerFeatureRequiredFeatures.ClosedClasses = 1 << 3`, metadata string `"ClosedClasses"`.

Derived types are recorded as **unbound generic definitions or the type itself**
(`GetUnboundGenericTypeOrSelf()`), and only **direct** derived types are recorded. The named argument is
written even when the array is empty. Observed shapes from `ClosedClassesTests`:
`{typeof(D1), typeof(D2)}`, `{typeof(D1), typeof(D2), typeof(D3<>), typeof(D4<>), typeof(D5<,>)}`,
`{typeof(Container<>.D1<>), typeof(Container<>.D2)}`. A hand-rolled or polyfilled `IsClosedTypeAttribute`
whose `DerivedTypes` property has the wrong shape gives `CS9395`; if the property is absent entirely, no
diagnostic is reported and the attribute is emitted with its parameterless constructor only.

**`closed` as an identifier**: `CS9380` "Types and aliases cannot be named 'closed'." Use `@closed`.

**Status at GA:** stable, `LanguageVersion.CSharp15`. Shipped in .NET 11 Preview 5 (roslyn #83120, #83736);
type-parameter exhaustiveness and the metadata format stabilised in Preview 7 (roslyn #83979, #84350).

Sources: <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/closed-hierarchies.md>,
<https://github.com/dotnet/csharplang/issues/9499>, <https://github.com/dotnet/roslyn/issues/81039>,
<https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/closed>,
<https://github.com/dotnet/runtime/issues/129009>.

---

### N11-LANG-05 — Extension indexers

**Grammar.** One production is added to the C# 14 extensions grammar; nothing else.

```antlr
extension_member_declaration
        : method_declaration
        | property_declaration
        | indexer_declaration   // new in C# 15
        | operator_declaration
        ;
```

```csharp
public static class SequenceIndexer
{
    extension(IEnumerable<int> sequence)
    {
        public int this[int index] => sequence.ElementAt(index);
    }
}
IEnumerable<int> numbers = Enumerable.Range(1, 10);
int third = numbers[2];

public static class ReadOnlyListExtensions
{
    extension<T>(IReadOnlyList<T> list)
    {
        public T this[Index index] => list[index.GetOffset(list.Count)];
    }
}
```

**Rules.**
- Indexers are always instance members, so an extension block declaring an indexer **must name its receiver
  parameter**. `extension(int)` without a name cannot declare one. Extension members have no implicit or
  explicit `this`; the receiver is reached through the named receiver parameter.
- Full ordinary-indexer feature set: accessor bodies, expression bodies, ref-returning accessors, `scoped`
  parameters, attributes, default parameter values, `params`.
- Prohibited modifiers (inherited from extension members generally): `abstract`, `virtual`, `override`,
  `new`, `sealed`, `partial`, `protected` and the other accessibility modifiers, and `init` accessors; the
  Roslyn test plan also lists `static`.
- The extension inferrability rule holds: all type parameters of the extension block must be used in the
  combined set of extension and member parameters.
- `IndexerNameAttribute` may be applied. It is **not emitted**, but it affects member-conflict checking,
  the metadata names of the property and accessors, and the emitted `DefaultMemberAttribute`.
- **Binding order for `E[A]`** (LDM 2026-03-09): (1) real instance indexers declared or inherited on the
  receiver type; (2) implicit instance indexers (`Index`/`Range` via `Length`/`Count` plus `this[int]` or
  `Slice(int,int)`); (3) real extension indexers; (4) extension implicit indexers. The scope walk is the
  extension-method one (current and enclosing lexical scopes, `using` namespaces, `using static`). Per
  scope, extension blocks in non-generic static classes contribute their indexers; inaccessible and
  inapplicable candidates are removed; an empty set moves to the next scope; a tie is a compile-time
  ambiguity. The winning access is processed as a **static invocation of the accessor's implementation
  method** with the receiver as the first argument and generic arguments inferred during the applicability
  check. Type inference uses only the receiver and the argument list; **the assigned value does not
  contribute** (LDM 2026-02-02).
- Extension members are **never** considered when the receiver is a `base_access`; and because an
  *element_access* is processed as an indexer access only when the receiver is a variable or value,
  extension indexers are never considered when the receiver is a type.
- **Never applicable to arrays or `string`** (LDM 2026-04-07), neither real nor implicit; declaring such an
  extension indexer is permitted but it can never bind.
- Null-conditional element access, null-conditional assignment, index assignment in object initializers,
  list patterns and spread elements all participate. Pointer element access is unaffected (an extension
  parameter may not be a pointer type). Dynamic arguments are disallowed (an element access with a
  `dynamic` argument is handled by the element-access clause and never becomes an indexer access).
  **Extension indexers cannot be captured in expression trees.**
- Consuming an extension indexer imported from another module also requires `LangVersion >= 15`
  (`Binder.ReportDisallowedExtensionBlockIndexer` fires at consumption).

**Knock-on behaviour changes beyond indexers** (these change existing code at `LangVersion 15`):
- **Extension `Length`/`Count` properties now make a type countable** and contribute to the implicit-indexer
  fallback (LDM 2026-02-02: "extensions should contribute everywhere"). Lookup proceeds scope by scope
  (instance first, then extension scopes), and within each scope `Length` is looked for before `Count`
  (LDM 2026-03-09).
- List patterns resolve `Length`/`Count`, the `Index` indexer and the `Range` indexer independently, in
  this order for the indexers: (a) instance-only real indexer; (b) instance-only implicit-indexer parts;
  (c) full lookup (instance plus extension) real indexer; (d) full lookup implicit-indexer parts, each in
  an individual lookup.
- **A classic `this`-parameter extension `Slice` method also contributes** to implicit `Range` indexer
  binding: "we're treating classic and new extension methods exactly the same" (LDM 2026-03-09).
- Extension `Length` does **not** contribute to spread-element size optimisation.

**Metadata.** For each CLR-level extension grouping type containing at least one indexer the compiler emits:
an extension property named `Item` (or the `IndexerName` value) whose accessors
`throw new NotImplementedException()`, carrying `[ExtensionMarkerName("<M>$…")]`; implementation methods
`get_Item`/`set_Item` on the **enclosing static class**, `static`, with the receiver parameter prepended and
holding the user-written bodies; and `[DefaultMemberAttribute]` on the grouping type with `MemberName`
equal to the indexer's metadata name. `params` on a setter emits `[ParamArray]` on the setter
implementation method (LDM 2026-02-02, option 3).

```csharp
[Extension]
static class BitExtensions
{
    [Extension, SpecialName, DefaultMember("Item")]
    public sealed class <G>$T0                      // grouping type
    {
        [SpecialName]
        public static class <M>$T_t                 // marker type
        {
            [SpecialName] public static void <Extension>$(T t) { }   // marker method
        }

        [ExtensionMarkerName("<M>$T_t")]
        public bool this[int index]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }

    public static bool get_Item<T>(T t, int index) => ...;
    public static void set_Item<T>(T t, int index, bool value) => ...;
}
```

The real attribute type is `System.Runtime.CompilerServices.ExtensionMarkerAttribute(string name)` (the
speclet spells it `[ExtensionMarkerName]`); it is embeddable via
`SynthesizedEmbeddedExtensionMarkerNameAttributeSymbol` and is **filtered out of `GetAttributes()`** for
extension-block members.

**Symbol API: no new public API.** An extension indexer is an `IPropertySymbol` with `IsIndexer == true`
and `Name == "this[]"`, declared on the extension grouping type (`INamedTypeSymbol.IsExtension == true`);
its accessors' implementation methods are reached through `IMethodSymbol.AssociatedExtensionImplementation`.
Reduction uses `IPropertySymbol.ReduceExtensionMember(ITypeSymbol receiverType)` (added in Roslyn 5.3).

**CREF syntax** — see N11-ROSLYN-19.

**Status at GA:** stable, `LanguageVersion.CSharp15`. Shipped in .NET 11 Preview 6 (roslyn #81607). The
Roslyn test plan (#81505) is still open with unchecked items: no implicit `this` accessor in the body,
base-receiver rejection, type-receiver rejection, analyzer actions, SemanticModel `GetSymbolInfo` /
`LookupSymbols` / `GetMemberGroup`, VB interop, unsafe-evolution interaction (extension indexers marked
`RequiresUnsafe`), **"Check that EnC is blocked"**, diagnostic quality and IDE features. `IOperation`,
flow graph, nullable analysis, ref-safety analysis, metadata production and consumption, symbol display and
CREF are marked done. See OQ-07.

Sources: <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/extension-indexers.md>,
<https://github.com/dotnet/csharplang/issues/9856>, <https://github.com/dotnet/roslyn/issues/81505>.

---

### N11-LANG-06 — Labeled `break` and `continue`

**Grammar.**

```antlr
break_statement    : 'break' identifier? ';' ;
continue_statement : 'continue' identifier? ';' ;
```

Roslyn additionally models attribute lists: `attribute_list* 'break' identifier_name? ';'`.

```csharp
outer: for (int row = 0; row < grid.Height; row++)
{
    for (int column = 0; column < grid.Width; column++)
    {
        if (grid[row, column].IsBlocked) { continue outer; }
        if (grid[row, column].IsGoal)    { break outer; }
    }
}
```

**Rules.**
- The label is an ordinary `labeled_statement` placed directly on the target. Only the statement
  **immediately** nested within the `labeled_statement` is "labeled with" that identifier. In
  `a: b: while (…) …` only `b` labels the loop; `break a;` or `continue a;` inside the body does not target
  the `while`. Roslyn realises this as
  `loopSyntax.Parent is LabeledStatementSyntax labeled ? labeled.Identifier.ValueText : null` in
  `LoopBinder`.
- Targets for `break`: `switch`, `while`, `do`, `for`, `foreach` (including `await foreach`).
  Targets for `continue`: iteration statements only — **`continue` can never target a `switch`**.
- Unlabeled `break`/`continue` keep their existing meaning (innermost applicable statement).
- The `finally`-block restriction is unchanged: a `break`/`continue` inside a `finally` block must target
  a statement within the same `finally` block. Intervening `try`/`finally` blocks run in the usual order
  before control transfers.
- **Parsing is unconditional** (no `LanguageVersion` check in `ParseBreakStatement` /
  `ParseContinueStatement`); the binder reports the feature-availability diagnostic. This is not a
  compatibility break, because `break x;` was never valid C# before.

**Lowering: none.** Binding resolves the label to the `GeneratedLabelSymbol` the targeted loop or switch
already owns, so `break outer;` emits exactly the branch a plain `break;` in that loop would emit. There is
no new `IOperation` node and no control-flow-graph shape change; a labeled `break`/`continue` is still an
`IBranchOperation` with `BranchKind.Break`/`Continue` whose `Target` is the loop's label. `BoundBreakStatement`
and `BoundContinueStatement` gained a `Label` (`BoundLabel?`) member alongside the target `LabelSymbol`;
that is what backs `GetSymbolInfo` on the `Name` node, returning an `ILabelSymbol`.

**Diagnostics.** CS9393 `ERR_NoBreakId` "No enclosing loop or switch statement with the label '{0}' out of
which to break"; CS9394 `ERR_NoContinueId` "No enclosing loop with the label '{0}' out of which to
continue". Unlabeled cases keep `ERR_NoBreakOrCont` (CS0139).

**IDE.** New style rule **IDE0410** "Use labeled jump statement", category Style, subcategory language
rules (code-block preferences), option `csharp_style_prefer_labeled_jump_statements`, values `true`
(default) / `false`, applicable to C# 15 and later. It detects (1) a `goto` jumping to a label immediately
after a nested loop → `break <label>`; (2) a `goto` jumping to an empty label at the end of a loop body →
`continue <label>`; (3) a Boolean flag set in an inner loop and checked at each outer level → a single
labeled jump.

**Status at GA:** stable, `LanguageVersion.CSharp15`. Shipped in .NET 11 Preview 7 (roslyn PR #84271,
merged 2026-06-25, commit `cb96af31028870b9647fab2883e8604e910be0b0`). Only "Public API review"
(roslyn #83266) remains unchecked. Two spec open questions are marked resolved in the test plan:
whether `break label;` naming a non-loop statement fails at identifier lookup or at label validation, and
whether nested labels should be supported (recommendation and implementation: no).

Sources: <https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/labeled-break-continue.md>,
<https://github.com/dotnet/csharplang/issues/9875>, <https://github.com/dotnet/roslyn/pull/84271>,
<https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0410>.

---

### N11-LANG-07 — Non-virtual static members in interfaces without DIM runtime support

The compiler now allows declaring and consuming **static, non-virtual** interface members even when the
target runtime does not support Default Interface Implementations. Previously such declarations were gated
on runtime DIM capability, which blocked them on .NET Framework and `netstandard2.0` targets.

A runtime-capability check remains for accessibility: the desktop runtime does not support `protected`
access even for static interface members (it throws at run time), so `protected` or `protected internal`
accessibility on an interface member reports
`ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember`.

**Status at GA:** `LanguageVersion.CSharp15` per `MessageID.cs`. Tracked only by Roslyn PR
<https://github.com/dotnet/roslyn/pull/83097> (merged 2026-04-10 into `features/Unions`, developer
AlekseyTs). **There is no csharplang proposal document, and the feature is absent from the "What's new in
C# 15" learn.microsoft.com page.** See OQ-06.

---

### N11-LANG-08 — Memory-safety evolution: pointer relaxations (PREVIEW ONLY)

**Status at GA: NOT active at `LangVersion 15` / `latest` / default.** `IDS_FeatureUnsafeEvolution` maps to
`LanguageVersion.Preview`, listed under the comment `// C# preview features.` Requires
`<LangVersion>preview</LangVersion>` **and** `AllowUnsafeBlocks`. Gated on LangVersion only, **not** on the
assembly opt-in: "anything you are required to do when you are opted in, you are allowed to do before you
opt in."

`unsafe` is redefined from "locations where pointer types are used" to "locations where memory unmanaged by
the runtime is dereferenced". A member marked `unsafe` becomes *requires-unsafe*: the audit obligation
flows to the caller.

**No longer requiring an `unsafe` context:**
1. Pointer type declaration in any position (`int*`, `int**`, `void*`, `int*[]`, `delegate*<...>`) —
   locals, fields, parameters, return types, type arguments where legal. C# spec §24.3 moves into §8.
2. Address-of `&` on a variable, including `&method` producing a function pointer (§24.6.5).
3. Pointer conversions (§24.5): implicit to `void*`; explicit pointer↔pointer and pointer↔integral. Move
   into §10.
4. All other pointer expressions (§24.6) except the three listed below: pointer arithmetic (`p + n`,
   `p - n`, `p - q`), `++`/`--` on pointers, pointer comparison.
5. The `fixed` statement (§24.7) — pinning is not itself a memory access. Moves to §13.
6. Fixed and moveable variables (§24.4).
7. Declaring a fixed-size buffer (`fixed char name[30];`, §24.8.2). Moves to §16.3. Reading the field is
   also safe; only `element_access` on it remains unsafe.
8. `stackalloc` converted to a pointer — *always* safe now, in every context.
9. `sizeof` on any unmanaged type (safe **regardless of opt-in**).
10. `await` inside an `unsafe` context (previously an error). **`await` remains disallowed inside a `fixed`
    statement**: new `ERR_BadAwaitInFixed` (CS9398). The pre-existing `ERR_AwaitInUnsafeContext` (CS4004)
    is commented out, "replaced with a langversion error". Standard text change: "It is a compile-time
    error for an unsafe context to contain ~~an `await` expression or~~ a `yield return` statement." plus
    "It is a compile-time error for a `fixed` statement to contain an `await` expression."

**Still requiring an `unsafe` context:** pointer indirection `*p` (§24.6.2); pointer member access
`p->member` (§24.6.3); pointer element access `p[i]` (§24.6.4); function-pointer invocation; element access
on a fixed-size buffer; the tightened `stackalloc` rule (opt-in only, N11-LANG-11); and any expression or
statement that uses a *requires-unsafe* member, **except inside `nameof(...)`** (Preview 7, roslyn #84325;
`Binder.ReportDiagnosticsIfUnsafeMemberAccess` begins with `if (IsInsideNameof) { return; }`).

```csharp
int number = 42;
int* pointer = &number;
int[] numbers = [10, 20, 30];
fixed (int* first = numbers) { /* dereferencing still requires an unsafe context */ }
```

`stackalloc` of a managed type remains an error; C# 11's warning for pointers to managed types is unchanged
(whether to relax it for address-of is an open spec question). The compiler does not attempt to prove that
a pointer kept live across an `await` is still valid after resumption.

Sources: <https://github.com/dotnet/csharplang/blob/main/proposals/unsafe-evolution.md>,
<https://github.com/dotnet/csharplang/issues/9704>, <https://github.com/dotnet/roslyn/issues/81207>,
<https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code>.

---

### N11-LANG-09 — `unsafe(expression)` (PREVIEW ONLY)

```antlr
unsafe_expression : 'unsafe' '(' expression ')' ;
```

An `unsafe_expression` is a *primary_no_array_creation_expression* establishing an `unsafe` context for a
single expression. **The context does not extend beyond the closing parenthesis.** Its type and value are
those of the enclosed expression. Motivating positions are those where an `unsafe` block cannot appear
syntactically:

```csharp
class Header
{
    static readonly int Signature = unsafe(ReadSignature());   // field initializer
    static unsafe int ReadSignature()
    {
        int rawValue = 0x1234;
        int* pointer = &rawValue;
        return *pointer;
    }
}
C() : this(unsafe(GetUnsafeValue())) { }                        // constructor initializer
catch (Exception e) when (unsafe(NowUnsafeCall(e))) { }         // catch filter
await unsafe(DoWork());                                          // keeps 'await' outside
Console.WriteLine(unsafe(Add(1, 2)));                            // narrows the unsafe region
int b = unsafe(c[null]);                                         // the documented CS9363 fix
```

Requires `AllowUnsafeBlocks` (else `CS0227`) and `LangVersion=preview`. **Not** conditional on the assembly
opt-in. Answered by LDM 2026-05-27.

**Status at GA: preview only**, `RSEXPERIMENTAL006`.

---

### N11-LANG-10 — The `safe` contextual keyword (PREVIEW ONLY)

`safe` may be applied wherever `unsafe` can mark a declaration *requires-unsafe* (LDM 2026-07-22); where it
is not required it is a no-op. It marks the declaration as **not** *requires-unsafe*. It does **not**
introduce a safe context; there is no `safe` block and no `safe` expression form. `safe` + `unsafe` on the
same declaration is `CS9388`. On a local function, `safe` says calling it needs no unsafe context; it does
not make the body a safe context, and a local function declared inside an `unsafe` context stays in it.
`DeclarationModifiers.Safe = 1 << 26` (`Unsafe = 1 << 15`). Driven by `LibraryImport` source generation
(roslyn #84555): whether the generated partial implementation is `extern` is an implementation detail of
the generator, so the user-authored partial declaration must be able to carry `safe` regardless. Cases that
need an explicit modifier where the language does not require one will need an analyzer. The spelling is
still provisional (LDM 2026-04-13 called it "a temporary spelling"; 2026-05-13 reaffirmed but kept it open).

Under the updated rules an explicit `safe` or `unsafe` modifier is **required** on `extern` members
(`CS9389`, enforced in `SourceMemberMethodSymbol`, `SourceEventSymbol`, `SourcePropertySymbolBase`,
`LocalFunctionSymbol`) and on instance fields of `[StructLayout(LayoutKind.Explicit)]` / `[ExtendedLayout]`
types (`CS9392`; if the field is hidden behind an auto-property or field-like event, the requirement moves
to that member).

```csharp
[LibraryImport("libc")]
internal static safe partial int getpid();

[LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
internal static unsafe partial nint strlen(byte* str);
```

Rationale for `extern`: "the calling convention used for the method could be incorrectly specified by the
user and must be manually verified by review." `extern` is the only place where `RequiresUnsafeAttribute`
is synthesised without an explicit `unsafe` keyword, and `extern` members from *legacy-rules* assemblies
are **not** treated as implicitly `unsafe`, because `extern` is an implementation detail not guaranteed to
be preserved in reference assemblies.

**Status at GA: preview only**, `SyntaxKind.SafeKeyword = 8454`, `RSEXPERIMENTAL006`.
---

### N11-LANG-11 — The *requires-unsafe* member model (PREVIEW + ASSEMBLY OPT-IN)

**Two independent switches are needed for the full model:**

```xml
<PropertyGroup>
  <LangVersion>preview</LangVersion>
  <Features>$(Features);updated-memory-safety-rules</Features>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

`Feature.UpdatedMemorySafetyRules = "updated-memory-safety-rules"`. Equivalently,
`CSharpCompilationOptions.WithMemorySafetyRulesVersion(MemorySafetyRulesVersion.Version2)` through the API.
**There is no `/memorysafetyrules:` csc switch and no `<MemorySafetyRules>` MSBuild property in .NET 11**
(confirmed absent from `CSharpCommandLineParser.cs` and `Microsoft.CSharp.Core.targets`); those are planned
for .NET 12/13. For file-based programs:
`#:property Features=$(Features);updated-memory-safety-rules` and `#:property LangVersion=preview`.
Opting in without `LangVersion=preview` gives `ERR_CompilationOptionNotAvailable` naming
`MemorySafetyRulesVersion`.

`AllowUnsafeBlocks` is **orthogonal** to the rules version. It gates every appearance of the `unsafe`
keyword and `SkipLocalsInitAttribute`. The design intent is that a project can opt into the updated rules
*without* `AllowUnsafeBlocks`, so it gets errors for calling `Unsafe.As`, `Marshal.*` and similar:

| Opt-in | `AllowUnsafeBlocks` | Result |
|---|---|---|
| On | Off (default) | safest: updated model, no unsafe code allowed |
| On | On | updated model, unsafe code allowed |
| Off | Off | original model, no pointer types |
| Off | On | original model, pointer types allowed |

**Terminology.** A member is *requires-unsafe* if, under the **updated** rules, it is marked `unsafe`; or,
under the **legacy** rules, it contains pointers in its signature (compat mode). Roslyn models this as
`internal enum CallerUnsafeMode { None, Implicit, Explicit }`.

**Model changes under opt-in:**
- `unsafe` on a member marks it *requires-unsafe*: every call site must be in an `unsafe` context.
- `unsafe` on a member or type **no longer introduces an `unsafe` context** in its body or initializers
  (LDM 2026-04-22); explicit `unsafe` blocks or `unsafe(...)` expressions are required inside.
- `unsafe` is an **error** (`CS9377` `ERR_UnsafeMeaningless`) on type declarations (`class`, `struct`,
  `interface`, `record`, `enum`), static constructors, destructors/finalizers and `delegate` declarations.
- `unsafe` on a **constructor** does introduce an `unsafe` context inside its **initializer**, so an
  `unsafe` constructor may call a *requires-unsafe* `base`/`this` constructor.
- A type whose parameterless constructor is *requires-unsafe* does not satisfy the `new()`/`struct`
  constraint in *declaration* positions at all, and satisfies it in *expression* positions only inside an
  `unsafe` context (`CS9376`).
- `unsafe`/`safe` are **not** inherited by nested lambdas or local functions; lambdas cannot be marked
  *requires-unsafe* at all. A local function must be marked explicitly to become *requires-unsafe*; a local
  function inside an `unsafe` block is in an unsafe context but is not itself *requires-unsafe*.
- `partial` members: both parts must agree on `unsafe` (`CS0764`) and on `safe` (`CS9390`).
- Property/indexer accessors may carry `unsafe`/`safe` independently and inherit the property's when
  unmarked; restrictions mirror `readonly` — not on both the property and its accessors (`CS9396`), and not
  the same modifier on *all* accessors (`CS9397`, put it on the property instead). Event `add`/`remove`
  accessors cannot carry modifiers; only the whole event can.
- `unsafe` on a **field** makes it *requires-unsafe* (every read and write needs an unsafe context) and does
  not make its initializer an unsafe context. Marking a property or field-like event `unsafe` does **not**
  make its backing field *requires-unsafe* (LDM 2026-05-13).
- Adding `unsafe` in an override or interface implementation of a safe member is an error
  (`CS9364`/`CS9365`/`CS9366`).
- Converting a *requires-unsafe* member to a delegate outside an `unsafe` context is an error; delegate
  types and lambda function types cannot be *requires-unsafe*.
- Indirect uses count: `foreach` (`GetEnumerator`/`Current`/`MoveNext`), `using` (`Dispose`),
  deconstruction (`Deconstruct`), `lock`, interpolated string handlers, interceptors, patterns, object
  initializers, `with` expressions, operators and extension operators, attribute application, object
  creation and `new()` constraint satisfaction.

**The only tightening: `stackalloc`.** A `stackalloc` expression is unsafe when **all** hold: it is being
converted to `Span<T>` or `ReadOnlySpan<T>`; it has no *stackalloc_initializer*; and it occurs within a
member carrying `SkipLocalsInitAttribute`. Diagnostic `CS9361`. Rationale: the stack space has unknown
contents and is being wrapped in a type that promises safe access. Because this is a tightening rather than
a relaxation, it applies **only under opt-in** (made opt-in-only in Preview 5, roslyn #83639); LDM has not
formally confirmed it (roslyn #82546). Also new: `stackalloc` pointers do not survive an `await` — before
this feature, pointers were disallowed in `async` methods, so this was not observable.

**Compat mode.** For modules that have **not** opted in, a member is treated as *requires-unsafe* if a
pointer or function-pointer type appears anywhere in its parameter or return types, including nested
(`int*[]`). Excluded: pointers in **constraint types** (`where T : I<int*[]>`); **substituted generic
parameters** (`I<T>.M(T)` with `T = int*[]`); `nint` / `System.IntPtr` (LDM 2026-04-29 declined to extend);
`extern`/`DllImport` from non-opted-in callees. There is no blanket warning when an opted-in assembly
references a non-opted-in one. **Compat mode applies even to callers that have not opted in** (Preview 7,
roslyn #83660, fixing #81967) — closing a window in which merely bumping `LangVersion` would make code
*less* protected.

| Caller | Callee | Behaviour |
|---|---|---|
| Updated | Updated | the callee's `unsafe` markers travel through metadata; each call to a *requires-unsafe* member needs an enclosing `unsafe` context |
| Updated | Legacy | compat mode: any callee member with a pointer type in its signature is *requires-unsafe* |
| Legacy | Updated | original pointer rules; a *requires-unsafe* member with **no** pointer in its signature becomes callable from safe code, because the legacy caller cannot read the new markers (LDM 2026-04-29) |
| Legacy | Legacy | compat mode still applies for pointer-in-signature members — this is the new CS9363 |

**Documentation conventions the feature recommends** (neither formalised nor enforced): a `<safety>` XML
documentation tag on *requires-unsafe* members stating the caller's contract, and `// SAFETY:` comments
inside each `unsafe` block (modelled on Rust). LDM 2026-05-27 reached no decision on compiler checking.

**Decided, for the record:** errors not warnings when the new rules are on (LDM 2025-11-05); no
source-generator exemption (2025-11-05); the keyword rather than an attribute marks *requires-unsafe*
(2025-11-12, reversed 2026-01-26 to an attribute, re-reversed 2026-04-06 back to the keyword); `unsafe` on
a type is an error not a warning (2026-05-13); no nullable-style region-based opt-in (2026-04-29); a
"middle" warning-level opt-in in principle but not blocking for preview (2026-04-29); members with `unsafe`
blocks or pointers in their signature do not need an explicit `safe` marker (2026-04-13); no Visual Basic
support is needed.

**Status at GA: preview only.** learn.microsoft.com states plainly: "the *requires-unsafe* member model and
the assembly opt-in to the updated memory safety rules aren't available yet, so `safe` and `unsafe`
currently have no effect on callers" — meaning through *supported* project properties. The Roslyn
implementation exists and is reachable through the `Features` escape hatch or the compilation-options API.
The dotnet/designs SDK document says .NET 12/13 will expose `<MemorySafetyRules>2</MemorySafetyRules>`,
first as opt-in, aspiring to on-by-default-with-opt-out eventually, plus a possible
`<MemorySafetySeverity>` to downgrade the errors during migration, and `#:property MemorySafetyRules=1`
opt-out for file-based programs. Roslyn API tracking: <https://github.com/dotnet/roslyn/issues/82791>.

Sources: <https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/sdk-memory-safety-enforcement.md>,
<https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/caller-unsafe.md>,
<https://devblogs.microsoft.com/dotnet/improving-csharp-memory-safety/>,
<https://github.com/dotnet/roslyn/pull/82547>.

---

### N11-LANG-12 — Memory-safety metadata attributes

```csharp
namespace System.Runtime.CompilerServices;
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Module, Inherited = false, AllowMultiple = false)]
public sealed class MemorySafetyRulesAttribute : Attribute
{
    public MemorySafetyRulesAttribute(int version) => Version = version;
    public int Version { get; }
}

namespace System.Diagnostics.CodeAnalysis;   // NOTE the namespace — see RES-03
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Method
              | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class RequiresUnsafeAttribute : Attribute { public RequiresUnsafeAttribute() { } }
```

**RES-03 (namespace).** The csharplang speclet places `RequiresUnsafeAttribute` in
`System.Runtime.CompilerServices`. The shipped runtime source
(`src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/RequiresUnsafeAttribute.cs`), the
`System.Runtime` reference assembly (line ~9184), and Roslyn's `WellKnownTypes.cs`
(`System_Diagnostics_CodeAnalysis_RequiresUnsafeAttribute`) and
`AttributeDescription.RequiresUnsafeAttribute` all say **`System.Diagnostics.CodeAnalysis`**. The
implementation wins; the speclet is stale.

**RES-04 (version value).** The speclet prose says the module attribute is "filled in with `15` as the
language version". The implementation emits **`2`**: `MemorySafetyRulesVersion { Version1 = 1,
Version2 = 2 }`, and `SourceModuleSymbol.AddSynthesizedAttributes` emits
`MemorySafetyRulesAttribute((int)MemorySafetyRulesVersion)` only when the version is not `Version1`. The
speclet still lists "`2`? `15`? `11`?" as an open question, so this may change before GA (OQ-08). For
contrast, the pre-existing `RefSafetyRulesAttribute` is emitted with `11`.

Both types ship in `System.Runtime` for .NET 11 and are **synthesised by the compiler when missing**
(standard well-known-member behaviour; `SynthesizedEmbeddedMemorySafetyRulesAttributeSymbol`,
`EmbeddableAttributes`), so **no polyfill is needed on any target framework**. **Applying either explicitly
in source is an error** (`CS9379` for `RequiresUnsafeAttribute`; a warning rather than an error under
legacy rules). `RequiresUnsafeAttribute` is **filtered out of `ISymbol.GetAttributes()`** on
`PEMethodSymbol`, `PEPropertySymbol`, `PEEventSymbol` and `PEFieldSymbol`; the information is surfaced as
`ISymbol.RequiresUnsafeContext`. The compiler **ignores** `RequiresUnsafeAttribute` on members read from
legacy-rules assemblies (compat mode is used instead). Well-known members such as `Array.Length` are
assumed safe for simplicity. `PEUtilities.DeriveUnrecognizedMemorySafetyRulesAttributeDiagnostic` reports
`ERR_UnrecognizedAttributeVersion` for a version that is neither 1 nor 2.
`SourceModuleSymbol.AddMemorySafetyRulesAttributeIfNeeded` runs during
`CompletionPart.StartValidatingReferencedAssemblies` and reports diagnostics only when
`OutputKind == OutputKind.NetModule`.

**Note.** `RequiresUnsafeAttribute`'s `AttributeTargets` do **not** include `Field`, even though the design
lets fields be marked `unsafe`; the field case is expressed through the explicit/extended-layout rule
instead.

---

### N11-LANG-13 — Complete new and changed diagnostic inventory for C# 15

| Code | ErrorCode | Message |
|---|---|---|
| CS9354 | `ERR_CollectionArgumentsMustBeFirst` | `'with(...)' element must be the first element` |
| CS9355 | `ERR_CollectionArgumentsNotSupportedForType` | `'with(...)' elements are not supported for type '{0}'` |
| CS9356 | `ERR_CollectionArgumentsDynamicBinding` | `'with(...)' element arguments cannot be dynamic` |
| CS9357 | `ERR_CollectionArgumentsMustBeEmpty` | `'with(...)' element for a read-only interface must be empty if present` |
| CS9358 | `ERR_CollectionRefLikeElementType` | `Element type of this collection may not be a ref struct or a type parameter allowing ref structs` |
| CS9359 | `ERR_BadCollectionArgumentsArgCount` | `No overload for method '{0}' takes {1} 'with(...)' element arguments` |
| CS9360 | `ERR_UnsafeOperation` | This operation may only be used in an unsafe context |
| CS9361 | `ERR_UnsafeUninitializedStackAlloc` | stackalloc expression without an initializer inside SkipLocalsInit may only be used in an unsafe context |
| CS9362 | `ERR_UnsafeMemberOperation` | '{0}' must be used in an unsafe context because it is marked as 'unsafe' |
| CS9363 | `ERR_UnsafeMemberOperationCompat` | '{0}' must be used in an unsafe context because it has pointers in its signature |
| CS9364 | `ERR_CallerUnsafeOverridingSafe` | Unsafe member '{0}' cannot override safe member '{1}' |
| CS9365 | `ERR_CallerUnsafeImplicitlyImplementingSafe` | (implicit interface implementation variant) |
| CS9366 | `ERR_CallerUnsafeExplicitlyImplementingSafe` | (explicit interface implementation variant) |
| CS9369 | `ERR_ExpressionTreeContainsUnionConversion` | An expression tree may not contain a union conversion. |
| CS9370 | `ERR_UnionDeclarationNeedsCaseTypes` | A union declaration must specify at least one case type. |
| CS9371 | `ERR_NoImplicitConversionToObject` | Cannot convert type '{0}' to 'object' via an implicit reference or boxing conversion |
| CS9372 | `ERR_UnionMatchingWrongPattern` | An expression of type '{0}' cannot be handled by this pattern, see additional errors at this location. |
| CS9373 | `ERR_InstanceFieldInUnion` | Instance fields, auto-properties or field-like events are not permitted in a 'union' declaration. |
| CS9374 | `ERR_InstanceCtorWithOneParameterInUnion` | Explicitly declared public constructors with a single parameter are not permitted in a 'union' declaration. |
| CS9375 | `ERR_UnionConstructorCallsDefaultConstructor` | A constructor declared in a 'union' declaration must have a 'this' initializer that calls a synthesized or explicitly declared constructor. |
| CS9376 | `ERR_UnsafeConstructorConstraint` | An unsafe context is required for constructor '{0}' marked as 'unsafe' to satisfy the 'new()' constraint of type parameter '{1}' in '{2}' |
| CS9377 | `ERR_UnsafeMeaningless` | The 'unsafe' modifier does not have any effect here under the current memory safety rules. |
| CS9378 | `ERR_PPShebangNotOnFirstLine` | `'#!' must be the first characters on the first line of the file` |
| CS9379 | `ERR_RequiresUnsafeAttributeInSource` | Do not use 'RequiresUnsafeAttribute' in source; use the 'unsafe' modifier instead. |
| CS9380 | `ERR_ClosedTypeNameDisallowed` | Types and aliases cannot be named 'closed'. |
| CS9381 | `ERR_ClosedSealedStatic` | '{0}': a closed type cannot be sealed or static |
| CS9382 | `ERR_ClosedBaseTypeBaseFromOtherAssembly` | '{0}': cannot use a closed type '{1}' from another assembly as a base type. |
| CS9383 | `ERR_UnderspecifiedClosedSubtype` | '{0}': The type parameter '{1}' must be referenced in the base type '{2}' because the base type is closed. |
| CS9384 | `ERR_ClosedExplicitlyAbstract` | '{0}': a closed type cannot be marked abstract because it is always implicitly abstract. |
| CS9385 | `ERR_MissingUnionCaseTypes` | A union type must have at least one union creation member. |
| CS9386 | `ERR_MissingUnionValueProperty` | A union member provider type must have an instance 'Value' property of type 'object?' or 'object'. The property must have a public get accessor. |
| CS9387 | `ERR_MemberProviderInUnionDeclaration` | A 'union' declaration cannot use a union member provider interface. |
| CS9388 | `ERR_SafeModifierCannotBeUsedWithUnsafe` | The 'safe' and 'unsafe' modifiers cannot be used together. |
| CS9389 | `ERR_ExternMemberRequiresUnsafeOrSafe` | 'extern' member must be marked 'unsafe' or 'safe'. |
| CS9390 | `ERR_PartialMemberSafeDifference` | (partial parts disagree on `safe`) |
| CS9392 | `ERR_ExplicitOrExtendedLayoutFieldRequiresUnsafeOrSafe` | Field in an explicit or extended layout type must be marked 'unsafe' or 'safe'. |
| CS9393 | `ERR_NoBreakId` | No enclosing loop or switch statement with the label '{0}' out of which to break |
| CS9394 | `ERR_NoContinueId` | No enclosing loop with the label '{0}' out of which to continue |
| CS9395 | `ERR_ClosedBadDerivedTypesProperty` | 'System.Runtime.CompilerServices.IsClosedTypeAttribute.DerivedTypes' must be an instance property with public get and set accessors, no parameters, and type 'System.Type[]'. |
| CS9396 | `ERR_InvalidPropertyUnsafeMods` | Cannot specify 'unsafe' or 'safe' modifiers on both property or indexer '{0}' and its accessor. Remove one of them. |
| CS9397 | `ERR_SamePropertyUnsafeAccessorMods` | Cannot specify the same 'unsafe' or 'safe' modifier on all accessors of property or indexer '{0}'. Instead, put that modifier on the property itself. |
| CS9398 | `ERR_BadAwaitInFixed` | Cannot await in context of a 'fixed' statement |
| CS9399 | `ERR_FeatureNotAvailableInVersion15` | Feature '{0}' is not available in C# 15.0. Please use language version {1} or greater. |
| CS9400 | `ERR_BadCompilationOptionValueAccepted` | (invalid `MemorySafetyRulesVersion` value) |
| CS9346 | `ERR_EncUpdateRequiresEmittingExplicitInterfaceImplementationNotSupportedByTheRuntime` | Update requires emitting explicit interface implementation, which is not supported by the runtime without restarting the application. |

Reused existing codes: CS0106 (`closed` on a non-class); CS0261 (mixed partial kinds — message updated to
include "all unions"); CS1669 (`__arglist` in a case-type list); `ERR_UnexpectedToken` (anything but a bare
type in a case-type entry); CS0656 (missing `CompilerFeatureRequiredAttribute`); CS1750 (union conversion in
a default parameter value); CS0029 (lifted union conversion); CS0227 (`unsafe(...)` without
`AllowUnsafeBlocks`); CS0764 (partial `unsafe` mismatch); CS0214, CS0233.

`ERR_NullableOptionNotAvailable` was **renamed** to `ERR_CompilationOptionNotAvailable`; the numeric
identifier **CS8630** and the message text ("Invalid '{0}' value: '{1}' for C# {2}. Please use language
version '{3}' or greater.") are unchanged.

**Note on source reliability.** One research pass read values from `ErrorCode.cs` that were inconsistent
with the learn.microsoft.com compiler-message pages (it attributed 9363, 9370 and 9380 to unrelated C# 12
inline-array errors). The table above is taken from the message pages and from `CSharpResources.resx`,
which agree with each other and with the breaking-changes document.

---

### N11-LANG-14 — Explicitly NOT in C# 15

**Dictionary expressions** (`Dictionary<string,int> ages = ["mads": 21, "dustin": 22];`, csharplang #8659,
roslyn #81860, branch `features/dictionary-expressions`, developer 333fred) — in progress, **not merged
into `main`, no `IDS_Feature` entry in `MessageID.cs`**. The dictionary-interface rows of the
collection-expression-arguments table (`IDictionary<K,V>` → `Dictionary<K,V>()/(int)/(IEqualityComparer<K>)/(int, IEqualityComparer<K>)`;
`IReadOnlyDictionary<K,V>` → `()` and `(IEqualityComparer<K>? comparer)`) belong to this feature.

Also in the Roslyn Working Set (in progress, not C# 15), each with a branch and a state issue:
Null-conditional await (`await? GetX()?.DoSomethingAsync()`, #8631/#83237); Chained relational comparison
(`0 <= i < array.Length`, #8861/#83255); Target-typed static member access (`.Red`, `new .Success(42)`,
#9138/#83323); Relax modifier ordering (`partial internal class C`, #8966/#83324); Compound assignment in
object initializers and `with` expressions (`Tick += …`, `counter with { Value -= 1 }`, #9896/#83420);
Extension members on typeless receivers (`[1,2,3].ToImmutableArray()`, #10146/#83428); Runtime Async
Streams (`features/runtime-async-streams`, #75960); Extension constants (#10242/#84269); Type Parameter
Inference from Constraints (#9453, PR #84655).

**Interceptors: no C# 15 change** (see N11-ROSLYN-20). **`field` keyword follow-ups: none**
(`fieldof` exists as a proposal file with no Roslyn branch). **`params` improvements: none.**
**Extension events, extension constructors, extension fields, extension nested types, extension
finalizers and extension static constructors: not in C# 15** — all sit in the C# 14 speclet's future-work
section under the priority list "1. Properties and methods, 2. Operators, 3. Indexers, 4. Anything else".
Extension **operators** are C# 14, not C# 15.

`proposals/csharp-15.0/` contains exactly five files: `closed-hierarchies.md`,
`collection-expression-arguments.md`, `extension-indexers.md`, `labeled-break-continue.md`, `unions.md`.

Everything else in `dotnet/csharplang/proposals/` — closed enums, case declarations, standard unions,
unsigned `sizeof`, `capability-safe`, `top-level-members`, `final-initializers`, `factory-methods`,
`readonly-parameters`, `ref-struct-closures`, `iterators-in-lambdas`,
`block-bodied-switch-expression-arms`, `enhanced-switch-statements`,
`left-right-join-in-query-expressions`, `pattern-variables`, `multiple-using-var-discards`,
`anonymous-using-declarations`, `deconstruction-in-lambda-parameters`, `expand-ref`,
`inference-for-constructor-calls`, `inference-for-type-patterns`, `target-typed-generic-type-inference`,
`immediately-enumerated-collection-expressions`, `mixed-object-and-collection-initializers`,
`relaxed-partial-ref-ordering`, `extra-accessor-in-property-override`,
`readonly-setter-calls-on-non-variables`, `interpolated-string-handler-argument-value`,
`conditional-operator-access-syntax-refinement`, `async-method-ref-parameters`, `async-main-update`,
`breaking-change-warnings`, `fieldof`, `partial-extension-members` — has **no Roslyn branch, no state issue
and no `MessageID`**. Design work only.
---

## 2. Roslyn API and grammar changes (Roslyn 5.0 → 5.12)

Baseline for every diff: `dotnet/roslyn` `release/dev18.0` (Roslyn 5.0, C# 14 / .NET 10 GA, Nov 2025).
Tip: `main` (Roslyn 5.12).

### N11-ROSLYN-01 — `LanguageVersion.CSharp15 = 1500`

```
Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp15 = 1500
```

Introduced in **Roslyn 5.11** (absent from 5.10 and earlier). `ToDisplayString` returns `"15.0"`;
`TryParse("15")` and `TryParse("15.0")` succeed; `MapSpecifiedToEffectiveVersion(Latest | Default |
LatestMajor) => CSharp15` (it returned `CSharp14` in 5.0). `LatestMajor = int.MaxValue - 2`,
`Preview = int.MaxValue - 1`, `Latest = int.MaxValue`, `Default = 0` are unchanged. **No
`LanguageVersion.CSharp16`.** No `LanguageVersionFacts` member was removed or had its signature changed.

**Status at GA:** stable public API.

### N11-ROSLYN-02 — Five new `SyntaxKind` members

`SyntaxKind` is still `: ushort`; **no existing value was renumbered or removed.**

| Kind | Value | Notes |
|---|---|---|
| `UnionKeyword` | 8452 | contextual keyword |
| `ClosedKeyword` | 8453 | contextual keyword |
| `SafeKeyword` | 8454 | contextual keyword, `[Experimental("RSEXPERIMENTAL006")]` |
| `UnsafeExpression` | 8769 | node kind, `RSEXPERIMENTAL006` (slot 8768 remains the commented-out `NameOfExpression`) |
| `WithElement` | 9081 | node kind |
| `UnionDeclaration` | 9082 | node kind, last member of the enum |

Adjacent pre-existing values for orientation: `ExtensionKeyword = 8451`, `ShebangDirectiveTrivia = 8922`,
`NullableDirectiveTrivia = 9055`, `LineDirectivePosition = 9070`, `LineSpanDirectiveTrivia = 9071`,
`ScopedType = 9075`, `CollectionExpression = 9076`, `ExpressionElement = 9077`, `SpreadElement = 9078`,
`ExtensionBlockDeclaration = 9079`, `IgnoredDirectiveTrivia = 9080`.

A new comment in the file: "When adding new experimental kinds, you will need to manually specify
RSEXPERIMENTAL006, as not all projects that reference this file have RoslynExperiments available."

**Status at GA:** `UnionKeyword`, `ClosedKeyword`, `WithElement`, `UnionDeclaration` stable;
`SafeKeyword` and `UnsafeExpression` experimental.

### N11-ROSLYN-03 — `UnionDeclarationSyntax` (new node)

```xml
<Node Name="UnionDeclarationSyntax" Base="TypeDeclarationSyntax" SkipConvenienceFactories="true">
  <Kind Name="UnionDeclaration"/>
  <Field Name="AttributeLists"    Type="SyntaxList&lt;AttributeListSyntax&gt;" Override="true"/>
  <Field Name="Modifiers"         Type="SyntaxList&lt;SyntaxToken&gt;"         Override="true"/>
  <Field Name="Keyword"           Type="SyntaxToken" Override="true"><Kind Name="UnionKeyword"/></Field>
  <Field Name="Identifier"        Type="SyntaxToken" Override="true"><Kind Name="IdentifierToken"/></Field>
  <Field Name="TypeParameterList" Type="TypeParameterListSyntax" Optional="true" Override="true"/>
  <Field Name="ParameterList"     Type="ParameterListSyntax"     Optional="true" Override="true"/>
  <Field Name="BaseList"          Type="BaseListSyntax"          Optional="true" Override="true"/>
  <Field Name="ConstraintClauses" Type="SyntaxList&lt;TypeParameterConstraintClauseSyntax&gt;" Override="true"/>
  <Field Name="OpenBraceToken"    Type="SyntaxToken" Override="true" Optional="true"/>
  <Field Name="Members"           Type="SyntaxList&lt;MemberDeclarationSyntax&gt;" Override="true"/>
  <Field Name="CloseBraceToken"   Type="SyntaxToken" Override="true" Optional="true"/>
  <Field Name="SemicolonToken"    Type="SyntaxToken" Optional="true" Override="true"/>
</Node>
```

**Field-for-field identical to `ClassDeclarationSyntax` / `StructDeclarationSyntax` /
`InterfaceDeclarationSyntax` except for `Keyword`'s kind.** It derives from `TypeDeclarationSyntax`, hence
also from `BaseTypeDeclarationSyntax` and `MemberDeclarationSyntax`.

**The case-type list is modelled as a `ParameterListSyntax`**, not a dedicated node. The parser calls
`ParseParenthesizedParameterList(forExtensionOrUnion: isExtension || isUnion)`, which passes
`identifierIsOptional: true` and `requireOneElement: true`, so each case type is a `ParameterSyntax` with
`Type` set and `Identifier` missing — the same shape as an unnamed extension receiver parameter.
`UnionDeclarationSyntax.AddParameterListParameters(params ParameterSyntax[])` is the public add-member API.

`SkipConvenienceFactories="true"`: the **only** factory is the full 12-argument
`SyntaxFactory.UnionDeclaration(attributeLists, modifiers, keyword, identifier, typeParameterList,
parameterList, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken)`.
There is **no** short convenience overload. (Class, struct and interface declarations are treated the same
way.)

`TypeDeclarationSyntax.Keyword`'s doc comment now reads *"Gets the type keyword token ("class", "struct",
"interface", "record", "extension", "union")"*. `BaseTypeDeclarationSyntax`'s doc still omits "union" —
stale but harmless.

New visitor methods: `CSharpSyntaxVisitor.VisitUnionDeclaration`,
`CSharpSyntaxVisitor<TResult>.VisitUnionDeclaration`, `CSharpSyntaxRewriter.VisitUnionDeclaration`.
`SyntaxFacts.IsTypeDeclaration(SyntaxKind.UnionDeclaration)` returns `true`;
`SyntaxFacts.GetTypeDeclarationKind(SyntaxKind.UnionKeyword)` returns `SyntaxKind.UnionDeclaration`.

**Parsing is language-version gated.** `IsEnabledRecordOrUnionKeyword` returns true for
`SyntaxKind.UnionKeyword` only when `IsFeatureEnabled(MessageID.IDS_FeatureUnions)`, exactly as for
`record`. Below `LangVersion 15`, `union` is an ordinary identifier. Unions are parsed by
`ParseMainTypeDeclaration`, reached from the ordinary member-declaration path, so a union nests anywhere a
struct does; `IsPartialType()` calls `IsClassStructInterfaceRecordOrUnionKeyword`, so `partial union`
parses.

**Status at GA:** stable public API (the `ExperimentalUrl` present on the `features/Unions` branch snapshot
was dropped on `main`).

### N11-ROSLYN-04 — `WithElementSyntax` (new node)

```xml
<Node Name="WithElementSyntax" Base="CollectionElementSyntax">
  <Kind Name="WithElement"/>
  <Field Name="WithKeyword" Type="SyntaxToken"><Kind Name="WithKeyword"/></Field>
  <Field Name="ArgumentList" Type="ArgumentListSyntax" />
</Node>
```

A third concrete `CollectionElementSyntax` alongside `ExpressionElementSyntax` and `SpreadElementSyntax`,
so **any exhaustive switch over collection elements now has a third case**.
`CollectionExpressionSyntax.Elements` remains `SeparatedSyntaxList<CollectionElementSyntax>` with
`AllowTrailingSeparator="true"`. **No new token kind**: the keyword is the pre-existing
`SyntaxKind.WithKeyword`.

```
Microsoft.CodeAnalysis.CSharp.Syntax.WithElementSyntax
    .WithKeyword.get -> SyntaxToken
    .ArgumentList.get -> ArgumentListSyntax!
    .Update(SyntaxToken withKeyword, ArgumentListSyntax! argumentList) -> WithElementSyntax!
    .WithWithKeyword(SyntaxToken) / .WithArgumentList(ArgumentListSyntax!)
    .AddArgumentListArguments(params ArgumentSyntax![]!) -> WithElementSyntax!
static SyntaxFactory.WithElement(ArgumentListSyntax? argumentList = null) -> WithElementSyntax!
static SyntaxFactory.WithElement(SyntaxToken withKeyword, ArgumentListSyntax! argumentList) -> WithElementSyntax!
virtual  CSharpSyntaxVisitor.VisitWithElement(WithElementSyntax!) -> void
virtual  CSharpSyntaxVisitor<TResult>.VisitWithElement(WithElementSyntax!) -> TResult?
override CSharpSyntaxRewriter.VisitWithElement(WithElementSyntax!) -> SyntaxNode?
```

**Parsing is unconditional** (`LanguageParser.ParseCollectionElement` has no version check), so the tree
shape changes for `[with( … )]` at every `LangVersion`.

Binder: `Binder.BindCollectionExpression` reports
`MessageID.IDS_FeatureCollectionExpressionArguments.CheckFeatureAvailability` on the `with` keyword, calls
`BindArgumentsAndNames(withElementSyntax.ArgumentList, …, allowArglist: true)`, rejects `dynamic`
arguments, and produces a new bound node `BoundUnconvertedWithElement` when the element is first;
otherwise it reports `ERR_CollectionArgumentsMustBeFirst` and produces a `BoundBadExpression` so the
arguments remain in the tree for IDE analysis. `BoundUnconvertedCollectionExpression` gained a
`WithElement` child; the converted `BoundCollectionExpression` carries only a `bool HasWithElement`, the
arguments having been folded into `CollectionCreation` (a `BoundObjectCreationExpression` for a
constructor, or a `BoundCall` for a `CollectionBuilder` factory). The nesting guard
`MaxNestingLevel = 64` is unchanged.

**SemanticModel.** `CSharpSemanticModel` gained an `internal SymbolInfo GetSymbolInfo(WithElementSyntax,
CancellationToken)`, and `WithElementSyntax` was added to the `CanGetSemanticInfo` allow-list, so the
**public** `semanticModel.GetSymbolInfo((SyntaxNode)withElementSyntax)` returns the selected constructor or
create method.

**Status at GA:** stable public API (experimental in 5.6–5.10, de-experimentalised in 5.11).

### N11-ROSLYN-05 — `BreakStatementSyntax` / `ContinueStatementSyntax` gained an optional `Name` child

```xml
<Node Name="BreakStatementSyntax" Base="StatementSyntax">
  <Kind Name="BreakStatement"/>
  <Field Name="AttributeLists" Type="SyntaxList&lt;AttributeListSyntax&gt;" Override="true"/>
  <Field Name="BreakKeyword" Type="SyntaxToken"><Kind Name="BreakKeyword"/></Field>
  <Field Name="Name" Type="IdentifierNameSyntax" Optional="true"/>   <!-- NEW -->
  <Field Name="SemicolonToken" Type="SyntaxToken"><Kind Name="SemicolonToken"/></Field>
</Node>
```

and identically for `ContinueStatementSyntax`.

**This is the change most likely to break a syntax rewriter generated from the grammar**: two long-standing
nodes changed their child count from three to four, and the new child was **inserted in the middle**, not
appended. `VisitBreakStatement` / `VisitContinueStatement` keep their signatures, so a rewriter that
reconstructs the node by calling an old `Update` overload **silently drops the label**.

New public API (no experimental marker on `main`, despite the test-plan draft showing one):

```
BreakStatementSyntax.Name.get -> IdentifierNameSyntax?
BreakStatementSyntax.WithName(IdentifierNameSyntax? name) -> BreakStatementSyntax!
BreakStatementSyntax.Update(SyntaxList<AttributeListSyntax!>, SyntaxToken breakKeyword,
                            IdentifierNameSyntax? name, SyntaxToken semicolonToken) -> BreakStatementSyntax!
static SyntaxFactory.BreakStatement(IdentifierNameSyntax? name = null)
static SyntaxFactory.BreakStatement(SyntaxList<AttributeListSyntax!>, IdentifierNameSyntax? name)
static SyntaxFactory.BreakStatement(SyntaxList<AttributeListSyntax!>, SyntaxToken, IdentifierNameSyntax?, SyntaxToken)
// identical set for ContinueStatementSyntax / SyntaxFactory.ContinueStatement
```

The previously shipped overloads are **retained**: `BreakStatement()`,
`BreakStatement(SyntaxList<AttributeListSyntax!>)`,
`BreakStatement(SyntaxList<AttributeListSyntax!>, SyntaxToken, SyntaxToken)`,
`BreakStatement(SyntaxToken, SyntaxToken)`, and the two- and three-argument `Update` forms. So
`SyntaxFactory.BreakStatement()` remains unambiguous (a candidate with fewer declared parameters is
preferred over one that omits an optional parameter), `SyntaxFactory.BreakStatement` now has **six**
overloads and `Update` has **three**. Nothing was removed, but **any code compiled against the old
`Update(attributeLists, keyword, semicolonToken)` must be recompiled**.

**Status at GA:** stable public API.

### N11-ROSLYN-06 — `UnsafeExpressionSyntax` (new node, experimental)

```xml
<Node Name="UnsafeExpressionSyntax" Base="ExpressionSyntax"
      ExperimentalUrl="https://github.com/dotnet/roslyn/issues/82789">
  <Kind Name="UnsafeExpression"/>
  <Field Name="Keyword"         Type="SyntaxToken"><Kind Name="UnsafeKeyword"/></Field>
  <Field Name="OpenParenToken"  Type="SyntaxToken"><Kind Name="OpenParenToken"/></Field>
  <Field Name="Expression"      Type="ExpressionSyntax"/>
  <Field Name="CloseParenToken" Type="SyntaxToken"><Kind Name="CloseParenToken"/></Field>
</Node>
```

**This is the only node in the entire `Syntax.xml` carrying an `ExperimentalUrl` attribute**, i.e. the only
syntax node whose generated public API is `[Experimental]`. Same shape as `CheckedExpressionSyntax`;
`Precedence.Unary`; `SyntaxKindFacts` maps `SyntaxKind.UnsafeKeyword -> SyntaxKind.UnsafeExpression`.
Parsing is **not** language-version gated; the diagnostic is reported during binding.

**Binder and semantic-model shape (important for tools).**
`LocalBinderFactory.VisitUnsafeExpression` creates `_enclosing.WithAdditionalFlags(BinderFlags.UnsafeRegion)`
and maps it to the node, exactly parallel to `CheckedExpression`.
`SyntaxNodeExtensions.CanHaveAssociatedLocalBinder` returns `true` for `SyntaxKind.UnsafeExpression`.
`MemberSemanticModel.GetEnclosingBinder` special-cases it so the binder applies only *between* the
parentheses. `MemberSemanticModel.GetBindableSyntaxNode` **unwraps** it
(`case UnsafeExpressionSyntax n: node = n.Expression; continue;`). There is **no `BoundUnsafeExpression`
and no dedicated `IOperation`**; binding returns the bound node of the inner expression via
`BindParenthesizedExpression`. So `GetTypeInfo` / `GetSymbolInfo` / `GetOperation` on an
`UnsafeExpressionSyntax` behave as on the inner expression, exactly like `checked(x)` and `(x)`.
The construct emits no IL of its own and no sequence point of its own.

**Status at GA:** present but `[Experimental("RSEXPERIMENTAL006")]`.

### N11-ROSLYN-07 — `ParameterSyntax` now validates (behavioural break)

```diff
- <Node Name="ParameterSyntax" Base="BaseParameterSyntax" SkipConvenienceFactories="true">
+ <Node Name="ParameterSyntax" Base="BaseParameterSyntax" SkipConvenienceFactories="true" HasValidate="true">
-   <Field Name="Type" Type="TypeSyntax" Optional="true" Override="true"/>
+   <Field Name="Type" Type="TypeSyntax" Optional="true" RequiredForTest="true" Override="true"/>
```

```csharp
private partial void Validate()
{
    if (Type is null && Identifier.IsKind(SyntaxKind.None))
        throw new System.ArgumentException(CSharpResources.ParameterRequiresTypeOrIdentifier);
}
```

Creating a `ParameterSyntax` with **both** `Type` and `Identifier` missing now throws `ArgumentException`
at construction time; it previously produced a degenerate node silently. Closes roslyn #78961. This is a
direct consequence of the union case-type list reusing `ParameterListSyntax`.

**Status at GA:** shipped behaviour change; no API signature change.

### N11-ROSLYN-08 — `Syntax.xml` complete delta and what did NOT change

The **entire** `Syntax.xml` diff from Roslyn 5.0 to 5.12 (5191 → 5265 lines) is six items:
`UnsafeExpressionSyntax` (new), `WithElementSyntax` (new), a `<TypeComment>` added to
`GlobalStatementSyntax`, the `Name` field on `BreakStatementSyntax` and `ContinueStatementSyntax`,
`UnionDeclarationSyntax` (new) plus the `TypeDeclarationSyntax.Keyword` doc update, and the
`ParameterSyntax` validation.

**`ExtensionBlockDeclarationSyntax` is unchanged.** Extension indexers reuse the existing
`IndexerDeclarationSyntax` inside the existing `ExtensionBlockDeclarationSyntax`
(`SyntaxKind.ExtensionBlockDeclaration = 9079`, C# 14). No new node, no new kind, no new factory.

**No directive node was touched.** `LineDirectiveTriviaSyntax`, `LineSpanDirectiveTriviaSyntax`,
`LineDirectivePositionSyntax`, `LineOrSpanDirectiveTriviaSyntax`, `PragmaChecksumDirectiveTriviaSyntax`,
`PragmaWarningDirectiveTriviaSyntax`, `NullableDirectiveTriviaSyntax`, `RegionDirectiveTriviaSyntax`,
`EndRegionDirectiveTriviaSyntax`, `ErrorDirectiveTriviaSyntax`, `WarningDirectiveTriviaSyntax`,
`DefineDirectiveTriviaSyntax`, `UndefDirectiveTriviaSyntax`, the `#if` family,
`BadDirectiveTriviaSyntax`, `ReferenceDirectiveTriviaSyntax`, `LoadDirectiveTriviaSyntax`,
`ShebangDirectiveTriviaSyntax` and `IgnoredDirectiveTriviaSyntax` are all byte-identical between the two
branches. `src/Compilers/CSharp/Portable/Parser/Directives.cs` is byte-identical.

### N11-ROSLYN-09 — `SyntaxFacts` behavioural changes

```csharp
// IsTypeDeclaration
case SyntaxKind.UnionDeclaration:   // NEW, returns true

// keyword-to-expression-kind map
case SyntaxKind.UnsafeKeyword: return SyntaxKind.UnsafeExpression;   // NEW

// GetTypeDeclarationKind
case SyntaxKind.UnionKeyword:     return SyntaxKind.UnionDeclaration;          // NEW
case SyntaxKind.ExtensionKeyword: return SyntaxKind.ExtensionBlockDeclaration; // NEW; previously SyntaxKind.None

// GetContextualKeywordKinds()
- for (int i = (int)SyntaxKind.YieldKeyword; i <= (int)SyntaxKind.ExtensionKeyword; i++)
+ for (int i = (int)SyntaxKind.YieldKeyword; i <= (int)SyntaxKind.SafeKeyword; i++)

// IsContextualKeyword: now also true for UnionKeyword, ClosedKeyword, SafeKeyword
// GetContextualKeywordKind("union" | "closed" | "safe"): new mappings
// GetText(UnionKeyword | ClosedKeyword | SafeKeyword): "union" | "closed" | "safe"
```

`SyntaxFacts.GetTypeDeclarationKind(SyntaxKind.ExtensionKeyword)` returning `ExtensionBlockDeclaration`
instead of `SyntaxKind.None` is a **silent behavioural change** for any caller that relied on `None` to
mean "not a type-declaration keyword"; the tracking comment for roslyn #78957 was removed at the same time.
Any code that hard-codes the upper bound of the contextual-keyword range must be updated.

**Status at GA:** shipped, unannounced in the breaking-changes document.

### N11-ROSLYN-10 — New `ITypeSymbol` members for unions and closed types

```csharp
/// <summary>True if language treats the type as a Union.</summary>
bool IsUnion { get; }

/// <summary>When IsUnion is true, returns the case types of the union. Otherwise, an empty array.</summary>
ImmutableArray<ITypeSymbol> UnionCaseTypes { get; }

/// <summary>Indicates that the type is restricted from being inherited from outside its containing module.</summary>
bool IsClosed { get; }

/// <summary>Gets the direct derived types of a closed type.</summary>
/// <exception cref="InvalidOperationException">If this is not a closed type.</exception>
ClosedDerivedTypeInfo GetClosedDerivedTypeInfo(CancellationToken cancellationToken);
```

Implementation:

```csharp
bool ITypeSymbol.IsUnion  => UnderlyingTypeSymbol is Symbols.NamedTypeSymbol { IsUnionType: true };
bool ITypeSymbol.IsClosed => UnderlyingTypeSymbol is Symbols.NamedTypeSymbol { IsClosed: true };

ImmutableArray<ITypeSymbol> ITypeSymbol.UnionCaseTypes
    => UnderlyingTypeSymbol is Symbols.NamedTypeSymbol { IsUnionType: true } namedType
       ? namedType.UnionCaseTypesNoUseSiteDiagnostics.GetPublicSymbols()
       : ImmutableArray<ITypeSymbol>.Empty;

ClosedDerivedTypeInfo ITypeSymbol.GetClosedDerivedTypeInfo(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    if (UnderlyingTypeSymbol is not Symbols.NamedTypeSymbol { IsClosed: true } namedType)
        throw new InvalidOperationException(CSharpResources.GetClosedDerivedTypeInfoMustBeClosed);
    var isComplete = namedType.TryGetClosedSubtypes(out var subtypes, cancellationToken);
    return new ClosedDerivedTypeInfo(subtypes.GetPublicSymbols(), isComplete);
}
```

`IsUnion` and `IsClosed` are `false` for anything that is not a `NamedTypeSymbol` (arrays, pointers, type
parameters, `dynamic`). **`GetClosedDerivedTypeInfo` throws `InvalidOperationException` on a non-closed
type — always guard with `IsClosed`.**

**`IsUnion` is true for BOTH a `union` declaration and any hand-written class or struct carrying
`[Union]`**: `SourceNamedTypeSymbol.IsUnionTypeCore => IsUnionDeclaration || HasUnionAttribute`, and
`NamedTypeSymbol.IsUnionType` is `TypeKind is TypeKind.Class or TypeKind.Struct && IsUnionTypeCore`. There
is **no public way to distinguish a `union` declaration from a `[Union]` type** except by inspecting
`DeclaringSyntaxReferences` for a `UnionDeclarationSyntax`; `SourceMemberContainerSymbol.IsUnionDeclaration`
is `internal`. For metadata symbols, union-ness is detected purely from the attribute
(`PENamedTypeSymbol` `FindTargetAttribute(AttributeDescription.UnionAttribute)`), and closed-ness from
`IsClosedTypeAttribute` (`lazyIsClosed`).

VB implements these too, as do `CodeGenerationTypeSymbol` and the MetadataAsSource `WrappedNamedTypeSymbol`
wrappers, so **any custom `ITypeSymbol` implementation outside Roslyn must add these four members**.

**Status at GA:** stable public API (experimental in 5.9/5.10, de-experimentalised in 5.11).

### N11-ROSLYN-11 — `Microsoft.CodeAnalysis.ClosedDerivedTypeInfo` (new public struct)

```csharp
public readonly struct ClosedDerivedTypeInfo
{
    /// <summary>Possible direct derived types of the closed type.</summary>
    public ImmutableArray<INamedTypeSymbol> ClosedDerivedTypes { get; }

    /// <summary>Whether ClosedDerivedTypes is a complete set. False, for example, when a generic
    /// closed type has an unspeakable derived type.</summary>
    public bool IsComplete { get; }

    public ClosedDerivedTypeInfo();
}
```

**Precise meaning of `IsComplete`.** `IsComplete == true` iff every candidate direct subtype that unifies
with the (possibly substituted) closed type is **speakable**, i.e. introduces no type parameter beyond
those appearing on the closed type. This is **always** true when neither the closed type nor any candidate
subtype is generic (early return). `IsComplete == false` iff at least one candidate subtype unified but was
**discarded** because it mentions a type parameter the closed type does not:

```csharp
closed class Base<T> { }
sealed class Derived<T, U> : Base<T> { }   // U is not speakable from Base<T>
// Base<int>.GetClosedDerivedTypeInfo() omits Derived<int, ?> and reports IsComplete == false
```

**`IsComplete` has nothing to do with accessibility.** Candidate subtypes are **not** filtered by
accessibility anywhere on this path.

**Known defect:** `PENamedTypeSymbol.CandidateClosedSubtypeDefinitions` swallows `BadImageFormatException`
and `UnsupportedSignatureContent` and returns whatever it gathered, so `IsComplete` can be `true` while the
set is short — tracked as <https://github.com/dotnet/roslyn/issues/83617>.

**Ref assemblies do not change the answer.** Roslyn **never reads `IsClosedTypeAttribute.DerivedTypes`**;
only the *presence* of the attribute is tested (`HasAttribute`), and
`AttributeDescription.IsClosedTypeAttribute` declares only the parameterless constructor signature. The
derived set is recomputed by scanning `metadataReader.TypeDefinitions`, and reference assemblies keep every
`TypeDef` row. Therefore `GetClosedDerivedTypeInfo` returns the same set and the same `IsComplete` whether
the closed class is read from the reference assembly or from the implementation assembly. (Derivation is
from the emitter code; `ClosedClassesTests.cs` contains no `refonly` / `refout` / `EmitMetadataOnly`
coverage.)

**Status at GA:** stable public API.

### N11-ROSLYN-12 — Union and closed-type attributes are FILTERED OUT of `GetAttributes()`

`ClosedClassesTests.Symbols_01` asserts, for both the source compilation and the metadata compilation:

```csharp
Assert.True(classC.IsClosed);
Assert.Empty(classC.GetAttributes());          // IsClosedTypeAttribute is filtered out
var ctor = classC.Constructors.Single();
Assert.Empty(ctor.GetAttributes());            // CompilerFeatureRequiredAttribute is filtered out
// but the raw metadata still shows it:
AssertEx.SetEqual(["System.Runtime.CompilerServices.IsClosedTypeAttribute(DerivedTypes = {})"],
    GetAttributeStrings(peModule.GetCustomAttributesForToken(peType.Handle)));
```

This is the same pattern used for `IsReadOnlyAttribute`, `RequiredMemberAttribute`,
`ExtensionMarkerAttribute` and `RequiresUnsafeAttribute`. **The practical rule for any tool with its own
code model: read these facts from the dedicated `ISymbol` property (`ITypeSymbol.IsClosed`,
`ISymbol.RequiresUnsafeContext`, `IModuleSymbol.MemorySafetyRulesVersion`) or from raw metadata, never from
`GetAttributes()`.** Whether `UnionAttribute` is likewise filtered is **unverified** (OQ-09).

### N11-ROSLYN-13 — A `union` declaration is `TypeKind.Struct` — RES-01

**RES-01.** One research pass stated "A union is `TypeKind.Class` with `IsUnion == true`". This is **wrong**
for a `union` declaration. The decisive evidence:

- `src/Compilers/Core/Portable/Symbols/TypeKind.cs` ends at `Submission = 12`, `FunctionPointer = 13`,
  `Extension = 14`. **There is no `TypeKind.Union` and no `TypeKind.ClosedClass`.** The enum is
  byte-for-byte identical between Roslyn 5.0 and 5.12.
- `DeclarationKind` gains a `Union` member, and `EnumConversions.ToTypeKind` maps
  `DeclarationKind.Struct`, `DeclarationKind.Union` and `DeclarationKind.RecordStruct` all to
  **`TypeKind.Struct`**.
- The emitted IL `extends [netstandard]System.ValueType` and is `sealed`.
- `SourceMemberContainerSymbol.MakeModifiers` handles a union under `case TypeKind.Struct:`.

**Reconciliation.** `NamedTypeSymbol.IsUnionType` is
`TypeKind is TypeKind.Class or TypeKind.Struct && IsUnionTypeCore`, so a **hand-written `[Union] class`**
*is* a union type with `TypeKind.Class`. Only a `union` **declaration** is necessarily `TypeKind.Struct`.

**Consequences.** A `union` declaration surfaces as an `INamedTypeSymbol` with
`TypeKind == TypeKind.Struct`, `IsValueType == true`, `IsSealed == true`, `IsReferenceType == false`,
`IsRecord == false`. Any existing `TypeKind` switch keeps working; union-ness is a *flag*, not a kind.

### N11-ROSLYN-14 — SemanticModel behaviour on a union's case-type list

```csharp
Assert.Same(s1, model.GetDeclaredSymbol(s1Decl).GetSymbol());
Assert.Null(model.GetDeclaredSymbol(s1Decl.ParameterList));
Assert.Null(model.GetDeclaredSymbol(s1Decl.ParameterList.Parameters[0]));
Assert.Null(model.GetDeclaredSymbol(s1Decl.ParameterList.Parameters[0].Type));

var typeInfo = model.GetTypeInfo(s1Decl.ParameterList.Parameters[0].Type);
Assert.Equal("System.Boolean", typeInfo.Type.ToTestDisplayString());
Assert.Equal("System.Boolean", typeInfo.ConvertedType.ToTestDisplayString());
```

**`GetDeclaredSymbol` on a union's `ParameterList` or any of its `Parameter`s returns `null`** — they are
not parameter symbols. Use `GetTypeInfo(parameter.Type)` to obtain the case type.

The synthesised constructors' `Locations` point at the **case-type syntax** (`bool`, `int`), not at the
union declaration; the `Value` property and its accessor point at the whole union declaration node.

### N11-ROSLYN-15 — Synthesised union members

- `SynthesizedUnionValuePropertySymbol` — a `SourcePropertySymbolBase` named
  `WellKnownMemberNames.ValuePropertyName` (`"Value"`), `DeclarationModifiers.Public`, get-only auto
  property of type `object?` (`System_Object` with `NullableAnnotation.Annotated`),
  **`IsImplicitlyDeclared => true`**. `SourcePropertyAccessorSymbol.TryGetBodyBinder` returns `null` for it
  (alongside `SynthesizedRecordEqualityContractProperty` and `SynthesizedRecordPropertySymbol`). Its backing
  field is exempted from the "instance field in union" check.
- `SynthesizedUnionCtor` — derives from `SynthesizedInstanceConstructor`; one parameter, ordinal 0,
  `RefKind.None`, named `ParameterSymbol.ValueParameterName` (`"value"`);
  `DeclaredAccessibility => Accessibility.Public`; `IsImplicitlyDeclared`; emits `[CompilerGenerated]`.
  Its body assigns `valueProperty.DeclaredBackingField` after
  `IsValidParameterTypeConversion(c) => c.Exists && c.IsImplicit && (c.IsIdentity || c.IsReference || c.IsBoxing)`;
  when invalid the body becomes a `BoundNoOpStatement` with `hasErrors: true` and `CS9371` is reported.
- **Each synthesised constructor ends with an explicit `BoundSequencePointWithSpan` whose syntax node is
  the `UnionDeclarationSyntax` but whose span is the *case type's* source span**, "so that a breakpoint
  placed on the case type can be hit whenever a new instance of the union for that case type is created".
  This is a sequence point whose span deliberately does not correspond to any statement.
- The `Value` getter is an ordinary auto-property getter whose `accessor.SyntaxNode` is the union's
  `TypeDeclarationSyntax`, so its sequence point is anchored to the whole union type declaration.
  (Derived from code reading; there is no PDB test for unions.)
- `HasValue` / `TryGetValue` are **not** synthesised by a `union` declaration; the two new
  `WellKnownMemberNames` constants exist only for recognising hand-written union types.
### N11-ROSLYN-16 — `IOperation` changes for collection expressions

```csharp
Microsoft.CodeAnalysis.OperationKind.CollectionExpressionElementsPlaceholder = 129   // the ONLY new OperationKind
public interface ICollectionExpressionElementsPlaceholderOperation : IOperation { }  // HasType = true, no extra properties
ImmutableArray<IOperation> ICollectionExpressionOperation.ConstructArguments { get; }
virtual void OperationVisitor.VisitCollectionExpressionElementsPlaceholder(ICollectionExpressionElementsPlaceholderOperation);
virtual TResult? OperationVisitor<TArgument, TResult>.VisitCollectionExpressionElementsPlaceholder(…, TArgument);
```

**`ICollectionExpressionOperation.ChildrenOrder` changed from `Elements` to `ConstructArguments,Elements`**,
so `IOperation.ChildOperations` for a collection expression now enumerates the construct arguments
**first**. This affects every collection expression, not only those with `with(...)`.

`ConstructMethod` documentation was rewritten: null for arrays, spans and type parameters; the builder
method for a `[CollectionBuilder]` type; the `List<T>` constructor for a mutable array interface initialised
with arguments (null for a read-only interface or when no arguments were provided); otherwise the collection
type's constructor.

`ConstructArguments` are in evaluation order, never `default`, and are `IArgumentOperation` when binding
succeeded (any operation otherwise). Params arguments are collected into arrays in expanded form and
defaults are supplied for missing optional arguments. **For a collection-builder method, the final
`ReadOnlySpan` argument is represented by an `IArgumentOperation` whose `Value` is an
`ICollectionExpressionElementsPlaceholderOperation`**; the actual elements remain in `Elements`.

**No new `OperationKind` was added for unions, closed classes, labeled jumps or `unsafe(...)`.** A labeled
`break`/`continue` is still an `IBranchOperation`; `unsafe(...)` has no operation of its own.
`CommonConversion.IsUnion` exists, but whether union conversions or union pattern matching introduce any
other `IOperation` shape is unverified (OQ-10).

**Status at GA:** stable public API.

### N11-ROSLYN-17 — Other new `Microsoft.CodeAnalysis` / `Microsoft.CodeAnalysis.CSharp` API

```csharp
// Conversions
bool Microsoft.CodeAnalysis.CSharp.Conversion.IsUnion { get; }
[MemberNotNullWhen(true, nameof(MethodSymbol))]
bool Microsoft.CodeAnalysis.Operations.CommonConversion.IsUnion { get; }
// true when MethodSymbol is a constructor whose containing type IsUnion, or a static Create method on a
// nested IUnionMembers interface of a union type. CommonConversion.MethodSymbol's doc was updated.

// Well-known member names (new public constants)
public const string WellKnownMemberNames.HasValuePropertyName  = "HasValue";
public const string WellKnownMemberNames.TryGetValueMethodName = "TryGetValue";
// UnionMembersInterfaceName = "IUnionMembers" and UnionFactoryMethodName = "Create" stay INTERNAL.
// ValuePropertyName ("Value") already existed; its doc now adds "Also required name for the IUnion.Value
// property used in Union matching."

// Type layout (previously internal, now public)
public readonly struct Microsoft.CodeAnalysis.TypeLayout : IEquatable<TypeLayout>
{
    public System.Runtime.InteropServices.LayoutKind Kind { get; }  // default(TypeLayout).Kind == LayoutKind.Auto
    public ushort PackingSize { get; }
    public int Size { get; }
    public bool Equals(TypeLayout other); public override bool Equals(object? obj);
    public override int GetHashCode();
    public static bool operator ==(TypeLayout, TypeLayout); public static bool operator !=(TypeLayout, TypeLayout);
    public TypeLayout();
}
Microsoft.CodeAnalysis.TypeLayout INamedTypeSymbol.TypeLayout { get; }

// Source text hashing
Microsoft.CodeAnalysis.Text.SourceHashAlgorithm.Sha384 = 3
Microsoft.CodeAnalysis.Text.SourceHashAlgorithm.Sha512 = 4

// Emit / EnC
bool Microsoft.CodeAnalysis.Emit.EmitDifferenceOptions.MethodImplEntriesSupported { get; init; }  // default true

// Semantic-model helpers (5.12)
static Conversion CSharpExtensions.GetValueConversion(this ICoalesceOperation coalesceExpression);
static VisualBasic.Conversion VisualBasicExtensions.GetValueConversion(this ICoalesceOperation);
// ^ the ONLY public API change in Microsoft.CodeAnalysis.VisualBasic between 5.0 and 5.12

// Memory safety (all RSEXPERIMENTAL006)
[Experimental] public enum Microsoft.CodeAnalysis.MemorySafetyRulesVersion { Version1 = 1, Version2 = 2 }
[Experimental] MemorySafetyRulesVersion IModuleSymbol.MemorySafetyRulesVersion { get; }
[Experimental] bool ISymbol.RequiresUnsafeContext { get; }
[Experimental] MemorySafetyRulesVersion CSharpCompilationOptions.MemorySafetyRulesVersion { get; }
[Experimental] CSharpCompilationOptions CSharpCompilationOptions.WithMemorySafetyRulesVersion(MemorySafetyRulesVersion);

// Workspaces (the ENTIRE Workspaces public-API delta: three lines)
bool Microsoft.CodeAnalysis.Editing.DeclarationModifiers.IsClosed { get; }
DeclarationModifiers Microsoft.CodeAnalysis.Editing.DeclarationModifiers.WithIsClosed(bool isClosed);
static DeclarationModifiers Microsoft.CodeAnalysis.Editing.DeclarationModifiers.Closed { get; }
```

`ISymbol.RequiresUnsafeContext` is a **cross-language** API on `ISymbol`, so it exists on every symbol kind,
and **its meaning is relative to the declaring module's rules version, not the consuming compilation's**:
under `Version1`, symbols with pointers in their signature are requires-unsafe; under `Version2`, symbols
marked `unsafe` are. Under legacy rules — the default at GA — it is therefore **true for members with
pointers anywhere in the signature, including symbols read from already-shipped assemblies**.

`CSharpCompilationOptions.MemorySafetyRulesVersion` defaults to `Version1` in every constructor, gained a
new internal constructor parameter, participates in `Equals`/`GetHashCode`, and is **not yet serialised**
into deterministic-build compilation options (roslyn #82546).

`ForEachStatementInfo.Equals` / `GetHashCode` now include `MoveNextAwaitableInfo` and `DisposeAwaitableInfo`
(added in Roslyn 5.3), so two `ForEachStatementInfo` values that compared equal under 5.0 can compare
unequal under 5.12.

`IMethodSymbol.ReduceExtensionMember(ITypeSymbol)` and `IPropertySymbol.ReduceExtensionMember(ITypeSymbol)`
were introduced in **Roslyn 5.3** and were never experimental; the property overload is what makes extension
indexers reachable. `ReduceExtensionMethod(ITypeSymbol)` is unchanged. `MethodKind` gained no member.
`IParameterSymbol`: **no change at all** between 5.0 and 5.12.

`RuntimeCapability.RuntimeAsyncMethods = 9` **already existed in Roslyn 5.0**; it is not new.

### N11-ROSLYN-18 — Pre-compilation source outputs (`RegisterPreCompilationSourceOutput`, experimental)

```
[RSEXPERIMENTAL007] IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput<TSource>(
    IncrementalValueProvider<TSource> source, Action<PreCompilationSourceProductionContext, TSource>! action) -> void
[RSEXPERIMENTAL007] IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput<TSource>(
    IncrementalValuesProvider<TSource> source, Action<PreCompilationSourceProductionContext, TSource>! action) -> void
[RSEXPERIMENTAL007] Microsoft.CodeAnalysis.PreCompilationSourceProductionContext          // readonly struct
[RSEXPERIMENTAL007]   .AddSource(string! hintName, SourceText! sourceText) -> void
[RSEXPERIMENTAL007]   .AddSource(string! hintName, string! source) -> void
[RSEXPERIMENTAL007]   .CancellationToken.get -> CancellationToken
Microsoft.CodeAnalysis.IncrementalGeneratorOutputKind.PreCompilation = 16                  // NOT experimental
const Microsoft.CodeAnalysis.WellKnownGeneratorOutputs.PreCompilationSourceOutput = "PreCompilationSourceOutput"  // NOT experimental
```

`RSEXPERIMENTAL007`, `UrlFormat = "https://github.com/dotnet/roslyn/issues/83089"`. Implementation PR
<https://github.com/dotnet/roslyn/pull/83088>, merged 2026-05-20. Present in Roslyn 5.10, 5.11 and 5.12.

**`IncrementalGeneratorOutputKind.PreCompilation = 16` and the `WellKnownGeneratorOutputs` constant are NOT
marked experimental**, so any generator-driver host sees the new output kind flow through
`GeneratorDriverOptions.DisabledOutputs` and `GeneratorRunResult.TrackedOutputSteps` without opting into the
experiment. See N11-SDK-14 for the full driver behaviour.

`PreCompilationSourceProductionContext` deliberately has **no `ReportDiagnostic`**: "Pre-compilation is an
early phase focused purely on producing source; diagnostic reporting should be done in a separate
analyzer."

**Status at GA:** present but `[Experimental("RSEXPERIMENTAL007")]`. The design document says
"Given that this is an experimental API, runtime enforcement with clear error messages is sufficient for
the initial release", which strongly implies it stays experimental (OQ-11).

### N11-ROSLYN-19 — Symbol display, documentation comment IDs and CREF

**Symbol display: NO new API and no union or closed awareness.** Every file under
`src/Compilers/CSharp/Portable/SymbolDisplay/` returns zero matches for "union" or "closed".
`SymbolDisplayPartKind` still ends at `RecordClassName = 31`, `RecordStructName = 32`.
`SymbolDisplayKindOptions`, `SymbolDisplayMemberOptions`, `SymbolDisplayMiscellaneousOptions`,
`SymbolDisplayTypeQualificationStyle`, `SymbolDisplayGenericsOptions` and `SymbolDisplayFormat` gained no
member.

Consequences:
- `AddTypeKind` has **no `case TypeKind.Struct when symbol.IsUnion`**, so with
  `SymbolDisplayKindOptions.IncludeTypeKeyword` a union renders as **`struct Pet`, never `union Pet`**, and
  `GetPartKind` returns `SymbolDisplayPartKind.StructName`.
- `VisitNamedType` emits **no type modifiers at all** beyond `readonly` and `ref` on structs, so `closed`
  is never rendered (consistent with `abstract`, `sealed`, `static`, `partial`, `file`).
- **None of `CSharpErrorMessageFormat`, `FullyQualifiedFormat` or `MinimallyQualifiedFormat` sets
  `IncludeTypeKeyword`**, so none of them prints a type keyword; the "union looks like a struct" problem is
  latent in those three and becomes visible with a format that sets it (as IDE QuickInfo does).

| Symbol | `CSharpErrorMessageFormat` | `FullyQualifiedFormat` | `MinimallyQualifiedFormat` |
|---|---|---|---|
| union `N.Pet` | `N.Pet` | `global::N.Pet` | `Pet` |
| closed class `N.GateState` | `N.GateState` | `global::N.GateState` | `GateState` |
| union constructor | `N.Pet.Pet(N.Cat)` | (types only) | `Pet.Pet(Cat value)` |
| union `Value` property | `N.Pet.Value` | (types only) | `object Pet.Value` |

- **`EscapeIdentifier` special-cases only `"record"`.** A type named `union` or `closed` is rendered
  **unescaped** even under `SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers`, whereas a type
  named `record` becomes `@record`. Harmful only when the string is reparsed in a type-declaration
  position.
- **Extension blocks and extension indexers DO display specially**: an extension block prints as
  `E.extension(int)` (the containing type is always visited, regardless of `TypeQualificationStyle`), and
  an extension indexer as **`E.extension(int).this[int]`** — the receiver is not in the indexer's own
  parameter list. `IPropertySymbol.Name == "this[]"`. The grouping/marker metadata names appear only under
  the `internal` `SymbolDisplayCompilerInternalOptions.UseMetadataMemberNames`, e.g.
  `Extensions.<G>$C43E…​.<M>$C43E…`.

**Documentation comment identifiers: NO union or closed handling in either generator**, and none is needed —
a union produces an ordinary named struct and a closed class an ordinary named class, both with speakable
names.

| Symbol | Documentation comment ID |
|---|---|
| union type `Pet` | `T:N.Pet` |
| union constructor `Pet(Cat)` | `M:N.Pet.#ctor(N.Cat)` |
| union `Value` property | `P:N.Pet.Value` |
| union `Value` getter | `M:N.Pet.get_Value` |
| generic union `Option<T>` | ``T:N.Option`1`` |
| closed class | `T:N.GateState` |
| extension block | ``T:E.<G>$HASH`1.<M>$HASH`` (grouping **and** marker) |
| extension member | ``M:E.<G>$HASH.M`` (grouping **only**, no marker) |
| extension indexer | ``P:E.<G>$HASH`1.Item(System.String)`` (metadata name `Item`, receiver **not** in the parameter list) |
| extension indexer `get` implementation | ``M:E.get_Item``1(``0,System.String)`` (receiver prepended) |
| extension indexer `set` implementation | ``M:E.set_Item``1(``0,System.String,System.Int32)`` (receiver first, `value` last) |

The case types of a union are **not represented in any identifier**; a union is addressed exactly like a
struct, and its case types as the independent types they are. There is no `Pet.Cat`-style form.

`CreateReferenceId(extensionMember)` returns the **empty string**, not `null`.
The union's synthesised `Value` property and constructors are `IsImplicitlyDeclared`, so
`DocumentationCommentCompiler.ShouldSkip` **omits them from the emitted XML file** and from `CS1591`, even
though `GetDocumentationCommentId()` still returns identifiers for them. There is **no union analogue of
the record `<param>`-copying logic**, so documentation written on a union's case-type list has nowhere to go.

Extension grouping and marker names are `<G>$` / `<M>$` plus a **32-upper-case-hex-character XxHash128**
over the UTF-16 code units. (The C# 14 extensions speclet is stale here: it still shows
`<>E__MarkerContentName_For_ExtensionOfT`.) Angle brackets survive into the ID raw and are XML-escaped only
when written into an attribute, by `Symbol.GetEscapedDocumentationCommentId`.

**CREF grammar.** `extension_member_cref : 'extension' type_argument_list? cref_parameter_list '.' member_cref`
(C# 14); the only C# 15 addition is that `member_cref` may now be an `indexer_member_cref`, gated on
`MessageID.IDS_FeatureExtensionIndexers`.

```csharp
/// <see cref="E.extension(int).this[string]"/>      -> the skeleton indexer property
/// <see cref="E.extension(int).get_Item(string)"/>  -> the skeleton indexer's GETTER
/// <see cref="E.extension(int).set_Item(string, int)"/>
/// <see cref="E.get_Item(int, string)"/>            -> the IMPLEMENTATION method on the static class
/// <see cref="E.set_Item(int, string, int)"/>
/// <see cref="E.extension(int).this[]"/>            -> CS1574, does NOT resolve
/// <see cref="E.extension(int).Item(string)"/>      -> CS1574, does NOT resolve
/// <see cref="E.extension{U}(int).this[U]"/>        -> generic form uses braces
```

A cref cannot address an extension block itself; `extension_member_cref` may not be used at top level or
nested in another extension. **There is no new cref production for unions or closed classes**; they are
addressed by ordinary type and member crefs (`cref="Pet"`, `cref="Pet.Value"`, `cref="Pet.Pet(Cat)"`,
``cref="Option{T}"``).

**Two pre-existing generator divergences that now reach the new constructs.**
(a) The C# generator's `GetEscapedMetadataName` maps `<`→`{`, `>`→`}` for **members**, while the core
`DocumentationCommentId.EncodeName` leaves them alone, so the union backing field is
`F:N.Pet.{Value}k__BackingField` from one and `F:N.Pet.<Value>k__BackingField` from the other. Type names,
including the grouping and marker names, bypass this in both, so the extension type IDs agree.
(b) The core generator's `EncodePropertyName` maps `"this[]"` to `"Item"` **unconditionally**, while the C#
generator uses `MetadataName` and therefore honours `[IndexerName]`. For an extension indexer declared
`[IndexerName("MyIndexer")]`, `ISymbol.GetDocumentationCommentId()` gives
``P:E.<G>$HASH.MyIndexer(System.String)`` while `DocumentationCommentId.CreateDeclarationId(symbol)` gives
``P:E.<G>$HASH.Item(System.String)``. **Only the first matches the identifier the compiler writes into the
XML file.**

**Test coverage.** There is **no** SymbolDisplay test and **no** documentation-comment-ID test for unions or
closed classes. Extension-block and extension-**method** ID round-tripping is proved by
`ExtensionTests2.DocumentationCommentId_01/_02/_03` (roslyn #78606); the extension-**indexer** round-trip is
untested and goes through `EncodePropertyName` plus `GetMatchingExtensions`, so the risk there is real
(OQ-12).

### N11-ROSLYN-20 — `SyntaxGenerator` and `DeclarationModifiers`

`DeclarationModifiers` gained **exactly** `IsClosed`, `WithIsClosed` and the static `Closed`. **There is no
`Union` member and no `Safe` member**; `TryParse` accepts `"Closed"` and rejects `"Union"` and `"Safe"`.

`DeclarationModifiers.From(ISymbol)` sets `isAbstract: symbol.IsAbstract && !isClosed`, suppressing the
redundant word, and `CSharpSyntaxGenerator.WithModifiers` drops `IsAbstract` when `IsClosed` is set.
`AsModifierList` emits `ClosedKeyword` after `FixedKeyword` and before the "must be last" `ref` and
`partial`. `s_classModifiers` includes `DeclarationModifiers.Closed`.

**Three gaps that a generator-based tool will hit:**
1. **A union regenerates as a plain `struct`.** `SyntaxGenerator.Declaration(ISymbol)` dispatches on
   `TypeKind.Struct => StructDeclaration(isRecord: false, …)`. There is **no `UnionDeclaration` factory**
   anywhere on `SyntaxGenerator` or `CSharpSyntaxGenerator`. The `union` keyword becomes `struct`, the
   case-type list is gone, the synthesised `Value` property and constructors are **regenerated
   individually** (because `CanBeDeclared`'s implicit-member skip is keyed on `IsRecord`, and a union is not
   a record), the implicit `IUnion` implementation is re-emitted through `type.Interfaces`, and the
   `[Union]` attribute is **lost** (`Declaration` never emits attributes). The result is a plain struct that
   is no longer a union type — **silently, with no exception and no diagnostic**.
2. **`closed record class` silently loses `closed`.** `ClassDeclaration(isRecord: true, …)` sets
   `kind = SyntaxKind.RecordDeclaration`, so the allowed set becomes `s_recordModifiers`, which **omits**
   `DeclarationModifiers.Closed` — even though `public closed record class GateState;` is the speclet's own
   opening example. `Closed` is also unavailable on struct and interface declarations.
3. **An extension block with a null `ExtensionParameter` throws.**
   `TypeKind.Extension when type.ExtensionParameter is { } p => ExtensionBlockDeclaration(...)`; if the
   `when` fails, `Declaration(ISymbol)` throws
   `ArgumentException("Symbol cannot be converted to a declaration")` (pinned by
   `TestExtensionDeclaration_04` for `extension(__arglist)`). `ExtensionBlockDeclaration` is
   **`internal abstract`**, reachable from outside `Microsoft.CodeAnalysis.Workspaces` only through
   `Declaration(ISymbol)`.

An extension **indexer** takes the ordinary `IndexerDeclaration(property)` path and works.
`SyntaxGeneratorTests.cs` has **zero** union tests, no `closed record class` test and no extension-indexer
test; it does pin `TestAddAbstractToClosedClass`, `TestAddPublicToClosedClass`,
`TestAddClosedModifierToAbstractClass`, `TestAddClosedModifierToPublicClass`, `TestClassModifiers2` and
`TestExtensionDeclaration_01..12`.

`CSharpCodeStyleOptions` places `SyntaxKind.ClosedKeyword` in the default `preferred_modifier_order`,
immediately after `AbstractKeyword`. `SyntaxKind.UnionKeyword` is absent, being a type-declaration keyword
rather than a modifier.

### N11-ROSLYN-21 — Interceptors: NO change in the .NET 11 wave

Every interceptor API is in `PublicAPI.Shipped.txt`; `PublicAPI.Unshipped.txt` contains **zero** interceptor
entries. `docs/features/interceptors.md` last changed **2024-12-13**; `InterceptableLocation.cs` last
changed 2025-02-17. `RSEXPERIMENTAL002` (the interceptors experimental identifier) has been **retired** from
`RoslynExperiments.cs`, confirming the API is stable and non-experimental.

Surface:

```csharp
public abstract class Microsoft.CodeAnalysis.CSharp.InterceptableLocation : IEquatable<InterceptableLocation>
{
    private protected InterceptableLocation() { }
    public abstract int Version { get; }
    public abstract string Data { get; }
    public abstract string GetDisplayLocation();
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();
    public abstract bool Equals(InterceptableLocation? other);
}
static InterceptableLocation? CSharpExtensions.GetInterceptableLocation(this SemanticModel? semanticModel,
    InvocationExpressionSyntax! node, CancellationToken cancellationToken = default);
static IMethodSymbol? CSharpExtensions.GetInterceptorMethod(this SemanticModel? semanticModel,
    InvocationExpressionSyntax! node, CancellationToken cancellationToken = default);
static string! CSharpExtensions.GetInterceptsLocationAttributeSyntax(this InterceptableLocation! location);
//   => $"""[global::System.Runtime.CompilerServices.InterceptsLocationAttribute({location.Version}, "{location.Data}")]"""
```

There is **no public constructor and no public subclass**; the only factory is
`SemanticModel.GetInterceptableLocation`, which returns `null` unless the invocation's `Expression` is
`X.Name(...)`, `X?.Name(...)` (member binding) or a bare `Name(...)`. `ptr->M()` is covered (it is a
`MemberAccessExpressionSyntax` of kind `PointerMemberAccessExpression`).

**Version 1 data encoding, base64 (standard alphabet, `=` padding), minimum decoded length 20 bytes:**

| Offset | Size | Content |
|---|---|---|
| 0 | 16 | xxHash128 content checksum of the **whole file** containing the intercepted call |
| 16 | 4 | `int32`, **little-endian**, `SyntaxNode.Position` of the simple-name syntax |
| 20 | rest | UTF-8 bytes of `Path.GetFileName(path)` — a display name, used only for diagnostics |

**There is no version 2**, confirmed three ways: `if (version != 1)` → `CS9232`; CS9232's resource string
hardcodes "The latest supported version is '1'."; and the workspaces decoder carries the placeholder
comment "Add more supported versions here in the future if the compiler adds any."

**The load-bearing fact: the compiler resolves the target file purely by content checksum — the path is
never used for matching.** `CSharpCompilation.GetSyntaxTreesByContentHash` builds a dictionary keyed on
`tree.GetText().GetContentHash()` over **all** `SyntaxTrees`; two trees with identical text make every
interceptor targeting either fail with `CS9233`; zero matches gives **`CS9234` "Cannot intercept a call in
file '{0}' because a matching file was not found in the compilation"**. `SourceText.GetContentHash()` is
xxHash128 over the UTF-16 code units forced to little-endian — **not** over the on-disk bytes, and
unaffected by encoding, BOM or `SourceHashAlgorithm`. Its XML documentation warns: "**Different versions of
Roslyn may produce different content hashes.**"

Interceptable call sites: **only ordinary member methods** (`MethodKind.Ordinary`), including within a
conditional access `receiver?.M()` and a pointer member access `ptr->M()`. **Not** interceptable
(`CS9207`): delegate invocation, delegate-typed field invocation, local functions, function pointers,
properties. **Not reachable at all** (they are not `InvocationExpressionSyntax`): property access, indexer
access, object creation, operators (including C# 15 extension operators), and `nameof` (`CS9160`).
The set widened **once**, in the .NET 10 wave (roslyn PR #79010, merged 2025-06-26), to include methods
declared in C# 14 `extension` blocks, on both sides. **It did not widen in the .NET 11 wave.** Extension
properties, extension indexers and extension operators are **not** interceptable.

`InterceptorsNamespaces` opt-in is **still required, unconditionally**, and is checked before file
resolution. `Feature.InterceptorsNamespaces = "InterceptorsNamespaces"` is the only compiler feature name;
`InterceptorsPreviewNamespaces` is an MSBuild-only alias (`Csc.AddInterceptorsNamespaces` concatenates them
as `$(InterceptorsNamespaces);$(InterceptorsPreviewNamespaces)`), and `<Features>InterceptorsPreview</Features>`
is obsolete with no effect. Matching is a **namespace prefix** match; the single segment **`global`** is a
wildcard. An interceptor in the global namespace is always `CS9206`. The .NET 11 SDK pre-enables three
namespaces for file-based programs: `Microsoft.AspNetCore.Http.Generated`,
`Microsoft.Extensions.Configuration.Binder.SourceGeneration`, `Microsoft.Extensions.Validation.Generated`.

`docs/features/interceptors.md` is stale: it does not mention extension-block interception and still carries
a "before releasing .NET 8" TODO. The learn.microsoft.com `source-generator-errors` page is also stale (it
still advertises `<Features>InterceptorsPreview</Features>` and lists the retired CS9145).

**Interaction with the new features is unspecified everywhere**: zero occurrences of "intercept" in the
runtime-async design document, in `unsafe-evolution.md` and in the .NET 11 compiler breaking-changes
document; zero occurrences of "async" in `InterceptorsTests.cs`. See OQ-13.

### N11-ROSLYN-22 — `#line`, `#pragma checksum` and generated-code detection: NO change

**`#line` in all its forms is byte-identical between Roslyn 5.0 and 5.12.** `SyntaxTree.GetLineSpan`,
`GetMappedLineSpan`, `GetLineMappings`, `GetLineVisibility`, `GetMappedLineSpanAndVisibility`,
`HasHiddenRegions`, `IsHiddenPosition`, `FileLinePositionSpan`, `LineDirectiveMap`,
`CSharpLineDirectiveMap` — all unchanged in signature and implementation. The only edit anywhere is a
culture-invariance fix in `LineMapping.ToString()` (PR #80800). The span form
(`#line (1, 1) - (5, 60) 10 "partial-class.cs"`) and its diagnostics CS8938 / CS8939 / CS9028 are
unchanged. `/embed` still follows only `SyntaxKind.LineDirectiveTrivia` and **still ignores `#line` span
directives**.

**`#pragma checksum` is unchanged and has always accepted an arbitrary algorithm GUID**, since
`ParsePragmaDirective` validates only that the three tokens are string literals, that
`Guid.TryParse` succeeds and that the byte string is even-length hex. There is therefore no new SHA-384 /
SHA-512 "identifier" for it. `CSharpCompilation.AddDebugSourceDocumentsForChecksumDirectives` is
byte-identical, including `WRN_ConflictingChecksum` (CS1697) and `WRN_IllegalPPChecksum` (CS1695).

**Generated-code detection is byte-identical.** `GeneratedCodeUtilities.cs` last changed 2024-06-28. The
decision order is (1) the EditorConfig key **`generated_code`** (`true`/`false` via `bool.TryParse`;
anything else falls through) → (2) the file-name heuristic → (3) the `<auto-generated>` header.

The file-name heuristic: `TemporaryGeneratedFile_*` as a case-insensitive **prefix** with any or no
extension; and, **only when the file has some extension**, the case-insensitive suffixes `.designer`,
`.generated`, `.g`, `.g.i` applied to the name with its final extension stripped. So `Foo.g.cs` matches and
bare `Foo.g` does not; the extension need not be `.cs`.

The `<auto-generated>` rule: **there is no line count.** Roslyn enumerates the **leading trivia of the root
node** and tests every trivium for which `isComment` returns true; a qualifying comment 500 blank lines in
still matches, and a comment after the first real token never does. The text must **contain**
(`string.Contains`, ordinal, **case-sensitive**) either `<autogenerated` or `<auto-generated` — no closing
bracket required. `isComment` is `SingleLineCommentTrivia || MultiLineCommentTrivia`, so
**`/// <auto-generated/>` does NOT mark the file as generated; `// <auto-generated/>` does.**

`System.CodeDom.Compiler.GeneratedCodeAttribute` drives the symbol-level rule: the check **walks up
containing symbols**, a symbol with **more than one `DeclaringSyntaxReference`** (a partial split across
files) is deliberately **never** treated as generated by attribute, and namespaces are excluded from the
attribute test. Independently, a symbol is generated if **every** declaring location is in generated **or
hidden** code — which is the direct link between `#line hidden` and generated-code detection.
**`System.Runtime.CompilerServices.CompilerGeneratedAttribute` plays no part whatsoever.**

`DefaultGeneratedCodeAnalysisFlags` is unchanged (`Analyze | ReportDiagnostics`), so an analyzer that never
calls `ConfigureGeneratedCodeAnalysis` **does run on and report on generated code**.

**Source-generator output is NOT automatically treated as generated code**, and this is unchanged. There is
no flag on a `SyntaxTree` saying a generator made it. `csc` does put generator trees into the
analyzer-config dictionary by matching their synthetic paths against the `.editorconfig` set, so
`generated_code = true` **can** be set for them by a glob, but nothing sets it by default. In practice most
generator output is detected only incidentally, because hint names conventionally end in `.g.cs` or the
generator emits an `// <auto-generated/>` header. **A generator whose hint name is `Foo.cs` and whose output
has no `<auto-generated>` comment produces a tree that analyzers treat as ordinary user code** — including
pre-compilation-phase output, which receives no special marking.

### N11-ROSLYN-23 — Debug information: Portable PDB and Edit-and-Continue

**Portable PDB format: exactly one change** — two new source-hash-algorithm GUIDs in the Document table
(0x30) `HashAlgorithm` column, added by dotnet/runtime PR #124573 (merged 2026-02-18):

| Algorithm | GUID |
|---|---|
| SHA-1 | `ff1816ec-aa5e-4d10-87f7-6f4963833460` |
| SHA-256 | `8829d00f-11b8-4213-878b-770e8597ac16` |
| **SHA-384** | `d99cfeb1-8c43-444a-8a6c-b61269d2a0bf` |
| **SHA-512** | `ef2d1afc-6550-46d6-b14b-d70afe9a5566` |

`PortablePdb-Metadata.md` has had **no other commit since 2025-03-31**. **Roslyn's set of emitted
custom-debug-information kinds is unchanged** (`PortableCustomDebugInfoKinds.cs` last semantic addition was
`PrimaryConstructorInformationBlob` in 2023). `CompilationOptionsSchemaVersion` is still `2` and the
`CompilationOptionNames` key list is unchanged — note that **the source checksum algorithm is not recorded
in the Compilation Options CDI**; it is per-document in the Document table.

**`SourceHashAlgorithm.Sha384 = 3` and `Sha512 = 4`** (Roslyn PR #82452, 2026-02-25) are selectable from
three surfaces: `csc /checksumalgorithm:sha384|sha512` (help text now "SHA1, SHA256 (default), SHA384, or
SHA512."), the MSBuild `ChecksumAlgorithm` property, and `SourceText.From(..., checksumAlgorithm:)`.
`CommandLineParser.TryParseHashAlgorithmName` was **deleted** and replaced by
`SourceHashAlgorithms.TryParseAlgorithmName`. **The default is still SHA-256**
(`SourceHashAlgorithms.Default`), and the .NET 11 SDK does not set `ChecksumAlgorithm` anywhere.
**`EmitOptions.PdbChecksumAlgorithm` stays `HashAlgorithmName.SHA256`** regardless — that is the hash of
the PDB *content* in the PE debug directory, a different thing.

**Generated `SourceText`s now inherit the command-line checksum algorithm** (PR #81934 / #81992). New
internal `GeneratorDriverOptions.ChecksumAlgorithm`; `SourceProductionContext`,
`GeneratorExecutionContext` and `PreCompilationSourceProductionContext` all force it onto every
`AddSource`, via a new internal `SourceText.WithChecksumAlgorithmIfAny` (one note calls it
`WithChecksumAlgorithm`) backed by a new `SourceText.SourceTextWithAlgorithm` wrapper. This matters because
`SourceText.From(...)` still defaults to **SHA-1**, so generated files previously recorded a SHA-1 PDB
document checksum while user files used SHA-256.

**Edit-and-Continue:**
- **New public API `EmitDifferenceOptions.MethodImplEntriesSupported`** (default `true`, PR #81304, merged
  2025-12-03). Maps from `EditAndContinueCapabilities.AddExplicitInterfaceImplementation (1 << 10)`.
  Violation gives **CS9346**, a build-only diagnostic. **CoreCLR does not advertise
  `AddExplicitInterfaceImplementation`** (`MetadataUpdater.GetCapabilities()` omits it), so on the CoreCLR
  hot-reload path (`dotnet watch`) `MethodImplEntriesSupported` is **false**; the Visual Studio managed
  debugger supplies its own capability list, which is where the flag comes from.
- **No new `EditAndContinueCapabilities` member** in this wave; the newest is `AddFieldRva = 1 << 11`
  (2025-05-10, .NET 10 wave).
- **No new `RudeEditKind`**; the highest is still `InsertOrMoveComInterfaceMember = 120`.
- **One new general EnC diagnostic: ENC1009** `UpdatingUnsupportedProject` (PR #82225, 2026-02-10),
  "Detect changes in non-Roslyn projects and report them as rude edits", so `dotnet-watch` can auto-restart
  when a change is made in a project whose compiler does not support Hot Reload (for example F#).
- `EditAndContinueMethodDebugInfoReader` was **renamed** `EditAndContinueDebugInfoReader`; EnC now reads
  `default-encoding` / `fallback-encoding` out of the PDB when materialising the committed document text
  (PR #81912). `ChecksumAlgorithm` is **not** tracked as a `ProjectSettingKind`.
- **EnC for runtime async is not implemented** (roslyn #77954, open, empty body, no milestone). There are
  **zero** runtime-async tests under `Test/Emit2/PDB` or `Test/Emit2/Emit/EditAndContinue`.
- **Unions have no EnC or debugging item at all** in their test plan; `CSharpEditAndContinueAnalyzer.cs`
  contains **no union handling** (it does handle `SyntaxKind.ExtensionBlockDeclaration`). Extension
  indexers' "Check that EnC is blocked" is unchecked.
- `docs/wiki/EnC-Supported-Edits.md` is still titled ".NET 8" and has not been refreshed.

**No change to `/pathmap`, determinism, `EmbeddedSource`, SourceLink defaults or debug directory entries.**
One determinism-adjacent change: the `DeterministicKeyBuilder` now includes SourceLink content and embedded
resources (PR #81629).

### N11-ROSLYN-24 — `System.Reflection.Metadata`: no public API change

Last commit to `src/libraries/System.Reflection.Metadata/ref/System.Reflection.Metadata.cs` is
**2025-07-01** (PR #116839, a .NET 10 change). No .NET 11 public API change to `MetadataReader`,
`MetadataBuilder`, `PEReader`, `PEBuilder`, `ManagedPEBuilder`, `BlobBuilder`, `BlobEncoder` or any
signature encoder. SRM does not validate `MethodImplAttributes` or `TypeAttributes` bits — both are straight
casts of the raw integer — so it **reads and writes the new flags correctly on every target framework,
including `netstandard2.0`**; only the *named* enum member requires .NET 11.

Five behavioural fixes matter to anything that writes IL or metadata:
`#128279` (2026-06-15) **incorrect operand size for long-form `ldloc`/`stloc`/`ldarg`/`starg` in
`InstructionEncoder`**; `#127262` (2026-04-27) **branch fixup skipping bytes at `BlobBuilder` chunk
boundaries**; `#127246` (2026-04-24) preserve the pre-linked suffix chain when linking into an empty
`BlobBuilder`; `#126924` (2026-04-19) **`BlobBuilder.ReserveBytes` could return uninitialised bytes**;
`#115268` (2025-12-01) `MetadataAggregator` cumulative-sum fix for the GUID-heap offset (EnC deltas).

### N11-ROSLYN-25 — Reference assemblies: rules unchanged, and what survives

The ref-assembly rules have **not changed since C# 7.1**; `docs/features/refout.md`'s last substantive edit
was 2021-06-17. The single filter is `Microsoft.Cci.Extensions.ShouldInclude(ITypeDefinitionMember,
EmitContext)`. `EmitContext.IsRefAssembly => MetadataOnly && !IncludePrivateMembers`.
**`ProduceReferenceAssembly` is defaulted to `true` by the .NET SDK for `.NETCoreApp >= 5.0` C#/VB
projects**, so every ordinary .NET project already goes down this path.

Kept: all types including private and nested; **all attributes, even internal ones, and their internal
constructors**; all virtual methods; explicit interface implementations; all fields of a struct; all
constructors of an attribute type. Dropped: private function members; internal function members unless the
assembly has any `InternalsVisibleTo`; anonymous types; manifest resources; references needed only by
implementation details. Method bodies become a **single shared `ldnull; throw` blob** (`TinyFormat`,
2 bytes of code) — so a ref-assembly body **contains no `ret` at all**.

The complete list of places `MetadataOnly` / `IsRefAssembly` changes emission: `SerializeMethodBodies` →
`SerializeThrowNullMethodBodies`; `GetAnonymousTypeDefinitions` returns empty; `GetResources` returns empty;
`PopulateCustomAttributeTableRows` passes `IsRefAssembly` into `GetSourceAssemblyAttributes` (adding
`ReferenceAssemblyAttribute`); and `MethodCompiler` skips body compilation. **Custom attributes,
`MethodImplAttributes`, `TypeAttributes`, interface implementations, generic constraints and
`MethodSemantics` rows all go through the identical code path for reference and implementation assemblies.**

Survival of the new artifacts:

| Artifact | Survives `/refonly` and `/refout`? |
|---|---|
| `MethodImplAttributes.Async` (0x2000) | **Yes, verbatim** — `implAttributes` is written unconditionally. The paradox is harmless: the body is `ldnull; throw`, so no `ret` exists to contradict the convention |
| `IsClosedTypeAttribute` including the `DerivedTypes` named argument | **Yes, full fidelity** |
| `[CompilerFeatureRequired("ClosedClasses")]` | **Yes**, whenever the constructor itself survives (a private constructor of a non-attribute type is dropped, taking the attribute with it — benign) |
| `UnionAttribute`, `IUnion` implementation, union constructors, `Value` property | **Yes** — all are public; the private backing field survives because the lowered type is a **struct** and all struct fields are kept. A hand-written `[Union] class` would lose its private backing field, harmlessly |
| `RequiresUnsafeAttribute` on a member | **Yes**, whenever the member survives; embedded copies are kept because ref assemblies keep all types and attributes |
| module-level `MemorySafetyRulesAttribute` | **Yes** — module attributes are guarded only by `IsFullMetadata`, never by `MetadataOnly` |
| `ExtendedLayoutAttribute` and `TypeAttributes.ExtendedLayout` | **Yes** |
| extension grouping type, marker type, marker method | **Always, unconditionally** — the marker method is emitted **without calling `ShouldInclude`**, even when its visibility would fail the filter |
| `[Extension]`, `[DefaultMember("Item")]` on the grouping type | **Always** |
| skeleton `Item` property and its skeleton accessors | **Iff visible** — a `public` extension indexer survives, an `internal` one without IVT does not |
| static `get_Item` / `set_Item` implementation methods on the enclosing class | **Iff visible** |
| `implAttributes` of any member inside an extension block | **Always zeroed** — `MethodSymbolAdapter.GetImplementationAttributes` returns `default` when `ContainingType.IsExtension`, so the `Async` bit (and `AggressiveInlining`, `NoInlining`, …) never appears on an extension skeleton member, only on its implementation method |

**For a `public` extension indexer, both the skeleton property and the static implementation methods survive,
so it is fully consumable from a reference-assembly-only reference.** They are dropped together, never one
without the other.

Grouping-type type parameters are **renamed** to `$T0`, `$T1`, … with all constraint attributes dropped
except a synthesised `IsUnmanagedAttribute`.

### N11-ROSLYN-26 — `RSEXPERIMENTAL` inventory and stabilisation timeline

```csharp
internal const string NullableDisabledSemanticModel = "RSEXPERIMENTAL001";
internal const string GeneratorHostOutputs          = "RSEXPERIMENTAL004";
internal const string PreviewLanguageFeatureApi     = "RSEXPERIMENTAL006";  // per-API UrlFormat
internal const string PreCompilationSourceOutput    = "RSEXPERIMENTAL007";
// Previously taken: RSEXPERIMENTAL003 (SyntaxTokenParser), RSEXPERIMENTAL005 (IgnoredDirectiveTrivia)
// RSEXPERIMENTAL002 (interceptors) has been retired.
```

Y = plain public API; X = public but `[Experimental("RSEXPERIMENTAL006")]`; `-` = absent.

| API | 5.0 | 5.3 | 5.6 | 5.9 | 5.10 | 5.11 | 5.12 |
|---|---|---|---|---|---|---|---|
| `LanguageVersion.CSharp15` | - | - | - | - | - | Y | Y |
| `SyntaxKind.UnionDeclaration`, `UnionDeclarationSyntax` | - | - | X | X | X | Y | Y |
| `SyntaxKind.WithElement`, `WithElementSyntax` | - | - | X | X | X | Y | Y |
| `ICollectionExpressionOperation.ConstructArguments` | - | - | X | X | X | Y | Y |
| `OperationKind.CollectionExpressionElementsPlaceholder` | - | - | X | X | X | Y | Y |
| `ITypeSymbol.IsUnion`, `UnionCaseTypes` | - | - | - | X | X | Y | Y |
| `ITypeSymbol.IsClosed`, `GetClosedDerivedTypeInfo`, `ClosedDerivedTypeInfo` | - | - | - | X | X | Y | Y |
| `BreakStatementSyntax.Name`, `ContinueStatementSyntax.Name` | - | - | - | X | X | Y | Y |
| `IMethodSymbol`/`IPropertySymbol.ReduceExtensionMember` | - | Y | Y | Y | Y | Y | Y |
| `ForEachStatementInfo.MoveNextAwaitableInfo`/`DisposeAwaitableInfo` | - | Y | Y | Y | Y | Y | Y |
| `EmitDifferenceOptions.MethodImplEntriesSupported` | - | Y | Y | Y | Y | Y | Y |
| `SourceHashAlgorithm.Sha384`/`Sha512` | - | - | Y | Y | Y | Y | Y |
| `IncrementalGeneratorOutputKind.PreCompilation`, `PreCompilationSourceProductionContext` | - | - | - | Y* | Y* | Y* | Y* |
| `INamedTypeSymbol.TypeLayout`, `Microsoft.CodeAnalysis.TypeLayout` | - | - | - | - | - | Y | Y |
| `CSharpExtensions.GetValueConversion(ICoalesceOperation)` | - | - | - | - | - | - | Y |
| `SyntaxKind.UnsafeExpression`, `UnsafeExpressionSyntax`, `SafeKeyword` | - | - | - | X | X | X | X |
| `ISymbol.RequiresUnsafeContext`, `IModuleSymbol.MemorySafetyRulesVersion` | - | - | - | - | - | X | X |
| `MemorySafetyRulesVersion`, `CSharpCompilationOptions.WithMemorySafetyRulesVersion` | - | - | - | - | - | - | X |

(*) the enum value and the constant are plain public API; the registration methods and the context struct
carry `RSEXPERIMENTAL007`.

**Reading: everything a C# 15 consumer needs is stable, non-experimental public API in Roslyn 5.11 and
5.12. The memory-safety surface is the only part still experimental, and it belongs to `LangVersion=preview`,
not to C# 15.**

### N11-ROSLYN-27 — Removed or changed-signature public API (breaking for a Roslyn consumer)

1. **Eleven analyzer-registration members changed to `params ImmutableArray<T>`** (Roslyn 5.9):
   `AnalysisContext.RegisterSymbolAction`, `RegisterSyntaxNodeAction<T>`, `RegisterOperationAction`;
   the `CompilationStartAnalysisContext` equivalents; `CodeBlockStartAnalysisContext<T>.RegisterSyntaxNodeAction`;
   `OperationBlockStartAnalysisContext.RegisterOperationAction`;
   `SymbolStartAnalysisContext.RegisterOperationAction` and `RegisterSyntaxNodeAction<T>`;
   `AssemblyMetadata.Create(ImmutableArray<ModuleMetadata>)`. The parameter **type** did not change, so
   this is not a binary break and existing call sites still compile.
2. **`default(Conversion)` boolean properties now return `false`** (Roslyn 5.11, PR #84628):
   `default(Conversion).Exists` and `.IsExplicit` were `true`, now `false`. **This is the only entry in
   `docs/Breaking API Changes.md` for the entire 5.x line.**
3. **`SyntaxFactory.Parameter` now throws** `ArgumentException` when both `Type` and `Identifier` are
   missing (N11-ROSLYN-07).
4. **`SyntaxFacts.GetTypeDeclarationKind(SyntaxKind.ExtensionKeyword)`** returns
   `ExtensionBlockDeclaration` instead of `SyntaxKind.None` (N11-ROSLYN-09).
5. **Interface members added**: `ITypeSymbol` (4), `ISymbol` (1), `IModuleSymbol` (1), `IMethodSymbol` (1),
   `IPropertySymbol` (1), `INamedTypeSymbol` (1), `ICollectionExpressionOperation` (1). Roslyn's symbol and
   operation interfaces are documented as not implementable outside Roslyn, but any external
   implementation breaks.
6. **`BuildParameters.IsLongLivedHost` and `MarkProcessAsLongLivedHost()` were REMOVED** from
   `Microsoft.Build` — see N11-SDK-09.
7. **Package target frameworks dropped** — see N11-ROSLYN-28.

Two entries that *look* like changes in a `PublicAPI` diff but are not:
`[RSEXPERIMENTAL001]SemanticModel.NullableAnalysisIsDisabled` and
`[RSEXPERIMENTAL004]GeneratorRunResult.HostOutputs` appear "removed then re-added with a prefix" only
because the PublicApiAnalyzer started recording the experimental identifier in the entry text from Roslyn 5.6.

### N11-ROSLYN-28 — Roslyn package target frameworks: `net9.0` and `net8.0` assets dropped

| Package version | Target frameworks in the nupkg |
|---|---|
| Microsoft.CodeAnalysis.CSharp **5.0.0** | `net8.0`, `net9.0`, `.NETStandard2.0` |
| **5.3.0** | `net8.0`, `net9.0`, `net10.0`, `.NETStandard2.0` |
| **5.6.0** | `net10.0`, `net8.0`, `.NETStandard2.0` (**net9.0 dropped**) |
| **5.9.0** | `net10.0`, `.NETStandard2.0` (**net8.0 dropped**) |

**This is the most consequential packaging change in the 5.x line.** From Roslyn 5.9 onward a consumer
targeting `net8.0` or `net9.0` no longer gets a matching .NET Core asset and falls back to the
`netstandard2.0` asset, which drags in `System.Memory`, `System.Runtime.CompilerServices.Unsafe`,
`System.Buffers`, `System.Numerics.Vectors`, `System.Text.Encoding.CodePages`,
`System.Threading.Tasks.Extensions`, `System.Collections.Immutable` and `System.Reflection.Metadata`.

Minimum runtime for Roslyn 5.12: **.NET Core `net10.0`** (nothing lower is shipped except the
`netstandard2.0` asset) and **.NET Framework `net472`** for the desktop `csc.exe`/`vbc.exe` and
`Microsoft.Net.Compilers.Toolset.Framework`. The `MSBuildWorkspace` BuildHost stays on **`net8.0`** "until
.NET 8 EOL in November 2026". Visual Studio's private runtime and VS Code DevKit are now **`net10.0`** (they
were `net8.0` in Roslyn 5.0).

`netstandard2.0` dependency versions moved: `System.Collections.Immutable` and
`System.Reflection.Metadata` 9.0.0 → **10.0.1**; `System.Memory` 4.6.0 → 4.6.3;
`System.Runtime.CompilerServices.Unsafe` 6.1.0 → 6.1.2; `System.Buffers` 4.6.0 → 4.6.1;
`System.Numerics.Vectors` 4.6.0 → 4.6.1. `Microsoft.CodeAnalysis.Common` 5.9.0 depends on
`Microsoft.CodeAnalysis.Analyzers` **`5.9.0-1.26328.17`** (a prerelease versioned in lockstep with Roslyn)
instead of the old stable `3.11.0`.

**As of 2026-09-03, Roslyn's own components still target `net10.0`, not `net11.0`**
(`NetRoslyn = NetRoslynAll = NetVS = NetVSCode = net10.0`), running on the .NET 11 runtime by roll-forward.
See OQ-14.
---

## 3. Compiler breaking changes

Source of truth: `dotnet/roslyn` `docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`
(newest commit `1284a4a`, 2026-08-11, "Add C# 15 language version (#84799)"), rendered at
<https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2011>
(`updated_at` 2026-08-13). Scope: "known breaking changes in Roslyn after .NET 10 general release
(.NET SDK version 10.0.100) through .NET 11 general release (.NET SDK version 11.0.100)".
**Twelve entries.** No entry mentions debug information, sequence points, PDBs, Edit and Continue, Hot
Reload, checksums, `/pathmap`, determinism, reference assemblies, interceptors or source generators.

### RES-05 — What "C# 16" means in that document

Entries 1–7, 11 and 12 are labelled **C# 14 vs C# 15**; entries 8, 9 and 10 (all unsafe-evolution) are
labelled **"C# 16"** and `langversion:16`. There is **no `LanguageVersion.CSharp16`** and
`CurrentVersion == CSharp15`. Reconstruction: before PR #84799 the C# 15 feature set was reachable only
through `<LangVersion>preview</LangVersion>`; when `CSharp15` became a concrete value, the features that
were **not** being finalised for C# 15 (the unsafe-evolution set) were re-labelled "C# 16" in prose,
meaning *the next, still-unnamed language version reachable only through `LangVersion=preview`*. The
`MessageID.cs` mapping is authoritative and agrees.

**Practical reading for .NET 11 GA: entries 1–7, 11 and 12 are live at `LangVersion 15` / `latest` /
default. Entries 8, 9 and 10 are gated behind `LangVersion=preview` and do not apply at the GA default.**
(learn.microsoft.com's `unsafe-code-errors` page muddies this by saying CS9360–CS9363 apply "Under C# 15's
updated memory safety rules"; see OQ-03.)

### N11-BREAK-01 — Safe-context of a `Span`/`ReadOnlySpan` collection expression is now *declaration-block*

*Introduced in VS 2026 18.3.* Previously *function-member*. Presented as an unconditional conformance fix;
the document does not state whether it is language-version gated (OQ-04). No new diagnostic identifier;
the existing ref-safety escape errors (the CS8352 / CS8347 family) now fire where they did not.

```cs
scoped Span<int> items1 = default;
scoped Span<int> items2 = default;
foreach (var x in new[] { 1, 2 })
{
    Span<int> items = [x];
    if (x == 1) items1 = items; // previously allowed, now an error
    if (x == 2) items2 = items; // previously allowed, now an error
}
```

Workarounds: use `int[] items = [x];` (an array converts to `Span<int>`), or hoist the collection
expression out of the loop. Tracking: <https://github.com/dotnet/csharplang/issues/9750>.

### N11-BREAK-02 — Synthesised `ref readonly`-returning delegates require `System.Runtime.InteropServices.InAttribute`

*Introduced in VS 2026 18.3.* Diagnostic **CS0518** "Predefined type
'System.Runtime.InteropServices.InAttribute' is not defined or imported". The compiler now emits
`modreq(InAttribute)` correctly on the return of synthesised delegate types (commit `64219bb`).

```cs
var d = this.MethodWithRefReadonlyReturn;
var d = ref readonly int () => ref x;
```

**Who this hits:** minimal reference assemblies, `NoStandardLib` test compilations, and any tool that builds
a `Compilation` from a hand-rolled or trimmed `MetadataReference` set. `InAttribute` is present in
`mscorlib`, `netstandard.dll` and every `System.Runtime`, so ordinary multi-targeting is unaffected.
Because it is a `CS0518` predefined-type lookup, **declaring the type in source satisfies it**.

### N11-BREAK-03 — `ref readonly` local functions require `InAttribute`

*Introduced in VS 2026 18.3.* Same **CS0518**, same mitigation.

```cs
void Method() { ref readonly int local() => ref x; }
```

### N11-BREAK-04 — Dynamic `&&` / `||` with an interface-typed left operand is now an error

*Introduced in VS 2026 18.3.* Diagnostic **CS7083** "Expression must be implicitly convertible to Boolean
or its type 'I1' must not be an interface and must define operator 'false'." Previously the code compiled
and threw `RuntimeBinderException` at run time, because the runtime binder cannot invoke operators declared
on interfaces. A runtime failure moved to compile time.

```cs
I1 x = new C1(); dynamic y = new C1();
_ = x && y;          // error CS7083
_ = (C1)x && y;      // valid
_ = (dynamic)x && y; // valid
```

Tracking: <https://github.com/dotnet/roslyn/issues/80954>.

### N11-BREAK-05 — `nameof(this.X)` / `nameof(base.X)` inside an attribute is now disallowed

*Introduced in VS 2026 18.3 **and serviced into .NET 10.0.200**.* Removal of an unintended permissiveness
present since C# 12. The document names no diagnostic identifier; the CS0026 / CS0027 family is expected
but **unverified** (OQ-15).

```cs
class C
{
    string P;
    [System.Obsolete(nameof(this.P))] // now disallowed
    [System.Obsolete(nameof(P))]      // workaround
    void M() { }
}
```

PR <https://github.com/dotnet/roslyn/pull/81628>, issue <https://github.com/dotnet/roslyn/issues/82251>.

### N11-BREAK-06 — Parsing of `with` inside a switch-expression arm

*Introduced in VS 2026 18.4.* **Parser / syntax-tree-shape change with no diagnostic: the same text
produces a different tree.**

Given `x switch { (X.Y) when … }`: `(X.Y)when` was parsed as a **cast expression** casting the contextual
identifier `when` to the type `(X.Y)`; it is now parsed as a **constant pattern** `(X.Y)` followed by a
**`when` clause**. A node that used to come back as `CastExpressionSyntax`
(`SyntaxKind.CastExpression`) in this position now comes back as `ConstantPatternSyntax` inside a
`SwitchExpressionArmSyntax` with a non-null `WhenClauseSyntax`. Consequence: a plain guard such as
`(X.Y) when a > b =>` now parses correctly, where before it did not.

Issue <https://github.com/dotnet/roslyn/issues/81837>, PR <https://github.com/dotnet/roslyn/pull/81863>,
commit `fc2b820` (2026-01-13). Language-version gating is not stated (OQ-04).

### N11-BREAK-07 — `with(...)` as a collection-expression element binds as construction arguments

*Introduced in VS 2026 18.4.* **Explicitly language-version gated: "when the LangVersion is set to 15 or
greater".**

```cs
object x, y, z = ...;
object[] items;
items = [with(x, y), z];  // C# 14: call to the with() method; C# 15: error, args not supported for object[]
items = [@with(x, y), z]; // call to the with() method
object with(object a, object b) { ... }
```

LDM-2025-03-17 resolved to "Keep previous behavior (no breaking change) when compiling with earlier
language version." **But as implemented on `main`, the parse is unconditional** and only the binder reports
the language-version diagnostic; no code path re-binds `with(` as an invocation at `LangVersion 12/13/14`
was located. The speclet still carries this as an open concern. See OQ-02.

### N11-BREAK-08 — Pointer types no longer require an unsafe context (overload-resolution break)

*Introduced in VS 2026 18.7.* **Preview-gated** ("C# 16" ⇒ `LangVersion=preview`). Because pointer types
become legal in safe contexts, overload resolution considers candidates it previously excluded, producing
new **CS0121** ambiguities:

```cs
M(x => { }); // C# 15: prints "2"; preview: error CS0121 (ambiguous)
static void M(F1 f) { Console.WriteLine(1); }
static void M(F2 f) { Console.WriteLine(2); }
unsafe delegate void F1(int* x);
delegate void F2(int x);
// Mitigation: M((int x) => { });
```

### N11-BREAK-09 — `safe` is a contextual keyword on member declarations

*Introduced in VS 2026 18.9.* **Preview-gated.** A type named `safe` no longer resolves in
member-declaration position. Workaround `@safe`.

```cs
class safe { }
class C
{
    safe M1() => new safe();  // previously refers to the type, now a keyword
    @safe M2() => new safe(); // workaround
}
```

Note `safe` is all-lowercase ASCII, so `class safe { }` already produced **CS8981** (warning wave 7).

### N11-BREAK-10 — `unsafe` required for more members (CS9363)

*Introduced in VS 2026 18.9.* **Preview-gated** (`langversion:16`). The compat-mode extension to legacy
callers. Diagnostic **CS9363** "'{0}' must be used in an unsafe context because it has pointers in its
signature."

```cs
var c = new C();
int a = c.M(null); // error always
int b = c[null];   // no error in C# 15, reports CS9363 in preview
class C
{
    public unsafe int M(int* x) => 0;
    public unsafe int this[int* x] => 0;
}
// Fix: int b = unsafe(c[null]);
```

Companion diagnostics from the same feature: CS9360, CS9361, CS9362, CS9376 (see N11-LANG-13).

### N11-BREAK-11 — `closed` is a contextual keyword in type-declaration contexts

*Introduced in VS 2026 18.10.* **C# 15 — live at the GA default `LangVersion`.** Two distinct effects:

1. **Declaring** a type or alias named `closed` without `@` is an error: **CS9380**.
2. **Using** `closed` as a type name in a *member-declaration* position now parses as a modifier, so the
   remainder becomes an incomplete declaration: **CS1519** "Invalid token in class, record, struct, or
   interface member declaration".

```cs
class @closed { }
class C
{
    closed oldField;      // C# 14: field of type 'closed'; C# 15: parsed as an incomplete declaration
    @closed currentField; // field of type 'closed'
}
```

`closed` is all-lowercase ASCII, so `class closed { }` already produced **CS8981** (warning wave 7).

### N11-BREAK-12 — `union` is a contextual keyword in type-declaration contexts

*Introduced in VS 2026 18.10.* **C# 15 — live at the GA default `LangVersion`.** `union` followed by a type
name is now parsed as the start of a union declaration, so `union OldField;` parses as a union named
`OldField` with an *empty* case-type list, giving **CS9370** rather than declaring a field.

```cs
class @union { }
class C
{
    union OldField;      // C# 14: field of type 'union'; C# 15: union declaration ⇒ CS9370
    @union CurrentField; // field of type 'union'
}
```

**Note the asymmetry with `closed`:** `union` is **not** banned as a type name (there is no equivalent of
CS9380 for `union`); only the *use* position breaks. Both are mitigated with `@`, and both were pre-announced
by CS8981 years in advance.

### N11-BREAK-13 — Syntax diagnostic offsets are now relative to `Start`, not `FullStart` (undocumented)

PR <https://github.com/dotnet/roslyn/pull/80393>, commit `88ac9452` (2025-09-24). Described by its author as
both a bug fix and a conceptual clarification: it "fixes up several cases where we were placing diagnostics
in incorrect locations, especially in nested parsing scenarios (like parsing in xml doc comments)".

Systemic changes: `AbstractLexer.GetErrors(int leadingTriviaWidth)` became `GetErrors()` and the leading-trivia
fix-up loop was deleted; `Lexer.GetFullWidth(SyntaxListBuilder?)` was deleted; `SyntaxTreeDiagnosticEnumerator`
was rewritten from a mutable struct enumerator into a static class with an
`IEnumerable<Diagnostic> EnumerateDiagnostics(SyntaxTree, GreenNode root, int position)` iterator, whose
adjustment inverted from "roll back leading trivia for tokens" to "add leading trivia width for non-tokens".
`CSharpSyntaxTree.EnumerateDiagnostics(GreenNode, int)` was removed.

**The reported `TextSpan` of some parser and lexer diagnostics therefore differs between Roslyn 5.0 and
5.12 — including `ERR_ErrorDirective` (CS1029) and `WRN_WarningDirective` (CS1030). This is not listed in
the .NET 11 compiler breaking-changes document.** See OQ-16.

### N11-BREAK-14 — `#error version` output gained a third argument (undocumented)

PR <https://github.com/dotnet/roslyn/pull/80894>, commit `3b8c4322` (2025-10-27). **CS8304**
`ERR_CompilerAndLanguageVersion`:

| Branch | Message |
|---|---|
| Roslyn 5.0 | `Compiler version: '{0}'. Language version: {1}.` |
| Roslyn 5.12 | `Compiler version: '{0}'. Language version: {1}. Compiler path: '{2}'.` |

Supporting change: `CommonCompiler.ExtractShortCommitHash` and `GetShortCommitHash` were **deleted**, so
`GetProductVersion` now uses the **full** `CommitHashAttribute.Hash` instead of an 8-character truncation;
a new `CommonCompiler.GetAssemblyLocation(Type)` returns `"<unknown>"` when
`RuntimeFeature.IsDynamicCodeSupported` is false or the location is empty.

### N11-BREAK-15 — Misplaced `#!` now reports CS9378 instead of CS1040 (undocumented)

PR <https://github.com/dotnet/roslyn/pull/83112>, commit `3a0c084c` (2026-04-10), driven by issue #83111
("Misleading error for #! not being on the first line and column"). The predicate is unchanged —
`hashPosition != 0 || hash.HasTrailingTrivia` — but the error moved from
`ERR_BadDirectivePlacement` (**CS1040**, "Preprocessor directives must appear as the first non-whitespace
character on a line") to `ERR_PPShebangNotOnFirstLine` (**CS9378**, "'#!' must be the first characters on
the first line of the file"). `ERR_PPShebangNotOnFirstLine` is present only on `main`, absent from
`release/dev18.3` (OQ-17). In project-based mode CS9378 is reported **in addition to** CS9314.

### N11-BREAK-16 — Warning waves: there is NO wave 11

`docs/compilers/CSharp/Warnversion Warning Waves.md` tops out at **wave 10**, whose only member is
**CS9265** ("Field is never ref-assigned to, and will always have its default value (null reference)"),
added in C# 14. learn.microsoft.com states "Warning wave 10 diagnostics were added in C# 14." and lists
nothing beyond. **No new warning wave and no new wave-gated warning has been introduced for C# 15.** The
.NET 11 SDK does set the default `WarningLevel` to **11** for `net11.0` (see N11-SDK-04), but that level
currently carries no new diagnostics.

Complete wave inventory for reference: wave 10 = CS9265; wave 8 = CS9123 plus the
`EnableGenerateDocumentationFile` helper used to enforce IDE0005 on build; wave 7 = CS8981 (all-lowercase
ASCII type name may become reserved — this is what pre-announced `union`, `closed` and `safe`);
wave 6 = CS8826; wave 5 = CS7023, CS8073, CS8848, CS8880–CS8887, CS8892, CS8897, CS8898.

### N11-BREAK-17 — No new default-on warnings, and no warnings promoted to errors

Every entry in the .NET 11 compiler breaking-change list is an **error** (new or newly reported), a
**parse-shape change**, or an **overload-resolution change**. No entry introduces a warning, and no warning
was promoted to an error.

**Indirect exhaustiveness effect.** `closed` and `union` change exhaustiveness analysis for `switch`, which
affects the existing **CS8509** ("the switch expression does not handle all possible values") in the
*permissive* direction: a switch over a closed hierarchy's direct descendants no longer needs a default arm
and no longer warns. Conversely, **adding a case type to a `union`, or a direct descendant to a `closed`
class, newly produces exhaustiveness warnings at every `switch` that does not handle it** — a
source-compatibility hazard for library authors, not a compiler break. The speclet acknowledges it: "It can
be a breaking change to add a `closed` modifier to an existing class, or to add an additional derived class
from a closed class."

Related, and separately hazardous: <https://github.com/dotnet/roslyn/issues/83055>, "[C# 15] Making an
existing type a custom union type breaks existing pattern matching" — adding `[Union]` to an existing type
changes how patterns over it bind (they begin to unwrap to `Value`), which is a source-breaking change for
that type's consumers.

### N11-BREAK-18 — No breaking change to nullable analysis, definite assignment or `#nullable`

No breaking change to nullable analysis or to definite-assignment analysis is listed. The only
nullable-adjacent statement is that union types provide "enhanced nullability tracking", which is new
analysis for a new construct rather than a change to existing analysis. `#nullable` directive syntax and
parsing are unchanged; the only edit is the internal rename `ERR_NullableOptionNotAvailable` →
`ERR_CompilationOptionNotAvailable`, with **CS8630's number and message text unchanged**.

### N11-BREAK-19 — Three identifiers change meaning; one method name changes meaning

| Identifier | Gate | Positions affected | Break |
|---|---|---|---|
| `union` | **C# 15, live at the GA default** | type-declaration contexts; `union` followed by a type name | a field declaration `union X;` reparses as a union declaration ⇒ CS9370 |
| `closed` | **C# 15, live at the GA default** | type/alias **declaration** (hard error) and member-declaration contexts (modifier) | CS9380 on the declaration; CS1519 on use as a type name in a member declaration |
| `safe` | preview | modifier position on member declarations | a type named `safe` is no longer resolved there |
| `with` | C# 15 (LangVersion-gated per the docs) | first token of a collection element followed by `(` | `[with(x, y), z]` stops being a method call; mitigate with `@with` |
---

## 4. .NET 11 runtime and BCL changes

Canonical index: <https://learn.microsoft.com/en-us/dotnet/core/compatibility/11> (note the URL is
`/compatibility/11`, **not** `/compatibility/11.0`, which returns HTTP 404). Microsoft's own caveat on that
page, verbatim: "**This article is a work in progress. It's not a complete list of breaking changes in
.NET 11.**" Preview 8, RC 1 and RC 2 are still ahead of GA, and the preview release notes have already
carried breaking changes that never reached the index.

### N11-BCL-01 — Support calendar and supported operating systems

GA **2026-11-10**; **STS**, supported **two years**, end of support **2028-11-09**. Under the current policy
STS releases get two years and ship in even-numbered years; LTS releases get three years and ship in
odd-numbered years. Support phases: Preview (unsupported) → Go-Live (RC, supported in production) → Active
→ Maintenance (last six months, security only) → End of life. Six months after a version goes out of
support, newer SDKs emit `NETSDK1138`.

Preview dates: P1 2026-02-10, P2 2026-03-10, P3 2026-04-14, P4 2026-05-12, P5 2026-06-09, P6 2026-07-14,
P7 2026-08-11.

Supported OS highlights: Windows 11 26H1/25H2/24H2/23H2(E) and Windows 10 21H2(E)/1809(E)/1607(E), Arm64,
x64, x86; Windows Server 2025/23H2/2022/2019/2016 plus Core and Nano variants. macOS 26, 15, 14 (Arm64,
x64; Rosetta 2 supported). Linux: Alpine 3.23/3.22, Azure Linux 3.0, CentOS Stream 10/9, Debian 13,
Fedora 44/43, openSUSE Leap 16.0, RHEL 10/9/8, SLES 16.0/15.7, Ubuntu 26.04/25.10/24.04/22.04.
Android 16/15/14 with **API 24 as the minimum SDK target**; iOS 26/18, iPadOS 26/18, tvOS 26, minimum
iOS 12.2.

### N11-BCL-02 — Minimum hardware requirements raised (JIT / whole runtime)

*Preview 1.* **x86/x64, all operating systems: the JIT/AOT baseline moves from `x86-64-v1` to
`x86-64-v2`.** Previously guaranteed: `CMOV`, `CX8`, `SSE`, `SSE2`. Now additionally guaranteed: `CX16`,
`POPCNT`, `SSE3`, `SSSE3`, `SSE4.1`, `SSE4.2`.

| OS | Previous JIT/AOT min | New JIT/AOT min | Previous R2R target | New R2R target |
|---|---|---|---|---|
| Apple x64 | x86-64-v1 | **x86-64-v2** | x86-64-v2 | (no change) |
| Linux x64 | x86-64-v1 | **x86-64-v2** | x86-64-v2 | **x86-64-v3** |
| Windows x64 | x86-64-v1 | **x86-64-v2** | x86-64-v2 | **x86-64-v3** |
| Apple Arm64 | Apple M1 | (no change) | Apple M1 | (no change) |
| Linux Arm64 | armv8.0-a | (no change) | armv8.0-a | **armv8.0-a + LSE** |
| Windows Arm64 | armv8.0-a | **see RES-06** | armv8.0-a | **armv8.2-a + RCPC** |

`x86-64-v3` adds `AVX`, `AVX2`, `BMI1`, `BMI2`, `F16C`, `FMA`, `LZCNT`, `MOVBE`.

**RES-06 — UNRESOLVED CONFLICT (Windows Arm64 baseline).** The **breaking-change page**
(`/compatibility/jit/11/minimum-hardware-requirements`, `ms.date` 2026-08-15, `updated_at` 2026-08-19)
says "For Windows, there's no change to the minimum hardware. .NET continues to support `armv8.0-a`
devices, including Windows 10 IoT devices that don't provide the `LSE` instruction set", and its table
shows "(No change)". The **what's-new runtime page** (same `ms.date`, same `updated_at`) says "For Windows,
the baseline is updated to require the `LSE` instruction set". Recency does not settle it. **The
breaking-change page is the normative compatibility document and gives a reason, so it is the better bet,
but this is not resolved.** See OQ-18.

Failure mode on unsupported hardware, verbatim: "The current CPU is missing one or more of the baseline
instruction sets." ReadyToRun images that do not meet the new R2R target still run but fall back to JIT
compilation, adding startup overhead.

### N11-BCL-03 — `Nullable.GetUnderlyingType` throws for custom `Type` subclasses

*Preview 4.* **The single most consequential BCL change for anything that implements a reflection model.**

```csharp
public virtual Type? GetNullableUnderlyingType();   // NEW virtual on System.Type
```

`Nullable.GetUnderlyingType(Type)` now forwards to it. **The base `System.Type` implementation throws
`NotSupportedException`** with the message "Derived classes must provide an implementation."

Previously `Nullable.GetUnderlyingType` hard-coded a comparison against the executing runtime's
`typeof(Nullable<>)`, so it returned `null` for `Type` instances from another reflection universe — most
notably, **`MetadataLoadContext` always reported `Nullable<T>` as non-nullable**.

Types shipped with .NET that override the new virtual and are therefore unaffected: the runtime `Type`
implementation, `TypeDelegator`, `TypeBuilder`, `EnumBuilder`, `GenericTypeParameterBuilder`,
`TypeBuilderInstantiation`, `SymbolType`, `ModifiedType`, the `SignatureType` family, and the
`MetadataLoadContext` types.

**Any custom `System.Type` subclass must add an override.** Recommended implementations:

```csharp
public override Type? GetNullableUnderlyingType() => null;                       // never Nullable<T>

public override Type? GetNullableUnderlyingType()                               // constructed generics
{
    if (IsGenericType && !IsGenericTypeDefinition && GetGenericTypeDefinition() == typeof(Nullable<>))
        return GetGenericArguments()[0];
    return null;
}

public override Type? GetNullableUnderlyingType() => _innerType.GetNullableUnderlyingType(); // wrapper
```

**There is no AppContext switch and no configuration to revert.** Compatibility detail: for the *open*
generic, `typeof(Nullable<>).GetNullableUnderlyingType()` returns the generic parameter, while
`Nullable.GetUnderlyingType(typeof(Nullable<>))` still returns `null`.

Sources: `/compatibility/core-libraries/11/nullable-getunderlyingtype-throws`, dotnet/runtime #126905, #124216.

### N11-BCL-04 — Union and closed-hierarchy runtime support types

```csharp
namespace System.Runtime.CompilerServices;

/// <summary>Indicates that a class or struct is a union type, enabling compiler support for union behaviors.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class UnionAttribute : Attribute { }

/// <summary>Provides a common interface for accessing the contents of a union type at runtime.</summary>
/// <remarks>Implementing this interface is not required for union behaviors provided by the compiler.</remarks>
public interface IUnion { object? Value { get; } }

[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IsClosedTypeAttribute : Attribute
{
    private Type[] _derivedTypes = Type.EmptyTypes;
    public IsClosedTypeAttribute() { }
    /// <summary>Gets or sets the derived types of the closed type. A null value is normalized to an empty array.</summary>
    public Type[] DerivedTypes { get => _derivedTypes; set => _derivedTypes = value ?? Type.EmptyTypes; }
}
```

All three ship in `System.Private.CoreLib` and in the `System.Runtime` reference assembly.
`UnionAttribute` and `IUnion` arrived in **.NET 11 Preview 5** (dotnet/runtime PR #127001).

**RES-07 — `UnionAttribute` shape.** The speclet sketch was
`public class UnionAttribute : Attribute;` with only `AttributeUsage(Class | Struct, AllowMultiple = false)`.
The shipped type is additionally `sealed` and `Inherited = false`. The runtime source is authoritative.
(The .NET 11 Preview 3 polyfill published in the release notes also omits `Inherited = false`.)

**RES-08 — `IsClosedTypeAttribute` shape.** The csharplang speclet shows a bare
`public sealed class IsClosedTypeAttribute : Attribute { }` with no members. The runtime declaration and
Roslyn's well-known-member table (`…IsClosedTypeAttribute__DerivedTypes`, a property of type
`System.Type[]`) add `DerivedTypes`, per dotnet/runtime #129009 (`api-approved`) and roslyn #84350. **The
runtime plus Roslyn are more recent and authoritative.** (.NET 11 Preview 5 release notes named the
polyfill `ClosedAttribute`; that name is superseded.)

**RES-09 — `DerivedTypes` does NOT omit internal derived types.** `ClosedClassesTests.DerivedTypesMetadata_01`
asserts `IsClosedTypeAttribute(DerivedTypes = {typeof(D1), typeof(D2)})` for two *implicitly internal*
top-level derived classes. The array is built from `CandidateClosedSubtypeDefinitions` with **no
accessibility filtering at all**. Any claim that `DerivedTypes` omits internal types, or that this is what
`ClosedDerivedTypeInfo.IsComplete == false` means, is false.

**The compiler does NOT synthesize `UnionAttribute` or `IUnion`** ("The compiler should not synthesize these
types and users should provide them explicitly, either by referencing assemblies or defining them
locally"). `IsClosedTypeAttribute` is likewise not synthesised, but **is polyfilled into the consuming
assembly by the compiler** when targeting a runtime that predates it — a fact recorded in System.Text.Json's
own source comment. See N11-BCL-05.

**`IsClosedTypeAttribute.DerivedTypes` is currently write-only from Roslyn's point of view:** Roslyn emits
it and never reads it (`AttributeDescription.IsClosedTypeAttribute` declares only the parameterless
constructor). The one confirmed reader is **System.Text.Json**, which matches the attribute by **full name**
and handles the CoreCLR/Mono difference in how array-valued named arguments materialise (`Type[]` on Mono,
`IList<CustomAttributeTypedArgument>` on CoreCLR).

### N11-BCL-05 — Down-level availability of each C# 15 feature (the polyfill table)

Two Roslyn conventions decide every row: **embedded (synthesized) attributes** — Roslyn generates the type
into the assembly itself when absent, so the feature works on any TFM; and **well-known / predefined types**
— Roslyn requires the type to exist and reports `CS0518` or `CS0656`, but **a source-declared polyfill of
the correct shape satisfies these** (the `IsExternalInit` / `RequiredMemberAttribute` /
`CollectionBuilderAttribute` pattern).

| Type | Compiler synthesizes? | Source polyfill accepted? | Usable below `net11.0`? |
|---|---|---|---|
| `System.Runtime.CompilerServices.UnionAttribute` | **No** ("not synthesized" per the test plan) | **Yes** — Preview 3 shipped the exact polyfill source | Yes, with a polyfill and explicit `LangVersion` |
| `System.Runtime.CompilerServices.IUnion` | **No** | **Yes** | Yes, same |
| `System.Runtime.CompilerServices.IsClosedTypeAttribute` | **No** (an error is reported if not found) | **Presumed yes** — unverified | Only with a polyfill **and** `CompilerFeatureRequiredAttribute` |
| `System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute` | **No** → `CS0656` | **Yes**, well established | Ships in `net7.0`+; needs a polyfill on `netstandard2.0`, `netstandard2.1`, `net472`, `net6.0` |
| `System.Runtime.CompilerServices.MemorySafetyRulesAttribute` | **Yes** | Declaring it is fine; **applying it in source is an error** | Yes (LangVersion-gated, not TFM-gated) |
| `System.Diagnostics.CodeAnalysis.RequiresUnsafeAttribute` | **Yes** | Same | Yes, same |
| `System.Runtime.CompilerServices.CollectionBuilderAttribute` | **No** (unchanged from C# 12) | **Yes**, routinely polyfilled | Yes; the relaxed C# 15 create-method rules need `LangVersion >= 15` and `ReadOnlySpan<T>` from `System.Memory` |
| `System.Runtime.InteropServices.InAttribute` | **No** → `CS0518` | Yes | Present in `mscorlib`, `netstandard.dll` and every `System.Runtime`; the break bites only trimmed or hand-assembled reference sets |
| `System.Runtime.InteropServices.ExtendedLayoutAttribute` | **No** | Unverified; a polyfill would emit metadata older runtimes cannot interpret | **`net11.0` only in practice** |
| `AsyncHelpers`, `MethodImplOptions.Async` | **No** | n/a — runtime support is required, not merely the type | **No — `net11.0` only** |

Features with **no framework-type dependency at all**, working on any TFM with `LangVersion >= 15`:
labeled `break`/`continue`; extension indexers (they need `ExtensionAttribute` and `DefaultMemberAttribute`,
both in `netstandard2.0`, plus the compiler-synthesized `ExtensionMarkerAttribute` and grouping/marker
types); collection-expression `with(...)` targeting constructors and interfaces.

The Preview 3 polyfill, published verbatim in the release notes (which settles that hand-written
declarations are accepted):

```csharp
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class UnionAttribute : Attribute;
    public interface IUnion { object? Value { get; } }
}
```

**Checklist for a multi-targeted library that wants C# 15.** (1) Set `<LangVersion>15.0</LangVersion>`
explicitly — the default is `7.3` on `netstandard2.0` and `net472`, `8.0` on `netstandard2.1` and
`netcoreapp3.1`, `12.0` on `net8.0`, `13.0` on `net9.0`, `14.0` on `net10.0`. Microsoft documents using a
language version newer than the TFM's as unsupported, but it works for every construct that does not need a
missing framework type. (2) For `union`, polyfill `UnionAttribute` and `IUnion` below `net11.0`, guarded by
`#if !NET11_0_OR_GREATER`. (3) For `closed`, polyfill `IsClosedTypeAttribute` below `net11.0` **and**
`CompilerFeatureRequiredAttribute` below `net7.0`. A polyfilled attribute declared `internal` still
round-trips, because a consuming compiler matches by full metadata name. (4) Do not expect
`ExtendedLayoutAttribute` or runtime async below `net11.0`. (5) Verify the effective language version per
TFM with `#error version` (CS8304).

### N11-BCL-06 — `ExtendedLayoutAttribute` and `TypeAttributes.ExtendedLayout`

*Shipped in .NET 11 Preview 1; **not** language-version gated.*

```csharp
namespace System.Runtime.InteropServices;
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class ExtendedLayoutAttribute : Attribute
{
    public ExtendedLayoutAttribute(ExtendedLayoutKind layoutKind) { }
}
public enum ExtendedLayoutKind { CStruct = 0, CUnion = 1 }
public enum LayoutKind { Sequential = 0, Extended = 1, Explicit = 2, Auto = 3 }   // 'Extended' is NEW
```

Compiler behaviour: the compiler emits `TypeAttributes.ExtendedLayout` for a decorated type;
`StructLayoutAttribute` may not be combined with it; in C# `InlineArrayAttribute` may not be combined
either; `ITypeSymbol.Layout` returns a `TypeLayout` with `LayoutKind = Extended (1)`, `Size = 0`,
`Pack = 0`; the attribute is preserved on NoPIA-embedded types; and "the compiler will not have knowledge
of the specific options available on the `ExtendedLayoutAttribute` … will not attempt to detect invalid
field types."

**Exact values, and a trap.** `System.Reflection.TypeAttributes.ExtendedLayout = 0x00000018` (24) — **the
same value as `TypeAttributes.LayoutMask`**. `ExtendedLayout` is the fourth, previously reserved, value of
the two-bit layout mask: `AutoLayout = 0x0`, `SequentialLayout = 0x8`, `ExplicitLayout = 0x10`,
`ExtendedLayout = 0x18`. **Any test written as `(flags & TypeAttributes.ExtendedLayout) != 0` is wrong** —
it also matches sequential and explicit layout. The correct test is
`(flags & TypeAttributes.LayoutMask) == TypeAttributes.ExtendedLayout`.

**ECMA-335 status: MERGED** into `docs/design/specs/Ecma-335-Augments.md`, section "Extended Layout":
I.9.5 adds the `extendedlayout` rule; II.10.1 adds `extended` as a `ClassAttr`; **"The `.pack` and `.size`
directives are not valid on a type marked with `extended`"**; **"A type with ExtendedLayout must
immediately inherit from `System.ValueType`"**; AutoLayout and ExtendedLayout types own no `ClassLayout`
rows; and II.23.1.15 gains the row `| ExtendedLayout | 0x00000018 | Layout is supplied by a
System.Runtime.InteropServices.ExtendedLayoutAttribute custom attribute |`.

Roslyn feature status: "ExtendedLayoutAttribute — main — Merged into 18.3" (PR #78741, developer
jkoritzinsky, runtime issue #100896). The diagnostic identifiers for the `StructLayout` and `InlineArray`
conflicts, and whether a source polyfill is honoured, are **unverified** (OQ-19).

### N11-BCL-07 — Core library breaking changes

| Id | Change | Preview | Detail |
|---|---|---|---|
| N11-BCL-07a | `Assembly.GetCallingAssembly` when `StackTraceSupport` is disabled | P7 | On NativeAOT it previously always threw `PlatformNotSupportedException`; it now returns the calling assembly by inspecting stack-trace data. On **both** NativeAOT and CoreCLR it now throws **`NotSupportedException`** when the `StackTraceSupport` feature switch is `false`: "Unable to retrieve stack trace information when StackTraceSupport feature switch is set to false." |
| N11-BCL-07b | `CborReader` / `CborWriter` enforce a default maximum nesting depth | P5 | Reader **default 64**, throws `CborContentException`; writer **default 1000**, throws `InvalidOperationException`. New `CborReaderOptions` / `CborWriterOptions` with `MaxDepth`. **Unlike `Utf8JsonReader`, `MaxDepth = 0` means no nesting allowed**; use `-1` or omit for the default. No AppContext switch |
| N11-BCL-07c | `System.Numerics.Complex` follows C23 Annex G special values | P7 | `Complex` now delegates to the new generic `Complex<double>`. `Atan((+∞, 1))` `(NaN,NaN)`→`(π/2, 0)`; `Acos((-∞, NaN))` `(NaN,NaN)`→`(NaN,+∞)`; `Cosh((+∞,+∞))` `(NaN,NaN)`→`(+∞,NaN)`; `(+∞,+∞) * (1,0)` `(NaN,NaN)`→`(+∞,+∞)`. Spans `*`, `/`, `Multiply`, `Divide`, `Reciprocal`, `Abs`, `Pow`, `Sqrt`, `Exp`, `Log`, `Log10` and the trigonometric families. No switch |
| N11-BCL-07d | CRC32 validation when reading ZIP entries | P3 | `ZipArchiveEntry.Open()` now throws `InvalidDataException` on a CRC32 mismatch |
| N11-BCL-07e | `DateOnly`/`TimeOnly` `TryParse`/`TryParseExact` throw for invalid input | P2 | Invalid `DateTimeStyles` values or invalid format specifiers now throw `ArgumentException` instead of returning `false` |
| N11-BCL-07f | **`decimal` and `BigInteger` floating-point conversions are correctly rounded** | P7 | The exact source value is now rounded **once**. `((decimal)1.23).ToString("G29")` `1.23` → `1.229999999999999982236431606`. **Critically for a compiler: "If a conversion is evaluated as a compile-time constant, a compiler hosted by the .NET 11 Preview 7 SDK or a later SDK can embed the new result in the output assembly when the project is rebuilt, *regardless of the project's target framework*."** Constant folding changes with the compiler host's runtime, not the target framework |
| N11-BCL-07g | `DeflateStream`/`GZipStream` write headers and footers for an empty payload | P1 | An empty compression now produces 2 bytes (Deflate) / 20 bytes (GZip) instead of 0 |
| N11-BCL-07h | `Environment.TickCount` / `TickCount64` on Windows | P1 | Now `QueryUnbiasedInterruptTime` instead of `GetTickCount64`: **excludes sleep/hibernation time** and updates at the interrupt-timer frequency instead of a fixed ~15.5 ms cadence. Matches Linux, macOS and the OS wait APIs |
| N11-BCL-07i | `Math.Round`/`MathF.Round` `digits` overloads correctly rounded | P7 | Previously `Round(value * 10^digits) / 10^digits`, wrong for ~5% of random inputs. **Any non-negative `digits` is now accepted** (previously 0–15 for `double`, 0–6 for `float`). `Math.Round(655.925, 2, AwayFromZero)` `655.93`→`655.92`. Out-of-range `MidpointRounding` now throws immediately |
| N11-BCL-07j | `NamedPipeServerStream` with `PipeOptions.CurrentUserOnly` on Unix | P4 | The backing socket file is `chmod`'d to **`0600`** immediately after `bind()` instead of inheriting the umask. **Ratcheted within a process**: once any instance for a pipe name specifies `CurrentUserOnly`, the file stays `0600` for the shared server entry's lifetime. Relevant to any cross-process IPC over named pipes on Linux/macOS |
| N11-BCL-07k | `PackagePart.GetStream()` returns a non-seekable stream | P7 | Only when *all* hold: package opened `FileAccess.ReadWrite`; part opened read-only; part compressed; part not previously written in the session. `CanSeek == false`; `Seek` and setting `Position` throw. `Length` still works. Gated behind `NET11_0_OR_GREATER` |
| N11-BCL-07l | `SafeFileHandle.IsAsync` / `FileStream.IsAsync` on Unix | P3 | Now reflects the actual `O_NONBLOCK` state: `false` for regular files, `true` for non-blocking pipes and sockets. Also, `SendPacketsElement` with a `FileStream` no longer throws on non-Windows |
| N11-BCL-07m | TAR-reading APIs verify header checksums | P1 | `TarReader.GetNextEntry` and `TarFile.ExtractToDirectory` throw `InvalidDataException` on mismatch |
| N11-BCL-07n | `TarWriter` uses `HardLink` entries for hard-linked files | P3 | Restore with `new TarWriterOptions { HardLinkMode = TarHardLinkMode.CopyContents }`. Extracting hard links to a file system without support throws `IOException`; `TarExtractOptions` controls it |
| N11-BCL-07o | `ZipArchive.CreateAsync` eagerly loads entries | P1 | The central directory is read inside `CreateAsync`; `InvalidDataException` now surfaces there rather than on first access to `Entries` |
| N11-BCL-07p | `SYSLIB0064` obsoletion | — | `RSACryptoServiceProvider.Encrypt(byte[], bool)` and `Decrypt(byte[], bool)`. **Custom diagnostic id, so suppressing `CS0618` does not suppress it** |

**Breaking changes recorded only in preview release notes, not on the Learn index:**
`WindowLog` renamed **`WindowLog2`** across `BrotliCompressionOptions`, `ZLibCompressionOptions`,
`ZstandardCompressionOptions` and `ZstandardDecompressionOptions` (**no compatibility alias**; affects only
code written against earlier .NET 11 previews); `TensorPrimitives.Clamp` no longer throws when `min > max`;
`Process.Run`/`RunAsync`/`RunAndCaptureText`/`StartAndForget` take `IEnumerable<string>?` instead of
`IList<string>?`, and `Process.Run`/`RunAsync` gained a `bool silent = false` parameter **positioned before
the existing optional parameters**; `Microsoft.Extensions.Logging` moved several internal types with
`[Obsolete]` shims left behind; `System.Security.Cryptography.Xml` mitigations may reject XML that
previously verified; `System.DirectoryServices.AccountManagement` now escapes LDAP filter values;
`ZipArchive` Update mode no longer drops data descriptors, so byte-for-byte archive comparisons differ;
**`MetadataLoadContext.CoreAssembly` lost its `[NotNull]` annotation**, which can produce new nullable
warnings in consumer code.

### N11-BCL-08 — Reflection, assembly loading and plug-in hosting

- **`Type.GetNullableUnderlyingType()`** — see N11-BCL-03.
- **`ConstructorInfo.GetGenericArguments()`** now has an override, giving a consistent way to retrieve
  generic type arguments for constructor definitions.
- **`AssemblyLoadContext.SetAssemblyLocationOverride`** *(P7, most relevant to plug-in hosts)*:

  ```csharp
  public static void SetAssemblyLocationOverride(Func<Assembly, string, string> callback);
  ```

  A **set-once** static callback overriding `Assembly.Location` on **CoreCLR, Mono and NativeAOT**. Intended
  for hosts that stage assemblies in temporary directories or bundle them (single-file publishing, embedded
  resources, virtual file systems). Set-once semantics prevent a later component from silently redirecting
  an in-flight override.
- **`MetadataLoadContext.GetLoadContext(Assembly)`** *(P4)* mirrors `AssemblyLoadContext.GetLoadContext`.
  Related fixes: `MetadataLoadContext` no longer returns internal array types instead of `Type[]` (P2);
  `CoreAssembly` lost `[NotNull]` (P4).
- **Function-pointer support in `System.Reflection.Emit`** *(P1)*, including references to unmanaged
  function pointers.
- Better `InvalidCastException` message when a generic argument comes from a different
  `AssemblyLoadContext` (P4).
- **`[RequiresUnsafe]` removed from a large set of pointer-taking BCL APIs.** Previously, calling these from
  `unsafe` code still required project-level `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` because the
  attribute enforced that independently; now only the standard `unsafe` block or member modifier is
  required. Affected: `Buffer.MemoryCopy`; `ReadOnlySpan<T>(void*, int)` and `Span<T>(void*, int)`;
  `System.Runtime.CompilerServices.Unsafe` pointer methods (`AsRef`, `Read`, `Write`, `Copy`);
  `System.Runtime.InteropServices.NativeMemory`; the `System.Text.Encoding` pointer overloads on all
  encoding classes; `System.Numerics.Vector` pointer-based `Load`/`Store`; and the interop marshalling types
  in `System.Runtime.InteropServices.Marshalling`.

### N11-BCL-09 — `System.Text.Json`: no breaking change, several new features

New: `JsonSerializerOptions.GetTypeInfo<T>()` and `TryGetTypeInfo<T>(out JsonTypeInfo<T>?)`;
`JsonNamingPolicy.PascalCase`; `System.Text.Json.Serialization.JsonNamingPolicyAttribute` (per-member
override); **type-level `[JsonIgnore]`** setting the default for all members; F# discriminated-union support
out of the box; `Utf8JsonWriter.Reset(Stream, JsonWriterOptions)` and the `IBufferWriter` equivalent;
`JsonSerializer.SerializeAsyncEnumerable` overloads writing to a `PipeWriter` plus a `topLevelValues: bool`
parameter emitting NDJSON; `JsonMetadataServices.CreateIReadOnlySetInfo`.

**Directly tied to C# 15:** a new `JsonTypeInfoKind.Union` contract kind, `JsonUnionAttribute` and
`JsonUnionCaseInfo`, plus type-classifier APIs `JsonTypeClassifier` and
`JsonSerializerOptions.TypeClassifiers`; union values serialise by writing the active case directly
(dotnet/runtime #128162). And **`JsonSerializerOptions.InferClosedTypePolymorphism`**, which infers
polymorphic metadata for C# closed hierarchies from `[IsClosedType].DerivedTypes` without explicit
`[JsonDerivedType]` annotations, synthesising a string discriminator of `DerivedType.Name`; explicit
registrations take precedence (dotnet/runtime #129041). ASP.NET Core surfaces unions in OpenAPI documents.

### N11-BCL-10 — Nine `Microsoft.Extensions.*` packages moved into the shared framework

*Preview 4.* Now part of the base `Microsoft.NETCore.App` shared framework:
`Microsoft.Extensions.Caching.Abstractions`, `Configuration.Abstractions`,
`DependencyInjection.Abstractions`, `Diagnostics.Abstractions`, `FileProviders.Abstractions`,
`Hosting.Abstractions`, `Logging.Abstractions`, `Options`, `Primitives`.

Documented consequences, verbatim: no `PackageReference` needed when targeting `net11.0` or later; an
explicit reference produces build warning **`NU1510`**; **these assemblies are no longer copied to the
output folder**; and "**In rare cases, the additional APIs in the default load set might cause name or type
conflicts.** To resolve a conflict, add more explicit `using` directives, use an alias, or use a fully
qualified type name."

The concrete new collision surface, **on the `net11.0` leg only**: any own type named `IOptions<T>`,
`IOptionsMonitor<T>`, `ILogger`, `ILoggerFactory`, `ILoggerProvider`, `IChangeToken`, `StringValues`,
`StringSegment`, `IFileProvider`, `IFileInfo`, `IConfiguration`, `IConfigurationSection`, `IHostedService`,
`IHostApplicationLifetime`, `IMemoryCache`, `IDistributedCache`, `IServiceCollection`, `ServiceDescriptor`,
`ActivatorUtilities`, and so on.

If a dependent library was compiled against an older version of one of these packages it can now fail at
run time with `MissingMethodException` or `TypeLoadException`; recompile against the .NET 11 reference
assemblies. The doc also lists the historical breaking changes in these packages that surface on upgrade
(`ActivatorUtilities.CreateInstance` behaviour, `FromKeyedServicesAttribute.Key` nullability, non-keyed
service used when keyed not found, `GetKeyedService`/`GetKeyedServices` with `AnyKey`,
`ProviderAliasAttribute` moved assembly, `BackgroundService` unhandled exceptions, `BackgroundService`
running all of `ExecuteAsync` as a `Task`).

**Multi-targeting note:** `NU1510` is raised **only when pruning applies to all runtime targets**. A library
targeting `netstandard2.0;net472;net8.0;net10.0;net11.0` that references one of these packages will
**not** get NU1510, because the package is still required for the older TFMs. What still changes on the
`net11.0` leg regardless: build-time conflict resolution picks the shared-framework assembly, and the
package assembly is not copied to the output folder — so if the referenced package version is older, that
leg silently compiles and runs against the newer API surface.

### N11-BCL-11 — Other `Microsoft.Extensions.*` breaking changes

- **`ChangeToken.OnChange` async overloads rebind existing callbacks** *(P7)* — **a silent
  overload-rebinding change with no compiler ambiguity.** New overloads
  `OnChange(Func<IChangeToken?>, Func<Task>)` and `OnChange<TState>(Func<IChangeToken?>, Func<TState,Task>, TState)`.
  Source that passed an `async` lambda previously bound to the `Action` overload and compiled as
  `async void` (fire and forget; re-registration at the first incomplete `await`; later exceptions on the
  synchronization context or thread pool). After recompiling against .NET 11 the same source binds to
  `Func<Task>`, compiles as `async Task`, and re-registration happens only after the returned task
  completes; multiple changes during the callback coalesce into one later invocation. **No AppContext
  switch — overload selection happens at compile time.** To keep the old behaviour, cast:
  `(Action)(async () => { … })`.
- **`IHost.RunAsync` / `StopAsync` throw when a `BackgroundService` fails** *(P3)* — when
  `ExecuteAsync` throws and `HostOptions.BackgroundServiceExceptionBehavior` is `StopHost` (the default),
  the tasks returned from `RunAsync`, `StopAsync`, `WaitForShutdownAsync` and their synchronous equivalents
  now **fail**, so the process exits with a non-zero exit code. Multiple failures combine into an
  `AggregateException`.
- **`FileConfigurationSource.OnLoadException` is called for I/O errors** *(P7)* — the exception can now be
  of **any** type (commonly `IOException`), so code that unconditionally casts to `InvalidDataException` or
  `FileNotFoundException` can throw `InvalidCastException`. I/O errors are no longer observable through
  `TaskScheduler.UnobservedTaskException` except when no callback is registered.
- **`FileConfigurationProvider` does not raise the reload token after an ignored load failure** *(P7)* —
  after `Load()` fails and the callback sets `FileLoadExceptionContext.Ignore = true`, `OnReload` is no
  longer called, so `GetReloadToken()` does not fire.

### N11-BCL-12 — Cryptography

- **DSA removed from macOS** *(P1)* — **only finite-field DSA; ECDSA is unaffected.** On macOS, `DSA`,
  `DSACryptoServiceProvider`, X.509 certificates with DSA keys and any API that interacts with DSA keys now
  throw **`PlatformNotSupportedException`**. Reason: .NET relied on Apple's obsolete `SecurityTransforms`
  library, which has no replacement and only ever supported DSA-1024 with SHA-1 and never key generation.
  DSA continues to work on Windows (CNG) and Linux (OpenSSL); the absence of a statement is not proof
  (OQ-20).
- **Composite ML-DSA on Windows uses the native CNG implementation** *(P7)* — Windows implements exactly
  four parameter sets natively, all ML-DSA + ECDSA (`MLDsa44WithECDsaP256`, `MLDsa65WithECDsaP256`,
  `MLDsa65WithECDsaP384`, `MLDsa87WithECDsaP384`). **Every ML-DSA + RSA composite worked before and no
  longer does** — a regression in coverage. Guard with `CompositeMLDsa.IsAlgorithmSupported(...)`.
- **Linux AIA certificate fetching limited to two fetches per chain build** *(P7)* — matching the limit
  Windows has always had. Chains requiring three or more AIA downloads now fail; mitigate with
  `chain.ChainPolicy.ExtraStore`.
- **`SYSLIB0065`** *(P6)* obsoletes the **`set` accessor** of `AsnEncodedData.RawData`.
- New: `System.Security.Cryptography.X25519DiffieHellman` (abstract) with `X25519DiffieHellmanCng` and
  `X25519DiffieHellmanOpenSsl`; `CryptographicOperations.FixedTimeEquals(ReadOnlySpan<byte>, byte)`;
  HMAC and KMAC verification APIs.

### N11-BCL-13 — Other runtime changes with observable semantics

- **`configProperties` in `.runtimeconfig.dev.json` now override `.runtimeconfig.json`** *(P6, breaking)* —
  precedence reversed. **A stale `.runtimeconfig.dev.json` in an output directory now silently overrides
  production settings**, which matters for anything driven by `AppContext` switches.
- **NativeAOT uses a `lib` prefix for native library outputs on Unix** *(P3, breaking)* — `libmylib.so`
  rather than `mylib.so`. Opt out with `<UseNativeLibPrefix>false</UseNativeLibPrefix>`.
- **Japanese calendar minimum date corrected** *(P1)* — `JapaneseCalendar.MinSupportedDateTime` moves from
  1868-09-08 to **1868-10-23**; dates in between are now rejected. No other globalization break.
- **`SslStream` server-side AIA downloads disabled by default** *(P3, breaking)* — when validating client
  certificates as a server, missing intermediates are no longer downloaded; if the client does not send the
  full chain the handshake fails. Applies only when no custom
  `SslServerAuthenticationOptions.CertificateChainPolicy` is supplied.
- **Saturating floating-point conversions** — unchecked `float`/`double` conversions to small integer types
  now **saturate to the type bounds** instead of wrapping through an intermediate truncation. Listed under
  JIT improvements rather than as a breaking change, but it is an observable semantic change for
  out-of-range conversions.
- **`Activator.CreateInstance<T>()` results are now treated as having an exact type**, and generic virtual
  methods and default interface methods on generic interfaces can now be devirtualised.
- `string.Equals` / `ReadOnlySpan<T>.SequenceEqual` on two compile-time constants fold to a constant.
- ARM64 with SVE: `Vector<T>` values are passed **by reference** rather than by value.
- **More than 1024 logical processors** now works (`sched_getaffinity` CPU set allocated dynamically); the
  GC retains a 1024-**heap** limit and `MAX_SUPPORTED_CPUS` was renamed `MAX_SUPPORTED_HEAPS`.
- In-process crash-report logging on mobile; `DOTNET_GCTrimYoungestKeepPercent`; GC heap hard limit for
  32-bit processes; WebCIL V1 as the default for CoreCLR WASM (header 28 → 32 bytes, `TableBase` added).
- `Comparer<T>.Default` and `EqualityComparer<T>.Default` are now specialised in R2R images (reported up to
  20× improvement); NativeAOT interface dispatch routes through a shared patchable helper (up to 200× on
  non-JIT platforms such as iOS).
- **`Console` honours `FORCE_COLOR`** alongside `NO_COLOR`: when set, `Console.IsOutputRedirected` no longer
  suppresses ANSI escape codes. Directly relevant to a build tool whose output is piped.
- `StringSyntaxAttribute` gains `CSharp`, `FSharp` and `VisualBasic` constants.
---

## 5. Runtime async ("Runtime Async V2")

Runtime async moves the async state machine out of compiler-generated classes and into the CLR. **It is
not a C# language feature**: no csharplang proposal, no `LangVersion` gate, no new syntax, no new
`SyntaxKind`, no new modifier, no new symbol shape, no new user-written attribute, and **no entry in the
.NET 11 compiler breaking-changes document**. It is a codegen strategy.

### N11-ASYNC-01 — Enablement: runtime unconditional, compiler opt-in

**The runtime side is unconditional.** Preview 1 enabled CoreCLR support by default; Preview 4 **removed**
the `DOTNET_RuntimeAsync` and `UNSUPPORTED_RuntimeAsync` environment variables (dotnet/runtime #125406)
"since runtime-async is now unconditionally enabled". **There is no runtime configuration switch and no
`AppContext` switch.** The CLR in .NET 11 always understands and executes runtime-async IL.

**The compiler side is opt-in per project**, through the Roslyn feature flag `runtime-async`, whose value
must be exactly the string `"on"`:

```xml
<PropertyGroup>
  <Features>runtime-async=on</Features>
</PropertyGroup>
```

or `csc /features:runtime-async=on`. A `net11.0` project **no longer requires**
`<EnablePreviewFeatures>true</EnablePreviewFeatures>` (the `[RequiresPreviewFeatures]` gate was removed in
Preview 3, dotnet/runtime #124488).

**RES-10 — `<UseRuntimeAsync>false</UseRuntimeAsync>` is NOT a user-facing SDK property.** The Learn page
says to use it to opt out per project. In fact `UseRuntimeAsync` is defined **only in the dotnet/runtime
repository's own build** (`src/libraries/Directory.Build.targets`), where it gates
`<Features>$(Features);runtime-async=on</Features>` for `IsNETCoreAppSrc` non-packable projects on
supported platforms. Grepping the .NET SDK's own targets finds **zero** occurrences of `runtime-async` or
`RuntimeAsync`. **The SDK does not set the flag for user projects, and setting `UseRuntimeAsync` in a user
project has no effect.**

**Conclusion as of Preview 7: the compiler feature is OFF by default for user code and ON for the .NET
runtime libraries themselves.** Several secondary sources conflate the two. Whether GA flips the SDK
default is unresolved (OQ-21); the Roslyn test plan still carries the unchecked item "replace feature flag
with a real switch and make the SDK set it by default".

**Platform coverage.** Supported: CoreCLR (JIT), ReadyToRun / crossgen2, NativeAOT, the CoreCLR
interpreter, RISC-V64 R2R, x86. **Excluded by the runtime's own build predicate: `browser`, `wasi`,
`android`, Apple mobile, and `Mono`**; the epic states "Feature becomes standard; Mono runtime remains
unsupported". On unsupported platforms `AsyncHelpers` is compiled to stubs that throw
`PlatformNotSupportedException("Runtime Async is not supported on this platform.")`.

### N11-ASYNC-02 — The single decision point

```csharp
internal bool CSharpCompilation.IsRuntimeAsyncEnabledIn(Symbol? symbol)
{
    if (!Assembly.RuntimeSupportsAsyncMethods) return false;
    if (symbol is not MethodSymbol { IsAsync: true } method) return false;

    var runtimeAsyncEnabledInMethod = method.RuntimeAsyncMethodGenerationAttributeSetting switch
    {
        ThreeState.True => true,
        ThreeState.False => false,
        _ => Feature(CodeAnalysis.Feature.RuntimeAsync) == "on"
    };
    if (!runtimeAsyncEnabledInMethod) return false;

    var methodReturn = method.ReturnType.OriginalDefinition;
    if ((object)methodReturn == LambdaSymbol.ReturnTypeIsBeingInferred) return true;  // optimistic

    return ((InternalSpecialType)methodReturn.ExtendedSpecialType) is (
        InternalSpecialType.System_Threading_Tasks_Task or
        InternalSpecialType.System_Threading_Tasks_Task_T or
        InternalSpecialType.System_Threading_Tasks_ValueTask or
        InternalSpecialType.System_Threading_Tasks_ValueTask_T);
}
```

`Assembly.RuntimeSupportsAsyncMethods` probes for the *type*
`System.Runtime.CompilerServices.AsyncHelpers` being a static class:

```csharp
internal bool RuntimeSupportsAsyncMethods
    => GetSpecialType(InternalSpecialType.System_Runtime_CompilerServices_AsyncHelpers)
       is { TypeKind: TypeKind.Class, IsStatic: true };
```

**RES-11 — the ECMA draft's "async-capable assembly" test is stale.** The spec says an async-capable
assembly is "one which references a corlib containing an `abstract sealed class RuntimeFeature` with a
`public const string` field member named `Async`". **`RuntimeFeature.Async` does not exist in the shipped
BCL** (`RuntimeFeature` has `ByRefFields`, `ByRefLikeGenerics`, `CovariantReturnsOfClasses`,
`DefaultImplementationsOfInterfaces`, `NumericIntPtr`, `PortablePdb`,
`UnmanagedSignatureCallingConvention`, `VirtualStaticsInInterfaces` — and no `Async`). Roslyn probes for
`AsyncHelpers` instead. The implementation ships and wins. See OQ-22.

The design document adds that `AsyncHelpers` "must be defined in the same assembly that defines `object`,
and the assembly cannot reference any other assemblies" — in practice the `System.Runtime` reference
assembly.

### N11-ASYNC-03 — What the compiler emits: `MethodImplOptions.Async`

```csharp
async Task M() { … }
// emitted as
[MethodImpl(MethodImplOptions.Async)]   // MethodImplOptions.Async == MethodImplAttributes.Async == 0x2000 == 8192
Task M() { … rewritten body … }
```

The marking is **not a custom attribute**. `MethodImplAttribute` is a pseudo-custom attribute: the compiler
sets a bit in the `MethodDef` row's `MethodImplAttributes`. `System.Reflection.MethodImplAttributes.Async`
and `System.Runtime.CompilerServices.MethodImplOptions.Async` are **new public BCL enum members in .NET 11**
(`System.Runtime.cs` lines ~13681 and ~15014; absent from netstandard2.0 / .NET 8 / 9 / 10). Roslyn itself
works around the absence with a C# 14 extension-member shim
(`MethodImplExtensions.cs`: `extension(MethodImplAttributes) { public static MethodImplAttributes Async => (MethodImplAttributes)0x2000; }`).

Consequences: **`IMethodSymbol.GetAttributes()` and `MethodInfo.GetCustomAttributes()` do NOT show it**; it
is read through `MethodBase.MethodImplementationFlags` or, in Roslyn, through the public
**`IMethodSymbol.MethodImplementationFlags`** (typed `System.Reflection.MethodImplAttributes`). Roslyn does
**not** map the bit onto `IMethodSymbol.IsAsync` for metadata methods (`PEMethodSymbol.IsAsync` is
unconditionally `false`), so a referenced assembly's runtime-async-ness is invisible to the symbol API by
design: "Exposed symbols do not give direct information about whether they were compiled with runtime
async, and indeed the compiler has no idea whether a method from a referenced assembly is compiled with
runtime async or not."

**No `[AsyncStateMachine]` attribute is emitted** (very strongly implied — there is no state machine type to
name and `stateMachineTypeOpt` is null on that path — but the emission site was not quoted; OQ-23).

**Applicability rules (ECMA augment I.8.4.5), verbatim:** the flag has effect only on method definitions
returning generic or non-generic `Task` or `ValueTask`; only on definitions with a CIL implementation; only
inside an async-capable assembly. `Async` + `Synchronized` is invalid. `byref` or ref-like returns are
invalid. Vararg methods are invalid. "These rules operate before generic substitution."

**ECMA-335 status: DRAFT ONLY.** The change lives in `docs/design/specs/runtime-async.md`, whose first line
says "This document is a draft of changes to ECMA-335 for the 'runtime async' feature. When the feature is
officially supported, it can be merged into the final ECMA-335 augments document."
**`Ecma-335-Augments.md` contains no async section.** Contrast `ExtendedLayoutAttribute`, which *is*
merged (N11-BCL-06).

**Hand-written `[MethodImpl(MethodImplOptions.Async)]` is forbidden**: `ERR_MethodImplAttributeAsyncCannotBeUsed`
= **CS9330**, "'MethodImplAttribute.Async' cannot be manually applied to methods. Mark the method 'async'."
**Never emit it in generated C# source.**

### N11-ASYNC-04 — The return convention: the IL returns `T`, not `Task<T>`

From the ECMA augment: "For async methods, the stack should be empty in the case of `Task` or `ValueTask`,
or the type argument in the case of `Task<T>` or `ValueTask<T>`."

Roslyn implements exactly this: `CodeGenerator.LazyReturnTemp` rewrites the return type to the type
argument, and `HandleReturn` treats an `async Task` / `async ValueTask` method as returning void.

**This is the single most disruptive IL-level fact.** A method whose `MethodDef` signature says it returns
`class Task`1<int32>` has IL whose `ret` pushes an `int32`, and a method whose signature says `Task` has IL
with a bare `ret`. **Any IL verifier, IL rewriter, decompiler or `System.Reflection.Metadata`-based analyzer
that assumes the return-type convention will reject or corrupt such a method unless it honours the 0x2000
flag.**

**ILVerify already honours it.** dotnet/runtime PR **#121503** "Update ILVerify to honor the *async* flag",
merged **2025-11-13**, changed `ILImporter.ImportReturn` to unwrap `Task`/`ValueTask`/`Task<T>`/`ValueTask<T>`
when `_method.IsAsync`, and to report `VerifierError.StackUnexpected` when the declared return type is not
one of the four. `EcmaMethod.IsAsync` reads `MethodImplAttributes.Async` via its private
`MethodFlags.Async = 0x02000`. **ILVerify is not in the SDK**; it ships as the `dotnet-ilverify` global
tool.

**`ilasm` and `ildasm` recognise the flag as the `async` keyword** (dotnet/runtime PR #115658); the managed
`ilasm` has `async` in its ANTLR grammar.

**Mono.Cecil has no `Async` member** in its own `MethodImplAttributes` enum (highest declared value is
`AggressiveOptimization = 0x0200`), but the reader does
`method.ImplAttributes = (MethodImplAttributes)ReadUInt16();` and the writer writes the raw `ushort` back,
so **unknown bits round-trip losslessly — they are just unnamed**. Anything that reconstructs
`MethodImplAttributes` from named members rather than the raw value will lose the flag.

### N11-ASYNC-05 — The suspension helpers

```csharp
namespace System.Runtime.CompilerServices;
[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class AsyncHelpers
{
    [MethodImpl(MethodImplOptions.Async)] public static void AwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion;
    [MethodImpl(MethodImplOptions.Async)] public static void UnsafeAwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion;

    [MethodImpl(MethodImplOptions.Async)] public static void Await(Task task);
    [MethodImpl(MethodImplOptions.Async)] public static void Await(ValueTask task);
    [MethodImpl(MethodImplOptions.Async)] public static T    Await<T>(Task<T> task);
    [MethodImpl(MethodImplOptions.Async)] public static T    Await<T>(ValueTask<T> task);
    [MethodImpl(MethodImplOptions.Async)] public static void Await(ConfiguredTaskAwaitable configuredAwaitable);
    [MethodImpl(MethodImplOptions.Async)] public static void Await(ConfiguredValueTaskAwaitable configuredAwaitable);
    [MethodImpl(MethodImplOptions.Async)] public static T    Await<T>(ConfiguredTaskAwaitable<T> configuredAwaitable);
    [MethodImpl(MethodImplOptions.Async)] public static T    Await<T>(ConfiguredValueTaskAwaitable<T> configuredAwaitable);

    // In the shipped reference assembly, not in the ECMA draft:
    public static void HandleAsyncEntryPoint(Task task);
    public static int  HandleAsyncEntryPoint(Task<int> task);
}
```

**RES-12 — `[Experimental("SYSLIB5007")]` has been REMOVED** from the shipped surface; the reference
assembly carries only `[EditorBrowsable(Never)]`. The Roslyn design document still shows it, and one
research pass reported it as current. The removal matches Preview 3's dropping of the
`[RequiresPreviewFeatures]` gate.

Implementation details: the helpers carry `[Intrinsic]`, `[BypassReadyToRun]`, `[StackTraceHidden]` (which
is why they do not pollute stack traces) and, for the awaiter overloads,
`MethodImplOptions.NoInlining | MethodImplOptions.Async`. Preview 7 added an internal
`AsyncHelpers.TransparentAwait` family that the JIT can inline so awaiting an already-completed task folds
into a flag check.

**Calling convention.** From the ECMA augment: "These methods are only legal to call inside async methods…
To achieve maximum performance, the IL sequence of two `call` instructions — one to the async method and
immediately one to the `Await` method — should be preferred."

```il
; await C.M()  where C.M() returns Task
call [System.Runtime]System.Threading.Tasks.Task C::M()
call void [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await(class …Task)

; int i = await C.M()  where C.M() returns Task<int>
call class …Task`1<int32> C::M()
call int32 …AsyncHelpers::Await<int32>(class …Task`1<int32>)
stloc.0

; await c  for a custom awaitable whose awaiter implements ICriticalNotifyCompletion
.locals init ([0] class C/Awaiter awaiter)
newobj   instance void C::.ctor()
callvirt instance class C/Awaiter C::GetAwaiter()
stloc.0
ldloc.0
callvirt instance bool C/Awaiter::get_IsCompleted()
brtrue.s IL_0019
ldloc.0
call     void …AsyncHelpers::UnsafeAwaitAwaiter<class C/Awaiter>(!!0)
IL_0019: ldloc.0
callvirt instance void C/Awaiter::GetResult()
ret
```

`ICriticalNotifyCompletion` lowering is always preferred over `INotifyCompletion` lowering when the compiler
statically knows the interface is implemented. Helper selection follows a five-step algorithm over the
methods named `Await` in corlib's `AsyncHelpers`, matching generic arity, a single parameter, the return
type, and an identity or implicit reference conversion. **Note the asymmetry: the *awaited expression* may
be a subtype of `Task<T>`, but the *enclosing method's own return type* must be exactly one of the four.**

**Dynamic awaits** also use runtime-async suspension: the compiler keeps the dynamic call sites, then tests
`awaiter as ICriticalNotifyCompletion` and calls `UnsafeAwaitAwaiter<ICriticalNotifyCompletion>` or
`AwaitAwaiter<INotifyCompletion>`. Lowering happens in `RuntimeAsyncRewriter`, not in the binder, and the
rewriter synthesises its own dynamic call-site container type.

**Async `Main` changes independently of the flag**: `SynthesizedEntryPointSymbol.AsyncForwardEntryPoint` now
prefers `AsyncHelpers.HandleAsyncEntryPoint(Task)` / `(Task<int>)` over the historical
`GetAwaiter().GetResult()` whenever the API exists in corlib — i.e. **for any `net11.0` compilation,
regardless of `runtime-async`**.

### N11-ASYNC-06 — Exception-handler rewriting and hoisting rules

**Exception handlers still need the pend-and-rethrow rewrite.** Runtime async forbids suspension inside
handler blocks, so the compiler performs the same rewrite it already does for state machines:

```csharp
// await in a catch
int pendingCatch = 0; Exception pendingException;
try { throw new Exception(); }
catch (Exception e) { pendingCatch = 1; pendingException = e; }
if (pendingCatch == 1) { AsyncHelpers.Await(C.M()); throw pendingException; }

// await in a finally
Exception pendingException;
try { throw new Exception(); }
catch (Exception e) { pendingException = e; }
AsyncHelpers.Await(C.M());
if (pendingException != null) { throw pendingException; }
```

Compound assignments are still spilled to temporaries around the `await`, and
`RuntimeAsyncRewriter.Rewrite` finishes by calling the same `SpillSequenceSpiller` the state-machine path
uses.

**Hoisting (ECMA augment).** "Local variables used across suspension points are considered 'hoisted'…
**By-ref variables may not be hoisted across suspension points, and any read of a by-ref variable after a
suspension point will produce null. Byref-like structs will also not be hoisted across suspension points
and will have their default value after a suspension point.** In the same way, pinning locals may not be
'hoisted' across suspension points and will have `null` value after a suspension point."

Roslyn's contribution: `IteratorAndAsyncCaptureWalker.Analyze(..., isRuntimeAsync: true, ...)` deliberately
does **not** hoist ordinary (non-`ref`) parameters, locals and fields, because the runtime handles them
("Runtime async only needs to preserve by-ref captures"). In **debug** builds the extra "hoist long-lived
locals and parameters" pass is skipped for runtime async, so **tools that reason about which locals survive
an `await` in a debug build will see fewer hoisted locals**. `this` in a struct or type-parameter receiver
is copied into a synthesized `SynthesizedLocalKind.AwaitByRefSpill` local before the first `await`, because
"any usage of `ldarg.0` in these scenarios is illegal after the first await".

**Restrictions on async IL.** Temporary, may be lifted: the `tail.` prefix is forbidden; the `localloc`
instruction is forbidden. Likely permanent: by-ref locals cannot be hoisted across suspension points;
suspension points may not appear in a `catch`, `filter`, `finally` or `fault` handler (they are permitted in
the protected `try`); and only the four `Task`-like types are supported as return types.

### N11-ASYNC-07 — What still generates a classic compiler state machine

`MethodCompiler.LowerBodyOrInitializer` branches on `IsRuntimeAsyncEnabledIn(method)`; the identical branch
exists in `CompileSynthesizedMethods` for lambdas and local functions. A **compiler state machine is still
produced** when any of the following holds:

1. **The corlib has no static `AsyncHelpers` class** — targeting `net10.0` or earlier, .NET Framework, or
   netstandard.
2. **The `runtime-async` feature flag is not `"on"`** — the default for user projects today.
3. **The method's return type is not exactly `Task`, `Task<T>`, `ValueTask` or `ValueTask<T>`.** The check
   uses `ReturnType.OriginalDefinition.ExtendedSpecialType`, so this excludes `async void`; **any user type
   derived from `Task` / `Task<T>`** (note the asymmetry with the awaited expression); any custom task-like
   type declared with `[AsyncMethodBuilder(...)]` and any method-level `[AsyncMethodBuilder]` override; and
   `IAsyncEnumerable<T>` / `IAsyncEnumerator<T>`.
4. **Async iterators.** `IteratorRewriter` runs first and unconditionally, and rule 3 then excludes the
   method. A separate, still-in-progress feature row "Runtime Async Streams"
   (`features/runtime-async-streams`, owner jcouv) exists, and the design document carries open TODOs for
   `IAsyncEnumerable`.
5. **`[RuntimeAsyncMethodGeneration(false)]`** on the method.
6. **Methods using a construct runtime async cannot express** — `ERR_UnsupportedFeatureInRuntimeAsync` =
   **CS9328**, "Method '{0}' uses a feature that is not supported by runtime async. Opt the method out of
   runtime async by attributing it with
   'System.Runtime.CompilerServices.RuntimeAsyncMethodGenerationAttribute(false)'." Preview 7 clarified this
   diagnostic and confirmed `__arglist` can never be lowered by the transform (roslyn #84263). The exact
   full set of triggering constructs was not located (OQ-24).
7. **Runtime-side opt-outs** — "Methods that are already pooled opt out of runtime-async" (Preview 6,
   dotnet/runtime #128943), a JIT/VM decision for pooled `ValueTask` methods, not a compiler one.
8. **Mono, browser, wasi, android, Apple mobile.**

**Async lambdas and async local functions ARE converted** when their (inferred) return type is one of the
four; `IsRuntimeAsyncEnabledIn` optimistically assumes runtime async during lambda return-type inference and
busts the binding cache if the inferred type turns out not to be Task-like.

**Mixed behaviour within one assembly is normal and expected.**

The per-method escape hatch is **not defined in the BCL**; the user must declare it:

```csharp
namespace System.Runtime.CompilerServices;
[AttributeUsage(AttributeTargets.Method)]
public class RuntimeAsyncMethodGenerationAttribute(bool runtimeAsync) : Attribute();
```

The design document calls it "an escape hatch for experimentation… It may be removed when the feature ships
in stable." It overrides the feature flag in **both** directions.

### N11-ASYNC-08 — Observable behaviour: stack traces, debugging, profiling, contexts

**Live stack traces.** For three nested `async Task` local functions awaiting each other, the trace drops
from **13 frames to 5**: without runtime async each level contributes the method, an
`AsyncMethodBuilderCore.Start<TStateMachine>` frame and the method again. **Important caveat, verbatim:**
"Exception stack traces (from `catch (Exception ex)`) already look the same with or without Runtime Async,
because existing `ExceptionDispatchInfo` cleanup in compiler-generated code handles that case. The
improvement is in what you see *during* live execution." So the difference shows up in `new StackTrace()`,
profilers, diagnostic logging and the debugger Call Stack window — **not** in `Exception.StackTrace`. There
is no longer a `<M>d__N` state-machine type and therefore no `MoveNext` frame.

**Two `MethodDesc` variants per metadata token.** dotnet/runtime PR #123644 (merged 2026-02-06): "Runtime
Async methods generate two `MethodDesc` variants with the same metadata token: an async 'thunk'
(Task-returning adapter) and the actual async method implementation." **This affects profiler `FunctionID`
handling, ReJIT, and DAC/ICorDebug consumers.** The PR adds an `AsyncThunkStubManager`; breakpoints are no
longer bound to a thunk by module/token id (binding requires an exact `MethodDesc`);
`DebuggerStepper::TriggerPatch` traces through the thunk to the real async variant and patches it; and
frames use `IsDiagnosticsHidden()` so async thunks, IL stubs and wrapper stubs are hidden from debugger
stack walks. Preview 6 went further: "The JIT now compiles a dedicated runtime-async version of a
synchronous, task-returning method rather than delegating to it through a thunk" — **so even a non-`async`
`Task`-returning method can get a JIT-generated async variant.**

**`ExecutionContext`, `SynchronizationContext` and `AsyncLocal<T>`: no semantic change.** Every leaf await
helper calls `state.CaptureContexts()` before suspending, and an `AsyncContexts` struct restores both on the
way out. `ContinuationFlags` encodes `ContinueOnThreadPool`,
`ContinueOnCapturedSynchronizationContext` and `ContinueOnCapturedTaskScheduler` plus an index to a stored
`ExecutionContext`, so `ConfigureAwait(false)` / `(true)` behave as before and `AsyncLocal<T>` flows as
before. The one *performance* change — continuations opting out of `ExecutionContext` capture and restore
when there is nothing to restore (Preview 6, #128323) — applies to **both** async models, so it is not a
runtime-async behaviour difference. Preview 7 fixed a correctness bug where contexts were skipped for
`ValueTask`-returning methods (#129890). User `[ThreadStatic]` fields are unaffected (they were never
flowed).

**Profiling.** "The async profiler now instruments both runtime-async methods and compiler-generated async
state-machine methods, so tools receive one consistent event model regardless of async implementation
style."

### N11-ASYNC-09 — Debug information for runtime-async methods

**Runtime-async methods emit NO state-machine debug information at all.** Because `stateMachineTypeOpt` is
null and the method is not a `SynthesizedStateMachineMethod` named `MoveNext`:

| Debug record | Emitted for a runtime-async method? |
|---|---|
| `StateMachineMethod` table (0x36) row | **No** |
| `AsyncMethodSteppingInformationBlob` CDI (`54FD2AC5-…`) | **No** |
| `StateMachineHoistedLocalScopes` CDI (`6DA9A61E-…`) | **No** |
| Hoisted-local and awaiter slots in the EnC state | **No** |
| `EncStateMachineStateMap` CDI (`8B78CD68-…`) | **No** |
| Ordinary `MethodDebugInformation` sequence points | **Yes** |
| Ordinary `LocalScope` / `LocalVariable` rows | **Yes** (the locals are real IL locals) |
| `EncLocalSlotMap` / `EncLambdaAndClosureMap` | **Yes**, when `EnableEditAndContinue` and full metadata |

**The replacement lives entirely in JIT native debug information**, not in the PDB:
`ICorDebugInfo::SourceTypes` gained `ASYNC = 0x20` ("Indicates suspension/resumption for an async call");
the implicit-argument IL numbers gained `ASYNC_CONTINUATION_ILNUM = -4`; and three new structures describe
suspension points — `AsyncInfo { uint32 NumSuspensionPoints; }`,
`AsyncSuspensionPoint { uint32 DiagnosticNativeOffset; uint32 NumContinuationVars; }` and
`AsyncContinuationVarInfo { uint32 VarNumber; uint32 Offset; }`. So the "hoisted local scope" role is taken
over by `AsyncContinuationVarInfo` (IL variable number → byte offset inside the continuation object) and the
"async stepping information" role by `ASYNC`-flagged entries in the native-to-IL `OffsetMapping` plus
`AsyncSuspensionPoint.DiagnosticNativeOffset`, which matches `ResumeInfo.DiagnosticIP` on the continuation
object. None of this is in the PDB; it is produced by the JIT and consumed through the DAC/DBI.

Runtime-async lowering **does** insert hidden sequence points: one before the `this` store when a
struct/type-parameter receiver must be hoisted, and one wrapping the "if not completed, suspend" branch of
every `await` (both the statically typed and the dynamic form). The awaiter temp uses
`SynthesizedLocalKind.Awaiter` (33) and the `this` spill uses `AwaitByRefSpill` (29); both are long-lived,
so both are recorded in the EnC local slot map.

Roslyn PR #82236 (2026-02-10) fixed four "is this an async state machine?" tests to be runtime-async aware:
the closing-brace sequence point in `CodeGenerator.HandleReturn`;
`SynthesizedClosureMethod.GenerateDebugInfo` (which is why local functions and lambdas now get sequence
points); `LocalStateTracingInstrumenter.InstrumentBlock`; and the implicit-return sequence point in
`LocalRewriter_ReturnStatement` / `_Yield`. The tracking issue "Debug experience for runtime async"
(roslyn #79793) was **closed 2026-06-02** with "Don't believe there's anything left to do here", although
the design document still carries the unresolved TODO "Clarify with the debugger team where NOPs need to be
inserted for debugging/ENC scenarios."

### N11-ASYNC-10 — What a source-level tool must do (and what an IL-level tool must do)

**Source level: essentially nothing.** Runtime async is applied *after* binding, in
`MethodCompiler.LowerBodyOrInitializer`, downstream of closure conversion and iterator rewriting and
upstream of code generation. A tool that produces C# syntax (or bound nodes) and lets Roslyn lower it needs
no changes. Source generators are likewise transparent: generated trees are bound and lowered by the same
compiler with the same `Features` value.

The specific things to check:
- **`AwaitExpressionInfo.GetAwaiterMethod`, `.IsCompletedProperty` and `.GetResultMethod` may now be
  `null`.** This is the one place where **binding changes**. `Microsoft.CodeAnalysis.CSharp.AwaitExpressionInfo`
  gained `IMethodSymbol? RuntimeAwaitMethod`, and for a supported task type `GetAwaitableExpressionInfo`
  short-circuits to the helper, so the other three are never computed. (`RuntimeAwaitMethod` is **already
  shipped**, so it exists in the Roslyn that ships with .NET 10 / VS 2026 as well.) `IAwaitOperation`
  exposes only `Operation`; nothing was added there.
- **`RuntimeCapability.RuntimeAsyncMethods = 9`** reports what the *target framework* can do, not whether
  the flag is on for this compilation. It **already existed in Roslyn 5.0**.
- **Never emit `[MethodImpl(MethodImplOptions.Async)]`** in generated source (CS9330).
- **Do not assume a `<M>d__N` state machine type, an `[AsyncStateMachine]` attribute, an
  `IAsyncStateMachine` implementation or an `AsyncTaskMethodBuilder` field** when reading back a compiled
  assembly — in particular when reading the .NET 11 framework assemblies, which are all built with the flag.
- A tool that performs its own async lowering before Roslyn sees the code would produce a non-`async` method
  and forfeit the feature; the result is correct but inconsistent with the rest of the assembly.

**IL level: this is where the feature breaks things.** A post-compile IL rewriter will encounter, in any
assembly compiled with `runtime-async=on` — **including every .NET 11 runtime library**: `MethodDef` rows
with `MethodImplAttributes` bit 0x2000 set, which older metadata libraries do not know and some validate
away; **method bodies whose `ret` does not match the signature return type**, so naive verification fails
and naive "wrap the body and return the original value" weaving produces invalid IL; **no
`IAsyncStateMachine` type, no builder field, no `[AsyncStateMachine]` attribute and no `MoveNext`**, so
tools that locate the state machine to instrument `await` boundaries find nothing; calls to
`AsyncHelpers.*`, which are "only legal to call inside async methods", so moving such a call into a helper
or inlining an async body into a non-async method produces invalid IL; a prohibition on introducing `tail.`
or `localloc`; and two `MethodDesc` variants per metadata token at run time.

### N11-ASYNC-11 — Timeline and performance work (context)

| Milestone | State |
|---|---|
| .NET 9 | Experiment in `dotnet/runtimelab @ feature/async2-experiment`, using a **completely different metadata encoding** (`int32 modopt(Task`1)` on the return type, plus `BindingFlags.Async2Visible`). **None of that shipped.** Do not use that document as a .NET 11 reference |
| .NET 10 | "Available for local testing"; required `net10.0` + `EnablePreviewFeatures` + `Features=runtime-async=on` + `DOTNET_RuntimeAsync=1`. Runtime, compiler, reflection, ilasm/ildasm and `Reflection.Emit` support "code complete". Public API approved (#114310) |
| 11 P1 | CoreCLR on by default (no env var); NativeAOT foundation. Runtime libraries **not** yet built with it |
| 11 P2 | Live stack traces; debugger breakpoint and stepping support (#123644) |
| 11 P3 | `[RequiresPreviewFeatures]` removed (#124488); R2R and NativeAOT land; continuation reuse and unchanged-local elision |
| 11 P4 | **Runtime libraries built with `runtime-async=on`**; `DOTNET_RuntimeAsync` removed (#125406); covariant `Task`→`Task<T>` thunks; crossgen2 inlining unblocked |
| 11 P5 | On-stack replacement resuming directly into optimized code (suspension-heavy benchmark 6357 ms → 457 ms); `IValueTaskSource` continuation reuse; RISC-V64 R2R fix |
| 11 P6 | JIT compiles a dedicated async version instead of a thunk; tail-merged suspension points; cached continuations; pooled methods opt out; `ExecutionContext` capture elision |
| 11 P7 | Tiered compilation for async versions (previously they bypassed tiering and always ran tier-0 code); `TransparentAwait` inlining (100 M completed-task awaits 191 ms → 32 ms); Task/ValueTask factory intrinsics; implicit tailcalls re-enabled; `Task.Yield()` allocation elision; uniform async profiler events |
| GA | Documented at Preview 7 as a **preview feature, opt-in per project**, with the runtime libraries themselves already using it |
---

## 6. SDK, MSBuild and NuGet

### N11-SDK-01 — Preprocessor symbols for `net11.0`

No new symbol *shapes*; the mechanical rule ("replace dots and hyphens with underscore, uppercase")
produces:

| Kind | Symbols |
|---|---|
| Versionless | `NET`, `NETCOREAPP` |
| Version-specific | `NET11_0` |
| `_OR_GREATER` (.NET 5+ shape) | `NET11_0_OR_GREATER`, `NET10_0_OR_GREATER`, `NET9_0_OR_GREATER`, `NET8_0_OR_GREATER`, `NET7_0_OR_GREATER`, `NET6_0_OR_GREATER`, `NET5_0_OR_GREATER` |
| `_OR_GREATER` (legacy .NET Core shape) | `NETCOREAPP3_1_OR_GREATER` … `NETCOREAPP1_0_OR_GREATER` |
| Always | `TRACE`; `DEBUG` in Debug |

Generated by `GenerateTargetFrameworkDefineConstants`, `GenerateTargetPlatformDefineConstants` and
`GenerateNETCompatibleDefineConstants` in `Microsoft.NET.Sdk.BeforeCommon.targets`, driven by the
`_NETCoreAppVersionsForDefines` item ("Full list used for compiler define generation (NET*_OR_GREATER);
never filtered by VS/MSBuild version"), to which the .NET 11 SDK appends `.NETCoreApp,Version=v11.0`.
Opt out with `DisableImplicitFrameworkDefines`.

**Collision hazard.** `NET11` (no underscore) is the **.NET Framework 1.1** symbol and `NET11_OR_GREATER`
is defined for .NET Framework 2.0 and later. The .NET 11 symbols carry the `_0` suffix, so there is no
literal collision, but **a hand-written `#if NET11` intended for .NET 11 silently means .NET Framework 1.1
and will never be true.**

learn.microsoft.com's `standard/frameworks` page and `includes/preprocessor-symbols.md` still stop at
`NET10_0`; they are maintained as "latest stable" pages.

### N11-SDK-02 — Default `LangVersion` for `net11.0` is 15.0

The default is **not** in the .NET SDK; it is in Roslyn's MSBuild targets
(`src/Compilers/Core/MSBuildTask/Microsoft.CSharp.Core.targets`, deployed as
`<dotnet>/sdk/<ver>/Roslyn/Microsoft.CSharp.Core.targets`). The formula is unchanged —
`_MaxSupportedLangVersion = 9 + (major - 5)` for `.NETCoreApp >= 5.0`, `8.0` for `netcoreapp < 5.0` and
`netstandard2.1`, `7.3` for everything else — and **the cap moved**:

```xml
<_MaxAvailableLangVersion>15.0</_MaxAvailableLangVersion>   <!-- was 14.0 in SDK 10.0.400 -->
```

| TFM | default `LangVersion` under the .NET 11 SDK |
|---|---|
| `net472` (any `.NETFramework`) | `7.3` |
| `netstandard2.0` | `7.3` |
| `netstandard2.1` | `8.0` |
| `netcoreapp3.1` | `8.0` |
| `net8.0` | `12.0` |
| `net9.0` | `13.0` |
| `net10.0` | `14.0` |
| **`net11.0`** | **`15.0`** |

**This is per-TFM**, so a multi-targeted build gets a different language version per leg unless
`LangVersion` is set explicitly. `MaxSupportedLangVersion` is also exposed as a property.

`EnablePreviewFeatures=true` still forces `LangVersion=Preview`, but only when
`IsNetCoreAppTargetingLatestTFM` — and `NETCoreAppMaximumVersion` moves from `10.0` to **`11.0`**, so under
the .NET 11 SDK that applies to the `net11.0` leg.

learn.microsoft.com explicitly warns against `LangVersion=latest`: "The `latest` setting means the installed
compiler uses its latest version. The value of `latest` can change from machine to machine, making builds
unreliable."

**Note (RES-13).** One research pass raised doubt because the Preview 6 and 7 release notes still instruct
users to set `<LangVersion>preview</LangVersion>` for unions and extension indexers, even though
`MessageID.cs` maps both to `CSharp15`. That instruction is consistent with `CSharp15` not yet being the
*default* during the preview SDKs. The `_MaxAvailableLangVersion = 15.0` value read directly from Roslyn
`main`, plus the learn `language-versioning` table showing ".NET 11.x → C# 15", settle the GA answer:
**C# 15 is the default for `net11.0`.**

### N11-SDK-03 — `ImplicitUsings` gains `System.Net.Http.Json` on `net11.0`

```xml
<ItemGroup Condition="'$(ImplicitUsings)' == 'true' Or '$(ImplicitUsings)' == 'enable'">
  <Using Include="System" />
  <Using Include="System.Collections.Generic" />
  <Using Include="System.IO" />
  <Using Include="System.Linq" />
  <Using Include="System.Net.Http" Condition="'$(TargetFrameworkIdentifier)' != '.NETFramework'"/>
  <Using Include="System.Net.Http.Json"
         Condition="'$(TargetFrameworkIdentifier)' == '.NETCoreApp' And $([MSBuild]::VersionGreaterThanOrEquals('$(TargetFrameworkVersion)', '11.0'))" />  <!-- NEW -->
  <Using Include="System.Threading" />
  <Using Include="System.Threading.Tasks" />
</ItemGroup>
```

So `net11.0` with `ImplicitUsings=enable` gets **eight** global usings; `net10.0` and lower get seven (six
on `.NETFramework`). **This is a real source-compatibility surface**: a user type named `JsonContent`, or an
extension method named `GetFromJsonAsync` in an unimported namespace, can newly become ambiguous on the
`net11.0` leg. Combined with N11-BCL-10, this is the entire reference-set delta that can change name
resolution.

`ImplicitUsings` itself still has **no SDK default**; the project templates set it. Unchanged in the same
file: `<NoWarn>$(NoWarn);1701;1702</NoWarn>`, `<WarningsAsErrors>$(WarningsAsErrors);NU1605</WarningsAsErrors>`,
`<DefineConstants>$(DefineConstants);TRACE</DefineConstants>`.

### N11-SDK-04 — `AnalysisLevel`, `WarningLevel`, `SdkAnalysisLevel`

```xml
<!-- Microsoft.NET.Sdk.Analyzers.targets, dotnet/sdk main -->
<_NoneAnalysisLevel>4.0</_NoneAnalysisLevel>
<_LatestAnalysisLevel>11.0</_LatestAnalysisLevel>     <!-- was 10.0 -->
<_PreviewAnalysisLevel>12.0</_PreviewAnalysisLevel>   <!-- was 11.0 -->
```

The surrounding logic is unchanged, so under the .NET 11 SDK a `net11.0` project with no explicit
`AnalysisLevel` gets `AnalysisLevel = latest`, resolving to `EffectiveAnalysisLevel = 11.0`, while a
`net10.0` project gets the literal `10.0`. `AnalysisLevel=preview` now means **12.0**, and the compound
forms (`latest-all`, `preview-none`, `11.0-recommended`, …) shift with it.

**`AnalysisLevel=latest` now really means .NET 11 rules.** Independently, the .NET 11 SDK **fixed a bug**
where `AnalysisLevel=latest` was resolving to **.NET 9** rules (dotnet/sdk issue #52467). **Any project
pinned to `latest` therefore sees two releases' worth of new default-on CA rules arrive at once.**

**`WarningLevel`** (C# warning waves) — logic unchanged, but the value is the TFM's major version:

```xml
<WarningLevel Condition="'$(Language)' == 'C#' And '$(WarningLevel)' == '' And '$(AnalysisLevel)' == 'preview'">9999</WarningLevel>
<WarningLevel Condition="'$(Language)' == 'C#' And '$(WarningLevel)' == '' And '$(TargetFrameworkIdentifier)' == '.NETFramework'">4</WarningLevel>
<WarningLevel Condition="'$(Language)' == 'C#' And '$(WarningLevel)' == '' And '$(TargetFrameworkIdentifier)' == '.NETCoreApp'">$(_TargetFrameworkVersionWithoutV.Split('.')[0])</WarningLevel>
```

So `net11.0` enables **warning wave 11** on that leg only — which, per N11-BREAK-16, currently carries no
diagnostics.

`EnableNETAnalyzers` is unchanged: `true` when `EffectiveAnalysisLevel >= 5.0`, therefore **never for
`netstandard2.0` or `net472`**. `EnforceCodeStyleInBuild` defaults `false`. `AnalysisMode` defaults
`Default`.

**`SdkAnalysisLevel`** defaults to the running SDK's feature band, i.e. **`11.0.100`**, and is
**SDK-wide, not per-TFM**. Two documented behaviours attach to `11.0.100`: **`NU1703`** warns for packages
using deprecated `MonoAndroid` framework assets, and **`NU1019`** becomes an **error** (was a warning) for
non-ASCII characters in `TargetFramework`. **New ageing-out rule: "The behavior enabled by the
`SdkAnalysisLevel` value ages out after three major releases. For example, version `11.0.100` only respects
values down to `8.0.100`"** — so pinning to `7.0.x` no longer has any effect.
`<SdkAnalysisLevel>10.0.100</SdkAnalysisLevel>` is the escape hatch for the whole project.

Analyzer behaviour changes that arrive with the SDK rather than the TFM: **CA1873** reworked (no longer
flags property accesses, `GetType()`, `GetHashCode()`, `GetTimestamp()`; applies only to Information-level
logging and below by default; the message now names one of nine reasons); **CA1515** and **CA1034** false
positives with C# extension members fixed; **CA1859** default-interface-implementation handling fixed;
**CA1033** no longer reported for interfaces with default implementations; **CA2007** no longer reported
for pattern-based `await using` / `await foreach`; **CA1860** works with abstract collections. New in
Preview 1: CA1517, CA1830, CA1876, CA1877, CA2026, CA2027.

### N11-SDK-05 — SDK defaults that do NOT change for `net11.0`

`Nullable` (no SDK default; the templates set `enable`); `Deterministic` (`true`, unconditional);
**`ChecksumAlgorithm` (SHA-256, and **not set by `Microsoft.NET.Sdk` at all** — it comes from
`Microsoft.Common.CurrentVersion.targets` and the csc default); `DebugType` (`portable`);
`OutputType` (`Library`); `AllowUnsafeBlocks`, `TreatWarningsAsErrors`, `SignAssembly` (`false`);
`GenerateDocumentationFile` (`false` unless `DocumentationFile` is set); **`ProduceReferenceAssembly`
(`true` for `.NETCoreApp >= 5.0` C#/VB, `>= 7.0` for `.fsproj`; **not** defaulted for `netstandard2.0` or
`net472`)**; `InvariantGlobalization` (`false`); `PublishTrimmed` (`false`, forced `true` when
`PublishAot=true`); `TrimMode` (`full`); `IsTrimmable` / `IsAotCompatible` (unset); `AnalysisMode`
(`Default`); `RollForward` (unset, host default `Minor`; `LatestMinor` when `EnableDynamicLoading=true`).

The only property the msbuild-props page documents as new in .NET 11 is **`UseNativeLibPrefix`**.

**Roll-forward is unchanged.** `RollForward` is written into `.runtimeconfig.json` by
`GenerateRuntimeConfigurationFiles`, participates in the runtimeconfig input hash, and requires TFM >= 3.0
(`NETSdkError` `RollForwardRequiresVersion30` otherwise). Valid values: `Minor`, `Major`, `LatestPatch`,
`LatestMinor`, `LatestMajor`, `Disable`.

**`NETSDK1045`** ("The current .NET SDK does not support 'X' as a target") is raised when
`_TargetFrameworkVersionWithoutV > NETCoreAppMaximumVersion`. With the .NET 10 SDK, `net11.0` produces it;
with the .NET 11 SDK, `NETCoreAppMaximumVersion` becomes `11.0` and `net12.0` produces it instead.
`SupportedNETCoreAppTargetFramework` is filtered by MSBuild version so an older Visual Studio does not offer
a TFM it cannot build; the exact guard the .NET 11 SDK places on `net11.0` is **unverified** (OQ-25).

### N11-SDK-06 — New and changed MSBuild properties in the .NET 11 SDK

| Property | Preview | Meaning |
|---|---|---|
| `PublishReferenceSymbols` | P1 | whether `.pdb` files from **referenced projects** land in publish output |
| `AppendPublishRuntimeIdentifierToRuntimeIdentifiers` | P6 | escape hatch for multi-RID publish workloads |
| `CheckSdkVulnerabilities` | P5 | opt in to SDK vulnerability / end-of-life checks during build |
| `LocalRegistry` | P7 | now accepts `Docker`, `Podman`, **`Wslc`**, **`MacOSContainer`** |
| `UseNativeLibPrefix` | — | default `true`; NativeAOT Unix native libraries get a `lib` prefix |
| **`BuildProjectReferences`** | P7 | **behaviour change: now defaults to `false` when `NoBuild=true`** |
| `RestoreEnableAnalyzerAssets` | P7 (NuGet) | opt in, per TFM, to the `analyzers` asset group — see N11-SDK-12 |
| `RuntimeEnvironmentVariableSupport` / `@(RuntimeEnvironmentVariable)` | P7 | projects that declare support receive `dotnet run -e` / `dotnet test -e` values as items |
| `ComputeToolPackageRuntimeIdentifiersToPack` | P7 | a target a tool-package author implements to declare buildable RIDs |
| `ComputeAvailableDevices` | P1/P7 | target called by `dotnet watch` / `run` / `test` for MAUI device selection |
| `AdditionalEndpointDefinitions` | P4 | new parameter on `DefineStaticWebAssetEndpoints` |
| **`FileBasedProgramCanSkipMSBuild`** | — | set `false` to disable the `dotnet run file.cs` fast path (N11-SDK-17) |

**New SDK warning `NETSDK1235`**: emitted when `PackAsTool=true` **and** a custom `NuspecFile` is specified;
pack still proceeds. (Whether this is the only new `NETSDK*` identifier is unverified — the negative comes
from two documentation pages, not from a diff of `Strings.resx`; OQ-26.)

### N11-SDK-07 — MSBuild server is enabled by default

"The MSBuild server is now enabled by default. This keeps a warm MSBuild worker between CLI invocations."
Opt out with `DOTNET_CLI_USE_MSBUILD_SERVER=false` **or** `MSBUILDUSESERVER=0`.
`DOTNET_CLI_USE_MSBUILD_SERVER=false` is **authoritative**: it forwards `MSBUILDUSESERVER=0` so the server
cannot be silently re-enabled by response files, `MSBUILDFORCEMULTITHREADED=1` or `/mt`. Separately
(Preview 6), the CLI **no longer unconditionally writes `MSBUILDUSESERVER=0`**: if
`DOTNET_CLI_USE_MSBUILD_SERVER` is unset, `MSBUILDUSESERVER` is left alone.

**Consequence: a warm, long-lived MSBuild process that persists across `dotnet build` invocations is now
the default.** Anything that caches per-build state in static fields of a task assembly, or assumes the
MSBuild process dies at the end of a build, now sees process reuse by default. This compounds the existing
situation where VBCSCompiler and MSBuild worker nodes outlive a build and hold user assemblies loaded.

Engine changes: server GC is now available even with `-nr:false` (`-mt` uses the server anyway, spawning a
**short-lived server that tears itself down after the build**); a new structured event
**`MSBuildServerLifecycleEventArgs`** reports spawned / spawned-short-lived / reused / not-used plus the
**server process ID** (logged at low importance, so it appears in binary logs and at `-v:diag`, not in
default console output); nested MSBuild processes no longer deadlock. Server GC on the server node gives
roughly 10–13% faster builds on large solutions at about 300 MB extra peak memory; worker nodes and
TaskHosts keep Workstation GC.

### N11-SDK-08 — MSBuild multithreaded mode `-mt`, and the `Csc` task

Introduced in **MSBuild 18.6**, still **experimental and CLI-only** in Preview 7. Visual Studio **does not**
support it: "In Visual Studio, all task execution continues to run out of process."

```
dotnet build -mt
dotnet msbuild -mt MySolution.sln
```

`-mt` builds a solution's projects **concurrently inside one MSBuild process**. **Task execution location
depends on an attribute, not an interface:**

- A task annotated **`[MSBuildMultiThreadableTask]`** runs **in-process**, sharing the process with every
  other project being built.
- **Every other task runs isolated in a long-lived sidecar `TaskHost` process** dedicated to its node.
  Existing tasks keep working unmodified, just slower because of the process hop.

**`MSBuildMultiThreadableTaskAttribute` is matched by namespace plus type name only, ignoring the defining
assembly**, so a task author may declare it locally as a compatibility bridge that works on both old and
new MSBuild:

```csharp
namespace Microsoft.Build.Framework
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class MSBuildMultiThreadableTaskAttribute : Attribute { }
}
```

The attribute is **`Inherited = false`**: a derived class does **not** inherit multithreadability.

```csharp
public interface IMultiThreadableTask : ITask { TaskEnvironment TaskEnvironment { get; set; } }
```

Initialise the property to `TaskEnvironment.Fallback` so the task works outside the engine.
`TaskEnvironment` members: `ProjectDirectory`, `GetAbsolutePath()`, `GetEnvironmentVariable()`,
`SetEnvironmentVariable()`, `GetProcessStartInfo()`. **`TaskEnvironment` is itself not thread-safe** —
capture values into locals before spawning your own threads.

New value type `AbsolutePath` (readonly struct in `Microsoft.Build.Framework`) with `Value`,
`OriginalValue`, a validating constructor, a base-path constructor and an implicit conversion to `string`.
**Typed task parameters (Preview 7):** parameters and outputs may be `AbsolutePath`, `FileInfo`,
`DirectoryInfo`, and the new generic **`ITaskItem<T>`**; the engine validates absoluteness against the
task's own `TaskEnvironment`. A Roslyn analyzer package to guide this migration is planned for a future
release.

**Constructor injection (Preview 7):** the engine looks for a public instance constructor taking a single
`TaskEnvironment` and calls it, falling back to the parameterless constructor. **Warning from the release
notes: a task that drops its parameterless constructor entirely "won't load in older MSBuild hosts, such as
SDKs earlier than .NET 11 or Visual Studio versions that ship before the November release."**

API-replacement table (all ERROR-level except the last):

| .NET API to avoid | Replacement |
|---|---|
| `Path.GetFullPath(path)` | `TaskEnvironment.GetAbsolutePath(path)` |
| `File.*` / `Directory.*` with relative paths | resolve with `TaskEnvironment.GetAbsolutePath()` first |
| `Environment.GetEnvironmentVariable()` | `TaskEnvironment.GetEnvironmentVariable()` |
| `Environment.SetEnvironmentVariable()` | `TaskEnvironment.SetEnvironmentVariable()` |
| `Environment.CurrentDirectory` | `TaskEnvironment.ProjectDirectory` |
| `new ProcessStartInfo()` | `TaskEnvironment.GetProcessStartInfo()` |
| `Process.Start()` | `ToolTask` or `TaskEnvironment.GetProcessStartInfo()` |
| static fields (WARNING) | instance fields, thread-safe collections, or `IBuildEngine4.RegisterTaskObject` with `RegisteredTaskObjectLifetime.Build` |

**The `Csc` task is already marked multithreadable.** `dotnet/roslyn` `src/Compilers/Core/MSBuildTask/Csc.cs`:

```csharp
[MSBuildMultiThreadableTask]
public class Csc : ManagedCompiler
```

`ManagedCompiler : ManagedToolTask : ToolTask` do **not** carry the attribute (it is non-inheritable) but
already consume `TaskEnvironment` throughout (`TaskEnvironment.BuildEnvironment`, `GetTempPath()`,
`ProjectDirectory`, `GetEnvironmentVariable(...)`, `FileExists(itemSpec)`,
`GetFullPathNoThrow(item.ItemSpec)`).

**Direct consequence for anything that replaces `Csc` with its own task:** under `-mt` the real `Csc` runs
in-process while a replacement task **without** the attribute is pushed into a sidecar `TaskHost` process —
a behaviour and performance difference, and a difference in which process holds loaded user assemblies. A
replacement task that **does** carry the attribute must satisfy the whole thread-safety contract above,
because multiple projects will run it concurrently in one process.

Measured: OrchardCore `-t:Rebuild` 146.2 s → 107.8 s on Windows (−26%), 118.8 s → 91.5 s on Linux (−23%).

### N11-SDK-09 — MSBuild engine API changes and two removals

**Partial (stop-after-pass) project evaluation — new public API:**

```csharp
var options = new ProjectOptions { EvaluationStage = ProjectEvaluationStage.Properties };
var project = ProjectInstance.FromFile("MyApp.csproj", options);
project.EvaluationStage;   // Properties
project.Targets.Count;     // 0
```

| `ProjectEvaluationStage` | Stops after |
|---|---|
| `Properties` | pass 1 (properties and imports) |
| `ItemDefinitions` | pass 2 |
| `Items` | pass 3 |
| `UsingTasks` | pass 4 |
| `Full` (default) | pass 5 (targets) |

**Exposed on `ProjectInstance` only.** Passing a non-`Full` stage to `Project.FromFile`,
`FromProjectRootElement` or `FromXmlReader` **throws `ArgumentException`** — an MSBuild breaking change.
The CLI uses it (`msbuild -getProperty:Foo` without `-target` stops after properties, ~15% faster;
`-getItem:Bar` stops after items, ~7% faster), and **`MSBUILDDISABLEFEATURESFROMVERSION=18.10` restores the
historical full evaluation**. The SDK adopted it for `dotnet sln add`, `dotnet reference list` and
release-property lookups.

**Two removed public API members:** **`BuildParameters.IsLongLivedHost` and `MarkProcessAsLongLivedHost()`
are gone** from `Microsoft.Build`. They existed as Preview 5's transient-`TaskHost` workaround for NuGet's
static singleton state (`PluginManager`, `EnvironmentWrapper`); the NuGet `RestoreTask` now uses normal
task-host routing again. **Hosts that called them must remove the calls.**

**Task-host IPC: the environment is now sent as a delta.** Packet version 5. The full build-process
environment (about 6 KB) is sent **once per task-host connection**; unchanged environments are a **1-byte
marker** on both the forward (`TaskHostConfiguration`) and return (`TaskHostTaskComplete`) paths. Orchard
Core, 17,975 external task-host invocations: 122.0 MB → 0.1 MB (−99.8%). No project or task changes needed.

Other engine changes: **faster metadata expansion** (`%(...)` moved from `Regex.Replace` to a
zero-allocation `ref struct` scanner; `Metadata_Unqualified` 413 ns → 124 ns, 624 B → 0 B; **no opt-out**);
a **trim- and Native-AOT-clean evaluation object model** (open-world reflective paths — loading tasks, SDK
resolvers, loggers and build checks by name — and task execution still require closed-world registration
and "fail observably at run time rather than silently when it's missing");
`ProjectGraphMode.Full`; `MSBuildImportedProject` items exposing the import tree when
`MSBuildProvideImportedProjects` is `true`; per-path locking in `ProjectCollection.LoadProject`.

MSBuild 18.12 ships as **`net472`** (for `MSBuild.exe` / Visual Studio) and **`net11.0`** (for
`dotnet build`); `Microsoft.NET.Build.Tasks` is compiled for **net11.0 and net472**. **`netstandard2.0`
remains the only single TFM that loads in both hosts.**

### N11-SDK-10 — NativeAOT `dotnet` CLI enabled by default

*Preview 6 behind `DOTNET_CLI_ENABLEAOT`; **Preview 7 flipped the default on every platform**.* Listed as a
breaking change. Opt out with `DOTNET_CLI_ENABLEAOT=false` (falsy: `false`, `0`, `no`, `off`; truthy:
`true`, `1`, `yes`, `on`).

Commands fully served from the AOT path: `dotnet --version`, `--info`, `--help`, `dotnet <command> --help`
for every built-in command, `--cli-schema`, `dotnet sdk check`, `dotnet sln list|migrate|remove`,
`dotnet tool list --local`, `tool run`, `tool uninstall --local`, `tool search`. **External-command
resolution and invocation now happens from the AOT path**: global tools, local tools, PATH commands and
app-base commands (`dotnet ef`, `dotnet dev-certs`) are resolved and launched out-of-process **without
booting the managed CLI**, skipping 600–700 ms of startup. Measured: `dotnet tool list` 378 ms → 68 ms;
`dotnet dev-certs https` ~700 ms → 200–220 ms. Still falls back to the managed CLI for anything needing
MSBuild or NuGet in-process: `build`, `run`, `test`, `pack`, `publish`, `sln add`, file-based app execution.
`dotnet --info` emits the workload version, workload list **and MSBuild version** from the AOT binary.

### N11-SDK-11 — SDK footprint, telemetry and CLI changes

**Footprint.** Linux and macOS installers deduplicate assemblies with **symbolic links** (duplicate
`.dll`/`.exe` identified by content hash); 35% of the SDK directory was duplicates — on `linux-x64`,
816 files and 140 MB. Tarball 230 MB → 189 MB (−17.8%), deb 164 → 122 (−25.6%), rpm 165 → 122 (−26.0%),
containers −8…17%. **crossgen is skipped for assemblies that exist only under `DotnetTools/`** — those ship
IL-only, saving a further 23.6 MB. **Windows deduplication is planned for a future preview.**
Consequence: **an assembly that lives only under `DotnetTools/` is IL-only in .NET 11**, and symlinked SDK
layouts surprise code that resolves an assembly by walking the SDK directory or compares file identity by
path.

**Telemetry.** **OpenTelemetry replaces `Microsoft.ApplicationInsights`** in the CLI (Azure Monitor plus
OTLP exporters); same data, same `DOTNET_CLI_TELEMETRY_OPTOUT`. Motivation: NativeAOT friendliness. The
OTLP exporter now also activates on any standard `OTEL_EXPORTER_OTLP_*` variable.

**CLI.** `dotnet sln` can create and edit `.slnf` solution filters (`dotnet new slnf`); `dotnet run -e KEY=VALUE`
(values surface to MSBuild as `RuntimeEnvironmentVariable` items); **"Using launch settings from…" moved
from stdout to stderr**; `dotnet format --framework` for multi-targeted projects and `hidden` severity
support; `dotnet reference add|remove` fall back to the current directory; `dotnet tool exec` and `dnx` no
longer prompt for an extra approval; `dotnet publish` no longer removes native DLLs on subsequent
single-file publishes; `DOTNET_CLI_FORCE_UTF8_ENCODING=false` restores the system-default console encoding;
the `dotnet/templating` repository has been **merged into `dotnet/sdk`**; containers support multi-arch with
Podman and prefer platform-native local runtimes (`wslc` on Windows, `container` on macOS) ahead of Docker
then Podman, and the legacy standalone `containerize` CLI is no longer packaged.

`dotnet watch`: Aspire app-host integration, automatic crash recovery (relaunch on the next relevant file
change), better Ctrl+C for WinForms and WPF, device selection (`--device <id>`), and re-restore when a
device needs a `RuntimeIdentifier` absent from the original restore. Requires
`<MtouchLink>None</MtouchLink>` for iOS Simulator projects.

`dotnet test` in Microsoft.Testing.Platform mode gained `--no-dependencies`, the `DOTNET_TEST_RUNNER`
environment variable, `--use-current-runtime`/`--ucr`, `!`-prefixed `--test-modules` exclusions,
per-assembly counts, live in-flight test display, two-stage Ctrl+C, `--device` / `--list-devices`,
run-level `--timeout` (exit code 3) and `--maximum-failed-tests` (exit code 13),
`Microsoft.Build.Traversal` project support, `--artifacts-path`, `--list-tests json`, `-nologo` variants,
`--no-artifact-post-processing`, and terminal-logger arguments forwarded to MSBuild. Protocol negotiation
covers MTP 1.1.0 / 1.2.0 / 1.3.0 and MTP 2.4 for `CancelSession`.

### N11-SDK-12 — NuGet: the `analyzers` asset group — RES-14

**RES-14 — the restore-side plumbing exists, but the SDK-side consumer does NOT ship in .NET 11.**

One research pass reported the `analyzers` asset group as a .NET 11 feature. A branch-level check shows
otherwise:

| Repo / branch | `RestoreEnableAnalyzerAssets` present? |
|---|---|
| `NuGet/NuGet.Client` `dev` | yes (5 occurrences in `ProjectRestoreMetadata.cs`) |
| `NuGet/NuGet.Client` `release-7.1.x`, `release-7.0.x`, `release-6.16.x` | **no** |
| `dotnet/sdk` `main` (12.0) — `ResolvePackageAssets.cs` | present |
| **`dotnet/sdk` `release/11.0.1xx`** | **absent** |

`dotnet/sdk` PR **#54646**, which teaches `ResolvePackageAssets` to read
`LockFileTargetLibrary.AnalyzerAssets` instead of scanning files, is milestone **12.0-preview1** and merged
2026-08-26.

**Therefore, in .NET 11 GA:** restore does **not** write an `analyzers` group into `project.assets.json`;
the SDK still discovers analyzers by scanning every file in every package
(`WriteAnalyzerPackageFiles` → `NuGetUtils.IsApplicableAnalyzer`); and **`ExcludeAssets` / `PrivateAssets` /
`IncludeAssets` are still not honoured for analyzers** (the long-standing dotnet/sdk #1212 behaviour;
`PrivateAssets="all"` on a `PackageReference` still stops the reference flowing transitively and remains the
only usable workaround; the default `PrivateAssets` value is `contentfiles;build;analyzers`). The compiler
never sees any of this: `csc` receives a flat list of `/analyzer:` paths from the `Csc` task's `Analyzers`
item and applies no asset-group logic.

The Preview 7 NuGet notes describe the restore-side shape, and the **call to action for analyzer package
authors is real regardless of when the SDK side lands**: lay analyzer assemblies out under
`analyzers/<codeLanguage>/` with a `roslynX.Y` segment when shipping compiler-API-specific builds, so they
are represented correctly in the new asset group. In the recorded JSON each analyzer assembly is annotated
with **`codeLanguage`** (`cs`, `vb`, `fs`, `any`) and **`compilerApiVersion`** (`roslynX.Y`); excluded
analyzers become the `_._` placeholder.

**Folder layout and selection (unchanged, and this part IS live in .NET 11).** The NuGet convention is
`$/analyzers/{framework_name}{version}/{supported_architecture}/{supported_language}/{name}.dll`, where
`dotnet` is the only valid framework name and the language is `cs`, `vb` or `fs` (omitted means all
languages). **The `roslynX.Y` segment is not in that document** — it is an SDK convention implemented in
`ResolvePackageAssets.AnalyzerResolver`, and it is located by a **raw substring search for `/roslyn`**
rather than by position, so both of these work:

```
analyzers/dotnet/roslyn4.14/cs/My.Analyzer.dll
analyzers/dotnet/cs/roslyn4.14/My.Analyzer.dll
```

Selection rules: an asset with **no** compiler version is always included; an asset whose version is
**greater than** the project's `CompilerApiVersion` is dropped; among the remaining versioned assets
**within one package**, only those equal to the maximum applicable version are included; state resets per
package.

**For .NET 11, `CompilerApiVersion` will be `roslyn5.12`. This is the first time the Roslyn major version
has moved since 4.x**, so a package shipping only `analyzers/dotnet/roslyn4.x/` folders is still selected
(4.x < 5.12), but a package wanting a .NET 11-specific build must add a `roslyn5.x` folder.

### N11-SDK-13 — Other NuGet changes

- **Restore runs safely under multithreaded MSBuild.** The Restore Task and its supporting tasks were
  migrated to the multithreaded-safe model, so restore produces the same results under `dotnet build -mt`, a
  reused MSBuild Server process, or several concurrent project restores. Previously restore could carry
  stale environment, credential and plugin state between builds sharing a reused process, and could resolve
  a relative path against the wrong project's directory. **New public API for plugin and
  credential-provider authors: a `NuGetProcessState` registry in `NuGet.Common` with
  `RegisterResetAction(ResetKey, Action)` / `Reset(ResetKey)`.** A plugin with process-wide state that must
  be refreshed between reused-process restores should register a reset action the same way NuGet's own
  environment, credential-service and plugin caches do. Static-graph restore, `nuget.exe` and Visual Studio
  builds are unaffected. Note the wording: "making this the default build mode is still coming in a future
  release."
- **Pack reuses existing project evaluations.** `dotnet pack` no longer passes
  `BuildProjectReferences=false` as a global property on its inner MSBuild calls, which used to produce a
  *distinct* evaluation from the ones `Build` had already created — roughly doubling evaluations for every
  affected TFM and project reference in a multi-targeting graph.
- **`NU5052`** — nuget.org is phasing in stricter package-ID rules; new IDs must be ASCII-only. Advisory
  only in .NET 11; pack still produces the package.
- **`NU1703`** warns for packages using deprecated `MonoAndroid` framework assets (gated on
  `SdkAnalysisLevel >= 11.0.100`).
- Restore no longer scans the full version list of the global packages folder or fallback folders when it
  cannot find an exact package version.
- **Nothing was published for .NET 11 about package source mapping, central package management or NuGet
  Audit** (OQ-27). Carried over from NuGet 7.0: projects targeting .NET 10+ default to
  `NuGetAuditMode=all`, and package pruning is enabled for all projects targeting .NET 10+.

### N11-SDK-14 — Source-generator driver: the three-phase pipeline

The generator driver now runs **three sequential passes, each a complete loop over all generators**, not a
per-generator pipeline:

```
1. RegisterPostInitializationOutput      -> source added to the initial compilation (takes no inputs)
2. RegisterPreCompilationSourceOutput    -> NEW: reads non-compilation inputs; source added to the
                                            initial compilation; the compilation is rebuilt
3. RegisterSourceOutput / RegisterImplementationSourceOutput / RegisterHostOutput
                                         -> reads the full compilation, which now includes post-init
                                            AND pre-compilation sources
```

A new internal `enum GeneratorRunPhase { Init, PreCompilation, Standard }`.

**Visibility answers.**
- **One generator's pre-compilation output is NOT visible to another generator's pre-compilation stage.**
  Two independent reasons: the pre-compilation pass runs to completion for *all* generators before any
  pre-compilation tree is added to a compilation; and a pre-compilation callback **has no way to observe a
  compilation at all**.
- **One generator's pre-compilation output IS visible to every other generator's standard stage,
  unconditionally and independently of registration order** (test
  `PreCompilationSource_Is_Visible_To_Other_Generators_ReversedOrder` registers the consumer first
  deliberately).
- **Ordering between generators** is index order in `GeneratorDriverState.Generators` — the order the
  generators were handed to the driver, which for `csc` derives from `/analyzer:` reference order and, within
  one reference, from `AnalyzerFileReference.GetGenerators`. There is no sorting, no priority and no
  dependency graph. It determines only the relative position of trees inside `Compilation.SyntaxTrees`.
- **Deterministic** for a fixed generator list and fixed inputs: each of the three passes is a plain
  sequential `for` loop with no parallelism. (Roslyn assumes generators are themselves deterministic and does
  not enforce it.)
- **Consequence stated only implicitly in the design document: one generator can now change what another
  generator's `SyntaxProvider` / `ForAttributeWithMetadataName` observes.** Post-init output already had this
  property; pre-compilation output extends it to content computed from `AdditionalTexts` and `.editorconfig`.

**Phase enforcement is at run time.** `CompilationProvider` and `SyntaxProvider` **throw** during the
pre-compilation phase:

```
"The compilation is not available during the pre-compilation phase, so {0} cannot be used as an input to a
 pre-compilation source output."
"Syntax-based providers (e.g. SyntaxProvider, ForAttributeWithMetadataName) cannot be used as inputs to a
 pre-compilation source output, because the compilation has not yet been built."
```

Both are wrapped as `UserFunctionException`, so the driver reports a generator error rather than crashing.
(The design document says the `DriverStateTable.Builder` property getters throw; in the shipped code they
carry only a `Debug.Assert` and the throw lives in `SharedInputNodes.GetCompilationOrThrow` and
`SyntaxInputNode.UpdateStateTable`.)

**Providers that DO work pre-compilation:** `ParseOptionsProvider`, `AdditionalTextsProvider`,
`AnalyzerConfigOptionsProvider`, **`CompilationOptionsProvider`** and **`MetadataReferencesProvider`** —
the last two read from the **initial** compilation, "because options and references are unaffected by
source generation". That is itself a behaviour change for the standard phase too, though semantically
equivalent.

**Error handling changes, both new in this wave.** (a) **A suppressed CS8785 no longer hides the exception
from `GeneratorRunResult`**: even with `/nowarn:CS8785`, `GeneratorRunResult.Exception` and `.Diagnostics`
still carry it. (b) **A pre-compilation failure skips that generator's standard phase entirely**
(`GeneratorState.PreCompilationFailed`); other generators are unaffected. Pre-compilation trees are
preserved when the *standard* phase throws (because other generators already saw them) and dropped when the
*pre-compilation* phase throws. **No new diagnostic identifier**: `Init` → **CS8784**, `PreCompilation` and
`Standard` → **CS8785**, so a pre-compilation failure is indistinguishable from a standard-phase failure by
identifier alone.

**Public types are unchanged**: `GeneratorDriverOptions`, `GeneratorDriver`, `GeneratorDriverRunResult`,
`GeneratorRunResult`, `GeneratedSourceResult`, `IncrementalGeneratorRunStep`, `IncrementalStepRunReason`
(no new reason) all gained nothing. But:
- **`GeneratorRunResult.GeneratedSources` now contains post-init sources, then pre-compilation sources, then
  standard sources**, and **there is no flag on `GeneratedSourceResult` saying which stage produced a
  source**. The only way to tell is `TrackedOutputSteps`, and only when `TrackIncrementalGeneratorSteps` is
  on.
- `TrackedSteps` / `TrackedOutputSteps` gain the key `"PreCompilationSourceOutput"`. Per-generator
  `GeneratorRunStateTable.Builder` instances are **shared** across the pre-compilation and standard passes,
  giving a unified view; post-init steps are still never tracked.

**A new `CompilationCache`** keys the augmented compilation on reference equality of the input compilation,
reference-sequence equality of post-init trees, and per-entry equality of
`(GeneratorIndex, HintName, ReferenceEquals(Text), ReferenceEquals(Options))`. **Consequence: once *any*
loaded generator registers a pre-compilation output, the `Compilation` instance handed to *every*
generator's `CompilationProvider` becomes reference-stable across runs when nothing changed, flipping those
generators' `CompilationProvider` steps from re-executing to `Cached` — a behaviour change for generators
that never opted into the feature.** When no generator produced pre-compilation output the cache is
deliberately bypassed, because standard-phase consumers expect the fresh post-init `AddSyntaxTrees`
reference.

`ReuseOrParsePreCompilationSources` reuses a previously parsed tree when the new `GeneratedSourceText` has
the same `SourceText` reference and hint name at the same index, keeping tree references stable so a cached
standard-phase diagnostic's `Location` cannot point at a tree absent from the output compilation.

Two other .NET 11-wave generator fixes worth knowing: **#83878** (2026-05-29) fixed `InputNode`
**selecting the same new item twice and silently dropping a genuinely new one** when an input set was
simultaneously reordered and had items replaced while keeping the same count — affecting
`AdditionalTextsProvider`, `MetadataReferencesProvider`, `AnalyzerConfigOptionsProvider`,
`ParseOptionsProvider` and `CompilationProvider`, with wrong generated output and no diagnostic; and
**#83875** (2026-06-05) frees pooled objects across cancellation exceptions (a leak fix, not a semantic
change).

### N11-SDK-15 — Generated-file paths, hint names and `EmitCompilerGeneratedFiles`

The `SyntaxTree.FilePath` rule is **identical for all three generated-tree kinds**:

```
<BaseDirectory>\<GeneratorAssemblySimpleName>\<GeneratorTypeFullName>\<normalisedHintName>
```

`BaseDirectory` is `GeneratorDriverOptions.BaseDirectory`; for `csc` it is
`Arguments.GeneratedFilesOutputDirectory` when `/generatedfilesout:` was passed, otherwise
`Arguments.OutputDirectory` (the `bin` directory) — **and in that case no file is written, but `FilePath`
still points into `bin`**. `GeneratorTypeFullName` is `Type.FullName`, so a nested generator type
contributes a `+` segment.

Hint-name normalisation: allowed characters are identifier-part characters plus `` . , - + ` _ ``, space,
`( ) [ ] { } / \`; backslashes become forward slashes on every platform; the regex `(\.{1,2}|/|^| )/`
rejects leading `/`, `./`, `../`, `//` and `" /"`; the language extension (`.cs`) is appended when absent;
uniqueness is `OrdinalIgnoreCase`; `source.Encoding` must not be null. **Because `Path.Combine` uses the
platform separator while the hint name keeps `/`, a hint name containing directories produces a
mixed-separator path on Windows.**

**New in this wave: hint names must be unique across phases within one generator.** `UpdateOutputs` takes a
`reservedHintNames` set — the pre-compilation pass reserves the post-init names, the standard pass reserves
post-init **and** pre-compilation names, compared `OrdinalIgnoreCase`. A collision throws `ArgumentException`
→ **CS8785**, and the earlier phase's tree survives. The reserved set is **per generator**, so two different
generators may still use the same hint name; their file paths differ by assembly and type name.

**On-disk emission (unchanged mechanism).**
`EmitCompilerGeneratedFiles` defaults `false`; when `true` and `CompilerGeneratedFilesOutputPath` is empty
it defaults to `$(IntermediateOutputPath)/generated`, and a target creates the directory before
`CoreCompile`. The path becomes `/generatedfilesout:`. Concrete shape:

```
obj\Debug\net11.0\generated\<GeneratorAssembly>\<GeneratorTypeFullName>\MyType.g.cs
```

**Pre-compilation-stage output is written there too, and nothing on disk distinguishes it from post-init or
standard output.** The emission loop iterates `compilation.SyntaxTrees.Skip(Arguments.SourceFiles.Length)`,
which contains all three kinds; the same path feeds `.editorconfig` option lookup
(`GetOptionsForSourcePath(tree.FilePath)`) and `EmbeddedText.FromSource`. There is **no
`EmitPreCompilationGeneratedFiles`, no separate output path and no new `Csc` switch**;
`GeneratorDriverOptions.BaseDirectory` remains the single knob and is still not settable per output kind.

**Ordering caveat.** The compilation the **standard phase** sees is
`[user trees][all post-init trees][all pre-comp trees]`, while the compilation returned to the host by
`RunGeneratorsAndUpdateCompilation` is `[user trees][gen0 post-init][gen0 pre-comp][gen0 standard][gen1 …]`.
**Anything that indexes `SyntaxTrees` positionally must know which compilation it holds.** (The IDE builds
its final compilation itself, so this ordering applies only to the command-line path.)

### N11-SDK-16 — Analyzer and generator loading and isolation: no .NET 11 change

The last structural rework was **PR #77004** ("Rework analyzer assembly loading", merged **2025-03-24**) —
the **.NET 10** wave. It made `AnalyzerAssemblyLoader` `internal sealed partial` and moved customisation to
interfaces: `IAnalyzerPathResolver` (where an assembly and its satellites load from; implementations
`ProgramFilesAnalyzerPathResolver` and `ShadowCopyAnalyzerPathResolver`; "The first instance to return true
from `IsAnalyzerPathHandled(string)` will be considered to be the owner of that path") and
`IAnalyzerAssemblyResolver` (`#if NET` only; "The `AnalyzerAssemblyLoader` will partition analyzers into the
directories they live in and will create a separate `AssemblyLoadContext` for each directory" — **so the
isolation unit on .NET Core is one `AssemblyLoadContext` per directory**).

The **only** commit touching `AnalyzerAssemblyLoader.cs` since 2025-11-01 is
`ShadowCopyAnalyzerPathResolver: Use cache to amortize cost of AV scans (#84765)`, 2026-08-13 — a
performance change with no contract change. **Nothing about analyzer or generator loading or isolation
changes in the .NET 11 wave.**

**Analyzers and source generators must still target `netstandard2.0`**, unchanged and for the unchanged
reason: the compiler loads them into hosts running on .NET Framework (`MSBuild.exe`, `devenv.exe`, the
in-proc VS pipeline) *and* on .NET (`dotnet build`, `csc.dll`), and `netstandard2.0` is the newest framework
both can load. Nothing in the .NET 11 release notes, the breaking-change index or the Roslyn repository
changes this.

### N11-SDK-17 — File-based apps and the `dotnet run file.cs` fast path

**Seven `#:` directive kinds** are recognised by the .NET 11 SDK — `sdk`, `property`, `package`, `project`,
`ref`, `include`, `exclude` — dispatched by a literal `switch`; anything else is
`"Unrecognized directive '{0}'."`, deliberately reserving the namespace.

```cs
#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net11.0
#:property LangVersion=preview
#:package System.CommandLine@2.0.0-*
#:package Microsoft.Build@17.0.0 ExcludeAssets=runtime PrivateAssets=all
#:project ../MyLibrary
#:ref ../lib/lib.cs
#:include ./**/*.cs
```

Grammar: `#:<kind> <name> [<separator> <value>] [Name=Value ...]`, separators `@` (version) and `=`
(property value and item metadata), whitespace permitted around them. A value is required for `#:property`,
optional for `#:package` and `#:sdk`, disallowed for `#:project`, `#:ref`, `#:include`/`#:exclude`. A quoted
value is lexed as a **regular** C# string literal, so escapes are decoded; verbatim and raw literals are
**not** supported; quotes may only enclose a whole value. Trailing MSBuild item metadata is supported by
`#:package`, `#:project` and `#:ref` only. Default extension→item-type mapping
(`FileBasedProgramsItemMapping`): `.cs=Compile;.resx=EmbeddedResource;.json=None;.razor=Content;.dll=Reference`
— **`.dll` → `Reference` is new in Preview 6**. Legacy unquoted values are still accepted and flagged by
analyzer **CA2267** with a code fix.

Shipping history: `#:sdk`/`#:property`/`#:package`/`#:project` in the **.NET 10 SDK**; `#:include` in
**.NET 11 Preview 3** (and SDK 10.0.300); `#:exclude` ungated in **Preview 5**; **directives inside
`#:include`d files processed transitively** in Preview 5; `#:ref` in Preview 5 but **still gated behind the
MSBuild property `ExperimentalFileBasedProgramEnableRefDirective`** (OQ-28); `.dll` in `#:include` in
Preview 6; duplicate `#:sdk`/`#:property`/`#:package` across included files when values match, in
Preview 6; `dotnet reference add --file app.cs` writing `#:project` in Preview 7.
**Documentation lag: `file-based-apps.md` and the learn what's-new page still list only five directives,
omitting `#:exclude` and `#:ref`.**

**Since Preview 5 the SDK collects directives from every `Compile` item, not only the entry point**, so
`IgnoredDirectiveTrivia` can appear in any source file of a file-based app.

**The virtual project.** `GetVirtualProjectPath(entryPointFilePath) => entryPointFilePath + ".csproj"`, so
`app.cs` → `app.cs.csproj`, **beside the source file but existing only in memory**. Defaults:
`OutputType=Exe`, `ImplicitUsings=enable`, `Nullable=enable`, **`PublishAot=true`**, **`PackAsTool=true`**,
`AssemblyName`/`RootNamespace` = the file name, `UserSecretsId` = a hash of the entry-point path,
`EnableDefaultCompileItems=false`, `RestoreUseStaticGraphEvaluation=false`, and
`<Compile Include="{entryPointFilePath}" Exclude="@(Compile)" />`.
**TargetFramework is the SDK's own band — `net11.0` for the .NET 11 SDK.**
**`LangVersion` is not set at all**, anywhere, so the compiler default applies (C# 15 on `net11.0`).

**The load-bearing line for any tooling author:**

```xml
<Features>$(Features);FileBasedProgram</Features>
```

That property reaches `csc` as `-features:FileBasedProgram`. **Any compiler replacement or design-time host
that drops `Features` turns every `#:` directive into CS9298.** Parsing a file-based app source with
default `CSharpParseOptions` produces CS9298 for every `#:` and CS9314 for `#!`.

**Three build levels** (`BuildLevel { None, Csc, All }`). The **`Csc` level bypasses MSBuild entirely**:
`VirtualProjectBuildingCommand` caches the csc command line taken from the `CoreCompile` target's
`Returns="@(CscCommandLineArgs)"` (fed from the `Csc` task's `CommandLineArgs` output), writes it to
`{artifactsPath}/csc.rsp`, and on a later `dotnet run` replays it **straight at the Roslyn compiler server**
(`BuildServerConnection.CreateBuildRequest(..., ["/noconfig", "/nologo", "@<rsp>"])`, pipe name from
`<sdk>/Roslyn/bincore`).

**Consequences:** analyzer and generator references **survive**, because they are `/analyzer:` entries in
the cached response file; but **a custom `Csc` MSBuild task or replacement compiler is NOT re-invoked on
that path** — only stock `csc` runs, against the arguments the custom task reported through
`CommandLineArgs` on the previous full build. Opt out with **`FileBasedProgramCanSkipMSBuild=false`**,
`--no-cache`, or `dotnet build file.cs`. The cache is also refused when the app has a `#:project` or `#:ref`
directive, or a glob `#:include`.

Artifacts live under `{temp}/dotnet/runfile/{fileNameWithoutExtension}-{sha256 of the full path}/bin/{configuration}/`,
created with `0700`; background cleanup every 2 days removes artifacts unused for 30 days (disable with
`DOTNET_CLI_DISABLE_FILE_BASED_APP_ARTIFACTS_AUTOMATIC_CLEANUP=true`; manual `dotnet clean file-based-apps`).

Per-command notes: `dotnet publish file.cs` uses **Native AOT implicitly** (so *building* emits AOT
warnings; opt out with `#:property PublishAot=false`); `dotnet pack file.cs` sets `PackAsTool=true`
implicitly; `dotnet build file.cs` forces a full build; `dotnet project convert` materialises the project on
disk and **removes `#:` and `#!` from the `.cs` files**.

**The shebang.** `SyntaxKind.ShebangDirectiveTrivia = 8922`. `ShebangDirectiveTriviaSyntax.Content` is a
**synthesised** token derived from `EndOfDirectiveToken.LeadingTrivia`, **not a child**, and `Update` still
takes four arguments — asymmetric with `IgnoredDirectiveTriviaSyntax`, whose `Content` **is** a real child
token (five fields: `HashToken`, `ColonToken`, optional `Content` of kind `StringLiteralToken`,
`EndOfDirectiveToken`, `IsActive`) present in `Update` and in the factories. **That node shape changed
between Roslyn 4.14 and 5.0, not between 5.0 and 5.12**: `Content` was added, `Update` went from four to
five arguments, and the `[RSEXPERIMENTAL005]` markers were dropped. A rewriter generated against Roslyn
4.14's grammar calls an `Update` overload that no longer exists.

The shebang rule is `hashPosition != 0 || hash.HasTrailingTrivia` — the `#` must be at **absolute offset 0**
of the `SourceText` and nothing may sit between `#` and `!`. It is **always parsed as a shebang even when
misplaced** (error recovery). **There is no special handling of the shebang anywhere in line mapping**: it
is ordinary leading trivia on the first token, it occupies line 1, and every subsequent line is shifted by
one, so a file executed as `./app.cs` reports its first statement on line 2. **Emitting any C# text before
a shebang, or shifting the file start, converts a valid shebang into CS9378.** Recommended form:

```csharp
#!/usr/bin/env -S dotnet --
```

Analyzer **CA2266** `MissingShebangInFileBasedProgram` warns when the entry point of a multi-file
(`#:include`, and since Preview 7 `#:ref`) file-based app lacks the shebang, because IDEs use it to discover
entry points.

### N11-SDK-18 — `netstandard2.0` and `net472` assets a tool must still ship

**Both are still required; .NET 11 does not relax this.** Analyzers and source generators:
`netstandard2.0`, unchanged. MSBuild tasks: `net472` plus a .NET leg
(`FullFrameworkTFM = net472`, `LatestDotNetCoreForMSBuild = net11.0`; the SDK's own
`Microsoft.NET.Build.Tasks` is built for both).

**The one place .NET 11 does drop `netstandard2.0` is the template engine** (Preview 4, classified
binary/source incompatible): `Microsoft.TemplateEngine.Abstractions`, `.Core`, `.Core.Contracts`, `.Edge`,
`.Orchestrator.RunnableProjects`, `.Utils`, `.IDE` and `Microsoft.TemplateEngine.TemplateLocalizer.Core` now
target only **`net9.0`, `net11.0` and `net472`**. Reason: "NuGet client SDK packages (`NuGet.*`) stopped
targeting `netstandard2.0` starting with **version 7.0**." Public API is unchanged; only the TFM set moved.

**Corollary worth flagging: the NuGet client libraries have not shipped a `netstandard2.0` asset since
NuGet 7.0** (the .NET 10 wave). Any tool that references `NuGet.Protocol`, `NuGet.Configuration` or
`NuGet.Credentials` from a `netstandard2.0` assembly is pinned to NuGet 6.x.

**VSTest removed its dependency on `Newtonsoft.Json`** (Preview 4, binary/source incompatible).
`Microsoft.NET.Test.SDK` no longer brings it transitively; `System.Text.Json` is used on .NET and JSONite on
.NET Framework. Symptoms: compile failures in test projects that used `Newtonsoft.Json` types without a
direct reference, and at run time `FileNotFoundException: Could not load 'Newtonsoft.Json'` for projects
using `<ExcludeAssets>runtime</ExcludeAssets>` and for **test extensions (data collectors, test adapters)**
that relied on VSTest supplying the assembly. Removed public APIs in
`Microsoft.VisualStudio.TestPlatform.CommunicationUtilities`: `Message.Payload`,
`Serialization.DefaultTestPlatformContractResolver`, `TestCaseConverter`, `TestObjectConverter`,
`TestPlatformContractResolver<T>`, `TestResultConverter`, `TestRunStatisticsConverter`, `VersionedMessage`.

Also: **the nine `Microsoft.Extensions.*` Abstractions/Options/Primitives assemblies are now in the .NET 11
shared framework**, so a tool that ships its own copies alongside a .NET 11 host can hit assembly-identity
conflicts.

### N11-SDK-19 — The complete .NET 11 SDK/MSBuild breaking-change list

| Title | Type |
|---|---|
| `dnx` scripts bypass `global.json` SDK selection | Behavioural |
| mono launch target not set for .NET Framework apps | Behavioural |
| **NativeAOT CLI command handling enabled by default** | Behavioural |
| **NU1703 warns for packages that use deprecated MonoAndroid framework assets** | Source incompatible |
| **NuGet pack warns for package IDs with restricted characters (NU5052)** | Behavioural |
| SDK local container runtime selection prefers platform-native tools | Behavioural |
| **Template engine packages no longer support `netstandard2.0`** | Binary/source incompatible |
| **VSTest removes dependency on `Newtonsoft.Json`** | Binary/source incompatible |

Two more from the Preview 7 notes that are **not yet on the compatibility index**:
**.NET tool packages use the portable RID graph** (distributions known only to the legacy graph, some BSD
variants, now need a portable RID entry); and **`NoBuild=true` no longer builds project references** — SDK
projects default `BuildProjectReferences` to `false` when `NoBuild=true`, so `dotnet publish --no-build` and
`dotnet pack --no-build` no longer trigger a hidden `NETSDK1085`. *If a build depends on `--no-build` still
building out-of-date project references, set `BuildProjectReferences=true` explicitly.*

Two MSBuild breaking changes: **`Project.FromFile` rejects partial evaluation** (`ArgumentException`); and
**`BuildParameters.IsLongLivedHost` / `MarkProcessAsLongLivedHost()` were removed** (N11-SDK-09).
---

## 7. Design-time hosts

### N11-IDE-01 — Visual Studio 2027 is an in-place update, version 18.12

learn.microsoft.com `visualstudio/releases/2026/release-rhythm`, verbatim: "beginning with Visual Studio
2026 we plan to deliver new **annual releases each November** along with the new major version of .NET.
These annual releases will be **in-place updates to the prior annual year's release, rather than
side-by-side**." Visual Studio **2027** is scheduled for **November 2026** and replaces Visual Studio 2026
in place.

Stable-channel product version format is `18.<Minor>.<Servicing>`, `<Minor>` incrementing every month
(18.0 = 2025-11-11, 18.3 = 2026-02-10, 18.6 = 2026-05-12, 18.9 = 2026-08-11), so **Visual Studio 2027 =
18.12** *(inference, corroborated by MSBuild `VersionPrefix 18.12.0` and Roslyn `5.12.0` on `main`)*.

Support: each annual release gets one year of feature updates and servicing, then one year of security
servicing as an **LTSC**. "The LTSC Channel for users of the Professional, Enterprise, and Build Tools
editions of Visual Studio 2026 will be available in **November of 2026**" — so in November 2026 a customer
may pin to the Visual Studio 2026 LTSC for one more year rather than take 2027. "Build tools choice": the
IDE is decoupled from the compilers and SDKs it carries, and multiple supported toolset versions ship side
by side.

**No Visual Studio 2027 release notes exist yet**, so its Roslyn version, private-runtime target framework,
ServiceHub runtime and any extension-model change are unconfirmed by a VS-authored document (OQ-29).

### N11-IDE-02 — Four distinct runtimes for the same analyzer assembly

- **`devenv.exe` still runs on .NET Framework 4.8.** Visual Studio 2026 requires .NET Framework 4.8,
  installed by setup if absent; no change was announced for 2027.
- **Roslyn's out-of-process services target `net10.0`** (`NetVS = net10.0` in
  `eng/targets/TargetFrameworks.props`), so `ServiceHub.RoslynCodeAnalysisService` runs on the .NET 10
  runtime or newer by roll-forward. This is a strong inference from the Roslyn build configuration, not a
  statement in a VS document, and it was `net8.0` in Roslyn 5.0.
- **`csc.dll` under `dotnet build`** runs on **.NET 11** by roll-forward (Roslyn's own assemblies still
  target `net10.0`).
- **`csc.exe` under `MSBuild.exe`** runs on .NET Framework 4.7.2+.

**`netstandard2.0` remains the only TFM that satisfies all four.** Unchanged from VS 2022 / 2026.

VS 2026 platform support is documented as ".NET Core 10.0, 9.0, 8.0" plus .NET Framework 4.8.1 through
3.5 SP1 — a page dated 2025-11-11 that predates .NET 11.

**No .NET 11-cycle change to the VS extension model or to how out-of-process analyzers are hosted was found
in any primary source.** The one adjacent change is MSBuild's: **Visual Studio does not support `-mt`
multithreaded in-process task execution.**

### N11-IDE-03 — The IDE runs the pre-compilation generator stage

`CSharpCompilationFactoryService.CreateGeneratorDriver` creates the driver with
`new GeneratorDriverOptions(baseDirectory: generatedFilesBaseDirectory)` — `DisabledOutputs` is `None` and
`TrackIncrementalGeneratorSteps` is `false`. **So the IDE runs the pre-compilation stage.**

`SolutionCompilationState` materialises **every** `GeneratedSourceResult` — post-init, pre-compilation and
standard alike — into a source-generated document keyed by `HintName` and carrying
`SyntaxTree.FilePath`. **Nothing in the workspace layer distinguishes a pre-compilation document.** The IDE
builds its final compilation itself by adding those documents to `compilationWithoutGeneratedFiles`, so the
command-line `SyntaxTrees` ordering rules of N11-SDK-15 do not apply there.

`GeneratorRunResult.GeneratedSources` remains how the IDE detects a generator that did not run:
`default` array = not invoked (must run); non-default empty = ran and produced nothing (may skip);
non-default non-empty = ran and produced documents (may skip).

Whether any other host (Rider, VS Code C# Dev Kit) passes
`IncrementalGeneratorOutputKind.PreCompilation` in `DisabledOutputs` was not investigated (OQ-30).

### N11-IDE-04 — File-based apps in the editor

`LooseDocumentKind { MiscellaneousFileWithNoReferences, MiscellaneousFileWithStandardReferences,
MiscellaneousFileWithStandardReferencesAndSemanticErrors, FileBasedApp }`.

Decision tree: (1) in a loaded project → project-based app. (2) `enableFileBasedPrograms` off → misc file
with no references. (3) not a plain `.cs` → misc with no references. (4) no absolute path / not on disk →
misc with standard references. (5) **has `#!` → file-based app**; restore if needed, show semantic errors.
(6) **has `#:` → file-based app if it has top-level statements**, else misc with standard references.
(7) otherwise, if `enableFileBasedProgramsWhenAmbiguous` is on, heuristics can give
"misc with standard references and semantic errors" (rich misc files, **not restored**).

**Roslyn issue #81252** ("Do not restore loose files which lack file-based app directives", milestone 18.3,
closed 2025-11-21) removed the "no top-level statements" criterion so that **a file is only ever restored
and treated as a file-based app when it carries `#!` or `#:`.** Rationale: the "part of a loaded project"
condition changes ambiently over time (projects load asynchronously, files move), producing frequent
unwanted restore pop-ups and stray artifacts. The replacement is a single **canonical miscellaneous-files
project** under the temp directory (roslyn #80743), giving semantic errors, completion and Quick Info for
the core library without a restore.

**Automatic discovery** across opened workspace folders (roslyn #82863) is controlled by
`dotnet.fileBasedApps.enableAutomaticDiscovery` — **off by default in the stable channel, on in
prerelease**. Excluded: folders containing a `.csproj`, folders named `artifacts`, `bin`, `obj`, and hidden
or dot-prefixed folders. **A discoverable file must start with the byte sequence `0x23 0x21` (`#!`), or
`0xEF 0xBB 0xBF 0x23 0x21` (UTF-8 BOM then `#!`)** — because `#:` will eventually be allowed in
non-entry-point files, and scanning for top-level statements is too expensive for a broad pass. (Note the
BOM is accepted by discovery but **not** by the compiler, whose rule is `hashPosition != 0`.)

Settings: `dotnet.projects.enableFileBasedPrograms` (default `true`; the master switch),
`dotnet.projects.enableFileBasedProgramsWhenAmbiguous` (`false` in release, `true` in prerelease; governs
only the heuristic case).

`FileBasedProgramsProjectSystem` translates the entry-point file into a virtual MSBuild project, runs a
design-time build on it, restores when assets are missing, and **uses file watchers on the project globs to
redo the design-time build when `#:` directives change**.
`FileLevelDirectiveDiagnosticAnalyzer` reports directive-content errors live, with
`DiagnosticId = "FileBasedPrograms"` (literally that string), severity `Error`,
`enforceOnBuild: EnforceOnBuild.Never`, `isConfigurable: false`, gated on
`tree.Options.Features.ContainsKey("FileBasedProgram")`. Completion providers exist for every directive
kind. Formatting explicitly preserves `#:` directives.

**Important: the IDE parses `#:` directive content with the SDK's own code, vendored into
`src/Workspaces/CSharp/Portable/SyncedSource/FileBasedPrograms/`** and kept in sync by
`eng/ensure-sources-synced.cs`, with `SyncedSource/commitid.txt` pinning the dotnet/sdk commit. So the IDE
and the CLI parse directive content with byte-identical logic.

"It is not valid for a file-based app *entry point* to be a member of an ordinary project." An error is
reported for `#:` / `#!` in ordinary projects, and depending on load order such a file may or may not also
be detected as an entry point.

### N11-IDE-05 — The IDE resolves interceptors itself, and only in generated documents

`CSharpSemanticFacts` (powering Go To Definition and Quick Info on an intercepted call) re-decodes the
attribute rather than asking the compiler, using `document.GetContentHashAsync()` and
`simpleName.FullSpan.Start` (the same value as the compiler's `nameSyntax.Position`). But:

```csharp
// We only look for interceptors in generated source documents. Interceptors cannot reasonably be written by
// hand (as they involve embedded an encoded version of a file's content hash, position, and other debugging
// information). So the only realistic way to create them is by asking the compiler to create the attribute
// using SemanticModel.GetInterceptableLocation as part of a generator.
foreach (var generatedDocument in await document.Project.GetSourceGeneratedDocumentsAsync(...))
```

**So an interceptor written by hand, or contributed by any non-generator mechanism, is invisible to Go To
Definition and Quick Info.** The workspaces-layer decoder also matches the attribute by **simple name
only** (`AttributeClass.Name`), unlike the compiler, which matches namespace plus name plus constructor
signature.

The feature document adds: "Interceptors are treated like a post-compilation step in this design.
Diagnostics are given for misuse of interceptors, but **some diagnostics are only given in the command-line
build and not in the IDE**. There is limited traceability in the editor for which calls in a compilation are
actually being intercepted." `GetInterceptorMethod` exists so that analyzers can determine whether a call is
being intercepted, and by what.

### N11-IDE-06 — VS Code C# extension and Rider

`dotnet/vscode-csharp` `main` `package.json` defaults: **`roslyn: 5.12.0-1.26428.1`**, `omniSharp: 1.39.14`,
`razorOmnisharp: 7.0.0-preview.23363.1`, `xamlTools: 18.10.12014.341`, `testDiscovery: 9.9.434-g84ca4d`,
`engines.vscode: ^1.106.0`. Roslyn bump trail: 2.147.x → 5.10/5.11, 2.148.x → 5.11.0-1.26380.4,
2.149.x → 5.11.0-1.26405.8, **2.150.x → 5.12.0-1.26428.1** (the latest released line). The extension
depends on the `ms-dotnettools` .NET Runtime extension and ships binaries for .NET Framework 4.7.2 / .NET 6+
depending on platform. Historic note: 2.122.x introduced a **"balanced" source-generator execution mode**.
Other file-based-app items: `#:ref` support (roslyn #83985) in 2.149.x; automatic discovery and "preserve
`#:` directives during formatting" in 2.134.x; "force using a single msbuild node for design-time builds"
for file-based apps (roslyn #84183).

**JetBrains Rider**: current line at consolidation time is **2026.2** (2026-07-22; 2026.2.1 on 2026-08-19).
Rider 2026.1 shipped "early support for C# 15 Preview" and already implements `ExtendedLayoutAttribute`.
JetBrains stated early .NET 11 support bits were expected in 2026.2, with collection expression arguments
and dictionary expressions in progress and **C# unions not started** when the roadmap was written.
**The Roslyn version Rider bundles is not published in any primary source**; Rider analyses C# with its own
ReSharper engine and hosts Roslyn only to run third-party analyzers and source generators (OQ-31).

### N11-IDE-07 — Design-time consequences of the new constructs

- **A union renders as `struct` in every display string that includes a type keyword** (N11-ROSLYN-19);
  `closed` never renders. The IDE's QuickInfo formats do set `IncludeTypeKeyword` in general, which is where
  this becomes user-visible.
- **`closed` is in the default `preferred_modifier_order`** for the modifier-ordering code style,
  immediately after `abstract`. `union` is not, being a type-declaration keyword.
- **New style rule IDE0410** ("Use labeled jump statement", default on) rewrites `goto` and Boolean-flag
  patterns into labeled `break`/`continue`.
- **New signature-help and completion providers for `with(...)`**:
  `WithElementSignatureHelpProvider`, plus named-argument completion inside `with(` through
  `NamedParameterCompletionProvider`, and `WithElementSyntaxExtensions` in the workspaces layer.
- **EnC is unresolved for the new constructs**: runtime async is not implemented (roslyn #77954, open, no
  milestone, no tests); extension indexers' "Check that EnC is blocked" is unchecked;
  `CSharpEditAndContinueAnalyzer.cs` contains no union handling.
- **`CSharpEditAndContinueAnalyzer` does handle `SyntaxKind.ExtensionBlockDeclaration`** (declaration span =
  the `extension` keyword span, display name `FeaturesResources.extension_block`).
- **`BreakpointSpans.cs` handles `BreakStatement` and `ContinueStatement` in the fallback group** ("All
  these cases are handled by just putting a breakpoint over the entire statement"), so the breakpoint span
  includes the label; no change was made for labeled break/continue. `DebugInfoInjector.InstrumentLabelStatement`
  produces a sequence point spanning only `outer:` — a construct that labeled jumps make common where it was
  previously rare.
- **`BreakpointSpans.cs` gained**: `using` and `await` keywords are now included in the breakpoint span for
  local declaration statements (2025-11-06), fixing an editor/debugger disagreement on `using var` ranges.

---

## 8. Resolved contradictions

| Id | Contradiction | Resolution |
|---|---|---|
| **RES-01** | One note says a union is `TypeKind.Class` with `IsUnion == true`; two others say `TypeKind.Struct`. | **`TypeKind.Struct`** for a `union` *declaration*: `EnumConversions.ToTypeKind` maps `DeclarationKind.Union` to `TypeKind.Struct`, the emitted IL `extends System.ValueType` and is `sealed`, and `MakeModifiers` handles it under `case TypeKind.Struct`. There is **no `TypeKind.Union`**. Reconciliation: `NamedTypeSymbol.IsUnionType` accepts `TypeKind.Class or TypeKind.Struct`, so a hand-written `[Union] class` *is* a union type with `TypeKind.Class`. |
| **RES-02** | Union pattern matching: Preview 7 release notes describe "Try-Both" (`pet is Pet` is true, plus a `UnionMatchingMode` property); csharplang PR #10302 (2026-08-18) and the learn reference page describe value-only unwrapping (`p is Pet` is an error). | **Value-only unwrapping** — PR #10302 and the learn page (updated 2026-08-19) are the more recent and more authoritative statements. **Treat Try-Both as reverted.** Still flagged as OQ-01 because GA behaviour is not established by the sources consulted. |
| **RES-03** | `RequiresUnsafeAttribute` namespace: csharplang speclet says `System.Runtime.CompilerServices`; Roslyn's well-known-type table and the runtime source say `System.Diagnostics.CodeAnalysis`. | **`System.Diagnostics.CodeAnalysis`.** The runtime source file path, the `System.Runtime` reference assembly and `AttributeDescription.RequiresUnsafeAttribute` all agree; the speclet is stale. |
| **RES-04** | `MemorySafetyRulesAttribute` version argument: speclet prose says `15`; implementation emits `2`. | **`2`** (`MemorySafetyRulesVersion.Version2`). The speclet still lists the value as an open question, so it could change before GA (OQ-08). |
| **RES-05** | The Roslyn breaking-changes document says "C# 16" and `langversion:16` for three entries, but `LanguageVersion.CSharp16` does not exist. | "C# 16" is **prose shorthand for `LanguageVersion.Preview`** (`LanguageVersionFacts.CSharpNext`). Introduced when PR #84799 made `CSharp15` concrete and the unsafe-evolution set stayed preview-only. |
| **RES-06** | Windows Arm64 minimum hardware baseline: the breaking-change page says unchanged (`armv8.0-a`); the what's-new runtime page says raised to `armv8.0-a + LSE`. Identical `ms.date` and `updated_at`. | **Unresolved.** The breaking-change page is the normative compatibility document and gives a reason ("so that .NET continues to support hardware that's supported by Windows 10 IoT"), so it is the better bet, but recency does not settle it. Carried as OQ-18. |
| **RES-07** | `UnionAttribute` shape: speclet sketch versus shipped runtime type. | The **shipped** type is `sealed` and `Inherited = false`; the speclet sketch and the Preview 3 polyfill omit both. |
| **RES-08** | `IsClosedTypeAttribute` shape: speclet shows no members; runtime and Roslyn add `DerivedTypes` (`System.Type[]`). | **`DerivedTypes` is the shipping shape** (dotnet/runtime #129009 `api-approved`, roslyn #84350). The Preview 5 polyfill name `ClosedAttribute` is superseded. |
| **RES-09** | Hypothesis that `IsClosedTypeAttribute.DerivedTypes` omits internal derived types, and that this is what `ClosedDerivedTypeInfo.IsComplete == false` means. | **False on both counts.** `ClosedClassesTests.DerivedTypesMetadata_01` records two implicitly *internal* derived classes; the array is built with **no accessibility filtering**. `IsComplete == false` means at least one candidate subtype unified but is **unspeakable** (it introduces a type parameter the closed type does not have). |
| **RES-10** | `<UseRuntimeAsync>false</UseRuntimeAsync>` documented on Learn as the per-project opt-out. | **Misleading.** `UseRuntimeAsync` is defined only inside the dotnet/runtime repository's own build. The .NET SDK targets contain **zero** occurrences of `runtime-async` or `RuntimeAsync`, so setting it in a user project has no effect; the flag is off for user code by default. |
| **RES-11** | The ECMA runtime-async draft defines an "async-capable assembly" by the presence of `RuntimeFeature.Async`. | **`RuntimeFeature.Async` does not exist in the shipped BCL.** Roslyn probes for the *type* `System.Runtime.CompilerServices.AsyncHelpers` being a static class. The implementation ships and wins; the spec clause is stale. |
| **RES-12** | `AsyncHelpers` marked `[Experimental("SYSLIB5007")]`. | **Removed** from the shipped surface; the reference assembly carries only `[EditorBrowsable(Never)]`. The Roslyn design document is stale on this point. |
| **RES-13** | Default `LangVersion` for `net11.0`: Preview 6/7 release notes still say to set `<LangVersion>preview</LangVersion>` for unions and extension indexers. | **C# 15 is the GA default.** `_MaxAvailableLangVersion` is `15.0` in Roslyn `main`'s `Microsoft.CSharp.Core.targets`, and the learn `language-versioning` table lists ".NET 11.x → C# 15". The preview-notes instruction is consistent with `CSharp15` not yet being the default *during the preview SDKs*. |
| **RES-14** | The `analyzers` asset group in `project.assets.json`, honouring `ExcludeAssets`/`PrivateAssets` for analyzers, reported as a .NET 11 feature. | **Not in .NET 11.** `RestoreEnableAnalyzerAssets` exists only on NuGet.Client `dev` (absent from `release-7.1.x` and earlier), and the SDK-side consumer (`dotnet/sdk` #54646, `ResolvePackageAssets.cs`) is present on `main` but **absent from `release/11.0.1xx`**, milestone 12.0-preview1. At GA, restore does not write the group and asset filtering still does not apply to analyzers. |
| **RES-15** | `SourceText.WithChecksumAlgorithm` versus `WithChecksumAlgorithmIfAny`. | Both names appear in the notes for the same internal helper introduced by PR #81934/#81992. The byte-level branch diff gives **`WithChecksumAlgorithmIfAny`**; either way it is `internal`, so it is not a consumer-visible difference. |
| **RES-16** | Whether `with(...)` under `LangVersion < 15` falls back to binding as a method invocation. | **The documented intent and the implementation disagree.** LDM-2025-03-17 and the published breaking-change note say the pre-C#-15 behaviour is preserved; `LanguageParser.ParseCollectionElement` on `main` parses `with (` as a `WithElementSyntax` **unconditionally** with no version check, and no re-binding path was located. Carried as OQ-02. |
| **RES-17** | One pass reported `ErrorCode.cs` values inconsistent with the Learn compiler-message pages (attributing CS9363, CS9370 and CS9380 to unrelated C# 12 inline-array errors). | The Learn message pages and `CSharpResources.resx` agree with each other and with the breaking-changes document; the anomalous raw-source reading was discarded. The table in N11-LANG-13 follows the former. |

---

## 9. Open questions

| Id | Question | Why it matters / where to look |
|---|---|---|
| **OQ-01** | **What are union pattern-matching semantics at GA?** Preview 7 shipped "Try-Both"; csharplang PR #10302 reverted the spec to value-only unwrapping. Compiler behaviour at GA is not established by the sources consulted. | Re-check `proposals/csharp-15.0/unions.md` and the Preview 8 / RC release notes. Also: does the `UnionMatchingMode` property mentioned in the Preview 7 notes (roslyn #84436, #84499) still exist? It appears in no `WellKnownTypes.cs`, `WellKnownMembers.cs` or `PublicAPI.Unshipped.txt`, so it is presumably compiler-internal, and it may have been removed by the revert. |
| **OQ-02** | **Does `with(...)` really fall back to a method invocation below `LangVersion 15`?** The docs say yes; the parser on `main` has no version check and the binder always treats it as a with-element. | Needs a direct compiler experiment against a .NET 11 preview SDK with `-langversion:14`. |
| **OQ-03** | **Do the unsafe-evolution breaks (N11-BREAK-08/09/10) apply at the GA default language version?** `MessageID.cs` says no (`Preview`). But learn's `unsafe-code-errors` page says CS9360–CS9363 apply "Under C# 15's updated memory safety rules" and "The C# 15 compiler tracks unsafe member usage at the call site", and the C# 15 what's-new page lists "Memory safety" as a C# 15 item. | Not resolvable from published sources; needs a check against a .NET 11 RC SDK. |
| **OQ-04** | **Are N11-BREAK-01 (span collection-expression safe-context), N11-BREAK-04 and N11-BREAK-06 language-version gated?** The breaking-changes document does not say. Roslyn commit `a5196d9` (2026-01-22) is titled "Use specific LangVersion in breaking change (#82099)" but which entry it applies to was not determined. | |
| **OQ-05** | **Which parts of the unions speclet were still unimplemented at GA?** learn.microsoft.com (2026-08-14) says "Some features from the proposal specification aren't yet implemented" but does not enumerate them, and no changelog does. Candidates from the speclet's still-open questions: whether the compiler should error on a `[Union]` type in source lacking a `Value` property or any creation member; whether direct `Value`-property matching (`u is S1 { Value: long }`) should apply union rules; the precise lookup rules for `HasValue` / `TryGetValue` (inheritance? read/write `HasValue`?). | |
| **OQ-06** | **What does "non-virtual static interface members" actually change, and what is its diagnostic set?** No csharplang proposal document exists; the only sources are Roslyn PR #83097 and the `MessageID` mapping. Absent from the what's-new page. Interaction with `netstandard2.0` and .NET Framework targets undocumented. | |
| **OQ-07** | **Do extension indexers ship fully stable at GA?** The gate says `CSharp15`, but the test plan (roslyn #81505) is still open with substantive compiler items unchecked (base-receiver rejection, type-receiver rejection, analyzer actions, `GetSymbolInfo`/`LookupSymbols`/`GetMemberGroup`, VB interop, EnC blocking). Some may be untested rather than unimplemented. | |
| **OQ-08** | **Final `MemorySafetyRulesAttribute` version value.** The speclet's open question "`2`? `15`? `11`?" is unresolved; the implementation emits `2`. | |
| **OQ-09** | **Is `UnionAttribute` filtered out of `ISymbol.GetAttributes()`?** The closed-classes tests assert filtering for `IsClosedTypeAttribute` and `CompilerFeatureRequiredAttribute`; no equivalent assertion exists for `UnionAttribute`, and the union IL dump shows it present in metadata. Unverified either way. | Matters to any code model that reads attributes from symbols. |
| **OQ-10** | **Do union conversions or union pattern matching introduce any new `IOperation` node or `OperationKind`?** `CommonConversion.IsUnion` exists; the only new `OperationKind` is `CollectionExpressionElementsPlaceholder = 129`, which is unrelated. | |
| **OQ-11** | **Will `RSEXPERIMENTAL007` (`RegisterPreCompilationSourceOutput`) be lifted before GA?** It is experimental on 5.10, 5.11 and 5.12; the design document says runtime enforcement "is sufficient for the initial release". Tracking: roslyn #83089. | |
| **OQ-12** | **Does an extension indexer's documentation-comment ID round-trip?** `CreateDeclarationId` goes through `EncodePropertyName` (`"this[]"` → `"Item"`) and `GetMatchingExtensions`; no test exercises `GetFirstSymbolForDeclarationId` on the result. And is the `[IndexerName]` divergence between `GetDocumentationCommentId()` (honours it) and `CreateDeclarationId` (hard-codes `"Item"`) a known issue in scope for GA? | |
| **OQ-13** | **Interceptors versus the new features.** (a) Does replacing a *requires-unsafe* callee with a non-*requires-unsafe* interceptor, or the reverse, change the call site's unsafe requirement? Substitution happens in lowering, after the binder has decided. (b) Can a runtime-async method's call be intercepted, and can the interceptor itself be runtime-async? **No tests, no specification text, and no code path forbidding either.** (c) Will a version-2 encoding land before GA? No branch or issue found. (d) Is emitting an interceptor from `RegisterPreCompilationSourceOutput` supported at all? That phase has no `SemanticModel`, so the supported attribute-creation API is unusable there. | |
| **OQ-14** | **Will Roslyn bump `NetVS` / `NetRoslyn` from `net10.0` to `net11.0` before GA?** As of 2026-09-03 it is `net10.0`, which would mean `ServiceHub.RoslynCodeAnalysisService` and `csc.dll` in the .NET 11 SDK are `net10.0` assemblies running on the .NET 11 runtime by roll-forward. Also unconfirmed: whether `Microsoft.Net.Compilers.Toolset` still ships a `net472` `csc.exe` alongside the `net10.0` one. | |
| **OQ-15** | **Which exact diagnostic does N11-BREAK-05 (`nameof(this.X)` in an attribute) produce?** The document names no identifier; the CS0026 / CS0027 family is expected but unverified against `ErrorCode.cs`. | |
| **OQ-16** | **Which specific diagnostics moved as a result of N11-BREAK-13** (Start-relative offsets)? The PR describes it as a fix for nested parsing scenarios; the `#error`/`#warning` offset arithmetic demonstrably changed, but the compiler test baselines were not diffed to enumerate the affected set. | |
| **OQ-17** | **Is CS9378 serviced back to any 5.x release branch, or is it .NET 11 only?** Only `main` carries `ERR_PPShebangNotOnFirstLine`; `release/dev18.3` does not, and no `release/dev18.4`…`dev18.12` branches exist publicly. | |
| **OQ-18** | **Windows Arm64 minimum baseline: `armv8.0-a` or `armv8.0-a + LSE`?** Two learn pages with identical dates disagree (RES-06). | |
| **OQ-19** | **`ExtendedLayoutAttribute` diagnostics and polyfills.** Which diagnostic identifiers does Roslyn report for `ExtendedLayoutAttribute` combined with `StructLayoutAttribute` or `InlineArrayAttribute`, and is a source-declared polyfill honoured? (A polyfill would emit metadata older runtimes cannot interpret, so this is effectively `net11.0`-only regardless.) | |
| **OQ-20** | **DSA on Windows and Linux.** The removal is documented only for macOS; no source states any restriction elsewhere, but the absence of a statement is not proof. | |
| **OQ-21** | **Will the .NET 11 SDK enable `runtime-async=on` by default for user projects at GA?** As of Preview 7 it does not (zero occurrences in the SDK targets), and the Learn page still calls it a preview feature. The Roslyn test plan carries the unchecked item "replace feature flag with a real switch and make the SDK set it by default", and a second unchecked item "use real `MethodImplOptions.Async` flag (#79792)". | The single most consequential unresolved item for anything that reads IL. |
| **OQ-22** | **`RuntimeFeature.Async`**: the ECMA augment requires it for the async-capable-assembly test but it is absent from the shipped BCL. Either the spec will be amended or the constant added. | |
| **OQ-23** | **Is `[AsyncStateMachine]` definitely absent from runtime-async methods?** Strongly implied (no state machine type exists, `stateMachineTypeOpt` is null, the stack-trace sample shows no builder frames) but the emission site was not quoted. | |
| **OQ-24** | **Where exactly is CS9328 (`ERR_UnsupportedFeatureInRuntimeAsync`) reported, and what is the full list of constructs that trigger it?** The code and message exist, but the reporting site was not located in `MethodCompiler.cs`, `CodeGenerator.cs`, `RuntimeAsyncRewriter.cs`, `SpillSequenceSpiller.cs`, `Binder_Await.cs`, `Binder_Statements.cs`, `Binder_Expressions.cs`, `LocalRewriter.cs` or `IteratorAndAsyncCaptureWalker.cs`. Related: whether async streams land any part in .NET 11 GA. | |
| **OQ-25** | **The minimum Visual Studio / MSBuild version guard the .NET 11 SDK places on `net11.0`** in `Microsoft.NET.SupportedTargetFrameworks.props`, and the documented minimum VS for the .NET 11 SDK. The `11.0.1xx` rows are missing from the versioning-sdk-msbuild-vs page. | |
| **OQ-26** | **Is `NETSDK1235` the only new `NETSDK*` diagnostic in .NET 11?** The negative comes from two documentation pages, not from a diff of the SDK's `Strings.resx`. | |
| **OQ-27** | **NuGet in .NET 11: nothing published on package source mapping, central package management or audit.** The NuGet 7.10 / 7.11 / 7.12 release notes had not been published when checked; the index stopped at 7.9. Which NuGet client version ships in SDK 11.0.100 is inferred, not stated. Also unresolved: whether NuGet's `RestoreEnableAnalyzerAssets` and dotnet/sdk #54646 get backported to `release/11.0.1xx` before GA (as of 2026-09-03, no). | |
| **OQ-28** | **Will `#:ref` be ungated (no `ExperimentalFileBasedProgramEnableRefDirective`) by GA?** It is still described as experimental in `dotnet-run-file.md` on `main`, and the learn what's-new page does not mention it. Will `#:exclude` and `#:ref` be added to `file-based-apps.md` before GA? The docs list five directives; the implementation supports seven. | |
| **OQ-29** | **Is there a .NET 11-specific change in Visual Studio proper (`devenv.exe`, not the LSP) for opening a loose `.cs` file with `#:`/`#!`?** No primary source found. No VS 2027 release notes exist yet. | |
| **OQ-30** | **Do the IDE hosts run pre-compilation output nodes at design time?** The Roslyn Workspaces layer does (`DisabledOutputs = None`); what Rider and the VS Code C# Dev Kit pass was not checked. Related: will the Razor source generator actually adopt `RegisterPreCompilationSourceOutput` in the .NET 11 SDK? The design document reports "roughly 50% performance improvement" from early experiments; whether `Microsoft.NET.Sdk.Razor.SourceGenerators` on `release/11.0.1xx` calls the new API was not verified. **If it does, every project with `.razor` files gets pre-compilation trees in its initial compilation by default.** | |
| **OQ-31** | **Rider's bundled Roslyn version** is not published, and Rider 2026.2's release material states neither a Roslyn version nor an MSBuild version nor its .NET 11 support level in detail. | |
| **OQ-32** | **Whether unions and closed classes appear in `SymbolKey`** (the Workspaces symbol-persistence format). Not examined; adjacent to symbol display and documentation IDs, and relevant to anything that persists symbol identities. | |
| **OQ-33** | **`closed record class` through `SyntaxGenerator`.** `s_recordModifiers` omits `DeclarationModifiers.Closed`, yet `public closed record class GateState` is the speclet's own opening example. Is this a known gap, or was `closed record` disallowed after the speclet was written? | |
| **OQ-34** | **Preview 8 / RC content.** Only Previews 1 through 7 existed at consolidation time. Anything landing between Preview 7 and GA is not covered here, and Microsoft states the breaking-change index "is not a complete list". The newest entry in the compiler breaking-changes document is dated VS 18.10; nothing exists yet for 18.11 or 18.12. | |
| **OQ-35** | **The assembly version of `System.Runtime` in the .NET 11 reference pack** (expected `11.0.0.0` by the pattern of every prior release, not confirmed). Matters to anything that hard-codes the corlib identity or builds a reference set by hand. | |
| **OQ-36** | **The exact diagnostic identifier reported when `UnionAttribute`, `IUnion` or `IsClosedTypeAttribute` is missing** (the pattern suggests `CS0656`, but no source states it), and whether a source-declared `IsClosedTypeAttribute` polyfill is accepted and validated for its `DerivedTypes` property. | |
| **OQ-37** | **`System.Reflection.Metadata` completeness.** No change is documented anywhere; whether the library is genuinely unchanged or merely undocumented could not be established (the public API surface *is* confirmed unchanged by the ref-assembly commit history). | |
| **OQ-38** | **Whether any debugger, `Microsoft.DiaSymReader` or `symreader-converter` release shipping alongside .NET 11 actually consumes the SHA-384 / SHA-512 document GUIDs.** Roslyn PR #82452 says it "references raw GUIDs pending tooling updates" and coordinates with `symreader-converter`, `metadata-tools` and `perfview`; the shipped tooling was not verified. Also: does the .NET 11 SDK ever set `<ChecksumAlgorithm>` to anything other than SHA-256? | |
| **OQ-39** | **Will Roslyn emit any Portable PDB record specific to runtime-async suspension points before GA, and will EnC/Hot Reload block runtime-async methods with an explicit rude edit or simply produce incorrect deltas?** As of 2026-09-03 it emits none, roslyn #79793 is closed as "nothing left to do", the design document still carries the unresolved NOP-placement TODO, roslyn #77954 is open with no content, and there are no runtime-async tests in the EnC test directory. | |
| **OQ-40** | **Is the union `Value` getter's sequence point — anchored to the whole `UnionDeclarationSyntax` — intended?** Derived from code reading only; there is no PDB test for unions, and no PDB test file was added for unions, extension indexers, labeled break/continue, `with(...)` elements or runtime async. | |
