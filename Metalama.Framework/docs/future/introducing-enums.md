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

A flags enum sets one more property, and states every value, because neither the language nor Metalama derives a
power of two from the position of a member:

```csharp
builder.IntroduceEnum(
    "Permissions",
    buildEnum: e =>
    {
        e.Accessibility = Accessibility.Public;
        e.UnderlyingType = TypeFactory.GetType( SpecialType.Byte );
        e.IsFlags = true;
        e.AddMember( "None", (byte) 0 );
        e.AddMember( "Read", (byte) 1 );
        e.AddMember( "Write", (byte) 2 );
        e.AddMember( "Execute", (byte) 4 );
    } );
```

An aspect that mirrors an enum of the domain copies the value of each member, which is a `TypedConstant`, and
annotates it:

```csharp
var source = builder.Target.Facets.Enum!;

builder.IntroduceEnum(
    builder.Target.Name + "ViewModel",
    buildEnum: e =>
    {
        e.Accessibility = Accessibility.Public;
        e.UnderlyingType = source.UnderlyingType;

        foreach ( var member in source.Members )
        {
            var added = e.AddMember( member.Name, member.ConstantValue!.Value );

            added.AddAttribute(
                AttributeConstruction.Create(
                    typeof(DescriptionAttribute),
                    new object?[] { member.Name } ) );
        }
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
/// than present and failing. The operations that it inherits and does not have are listed in the design document.
/// </para>
/// <para>
/// Every overload of <c>AddMember</c> applies the same three rules. The name must be a valid C# identifier and
/// must not be the name of a member already added, and a duplicate throws an <see cref="ArgumentException"/>. The
/// value is converted to <see cref="UnderlyingType"/>, and a value that does not fit in that type throws an
/// <see cref="ArgumentOutOfRangeException"/>. The members are declared in the order in which they were added.
/// </para>
/// <para>
/// An enum builder is not an <see cref="INamedType"/>, so it may not be used where an <see cref="IType"/> is
/// expected. The introduced enum is read from
/// <see cref="Metalama.Framework.Advising.IIntroductionAdviceResult{T}.Declaration"/>.
/// </para>
/// </remarks>
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
    /// Adds a member whose value the language assigns, which is zero for the first member and the value of the
    /// preceding member plus one for any other.
    /// </summary>
    /// <param name="name">The name of the member.</param>
    /// <returns>An <see cref="IEnumMemberBuilder"/> that allows you to add custom attributes to the new
    ///     member.</returns>
    IEnumMemberBuilder AddMember( string name );

    /// <summary>
    /// Adds a member of the given value.
    /// </summary>
    /// <param name="name">The name of the member.</param>
    /// <param name="value">The value of the member, converted to <see cref="UnderlyingType"/>.</param>
    /// <returns>An <see cref="IEnumMemberBuilder"/> that allows you to add custom attributes to the new
    ///     member.</returns>
    IEnumMemberBuilder AddMember( string name, sbyte value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, byte value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, short value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, ushort value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, int value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, uint value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, long value );

    /// <inheritdoc cref="AddMember(string,sbyte)"/>
    IEnumMemberBuilder AddMember( string name, ulong value );

    /// <summary>
    /// Adds a member whose value is given as a <see cref="TypedConstant"/>, which is the form in which the value of
    /// a member of another enum is read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The type of the constant must be an integral type or an enum, and any other type throws an
    /// <see cref="ArgumentException"/>. A constant that <see cref="TypedConstant.Create(IField)"/> produced
    /// references the field rather than its value, and the generated code names that field, which is what the
    /// language allows in the value of a member of an enum.
    /// </para>
    /// </remarks>
    /// <param name="name">The name of the member.</param>
    /// <param name="value">The value of the member, converted to <see cref="UnderlyingType"/>.</param>
    /// <returns>An <see cref="IEnumMemberBuilder"/> that allows you to add custom attributes to the new
    ///     member.</returns>
    IEnumMemberBuilder AddMember( string name, TypedConstant value );}
```

There are eight integral overloads and not one, and section 6.3 states why.

### 3.2. `IEnumMemberBuilder`

```csharp
// Metalama.Framework/Code/DeclarationBuilders/IEnumMemberBuilder.cs
namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to add custom attributes to a member of an enum that has been created by one of the
/// <c>AddMember</c> methods of <see cref="IEnumBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// This interface declares no member of its own. It exists for the custom attributes that
/// <see cref="IDeclarationBuilder"/> declares, which are the only thing about a member of an enum that remains to
/// be chosen after it is added: its name and its value are arguments of <c>AddMember</c>, and it has nothing else.
/// </para>
/// <para>
/// A member of an enum is a constant field, and the code model reports it as an <see cref="IField"/> once the enum
/// is introduced. This interface is nevertheless not an <see cref="IFieldBuilder"/>, for the reason that section
/// 6.1 of the design document gives.
/// </para>
/// </remarks>
/// <seealso cref="IEnumBuilder"/>
/// <seealso cref="IField"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IEnumMemberBuilder : IDeclarationBuilder, INamedDeclaration;
```

The interface declares no member. `IDeclarationBuilder` carries `AddAttribute`, two overloads of `AddAttributes`,
`RemoveAttributes`, `Freeze` and `IsFrozen`, and that is the whole of what the interface is for.
`INamedDeclaration` adds a read-only `Name`, so that a builder an aspect has collected says which member it is;
`Name` is not on `IDeclaration`, which is why it is named here.

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

The member introduction advices are refused by the rule of section 3.1 of
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
| `Members` | One `IField` per member added to the builder, in the order in which they were added. The value of each field is the value that the aspect assigned, converted to the underlying type, or the value the language computes when the aspect assigned none. |
| `IsFlags` | Whether the attribute is present, which is what `IsFlags` on the builder sets. |
| `FacetKind` | `TypeFacetKind.Enum`. |
| `Type` | The introduced type. |

`INamedType.IsEnum` reports `true`, and `INamedType.UnderlyingType` reports the same type as
`IEnumFacet.UnderlyingType`, which is what it reports for an enum read from source.

An enum is the one kind of this set whose members are emitted. The declaration that Metalama generates is
`enum State { Idle, Running, Stopped = 10 }`, so the members are inside it, and they are registered in the code
model from the same builders. That is not the case for a record, a union, a delegate or a struct, whose
synthesized members the compiler creates from the declaration and which section 4.2 of
[`introducing-types.md`](introducing-types.md) therefore keeps out of the generated code.

The reason for the difference is the one that section: a member is exempt from emission exactly when the compiler
creates it. The members of an enum are written by the aspect author, so they are emitted and registered like any
declared member, and the transformation that carries them is an ordinary injecting one.

The synthetic field whose metadata name is `value__` is the exception, and it is neither emitted nor materialized.
It is the field that the compiler synthesizes, `IEnumFacet.Members` excludes it by contract, and no consumer of the
code model reaches it, so materializing it would add a field to `INamedType.Fields` that a source enum does not
show there either.

The facet is tested by unit tests and not by aspect tests, for the reason that section 5.3 of
[`introducing-types.md`](introducing-types.md) gives: an aspect test compares generated code and cannot observe the
code model that the pipeline built. The tests belong beside `EnumFacetTests.cs`, which issue
[#1996](https://github.com/metalama/Metalama/issues/1996) added for an enum read from source. The assertion that
matters most is that an introduced enum and the equivalent enum read from source report the same underlying type,
the same members in the same order, the same values, and the same `IsFlags`.

## 6. Decisions

### 6.1. A member of an enum is built by `IEnumMemberBuilder` and not by `IFieldBuilder`

`AddMember` returns an `IEnumMemberBuilder`, and that interface exists for one reason: the custom attributes of the
member. Everything else about a member of an enum is an argument of `AddMember`, so the interface declares no
member of its own and derives from `IDeclarationBuilder`, which carries the attribute operations.

The attributes are not a marginal case. A view model that mirrors an enum of the domain, which is the pattern that
issue [#866](https://github.com/metalama/Metalama/issues/866) exists for, annotates each member with
`[Display]`, `[Description]` or `[EnumMember]`, and an aspect that could not do that would be of little use. An
earlier revision of this document returned nothing from `AddMember` and lost that capability, which is why the
interface is back.

`IFieldBuilder` was rejected as the return type and stays rejected. It derives from `IFieldOrPropertyBuilder`, from
`IFieldOrPropertyOrIndexerBuilder`, from `IMemberBuilder` and from `IHasTypeBuilder`. A member of an enum has none
of what those interfaces offer: its type is the enum and may not be set, its accessibility is that of the enum and
may not be set, it has no initializer expression, it has no writeability to choose, and every modifier that
`IMemberBuilder` adds is invalid on it. A field builder would present roughly fifteen operations of which one, the
addition of an attribute, is valid, which is the one this design needs.

The read side is unaffected and reports a member of an enum as an `IField`, which is what it is. The two sides
answer different questions: a reader asks what a member of an enum is, and a writer chooses a name, a value and the
attributes.

### 6.2. `IsFlags` is a property and not only an attribute

`IsFlags` sets an attribute, and an author can add that attribute directly. The property is declared nevertheless,
for symmetry with `IEnumFacet.IsFlags`, which exists so that no consumer has to search the attributes of a type for
a well-known name. A writer that had to construct an `AttributeConstruction` for `System.FlagsAttribute` would be
doing by hand what the reader is explicitly spared.

The property and the attribute are one state and not two: the setter adds or removes the attribute, and the getter
reports whether it is present. There is no third state in which they disagree.

### 6.3. There are eight integral overloads

`AddMember` has one overload per integral type that the language allows as the underlying type of an enum, plus one
that takes a `TypedConstant` and one that takes no value at all.

One overload taking the widest integral type would not do. `ulong` cannot express a negative value and `long`
cannot express a value above `long.MaxValue`, so a single overload of either type refuses a value that some enum
can hold. Taking `object` would move every type error from the compiler to run time. Eight overloads let the author
write the value in the type the enum actually has, the compiler picks the overload, and the builder converts to
`UnderlyingType` and checks the range.

The `TypedConstant` overload is the one an aspect uses when it copies from another enum, because
`IField.ConstantValue` is a `TypedConstant`. It also accepts a constant that `TypedConstant.Create(IField)`
produced, which renders as a reference to that field rather than as a literal, and the language allows a member of
an enum to be defined that way.

### 6.4. The value of a member is validated when it is added

Every overload of `AddMember` validates the value against `UnderlyingType`, and `UnderlyingType` may not be set
after a member has been added. The alternative is to validate every value when the builder is frozen, which reports
the error at a place the author did not write.

The cost is that an author who wants a non-default underlying type sets it before adding any member. The
documentation of `UnderlyingType` states that, and the exception names it.

## 7. Open questions

None.

Two questions that earlier revisions recorded are answered. An aspect copies the members of another enum through
the `TypedConstant` overload of `AddMember`, which section 6.3 describes and section 1 shows. An aspect puts a
custom attribute on a member through the `IEnumMemberBuilder` that `AddMember` returns, which section 6.1
describes.

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
