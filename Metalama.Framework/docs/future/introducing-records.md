# Introducing a record

This document designs the introduction of a record class and of a record struct, which is issue
[#867](https://github.com/metalama/Metalama/issues/867). It is a design proposal. Nothing described here is
implemented.

The cross-cutting decisions are in [`introducing-types.md`](introducing-types.md), which this document does not
repeat. A record declares members that an aspect can introduce, so `IRecordBuilder` derives from
`INamedTypeBuilder` and section 2 of that document does not revise it.

A record struct is designed here and not in [`introducing-structs.md`](introducing-structs.md), and it shares one
application programming interface with a record class. Section 6.1 records that decision.

This is the largest of the five designs, for the reason that section 5 of
[`introducing-types.md`](introducing-types.md) gives: the introduction pipeline never re-reads the final model from
Roslyn, so the six members that `IRecordFacet` names have to exist as builders. Section 5 below states which.

## 1. What the aspect author writes

```csharp
public class GenerateSnapshotAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceRecord(
            "Snapshot",
            buildRecord: r =>
            {
                r.Accessibility = Accessibility.Public;

                foreach ( var property in builder.Target.Properties )
                {
                    r.AddPositionalParameter( property.Name, property.Type );
                }
            } );
    }
}
```

A record struct differs by one argument, and by nothing else:

```csharp
builder.IntroduceRecord(
    "Point",
    RecordKind.Struct,
    buildRecord: r =>
    {
        r.Accessibility = Accessibility.Public;
        r.IsReadOnly = true;
        r.AddPositionalParameter( "X", typeof(double) );
        r.AddPositionalParameter( "Y", typeof(double) );
    } );
```

## 2. What Metalama produces

```csharp
public record Snapshot( string Name, int Count );
```

```csharp
public readonly record struct Point( double X, double Y );
```

## 3. The interfaces

### 3.1. `RecordKind`

```csharp
// Metalama.Framework/Code/RecordKind.cs
namespace Metalama.Framework.Code;

/// <summary>
/// Authoring forms of a record, given to
/// <see cref="Metalama.Framework.Advising.IAdviceFactory.IntroduceRecord"/>.
/// </summary>
/// <remarks>
/// <para>
/// This enumeration names what an author chooses when introducing a record. It is not reported by the code model:
/// a record read from source is a class or a struct according to <see cref="IType.TypeKind"/>, and
/// <see cref="INamedType.IsRecord"/> states that it is a record. The obsolete members
/// <c>TypeKind.RecordClass</c> and <c>TypeKind.RecordStruct</c> are not revived by this enumeration.
/// </para>
/// </remarks>
[CompileTime]
public enum RecordKind
{
    /// <summary>
    /// The record is a reference type, declared as <c>record</c> or <c>record class</c>. This is the default
    /// value.
    /// </summary>
    Class = 0,

    /// <summary>
    /// The record is a value type, declared as <c>record struct</c>.
    /// </summary>
    Struct
}
```

### 3.2. `IRecordBuilder`

```csharp
// Metalama.Framework/Code/DeclarationBuilders/IRecordBuilder.cs
namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of a record that has been created by an advice. One interface serves a
/// record class and a record struct.
/// </summary>
/// <remarks>
/// <para>
/// The authoring form is chosen when the advice is called and is reported by <see cref="RecordKind"/>. The members
/// of this interface that a record struct does not accept are documented individually.
/// </para>
/// <para>
/// The members that the compiler synthesizes for a record are not set through this interface. They are created by
/// the advice and are read through
/// <see cref="Metalama.Framework.Code.INamedType.Facets"/> on the introduced type, as they are for a record that
/// the user wrote.
/// </para>
/// </remarks>
/// <seealso cref="Metalama.Framework.Code.Types.IRecordFacet"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IRecordBuilder : INamedTypeBuilder
{
    /// <summary>
    /// Gets the authoring form of the record, which was chosen when the advice was called.
    /// </summary>
    RecordKind RecordKind { get; }

    /// <summary>
    /// Gets the positional parameters that have been added so far, in the order in which they were added.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Code.Types.IRecordFacet.PositionalProperties"/>
    IParameterBuilderList PositionalParameters { get; }

    /// <summary>
    /// Appends a positional parameter to the record, which declares a parameter of the primary constructor and a
    /// public init-only property of the same name and type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A record that has at least one positional parameter is a positional record, and the compiler synthesizes a
    /// <c>Deconstruct</c> method for it. A record that has none declares no parameter list, and
    /// <see cref="Metalama.Framework.Code.Types.IRecordFacet.DeconstructMethod"/> is then <c>null</c> on the
    /// introduced type.
    /// </para>
    /// <para>
    /// A positional parameter whose name is that of a member already declared by the record, or by one of its base
    /// records, declares no property, which is the rule of the language. The advice reports an error instead of
    /// generating a declaration that the compiler would reject.
    /// </para>
    /// </remarks>
    /// <param name="name">The name of the parameter, which is also the name of the property it declares.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="defaultValue">The default value of the parameter, or <c>null</c> when it has none.</param>
    /// <returns>An <see cref="IParameterBuilder"/> that allows you to further build the new parameter, in
    ///     particular to add custom attributes to it.</returns>
    IParameterBuilder AddPositionalParameter( string name, IType type, TypedConstant? defaultValue = default );

    /// <summary>
    /// Appends a positional parameter to the record.
    /// </summary>
    /// <param name="name">The name of the parameter, which is also the name of the property it declares.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="defaultValue">The default value of the parameter, or <c>null</c> when it has none.</param>
    /// <returns>An <see cref="IParameterBuilder"/> that allows you to further build the new parameter.</returns>
    IParameterBuilder AddPositionalParameter( string name, Type type, TypedConstant? defaultValue = null );
}
```

### 3.3. The advice method

```csharp
// Metalama.Framework/Advising/IAdviceFactory.cs
/// <summary>
/// Introduces a new record to the target namespace or type.
/// </summary>
/// <param name="targetNamespaceOrType">The namespace or type into which the record must be introduced.</param>
/// <param name="name">The name of the introduced record.</param>
/// <param name="recordKind">Whether the record is a record class or a record struct. The default is a record
///     class.</param>
/// <param name="whenExists">Determines the implementation strategy when a type of the same name is already declared
///     in the target namespace or type. The default strategy is to fail with a compile-time error.</param>
/// <param name="buildRecord">An optional callback that allows you to configure the introduced record, such as
///     adding positional parameters, members, base types, or custom attributes.</param>
/// <returns>An <see cref="IIntroductionAdviceResult{T}"/> representing the result of the advice. The
/// <see cref="IIntroductionAdviceResult{T}.Declaration"/> property provides access to the introduced record.
/// The <see cref="IIntroductionAdviceResult{T}"/> interface itself implements <see cref="IAdviser{T}"/> and can be
/// used to introduce members to the record.</returns>
IIntroductionAdviceResult<INamedType> IntroduceRecord(
    INamespaceOrNamedType targetNamespaceOrType,
    string name,
    RecordKind recordKind = RecordKind.Class,
    OverrideStrategy whenExists = OverrideStrategy.Default,
    Action<IRecordBuilder>? buildRecord = null );
```

```csharp
// Metalama.Framework/Aspects/AdviserExtensions.cs
/// <summary>
/// Introduces a new record into the current namespace (as a top-level type) or type (as a nested type).
/// Use the <see cref="IAdviser.With{TNewDeclaration}"/> or <see cref="WithNamespace"/> method to introduce the
/// record to a different type or namespace than the current one.
/// </summary>
/// <param name="adviser">An adviser for a named type or namespace.</param>
/// <param name="name">The record name.</param>
/// <param name="recordKind">Whether the record is a record class or a record struct. The default is a record
///     class.</param>
/// <param name="whenExists">Determines the implementation strategy when a type of the same name is already declared
///     in the target type or namespace. The default strategy is to fail with a compile-time error.</param>
/// <param name="buildRecord">An optional delegate that modifies the <see cref="IRecordBuilder"/> that represents
///     the introduced record.</param>
/// <returns>An <see cref="IIntroductionAdviceResult{T}"/> that exposes the outcome of the operation and the
/// introduced <see cref="INamedType"/>.</returns>
/// <seealso href="@introducing-types"/>
public static IIntroductionAdviceResult<INamedType> IntroduceRecord(
    this IAdviser<INamespaceOrNamedType> adviser,
    string name,
    RecordKind recordKind = RecordKind.Class,
    OverrideStrategy whenExists = OverrideStrategy.Default,
    Action<IRecordBuilder>? buildRecord = null )
    => ((IAdviserInternal) adviser).AdviceFactory.IntroduceRecord(
        adviser.Target,
        name,
        recordKind,
        whenExists,
        buildRecord );
```

## 4. The inherited operations that are not valid

`IRecordBuilder` derives from `INamedTypeBuilder`, so it inherits the whole type-building surface. Most of it is
valid, and what is not depends on the authoring form.

| Member | Record class | Record struct |
| --- | --- | --- |
| `Accessibility`, `Name`, `IsPartial` | Valid. | Valid. |
| `AddTypeParameter` | Valid. | Valid. |
| `BaseType` | Valid, and the base must itself be a record class. The setter throws an `InvalidOperationException` for a base that is not a record, which is the rule of the language. | The setter throws a `NotSupportedException`. A record struct derives from `System.ValueType` and the language allows no other base. |
| `IsAbstract` | Valid. | The setter throws a `NotSupportedException`. |
| `IsSealed` | Valid. | The setter throws a `NotSupportedException`. A record struct is implicitly sealed. |
| `IsStatic` | The setter throws a `NotSupportedException`. A record is never static. | Same. |
| `IsReadOnly` | The setter throws an `InvalidOperationException`, as it does for any class. | Valid, and produces a `readonly record struct`. |
| `IsRef` | The setter throws an `InvalidOperationException`, as it does for any class. | The setter throws a `NotSupportedException`. The language has no `ref record struct`. |
| `IsClosed` | See section 7.1, which records this as open. | The setter throws an `InvalidOperationException`, as it does for any type that is not a class. |

`IsReadOnly` and `IsRef` are the two properties that [`introducing-structs.md`](introducing-structs.md) adds to
`INamedTypeBuilder`, so this issue depends on that one for them as well as for the emission machinery.

Member introduction is valid on a record, unlike on an enum and on a delegate. The rule of section 3 of
[`introducing-types.md`](introducing-types.md) does not apply here, and an aspect may add methods, properties and
constructors to an introduced record through the adviser in the ordinary way.

## 5. The synthesized members, and what the facet reports

The introduced type reports an `IRecordFacet` rather than the empty collection, which replaces implementation
guideline 5 of [`type-facets.md`](type-facets.md) for this kind. Every member the facet names has to be
materialized as a builder, because the facet of an introduced type is built from the builder data. This is the
substance of the issue, and it is the reason the issue is sized larger than the other three.

| `IRecordFacet` member | Record class | Record struct |
| --- | --- | --- |
| `EqualityContractProperty` | A protected virtual property named `EqualityContract`, materialized. | `null`. A record struct has none. |
| `PrintMembersMethod` | A method named `PrintMembers`, materialized. | Materialized. Its accessibility differs: private on a record struct, protected on a record class that is sealed, protected virtual otherwise. |
| `CloneMethod` | A method whose metadata name is `<Clone>$`, materialized. | `null`. A record struct has none. |
| `CopyConstructor` | A constructor taking the record type, materialized. | `null`. A record struct has none. |
| `DeconstructMethod` | Materialized when the record has at least one positional parameter, and `null` otherwise. | Same. |
| `PositionalProperties` | One public init-only property per positional parameter, in order. | Same. |
| `FacetKind` | `TypeFacetKind.Record`. | Same. |
| `Type` | The introduced type. | Same. |

Three members that the compiler also synthesizes are not part of the facet and are materialized nevertheless,
because `INamedType.Methods` has to report them: `Equals`, `GetHashCode` and `ToString`, together with the `==` and
`!=` operators. `IRecordFacet` documents them as reached through the ordinary member collections.

The primary constructor is materialized and is reported by `INamedType.PrimaryConstructor`, which is where
`IRecordFacet` says it is reached, because a primary constructor is not specific to a record since C# 12. This
answers the comment `// TODO: Primary constructor handling.` on `INamedTypeBuilder`, for the record at least; that
comment concerns a primary constructor on any type, and the rest of it is out of scope here.

The name of the clone method is not a C# identifier, so it is absent from `INamedType.Methods` and is reached
through `IRecordFacet.CloneMethod` alone. The builder that materializes it is registered so that the facet can name
it and so that the linker can emit it, and not so that a member collection can list it.

The engine already carries the record flag from the builder to the introduced type: `NamedTypeBuilder` takes an
`isRecord` constructor parameter, `NamedTypeBuilderData` stores it and `IntroducedNamedType` reports it. What
blocks the flag today is the assertion `Invariant.Assert( !isRecord )` beside it, and the missing arm in the
transformation. This design therefore says what the existing flag becomes and adds no second flag.

## 6. Decisions

### 6.1. A record struct uses the same application programming interface as a record class

One builder interface and one advice method serve both forms, and the form is an argument.

The reason is symmetry with the reader, which has already taken this decision. `IRecordFacet` is one interface
whose `EqualityContractProperty`, `CloneMethod` and `CopyConstructor` are `null` for a record struct, and section
2.2 of [`type-facets.md`](type-facets.md) records that it is deliberately not split in two. Splitting the writer
while the reader is unsplit would describe the same type by one structure when reading it and by two when writing
it.

The second reason is that the two forms differ in what they refuse rather than in what they offer. The table of
section 4 has one row per inherited member, and the rows that differ are those in which a record struct refuses
something a record class accepts. An interface per form would have the same rows, expressed as absent members
instead of throwing ones, at the cost of a second interface, a second advice method, and a decision for every
future member about whether it belongs to one form or to both.

This differs from the decision of section 2 of [`introducing-types.md`](introducing-types.md), which splits the
hierarchy for the enum and the delegate, and the difference is one of proportion. An enum shares almost nothing
with a class. A record struct shares almost everything with a record class.

### 6.2. The authoring form is an argument and not a property of the builder

`RecordKind` is read-only on `IRecordBuilder` and is chosen when the advice is called. The alternative is a
settable property that the callback assigns.

The form decides which inherited members are valid, which the table of section 4 shows, so a settable property
would mean that the validity of `BaseType` depends on the order in which the callback assigns two properties.
Making it an argument means every validation in the callback has a definite answer.

The engine takes the same shape already: `NamedTypeBuilder` receives its `typeKind` and its `isRecord` as
constructor parameters and asserts them there.

### 6.3. `RecordKind` is a new enumeration and not `TypeKind`

`TypeKind` has `RecordClass` and `RecordStruct` members, and both are marked obsolete as errors. The code model
represents a record as `TypeKind.Class` or `TypeKind.Struct` with `INamedType.IsRecord` set, and this design does
not revive the obsolete members.

Passing `TypeKind` itself, restricted to `Class` and `Struct`, was considered. It reuses a shipped type, and it
admits thirteen values that the method has to reject at run time. A two-member enumeration rejects them at compile
time, and its documentation states its relation to `TypeKind` so that a reader does not take it for a second
representation of a record in the code model.

### 6.4. The synthesized members are materialized rather than discovered

The pipeline never re-reads the final model from Roslyn, so a member that an aspect must see has to exist as a
builder. The precedent is `IntroduceNamedTypeAdvice.IntroduceImplicitConstructorIfNeeded`, which materializes the
implicit constructor of an introduced class for the same reason.

The alternative is to let the facet of an introduced record report `null` for the synthesized members, and to
document that an introduced record is less complete than one the user wrote. That is rejected because the facet is
the interface through which an aspect reads a record, and a facet that answers differently according to the origin
of the type is a defect rather than a limitation.

## 7. Open questions

### 7.1. May an introduced record class be closed?

`INamedTypeBuilder.IsClosed` is documented as valid on a class, and as throwing on a sealed class and on a static
class. A record class is a class, so the property appears to apply, and the language rule for a `closed record` was
not read against the C# 15 specification while this document was written.

What would settle it is the section of the C# 15 unions and closed hierarchies proposal that lists the declarations
the `closed` modifier accepts. The row of section 4 is left open rather than guessed, because a wrong answer there
either forbids something the language allows or emits a modifier the compiler rejects.

### 7.2. How does an aspect override a synthesized member of an introduced record?

Pull request metalama/Metalama#1879 makes `meta.Proceed()` work in an aspect that overrides a
compiler-synthesized member of a record, and every gate of that mechanism is keyed on whether the type is a record.
That work targets a record the user wrote.

Whether the same mechanism serves a record that an aspect introduced is not decided here. The two differ in where
the body of the synthesized member comes from: for a record read from source it is reproduced from the symbol, and
for an introduced record there is no symbol. What would settle it is a test that overrides `PrintMembers` on an
introduced record, which is worth writing early because the answer may add work to this issue.

### 7.3. Does a positional parameter accept an attribute target?

The language allows an attribute on a positional parameter of a record to be directed at the parameter, at the
property or at the field, through `[property: ...]` and `[field: ...]`. `AddPositionalParameter` returns an
`IParameterBuilder`, whose `AddAttribute` targets the parameter.

Reaching the property instead is possible through the introduced type once the advice completes, so nothing is
unreachable. Whether the shorter path is worth a member on the builder is not decided, and what would settle it is
an aspect that needs it.

## 8. References

- [`introducing-types.md`](introducing-types.md), sections 2, 4 and 5.
- [`introducing-structs.md`](introducing-structs.md), section 3.2, which adds `IsReadOnly` and `IsRef` to
  `INamedTypeBuilder`.
- [`type-facets.md`](type-facets.md), section 2.2 and implementation guideline 5.
- `Metalama.Framework/Code/Types/IRecordFacet.cs`, the interface this design mirrors.

Issues:

- [#867](https://github.com/metalama/Metalama/issues/867), which this document designs.
- [#1997](https://github.com/metalama/Metalama/issues/1997), code model: the record facet, closed in
  2027.0.2-preview. It delivered `IRecordFacet`, which section 5 mirrors, and it converted the
  synthesized-member lookups of the linker.
- [#1343](https://github.com/metalama/Metalama/issues/1343), support `meta.Proceed()` for compiler-synthesized
  record members, closed in 2027.0.1-preview through pull request metalama/Metalama#1879. It materializes the
  synthesized members of a record read from source, and section 7.2 asks whether an introduced record can reuse the
  mechanism. Every gate of that mechanism is keyed on whether the type is a record, so this is the issue to read
  before starting.
- [#868](https://github.com/metalama/Metalama/issues/868), type introduction: introduce primary constructor, open.
  This issue materializes the primary constructor of a record, which is part of that one. The rest of it, which is
  a primary constructor on a class or a struct that is not a record, stays open, and the comment
  `// TODO: Primary constructor handling.` on `INamedTypeBuilder` belongs to it.
- [#869](https://github.com/metalama/Metalama/issues/869), type introduction: introduce struct, open. It carries
  the emission machinery and the two properties of its section 3.2, and it blocks this one.
- [#1950](https://github.com/metalama/Metalama/issues/1950), C# 15 closed classes: introducing, closed. Section 7.1
  asks whether a record class may be closed, and that issue is where `IsClosed` and its validation were written.
- Section 8.4 of [`introducing-types.md`](introducing-types.md) lists the rest of the type introduction backlog.

— Claude for @gfraiteur
