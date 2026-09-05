# 10. Introducing a closed class and introducing a union

Analysis of decision 5 of `Metalama.Framework/docs/2027.0/DECISIONS.md`, written on 2026-09-04 against the branch
`topic/2027.0/26-09-03-update-eng-7e3j07`. Every path is relative to `/home/user/Metalama`. The established facts of
`FACTS.md` and the findings CM-4 and CM-5 of `impact/03-code-model-unions-closed.md` are not re-derived. Claims about
the code carry a file and line reference; claims about the tracker carry an issue number.

## Question

Decision 5 asks two questions that must be answered separately.

1. May an aspect introduce a closed class, that is, may `INamedTypeBuilder` gain a settable `IsClosed` property that
   causes `closed` to be emitted on the introduced class declaration?
2. May an aspect introduce a union, that is, may the advice surface gain an `IntroduceUnion` method that causes a
   `UnionDeclarationSyntax` to be emitted?

The reading half of both features, meaning `INamedType.IsClosed` and `INamedType.IsUnion` on types that the aspect
observes, is already in scope by decision 3 and by findings CM-1 and CM-5. This document concerns only the writing
half.

## Recommendation

1. The closed writer is out of scope for Metalama 2027.0. Size M if it were taken. The reason is not cost. The reason
   is that closedness is a contract over a set of subtypes, an aspect can only assert it over a hierarchy that the
   aspect itself introduces in full, no requested scenario needs that, and on the Roslyn 5.0 variant that serves
   Rider and the C# Dev Kit the `closed` token cannot be emitted at all, so the development environment and the build
   would disagree about exhaustiveness without any diagnostic.
2. The union writer is out of scope for Metalama 2027.0. Size L. It is not blocked by the absence of struct
   introduction, contrary to the reading that issue #869 suggests, because the struct path already exists inside the
   builder and the transformation. It is blocked by two things that have no model today: the mandatory case list, and
   the compiler-synthesized members of an introduced type, which the introduction pipeline must materialize as
   builders rather than discover from Roslyn.
3. Ship instead the reading half only: `INamedType.IsClosed` (CM-5, size S) and `INamedType.IsUnion` with
   `UnionCaseTypes` (CM-1, size M). Record the two writers as new issues in the family of #865, #866, #867 and #869.

## What can be introduced today

The public advice surface offers exactly two type introduction methods. `IAdviceFactory.IntroduceClass` is declared at
`Metalama.Framework/src/Metalama.Framework/Advising/IAdviceFactory.cs:1015` and `IntroduceInterface` at `:1031`; the
adviser extension that aspect authors actually call is
`Metalama.Framework/src/Metalama.Framework/Aspects/AdviserExtensions.cs:1702`. The engine implementations are
`Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:2050-2071` and `:2073-2093`; both construct
an `IntroduceNamedTypeAdvice` and differ only in the `TypeKind` argument, `TypeKind.Class` at `:2068` and
`TypeKind.Interface` at `:2090`. There is no `IntroduceStruct`, no `IntroduceRecord`, no `IntroduceEnum`, no
`IntroduceDelegate` and no `IntroduceUnion`. The claim of CM-4 on this point is confirmed.

The builder rejects more than the advice surface offers, and it rejects less than one might expect.
`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Builders/NamedTypeBuilder.cs:52` asserts
`typeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface or TypeKind.Extension`, and `:53` asserts
`!isRecord` with the comment "Introducing records is not yet supported". The claim of CM-4 that records are rejected by
an assertion is confirmed, and so is the observation that `TypeKind.Struct` is already accepted by the builder even
though no advice method can produce it. `IntroduceNamedTypeTransformation` likewise already emits a struct: the switch
at `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceNamedTypeTransformation.cs:61-92`
has arms for `TypeKind.Class` at `:64`, `TypeKind.Struct` at `:73` and `TypeKind.Interface` at `:82`, and throws an
`AssertionFailedException` at `:91` for anything else. The struct path is therefore reachable dead code, waiting for a
public method.

The modifiers that a type builder exposes are the four of
`Metalama.Framework/src/Metalama.Framework/Code/DeclarationBuilders/IMemberOrNamedTypeBuilder.cs`, namely
`Accessibility` at `:20`, `Name` at `:25`, `IsStatic` at `:30`, `IsSealed` at `:35`, `IsAbstract` at `:40` and
`IsPartial` at `:45`, plus the type specific members of
`Metalama.Framework/src/Metalama.Framework/Code/DeclarationBuilders/INamedTypeBuilder.cs`, namely `IsPartial` at `:18`,
`BaseType` at `:35` and `AddTypeParameter` at `:49`. There is no `IsClosed`, no `IsReadOnly` and no `IsRef`; the last
two are present as commented-out declarations at `:22-30` under the comment "TODO: Struct introduction" at `:20`, and a
primary constructor is a further TODO at `:37-42`. All four settable modifiers are `virtual` on the base class
(`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Builders/MemberOrNamedTypeBuilder.cs:25` for
`IsSealed`, `:75` for `IsAbstract`, `:86` for `IsStatic`, `:97` for `IsPartial`), so a derived builder can override
them to add validation. The precedent for rejecting an invalid combination from a setter is the `BaseType` setter of
`NamedTypeBuilder.cs:107-121`, which throws `InvalidOperationException` at `:116` when an interface is given a base
type.

Two behaviours of the introduction path matter for what follows.

- `IntroduceNamedTypeAdvice.IntroduceImplicitConstructorIfNeeded`
  (`Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceNamedTypeAdvice.cs:104-118`)
  introduces an explicit `ConstructorBuilder` for every introduced non static class, with the comment at `:106-107`
  that this mirrors the constructor Roslyn synthesizes for a source type. This is the established rule of the
  introduction pipeline: a member that the C# compiler synthesizes for an introduced type must be materialized as a
  builder, because the final code model of the compile-time pipeline is built from builders and is never re-read from
  Roslyn. The member collections of the builder itself are all empty (`NamedTypeBuilder.cs:153`, `:165`, `:183`,
  `:191`), and `IntroducedNamedType` fills its collections from the builder registry of the compilation model
  (`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Introduced/IntroducedNamedType.cs:107`,
  `:149`, `:160`).
- The type modifier list is produced by `ModifierHelper.GetTypeSyntaxModifierList`
  (`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/ModifierHelper.cs:198-236`), reached through the
  `DeclarationKind.NamedType` arm of the dispatch at `:50-51`. It emits accessibility at `:207-210`, `static` at
  `:212-215`, `new` at `:219-222`, `abstract` at `:224-227` and `sealed` at `:229-232`. It never emits `partial`:
  `ModifierCategories.Partial` (`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/ModifierCategories.cs:21`)
  is honoured only by the member overload at `ModifierHelper.cs:105-108`. In the design-time pipeline the `partial`
  token is appended afterwards by `AddPartialModifierToTypes`
  (`Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:506-523`),
  which uses `SyntaxTokenList.Add` at `:516` and therefore always places `partial` last.

## The closed writer

### What it would require

The change is mechanical and its parts are all identifiable.

1. A property `bool IsClosed { get; set; }` on `INamedTypeBuilder` (`INamedTypeBuilder.cs`, beside `IsPartial` at
   `:18`). It belongs there and not on `IMemberOrNamedTypeBuilder`, because `closed` applies to classes only.
2. Validation. The setter must reject a `TypeKind` other than `Class` (`NamedTypeBuilder.cs:34` holds the kind), and it
   must reject `IsSealed` and `IsStatic`, because Roslyn reports `ERR_ClosedSealedStatic` for those combinations. The
   validation has to run in both directions, so `NamedTypeBuilder` must also override the `IsSealed` and `IsStatic`
   setters of `MemberOrNamedTypeBuilder.cs:25` and `:86`. The precedent for throwing from a setter is
   `NamedTypeBuilder.cs:116`.
3. The interaction with `IsAbstract`. Roslyn sets the abstract flag implicitly on a closed class and reports
   `ERR_ClosedExplicitlyAbstract` when `abstract` is also written. Mirroring that means `IsAbstract` must report true
   when `IsClosed` is true, which is an override of `MemberOrNamedTypeBuilder.cs:75`, and the setter must reject an
   explicit `IsAbstract = true` on a closed class. The pattern already exists one method away: the constructor of
   `NamedTypeBuilder` sets `IsAbstract = true` for an interface at `:70-75`.
4. Storage. `NamedTypeBuilderData` is the type-specific data class; `IsClosed` goes beside `IsRecord` at
   `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/BuilderData/NamedTypeBuilderData.cs:55`,
   read from the builder in the constructor beside `:46`. It does not belong in `MemberOrNamedTypeBuilderData`, whose
   modifier set is at
   `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/BuilderData/MemberOrNamedTypeBuilderData.cs:26-38`.
5. Exposure. `IntroducedNamedType.IsClosed` beside `IsRecord` at `IntroducedNamedType.cs:200`. Because `IsClosed` must
   be declared on `INamedType` for `ModifierHelper` to read it, the property has to be implemented at every site that
   implements `IsRecord`, which is six: `Metalama.Framework/src/Metalama.Framework/Code/INamedType.cs:202`,
   `NamedTypeBuilder.cs:36`, `NamedTypeBuilderData.cs:55`, `IntroducedNamedType.cs:200`,
   `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Introduced/IntroducedExtensionBlock.cs:190`,
   `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedType.cs:514` and
   `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:173`. This is the same
   work as CM-5 and is shared with the reading half.
6. Token emission. In `ModifierHelper.GetTypeSyntaxModifierList` the condition at `:224` must become
   `namedType.IsAbstract && namedType.TypeKind != TypeKind.Interface && !namedType.IsClosed`, so that `abstract` is
   suppressed, and a `Token(SyntaxKind.ClosedKeyword)` must be added inside the `Inheritance` block at `:217-233`.

### Position of the token, and the variant gate

The position is not a problem. `GetTypeSyntaxModifierList` never produces `partial`, and the design-time pipeline
appends `partial` at the end of the list (`DesignTimeSyntaxTreeGenerator.cs:516`). Emitting `closed` anywhere inside
`:217-233` therefore yields `closed partial class C` at design time and `closed class C` in the compile-time pipeline.
Both satisfy the Roslyn parsing rule that `partial` must immediately precede `class`.

The variant gate is a problem. By decision 2 the C# 15 Roslyn members are reached through preprocessor blocks in the
latest variant only. `SyntaxKind.ClosedKeyword` is one of those members: `FACTS.md` records it as present in the
Roslyn 5.9 assemblies and absent from Roslyn 5.0.0, and the numeric-kind alternative is explicitly rejected by
decision 2. `Metalama.Framework.Engine` is compiled per variant, so the `#if` is available there; but
`Metalama.Framework`, which holds `INamedTypeBuilder`, is not a per-variant assembly, as the variant project list shows
(`Metalama.Framework/src/Metalama.Framework.Engine.5.0.0/`,
`Metalama.Framework/src/Metalama.Framework.DesignTime.5.0.0/` and so on, with no `Metalama.Framework.5.0.0`). The
consequence is that `IsClosed` would be settable from every aspect, and under the Roslyn 5.0 variant, which serves
Rider and the Visual Studio Code C# Dev Kit, the token would silently not be emitted. The design-time partial would
then declare a plain class while the build declares a closed class. That is the worst shape a divergence can take for
this particular feature, because the whole point of `closed` is an exhaustiveness guarantee that the development
environment shows to the user. A diagnostic on the lower variant is required, which is one more piece of work and one
more decision (report an error, or degrade silently).

### Can the derived types exist, and can one aspect introduce them?

Yes to both, and this is verified by existing tests rather than reasoned about.

A closed class is useful only when its subtypes are declared, and the subtypes must be in the same assembly. An
introduced type is always in the current compilation, so the same-assembly rule is satisfied by construction. An aspect
can introduce a type into a type it has just introduced: `Tests/Aspects/Introductions/Classes/Recursive.cs:14-16`
introduces `Test`, then `InnerTest` into it, then `InnerInnerTest` into that. An aspect can set the base type of an
introduced type to another type of the compilation:
`Tests/Aspects/Introductions/Classes/BaseType_Abstract.cs:16` sets `t.BaseType = builder.Target`, and
`Tests/Aspects/Inheritance/IntroducedDerivedType.cs:32` sets it to a nested source type. The Builder sample sets the
base type of an introduced nested class to the corresponding nested class of the base type
(`Tests/Aspects/Samples/Builder.cs:222-230`, with `t.BaseType = baseBuilderType` at `:228`), which in the inheritance
scenario is itself an introduced type, so the introduced-derives-from-introduced case is exercised. One related test is
skipped, `Tests/Aspects/Introductions/Classes/BaseType_SelfReferencing.cs`, but for an unrelated reason recorded in its
header, "constructed generics not supported".

Transformation ordering does not constrain this. Each introduced type becomes an `InjectedMember` and the injected
declarations are merged into syntax trees by `LinkerInjectionStep.Rewriter`; C# imposes no ordering between a base
class declaration and a derived class declaration in the same compilation, so no topological ordering of
transformations is needed. `IntroduceNamedTypeTransformation.GetInjectedMembers` resolves the base type against
`context.FinalCompilation` (`IntroduceNamedTypeTransformation.cs:31`, `:35-38`), which already contains every
introduced type of the run.

One wart is inherited rather than created. `IntroduceImplicitConstructorIfNeeded`
(`IntroduceNamedTypeAdvice.cs:104-118`) gives every introduced non static class a public parameterless constructor,
guarded only on `IsStatic` at `:108` and with `Accessibility.Public` at `:112`. A closed class is implicitly abstract,
and a public constructor on an abstract class is legal C# but is what code quality analyzers flag. The same already
happens for `IsAbstract = true`, as the committed baseline
`Tests/Aspects/Introductions/Classes/Abstract.t.cs` shows, so this is a pre-existing question, not a new cost.

### Size and value

Size M. The property, the validation, the storage, the six `INamedType` implementations, the token, the variant gate,
the lower-variant diagnostic, an aspect test with a committed baseline and a design-time test with committed
`.0.i.cs` output add up to one to two days. The six `INamedType` implementations are shared with CM-5 and would not be
charged twice if the reading half ships.

The user value is thin. The scenario an aspect author would write is the whole hierarchy in one call sequence:

    builder.IntroduceClass( "Shape", buildType: t => t.IsClosed = true );
    builder.IntroduceClass( "Circle", buildType: t => t.BaseType = shape.Declaration );
    builder.IntroduceClass( "Square", buildType: t => t.BaseType = shape.Declaration );

This compiles under the analysis above, and it is the only shape that works, because Metalama offers no advice that
changes the modifiers of an existing source type, so an aspect can never make the user's own class closed. An aspect
that generates a complete hierarchy and wants the compiler to check exhaustiveness at the use sites is a real pattern,
but no issue in the tracker asks for it, and none of the samples in `Tests/Aspects/Samples/` would use it. Against that
thin value stands the design-environment divergence on the Roslyn 5.0 variant described above.

## The union writer

Everything required for the closed writer is also required here, because a union is emitted with the same
transformation, plus four further items that are of a different order.

### The case list has no model

The union grammar carries a mandatory case list in the inherited `ParameterList`. `NamedTypeBuilder` has no parameter
list of any kind: the primary constructor of an introduced type is an unimplemented TODO at
`INamedTypeBuilder.cs:37-42`, and `NamedTypeBuilder.PrimaryConstructor` returns null at `NamedTypeBuilder.cs:188`.
Introducing a union therefore needs a new builder concept, an ordered list of case types, with its own API shape (a
method in the manner of `AddCase( IType type, string? name )`, since a union case may be named), its own storage in
`NamedTypeBuilderData`, its own projection into `IntroducedNamedType`, and its own validation. This is not a property;
it is a new collection on the builder in the manner of `TypeParameters` (`NamedTypeBuilder.cs:40`, `:93-101`), and it
is the first collection of the builder whose elements are types supplied by the aspect rather than declarations owned
by the builder.

### The synthesized members must be materialized as builders

The C# compiler synthesizes, for a union, one public constructor per case type, a `Value` property, and the implicit
`IUnion` interface. The introduction pipeline never re-reads the emitted code, so those members would exist in the
output and not in the code model. The established rule of the pipeline is the opposite, and
`IntroduceImplicitConstructorIfNeeded` (`IntroduceNamedTypeAdvice.cs:104-118`) exists precisely to keep an introduced
class in step with Roslyn on this point. An `IntroduceUnion` advice would have to fabricate one `ConstructorBuilder`
per case plus a `PropertyBuilder` for `Value`, mark them implicitly declared, and ensure the linker does not emit them
a second time. This is exactly the reason record introduction is refused today at `NamedTypeBuilder.cs:53`: a record
has the same class of synthesized members. Union introduction and record introduction (#867) share this blocker, and
solving it once would serve both.

The relation to pull request metalama/Metalama#1879 is a family resemblance rather than a dependency. #1879 is open,
carries milestone 2027.0, targets `develop/2027.0` and changes 128 files; it adds
`Metalama.Framework/src/Metalama.Framework.Engine/Linking/SynthesizedRecordMemberBodyGenerator.cs` and
`Metalama.Framework/src/Metalama.Framework.Engine/Linking/Substitution/SynthesizedRecordMemberSubstitution.cs`, which
generate, from the symbol, the body the compiler would have synthesized, so that an aspect overriding a synthesized
record member can call `meta.Proceed()`. That machinery answers a different question: what an override of an existing
synthesized member proceeds into. It applies to a union that an aspect targets, which decision 3 already puts in
scope, and it would apply to an aspect that overrides the `Value` property of a union it has introduced. It does not
answer the introduction question, which is how a synthesized member of a type that does not yet exist enters the code
model. Union introduction would need the #1879 machinery in addition to the materialization described above, not
instead of it.

### The node kind must be handled in three more places

`IntroduceNamedTypeTransformation` produces the node, and at least three hand-written syntax kind lists downstream
decide what happens to it. None of them mentions a union today, and each fails silently rather than loudly.

- `LinkerInjectionStep.Rewriter.cs:641-642` switches on
  `SyntaxKind.ClassDeclaration or StructDeclaration or InterfaceDeclaration or RecordDeclaration or
  RecordStructDeclaration` to decide whether to inject members into an introduced type (`:647-653`) and whether to add
  its injected interfaces (`:654`, `:684-701`). A `UnionDeclaration` falls through the switch, so members introduced
  into an introduced union would be dropped and its introduced interfaces would not be added. This is silent wrong
  output, and it is specific to the writer; a union in source is unaffected by this site.
- `DesignTimeSyntaxTreeGenerator.AddPartialModifierToTypes` at `:510-511` carries the same list, so the design-time
  copy of an introduced union would not be made partial.
- `SyntaxKindExtensions.IsTypeDeclaration` at
  `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35` is the shared
  list; this one is already counted by CM-2 and CM-6 for the reading half.

The first two are new work that the reading half does not pay for.

### The member restrictions

Decision 3 already requires that advice a union cannot carry, meaning an instance field, an auto-property, a
field-like event, a public single-parameter constructor and a constructor that does not chain, is refused with a clear
diagnostic. For a union that an aspect introduces, the same rules must be enforced on the members introduced into it,
which is the same diagnostic set applied at a second place.

### The variant gate is harder than for `closed`

`closed` needs one token whose `SyntaxKind` is missing from Roslyn 5.0.0. A union needs `SyntaxFactory.UnionDeclaration`
and `UnionDeclarationSyntax`, which are absent from Roslyn 5.0.0 entirely, inside a method,
`IntroduceNamedTypeTransformation.GetInjectedMembers`, whose switch at `:61-92` currently has no preprocessor block at
all. The public `IntroduceUnion` method would sit in the non-variant `Metalama.Framework` assembly and would have to
fail with a diagnostic on the lower variant, in a way that the design-time pipeline of Rider and the C# Dev Kit
presents sensibly.

### Size and value

Size L. The case list model alone is a day; the synthesized member materialization is the same problem that has kept
record introduction unimplemented since 2024; the three kind lists, the diagnostics, the variant gate and the tests
follow.

The user value is the same thin value as the closed writer, one step further out. The scenario would be an aspect that
derives a union from something the aspect already knows, for example a union over the exception types a method
declares, or a result union over a method return type. It is a coherent idea and it is more attractive than the closed
writer, because the aspect owns the whole declaration and does not depend on controlling a subtype set. It is also the
scenario for which no user has asked, in a release where unions as an aspect target are new and unproven.

## The four open issues

All four are open, of issue type User Story, labelled `Imported` and `Area-Framework-Types`, unassigned, with no
milestone and no body text. Each was imported on 2025-07-10 from a TargetProcess ticket created on 2024-04-24 with the
business value recorded as "Average". They are titles without specifications.

| Issue | Title | State | Milestone |
| --- | --- | --- | --- |
| #869 | Type introduction: introduce struct | open | none |
| #867 | Type introduction: introduce record struct/class | open | none |
| #866 | Type introduction: introduce enum | open | none |
| #865 | Type introduction: introduce delegate | open | none |

Whether each is a prerequisite of union introduction:

- #869, introduce struct. Not a technical prerequisite. `NamedTypeBuilder.cs:52` already accepts `TypeKind.Struct` and
  `IntroduceNamedTypeTransformation.cs:73-81` already emits a `StructDeclaration`. What #869 lacks is the public advice
  method and the struct-specific builder properties that are commented out at `INamedTypeBuilder.cs:22-30`. A union is
  a struct in metadata, and it would be introduced through the same `TypeKind.Struct` path with an `IsUnion` flag, so
  the answer to the question posed plainly is: union introduction is not blocked by the absence of struct
  introduction. It is a product-sequencing oddity to ship union introduction before struct introduction, not a
  technical dependency.
- #867, introduce record. The nearest neighbour and the real precedent. A record is refused by the assertion at
  `NamedTypeBuilder.cs:53` for the same reason a union would be: the compiler synthesizes members that the builder
  model does not contain. Whoever solves #867 solves most of the union blocker, and the reverse holds as well.
- #866, introduce enum, and #865, introduce delegate. Unrelated. Neither an enum nor a delegate is a
  `TypeDeclarationSyntax`, both are excluded from `SyntaxKindExtensions.IsTypeDeclaration` at `:33-35` and appear only
  in `IsBaseTypeDeclaration` at `:41`, and neither shares a code path with a union.

Note that none of the four carries a milestone, so none is committed to 2027.0.

## What would settle the uncertainty

The analysis above rests on four points that could not be verified in this session, and on one product judgement.

1. Whether the stable Roslyn 5.12 exposes `SyntaxKind.ClosedKeyword` and `SyntaxFactory.UnionDeclaration` without the
   experimental marker. `FACTS.md` records both as present and experimental in Roslyn 5.9.0, and records that the
   experimental markers are removed on main. Settled by inspecting the stable package once it exists, which is the
   same gate every other C# 15 story waits on.
2. Whether Roslyn accepts a partial type where only one part carries `closed`. This decides whether the design-time
   part of an introduced closed class is correct when a second aspect layer contributes a further part. CM-3 reports
   that Roslyn merges the modifiers of partial parts with a bitwise or, but reports it as plausible rather than
   verified, and records that no Roslyn test with a single `closed` part was found. Settled by a two-part parsing test
   against the stable compiler.
3. Whether the closed-classes rules constrain the accessibility of a closed class constructor. If they do, the public
   parameterless constructor that `IntroduceNamedTypeAdvice.cs:112` gives every introduced non static class becomes an
   error rather than an analyzer complaint, and the closed writer grows a change to that method. Settled by reading
   `SourceMemberContainerSymbol` in the stable Roslyn, or by one compilation test.
4. Whether an aspect author has asked for either writer. Neither #865, #866, #867 nor #869 mentions closed classes or
   unions, and no issue in the tracker requests them. If the product owner knows of a customer scenario that needs an
   aspect-generated closed hierarchy, the recommendation on the closed writer changes, because its cost is genuinely M
   and its parts are all identified above. That is the one input this analysis cannot supply.
