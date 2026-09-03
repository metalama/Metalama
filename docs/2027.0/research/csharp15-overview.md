# C# 15 / .NET 11 — Complete Language Feature Inventory

Research date: **2026-09-03**. .NET 11 and C# 15 GA: **November 2026**. Latest public preview at time of
research: **.NET 11 Preview 7** (release notes dated August 2026). Visual Studio 2026 versions referenced in
Roslyn breaking-change docs go up to **18.10**.

All facts below were verified against primary sources fetched on 2026-09-03. Source URLs are given inline.
Where a source contradicts another, both are reported with a note on which is more recent.

---

## 0. Ground truth: how C# 15 is gated in the compiler

### 0.1 `LanguageVersion` enum

Source: `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/LanguageVersion.cs`

```csharp
CSharp14 = 1400,
CSharp15 = 1500,          // NEW
LatestMajor = int.MaxValue - 2,
Preview     = int.MaxValue - 1,
Latest      = int.MaxValue,
Default     = 0,
```

There is **no `CSharp16`** in the enum. `LanguageVersionFacts.CSharpNext == LanguageVersion.Preview`.
New error code `ERR_FeatureNotAvailableInVersion15`.

The XML doc on `CSharp15` lists exactly these features:

- Collection expression arguments
- Unions
- Non-virtual static members in interfaces
- Closed class hierarchies
- Labeled `break` and `continue`
- Extension indexers

Public API delta (`src/Compilers/CSharp/Portable/PublicAPI.Unshipped.txt`):
`Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp15 = 1500`.

### 0.2 Authoritative feature → language-version map

Source: `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Errors/MessageID.cs`

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

MessageID numeric values (`MessageBase + n`):

| MessageID | value |
|---|---|
| `IDS_FeatureCollectionExpressionArguments` | MessageBase + 12858 |
| `IDS_FeatureUnsafeEvolution` | MessageBase + 12859 |
| `IDS_FeatureUnions` | MessageBase + 12860 |
| `IDS_FeatureStaticMembersInInterfaces` | MessageBase + 12861 |
| `IDS_FeatureClosedClasses` | MessageBase + 12862 |
| `IDS_FeatureExtensionIndexers` | MessageBase + 12863 |
| `IDS_FeatureLabeledBreakContinue` | MessageBase + 12864 |

**Key conclusion.** Six features are `LanguageVersion.CSharp15` and will therefore be on by default for
`net11.0` projects at GA. **Unsafe evolution is `LanguageVersion.Preview` only** and will NOT be on by
default at GA. There is currently **no `IDS_Feature...` for dictionary expressions**, confirming that
dictionary expressions are not merged into `main`.

**Contradiction to note.** The Roslyn breaking-changes document
(`docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`) describes the unsafe-evolution changes as
being "in C# 16" and "under `langversion:16`". Since no `CSharp16` exists in the enum, "C# 16" in that
document is prose shorthand for `LanguageVersionFacts.CSharpNext` (= `Preview`). The `MessageID.cs` mapping
is the authoritative statement and agrees with the learn.microsoft.com page, which says the pointer
relaxations require `<LangVersion>preview</LangVersion>`.

---

## 1. Feature-by-feature inventory (C# 15 proper)

### 1.1 Collection expression arguments — `with(...)`

- Champion issue: <https://github.com/dotnet/csharplang/issues/8887>
- Spec: `https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/collection-expression-arguments.md`
- Roslyn test plan / state issue: <https://github.com/dotnet/roslyn/issues/80613>
- Branch: `features/collection-expression-arguments`. Developer: CyrusNajmabadi.
- Shipped in **.NET 11 Preview 1**. `LanguageVersion.CSharp15`. Stable at GA.

**Grammar (diff against C# 12 collection expressions):**

```diff
collection_element
   : expression_element
   | spread_element
+  | with_element
   ;

+with_element
+  : 'with' argument_list
+  ;
```

**Disambiguation rule (important for a parser/rewriter):** if a collection element *lexically* starts with
the token sequence `with` `(`, it is **always** a `with_element`. To call a method literally named `with`,
write `@with(...)`.

**Placement rule:** if a `with_element` is present and is not the first element of the collection
expression, a compile-time error is reported. `dynamic` arguments in the argument list are a compile-time
error (LDM-2025-01-22).

**Examples (verbatim from learn.microsoft.com / csharplang):**

```csharp
string[] values = ["one", "two", "three"];

// Pass capacity argument to List<T> constructor
List<string> names = [with(capacity: values.Length * 2), .. values];

// Pass comparer argument to HashSet<T> constructor
HashSet<string> set = [with(StringComparer.OrdinalIgnoreCase), "Hello", "HELLO", "hello"];
```

```csharp
// List<T> candidates: List<T>(), List<T>(IEnumerable<T>), List<T>(int capacity)
List<int> l;
l = [with(capacity: 3), 1, 2]; // new List<int>(capacity: 3)
l = [with([1, 2]), 3];         // new List<int>(IEnumerable<int> collection)
l = [with(default)];           // error: ambiguous constructor
```

**Conversion rule change.** The "struct or class implementing `IEnumerable`" conversion clause becomes:

```diff
-  * The type has an applicable constructor that can be invoked with no arguments ...
+  a. the collection expression has no `with_element` and the type has an applicable constructor
+     that can be invoked with no arguments, accessible at the location of the collection expression. or
+  b. the collection expression has a `with_element` and the type has at least one constructor
+     accessible at the location of the collection expression.
```

Only the *presence* of the `with_element` affects whether the conversion exists, not the arguments in it.

**`CollectionBuilderAttribute` changes.** Create methods may now have additional parameters *before* the
`ReadOnlySpan<E>` parameter, and **multiple create methods** are supported. Overload resolution runs over
*projection methods* (the create method minus its last parameter).

```csharp
[CollectionBuilder(typeof(MyBuilder), "Create")]
class MyCollection<T> { ... }

class MyBuilder
{
    public static MyCollection<T> Create<T>(ReadOnlySpan<T> elements);
    public static MyCollection<T> Create<T>(IEqualityComparer<T> comparer, ReadOnlySpan<T> elements);
}
```

**Interface target types.** Candidate signatures:

| Interfaces | Candidate signatures |
|---|---|
| `IEnumerable<E>`, `IReadOnlyCollection<E>`, `IReadOnlyList<E>` | `()` (no parameters) |
| `ICollection<E>`, `IList<E>` | `List<E>()`, `List<E>(int)` |
| `IReadOnlyDictionary<K,V>` (dictionary-expressions feature) | `()`, `(IEqualityComparer<K>? comparer)` |
| `IDictionary<K,V>` (dictionary-expressions feature) | `Dictionary<K,V>()`, `(int)`, `(IEqualityComparer<K>)`, `(int, IEqualityComparer<K>)` |

**Roslyn syntax model:**

```xml
<Node Name="WithElementSyntax" Base="CollectionElementSyntax">
  <Kind Name="WithElement"/>
  <Field Name="WithKeyword" Type="SyntaxToken"><Kind Name="WithKeyword"/></Field>
  <Field Name="ArgumentList" Type="ArgumentListSyntax" />
</Node>
```

- `SyntaxKind.WithElement = 9081`
- `Microsoft.CodeAnalysis.CSharp.Syntax.WithElementSyntax` (sibling of `ExpressionElementSyntax` and
  `SpreadElementSyntax` under abstract `CollectionElementSyntax`)
- `SyntaxFactory.WithElement(ArgumentListSyntax? argumentList = null)` and the 2-arg overload
- `CSharpSyntaxVisitor.VisitWithElement`, `CSharpSyntaxVisitor<TResult>.VisitWithElement`,
  `CSharpSyntaxRewriter.VisitWithElement`
- No new token kind: the keyword is the pre-existing `SyntaxKind.WithKeyword`.

**IOperation model (new public API in `Microsoft.CodeAnalysis`):**

- `Microsoft.CodeAnalysis.Operations.ICollectionExpressionOperation.ConstructArguments` →
  `ImmutableArray<IOperation>`
- `Microsoft.CodeAnalysis.OperationKind.CollectionExpressionElementsPlaceholder = 129`
- `Microsoft.CodeAnalysis.Operations.ICollectionExpressionElementsPlaceholderOperation`
- `OperationVisitor.VisitCollectionExpressionElementsPlaceholder(...)` (and the generic overload)

**Breaking change** (VS 2026 18.4): `with(...)` as a collection element under `LangVersion >= 15` binds as
collection-construction arguments instead of an invocation of a method named `with`.

```csharp
items = [with(x, y), z];  // C# 14: call to with() method; C# 15: error args not supported for object[]
items = [@with(x, y), z]; // call to with() method
```

**Adjacent breaking change** (VS 2026 18.4, roslyn #81837 / #81863): parsing of `with` inside a
switch-expression arm. `(X.Y) when` was previously parsed as a cast of the identifier `when`; it is now
parsed as a constant pattern followed by a `when` clause.

---

### 1.2 Union types — `union` keyword

- Champion issue: <https://github.com/dotnet/csharplang/issues/9662>
- Spec: `https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md` (1524 lines;
  last substantive commit 2026-08-18, "Update unions.md based on recent LDM decision (#10302)")
- Roslyn test plan: <https://github.com/dotnet/roslyn/issues/81074>. Branch `features/Unions`.
  Developer: AlekseyTs. LDM champ: MadsTorgersen.
- Compiler support first in **.NET 11 Preview 2**; IDE support Preview 3; declarations + patterns
  Preview 5; support types shipped in-box in **Preview 6**; matching semantics revised in Preview 7 then
  revised again by LDM (see §1.2.6).
- `LanguageVersion.CSharp15`. Release notes for Previews 6 and 7 still say "Unions remain a preview
  feature; enable `<LangVersion>preview</LangVersion>`", which is consistent with `CSharp15` not yet being
  the default `LangVersion` during the preview SDKs.

#### 1.2.1 Union declaration syntax

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

```csharp
public record class Cat(string Name);
public record class Dog(string Name);
public record class Bird(string Name);

public union Pet(Cat, Dog, Bird);

// Union with a body
public union OneOrMore<T>(T, IEnumerable<T>)
{
    public IEnumerable<T> AsEnumerable() => Value switch
    {
        IEnumerable<T> list => list,
        T value => [value],
    };
}

// "Discriminated" union with freshly declared case types
public record class None();
public record class Some<T>(T value);
public union Option<T>(None, Some<T>);
```

Restrictions on a `union` declaration body:
- No instance fields, auto-properties, or field-like events.
- No explicitly declared **public** constructors with a single parameter (the compiler generates those).
  Preview 6 relaxed this: a **non-public** single-parameter constructor is permitted (roslyn #83788).
- Explicitly declared constructors must chain through `this(...)` to a generated constructor.

Case types can be any type convertible to `object`: classes, structs, interfaces, type parameters,
nullable types, and other unions. Overlapping and nested unions are allowed.

#### 1.2.2 Lowering

```csharp
public union Pet(Cat, Dog) { ... }
```

lowers to

```csharp
[Union] public struct Pet : IUnion
{
    public Pet(Cat value) => Value = value;
    public Pet(Dog value) => Value = value;

    public object? Value { get; }

    ... // original body
}
```

The generated struct: same attributes, modifiers, name, type parameters, constraints; implements `IUnion`;
`public object? Value { get; }` auto-property; one public constructor per case type. It is an error for
user-declared members to conflict with generated members.

#### 1.2.3 Runtime support types (shipping in .NET 11 Preview 5 / Preview 6)

```csharp
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class UnionAttribute : Attribute;

public interface IUnion
{
    object? Value { get; }
}
```

Roslyn well-known types (`src/Compilers/Core/Portable/WellKnownTypes.cs`):
- `System.Runtime.CompilerServices.UnionAttribute` (parameterless `.ctor` is a well-known member)
- `System.Runtime.CompilerServices.IUnion`

Preview 3 required hand-authored polyfills for both; Preview 6 ships them in the framework.
`System.Text.Json` serializes union values by writing the active case directly (dotnet/runtime #128162);
ASP.NET Core surfaces unions in OpenAPI documents.

#### 1.2.4 The union pattern (adopting an existing type)

Any class or struct with `[Union]` that follows the *basic union pattern* is a union type. Required
members, all public:

- **Union creation members.** If the union-defining type is the union type itself: every constructor with a
  single parameter (by-value or `in`) is a *union constructor*, and its parameter type is a case type.
- **`Value` property.** `public object? Value { get; }` (or `object`). May optionally have `init`/`set` of
  any accessibility, unused by the compiler.

Optional *non-boxing union access pattern*:
- `public bool HasValue { get; }` — true iff `Value` is not null.
- `public bool TryGetValue(out TCase value)` — one per case type; returns `bool`; the `out` parameter type
  is identity-convertible to the case type, or to the underlying value type if the case type is a nullable
  value type.

New public constants in `Microsoft.CodeAnalysis.WellKnownMemberNames`:
- `HasValuePropertyName = "HasValue"`
- `TryGetValueMethodName = "TryGetValue"`

**Union member provider.** If the union type *directly contains* a public nested interface declaration
named `IUnionMembers`, that interface becomes the union-defining type, and union members are found *only*
there. The union type must implement it. Creation members then take the form of static `Create` methods
with a single parameter and a return type identity-convertible to the union type.

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

Well-formedness assumptions the compiler makes but does not verify: *soundness*, *stability*,
*creation equivalence*, *access pattern consistency*. Preview 6 added a clear error when a custom union
declaration is missing the minimal set of required APIs (roslyn #83813) and added inheritance of
generated `Create` methods (roslyn #83991).

#### 1.2.5 Union conversions

There is an implicit *union conversion* to union type `U` from expression/type `E` if a standard implicit
conversion exists from `E` to some `C` where `C` is the parameter type of a union creation member of `U`.
If `U` is a struct, there is also a union conversion to `U?`. A user-defined implicit conversion operator
takes priority over the union conversion. If two case types are equally applicable, the conversion is
ambiguous and an error is reported (roslyn #83625).

New public API:
- `Microsoft.CodeAnalysis.Operations.CommonConversion.IsUnion` → `bool`
- `Microsoft.CodeAnalysis.CSharp.Conversion.IsUnion` → `bool`

#### 1.2.6 Union pattern matching — MOVING TARGET

Two conflicting descriptions exist:

**(A) .NET 11 Preview 7 release notes** (`dotnet/core release-notes/11.0/preview/preview7/csharp.md`) say
unions adopt the **"Try-Both"** approach: the pattern is first tested against the union instance itself and,
if that fails, against the union's `Value`. It states `pet is Pet` is **true**. It also mentions a new
`UnionMatchingMode` property used to control lowering for custom union declarations (roslyn #84436,
#84499).

**(B) csharplang `proposals/csharp-15.0/unions.md`, updated 2026-08-18 via PR #10302
("Update unions.md based on recent LDM decision")** reverses this: "test is done against the union's
value" only. Under this rule `p is Pet` is an **error** ("`p.Value` is not pattern compatible with
`Pet`"). The learn.microsoft.com reference page (ms.date 2026-08-14, updated 2026-08-19) agrees with (B).

**(B) is the more recent and more authoritative statement.** Treat Try-Both as reverted. Flagged as an
open question below.

Rules under (B), the current spec:

- Most patterns "unwrap": the pattern applies to `Value`, not to the union value.
- Exceptions that apply to the **union value itself**: the discard pattern `_`, the `var` pattern, and the
  `not` pattern (roslyn #83904 made `not` apply to the incoming union value).
- Lowering preference: for a pattern implying a check for type `T`, if `TryGetValue(out S)` exists and there
  is an identity or implicit reference/boxing conversion from `T` to `S`, that method is used. Otherwise for
  a null check, `HasValue` is used if present. Otherwise, the pattern applies to `Value`.
- The `is`-type operator applied to a union type has the same meaning as a type pattern.
- Declaration pattern `type simple_designation` is equivalent to `type and var simple_designation`.
- `null` pattern: for struct unions checks `Value is null`; for class unions succeeds when either the
  reference is null or `Value` is null; for `U?` succeeds when the wrapper has no value or `Value` is null.

```csharp
GetPet() switch
{
    var pet and not null => ...,   // 'var pet' captures the Pet?; 'not null' applies to Pet?
    not null and var value => ..., // 'not null' does not unwrap; 'var value' still captures Pet?
}
```

#### 1.2.7 Exhaustiveness and nullability

A `switch` expression over a union is exhaustive when it handles all case types; no discard needed.

```csharp
var name = pet switch
{
    Dog dog => ...,
    Cat cat => ...,
    // No warning about non-exhaustive switch
};
```

If the null state of `Value` is "maybe null", an unhandled `null` produces a warning even when otherwise
exhaustive. Null-state rules:
- Default null state of `Value` is "maybe null" if any case type's default null state is "maybe null";
  otherwise "not null".
- Creating a union from a case type gives `Value` the incoming value's null state.
- `HasValue` / `TryGetValue` on the `true` branch make `Value` "not null".

#### 1.2.8 Roslyn syntax and symbol model

```xml
<Node Name="UnionDeclarationSyntax" Base="TypeDeclarationSyntax" SkipConvenienceFactories="true">
  <Kind Name="UnionDeclaration"/>
  <Field Name="AttributeLists"    Type="SyntaxList<AttributeListSyntax>" Override="true"/>
  <Field Name="Modifiers"         Type="SyntaxList<SyntaxToken>"         Override="true"/>
  <Field Name="Keyword"           Type="SyntaxToken" Override="true"><Kind Name="UnionKeyword"/></Field>
  <Field Name="Identifier"        Type="SyntaxToken" Override="true"><Kind Name="IdentifierToken"/></Field>
  <Field Name="TypeParameterList" Type="TypeParameterListSyntax" Optional="true" Override="true"/>
  <Field Name="ParameterList"     Type="ParameterListSyntax"     Optional="true" Override="true"/>
  <Field Name="BaseList"          Type="BaseListSyntax"          Optional="true" Override="true"/>
  <Field Name="ConstraintClauses" Type="SyntaxList<TypeParameterConstraintClauseSyntax>" Override="true"/>
  <Field Name="OpenBraceToken"    Type="SyntaxToken" Override="true" Optional="true"/>
  <Field Name="Members"           Type="SyntaxList<MemberDeclarationSyntax>" Override="true"/>
  <Field Name="CloseBraceToken"   Type="SyntaxToken" Override="true" Optional="true"/>
  <Field Name="SemicolonToken"    Type="SyntaxToken" Optional="true" Override="true"/>
</Node>
```

- `SyntaxKind.UnionDeclaration = 9082`
- `SyntaxKind.UnionKeyword = 8452` (contextual keyword)
- The case-types list is modelled as a **`ParameterListSyntax`**, not a dedicated node. The parser calls
  `ParseParenthesizedParameterList(forExtensionOrUnion: isExtension || isUnion)`, so each case type is a
  `ParameterSyntax` with a `Type` and no identifier — the same shape as an unnamed extension receiver
  parameter. `UnionDeclarationSyntax.AddParameterListParameters(params ParameterSyntax[])` is the public
  add-member API.
- `TypeDeclarationSyntax.Keyword` doc comment now reads: *"Gets the type keyword token ("class", "struct",
  "interface", "record", "extension", "union")"*.
- `SkipConvenienceFactories="true"`: the only `SyntaxFactory.UnionDeclaration` overload is the full
  12-parameter one. There is **no** short convenience factory.
- Visitors: `CSharpSyntaxVisitor.VisitUnionDeclaration`, `CSharpSyntaxVisitor<TResult>.VisitUnionDeclaration`,
  `CSharpSyntaxRewriter.VisitUnionDeclaration`.

New public symbol API in `Microsoft.CodeAnalysis`:
- `ITypeSymbol.IsUnion` → `bool`
- `ITypeSymbol.UnionCaseTypes` → `ImmutableArray<ITypeSymbol>`

---

### 1.3 Closed hierarchies — `closed` modifier

- Champion issue: <https://github.com/dotnet/csharplang/issues/9499>
- Spec: `https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/closed-hierarchies.md`
- Roslyn test plan: <https://github.com/dotnet/roslyn/issues/81039>. Branch `features/closed-class`.
  Developer: RikkiGibson. LDM champ: mattwar.
- Shipped in **.NET 11 Preview 5** (roslyn #83120, #83736); type-parameter exhaustiveness and metadata
  format stabilized in **Preview 7** (roslyn #83979, #84350). `LanguageVersion.CSharp15`.

```csharp
// Assembly 1
public closed record class GateState;
public record class Closed : GateState;
public record class Open(float Percent) : GateState;

// Assembly 2
public record class Locked : GateState; // ERROR - 'GateState' is a closed class
```

```csharp
string Describe(GateState state) => state switch
{
    Closed => "closed",
    Open(var percent) => $"{percent}% open",
    // No warning: every direct descendant of 'GateState' is handled.
};
```

**Rules:**

- `closed` is a **contextual keyword** modifier on **classes** only (interfaces are listed as a possible
  future extension). `SyntaxKind.ClosedKeyword = 8453`. There is **no new syntax node**; `closed` appears in
  `TypeDeclarationSyntax.Modifiers`.
- A `closed` class is implicitly `abstract`. It cannot combine with `sealed`, `static`, or an explicit
  `abstract` modifier.
- Direct derivation from a `closed` class is an error outside the declaring **assembly** (and outside the
  declaring **module**).
- Derivation is **not transitive**: a non-closed descendant of a closed class can still be derived from in
  other assemblies. Mark intermediate descendants `closed` to extend exhaustiveness downward.
- **Type-parameter restriction:** if a generic class directly derives from a closed class, all of its type
  parameters must be used in the base class specification.
  ```csharp
  closed class C<T> { ... }
  class D1<U> : C<U> { ... }   // Ok
  class D2<V> : C<V[]> { ... } // Ok
  class D3<W> : C<int> { ... } // Error, 'W' is not used in base class
  ```
- **Exhaustiveness:** a `switch` handling all direct descendants exhausts the closed class. Consequently
  listing the closed base after all descendants is an *error* (unreachable case).
- Exhaustiveness is defeated when a subtype is not usable at the use site (accessibility, constraint
  violation, unspeakable generic construction). Subtype constraints are **not** analyzed precisely.
- An empty switch over a closed class with no subtypes is **not** exhaustive (a deliberate quirk).
- A type parameter constrained to a closed class is treated like the closed class for exhaustiveness
  (added in Preview 7):
  ```csharp
  public closed record class Shape;
  public record class Circle(double Radius) : Shape;
  public record class Square(double Side) : Shape;

  static double Area<T>(T shape) where T : Shape => shape switch
  {
      Circle(var r) => Math.PI * r * r,
      Square(var s) => s * s
  };
  ```
- **Interface convertibility restriction:** if a closed class has a *sealed hierarchy* (every class in the
  expanded hierarchy is sealed or closed), an explicit reference conversion to an interface that no member
  of the hierarchy implements is an error, mirroring the existing rule for sealed classes.

**Metadata / lowering:**

The spec text says the attribute is emitted as:

```csharp
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class IsClosedTypeAttribute : Attribute { }
}
```

The **current Roslyn well-known member table** is more recent and shows the shape decided in
dotnet/runtime #129009 and implemented by roslyn #84350:

- Well-known type `System.Runtime.CompilerServices.IsClosedTypeAttribute`
- Well-known members: `IsClosedTypeAttribute..ctor` (parameterless) and
  `IsClosedTypeAttribute.DerivedTypes` — a **property of type `System.Type[]`**.

(.NET 11 Preview 5 release notes named the polyfill attribute `ClosedAttribute`; that name is superseded by
`IsClosedTypeAttribute`.)

Constructors of closed classes are additionally decorated with
`[CompilerFeatureRequired("ClosedClasses")]` so that older compilers cannot derive from them. Unlike
required members, no `ObsoleteAttribute` is emitted alongside. Multiple `CompilerFeatureRequired`
attributes stack, for example with `RequiredMembers`.

**New public Roslyn API (`Microsoft.CodeAnalysis`):**

```
Microsoft.CodeAnalysis.ClosedDerivedTypeInfo                    // struct
Microsoft.CodeAnalysis.ClosedDerivedTypeInfo.ClosedDerivedTypes.get -> ImmutableArray<INamedTypeSymbol!>
Microsoft.CodeAnalysis.ClosedDerivedTypeInfo.IsComplete.get -> bool
Microsoft.CodeAnalysis.ITypeSymbol.IsClosed.get -> bool
Microsoft.CodeAnalysis.ITypeSymbol.GetClosedDerivedTypeInfo(CancellationToken) -> ClosedDerivedTypeInfo
```

Workspaces layer:

```
Microsoft.CodeAnalysis.Editing.DeclarationModifiers.IsClosed.get -> bool
Microsoft.CodeAnalysis.Editing.DeclarationModifiers.WithIsClosed(bool isClosed) -> DeclarationModifiers
static Microsoft.CodeAnalysis.Editing.DeclarationModifiers.Closed.get -> DeclarationModifiers
```

**Breaking change** (VS 2026 18.10): `closed` is a contextual keyword in type-declaration contexts. A type
or alias declaration named `closed` without `@` produces **CS9380**; in member-declaration contexts `closed`
is treated as a modifier and previously valid field declarations of type `closed` now produce **CS1519**.

```csharp
class @closed { }

class C
{
    closed oldField;      // C# 14: field of type 'closed'; C# 15: parsed as an incomplete declaration
    @closed currentField; // field of type 'closed'
}
```

---

### 1.4 Extension indexers

- Champion issue: <https://github.com/dotnet/csharplang/issues/9856>
- Spec: `https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/extension-indexers.md`
- Roslyn test plan: <https://github.com/dotnet/roslyn/issues/81505>. Branch `features/extensions`.
  Developer: jcouv.
- Shipped in **.NET 11 Preview 6** (roslyn #81607). `LanguageVersion.CSharp15`.

**Grammar (diff against the C# 14 extensions proposal):**

```antlr
extension_member_declaration
        : method_declaration
        | property_declaration
        | indexer_declaration // new
        | operator_declaration
        ;
```

**No new syntax node.** `IndexerDeclarationSyntax` is simply now permitted inside an
`ExtensionBlockDeclarationSyntax` (`SyntaxKind.ExtensionBlockDeclaration = 9079`, from C# 14).

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
```

```csharp
public static class ReadOnlyListExtensions
{
    extension<T>(IReadOnlyList<T> list)
    {
        public T this[Index index] => list[index.GetOffset(list.Count)];
    }
}
```

**Rules:**

- Indexers are always instance members, so an extension block declaring an indexer **must** name its
  receiver parameter.
- All ordinary indexer features are supported: accessor bodies, expression bodies, ref-returning accessors,
  `scoped` parameters, attributes.
- Prohibited modifiers (unchanged from other extension members): `abstract`, `virtual`, `override`, `new`,
  `sealed`, `partial`, `protected` and related accessibility modifiers, and `init` accessors.
- The extension inferrability rule holds: all type parameters of the extension block must be used in the
  combined set of the extension and member parameters.
- `IndexerNameAttribute` may be applied. It is not emitted, but it affects member conflicts, the metadata
  names of the property and accessors, and the emitted `DefaultMemberAttribute`.
- **Binding order** for `E[A]`: (1) instance indexers on the receiver type; (2) implicit instance indexers;
  (3) extension indexer access; (4) extension implicit indexer access. Extension members are never
  considered when the receiver is a `base_access` or a type.
- The extension-method scope walk is reused (lexical scopes, `using` namespaces, `using static`).
- Extension implicit `System.Index`/`System.Range` indexer access works when a `Length`/`Count` property and
  a `this[int]`/`Slice(int,int)` member (instance **or extension**) is found on the receiver type.
- Null-conditional element access, index assignment in object initializers, list patterns and spread
  patterns all participate.
- **Extension indexers cannot be captured in expression trees.**
- CREF syntax: `<see cref="E.extension(int).this[string]"/>`, `<see cref="E.extension(int).get_Item(string)"/>`,
  `<see cref="E.get_Item(int, string)"/>`, etc.

**Metadata (Metalama-relevant):**

For each CLR-level extension grouping type containing at least one indexer, the compiler emits:
- an extension property named `Item` (or the `IndexerNameAttribute` value) whose accessors
  `throw new NotImplementedException()`, carrying `[ExtensionMarkerName("<M>$...")]`;
- implementation methods `get_Item`/`set_Item` in the enclosing static class, `static`, with the receiver
  parameter prepended;
- `[DefaultMemberAttribute]` on the grouping type, with `MemberName` equal to the indexer's metadata name.

```csharp
[Extension]
static class BitExtensions
{
    [Extension, SpecialName, DefaultMember("Item")]
    public sealed class <G>$T0 // grouping type
    {
        [SpecialName]
        public static class <M>$T_t // marker type
        {
            [SpecialName]
            public static void <Extension>$(T t) { } // marker method
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

---

### 1.5 Labeled `break` and `continue`

- Champion issue: <https://github.com/dotnet/csharplang/issues/9875>
- Spec: `https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/labeled-break-continue.md`
- Roslyn PR: <https://github.com/dotnet/roslyn/pull/84271> (and #84281). Branch
  `features/labeled-break-and-continue`. Developer: CyrusNajmabadi.
- Shipped in **.NET 11 Preview 7**. `LanguageVersion.CSharp15`.

**Grammar:**

```antlr
break_statement
    : 'break' identifier? ';'
    ;

continue_statement
    : 'continue' identifier? ';'
    ;
```

```csharp
outer: for (int row = 0; row < grid.Height; row++)
{
    for (int column = 0; column < grid.Width; column++)
    {
        if (grid[row, column].IsBlocked)
        {
            continue outer;
        }

        if (grid[row, column].IsGoal)
        {
            break outer;
        }
    }
}
```

**Rules:**

- The label is an ordinary `labeled_statement` placed directly on the target statement. Only the statement
  **immediately** nested within the `labeled_statement` is "labeled with" that identifier. In
  `a: b: while (…) …`, only `b` labels the loop; `break a;`/`continue a;` inside the loop body does not
  target the `while`.
- `break identifier;` exits the nearest enclosing *switch_statement* or *iteration_statement* labeled with
  that identifier. `continue identifier;` starts the next iteration of the nearest enclosing
  *iteration_statement* labeled with that identifier. `continue` cannot target a `switch`.
- The label must be attached directly to `for`, `foreach`, `while`, `do`, or `switch`, and the
  `break`/`continue` must be inside that statement. Otherwise a compile-time error.
- Unlabeled `break`/`continue` keep their existing meaning (innermost applicable statement).
- The existing `finally`-block restriction is unchanged: the target must be within the same `finally` block.
- `try`/`finally` unwinding semantics are unchanged.

**Roslyn syntax model.** No new node kinds. `BreakStatementSyntax` and `ContinueStatementSyntax` gain an
optional `Name` field of type `IdentifierNameSyntax`:

```xml
<Node Name="BreakStatementSyntax" Base="StatementSyntax">
  <Kind Name="BreakStatement"/>
  <Field Name="AttributeLists" Type="SyntaxList<AttributeListSyntax>" Override="true"/>
  <Field Name="BreakKeyword" Type="SyntaxToken"><Kind Name="BreakKeyword"/></Field>
  <Field Name="Name" Type="IdentifierNameSyntax" Optional="true"/>   <!-- NEW -->
  <Field Name="SemicolonToken" Type="SyntaxToken"><Kind Name="SemicolonToken"/></Field>
</Node>
```

New public API:

```
BreakStatementSyntax.Name.get -> IdentifierNameSyntax?
BreakStatementSyntax.WithName(IdentifierNameSyntax? name) -> BreakStatementSyntax!
BreakStatementSyntax.Update(SyntaxList<AttributeListSyntax!>, SyntaxToken breakKeyword,
                            IdentifierNameSyntax? name, SyntaxToken semicolonToken) -> BreakStatementSyntax!
SyntaxFactory.BreakStatement(IdentifierNameSyntax? name = null)
SyntaxFactory.BreakStatement(SyntaxList<AttributeListSyntax!>, IdentifierNameSyntax? name)
SyntaxFactory.BreakStatement(SyntaxList<AttributeListSyntax!>, SyntaxToken, IdentifierNameSyntax?, SyntaxToken)
// identical set for ContinueStatementSyntax / SyntaxFactory.ContinueStatement
```

**Note on source compatibility for rewriters.** The `Update` arity of `BreakStatementSyntax` and
`ContinueStatementSyntax` **changed**, and `SyntaxFactory.BreakStatement()` / `ContinueStatement()` gained a
new optional-parameter overload. Any code calling the old `Update(attributeLists, keyword, semicolonToken)`
must be recompiled, and a generated visitor that reconstructs these nodes must be regenerated to carry the
new field.

**IDE:** style rule **IDE0410** flags the Boolean-flag and `goto` patterns that a labeled jump can replace.

**Open question in the spec:** whether `break label;` where `label` names a non-loop statement should fail at
identifier lookup or at label validation; and whether nested labels (`a: b: while(true) continue a;`) should
be supported (recommendation: no).

---

### 1.6 Non-virtual static interface members without DIM runtime support

- No csharplang proposal link in the status table; tracked by Roslyn PR
  <https://github.com/dotnet/roslyn/pull/83097>, merged 2026-04-10 into `features/Unions`.
  Developer: AlekseyTs. LDM champ: N/A.
- `MessageID.IDS_FeatureStaticMembersInInterfaces` → `LanguageVersion.CSharp15`.
- **This feature is in the `CSharp15` XML documentation list but is absent from the
  "What's new in C# 15" learn.microsoft.com page.**

The compiler now allows declaring and consuming **static, non-virtual** members in interfaces even when the
target runtime does not support Default Interface Implementations. Previously such declarations were gated
on runtime DIM capability, which blocked them on the .NET Framework / netstandard2.0 targets.

A new runtime-capability check remains for accessibility: the desktop runtime does not support `protected`
access even for static interface members (it throws at runtime), so `protected` or `protected internal`
accessibility on an interface member reports
`ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember`.

---

## 2. Preview-only in .NET 11: the updated memory-safety model ("unsafe evolution")

- Champion issue: <https://github.com/dotnet/csharplang/issues/9704>
- Spec: `https://github.com/dotnet/csharplang/blob/main/proposals/unsafe-evolution.md` (1138 lines).
  Broader ecosystem design: `https://github.com/dotnet/designs/blob/main/accepted/2025/memory-safety/caller-unsafe.md`
- Roslyn test plan: <https://github.com/dotnet/roslyn/issues/81207>. Branch `features/UnsafeEvolution`.
  Developer: jjonescz. LDM champ: agocke.
- Status: **"Merged as preview feature into .NET 11p2 and VS 18.6."** Gated on
  `LanguageVersion.Preview`, i.e. **NOT stable at .NET 11 GA**. Roslyn public API entries carry the
  experimental diagnostic id **`RSEXPERIMENTAL006`**
  (`UrlFormat = "https://github.com/dotnet/roslyn/issues/82789"`).

### 2.1 Concept

`unsafe` is redefined from "locations where pointer types are used" to "locations where memory unmanaged by
the runtime is dereferenced". A member marked `unsafe` becomes *requires-unsafe*: the audit obligation flows
to the caller.

```csharp
void M()
{
    int i = 1;
    int* ptr = &i; // Allowed: creating a pointer is not itself unsafe
    unsafe
    {
        Console.WriteLine(*ptr);
        ref int intRef = Unsafe.AsRef(ptr);
    }
}
```

### 2.2 Pointer relaxations (Preview 5, roslyn #83133, #83295, #83452)

These apply **regardless of whether the assembly opts in** to the updated memory-safety rules, but require
`<LangVersion>preview</LangVersion>` and `AllowUnsafeBlocks`. Outside an `unsafe` context you may now:

- Declare a pointer type and take an address with `&`.
- Use the `fixed` statement.
- Convert a `stackalloc` expression to a pointer.
- Apply `sizeof` to any unmanaged type.
- Declare a fixed-size buffer.

Still requiring an `unsafe` context:

- Pointer indirection `*p`
- Pointer member access `p->member`
- Pointer element access `p[i]`
- Function pointer invocation
- Element access on a fixed-size buffer
- `stackalloc` converted to `Span<T>`/`ReadOnlySpan<T>` with no initializer inside a `[SkipLocalsInit]`
  member — but **only when the assembly opts in** to the updated rules (a tightening, so it is gated;
  roslyn #83639).

`await` is now allowed inside an `unsafe` context (previously an error). `await` remains disallowed inside a
`fixed` statement.

```csharp
int number = 42;
int* pointer = &number;

int[] numbers = [10, 20, 30];
fixed (int* first = numbers)
{
    // Dereferencing the pointer still requires an unsafe context.
}
```

### 2.3 `unsafe` expressions

```antlr
unsafe_expression
    : 'unsafe' '(' expression ')'
    ;
```

An `unsafe_expression` is a *primary_no_array_creation_expression* that establishes an `unsafe` context for
a single expression. Useful where an `unsafe` block cannot appear: field initializers, constructor
initializers, `catch` filters, and around a single `await`.

```csharp
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

```csharp
await unsafe(DoWork());
int b = unsafe(c[null]);
```

**Roslyn syntax model:**

```xml
<Node Name="UnsafeExpressionSyntax" Base="ExpressionSyntax"
      ExperimentalUrl="https://github.com/dotnet/roslyn/issues/82789">
  <Kind Name="UnsafeExpression"/>
  <Field Name="Keyword"        Type="SyntaxToken"><Kind Name="UnsafeKeyword"/></Field>
  <Field Name="OpenParenToken" Type="SyntaxToken"><Kind Name="OpenParenToken"/></Field>
  <Field Name="Expression"     Type="ExpressionSyntax"/>
  <Field Name="CloseParenToken" Type="SyntaxToken"><Kind Name="CloseParenToken"/></Field>
</Node>
```

- `SyntaxKind.UnsafeExpression = 8769` (marked `[Experimental("RSEXPERIMENTAL006")]` in the public API file)
- `SyntaxFactory.UnsafeExpression(ExpressionSyntax)` and the 4-token overload
- `CSharpSyntaxVisitor.VisitUnsafeExpression`, `CSharpSyntaxVisitor<TResult>.VisitUnsafeExpression`,
  `CSharpSyntaxRewriter.VisitUnsafeExpression`

### 2.4 The `safe` contextual keyword

- `SyntaxKind.SafeKeyword = 8454`, annotated
  `[Experimental("RSEXPERIMENTAL006", UrlFormat = "https://github.com/dotnet/roslyn/issues/82789")]`.
- `safe` may be applied wherever `unsafe` may. It marks the declaration as *not* requires-unsafe. It does
  **not** introduce a safe context, and there is no `safe` block or expression form.
- `safe` and `unsafe` on the same declaration is an error.
- Under the updated rules, an explicit `safe` or `unsafe` modifier is **required** on `extern` members and on
  instance fields of `[StructLayout(LayoutKind.Explicit)]` / `[ExtendedLayout]` types.

**Breaking change** (VS 2026 18.9): `safe` as a modifier on member declarations is now a keyword, breaking
code where `safe` named a type. Workaround: `@safe`.

```csharp
class safe { }

class C
{
    safe M1() => new safe(); // previously `safe` refers to a type, now it is a keyword
    @safe M2() => new safe(); // workaround
}
```

### 2.5 Modifier and context rules under opt-in

- `unsafe` on a member marks it *requires-unsafe* and **no longer introduces an `unsafe` context** in its
  body; only explicit `unsafe` regions do.
- `unsafe` is an **error** on: `delegate` declarations, static constructors, destructors, and type
  declarations (`class`, `struct`, …).
- `unsafe` on a constructor introduces an `unsafe` context inside its **initializer**.
- Types with a parameterless *requires-unsafe* constructor do not satisfy `new()`/`struct` constraints in
  declaration positions, and satisfy them in expression positions only inside an `unsafe` context.
- `unsafe`/`safe` on a member is **not** inherited by nested lambdas or local functions. Lambdas cannot be
  marked *requires-unsafe*.
- `partial` members: both parts must agree on `unsafe`/`safe`.
- Property accessors may independently carry `unsafe`/`safe`; the modifier may be on the property or on the
  accessors, not both; applying the same modifier to all accessors is an error. Event accessors cannot be
  modified independently.
- Adding `unsafe` to an override/implementation of a member that is not *requires-unsafe* is an error.
- Converting a *requires-unsafe* member to a delegate outside an `unsafe` context is an error. Delegate types
  and function types cannot be *requires-unsafe*.
- `nameof(requiresUnsafeMember)` no longer reports an unsafe-context error (Preview 7, roslyn #84325).

### 2.6 Metadata

```csharp
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Module, Inherited = false)]
    public sealed class MemorySafetyRulesAttribute : Attribute
    {
        public MemorySafetyRulesAttribute(int version) => Version = version;
        public int Version { get; }
    }
}
```

Per the spec, the assembly is stamped with `MemorySafetyRulesAttribute` filled with `15` as the language
version, and *requires-unsafe* non-type members get a `RequiresUnsafeAttribute`. Applying either attribute
explicitly in source is an error.

**Roslyn well-known types (current, more precise than the spec text):**

- `System.Runtime.CompilerServices.MemorySafetyRulesAttribute` — `.ctor(int)`
- `System.Diagnostics.CodeAnalysis.RequiresUnsafeAttribute` — parameterless `.ctor`.
  **Note the namespace: `System.Diagnostics.CodeAnalysis`, not `System.Runtime.CompilerServices` as the
  csharplang spec text states.** The Roslyn table is the more recent source.

**Compat mode.** For modules that have not opted in, a member is treated as *requires-unsafe* if a pointer or
function-pointer type appears anywhere in its parameter or return types (including nested, e.g. `int*[]`).
This does **not** apply to pointers in constraint types or substituted generic parameters. Preview 7
(roslyn #83660) extended compat-mode enforcement to callers that have not opted in, closing a window in
which merely bumping `LangVersion` would make code *less* protected.

**Breaking change** (VS 2026 18.9) "unsafe required for more members":

```csharp
var c = new C();
int a = c.M(null); // error always
int b = c[null];   // no error in C# 15, reports CS9363 in preview/next

class C
{
    public unsafe int M(int* x) => 0;
    public unsafe int this[int* x] => 0;
}
```

**Breaking change** (VS 2026 18.7) "pointer types no longer require an unsafe context": lambda inference now
considers more candidates, producing new **CS0121** ambiguities:

```csharp
M(x => { }); // previously prints "2"; now error CS0121 (ambiguous)

static void M(F1 f) { Console.WriteLine(1); }
static void M(F2 f) { Console.WriteLine(2); }

unsafe delegate void F1(int* x);
delegate void F2(int x);

// Mitigation:
M((int x) => { });
```

### 2.7 New Roslyn public API (all `RSEXPERIMENTAL006`)

```
Microsoft.CodeAnalysis.MemorySafetyRulesVersion                       // enum
Microsoft.CodeAnalysis.MemorySafetyRulesVersion.Version1 = 1
Microsoft.CodeAnalysis.MemorySafetyRulesVersion.Version2 = 2
Microsoft.CodeAnalysis.IModuleSymbol.MemorySafetyRulesVersion.get -> MemorySafetyRulesVersion
Microsoft.CodeAnalysis.ISymbol.RequiresUnsafeContext.get -> bool
Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions.MemorySafetyRulesVersion.get -> MemorySafetyRulesVersion
Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions.WithMemorySafetyRulesVersion(MemorySafetyRulesVersion) -> CSharpCompilationOptions!
Microsoft.CodeAnalysis.CSharp.SyntaxKind.SafeKeyword = 8454
```

`ISymbol.RequiresUnsafeContext` is a **cross-language** (`Microsoft.CodeAnalysis`) API, so it appears on
every symbol.

### 2.8 VB

No Visual Basic support is planned; VB has no `unsafe` contexts and no pointer support.

---

## 3. Also shipping in the .NET 11 compiler (not "C# 15 language features")

### 3.1 `ExtendedLayoutAttribute`

- Roslyn doc: `https://github.com/dotnet/roslyn/blob/main/docs/features/ExtendedLayoutAttribute.md`
- Runtime issue: <https://github.com/dotnet/runtime/issues/100896>. Roslyn PR
  <https://github.com/dotnet/roslyn/pull/78741>, "Merged into 18.3". Developer: jkoritzinsky.
- Shipped in **.NET 11 Preview 1**. Not language-version gated.

Behavior of `System.Runtime.InteropServices.ExtendedLayoutAttribute`:
- The compiler emits `TypeAttributes.ExtendedLayout` in the type's `TypeAttributes` flags.
- `StructLayoutAttribute` may not be combined with it.
- (C# only) `InlineArrayAttribute` may not be combined with it.
- `ITypeSymbol.Layout` returns a `TypeLayout` with `LayoutKind` = `Extended` (`1`), `Size` = 0, `Pack` = 0.
- The attribute is preserved on NoPIA-embedded types.
- The compiler does not know the specific options on the attribute and does not validate field types.

**New public API:**

```
Microsoft.CodeAnalysis.TypeLayout                            // struct, now public
Microsoft.CodeAnalysis.TypeLayout.Kind.get -> System.Runtime.InteropServices.LayoutKind
Microsoft.CodeAnalysis.TypeLayout.Size.get -> int
Microsoft.CodeAnalysis.TypeLayout.PackingSize.get -> ushort
Microsoft.CodeAnalysis.INamedTypeSymbol.TypeLayout.get -> Microsoft.CodeAnalysis.TypeLayout
```

`Microsoft.CodeAnalysis.TypeLayout` and `INamedTypeSymbol.TypeLayout` are **newly public** — previously
`TypeLayout` was internal to the compilers.

### 3.2 Runtime async (Runtime Async V2)

- Roslyn design doc: `https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Runtime Async Design.md`
- ECMA-335 spec change: `https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md`
- Runtime tracking issue: <https://github.com/dotnet/runtime/issues/109632>
- Roslyn test plan: <https://github.com/dotnet/roslyn/issues/75960>. Developer: 333fred.
  Status: "Main feature merged into main in preview". A separate `features/runtime-async-streams` branch
  (jcouv) is still in progress.
- **Preview feature. Opt-in per project:**

```xml
<PropertyGroup>
  <Features>runtime-async=on</Features>
</PropertyGroup>
```

A `net11.0` project no longer needs `<EnablePreviewFeatures>true</EnablePreviewFeatures>`. The
`DOTNET_RuntimeAsync` and `UNSUPPORTED_RuntimeAsync` environment variables have been **removed**; opt out
with `<UseRuntimeAsync>false</UseRuntimeAsync>`. **The .NET runtime libraries themselves ship compiled with
`runtime-async=on` and contain no compiler-generated async state machines.**

**Codegen change.** An `async Task M()` is emitted as:

```csharp
[MethodImpl(MethodImplOptions.Async)]   // MethodImplOptions.Async = 0x2000
Task M()
{
  // awaits lowered to runtime-async call format or AsyncHelpers.Await(...)
}
```

Same for `Task<T>`, `ValueTask`, `ValueTask<T>`. Any other `Task`-like return type still uses a
compiler-generated state machine. `MethodImplOptions.Async` may not be applied by hand; the compiler
reports an error.

Supporting APIs, which must live in the assembly that defines `System.Object` and references nothing else
(`System.Runtime`):

```csharp
namespace System.Runtime.CompilerServices;

[Experimental("SYSLIB5007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
public static partial class AsyncHelpers
{
    public static void UnsafeAwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion { }
    public static void AwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion { }

    public static void Await(Task task);
    public static T    Await<T>(Task<T> task);
    public static void Await(ValueTask task);
    public static T    Await<T>(ValueTask<T> task);
    public static void Await(ConfiguredTaskAwaitable configuredAwaitable);
    public static T    Await<T>(ConfiguredTaskAwaitable<T> configuredAwaitable);
    public static void Await(ConfiguredValueTaskAwaitable configuredAwaitable);
    public static T    Await<T>(ConfiguredValueTaskAwaitable<T> configuredAwaitable);
}
```

An experimentation escape hatch attribute is recognized but not defined in the BCL and may be removed:

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class RuntimeAsyncMethodGenerationAttribute(bool runtimeAsync) : Attribute();
```

Preview 7 clarified the "feature not supported by runtime-async" diagnostic; `__arglist` can never be
lowered by the runtime-async transform (roslyn #84263). Exposed symbols give no direct indication of
whether they were compiled with runtime async, and the compiler does not know whether a referenced
assembly's method was.

### 3.3 Pre-compilation source outputs (source generators)

- Roslyn doc: `https://github.com/dotnet/roslyn/blob/main/docs/features/pre-compilation-source-outputs.md`
- Experimental diagnostic id **`RSEXPERIMENTAL007`**.

A new incremental-generator stage between `RegisterPostInitializationOutput` (no inputs) and
`RegisterSourceOutput` (full compilation). Its output is added to the **initial** compilation, so it is
visible to every generator's standard phase, while the generation logic may read
`AdditionalTextsProvider`, `ParseOptionsProvider`, and analyzer config options. Primary motivation is a
~50% speedup for the Razor generator by removing its private intermediate compilation.

```
[RSEXPERIMENTAL007] IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput<TSource>(
    IncrementalValueProvider<TSource> source, Action<PreCompilationSourceProductionContext, TSource> action)
[RSEXPERIMENTAL007] IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput<TSource>(
    IncrementalValuesProvider<TSource> source, Action<PreCompilationSourceProductionContext, TSource> action)
[RSEXPERIMENTAL007] Microsoft.CodeAnalysis.PreCompilationSourceProductionContext          // struct
[RSEXPERIMENTAL007]   .AddSource(string hintName, SourceText sourceText)
[RSEXPERIMENTAL007]   .AddSource(string hintName, string source)
[RSEXPERIMENTAL007]   .CancellationToken.get -> CancellationToken
Microsoft.CodeAnalysis.IncrementalGeneratorOutputKind.PreCompilation = 16
const Microsoft.CodeAnalysis.WellKnownGeneratorOutputs.PreCompilationSourceOutput = "PreCompilationSourceOutput"
```

### 3.4 Other compiler/tooling changes shipped in .NET 11 previews

- **`SourceHashAlgorithm.Sha384 = 3`, `SourceHashAlgorithm.Sha512 = 4`** (new public API in
  `Microsoft.CodeAnalysis.Text`).
- **`CSharpExtensions.GetValueConversion(this ICoalesceOperation)`** → `Conversion` (new public API).
- **CS9378**, a dedicated error for a misplaced `#!` shebang directive (Preview 4, roslyn #83112),
  replacing the misleading CS1040. Covers leading whitespace before `#!` and whitespace between `#` and `!`.
- **Opt-in VBCSCompiler compilation cache** (Preview 4, roslyn #82881), enabled via the
  `ROSLYN_CACHE_PATH` environment variable or the `use-global-cache` feature flag through MSBuild. Cache key
  is the existing deterministic compilation key. No eviction. Warnings and `/reportanalyzer` output are not
  replayed on a hit. Experimental.
- **Null check in compiler-synthesized inline-array helpers** (Preview 7, roslyn #84488, #84523). The
  `<PrivateImplementationDetails>` helpers `InlineArrayElementRef`, `InlineArrayElementRefReadOnly`,
  `InlineArrayAsSpan`, `InlineArrayAsReadOnlySpan` now null-check the incoming byref, so forming a `ref` or
  `Span<T>` from a null inline-array reference deterministically throws `NullReferenceException`. This is a
  **codegen change that requires no source change**; the JIT elides the check when it can prove non-null.
- Ref-safety fix for collection expressions targeting `IEnumerable<T>` of a `ref struct` (Preview 4,
  roslyn #82401).

### 3.5 Pre-existing breaking changes in .NET 11 not tied to a C# 15 feature

From `docs/compilers/CSharp/Compiler Breaking Changes - DotNet 11.md`:

1. **The *safe-context* of a collection expression of `Span`/`ReadOnlySpan` type is now *declaration-block*.**
2. **Synthesizing a `ref readonly` returning delegate now requires `System.Runtime.InteropServices.InAttribute`.**
3. **`ref readonly` local functions now require `System.Runtime.InteropServices.InAttribute`.**
4. **Dynamic evaluation of `&&`/`||` is not allowed with the left operand statically typed as an interface.**
5. **`nameof(this.)` in attributes is disallowed.**

---

## 4. Working set: features in progress, NOT in C# 15

Source: `https://github.com/dotnet/roslyn/blob/main/docs/Language Feature Status.md`, "Working Set C#"
section, above the horizontal break (in progress). The status document's own FAQ states explicitly that a
target version is **not** a guarantee.

| Feature | csharplang issue | Roslyn branch | Roslyn state issue | Developer |
|---|---|---|---|---|
| Dictionary expressions | [8659](https://github.com/dotnet/csharplang/issues/8659) | `features/dictionary-expressions` | [81860](https://github.com/dotnet/roslyn/issues/81860) | 333fred |
| Null-conditional await | [8631](https://github.com/dotnet/csharplang/issues/8631) | `features/null-conditional-await` | [83237](https://github.com/dotnet/roslyn/issues/83237) | CyrusNajmabadi |
| Chained relational comparison | [8861](https://github.com/dotnet/csharplang/issues/8861) | `features/chained-relational-comparison` | [83255](https://github.com/dotnet/roslyn/issues/83255) | CyrusNajmabadi |
| Target-typed static member access | [9138](https://github.com/dotnet/csharplang/issues/9138) | `features/target-typed-static-member-access` | [83323](https://github.com/dotnet/roslyn/issues/83323) | CyrusNajmabadi |
| Relax modifier ordering | [8966](https://github.com/dotnet/csharplang/issues/8966) | `features/relaxed-modifier-ordering` | [83324](https://github.com/dotnet/roslyn/issues/83324) | CyrusNajmabadi |
| Compound assignment in initializers | [9896](https://github.com/dotnet/csharplang/issues/9896) | `features/compound-assignment-in-initializer` | [83420](https://github.com/dotnet/roslyn/issues/83420) | CyrusNajmabadi |
| Extension members on typeless receivers | [10146](https://github.com/dotnet/csharplang/issues/10146) | `features/extension-members-on-typeless-receivers` | [83428](https://github.com/dotnet/roslyn/issues/83428) | CyrusNajmabadi |
| Runtime Async Streams | — | `features/runtime-async-streams` | [75960](https://github.com/dotnet/roslyn/issues/75960) | jcouv |
| Extension constants | [10242](https://github.com/dotnet/csharplang/issues/10242) | `features/extension-consts` | [84269](https://github.com/dotnet/roslyn/issues/84269) | jcouv |
| Type Parameter Inference from Constraints | [9453](https://github.com/dotnet/csharplang/issues/9453) | [PR 84655](https://github.com/dotnet/roslyn/pull/84655) | [84868](https://github.com/dotnet/roslyn/issues/84868) | agocke |

Below the break (merged): **Unsafe evolution** (preview, .NET 11p2 / VS 18.6) and **Runtime Async**.

### 4.1 Dictionary expressions

New grammar production, `key_value_pair_element`:

```diff
collection_element
  : expression_element
  | spread_element
+ | key_value_pair_element
  ;
```

```csharp
Dictionary<string, int> ages = ["mads": 21, "dustin": 22];
var merged = [.. currentStudents, "mads": 21, "dustin": 22];
```

Targets concrete dictionary-like types with a read/write indexer `TValue this[TKey] { get; set; }`
(`Dictionary<,>`, `ConcurrentDictionary<,>`), and the BCL interfaces `IDictionary<K,V>` and
`IReadOnlyDictionary<K,V>`. `CollectionBuilderAttribute` support extends it to `ImmutableDictionary<,>`,
`FrozenDictionary<,>`, etc. The dictionary-interface rows of the collection-expression-arguments table
(§1.1) are specified as part of this feature.

**Not merged into `main`:** there is no `IDS_Feature...` entry for it in `MessageID.cs`. It will not be in
C# 15 GA.

### 4.2 Null-conditional await — `await? e`

```csharp
await? GetX()?.DoSomethingAsync();
```

Semantics: `((object)t == null) ? default(X) : await t` (or `!t.HasValue ? default(X) : await t.Value` for
`Nullable<V>`), with `t` evaluated once. Formally the *null_conditional_member_access* of §12.8.8 with
`await` substituted on the non-null branch.

### 4.3 Chained relational comparison

`a < b < c`, `min <= x <= max`, `0 <= i < array.Length`, with the middle operand evaluated once.
Extends to `<`, `<=`, `>`, `>=` in any order and any length.

### 4.4 Target-typed static member access

```csharp
type.GetMethod("Name", .Public | .Instance | .DeclaredOnly);
if (someString.Equals("Value", .OrdinalIgnoreCase)) ...
control.ForeColor = .Red;
entity.InvoiceDate = .Today;
Option<int> option = condition ? .None : .Some(42);
CustomResult result = condition ? new .Success(42) : new .Error("message");
return result switch { .Success(var val) => val, .Error => defaultVal };
[AttributeUsage(.Class | .Struct | .Interface | .Enum | .Delegate)]
class MyAttribute : Attribute;
```

### 4.5 Relax modifier ordering

Allow `partial` in any position in a modifier list on a type or member declaration, and `ref` in any
position on a struct declaration:

```csharp
partial internal class C { }
ref internal struct RS { }
internal partial ref struct RS { }
partial public Program() { }
partial public int Prop { }
partial public void Method() { }
```

### 4.6 Compound assignment in object initializers and `with` expressions

```csharp
var timer = new DispatcherTimer
{
    Interval = TimeSpan.FromSeconds(1d),
    Tick += (_, _) => { /*actual work*/ },
};

var newCounter = counter with { Value -= 1 };
```

### 4.7 Extension members on typeless receivers

```csharp
var a = [1, 2, 3].ToImmutableArray();
var memoized = SomeMethod.Memoize();
var x = (cond ? null : GetInt()).SomeNullableExtension();
```

The receiver is treated as the first argument of each candidate extension member; receivers that already
have a type bind unchanged.

### 4.8 Extension constants

```csharp
public static class E
{
    extension(object)
    {
        public const int Member = 42;
    }
}

_ = object.Member; // 42
```

Grammar: `extension_member_declaration` gains `constant_declaration`.

### 4.9 Type parameter inference from constraints

```csharp
List<int> l = [1, 2, 3];
M(l); // Today: TElement cannot be inferred. With this proposal, successful call.

void M<TEnumerable, TElement>(TEnumerable t) where TEnumerable : IEnumerable<TElement> { ... }
```

Constraints of inferred type parameters are promoted to "fake arguments" during type inference.

---

## 5. Explicitly checked and NOT in C# 15

- **Dictionary expressions.** In progress; not merged into `main`; no `MessageID`. See §4.1.
- **Interceptors.** Per `https://github.com/dotnet/roslyn/blob/main/docs/features/interceptors.md`:
  "first shipped experimentally in .NET 8, with stable support in .NET 9.0.2xx SDK and later". The
  attribute form is `[InterceptsLocation(int version, string data)]`. **No C# 15 change** was found: no
  interceptors entry in the Language Feature Status working set, and no new `IDS_Feature` for them.
- **`field` keyword follow-ups.** The `field` keyword shipped in C# 14 (merged into 17.12p3, roslyn
  issue 57012). No follow-up appears in the working set. `fieldof` exists as a csharplang proposal file
  (`proposals/fieldof.md`) but has **no Roslyn branch or state issue**, so it is not in progress.
- **`params` improvements.** `params`-collections shipped in C# 13. No `params` item appears anywhere in
  the C# 15 table or the working set. The only `params` mention in the C# 15 specs is the
  `with_element`-invokes-a-`params`-constructor-in-expanded-form rule (§1.1) and an open issue about
  `params` in extension indexers.
- **Extension follow-ups beyond indexers.** Extension **constants** and extension members on **typeless
  receivers** are both in the working set (in progress), not in C# 15. csharplang also has
  `partial-extension-members.md` with no Roslyn branch.
- **Closed enums, case declarations, standard unions, unsigned `sizeof`, `capability-safe`,
  `top-level-members`, `final-initializers`, `factory-methods`, `readonly-parameters`,
  `ref-struct-closures`, `iterators-in-lambdas`, `block-bodied-switch-expression-arms`,
  `enhanced-switch-statements`, `left-right-join-in-query-expressions`, `pattern-variables`,
  `multiple-using-var-discards`, `anonymous-using-declarations`, `deconstruction-in-lambda-parameters`,
  `expand-ref`, `inference-for-constructor-calls`, `inference-for-type-patterns`,
  `target-typed-generic-type-inference`, `immediately-enumerated-collection-expressions`,
  `mixed-object-and-collection-initializers`, `relaxed-partial-ref-ordering`,
  `extra-accessor-in-property-override`, `readonly-setter-calls-on-non-variables`,
  `interpolated-string-handler-argument-value`, `conditional-operator-access-syntax-refinement`,
  `async-method-ref-parameters`, `async-main-update`, `breaking-change-warnings`.** These all exist as
  csharplang proposal files at `proposals/` but have **no Roslyn branch, no state issue, and no `MessageID`**.
  They are design work only.

Full listing of `dotnet/csharplang/proposals/` (top level, 2026-09-03):
`anonymous-using-declarations, async-main-update, async-method-ref-parameters,
block-bodied-switch-expression-arms, breaking-change-warnings, capability-safe, case-declarations,
chained-relational-comparison, closed-enums, compound-assignment-in-initializer-and-with,
conditional-operator-access-syntax-refinement, deconstruction-in-lambda-parameters, dictionary-expressions,
enhanced-switch-statements, expand-ref, extension-constants, extension-members-on-typeless-receivers,
extra-accessor-in-property-override, factory-methods, fieldof, final-initializers,
immediately-enumerated-collection-expressions, inference-for-constructor-calls, inference-for-type-patterns,
interpolated-string-handler-argument-value, iterators-in-lambdas, left-right-join-in-query-expressions,
mixed-object-and-collection-initializers, multiple-using-var-discards, null-conditional-await,
partial-extension-members, pattern-variables, readonly-parameters, readonly-setter-calls-on-non-variables,
ref-struct-closures, relaxed-partial-ref-ordering, standard-unions, target-typed-generic-type-inference,
target-typed-static-member-access, top-level-members, type-parameter-inference-from-constraints,
unsafe-evolution, unsigned-sizeof`
plus the versioned folders `csharp-6.0` … `csharp-15.0`, `inactive`, and `rejected`.

`proposals/csharp-15.0/` contains exactly five files:
`closed-hierarchies.md`, `collection-expression-arguments.md`, `extension-indexers.md`,
`labeled-break-continue.md`, `unions.md`.

---

## 6. Consolidated Roslyn API/syntax delta (the part that matters to a Roslyn-based rewriter)

### 6.1 New `SyntaxKind` values

Source: `src/Compilers/CSharp/Portable/Syntax/SyntaxKind.cs`

| Kind | Value | Notes |
|---|---|---|
| `UnionKeyword` | 8452 | contextual keyword |
| `ClosedKeyword` | 8453 | contextual keyword |
| `SafeKeyword` | 8454 | contextual keyword, `[Experimental("RSEXPERIMENTAL006")]` |
| `UnsafeExpression` | 8769 | node kind, `RSEXPERIMENTAL006` |
| `WithElement` | 9081 | node kind |
| `UnionDeclaration` | 9082 | node kind |

Adjacent pre-existing values for orientation: `ExtensionKeyword = 8451`, `ScopedType = 9075`,
`CollectionExpression = 9076`, `ExpressionElement = 9077`, `SpreadElement = 9078`,
`ExtensionBlockDeclaration = 9079`, `IgnoredDirectiveTrivia = 9080`.

When adding a contextual keyword, `SyntaxFacts.GetContextualKeywordKinds()`,
`SyntaxFacts.IsContextualKeyword(SyntaxKind)`, `SyntaxFacts.GetContextualKeywordKind(string)` and
`SyntaxFacts.GetText(SyntaxKind)` are all updated by the compiler team; a consumer that enumerates
contextual keywords will see three new entries.

### 6.2 New / changed syntax nodes

| Node | Change |
|---|---|
| `UnionDeclarationSyntax` | **new**, `Base="TypeDeclarationSyntax"`, `SkipConvenienceFactories="true"` |
| `WithElementSyntax` | **new**, `Base="CollectionElementSyntax"` |
| `UnsafeExpressionSyntax` | **new**, `Base="ExpressionSyntax"`, `ExperimentalUrl` set |
| `BreakStatementSyntax` | **changed**: new optional `Name` field of type `IdentifierNameSyntax` |
| `ContinueStatementSyntax` | **changed**: new optional `Name` field of type `IdentifierNameSyntax` |
| `IndexerDeclarationSyntax` | unchanged, but now legal inside `ExtensionBlockDeclarationSyntax` |
| `TypeDeclarationSyntax` | `Keyword` doc updated to include `"union"` |

Three new `CSharpSyntaxVisitor` / `CSharpSyntaxVisitor<TResult>` / `CSharpSyntaxRewriter` virtual methods:
`VisitUnionDeclaration`, `VisitWithElement`, `VisitUnsafeExpression`.

### 6.3 New symbol / semantic-model API

```
// Microsoft.CodeAnalysis (cross-language)
ITypeSymbol.IsUnion.get -> bool
ITypeSymbol.UnionCaseTypes.get -> ImmutableArray<ITypeSymbol!>
ITypeSymbol.IsClosed.get -> bool
ITypeSymbol.GetClosedDerivedTypeInfo(CancellationToken) -> ClosedDerivedTypeInfo
ClosedDerivedTypeInfo.ClosedDerivedTypes.get -> ImmutableArray<INamedTypeSymbol!>
ClosedDerivedTypeInfo.IsComplete.get -> bool
INamedTypeSymbol.TypeLayout.get -> TypeLayout
TypeLayout (struct: Kind, Size, PackingSize)
Operations.CommonConversion.IsUnion.get -> bool
Operations.ICollectionExpressionOperation.ConstructArguments.get -> ImmutableArray<IOperation!>
Operations.ICollectionExpressionElementsPlaceholderOperation
OperationKind.CollectionExpressionElementsPlaceholder = 129
OperationVisitor.VisitCollectionExpressionElementsPlaceholder(...)
WellKnownMemberNames.HasValuePropertyName = "HasValue"
WellKnownMemberNames.TryGetValueMethodName = "TryGetValue"
Text.SourceHashAlgorithm.Sha384 = 3
Text.SourceHashAlgorithm.Sha512 = 4
[RSEXPERIMENTAL006] ISymbol.RequiresUnsafeContext.get -> bool
[RSEXPERIMENTAL006] IModuleSymbol.MemorySafetyRulesVersion.get -> MemorySafetyRulesVersion
[RSEXPERIMENTAL006] MemorySafetyRulesVersion { Version1 = 1, Version2 = 2 }
[RSEXPERIMENTAL007] IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput<TSource>(...)
[RSEXPERIMENTAL007] PreCompilationSourceProductionContext
IncrementalGeneratorOutputKind.PreCompilation = 16
WellKnownGeneratorOutputs.PreCompilationSourceOutput = "PreCompilationSourceOutput"

// Microsoft.CodeAnalysis.CSharp
LanguageVersion.CSharp15 = 1500
Conversion.IsUnion.get -> bool
CSharpExtensions.GetValueConversion(this ICoalesceOperation) -> Conversion
[RSEXPERIMENTAL006] CSharpCompilationOptions.MemorySafetyRulesVersion.get / WithMemorySafetyRulesVersion(...)

// Microsoft.CodeAnalysis.Workspaces
Editing.DeclarationModifiers.IsClosed.get -> bool
Editing.DeclarationModifiers.WithIsClosed(bool) -> DeclarationModifiers
static Editing.DeclarationModifiers.Closed.get -> DeclarationModifiers
```

### 6.4 New well-known types and members recognized by the compiler

| Well-known type | Members |
|---|---|
| `System.Runtime.CompilerServices.UnionAttribute` | `.ctor()` |
| `System.Runtime.CompilerServices.IUnion` | (interface, `object? Value { get; }`) |
| `System.Runtime.CompilerServices.IsClosedTypeAttribute` | `.ctor()`, `DerivedTypes` (`System.Type[]`) |
| `System.Runtime.CompilerServices.MemorySafetyRulesAttribute` | `.ctor(int)` |
| `System.Diagnostics.CodeAnalysis.RequiresUnsafeAttribute` | `.ctor()` |
| `System.Runtime.InteropServices.ExtendedLayoutAttribute` | recognized; drives `TypeAttributes.ExtendedLayout` |
| `System.Runtime.CompilerServices.AsyncHelpers` | `Await`, `AwaitAwaiter`, `UnsafeAwaitAwaiter` (runtime async) |

`[CompilerFeatureRequired("ClosedClasses")]` is emitted on constructors of `closed` classes.

### 6.5 New experimental diagnostic ids in the Roslyn public API

| Id | Covers | Tracking URL |
|---|---|---|
| `RSEXPERIMENTAL006` | `SafeKeyword`, `UnsafeExpression`, `UnsafeExpressionSyntax`, `MemorySafetyRulesVersion`, `ISymbol.RequiresUnsafeContext`, `IModuleSymbol.MemorySafetyRulesVersion`, `CSharpCompilationOptions.MemorySafetyRulesVersion` | <https://github.com/dotnet/roslyn/issues/82789> |
| `RSEXPERIMENTAL007` | `RegisterPreCompilationSourceOutput`, `PreCompilationSourceProductionContext` | — |
| `SYSLIB5007` | `System.Runtime.CompilerServices.AsyncHelpers` (BCL side) | <https://aka.ms/dotnet-warnings/SYSLIB5007> |

---

## 7. Primary sources used

- `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15` (ms.date 2026-08-14, updated 2026-08-19)
- `https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union` (ms.date 2026-08-14)
- `https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime` (last updated for Preview 7)
- `https://raw.githubusercontent.com/dotnet/roslyn/main/docs/Language%20Feature%20Status.md`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/docs/compilers/CSharp/Compiler%20Breaking%20Changes%20-%20DotNet%2011.md`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/docs/compilers/CSharp/Runtime%20Async%20Design.md`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/docs/features/interceptors.md`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/docs/features/ExtendedLayoutAttribute.md`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/docs/features/pre-compilation-source-outputs.md`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Syntax/Syntax.xml`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Syntax/SyntaxKind.cs`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/LanguageVersion.cs`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Errors/MessageID.cs`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/PublicAPI.Unshipped.txt`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/Core/Portable/PublicAPI.Unshipped.txt`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/Core/Portable/WellKnownTypes.cs`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/Core/Portable/WellKnownMembers.cs`
- `https://raw.githubusercontent.com/dotnet/roslyn/main/src/Workspaces/Core/Portable/PublicAPI.Unshipped.txt`
- `https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/{unions,closed-hierarchies,collection-expression-arguments,extension-indexers,labeled-break-continue}.md`
- `https://github.com/dotnet/csharplang/blob/main/proposals/{unsafe-evolution,dictionary-expressions,null-conditional-await,chained-relational-comparison,target-typed-static-member-access,compound-assignment-in-initializer-and-with,extension-members-on-typeless-receivers,extension-constants,type-parameter-inference-from-constraints}.md`
- `https://github.com/dotnet/csharplang/pull/10302` (LDM revert of Try-Both union matching, 2026-08-18)
- `https://github.com/dotnet/roslyn/pull/83097` (non-virtual static interface members)
- `https://github.com/dotnet/csharplang/issues/8966` (relax modifier ordering)
- `https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview{1,3,4,5,6,7}/csharp.md`

Local working copies of everything downloaded are under
`C:\Users\GaelFraiteur\AppData\Local\Temp\claude\C--src-Metalama-2027-0-Metalama\86248111-7c7e-4f30-bf61-ae10afe3e5e4\scratchpad\net11\`.

---

## 8. Open questions

1. **Union pattern matching semantics are unstable.** .NET 11 Preview 7 shipped "Try-Both" matching
   (`pet is Pet` is true). csharplang PR #10302 (2026-08-18) reverted the spec to value-only unwrapping
   (`p is Pet` is an error). The compiler behaviour at GA is therefore not established by the sources
   consulted. Re-check `proposals/csharp-15.0/unions.md` and the Preview 8 / RC release notes.
2. **`UnionMatchingMode`.** Preview 7 notes mention a new `UnionMatchingMode` property controlling how union
   patterns are lowered for custom union declarations (roslyn #84436, #84499). It does not appear in
   `WellKnownTypes.cs`, `WellKnownMembers.cs`, or either `PublicAPI.Unshipped.txt`, so it is presumably
   compiler-internal. Whether it is exposed anywhere a consumer can observe is unresolved. It may also
   have been removed by the LDM revert.
3. **`RequiresUnsafeAttribute` namespace.** The csharplang spec declares it in
   `System.Runtime.CompilerServices`; Roslyn's well-known type table says
   `System.Diagnostics.CodeAnalysis`. The Roslyn table is presumed correct and more recent, but the spec has
   not been updated.
4. **`IsClosedTypeAttribute` exact shape.** The spec shows a parameterless attribute with no properties.
   Roslyn's well-known members add a `DerivedTypes` property of type `System.Type[]`, per
   dotnet/runtime #129009 and roslyn #84350. The public runtime declaration (constructor overloads,
   `AttributeUsage`, whether `DerivedTypes` is settable via a named argument) was not located in a runtime
   source file.
5. **Default `LangVersion` for `net11.0` at GA.** The .NET 11 preview release notes for Previews 6 and 7
   still instruct users to set `<LangVersion>preview</LangVersion>` for unions and extension indexers, even
   though `MessageID.cs` maps both to `LanguageVersion.CSharp15`. Whether the preview SDKs default `net11.0`
   to `latestMajor` (14) or to 15 was not confirmed; the GA SDK is expected to default `net11.0` to
   `CSharp15`, but this was not verified against an SDK targets file.
6. **Non-virtual static interface members.** No csharplang proposal document exists for this feature; the
   only source is Roslyn PR #83097 and the `MessageID` mapping. The precise diagnostic set and the
   interaction with `netstandard2.0` / .NET Framework targets is not documented on learn.microsoft.com.
7. **Whether "unsafe evolution" ships any part as stable at GA.** The pointer relaxations are described on
   learn.microsoft.com under the C# 15 "Memory safety" heading, but `MessageID.cs` gates
   `IDS_FeatureUnsafeEvolution` on `LanguageVersion.Preview` as a whole. It is unclear whether the
   relaxations will be split out into `CSharp15` before GA.
8. **`IsClosedTypeAttribute.DerivedTypes` and cross-assembly exhaustiveness.** The spec describes
   exhaustiveness as derived from source declarations in the same assembly; the `DerivedTypes` property
   suggests the derived-type set is now recorded in metadata for consuming compilers. The exact algorithm
   the compiler uses when reading a closed class from a reference assembly, and what `ClosedDerivedTypeInfo.IsComplete`
   means when it is `false`, is not documented in the sources consulted.
9. **Preview 8 / RC content.** Only Previews 1 through 7 exist in `dotnet/core/release-notes/11.0/preview`
   as of 2026-09-03. Any feature landing between Preview 7 and GA is not covered here.
