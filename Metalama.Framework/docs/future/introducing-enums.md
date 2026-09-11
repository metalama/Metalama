# Introducing an enum

This document designs the introduction of an enum, which is issue
[#866](https://github.com/metalama/Metalama/issues/866). It is a design proposal. Nothing described here is
implemented.

The cross-cutting decisions are in [`introducing-types.md`](introducing-types.md), which this document does not
repeat. The one that shapes the interface below is section 2 of that document: an enum builder derives from
`IMemberOrNamedTypeBuilder` and not from `INamedTypeBuilder`, because an enum declares no member that an aspect can
introduce.

The read side of an enum is `IEnumFacet`, which is delivered. The design below mirrors it member for member, and
section 5 states the mapping.

## 1. What the aspect author writes

```csharp
public class GenerateStateEnumAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceEnum(
            "State",
            buildEnum: e =>
            {
                e.Accessibility = Accessibility.Public;
                e.AddMember( "Idle" );
                e.AddMember( "Running" );
                e.AddMember( "Stopped", 10 );
            } );
    }
}
```

A flags enum sets one more property, and states every value, because the language computes no power of two on the
author's behalf and neither does Metalama:

```csharp
builder.IntroduceEnum(
    "Permissions",
    buildEnum: e =>
    {
        e.Accessibility = Accessibility.Public;
        e.UnderlyingType = TypeFactory.GetType( SpecialType.Byte );
        e.IsFlags = true;
        e.AddMember( "None", 0 );
        e.AddMember( "Read", 1 );
        e.AddMember( "Write", 2 );
        e.AddMember( "Execute", 4 );
    } );
```

## 2. What Metalama produces

```csharp
public enum State
{
    Idle,
    Running,
    Stopped = 10
}
```

```csharp
[Flags]
public enum Permissions : byte
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4
}
```

## 3. The interfaces

### 3.1. `IEnumBuilder`

```csharp
// Metalama.Framework/Code/DeclarationBuilders/IEnumBuilder.cs
namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of an enum that has been created by an advice.
/// </summary>
/// <remarks>
/// <para>
/// This interface derives from <see cref="IMemberOrNamedTypeBuilder"/> and not from
/// <see cref="INamedTypeBuilder"/>, because an enum declares no member that an aspect can introduce and has
/// neither a base type nor type parameters. The operations that an enum does not have are therefore absent rather
/// than present and failing. The operations that it inherits and does not have are listed below.
/// </para>
/// <para>
/// The following inherited properties are not valid for an enum, and their setter throws a
/// <see cref="NotSupportedException"/>: <see cref="IMemberOrNamedTypeBuilder.IsStatic"/>,
/// <see cref="IMemberOrNamedTypeBuilder.IsSealed"/>, <see cref="IMemberOrNamedTypeBuilder.IsAbstract"/> and
/// <see cref="IMemberOrNamedTypeBuilder.IsPartial"/>. An enum is implicitly sealed, it may not be abstract or
/// static, and the language has no partial enum.
/// </para>
/// <para>
/// An enum builder is not an <see cref="INamedType"/>, so it may not be used where an <see cref="IType"/> is
/// expected. The introduced enum is read from
/// <see cref="Metalama.Framework.Advising.IIntroductionAdviceResult{T}.Declaration"/>.
/// </para>
/// </remarks>
/// <seealso cref="IEnumMemberBuilder"/>
/// <seealso cref="Metalama.Framework.Code.Types.IEnumFacet"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IEnumBuilder : IMemberOrNamedTypeBuilder
{
    /// <summary>
    /// Gets or sets the underlying integral type of the enum. The default value is <c>int</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The language allows one of <c>byte</c>, <c>sbyte</c>, <c>short</c>, <c>ushort</c>, <c>int</c>,
    /// <c>uint</c>, <c>long</c> and <c>ulong</c>. The setter throws an <see cref="ArgumentOutOfRangeException"/>
    /// for any other type.
    /// </para>
    /// <para>
    /// Setting this property after a member has been added throws an <see cref="InvalidOperationException"/>,
    /// because the value of a member is validated against the underlying type when the member is added.
    /// </para>
    /// </remarks>
    /// <seealso cref="Metalama.Framework.Code.Types.IEnumFacet.UnderlyingType"/>
    INamedType UnderlyingType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the enum is annotated with <see cref="System.FlagsAttribute"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Setting this property to <c>true</c> adds the attribute, and setting it to <c>false</c> removes it. Adding
    /// the attribute through <see cref="IDeclarationBuilder.AddAttribute"/> has the same effect, and this property
    /// then reports <c>true</c>.
    /// </para>
    /// <para>
    /// The attribute changes how the value of the enum is formatted and parsed at run time, and it does not change
    /// the value of any member. An author who sets this property assigns the value of every member explicitly,
    /// because neither the language nor Metalama derives a power of two from the position of a member.
    /// </para>
    /// </remarks>
    /// <seealso cref="Metalama.Framework.Code.Types.IEnumFacet.IsFlags"/>
    bool IsFlags { get; set; }

    /// <summary>
    /// Gets the members that have been added so far, in the order in which they were added.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Code.Types.IEnumFacet.Members"/>
    IReadOnlyList<IEnumMemberBuilder> Members { get; }

    /// <summary>
    /// Adds a member to the enum.
    /// </summary>
    /// <param name="name">The name of the member.</param>
    /// <param name="value">The value of the member, or <c>null</c> to let the language assign it, which gives zero
    ///     to the first member and the value of the preceding member plus one to any other. A value that does not
    ///     fit in <see cref="UnderlyingType"/> throws an <see cref="ArgumentOutOfRangeException"/>. A value outside
    ///     the range of <see cref="long"/>, which only an enum whose underlying type is <c>ulong</c> can have, is
    ///     assigned through <see cref="IEnumMemberBuilder.Value"/> instead.</param>
    /// <returns>An <see cref="IEnumMemberBuilder"/> that allows you to further build the new member, in particular
    ///     to add custom attributes to it.</returns>
    IEnumMemberBuilder AddMember( string name, long? value = null );
}
```

### 3.2. `IEnumMemberBuilder`

```csharp
// Metalama.Framework/Code/DeclarationBuilders/IEnumMemberBuilder.cs
namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of a member of an enum that has been created by
/// <see cref="IEnumBuilder.AddMember"/>.
/// </summary>
/// <remarks>
/// <para>
/// A member of an enum is a constant field, and the code model reports it as an <see cref="IField"/> once the enum
/// is introduced. This interface is nevertheless not an <see cref="IFieldBuilder"/>: a member of an enum has no
/// type of its own, no accessibility of its own, no initializer and no modifier, so almost every operation of a
/// field builder would be invalid on it. Section 6 of the design document states the decision.
/// </para>
/// </remarks>
/// <seealso cref="IEnumBuilder.AddMember"/>
/// <seealso cref="IField"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IEnumMemberBuilder : IDeclarationBuilder
{
    /// <summary>
    /// Gets or sets the name of the member.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the value of the member, or <c>null</c> when the language assigns it, which gives zero to the
    /// first member and the value of the preceding member plus one to any other.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is converted to <see cref="IEnumBuilder.UnderlyingType"/>. A value that does not fit in that type,
    /// or whose type is not an integral type, throws an <see cref="ArgumentOutOfRangeException"/>.
    /// </para>
    /// <para>
    /// This property is the way to assign a value outside the range of <see cref="long"/>, which
    /// <see cref="IEnumBuilder.AddMember"/> cannot express and which an enum whose underlying type is <c>ulong</c>
    /// can have.
    /// </para>
    /// </remarks>
    TypedConstant? Value { get; set; }
}
```

### 3.3. The advice method

```csharp
// Metalama.Framework/Advising/IAdviceFactory.cs
/// <summary>
/// Introduces a new enum to the target namespace or type.
/// </summary>
/// <param name="targetNamespaceOrType">The namespace or type into which the enum must be introduced.</param>
/// <param name="name">The name of the introduced enum.</param>
/// <param name="whenExists">Determines the implementation strategy when a type of the same name is already declared
///     in the target namespace or type. The default strategy is to fail with a compile-time error.</param>
/// <param name="buildEnum">An optional callback that allows you to configure the introduced enum, in particular to
///     add its members.</param>
/// <returns>An <see cref="IIntroductionAdviceResult{T}"/> representing the result of the advice. The
/// <see cref="IIntroductionAdviceResult{T}.Declaration"/> property provides access to the introduced enum. Unlike
/// the result of introducing a class, this result must not be used to introduce members, because an enum accepts
/// none.</returns>
IIntroductionAdviceResult<INamedType> IntroduceEnum(
    INamespaceOrNamedType targetNamespaceOrType,
    string name,
    OverrideStrategy whenExists = OverrideStrategy.Default,
    Action<IEnumBuilder>? buildEnum = null );
```

```csharp
// Metalama.Framework/Aspects/AdviserExtensions.cs
/// <summary>
/// Introduces a new enum into the current namespace (as a top-level type) or type (as a nested type).
/// Use the <see cref="IAdviser.With{TNewDeclaration}"/> or <see cref="WithNamespace"/> method to introduce the enum
/// to a different type or namespace than the current one.
/// </summary>
/// <param name="adviser">An adviser for a named type or namespace.</param>
/// <param name="name">The enum name.</param>
/// <param name="whenExists">Determines the implementation strategy when a type of the same name is already declared
///     in the target type or namespace. The default strategy is to fail with a compile-time error.</param>
/// <param name="buildEnum">An optional delegate that modifies the <see cref="IEnumBuilder"/> that represents the
///     introduced enum. The members of the enum are added through this delegate.</param>
/// <returns>An <see cref="IIntroductionAdviceResult{T}"/> that exposes the outcome of the operation and the
/// introduced <see cref="INamedType"/>.</returns>
/// <seealso href="@introducing-types"/>
public static IIntroductionAdviceResult<INamedType> IntroduceEnum(
    this IAdviser<INamespaceOrNamedType> adviser,
    string name,
    OverrideStrategy whenExists = OverrideStrategy.Default,
    Action<IEnumBuilder>? buildEnum = null )
    => ((IAdviserInternal) adviser).AdviceFactory.IntroduceEnum(
        adviser.Target,
        name,
        whenExists,
        buildEnum );
```

## 4. The inherited operations that are not valid

`IEnumBuilder` inherits six settable properties from `IMemberOrNamedTypeBuilder`. Two are valid and four are not.

| Member | State |
| --- | --- |
| `Accessibility` | Valid. |
| `Name` | Valid. |
| `IsStatic` | The setter throws a `NotSupportedException`. An enum is neither static nor an instance type. |
| `IsSealed` | The setter throws a `NotSupportedException`. An enum is implicitly sealed. |
| `IsAbstract` | The setter throws a `NotSupportedException`. An enum may not be abstract. |
| `IsPartial` | The setter throws a `NotSupportedException`. The language has no partial enum. |

The operations of `IDeclarationBuilder`, which are `AddAttribute`, `AddAttributes`, `RemoveAttributes` and
`Freeze`, are all valid. An enum carries attributes, and `IsFlags` is a shortcut for one of them.

Every operation of `INamedTypeBuilder` is absent rather than failing, because `IEnumBuilder` does not derive from
that interface. That is `BaseType`, `AddTypeParameter`, `IsClosed`, and the whole of `INamedType`, which includes
`Facets`. A caller that reaches the engine object as an `INamedType` and reads `Facets` on it meets the
`NotSupportedException` of section 5.1 of [`introducing-types.md`](introducing-types.md).

The member introduction advices are refused by the rule of section 3 of
[`introducing-types.md`](introducing-types.md), because they reach the introduced enum through the adviser and not
through this interface.

## 5. What the facet of the introduced enum reports

An enum builder declares no `Facets` member, because `IEnumBuilder` does not derive from `INamedType`. The engine
class behind it does, and it throws a `NotSupportedException`, which section 5.1 of
[`introducing-types.md`](introducing-types.md) states for every builder. The kind of a builder is read from
`IsEnum`, which answers without allocating and does not throw.

The introduced type reports an `IEnumFacet`. Each member of the facet is built from the builder data, because the
introduction pipeline never re-reads the final model from Roslyn.

| `IEnumFacet` member | Source |
| --- | --- |
| `UnderlyingType` | `IEnumBuilder.UnderlyingType`, or `int` when the aspect set none. |
| `Members` | One `IField` per `IEnumMemberBuilder`, in the order in which they were added. The value of each field is the value that the builder assigned, or the value the language computes when the builder assigned none. |
| `IsFlags` | Whether the attribute is present, which is what `IsFlags` on the builder sets. |
| `FacetKind` | `TypeFacetKind.Enum`. |
| `Type` | The introduced type. |

`INamedType.IsEnum` reports `true`, and `INamedType.UnderlyingType` reports the same type as
`IEnumFacet.UnderlyingType`, which is what it reports for an enum read from source.

The synthetic field whose metadata name is `value__` is not materialized as a builder. `IEnumFacet.Members`
excludes it by contract, and no consumer of the code model reaches it, so materializing it would add a field to
`INamedType.Fields` that a source enum does not show there either.

The facet is tested by unit tests and not by aspect tests, for the reason that section 5.3 of
[`introducing-types.md`](introducing-types.md) gives: an aspect test compares generated code and cannot observe the
code model that the pipeline built. The tests belong beside `EnumFacetTests.cs`, which issue
[#1996](https://github.com/metalama/Metalama/issues/1996) added for an enum read from source. The assertion that
matters most is that an introduced enum and the equivalent enum read from source report the same underlying type,
the same members in the same order, the same values, and the same `IsFlags`.

## 6. Decisions

### 6.1. A member of an enum is built by `IEnumMemberBuilder` and not by `IFieldBuilder`

The read side reports a member of an enum as an `IField`, and `IEnumFacet` states that no interface of its own is
declared for it. The write side does not follow, and the asymmetry is deliberate.

`IFieldBuilder` derives from `IFieldOrPropertyBuilder`, from `IFieldOrPropertyOrIndexerBuilder`, from
`IMemberBuilder` and from `IHasTypeBuilder`. A member of an enum has none of what those interfaces offer: its type
is the enum and may not be set, its accessibility is that of the enum and may not be set, it has no initializer
expression, it has no writeability to choose, and every modifier that `IMemberBuilder` adds is invalid on it. A
field builder would present roughly fifteen operations of which one, the addition of an attribute, is valid.

The reason the asymmetry is acceptable is that the two sides answer different questions. A reader asks what a
member of an enum is, and it is a constant field, so `IField` is the right answer and a narrower interface would
only remove members that the reader may legitimately ask for. A writer asks what may be chosen about a member, and
the answer is its name, its value and its attributes, which is three things.

The alternative, which is to declare `IFieldBuilder` as the return type and to throw on the invalid operations, is
the shape that section 2 of [`introducing-types.md`](introducing-types.md) rejects at the level of the type. It is
rejected here for the same reason and at a worse ratio.

### 6.2. `IsFlags` is a property and not only an attribute

`IsFlags` sets an attribute, and an author can add that attribute directly. The property is declared nevertheless,
for symmetry with `IEnumFacet.IsFlags`, which exists so that no consumer has to search the attributes of a type for
a well-known name. A writer that had to construct an `AttributeConstruction` for `System.FlagsAttribute` would be
doing by hand what the reader is explicitly spared.

The property and the attribute are one state and not two: the setter adds or removes the attribute, and the getter
reports whether it is present. There is no third state in which they disagree.

### 6.3. The underlying type is set on the builder and not passed to the advice method

`IntroduceEnum` takes the name, the conflict strategy and the callback, and nothing else, which is the shape of
`IntroduceClass` and `IntroduceInterface`. The underlying type is configuration of the type, and configuration of
the type is what the callback is for. Adding a parameter for it would make `IntroduceEnum` the only introduction
method whose signature carries a property of the type being built.

### 6.4. The value of a member is validated when it is assigned

`AddMember` and `IEnumMemberBuilder.Value` both validate the value against `UnderlyingType`, and
`UnderlyingType` may not be set after a member has been added. The alternative is to validate every value when the
builder is frozen, which reports the error at a place the author did not write.

The cost is that an author who wants a non-default underlying type sets it before adding any member. The
documentation of `UnderlyingType` states that, and the exception names it.

## 7. Open questions

### 7.1. How does an aspect introduce an enum whose members are copied from another enum?

The motivating pattern of issue [#866](https://github.com/metalama/Metalama/issues/866) is a view model that
mirrors an enum of the domain. Such an aspect reads `source.Facets.Enum!.Members` and calls `AddMember` for each,
and the value of each member is an `IField.ConstantValue`, which is a `TypedConstant`. That works through
`IEnumMemberBuilder.Value` and not through the `long?` parameter of `AddMember`.

Whether `AddMember` should take a `TypedConstant?` instead of a `long?`, or an overload of each, is not decided.
The `long?` reads better for a literal, which is the common case in an aspect that writes the members itself, and
the `TypedConstant` reads better for a copy. What would settle it is the balance of the two in the aspects that are
written once the feature ships, which is not known now. The design above takes `long?` and leaves the copy to the
property, because a property that is needed for `ulong` in any case costs nothing more.

### 7.2. Is a member of an enum reachable as an `IFieldBuilder` during construction?

Section 6.1 decides what `AddMember` returns. It does not decide whether the engine object behind an
`IEnumMemberBuilder` is a `FieldBuilder`, which is an implementation question, nor whether an aspect that holds an
`IEnumMemberBuilder` can obtain the eventual `IField` from it before the enum is introduced.

The second half matters to an aspect that introduces an enum and then refers to one of its members in a template.
The member exists as an `IField` once the advice completes, reachable through the introduced type, so the question
is only whether the shorter path is needed. What would settle it is one sample aspect that does this, written
against the interface above.

## 8. References

- [`introducing-types.md`](introducing-types.md), sections 2, 3 and 5.
- [`type-facets.md`](type-facets.md), section 2.2. Its implementation guideline 5, which makes a builder return
  the empty facet collection, is superseded by section 5.1 of [`introducing-types.md`](introducing-types.md).
- `Metalama.Framework/Code/Types/IEnumFacet.cs`, the interface this design mirrors.
- `Metalama.Framework/Code/DeclarationBuilders/IDeclarationBuilder.cs`, whose comment at line 35 records that there
  is no way to provide the value of an enum when the enum type exists at run time only. That comment concerns an
  attribute argument rather than a member of an enum, and this design does not close it.

Issues:

- [#866](https://github.com/metalama/Metalama/issues/866), which this document designs.
- [#1996](https://github.com/metalama/Metalama/issues/1996), code model: the enum facet, closed in
  2027.0.2-preview. It delivered `IEnumFacet`, which section 5 mirrors, and its unit tests are the baseline against
  which an introduced enum is compared.
- [#869](https://github.com/metalama/Metalama/issues/869), type introduction: introduce struct, open. It carries
  the emission machinery that this issue consumes and it blocks this one.
- [#863](https://github.com/metalama/Metalama/issues/863), type introduction: reflection wrappers and syntax
  serialization, open. An enum is the kind most often serialized into a template, so this issue may gain work from
  this one.
- [#1950](https://github.com/metalama/Metalama/issues/1950), C# 15 closed classes: introducing, closed. The shape
  of that pull request is the shape this one should have.
- Section 8.4 of [`introducing-types.md`](introducing-types.md) lists the rest of the type introduction backlog.

— Claude for @gfraiteur
