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
/// </remarks>
/// <seealso cref="Metalama.Framework.Code.Types.IDelegateFacet"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IDelegateBuilder : IMemberOrNamedTypeBuilder
{
    /// <summary>
    /// Gets the builder of the <c>Invoke</c> method, which carries the signature of the delegate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is the complete signature of the delegate, and
    /// <see cref="ReturnType"/>, <see cref="Parameters"/> and
    /// <see cref="AddParameter(string,IType,Code.RefKind,TypedConstant?)"/>
    /// are shortcuts onto it. It is exposed so that an aspect that already has an <see cref="IMethod"/> to copy, in
    /// particular the <c>Invoke</c> method of another delegate, can configure the signature by the same operations
    /// it would use on any method.
    /// </para>
    /// <para>
    /// The following operations of the method builder are not valid on the <c>Invoke</c> method of a delegate, and
    /// each of them throws a <see cref="NotSupportedException"/>:
    /// <see cref="IMethodBuilder.AddTypeParameter"/>, because the type parameters of a delegate belong to the type
    /// and are added by <see cref="AddTypeParameter"/> on this interface;
    /// <see cref="IMemberOrNamedTypeBuilder.Name"/> and
    /// <see cref="IMemberOrNamedTypeBuilder.Accessibility"/>, which the language fixes;
    /// <see cref="IMethodBuilder.OperatorKind"/>; <see cref="IMethodBuilder.IsReadOnly"/>; and every modifier that
    /// <see cref="IMemberBuilder"/> and <see cref="IMemberOrNamedTypeBuilder"/> declare.
    /// </para>
    /// <para>
    /// The <c>BeginInvoke</c> and <c>EndInvoke</c> methods are not exposed and are not built. They exist only for a
    /// delegate compiled for .NET Framework, and they are the asynchronous pattern that preceded <c>async</c>.
    /// </para>
    /// </remarks>
    /// <seealso cref="Metalama.Framework.Code.Types.IDelegateFacet.InvokeMethod"/>
    IMethodBuilder InvokeMethod { get; }

    /// <summary>
    /// Gets or sets the return type of the delegate, which is the return type of <see cref="InvokeMethod"/>. The
    /// default value is <c>void</c>.
    /// </summary>
    /// <seealso cref="Metalama.Framework.Code.Types.IDelegateFacet.ReturnType"/>
    IType ReturnType { get; set; }

    /// <summary>
    /// Gets the parameters of the delegate, which are the parameters of <see cref="InvokeMethod"/>.
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
    /// The type parameters of a delegate belong to the type and not to its <c>Invoke</c> method, so they are added
    /// here. A delegate is the one kind of type whose type parameters may declare variance, through
    /// <see cref="ITypeParameterBuilder.Variance"/>.
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

The restrictions on `InvokeMethod` are listed in the documentation of that property in section 3.1, and they exist
because `IMethodBuilder` is reused whole rather than narrowed. Section 6.2 states why reuse is preferred to a
narrower interface here and not for the members of an enum.

Every operation of `INamedTypeBuilder` is absent rather than failing, because `IDelegateBuilder` does not derive
from that interface. The one operation of `INamedTypeBuilder` that a delegate genuinely needs is
`AddTypeParameter`, which section 3.1 declares again.

## 5. What the facet of the introduced delegate reports

The introduced type reports an `IDelegateFacet` rather than the empty collection, which replaces implementation
guideline 5 of [`type-facets.md`](type-facets.md) for this kind.

| `IDelegateFacet` member | Source |
| --- | --- |
| `InvokeMethod` | The method that `IDelegateBuilder.InvokeMethod` built, materialized as a builder so that the facet can name it without re-reading the model from Roslyn. |
| `ReturnType` | The return type of that method. |
| `Parameters` | The parameters of that method. |
| `FacetKind` | `TypeFacetKind.Delegate`. |
| `Type` | The introduced type. |

`INamedType.IsDelegate` reports `true`, and `INamedType.Methods` contains the `Invoke` method, which is what it
contains for a delegate read from source.

`DelegateFacet` resolves the `Invoke` method by name today, through
`this.Type.Methods.OfName( "Invoke" ).Single()`. That continues to work for an introduced delegate, because the
method is a member of the type like any other. The facet needs no branch for an introduced delegate.

`BeginInvoke` and `EndInvoke` are not materialized. The facet does not expose them, no consumer reaches them, and
an introduced delegate is compiled for the target framework of the project rather than for .NET Framework
specifically.

## 6. Decisions

### 6.1. The signature is the `Invoke` method and not a flat surface on the type

`IDelegateBuilder` could declare a return type and a parameter list of its own, with no `InvokeMethod` property.
The design exposes the method instead, for two reasons.

The first is symmetry with the reader. `IDelegateFacet` declares `InvokeMethod`, `ReturnType` and `Parameters`, in
that order, with the second and the third documented as being those of the first. A writer that inverted the
relation, making the flat members the truth and offering no method, would describe the same delegate by a different
structure on each side.

The second is that an aspect that introduces a delegate usually has a signature to copy, and the thing it is
copying from is an `IMethod`. Exposing an `IMethodBuilder` lets it use the operations it already knows, including
`AddParameter` with a `RefKind` and a default value, and `ReturnParameter` for the attributes of the return value,
which a flat surface would have to declare again.

The shortcuts are declared nevertheless, because the common case is an aspect that writes a short signature by
hand, and `d.ReturnType = ...` reads better than `d.InvokeMethod.ReturnType = ...` when repeated. The facet makes
the same trade and for the same reason.

### 6.2. `IMethodBuilder` is reused whole, and the members of an enum are not

Section 6.1 of [`introducing-enums.md`](introducing-enums.md) rejects `IFieldBuilder` as the return type of
`AddMember`, and this document accepts `IMethodBuilder` as the type of `InvokeMethod`. The two decisions are
consistent, because the ratio of valid to invalid operations is not the same.

Of the operations of `IFieldBuilder`, one is valid for a member of an enum. Of the operations of `IMethodBuilder`,
the parameters, the return type, the return parameter and the attributes are all valid, which is most of the
interface, and the ones that are not are the name, the accessibility, the modifiers, the operator kind and the
addition of a type parameter. An `Invoke` method genuinely is a method, with a signature that an author chooses
freely. A member of an enum is a name and a number.

### 6.3. The type parameters belong to the type

`IDelegateBuilder.AddTypeParameter` adds a type parameter to the delegate, and
`IDelegateBuilder.InvokeMethod.AddTypeParameter` throws. The language places the type parameters of a delegate on
the type: `Func<T, TResult>` is a generic type whose `Invoke` method is not generic.

The consequence that the documentation states is that a delegate is the one kind of type whose type parameters may
declare variance, so `ITypeParameterBuilder.Variance` is meaningful here and on an interface, and nowhere else.

### 6.4. A delegate is introduced by its own advice method

The reasoning is that of section 6.2 of [`introducing-structs.md`](introducing-structs.md). A delegate accepts a
configuration that no other kind accepts, which is a signature, and it accepts almost nothing that the other kinds
accept.

## 7. Open questions

### 7.1. Does the `Invoke` method need to be reachable before the advice completes?

The design exposes `InvokeMethod` as an `IMethodBuilder` during construction, and the introduced delegate exposes
it as an `IMethod` afterwards. Whether an aspect needs to hold the eventual `IMethod` while the type is still being
built is not known. It would matter to an aspect that introduces a delegate and, in the same `BuildAspect`, writes
a template that calls it.

What would settle it is one sample aspect that introduces a delegate, an event of that type and a method that
raises the event, written against the interface above. That aspect is worth writing before the interface is
frozen, because it is the pattern the issue exists for.

### 7.2. Is a `ref` return or a pointer parameter in scope?

A delegate may return by reference and may take a pointer parameter in an unsafe context.
`IMethodBuilder.ReturnType` and `AddParameter` accept a `RefKind`, so the interface expresses both, and whether the
emission path handles them is an implementation question rather than a design one.

What would settle it is a test of each. Neither is a reason to change the interface, because the alternative
would be to refuse a signature that `IMethodBuilder` can already express.

## 8. References

- [`introducing-types.md`](introducing-types.md), sections 2, 3 and 5.
- [`introducing-enums.md`](introducing-enums.md), section 6.1, which this document contrasts with in section 6.2.
- [`type-facets.md`](type-facets.md), section 2.2 and implementation guideline 5.
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
