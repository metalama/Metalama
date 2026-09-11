# Introducing a delegate

This document designs the introduction of a delegate, which is issue
[#865](https://github.com/metalama/Metalama/issues/865). It is a design proposal. Nothing described here is
implemented.

The cross-cutting decisions are in [`introducing-types.md`](introducing-types.md), which this document does not
repeat. The one that shapes the interface below is section 2.3 of that document: a delegate declaration is a method
signature with the `delegate` keyword in front of it, so `IDelegateBuilder` derives from `IMethodBuilder` and adds
no member of its own.

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
/// A delegate declaration is a method signature with the <c>delegate</c> keyword in front of it, so this interface
/// derives from <see cref="IMethodBuilder"/> and declares no member of its own. The return type, the return
/// parameter and the parameters are the signature of the delegate. The name, the accessibility, the custom
/// attributes and the type parameters are the delegate's, because the <c>Invoke</c> method that the compiler
/// synthesizes has no choice about any of them: it is named <c>Invoke</c>, it is public, it carries no attribute
/// and it is never generic.
/// </para>
/// <para>
/// The following inherited members are not valid for a delegate, and their setter throws a
/// <see cref="NotSupportedException"/>: <see cref="IMemberOrNamedTypeBuilder.IsStatic"/>,
/// <see cref="IMemberOrNamedTypeBuilder.IsSealed"/>, <see cref="IMemberOrNamedTypeBuilder.IsAbstract"/>,
/// <see cref="IMemberOrNamedTypeBuilder.IsPartial"/>, <see cref="IMemberBuilder.IsVirtual"/>,
/// <see cref="IMemberBuilder.IsExtern"/>, <see cref="IMethodBuilder.IsReadOnly"/> and
/// <see cref="IMethodBuilder.OperatorKind"/>. The design document lists them with the reason for each.
/// </para>
/// <para>
/// <see cref="IMember.DeclaringType"/> reports the delegate type and is read-only. It is not how the delegate is
/// configured, and an aspect has no reason to reach it: everything an author chooses is on this interface.
/// </para>
/// <para>
/// This interface is not an <see cref="INamedType"/>, so it may not be used where an <see cref="IType"/> is
/// expected. The introduced delegate is read from
/// <see cref="Metalama.Framework.Advising.IIntroductionAdviceResult{T}.Declaration"/>, which is how an aspect gives
/// it as the type of a field, of a property or of an event.
/// </para>
/// <para>
/// The <c>BeginInvoke</c> and <c>EndInvoke</c> methods are not exposed and are not built. They exist only for a
/// delegate compiled for .NET Framework, and they are the asynchronous pattern that preceded <c>async</c>.
/// </para>
/// </remarks>
/// <seealso cref="Metalama.Framework.Code.Types.IDelegateFacet"/>
/// <seealso href="@introducing-types"/>
[InternalImplement]
public interface IDelegateBuilder : IMethodBuilder;
```

The interface adds no member, in the manner of `IFieldBuilder`, which is also a name for a combination of
interfaces that carries nothing of its own. It exists so that the advice method has a parameter type of its own,
so that the restrictions above have a place to be documented, and so that a member can be added later without a
breaking change.

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

## 4. The inherited operations, and which of them are not valid

`IDelegateBuilder` inherits the whole of `IMethodBuilder` and adds nothing. Most of it is valid. The members that
describe a signature describe the signature of the delegate, and the members that describe a declaration describe
the delegate, because the `Invoke` method has no choice about any of them.

| Member | State |
| --- | --- |
| `ReturnType`, `ReturnParameter` | Valid. `ReturnParameter` also carries the reference kind of a `ref` return and the custom attributes of the return value. |
| `Parameters`, `AddParameter`, `InsertParameter` | Valid. These are the parameters of the delegate. |
| `Name` | Valid. It is the name of the delegate, which the advice method also takes. |
| `Accessibility` | Valid. It is the accessibility of the delegate. |
| `AddTypeParameter` | Valid. The type parameters belong to the delegate, and a delegate is one of the two kinds of type whose type parameters may declare variance. |
| `AddAttribute`, `AddAttributes`, `RemoveAttributes` | Valid. The attributes are those of the delegate. An attribute on the return value is added through `ReturnParameter`. |
| `DeclaringType` | Read-only, and reports the delegate type. Section 6.2 states what it is for. |
| `IsStatic`, `IsSealed`, `IsAbstract`, `IsPartial` | The setter throws a `NotSupportedException`. A delegate is implicitly sealed, and it is never static, abstract or partial. |
| `IsVirtual`, `IsExtern` | The setter throws a `NotSupportedException`. |
| `IsReadOnly` | The setter throws a `NotSupportedException`. It applies to a member of a struct. |
| `OperatorKind` | The setter throws a `NotSupportedException`. |

`IMethod` derives from `IMethodInvoker`, so the builder inherits the operations that generate a call. They are not
usable while the delegate is being built, because the type does not exist yet, and they are usable on the `Invoke`
method that the facet of the introduced type reports. `MethodBuilder`, which backs every introduced method, already
faces that question, so this design adds nothing to it.

## 5. What the facet of the introduced delegate reports

A delegate builder declares no `Facets` member, because `IDelegateBuilder` derives from `IMethodBuilder` and a
method has no facet. `DeclaringType`, which reports the delegate type while it is being built, does declare one,
and it throws a `NotSupportedException`, which section 5.1 of [`introducing-types.md`](introducing-types.md) states
for every builder. The kind is read from `IsDelegate`, which answers without allocating and does not throw.

That no object reachable during construction is usable as a finished type bounds the risk of the exception. Section
5.1 lists `EventBuilder.Signature` and the eligibility rule of `AdviceKind.OverrideEventInvoke` as the readers that
take the type of an event from the aspect. A delegate builder cannot be given as the type of an event, because it
is not an `IType` at all, and an aspect has no reason to reach `DeclaringType`, because the finished delegate is
the result of the advice. Neither reader meets a builder in the course an aspect actually takes.

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

### 6.1. The builder derives from `IMethodBuilder` and adds no member

A delegate declaration is a method signature with the `delegate` keyword in front of it. Everything an author
chooses about it is something an author chooses about a method, so `IDelegateBuilder` derives from `IMethodBuilder`
and declares nothing of its own. The precedent for an interface that is a name for a combination and carries no
member is `IFieldBuilder`.

An earlier revision of this document rejected that base, and the reason it gave does not hold. The reason was that
`IMethodBuilder` reaches `IMember`, whose `DeclaringType` is not nullable, and that a delegate declared in a
namespace has no declaring type. That confuses the delegate with its `Invoke` method. The declaring type of that
method is the delegate, and a delegate always has one, so the obstacle that section 2.1 of
[`introducing-types.md`](introducing-types.md) records against `IMemberBuilder` for an enum does not arise here.
The same revision objected that `IMethod` derives from `IMethodInvoker`. That is not a cost either: generating a
call to the `Invoke` method of a delegate is what `IDelegateFacet` exists for, and the facet documents it.

Two alternatives were weighed and both are rejected.

Declaring the signature members on a builder of the type restates `ReturnType`, `ReturnParameter`, `Parameters`,
`AddParameter` and `InsertParameter` on an interface that is not a method. It carries no member that
`IMethodBuilder` does not already carry, and an aspect that copies a signature from an existing method has to
transfer it member by member instead of using the operations it already knows.

Exposing an `InvokeMethod` property typed as `IMethodBuilder` gives the same operations through one more
indirection, and leaves the builder of the type carrying a name and an accessibility beside a method that has its
own.

### 6.2. The inherited members describe the delegate, and `DeclaringType` is not used

`Name`, `Accessibility`, `AddAttribute` and `AddTypeParameter` describe the delegate and not the `Invoke` method.
The reason they can is that the `Invoke` method has no choice about any of them: the language names it `Invoke`,
makes it public, gives it no attribute and forbids it to be generic. A setter that would otherwise be refused as
invalid therefore carries one meaning and not two.

The alternative, which a revision of this document took, is to define those members as the method's, to refuse
their setter, and to expose `DeclaringType` narrowed to an `INamedTypeBuilder` so that the type can be configured
through it. It is rejected on two grounds.

The first is that it is not more honest, only more indirect. A builder is an authoring object and not a projection
of the code model, and every other builder of the namespace already names the declaration the advice introduces.
`IDelegateBuilder.Name` naming the delegate is what a reader of the other four documents expects.

The second is the cost. Making a delegate public would be
`d.DeclaringType.Accessibility = Accessibility.Public`, and adding a type parameter would be
`d.DeclaringType.AddTypeParameter( "T" )`. That is paid on every delegate an aspect introduces, in exchange for a
distinction that has no consequence, because the two objects can never disagree: the `Invoke` method has exactly
one possible name, accessibility and attribute set.

`DeclaringType` therefore stays as `IMember` declares it, reports the delegate type, and is read-only. The
implementation supplies a read-only named type rather than a second builder, and the configuration flows from the
method builder into it when the builder is frozen. An aspect has no reason to reach it, and the documentation of
the interface says so.

The one residue is that `Name` reports the name of the delegate on an object that is an `IMethod`, whose
`Invoke` method is named `Invoke`. The introduced model does not inherit that: `IDelegateFacet.InvokeMethod.Name`
is `Invoke`, as it is for a delegate read from source. The divergence is confined to the authoring object, and a
unit test pins it.

### 6.3. The type parameters belong to the delegate

The language places the type parameters of a delegate on the type: `Func<T, TResult>` is a generic type whose
`Invoke` method is not generic. `AddTypeParameter` therefore adds them to the delegate, which is section 6.2, and
there is no operation that adds one to the `Invoke` method, because none is valid.

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
and is answered by section 6.2. A `ref` return and a pointer parameter are in scope: a delegate may return by
reference and may take a pointer parameter in an unsafe context, `ReturnParameter` and `AddParameter` express both,
and each is covered by a test rather than refused.

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
