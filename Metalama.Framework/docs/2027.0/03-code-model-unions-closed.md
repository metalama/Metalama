# 03. Union types and closed hierarchies in the code model

This document covers the public code model (`INamedType` and the declaration builders), the source implementations
behind it, the hand-written lists of syntax kinds and the syntax visitors on which the code model depends, and the
compile-time and design-time paths that consume them. It records how each of them behaves when a C# 15 union
declaration or a closed class reaches it, and what has to change for Metalama 2027.0. The analysis reads the code as
it stands on 2026-09-03 on branch `topic/2027.0/26-09-03-update-eng-7e3j07` of the `Metalama` repository. Each
finding was then re-checked by three verification passes: a code pass that re-read the cited code and tried to
falsify the claim, a semantics pass that re-checked every external premise against `dotnet/roslyn` and
`dotnet/csharplang`, and a scope pass that established whether the proposed change is already implemented, in flight
or tracked. The platform baseline PB-2027.0 is decided by [`platform-support.md`](../platform-support.md), the
permitted package versions by [`Directory.Packages.md`](../../../Directory.Packages.md), and the procedure for moving
to a new Roslyn by [`updating-roslyn.md`](../updating-roslyn.md); this document cites them rather than restating them.

No project was built and no test was run for this analysis.

## Summary

1. A C# 15 union lowers to a plain struct. Roslyn reports it as `TypeKind.Struct` with `IsRecord` false, adds one
   synthesized public constructor per case type, a synthesized `public object? Value { get; }` property and an
   implicit `IUnion` interface. The case types are pre-existing types written in the header; no nested case type is
   synthesized. Roslyn additionally treats any class or struct carrying `System.Runtime.CompilerServices.UnionAttribute`
   as a union, so a union is not necessarily a struct.
2. Because a union is a struct, no switch over `TypeKind`, `DeclarationKind`, `SymbolKind` or `MethodKind` in the
   engine crashes on a union or on a closed class. The recommendation is therefore a flag `INamedType.IsUnion`, on
   the precedent of `IsRecord`, rather than a new `TypeKind` value that would need a new arm in 17 switches.
3. The defects are in the syntax layer. `SyntaxKind.UnionDeclaration` is absent from the hand-written kind lists and
   no syntax visitor overrides `VisitUnionDeclaration`. The consequences are a wrong `LAMA0048` diagnostic because
   `IsPartial` is false for a `partial union`, a design-time partial declaration emitted as `partial struct` which
   the compiler rejects with CS0261, an assertion in `FindMemberDeclaration` for a union in the global namespace,
   members silently dropped by the linker injection rewriter, and a run-time union copied into the compile-time
   compilation.
4. The `closed` modifier is already modelled correctly. Roslyn makes a closed class implicitly abstract, so
   `IsAbstract` is true and `IsSealed` is false, which is what the code model reports. The work is to expose
   `IsClosed` for reading, to add it to the type builder, and to emit `closed` instead of `abstract` for an
   introduced closed class.
5. Identity and serialization are unaffected. A union is a named type with a `T:` documentation identifier, its
   synthesized members carry ordinary `M:` and `P:` identifiers, and a case type is an ordinary type.
6. Nothing in this theme compiles without a Roslyn variant decision. `Metalama.Framework.Engine.5.0.0` compiles the
   same source files against Roslyn 5.0.0, which exposes none of the union or closed API, and the Roslyn build
   consumed by the latest variant marks the API it does expose with `RSEXPERIMENTAL006`, which the compiler reports
   as an error. That decision is CM-10 and it gates CM-1 to CM-9.
7. The whole theme is latent and is sequenced after the move to the stable Roslyn. `ITypeSymbol.UnionCaseTypes` does
   not exist in the consumed build, `LanguageVersion.CSharp15` does not exist either, and the union feature is
   reachable today only under `LanguageVersion.Preview`, which the pipeline accepts only when
   `AllowPreviewLanguageFeatures` is set.
8. None of the ten findings is implemented, none is in progress in an open pull request, and no issue of
   `metalama/Metalama` mentions unions, closed hierarchies or C# 15. There is no C# 15 umbrella issue comparable to
   the closed #1039 for C# 14.

## Findings

### CM-1. A union is an undistinguishable struct in the public code model

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:69-79` (the `TypeKind`
    mapping, with no union arm and an `InvalidOperationException` default arm at `:78`), `:133`
    (`IsReferenceType`), `:173` (`IsRecord`), `:247` (`Constructors`)
  - `Metalama.Framework/src/Metalama.Framework/Code/INamedType.cs:15` (the summary lists "class, struct, interface,
    enum, delegate, or record"), `:192` (`IsReadOnly`), `:197` (`IsRef`), `:202` (`IsRecord`)
  - `Metalama.Framework/src/Metalama.Framework/Code/TypeKind.cs:29-30` (the obsolete `RecordClass`, with the message
    "TypeKind.Class and INamedType.IsRecord") and `:67-68` (the obsolete `RecordStruct`, with the message
    "TypeKind.Struct and INamedType.IsRecord")
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedType.cs:514-522` (the `IsRecord`
    facade, which forwards through `OnUsingDeclaration`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Builders/NamedTypeBuilder.cs:36`
    and `:56`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/BuilderData/NamedTypeBuilderData.cs:46`
    and `:55`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Introduced/IntroducedNamedType.cs:200`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Introduced/IntroducedExtensionBlock.cs:190`
  - `Metalama.Extensions/src/Metalama.Extensions.Multicast/MulticastImplementation.cs:166-179` (the type-kind filter)
  - `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:47`, `:121`, `:143`, `:175`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs:316-324` and `:359`
    (the five type-declaration overrides and the method that adds the introduced members)
- What happens today: Roslyn maps `DeclarationKind.Union` to `TypeKind.Struct`, so `SourceNamedTypeImpl.TypeKind`
  returns `TypeKind.Struct`, `IsRecord` returns false because Roslyn restricts `IsRecord` to `DeclarationKind.Record`
  and exposes the union declaration through a separate internal member, and `IsReferenceType` returns false. The
  synthesized per-case constructors and the synthesized `Value` property are ordinary members of the symbol, so
  `Constructors` and `Properties` list them. An aspect has no member with which to tell a union from a struct: the
  repository contains no occurrence of `IsUnion`, `UnionCaseTypes`, `IsClosed` or `UnionDeclaration` in any C# file.
  The `[Union]` attribute is synthesized on the emit path only, and only when the user did not write it, so it is
  absent from `Attributes` for a union of the current compilation and present for a union read from a referenced
  assembly. `ImplementedInterfaces` contains `IUnion` in both cases, because Roslyn adds the interface to the
  declared base interfaces of the source symbol rather than at emit time. One qualification that the original report
  did not state: Roslyn defines `ITypeSymbol.IsUnion` as the union declaration or the presence of the `[Union]`
  attribute, so a hand-written `[Union]` class is also a union while its `TypeKind` is `Class`.
- Consequence: silent wrong output. `MulticastImplementation.cs:166-179` and `EligibilityRuleFactory.cs:47`, `:121`,
  `:143` and `:175` admit a union wherever they admit a struct, so an aspect written for structs is applied to a
  union with no diagnostic; when such an aspect introduces an instance field or an auto-property, the member never
  reaches the generated code, because `LinkerInjectionStep.Rewriter` has no `VisitUnionDeclaration` override and the
  generated Roslyn rewriter method only rewrites the children, so the member is dropped without a diagnostic and the
  compiler error CS9373 that the union restriction would otherwise produce is never reported. At design time the
  union fails earlier and does produce a diagnostic, because `IsPartial` is false for a `partial union` and
  `LAMA0048` is reported instead; see CM-2.
- Proposed change: add `bool IsUnion { get; }` and `IReadOnlyList<IType> UnionCaseTypes { get; }` to `INamedType`,
  next to `IsRecord`, and document the union in the summary at `INamedType.cs:15`. Do not add a `TypeKind.Union`
  value: the precedent is the obsolete `TypeKind.RecordClass` and `TypeKind.RecordStruct`, and a new value would need
  a new arm in the 17 switches over the Metalama `TypeKind` that this analysis inventoried. The two inventories
  disagree on how many of those switches throw in the default arm, 9 in the exhaustiveness table of the original
  report and 12 in the Roslyn API delta report, and the disagreement does not change the conclusion. Implement
  `IsUnion` in `SourceNamedTypeImpl` from `ITypeSymbol.IsUnion`, forward it in the facade at `SourceNamedType.cs:514-522`
  with the same pattern as `IsRecord`, and return the constant false and an empty list in `NamedTypeBuilder.cs:36`,
  `NamedTypeBuilderData.cs:55`, `IntroducedNamedType.cs:200` and `IntroducedExtensionBlock.cs:190`. Because the
  Roslyn predicate covers any class or struct carrying the `[Union]` attribute, the documentation of `IsUnion` must
  state that `IsUnion` is independent of `TypeKind` and that a union is not necessarily a value type. Add an
  eligibility rule that rejects the introduction of an instance field, an auto-property or a field-like event into a
  union, and cover the constructor case as well, because an explicitly declared public constructor with a single
  parameter is rejected by CS9374 and a declared constructor without a `this` initializer by CS9375. Note that those
  three restrictions are checked by the compiler for a union declaration only, so a rule phrased as "must not be a
  union" would be stricter than the compiler for an attribute-based union type. Independently of the public API, add
  the missing `VisitUnionDeclaration` override to `LinkerInjectionStep.Rewriter`, which is owned by the linker and
  advice theme, otherwise members introduced into a union continue to be dropped without a diagnostic.
- Size: medium for the public API and the five implementation sites. `UnionCaseTypes` cannot be implemented from the
  Roslyn member against the currently consumed build and must wait for the stable Roslyn, so either the change is
  split, shipping `IsUnion` first, or the whole member pair is deferred to that move.
- Status: new work. No issue of `metalama/Metalama` mentions unions; the parent is the open meta-issue #1921
  (.NET 11 Support), none of whose sub-issues covers unions, and the shape to follow is that of the closed C# 14
  code-model stories #1034 and #1115 under the closed umbrella #1039. The related open issue #869 (type introduction:
  introduce struct) is worth referencing if the story also adds the eligibility rule. The implementation depends on
  the variant decision of CM-10.
- Verification: the code pass confirmed every cited location and the absence of any distinguishing member, and
  corrected the failure mechanism (the member is dropped by the injection rewriter rather than rejected by the
  compiler), three builder paths and one quotation of an obsolete message. The semantics pass confirmed the union
  lowering, the emit-time synthesis of the attribute, the declared-interface addition of `IUnion` and the wording of
  CS9373, and established that `ITypeSymbol.IsUnion` exists in the consumed build under `RSEXPERIMENTAL006` while
  `ITypeSymbol.UnionCaseTypes` does not exist there at all. The scope pass found the change neither implemented, in
  progress nor tracked, and confirmed that no member named `IsUnion` exists in either repository.
- Open questions: whether the public API should also expose the synthesized `Value` property as a distinguished
  member. Nothing in Roslyn marks that property, because its symbol class is internal, so the member is discoverable
  only by name and signature.

### CM-2. `IsPartial` is false for a `partial union`, which reports `LAMA0048` wrongly

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:329-352` (`IsPartial`;
    the type-declaration arm is at `:344`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35`
    (`IsTypeDeclaration` lists class, struct, interface, record and record struct) and `:41`
    (`IsBaseTypeDeclaration`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:158-164`
    (the `LAMA0048` report at `:162` and the early return at `:164`) and `:940-951` (`IsInNonPartialSourceType`,
    which reads `IsPartial` again at `:950`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Diagnostics/GeneralDiagnosticDescriptors.cs:209`
    (`TypeNotPartial`, that is `LAMA0048`, severity warning)
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceMemberAdvice.cs:217`
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/CodeFixes/TheCodeFixProvider.cs:57`, `:111`, `:173`,
    `:187-193`
  - The consumers to review with the predicate:
    `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SymbolExtensions.cs:289`,
    `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.FindCompileTimeCodeVisitor.cs:77`
    and `:81`,
    `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs:1460`
    and `:1530`,
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/DependencyAnalysisHelper.FindDeclaredTypesVisitor.cs:35`
    and `:39`,
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/DependencyAnalysisHelper.FindDeclaredAndAttributeTypesVisitor.cs:41`
    and `:45`,
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceConstructor.cs:92` and `:94`, and
    `Metalama.Framework/src/Metalama.Framework.Engine/Linking/Inlining/ImplicitLastOverrideReferenceInliner.cs:72`
  - `Metalama.Framework/src/Metalama.Framework.Engine.5.0.0/Metalama.Framework.Engine.5.0.0.csproj:6` (the same
    sources are compiled against Roslyn 5.0.0) and `eng/RoslynVersions/Roslyn.5.10.0.props:10` (the sole definition
    of `ROSLYN_5_10_0_OR_GREATER`)
- What happens today: `IsPartial` reads the modifiers of the primary syntax only when the kind satisfies
  `IsTypeDeclaration`, or is an enum or a delegate declaration. `UnionDeclarationSyntax` derives from
  `TypeDeclarationSyntax` and overrides `Modifiers`, so the type test of the first arm matches, but
  `SyntaxKind.UnionDeclaration` is not in `IsTypeDeclaration`, so the arm does not match, the switch falls to the
  default arm, the modifier list is empty and `IsPartial` is false even when the source says `partial union`. The
  design-time generator then reports `LAMA0048` ("Aspects add members to '{0}' but it is not marked as 'partial'")
  and returns from the local function that processes the transformations of that type, so no design-time partial
  declaration is generated for the union while the other types of the project are still processed. The code fix
  registered for `LAMA0048` selects its target with a type test on `BaseTypeDeclarationSyntax` and no kind test, so
  it does match a union declaration and applies `AddModifiers(partial)` unconditionally. The defect is latent: no
  Roslyn that Metalama consumes today parses `partial union` outside a preview language version, and the compiler
  gates the union keyword on the union feature.
- Consequence: diagnostic reported wrongly, the design-time artefacts of the union are never generated, and the
  offered code fix either produces invalid source by appending a second `partial` modifier to a union that already
  carries one, or applies correctly to a union that is not partial and still fails to silence the warning, because
  `IsPartial` continues to ignore the union modifiers.
- Proposed change: make the predicate recognize a union declaration, but not by writing `SyntaxKind.UnionDeclaration`
  in the shared source file. `SyntaxKindExtensions.cs` is also compiled against Roslyn 5.0.0, where the union syntax
  does not exist at all, and in the consumed Roslyn build the member carries `RSEXPERIMENTAL006`, which is a compile
  error unless suppressed; the repository has no conditional compilation in production source and no such
  suppression, so either option is a first for this codebase. Two workable shapes exist: guard the added kind with
  `ROSLYN_5_10_0_OR_GREATER` and suppress the experimental diagnostic until the move to the stable Roslyn removes the
  marker, keeping the guard because the Roslyn 5.0 variant remains; or reformulate the affected call sites so that
  they do not name the kind at all, for example by keeping only the `TypeDeclarationSyntax` type test at
  `SourceNamedTypeImpl.cs:344`, which is version-agnostic but raises `LAMA0860` from the
  `KindCheckOptimizationAnalyzer` and therefore needs the analyzer taught or the diagnostic suppressed. Whichever
  shape is chosen, review every consumer of `IsTypeDeclaration` and `IsBaseTypeDeclaration`, including the two that
  the original report omitted, `SourceConstructor.cs:92` and `:94` and `ImplicitLastOverrideReferenceInliner.cs:72`,
  the first of which throws an `AssertionFailedException` in its default arm. Also correct `TheCodeFixProvider` so
  that it does not add a second `partial` modifier. No existing test has to be extended: the tests of the
  `KindCheckOptimizationAnalyzer` assert only that the analyzer reports no diagnostic on a given pattern shape and
  never enumerate the kinds of `IsTypeDeclaration`, and the range cited by the original report,
  `Metalama.Framework/src/tests/Metalama.Framework.Engine.Analyzers.Tests/KindCheckOptimizationAnalyzerTests.cs:1115`,
  opens the `IsRecordDeclaration` region, while the `IsTypeDeclaration` tests are at `:951` and `:1309`. A new
  behavioural test of `SyntaxKindExtensions` is therefore needed.
- Size: medium, corrected upwards from the original estimate because the edit requires the variant decision of CM-10
  rather than one added enumeration member.
- Status: new work. No issue tracks it. The doctrine of the kind-check pattern comes from the closed #1307, which
  created both `SyntaxKindExtensions.cs` and the `KindCheckOptimizationAnalyzer`, and the reason the kind cannot be
  named unconditionally comes from the closed #1881, which removed every conditional compilation block from
  production source.
- Verification: the code pass confirmed the wrong `IsPartial`, the wrong `LAMA0048` and the early return, refuted the
  test citation, added the two omitted consumers and showed that the code fix is worse than described because it
  duplicates the modifier. The semantics pass confirmed that `partial union` is legal in the grammar and in the
  compiler modifier table, that `UnionDeclarationSyntax` is a `TypeDeclarationSyntax` with a `Modifiers` property, and
  that the generated part must repeat the `union` keyword and omit the case list. The scope pass found the change
  neither implemented, in progress nor tracked, and confirmed that no production or test source names a union.
- Open questions: none.

### CM-3. The design-time partial declaration of a union is emitted as `partial struct`

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:697-790`
    (`CreatePartialType`, whose default arm at `:788` throws an `AssertionFailedException`), `:749` (the
    `TypeKind.Struct when !type.IsRecord` arm that a union reaches), `:722` (the arm a closed class reaches)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:817-823`
    (`AddHeader`), `:332` (the single application of `AddHeader`, to the outermost declaration), `:294-307` and
    `:310-324` (the construction of that outermost declaration)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:214-217`
    (the base list that carries the interfaces added by `IInjectInterfaceTransformation`)
  - `Metalama.Framework/src/Metalama.Framework.Engine.5.0.0/Metalama.Framework.Engine.5.0.0.csproj:6`
- What happens today: `CreatePartialType` switches on `type.TypeKind` and `type.IsRecord`. Once CM-2 is corrected, a
  partial union reaches the arm at `:749`, because Roslyn maps the union declaration to `TypeKind.Struct` and
  `IsRecord` is false, and the generated document declares `partial struct Pet { ... }`. Roslyn treats a union
  declaration as a partial kind of its own, and the message of `ERR_PartialTypeKindConflict` names it explicitly
  ("Partial declarations of '{0}' must be all classes, all record classes, all structs, all unions, all record
  structs, or all interfaces"), so the design-time compilation of the user's project reports CS0261. Because the two
  parts do not merge into one symbol, the members that the aspects introduce are also not visible on the union in the
  editor. `AddHeader` recognizes class, struct, record, interface and namespace declarations only, but it is applied
  once, to the outermost declaration of the generated compilation unit, so a union declaration reaches it only for a
  non-nested union in the global namespace, which is the only case in which the generated file would carry no header.
  The same generator emits `partial class` for a closed class, and that part is correct: Roslyn combines the
  modifiers of the parts with a bitwise disjunction, `Closed` is in the allowed set for a class, and the implicit
  `abstract` is applied to the merged value, so a part without the `closed` modifier compiles. That statement is now
  verified in the Roslyn source rather than plausible.
- Consequence: diagnostic reported, namely the compiler error CS0261 in the generated design-time file, for a partial
  union with introduced members, together with the loss of the introduced members in the editor. There is no impact
  for a closed class.
- Proposed change: add an arm for the union to the switch of `CreatePartialType` and add `UnionDeclarationSyntax` to
  `AddHeader`, under four constraints. First, the arm must be placed before the existing arm at `:749`: a `when`
  clause does not prevent that earlier arm from matching a union, whose `IsRecord` is false, so an arm added after it
  would never execute. Second, the arm must pass the `baseList` parameter like every other arm, because the base list
  carries the interfaces that aspects introduce and `UnionDeclarationSyntax` has a base list, and it must pass a null
  parameter list, because only the first part carrying a parameter list becomes the declaration with parameters and a
  second one is reported as CS8863, while the error that requires case types is reported only when no part carries
  them. Third, `SyntaxFactory.UnionDeclaration` is marked `RSEXPERIMENTAL006` and does not exist in Roslyn 5.0.0, so
  the arm needs the mechanism decided in CM-10. Fourth, the arm needs `INamedType.IsUnion` from CM-1. Write a
  design-time aspect test with a `partial union` target, mark it so that it is skipped in the Roslyn 5.0 variant, and
  commit its generated output, per the testing rules of `CLAUDE.md`.
- Size: medium, being a gated factory call on an experimental node, the variant gating of CM-10, the dependency on
  CM-1 and one design-time test.
- Status: new work. No issue tracks it. The story must state which Roslyn variant compiles the arm, which is the
  subject of CM-10, because the closed #1881 removed every conditional compilation block from production source. The
  arm is reachable only once CM-2 is corrected, and the test can only be written once the language version plumbing
  admits C# 15.
- Verification: the code pass confirmed the arm that a union reaches and the default arm that throws, and corrected
  the method range, the single application of `AddHeader`, the omission of the base list from the proposed arm and
  the placement of the new arm. The semantics pass confirmed CS0261 from the compiler sources, confirmed that the
  closed-class part compiles because the modifiers of the parts are merged, and resolved the open question in the
  direction opposite to the hypothesis, namely that the generated part must not repeat the case list. The scope pass
  found the change neither implemented, in progress nor tracked, and confirmed that the switch still has five arms.
- Open questions: none. The question of whether a partial union part may omit the case list is answered: it must omit
  it.

### CM-4. Introduced types cannot be closed, and an introduced closed class would be emitted as `abstract`

- Where:
  - `Metalama.Framework/src/Metalama.Framework/Code/DeclarationBuilders/INamedTypeBuilder.cs:13-49` (`IsPartial` at
    `:18`, `BaseType` at `:35`, `AddTypeParameter` at `:49`)
  - `Metalama.Framework/src/Metalama.Framework/Code/DeclarationBuilders/IMemberOrNamedTypeBuilder.cs:26-45`
    (settable `IsStatic` at `:30`, `IsSealed` at `:35`, `IsAbstract` at `:40`, `IsPartial` at `:45`; the file ends at
    line 46, so the range 64-95 cited by the original report does not exist)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/ModifierHelper.cs:198-236`
    (`GetTypeSyntaxModifierList`, which emits the abstract keyword at `:226` when `IsAbstract` and the kind is not an
    interface, and the sealed keyword at `:231` when `IsSealed`), and `:105` (the partial modifier, handled for
    members only)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Abstractions/INamedTypeImpl.cs:10` (the parameter
    type of `GetTypeSyntaxModifierList`, which derives from `INamedType` and adds no modifier member)
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceNamedTypeTransformation.cs:61-92`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Builders/NamedTypeBuilder.cs:52-53`
    (asserts class, struct, interface or extension, and that the type is not a record)
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Invariant.cs:29-41` (`Invariant.Assert` is an empty method outside
    a debug build)
  - `Metalama.Framework/src/Metalama.Framework/Advising/IAdviceFactory.cs:1015-1035` and
    `Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:2050-2093` (`IntroduceClass` and
    `IntroduceInterface` only)
  - `Metalama.Framework/src/Metalama.Framework/Code/TypeKind.cs:27-30` (`TypeKind.Class` covers record classes)
  - `Metalama.Framework/src/Metalama.Framework.Engine.5.0.0/Metalama.Framework.Engine.5.0.0.csproj:6`
- What happens today: the public API offers `IntroduceClass` and `IntroduceInterface` only; there is no
  `IntroduceStruct`, `IntroduceRecord` or `IntroduceUnion`. Record introduction is guarded by an assertion in the
  `NamedTypeBuilder` constructor, which is an empty method outside a debug build and which no call site can reach,
  because every construction site passes the default value. A closed class cannot be introduced, because no builder
  property exists and no member named `IsClosed` exists anywhere in the source of `Metalama.Framework`. If `IsClosed`
  were added by copying `IsAbstract`, `GetTypeSyntaxModifierList` would emit `abstract closed class`, which Roslyn
  rejects with `ERR_ClosedExplicitlyAbstract` (CS9384, "a closed type cannot be marked abstract because it is always
  implicitly abstract"), because a closed class is implicitly abstract.
- Consequence: no impact today, because the feature is absent, no code path reaches the record assertion and no
  diagnostic fires. For a future implementation the failure would not be an assertion inside Metalama: an unadapted
  modifier list produces syntax that the compiler rejects on generated code, and a reference to
  `SyntaxKind.ClosedKeyword` that is not gated breaks the compilation of the `Metalama.Framework.Engine.5.0.0`
  variant itself.
- Proposed change: add `new bool IsClosed { get; set; }` to `INamedTypeBuilder`, following the pattern of
  `new bool IsPartial` at `INamedTypeBuilder.cs:18`. The `new` keyword is required because the getter must also exist
  on `INamedType`, which is the change proposed in CM-5, and because `GetTypeSyntaxModifierList` receives an
  `INamedTypeImpl` rather than the builder, so a builder-only property would be unreachable from `ModifierHelper`.
  The modifier applies to classes, so do not add the member to `IMemberOrNamedTypeBuilder`; note that Roslyn allows it
  on record classes as well, so the restriction is against structs, interfaces, enums and delegates rather than
  against records, and that `TypeKind.Class` already covers record classes in the Metalama enumeration. Validate in
  the setter that the type kind is a class and that `IsSealed` and `IsStatic` are false, mirroring
  `ERR_ClosedSealedStatic`, and make `IsAbstract` read true when `IsClosed` is true, mirroring the implicit
  abstractness that Roslyn applies. Store the value in `NamedTypeBuilderData` and expose it in `IntroducedNamedType`.
  In `GetTypeSyntaxModifierList`, emit the closed keyword and skip the abstract keyword when the type is closed.
  There is no ordering question with respect to the partial modifier in that method, because it never emits one; the
  language rule nevertheless matters for the design-time generator of CM-3, because the compiler requires `partial`
  to be the last modifier, so `closed partial class` is the only valid order and `partial closed class` is CS0267.
  The token cannot be written as `SyntaxKind.ClosedKeyword` in shared engine source, because that value does not
  exist in Roslyn 5.0.0 and carries `RSEXPERIMENTAL006` in the consumed build, so this change requires the mechanism
  decided in CM-10. Guard the feature at run time as well: emitting `closed` requires the target compilation to
  provide `System.Runtime.CompilerServices.IsClosedTypeAttribute` and `CompilerFeatureRequiredAttribute`, which the
  compiler does not embed, so an introduced closed class must be diagnosed rather than emitted when either
  prerequisite is missing. Do not add union introduction for 2027.0: record introduction is not supported either, and
  a union declaration requires a case list for which the builder has no model.
- Size: medium.
- Status: new work. No issue tracks it, and this finding duplicates LK-5 of the linker and advice theme, which
  proposes the same change in the same words; the two must be counted once. The direct precedent is the closed #1869
  ("Cannot introduce a partial class"), fixed by the merged pull request #1878, which added the same shape of change,
  namely a modifier flag on `INamedTypeBuilder` that must also be emitted in the type modifier list. The family of
  open type-introduction issues (#862, #865, #866, #867, #868, #869) shows that each unsupported introduced type kind
  is tracked separately, which is why the decision not to add union introduction has to be stated rather than
  assumed.
- Verification: the code pass confirmed that no `IsClosed` exists, that the introduction surface is limited to two
  methods and that the modifier list would emit `abstract closed class`, and corrected a wrong line range, the
  missing dependency on CM-5, the missing variant gating and the observation that the record assertion is dormant
  outside a debug build. The semantics pass confirmed the implicit abstractness, the exact error code and message,
  the fact that a record class may be closed, and added the target-framework prerequisite and the modifier ordering
  rule. The scope pass found the change neither implemented, in progress nor tracked, and confirmed that the
  identifier `IsClosed` appears nowhere in production source.
- Open questions: whether every partial declaration of a closed class must repeat the `closed` modifier is not
  addressed by the proposal; the compiler merges the modifiers of the parts, which is why CM-3 concludes that the
  generated part need not repeat it, but no Roslyn test with a single closed part was found.

### CM-5. `IsClosed` is not exposed; the derived closedness is otherwise modelled correctly

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceMemberOrNamedType.cs:23` (`IsSealed`)
    and `:36-44` (`IsAbstract`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:44`
    (`CanBeInherited`) and `:175-179` (`HasDefaultConstructor`)
  - `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityExtensions.cs:740` (`MustNotBeAbstract`)
  - `Metalama.Framework/src/Metalama.Framework/Code/DerivedTypesOptions.cs:22-44` (`All` and `DirectOnly` return
    types declared in the current compilation; `IncludingExternalTypesDangerous` is documented as incomplete)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/DerivedTypeIndex.cs:40-92` and `:118-127`
    (`GetDirectlyDerivedTypesCore`, which filters every candidate through `IsContainedInCurrentCompilation`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/PartialCompilation.cs:449-460` (the index of a
    complete compilation) and `:237-280` with `:366-420` (the index of a partial compilation)
- What happens today: a closed class is implicitly abstract, both by the language rule and in the compiler, so
  `IsAbstract` is true, `IsSealed` is false, `CanBeInherited` is true, `HasDefaultConstructor` is false and
  `MustNotBeAbstract` rejects the type. All of that is semantically correct. Nothing tells an aspect that the set of
  direct subtypes is complete. One qualification applies to the optimization that closedness allows: for a closed
  type declared in the current compilation, `DerivedTypesOptions.DirectOnly` is already exhaustive, because a
  subtype of a closed type must be in the same module and `DirectOnly` returns every directly derived type declared
  in the current compilation. That holds only when the compilation model is built on a complete compilation,
  because a partial compilation indexes only the closure of the selected syntax trees. It is the closed type coming
  from a referenced assembly for which no complete answer exists: `DirectOnly` and `All` exclude external types by
  construction, and `IncludingExternalTypesDangerous` is documented as incomplete.
- Consequence: no impact.
- Proposed change: add `bool IsClosed { get; }` to `INamedType`, returning false in the builders and the introduced
  types unless CM-4 is implemented, and document in `DerivedTypesOptions` that for a closed type declared in the
  current compilation, and only for a complete compilation, `DirectOnly` returns the whole set of direct subtypes.
  Three constraints on the implementation, all external. First, `ITypeSymbol.IsClosed` cannot be read from shared
  source: it does not exist in the Roslyn 5.0.0 variant, and in the consumed build and in the stable 5.9.0 it carries
  the experimental marker, so the read must be gated to the latest variant with `IsClosed` returning false in the
  lower variant. That constant is not observable, because Roslyn 5.0 cannot parse the `closed` modifier, so no
  compilation served by that variant contains a closed class declared in source. Second, the work is blocked on the
  move to the stable Roslyn for a second reason: no Roslyn that Metalama consumes today exposes C# 15 as a
  non-preview language version. Third, `GetClosedDerivedTypeInfo` is not merely a faster source for
  `GetDerivedTypes`: for a closed type of the current compilation the index is already complete, so there is nothing
  to gain, while for a closed type of a referenced assembly the index cannot answer at all and the Roslyn member is
  the only complete source, because Roslyn scans the type definitions of the referenced module. If the new member is
  used, its result must be honoured only when the returned information reports itself complete, because it is
  incomplete when a generic closed type has a derived type that cannot be named, and Roslyn returns derived type
  definitions, whereas exhaustiveness for a particular construction of a generic closed type is narrower.
- Size: small for the code model member and the documentation; medium once the Roslyn variant gating and the external
  closed type path are included.
- Status: new work. No issue tracks it, and no open or closed issue of `metalama/Metalama` concerns closed
  hierarchies. The precedent for splitting a language feature into a separate code-model issue is the closed #1034
  under the umbrella #1039. The open #985 (template compiler catch-all for later C# features) does not cover it,
  because it concerns the template compiler and not the public code model. The change depends on the variant decision
  of CM-10, and it belongs in one story with CM-1.
- Verification: the code pass confirmed every cited member and the "no impact" classification, and corrected the
  proposed change on two points: the member does not exist in the package that the lower variant binds against, and
  the exhaustiveness of `DirectOnly` holds only for a closed type of the current compilation and only for a complete
  compilation. The semantics pass confirmed the implicit abstractness, the same-module restriction, the shape of the
  Roslyn API and the incompleteness flag, and answered the open question of the original report from the compiler
  source. The scope pass found the change neither implemented, in progress nor tracked, and confirmed that
  `DerivedTypesOptions` carries no mention of closed types.
- Open questions: none. The question of whether `[CompilerFeatureRequired("ClosedClasses")]` reaches
  `IConstructor.Attributes` is answered and can be closed as no impact: on a source constructor the attribute is
  synthesized during emit, and synthesized attributes are not returned by the symbol; on a constructor read from
  metadata, Roslyn filters that attribute out whenever the containing type is closed and the compiler supports the
  feature. No advice that copies constructor attributes can copy it.

### CM-6. Hand-written lists of type-declaration syntax kinds omit `UnionDeclaration`

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:19-21`
    (`FindMemberDeclaration`, the wrapper that throws an `AssertionFailedException`), `:23-46`
    (`FindMemberDeclarationOrNull`, with the kind list at `:29-36`), `:51-75` (`FindSymbolDeclaringNode`, with the
    kind list at `:57-64`), `:113-120` (`GetDeclaringType`, with the kind list at `:116-118` and the walk to the
    parent at `:119`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35` and `:41`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/CodeModelExtensions.cs:66-89`
    (`ToInsertPosition`, whose first branch is at `:70-82` and whose containing-type branch is at `:83-88`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SymbolExtensions.cs:181-203` (`HasModifier`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceMemberOrNamedType.cs:133-179`
    (`HasNewKeyword`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceConstructor.cs:116-148`
    (`GetBaseConstructor`, whose kind list is at `:124-125` and whose throwing default arm is at `:127`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/SymbolExtensions.cs:18-64` (`GetDeclarationFlags`, with
    the kind list at `:29` and the throwing default arm at `:63`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs:397-402`, `:405-409`,
    `:1854-1859`, `:1874-1891` and `:1894-1912` (the only insert positions that any visitor queries)
  - `Metalama.Framework/src/Metalama.Framework.Engine/ReferenceGraph/ReferenceIndexWalker.cs:211` (the one site that
    already uses the Roslyn helper)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/ModifierHelper.cs:198-236` (the type modifier
    list, which never calls `HasModifier`)
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/CodeModel/` (no test of `ToInsertPosition`
    exists today)
- What happens today: `ToInsertPosition` takes the primary declaration of the union type and calls
  `FindMemberDeclaration`, which examines the node and then each of its ancestors in turn until it finds a kind in
  the list. The union kind is in no list, so the search continues past the union. When the union is declared in a
  namespace, the search reaches the namespace declaration, which is in the list, and because a namespace declaration is not a
  `BaseTypeDeclarationSyntax` the position becomes `After` the namespace. No visitor of the injection rewriter ever
  queries that position: the rewriter queries `After` only for the members of a type declaration, `Within` for a type
  declaration and for a namespace, and the root position for the compilation unit. A member introduced into a union
  is therefore not emitted at all, rather than emitted in the wrong place. When the union is declared in the global
  namespace, the search reaches the compilation unit, returns null, and `FindMemberDeclaration` throws an
  `AssertionFailedException`. When the union is nested in a type, the search stops at the enclosing type and the
  position becomes `Within` the enclosing type. The two families of compiler-synthesized union members reach the same
  code by two different paths: the `Value` property reports the union declaration as its own declaring syntax and
  therefore takes the first branch, while the per-case constructors have no declaring syntax and take the
  containing-type branch. `GetDeclaringType` returns the type that encloses the union, or null when the union is not
  nested in a type. `HasModifier` returns false for every modifier of a union type, which has no consequence, because the
  type modifier list is built from the accessibility, `IsStatic`, `HasNewKeyword`, `IsAbstract` and `IsSealed` and
  never calls it. `HasNewKeyword` reaches the default arm for a member declaration and answers from the union
  modifiers, which is correct.
- Consequence: assertion or crash when the union is in the global namespace; silent loss of the introduced member,
  rather than misplacement, when the union is in a namespace; silent wrong answers for the remaining sites.
- Proposed change: make each list recognize the union kind, and do not substitute Roslyn's own
  `SyntaxFacts.IsTypeDeclaration` without further analysis. That helper is strictly wider, because it also returns
  true for a delegate, an enum and an extension block, whereas `SyntaxKindExtensions.IsTypeDeclaration` is documented
  as the narrower set of type declarations that can contain members; since the extension block kind already exists in
  Roslyn 5.0, an unconditional substitution would change current behaviour for extension blocks in addition to
  admitting unions. Several of the lists also contain member and namespace kinds and cannot be replaced by the helper
  at all, so where a list must accept non-type members, keep the member kinds and replace only the type-declaration
  part. Prefer extending `SyntaxKindExtensions.IsTypeDeclaration`, which is the single predicate from which several
  of the wrong answers derive, in addition to the individual lists. The kind cannot be named directly in shared
  engine source, for the reason stated in CM-2 and CM-10; the Roslyn helper itself is not experimental and compiles
  against both variants, which makes it usable where the broad set is genuinely wanted. Add a unit test under
  `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/CodeModel/` that resolves `ToInsertPosition` for a
  union in the global namespace, for one in a namespace and for one nested in a class; the test can run only on the
  latest Roslyn variant with C# 15 enabled.
- Size: small for the code-model sites, plus one shared decision about the narrow predicate against the broad one
  that the linker, templating and design-time themes also depend on. The sites of those themes are counted there.
- Status: new work. No issue tracks it. Correcting the insert position alone does not make member introduction into a
  union work, because the injection rewriter still has no `VisitUnionDeclaration` override, which is CM-7 and the
  linker and advice theme.
- Verification: the code pass confirmed every kind list and the two failure modes, corrected the namespace outcome
  from misplacement to loss, showed that none of the lists is a copy of the Roslyn helper and added the omitted
  `SyntaxKindExtensions` predicate. The semantics pass confirmed that a union declaration is a namespace member
  with a member body, that the Roslyn helper is union-aware already in the stable 5.9.0 and that it is wider than the
  Metalama predicate, and dated the reachability of the defect to the move to the stable Roslyn. The scope pass found
  the change neither implemented, in progress nor tracked, and identified sixteen further consumers of that
  predicate.
- Open questions: none.

### CM-7. Syntax visitors have no `VisitUnionDeclaration` override

- Where, for the code-model visitors:
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/DependencyAnalysisHelper.FindDeclaredTypesVisitor.cs:47-58`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/DependencyAnalysisHelper.FindDeclaredAndAttributeTypesVisitor.cs:57-88`
  - The consumers that make the omission observable:
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/PartialCompilation.cs:434-445` and
    `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/SyntaxTreePipelineResult.Builder.cs:56-65`
  - The three classes whose base type does not descend at all:
    `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/PartialTypesHasher.cs:43-47`,
    `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/PartialTypesVisitor.cs:38-42` and
    `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.EmbeddedAttributeDetectorVisitor.cs:49`
  - The base classes that establish that Metalama does not alter the Roslyn dispatch:
    `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxWalker.cs:35`,
    `SafeSyntaxVisitor.cs:31`, `SafeSyntaxVisitor{T}.cs:32` and `SafeSyntaxRewriter.cs:35`
  - The kind list on which the fix also depends:
    `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35` and `:41`
  - The other affected visitors, which belong to the templating, linker and design-time themes:
    `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs:204-212`,
    `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateAnnotator.cs:743-754`,
    `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerLinkingStep.LinkingRewriter.cs:37-70`,
    `Metalama.Framework/src/Metalama.Framework.Engine/ReferenceGraph/ReferenceIndexWalker.cs:221-280` and
    `Metalama.Framework/src/Metalama.Framework.Sdk/AssertionFailedInterpolatedStringHandler.cs:347-355`
- What happens today: the outcome depends on the base class, and the three base classes behave differently. The
  generated `VisitUnionDeclaration` of `CSharpSyntaxVisitor` and of its generic counterpart calls `DefaultVisit`,
  and `CSharpSyntaxRewriter` declares its own override that calls the update method on the visited children without
  going through `DefaultVisit`. Only `CSharpSyntaxWalker` overrides `DefaultVisit` to descend into the children. In a
  walker the members of a union are visited and only the type-level logic is skipped, which is the case of both
  dependency visitors; in a rewriter the members are rewritten and only the type-level logic is skipped; in a plain
  visitor nothing happens at all, so the two partial-type visitors return a null hash and an empty array and the
  embedded-attribute detector examines nothing. For the two code-model visitors the union is not recorded as a
  declared type, and the effect is larger than the original report stated. In the design-time result builder the
  declared types are used only to compute the file paths of the other declaring syntax references, so the recorded
  dependencies of a file omit the other files that declare parts of a partial union. In the closure computation of
  `PartialCompilation` the declared types seed the recursion, so a top-level union is absent from the closure, from
  the set of types of the partial compilation and from the derived-type index, which is why it is also absent from
  `ICompilation.Types` at design time and why a type fabric on a union is silently skipped.
- Consequence: silent wrong output for the two code-model visitors, with one downstream path that throws: the
  incremental aspect repository raises an `InvalidOperationException` when an aspect queries it for a declaration of
  a union that the partial compilation does not contain.
- Proposed change: add `VisitUnionDeclaration` overrides that route to the same helper as `VisitStructDeclaration`,
  with four qualifications. First, the overrides cannot be unconditional: neither `UnionDeclarationSyntax` nor the
  visitor method exists in the Roslyn 5.0.0 variant, and both `Metalama.Framework.Engine` and
  `Metalama.Framework.DesignTime` are compiled in that variant, so this requires the mechanism of CM-10 and, while
  the latest variant is built against the consumed preview, a suppression of the experimental diagnostic that becomes
  unnecessary once the latest variant moves to the stable Roslyn. Second, three of the affected sites cannot receive
  the override at all under the current package references, because `Metalama.Framework.Sdk` and
  `Metalama.Framework.Engine.Analyzers` are single builds pinned to the minimum Roslyn API version and
  `Metalama.SourceTransformer` takes Roslyn through `Metalama.Compiler.Sdk`; those rows are cosmetic or internal, so
  the practical answer is to leave them unchanged and record why. Third, for the two code-model visitors the override
  alone is not sufficient, because the nested-type loop of the shared helper is guarded by the kind predicate of
  CM-2, so types nested in a union would still be skipped. Fourth, the struct helper may be shared only where it does
  not read the parameter list as a primary constructor parameter list, because on a union that slot holds the case
  types and its entries normally have no identifier. A guard rule that flags a visitor overriding
  `VisitStructDeclaration` without `VisitUnionDeclaration` is feasible, but it would be a new and unrelated
  diagnostic in a performance analyzer whose only diagnostic today is `LAMA0860`, and it would fire on the projects
  that cannot comply, which under the continuous integration build is an error.
- Size: small per visitor where the struct helper can be reused unchanged, medium in total across themes, plus the
  one-off cost of introducing per-variant conditional compilation into files that have none today.
- Status: new work. No issue tracks it, and no visitor override exists anywhere in the repository. The closed #1881
  removed every conditional compilation block from production source and made the generator strip every experimental
  declaration from the grammar, which is why no generated visitor knows the node either; the variant policy of
  #1898, delivered by #1911, is the reason the Roslyn 5.0.0 variant still has to compile the same sources.
- Verification: the code pass confirmed that no override exists in the repository, that the enumerated production
  files are exactly the set that overrides the class, struct or record visit, and corrected the design-time
  consequence, the consequence class and several line ranges. The semantics pass confirmed the syntax node shape, the
  synthesized members, and refuted the uniform descent mechanism by distinguishing walkers, rewriters and plain
  visitors. The scope pass found the change neither implemented, in progress nor tracked, and confirmed that neither
  named tool carries a visitor-completeness rule today.
- Open questions: none.

### CM-8. The synthesized union members reach the code model with syntax the model does not expect

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceConstructor.cs:112` (`IsPrimary`) and
    `:116-148` (`GetBaseConstructor`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SymbolExtensions.cs:249-252`
    (`GetBackingField`), `:283-291` (`IsPrimaryConstructor`), `:393-467` (`GetPrimarySyntaxReference`) and `:472`
    (`GetPrimaryDeclarationSyntax`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/DeclarationExtensions.cs:279-334`
    (`IsAutoProperty` and `GetPropertyKind`), `:380-391` (`HasExplicitAccessorBody`), `:664-665` (the
    implicit-constructor predicate)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceProperty.cs:46` and `:115`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceMemberOrNamedType.cs:110-130`
    (`PrimarySyntaxTree`) and
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SymbolBasedDeclaration.cs:53-57`
    (`IsImplicitlyDeclared`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/SymbolExtensions.cs:18-64`, with the throw at `:63`,
    and its only two call sites at
    `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.InlineabilityAnalyzer.cs:94` and
    `:285`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.ReachabilityAnalyzer.cs:55-120`,
    `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SubstitutionGenerator.cs:908-911`
    and `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRecordHelper.cs:45`, `:65`, `:236`
  - `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:83-93` and `:241-247`,
    `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityExtensions.cs:586-589`,
    `Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:404-421` and `:1029`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/Constructors/IntroduceConstructorAdvice.cs:86`
    and `:96`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/Bugs/Bug582_EqualityContract.cs:16`
    and its expected output `Bug582_EqualityContract.t.cs:2` (the pinned precedent for a compiler-synthesized member
    that becomes an override target)
- What happens today: the synthesized per-case constructors have no declaring syntax, which is now verified in the
  Roslyn sources rather than assumed, so `IsPrimary` is false, `GetBaseConstructor` takes the arm for the absent
  syntax and resolves the implicit base constructor without throwing, and `PrimarySyntaxTree` falls back to the
  containing type. Those constructors have one parameter, so the implicit-constructor predicate does not classify
  them as implicit constructors, which matches their public signature. Because a union is a struct, the type also
  carries the ordinary synthesized parameterless struct constructor, and it is that constructor that the predicate
  classifies as the implicit one, so advice that replaces the implicit constructor of a union targets it rather than
  a case constructor. The synthesized `Value` property is declared with the union declaration as its syntax, which is
  also verified. `GetPropertyKind` finds the backing field and a non-empty set of declaring syntax references and
  then calls `HasExplicitAccessorBody`, which does not crash: its switch expression matches a property, an event and
  an indexer declaration only and falls into the default arm, so the property is classified as an automatic property,
  which is accurate. The cast in `SourceProperty` uses a safe conversion and survives. The crash is real but reaches
  the linker by one route only: `Value` is implicitly declared, and the override advice for a field, property or
  indexer requires an explicitly declared target, so a plain override is rejected as ineligible before any linking.
  The route that reaches the linker is an introduction with an override strategy, which is exactly the route the
  pinned record test uses for the synthesized equality contract property. Once `Value` is an override target, the
  linker analysis calls the declaration-flag reader, which switches on the kind of the primary declaration and throws
  an `AssertionFailedException` for a kind outside its list. The same omission appears in the substitution generator,
  where a call to `meta.Proceed()` reports `LAMA0651` for a record declaration and throws for any other kind, and in
  the record helper, which silently skips any kind other than the two record kinds.
- Consequence: assertion or crash in the linker analysis for an aspect that introduces a member named `Value` with an
  override strategy on a union. For the constructors there is no crash, but an aspect that introduces an instance
  field, an auto-property, a field-like event, a public single-parameter constructor or a constructor without a
  `this` initializer into a union produces code that the compiler rejects with CS9373, CS9374 or CS9375, and the
  compiler adds `Value` unconditionally, without the duplicate-signature check that it performs for record members,
  so a `Value` property emitted into the union declaration collides with the synthesized one.
- Proposed change: treat `Value` like the synthesized record members. Add the union declaration kind to the kind list
  of the declaration-flag reader, whose cast to a member declaration remains valid because the union declaration
  syntax derives from the type declaration syntax; add the kind to the arm of the substitution generator that reports
  `LAMA0651`, so that `meta.Proceed()` in an override of `Value` is rejected with a diagnostic instead of an
  assertion, noting that the message of the existing descriptor names records only and needs a general wording or a
  second descriptor; and extend the record helper, which belongs to the linker and advice theme. Beyond the switch
  arms, decide and implement what happens when an aspect targets a union at all, because the compiler rejects most of
  what the pipeline would emit; the likely outcome is a diagnostic that rejects the unsupported advice rather than a
  code path that emits it. Add a code-model unit test that reads `Constructors`, `Properties`, `IsAutoPropertyOrField`
  and `IsImplicitlyDeclared` of a union, remembering that the test can run only in the latest Roslyn variant. The
  variant constraint of CM-10 applies, and a numeric case label is valid in both variants.
- Size: medium, shared with the linker and advice theme, plus the diagnostics for unsupported advice on a union.
- Status: new work. The clause that adds the union kind to the linker kind list is one instance of CM-6 and must be
  counted once. The mechanism to extend is the one built by the open pull request #1879, which materializes
  compiler-synthesized record members so that `meta.Proceed()` works (#1343); every gate of that mechanism is keyed on
  a record, so a union story must be based on it and must state the dependency on it merging. The open #985 is the
  nearest placeholder for post-C#-14 language work but does not scope this.
- Verification: the code pass confirmed every cited member against the Roslyn union symbol sources and against the
  repository, established that `HasExplicitAccessorBody` copes, identified the single reachable crash route through
  the introduction advice and added the omitted substitution-generator site. The semantics pass confirmed the absent
  declaring syntax of the constructors, the union declaration syntax of the `Value` property and its accessor, the
  additional parameterless struct constructor, and the three compiler restrictions with their error codes. The scope
  pass found the change neither implemented, in progress nor tracked, and confirmed that the record helper is still
  gated on the two record kinds.
- Open questions: none. The behaviour of `HasExplicitAccessorBody` on a union declaration is answered, and
  `meta.Proceed()` in an override of `Value` must be rejected in the same way as for a synthesized record member. Two
  residual uncertainties do not change the verdict: the injection step may fail earlier for its own reason, and an
  introduction over one of the synthesized single-parameter constructors was not traced end to end.

### CM-9. A run-time union in a file that also contains compile-time code breaks the compile-time compilation

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.FindCompileTimeCodeVisitor.cs:58-99`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:760-767` (the gate
    that selects the trees), `:279` and `:347-353` (the compile-time language version and the parse options of the
    generated tree), `:599-651` (how the emit failure is reported), `:509-524` (the reparse when the compile-time
    source is written to disk)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.ProduceCompileTimeCodeRewriter.cs:1452-1478`
    (`VisitTypeOrNamespaceMembers`, the kind-based dispatch, with the case label at `:1460`), `:1496-1524` (the two
    namespace visits), `:1526-1531` (the kind-based filter of `VisitCompilationUnit`), `:540-560` (the member switch
    of the compile-time type transformation and its default arm), `:254-376` (`PopulateNestedCompileTimeTypes`, whose
    default arm at `:372-375` drops the member)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-41`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-92` (the
    rejection of a preview language version unless preview features are allowed)
- What happens today: the outcome depends on where the union is declared, and the mechanism is a dispatch on the
  syntax kind rather than a missing visitor override. `FindCompileTimeCodeVisitor` overrides the six classic
  type-declaration visits only, so a union never marks a tree as containing compile-time code, which is correct
  because a union is run-time code. When the same file also declares an aspect, the tree is rewritten. A union
  declared outside any namespace is removed by the filter of the compilation-unit visit and never reaches the
  compile-time compilation, so the case named by the original report is in fact the safe one. A union declared inside
  a block-scoped or file-scoped namespace falls into the default arm of the member dispatch, which calls the base
  rewriter and copies the union; the namespace is preserved only when the aspect is declared in the same namespace. A
  union nested in a compile-time type, for example inside an aspect class, falls into the default arm of the member
  switch of the compile-time type transformation and is likewise copied. A union nested in a run-time type reaches
  the default arm of the nested-type population and is dropped, which is the correct outcome although the code does
  not recognize the union kind. For the two copying
  paths the compile-time tree is created with the parse options of the compile-time language version, which is C# 14,
  and Roslyn checks the union feature when it builds the declaration table rather than only in the parser, so the
  compile-time build fails on a declaration the user never wrote for compile-time. The scenario is reachable today
  only when preview language features are allowed, because the pipeline rejects a preview language version otherwise
  and admits no version above C# 14.
- Consequence: build error in the compile-time compilation, with a message that names a language version or a missing
  framework type rather than the real cause. The specific message depends on the Roslyn build and on the compile-time
  language version: the preview-feature error against the Roslyn build consumed today, CS9327 against the stable
  Roslyn while the compile-time compilation stays at C# 14, and a missing `System.Runtime.CompilerServices.IUnion`
  error once the compile-time compilation is raised to C# 15, because that compilation always targets
  `netstandard2.0`, which provides neither `IUnion` nor the union attribute, while Roslyn adds `IUnion` to the
  implicitly implemented interfaces of every union.
- Proposed change: route every type declaration through the existing type-declaration path instead of adding a
  visitor override for the new node. The minimal and variant-safe change is to relax the two kind-based tests that
  gate the dispatch, the case label of the member dispatch at `:1460` and the member filter of the compilation-unit
  visit at `:1529-1531`, so that any type declaration or base type declaration is recognized, and to add a matching
  arm to the member switch of the compile-time type transformation at `:540-546`. Overriding `VisitUnionDeclaration`,
  as originally proposed, does not compile in the Roslyn 5.0.0 variant project, which compiles the same sources.
  Apply the same treatment to `FindCompileTimeCodeVisitor`, so that a compile-time type nested in a union is
  discovered and a compile-time union is classified rather than silently skipped. Add compile-time tests for the two
  copying placements, a run-time union and an aspect in the same namespace, and a union nested in an aspect class;
  the test must be validated both while the compile-time compilation is at C# 14 and after it is raised to C# 15,
  because the failure it guards against changes form between the two.
- Size: small.
- Status: new work, owned by the templating and compile-time theme rather than by this one; it is recorded here
  because the defect originates in the kind-based dispatch discussed under CM-6 and CM-7. No issue tracks it. The
  closed #1881 added the Roslyn 5.10 grammar and stated that C# 15 required no change, and the closed #1896, merged
  as #1910, pins the template language to C# 14, so no test can declare a union in compile-time code today. The
  precedents for the same shape of defect in the same rewriter are the closed #627, #1533 and #1431.
- Verification: the code pass confirmed the gate, the parse options and the misleading report, and refuted three
  points: the mechanism is a kind-based dispatch, the top-level union is dropped rather than copied, and a union
  nested in a compile-time type is copied, which the original report did not mention. The semantics pass confirmed
  that the union feature is checked while the declaration table is built and not only in the parser, which is what
  makes the transplanted node fail, and corrected the diagnostic identity and the additional failure regime once the
  compile-time compilation is raised. The scope pass found the change neither implemented, in progress nor tracked,
  and confirmed that two of the cited paths are already kind-agnostic while the failing ones are the hand-written
  kind lists.
- Open questions: none.

### CM-10. Every engine reference to the new Roslyn members needs a variant strategy

- Where:
  - `eng/RoslynVersions/Roslyn.5.0.0.props:8-10` ("This variant defines no constant. No production source branches on
    the variant.") and `eng/RoslynVersions/Roslyn.5.10.0.props:8-10`, whose line `:10` is the sole definition of
    `ROSLYN_5_10_0_OR_GREATER`
  - `Metalama.Framework/src/Metalama.Framework.Engine.5.0.0/Metalama.Framework.Engine.5.0.0.csproj:6` (the variant
    compiles the same source glob as the latest variant)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Metalama.Framework.Engine.csproj:37-38` (the per-variant
    generated compile directory) and `.gitignore:62` (that directory is generated and ignored)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/LanguageVersionExtensions.cs:33-34` (the
    numeric casts, the precedent for a value-based mechanism)
  - `Metalama.Framework/src/Metalama.Framework.Engine/SerializableIds/SymbolId.cs:44-60` (reflection over an internal
    Roslyn type, the precedent for a shim)
  - `Metalama.Framework/src/Metalama.Framework.Engine/SyntaxGeneration/SyntaxFactoryDebugHelper.cs:21` (the
    per-variant run-time guard)
  - `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:25-43` (the repository already treats a reference to an
    experimental Roslyn declaration as a compile error and strips such declarations)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:9` and `:14-15` (the
    public and annotated helper, declared in `Metalama.Framework.Engine` and not in `Metalama.Framework.Sdk`)
- What happens today: `Metalama.Framework.Engine` is compiled once per Roslyn variant from one shared source glob,
  and Roslyn 5.0 has none of `UnionDeclarationSyntax`, `SyntaxKind.UnionDeclaration`, `SyntaxKind.UnionKeyword`,
  `SyntaxKind.ClosedKeyword`, `ITypeSymbol.IsUnion`, `ITypeSymbol.UnionCaseTypes`, `ITypeSymbol.IsClosed` or
  `ITypeSymbol.GetClosedDerivedTypeInfo`, so any of CM-1 to CM-9 that names them fails to compile in that variant.
  Two further constraints apply to the latest variant as it stands. First, the consumed build does not expose
  `ITypeSymbol.UnionCaseTypes` either, because that member was added to the Roslyn public API after the build was
  produced, so it becomes available only with the move to the next stable Roslyn. Second, every remaining member
  carries `RSEXPERIMENTAL006` in the consumed build, and the experimental diagnostic is treated as an error and fails
  the build unless it is suppressed; the repository has no such suppression, and its own code generator strips
  experimental declarations from the grammar for exactly this reason. The Roslyn 5.0.0 variant serves Rider and the
  C# Dev Kit, whose compiler cannot parse a union or a closed class, but it can still see such types in referenced
  assemblies, because Roslyn derives both predicates from metadata attributes on types read from a reference.
- Consequence: build error of the engine itself, for every implementation of the findings above: a missing member in
  the `Roslyn.5.0.0` variant and, in the latest variant while it is built against the consumed preview, an
  unsuppressed experimental diagnostic reported as an error. The failure occurs at build time. Nothing fails without
  a diagnostic and nothing fails at run time.
- Proposed change: decide once, for all themes, between four mechanisms, noting that only the last of them avoids the
  experimental diagnostic on the latest variant. The first is a conditional compilation block on
  `ROSLYN_5_10_0_OR_GREATER`, which requires updating the policy sentences of the two variant property files and of
  [`Directory.Packages.md`](../../../Directory.Packages.md) and [`updating-roslyn.md`](../updating-roslyn.md), which
  must be renamed together with the latest variant when it is renumbered, and which still needs the experimental
  diagnostic suppressed until the marker is removed. The second is a numeric kind value, 9082 for the union
  declaration, 8452 for the union keyword and 8453 for the closed keyword, behind a run-time guard on the variant;
  this compiles in both variants and names no experimental member, but a preview value may move before the stable
  release, so resolving the kind once by name into a static field is safer than a literal. The third is a per-variant
  source file, for which the project already has a compile hook, but the directory that hook names is produced by the
  code generator and is ignored by version control, so a hand-written file there is not viable without extending the
  mechanism. The fourth is a single reflection-based shim that answers `IsUnion`, `IsClosed`, `UnionCaseTypes` and
  the closed derived-type information; one implementation compiles in both variants, returns false or an empty result
  on Roslyn 5.0, raises no experimental diagnostic and is the only mechanism that can reach `UnionCaseTypes` before
  the move to the stable Roslyn, but it repeats the mechanism that the closed #1215 deliberately removed from
  `SymbolExtensions` in favour of conditional compilation. The syntax factory call of CM-3 and the visitor overrides
  of CM-7 fit neither the numeric value nor the shim, because a numeric kind cannot override a virtual method or
  construct a node. The decision must also state what the Roslyn 5.0 variant answers for a union or closed type that
  arrives through a referenced assembly: returning false disagrees with the latest variant on the same
  reference, whereas a fallback that looks for the two metadata attributes by name agrees with it.
- Size: a decision, then small per site. The decision additionally covers the experimental suppression and the
  behaviour of the lower variant on referenced union and closed types.
- Status: decision required. The decision is which of the four mechanisms the engine adopts for the C# 15 Roslyn
  members, and it must be taken before any union or closed-hierarchy code is written. It belongs as a sub-issue of
  the open meta-issue #1921, none of whose sixteen sub-issues covers it. The closed #1881 created the present
  situation by removing 177 conditional compilation blocks from production source; the closed #1215 is the precedent
  that argues against the shim; the closed #1898 fixed which variant serves which host, and therefore fixed that the
  members are needed in the latest variant only; the open #1217 is the adjacent story for source that must behave
  differently between Roslyn versions.
- Verification: the code pass confirmed the shared source glob, the absence of the members from the Roslyn 5.0 public
  API and both precedents, and corrected three points: the consumed build lacks `UnionCaseTypes`, every union and
  closed member of that surface is experimental and therefore a compile error, and a per-variant registration of a
  shim is not possible because every variant project compiles the same glob. The semantics pass confirmed the
  absence of all members from the 5.0 branch, the numeric kind values, the treatment of the experimental attribute as
  an error, and that the lower variant can still observe union and closed types through metadata. The scope pass
  found the decision neither taken nor tracked, corrected the claim that the conditional compilation option requires
  lifting a policy (the written policy already permits a symbol when the source has to branch on a distinction that
  no existing symbol expresses) and added the per-variant generated directory as a fourth mechanism.
- Open questions: the outcome of the decision, and whether the public helper `SyntaxKindExtensions`, which is part of
  the annotated public surface of `Metalama.Framework.Engine`, may mention the new kinds at all.

## Withdrawn findings

No finding of the original report was withdrawn. All ten findings survived the three verification passes. Two of
them changed materially and are recorded above rather than withdrawn, because their central claim held while their
mechanism did not: CM-1 predicted the compiler error CS9373 on generated code, whereas the introduced member is in
fact dropped by the linker injection rewriter and no compiler error is reported; and CM-9 predicted that a top-level
run-time union is copied into the compile-time compilation, whereas a top-level union is filtered out and the copying
cases are a union declared inside a namespace and a union nested in a compile-time type. Three claims of the original
report were refuted in detail without removing the finding that carried them: the citation of the
`KindCheckOptimizationAnalyzer` tests in CM-2 names the region of a different property and those tests do not pin any
set of syntax kinds; the builder and introduced-type paths of CM-1 named a directory layout and a line number that do
not exist; and the proposed substitution of Roslyn's own type-declaration helper in CM-6 is not behaviour-preserving,
because that helper is wider than the Metalama predicate.

## Non-findings

The following were checked and found unaffected by union types and by closed hierarchies.

- Identity. A union type receives the documentation identifier `T:Ns.Pet`, its synthesized constructors and `Value`
  property receive ordinary member identifiers, and a case type is an ordinary type with its own identifier
  (`Metalama.Framework/src/Metalama.Framework.Engine/SerializableIds/SerializableDeclarationIdProvider.FromSymbol.cs:97-155`,
  `Metalama.Framework/src/Metalama.Framework.Engine/SerializableIds/DocumentationIdHelper.GeneratorOfDeclarationIdFromDeclaration.cs:59-63`).
  The serializable type identifier writes the type in C# syntax
  (`Metalama.Framework/src/Metalama.Framework.Engine/SerializableIds/SerializableTypeIdGenerator.cs:117-141` and
  `:202-226`), and `SymbolId.cs:44-60` wraps the Roslyn symbol key, which handles every symbol. The durable and
  identifier-based references build on these identifiers. No round trip is affected.
- Serialization. The compile-time serialization binders bind assembly-qualified names of compile-time types
  (`Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/Serialization/`), and the type serializers serialize
  the identifiers above. The intrinsic type extensions switch on the Roslyn type kind for enums, type parameters and
  arrays only. Unaffected.
- `Metalama.Framework.Introspection` and the introspection layer of the engine do not read `TypeKind`, `IsRecord` or
  `IsSealed`. `Metalama.Framework.Workspaces`, `Metalama.Framework.DesignTime` and `Metalama.LinqPad` do not switch on
  `TypeKind`. Unaffected.
- The declaration factory. The type accessor dispatches on the symbol kind and reaches the named-type path for a
  union, which treats it as any named type
  (`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Factories/DeclarationFactory.Symbols.cs:117-131` and
  `:160-185`). The reference factories are keyed by the symbol kind and by the reference target kind, neither of which
  gains a value. Unaffected.
- Equality and structural comparison. Equality of two references to the same union, or to the same closed class, is
  structural on the name, the arity and the containing declaration and does not read the type kind beyond the struct
  arm. The symbol-free conversion classifier does not know the union conversion from a case type to the union, but it
  is used only when symbols are bypassed, that is for introduced types, which cannot be unions.
- The derived-type index records the implicit `IUnion` interface of a union as any interface, and a closed class as
  any base class
  (`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/DerivedTypeIndex.Builder.cs:58-96`). Unaffected.
- Generic context and type-symbol rewriting. A generic union is a generic struct for both. Unaffected.
- The type modifier list of the code model is used only by the type-introduction transformation, so the modifiers of a
  source closed class or union are never regenerated, and the linker keeps the original modifier list, including
  `closed`.
- The type-reference generator produces a reference to a union from its name and type arguments, like any named type.
  Unaffected.
- The type-kind constraint of a generic parameter is unaffected: a union satisfies the struct constraint, and neither
  proposal adds a constraint kind.
- The record copy-constructor predicate requires `IsRecord`, which is false for a union, so the record-specific
  constructor logic does not misfire on the synthesized union constructors.
- Closed enums are not part of C# 15, so the handling of `TypeKind.Enum` is unaffected.
- A `closed` or `union` declaration inside compile-time code fails because the compile-time compilation is C# 14. That
  is the language version theme, not a code-model defect; see also CM-9 for the case of a run-time union in the same
  file.

## Related themes

The findings of this theme cross-reference the following work owned elsewhere. The prefix of a finding identifies its
theme: LV for the language version and the hosts, TP for the syntax generator and the templates, CM for this theme, LK
for the linker and the advice, DT for the design time, UT for the user target frameworks, the tests and the
documentation, and PR for `Metalama.Premium`.

- Hand-written type-declaration kind lists. CM-2 and CM-6 are the code-model half of one edit that LK-3, DT-1 and
  DT-6 also report. The predicate at
  `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35` and `:41` is the
  single choke point, and the decision inside that edit, a type test against an added kind, is shared with those three
  findings. It is one work item and not five.
- Syntax visitors. CM-7 is the inventory, and LK-10 is one member of it, the design-time classifier, which cannot be
  corrected alone because the template annotator has no union dispatch either. Both need the mechanism of CM-10.
- The compile-time code finder and rewriter. CM-9 and TP-6 name the same two visitors of the compile-time compilation
  builder. The theme that owns them is the syntax generator and templates theme; the edit must be made once.
- The design-time generated partial part. CM-3, DT-2 and LK-4 describe one arm of one factory, owned by the design-time
  theme. LK-4 contributes the verified statement that the closed modifier needs no counterpart in the generated part.
- Injection, linking and eligibility for union members. CM-8 belongs to the chain formed with LK-1, LK-2 and LK-8, owned
  by the linker and advice theme: the injection dispatch, the linking dispatch and the eligibility rules must move
  together, because correcting the injection alone turns a dropped member into code that the compiler rejects. The
  mechanism to extend is the one built by the open pull request #1879.
- Closed hierarchies. CM-4 and CM-5 are the writer half and the reader half of one feature that also contains LK-5,
  which duplicates CM-4, and the two verified negative statements TP-10 (the template compiler and the syntax generator
  need nothing) and UT-15 (the pattern libraries need no product change).
- The Roslyn variant strategy. CM-10 is a prerequisite of every other finding of this theme and of at least fourteen
  findings of the other themes. It must be decided before any union or closed-hierarchy code is written.
- The language version. Every finding of this theme is latent until the language version plumbing admits C# 15, which
  is owned by the language version theme (LV-2, LV-3, LV-6, LV-7 and the template constants of TP-2 and TP-8), and
  until the latest Roslyn variant is renumbered to the stable 5.12, which is owned by LV-12, LV-13 and LV-14 with the
  regeneration of TP-1 and TP-9 and the mirror edit PR-1 in `Metalama.Premium`.
- Consumers of the code model surface. The pattern and extension libraries read a union as an ordinary struct with an
  opaque `Value` property (UT-14 and its sub-findings), and the architecture rules read it through the reference graph
  (PR-12). Both consume the members proposed by CM-1 and share one test matrix.
