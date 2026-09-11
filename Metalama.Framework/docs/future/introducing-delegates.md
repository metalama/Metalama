# Introducing a delegate

This document designs the introduction of a delegate, which is issue
[#865](https://github.com/metalama/Metalama/issues/865). It is a design proposal. Nothing described here is
implemented.

The cross-cutting decisions are in [`introducing-types.md`](introducing-types.md), which this document does not
repeat. The one that shapes the interface below is section 2 of that document: a delegate builder derives from
`IMemberOrNamedTypeBuilder` and not from `INamedTypeBuilder`, because a delegate declaration has no member list.

The read side of a delegate is `IDelegateFacet`, which is delivered. The design below mirrors it member for member,
and section 5 states the mapping.

## 1. What the aspect author writes

```csharp
public class GenerateChangedEventAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var handler = builder.IntroduceDelegate(
            "ValueChangedHandler",
            buildDelegate: d =>
            {
                d.Accessibility = Accessibility.Public;
                d.ReturnType = TypeFactory.GetType( SpecialType.Void );
                d.AddParameter( "sender", builder.Target );
                d.AddParameter( "oldValue", typeof(object) );
                d.AddParameter( "newValue", typeof(object) );
            } );

        builder.IntroduceField( "_onValueChanged", handler.Declaration );
    }
}
```

The introduced delegate is reached through `handler.Declaration`, which is an `INamedType`, and is used there as the
type of an introduced field. The builder itself could not be used in that position, for the reason that section 2.4
of [`introducing-types.md`](introducing-types.md) gives.

A generic delegate adds type parameters to the type, and may give them variance:

```csharp
builder.IntroduceDelegate(
    "Transformer",
    buildDelegate: d =>
    {
        d.Accessibility = Accessibility.Public;

        var input = d.AddTypeParameter( "TInput" );
        input.Variance = VarianceKind.In;

        var output = d.AddTypeParameter( "TOutput" );
        output.Variance = VarianceKind.Out;

        d.ReturnType = output;
        d.AddParameter( "value", input );
    } );
```

## 2. What Metalama produces

```csharp
public delegate void ValueChangedHandler( TargetType sender, object oldValue, object newValue );

// and, in the target type:
private ValueChangedHandler _onValueChanged;
```

```csharp
public delegate TOutput Transformer<in TInput, out TOutput>( TInput value );
```

## 3. The interfaces

### 3.1. `IDelegateBuilder`

```csharp
// Metalama.Framework/Code/DeclarationBuilders/IDelegateBuilder.cs
namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of a delegate that has been created by an advice.
/// </summary>
/// <remarks>
/// <para>
/// This interface derives from <see cref="IMemberOrNamedTypeBuilder"/> and not from
/// <see cref="INamedTypeBuilder"/>, because a delegate declaration has no member list and no base type that an
/// author may choose. The operations that a delegate does not have are therefore absent rather than present and
/// failing. The operations that it inherits and does not have are listed below.
/// </para>
/// <para>
/// The following inherited properties are not valid for a delegate, and their setter throws a
/// <see cref="NotSupportedException"/>: <see cref="IMemberOrNamedTypeBuilder.IsStatic"/>,
/// <see cref="IMemberOrNamedTypeBuilder.IsSealed"/>, <see cref="IMemberOrNamedTypeBuilder.IsAbstract"/> and
/// <see cref="IMemberOrNamedTypeBuilder.IsPartial"/>. A delegate is implicitly sealed, it may not be abstract or
/// static, and the language has no partial delegate.
/// </para>
/// <para>
/// A delegate builder is not an <see cref="INamedType"/>, so it may not be used where an <see cref="IType"/> is
/// expected. The introduced delegate is read from
/// <see cref="Metalama.Framework.Advising.IIntroductionAdviceResult{T}.Declaration"/>, which is how an aspect gives
/// the introduced delegate as the type of a field, of a property or of an event.
/// </para>
/// <para>
/// The members below are the signature of the delegate, which is the signature of the <c>Invoke</c> method that the
/// compiler synthesizes for it. They are the members of <see cref="IMethodBuilder"/> that apply to a delegate, and
/// they carry the same names, so an aspect that configures a delegate writes what it would write for a method. The
/// <c>Invoke</c> method itself is not exposed during construction: it is read from
/// <see cref="Metalama.Framework.Code.Types.IDelegateFacet.InvokeMethod"/> on the introduced type.
/// </para>
/// <para>
/// The <c>BeginInvoke</c> and <c>EndInvoke</c> methods are not exposed and are not built. They exist only for a
/// delegate compiled for .NET Framework, and they are the asynchronous pattern that preceded <c>async</c>.
/// </para>
/// </remarks>
/// <seealso cref="Metalama.Framework.Code.Types.IDelegateFacet"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IDelegateBuilder : IMemberOrNamedTypeBuilder
{
    /// <summary>
    /// Gets or sets the return type of the delegate. The default value is <c>void</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A delegate may return by reference. The reference kind of the return is set on
    /// <see cref="ReturnParameter"/>, as it is on a method.
    /// </para>
    /// </remarks>
    /// <seealso cref="Metalama.Framework.Code.Types.IDelegateFacet.ReturnType"/>
    IType ReturnType { get; set; }

    /// <summary>
    /// Gets an object allowing to read and modify the return type and the custom attributes of the return value.
    /// </summary>
    IParameterBuilder ReturnParameter { get; }

    /// <summary>
    /// Gets the parameters of the delegate.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Code.Types.IDelegateFacet.Parameters"/>
    IParameterBuilderList Parameters { get; }

    /// <summary>
    /// Appends a parameter to the delegate.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="refKind">The reference kind of the parameter.</param>
    /// <param name="defaultValue">The default value of the parameter, or <c>null</c> when it has none.</param>
    /// <returns>An <see cref="IParameterBuilder"/> that allows you to further build the new parameter.</returns>
    IParameterBuilder AddParameter(
        string name,
        IType type,
        RefKind refKind = RefKind.None,
        TypedConstant? defaultValue = default );

    /// <summary>
    /// Appends a parameter to the delegate.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="refKind">The reference kind of the parameter.</param>
    /// <param name="defaultValue">The default value of the parameter, or <c>null</c> when it has none.</param>
    /// <returns>An <see cref="IParameterBuilder"/> that allows you to further build the new parameter.</returns>
    IParameterBuilder AddParameter(
        string name,
        Type type,
        RefKind refKind = RefKind.None,
        TypedConstant? defaultValue = null );

    /// <summary>
    /// Adds a type parameter to the delegate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The type parameters of a delegate belong to the type and not to its <c>Invoke</c> method, which is why this
    /// member exists on the builder of the type. A delegate is the one kind of type, besides an interface, whose
    /// type parameters may declare variance, through <see cref="ITypeParameterBuilder.Variance"/>.
    /// </para>
    /// </remarks>
    /// <param name="name">The name of the type parameter.</param>
    /// <returns>An <see cref="ITypeParameterBuilder"/> that allows you to further configure the new type
    ///     parameter, including its constraints and its variance.</returns>
    ITypeParameterBuilder AddTypeParameter( string name );
}
```

### 3.2. The advice method

```csharp
// Metalama.Framework/Advising/IAdviceFactory.cs
/// <summary>
/// Introduces a new delegate to the target namespace or type.
/// </summary>
/// <param name="targetNamespaceOrType">The namespace or type into which the delegate must be introduced.</param>
/// <param name="name">The name of the introduced delegate.</param>
/// <param name="whenExists">Determines the implementation strategy when a type of the same name is already declared
///     in the target namespace or type. The default strategy is to fail with a compile-time error.</param>
/// <param name="buildDelegate">An optional callback that allows you to configure the introduced delegate, in
///     particular to give it a return type and parameters. A delegate that the callback leaves unconfigured
///     returns <c>void</c> and takes no parameter.</param>
/// <returns>An <see cref="IIntroductionAdviceResult{T}"/> representing the result of the advice. The
/// <see cref="IIntroductionAdviceResult{T}.Declaration"/> property provides access to the introduced delegate.
/// Unlike the result of introducing a class, this result must not be used to introduce members, because a delegate
/// accepts none.</returns>
IIntroductionAdviceResult<INamedType> IntroduceDelegate(
    INamespaceOrNamedType targetNamespaceOrType,
    string name,
    OverrideStrategy whenExists = OverrideStrategy.Default,
    Action<IDelegateBuilder>? buildDelegate = null );
```

```csharp
// Metalama.Framework/Aspects/AdviserExtensions.cs
/// <summary>
/// Introduces a new delegate into the current namespace (as a top-level type) or type (as a nested type).
/// Use the <see cref="IAdviser.With{TNewDeclaration}"/> or <see cref="WithNamespace"/> method to introduce the
/// delegate to a different type or namespace than the current one.
/// </summary>
/// <param name="adviser">An adviser for a named type or namespace.</param>
/// <param name="name">The delegate name.</param>
/// <param name="whenExists">Determines the implementation strategy when a type of the same name is already declared
///     in the target type or namespace. The default strategy is to fail with a compile-time error.</param>
/// <param name="buildDelegate">An optional delegate that modifies the <see cref="IDelegateBuilder"/> that
///     represents the introduced delegate. The signature of the delegate is given through this callback.</param>
/// <returns>An <see cref="IIntroductionAdviceResult{T}"/> that exposes the outcome of the operation and the
/// introduced <see cref="INamedType"/>.</returns>
/// <seealso href="@introducing-types"/>
public static IIntroductionAdviceResult<INamedType> IntroduceDelegate(
    this IAdviser<INamespaceOrNamedType> adviser,
    string name,
    OverrideStrategy whenExists = OverrideStrategy.Default,
    Action<IDelegateBuilder>? buildDelegate = null )
    => ((IAdviserInternal) adviser).AdviceFactory.IntroduceDelegate(
        adviser.Target,
        name,
        whenExists,
        buildDelegate );
```

## 4. The inherited operations that are not valid

`IDelegateBuilder` inherits six settable properties from `IMemberOrNamedTypeBuilder`. Two are valid and four are
not.

| Member | State |
| --- | --- |
| `Accessibility` | Valid. |
| `Name` | Valid. |
| `IsStatic` | The setter throws a `NotSupportedException`. A delegate is neither static nor an instance type. |
| `IsSealed` | The setter throws a `NotSupportedException`. A delegate is implicitly sealed. |
| `IsAbstract` | The setter throws a `NotSupportedException`. A delegate may not be abstract. |
| `IsPartial` | The setter throws a `NotSupportedException`. The language has no partial delegate. |

The operations of `IDeclarationBuilder` are all valid: a delegate carries attributes.

There is no `Invoke` method builder to restrict, because section 6.1 puts the signature on the builder of the type
itself. The members that `IMethodBuilder` declares and that a delegate does not accept are therefore absent rather
than failing: `IsReadOnly`, `OperatorKind`, and the modifiers that `IMemberBuilder` adds, which are `IsVirtual` and
`IsExtern`.

Every operation of `INamedTypeBuilder` is absent rather than failing, because `IDelegateBuilder` does not derive
from that interface. That includes `Facets`: a caller that reaches the engine object as an `INamedType` and reads
`Facets` on it meets the `NotSupportedException` of section 5.1 of
[`introducing-types.md`](introducing-types.md). The one operation of `INamedTypeBuilder` that a delegate genuinely
needs is `AddTypeParameter`, which section 3.1 declares again.

## 5. What the facet of the introduced delegate reports

A delegate builder declares no `Facets` member, because `IDelegateBuilder` does not derive from `INamedType`. The
engine class behind it does, and it throws a `NotSupportedException`, which section 5.1 of
[`introducing-types.md`](introducing-types.md) states for every builder. The kind of a builder is read from
`IsDelegate`, which answers without allocating and does not throw.

That the delegate builder is not a type also bounds the risk of the exception. Section 5.1 lists
`EventBuilder.Signature` and the eligibility rule of `AdviceKind.OverrideEventInvoke` as the readers that take the
type of an event from the aspect. An aspect cannot give a delegate builder as the type of an event, because the
builder is not an `IType`, so neither reader can meet one.

The introduced type reports an `IDelegateFacet`.

| `IDelegateFacet` member | Source |
| --- | --- |
| `InvokeMethod` | The `Invoke` method, materialized as a builder from the signature that the aspect gave to `IDelegateBuilder`, so that the facet can name it without re-reading the model from Roslyn. |
| `ReturnType` | The return type of that method. |
| `Parameters` | The parameters of that method. |
| `FacetKind` | `TypeFacetKind.Delegate`. |
| `Type` | The introduced type. |

`INamedType.IsDelegate` reports `true`, and `INamedType.Methods` contains the `Invoke` method, which is what it
contains for a delegate read from source.

Materialized means present in the code model and not emitted as syntax. Metalama generates the delegate
declaration, which is the `delegate` keyword and the signature, and the compiler synthesizes the `Invoke` method
and the constructor from it exactly as it does for a delegate the user wrote. A transformation that injected the
`Invoke` method as well would declare it twice, and a delegate declaration has no member list to put it in.
Section 5.2 of [`introducing-types.md`](introducing-types.md) states the rule.

`DelegateFacet` resolves the `Invoke` method by name today, through
`this.Type.Methods.OfName( "Invoke" ).Single()`. That continues to work for an introduced delegate, because the
method is a member of the type like any other once it is registered in the code model. The facet needs no branch
for an introduced delegate.

`BeginInvoke` and `EndInvoke` are not materialized. The facet does not expose them, no consumer reaches them, and
an introduced delegate is compiled for the target framework of the project rather than for .NET Framework
specifically.

The facet is tested by unit tests and not by aspect tests, for the reason that section 5.3 of
[`introducing-types.md`](introducing-types.md) gives. Two assertions are specific to this kind. An event whose type
is an introduced delegate reports the `Invoke` method of that delegate as its `Signature`, which is the shape that
`TypeFacetTests.SignatureOfIntroducedEventIsTheInvokeMethodOfTheFacet` already tests for a delegate read from
source. And an introduced delegate and the equivalent delegate read from source report the same return type and the
same parameters.

## 6. Decisions

### 6.1. The signature is on the builder of the type, and the builder does not derive from `IMethodBuilder`

A delegate declaration is a method signature with the `delegate` keyword in front of it, so the members that
configure it are the members that configure a method. This design declares them on `IDelegateBuilder` with the
names that `IMethodBuilder` uses, rather than exposing an `IMethodBuilder` for the `Invoke` method or deriving from
that interface. Two alternatives were considered and both are rejected.

The first alternative is `IDelegateBuilder : IMethodBuilder`, with the inapplicable operations made illegal. It
would add no member of its own, which is what makes it attractive. It cannot be taken, because the chain
`IMethodBuilder : IMethodBaseBuilder`, `IMethodBaseBuilder : IHasParametersBuilder`,
`IHasParametersBuilder : IMemberBuilder`, `IMemberBuilder : IMember` ends at the interface that narrows the
declaring type to a value that is not nullable, and a delegate declared in a namespace has no declaring type. That
is the same obstacle that section 2.1 of [`introducing-types.md`](introducing-types.md) records against
`IMemberBuilder`, reached through four interfaces instead of one. `IMethod` also derives from `IMethodInvoker`, so
the builder of a delegate type would offer the operations that invoke a method.

The second alternative is an `InvokeMethod` property typed as `IMethodBuilder`, which an earlier revision of this
document took. The `Invoke` method does not need to be reachable while the delegate is being built: everything an
author sets on it is the signature, and the signature is what the members below are. Keeping the property would
mean two ways to write the same thing, and a second object whose name, accessibility and modifiers are all invalid.

What is lost is the symmetry with `IDelegateFacet`, which declares `InvokeMethod` first and documents `ReturnType`
and `Parameters` as being those of the first. The asymmetry is accepted, and it is the same one that section 6.1 of
[`introducing-enums.md`](introducing-enums.md) accepts: a reader asks what a delegate is made of, and the answer
names the method, while a writer chooses a signature and never needs the method as an object. The `Invoke` method
of an introduced delegate is reached through the facet as soon as the advice completes.

### 6.2. The type parameters belong to the type

`IDelegateBuilder.AddTypeParameter` adds a type parameter to the delegate and not to its `Invoke` method. The
language places the type parameters of a delegate on the type: `Func<T, TResult>` is a generic type whose `Invoke`
method is not generic.

The consequence that the documentation states is that a delegate is the one kind of type, besides an interface,
whose type parameters may declare variance, so `ITypeParameterBuilder.Variance` is meaningful here and nowhere
else among the five kinds.

### 6.3. A delegate is introduced by its own advice method

The reasoning is that of section 6.2 of [`introducing-structs.md`](introducing-structs.md). A delegate accepts a
configuration that no other kind accepts, which is a signature, and it accepts almost nothing that the other kinds
accept.

## 7. Open questions

None. Two questions that an earlier revision recorded are answered, and section 6 carries both.

The `Invoke` method does not need to be reachable while the delegate is being built, which is why section 6.1
removes the `InvokeMethod` property rather than keeping it for a case that does not arise. The method is reached
through the facet of the introduced type as soon as the advice completes.

A `ref` return and a pointer parameter are in scope. A delegate may return by reference and may take a pointer
parameter in an unsafe context, `ReturnParameter` and `AddParameter` express both, and the emission path handles
them. Each is covered by a test rather than refused.

## 8. References

- [`introducing-types.md`](introducing-types.md), sections 2, 3 and 5.
- [`introducing-enums.md`](introducing-enums.md), section 6.1, which this document contrasts with in section 6.2.
- [`type-facets.md`](type-facets.md), section 2.2. Its implementation guideline 5, which makes a builder return
  the empty facet collection, is superseded by section 5.1 of [`introducing-types.md`](introducing-types.md).
- `Metalama.Framework/Code/Types/IDelegateFacet.cs`, the interface this design mirrors.
- `Metalama.Framework.Engine/CodeModel/Facets/DelegateFacet.cs`, which resolves the `Invoke` method by name and is
  described there as the single site of the code model that does so.

Issues:

- [#865](https://github.com/metalama/Metalama/issues/865), which this document designs.
- [#1995](https://github.com/metalama/Metalama/issues/1995), code model: type facets and the delegate facet, closed
  in 2027.0.2-preview. It delivered `ITypeFacetCollection` and `IDelegateFacet`, which section 5 mirrors, and it
  converted the fifteen call sites that resolved the `Invoke` method by a string literal.
- [#869](https://github.com/metalama/Metalama/issues/869), type introduction: introduce struct, open. It carries
  the emission machinery that this issue consumes and it blocks this one.
- [#863](https://github.com/metalama/Metalama/issues/863), type introduction: reflection wrappers and syntax
  serialization, open. A delegate is reached through `IEvent.Signature`, which the facet now defines, so the
  wrappers of an introduced delegate are worth a test in this issue.
- [#1950](https://github.com/metalama/Metalama/issues/1950), C# 15 closed classes: introducing, closed. The shape
  of that pull request is the shape this one should have.
- Section 8.4 of [`introducing-types.md`](introducing-types.md) lists the rest of the type introduction backlog.

— Claude for @gfraiteur
