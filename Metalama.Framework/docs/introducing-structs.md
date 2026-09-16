# Introducing a struct

This document describes the introduction of a struct, which is issue
[#869](https://github.com/metalama/Metalama/issues/869), and which is implemented.

The cross-cutting decisions are in [`introducing-types.md`](introducing-types.md), which this document does not
repeat. A record struct is not designed here: it is a record, and
[`introducing-records.md`](introducing-records.md) designs it.

This is the smallest of the five designs, because a struct is a named type with no structure of its own. A struct
has no facet, so no builder interface is added and `INamedTypeBuilder` is used directly. The value of the issue is
that it carries the emission machinery that section 4 of [`introducing-types.md`](introducing-types.md) describes,
and which the other four consume.

## 1. What the aspect author writes

```csharp
public class GeneratePointAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var point = builder.IntroduceStruct(
            "Point",
            buildType: t =>
            {
                t.Accessibility = Accessibility.Public;
                t.IsReadOnly = true;
            } );

        point.IntroduceAutomaticProperty(
            "X",
            typeof(double),
            buildProperty: p => p.Writeability = Writeability.InitOnly );

        point.IntroduceAutomaticProperty(
            "Y",
            typeof(double),
            buildProperty: p => p.Writeability = Writeability.InitOnly );
    }
}
```

## 2. What Metalama produces

```csharp
public readonly struct Point
{
    public double X { get; init; }
    public double Y { get; init; }
}
```

A readonly struct may declare no settable instance auto-property, which is why the example makes both properties
init-only. Metalama does not refuse a settable one, and section 3.2 of
[`introducing-types.md`](introducing-types.md) states why: the compiler reports the error on the generated
declaration.

## 3. The interfaces

### 3.1. The advice method

The method follows `IntroduceClass` and `IntroduceInterface`, which it sits beside, and takes the same parameters.

```csharp
// Metalama.Framework/Advising/IAdviceFactory.cs
/// <summary>
/// Introduces a new struct to the target namespace or type.
/// </summary>
/// <param name="targetNamespaceOrType">The namespace or type into which the struct must be introduced.</param>
/// <param name="name">The name of the introduced struct.</param>
/// <param name="whenExists">Determines the implementation strategy when a type of the same name is already declared
///     in the target namespace or type. The default strategy is to fail with a compile-time error.</param>
/// <param name="buildType">An optional callback that allows you to configure the introduced struct, such as adding
///     members, implemented interfaces, or custom attributes.</param>
/// <returns>An <see cref="IIntroductionAdviceResult{T}"/> representing the result of the advice. The
/// <see cref="IIntroductionAdviceResult{T}.Declaration"/> property provides access to the introduced struct.
/// The <see cref="IIntroductionAdviceResult{T}"/> interface itself implements <see cref="IAdviser{T}"/> and can be
/// used to introduce members to the type.</returns>
IIntroductionAdviceResult<INamedType> IntroduceStruct(
    INamespaceOrNamedType targetNamespaceOrType,
    string name,
    OverrideStrategy whenExists = OverrideStrategy.Default,
    Action<INamedTypeBuilder>? buildType = null );
```

```csharp
// Metalama.Framework/Aspects/AdviserExtensions.cs
/// <summary>
/// Introduces a new struct into the current namespace (as a top-level type) or type (as a nested type).
/// Use the <see cref="IAdviser.With{TNewDeclaration}"/> or <see cref="WithNamespace"/> method to introduce the
/// struct to a different type or namespace than the current one.
/// </summary>
/// <param name="adviser">An adviser for a named type or namespace.</param>
/// <param name="name">The struct name.</param>
/// <param name="whenExists">Determines the implementation strategy when a type of the same name is already declared
///     in the target type or namespace. The default strategy is to fail with a compile-time error.</param>
/// <param name="buildType">An optional delegate that modifies the
///     <see cref="INamedTypeBuilder"/> that represents the introduced struct.</param>
/// <returns>An <see cref="IIntroductionAdviceResult{T}"/> that exposes the outcome of the operation and the
/// introduced <see cref="INamedType"/>.</returns>
/// <seealso href="@introducing-types"/>
public static IIntroductionAdviceResult<INamedType> IntroduceStruct(
    this IAdviser<INamespaceOrNamedType> adviser,
    string name,
    OverrideStrategy whenExists = OverrideStrategy.Default,
    Action<INamedTypeBuilder>? buildType = null )
    => ((IAdviserInternal) adviser).AdviceFactory.IntroduceStruct(
        adviser.Target,
        name,
        whenExists,
        buildType );
```

### 3.2. The members added to `INamedTypeBuilder`

`INamedTypeBuilder` carries two commented-out declarations under the comment `// TODO: Struct introduction`. This
issue uncomments them and documents them. `INamedType` already declares both as read-only properties, so each is a
`new` member that adds a setter.

```csharp
// Metalama.Framework/Code/DeclarationBuilders/INamedTypeBuilder.cs
/// <summary>
/// Gets or sets a value indicating whether the type is declared with the <c>readonly</c> modifier.
/// </summary>
/// <remarks>
/// <para>
/// The language allows the <c>readonly</c> modifier on a struct only. The setter throws an
/// <see cref="InvalidOperationException"/> when the type being built is not a struct.
/// </para>
/// <para>
/// A readonly struct may declare no settable instance field and no automatic property that has a setter. Metalama
/// does not refuse an advice that introduces one, and the compiler reports the error on the generated
/// declaration.
/// </para>
/// </remarks>
new bool IsReadOnly { get; set; }

/// <summary>
/// Gets or sets a value indicating whether the type is declared with the <c>ref</c> modifier, which makes it a
/// type that may live on the stack only.
/// </summary>
/// <remarks>
/// <para>
/// The language allows the <c>ref</c> modifier on a struct only. The setter throws an
/// <see cref="InvalidOperationException"/> when the type being built is not a struct.
/// </para>
/// <para>
/// A ref struct may not be used as a type argument, may not be a field of a type that is not itself a ref struct,
/// and may not be boxed. Metalama does not verify those restrictions on the code that an aspect generates, so the
/// compiler reports them on the generated code.
/// </para>
/// </remarks>
new bool IsRef { get; set; }
```

The two properties are declared on `INamedTypeBuilder` rather than on a builder interface of their own, because
there is no struct builder. Setting either on a class or an interface throws, in the manner of `IsClosed`, which
the same interface already declares and which throws when the type is not a class.

## 4. The inherited operations that are not valid

`INamedTypeBuilder` is used unchanged, so the restrictions are those of the language rather than of the interface.

- `BaseType` may not be set. A struct derives from `System.ValueType` and the language allows no other base. The
  setter throws an `InvalidOperationException`, in the manner of the existing check that refuses a base type other
  than `object` on an interface.
- `IsAbstract` and `IsSealed` may not be set to `true`. A struct is implicitly sealed and may not be abstract.
- `IsClosed` may not be set. The `closed` modifier applies to a class only, which the property already documents.
- `IsStatic` may not be set to `true`. A static struct is not a language construct.
- `Facets` may not be read. The getter throws a `NotSupportedException` on every builder, which section 5 states and
  which this issue introduces.

Implemented interfaces, type parameters, members and attributes are all valid, and no advice is refused on an
introduced struct beyond what the language refuses on any struct.

## 5. What the facet reports, and what this issue changes about facets

A struct has no facet, so an introduced struct reports the empty collection, as an introduced class and an
introduced interface do. It is the one of the five kinds that adds no facet.

It is nevertheless the issue that changes how a builder answers `Facets`, because section 4 gives it the machinery
that the five kinds share and this is part of it. Section 5.1 of [`introducing-types.md`](introducing-types.md)
states the change: `NamedTypeBuilder.Facets` throws a `NotSupportedException` instead of returning the empty
collection, and the three readers listed there are guarded before the exception is introduced. Implementation
guideline 5 of [`type-facets.md`](future/type-facets.md) is superseded by it.

The flags stay as they are. `IsEnum`, `IsDelegate`, `IsRecord` and `IsUnion` are answered by a builder from its own
kind, they allocate nothing, and this issue does not change them. That separation is what lets the structure throw
without breaking a caller that only asks what kind a type is.

The unit tests of this issue are therefore about the exception rather than about a facet:
`TypeFacetTests.FacetsOfBuilderDoNotThrow` is rewritten to assert that the builder throws and that the four flags
do not, and a test asserts that an introduced struct reports the empty collection. Section 5.3 of
[`introducing-types.md`](introducing-types.md) states why these are unit tests and not aspect tests.

## 6. Decisions

### 6.1. There is no `IStructBuilder`

A builder interface exists to carry the operations that one kind of type has and others do not. A struct has none:
every operation it needs is on `INamedTypeBuilder` already, and `IsReadOnly` and `IsRef` are added there because
`INamedType` declares them there. An `IStructBuilder` that added no member would be a name for the same set of
operations, and it would force the advice method to return a different callback type for no gain.

The same reasoning gives the answer for a class and an interface, which have `IntroduceClass` and
`IntroduceInterface` and no builder interface of their own.

### 6.2. The struct is introduced by its own advice method and not by a parameter

`IntroduceStruct` is a method beside `IntroduceClass` and `IntroduceInterface`, and not a parameter of a single
`IntroduceType` method that takes a kind. The precedent is the pair that already exists: the two shipped methods
differ only in the kind they pass to `IntroduceNamedTypeAdvice`, and they are nevertheless two methods.

The reason the precedent is right is that the kinds do not accept the same configuration. An interface takes no
base type, a struct takes neither a base type nor the abstract modifier, and a record takes positional parameters
that no other kind takes. A single method with a kind parameter would accept every combination and reject most of
them at run time.

### 6.3. This issue is implemented first

A struct has no facet and adds no public interface, so it exercises the emission machinery of section 4 of
[`introducing-types.md`](introducing-types.md) alone. Every defect that the first implementation of a new type kind
meets is found here, where no builder interface and no synthesized member is present to confuse the diagnosis.

The machinery is already half present, which makes this the cheapest place to find those defects: the
`TypeKind.Struct` arm of `IntroduceNamedTypeTransformation` exists and is correct, and what is missing is the
public advice method that reaches it, the two properties of section 3.2, and the design-time path.

## 7. Open questions

None. Two questions that an earlier revision recorded are answered below.

### 7.1. `IsRef` needs no diagnostic of its own

A ref struct carries restrictions that the compiler reports on generated code, and Metalama does not refuse an
advice that breaks one. Introducing a ref struct as the type of a field of a class is left to the aspect author,
and the compiler reports the error on the generated declaration.

This is the general rule of section 3.2 of [`introducing-types.md`](introducing-types.md) rather than a decision
about ref structs: the framework does not prevent an aspect author from generating invalid code, and a framework
that reproduced the rules of the language would reproduce a part of them and be wrong about some of that part.

### 7.2. The implicit parameterless constructor is materialized in the code model and is not emitted

A struct has an implicit parameterless constructor, and the code model of an introduced struct reports it, so that
an introduced struct answers `Constructors` as a struct read from source does. That matches the behaviour of
Roslyn, which is the standard the code model is held to.

It is not emitted as syntax. The compiler synthesizes the constructor of a struct from the declaration, so emitting
one would declare it twice. Section 4.2 of [`introducing-types.md`](introducing-types.md) states the rule and the
mechanism for all five kinds: the transformation implements `IIntroduceDeclarationTransformation`, does not
implement `IInjectMemberTransformation`, and therefore reaches the code model while neither the linker nor the
design-time generator emits it.

The struct is the smallest instance of that rule, with one member and no facet, which is a further reason for this
issue to be implemented first. The prototype that story S-29 asks for belongs here rather than in the union work,
because this is where it is cheapest to run.

`IntroduceNamedTypeAdvice.IntroduceImplicitConstructorIfNeeded`, which materializes the parameterless constructor
of an introduced class, is the precedent a reader reaches for and it is the wrong one: it derives from
`IntroduceDeclarationTransformation<T>`, which implements both interfaces, so it emits the constructor as well.
That is correct for a class and is not the shape this issue needs.

## 8. References

- [`introducing-types.md`](introducing-types.md), sections 2, 4 and 6.
- [`type-facets.md`](future/type-facets.md), section 2.2. Its implementation guideline 5, which makes a builder return
  the empty facet collection, is superseded by section 5.1 of [`introducing-types.md`](introducing-types.md).
- [`../2027.0/analysis-reports/10-introducing-closed-and-unions.md`](../2027.0/analysis-reports/10-introducing-closed-and-unions.md),
  which records that the struct path of the builder and of the transformation is reachable code that no public
  method reaches.

Issues:

- [#869](https://github.com/metalama/Metalama/issues/869), which this document designs.
- [#1950](https://github.com/metalama/Metalama/issues/1950), C# 15 closed classes: introducing, which is user story
  S-28 and is closed. It is the most recent change to `INamedTypeBuilder`, it added `IsClosed` beside the two
  properties of section 3.2, and the pull request that implements this issue should resemble it.
- [#1869](https://github.com/metalama/Metalama/issues/1869), cannot introduce a partial class, closed. It added
  `IsPartial` to the named type builder, so the member set of section 3.2 changed in this release.
- [#868](https://github.com/metalama/Metalama/issues/868), type introduction: introduce primary constructor, open.
  Section 7.2 asks whether the implicit parameterless constructor of a struct is materialized, and the explicit
  primary constructor of a struct belongs to that issue.
- [#863](https://github.com/metalama/Metalama/issues/863), type introduction: reflection wrappers and syntax
  serialization, open. An introduced struct reaches the same reflection wrappers as an introduced class, and
  whether they need work for a value type is not answered here.
- Section 8.4 of [`introducing-types.md`](introducing-types.md) lists the rest of the type introduction backlog.

— Claude for @gfraiteur
