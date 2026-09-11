# Introducing a union

This document designs the introduction of a union of C# 15, which is issues
[#1951](https://github.com/metalama/Metalama/issues/1951) and
[#1952](https://github.com/metalama/Metalama/issues/1952). It is a design proposal. Nothing described here is
implemented.

The two issues are user stories S-29 and S-30 of the 2027.0 release, and the capability, the scope and the
acceptance criteria are stated there rather than here:

| Issue | Story | Content |
| --- | --- | --- |
| [#1951](https://github.com/metalama/Metalama/issues/1951) | [S-29](../2027.0/user-stories/S-29-introduce-union-and-case-attribute-form.md) | Introducing a union type, and introducing a case into a type carrying the union attribute. |
| [#1952](https://github.com/metalama/Metalama/issues/1952) | [S-30](../2027.0/user-stories/S-30-introduce-case-into-union-declaration.md) | Introducing a case into a type declared with the `union` keyword. Filed only if question Q1 of [`OPEN-QUESTIONS.md`](../2027.0/OPEN-QUESTIONS.md) chooses to ship both forms. |

Section 11 of [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md) rules that a story states no application
programming interface, so the shape is designed here and the two stories keep their authority over the scope. This
document does not restate them.

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
                u.AddCase( TypeFactory.GetType( typeof(int) ) );
                u.AddCase( TypeFactory.GetType( typeof(string) ) );
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
        u.AddCase( TypeFactory.GetType( typeof(int) ) );
        u.AddCase( TypeFactory.GetType( typeof(string) ) );
    } );
```

Adding a case to a union that already exists is the second half of the work, and it is an advice on that type
rather than a type introduction:

```csharp
builder.With( existingUnion ).IntroduceUnionCase( TypeFactory.GetType( typeof(decimal) ) );
```

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
    /// Gets the cases that have been added so far, in the order in which they were added.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Code.Types.IUnionFacet.Cases"/>
    IReadOnlyList<IUnionCaseBuilder> Cases { get; }

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
    /// <returns>An <see cref="IUnionCaseBuilder"/> that allows you to further build the new case.</returns>
    IUnionCaseBuilder AddCase( IType caseType );

    /// <summary>
    /// Adds a case to the union.
    /// </summary>
    /// <param name="caseType">The type of the case.</param>
    /// <returns>An <see cref="IUnionCaseBuilder"/> that allows you to further build the new case.</returns>
    IUnionCaseBuilder AddCase( Type caseType );
}
```

### 3.2. `IUnionCaseBuilder`

```csharp
// Metalama.Framework/Code/DeclarationBuilders/IUnionCaseBuilder.cs
namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of a case of a union that has been created by
/// <see cref="IUnionBuilder.AddCase(IType)"/> or by the advice that adds a case to an existing union.
/// </summary>
/// <remarks>
/// <para>
/// This interface is the counterpart of <see cref="Metalama.Framework.Code.Types.IUnionCase"/>, and it is not that
/// interface. A case that is being built has no creation member yet, because the member is synthesized when the
/// union is introduced, so an interface that declared
/// <see cref="Metalama.Framework.Code.Types.IUnionCase.CreationMember"/> could not answer it. The creation member
/// of an introduced union is read from the facet of the introduced type.
/// </para>
/// <para>
/// Like <see cref="Metalama.Framework.Code.Types.IUnionCase"/>, and unlike every other builder of this namespace,
/// this interface does not derive from <see cref="IDeclarationBuilder"/>. A case of a union is not a declaration:
/// it declares no type, and the declaration that carries it is its creation member. The interface therefore
/// carries no operation to add a custom attribute, because a case of a union takes none.
/// </para>
/// </remarks>
/// <seealso cref="IUnionBuilder.AddCase(IType)"/>
/// <seealso cref="Metalama.Framework.Code.Types.IUnionCase"/>
[CompileTime]
[InternalImplement]
public interface IUnionCaseBuilder
{
    /// <summary>
    /// Gets the type of the case.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Code.Types.IUnionCase.Type"/>
    IType Type { get; }

    /// <summary>
    /// Gets the zero-based index of the case, in the order in which the cases were added.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Code.Types.IUnionCase.Index"/>
    int Index { get; }
}
```

### 3.3. The advice methods

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

/// <summary>
/// Adds a case to a union that already exists.
/// </summary>
/// <remarks>
/// <para>
/// For a type carrying the union attribute, the case is a constructor, a generated partial part expresses it, and
/// the editor and the build agree about the result.
/// </para>
/// <para>
/// For a type declared with the <c>union</c> keyword, exactly one part of the type carries the case list, so a
/// generated partial part cannot add a case and the operation rewrites the part that the user wrote. It therefore
/// takes effect at build time only, and the editor does not show the added case. A design-time diagnostic reports
/// that divergence.
/// </para>
/// </remarks>
/// <param name="targetUnion">The union into which the case must be added.</param>
/// <param name="caseType">The type of the case.</param>
/// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the added case.</returns>
IIntroductionAdviceResult<IUnionCase> IntroduceUnionCase( INamedType targetUnion, IType caseType );
```

The matching extension methods on `AdviserExtensions` follow the pattern of the other four documents:
`IntroduceUnion` extends `IAdviser<INamespaceOrNamedType>` and `IntroduceUnionCase` extends `IAdviser<INamedType>`.

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

Member introduction through the adviser is valid on a union, and is restricted rather than refused. An instance
field, an automatic property and a field-like event introduced into a union declaration produce the compiler error
CS9373, so the eligibility rules refuse them and report a Metalama diagnostic instead. The same advices are
accepted on the attribute form. An explicit constructor of a union declaration must chain to a generated one, which
is a further rule.

## 5. What the facet of the introduced union reports

The introduced type reports an `IUnionFacet` rather than the empty collection, which replaces implementation
guideline 5 of [`type-facets.md`](type-facets.md) for this kind.

| `IUnionFacet` member | Source |
| --- | --- |
| `UnionKind` | `IUnionBuilder.UnionKind`. |
| `Cases` | One `IUnionCase` per `IUnionCaseBuilder`, in the order in which they were added. |
| `ValueProperty` | The `Value` property, materialized as a builder. |
| `FacetKind` | `TypeFacetKind.Union`. |
| `Type` | The introduced type. |

`IUnionCase.CreationMember` is the member that creates a value of the case, and it is materialized: a constructor
that the compiler synthesizes for a union declaration, and the single-parameter constructor of the attribute form.
This is the substance of the story, and section 6.2 states why it is not free.

`INamedType.IsUnion` reports `true`. `IType.TypeKind` reports `TypeKind.Struct` for a union declaration and the
kind of the carrying type for the attribute form, because a union is not a kind of its own in the code model, which
section 4.3 of [`type-facets.md`](type-facets.md) decides.

## 6. Decisions

### 6.1. `AddCase` returns a builder and not an `IUnionCase`

Section 2.4 of [`type-facets.md`](type-facets.md) drafts `IUnionCase AddCase( IType caseType );`. This design
returns an `IUnionCaseBuilder` instead, and the reason is the one that the same section states two paragraphs
later: a facet describes a type that exists, and a builder describes a type that is being constructed and whose
members are not yet resolvable.

`IUnionCase` declares `CreationMember`. That member is synthesized when the union is introduced, so a case that is
still being built cannot answer it, and returning `IUnionCase` from `AddCase` would hand the author an object with
one property that throws. The draft did not weigh that, because the interface it returned was designed for the
reader.

The same reasoning produces `IEnumMemberBuilder` in [`introducing-enums.md`](introducing-enums.md), where the
member that cannot be answered during construction is the field.

### 6.2. The synthesized members are materialized, and that is the risk of the story

The introduction pipeline never re-reads the final model from Roslyn, so the `Value` property and the per-case
creation members have to exist as builders. Story S-29 identifies the precedent as the introduction of a namespace,
which registers a builder without injecting syntax, and not as the record materialization of
[#1343](https://github.com/metalama/Metalama/issues/1343), which does not generalise because a user may not declare
the synthesized union members at all and there is therefore no override to serve.

S-29 asks for that step to be prototyped first, because whether a member builder with no injected member survives
the linker injection registry was not verified. This document does not settle it, and it records that the answer
decides whether the step is one day or three.

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

## 7. Open questions

### 7.1. Does story S-30 ship at all?

Question Q1 of [`../2027.0/OPEN-QUESTIONS.md`](../2027.0/OPEN-QUESTIONS.md) chooses between shipping both
authoring forms and shipping the attribute form alone. `IntroduceUnionCase` on a union declaration works at build
time only, and the editor cannot show the added case, which needs a design-time diagnostic that reports the
divergence without repairing it.

The recommendation recorded in the story README is to ship both, taking the attribute form first. If only one form
fits the release it is the attribute form, and [#1952](https://github.com/metalama/Metalama/issues/1952) is then
not implemented. The interface above is unchanged either way: the method exists and reports that the operation is
not supported on a union declaration.

### 7.2. Is `IntroduceUnionCase` the right name and shape?

The method adds a case to a type that already exists, so it is closer to `IntroduceParameter`, which changes the
signature of a constructor the user wrote, than to the introduction of a declaration. Whether it should be named
for the case or for the operation on the union is not decided, and neither is whether it should take a callback.

What would settle it is the aspect that S-29 uses as its acceptance test. The precedent that S-29 names for the
operation is the introduction of a parameter into a partial constructor, delivered for C# 14 in
metalama/Metalama#1143, and the name of that advice is `IntroduceParameter`.

### 7.3. What does `Cases` report while the union is being built?

`IUnionCaseBuilder.Index` is the position in the order of addition. The compiler reports the case types of a union
as a set, and `AddCase` refuses a duplicate, so the two orders agree for an introduced union. Whether they agree
for a union that gains a case through `IntroduceUnionCase`, where the existing cases come from the source and the
added one does not, is not verified here.

## 8. References

- [`introducing-types.md`](introducing-types.md), sections 2 and 5.
- [`introducing-records.md`](introducing-records.md), section 6.1, and
  [`introducing-enums.md`](introducing-enums.md), section 6.1, which take the same decisions for their kinds.
- [`type-facets.md`](type-facets.md), sections 2.4 and 4.3, the first of which this document revises.
- [`../2027.0/user-stories/S-29-introduce-union-and-case-attribute-form.md`](../2027.0/user-stories/S-29-introduce-union-and-case-attribute-form.md)
  and [`../2027.0/user-stories/S-30-introduce-case-into-union-declaration.md`](../2027.0/user-stories/S-30-introduce-case-into-union-declaration.md).
- [`../2027.0/analysis-reports/11-introducing-unions-design.md`](../2027.0/analysis-reports/11-introducing-unions-design.md),
  which records the derivation of the case set of the attribute form.
- [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md), sections 3, 4 and 11.
- `Metalama.Framework/Code/Types/IUnionFacet.cs` and `IUnionCase.cs`, the interfaces this design mirrors.
- Issues [#1951](https://github.com/metalama/Metalama/issues/1951) and
  [#1952](https://github.com/metalama/Metalama/issues/1952), and their blockers
  [#1941](https://github.com/metalama/Metalama/issues/1941) and
  [#1945](https://github.com/metalama/Metalama/issues/1945).

— Claude for @gfraiteur
