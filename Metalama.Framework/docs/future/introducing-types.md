# Introducing types

This document proposes the addition of five capabilities to the advice interface: introducing a struct, an enum, a
delegate, a record and a union. It carries the decisions that the five are subject to together, and the design of
each kind is a document of its own, listed in section 7. It is a design proposal. Nothing described here is
implemented.

The companion document is [`type-facets.md`](type-facets.md), which designs the read side of the same five kinds.
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

A record, a struct and a union declare members that an aspect can introduce. An enum and a delegate do not: the
members of an enum are its own members and nothing else may be added, and a delegate declaration has no member
list at all. The builders therefore derive from two different bases.

| Kind | Builder | Base |
| --- | --- | --- |
| Struct | none, `INamedTypeBuilder` is used directly | `INamedTypeBuilder` |
| Record | `IRecordBuilder` | `INamedTypeBuilder` |
| Union | `IUnionBuilder` | `INamedTypeBuilder` |
| Enum | `IEnumBuilder` | `IMemberOrNamedTypeBuilder` |
| Delegate | `IDelegateBuilder` | `IMemberOrNamedTypeBuilder` |

Section 2.4 of [`type-facets.md`](type-facets.md) decided that all four builders derive from `INamedTypeBuilder`
and that the inapplicable inherited operations throw, after the precedent of `IExtensionBlockBuilder`. That
decision is kept for the record and the union and is revised for the enum and the delegate. Three findings support
the revision, and each of them answers an alternative that a reader is likely to propose.

### 2.1. `IMemberBuilder` cannot serve

The first proposal a reader makes is that a builder for a kind without members should derive from `IMemberBuilder`
rather than from `INamedTypeBuilder`. It cannot, because `IMemberBuilder` derives from `IMember`, which narrows the
declaring type to a value that is not nullable:

```csharp
// Metalama.Framework/Code/IMemberOrNamedType.cs:49
INamedType? DeclaringType { get; }

// Metalama.Framework/Code/IMember.cs:57
new INamedType DeclaringType { get; }
```

An enum or a delegate declared in a namespace has no declaring type, so it cannot satisfy the second declaration.
`IMemberBuilder` would also contribute `IsVirtual` and `IsExtern`, which apply to a type less than the members it
would remove.

`IMemberOrNamedTypeBuilder` is the common base of `IMemberBuilder` and `INamedTypeBuilder`, it keeps the nullable
declaring type, and it is the base this document takes.

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

### 2.3. The restriction is reduced and not removed

`IMemberOrNamedTypeBuilder` declares six settable members: `Accessibility`, `Name`, `IsStatic`, `IsSealed`,
`IsAbstract` and `IsPartial`. Two of them apply to an enum and to a delegate, and four do not, so four setters
still throw. The choice does not produce a base on which every inherited member is valid, and no base in the
hierarchy would.

What it removes is larger than what it leaves. Compared with `INamedTypeBuilder`, the enum builder and the delegate
builder no longer inherit `BaseType`, `AddTypeParameter`, `IsClosed`, and the whole of `INamedType`, which is the
member collections, the implemented interfaces, `PrimaryConstructor`, `ExtensionBlocks`, `MakeGenericInstance` and
the type construction methods. That is approximately thirty members against the four that remain.

### 2.4. The consequence: the builder is not a type

`INamedTypeBuilder` derives from `INamedType`, and `IMemberOrNamedTypeBuilder` does not. An `IEnumBuilder` and an
`IDelegateBuilder` are therefore not types, and neither can be passed where an `IType` is expected. An aspect that
needs the introduced type reads it from the result of the advice:

```csharp
var result = builder.IntroduceEnum( "Color" );
var enumType = result.Declaration;
```

This costs nothing in the implementation. The engine class still derives from `NamedTypeBuilder` and still
implements `INamedTypeImpl`, because the compilation model requires it, so a builder nested inside it, such as the
`Invoke` method builder of a delegate, resolves a declaring type that is not null in the ordinary way. The public
interface is a narrowed view of an object that is a named type internally.

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

Two exceptions are deliberate. Section 3 of [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md) decides that the
advices that a union declaration cannot carry are refused with a clear diagnostic, because the compiler reports
those errors on generated code that the user cannot edit. And an operation whose result could not be expressed at
all, rather than expressed and rejected, throws, which is what section 4 of each document lists.

The practical test is whether the operation has a syntax to produce. Setting a base type on a struct has none, so
it throws. Introducing a ref struct as the type of a field has one, so it is generated and the compiler judges
it.

## 4. The emission machinery that the five kinds share

Four changes are common to the five documents, and the struct document owns them because it is the first of the
five to be implemented.

An enum declaration and a delegate declaration are not type declarations in the Roslyn syntax model.
`EnumDeclarationSyntax` and `DelegateDeclarationSyntax` derive from `BaseTypeDeclarationSyntax` and from
`MemberDeclarationSyntax` respectively, and neither derives from `TypeDeclarationSyntax`. The cast on the first arm
of the switch quoted in section 1, and the type of the local variable it produces, therefore widen to
`MemberDeclarationSyntax`. The code that wraps the declaration in a namespace already accepts that wider type.

`ModifierHelper.GetTypeSyntaxModifierList` gains the `record` modifier. The `enum` and `delegate` keywords are not
modifiers and belong to the syntax factory call of their arm.

The design-time generator produces the partial type for the editor, and
`DesignTimeSyntaxTreeGenerator.CreatePartialType` already has arms for a record class and a record struct. Those
arms serve the reading of a type that the user wrote. Introducing a type of a new kind requires the same arms on
the introduction path, and the record document states which of the two paths each arm serves, because the two are
easy to mistake for each other.

Every new builder follows the freeze pattern that [`../compilation-model.md`](../compilation-model.md) describes: a
mutable builder is handed to the aspect, is frozen at the end of the advice, and is snapshotted into an immutable
builder data object that the compilation model stores.

## 5. Facets, on a builder and on an introduced type

A builder and an introduced type answer `Facets` differently, and the difference is the subject of this section.

### 5.1. A builder throws

`INamedType.Facets` throws a `NotSupportedException` on a type builder. A builder describes a type that is being
constructed, whose members are not resolvable, so it has no structure to report and reporting an empty structure
would be a false answer rather than an incomplete one. An aspect that reads the facet of a builder has made a
mistake, and the exception says so at the place the mistake was made.

This reverses implementation guideline 5 of [`type-facets.md`](type-facets.md), which states that the
implementations of `INamedType` that back a builder return the empty collection rather than throwing. That
guideline is superseded, and the one site that implements it,
`Metalama.Framework.Engine/CodeModel/Introductions/Builders/NamedTypeBuilder.cs:336`, changes with it. The
precedent for throwing is on the same class: `ExtensionBlocks`, a few lines above, already throws, and the comment
that currently sits above `Facets` exists only to say that `Facets` does not follow it.

The flags do not throw, and that distinction is what makes the change safe. `IsDelegate`, `IsEnum`, `IsRecord` and
`IsUnion` are Boolean properties that a builder answers from its own kind without allocating anything, and section
4.4 of [`type-facets.md`](type-facets.md) declares them for exactly this reason. A caller that asks what kind a type
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
exist as a builder. That is the reason the record is the largest of the five: an `IRecordFacet` names six members
that the compiler synthesizes, and each of them has to be materialized. The precedent is
`IntroduceNamedTypeAdvice.IntroduceImplicitConstructorIfNeeded`, which already materializes the implicit
constructor of an introduced class for exactly this reason.

A member that the compiler synthesizes is materialized in the code model and is not emitted as syntax. Metalama
generates the declaration of the type, which is the `record` or the `union` keyword and the header that follows it,
and the compiler synthesizes the members from that declaration exactly as it does for a type the user wrote.
Emitting them as well would declare them twice. The transformation that registers a builder without injecting a
member is therefore a shape of its own, which story S-29 describes and models on the introduction of a namespace.
This is the one point at which the code model and the generated code deliberately differ, and every document of
this set states it for its own kind, because reading it the other way produces a type whose members are declared
twice.

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

This table supersedes section 6.2 of [`type-facets.md`](type-facets.md), which does not include the union and which
records the superseded hierarchy decision.

| Issue | Content | Blocked by |
| --- | --- | --- |
| [#869](https://github.com/metalama/Metalama/issues/869) | Introduce a struct. It adds no builder interface, because a struct has no facet, and it carries the machinery of section 4 that the other four consume. | nothing |
| [#866](https://github.com/metalama/Metalama/issues/866) | Introduce an enum, with `IEnumBuilder`. | #869 |
| [#865](https://github.com/metalama/Metalama/issues/865) | Introduce a delegate, with `IDelegateBuilder`. | #869 |
| [#867](https://github.com/metalama/Metalama/issues/867) | Introduce a record class and a record struct, with `IRecordBuilder`. | #869 |
| [#1951](https://github.com/metalama/Metalama/issues/1951) | Introduce a union, with `IUnionBuilder`. This is user story S-29, narrowed to its first half: adding a case to a union that already exists is not supported, which section 6.5 of [`introducing-unions.md`](introducing-unions.md) decides. | #869, [#1941](https://github.com/metalama/Metalama/issues/1941), [#1945](https://github.com/metalama/Metalama/issues/1945) |

The struct is first because it carries the machinery, and because it is the only one of the five whose design adds
no public interface, so it exercises the emission path alone. The enum and the delegate are independent of each
other and either may follow. The record is the last of the four issues, because it is the one that materializes
synthesized members.

The facet issues of section 6.1 of [`type-facets.md`](type-facets.md) are delivered, so no document of this set is
blocked by one.

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
application programming interface because section 11 of [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md) forbids
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
| [#1954](https://github.com/metalama/Metalama/issues/1954) | Documentation: internal architecture documents. User story S-26. | It names S-28, S-29 and S-30 as blockers, so the sections it writes about an introduction interface follow the stories of this set. Its dependency on S-30 lapses, because section 6.5 of [`introducing-unions.md`](introducing-unions.md) withdraws that story. |

The conceptual documentation of `metalama/Metalama.Documentation` carries a note that support for structs,
delegates and enums will be added in a future release. Whichever of the five ships last removes it, and user story
S-27, which is the conceptual documentation of C# 15, names S-29 among its blockers.

## 9. References

- [`type-facets.md`](type-facets.md), sections 2.4, 5 and 6.2.
- [`../compilation-model.md`](../compilation-model.md), the builder and builder data freeze pattern.
- [`../2027.0/DECISIONS.md`](../2027.0/DECISIONS.md), sections 4 and 11.
- [`../2027.0/user-stories/S-29-introduce-union-and-case-attribute-form.md`](../2027.0/user-stories/S-29-introduce-union-and-case-attribute-form.md),
  whose first half is designed here, and
  [`../2027.0/user-stories/S-30-introduce-case-into-union-declaration.md`](../2027.0/user-stories/S-30-introduce-case-into-union-declaration.md),
  which is withdrawn.
- [`../2027.0/analysis-reports/10-introducing-closed-and-unions.md`](../2027.0/analysis-reports/10-introducing-closed-and-unions.md),
  the audit of what can be introduced today.

— Claude for @gfraiteur
