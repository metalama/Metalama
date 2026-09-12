# Introducing types

This document describes five capabilities of the advice interface: introducing a struct, an enum, a delegate, a
record and a union. It carries the decisions that the five are subject to together, and each kind is described by a
document of its own, listed in section 7. All five are implemented.

The companion document is [`type-facets.md`](future/type-facets.md), which designs the read side of the same five kinds.
Section 2.4 of that document decided the shape of the builder interfaces, and section 2 below revises that decision
for the enum and the delegate.

## 1. What can be introduced today

An aspect can introduce a class, an interface and an extension block. It can introduce nothing else, and the four
places that refuse the other kinds are the following.

The transformation that emits the declaration switches on three kinds and throws for the rest:

```csharp
// Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceNamedTypeTransformation.cs:61
var type =
    (this.BuilderData.TypeKind switch
    {
        TypeKind.Class => (TypeDeclarationSyntax) ClassDeclaration( /* ... */ ),
        TypeKind.Struct => StructDeclaration( /* ... */ ),
        TypeKind.Interface => InterfaceDeclaration( /* ... */ ),
        _ => throw new AssertionFailedException( /* unsupported type kind */ )
    }).NormalizeWhitespaceIfNecessary( context.SyntaxGenerationContext );
```

The `TypeKind.Struct` arm is reachable code that nothing reaches, because no public advice method constructs a
builder of that kind. `AdviceFactory.IntroduceClass` passes `TypeKind.Class` and `AdviceFactory.IntroduceInterface`
passes `TypeKind.Interface`, and there is no third caller.

The builder refuses the other kinds before the transformation is reached:

```csharp
// Metalama.Framework.Engine/CodeModel/Introductions/Builders/NamedTypeBuilder.cs:186
Invariant.Assert( typeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface or TypeKind.Extension );
Invariant.Assert( !isRecord ); // Introducing records is not yet supported.
```

The modifier emission has no token for the kinds that need one.
`ModifierHelper.GetTypeSyntaxModifierList`, at line 198 of the same file, emits the accessibility, `static`, `new`,
`closed`, `abstract`, `sealed`, `partial` and `unsafe`, and nothing else. The `record` modifier and the `enum` and
`delegate` keywords have no source there.

The introduced type reports no facet:

```csharp
// Metalama.Framework.Engine/CodeModel/Introductions/Introduced/IntroducedNamedType.cs:182
public ITypeFacetCollection Facets => TypeFacetCollection.Empty;
```

The comment above that line states that the type introduction stories add the facet of the kind that each of them
introduces, and section 5 below is that statement made precise.

What a user sees is the note in the conceptual documentation, in
`Metalama.Documentation/content/conceptual/aspects/advising/introducing-types.md`, which says that support for
structs, delegates and enums will be added in a future release.

## 2. The builder hierarchy, and where it splits

A record, a struct and a union declare members that an aspect can introduce, and their builder is the builder of
the type. An enum and a delegate do not: the members of an enum are its own members and nothing else may be added,
and a delegate declaration has no member list at all. The builders therefore do not share one base, and the
delegate does not even build the same thing as the other four.

| Kind | Builder | Base | What the builder is |
| --- | --- | --- | --- |
| Struct | none, `INamedTypeBuilder` is used directly | `INamedTypeBuilder` | the type |
| Record | `IRecordBuilder` | `INamedTypeBuilder` | the type |
| Union | `IUnionBuilder` | `INamedTypeBuilder` | the type |
| Enum | `IEnumBuilder` | `IMemberOrNamedTypeBuilder` | the type |
| Delegate | `IDelegateBuilder` | `IMethodBuilder` | the declaration, whose shape is a method signature |

Section 2.4 of [`type-facets.md`](future/type-facets.md) decided that all four builders derive from `INamedTypeBuilder`
and that the inapplicable inherited operations throw, after the precedent of `IExtensionBlockBuilder`. That
decision is kept for the record and the union and is revised for the enum and the delegate, for reasons that are
not the same in the two cases. Sections 2.1 to 2.3 give them.

### 2.1. An enum builder cannot derive from `IMemberBuilder`

The first proposal a reader makes is that a builder for a kind without members should derive from `IMemberBuilder`
rather than from `INamedTypeBuilder`. For an enum it cannot, because `IMemberBuilder` derives from `IMember`, which
narrows the declaring type to a value that is not nullable:

```csharp
// Metalama.Framework/Code/IMemberOrNamedType.cs:49
INamedType? DeclaringType { get; }

// Metalama.Framework/Code/IMember.cs:57
new INamedType DeclaringType { get; }
```

An enum declared in a namespace has no declaring type, so it cannot satisfy the second declaration.
`IMemberBuilder` would also contribute `IsVirtual` and `IsExtern`, which apply to a type less than the members it
would remove.

`IMemberOrNamedTypeBuilder` is the common base of `IMemberBuilder` and `INamedTypeBuilder`, it keeps the nullable
declaring type, and it is the base the enum builder takes.

This argument is about the enum and does not carry to the delegate, which section 2.3 settles differently. The
difference is that an enum has no single member that carries its structure, while a delegate does.

### 2.2. The base does not decide whether members can be introduced

The second proposal is that deriving from a base without member operations is what prevents an aspect from adding a
member to an introduced enum. It is not, because the member operations are not on the builder. They are extension
methods on the adviser:

```csharp
// Metalama.Framework/Aspects/AdviserExtensions.cs:106
public static IIntroductionAdviceResult<IMethod> IntroduceMethod(
    this IAdviser<INamedType> adviser, /* ... */ )
```

`IIntroductionAdviceResult<T>` derives from `IAdviser<T>`, which is what makes
`IntroduceClass( "Foo" ).IntroduceMethod( "Bar" )` compile. An introduced enum has to be an `INamedType`, because
an aspect has to be able to use it as a type, so the result of introducing an enum is an
`IIntroductionAdviceResult<INamedType>` and `IntroduceMethod` remains callable on it whatever base the builder has.

Refusing the operation is therefore an advice validation rule, described in section 3, and not a consequence of the
hierarchy. The hierarchy decides what the callback that configures the type can do, and nothing more.

### 2.3. A delegate builder derives from `IMethodBuilder`

A delegate is the one kind of this set whose whole structure is one member. A delegate declaration is a method
signature with the `delegate` keyword in front of it, and `IDelegateFacet` says the same thing on the read side:
it declares `InvokeMethod`, and its `ReturnType` and `Parameters` are that method's.

`IDelegateBuilder` therefore derives from `IMethodBuilder` and declares no member of its own. The obstacle of
section 2.1 does not arise, because `IMember.DeclaringType` reports the delegate type, and a delegate always has
one. The inherited members that describe a declaration rather than a signature, which are `Name`, `Accessibility`,
`AddAttribute` and `AddTypeParameter`, describe the delegate, because the `Invoke` method has no choice about any
of them: the language names it `Invoke`, makes it public, gives it no attribute and forbids it to be generic. A
setter that would otherwise be refused as invalid therefore carries one meaning and not two, and an aspect
configures the delegate on one object. [`introducing-delegates.md`](introducing-delegates.md) carries the design,
the alternatives and the one residue.

### 2.4. The restriction is reduced and not removed

Neither choice produces a base on which every inherited member is valid, and no base in the hierarchy would. What
each one does is make the invalid members few enough to list.

`IMemberOrNamedTypeBuilder` declares six settable members: `Accessibility`, `Name`, `IsStatic`, `IsSealed`,
`IsAbstract` and `IsPartial`. Two of them apply to an enum and four do not, so four setters throw. What the choice
removes is larger: compared with `INamedTypeBuilder`, the enum builder no longer inherits `BaseType`,
`AddTypeParameter`, `IsClosed`, and the whole of `INamedType`, which is the member collections, the implemented
interfaces, `PrimaryConstructor`, `ExtensionBlocks`, `MakeGenericInstance` and the type construction methods. That
is approximately thirty members against the four that remain.

The delegate trades differently, because its base is chosen for what it carries rather than for what it omits.
`IMethodBuilder` gives the return type, the return parameter and the parameter operations, which are the signature,
and the name, the accessibility, the attributes and the type parameters, which are the declaration. That is
everything an author configures on a delegate. What throws is the set of modifiers that a delegate cannot carry,
which is `IsStatic`, `IsSealed`, `IsAbstract`, `IsPartial`, `IsVirtual`, `IsExtern`, `IsReadOnly` and
`OperatorKind`. Section 4 of [`introducing-delegates.md`](introducing-delegates.md) lists them, and the count is
close to the enum's.

### 2.5. The consequence: neither builder is a type

`INamedTypeBuilder` derives from `INamedType`. Neither `IMemberOrNamedTypeBuilder` nor `IMethodBuilder` does, so an
`IEnumBuilder` and an `IDelegateBuilder` are not types and neither can be passed where an `IType` is expected. An
aspect that needs the introduced type reads it from the result of the advice:

```csharp
var result = builder.IntroduceEnum( "Color" );
var enumType = result.Declaration;
```

`IDelegateBuilder.DeclaringType` reports the delegate type and is read-only, so it is an `INamedType` and is a
type in the sense of the compiler. It is not the finished delegate: it is the type under construction, whose
members are not resolvable, so an aspect does not use it as the type of a declaration either, and it has no reason
to reach it at all. The result of the advice is the one object that is safe to use for that.

This costs nothing in the implementation. The engine class behind a type builder derives from `NamedTypeBuilder`
and implements `INamedTypeImpl`, because the compilation model requires it, so a builder nested inside it resolves
a declaring type that is not null in the ordinary way. The public interface is a narrowed view of an object that is
a named type internally.

## 3. What the framework refuses, and what it leaves to the aspect author

### 3.1. Member introduction into an enum or a delegate is refused by advice validation

Section 2.2 establishes that `IntroduceMethod`, `IntroduceField`, `IntroduceProperty`, `IntroduceEvent`,
`IntroduceConstructor`, `IntroduceIndexer` and `ImplementInterface` remain callable on the result of introducing an
enum or a delegate. Each of them reports an error when the target type is one of those two kinds.

The diagnostic states the kind of the target, because an aspect author who reaches this has usually introduced the
wrong kind of type rather than misunderstood the language. The rule belongs with the other target validations of
`AdviceFactory`, beside `ValidateNotExtensionBlock`, which refuses the same operations on an extension block for
the same reason.

An enum does declare members, and they are nevertheless covered by the same rule. They are added through
`IEnumBuilder` while the type is being constructed, and not through the adviser afterwards. The enum document
states why.

### 3.2. The framework does not prevent an aspect from generating invalid code

A builder refuses an operation when the operation cannot be represented, and not when the code it would produce is
invalid for a reason of the language. Setting a base type on a struct is refused, because a struct declaration has
nowhere to put one. Introducing a ref struct as the type of a field of a class is not refused, because the
declaration is representable and the compiler reports the error on it.

The reason is that the aspect author is responsible for the code the aspect generates, and a framework that tried
to reproduce the rules of the language would reproduce a part of them, would be wrong about some of that part, and
would refuse patterns that are valid in a context it did not model. The compiler is the authority on whether the
generated code is valid, and its diagnostic names the generated declaration.

Two exceptions are deliberate. Section 3 of [`../2027.0/DECISIONS.md`](2027.0/DECISIONS.md) decides that the
advices that a union declaration cannot carry are refused with a clear diagnostic, because the compiler reports
those errors on generated code that the user cannot edit. And an operation whose result could not be expressed at
all, rather than expressed and rejected, throws, which is what section 4 of each document lists.

The practical test is whether the operation has a syntax to produce. Setting a base type on a struct has none, so
it throws. Introducing a ref struct as the type of a field has one, so it is generated and the compiler judges
it.

## 4. The emission machinery that the five kinds share

### 4.1. The syntax that Metalama emits

Four changes are common to the five documents, and the struct document owns them because it is the first of the
five to be implemented.

An enum declaration and a delegate declaration are not type declarations in the Roslyn syntax model.
`EnumDeclarationSyntax` and `DelegateDeclarationSyntax` derive from `BaseTypeDeclarationSyntax` and from
`MemberDeclarationSyntax` respectively, and neither derives from `TypeDeclarationSyntax`. The cast on the first arm
of the switch quoted in section 1, and the type of the local variable it produces, therefore widen to
`MemberDeclarationSyntax`. The code that wraps the declaration in a namespace already accepts that wider type.

`ModifierHelper.GetTypeSyntaxModifierList` gains the `record` modifier. The `enum` and `delegate` keywords are not
modifiers and belong to the syntax factory call of their arm.

An introduced type reaches the editor by two different routes, and only one of them was examined when this section
was first written. Both are described here, because this is the point at which an implementer is most likely to go
wrong.

A nested introduced type is emitted by `IntroduceNamedTypeTransformation.GetInjectedMembers`, which is the same
method the build uses, as a member of the generated partial part of its containing type.
`DesignTimeSyntaxTreeGenerator.AddPartialModifierToTypes` then adds `partial` to the members it recognises, which
are a class, a struct, an interface, a record and a union. That method tests the syntax kind and falls through
unchanged for anything else, so a nested introduced enum or delegate works untouched and correctly receives no
`partial` modifier, which is what the language requires of those two kinds.

A nested introduced type that another transformation targets is also the key of a bucket of its own, and that
bucket produces a second generated part. That part is created by `CreatePartialType`, carries the members and is
declared partial, while the part described above carries the declaration. For a union this is what makes an
introduced union able to receive a member at design time, and the division of labour between the two parts is the
one that section 6.4 of [`introducing-unions.md`](introducing-unions.md) describes: exactly one part carries the
case list, and it is the part that the introduction transformation emits.

A top-level introduced type is routed by `ProcessTransformationsOnNamespace` into `ProcessTransformationsOnType`.
Such a type has no declaration outside the generated file, so the file carries the declaration itself rather than a
partial part of it: `ProcessTransformationsOnType` takes the declaration from the introduction transformation and
adds to it the interfaces and the members that other transformations contribute. Re-creating the declaration
through `CreatePartialType`, which an earlier revision of this section proposed for the kinds that can be partial,
is wrong for every kind and not only for the two that cannot, because the transformation is the only place that
knows the modifiers, the base list, the positional parameter list of a record, the case list of a union and the
members of an enum. The transformation wraps a top-level type in its namespace, because at build time it is
injected into a compilation unit, and the caller adds the namespace itself, so the wrapper is removed.

The bucket of a top-level introduced type is moved out of the list of buckets before they are processed and is
handed to the transformation that introduces the type. Left as a bucket of its own it would produce a partial part
with no other part to join, which for a record or a union does not compile at all: a record part carrying no
positional parameter list declares a different record, and a union part carrying no case list is CS9370.

The design-time generator must also skip a transformation that implements `IIntroduceDeclarationTransformation`
without implementing `IInjectMemberTransformation`, which is the mechanism of section 4.2. `LinkerInjectionStep`
skips it at build time by construction, because it enumerates the second interface, while the design-time generator
groups every observable transformation and then throws for one it does not recognise. The implicit parameterless
constructor of an introduced struct is the smallest instance.

Every new builder follows the freeze pattern that [`../compilation-model.md`](compilation-model.md) describes: a
mutable builder is handed to the aspect, is frozen at the end of the advice, and is snapshotted into an immutable
builder data object that the compilation model stores.

### 4.2. A member that the compiler synthesizes enters the code model and is emitted by nothing

This is the decision that the five kinds share and that is easiest to implement backwards, so it is stated once
here and each document names the members it applies to.

Metalama emits the declaration of the type and nothing inside it. The compiler then synthesizes, from that
declaration, exactly what it synthesizes for a type the user wrote: the `Invoke` method of a delegate, the
`EqualityContract` property and the clone method of a record, the `Value` property and the per-case constructors of
a union, the parameterless constructor of a struct. Those members must nevertheless exist in the code model,
because the introduction pipeline never re-reads the final model from Roslyn, so an aspect that reads
`INamedType.Facets` or `INamedType.Methods` on an introduced type would otherwise find nothing.

A member that is registered in the code model and also emitted is declared twice, and the compiler reports the
duplicate on generated code that the user cannot edit.

The two are separate axes in the engine, which is what makes the rule implementable rather than a matter of care.

| Question | What decides it |
| --- | --- |
| Is the member in the code model? | `CompilationModel.AddTransformation` ignores a transformation whose `Observability` is `TransformationObservability.None`, and registers any other. A transformation that implements `IIntroduceDeclarationTransformation` contributes its `DeclarationBuilderData`. |
| Is the member emitted at build time? | `LinkerInjectionStep` collects the transformations that implement `IInjectMemberTransformation` and calls `GetInjectedMembers` on each. |
| Is the member emitted at design time? | `DesignTimeSyntaxTreeGenerator` takes the transformations whose `Observability` is `TransformationObservability.Always` and switches on them; only the arm `case IInjectMemberTransformation` emits anything. |

A synthesized member therefore needs a transformation that implements `IIntroduceDeclarationTransformation`, does
**not** implement `IInjectMemberTransformation`, and whose `Observability` is not `None`. Both emitters are keyed
on the same interface, so not implementing it satisfies both at once, and neither the linker nor the design-time
generator needs a rule of its own.

The precedent exists and is the one that story S-29 names:

```csharp
// Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceNamespaceTransformation.cs:15
internal sealed class IntroduceNamespaceTransformation : BaseTransformation, IIntroduceDeclarationTransformation
{
    public override TransformationObservability Observability => TransformationObservability.Always;

    DeclarationBuilderData IIntroduceDeclarationTransformation.DeclarationBuilderData => this._introducedDeclaration;
}
```

It registers a namespace in the code model and emits nothing, because a namespace has no syntax of its own either.
`Observability.Always` is correct there and here: the value decides whether the transformation reaches the code
model and the design-time pipeline, and not whether syntax is produced, which the interface decides.

The trap is the base class. `IntroduceDeclarationTransformation<T>`, which every existing introduce-declaration
transformation derives from, implements both interfaces:

```csharp
// Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceDeclarationTransformation.cs:17
internal abstract class IntroduceDeclarationTransformation<T> : BaseSyntaxTreeTransformation,
                                                                IIntroduceDeclarationTransformation,
                                                                IInjectMemberTransformation
```

So does `IntroduceNamedTypeAdvice.IntroduceImplicitConstructorIfNeeded`, which materializes the parameterless
constructor of an introduced class and is the precedent a reader reaches for first. It adds a transformation built
on that base, so it emits the constructor as well as registering it. That is correct for a class, whose
parameterless constructor Metalama does declare, and it is not the shape the five kinds need. A document that
cites it as the precedent, as an earlier revision of the record and union documents did, is citing the half of it
that does not carry.

Which members each kind registers without emitting:

| Kind | Registered and not emitted |
| --- | --- |
| Struct | The implicit parameterless constructor. |
| Delegate | The `Invoke` method and the constructor of the delegate. |
| Record | The six members that `IRecordFacet` names, the primary constructor, and `Equals`, `GetHashCode`, `ToString` and the equality operators. |
| Union | The `Value` property and one constructor per case. |
| Enum | None. The members of an enum are written by the aspect author and are part of the declaration that Metalama emits, so they are emitted and registered like any declared member. |

The enum is the exception that shows the rule is about the compiler and not about Metalama: a member is exempt
from emission exactly when the compiler creates it.

Story S-29 asks for this step to be prototyped before the rest of the union work, because whether a member builder
with no injected member survives the linker injection registry was not verified. The prototype answers the
question for all five kinds at once and is worth running before the record work starts as well.

## 5. Facets, on a builder and on an introduced type

A builder and an introduced type answer `Facets` differently, and the difference is the subject of this section.

### 5.1. A builder throws

`INamedType.Facets` throws a `NotSupportedException` on a type builder. A builder describes a type that is being
constructed, whose members are not resolvable, so it has no structure to report and reporting an empty structure
would be a false answer rather than an incomplete one. An aspect that reads the facet of a builder has made a
mistake, and the exception says so at the place the mistake was made.

This reverses implementation guideline 5 of [`type-facets.md`](future/type-facets.md), which states that the
implementations of `INamedType` that back a builder return the empty collection rather than throwing. That
guideline is superseded, and the one site that implements it,
`Metalama.Framework.Engine/CodeModel/Introductions/Builders/NamedTypeBuilder.cs:336`, changes with it. The
precedent for throwing is on the same class: `ExtensionBlocks`, a few lines above, already throws, and the comment
that currently sits above `Facets` exists only to say that `Facets` does not follow it.

The flags do not throw, and that distinction is what makes the change safe. `IsDelegate`, `IsEnum`, `IsRecord` and
`IsUnion` are Boolean properties that a builder answers from its own kind without allocating anything, and section
4.4 of [`type-facets.md`](future/type-facets.md) declares them for exactly this reason. A caller that asks what kind a type
is keeps working on a builder; only a caller that asks for the structure meets the exception.

Three readers have to be checked before the exception is introduced, because each of them reads `Facets` on a type
that an aspect supplies:

- `Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:101` and `:105`, which read
  `e.Type.Facets.Delegate?.ReturnType` and `e.Type.Facets.Delegate?.Parameters` for the eligibility of
  `AdviceKind.OverrideEventInvoke`. These must test `e.Type.IsDelegate` first, so that a type that is not a
  delegate reports that the advice is not eligible, as it does today, instead of throwing. Turning a clean
  ineligibility into an exception would be a regression, and it is the failure that guideline 5 was written to
  prevent.
- `Metalama.Framework.Engine/CodeModel/Introductions/Builders/EventBuilder.cs:62`, which defines `Signature` as
  `this.Type.Facets.Delegate.AssertNotNull().InvokeMethod`. The type of an event is supplied by the aspect through
  `IEventBuilder.Type`.

The hierarchy of section 2 bounds that risk rather than leaving it open. `IEnumBuilder` and `IDelegateBuilder` do
not derive from `INamedType`, so they declare no `Facets` member at all and an aspect cannot pass either of them
where a type is expected. A delegate builder can therefore never be the type of an event. The builders that remain
reachable as a type are those of a class, a struct, a record and a union, and `Facets.Delegate` is meaningless on
all four.

### 5.2. An introduced type reports its facet

`IntroducedNamedType`, which is how the compilation model exposes a type after the transformation is applied,
reports the facet of its kind: a type introduced as an enum reports an `IEnumFacet`, a type introduced as a record
reports an `IRecordFacet`, and so on. Each of the five documents states the mapping member by member. A type
introduced as a class or an interface reports the empty collection, because those kinds have no facet.

The site is `Metalama.Framework.Engine/CodeModel/Introductions/Introduced/IntroducedNamedType.cs:182`, whose
comment already says that the type introduction stories add the facet of the kind that each of them introduces.

The facet of an introduced type is built from the builder data and not from a Roslyn symbol, because the
introduction pipeline never re-reads the final model from Roslyn. Every member that a facet exposes must therefore
exist as a builder, and must not be emitted, which is the decision of section 4.2. That is the reason the record is
the largest of the five: an `IRecordFacet` names six members that the compiler synthesizes, and each of them has to
be registered in the code model while the generated code stays a bare `record` declaration.

### 5.3. The facets are tested by unit tests and not by aspect tests

An aspect test compares generated code against an expected file. It proves that the right declaration was emitted,
and it cannot observe the code model that the pipeline built on the way there, so it cannot assert that an
introduced enum reports an `IEnumFacet` whose `Members` are in declaration order. Each of the five kinds therefore
carries unit tests for its facet, and the aspect tests of the same issue cover the generated syntax alone.

The suite is `Metalama.Framework.Tests.UnitTests/CodeModel/TypeFacetTests.cs` for the shared behaviour, beside
`EnumFacetTests.cs`, `RecordFacetTests.cs` and `UnionTypeTests.cs`, which the facet issues of section 8.2 added.
The pattern for reaching an introduced declaration from a unit test already exists in
`CodeModelUpdateTests.IntroducedTypes.cs` and in `TypeFacetTests.cs` itself:

```csharp
using var testContext = this.CreateTestContext();

var compilation = testContext.CreateCompilationModel( "" ).CreateMutableClone();

var builder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "IntroducedType", TypeKind.Class );
builder.Freeze();
compilation.AddTransformation( builder.CreateTransformation() );

var introducedType = compilation.Types.OfName( "IntroducedType" ).Single();
```

Each kind adds tests of three shapes:

1. Reading `Facets` on the builder throws a `NotSupportedException`, while `IsEnum`, `IsDelegate`, `IsRecord` and
   `IsUnion` answer on the builder without throwing.
2. The introduced type reports the facet of its kind, and every member of that facet holds the value that the
   builder was given. This is the table that each document states in its own section 5.
3. The introduced type and the equivalent type read from source report the same thing, which is the assertion that
   catches a facet built from builder data that diverges from one built from a symbol.

The existing test `TypeFacetTests.FacetsOfBuilderDoNotThrow` pins the behaviour that section 5.1 reverses, and its
documentation comment cites guideline 5. It is replaced rather than deleted: the same test becomes the one that
asserts the exception, and it keeps its assertions that the flags do not throw.

## 6. Order of implementation

### 6.1. The order

This table supersedes section 6.2 of [`type-facets.md`](future/type-facets.md), which does not include the union and which
records the superseded hierarchy decision.

| Issue | Content | Blocked by |
| --- | --- | --- |
| [#869](https://github.com/metalama/Metalama/issues/869) | Introduce a struct. It adds no builder interface, because a struct has no facet, and it carries the machinery of section 4 that the other four consume. | nothing |
| [#866](https://github.com/metalama/Metalama/issues/866) | Introduce an enum, with `IEnumBuilder`. | #869 |
| [#865](https://github.com/metalama/Metalama/issues/865) | Introduce a delegate, with `IDelegateBuilder`. | #869 |
| [#867](https://github.com/metalama/Metalama/issues/867) | Introduce a record class and a record struct, with `IRecordBuilder`. | #869 |
| [#1951](https://github.com/metalama/Metalama/issues/1951) | Introduce a union, with `IUnionBuilder`. This is user story S-29, narrowed twice by sections 6.3 and 6.4 of [`introducing-unions.md`](introducing-unions.md): only the form written with the `union` keyword is introduced, and adding a case to a union that already exists is not supported. The issue is revised before the work starts. | #869, [#1941](https://github.com/metalama/Metalama/issues/1941), [#1945](https://github.com/metalama/Metalama/issues/1945) |

The struct is first because it carries the machinery, and because it is the only one of the five whose design adds
no public interface, so it exercises the emission path alone. The record is the largest, because it registers the
most synthesized members.

The facet issues of section 6.1 of [`type-facets.md`](future/type-facets.md) are delivered, so no document of this set is
blocked by one.

### 6.2. What can be implemented concurrently

The five are not independent, and they are not one piece of work either. The shared engine changes are few, they
are all in the first issue, and once they are merged the remaining four touch mostly disjoint files.

The changes that happen once, whatever the number of kinds, belong to
[#869](https://github.com/metalama/Metalama/issues/869):

| Site | Change |
| --- | --- |
| `NamedTypeBuilder.cs:186` | The assertion that restricts the kind is relaxed to accept every kind this set introduces. |
| `NamedTypeBuilder.cs:336` | `Facets` throws, which is section 5.1. |
| `IntroducedNamedType.cs:182` | `Facets` becomes `TypeFacetCollection.Create( this )`, which is what `SourceNamedTypeImpl` already does. |
| `EligibilityRuleFactory.cs:101` and `:105` | The two rules test `IsDelegate` before reading the facet, which is section 5.1. |
| A new transformation shape | Registers a builder without injecting a member, which is section 4.2. |
| `IntroduceNamedTypeTransformation.cs:65` | The cast and the local widen to `MemberDeclarationSyntax`, which is section 4.1. |

The third row is the one that decides the answer, and it is a single line rather than one line per kind.
`TypeFacetCollection.Create` dispatches on `IsDelegate`, `IsUnion`, `IsRecord` and `IsEnum`, and
`IntroducedNamedType` already declares all four. An introduced type therefore reports the facet of its kind as soon
as its flag is correct, and no later issue edits that site.

What each later issue adds, once those are merged:

| Site | Which issues touch it |
| --- | --- |
| A public builder interface, an engine builder, a builder data type, and the tests | Each kind, in files of its own. |
| The facet implementation, which needs a path that reads the code model rather than a Roslyn symbol | `EnumFacet`, `RecordFacet` and `UnionFacet`, one file each. `DelegateFacet` names no symbol and already works on an introduced type. |
| `IAdviceFactory`, `AdviserExtensions` and `AdviceFactory` | Each kind, appending a method to a different part of each file. |
| The arm of the switch in `IntroduceNamedTypeTransformation` | Each kind. |
| Emitting a top-level introduced type from its own transformation in `DesignTimeSyntaxTreeGenerator.ProcessTransformationsOnType`, and skipping a transformation that injects no member | Every kind. Section 4.1 states why `CreatePartialType` serves the nested route alone. |
| `ModifierHelper.GetTypeSyntaxModifierList` | The record only, for the `record` modifier. |

The recommendation follows from the two tables. One issue first, implemented by one agent in one pull request,
which is [#869](https://github.com/metalama/Metalama/issues/869) and which carries every row of the first table
together with the struct itself. Then the enum, the delegate and the record concurrently, and the union beside them
or after the record.

Three things are worth knowing before that second phase starts.

The textual conflicts are real but mechanical. Two agents adding an arm to the switch of
`IntroduceNamedTypeTransformation` conflict, because the arms are adjacent lines, and so do two agents adding a
field to `NamedTypeBuilderData`. Neither conflict is a design question, and a rebase resolves each of them.

The semantic hazard is the reason the first phase exists. Section 4.2 is the part of this work that is easy to
implement backwards, and an agent that meets it alone will implement it for its own kind. Two agents doing that
independently produce two shapes for the same problem, and the second one to merge has to be rewritten rather than
rebased. The struct issue settles it once, with one synthesized member and no facet to obscure it.

Implementing all five in one pull request is the other way to avoid both, and it is not recommended. It produces a
change of five public builder interfaces, five advice methods, four facet paths and the whole of the machinery
above, which no reviewer can hold at once, and it serialises work that the tables show is mostly parallel. The
first phase is small enough to review closely, which is what it needs, because every later issue is built on it.

## 7. The documents

| Document | Kind | Issue |
| --- | --- | --- |
| [`introducing-structs.md`](introducing-structs.md) | Struct | [#869](https://github.com/metalama/Metalama/issues/869) |
| [`introducing-enums.md`](introducing-enums.md) | Enum | [#866](https://github.com/metalama/Metalama/issues/866) |
| [`introducing-delegates.md`](introducing-delegates.md) | Delegate | [#865](https://github.com/metalama/Metalama/issues/865) |
| [`introducing-records.md`](introducing-records.md) | Record class and record struct | [#867](https://github.com/metalama/Metalama/issues/867) |
| [`introducing-unions.md`](introducing-unions.md) | Union | [#1951](https://github.com/metalama/Metalama/issues/1951) |

## 8. The issues

### 8.1. The issues this set of documents designs

| Issue | Title | State |
| --- | --- | --- |
| [#869](https://github.com/metalama/Metalama/issues/869) | Type introduction: introduce struct | open |
| [#866](https://github.com/metalama/Metalama/issues/866) | Type introduction: introduce enum | open |
| [#865](https://github.com/metalama/Metalama/issues/865) | Type introduction: introduce delegate | open |
| [#867](https://github.com/metalama/Metalama/issues/867) | Type introduction: introduce record struct/class | open |
| [#1951](https://github.com/metalama/Metalama/issues/1951) | C# 15 unions: introducing a union and a case on the attribute form. User story S-29. | open |

The first four are imported issues that carry no body, and this set of documents is the design they lack. The last
is a user story whose body states the capability, the scope and the acceptance criteria, and which states no
application programming interface because section 11 of [`../2027.0/DECISIONS.md`](2027.0/DECISIONS.md) forbids
a story from doing so.

One issue is withdrawn by this set rather than designed by it.
[#1952](https://github.com/metalama/Metalama/issues/1952), user story S-30, which added a case to a type declared
with the `union` keyword, is not implemented: the operation cannot be expressed in a generated partial part, so the
editor and the build would disagree about the result. Section 6.5 of
[`introducing-unions.md`](introducing-unions.md) carries the decision, the workaround for the other authoring form,
and the condition under which it should be revisited. That section also narrows
[#1951](https://github.com/metalama/Metalama/issues/1951), which carried the same operation for a type carrying the
union attribute.

### 8.2. The issues that delivered the read side

Every one of these is closed, and each delivered the facet that the matching document mirrors. No document of this
set is blocked by any of them.

| Issue | Title | Milestone |
| --- | --- | --- |
| [#1995](https://github.com/metalama/Metalama/issues/1995) | Code model: type facets and the delegate facet | 2027.0.2-preview |
| [#1996](https://github.com/metalama/Metalama/issues/1996) | Code model: the enum facet | 2027.0.2-preview |
| [#1997](https://github.com/metalama/Metalama/issues/1997) | Code model: the record facet | 2027.0.2-preview |
| [#1941](https://github.com/metalama/Metalama/issues/1941) | C# 15 unions: code model, which delivered the union facet. User story S-18-1. | 2027.0.2-preview |

### 8.3. The issues that are precedents

| Issue | Why it matters |
| --- | --- |
| [#1950](https://github.com/metalama/Metalama/issues/1950) | C# 15 closed classes: introducing. User story S-28, closed. It is the most recent change to `INamedTypeBuilder` and it is the shape that a pull request of this set should resemble: a settable property, its validation, its storage in the builder data, its exposure on the introduced type and its token in `ModifierHelper`. |
| [#1869](https://github.com/metalama/Metalama/issues/1869) | Cannot introduce a partial class, closed. It added `IsPartial` to the named type builder, so the member set that these documents extend changed in this release. |
| [#1343](https://github.com/metalama/Metalama/issues/1343) | Support `meta.Proceed()` for compiler-synthesized record members, closed. It materializes the synthesized members of a record read from source, which [`introducing-records.md`](introducing-records.md) section 7.2 asks whether an introduced record can reuse. |
| [#1622](https://github.com/metalama/Metalama/issues/1622) | Introduced constructor on introduced type missing at design time, closed. Story S-29 verifies its design-time claim against this fix rather than assuming it. |

### 8.4. The rest of the type introduction backlog

These issues are open, they are not designed by this set of documents, and each of them touches the same area. They
are named so that a reader who implements one of the five knows what is adjacent to it.

| Issue | Title | Relation |
| --- | --- | --- |
| [#868](https://github.com/metalama/Metalama/issues/868) | Type introduction: introduce primary constructor | [`introducing-records.md`](introducing-records.md) materializes the primary constructor of a record, which is part of this issue. The rest of it, which is a primary constructor on any type, stays open and is the subject of the comment `// TODO: Primary constructor handling.` on `INamedTypeBuilder`. |
| [#862](https://github.com/metalama/Metalama/issues/862) | Type introduction: introduced attribute types | An introduced class that derives from `System.Attribute`. Independent of the five kinds. |
| [#863](https://github.com/metalama/Metalama/issues/863) | Type introduction: reflection wrappers and syntax serialization | An introduced type of a new kind reaches the same reflection wrappers, so this issue may gain work from each of the five. |
| [#861](https://github.com/metalama/Metalama/issues/861) | Type introduction: derived fabric on introduced type | Independent. |
| [#860](https://github.com/metalama/Metalama/issues/860) | Type introductions: awaitable and iterable types based on introduced types | Independent. |
| [#912](https://github.com/metalama/Metalama/issues/912) | Introductions: hidden visibility | Independent. |
| [#1998](https://github.com/metalama/Metalama/issues/1998) | Code model: move the tuple type interfaces to the type namespace and add a tuple flag | A tuple is not declared and is not introduced, so it produces no document here. |
| [#1999](https://github.com/metalama/Metalama/issues/1999) | Code model: an invoker on `IMethodBase` | It would let a consumer invoke the creation member of a union case, which [`introducing-unions.md`](introducing-unions.md) needs no more than the reader does. |
| [#1954](https://github.com/metalama/Metalama/issues/1954) | Documentation: internal architecture documents. User story S-26. | It names S-28, S-29 and S-30 as blockers, so the sections it writes about an introduction interface follow the stories of this set. Its dependency on S-30 lapses, because section 6.4 of [`introducing-unions.md`](introducing-unions.md) withdraws that story. |

The conceptual documentation of `metalama/Metalama.Documentation` carries a note that support for structs,
delegates and enums will be added in a future release. Whichever of the five ships last removes it, and user story
S-27, which is the conceptual documentation of C# 15, names S-29 among its blockers.

## 9. References

- [`type-facets.md`](future/type-facets.md), sections 2.4, 5 and 6.2.
- [`../compilation-model.md`](compilation-model.md), the builder and builder data freeze pattern.
- [`../2027.0/DECISIONS.md`](2027.0/DECISIONS.md), sections 4 and 11.
- [`../2027.0/user-stories/S-29-introduce-union-and-case-attribute-form.md`](../2027.0/user-stories/S-29-introduce-union-and-case-attribute-form.md),
  whose first half is designed here, and
  [`../2027.0/user-stories/S-30-introduce-case-into-union-declaration.md`](../2027.0/user-stories/S-30-introduce-case-into-union-declaration.md),
  which is withdrawn.
- [`../2027.0/analysis-reports/10-introducing-closed-and-unions.md`](../2027.0/analysis-reports/10-introducing-closed-and-unions.md),
  the audit of what can be introduced today.

— Claude for @gfraiteur
