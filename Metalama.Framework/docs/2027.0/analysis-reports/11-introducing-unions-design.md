# 11. Introducing a union and introducing a union leg: design analysis

Written on 2026-09-04 against the branch `topic/2027.0/26-09-03-update-eng-7e3j07` of `/home/user/Metalama`, in answer to
section 5c of `Metalama.Framework/docs/2027.0/DECISIONS.md`, which overrides section 5b and makes both halves mandatory
for Metalama 2027.0. Sections 7 and 7b of the same document govern the depth of the application programming interface
material: the drafted shapes below are illustrative, not binding.

Every path is relative to `/home/user/Metalama`. Claims about the code carry a file and line reference; claims about
Roslyn and about the language proposal carry a uniform resource locator; claims about the tracker carry an issue or a
pull request number. The established facts of `FACTS.md` and the findings of
`impact/03-code-model-unions-closed.md` and `impact/10-introducing-closed-and-unions.md` are not re-derived, and where
this document contradicts one of them it says so.

## Question

Three questions, in the order in which they must be answered.

1. Does every part of a partial union have to repeat the case list, or may one part carry it while another does not?
   This decides both halves and is answered first, from the language proposal and the Roslyn implementation.
2. What does it take for an aspect to introduce a whole union: the advice surface, the builder model for the case list,
   the transformation, the syntax generation, and the way the compiler-synthesized members (one public constructor per
   case type, and the `Value` property) become visible to an aspect.
3. What does it take for an aspect to add a leg to a union that the user wrote: what the linker must rewrite, whether
   the design-time pipeline can express the result, what happens when the union is not partial, and which diagnostics
   are needed.

## The partial union case list rule (the settled answer)

### The answer

Exactly one part of a partial union carries the case list. The other parts must not repeat it, and at least one part
must carry it.

Three separate rules combine to give that answer.

1. A part without a case list parses and binds. The parser accepts `union U1;` and produces a `UnionDeclaration` with
   no `ParameterList` and no diagnostic
   ([`UnionParsingTests.Union_10`](https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Test/Syntax/Parsing/UnionParsingTests.cs),
   lines 681 to 701 of the file as fetched on 2026-09-04). The grammar consumed by this repository declares
   `ParameterList` as an optional field of `UnionDeclarationSyntax`
   (`eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1954-1975`, recorded in
   `impact/03-code-model-unions-closed.md` line 48).

2. A second part carrying a case list is an error. The merging of the parts is done by the local function
   `noteTypeParameters` in
   [`SourceMemberContainerSymbol.cs`](https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Symbols/Source/SourceMemberContainerSymbol.cs)
   at lines 4108 to 4151 of the file as fetched on 2026-09-04. It iterates over every declaration of the type. The
   first declaration whose `ParameterList` is not null is stored as `builder.DeclarationWithParameters` (line 4121);
   the arm at line 4123 handles `SyntaxKind.UnionDeclaration` by recording the part without creating a
   `SynthesizedPrimaryConstructor`, which is what the record and class arms do at line 4130. Any further declaration
   with a `ParameterList` reaches line 4148 and reports `ERR_MultipleRecordParameterLists`, which is CS8863
   (`Errors/ErrorCode.cs:1872`) with the message "Only a single partial type declaration may have a parameter list"
   (`CSharpResources.resx:6621-6623`). The rule is shared with records verbatim; the union declaration was added to
   the same code path rather than given one of its own.

3. Some part must carry it. The union constructors are synthesized from
   `declaredMembersAndInitializers.DeclarationWithParameters?.ParameterList` at `SourceMemberContainerSymbol.cs:4993`.
   When that is null, no constructor is synthesized and line 5060 reports `ERR_UnionDeclarationNeedsCaseTypes`, which
   is CS9370 (`Errors/ErrorCode.cs:2475`) with the message "A union declaration must specify at least one case type."
   (`CSharpResources.resx:8333-8335`). The location is the identifier of the first declaration.

There is no diagnostic that compares case lists between parts, and there cannot be one, because only one part may hold
a case list. The union error codes are CS9370 to CS9375 and CS9385 to CS9387 (`Errors/ErrorCode.cs:2474-2497`) and
none of them mentions partial declarations.

Two further rules of the same family matter for the design.

- Every part must use the `union` keyword. Partial declarations of different kinds report
  `ERR_PartialTypeKindConflict`, which is CS0261, whose message names the kind explicitly: "Partial declarations of
  '{0}' must be all classes, all record classes, all structs, all unions, all record structs, or all interfaces"
  (`CSharpResources.resx:1161-1163`); it is reported from
  [`SourceNamespaceSymbol.cs`](https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Symbols/Source/SourceNamespaceSymbol.cs)
  for a type in a namespace and from `SourceMemberContainerSymbol.cs:1459` for a nested type. A generated part of a
  union may not be written as `partial struct`.
- The synthesized `Value` property is attached to the first declaration, not to the one that carries the case list:
  `SourceMemberContainerSymbol.cs:4980-4981` reads `declaration.Declarations[0].SyntaxReference.GetSyntax()`. The two
  parts may therefore be different parts.

The language proposal is silent on the subject beyond permitting the modifier. The word `partial` occurs exactly once
in
[`proposals/csharp-15.0/unions.md`](https://raw.githubusercontent.com/dotnet/csharplang/main/proposals/csharp-15.0/unions.md),
in the grammar of the section "Union declarations / Syntax", where the production reads
`attributes? struct_modifier* 'partial'? 'union' identifier type_parameter_list? '(' case_types ')' ...`. Read as a
production for a single declaration it makes the case list mandatory; the implementation makes it mandatory for the
type rather than for each part, in the same way as a record positional parameter list. The implementation is the
authority here, and it is unambiguous.

### What it forbids

1. A generated part cannot add a case to a union that already declares one. This is the decisive constraint on half
   two. The leg must be added by rewriting the part the user wrote.
2. A generated part must not repeat the case list of a partial union the aspect targets. This settles the open
   question recorded in finding CM-3 of `impact/03-code-model-unions-closed.md` (line 98): the design-time part is
   built with no case list, exactly as `CreatePartialType` already does for records, where it passes
   `parameterList: null` at `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:742`
   and `:761`.
3. For a union that an aspect introduces, exactly one of the generated parts may carry the case list. The design-time
   pipeline emits an introduced type as more than one partial part: the committed baselines
   `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/Introductions/Classes/DefaultConstructor_Parameterless_DesignTime.0.i.cs`
   and `.1.i.cs` show `partial class GeneratedClass` twice, once empty and once with its constructor. The part produced
   by `IntroduceNamedTypeTransformation.GetInjectedMembers` carries the case list; the part produced by
   `CreatePartialType` must not.
4. A generated part of a partial union must be written with the `union` keyword.

### Confidence

Verified against `dotnet/roslyn` `main` on 2026-09-04, which carries `MajorVersion 5, MinorVersion 12`
(`FACTS.md` addendum). The stable Roslyn 5.12 is not published, so the rule is verified against the branch that will
become it, not against the shipping compiler. A two-part parsing and binding test against the stable package is the
cheap confirmation, and it is the same gate every other C# 15 story waits on.

## Introducing a whole union

### What already exists and what is genuinely new

The public advice surface offers `IntroduceClass` (`Metalama.Framework/src/Metalama.Framework/Advising/IAdviceFactory.cs:1015`)
and `IntroduceInterface` (`:1031`), implemented at
`Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:2050-2071` and `:2073-2093`; both build an
`IntroduceNamedTypeAdvice` and differ only in the `TypeKind` argument. The aspect-facing extension is
`Metalama.Framework/src/Metalama.Framework/Aspects/AdviserExtensions.cs:1702`.

The struct path already exists inside the engine and is unreachable only because no public method produces it.
`NamedTypeBuilder` accepts `TypeKind.Struct` at
`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Builders/NamedTypeBuilder.cs:52`, and
`IntroduceNamedTypeTransformation` emits a `StructDeclaration` at
`Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceNamedTypeTransformation.cs:73-81`.
Because a union is reported by Roslyn as `TypeKind.Struct` and is distinguished by a flag rather than by a new
`TypeKind` value (finding CM-1 of `impact/03-code-model-unions-closed.md`), the new arm in the transformation is keyed
on that flag, not on a new enumeration value, and the seventeen switches over `TypeKind` listed in the exhaustiveness
table of that analysis stay untouched.

One item that analysis 10 listed as work is not work. `ModifierHelper.GetTypeSyntaxModifierList`
(`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/ModifierHelper.cs:198-236`) needs no change for a
union: `union` is the `Keyword` field of `UnionDeclarationSyntax`, not a modifier, so it is supplied by the syntax
factory call and never passes through the modifier list. This is the difference from `closed`, which is a modifier and
does need a token in that method.

Four things are genuinely new.

### The case list has no model in the builder

`NamedTypeBuilder` has one ordered collection, `TypeParameters` (`NamedTypeBuilder.cs:40`, `:93-101`). It has no
parameter list of any kind: `PrimaryConstructor` returns null (`NamedTypeBuilder.cs:188`) and the primary constructor of
a builder is an unimplemented item recorded at
`Metalama.Framework/src/Metalama.Framework/Code/DeclarationBuilders/INamedTypeBuilder.cs:37-42`.

The case list is easier than a primary constructor, and the difference should be stated because it changes the size.
A union case is a type and nothing else: the proposal says "The *case_types* can be any type that converts to
`object`", and the Roslyn parsing tests show the case list as a `ParameterListSyntax` whose `ParameterSyntax` nodes
carry a type and no identifier (`impact/03-code-model-unions-closed.md` line 47). There is no name, no default value,
no `ref` kind, no attribute list. So the builder does not need a `ParameterBuilder`; it needs an ordered list of
`IType`. That is a smaller object than `TypeParameters`, whose elements are declarations the builder owns.

The storage sites follow the `IsRecord` precedent exactly:
`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/BuilderData/NamedTypeBuilderData.cs:20` for
the shape of an immutable ordered collection and `:46`, `:55` for a flag read from the builder, and
`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Introduced/IntroducedNamedType.cs:200` for
the projection onto the introduced type.

### The synthesized members must enter the code model without being emitted

Roslyn synthesizes, for a union declaration, the `Value` property, its getter, its backing field, and one constructor
per case type (`SourceMemberContainerSymbol.cs:4977-5060`). The introduction pipeline never re-reads the emitted code:
the member collections of the builder are all empty (`NamedTypeBuilder.cs:153`, `:165`, `:183`, `:191`) and
`IntroducedNamedType` fills its collections from the builder registry of the compilation model
(`IntroducedNamedType.cs:107`, `:160`). An aspect that introduces a union and then wants to see its constructors, or a
second aspect layer that wants to invoke one, would find nothing.

The established response to that problem is `IntroduceNamedTypeAdvice.IntroduceImplicitConstructorIfNeeded`
(`Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceNamedTypeAdvice.cs:104-118`), which
materializes, as a `ConstructorBuilder`, the parameterless constructor that Roslyn synthesizes for an introduced class.
It cannot be copied literally for a union, because it adds a transformation at `:116`, and
`IntroduceDeclarationTransformation<T>` implements both `IIntroduceDeclarationTransformation` and
`IInjectMemberTransformation`
(`Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceDeclarationTransformation.cs:17-18`),
so the constructor would also be emitted. Emitting an explicit public one-parameter constructor into a union
declaration is an error: `ERR_InstanceCtorWithOneParameterInUnion`, CS9374, reported at
`SourceMemberContainerSymbol.cs:2099` for every public constructor with one parameter that is not a
`SynthesizedUnionCtor`.

The mechanism that is needed already exists in the code base and analysis 10 did not identify it.
`IntroduceNamespaceTransformation`
(`Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceNamespaceTransformation.cs:15`)
implements `IIntroduceDeclarationTransformation` and does not implement `IInjectMemberTransformation`. Its builder data
is registered into the compilation model by `CompilationModel.Members.cs:243` and `:329`, which key on
`IIntroduceDeclarationTransformation`, and nothing is injected into any syntax tree, because the injection registry and
the injection step key on `IInjectMemberTransformation`. A transformation of that shape, carrying a
`ConstructorBuilderData` per case type and a `PropertyBuilderData` for `Value`, puts the synthesized members into the
code model and leaves the emission to the C# compiler, which is exactly the required behaviour.

Two consequences to verify during implementation rather than to assume. First, the linker maps a builder to its
injected member through `LinkerInjectionRegistry.GetTransformationForBuilder`
(`Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionRegistry.cs:530`) and
`LinkerInjectionStep.cs:448`, `:527`; a member builder with no injected member is new territory there, whereas a
namespace builder is not a member and never reaches those paths. Second, these builders must be marked implicitly
declared so that the eligibility rules described below refuse advice on them.

### The relation to pull request metalama/Metalama#1879, stated exactly

Pull request metalama/Metalama#1879 is open, carries milestone 2027.0 and targets `develop/2027.0`; on the local branch
`pr1879` it changes 110 files with 3121 insertions. It exists to make `meta.Proceed()` work when an aspect overrides a
compiler-synthesized record member, which is issue metalama/Metalama#1343.

What generalises to unions:

1. The concept and the public predicate it adds. `IMember.CanBeDeclaredExplicitly()`
   (`Metalama.Framework/src/Metalama.Framework/Code/MemberExtensions.cs`, added by the pull request) answers whether an
   explicit declaration of a member can be written in source code, and returns false for "a member that the C#
   compiler adds to a record even when the record declares this member itself". It is exposed as the eligibility rule
   `MustBeDeclarableExplicitly` (`Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityExtensions.cs`) and
   wired into two rules of `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.cs`, replacing
   an ad hoc predicate and `MustBeExplicitlyDeclared`. The `Value` property of a union and its case constructors are
   precisely members that the compiler adds unconditionally, so extending the private helper
   `IsRecordMemberAddedUnconditionally` with a union arm makes every override advice refuse them through machinery that
   already exists. This is the single highest-value generalisation and it is a few lines.
2. The kind lists. `LinkerRecordHelper.GetSynthesizedMethodOverrideTargets` and
   `GetSynthesizedPropertyOverrideTargets`
   (`Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRecordHelper.cs:48` and `:75` on `pr1879`) select
   a symbol by `IsImplicitlyDeclared` together with
   `GetPrimaryDeclarationSyntax()?.Kind() is SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration`. The
   synthesized `Value` of a union reports a `UnionDeclarationSyntax`, because
   `SynthesizedUnionValuePropertySymbol` derives from `SourcePropertySymbolBase` and is constructed with the union
   declaration
   ([`SynthesizedUnionValuePropertySymbol.cs`](https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Symbols/Synthesized/SynthesizedUnionValuePropertySymbol.cs),
   lines 13 and 44). It therefore lands in exactly the shape those two methods select, as does
   `Linking/SymbolExtensions.GetDeclarationFlags`, which finding CM-8 of `impact/03-code-model-unions-closed.md`
   records as throwing for a kind outside its list.
3. The determinism fix. The pull request sorts the synthesized members with `StructuralSymbolComparer` because "these
   members have no declaration in source whose position could order them"
   (`LinkerRecordHelper.cs:54-56` on `pr1879`). The union case constructors have the same property and need the same
   treatment.
4. The general shape "build a declaration from a symbol that has none".
   `LinkerRecordHelper.RewriteSynthesizedMethodOverrideTarget` (`LinkerRecordHelper.cs:92` and following on `pr1879`)
   constructs a `MethodDeclarationSyntax` from the symbol's return type, parameters and modifiers. Any future union
   work that must emit a declaration for a symbol-only member reuses that shape.

What does not generalise, and this is the point the question asks to be exact about:

`SynthesizedRecordMemberBodyGenerator`
(`Metalama.Framework/src/Metalama.Framework.Engine/Linking/SynthesizedRecordMemberBodyGenerator.cs` on `pr1879`, 659
lines) reproduces in C# the bodies that the compiler builds directly as bound nodes: the `EqualityContract` getter,
the strongly typed `Equals`, `GetHashCode`, `ToString`, `PrintMembers` and `Deconstruct` (the enumeration at lines 22
to 58). Each of those is an algorithm over members that have names in C#.

A union has no member of that kind. The distinction is between two different meanings of "compiler-synthesized".

- `Equals` on a record is a member whose **declaration** the user may write and whose **body** the compiler supplies
  when the user does not. There is therefore a body to reproduce, and an override to serve, and #1879 reproduces it so
  that `meta.Proceed()` reaches the original implementation.
- `Value` and the case constructors of a union are members whose **declaration** the user may not write at all. The
  proposal states that "It is an error for user-declared members to conflict with generated members", and the compiler
  enforces the constructor half directly: `ERR_InstanceCtorWithOneParameterInUnion` at
  `SourceMemberContainerSymbol.cs:2099` rejects every public one-parameter constructor of a union declaration that is
  not a `SynthesizedUnionCtor`. There is consequently no override for `meta.Proceed()` to serve and no body to
  reproduce.

Even if there were an override to serve, the body could not be written. `SynthesizedUnionCtor.GenerateMethodBodyStatements`
([`SynthesizedUnionCtor.cs`](https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Symbols/Synthesized/SynthesizedUnionCtor.cs))
assigns `valueProperty.DeclaredBackingField`, the compiler-generated backing field of the `Value` auto-property, whose
name is not a C# identifier. Pull request #1879 met the same wall for record auto-properties and answered it in two
ways: `LinkerRewritingDriver.HasMaterializedBackingField` (added by the pull request at
`Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRewritingDriver.cs`) lets the generated body read an
explicit backing field when the linker emits one for an override target, and where it does not, the new warning
`SynthesizedRecordMemberReadsPropertyVirtually` (LAMA0652, in
`Metalama.Framework/src/Metalama.Framework.Engine/Linking/AspectLinkerDiagnosticDescriptors.cs` on `pr1879`) reports
that the generated body reads the property while the compiler reads the field. Neither answer transfers, because both
depend on the member being an override target, and a union member cannot be one.

The conclusion is that union introduction needs #1879 for its eligibility concept and its kind lists, and does not need
its body generator at all. The problem half one has to solve is the other one: how a member of a type that does not yet
exist enters the code model. That is a builder-registration problem, answered by the namespace-transformation pattern
above, and pull request #1879 does not touch it.

### The syntax kind lists that decide the output

Three hand-written lists decide what happens to an introduced union, and each fails silently rather than loudly.

- `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs:641-642` switches on
  `ClassDeclaration or StructDeclaration or InterfaceDeclaration or RecordDeclaration or RecordStructDeclaration` to
  decide whether members are injected into an introduced type and whether its introduced interfaces are added. A union
  falls through, so members introduced into an introduced union are dropped. The same file needs a
  `VisitUnionDeclaration` override beside `VisitStructDeclaration` at `:318`.
- `DesignTimeSyntaxTreeGenerator.cs:510-511` (`AddPartialModifierToTypes`, reached from `:207`) carries the same list,
  so the first design-time part of an introduced union would not be made partial.
- `DesignTimeSyntaxTreeGenerator.cs:817-822` (`AddHeader`) does not add the "Generated by Metalama" comment to a union.

`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35`
(`IsTypeDeclaration`) is shared with the reading half and is already counted by findings CM-2 and CM-6.

### The design-time result of half one

The design-time pipeline generates an introduced type as more than one partial part, as the committed baselines
`DefaultConstructor_Parameterless_DesignTime.0.i.cs` and `.1.i.cs` show. Under the rule settled above, the part
produced by `IntroduceNamedTypeTransformation.GetInjectedMembers` carries the case list and every later part carries
none. `CreatePartialType` (`DesignTimeSyntaxTreeGenerator.cs:697-790`) gains a union arm that passes
`parameterList: null`, mirroring the record arms at `:742` and `:761`. Nothing else about half one is problematic at
design time: the editor sees a complete union, including the case list, and Roslyn synthesizes `Value` and the case
constructors in the editor's own compilation exactly as it does in the build.

## Introducing a leg into an existing union

### What the operation is

Adding a leg changes `UnionDeclarationSyntax.ParameterList` of a declaration the user wrote. Under the rule settled
above, it cannot be done from a generated part. It must be done by rewriting the user's own part, and only the part
that carries the case list.

### The precedent

Issue metalama/Metalama#1143, "C# 14 partial constructors: parameter introduction", is closed as completed on
2026-01-29, milestone `2026.1.0-preview`, a sub-issue of the C# 14 umbrella metalama/Metalama#1039. Its tests are in
`Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp14/PartialConstructor/`.

The precedent shows the two halves of the answer.

At build time, the linker rewrites the user's own declaration. Both partial parts of the constructor receive the new
parameter: `PartialConstructor_IntroduceParameter.t.cs:7-8` reads
`public partial ClassWithPartialConstructor(int x, global::System.Int32 p = 42);` for the defining part and the same
signature for the implementing part.

At design time, the user's declaration is left alone and the result is expressed as a separate declaration. The
expected output of the design-time scenario shows the user's constructor unchanged
(`PartialConstructor_IntroduceParameter_DesignTime.t.cs:4-8`) and the generated document carrying an overload that
chains to it (`PartialConstructor_IntroduceParameter_DesignTime.0.i.cs:5-7`):
`public ClassWithPartialConstructor(global::System.Int32 x, global::System.Int32 p = 42) : this(x) { }`. The code that
produces it is `DesignTimeSyntaxTreeGenerator.CreateInjectedConstructors` (`:525-660`), which compares the initial and
final parameter lists at `:580-585` and emits a constructor with a `this(...)` initializer at `:592-612`.

### What the linker must rewrite

The mechanism already exists in the shape the operation needs.
`LinkerInjectionStep.Rewriter.VisitTypeDeclaration` reads the type's parameter list at
`Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs:368` through the helper
`SyntaxExtensions.GetParameterList`
(`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:131`, which is simply
`typeDeclaration.ParameterList` and therefore already correct for a `UnionDeclarationSyntax`), and
`ApplyMemberLevelTransformationsToPrimaryConstructor` (`:1150-1199`) rewrites it by reference, appending through
`AppendParameters` (`:1201-1230`). Adding a case is the same edit, on the same field, in the same method. That is the
good news, and it is the reason the build-time half is not large.

Three specific defects have to be dealt with.

1. **Routing to the right part.** `TransformationCollection.GetOrAddMemberLevelTransformations(IRef<IDeclaration>)`
   keys a symbol transformation by `symbolRef.Symbol.GetPrimaryDeclarationSyntax()`
   (`Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.TransformationCollection.cs:420-431`).
   For a record primary constructor that resolves to the part carrying the parameter list by construction, because
   Roslyn gives `SynthesizedPrimaryConstructor` that syntax, which is why
   `SymbolExtensions.IsPrimaryConstructor`
   (`Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SymbolExtensions.cs:283-291`) can test
   `declarationSyntax is TypeDeclarationSyntax { ParameterList: not null }`. For a union there is no symbol with that
   property. `SynthesizedUnionCtor` sets only `Locations` and inherits the empty `DeclaringSyntaxReferences` of
   `SynthesizedInstanceConstructor`, and the union type's own primary syntax is chosen by shortest file path and then
   by span (`SymbolExtensions.cs:400-445`, `:464-466`), which may well be a part without the case list. The
   transformation must therefore be routed to the declaration whose `ParameterList` is not null, which is a new
   selector. The invariant at `Rewriter.cs:1162`, `Invariant.AssertNot( typeDeclaration.GetParameterList() == null )`,
   is exactly the assertion that fires when it is routed wrongly, so the failure mode is loud rather than silent.
2. **Duplicate cases.** Roslyn collapses duplicate case types silently: `UnionCaseTypes` builds its result through a
   set (`NamedTypeSymbol.cs:1996-1999` for the constructed case and `:2007-2010` for the definition). Metalama must
   decide whether adding a case that is already present is a no-op or a reported error, and the existing
   de-duplication of `MemberLevelTransformations.Parameters`
   (`Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.MemberLevelTransformations.cs:69-160`)
   is written for parameters matched by index and name, not for types.
3. **`IsPartial` is false for a `partial union`.** Finding CM-2 of `impact/03-code-model-unions-closed.md` shows that
   `SyntaxKindExtensions.IsTypeDeclaration` (`:33-35`) does not list the union kind, so
   `SourceNamedTypeImpl.IsPartial` reads no modifiers and returns false. Any design-time work on a union therefore
   reports LAMA0048 today
   (`Metalama.Framework/src/Metalama.Framework.Engine/Diagnostics/GeneralDiagnosticDescriptors.cs:208-216`, reported at
   `DesignTimeSyntaxTreeGenerator.cs:158-166`). This must be fixed before either half is testable.

### Whether the design-time pipeline can express the result

It cannot. This is the finding that most affects whether the capability fits the release, and it is stronger than the
corresponding finding for partial constructors.

The design-time pipeline never rewrites the user's file; it generates additional partial parts
(`DesignTimeSyntaxTreeGenerator.CreatePartialType`, `:697-790`). Four ways of expressing an added leg from a generated
part were considered and all four are closed by the compiler.

1. A second case list in the generated part: `ERR_MultipleRecordParameterLists`, CS8863, per the rule settled above.
2. An explicit public constructor taking the new case type: `ERR_InstanceCtorWithOneParameterInUnion`, CS9374,
   `SourceMemberContainerSymbol.cs:2099`.
3. A nested member provider interface declaring a factory for the new case:
   `ERR_MemberProviderInUnionDeclaration`, CS9387, "A 'union' declaration cannot use a union member provider
   interface." (`CSharpResources.resx:8381-8383`), checked at `SourceMemberContainerSymbol.cs:2105`.
4. A user-defined implicit conversion from the new case type to the union. The union conversion is a language
   conversion computed from `UnionFactoryMethods`
   ([`UserDefinedImplicitConversions.cs`](https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/CSharp/Portable/Binder/Semantics/Conversions/UserDefinedImplicitConversions.cs)),
   not a member an aspect can supply, and a hand-written conversion could not construct the union anyway, because
   `Value` is get-only and only the synthesized constructors assign its backing field.

The consequence is concrete and visible to the user. In the editor, the union has the case list the user wrote; the
conversion from the new case type does not compile; a `switch` that handles the new case reports an unreachable
pattern; a `switch` that omits it is reported as exhaustive. At build time, all three are the opposite. The editor and
the build disagree, in a feature whose entire value is a compiler-checked exhaustiveness guarantee.

That is worse than #1143, where at least an overload could be offered, and it is the same class of failure that
section 2b of `DECISIONS.md` describes for the Roslyn 5.0 variant. A design-time diagnostic saying that the aspect adds
a case the editor cannot show is the only available mitigation, and it is a mitigation, not a fix.

### When the union is not partial

`partial` is not the gate for the build-time half. The linker edits the single declaration in place, exactly as it
edits a non-partial constructor's parameter list, so a non-partial `union Pet(Cat, Dog);` can have a leg added at
build time. `partial` is the gate for the design-time half, and the design-time half produces nothing for a leg
whether the union is partial or not, so requiring `partial` would buy nothing.

The practical consequence is the opposite of the usual advice: telling the user to make the union partial does not
improve the design-time result here, and a diagnostic that says so would be misleading. If a diagnostic is reported,
it should say that the case is added at build time only.

### The shape that does work, and which I would take

There are two kinds of union type and the distinction is load-bearing.

The public `ITypeSymbol.IsUnion` is documented as "True if language treats the type as a Union"
([`ITypeSymbol.cs`](https://raw.githubusercontent.com/dotnet/roslyn/main/src/Compilers/Core/Portable/Symbols/ITypeSymbol.cs),
lines 147 to 153), which is `NamedTypeSymbol.IsUnionType`, defined as
`TypeKind is TypeKind.Class or TypeKind.Struct && IsUnionTypeCore` (`NamedTypeSymbol.cs:1944-1951`), that is, a type
carrying `System.Runtime.CompilerServices.UnionAttribute`. It covers both a `union` declaration and a hand-written
union, which the proposal describes as the second supported form ("Adapting existing types to the union patterns to
gain union behaviors").

For a hand-written union the case set is not syntactic. `UnionCaseTypes` is the set of first parameter types of the
union creation members (`NamedTypeSymbol.cs:2002-2012`), and those members are either the public instance constructors
with one parameter (`IsSuitableUnionConstructor`, `NamedTypeSymbol.cs:2395-2399`) or the static public `Create`
methods on the nested member provider interface (`isSuitableUnionFactory`, `NamedTypeSymbol.cs:2181` and following).
None of the union declaration restrictions applies, because all of them are inside `if (IsUnionDeclaration)` at
`SourceMemberContainerSymbol.cs:2084`.

Adding a leg to a hand-written union is therefore ordinary member introduction, expressible with
`IntroduceConstructor` today, and expressible in a generated partial part, so the editor and the build agree. Adding a
leg to a `union` declaration requires a linker rewrite of the user's source and cannot be shown in the editor at all.

I would take both, in that order: ship the hand-written form first, because it is small and its design-time story is
correct, and ship the `union` declaration form second with an explicit design-time diagnostic. If only one fits the
release, the hand-written form is the one worth shipping, and the product owner should be told plainly that the
`union` declaration form buys a build-time-only capability.

## Drafted interfaces

**This section is a draft for discussion.** It exists because a shape on the page makes a trade-off arguable, per
section 7b of `DECISIONS.md`. It is illustrative material, written to be criticised and replaced. No user story becomes
a specification by citing it, and no name, parameter order or overload set below is proposed as final.

### 1. The code an aspect author writes to introduce a whole union

The case list is mandatory (CS9370), so it cannot be supplied by an optional builder callback that the author may
forget. Two shapes are plausible and the choice matters.

Shape A, the case list as a required parameter of the advice method:

```csharp
public sealed class ResultAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceUnion(
            "Result",
            caseTypes: [builder.Target, TypeFactory.GetType( typeof(Exception) )] );
    }
}
```

Shape B, the case list as a builder collection, in the manner of `AddTypeParameter`
(`INamedTypeBuilder.cs:49`):

```csharp
builder.IntroduceUnion(
    "Result",
    buildType: t =>
    {
        t.AddCase( builder.Target );
        t.AddCase( TypeFactory.GetType( typeof(Exception) ) );
    } );
```

The argument for A is that the compiler makes an empty case list an error, so the advice can fail in the aspect rather
than in the generated code, and the author cannot produce an unbuildable union by writing a callback that adds
nothing. The argument for B is uniformity with `AddTypeParameter` and with the way every other structured part of a
type builder is expressed, and it composes better when the case types are computed in a loop.

I would take A, and add `AddCase` on the builder as well so that a later layer can extend a union the same aspect run
introduced. A required parameter that expresses a compiler-enforced requirement is worth the asymmetry.

### 2. The code an aspect author writes to add a leg

```csharp
public sealed class AddNotFoundCaseAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceUnionCase( TypeFactory.GetType( typeof(NotFound) ) );
    }
}
```

The target is the union, so the advice sits beside `IntroduceParameter`
(`IAdviceFactory.cs:870`, `:892`, `:924`, `:945`, `:967` and `:997`, six overloads), whose target is the constructor
rather than the type. There is no second plausible
shape worth arguing: the operation takes one type and has no other degree of freedom. The interesting question is not
the signature but the result the author gets, which the next two subsections show.

### 3. The code the author's user writes

The union the aspect of subsection 2 targets, written as a single declaration:

```csharp
[AddNotFoundCase]
public union Result( Document, Error );
```

The same union written as a partial declaration in two files. Under the rule settled above, exactly one part carries
the case list, and both parts use the `union` keyword:

```csharp
// Result.cs
[AddNotFoundCase]
public partial union Result( Document, Error );

// Result.Extra.cs
public partial union Result
{
    public bool IsError => this.Value is Error;
}
```

Writing the case list in the second part as well is CS8863; writing the second part as `partial struct Result` is
CS0261; omitting it from both is CS9370.

### 4. The code Metalama produces

**Half one, the introduced union, in the transformed compilation.** The aspect of subsection 1 applied to
`public class Document { }` produces, in the manner of
`Tests/Aspects/Introductions/Classes/IntroduceField.t.cs`:

```csharp
[ResultAspect]
public class Document
{
  union Result(global::Document, global::System.Exception)
  {
  }
}
```

Nothing declares `Value` or the case constructors: the C# compiler synthesizes them from the case list. Inside
Metalama, `Result.Constructors` and `Result.Properties` are non-empty for a later aspect layer, because the
transformation described above registered a `ConstructorBuilderData` per case and a `PropertyBuilderData` for `Value`
without injecting any syntax.

**Half one, the design-time parts.** Two documents, mirroring
`DefaultConstructor_Parameterless_DesignTime.0.i.cs` and `.1.i.cs`. The first carries the case list:

```csharp
// Introduced.0.i.cs
namespace Sample
{
  partial class Document
  {
    partial union Result(global::Sample.Document, global::System.Exception)
    {
    }
  }
}
```

and every later part carries none, which is what makes the two parts legal together:

```csharp
// Introduced.1.i.cs
namespace Sample
{
  partial class Document
  {
    partial union Result
    {
      public global::System.String Describe()
      {
      }
    }
  }
}
```

Had the settled answer been the opposite, that every part must repeat the case list, both documents would carry
`(global::Sample.Document, global::System.Exception)` and `CreatePartialType` would have to copy the case list from the
builder rather than pass null. The answer found is that exactly one part may carry it, so the shape above is the
correct one and the second part is the plain form.

**Half two, the added leg, in the transformed compilation.** The user's own declaration is rewritten, exactly as
`PartialConstructor_IntroduceParameter.t.cs:7` rewrites the user's constructor:

```csharp
[AddNotFoundCase]
public union Result(Document, Error, global::Sample.NotFound);
```

For the partial form, only the part that carried the case list is rewritten; the other part is untouched.

**Half two, the design-time part.** Empty. There is no legal generated part that adds a case, for the four reasons
given above, so the design-time pipeline emits no `.i.cs` document for this advice, and the diagnostic below is the
whole of the design-time result:

```
// Warning LAMA05xx on `Result`: `The aspect 'AddNotFoundCase' adds the case 'Sample.NotFound' to the union
// 'Sample.Result' when the project is built. The editor cannot show an added union case, because C# allows only
// one partial declaration of a union to carry the case list. Conversions from 'Sample.NotFound' and the
// exhaustiveness of switch expressions over 'Sample.Result' therefore differ between the editor and the build.`
```

Contrast this with the partial-constructor precedent, where the design-time part carries a usable overload
(`PartialConstructor_IntroduceParameter_DesignTime.0.i.cs:5-7`). That contrast is the argument for preferring the
hand-written union shape of the previous section, where the added leg is an introduced constructor and appears in the
generated part like any other member.

### 5. An eligibility failure as the author would meet it

The author writes an aspect that introduces a counter into a union:

```csharp
public sealed class CountAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceField( "_count", typeof(int) );
    }
}

// <target>
[Count]
public union Result( Document, Error );
```

and gets, in the manner of
`Tests/Aspects/Introductions/Interfaces/IntroduceField_Error.t.cs:2`, which is the committed form of the existing
LAMA0534:

```
// Error LAMA05xx on `Result`: `The aspect 'Count' cannot introduce the instance field 'Result._count' into
// 'Result', because a union declaration cannot declare an instance field, an auto-property or a field-like
// event. Declare a property with an explicit body instead.`
```

The diagnostic identifier is a placeholder; the highest advice identifier in `develop/2027.0` is LAMA0551
(`AdviceDiagnosticDescriptors.cs`) and pull request #1879 adds LAMA0552.

The reporting site is `IntroduceFieldAdvice.ValidateBuilder`
(`Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceFieldAdvice.cs:64-76`), one arm
beside the existing interface check at `:68-75`.

## Eligibility and diagnostics

The restrictions of the proposal, quoted in `impact/03-code-model-unions-closed.md` line 35, are "Instance fields,
auto-properties or field-like events are not permitted. Explicitly declared public constructors with a single
parameter are not permitted. Explicitly declared constructors must use a `this(...)` initializer to (directly or
indirectly) delegate to one of the generated constructors."

One point governs all of them and is easy to get wrong. **The restrictions apply to a union declaration, not to every
union type.** All three checks are inside `if (IsUnionDeclaration)` at `SourceMemberContainerSymbol.cs:2084`, and
`IsUnionDeclaration` reads `declaration.Declarations[0].Kind is DeclarationKind.Union` (`:1054-1060`), that is, the
`union` keyword. A hand-written `[Union]` struct may have instance fields, and its public one-parameter constructors
are the union creation members rather than an error. The public `ITypeSymbol.IsUnion` does not make the distinction,
so Metalama cannot answer it from a symbol member; it has to read the primary declaration syntax kind, which means the
predicate is only available where syntax is, and it must return false in the Roslyn 5.0 variant. This is a real cost
and analysis 03 did not identify it.

| Rule | Roslyn diagnostic | Where Metalama must check |
| --- | --- | --- |
| No instance field | `ERR_InstanceFieldInUnion`, CS9373, `SourceMemberContainerSymbol.cs:2091` | `IntroduceFieldAdvice.ValidateBuilder` (`IntroduceFieldAdvice.cs:64`), beside the interface arm at `:68` |
| No auto-property | same | `IntroducePropertyAdvice`, on `PropertyBuilder.IsAutoPropertyOrField` (`PropertyBuilder.cs:106`) |
| No field-like event | same | `IntroduceEventAdvice`, on the field-like form |
| No public single-parameter constructor | `ERR_InstanceCtorWithOneParameterInUnion`, CS9374, `:2099` | `IntroduceConstructorAdvice` (`AdviceImpl/Introduction/Constructors/IntroduceConstructorAdvice.cs`), and `IntroduceConstructorParameterAdvice`, since adding a parameter can produce the forbidden signature |
| Constructors must chain with `this(...)` | `ERR_UnionConstructorCallsDefaultConstructor`, CS9375 | `IntroduceConstructorAdvice` together with `ConstructorInitializeAdvice` |
| No nested member provider interface | `ERR_MemberProviderInUnionDeclaration`, CS9387, `:2105` | `IntroduceInterface` into a union declaration |
| `Value` and the case constructors cannot be overridden | no Roslyn diagnostic, because the declaration is never written | `CanBeDeclaredExplicitly` (`MemberExtensions.cs` on `pr1879`), extended with a union arm; the eligibility rules of `EligibilityRuleFactory` then refuse the advice with no new site |

The last row is the one that costs least and buys most, and it exists only because of pull request #1879. Without it,
each override advice would need its own check.

The generic checks in `IntroduceMemberAdvice.ValidateBuilder`
(`Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceMemberAdvice.cs:168-235`) are the
model for how such a rule is written and reported; the union rules that apply to more than one member kind belong
there rather than in each advice.

Two diagnostics are needed that have no Roslyn counterpart.

- The design-time diagnostic of half two, drafted above, reporting that the editor cannot show an added case.
- A diagnostic for the Roslyn 5.0 variant, discussed next.

## The Roslyn 5.0 variant

By decision 2 of `DECISIONS.md`, the C# 15 Roslyn members are reached through preprocessor blocks in the latest
variant only, and the symbol is defined by `eng/RoslynVersions/Roslyn.5.10.0.props:10`; `eng/RoslynVersions/Roslyn.5.0.0.props:8-10`
records that the lower variant defines no constant. The comments in both files state that no production source branches
on the variant and are superseded by decision 2.

By decision 2b, `Metalama.Framework`, which holds `IAdviceFactory` and `INamedTypeBuilder`, is not built per Roslyn
version; only `Metalama.Framework.Engine`, `Metalama.Framework.DesignTime` and `Metalama.Framework.Implementation`
carry the variant suffix. The consequence for this work is exact:

- `IntroduceUnion` and `IntroduceUnionCase` exist in every host, including Rider and the Visual Studio Code C# Dev Kit,
  which the Roslyn 5.0 variant serves.
- `IntroduceNamedTypeTransformation.GetInjectedMembers` cannot call `SyntaxFactory.UnionDeclaration` in that variant,
  because the type does not exist there; the whole arm is inside a preprocessor block.
- `INamedType.IsUnion` returns false there, so the eligibility rules above never fire, and every union looks like an
  ordinary struct.

Staying silent means the aspect produces nothing in the editor and a union in the build, which is exactly the
divergence that decision 2b calls out as unsettled. Reporting an error means the aspect fails in an editor whose user
cannot act on it except by changing the development environment.

I would report a warning, once per aspect instance, from the advice method rather than from the transformation, saying
that the host compiler predates C# 15 and that the union will be produced at build time only. A warning is visible,
does not break the editor, and can be suppressed per project by a user who has accepted the situation. This is a
decision for the product owner; it costs one descriptor and one call site either way, and it is the same decision that
decision 2b leaves open for the reading half, so it should be taken once for both.

Two consequences of the variant that are easy to miss. First, the code generator of the syntax rewriter strips
experimental declarations before generating
(`eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:19`, `:35-43`), so `MetaSyntaxRewriter` and the template
compiler's generated visitors know nothing of `UnionDeclarationSyntax` until the stable grammar drops the
`ExperimentalUrl` attributes and the generator is re-run. Second, the two existing tests that differ between variants
use `@RequiredConstant(ROSLYN_5_10_0_OR_GREATER)` and `@ForbiddenConstant(ROSLYN_5_10_0_OR_GREATER)`
(`Tests/Aspects/DesignTimeInvalidCode/UnknownAccessorInTemplate.cs:7` and
`UnknownAccessorInTemplate_Roslyn5_0.cs:7`), which is the mechanism the union tests use as well.

## Tests

Following `Metalama.Framework/docs/testing.md`, sections "The `.cs` / `.t.cs` convention", "Discovery and test naming"
and "Scenarios", and the rule of `CLAUDE.md` that a new aspect test is never committed without its expected output.

A new directory
`Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp15/Unions/`, beside the
existing `CSharp14/` directory, holding, with `// @RequiredConstant(ROSLYN_5_12_0_OR_GREATER)` in an `#if TEST_OPTIONS`
block:

Half one, introducing a union:

- `IntroduceUnion_Simple.cs` and `.t.cs`. One case type, into a class.
- `IntroduceUnion_Generic.cs` and `.t.cs`. A union with a type parameter and a constructed case type.
- `IntroduceUnion_IntoNamespace.cs`, `.t.cs` and `IntroduceUnion_IntoNamespace.Result.i.cs`, following the naming of
  `Tests/Aspects/Introductions/Classes/IntoNamespace.TestType.i.cs`.
- `IntroduceUnion_WithMembers.cs` and `.t.cs`. A method introduced into the introduced union; this is the test that
  fails today because of the kind list at `LinkerInjectionStep.Rewriter.cs:641-642`.
- `IntroduceUnion_SynthesizedMembers.cs` and `.t.cs`. A second aspect layer that reads `Constructors` and
  `Properties` of the introduced union and writes them with `ITestOutputService`, with the expected console output in
  `.t.txt`. This is the test that proves the code model contains the synthesized members.
- `IntroduceUnion_DesignTime.cs`, `.t.cs`, `.0.i.cs` and `.1.i.cs`, with `// @TestScenario(DesignTime)`. The two
  generated parts, the first with the case list and the second without. This is the test that pins the settled rule.

Half two, adding a leg:

- `IntroduceUnionCase_Simple.cs` and `.t.cs`. Non-partial union, one case added.
- `IntroduceUnionCase_Partial.cs`, its companion `IntroduceUnionCase_Partial.Dependency.cs` or an `@Include`d second
  file, and `.t.cs`. Two parts, the case list on one of them; the point of the test is that the rewrite lands on the
  right part.
- `IntroduceUnionCase_PartialListInSecondFile.cs` and `.t.cs`. The case list on the part that is not the first by file
  path, which is the case that `SymbolExtensions.GetPrimarySyntaxReference` (`:400-445`) resolves wrongly.
- `IntroduceUnionCase_Duplicate.cs` and `.t.cs`. Adding a case that is already present.
- `IntroduceUnionCase_DesignTime.cs`, `.t.cs` and no `.i.cs`, with `// @TestScenario(DesignTime)`. The test asserts
  that nothing is generated and that the design-time diagnostic is reported.
- `IntroduceUnionCase_HandWritten.cs` and `.t.cs`. A `[Union]` struct whose leg is added by introducing a constructor,
  and the matching `IntroduceUnionCase_HandWritten_DesignTime.cs` with its `.0.i.cs`, which does have generated
  output.

Eligibility:

- `Union_IntroduceField_Error.cs` and `.t.cs`, `Union_IntroduceAutoProperty_Error.cs` and `.t.cs`,
  `Union_IntroduceEventField_Error.cs` and `.t.cs`, `Union_IntroduceConstructor_OneParameter_Error.cs` and `.t.cs`,
  `Union_IntroduceConstructor_Unchained_Error.cs` and `.t.cs`, `Union_OverrideValue_Error.cs` and `.t.cs`,
  each with the diagnostic as the leading comment of the expected file, in the manner of
  `Tests/Aspects/Introductions/Interfaces/IntroduceField_Error.t.cs:2`.
- `Union_HandWritten_IntroduceField.cs` and `.t.cs`, which must succeed, proving that the rules key on the union
  declaration and not on `IsUnion`.

Variant behaviour:

- `IntroduceUnion_Roslyn5_0.cs` and `.t.cs` with `// @ForbiddenConstant(ROSLYN_5_12_0_OR_GREATER)`, pinning whatever
  the product owner decides the lower variant does.

Unit tests, in `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/CodeModel/`, following the
`UnitTestClass` and `CreateCompilationModel(code)` conventions of `docs/testing.md` section "Unit tests":

- A test that `INamedType.UnionCaseTypes` of an introduced union matches the case list given to the advice.
- A test that `ToInsertPosition` resolves for a union in a namespace and in the global namespace, which finding CM-6
  of `impact/03-code-model-unions-closed.md` shows crashes today.
- A test of the selector that picks the part carrying the case list, over a two-part union whose parts are in files
  whose names order the wrong way.

Linker tests, in `Metalama.Framework/src/tests/Metalama.Framework.Tests.LinkerTests/`, are not needed for either
half: neither half introduces a new linker semantic, and the case list rewrite is exercised end to end by the aspect
tests above.

## Work breakdown

Sizes follow the convention of the request: S is under half a day, M is one to two days, L is more.

Steps that can proceed before Roslyn 5.12 is available. These touch no C# 15 Roslyn member and can be written,
built and tested today.

| # | Step | Size | Note |
| --- | --- | --- | --- |
| 1 | Add `SyntaxKind.UnionDeclaration` to `SyntaxKindExtensions.IsTypeDeclaration` (`:33-35`) and review its consumers. | S | Blocks everything at design time; the kind value is a numeric constant in the lower variant or an `#if` in the upper one, which is the one place where the numeric alternative is defensible because the value is written by Roslyn's own generator. |
| 2 | Add a selector that returns the part of a type declaration carrying the parameter list, and route member-level transformations through it. | S | Independent of unions; it is a correctness improvement for records as well. |
| 3 | Extend `CanBeDeclaredExplicitly` and the eligibility rules with a union arm, behind an `IsUnionDeclaration` predicate read from the syntax kind. | S | Depends on pull request #1879 being merged. |
| 4 | Add the case list model to `NamedTypeBuilder`, `NamedTypeBuilderData` and `IntroducedNamedType`, with validation. | M | No Roslyn C# 15 member is involved; the model is a list of `IType`. |
| 5 | Add the transformation shape that registers a builder into the code model without injecting syntax, modelled on `IntroduceNamespaceTransformation`, and materialize the union `Value` and case constructors through it. | M | The hard part of half one, and it needs no C# 15 Roslyn member: it is entirely inside Metalama's own model. |
| 6 | Add `IntroduceUnion` and `IntroduceUnionCase` to the advice surface, the adviser extension and `AdviceFactory`, reporting "not supported by this compiler" in every path that would need C# 15 syntax. | S | Lets the public surface, the documentation and the eligibility tests be written and reviewed before the compiler exists. |
| 7 | Add the leg advice for a hand-written `[Union]` type, which is member introduction. | M | Delivers the shape whose design-time result is correct, and it needs C# 15 only for `IsUnion`, which can be stubbed behind a service for the test. |
| 8 | Write the eligibility tests and the unit tests of steps 1 to 4. | M | |

Steps that cannot proceed before Roslyn 5.12 is available, because they name `UnionDeclarationSyntax`,
`SyntaxFactory.UnionDeclaration`, `SyntaxKind.UnionDeclaration` as a compile-time constant, `ITypeSymbol.IsUnion` or
`ITypeSymbol.UnionCaseTypes`.

| # | Step | Size | Note |
| --- | --- | --- | --- |
| 9 | Rename the latest variant to 5.12 and define its symbol, per `docs/updating-roslyn.md` step 7. | M | Prerequisite of everything below; itself blocked on Metalama.Compiler moving to Roslyn 5.12, which `FACTS.md` records as unverified. |
| 10 | Emit the `UnionDeclarationSyntax` in `IntroduceNamedTypeTransformation.GetInjectedMembers`, inside the variant block. | S | The case list is a `ParameterListSyntax` of type-only parameters. |
| 11 | Add the union arms to `LinkerInjectionStep.Rewriter.cs:641-642` and `:318`, `DesignTimeSyntaxTreeGenerator.cs:510-511`, `:697-790` and `:817-822`. | M | The design-time part passes `parameterList: null`. |
| 12 | Add the leg rewrite: the transformation, its routing through step 2, and the `AppendParameters` call in `ApplyMemberLevelTransformationsToPrimaryConstructor`. | M | Small in code, and it is the step where the routing defect of subsection "What the linker must rewrite" bites. |
| 13 | Add the design-time diagnostic of half two and the lower-variant diagnostic. | S | One descriptor and one call site each, once the product owner decides the severity. |
| 14 | Write the aspect tests of the section above and commit their expected output. | L | Twenty files with committed baselines, including four design-time scenarios; this is the largest single item and it cannot start before step 9. |
| 15 | Documentation: the introduction page under `../Metalama.Documentation/content`, and the note that an added case is a build-time-only change. | M | |

Total, excluding step 9 which the release pays for anyway: roughly six to nine days of work, of which about four days
can proceed before Roslyn 5.12 exists. That is the shape of the schedule risk: most of the cost sits after a gate the
project does not control.

## Risks

1. **The Roslyn 5.12 gate.** `FACTS.md` records that no stable Roslyn beyond 5.9.0 exists on nuget.org, that
   `dotnet/roslyn` `main` reads minor version 12, and that the November 2026 baseline is expected to carry Roslyn
   5.12. Every step from 9 onwards is blocked, and step 9 is itself blocked on Metalama.Compiler, which was not
   verified in this session. Nine days of work compressed into the window between the November 2026 Roslyn and the
   2027-01-01 general availability date is the largest risk to the capability, and it is a schedule risk rather than a
   technical one.
2. **The design-time result of half two is not a good product.** Adding a case that the editor cannot show is a
   feature that behaves differently in the editor and in the build, in a language feature whose value is a
   compiler-checked exhaustiveness guarantee. If the product owner is not willing to ship that, half two must be
   narrowed to the hand-written union form, and the shape of the release changes.
3. **The case list rule could change before Roslyn 5.12 is stable.** It is verified against `main` on 2026-09-04, and
   the union feature is still marked experimental in the shipping 5.9.0 assemblies (`FACTS.md`). A change from "one
   part carries it" to "every part repeats it" would invert the design-time part of half one, though it would not
   rescue half two, whose blocker is that only one authoritative case list exists in either reading.
4. **The synthesized-member materialization is unproven in the linker.** The mechanism identified above,
   `IIntroduceDeclarationTransformation` without `IInjectMemberTransformation`, is used today only for namespaces,
   which are not members. Whether a `ConstructorBuilderData` with no injected member survives
   `LinkerInjectionRegistry` (`:217`, `:530`) and `LinkerInjectionStep` (`:448`, `:527`) was not tested. If it does
   not, step 5 grows from M to L, and it is the step that half one cannot ship without.
5. **The union declaration and the hand-written union are conflated by the public Roslyn API.** `ITypeSymbol.IsUnion`
   is true for both, and only the declaration form carries the member restrictions. Every eligibility rule therefore
   depends on a syntax-kind predicate that is unavailable in the Roslyn 5.0 variant and unavailable for a union read
   from a referenced assembly. A rule that keys on `IsUnion` alone would reject legal advice on hand-written unions,
   which is a silent loss of function.
6. **Union introduction is being shipped before struct and record introduction.** Issues metalama/Metalama#869,
   #867, #866 and #865 remain open with no milestone. A union is emitted through the struct path, so shipping union
   introduction makes the absence of `IntroduceStruct` conspicuous, and `NamedTypeBuilder.cs:53` still refuses
   records. This is a product-sequencing risk rather than a technical one, and analysis 10 already recorded it.

## What could not be verified

1. **The stable Roslyn 5.12.** Everything about the union implementation above is read from `dotnet/roslyn` `main` on
   2026-09-04. The stable package does not exist. Settled by inspecting it when it ships, which is the same gate as
   every other C# 15 story.
2. **That a user-declared `Value` in a union body is an error.** The proposal states that "It is an error for
   user-declared members to conflict with generated members", and `SourceMemberContainerSymbol.cs:4980-4986` adds the
   synthesized `Value` unconditionally, so a duplicate-name error follows; but no Roslyn test asserting it was found,
   and the GitHub code search returned no semantic test file for unions beyond the parsing tests. Settled by one
   compilation test.
3. **Whether a member builder with no injected member survives the linker.** Risk 4 above. Settled by a prototype of
   step 5, which needs no C# 15 Roslyn member and can therefore be done now.
4. **Whether Metalama.Compiler can move to Roslyn 5.12.** `Metalama.Compiler` is not cloned
   (`FACTS.md`). Settled by the Metalama.Compiler maintainer.
5. **The behaviour of `HasExplicitAccessorBody` on a `UnionDeclarationSyntax`**, which finding CM-8 of
   `impact/03-code-model-unions-closed.md` records as unread and which affects whether `Value` is classified as an
   auto-property in the code model. Settled by reading
   `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/DeclarationExtensions.cs:279-330` against a
   union, which needs the compiler.
6. **Whether any customer has asked for either half.** Analysis 10 recorded that no issue in the tracker requests
   union introduction, and section 5c of `DECISIONS.md` overrides that analysis on the product owner's authority
   rather than on new evidence. This document does not revisit the decision; it records that the design-time defect of
   half two is the input the product owner may not have had when taking it.
