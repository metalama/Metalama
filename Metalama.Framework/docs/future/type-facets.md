# Type facets

This document proposes an addition to the public code model: a uniform way to expose the structure that is specific
to a kind of declared named type. It covers delegates, enums, records and C# 15 unions. Section 4.5 explains why it
does not cover tuples. It is a design proposal. Nothing described here is implemented.

## 1. The problem

A named type of some kinds carries structure that other named types do not have. A delegate has a signature. An
enum has an underlying type and a set of members. A record has an equality contract and a set of synthesized
members. A C# 15 union has case types and a `Value` property. The code model exposes that structure in four
different ways today. Two of them are defective, the third is correct only for a type that a type expression forms,
and the fourth is the one this proposal replaces.

### 1.1. A member reached by a string literal

The `Invoke` method of a delegate has no name in the code model, so it is reached by its identifier. The repository
contains about fifteen such lookups. Two of them are in `Metalama.Framework`, the public application programming
interface assembly:

```csharp
// Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:98
e => e.Type.Methods.OfName( "Invoke" ).Single().ReturnType.SpecialType == SpecialType.Void,
```

Four of them are the same body of `IEvent.Signature`, repeated in four implementations, and they do not agree with
each other on whether a missing `Invoke` method is `null` or an exception.

The synthesized members of a record and of a union have the same shape of problem. No public member names
`EqualityContract`, `PrintMembers` or the clone method. The `Value` property of a union would have to be reached as
`type.Properties.OfName( "Value" ).Single()`, which throws when a union written with the union attribute declares a
member of that name.

### 1.2. One member with several meanings

`INamedType.UnderlyingType` is documented as follows.

```csharp
/// Gets the underlying type of an enum, the non-nullable type of a nullable reference type, or the current type.
INamedType UnderlyingType { get; }
```

The caller has to know the kind of the type to know what the property returned.

### 1.3. A derived interface reached by a type test

`ITupleType` derives from `INamedType`, adds `TupleElements`, `TupleLength` and `CreateCreateInstanceExpression`,
and is reached by a type test. `IArrayType`, `IPointerType`, `IFunctionPointerType` and `IDynamicType` follow the
same pattern, and so does `IExtensionBlock`.

This mechanism is correct for a type that a type expression forms. Such a type reaches the model through one
construction path, the type factory, which decides its runtime interface at one site. Section 4.5 keeps the
mechanism for those types.

The mechanism does not extend to a declared type, for two reasons. First, a declared type reaches the model through
source symbols, metadata symbols, generic instantiation, builders, introduced types and the design-time partial
pipeline. Deciding the runtime type of the code model object correctly at every one of those sites is not reliable,
and for a union written with the union attribute the identity depends on an attribute that Roslyn synthesizes at
emit time. Second, the type test is normally paired with a value of `TypeKind`, and a value `TypeKind.Union` would
require a new arm in the seventeen switches over `TypeKind` that the analysis in
[`../2027.0/03-code-model-unions-closed.md`](../2027.0/03-code-model-unions-closed.md) inventoried, while the union
design depends on a union continuing to behave as a struct.

### 1.4. Flat members on `INamedType`

`IsRecord` is a flag on `INamedType`. Adding `IsUnion`, `IsUnionDeclaration` and `UnionCaseTypes` beside it, which
is what pull request #1991 proposes, continues that pattern. The count grows: the union introduction story adds the
`Value` property and the case constructors, which would be two more members. Each one is meaningless for the great
majority of named types.

## 2. The public interfaces

### 2.1. One collection of facets

A facet is the structure that a declared named type has because of its kind, and that other named types do not
have. A type has a collection of facets, which is empty for an ordinary class, struct or interface.

```csharp
// Metalama.Framework/Code/TypeFacetKind.cs
[CompileTime]
public enum TypeFacetKind
{
    Delegate,
    Enum,
    Record,
    Union
}
```

The enum has no `None` member. A facet that exists has a kind, and the absence of a facet is represented by a `null`
property of the collection.

```csharp
// Metalama.Framework/Code/ITypeFacet.cs
[CompileTime]
public interface ITypeFacet
{
    /// <summary>
    /// Gets the kind of the facet.
    /// </summary>
    TypeFacetKind Kind { get; }

    /// <summary>
    /// Gets the type to which the facet belongs.
    /// </summary>
    INamedType Type { get; }
}
```

```csharp
// Metalama.Framework/Code/Collections/ITypeFacetCollection.cs
public interface ITypeFacetCollection : IReadOnlyCollection<ITypeFacet>
{
    IDelegateFacet? Delegate { get; }

    IEnumFacet? Enum { get; }

    IRecordFacet? Record { get; }

    IUnionFacet? Union { get; }
}
```

```csharp
// Metalama.Framework/Code/INamedType.cs
/// <summary>
/// Gets the facets of the type, that is, the structure that is specific to its kind. The collection is empty for a
/// type that has no such structure. Enumerating the collection yields exactly the facets that are not <c>null</c>,
/// in the order of the members of <see cref="TypeFacetKind"/>.
/// </summary>
ITypeFacetCollection Facets { get; }
```

The collection follows the precedent of `IExtensionBlockCollection`, which derives from
`IReadOnlyCollection<IExtensionBlock>` and is reached by `INamedType.ExtensionBlocks`.

Consumer code:

```csharp
if ( type.Facets.Union is { } union )
{
    foreach ( var unionCase in union.Cases ) { /* ... */ }
}

var returnType = eventType.Facets.Delegate?.InvokeMethod.ReturnType;
```

The invariant is that the collection contains exactly the typed properties that are not `null`, and that `Count` is
their number. The invariant is not that `Count` is at most one. Section 4.2 gives the case where two facets
coexist.

### 2.2. Facets expose the special members as properties

A facet names every member that the compiler synthesizes for that kind of type, so that no consumer has to reach a
member by a string literal.

```csharp
// Metalama.Framework/Code/IDelegateFacet.cs
[CompileTime]
public interface IDelegateFacet : ITypeFacet
{
    /// <summary>
    /// Gets the <c>Invoke</c> method, which carries the signature of the delegate.
    /// </summary>
    IMethod InvokeMethod { get; }

    IType ReturnType { get; }

    IParameterList Parameters { get; }
}
```

```csharp
// Metalama.Framework/Code/IEnumFacet.cs
[CompileTime]
public interface IEnumFacet : ITypeFacet
{
    /// <summary>
    /// Gets the underlying integral type of the enum. Unlike <see cref="INamedType.UnderlyingType"/>, this property
    /// has one meaning.
    /// </summary>
    INamedType UnderlyingType { get; }

    /// <summary>
    /// Gets the members of the enum, in the order in which they are declared.
    /// </summary>
    IReadOnlyList<IField> Members { get; }

    /// <summary>
    /// Gets a value indicating whether the enum has the <see cref="System.FlagsAttribute"/> attribute.
    /// </summary>
    bool IsFlags { get; }
}
```

```csharp
// Metalama.Framework/Code/IRecordFacet.cs
[CompileTime]
public interface IRecordFacet : ITypeFacet
{
    /// <summary>
    /// Gets the <c>EqualityContract</c> property, or <c>null</c> when the type is a record struct, which has none.
    /// </summary>
    IProperty? EqualityContract { get; }

    IMethod PrintMembersMethod { get; }

    /// <summary>
    /// Gets the clone method, whose metadata name is <c>&lt;Clone&gt;$</c>, or <c>null</c> when the type is a record
    /// struct, which has none.
    /// </summary>
    IMethod? CloneMethod { get; }

    /// <summary>
    /// Gets the copy constructor, or <c>null</c> when the type is a record struct, which has none.
    /// </summary>
    IConstructor? CopyConstructor { get; }

    /// <summary>
    /// Gets the <c>Deconstruct</c> method, or <c>null</c> when the record is not positional.
    /// </summary>
    IMethod? DeconstructMethod { get; }

    /// <summary>
    /// Gets the properties that the positional parameters of the record declare, or an empty list when the record is
    /// not positional.
    /// </summary>
    IReadOnlyList<IProperty> PositionalProperties { get; }
}
```

```csharp
// Metalama.Framework/Code/IUnionFacet.cs
[CompileTime]
public interface IUnionFacet : ITypeFacet
{
    /// <summary>
    /// Gets the form in which the union is written.
    /// </summary>
    UnionForm Form { get; }

    /// <summary>
    /// Gets the cases of the union, in the order in which the compiler reports them.
    /// </summary>
    IReadOnlyList<IUnionCase> Cases { get; }

    /// <summary>
    /// Gets the <c>Value</c> property, which the compiler synthesizes for a union declaration and which the author
    /// writes for the attribute form.
    /// </summary>
    IProperty ValueProperty { get; }
}

[CompileTime]
public enum UnionForm
{
    /// <summary>The type is declared with the <c>union</c> keyword.</summary>
    Declaration,

    /// <summary>The type is a class or a struct that carries <c>UnionAttribute</c>.</summary>
    Attribute
}

[CompileTime]
public interface IUnionCase
{
    IType Type { get; }

    /// <summary>
    /// Gets the zero-based index of the case, in the order in which the compiler reports the cases.
    /// </summary>
    int Index { get; }

    /// <summary>
    /// Gets the creation member of the case: the constructor synthesized for a union declaration, the union
    /// constructor of the attribute form, or the static <c>Create</c> method of the union member provider.
    /// </summary>
    IMethodBase CreationMember { get; }

    /// <summary>
    /// Creates an expression that creates an instance of the union holding a value of this case.
    /// </summary>
    IExpression CreateCreateInstanceExpression( IExpression value );
}
```

`IUnionCase` follows `ITupleElement`, which is richer than the type of the element and carries `Index`,
`HasFriendlyName` and `CorrespondingTupleField`. The richer abstraction is required rather than convenient: Roslyn
collapses duplicate case types through a set, so the index of a case is not recoverable from its type, and the
creation member of the attribute form may be a static method rather than a constructor, which `IConstructor` cannot
express.

### 2.3. Facets expose invokers

The members that a facet exposes are typed as `IMethod`, `IProperty`, `IConstructor` and `IField`. Those interfaces
derive from the invoker interfaces: `IMethod` derives from `IMethodInvoker`, `IConstructor` from
`IConstructorInvoker`, and `IFieldOrProperty` from `IFieldOrPropertyInvoker`. A consumer therefore reaches the
invoker through the member, and no facet declares an invoker member of its own.

```csharp
// Generates a call to the Invoke method of the delegate.
var call = eventType.Facets.Delegate!.InvokeMethod.With( handler ).Invoke( args );
```

A facet declares a method that creates an expression only where no member of the type carries the operation. The
precedent is `ITupleType.CreateCreateInstanceExpression`, which exists because a tuple is created by a syntactic
construct and not by calling a member. `IUnionCase.CreateCreateInstanceExpression` exists for the same reason: the
creation member differs by form, so the consumer would otherwise have to test whether it is a constructor or a
static method.

### 2.4. Writing is on builders, not on facets

A facet is read-only. An aspect that introduces a type of a specific kind uses a builder interface derived from
`INamedTypeBuilder`.

```csharp
// Metalama.Framework/Code/DeclarationBuilders/IUnionBuilder.cs
[InternalImplement]
public interface IUnionBuilder : INamedTypeBuilder
{
    /// <summary>
    /// Adds a case to the union.
    /// </summary>
    IUnionCase AddCase( IType caseType );
}
```

The precedent is `IExtensionBlockBuilder`, which derives from `INamedTypeBuilder` and from `IExtensionBlock`, and
whose documentation lists the inherited operations that are not valid for an extension block. Those operations throw
`NotSupportedException`. A union builder does the same: `BaseType` cannot be set, because a union declaration is a
struct, and an instance field, an automatic property and a field-like event cannot be introduced, because the
compiler reports CS9373 for them in a union declaration.

`IDelegateBuilder`, `IEnumBuilder` and `IRecordBuilder` follow the same shape. They are not part of this proposal
beyond the shape, because the introduction of those kinds is not currently supported.

The reason for keeping the writing surface off the facet is that the reader and the writer have different
lifetimes. A facet describes a type that exists. A builder describes a type that is being constructed and whose
members are not yet resolvable.

## 3. What this replaces, and what it leaves in place

| Existing member | Disposition |
| --- | --- |
| `INamedType.IsRecord` | Kept. It is shipped, widely used, and cheap. The documentation gains a reference to `Facets.Record`. |
| `INamedType.UnderlyingType` | Kept. It is shipped and it also serves nullable reference types. `IEnumFacet.UnderlyingType` is the member with one meaning, and the documentation of both says so. |
| `IEvent.Signature` | Kept, and defined as `Type.Facets.Delegate!.InvokeMethod`. The four duplicate implementations are removed. The member currently has no documentation and gains it. |
| `ITupleType`, `ITupleElement`, `TypeKind.Tuple`, `TypeFactory.CreateTupleType` | Unchanged. A tuple is not a facet. See section 4.5. |
| `INamedType.PrimaryConstructor` | Kept. A primary constructor is not specific to records since C# 12, so it does not move to `IRecordFacet`. |
| `IExtensionBlock` | Unchanged, and deliberately not a facet. See section 4.1. |

No member is made obsolete by this proposal, and it carries no user-facing breaking change.

## 4. Decisions

### 4.1. An extension block is not a facet

A facet describes the structure of a type that a program can use. An extension block is not such a type: it has no
usable name, no variable can be declared of it, and it is reached from its containing type through
`INamedType.ExtensionBlocks`, where it is a member rather than a facet of itself. `IExtensionBlock` derives from
`INamedType` for the convenience of the implementation, not because an extension block is a type in the sense of the
language.

### 4.2. A type may have more than one facet

A record can be a union. The C# 15 proposal resolves that `record union` is not supported, but it also states that
"any class or struct type with a `System.Runtime.CompilerServices.UnionAttribute` attribute is considered a union
type", and Roslyn implements exactly that:

```csharp
internal bool IsUnionType
    => TypeKind is TypeKind.Class or TypeKind.Struct && IsUnionTypeCore;
```

A record class is `TypeKind.Class` and a record struct is `TypeKind.Struct`, and no rule excludes them. For the
attribute form the case set is derived from the signatures: "each constructor with a single parameter is a union
constructor". The primary constructor of a record with one positional parameter is such a constructor. The
declaration `[Union] public record Wrapper( int Value );` is therefore a record and a union with one case of type
`int`, and both `Facets.Record` and `Facets.Union` are not `null` for it.

The copy constructor of a record does not become a case, because it is protected or private and the rule requires
the member to be public. A record with two or more positional parameters has no union creation member at all, which
the proposal makes an error.

This is the reason the collection does not promise that `Count` is at most one, and the reason there is no ordering
of facets by priority. A test with `[Union] public record Wrapper( int Value );` pins the invariant.

### 4.3. No `TypeKind.Union` and no `IUnionType`

The proposal adds no value to `TypeKind` and no interface derived from `INamedType` for unions. Section 1.3 gives
the two reasons. Roslyn made the same choice: `IsUnion` and `UnionCaseTypes` are declared on `ITypeSymbol`, and
there is no `IUnionTypeSymbol`.

### 4.4. No flat union members on `INamedType`

Pull request #1991 proposes `INamedType.IsUnion`, `INamedType.IsUnionDeclaration` and
`INamedType.UnionCaseTypes`. This proposal replaces all three with `Facets.Union`. The recommendation is to hold
them back from #1991 rather than ship them and duplicate them later, because a shipped public member cannot be
withdrawn and the repository already carries two members in that situation, `UnderlyingType` and `IsRecord`.

Two of the three would be wrong as specified in any case. `UnionCaseTypes` is documented there as empty when
`IsUnionDeclaration` is false, which describes the current implementation from syntax rather than the contract:
Roslyn documents `ITypeSymbol.UnionCaseTypes` as returning the case types "when `IsUnion` is true", and it is
defined for the attribute form. `IsUnionDeclaration` reads as a question about a declaration, on an object that is
already an `IDeclaration`. `UnionForm` states the distinction with the vocabulary of the language proposal, which
defines "union type" and "union declaration" as two terms, and it does so without that collision.

The counter-argument is that `IsUnion` is the cheap first test, that it is what `ITypeSymbol` exposes, and that
`type.Facets.Union is not null` is longer to write in a predicate over a collection of types. The proposal accepts
that cost, on the ground that a second way to ask the same question is the defect this document exists to remove.

### 4.5. A tuple is not a facet

A tuple is not declared. The type `(int X, string Y)` is formed by a type expression at the place where it is used,
and the element names are attached there. Metalama models it that way: `TupleType` and `TupleTypeImpl` are in
`CodeModel/Source/ConstructedTypes/`, beside the array, the pointer, the function pointer and the dynamic type, and
the elements of a tuple are a view over the fields of the underlying `ValueTuple` with the names overridden.

`ITupleType` therefore belongs to the family of `IArrayType`, `IPointerType` and `IFunctionPointerType`: interfaces
that describe the structure of a type that a type expression forms, that are reached by a type test, and that are
constructed at one site. Section 1.3 states why that mechanism is correct for that family and does not extend to a
declared type.

A facet describes the other thing: the structure that a named type has because of what its declaration says. A
delegate, an enum, a record and a union are declared. A tuple is not, so `ITupleType`, `ITupleElement`,
`TypeKind.Tuple` and `TypeFactory.CreateTupleType` are unchanged, and `ITypeFacetCollection` has no tuple property.

The cost of this decision is that a consumer that wants the structure of an arbitrary type has to know two
mechanisms: a type test for a type that a type expression forms, and the facet collection for a declared one. An
earlier revision of this document proposed the alternative, which is to declare `ITupleFacet` with the members of
`ITupleType`, to make `ITupleType` obsolete and forward its members to the facet, and to change the return type of
`TypeFactory.CreateTupleType` to `INamedType`. That alternative removes the second mechanism at the price of a
user-facing breaking change, sixty-three references to convert inside the repository, and a facet that describes a
type that nobody declared.

## 5. Implementation guidelines

These are constraints on the implementation, not a design of it.

1. `Facets` is one member on `INamedType`, for any number of facets. The number of members to implement does not
   grow when a facet is added. That is the difference from the flat design, which needs one member per exposed
   value.
2. The collection constructs the facets. No facet type declares a static factory method that returns `null`. A facet
   reference that a consumer holds is never `null`, and nullability is expressed only by the properties of the
   collection.
3. A type that has no facet returns a shared empty collection. Generic code that walks the model reads `Facets` on
   every type, so the common case must not allocate.
4. The implementations of `INamedType` that back a builder return the empty collection rather than throwing.
   Eligibility rules and advice validation run against builders, so an exception there is reached in normal use.
5. The facet interfaces name no Roslyn type. `Metalama.Framework` is not built per Roslyn version, while
   `Metalama.Framework.Engine` is, and the union facet reads `ITypeSymbol.IsUnion` and `ITypeSymbol.UnionCaseTypes`,
   which exist only in the latest variant. The conditional compilation is therefore confined to the construction of
   the collection in the engine, under the condition that section 6 of
   [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md) decides. The flat design instead needs a conditional block in
   each implementation of `INamedType`.
6. A facet is computed once per type and cached, in the manner of the other collection members of `INamedType`.

## 6. Order of implementation

The delegate facet is implemented first, and deliberately. It requires no C# 15, no Roslyn variant and no preview
language version, and it has about fifteen call sites to convert, so the shape is exercised against shipped
behaviour before anything that cannot be revised is public.

| Pull request | Content | Depends on |
| --- | --- | --- |
| 1 | `TypeFacetKind`, `ITypeFacet`, `ITypeFacetCollection`, `INamedType.Facets`, `IDelegateFacet`. Conversion of the `Invoke` lookups and of `IEvent.Signature`. | — |
| 2 | `IEnumFacet`. | 1 |
| 3 | `IRecordFacet`, and the conversion of the synthesized-member lookups of the linker. | 1 |
| 4 | `IUnionFacet`, `IUnionCase`, `UnionForm`. | 1, and the move to the stable Roslyn |
| 5 | `IUnionBuilder`, and the builder shapes of the other kinds. | 4, and the union introduction story |

Pull request 1 changes one shipped behaviour: `EligibilityRuleFactory` currently throws `InvalidOperationException`
when the type of an event is not a well-formed delegate, and it returns `false` after the conversion.

Pull request 1 is also the acceptance test of the design. Fourteen of the fifteen call sites assert that the
`Invoke` method exists, while `Facets.Delegate` is `null` for a malformed type. If several converted sites end up
asserting that the facet is not `null`, the nullable collection property is the wrong shape for those consumers, and
the design has to be revised before the union facet is built on it.

## 7. Open questions

1. Whether `IDelegateFacet` exposes `BeginInvoke` and `EndInvoke`. They exist for a delegate compiled for .NET
   Framework and not for one compiled for .NET. Exposing them adds two nullable members for a case that no known
   consumer needs.
2. Whether `IRecordFacet` is worth its nullable members. `EqualityContract`, `CloneMethod` and `CopyConstructor` are
   all `null` for a record struct. Splitting the interface in two would remove the nullability and add a type.
3. Whether the enum members deserve an abstraction of their own, in the manner of `ITupleElement`, carrying the
   constant value and the index, rather than `IReadOnlyList<IField>`.
4. Whether the `Delegate`, `Enum` and `Record` property names of `ITypeFacetCollection` pass the naming analyzers of
   this repository.
5. Whether the union facet is populated for a union read from a referenced assembly. The compiled form does not
   record the authoring form, so `Form` would report `Attribute` for a type that the author wrote with the `union`
   keyword.

## 8. References

- [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md), sections 3, 4 and 6.
- [`../2027.0/03-code-model-unions-closed.md`](../2027.0/03-code-model-unions-closed.md), finding CM-1.
- [`../2027.0/analysis-reports/12-csharp15-api-drafts.md`](../2027.0/analysis-reports/12-csharp15-api-drafts.md),
  which drafts the flat design and a bundle named `IUnionInfo`, and recommends the flat design.
- [`../2027.0/analysis-reports/11-introducing-unions-design.md`](../2027.0/analysis-reports/11-introducing-unions-design.md),
  which records the derivation of the case set of the attribute form.
- The C# 15 unions proposal, `dotnet/csharplang`, `proposals/csharp-15.0/unions.md`.
