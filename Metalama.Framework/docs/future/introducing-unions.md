# Introducing a union

This document designs the introduction of a union of C# 15, which is issue
[#1951](https://github.com/metalama/Metalama/issues/1951). It is a design proposal. Nothing described here is
implemented.

The issue is user story S-29 of the 2027.0 release, and the capability, the scope and the acceptance criteria are
stated there rather than here. Section 11 of [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md) rules that a story
states no application programming interface, so the shape is designed here and the story keeps its authority over
the scope. This document does not restate it.

The scope is narrower than that story. S-29 carries two halves, which are introducing a whole union and adding a
case to a union that already exists, and this design delivers the first half only. Section 6.5 states why, and what
an aspect author does instead. Issue [#1952](https://github.com/metalama/Metalama/issues/1952), user story S-30,
which carried the second half for a type declared with the `union` keyword, is not implemented, and question Q1 of
[`../2027.0/OPEN-QUESTIONS.md`](../2027.0/OPEN-QUESTIONS.md) is answered by that decision.

The cross-cutting decisions are in [`introducing-types.md`](introducing-types.md). A union declares members that an
aspect can introduce, within the limits that section 4 states, so `IUnionBuilder` derives from `INamedTypeBuilder`
and section 2 of that document does not revise it.

## 1. What the aspect author writes

A union declaration names its cases in its header:

```csharp
public class GenerateResultAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceUnion(
            "Result",
            UnionKind.Declaration,
            buildUnion: u =>
            {
                u.Accessibility = Accessibility.Public;
                u.AddCase( typeof(int) );
                u.AddCase( typeof(string) );
                u.AddCase( builder.Target );
            } );
    }
}
```

The attribute form is a class or a struct carrying `UnionAttribute`, and it accepts members that a union
declaration refuses:

```csharp
builder.IntroduceUnion(
    "Result",
    UnionKind.Attribute,
    buildUnion: u =>
    {
        u.Accessibility = Accessibility.Public;
        u.AddCase( typeof(int) );
        u.AddCase( typeof(string) );
    } );
```

There is no advice that adds a case to a union that already exists. Section 6.5 states why, and gives what an
aspect author writes instead for a type carrying the union attribute.

## 2. What Metalama produces

```csharp
public union Result( int, string, TargetType );
```

```csharp
[Union]
public partial struct Result
{
    public Result( int value ) { /* ... */ }
    public Result( string value ) { /* ... */ }
    public object Value { get; }
}
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
/// The authoring form is chosen when the advice is called and is reported by <see cref="UnionKind"/>. The language
/// applies the restrictions on the members of a union to a union declaration only, so the members that this
/// interface refuses depend on that value. They are listed in the design document.
/// </para>
/// <para>
/// A union requires at least one case, so an advice whose callback adds none reports an error.
/// </para>
/// <para>
/// The members that the compiler synthesizes for a union declaration, which are one constructor per case and the
/// <c>Value</c> property, are not set through this interface. They are created by the advice and are read through
/// <see cref="Metalama.Framework.Code.INamedType.Facets"/> on the introduced type.
/// </para>
/// </remarks>
/// <seealso cref="Metalama.Framework.Code.Types.IUnionFacet"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IUnionBuilder : INamedTypeBuilder
{
    /// <summary>
    /// Gets the authoring form of the union, which was chosen when the advice was called.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Code.Types.IUnionFacet.UnionKind"/>
    UnionKind UnionKind { get; }

    /// <summary>
    /// Gets the case types that have been added so far, in the order in which they were added.
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
/// Introduces a new union to the target namespace or type.
/// </summary>
/// <param name="targetNamespaceOrType">The namespace or type into which the union must be introduced.</param>
/// <param name="name">The name of the introduced union.</param>
/// <param name="unionKind">Whether the union is declared with the <c>union</c> keyword or is a struct carrying the
///     union attribute. <see cref="UnionKind.None"/> is not accepted, because it reports a union whose authoring
///     form is not known and is not a form that can be written.</param>
/// <param name="whenExists">Determines the implementation strategy when a type of the same name is already declared
///     in the target namespace or type. The default strategy is to fail with a compile-time error.</param>
/// <param name="buildUnion">A callback that configures the introduced union. It must add at least one case,
///     because the language requires a union to have one.</param>
/// <returns>An <see cref="IIntroductionAdviceResult{T}"/> representing the result of the advice. The
/// <see cref="IIntroductionAdviceResult{T}.Declaration"/> property provides access to the introduced union.</returns>
IIntroductionAdviceResult<INamedType> IntroduceUnion(
    INamespaceOrNamedType targetNamespaceOrType,
    string name,
    UnionKind unionKind,
    OverrideStrategy whenExists = OverrideStrategy.Default,
    Action<IUnionBuilder>? buildUnion = null );
```

The matching extension method on `AdviserExtensions` follows the pattern of the other four documents:
`IntroduceUnion` extends `IAdviser<INamespaceOrNamedType>`.

There is no `IntroduceUnionCase`, and section 6.5 states why.

## 4. The inherited operations that are not valid

The language forbids an instance field, an automatic property and a field-like event in a union declaration, and
allows all three in the attribute form. `ITypeSymbol.IsUnion` is true for both forms, so every rule states which of
the two it tests. That condition is required by section 3 of [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md) and
it is the one that is most often misread.

| Member | Union declaration | Attribute form |
| --- | --- | --- |
| `Accessibility`, `Name`, `IsPartial` | Valid. | Valid. |
| `AddTypeParameter` | Valid. | Valid. |
| `BaseType` | The setter throws a `NotSupportedException`. A union declaration is a struct. | Valid when the carrying type is a class. |
| `IsAbstract`, `IsSealed`, `IsStatic` | The setter throws a `NotSupportedException`. | Valid when the carrying type is a class, and refused otherwise, as for any struct. |
| `IsReadOnly`, `IsRef` | The setter throws a `NotSupportedException`. | As for any struct or class. |
| `IsClosed` | The setter throws an `InvalidOperationException`, as it does for any type that is not a class. | Valid when the carrying type is a class. |
| `Facets` | The getter throws a `NotSupportedException`, as it does on every builder. | Same. |

Member introduction through the adviser is valid on a union, and is restricted rather than refused. An instance
field, an automatic property and a field-like event introduced into a union declaration produce the compiler error
CS9373, so the eligibility rules refuse them and report a Metalama diagnostic instead. The same advices are
accepted on the attribute form. An explicit constructor of a union declaration must chain to a generated one, which
is a further rule.

## 5. What the facet of the introduced union reports

A union builder throws a `NotSupportedException` from `Facets`, which section 5.1 of
[`introducing-types.md`](introducing-types.md) states for every builder. `IUnionBuilder` derives from `INamedType`
and therefore declares the member, so the exception is reached through the public interface here. The cases that
have been added so far are read from `IUnionBuilder.Cases`, which is the member that exists for that purpose, and
the kind of a builder is read from `IsUnion`, which does not throw.

The introduced type reports an `IUnionFacet`.

| `IUnionFacet` member | Source |
| --- | --- |
| `UnionKind` | `IUnionBuilder.UnionKind`. |
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
transformation that injected them as well would declare each of them twice. Section 5.2 of
[`introducing-types.md`](introducing-types.md) states the rule, and the table above therefore describes the code
model rather than the generated code.

`INamedType.IsUnion` reports `true`. `IType.TypeKind` reports `TypeKind.Struct` for a union declaration and the
kind of the carrying type for the attribute form, because a union is not a kind of its own in the code model, which
section 4.3 of [`type-facets.md`](type-facets.md) decides.

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
the union declaration that Metalama does emit. The operation is therefore a transformation that registers a builder
into the code model and injects no member, which is a shape that does not exist yet.

Story S-29 identifies the precedent as the introduction of a namespace, which registers a builder without injecting
syntax, and not as the record materialization of
[#1343](https://github.com/metalama/Metalama/issues/1343), which does not generalise because a user may not declare
the synthesized union members at all and there is therefore no override to serve. The analysis in
[`../2027.0/analysis-reports/11-introducing-unions-design.md`](../2027.0/analysis-reports/11-introducing-unions-design.md)
states why the record precedent cannot be copied literally:
`IntroduceNamedTypeAdvice.IntroduceImplicitConstructorIfNeeded` adds a transformation, and
`IntroduceDeclarationTransformation<T>` implements both the interface that registers a declaration and the one that
injects a member, so using it would emit the member as well.

S-29 asks for that step to be prototyped first, because whether a member builder with no injected member survives
the linker injection registry was not verified. This document does not settle it, and it records that the answer
decides whether the step is one day or three. Every other kind of this set needs the same shape, so the prototype
is worth running before the record work starts as well.

### 6.3. One interface serves both authoring forms

The two forms differ in what they refuse rather than in what they offer, which is the table of section 4, and the
reader has already taken this decision: `IUnionFacet` is one interface with a `UnionKind` property and not two
interfaces. The reasoning is that of section 6.1 of [`introducing-records.md`](introducing-records.md).

### 6.4. `UnionKind` is reused and `None` is refused

The enumeration exists and is shipped. `UnionKind.None` reports a union whose authoring form is not known, which is
the case of a union read from a referenced assembly, and it is not a form that can be written. The advice method
therefore accepts the two writable members and throws an `ArgumentOutOfRangeException` for `None`.

The parameter has no default value, unlike the `RecordKind` parameter of
[`introducing-records.md`](introducing-records.md), because the two forms differ in what the introduced type
accepts afterwards and neither is the obvious choice. S-29 delivers the attribute form first, and that is a
delivery order rather than a default.

### 6.5. Adding a case to a union that already exists is not supported

No advice adds a case to a union that the user wrote. An earlier revision of this design carried one, named
`IntroduceUnionCase`, and it is withdrawn. The reason is that the two authoring forms cannot both be served.

For a type declared with the `union` keyword the operation is not expressible at design time. Exactly one part of a
partial union carries the case list: a second part carrying one is CS8863, and a part carrying none while no other
part carries one is CS9370. A generated partial part therefore cannot add a case, so the operation would have to
rewrite the part that the user wrote. It would take effect at build time only, the editor would show the union
without the added case, and the code that the aspect generated against that case would not compile in the editor.
An advice about which the editor and the build disagree is worse than no advice.

For a type carrying the union attribute the operation is expressible. Roslyn derives the case set of that form from
the public single-parameter constructors, so adding a case is adding such a constructor, and a generated partial
part carries it. Metalama nevertheless declares no advice, because one method that works on one authoring form and
reports an error on the other reads as a defect rather than as a design, and because the operation it would perform
is one an aspect can already perform.

That is the workaround, and it is not restricted. An aspect adds a case to a type carrying the union attribute by
introducing the constructor that defines it:

```csharp
[Template]
public void UnionCaseConstructor( decimal value ) { }

// in BuildAspect:
builder.With( existingUnion )
    .IntroduceConstructor(
        nameof(this.UnionCaseConstructor),
        buildConstructor: c => c.Accessibility = Accessibility.Public );
```

The compiler reads the new case from that constructor, the generated partial part carries it, and the editor and
the build agree. This follows the rule of section 3.2 of [`introducing-types.md`](introducing-types.md): the
framework does not prevent an aspect author from generating code, and it declares no advice of its own only
because the advice would be uneven across the two forms.

There is no workaround for a type declared with the `union` keyword. An aspect cannot add a case to it, and the
case list has to be written in source. An aspect that needs a case set it controls introduces the whole union, which
is what this document designs.

This decision answers question Q1 of [`../2027.0/OPEN-QUESTIONS.md`](../2027.0/OPEN-QUESTIONS.md), which chose
between shipping both authoring forms of case addition and shipping the attribute form alone. Neither ships. It
should be revisited if the language ever lets a part of a partial union contribute cases, which would remove the
reason.

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
  which section 6.5 withdraws.
- [`../2027.0/analysis-reports/11-introducing-unions-design.md`](../2027.0/analysis-reports/11-introducing-unions-design.md),
  which records the derivation of the case set of the attribute form.
- [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md), sections 3, 4 and 11.
- `Metalama.Framework/Code/Types/IUnionFacet.cs` and `IUnionCase.cs`, the interfaces this design mirrors.
- Issue [#1951](https://github.com/metalama/Metalama/issues/1951), which this document designs, and its blockers
  [#1941](https://github.com/metalama/Metalama/issues/1941) and
  [#1945](https://github.com/metalama/Metalama/issues/1945).
- Issue [#1952](https://github.com/metalama/Metalama/issues/1952), which section 6.5 withdraws. It is closed
  without being implemented, and the reason is recorded there.

— Claude for @gfraiteur
