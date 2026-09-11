# Introducing a delegate

This document describes the introduction of a delegate, which is issue
[#865](https://github.com/metalama/Metalama/issues/865), and which is implemented.

The cross-cutting decisions are in [`introducing-types.md`](introducing-types.md), which this document does not
repeat. A delegate declaration is a method signature with the `delegate` keyword in front of it, so
`IDelegateBuilder` declares the members of a signature. It derives from `IMemberOrNamedTypeBuilder` and not from
`IMethodBuilder`, for the reason that section 6.1 gives.

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

The introduced delegate is read from `handler.Declaration`, which is an `INamedType`, and is used there as the type
of an introduced field.

A generic delegate adds type parameters, and may give them variance:

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

A delegate cannot be partial, and the design-time path reaches it by two different routes.

A nested delegate is emitted by `IntroduceNamedTypeTransformation.GetInjectedMembers` as a member of the partial
wrapper of its containing type, and `DesignTimeSyntaxTreeGenerator.AddPartialModifierToTypes` then adds `partial`
to the members it recognises. That method tests the syntax kind and falls through unchanged for anything that is
not a class, a struct, an interface or a record, so a nested introduced delegate works untouched and correctly
receives no `partial` modifier.

A top-level delegate is routed by `ProcessTransformationsOnNamespace` into `ProcessTransformationsOnType`, which
re-creates the declaration through `CreatePartialType`. That method returns a `TypeDeclarationSyntax` and emits the
`partial` modifier, so a delegate reaching it falls to the default arm of its switch and throws. Skipping the
routing would make the delegate vanish from the editor rather than crash, which is not better, so
`ProcessTransformationsOnType` emits the declaration from the introduction transformation for a kind that cannot be
partial, and `CreatePartialType` gains no arm. The transformation wraps a top-level type in its namespace, which
the caller adds as well, so the wrapper is removed.

Section 4.1 of [`introducing-types.md`](introducing-types.md) is corrected to match.

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
/// This interface derives from <see cref="IMemberOrNamedTypeBuilder"/> and declares the members of a signature
/// itself. A delegate declaration is a method signature with the <c>delegate</c> keyword in front of it, so the
/// return type, the return parameter and the parameters describe the <c>Invoke</c> method that the compiler
/// synthesizes, while the name, the accessibility, the custom attributes and the type parameters describe the
/// delegate type.
/// </para>
/// <para>
/// The following inherited properties are not valid for a delegate, and their setter throws a
/// <see cref="NotSupportedException"/>: <see cref="IMemberOrNamedTypeBuilder.IsStatic"/>,
/// <see cref="IMemberOrNamedTypeBuilder.IsSealed"/>, <see cref="IMemberOrNamedTypeBuilder.IsAbstract"/> and
/// <see cref="IMemberOrNamedTypeBuilder.IsPartial"/>. A delegate is implicitly sealed, and it is never static,
/// abstract or partial.
/// </para>
/// <para>
/// A delegate builder is not an <see cref="INamedType"/>, so it may not be used where an <see cref="IType"/> is
/// expected. The introduced delegate is read from
/// <see cref="Metalama.Framework.Advising.IIntroductionAdviceResult{T}.Declaration"/>, which is how an aspect gives
/// it as the type of a field, of a property or of an event.
/// </para>
/// <para>
/// The <c>BeginInvoke</c> and <c>EndInvoke</c> methods are neither exposed nor built. They exist only for a
/// delegate compiled for .NET Framework, and they are the asynchronous pattern that preceded <c>async</c>.
/// </para>
/// </remarks>
/// <seealso cref="Metalama.Framework.Code.Types.IDelegateFacet"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IDelegateBuilder : IMemberOrNamedTypeBuilder
{
    IType ReturnType { get; set; }

    IParameterBuilder ReturnParameter { get; }

    IParameterBuilderList Parameters { get; }

    ITypeParameterList TypeParameters { get; }

    IParameterBuilder AddParameter( string name, IType type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = default );

    IParameterBuilder AddParameter( string name, Type type, RefKind refKind = RefKind.None, TypedConstant? defaultValue = null );

    ITypeParameterBuilder AddTypeParameter( string name );
}
```

The seven members above are the whole surface. The engine class `DelegateBuilder` derives from `NamedTypeBuilder`,
because the compilation model requires an `INamedTypeImpl`, and owns an internal `MethodBuilder` named `Invoke` in
the way `ExtensionBlockBuilder` owns its receiver parameter builder: created in the constructor, frozen in
`FreezeChildren`, and registered as its own transformation. `ReturnType`, `ReturnParameter`, `Parameters` and the
two `AddParameter` overloads forward to it, so no logic is duplicated. `AddTypeParameter` and `TypeParameters` go
to the type, which is where the language places them.

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
/// <param name="buildDelegate">An optional callback that allows you to configure the introduced delegate, which
///     means its accessibility, its custom attributes, its type parameters and its signature. A delegate that the
///     callback leaves unconfigured is internal, returns <c>void</c> and takes no parameter.</param>
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
///     represents the introduced delegate. Its signature is given through this callback.</param>
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

## 4. The operations, and which of them are not valid

`IDelegateBuilder` declares the seven members of section 3.1 and inherits `IMemberOrNamedTypeBuilder`. Everything
it carries describes the delegate, and the three members that describe a signature describe the signature of its
`Invoke` method.

| Member | State |
| --- | --- |
| `ReturnType`, `ReturnParameter` | Valid. `ReturnParameter` also carries the reference kind of a `ref` return and the custom attributes of the return value. |
| `Parameters`, `AddParameter` | Valid. These are the parameters of the delegate. |
| `Name` | Valid. It is the name of the delegate, which the advice method also takes. |
| `Accessibility` | Valid. It is the accessibility of the delegate. |
| `TypeParameters`, `AddTypeParameter` | Valid. The type parameters belong to the delegate, and a delegate is one of the two kinds of type whose type parameters may declare variance. |
| `AddAttribute`, `AddAttributes`, `RemoveAttributes` | Valid. The attributes are those of the delegate. An attribute on the return value is added through `ReturnParameter`. |
| `IsStatic`, `IsSealed`, `IsAbstract`, `IsPartial` | The setter throws a `NotSupportedException`. A delegate is implicitly sealed, and it is never static, abstract or partial. |

`InsertParameter` is not exposed. It exists on `IMethodBuilder` so that an aspect can place a parameter among those
a template already declares, and a delegate has no template: its parameters are added in order by the aspect that
declares them.

Setting the reference kind of a return parameter is refused for every other kind of declaration, because the
template decides it and Metalama does not introduce a method that returns by reference. A delegate has no template,
so `ParameterBuilder` allows it there, and `IntroduceNamedTypeTransformation` emits the `ref` and `ref readonly`
modifiers before the return type, which `ContextualSyntaxGenerator.ReturnType` does not do.

## 5. What the facet of the introduced delegate reports

A delegate builder declares no `Facets` member, because `IDelegateBuilder` does not derive from `INamedType`. The
engine class does, and its `Facets` property throws a `NotSupportedException`, which section 5.1 of
[`introducing-types.md`](introducing-types.md) states for every builder. The kind is read from `IsDelegate`, which
answers without allocating and does not throw.

That no object reachable during construction is usable as a finished type bounds the risk of the exception. Section
5.1 lists `EventBuilder.Signature` and the eligibility rule of `AdviceKind.OverrideEventInvoke` as the readers that
take the type of an event from the aspect. A delegate builder cannot be given as the type of an event, because it
is not an `IType` at all. Neither reader meets a builder in the course an aspect actually takes.

The introduced type reports an `IDelegateFacet`.

| `IDelegateFacet` member | Source |
| --- | --- |
| `InvokeMethod` | The `Invoke` method, which is what `IDelegateBuilder` built, materialized so that the facet can name it without re-reading the model from Roslyn. |
| `ReturnType` | The return type of that method. |
| `Parameters` | The parameters of that method. |
| `FacetKind` | `TypeFacetKind.Delegate`. |
| `Type` | The introduced type. |

`INamedType.IsDelegate` reports `true`, and `INamedType.Methods` contains the `Invoke` method, which is what it
contains for a delegate read from source.

Materialized means present in the code model and not emitted as syntax. Metalama generates the delegate
declaration, which is the `delegate` keyword and the signature, and the compiler synthesizes the `Invoke` method
and the constructor from it exactly as it does for a delegate the user wrote. A transformation that injected the
`Invoke` method as well would declare it twice, and a delegate declaration has no member list to put it in at all,
so the failure would be a syntax error rather than a duplicate member. Section 4.2 of
[`introducing-types.md`](introducing-types.md) states the rule and the mechanism.

`DelegateFacet` resolves the `Invoke` method by name, through `this.Type.Methods.OfName( "Invoke" ).Single()`.
That works for an introduced delegate in any compilation to which the transformation that registers the method has
been applied, and not in one to which it has not, because the member collections of `IntroducedNamedType` come from
the compilation model rather than from the builder data. An aspect that types an event by a delegate it has just
introduced meets the second case: the event builder belongs to the compilation the aspect sees, and the advice
writes to a later one, so the method is absent there and `Single` throws.

The facet therefore takes the method from the builder data for an introduced delegate, which answers in every
compilation that knows the type. `NamedTypeBuilderData` records the `Invoke` method as a reference for that
purpose, and `EnumFacet` takes the members of an introduced enum the same way for the same reason.

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

### 6.1. The builder declares the signature and does not derive from `IMethodBuilder`

An earlier revision of this document specified `IDelegateBuilder : IMethodBuilder`, with the inherited `Name`
carrying the name of the delegate. That shape cannot be implemented, and the obstacle is specific rather than
aesthetic.

`DelegateFacet` resolves the `Invoke` method by the literal name `Invoke`, through
`this.Type.Methods.OfName( "Invoke" ).Single()`, and that literal is the point of issue
[#1995](https://github.com/metalama/Metalama/issues/1995), which made this class the single site of the code model
that resolves it. The name a builder contributes to the code model is the one that
`NamedDeclarationBuilderData` snapshots off it when it is frozen. If the builder is the `Invoke` method and its
`Name` is the name of the delegate, the snapshot carries the name of the delegate, `OfName( "Invoke" )` matches
nothing, and `Single` throws for every introduced delegate. Section 5 of the earlier revision claimed that
`DelegateFacet` needed no change, which holds only if the method really is named `Invoke`.

Keeping the base and naming the method `Invoke` would mean that `IDelegateBuilder.Name` reports `Invoke` rather
than the name of the delegate, so an author could not rename the delegate and the property would contradict every
other builder in the namespace.

The shape that ships keeps the ergonomics and drops the contradiction. `IDelegateBuilder` derives from
`IMemberOrNamedTypeBuilder` and declares the five signature members itself; the engine class owns a `MethodBuilder`
named `Invoke` and forwards to it. The five declarations are the whole cost, `Name` is the name of the delegate
with no conflict, and `DelegateFacet` resolves the method under the name the language gives it.

This also makes the delegate consistent with the other four kinds. Every engine builder in this set derives from
`NamedTypeBuilder`, and its public interface is a narrowed view of it: `IEnumBuilder` likewise does not derive from
`INamedType`, while `EnumBuilder` implements the `INamedTypeImpl` that the compilation model requires.

The alternative of exposing an `InvokeMethod` property typed as `IMethodBuilder` is rejected. It gives the same
operations through one more indirection, and it leaves the builder of the type carrying a name and an accessibility
beside a method that has its own.

### 6.2. The members of the builder describe the delegate, and the `Invoke` method is not exposed

`Name`, `Accessibility`, `AddAttribute` and `AddTypeParameter` describe the delegate, and `ReturnType`,
`ReturnParameter`, `Parameters` and `AddParameter` describe its signature. One object carries both, and the two can
never disagree, because the `Invoke` method has exactly one possible name, accessibility and attribute set: the
language names it `Invoke`, makes it public, gives it no attribute and forbids it to be generic.

The cost of the shape is that five members are declared on `IDelegateBuilder` that `IMethodBuilder` also declares.
That is accepted. It buys an interface whose every member is valid, which is what distinguishes it from the two
alternatives: a builder deriving from `IMethodBuilder` inherits `IsVirtual`, `IsExtern`, `IsReadOnly`,
`OperatorKind`, `DeclarationKind`, `Definition`, `ToRef`, the conversion to a reflection object and the invoker,
none of which describes a delegate, and a builder exposing an `InvokeMethod` property carries a second object whose
name, accessibility and modifiers are all invalid.

An aspect that copies a signature from an existing method transfers it parameter by parameter through
`AddParameter`, which is what it would do through `IMethodBuilder` as well, because there is no operation that
copies a signature wholesale.

The `Invoke` method is not exposed by the builder at all. The introduced model reports it:
`IDelegateFacet.InvokeMethod.Name` is `Invoke`, as it is for a delegate read from source, and a unit test pins
that.

### 6.3. The type parameters belong to the delegate

The language places the type parameters of a delegate on the type: `Func<T, TResult>` is a generic type whose
`Invoke` method is not generic. `AddTypeParameter` therefore adds them to the delegate and not to the owned method,
and there is no operation that adds one to the `Invoke` method, because none is valid.

The consequence that the documentation states is that a delegate is the one kind of type, besides an interface,
whose type parameters may declare variance, so `ITypeParameterBuilder.Variance` is meaningful here and nowhere
else among the five kinds.

### 6.4. A delegate is introduced by its own advice method

The reasoning is that of section 6.2 of [`introducing-structs.md`](introducing-structs.md). A delegate accepts a
configuration that no other kind accepts, which is a signature, and it accepts almost nothing that the other kinds
accept.

## 7. Open questions

None.

Whether the type-level members should describe the delegate or the `Invoke` method was open in an earlier revision
and is answered by section 6.2. A `ref` return is in scope: a delegate may return by reference,
`ReturnParameter.RefKind` expresses it, and a test covers it rather than refusing it.

## 8. References

- [`introducing-types.md`](introducing-types.md), sections 2, 3 and 5.
- [`introducing-enums.md`](introducing-enums.md), section 6.1, which this document contrasts with in section 6.2.
- [`type-facets.md`](future/type-facets.md), section 2.2. Its implementation guideline 5, which makes a builder return
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
