# Introducing a union

This document designs the introduction of a union of C# 15, which is issue
[#1951](https://github.com/metalama/Metalama/issues/1951). It is a design proposal. Nothing described here is
implemented.

The issue is user story S-29 of the 2027.0 release, and the capability, the scope and the acceptance criteria are
stated there rather than here. Section 11 of [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md) rules that a story
states no application programming interface, so the shape is designed here and the story keeps its authority over
the scope. This document does not restate it.

The scope is narrower than that story, in two ways, and the story has to be revised before it is implemented.

S-29 carries two halves, which are introducing a whole union and adding a case to a union that already exists, and
this design delivers the first half only. Section 6.4 states why, and what an aspect author does instead. Issue
[#1952](https://github.com/metalama/Metalama/issues/1952), user story S-30, which carried the second half for a
union declaration, is not implemented either. That was question Q1 of the release, the product owner answered it on
2026-09-11, and section 4 of [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md) records the answer.

S-29 also names the attribute form of a union, which is a class or a struct carrying
`System.Runtime.CompilerServices.UnionAttribute`. `IntroduceUnion` produces a union written with the `union`
keyword and no other form, and section 6.3 states why and what an aspect writes instead.

Both narrowings are decisions of this design and not of the story, so the body of
[#1951](https://github.com/metalama/Metalama/issues/1951) states a scope that this document does not deliver. The
issue is revised before the work starts.

The cross-cutting decisions are in [`introducing-types.md`](introducing-types.md). A union declares members that an
aspect can introduce, within the limits that section 4 states, so `IUnionBuilder` derives from `INamedTypeBuilder`
and section 2 of that document does not revise it.

## 1. What the aspect author writes

```csharp
public class GenerateResultAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceUnion(
            "Result",
            u =>
            {
                u.Accessibility = Accessibility.Public;
                u.AddCase( typeof(int) );
                u.AddCase( typeof(string) );
                u.AddCase( builder.Target );
            } );
    }
}
```

`IntroduceUnion` introduces a union written with the `union` keyword, and no other authoring form. Section 6.3
states why, and what an aspect writes instead when it wants the attribute form.

## 2. What Metalama produces

```csharp
public union Result( int, string, TargetType );
```

## 3. The interfaces

### 3.1. `IUnionBuilder`

```csharp
// Metalama.Framework/Code/DeclarationBuilders/IUnionBuilder.cs
namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of a union that has been created by an advice. One interface serves a union
/// declaration and the attribute form.
/// </summary>
/// <remarks>
/// <para>
/// The union is written with the <c>union</c> keyword. The language applies the restrictions on the members of a
/// union to that form, so an instance field, an automatic property and a field-like event may not be introduced
/// into it. The design document lists what this interface refuses.
/// </para>
/// <para>
/// A union requires at least one case, so an advice whose callback adds none reports an error.
/// </para>
/// <para>
/// The members that the compiler synthesizes for a union, which are one constructor per case and the
/// <c>Value</c> property, are not set through this interface. They are created by the advice and are read through
/// <see cref="Metalama.Framework.Code.INamedType.Facets"/> on the introduced type.
/// </para>
/// </remarks>
/// <seealso cref="Metalama.Framework.Code.Types.IUnionFacet"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IUnionBuilder : INamedTypeBuilder
{
    /// <summary>    /// Gets the case types that have been added so far, in the order in which they were added.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The list holds types and not cases. A case of a union is a type and nothing else, which section 6.1 of the
    /// design document establishes from the grammar of the language, so there is nothing else for an element of
    /// this list to carry. <see cref="Metalama.Framework.Code.Types.IUnionCase"/>, which the introduced type
    /// reports, carries the index and the creation member in addition, and neither exists while the union is being
    /// built.
    /// </para>
    /// </remarks>
    /// <seealso cref="Metalama.Framework.Code.Types.IUnionFacet.Cases"/>
    IReadOnlyList<IType> Cases { get; }

    /// <summary>
    /// Adds a case to the union.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The compiler reports the case types of a union as a set, so adding a case whose type is already a case of
    /// this union throws an <see cref="ArgumentException"/> rather than declaring a second case.
    /// </para>
    /// <para>
    /// The type of a case is an ordinary type declared elsewhere, and the union declares no type itself.
    /// </para>
    /// </remarks>
    /// <param name="caseType">The type of the case.</param>
    void AddCase( IType caseType );

    /// <summary>
    /// Adds a case to the union.
    /// </summary>
    /// <param name="caseType">The type of the case.</param>
    void AddCase( Type caseType );
}
```

### 3.2. The advice method

```csharp
// Metalama.Framework/Advising/IAdviceFactory.cs
/// <summary>
/// Introduces a new union, written with the <c>union</c> keyword, to the target namespace or type.
/// </summary>
/// <param name="targetNamespaceOrType">The namespace or type into which the union must be introduced.</param>
/// <param name="name">The name of the introduced union.</param>
/// <param name="buildUnion">A callback that configures the introduced union. It must add at least one case,
///     because the language requires a union to have one. The parameter is required and precedes
///     <paramref name="whenExists"/> for that reason: an advice that adds no case reports an error, so a call
///     that omitted the callback could never succeed.</param>
/// <param name="whenExists">Determines the implementation strategy when a type of the same name is already declared
///     in the target namespace or type. The default strategy is to fail with a compile-time error.</param>
/// <returns>An <see cref="IIntroductionAdviceResult{T}"/> representing the result of the advice. The
/// <see cref="IIntroductionAdviceResult{T}.Declaration"/> property provides access to the introduced union.</returns>
IIntroductionAdviceResult<INamedType> IntroduceUnion(
    INamespaceOrNamedType targetNamespaceOrType,
    string name,
    Action<IUnionBuilder> buildUnion,
    OverrideStrategy whenExists = OverrideStrategy.Default );
```

The matching extension method on `AdviserExtensions` follows the pattern of the other four documents:
`IntroduceUnion` extends `IAdviser<INamespaceOrNamedType>`.

`buildUnion` is the one callback of this set that is required, and it is the one parameter of this set that
precedes `whenExists`. Every other introduction method configures a type that is well formed when the callback
does nothing, so the callback is optional and sits last. A union with no case is not a declaration the language
accepts, and the callback is the only way to add one, so a call that omitted it could only report an error.

There is no `IntroduceUnionCase`, and section 6.4 states why.

## 4. The inherited operations that are not valid

A union declaration is a struct, and the language forbids an instance field, an automatic property and a field-like
event in it. Those restrictions belong to this form and not to the attribute form, which this advice does not
introduce, so every rule this issue writes tests the form it has rather than `ITypeSymbol.IsUnion`. Section 3 of
[`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md) requires that distinction, and it is the one most often misread.

| Member | State |
| --- | --- |
| `Accessibility`, `Name`, `IsPartial` | Valid. |
| `AddTypeParameter` | Valid. |
| `BaseType` | The setter throws a `NotSupportedException`. A union declaration is a struct, and there is no base list to emit. |
| `IsAbstract`, `IsSealed`, `IsStatic` | The setter throws a `NotSupportedException`. |
| `IsReadOnly`, `IsRef` | The setter throws a `NotSupportedException`. |
| `IsClosed` | The setter throws an `InvalidOperationException`, as it does for any type that is not a class. |
| `Facets` | The getter throws a `NotSupportedException`, as it does on every builder. |

Member introduction through the adviser is restricted rather than refused. An instance field, an automatic property
and a field-like event introduced into a union produce the compiler error CS9373, so the eligibility rules refuse
them and report a Metalama diagnostic instead. An explicit constructor must chain to a generated one, which is a
further rule. These are the exception that section 3.2 of [`introducing-types.md`](introducing-types.md) names:
the compiler reports those errors on generated code that the user cannot edit.

## 5. What the facet of the introduced union reports

A union builder throws a `NotSupportedException` from `Facets`, which section 5.1 of
[`introducing-types.md`](introducing-types.md) states for every builder. `IUnionBuilder` derives from `INamedType`
and therefore declares the member, so the exception is reached through the public interface here. The cases that
have been added so far are read from `IUnionBuilder.Cases`, which is the member that exists for that purpose, and
the kind of a builder is read from `IsUnion`, which does not throw.

The introduced type reports an `IUnionFacet`.

| `IUnionFacet` member | Source |
| --- | --- |
| `UnionKind` | `UnionKind.Declaration`, always, because this advice introduces no other form. |
| `Cases` | One `IUnionCase` per case type added to the builder, in the order in which they were added. Each one carries its index and its creation member, which the builder does not. |
| `ValueProperty` | The `Value` property, materialized as a builder. |
| `FacetKind` | `TypeFacetKind.Union`. |
| `Type` | The introduced type. |

`IUnionCase.CreationMember` is the member that creates a value of the case, and it is materialized: a constructor
that the compiler synthesizes for a union declaration, and the single-parameter constructor of the attribute form.
This is the substance of the story, and section 6.2 states why it is not free.

Materialized means present in the code model and not emitted as syntax. Metalama generates the union declaration,
which for the declaration form is the `union` keyword, the name and the case list, and the compiler synthesizes the
`Value` property and one constructor per case from it exactly as it does for a union the user wrote. A
transformation that injected them as well would declare each of them twice. Section 4.2 of
[`introducing-types.md`](introducing-types.md) states the rule and the mechanism, and the table above therefore
describes the code model rather than the generated code.

`INamedType.IsUnion` reports `true`, and `IType.TypeKind` reports `TypeKind.Struct`, because a union is not a kind
of its own in the code model, which section 4.3 of [`type-facets.md`](type-facets.md) decides.

The facet is tested by unit tests and not by aspect tests, for the reason that section 5.3 of
[`introducing-types.md`](introducing-types.md) gives. The synthesized members of a union are in the same position
as those of a record: the compiler creates them, an aspect test does not show them, and only a unit test can assert
that they exist in the code model. The tests belong beside `UnionTypeTests.cs`, which issue
[#1941](https://github.com/metalama/Metalama/issues/1941) added, and they cover both authoring forms, because the
two differ in what the creation member of a case is.

## 6. Decisions

### 6.1. A case is a type, so there is no case builder and `AddCase` returns nothing

Section 2.4 of [`type-facets.md`](type-facets.md) drafts `IUnionCase AddCase( IType caseType );`. This design
returns nothing, and declares no builder for a case. Two findings settle it.

The first is the grammar. The language defines the case list as bare types:

```antlr
case_types
    : type (',' type)*
    ;
```

A case carries no attribute, no modifier and no name, the proposal states no plan to allow any of the three, and
the analysis in
[`../2027.0/analysis-reports/11-introducing-unions-design.md`](../2027.0/analysis-reports/11-introducing-unions-design.md)
reaches the same conclusion from the other direction: Roslyn parses the case list as a parameter list whose
parameters carry a type and no identifier, so a case has no name, no default value, no reference kind and no
attribute list. A builder exists to carry what an author may choose about a declaration. An author chooses nothing
about a case except its type, which is the argument of `AddCase`.

The second is that `IUnionCase` cannot be returned either. It declares `CreationMember`, which is synthesized when
the union is introduced, so a case that is still being built cannot answer it. Returning it would hand the author
an object with a property that throws, which is the failure that the lifetime argument of section 2.4 of
[`type-facets.md`](type-facets.md) predicts two paragraphs after the draft that ignores it.

What remains is `Cases`, typed as an ordered list of `IType`, which is what the builder needs to store and what the
transformation needs to emit. The same analysis reaches that shape independently and contrasts it with
`TypeParameters`, whose elements are declarations the builder owns.

This decision should be revisited if the language gains attributes or modifiers on a case. At that point a case
becomes a declaration, a builder for it carries those, and `AddCase` returns one. Nothing else in this design
changes.

### 6.2. The synthesized members enter the code model without being emitted, and that is the risk of the story

The introduction pipeline never re-reads the final model from Roslyn, so the `Value` property and the per-case
creation members have to exist as builders. They must not be emitted, because the compiler synthesizes them from
the union declaration that Metalama does emit. Section 4.2 of [`introducing-types.md`](introducing-types.md) states
the mechanism, which is a transformation implementing `IIntroduceDeclarationTransformation` and not
`IInjectMemberTransformation`, and names `IntroduceNamespaceTransformation` as the precedent that story S-29 also
names.

What is specific to the union is the scale and the risk. This kind registers one constructor per case, so the
number of registered members depends on what the aspect declares, and the record materialization of
[#1343](https://github.com/metalama/Metalama/issues/1343) does not generalise to it, because a user may not declare
the synthesized union members at all and there is therefore no override to serve.

S-29 asks for that step to be prototyped first, because whether a member builder with no injected member survives
the linker injection registry was not verified. This document does not settle it, and it records that the answer
decides whether the step is one day or three. Every other kind of this set needs the same shape, so the prototype
belongs with the struct work, which section 4.2 identifies as the cheapest place to run it.

### 6.3. Only the form written with the `union` keyword is introduced

`IntroduceUnion` produces a union declaration. It does not produce the attribute form, which is a class or a struct
carrying `System.Runtime.CompilerServices.UnionAttribute`, and it takes no argument that would choose between the
two.

The reason is that the attribute form needs nothing from this advice. It is an ordinary class or struct, and an
aspect builds one with the advice that already exists:

```csharp
var union = builder.IntroduceClass(
    "Result",
    buildType: t =>
    {
        t.Accessibility = Accessibility.Public;
        t.AddAttribute( AttributeConstruction.Create( typeof(UnionAttribute) ) );
    } );

union.IntroduceConstructor( nameof(this.CaseConstructor), buildConstructor: c => c.Accessibility = Accessibility.Public );
union.IntroduceAutomaticProperty( "Value", typeof(object) );
```

An advice that served both forms would carry an argument that changes which of its own members are valid, a table
of restrictions with two columns, and a set of eligibility rules that each state which form they test. The second
form would buy an attribute and a naming convention.

One thing is lost, which is the "almost" in that trade. `INamedType.IsUnion` is derived from the type kind and the
builder data, and an introduced class that carries the union attribute is reported as an ordinary class:
`IsUnion` is `false` for it and `Facets.Union` is `null`, although the compiler treats it as a union. A type the
user wrote with the same attribute is reported correctly, because the code model reads its symbol. Deriving the
flag from the attribute of an introduced type would close that gap and is not required here, because the aspect
that wrote the attribute knows what it built.

### 6.4. Adding a case to a union that already exists is not supported

No advice adds a case to a union that the user wrote. An earlier revision of this design carried one, named
`IntroduceUnionCase`, and it is withdrawn. The reason is that the two authoring forms cannot both be served.

For a union declaration the operation is not expressible at design time. Exactly one part of a partial union carries
the case list: a second part carrying one is CS8863, and a part carrying none while no other part carries one is
CS9370. A generated partial part therefore cannot add a case, so the operation would have to rewrite the part that
the user wrote. It would take effect at build time only, the editor would show the union without the added case, and
the code that the aspect generated against that case would not compile in the editor. An advice about which the
editor and the build disagree is worse than no advice.

For a type carrying the union attribute the operation is expressible, and an aspect performs it with the advice that
already exists. Roslyn derives the case set of that form from the public single-parameter constructors, so adding a
case is adding such a constructor, and a generated partial part carries it:

```csharp
[Template]
public void UnionCaseConstructor( decimal value ) { }

// in BuildAspect:
builder.With( existingUnion )
    .IntroduceConstructor(
        nameof(this.UnionCaseConstructor),
        buildConstructor: c => c.Accessibility = Accessibility.Public );
```

The compiler reads the new case from that constructor, the generated partial part carries it, and the editor and the
build agree. This follows the rule of section 3.2 of [`introducing-types.md`](introducing-types.md): the framework
does not prevent an aspect author from generating invalid code, so it does not stand between the author and an
operation the language allows. It declares no advice of its own for a different reason, which is that the advice
would be uneven across the two authoring forms.

There is no workaround for a union declaration. An aspect cannot add a case to one, and the case list has to be
written in source. An aspect that needs a case set it controls introduces the whole union, which is what this
document designs.

This was question Q1 of the release, which chose between shipping both authoring forms of case addition and
shipping the attribute form alone. The product owner answered on 2026-09-11 that neither ships, so the question is
closed and has left [`../2027.0/OPEN-QUESTIONS.md`](../2027.0/OPEN-QUESTIONS.md); section 4 of
[`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md) records it. The decision should be revisited if the language
ever lets a part of a partial union contribute cases, which would remove the reason.

## 7. Open questions

### 7.1. Does the order of `Cases` survive the round trip?

`IUnionBuilder.Cases` is in the order of addition, and `IUnionFacet.Cases` on the introduced type is in the order
that the creation members declare, which `IUnionCase.Index` numbers. The compiler reports the case types of a union
as a set, and `AddCase` refuses a duplicate, so the two orders should agree for an introduced union.

What would settle it is a unit test that adds three cases and reads the index of each from the facet of the
introduced type. The order matters, because the index of a case is not recoverable from its type, which is the
reason `IUnionCase` carries `Index` at all.

## 8. References

- [`introducing-types.md`](introducing-types.md), sections 2 and 5.
- [`introducing-records.md`](introducing-records.md), section 6.1, and
  [`introducing-enums.md`](introducing-enums.md), section 6.1, which take the same decisions for their kinds.
- [`type-facets.md`](type-facets.md), sections 2.4 and 4.3, the first of which this document revises.
- [`../2027.0/user-stories/S-29-introduce-union-and-case-attribute-form.md`](../2027.0/user-stories/S-29-introduce-union-and-case-attribute-form.md),
  whose first half this document designs, and
  [`../2027.0/user-stories/S-30-introduce-case-into-union-declaration.md`](../2027.0/user-stories/S-30-introduce-case-into-union-declaration.md),
  which section 6.4 withdraws.
- [`../2027.0/analysis-reports/11-introducing-unions-design.md`](../2027.0/analysis-reports/11-introducing-unions-design.md),
  which records the derivation of the case set of the attribute form.
- [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md), sections 3, 4 and 11.
- `Metalama.Framework/Code/Types/IUnionFacet.cs` and `IUnionCase.cs`, the interfaces this design mirrors.
- Issue [#1951](https://github.com/metalama/Metalama/issues/1951), which this document designs, and its blockers
  [#1941](https://github.com/metalama/Metalama/issues/1941) and
  [#1945](https://github.com/metalama/Metalama/issues/1945).
- Issue [#1952](https://github.com/metalama/Metalama/issues/1952), which section 6.4 withdraws. It stays open
  until the product owner accepts the withdrawal, and the reason is recorded there.

— Claude for @gfraiteur
