# Type facets

This document proposes an addition to the public code model: a uniform way to expose the structure that is specific
to a kind of declared named type. It covers delegates, enums, records and C# 15 unions. Section 4.5 explains why it
does not cover tuples today. It is a design proposal. Nothing described here is implemented.

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

The flag itself is not the problem, and section 4.4 keeps the flags. The problem is the structure that follows the
flag.

## 2. The public interfaces

The facet types are declared in the namespace `Metalama.Framework.Code.Types`, beside `IArrayType`, `IPointerType`,
`IFunctionPointerType` and `IDynamicType`, because they describe the structure of a type.
`ITypeFacetCollection` is declared in `Metalama.Framework.Code.Collections`, beside `IExtensionBlockCollection`,
because it is a collection.

Every interface below carries `[InternalImplement]`. Adding a member to such an interface, including an inherited
one, is not a user-facing breaking change, so a facet can gain a member in a later release.

### 2.1. One collection of facets

A facet is the structure that a declared named type has because of its kind, and that other named types do not
have. A type has a collection of facets, which is empty for an ordinary class, struct or interface.

```csharp
// Metalama.Framework/Code/Types/TypeFacetKind.cs
[CompileTime]
public enum TypeFacetKind
{
    /// <summary>The type has no facet.</summary>
    None = 0,

    Delegate,

    Enum,

    Record,

    Union
}
```

```csharp
// Metalama.Framework/Code/Types/ITypeFacet.cs
[CompileTime]
[InternalImplement]
public interface ITypeFacet
{
    /// <summary>
    /// Gets the kind of the facet.
    /// </summary>
    TypeFacetKind FacetKind { get; }

    /// <summary>
    /// Gets the type to which the facet belongs.
    /// </summary>
    INamedType Type { get; }
}
```

```csharp
// Metalama.Framework/Code/Collections/ITypeFacetCollection.cs
[InternalImplement]
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
if ( type.IsUnion )
{
    foreach ( var unionCase in type.Facets.Union!.Cases ) { /* ... */ }
}

var returnType = eventType.Facets.Delegate?.InvokeMethod.ReturnType;
```

The invariant is that the collection contains exactly the typed properties that are not `null`, and that `Count` is
their number. The invariant is not that `Count` is at most one. Section 4.2 gives the case where two facets
coexist.

### 2.2. Facets expose the special members as properties

A facet names every member that the compiler synthesizes for that kind of type, so that no consumer has to reach a
member by a string literal.

Every member of a facet is a lazy expression, cached with `[Memo]`. A consumer that obtains a facet and reads one
member does not pay for resolving the others.

```csharp
// Metalama.Framework/Code/Types/IDelegateFacet.cs
[CompileTime]
[InternalImplement]
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

`BeginInvoke` and `EndInvoke` are not exposed. They exist only for a delegate compiled for .NET Framework, and they
are the asynchronous pattern that preceded `async`.

```csharp
// Metalama.Framework/Code/Types/IEnumFacet.cs
[CompileTime]
[InternalImplement]
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

The members of an enum are exposed as `IField`, and no interface of their own is declared for them. An enum member
is a field, so an interface derived from `IField` would exist only to remove members that do not apply, which is
more cumbersome than the property it would replace.

```csharp
// Metalama.Framework/Code/Types/IRecordFacet.cs
[CompileTime]
[InternalImplement]
public interface IRecordFacet : ITypeFacet
{
    /// <summary>
    /// Gets the <c>EqualityContract</c> property, or <c>null</c> when the type is a record struct, which has none.
    /// </summary>
    IProperty? EqualityContractProperty { get; }

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

`IRecordFacet` is one interface with nullable members, and it is not split into a record class interface and a
record struct interface. Three of its members are `null` for a record struct, and that is accepted.

```csharp
// Metalama.Framework/Code/Types/IUnionFacet.cs
[CompileTime]
[InternalImplement]
public interface IUnionFacet : ITypeFacet
{
    /// <summary>
    /// Gets the kind of the union.
    /// </summary>
    UnionKind UnionKind { get; }

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
public enum UnionKind
{
    /// <summary>
    /// The kind of the union is not known. This is the case for a union declared in a referenced assembly, because
    /// the compiled form does not record the authoring form, so the two kinds below cannot be told apart there.
    /// </summary>
    None = 0,

    /// <summary>The type is declared with the <c>union</c> keyword.</summary>
    Declaration,

    /// <summary>The type is a class or a struct that carries <c>UnionAttribute</c>.</summary>
    Attribute
}

[CompileTime]
[InternalImplement]
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
}
```

`IUnionCase` follows `ITupleElement`, which is richer than the type of the element and carries `Index`,
`HasFriendlyName` and `CorrespondingTupleField`. The richer abstraction is required rather than convenient: Roslyn
collapses duplicate case types through a set, so the index of a case is not recoverable from its type, and the
creation member of the attribute form may be a static method rather than a constructor.

### 2.3. Facets expose invokers

The members that a facet exposes are typed as `IMethod`, `IProperty`, `IConstructor` and `IField`. Those interfaces
derive from the invoker interfaces: `IMethod` derives from `IMethodInvoker`, `IConstructor` from
`IConstructorInvoker`, and `IFieldOrProperty` from `IFieldOrPropertyInvoker`. A consumer therefore reaches the
invoker through the member, and no facet declares an invoker member of its own.

```csharp
// Generates a call to the Invoke method of the delegate.
var call = eventType.Facets.Delegate!.InvokeMethod.With( handler ).Invoke( args );
```

`IUnionCase.CreationMember` is typed as `IMethodBase`, which derives from no invoker, because the creation member is
a constructor for one form of union and a static method for another. No facet declares a method to create an
expression for it.

The member that this calls for is `IMethodBaseInvoker`, declaring the operations that `IMethodInvoker` and
`IConstructorInvoker` have in common, with `IMethodBase` deriving from it. Creating an instance of a union case
would then be the invocation of its creation member. That is a story of its own, issue
[#1999](https://github.com/metalama/Metalama/issues/1999), because it changes `IMethodBase` and because the
relationship between the three invoker interfaces has to be settled: `IMethod` and `IConstructor` already derive
from the two specific invokers. It is not part of this proposal, and until it exists a consumer that invokes a
creation member tests whether it is an `IMethod` or an `IConstructor`.

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

> The builder interfaces are now designed in [`introducing-types.md`](introducing-types.md) and in the five
> documents it indexes, which supersede this section on two points. First, `IEnumBuilder` and `IDelegateBuilder`
> derive from `IMemberOrNamedTypeBuilder` rather than from `INamedTypeBuilder`, for the reasons that section 2 of
> that document gives; `IRecordBuilder` and `IUnionBuilder` derive from `INamedTypeBuilder` as drafted here.
> Second, `IUnionBuilder.AddCase` returns nothing, and no interface describes a case under construction. A case is
> a bare type in the grammar of the language, so there is nothing for a builder to carry, and `IUnionCase` cannot
> be returned either, because a case that is being built has no creation member yet, which is the lifetime argument
> that the next paragraph of this section makes.

The reason for keeping the writing surface off the facet is that the reader and the writer have different
lifetimes. A facet describes a type that exists. A builder describes a type that is being constructed and whose
members are not yet resolvable.

## 3. What this replaces, and what it leaves in place

| Existing member | Disposition |
| --- | --- |
| `INamedType.IsRecord` | Kept, and joined by `IsDelegate`, `IsEnum`, `IsTuple` and `IsUnion`. See section 4.4. |
| `INamedType.UnderlyingType` | Kept. It is shipped and it also serves nullable reference types. `IEnumFacet.UnderlyingType` is the member with one meaning, and the documentation of both says so. |
| `IEvent.Signature` | Kept, and defined as `Type.Facets.Delegate!.InvokeMethod`. The four duplicate implementations are removed. The member currently has no documentation and gains it. |
| `IMethodBase` | Unchanged. `IMethodBaseInvoker` is a story of its own. See section 2.3. |
| `ITupleType`, `ITupleElement` | Moved from `Metalama.Framework.Code` to `Metalama.Framework.Code.Types`. Otherwise unchanged. A tuple has no facet today. See section 4.5. |
| `TypeKind.Tuple`, `TypeFactory.CreateTupleType` | Unchanged. |
| `INamedType.PrimaryConstructor` | Kept. A primary constructor is not specific to records since C# 12, so it does not move to `IRecordFacet`. |
| `IExtensionBlock` | Unchanged, and deliberately not a facet. See section 4.1. |

No member is made obsolete by this proposal. It carries one user-facing breaking change, the namespace of
`ITupleType` and `ITupleElement`, so the pull request that carries it takes the `breaking` label. The change ships
in 2027.0 as a plain namespace change, with no compatibility measure.

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

### 4.4. The flags stay on `INamedType`, the structure does not

`INamedType` keeps `IsRecord` and gains `IsDelegate`, `IsEnum`, `IsTuple` and `IsUnion`. Two reasons.

The first is ease of use. A flag reads well in a predicate over a collection of types, in an eligibility rule and in
a template, where `type.Facets.Union is not null` does not.

The second is cost. Testing a flag through the collection allocates the collection and the facet in order to answer
a Boolean question, and generic code that walks the model asks that question on every type. A flag answers it
without allocating.

The flags are the discriminators and the facets are the structure, so they answer different questions rather than
the same one twice. That is the same relationship that `TypeKind.Delegate` and `TypeKind.Enum` already have with
their facets.

What does not go on `INamedType` is the structure. Pull request
[#1991](https://github.com/metalama/Metalama/pull/1991) proposes `IsUnion`, `IsUnionDeclaration` and
`UnionCaseTypes`. Under this proposal `IsUnion` ships as a flag, and the other two are replaced by `Facets.Union`,
which issue [#1941](https://github.com/metalama/Metalama/issues/1941) delivers. The recommendation is to hold those
two back from #1991 rather than ship them and duplicate them later, because a shipped public member cannot be
withdrawn.

Both would be wrong as specified there in any case. `UnionCaseTypes` is documented as empty when
`IsUnionDeclaration` is false, which describes the current implementation from syntax rather than the contract:
Roslyn documents `ITypeSymbol.UnionCaseTypes` as returning the case types "when `IsUnion` is true", and it is
defined for the attribute form. `IsUnionDeclaration` reads as a question about a declaration, on an object that is
already an `IDeclaration`. `UnionKind` states the distinction with the vocabulary of the language proposal, which
defines "union type" and "union declaration" as two terms, and it does so without that collision.

### 4.5. A tuple has no facet today

A tuple is not declared. The type `(int X, string Y)` is formed by a type expression at the place where it is used,
and the element names are attached there. Metalama models it that way: `TupleType` and `TupleTypeImpl` are in
`CodeModel/Source/ConstructedTypes/`, beside the array, the pointer, the function pointer and the dynamic type, and
the elements of a tuple are a view over the fields of the underlying `ValueTuple` with the names overridden.

`ITupleType` therefore belongs to the family of `IArrayType`, `IPointerType` and `IFunctionPointerType`: interfaces
that describe the structure of a type that a type expression forms, that are reached by a type test, and that are
constructed at one site. Section 1.3 states why that mechanism is correct for that family and does not extend to a
declared type. `ITupleType` and `ITupleElement` move to `Metalama.Framework.Code.Types` so that they sit with that
family, which is issue [#1998](https://github.com/metalama/Metalama/issues/1998).

`ITypeFacetCollection` has no tuple property in this proposal, and `INamedType.IsTuple` is the flag. An
`ITupleFacet` declaring the members of `ITupleType` may be added later for completeness, which would make the
collection a complete index of the structure of a type. That addition is not required by any consumer known today,
and it is deliberately left out of the pull requests that section 6 lists.

## 5. Implementation guidelines

These are constraints on the implementation, not a design of it.

1. `Facets` is one member on `INamedType`, for any number of facets. The structure does not add a member per kind.
   The flags of section 4.4 do add one member per kind, and they are Boolean and non-allocating.
2. The collection constructs the facets. No facet type declares a static factory method that returns `null`. A facet
   reference that a consumer holds is never `null`, and nullability is expressed only by the properties of the
   collection.
3. Every member of a facet is a lazy expression cached with `[Memo]`, and the facet resolves no member that a
   consumer does not read.
4. A type that has no facet returns a shared empty collection. Generic code that walks the model reads `Facets` on
   every type, so the common case must not allocate.
5. The implementations of `INamedType` that back a builder return the empty collection rather than throwing.
   Eligibility rules and advice validation run against builders, so an exception there is reached in normal use.

   > This guideline is superseded by section 5.1 of [`introducing-types.md`](introducing-types.md), which makes a
   > builder throw a `NotSupportedException` instead: a builder describes a type whose members are not resolvable,
   > so an empty structure is a false answer rather than an incomplete one. The concern that this guideline records
   > is answered rather than dismissed. The flags of section 4.4 continue to answer on a builder without
   > allocating, so a caller that asks what kind a type is keeps working, and the three readers that take a type
   > from an aspect are guarded before the exception is introduced. That document names them.
6. The facet interfaces name no Roslyn type. `Metalama.Framework` is not built per Roslyn version, while
   `Metalama.Framework.Engine` is, and the union facet reads `ITypeSymbol.IsUnion` and `ITypeSymbol.UnionCaseTypes`,
   which exist only in the latest variant. The conditional compilation is therefore confined to the construction of
   the collection in the engine, under the condition that section 6 of
   [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md) decides.

## 6. Order of implementation

The delegate facet is implemented first, and deliberately. It requires no C# 15, no Roslyn variant and no preview
language version, and it has about fifteen call sites to convert, so the shape is exercised against shipped
behaviour before anything that cannot be revised is public.

### 6.1. Reading the facets

| Issue | Content | Blocked by |
| --- | --- | --- |
| [#1995](https://github.com/metalama/Metalama/issues/1995) | `TypeFacetKind`, `ITypeFacet`, `ITypeFacetCollection`, `INamedType.Facets`, `INamedType.IsDelegate`, `IDelegateFacet`. Conversion of the `Invoke` lookups and of `IEvent.Signature`. | nothing |
| [#1996](https://github.com/metalama/Metalama/issues/1996) | `IEnumFacet` and `INamedType.IsEnum`. | #1995 |
| [#1997](https://github.com/metalama/Metalama/issues/1997) | `IRecordFacet`, and the conversion of the synthesized-member lookups of the linker. | #1995 |
| [#1998](https://github.com/metalama/Metalama/issues/1998) | `INamedType.IsTuple`, and the move of `ITupleType` and `ITupleElement` to `Metalama.Framework.Code.Types`. Carries the `breaking` label. | nothing |
| [#1941](https://github.com/metalama/Metalama/issues/1941) | `IUnionFacet`, `IUnionCase`, `UnionKind`, `INamedType.IsUnion`. The union facet is delivered by the existing union code model story, not by an issue of its own. | #1995, and the move to the stable Roslyn |
| [#1999](https://github.com/metalama/Metalama/issues/1999) | An invoker on `IMethodBase`, described in section 2.3. | nothing |
| [#2000](https://github.com/metalama/Metalama/issues/2000) | Conceptual and reference documentation of the facets. | #1995, and completed after the other facets exist |

### 6.2. Introducing the types

The four issues below are the type introduction backlog, which predates this proposal. They are named here because
this document decides the shape of their builder interfaces, in section 2.4, and because each of them replaces
implementation guideline 5 for its own kind: the type that the builder produces has to report the facet of that
kind, and the builder itself throws rather than reporting an empty one. They are not C# 15 work, they are not gated on the move to the stable Roslyn, and their milestone is a
separate decision.

| Issue | Content | Blocked by |
| --- | --- | --- |
| [#869](https://github.com/metalama/Metalama/issues/869) | Introduce a struct. A struct has no facet, so this issue adds no builder interface. It carries the machinery that the other three need, which is emitting a type kind other than `class` at build time and at design time. | nothing |
| [#865](https://github.com/metalama/Metalama/issues/865) | Introduce a delegate, with `IDelegateBuilder`. Every member-introduction operation inherited from `INamedTypeBuilder` throws, because a delegate declaration has no members. | #1995, #869 |
| [#866](https://github.com/metalama/Metalama/issues/866) | Introduce an enum, with `IEnumBuilder`. | #1996, #869 |
| [#867](https://github.com/metalama/Metalama/issues/867) | Introduce a record, with `IRecordBuilder`. The largest of the four, because the synthesized members have to exist as builders. | #1997, #869 |

`IUnionBuilder` belongs to the union introduction stories S-29 and S-30 of
[`../2027.0/user-stories/README.md`](../2027.0/user-stories/README.md), which are filed as issues
[#1951](https://github.com/metalama/Metalama/issues/1951) and
[#1952](https://github.com/metalama/Metalama/issues/1952).

> This table is superseded by section 6 of [`introducing-types.md`](introducing-types.md), which adds the union to
> it and records the revised hierarchy of section 2.4. The facet blockers listed above are satisfied, because
> #1995, #1996, #1997 and #1941 are closed, so #869 alone blocks the enum, the delegate and the record. The union
> is blocked by #869 and also by #1945, which is open. Each of the five kinds is designed in a document of its own,
> and section 8 of that document indexes every related issue.

### 6.3. What the first issue measures

Issue #1995 changes one shipped behaviour: `EligibilityRuleFactory` currently throws `InvalidOperationException`
when the type of an event is not a well-formed delegate, and it returns `false` after the conversion.

It is also the acceptance test of the design. Fourteen of the fifteen call sites assert that the `Invoke` method
exists, while `Facets.Delegate` is `null` for a malformed type. If several converted sites end up asserting that the
facet is not `null`, the nullable collection property is the wrong shape for those consumers, and the design has to
be revised before the union facet is built on it.

## 7. Resolved questions

These were open in an earlier revision of this document and are settled. No question is open.

- `IDelegateFacet` does not expose `BeginInvoke` and `EndInvoke`. They are the asynchronous pattern that preceded
  `async`.
- `IRecordFacet` keeps its nullable members and is not split in two.
- The members of an enum stay `IReadOnlyList<IField>`, and no interface of their own is declared for them.
- `UnionKind` gains a `None` member, of value zero, for a union read from a referenced assembly, whose authoring
  form the compiled form does not record.
- The property names `Delegate`, `Enum` and `Record` on `ITypeFacetCollection` are not a concern.
- The two kind properties are named apart rather than shadowed: `ITypeFacet.FacetKind` returns `TypeFacetKind` and
  `IUnionFacet.UnionKind` returns `UnionKind`.
- `IMethodBaseInvoker` is a story of its own and is not part of this proposal.
- The namespace of `ITupleType` and `ITupleElement` changes in 2027.0 with no compatibility measure.

## 8. References

- [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md), sections 3, 4 and 6.
- [`../2027.0/03-code-model-unions-closed.md`](../2027.0/03-code-model-unions-closed.md), finding CM-1.
- [`../2027.0/analysis-reports/12-csharp15-api-drafts.md`](../2027.0/analysis-reports/12-csharp15-api-drafts.md),
  which drafts the flat design and a bundle named `IUnionInfo`, and recommends the flat design.
- [`../2027.0/analysis-reports/11-introducing-unions-design.md`](../2027.0/analysis-reports/11-introducing-unions-design.md),
  which records the derivation of the case set of the attribute form.
- The C# 15 unions proposal, `dotnet/csharplang`, `proposals/csharp-15.0/unions.md`.
