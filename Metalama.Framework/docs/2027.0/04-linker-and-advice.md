# 04. The linker, advice, transformations and eligibility

This document covers the aspect linker (`Metalama.Framework.Engine/Linking`), the advice factory and the advice
implementations (`Advising`, `AdviceImpl`), the transformations, the eligibility rules, the code formatter and the
introduction builders, and the design-time partial-type generator insofar as it emits type declarations. It records
how each of them behaves when C# 15 syntax reaches it, and what has to change for Metalama 2027.0. The analysis reads
the code as it stands on 2026-09-03 on branch `topic/2027.0/26-09-03-update-eng-7e3j07` of the `Metalama` repository.
Each finding was then re-checked by three verification passes: a code pass that re-read the cited code and tried to
falsify the claim, a semantics pass that re-checked every external premise against `dotnet/roslyn` and
`dotnet/csharplang`, and a scope pass that established whether the proposed change is already implemented, in flight
or tracked. The platform baseline PB-2027.0 is decided by [`platform-support.md`](../platform-support.md), the
permitted package versions by [`Directory.Packages.md`](../../../Directory.Packages.md), and the procedure for moving
to a new Roslyn by [`updating-roslyn.md`](../updating-roslyn.md); this document cites them rather than restating them.

No project was built and no test was run for this analysis.

## Summary

1. Unions are the only C# 15 feature that reaches the linker as a new syntax node kind, and no component has a
   dispatch for it. The injection rewriter, the linking rewriter and the design-time text-span classifier each
   dispatch on the concrete type-declaration node types they know and let every other one fall through to the Roslyn
   default visit (LK-1, LK-2, LK-10). Advice that produces an injected member on a union therefore fails, and advice
   that only adds an attribute or an interface is dropped without a diagnostic.
2. None of that is observable in the shipped configuration. No Roslyn that Metalama consumes exposes C# 15 as a
   non-preview language version, and Metalama caps the language version at C# 14, so a union declaration cannot reach
   the linker unless the project sets `AllowPreviewLanguageFeatures` on the latest variant. The failures become
   reachable with the move to the stable Roslyn 5.12 and the C# 15 language-version work, both owned by theme 01, and
   every repair that names a C# 15 API member depends on the variant gating strategy owned by theme 03.
3. Repairing the dispatch is necessary and not sufficient. A union forbids instance fields, auto-properties and
   field-like events, forbids an explicitly declared public constructor with a single parameter, and requires every
   explicit constructor to chain to a synthesized or explicitly declared constructor. Several advice kinds emit
   exactly those shapes, so the injection has to be refused by eligibility or by a validation diagnostic (LK-8).
4. The shared syntax-kind lists produce one wrong diagnostic today, independently of any C# 15 work: a
   `partial union` reports `IsPartial == false`, so the design-time generator reports LAMA0048 and
   `IntroduceMemberAdvice` reports `CannotIntroducePartialMemberToNonPartialType` (LK-3). Two further sites of the
   same family raise an assertion once a union becomes reachable, and both are reached through the synthesized
   `Value` property rather than through the per-case constructors.
5. The design-time partial-type generator emits a struct declaration for a union target, which the compiler rejects
   with CS0261 because two partial declarations of different kinds never merge (LK-4). The same finding establishes
   that the `closed` modifier needs no counterpart there, because the compiler unions the modifiers of all partial
   parts.
6. Extension indexers require two separate changes: lifting the deliberate rejection of an indexer target in
   `IntroduceIndexer` and adding the rules and the implementation methods the language requires (LK-6), and covering
   the override path, whose proceed expression already substitutes the receiver parameter for `this` (LK-7). The
   second finding also records a pre-existing defect of the already shipped C# 14 extension members: the non-inlined
   trampolines emit `this.<member>`, which is invalid inside an extension block.
7. Closed classes cause no failure. An aspect cannot introduce one, which is a feature gap bounded by four external
   constraints (LK-5); a class that an aspect introduces below a closed base type is admitted, and the one semantic
   consequence is that such an introduction can make a previously exhaustive user switch non-exhaustive.
8. Labeled `break` and `continue` break no linker rewrite, because no rewriter reconstructs those statements. The
   exposure is elsewhere: inlining copies user labels verbatim into one flattened statement list, so a labeled loop
   in a template collides with a labeled loop in the target, and two overrides expanded from one template collide
   with each other (LK-9).

## Findings

### LK-1. The injection rewriter does not visit union declarations, so injected members targeting a union are never inserted

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs:316`, `:318`, `:320`,
    `:322`, `:324` (the five overrides), `:359-455` (`VisitTypeDeclaration<T>`), `:398-402`, `:405-409` (the only
    consumers of the two insert-position forms), `:364` (introduced interfaces), `:370` (member-level
    transformations of the primary constructor), `:451-452` and `:70-136` (attribute lists), `:1113-1134`
    (`VisitMember`), `:473` (the only node-tracking site), `:1836`, `:1874`, `:1894` (the namespace and compilation
    unit visitors)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.TransformationCollection.cs:334-344`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.cs:352`, `:377`, `:565`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionRegistry.cs:81-83`, `:147`, `:256`,
    `:323-341`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LexicalScopeFactory.cs:52`, `:121`, `:186`, `:193`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateExpansionContext.cs:145`, `:161`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/CodeModelExtensions.cs:66-89`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:19-21`, `:23-46`,
    `:51-76`, `:112-120`, `:131`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/BuilderData/NamedDeclarationBuilderData.cs:25-42`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Override/OverrideMemberTransformation.cs:41`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:69-79`
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Invariant.cs:146-165`
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxRewriter.cs:44-67`
  - `Metalama.Framework/src/Metalama.Framework.Engine.5.0.0/Metalama.Framework.Engine.5.0.0.csproj:6`
  - `eng/RoslynVersions/Roslyn.5.10.0.props:10`, `eng/RoslynVersions/Roslyn.5.0.0.props:8-10`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.LinkerTests/Tests`,
    `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/DesignTimeInvalidCode/UnknownAccessorInTemplate.cs:7`
- What happens today: nothing, because the situation is not reachable in the shipped configuration. The C# parser
  produces a union declaration only when the language version enables the feature, and that feature requires
  `LanguageVersion.Preview` in the consumed Roslyn build. Metalama caps the language version at C# 14, and its
  targets rewrite an implicitly set language version of 15.0 down to 12.0. The description below therefore applies
  once C# 15 parsing is enabled, that is after the move to the stable Roslyn 5.12 with C# 15 added to the supported
  versions, or earlier in a project that sets `AllowPreviewLanguageFeatures` while running on the latest variant.
- The injection rewriter overrides `VisitClassDeclaration`, `VisitStructDeclaration`, `VisitInterfaceDeclaration`,
  `VisitRecordDeclaration` and `VisitExtensionBlockDeclaration` (`LinkerInjectionStep.Rewriter.cs:316-324`), each
  delegating to `VisitTypeDeclaration<T>` (`:359`). There is no `VisitUnionDeclaration` and no override of
  `VisitCore` or `DefaultVisit`, so a union is reproduced by the base Roslyn rewriter, and `VisitMember` (`:1132`)
  routes a nested union through the same default visit. `VisitTypeDeclaration<T>` is the only consumer of
  `InsertPosition( After, member )` (`:398-402`) and of `InsertPosition( Within, typeNode )` (`:405-409`), the only
  caller of `GetIntroducedInterfacesForTypeDeclaration` (`:364`) and of the primary-constructor member-level
  transformations (`:370`), and the only place that rewrites the attribute lists of the type itself (`:451-452`).
  Members of a union are still reached by the default visit, so `VisitParameter` and `VisitAccessorDeclaration` run,
  while the four private handlers for methods, constructors, properties and indexers do not, so contracts,
  initializer statements and introduced attributes targeting union members are never injected.
- The insert positions computed for a union are consumed by nothing. `CodeModelExtensions.cs:66-89` derives the
  position of a source declaration from `FindMemberDeclaration()` and tests `is BaseTypeDeclarationSyntax` at
  `CodeModelExtensions.cs:73`; `SyntaxExtensions.cs:23-46` lists every member kind except the union declaration, so
  the walk continues to the enclosing namespace and the result is `InsertPosition( After, <namespace> )`, which no
  visitor consumes. A member introduced into a union receives `InsertPosition( Within, <union declaration> )`
  (`NamedDeclarationBuilderData.cs:25-42`) and an override of a source member receives
  `InsertPosition( After, <member declaration> )` (`OverrideMemberTransformation.cs:41`), and both forms are consumed
  only from `VisitTypeDeclaration`.
- The failure sites, in the order in which they are reached, are three. Template expansion fails first:
  `TemplateExpansionContext.cs:145` and `:161` obtain a lexical scope inside `GetInjectedMembers`
  (`LinkerInjectionStep.cs:565`), which runs before the rewriter (`:352`) and before the registry (`:377`), and
  `LexicalScopeFactory.cs:186` and `:193` derive the type declaration through `GetDeclaringType`, whose kind list
  (`SyntaxExtensions.cs:112-120`) also omits the union declaration, so the value is null for a member of a
  namespace-level union. For a union nested in a class, `GetDeclaringType` returns the enclosing class, which is the
  wrong lexical scope rather than a failure, and execution continues. A union declared directly in a compilation unit
  with no namespace fails still earlier, in `SyntaxExtensions.cs:19-21`. When neither applies, the failure is in the
  registry: `LinkerInjectionRegistry.cs:256` runs every injected member through `GetCanonicalSymbolForInjectedMember`
  (`:323-341`), whose line 326 calls `GetCurrentNode( injectedMember.Syntax ).AssertNotNull()`, and tracking is
  performed only inside the rewriter (`LinkerInjectionStep.Rewriter.cs:473`), so a member that was never inserted is
  untracked. When the union is the only content of its file, the rewriter changes nothing and line 325 throws first,
  because `_transformedSyntaxTreeMap` (`:81-83`) holds only the trees the rewriter modified.
- Consequence: crash, with silent wrong output for two advice kinds. Every advice that expands a template or produces
  an injected member on a union or on a member of a union fails, and the exception type depends on the build
  configuration, because `Invariant.AssertNotNull` throws only under the `DEBUG` symbol
  (`Metalama.Framework.Sdk/Invariant.cs:146-165`) and otherwise returns the null reference, so the same sites raise
  `NullReferenceException` or `ArgumentNullException` in a shipped build; `IntroduceAttribute` on the union type and
  `ImplementInterface` with no member to introduce produce no injected member and are lost without a diagnostic.
- Proposed change: add a dispatch that does not depend on the Roslyn 5.10 API surface, so that the same source builds
  for the 5.0.0 variant. The variant project compiles every source file of the base project
  (`Metalama.Framework.Engine.5.0.0.csproj:6`) and the Roslyn 5.0 public API contains no union member at all, which
  is the reason to avoid naming `UnionDeclarationSyntax`; the experimental marker is a separate and lesser obstacle,
  and it is already absent on the Roslyn version the 2027.0 baseline is expected to consume. `Rewriter` derives from
  `SafeSyntaxRewriter`, whose `Visit` is sealed and whose documented extension point is `VisitCore`
  (`SafeSyntaxRewriter.cs:44-67`), so override `VisitCore` and route a `TypeDeclarationSyntax` that is none of the
  five handled node types to `VisitTypeDeclaration<TypeDeclarationSyntax>`. The test must be on the node type and not
  on the syntax kind, because `RecordDeclarationSyntax` carries two kinds. `VisitTypeDeclaration<T>` uses only
  members of the abstract base and two helpers typed on `TypeDeclarationSyntax`, so it compiles unchanged with the
  base type as the type argument.
- Fix `GetDeclaringType` (`SyntaxExtensions.cs:112-120`) as well, and first, because the lexical scope factory
  consumes it before the rewriter runs. Extend `FindMemberDeclarationOrNull` (`:23-46`) and `FindSymbolDeclaringNode`
  (`:51-76`) by adding a type test to the existing kind list rather than replacing that list, which would otherwise
  drop the method, field and namespace kinds. The only callers of the three helpers are
  `Metalama.Framework/src/Metalama.Framework.DesignTime/DiagnosticSuppressing/TheDiagnosticSuppressor.cs:192`,
  `LexicalScopeFactory.cs:121` and `:186`, `CodeModelExtensions.cs:72` and `:87`, and
  `Metalama.Framework/src/Metalama.Framework.Engine/Diagnostics/ScopedSuppression.cs:60`. The same kind lists are the
  subject of LK-3 and are owned by theme 03, so the two changes must be made together.
- Exclude the primary-constructor branch for a union. `VisitTypeDeclaration` calls
  `ApplyMemberLevelTransformationsToPrimaryConstructor` (`LinkerInjectionStep.Rewriter.cs:1150-1165`), which appends
  advice parameters to the parameter list of the type declaration and rewrites the base list arguments. For a union
  that parameter list holds the case types, parsed as parameter entries whose identifier is optional and usually
  absent, so an introduce-parameter advice would silently add a case type to the union. Report the advice as
  ineligible instead, which is the subject of LK-8.
- Decide about extension blocks rather than widening by accident. `ExtensionBlockDeclarationSyntax` also derives from
  `TypeDeclarationSyntax` and is absent from all three kind lists of `SyntaxExtensions.cs`, so a broader test changes
  the behaviour for C# 14 extension blocks as well. Either name the union kind explicitly or make the widening
  deliberate and cover extension blocks by tests.
- Add a linker test under `Metalama.Framework/src/tests/Metalama.Framework.Tests.LinkerTests/Tests` and aspect tests
  under a new `Tests/Aspects/CSharp15/Unions` folder guarded by a required-constant directive, in the manner of
  `Tests/Aspects/DesignTimeInvalidCode/UnknownAccessorInTemplate.cs:7`. The guard name is hardcoded in the variant
  property file (`eng/RoslynVersions/Roslyn.5.10.0.props:10`) and is renamed with the variant, so it becomes
  `ROSLYN_5_12_0_OR_GREATER` after the renumbering. The aspect tests also require the language-version plumbing to
  accept C# 15 first, because a union does not parse otherwise.
- Size: medium for the dispatch and the insert-position walks. The eligibility rules and diagnostics that the union
  member restrictions require are additional work and are tracked as LK-8.
- Status: new work. It is not implemented, not in progress and not tracked. A search of the whole working tree for
  the union identifiers matches only the analysis documents and the grammar file. The story belongs under the open
  meta-issue #1921, whose sixteen sub-issues contain no union, C# 15 or linker item, and it must cite #1881, which
  removed every conditional-compilation block from production code and is therefore the constraint that forces a
  version-neutral dispatch. The open pull request #1879 (issue #1343) is the only in-flight work in the linker; it
  does not change the type-declaration dispatch, but a union story that later needs its substitution pattern for
  compiler-synthesized members should reference it.
- Verification: the code pass re-read the rewriter, the registry and the lexical scope factory end to end, confirmed
  the absence of the dispatch and of any tracking for an uninserted member, and corrected the failure order (template
  expansion fails before the registry), the exception type in a release build, one earlier failure in
  `LinkerInjectionRegistry.cs:325`, and two citations that named the wrong file or line. The semantics pass confirmed
  that the union node derives from `TypeDeclarationSyntax` with its own kind, that the Roslyn 5.0 public API contains
  no union member, and that the generic instantiation compiles against both variants, and it corrected the target
  version, the rationale for avoiding the type name, and the treatment of the case list and of extension blocks. The
  scope pass found no implementation, no pull request and no issue, and related the story to #1921, #1881 and #1343.
- Open questions: the two open questions of the original report are answered. The experimental marker on the union
  declaration was removed from the Roslyn grammar on 2026-08-11, and a union symbol reports `TypeKind.Struct`, which
  `SourceNamedTypeImpl.cs:69-79` maps without throwing. The exact exception site in the registry is verified by
  reading and not by running.

### LK-2. The linking rewriter has no union dispatch, so once LK-1 is fixed union members would be injected but never linked

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerLinkingStep.LinkingRewriter.cs:22` (the class),
    `:37`, `:50`, `:63`, `:66`, `:79-85` (the five overrides, of which the interface and extension-block ones are the
    one-line treatment the proposal reuses), `:88-247` (`GetMembersForTypeDeclaration`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRewritingDriver.Types.cs:18-44` (`RewriteClass`),
    `:46-62` (`RewriteStruct`), `:64-103` (`RewriteRecord`, positional branch at `:86-103`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRewritingDriver.cs:447`, `:1003-1032`
    (`GetSharedTypeMembers`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerLinkingStep.cs:20-31`, `:66-69` (the removal of
    the injection-helper tree)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAspectReferenceSyntaxProvider.cs:26`, `:34`,
    `:46`, `:85`, `:89`, `:121`, `:167`, `:196`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Transformations/ProceedHelper.cs:161`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SymbolExtensions.cs:283-291`,
    `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35`
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxRewriter.cs:44`, `:64`
- What happens today: the situation is unreachable, because LK-1 fails first. The linking rewriter declares
  overrides for the struct, class, interface, record and extension-block declarations and has no override of
  `VisitCore` or `DefaultVisit`, so a union node is reproduced by the Roslyn default visit and
  `GetMembersForTypeDeclaration` is never called for it. That method is the only caller of
  `LinkerRewritingDriver.RewriteMember` (`:447`), of `GetSharedTypeMembers` (`:1003`) and of the record helpers, so
  after LK-1 alone is repaired a union would keep the intermediate form that the header comment of
  `LinkerLinkingStep.cs:20-31` describes: the original member unchanged, and the override members present as separate
  declarations that still call the original member through annotated expressions. Nothing reports the condition,
  because the linker diagnostics cover only the three descriptors declared for it and the cleanup rewriter does not
  inspect aspect-reference annotations. Members of a nested type inside a union are still linked, because
  `GetMembersForTypeDeclaration` is not involved in reaching them.
- A statement of the finding needs care and is restated here, because it is easy to misread. A union has no primary
  constructor, in the language and in the Roslyn symbol model, but this does not mean that the parameter list is
  absent. The union declaration overrides the parameter-list field of `TypeDeclarationSyntax` and holds the case
  types there, so the parameter list is non-null for every union that compiles and contains at least one parameter
  entry whose identifier is absent. Roslyn separates the two cases by node type and not by the presence of the list,
  and the three existing rewrite methods erase that slot when a primary constructor has been removed.
- Consequence: silent wrong output, or a build error in the user compilation. The aspects have no effect on the
  union, and an unlinked override keeps a reference to the injection-helper type that `LinkerLinkingStep.cs:66-69`
  removes from the final compilation, which does not compile. The realistic sources of such a dangling reference in a
  union are the helper for an asynchronous void method (`ProceedHelper.cs:161`), the operator helper and the
  static-constructor helper (`LinkerAspectReferenceSyntaxProvider.cs:167` and `:34`) and a static automatic property,
  because a union may not declare an instance automatic property.
- Proposed change: apply the technique of LK-1 and route an unknown `TypeDeclarationSyntax` to
  `node.WithMembers( List( this.GetMembersForTypeDeclaration( node ) ) )`, which is exactly the treatment already
  used for interfaces (`:63-64`) and for extension blocks (`:79-85`). `GetMembersForTypeDeclaration` already takes
  the abstract base type (`:88`), so no signature changes. The fallback must be that expression and must not be
  routed through `RewriteClass`, `RewriteStruct` or `RewriteRecord`: their primary-constructor branch passes a
  default parameter list and would delete the case types of the union, and the positional branch of `RewriteRecord`
  asks the semantic model for the declared symbol of a parameter that has no declared symbol in a union. The shared
  static members that `GetSharedTypeMembers` emits are private static fields and are not affected by the union
  member restrictions.
- Record in the story that correct linking is necessary and not sufficient. A union forbids instance fields,
  automatic properties and field-like events, forbids an explicitly declared public constructor with a single
  parameter, and requires every explicit constructor to chain, so an introduction of field-shaped state produces a
  compilation error even after the dispatch is correct. That work is LK-8.
- Size: small, and part of the same change as LK-1.
- Status: new work. The whole repository contains no reference to a union declaration in any C# source. Pull request
  #1879 is the only open work in the linker and does not touch either of the two files of this finding. The story
  must cite #1881, which created the two-variant layout and is the reason a union override cannot be written
  unconditionally in a file that both variants compile, and #1913, because the shared sources of the Premium engines
  face the same gating decision and it must be settled once.
- Verification: the code pass confirmed the complete set of overrides, that `GetMembersForTypeDeclaration` is the
  sole path to the rewriting driver, that no diagnostic reports the condition, and that the proposed fallback
  compiles in both variants, and it found the precedent in the extension-block override; it also observed that a
  union case constructor would be treated as a primary constructor if LK-3 added the union kind to
  `IsTypeDeclaration` without excluding it in `IsPrimaryConstructor`. The semantics pass confirmed that a union has
  no primary constructor and no positional properties, so the class and record rewrite logic does not apply, and
  corrected the reading of the parameter list and the interaction with the union member restrictions. The scope pass
  confirmed the file contents on the working branch and found no pull request and no issue on the subject.

### LK-3. Syntax-kind lists and record special cases that exclude unions

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35`
    (`IsTypeDeclaration`) and `:41` (`IsBaseTypeDeclaration`, defined in terms of it)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:329-352` (`IsPartial`:
    the kind test is at `:344`, the result at `:351`), `:260-275` (`GetPrimaryConstructorImpl`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:158-163`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceMemberAdvice.cs:216-224`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SymbolExtensions.cs:283-291`
    (`IsPrimaryConstructor`), `:181-202` (`HasModifier`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/SymbolExtensions.cs:18-65` (`GetDeclarationFlags`; kind
    list at `:25-31`, null arm at `:57-60`, throwing default at `:62`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.InlineabilityAnalyzer.cs:94`, `:285`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/Inlining/ImplicitLastOverrideReferenceInliner.cs:22-29`
    (the fallback to the containing type syntax), `:72-73`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerLateTransformationRegistry.cs:143-160` (kind list
    at `:148-150`), `:181-197` (kind list at `:187-189`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerSyntaxHandler.cs:104-105`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs:244`, `:418`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SubstitutionGenerator.cs:908`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.AspectReferenceCollector.cs:203`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRewritingDriver.cs:323`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRecordHelper.cs:45`, `:65`
  - `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:88`
    (`MustBeExplicitlyDeclared`), `:58-79` (the method rule), `:39-49` and `:51-56` (the declaring-type rule and the
    constructor rule, which has no explicit-declaration requirement)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:51-76`
    (`FindSymbolDeclaringNode`), `:112-120` (`GetDeclaringType`)
  - `Metalama.Framework/src/Metalama.Framework.Engine.Analyzers/KindCheckOptimizationAnalyzer.cs:24-31`, `:45-52`,
    `:722-725`
- What happens today: a `partial union` reports `IsPartial == false`, because `SourceNamedTypeImpl.IsPartial`
  switches on the syntax-kind predicate (`SourceNamedTypeImpl.cs:344`) whose list omits the union kind, so the switch
  falls to its default and the modifier test at `:351` returns false. The premise holds on the language side: the
  grammar allows `partial` on a union, and Roslyn allows the partial, read-only, unsafe and safe modifiers on it
  because a union has `TypeKind.Struct`. As a result the design-time generator reports LAMA0048
  (`DesignTimeSyntaxTreeGenerator.cs:158-163`) and `IntroduceMemberAdvice.ValidateBuilder` reports
  `CannotIntroducePartialMemberToNonPartialType` (`IntroduceMemberAdvice.cs:216-224`) for a partial member introduced
  into a union. Both diagnostics are wrong, because the union is in fact partial.
- The other sites are latent, and the risk is distributed differently from what the original report states, because
  the two families of synthesized union member differ in whether they have a declaring syntax. The per-case
  constructors are synthesized methods: they are implicitly declared, they carry an empty declaring-syntax
  collection, and Roslyn creates no primary constructor for a union at all. Every site that switches on the primary
  declaration syntax of such a constructor therefore observes a null value and takes an existing null arm.
  `IsPrimaryConstructor` is false for that reason rather than because of the kind list, and
  `CodeModel/Source/SourceConstructor.cs:92-94` is not an affected site at all, because its switch has an explicit
  null arm above the type-declaration arms. The synthesized `Value` property is the opposite case: it derives from
  the source property symbol base and is constructed with the union declaration syntax, so it and its getter report
  the union declaration. `Linking/SymbolExtensions.GetDeclarationFlags` can therefore be entered with the union kind,
  which matches neither the kind list nor the accessor case nor the null case, and reaches the throwing default at
  `:62`. `ImplicitLastOverrideReferenceInliner` is reachable for both families, because `:22-29` falls back to the
  primary declaration syntax of the containing type when the symbol has none, so the kind test at `:72` fails and the
  throw at `:73` is taken either way.
- The two checks in `LinkerLateTransformationRegistry` remain unreachable, because they run only after a primary
  constructor was removed and a union has none. The record-only kind switches remain unreached for the `Value`
  property and for methods, because the override eligibility rules require an explicitly declared member
  (`EligibilityRuleFactory.cs:88` and `:58-79`), but the code pass established one exception: the constructor rule
  (`:51-56`) requires only that the constructor is not a record copy constructor and that the declaring type passes
  the type rule, which admits a struct, so a union case constructor is eligible for an override-constructor advice.
  `LinkerRecordHelper.GetSynthesizedMethodOverrideTargets` (`:45`) filters on a record declaration kind and would
  silently omit such a constructor.
- One qualification of a supporting statement: a union has a parameter list in every declaration that compiles, since
  the case list is required by the grammar, but the syntax field is optional and the parser produces a null parameter
  list for a union written without a case list, which the compiler then rejects with CS9370. Design-time code may
  therefore observe a union declaration whose parameter list is null. A second qualification concerns `HasModifier`:
  the type-level modifier list emitted by `ModifierHelper.GetTypeSyntaxModifierList` has no unsafe branch and the
  symbol-based helper throws for a named type, so the omission of the union kind from that list has no effect on type
  declarations and remains correct only for members declared inside a union with ordinary member syntax.
- Consequence: a wrong diagnostic today on a `partial union`; an assertion failure once a union becomes reachable and
  its synthesized `Value` property or getter reaches `GetDeclarationFlags` or the implicit-last-override inliner; and
  a silent wrong result, rather than an assertion, at the sites that would match a union case list as a parameter
  list after a careless broadening.
- Proposed change: widen the predicate where the intent is a declaration that can contain members, that is the
  callers of `IsTypeDeclaration` (in particular `SourceNamedTypeImpl.IsPartial`, which produces today's wrong
  diagnostics), `FindMemberDeclarationOrNull`, `FindSymbolDeclaringNode`, `GetDeclaringType` and
  `Linking.SymbolExtensions.GetDeclarationFlags`. Keep the record-only lists for the record-synthesized-member logic,
  and treat `LinkerRecordHelper.GetSynthesizedMethodOverrideTargets` as in scope rather than out of scope, for the
  reason given above. Note that `IsBaseTypeDeclaration` is defined in terms of `IsTypeDeclaration` and must keep
  working, and that `GetDeclaringType` already requires the abstract node type in its condition, so its kind list is
  redundant there.
- Two constraints on the shape of that widening. The first comes from the scope pass and is the more important one:
  the proposal to replace the kind lists by a bare type test contradicts a doctrine that this repository enforces
  automatically. Issue #1307, completed by pull request #1309, converted such tests to a kind check followed by a
  type pattern, and `KindCheckOptimizationAnalyzer` reports LAMA0860 on a bare pattern match against a syntax node
  type in the Metalama assemblies, accepting `IsTypeDeclaration` as a valid preceding check (`:722-725`). Under the
  zero-warning gate every such site would fail the continuous integration build, so the correct shape is to widen the
  list at `SyntaxKindExtensions.cs:33-35` and then to extend the remaining explicit lists one by one. The second
  constraint is that `IsPrimaryConstructor` must not exclude the record declaration: records do have primary
  constructors, the method returns true for them today, and that behaviour is pinned by
  `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/CodeModel/PrimaryConstructorTests.cs:300-320`,
  `:330-345` and `:347-371`. A union exclusion is unnecessary in any case, because a constructor symbol never carries
  a union declaration as its primary declaration syntax.
- Apply the widening with care at the two site families where the union parameter list is a case list and not a
  parameter list: the implicit-last-override inliner at `:72` would otherwise return the case list as the member
  body, and the two late-transformation registry methods test the parameter list on the type declaration. Those sites
  need an explicit union arm rather than a silent match. Where the union kind has to be named in code shared by both
  Roslyn variants, the constraint of #1881 applies and the gating strategy owned by theme 03 decides the mechanism.
- Size: medium, and possibly light, because `IsTypeDeclaration` is a public member of a public static class of
  `Metalama.Framework.Engine` with eight call sites.
- Status: new work, with a rebase dependency. Every cited site is unchanged on the working branch and the repository
  contains no union identifier in any C# source. Pull request #1879 rewrites two of the eligibility lines cited here
  and edits three of the record sites, and adds no union case anywhere, so the story must be based on or rebased onto
  that branch. The story must reference #1307, which created the kind lists and the analyzer that enforces them, and
  #1881, which is why the check has to be version-neutral. The open catch-all #985 concerns the template compiler and
  does not scope this work.
- Verification: the code pass confirmed every cited site, established the split between the two families of
  synthesized union member from the Roslyn sources, found the fallback in the implicit-last-override inliner that
  makes its throw reachable for both families, and refuted the proposed exclusion of the record declaration from
  `IsPrimaryConstructor` by naming the three unit tests it would break. The semantics pass confirmed that `partial`
  and `unsafe` are legal on a union, that the union declaration derives from `TypeDeclarationSyntax` with its own
  kind, and that both synthesized member families are implicitly declared, and it corrected the version statement and
  removed `SourceConstructor.cs:92-94` from the list of affected sites. The scope pass found the change neither
  implemented, in progress nor tracked, and contributed the LAMA0860 constraint that changes the shape of the fix.
- Open questions: the numeric value that the union kind carries should be re-read from the shipped compiler before it
  is written into any source, and it should be introduced as one named constant rather than repeated at each site.

### LK-4. Design-time partial declarations emit a struct for a union, and need no `closed` modifier

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:697-790`
    (`CreatePartialType`), `:710-714` (the modifier list), `:720-789` (the type-kind switch, throwing default at
    `:788`), `:506-522` (`AddPartialModifierToTypes`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:69-79` and `:173` (the
    type kind and the record flag, which select the struct arm for a union)
  - `Metalama.Framework/src/Metalama.Framework.Engine.5.0.0/Metalama.Framework.Engine.5.0.0.csproj:6`
  - `eng/RoslynVersions/Roslyn.5.10.0.props:8-10` (the only definition of the variant symbol)
- What happens today: `CreatePartialType` copies only the static and partial modifiers from the source declaration
  (`:710-714`) and switches on the type kind and the record flag to emit a class, a record class, a struct, a record
  struct or an interface, throwing for every other value (`:720-789`). A union reports `TypeKind.Struct`, which is a
  verified fact and not an assumption, and it is neither a record nor a record struct, so the arm for a non-record
  struct is selected and the generated document declares `partial struct U` beside the source `partial union U(...)`.
  The two parts keep distinct declaration kinds, declarations of different kinds never merge, and the compiler
  reports CS0261, whose message on the target Roslyn names unions explicitly. The alternative branch of the original
  report, in which a new type-kind value would make the code model throw first, does not exist.
- For a `closed partial class C` the generated part is `partial class C`, and that is legal. The compiler unions the
  modifiers of all partial parts, reads the closed flag from the merged value and applies the implicit abstractness
  to it, and no error code requires the modifier on every part. This is no longer an open question and no longer
  depends on the language proposal, which does not mention partial declarations.
- Consequence: a compilation error in the generated document at design time for unions, so the development
  environment shows errors on the type; no impact at all for the `closed` modifier.
- Proposed change: add a union case that emits a union declaration with a null parameter list. The source case list
  must not be copied: Roslyn accepts the case list on at most one partial part and reports CS8863 on every further
  part that carries one, while the source part already satisfies the requirement that at least one part carry it. A
  part without a case list is representable and parseable, because the field is optional and the parser produces a
  null value when the token after the type parameter list is not an opening parenthesis. The factory has no
  convenience overload, so all twelve arguments must be passed. The case also needs a discriminator, because a union
  and an ordinary struct both report `TypeKind.Struct` and the code model exposes no union flag today, which is
  finding CM-1 of theme 03. The case cannot be written against the Roslyn 5.0 API, and the variant project compiles
  every source file of the base project (`Metalama.Framework.Engine.5.0.0.csproj:6`), so a separate per-variant file
  would also require a new exclusion entry; the gating mechanism is the subject of theme 03, and the gate belongs to
  the latest variant, whose symbol follows the variant version. For the `closed` modifier no production change is
  required, and a regression test with the design-time scenario on a `closed partial class` is optional confirmation
  rather than a step whose outcome is unknown.
- Size: small overall. Medium for the union case, because of the variant gating, the twelve-argument factory and the
  missing discriminator; nothing for the `closed` modifier beyond an optional test.
- Status: new work. Neither `CreatePartialType` nor `AddPartialModifierToTypes` mentions a union, there is no
  `Tests/Aspects/CSharp15` directory, and no open or merged pull request touches the design-time generator for this
  purpose. The story must cite #1881, whose pull request removed the conditional-compilation blocks from production
  code, and #1039, the C# 14 umbrella, as the precedent for how a C# 15 feature story is filed. LK-3 is a
  prerequisite, because a `partial union` reports `IsPartial == false` and the generator reports LAMA0048 before
  `CreatePartialType` is reached.
- Verification: the code pass confirmed the modifier list, the type-kind switch and the throwing default, confirmed
  that no unit test pins that switch, and corrected the size by adding the variant-project glob, the experimental
  suppression and the missing union discriminator. The semantics pass established from the Roslyn sources that a
  union reports `TypeKind.Struct`, identified the compiler error as CS0261, refuted the open question about the
  `closed` modifier by reading the modifier merge of partial parts, and refuted the proposal to copy the case list by
  naming CS8863. The scope pass confirmed the file contents on the working branch and found no implementation, no
  pull request and no issue.

### LK-5. Type introduction cannot emit `closed` or `union`, which is a feature gap and not a defect

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Builders/NamedTypeBuilder.cs:52-53`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceNamedTypeTransformation.cs:61-91`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/ModifierHelper.cs:198-236`
    (`GetTypeSyntaxModifierList`), `:178` (the member-level unsafe branch)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/ModifierCategories.cs:17`, `:21`
  - `Metalama.Framework/src/Metalama.Framework/Code/DeclarationBuilders/INamedTypeBuilder.cs:13-50`,
    `Metalama.Framework/src/Metalama.Framework/Code/DeclarationBuilders/IMemberOrNamedTypeBuilder.cs:20-45`
  - `Metalama.Framework/src/Metalama.Framework/Aspects/AdviserExtensions.cs:1702`, `:1725`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:2050-2093`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs:359-455`,
    `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRewritingDriver.Types.cs:18-44`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:510-516`,
    `:697-789`
  - `eng/RoslynVersions/Roslyn.5.10.0.props:8-10`
- What happens today: an aspect cannot introduce a closed class or a union, because no application programming
  interface expresses either. The public surface offers class and interface introduction only
  (`AdviserExtensions.cs:1702` and `:1725`, forwarding to `AdviceFactory.cs:2050-2093`); the builder asserts that the
  type kind is class, struct, interface or extension and that the type is not a record
  (`NamedTypeBuilder.cs:52-53`); the transformation switches on class, struct and interface and throws otherwise
  (`IntroduceNamedTypeTransformation.cs:61-91`); and the builder interfaces expose the accessibility, the name and
  the static, sealed, abstract and partial flags and no closed flag. `GetTypeSyntaxModifierList`
  (`ModifierHelper.cs:198-236`) emits the accessibility and the static, new, abstract and sealed keywords, and
  nothing else; in particular it emits neither `partial` nor `unsafe` for a type, although the modifier category
  enumeration defines both. A `closed` modifier written by the user is preserved, because the rewriters update the
  original node instead of regenerating it, and the design-time generator adds the partial keyword to the existing
  modifier list rather than rebuilding it.
- Introducing a class derived from a closed class is permitted, with two precisions. The language requires the same
  module and not only the same assembly, and an introduction always lands in the compilation being transformed, so
  both conditions hold. The compiler discovers the subtypes by walking the source module of the compilation, so an
  introduced derived type is included in the synthesized attribute that records the derived types.
- Consequence: no impact for the missing ability to introduce a closed class or a union, because the aspect author has
  no way to request either, so there is no diagnostic and no crash. One low-probability semantic consequence exists
  for the supported case: because a switch is exhaustive only when it handles all direct descendants of the closed
  class, an aspect that introduces a derived type can make a previously exhaustive user switch non-exhaustive and
  produce CS8509 or CS8655 in the transformed compilation at the location of the user source.
- Proposed change, optional for 2027.0: add a closed flag to `INamedTypeBuilder` and a reader on `INamedType`, which
  is finding CM-4 and CM-5 of theme 03, and emit the closed keyword in `GetTypeSyntaxModifierList`, subject to four
  external constraints. The first is the Roslyn variant: the closed keyword does not exist in Roslyn 5.0 and carries
  the experimental marker in the stable 5.9.0 and in the consumed 5.10 preview, so the emission belongs to the latest
  variant after that variant moves to the stable Roslyn, and naming it earlier requires an explicit suppression that
  the continuous integration build would otherwise reject. The second is the language version, which no consumed
  Roslyn exposes as C# 15, so this work depends on the C# 15 enablement owned by theme 01. The third is the
  interaction with the modifiers that the helper already emits: a closed class is implicitly abstract, and the
  `abstract`, `sealed` and `static` keywords on it are errors, so the flag must be mutually exclusive with them and
  the abstract branch must be suppressed when it is set; the modifier is allowed only on a class declaration. The
  fourth is that the compiler requires two well-known attribute members for a closed type, so their availability in
  the target framework is an eligibility condition, which matters for the desktop flavour. A generated partial part
  need not repeat the modifier, as LK-4 establishes. Union introduction is out of scope for 2027.0.
- If the closed base type may come from another assembly, add a validation that reports a Metalama diagnostic rather
  than letting the compiler reject the generated code: the base type of a builder accepts any named type today, with
  no validation of the same-assembly rule or of the rule that every type parameter of the introduced class must be
  used in the base type specification.
- Size: small for the closed modifier, and only after the move to the stable Roslyn and the C# 15 enablement, plus a
  small amount of validation for the abstract, sealed and base-type rules. Large for unions.
- Status: decision required. The decision is whether an aspect may introduce a closed class in 2027.0. There is no
  correctness defect to fix here, so the work is a feature and can be deferred as a whole. The story, if taken,
  belongs under #1921 and must cite #1881 for the variant gating, #1034 as the precedent for the separate code-model
  reader, and #1159 and #1131 as the precedents for the shape of an introduction story for a new language feature.
- Verification: the code pass confirmed that no application programming interface expresses either modifier, that the
  rewriters and the design-time generator preserve a user-written `closed` modifier, and corrected the line range of
  `GetTypeSyntaxModifierList` and the claim that it emits `partial` and `unsafe`; it also found that the proposal is
  not minimal as written, because the abstract and sealed branches must be suppressed and the closed keyword is an
  experimental application programming interface today. The semantics pass confirmed every language rule cited above
  from the Roslyn sources and the proposal, added the well-known attribute requirement and the exhaustiveness
  consequence, and answered the open question about partial parts. The scope pass confirmed that no closed or union
  identifier exists in the framework sources, that the public type-kind enumeration has no union member, and that no
  issue tracks the change.

### LK-6. `IntroduceIndexer` rejects extension blocks by design, and C# 15 extension indexers require lifting the restriction

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:1406` (the call), `:527-534`
    (`ValidateNotExtensionBlock`, whose other nine call sites are at `:957`, `:1128`, `:1158`, `:1206`, `:1490`,
    `:1879`, `:2060`, `:2083` and `:2106`), `:404-417` (`Validate`, which also throws)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/UserCode/UserCodeInvoker.cs:133-140` and
    `Metalama.Framework/src/Metalama.Framework.Engine/Diagnostics/GeneralDiagnosticDescriptors.cs:182-190` (the
    conversion of the exception into LAMA0041)
  - `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:117-125` (`_introduceRule`),
    `:250-259` (the nine advice kinds that share it)
  - `Metalama.Framework/src/Metalama.Framework/Code/IExtensionBlock.cs:11-21`,
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/ExtensionBlockImpl.cs:24`,
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Builders/ExtensionBlockBuilder.cs:34`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceIndexerTransformation.cs:28`,
    `:35-46`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceMemberAdvice.cs:194-197`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/ExtensionImplementationHelper.cs`,
    `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroduceMethodTransformation.cs:228`,
    `:249`,
    `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/IntroducePropertyTransformation.cs:226`,
    `:243`, `:262`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:662-683`
  - `Metalama.Framework/src/Metalama.Framework/Advising/IAdviceFactory.cs:1039`, `:1061`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/Introductions/ExtensionBlocks/ErrorIndexerIntoExtensionBlock.cs`
    and its expected output
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp14/ExtensionMembers`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs` and
    `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-90` (the
    language-version prerequisite)
- What happens today: the rejection is deliberate and is pinned by a test. `AdviceFactory.cs:1406` calls
  `ValidateNotExtensionBlock`, whose helper throws an invalid-operation exception with the message that an indexer
  cannot be introduced into a declaration that represents an extension block. Because the call happens inside
  `BuildAspect`, `UserCodeInvoker.cs:133-140` converts the exception into the error diagnostic LAMA0041, so the
  result is a build error and not a crash and not silent output. The eligibility rule for the introduction advice
  kinds already admits the extension type kind (`EligibilityRuleFactory.cs:117-125`, mapped at `:258`), so the
  imperative check is the only barrier. The transformation already emits the syntax that C# 15 requires once it is
  placed inside an extension block (`IntroduceIndexerTransformation.cs:35-46`), and the introduction advice already
  suppresses the diagnostic about an instance member in a static type for extension blocks
  (`IntroduceMemberAdvice.cs:194-197`). What is missing is the implementation-method half:
  `ExtensionImplementationHelper` creates the implicit static implementation methods for methods and for property
  accessors and has no indexer counterpart, and `IntroduceIndexerTransformation` overrides only `GetInjectedMembers`
  and has no `GetImplicitDeclarations` override at all.
- Consequence: a diagnostic is reported. The aspect fails at build time with LAMA0041 and the message names the
  extension block.
- Proposed change: remove the validation at `AdviceFactory.cs:1406` and add a dedicated eligibility rule for the
  indexer introduction rather than modifying `_introduceRule`, which nine advice kinds share. Express the rule in the
  public code model, because the `Metalama.Framework` project does not reference `Metalama.Framework.Engine` and
  therefore cannot read the builder property: test the name of `IExtensionBlock.ReceiverParameter` for emptiness,
  which is the same condition that the builder evaluates at `ExtensionBlockBuilder.cs:34` and which also holds for
  source blocks through `ExtensionBlockImpl.cs:24`. The rule is required by the language, which states that an
  extension block declaring an indexer must provide a named receiver parameter, because an indexer is always an
  instance member. The rule must also reject an `init` accessor and the `abstract`, `virtual`, `override`, `new`,
  `sealed`, `partial` and `protected` modifiers, which the proposal forbids in the same sentence. Note that an
  ineligible target still raises an invalid-operation exception from `AdviceFactory.Validate` (`:404-417`) and
  therefore still surfaces as LAMA0041, so the consequence class does not change for a static extension block; only
  the message does.
- Extend `ExtensionImplementationHelper.CreateImplicitAccessorMethod`, which today adds only the receiver parameter
  and, for a setter, a single value parameter: it needs the index parameters, inserted after the receiver and before
  the value parameter. Add a `GetImplicitDeclarations` override to `IntroduceIndexerTransformation`, following
  `IntroducePropertyTransformation.cs:226`. The implementation methods are static methods of the enclosing static
  class, generic over the type parameters of the extension block, with the receiver prepended and, for the setter,
  the assigned value last. Their names are the accessor names of the indexer and must be derived from the indexer
  metadata name rather than hardcoded, because the indexer-name attribute determines the name of the property and of
  its accessors in metadata. When implementing the inferrability check, apply the rule that the proposal states,
  namely that every type parameter of the block must be used in the combined set of the extension parameter and the
  indexer parameters; a rule copied from extension properties, which have no parameters, would be too strict.
- Add design-time coverage, because `CreateExtensionBlock` (`DesignTimeSyntaxTreeGenerator.cs:662-683`) copies
  members verbatim. Tests go under `Tests/Aspects/CSharp15/ExtensionIndexers`, beside the existing
  `Tests/Aspects/CSharp14/ExtensionMembers`, and must be guarded by the variant constant, because the Roslyn 5.0
  variant cannot parse an indexer inside an extension block. Delete or replace the test
  `Tests/Aspects/Introductions/ExtensionBlocks/ErrorIndexerIntoExtensionBlock.cs` and its expected output, and
  restore the word "indexers" in the two documentation summaries at `IAdviceFactory.cs:1039` and `:1061`.
- Sequencing: this work cannot be tested before two prerequisites land, because the feature is gated on a message
  identifier that resolves to `LanguageVersion.Preview` in the consumed Roslyn preview and in the stable 5.9.0, and
  to C# 15 only from the Roslyn commit of 2026-08-11. The prerequisites are the move of the latest Roslyn variant to
  the stable 5.12 and the addition of C# 15 to the supported versions and to the checks of `VerifyLanguageVersion`.
- Size: medium.
- Status: new work. No open or merged pull request touches the advice factory for this purpose, and no issue proposes
  the change. The story must cite #1587, which is the opposite decision: it recorded that indexers are rejected and
  corrected the documentation to match, so the summaries and the pinning test are part of this work. It must also
  cite #1160 and #1159, which established the acceptance and rejection matrix and the extension block builder, and
  #1035, which is the precedent for the test layout.
- Verification: the code pass confirmed every cited location and the conversion into LAMA0041, found the test that
  pins the current behaviour, and refuted three parts of the proposal by showing that the eligibility rule cannot
  reference an engine type, that the shared rule cannot be tightened in place, and that the accessor helper has no
  way to express index parameters. The semantics pass confirmed from the language proposal that the feature reuses
  the ordinary indexer syntax with no new node or symbol member, that a named receiver is required, that the `init`
  accessor and six further modifiers are forbidden, and that the implementation methods have the shape described, and
  it corrected the version reference and added the indexer-name and inferrability rules. The scope pass established
  that eligibility already admits an extension block, so the imperative check is the only gate, and found no
  implementation, no pull request and no issue.

### LK-7. Overriding a source extension indexer follows the extension-property path, and the non-inlined trampolines are wrong for extension members

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:640-646`, `:671-677`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Override/OverrideIndexerBaseTransformation.cs:36-97`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAspectReferenceSyntaxProvider.cs:142-161`
    (`GetIndexerReference`), `:199-227` (`CreateIndexerAccessExpression`, the extension-block branch at `:213-218`),
    `:268-274`, `:289-296` (the property path)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Transformations/ProceedHelper.cs:234-241`, `:252-259`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.cs:850-906` and
    `Metalama.Framework/src/Metalama.Framework.Engine/Linking/AspectLinkerDiagnosticDescriptors.cs:33-39` (LAMA0699)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRewritingDriver.cs:455-459`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRewritingDriver.Indexers.cs:272-299`, `:404-455`
    (`GetTrampolineForIndexer`, the receiver at `:443-454`, the name at `:447` and `:451`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRewritingDriver.Methods.cs:340-351`,
    `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRewritingDriver.Properties.cs:668-679`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.cs:1136-1167`
    (`ForEachMethodInExtensionBlock`, the indexers at `:1155-1166`) and
    `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.AuxiliaryMemberFactory.cs:472-580`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/ExtensionBlockImpl.cs:21-24`, `:34` and
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/DeclarationExtensions.cs:44` (the Roslyn
    members already used unconditionally)
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/CSharp14/ExtensionMembers/ExtensionMembers_OverrideProperty.cs:26`,
    `:39`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/Overrides/Indexers/NotInlineable.cs:26-38`
    and its expected output
- What happens today: no test exercises an indexer inside an extension block, because the C# 14 grammar for an
  extension member admits only a method, a property and an operator, and the C# 15 proposal adds the indexer
  declaration to that production. The feature is gated on a message identifier that requires the preview language
  version in the consumed Roslyn, so the situation is reachable only once C# 15 is enabled. An extension indexer adds
  no syntax node, no syntax kind and no symbol member: it is an ordinary indexer declaration inside an extension
  block and surfaces as a property symbol with the indexer flag on a type of the extension kind, so the paths
  described here are the paths that will be taken.
- Those paths are the ones already used for extension properties. The advice factory routes the accessors of an
  indexer to the override-indexer advice, the transformation emits a private indexer whose parameter list is extended
  by a marker type, and the proceed expression comes from `GetIndexerReference`, whose receiver is built by
  `CreateIndexerAccessExpression`; the branch at `:213-218` substitutes the receiver parameter name for `this` when
  the declaring type is an extension block, exactly as the property path and the method path do. The existing
  extension property test does not pin that substitution, because the receiver identifier in its expected output
  comes from a `meta.Receiver` call in the template and the override is inlined, so no proceed reference survives in
  the baseline. Contract statements already reach extension indexers, because the injection step enumerates the
  indexers of an extension block and the auxiliary member factory builds the auxiliary contract indexer from the same
  reference provider.
- The non-inlined case is refused earlier. `LinkerAnalysisStep.cs:850-906` reports LAMA0699 for every non-inlined
  semantic whose symbol is a property with parameters, and `LinkerRewritingDriver.cs:455-459` then returns the member
  unrewritten, so `GetTrampolineForIndexer` is unreachable for extension indexers and for ordinary indexers alike.
  That helper is in any case already incorrect for every indexer, because it passes the indexer name, which is
  `this[]`, to an identifier factory and emits no bracketed argument list and no marker argument.
- Consequence: no impact expected when the override is inlineable, and a diagnostic otherwise. The related
  pre-existing gap is outside C# 15: for extension methods and properties, the non-inlined trampolines at
  `LinkerRewritingDriver.Methods.cs:340-351` and `LinkerRewritingDriver.Properties.cs:668-679` emit `this.<member>`,
  which is invalid inside an extension block because an extension member has no implicit or explicit `this`, and no
  LAMA0699 guard applies to them.
- Proposed change: add aspect tests under `Tests/Aspects/CSharp15/ExtensionIndexers` covering the getter, the setter,
  an expression-bodied accessor, a contract on the receiver and a non-inlineable override. The matrix follows the
  feature rules: there is no static extension indexer and the receiver parameter is always named, so there is no
  counterpart to the static receiver-contract test, and there is no `init` accessor case.
- Fix the method and property trampolines, which is independent of C# 15 and can proceed now, because it repairs the
  already shipped C# 14 extension members: replace the `this` expression by the extension receiver when the
  containing type is an extension block. The test cannot be the one used in `CreateIndexerAccessExpression`, because
  the rewriting driver holds no compilation model and its helpers receive Roslyn symbols; use the Roslyn extension
  flag and the extension parameter name, which the engine already names unconditionally at
  `ExtensionBlockImpl.cs:21-24` and `DeclarationExtensions.cs:44`, so the change compiles in both variants. Do not
  extend the fix to `GetTrampolineForIndexer`: that helper is unreachable behind the LAMA0699 guard and would need a
  full rewrite rather than a receiver substitution.
- Size: small.
- Status: new work, with two dependencies. The tests depend on the C# 15 language-version work owned by theme 01, and
  the observable behaviour of the indexer trampoline depends on the open issue #937, which proposes to remove the
  LAMA0699 restriction by giving each indexer override a distinguishing marker parameter. The story must also cite
  #1587, which fixes the boundary with LK-6, and #1035 and #1127, which produced the override and receiver-contract
  paths that this work extends. No pull request touches these files.
- Verification: the code pass confirmed the routing, the receiver substitution, the LAMA0699 guard and the
  contract-statement path, and corrected three details: the extension property baseline does not pin the
  substitution, the indexer trampoline is unreachable and already incorrect, and the proposed fix cannot read the
  code model from the rewriting driver. The semantics pass confirmed from the two language proposals that the feature
  adds no node and no symbol member, that an extension member has no implicit or explicit `this`, that the receiver
  parameter is always named and no `init` accessor is allowed, and that a private extension member is not among the
  forbidden shapes. The scope pass confirmed that the receiver handling exists in exactly one place, that the same
  gap is present in the two sibling trampolines so that the correction is a three-site change, and that no test
  directory for C# 15 exists.
- Open questions: whether Roslyn accepts a private extension indexer is taken from the list of forbidden modifiers in
  the proposals, which does not include `private`; no compiler source was read on that point, so it is plausible
  rather than verified.

### LK-8. Advice on unions produces code that the compiler rejects, and eligibility does not prevent it

- Where:
  - `Metalama.Framework/src/Metalama.Framework/Eligibility/EligibilityRuleFactory.cs:164-195`
    (`_addInitializerRule`; the type branch at `:174-176`), `:117-127` (`_introduceRule`, shared by nine advice kinds
    per `:250-259`), `:152-162` (`_introduceParameterRule`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/DeclarationExtensions.cs:664-665`
    (`IsImplicitInstanceConstructor`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Initialization/ConstructorInitializeAdvice.cs:60-101`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Initialization/OnConstructedEpilogueEmitter.cs:62-150`,
    `:191-254`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/Constructors/ForwardingConstructorHelper.cs:57`,
    `:64-67`, `:146-236` and
    `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Introduction/Constructors/IntroduceConstructorParameterAdvice.cs:196-208`,
    `:254-262`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Advising/AdviceFactory.cs:1938-1952`, `:404-422`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/Override/OverrideHelper.cs:213-227`
  - `Metalama.Framework/src/Metalama.Framework.Engine/AdviceImpl/InterfaceImplementation/ImplementInterfaceAdvice.cs:754`,
    `:924`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Builders/ConstructorBuilder.cs:43-57`
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:69-79`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.cs:827-833` and
    `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionRegistry.cs:93-110`
  - `Metalama.Framework/docs/initialization-advice.md`, section 4.3
- What happens today: the scenario requires a compilation that contains a union, so it is unreachable until C# 15 is
  enabled, and it is masked by LK-1 until the dispatch is repaired. Eligibility admits a union everywhere a struct is
  admitted, because the code model reports `TypeKind.Struct` for it. After LK-1 and LK-2, four outcomes follow.
- First, an initializer added before the instance constructors enumerates every constructor of the union. The
  synthesized per-case constructors are public, have exactly one parameter, are implicitly declared and carry an
  empty declaring-syntax collection, so `IsImplicitInstanceConstructor` is false for them, because it also requires an
  empty parameter list, and the statements are attached to a constructor that has no syntax. The linker stores them
  in a dictionary keyed by the constructor reference (`LinkerInjectionStep.cs:827-833`) and skips a constructor that
  cannot be translated (`LinkerInjectionRegistry.cs:93-110`), so they are lost without a diagnostic and without an
  assertion.
- Second, the same enumeration also reaches the implicit parameterless constructor that Roslyn synthesizes for every
  struct, including a union. That one satisfies the test, so a builder materializes an explicit parameterless
  constructor, which the compiler rejects with CS9375, because a constructor declared in a union must chain to a
  synthesized or an explicitly declared constructor.
- Third, an initializer added after the last instance constructor goes through the epilogue emitter, which introduces
  an optional initialization-context parameter. On a union it materializes the implicit parameterless constructor and
  appends that parameter to it, producing an explicitly declared public constructor with a single parameter, which
  the compiler rejects with CS9374 in addition to CS9375. The forwarding constructor with the pre-mutation
  single-parameter signature comes instead from the required-parameter overload of the parameter introduction, whose
  default overloading strategy forwards the source constructors; applied to a case constructor it emits a public
  constructor with that one parameter, rejected for the same reason and duplicating the signature of the synthesized
  case constructor.
- Fourth, field introduction, automatic property and field-like event introduction, and an interface implementation
  whose member template is an automatic property or a field-like event, emit an instance field, which the compiler
  rejects with CS9373. The prohibition covers every non-static field to be emitted, at any accessibility, including a
  backing field, and excludes only the backing field of the synthesized `Value` property; static fields are
  permitted, so a static introduction into a union is not affected.
- Consequence: a build error in the user compilation, on generated code, with CS9373, CS9374 and CS9375, and in
  addition a silent loss of the statements and parameters that target the synthesized case constructors.
- Proposed change: two options, and the choice between them is the decision below. The conservative option is a
  blanket check on the target type: add a union rule to the shared introduction rule, to both branches of the
  add-initializer rule, to the parameter-introduction rule through the declaring type, and to the interface
  implementation rule, so that 2027.0 fails early with a clear message instead of failing at the linker or in the
  transformed compilation. It costs one rule and covers the three compiler errors as well as the silent case. The
  precise option keeps the legal cases working and moves the check into the advice implementations that actually emit
  an instance field or a constructor, each reporting a dedicated diagnostic.
- Three constraints apply to either option. The eligibility rules are shared objects: one rule serves nine advice
  kinds, so a rule cannot be added for field introduction alone without splitting it. The rules observe only the
  target type, so they cannot distinguish an automatic property from a template property, nor a field-like event from
  an accessor event, because both map to the same advice kind. And introducing a method, a static field or a nested
  type into a union is valid C#, so a rule on the target type over-restricts. Note also that `AdviceFactory.Validate`
  (`:404-422`) does not report a diagnostic but throws an invalid-operation exception carrying the justification,
  which surfaces as LAMA0041; an eligibility rule and a validation diagnostic are therefore not interchangeable,
  because a fabric or a conditional aspect can query eligibility and skip the target.
- Either option depends on a union predicate in the code model, which is finding CM-1 of theme 03. The rule cannot
  read the Roslyn flag directly, because the eligibility rules live in the public assembly, which does not reference
  Roslyn, and the flag is declared on the type symbol interface and is absent from the Roslyn 5.0 variant that serves
  Rider. The predicate must therefore be an `INamedType` member whose implementation is variant-gated and returns
  false in the lower variant.
- Size: small for the blanket check; large for the precise per-advice validation, because it requires the
  variant-gated predicate, changes in eight advice implementations, new diagnostics and tests.
- Status: decision required. The decision is whether 2027.0 refuses every advice on a union with one diagnostic or
  validates per advice kind, and, within that, whether the refusal is expressed as an eligibility rule or as a
  validation exception. The story must be written on top of pull request #1879, which adds the neighbouring
  eligibility plumbing for compiler-synthesized record members and takes the diagnostic identifiers LAMA0552 and
  LAMA0652, so a new diagnostic must take another number, and it must cite #1881 for the variant gating of the union
  predicate. There is no implementation, no pull request and no issue on the subject.
- Verification: the code pass confirmed that eligibility admits a union, that the implicit-constructor test excludes
  the case constructors and that the initializer statements are then lost silently rather than asserting, added the
  third compiler error CS9375 that the original report omits, and corrected two mechanisms: the epilogue emitter does
  not emit forwarding constructors, and the struct-field helper materializes a constructor rather than introducing a
  field. The semantics pass verified the symbol shape of the synthesized union members and the three error codes and
  their message texts in the Roslyn sources, and narrowed the two prohibitions to instance fields and to public
  single-parameter constructors. The scope pass confirmed that no union predicate and no union rule exists anywhere
  in the framework, that the extension-block guards are the model to copy, and that no issue tracks the change.

### LK-9. Inlining copies user labels verbatim, and labeled loops make label collisions realistic

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/Substitution/InliningSubstitution.cs:34`, `:51-53`,
    `:57`, `:71-72`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerRewritingDriver.cs:73`, `:105-111`, `:137-147`,
    `:160`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerLinkingStep.CleanupBodyRewriter.cs:93-97`,
    `:194-208`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LexicalScopeFactory.Visitor.cs:86-91`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/Inlining/InliningAnalysisContext.cs:45-53` and
    `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.Rewriter.cs:921-922`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerInjectionStep.cs:75`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.InlineabilityAnalyzer.cs:165-174`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SubstitutionGenerator.cs:339`, `:530`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateCompilerRewriter.cs:390-441`, `:491-497`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateAnnotator.cs:2601-2606` (no
    `VisitLabeledStatement` override) and
    `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplatingDiagnosticDescriptors.cs:24-30`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateLexicalScope.cs:13-17`
  - `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:35-43`, `:55-57`,
    `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:425-440`,
    `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1296`, `:1307`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.LinkerTests/Tests/Methods/Overrides/TargetBody/UsingLocal_Jump.cs:24`,
    `:27` and its expected output at `:32`, `:34`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/Overrides/Methods/MultipleAspects.t.cs:5-11`
- What happens today: a label is useful only as the target of a `goto` statement, and a template may not contain one,
  because the template annotator reports LAMA0101 for it. A label in a template body is therefore legal and useless,
  and it is not impossible, because an unreferenced label is reported only by the warning CS0164. With C# 15,
  `outer: foreach (...) { ... break outer; }` becomes an ordinary idiom and the case becomes realistic. A template
  label is emitted into the target verbatim, because the template compiler renames only declarations whose symbol is
  a local, a parameter, a type parameter, a local function or an anonymous function, which does not include a label
  symbol. The linker then merges bodies into one statement list: the inlining substitution returns the inlined body
  inside a block flagged as flattenable, every substituted body carries the same flag, and the cleanup rewriter
  splices those blocks recursively and unconditionally into the enclosing statement list. No substitution renames a
  user label.
- Two labels of the same name therefore end in one block after flattening, which is CS0140, or one label shadows a
  label of a contained scope when the inlined body remains inside a nested block, which is CS0158. Both are errors,
  both are reported on the labeled statement itself, and both are independent of any `break` or `continue` that
  references the label; the language proposal changes neither the label declaration space nor the shadowing rule and
  quotes both unchanged. The collision arises in two configurations that are equally realistic. The first is a
  template label against a label of the target body, which the linker test `UsingLocal_Jump` already shows for a
  user label copied verbatim through an inlining. The second is a template label against itself: the inlineability
  rule counts the references to one semantic and not the expansions of one template, so a chain of overrides is
  inlined into one method, and two aspects that expand the same template place two copies of the same label in one
  block.
- The scenario is gated on C# 15 being enabled in Metalama, and one further defect currently precedes it: the
  generator strips the experimental label field of the break and continue statements before generation, so a template
  written with `break outer;` is rebuilt as a plain `break;` today. That is finding TP-3 of theme 02.
- Consequence: a build error in the transformed compilation, CS0140 or CS0158. Two adjacent failure modes of the same
  rewrite are CS9393 or CS9394, when a label no longer labels the loop it precedes, and, if the label of a break or
  continue statement is dropped, a silent change of control flow with no diagnostic.
- Proposed change: rename the labels of the inlined body when it is spliced into the caller. The natural place is
  `InliningSubstitution.Substitute`, which already holds the substituted body, or a dedicated rewriter invoked from
  there; a new node substitution is a poor fit, because a substitution is keyed on a single pre-registered node while
  the rename covers a whole body. Do not allocate the new names through the lexical scope factory: it is constructed
  in the injection step (`LinkerInjectionStep.cs:75`) and is not available to the analysis or linking steps, and the
  template lexical scope is documented as intentionally single-threaded while substitutions are generated
  concurrently. Follow instead the precedent of the return label and derive the new name from the inlining context,
  which is deterministic and thread-safe.
- The rewrite has to cover the labeled statement identifier, the target expression of a `goto` statement, because
  user `goto` statements do occur in a target body and the linker emits its own, and the label of a break or continue
  statement. The last two members do not exist in Roslyn 5.0 and are experimental in the build consumed today, so a
  strongly typed rewrite belongs to the latest variant only and needs a suppression until that variant is built from
  a Roslyn later than 2026-08-11. The alternative that compiles in both variants without a suppression is an override
  of the identifier-name visit that tests whether the parent node is a break or continue statement, which uses only
  syntax kinds that already exist in Roslyn 5.0 and is inert under the lower variant. Two language rules must be
  respected by whatever rewrites these statements: only the statement immediately nested within a labeled statement
  is labeled with that identifier, so the rewrite must not insert a statement or a block between a label and its
  loop; and dropping the label of a break or continue statement produces no diagnostic and silently retargets the
  jump to the innermost loop.
- A smaller alternative is worth measuring first: leave the syntax alone and refuse the inlining when the inlined body
  and the destination body declare a label of the same name, by treating the semantic as not inlineable in the
  inlineability analyzer. That produces a call instead of an error and needs no new rewriting.
- Add linker tests under `Tests/Methods/Overrides/Labels` covering a labeled loop in the override only, in the target
  only, in both, and in two chained overrides expanded from the same template.
- Size: medium.
- Status: decision required. The decision is whether the labels of an inlined body are renamed or the inlining is
  refused when the labels collide. The story is the linker half of a feature whose template half is finding TP-3 of
  theme 02, and it must cite #1896 as the precedent for raising the template language version and #985 as the open
  catch-all umbrella, which does not scope this work. The linker documentation issue #964 owns the section of
  [`linker-inlining.md`](../linker-inlining.md) that the proposal extends, and #966 places the work in the existing
  inlining-correctness backlog. Nothing is implemented and nothing is in flight.
- Verification: the code pass confirmed that the inlined body is flattened into the caller statement list, that no
  component renames user labels, that the template compiler does not rename them either, and that an existing linker
  baseline shows a user label copied through an inlining, and it corrected three statements: two overrides expanded
  from one template are chained into one method, the linker return label does not come from the lexical scope, and
  the proposed allocation mechanism is neither reachable from the linking step nor thread-safe there. The semantics
  pass confirmed the language feature, the unchanged declaration space and shadowing rules, and the two diagnostic
  identifiers from the Roslyn sources, and it corrected the target version, the experimental status of the two
  members and the framing of the case as impossible today. The scope pass confirmed the file contents, found no
  issue, no pull request and no test directory, and recorded that the version-neutral rewrite is the only form that
  compiles for both variants.
- Open questions: the open question of the original report is answered. The template compiler does not preserve the
  label of a break or continue statement today, because the generator strips the experimental field before
  generation.

### LK-10. The text-span classifier has no union dispatch, so compile-time union declarations are not classified

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Formatting/TextSpanClassifier.cs:54-75` (`VisitCore`, which
    only guards recursion), `:77-141` (the five handled kinds and the shared helper), `:163-173` (`VisitMember`,
    which marks a member only when the compile-time-type flag is set), `:278-298` (`DefaultVisit`, which marks only
    inside a template)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateAnnotator.cs:743-775` (the same five kinds,
    and the run-time-only annotation that makes the classifier skip run-time types), `:446-451` and `:627-696` (the
    default path, which annotates an unhandled type declaration as run-time or compile-time)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Formatting/ClassificationService.cs:32-52`,
    `Metalama.Framework/src/Metalama.Framework.Engine/Formatting/FormattedCodeWriter.cs:102-110`,
    `Metalama.Framework/src/Metalama.Framework.DesignTime/VisualStudio/Classification/DesignTimeClassificationService.cs:35-47`,
    `Metalama.Framework/src/Metalama.Extensions.HtmlWriter/HtmlCodeWriter.cs:32-43`,
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/BaseTestRunner.cs:943-1001`
  - `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxWalker.cs:44-72`
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Formatting/AllDeclarations.cs.html:84`
- What happens today: a union declared in a project that contains compile-time code is not classified at all, and the
  effect is wider than the declaration line. `VisitTypeDeclaration` is the only place that sets the flag recording
  that the walk is inside a compile-time type, so `VisitMember` returns without marking and without recursing, and no
  member of the union is classified either. Nothing throws and nothing is reported, because the safe walker only
  wraps exceptions and the union node falls through to the default visit, which marks nothing outside a template.
  Interfaces and extension blocks have the same gap today, since neither the classifier nor the template annotator
  has a dispatch for them. The output is not used at design time only: the same classification feeds the Visual
  Studio classifier, the Aspect Workbench, the writer of the documentation in hypertext markup language and the
  corresponding baselines of the formatting aspect tests.
- Consequence: silent wrong output, cosmetic. The compile-time colouring is missing inside the union declaration, and
  no exception and no incorrect generated code follows.
- Proposed change: the classifier route alone is not sufficient and would introduce a false positive. The template
  annotator has no dispatch for unions either, and its default path annotates a type declaration that no override
  handles as run-time or compile-time, a value that the classifier accepts as compile-time. Routing every unhandled
  type declaration to the classifier helper would therefore mark run-time unions, run-time interfaces and extension
  blocks as compile-time, whereas a run-time class is correctly skipped today because the annotator annotates it as
  run-time only. The fix has two parts, both written against the abstract type-declaration node so that the source
  still compiles for the Roslyn 5.0 variant and does not name the experimental union type: first, in the annotator,
  route an unhandled type declaration to its own `VisitTypeDeclaration` so that the real symbol scope is computed;
  second, in the classifier, route the same nodes to its helper through the interception point used by LK-1. The
  classifier helper uses only members of the abstract base, so it compiles unchanged. Because the same route also
  changes interfaces and extension blocks, the formatting baselines must be adopted again; no current baseline
  contains a declared interface, so a new case should be added to `Tests/Formatting/AllDeclarations.cs`.
- Size: small for the two routes, plus the adoption of the formatting baselines that the interface and extension-block
  change affects.
- Status: new work. The classifier contains no union and no interface override, no pull request touches the
  formatting directory, and no issue scopes the change; #985 concerns the template compiler and #940 concerns
  highlighting robustness rather than the set of classified kinds. This is the lowest severity of the theme and
  should ride along with the dispatch story rather than justify one of its own. It is also the one member of the
  union dispatch family that needs no new type reference and no gating, so it can precede the move to Roslyn 5.12.
- Verification: the code pass confirmed the absence of the dispatch, established that the whole union body loses its
  classification rather than the declaration header only, listed the four surfaces that consume the classification,
  and refuted the proposed change as incomplete by showing that the annotator must be corrected in the same work,
  with a baseline that pins the contrast for a run-time struct. The semantics pass did not run on this finding,
  because it depends on no external semantics. The scope pass confirmed that the same gap already exists for
  interfaces, that the fix is variant-safe, and that nothing is implemented, in flight or tracked.
- Open questions: the effect is verified for the absence of the dispatch and plausible for its visible consequence,
  because it requires a union in compile-time code, which is unlikely in 2027.0 while the compile-time language
  version stays at C# 14.

## Withdrawn findings

No finding of this theme was withdrawn. All ten findings of the original report survived the three verification
passes, and none was refuted at its core. Several statements inside them were refuted and are corrected above; the
five that most change the picture are recorded here so that a reader of the original report knows they were
considered.

The original report described LK-1 as a defect observable today. No Roslyn that Metalama consumes exposes C# 15 as a
non-preview language version, and Metalama caps the language version at C# 14 and rewrites an implicitly set language
version of 15.0 down to 12.0, so no union node reaches the linker in the shipped configuration. Every union finding of
this theme is conditional on the C# 15 enablement, which does not reduce its importance for 2027.0 but does fix the
order in which the work can be done and tested.

The original report placed the failure of LK-1 in the injection registry. Template expansion fails earlier for most
advice, because it needs a lexical scope and the helper that derives the declaring type omits the union kind as well.
The exception type also depends on the build configuration, because the assertion helper throws only under the
`DEBUG` symbol and returns the null reference otherwise, so a shipped build raises a null-reference or
argument-null exception at the same sites.

The original report proposed to replace the syntax-kind lists by a bare type test. That contradicts the doctrine of
issue #1307, which an analyzer enforces with LAMA0860, so every rewritten site would fail the continuous integration
build under the zero-warning gate. It also proposed to exclude the record declaration from `IsPrimaryConstructor`,
which would make the method return false for the primary constructor of a positional record and break three unit
tests. Both parts of the LK-3 proposal are replaced above.

The original report left open whether a partial declaration of a closed class must repeat the modifier, and proposed
to copy the case list of a union into the generated partial part. The compiler unions the modifiers of all partial
parts, so the generated part is correct as it stands; and it accepts the case list on at most one part and reports
CS8863 on every further part that carries one, so copying it would trade one compilation error for another.

The original report treated the union member restrictions as a consequence of the epilogue emitter emitting
forwarding constructors, and attributed a backing-field introduction to the struct-field helper. Neither mechanism
works that way: the epilogue emitter introduces an optional parameter into a materialized constructor, the forwarding
constructors come from the required-parameter overload of the parameter introduction, and the struct-field helper
materializes the implicit parameterless constructor. The three compiler errors of LK-8, one of which the original
report omits, are reached by those routes instead.

## Non-findings

The following were checked and found unaffected. The line references are those of the original report and were
re-verified only where a finding above depends on them.

- Labeled `break` and `continue` in the linker rewriters. No linker rewriter reconstructs an existing break or
  continue statement. The only construction sites are
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/Substitution/ReturnStatementSubstitution.cs:79-110` and
  `:150-160`, which create a fresh unlabeled `break;` to replace a `return;` located directly in a switch section,
  and an unlabeled break still targets the innermost switch under C# 15.
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerLinkingStep.CountLabelUsesWalker.cs:24-31` counts
  `goto` uses only and
  `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerLinkingStep.RemoveTrivialLabelRewriter.cs:49-60`
  removes a `goto` immediately followed by its label; a labeled break is not counted, but the removed pair can only be
  linker-generated, which user code does not reference. The cleanup rewriter may attach a linker label to a following
  user loop, which is legal and does not change the resolution of user labels. The constructor epilogue rewriter
  rewrites `return;` only. The substitution for a block with a return before a using local reasons about outgoing
  `goto` statements only, and a labeled break can only target an enclosing statement, so it cannot jump forward past
  a using declaration. All linker rewriters derive from the safe rewriter, whose interception point calls the Roslyn
  base visit, so the label child of a break or continue statement is carried by the generated Roslyn visitor.
- The discovery of exit-flowing statements
  (`Metalama.Framework/src/Metalama.Framework.Engine/Linking/LinkerAnalysisStep.SemanticBodyAnalyzer.cs:330-336`)
  already descends into the inner statement of a labeled statement, so a labeled loop that ends a method is analyzed
  like an unlabeled one.
- Collection expression arguments. The custom simplifier passes unknown nodes through
  (`Metalama.Framework/src/Metalama.Framework.Engine/Formatting/CodeFormatter.CustomSimplifier.cs:29-40`), a
  collection expression needs no parentheses under a cast whether or not it carries a leading `with(...)` element
  (`Metalama.Framework/src/Metalama.Framework.Engine/SyntaxGeneration/ContextualSyntaxGenerator.cs:1052`), the
  similarly named member of the nullable-annotation rewriter is the element list of a tuple type and is unrelated,
  and the template annotator visits each element of a collection expression through the generic visit
  (`Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateAnnotator.cs:3495-3502`). No throwing default
  over the collection-element kinds exists in the engine.
- The code formatter pipeline
  (`Metalama.Framework/src/Metalama.Framework.Engine/Formatting/CodeFormatter.cs:86-207`). The custom
  simplification, the import adder, the Roslyn reducer, the token-level fixer and the Roslyn formatter are all
  driven by Roslyn or are agnostic of the node kind, and the annotation file is plumbing only. The one throwing kind
  switch of the contextual syntax generator (`ContextualSyntaxGenerator.cs:786-815`), which lacks the record, the
  extension block and the union declaration, has no caller in `Metalama.Framework`, `Metalama.Patterns`,
  `Metalama.Extensions` or `Metalama.Migration`; it is a public helper for weavers.
- Closed classes in the linker and in advice. The rewriters preserve the modifier list of the original declaration
  (`LinkerInjectionStep.Rewriter.cs:359-455`, `LinkerRewritingDriver.Types.cs:18-44`). The validation of an
  introduced member (`IntroduceMemberAdvice.cs:206-214`) reads the abstract flag, which a closed class reports as
  true through the symbol, so abstract members may be introduced. The constructed-object advice and its epilogue
  emitter key their guard on the sealed flag and on the struct type kind, both unaffected, and the initialize-method
  advice likewise. The aspect reference resolver special-cases interface members only.
- Extension indexers and the aspect reference resolver. The resolver has no indexer-specific and no
  extension-specific branch; element accesses are rewritten by the renaming and base substitutions regardless of the
  receiver, and the property inliners match on the shape of the statement around the annotated root and not on the
  receiver.
- Extension blocks in the injection step. Members are injected into introduced extension blocks
  (`LinkerInjectionStep.Rewriter.cs:622-637`) and receiver-contract statements are distributed per member including
  the indexers of an extension block (`LinkerInjectionStep.cs:251-263`, `:837-880`, `:1155-1166`).
- Static members in interfaces. The validation of an introduced member (`IntroduceMemberAdvice.cs:174-186`) rejects
  only static virtual members outside interfaces and static sealed members, and the member modifier helper emits the
  static keyword for interface members and the abstract and virtual keywords only when the member declares them. No
  engine code checks the runtime support for default interface members. The feature therefore changes nothing in
  this theme; whether the eligibility of an introduced member has to be relaxed for a desktop target is a code-model
  and platform question.
- Records as union candidates, with one correction. The record-specific code in the injection rewriter, the primary
  constructor materialization, the positional properties of the rewriting driver, the record helper and the
  copy-constructor exclusions all key on the record flag or on the record declaration kinds. A union is not a record
  and has no positional properties, no primary constructor and no copy constructor, so those sites are not extended
  to unions, and the union-specific constraints are LK-8. The correction is that
  `LinkerRecordHelper.GetSynthesizedMethodOverrideTargets` (`LinkerRecordHelper.cs:45`) is in scope after all,
  because the override-constructor eligibility rule requires no explicit declaration and therefore admits the
  synthesized case constructors of a union, which that helper would silently omit.
- Introduction builders. The indexer builder
  (`Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Introductions/Builders/IndexerBuilder.cs:28-38`)
  accepts any named type including an extension block, so LK-6 needs no builder change, and the extension block
  builder already models the static-extension case that the LK-6 rule reads.
- Tests that pin current behaviour and that a union or closed change must keep green: 82 record aspect tests, the
  initialization tests for records, the record fabric tests, the 19 inputs of
  `Tests/Aspects/CSharp14/ExtensionMembers`, the 13 inputs of `Tests/Aspects/Overrides/Indexers` including
  `NotInlineable`, which pins LAMA0699, and the linker tests that pin the rewrite of a `return` into a `break` inside
  a switch section. No existing test contains `union` or `closed` as a keyword.

## Related themes

- The union dispatch of the injection rewriter, of the linking rewriter and of the eligibility rules is the one
  cluster that this theme owns. It carries LK-1, LK-2 and LK-8 together with finding CM-8 of theme 03, which extends
  the substitution mechanism of pull request #1879 to the compiler-synthesized union members. The three linker
  findings form a chain that cannot be broken safely, because repairing the injection dispatch without the linking
  dispatch produces a worse state than repairing neither, and repairing both without the eligibility rules turns a
  dropped injection into code that the compiler rejects.
- Extension indexers are the second cluster that this theme owns, carrying LK-6 and LK-7 with finding UT-16 of theme
  06, which covers the contract advice and the not-null fabric over the same member shape. The three share one
  prerequisite, a compilation that accepts an extension indexer, so none of them can be validated separately.
- The hand-written type-declaration kind lists are owned by theme 03, which carries LK-3 together with findings CM-2
  and CM-6 of that theme and DT-1 and DT-6 of theme 05. It is one edit with one shared decision, and it must land
  before LK-1, LK-2 and LK-8, because those call the predicates it repairs.
- The inventory of syntax visitors that inherit the Roslyn dispatch and therefore never observe a union declaration
  is finding CM-7 of theme 03, with LK-10 as one of its members. LK-10 cannot be corrected alone, because the
  template annotator has no union dispatch either.
- The design-time generated partial part of a union is owned by theme 05, which carries LK-4 with findings CM-3 of
  theme 03 and DT-2 of that theme. The three describe one arm of one factory and disagree only about the
  discriminator, which is one decision.
- Closed hierarchies are owned by theme 03, which carries LK-5 with findings CM-4 and CM-5 of that theme, TP-10 of
  theme 02 and UT-15 of theme 06. Grouping them allows the closed feature to be deferred as a whole rather than
  leaving a half-implemented modifier behind.
- Labeled `break` and `continue` are owned by theme 02, which carries LK-9 with finding TP-3 of that theme and UT-17
  of theme 06. Both halves read the label of a break or continue statement, which only the regenerated latest variant
  exposes, so writing them apart would duplicate the gating work.
- The renumbering of the latest Roslyn variant to the stable 5.12 and the regeneration of the syntax model are owned
  by theme 01. Every union finding of this document and every test proposed for extension indexers depends on it.
- The C# language version tables that stop at C# 14 are owned by theme 01. Until they are raised, no test proposed in
  this document can request C# 15, and the behaviour of the test harness when a test requests a language version that
  the running Roslyn does not recognise, which is to skip the test with a reason and report no failure, is owned by
  theme 06.
- The Roslyn variant gating strategy, that is how the engine may name an application programming interface member
  that exists only in the latest variant, is finding CM-10 of theme 03. LK-1, LK-2, LK-4, LK-5, LK-9 and LK-10 each
  depend on its outcome, and each of them is written above so that the version-neutral form is available if the
  decision goes that way.
- The union predicate in the public code model, on which the eligibility rules of LK-8 and the discriminator of LK-4
  depend, is finding CM-1 of theme 03.
- The Roslyn public API delta and the semantics of each C# 15 feature are recorded in
  [`analysis-reports/08-roslyn-api-delta.md`](analysis-reports/08-roslyn-api-delta.md).
