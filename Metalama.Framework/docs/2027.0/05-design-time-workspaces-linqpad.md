# 05. The design-time pipeline, workspaces, LinqPad and the test framework

This document covers the design-time half of Metalama: the generator that produces the partial declarations shown in
the editor, the diff and hashing layer that decides when the design-time pipeline is re-run, the diagnostic
suppressor, the code fixes and code refactorings, the `Metalama.Framework.Workspaces` introspection package, the
LinqPad driver, and the aspect and unit test frameworks that exercise all of them. It records how each of them
behaves when a C# 15 union declaration, a labeled `break` or a C# 15 language version reaches it, and what has to
change for Metalama 2027.0. The analysis reads the code as it stands on 2026-09-03 on branch
`topic/2027.0/26-09-03-update-eng-7e3j07` of the `Metalama` repository, and the `Metalama.Premium` code as it stands
on `develop/2027.0`. Each finding was then re-checked by up to three verification passes: a code pass that re-read the
cited code and tried to falsify the claim, a semantics pass that re-checked every external premise against
`dotnet/roslyn`, `dotnet/csharplang`, `dotnet/msbuild`, `dotnet/arcade` and nuget.org, and a scope pass that
established whether the proposed change is already implemented, in progress or tracked by an issue. The platform
baseline PB-2027.0 is decided by [`platform-support.md`](../platform-support.md), the permitted package versions by
[`Directory.Packages.md`](../../../Directory.Packages.md), and the procedure for moving to a new Roslyn by
[`updating-roslyn.md`](../updating-roslyn.md); this document cites them rather than restating them.

No project was built and no test was run for this analysis.

## Summary

1. A `partial union` is not recognized as partial by the code model, because the hand-written list of type
   declaration kinds does not name the union kind. The design-time generator therefore reports `LAMA0048` instead of
   producing the partial declaration, and the code fix registered for `LAMA0048` appends a second `partial` modifier
   to a declaration that already carries one (DT-1).
2. Correcting only the kind test is not sufficient. The factory that builds the generated partial declaration selects
   the declaration form from the Metalama `TypeKind`, where a union is a struct, so it would emit `partial struct`
   beside the source `partial union` and the compiler would report CS0261 (DT-2).
3. The generated design-time hashers were produced from a grammar with the C# 15 nodes stripped as experimental, so a
   rename of a union, the addition of `partial` to a union, and a change of the target label of a `break` or a
   `continue` inside a template are invisible to the design-time diff. The pipeline is not re-run and the stale
   result persists until an unrelated edit (DT-3).
4. The design-time pipeline verifies no language version, and that is the correct design, because the design-time
   host may present a Roslyn whose default language version differs from the one used during the build. The
   implementation work is in the compile-time language version tables and in the MSBuild `LangVersion` rewrite, which
   belong to the language version theme (DT-4).
5. The helper that adds an attribute for the "add aspect" code action enumerates declaration kinds and returns `null`
   for records, record structs and extension blocks today, and would return `null` for unions. The code action then
   reports success and applies no change (DT-5).
6. A suppression registered on a union type is never matched for a diagnostic located on the union header, because
   the helpers that find the declaring node enumerate kinds and omit the union kind. The same helper serves the
   compile-time scoped suppressions, so the gap is not confined to design time (DT-6).
7. C# 15 cannot be requested by a test on any Roslyn variant that Metalama consumes today, and a test that requests
   it is marked skipped rather than failed, so a suite with no C# 15 test executed reports success (DT-7).
8. Two constraints of the release complete the theme. Every published package that depends on `Microsoft.CodeAnalysis.*`
   at the consumed prerelease version cannot be restored from nuget.org, which serves nothing above 5.9.0 (DT-8); and
   a .NET 10 host of the introspection packages cannot use a machine that carries only the .NET 11 SDK (DT-9).

## Findings

### DT-1. A `partial union` is reported as not partial, and the code fix duplicates the modifier

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:329-352` (`IsPartial`;
    the type-declaration arm is at `:344`, the default arm at `:347` and the modifier test at `:350`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35`
    (`IsTypeDeclaration`) and `:41` (`IsBaseTypeDeclaration`, which is defined in terms of `IsTypeDeclaration`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:158-165`
    (the `LAMA0048` report at `:162` and the early return at `:164`), `:720-760` (the `TypeKind` switch that builds
    the generated partial declaration) and `:940-951` (`IsInNonPartialSourceType`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Diagnostics/GeneralDiagnosticDescriptors.cs:208-216`
    (`TypeNotPartial`, that is `LAMA0048`, severity warning)
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/CodeFixes/TheCodeFixProvider.cs:57` (the identifier is
    registered as fixable), `:111-123` (the "Make partial" action), `:164-173` (the unconditional
    `AddModifiers(partial)`) and `:187-193` (`GetTypeDeclaration`, whose only match is `BaseTypeDeclarationSyntax`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SymbolExtensions.cs:283-291`
    (`IsPrimaryConstructor`, whose kind test is at `:289` and which fails on a union primary constructor for the same
    reason)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/LinkerPipelineStage.cs:113-124` (the same
    generator runs during a build with a deliberately discarded diagnostic sink)
  - `Metalama.Framework/src/Metalama.Framework.Engine.Analyzers/KindCheckOptimizationAnalyzer.cs:722-727` (the
    analyzer recognizes the predicate by name only, and requires a kind check before a type test)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/AllLanguageVersions.cs:13-19` (the numeric-cast
    precedent) and `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:69-79`
    (the `TypeKind` mapping that a union reaches)
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1954-1978` (`UnionDeclarationSyntax`, whose base is
    `TypeDeclarationSyntax` and which overrides `Modifiers`) and `:2083` (`ExtensionBlockDeclarationSyntax`, which
    also derives from `TypeDeclarationSyntax` and is deliberately excluded)
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.0.0.xml` (no occurrence of the union node) and
    [`testing.md`](../testing.md):58 (no production source branches on a variant constant)
- What happens today: `IsPartial` reads the modifiers of the primary declaration only when its kind satisfies
  `IsTypeDeclaration`, which lists class, struct, interface, record and record struct. `UnionDeclarationSyntax`
  derives from `TypeDeclarationSyntax` and carries its own `Modifiers` field, so the type test of that arm matches,
  but the kind test does not, the switch falls to the default arm, the modifier list is empty and `IsPartial` is
  false even when the source says `public partial union U(...)`. `ProcessTransformationsOnType` then reports
  `LAMA0048` and returns before generating anything, and `IsInNonPartialSourceType` gives the same answer for types
  nested in the union. The code fix registered for `LAMA0048` selects its target with a type test on
  `BaseTypeDeclarationSyntax` and no kind test, so it does match a union declaration, and it applies
  `AddModifiers(partial)` without inspecting the existing modifier list, so applying it to a union that is already
  partial produces `public partial partial union U`, which is the duplicate-modifier error CS1004. The same root
  cause makes `SymbolExtensions.IsPrimaryConstructor` return false for the primary constructor of a union, and unions
  are declared with a parameter list, so that path is exercised by the same source. The defect is latent: `union` is a
  contextual keyword that the parser recognizes only when the unions feature is enabled, and in the Roslyn build that
  Metalama consumes today that feature requires `LanguageVersion.Preview`, so the scenario is reachable only in a
  preview-enabled project and becomes an ordinary C# 15 scenario after the move to the stable Roslyn 5.12.
- Consequence: a diagnostic reported wrongly and a silent omission. In the editor the user sees a `LAMA0048` warning
  that the source already satisfies, and the members introduced into the union are never shown, because the generator
  returns before producing a file. During a build the same generator runs from `LinkerPipelineStage`, whose
  diagnostic sink is deliberately discarded, so the build reports nothing and merely omits the
  `DesignTimeGeneratedCode` output file while the linker still injects the members into the produced assembly. The
  offered code fix then produces source that does not compile. A partial correction that fixes only the kind test
  converts the wrong `LAMA0048` into the compilation error CS0261 in the generated file, which is DT-2.
- Proposed change: make `SyntaxKindExtensions.IsTypeDeclaration` recognize the union kind. `IsBaseTypeDeclaration`
  needs no separate change, because it is defined as `kind.IsTypeDeclaration || ...`. Do not replace the kind test by
  a plain `TypeDeclarationSyntax` type test: `ExtensionBlockDeclarationSyntax` also derives from that base type and is
  deliberately excluded, and `KindCheckOptimizationAnalyzer` requires a kind check before a type test. Two shapes
  obtain the kind without naming a member that Roslyn 5.0.0 does not define. The first declares a numeric constant in
  the manner of `AllLanguageVersions`, which keeps the `is ... or ...` pattern intact but hard-codes a value that may
  still move before the stable release. The second resolves the kind once with `Enum.TryParse<SyntaxKind>` into a
  `static readonly` field, which is version-proof but is not a constant, so the body of the property has to be
  restructured into an equality test combined with the pattern. The choice is part of the variant decision recorded as
  CM-10. Review every consumer of the predicate rather than treating the edit as a one-line change: about nine other
  production call sites read it, including `SymbolExtensions.IsPrimaryConstructor` and the compile-time code
  detection. Independently of unions, harden `TheCodeFixProvider.GetFixedDocumentAsync` so that it does not add a
  `partial` token to a declaration whose modifier list already contains one. Add a unit test for `IsPartial` on a
  union and a design-time aspect test under the test plan of DT-7; the aspect of that test must introduce a member
  that a union body permits, because instance fields, auto-properties and field-like events are CS9373 and a public
  single-parameter constructor is CS9374, so an introduced method is the safe choice.
- Size: large as merged. The kind predicate alone is a medium change; the estimate was raised because a correct fix
  must also emit a union declaration in the generated partial part, which is tracked separately as DT-2.
- Status: new work. The change is not implemented, not in progress in any open pull request and not tracked by any
  issue: no C# source in either repository names the union kind, and no issue of `metalama/Metalama` mentions unions
  or C# 15. The related issues are #1881, whose pull request added the Roslyn 5.10.0 variant and removed all 177
  conditional-compilation blocks from production source, which is the constraint that forces the kind to be obtained
  without a variant branch; #1039 and #1034, the C# 14 umbrella and its code model story, which are the precedent for
  the shape of a C# 15 union story; and the open #985, the template compiler catch-all, which a union story should
  state that it does or does not take over. The implementation is gated by the variant decision CM-10.
- Verification: the code pass confirmed every cited location, corrected the description of `IsBaseTypeDeclaration`,
  corrected the cited precedent (the existing file declares numeric casts as constants, which the original proposal
  rejected), added the build-time consequence through `LinkerPipelineStage`, and found the duplicate-modifier defect
  in the code fix to be independent of unions. The semantics pass confirmed on `dotnet/roslyn` that a union
  declaration is a `TypeDeclarationSyntax` whose kind is absent from the predicate, that `partial` is a legal union
  modifier, that a union body may carry members, and that a union symbol arrives as an ordinary named type of
  `TypeKind.Struct`, and it corrected the version naming from 5.10 to 5.12. The scope pass confirmed that the
  predicate, the consumer and the code fix are unchanged on the working branch, that no open pull request touches
  them, and that no issue tracks the work.
- Open questions: whether the code model should expose `INamedType.IsUnion`, which is owned by the code model theme
  as CM-1. One residual risk remains: the claim that a union symbol carries `TypeKind.Struct` rests on the
  state of `dotnet/roslyn` main. If the stable Roslyn were to give unions a new `TypeKind`, the default arm of the
  mapping at `SourceNamedTypeImpl.cs:69-79` would throw and the consequence class would become a crash rather than a
  wrong warning.

### DT-2. The generated partial part of a union is emitted as `partial struct`, which conflicts with the union declaration

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:697-790`
    (`CreatePartialType`, whose switch at `:720` has four arms and whose throwing default arm is at `:788`), `:749`
    (the `TypeKind.Struct when !type.IsRecord` arm), `:817-823` (`AddHeader`), `:506-523`
    (`AddPartialModifierToTypes`), `:54-57` (the parse options of the compilation, already reused) and `:86-105` (the
    per-group containment that reports `LAMA0049`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Source/SourceNamedTypeImpl.cs:69-79` (the Roslyn
    `TypeKind.Struct` becomes the Metalama `TypeKind.Struct`) and `:173` (`IsRecord` reads `INamedTypeSymbol.IsRecord`)
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1954` (`UnionDeclarationSyntax`, base
    `TypeDeclarationSyntax`, marked with an experimental URL) and `:1965` (its `ParameterList` is optional)
  - `dotnet/roslyn` main, `src/Compilers/CSharp/Portable/Symbols/EnumConversions.cs:35-38` (the union declaration kind
    maps to `TypeKind.Struct`), `src/Compilers/CSharp/Portable/Symbols/Source/SourceMemberContainerSymbol.cs:1038-1058`
    (`IsRecord`, `IsRecordStruct` and `IsUnionDeclaration`),
    `src/Compilers/CSharp/Portable/Symbols/PublicModel/TypeSymbol.cs:203-205` (`ITypeSymbol.IsRecord` and
    `ITypeSymbol.IsUnion`), `src/Compilers/CSharp/Portable/Symbols/NamedTypeSymbol.cs:1944-1953` and
    `src/Compilers/CSharp/Portable/Symbols/Source/SourceNamedTypeSymbol.cs:1463-1477` (a union type is a union
    declaration or a type carrying the union attribute),
    `src/Compilers/CSharp/Portable/Declarations/SingleTypeDeclaration.cs:260-266` (parts merge only when the arity,
    the kind and the name match), `src/Compilers/CSharp/Portable/Symbols/Source/SourceMemberContainerSymbol.cs:1455-1461`
    and `src/Compilers/CSharp/Portable/Errors/ErrorCode.cs:224` (the partial kind conflict is CS0261),
    `src/Compilers/CSharp/Portable/Parser/LanguageParser.cs:1838-1839` (the case-type list is parsed only when an
    open parenthesis follows), and
    `src/Compilers/CSharp/Portable/Symbols/Source/SourceMemberContainerSymbol.cs:4119-4149` and `:4992-5060` (a second
    parameter list is CS8863 and no parameter list at all is CS9370)
  - `dotnet/csharplang` main, `proposals/csharp-15.0/unions.md:742-747` (the grammar) and `:872` (a union declaration
    is a plain struct and `record union` is not supported)
- What happens today: `CreatePartialType` switches on the Metalama `TypeKind` and on `IsRecord`. A union symbol has
  `TypeKind.Struct`, because Roslyn maps the union declaration kind to that type kind, and `IsRecord` is false,
  because the public `IsRecord` is the disjunction of the internal record and record-struct predicates, which test
  the declaration kind against record and record struct only. The arm at `:749` therefore emits `partial struct U`.
  The generated part and the union declaration do not merge, because the merge identity of a type declaration
  includes its kind, and both parts carry the `partial` modifier, which is permitted on a union. Roslyn therefore
  reports CS0261, and the generated file breaks the compilation in the editor as soon as DT-1 lets the file be
  generated. `AddHeader` also matches concrete syntax types and would not put the generated-code header on a union
  declaration. The throwing arm at `:788` is not reached for a union, and if it were, the failure would be contained
  per group and reported as `LAMA0049` rather than as a crash.
- Consequence: a diagnostic reported, namely CS0261, raised on whichever of the two conflicting partial declarations
  Roslyn visits second, which may be the source union declaration of the user rather than the generated document. The
  members introduced into the union remain unusable at design time.
- Proposed change: detect a union in `CreatePartialType` from the kind of the primary declaration syntax, resolved as
  in DT-1, and emit a union part. Do not use `ITypeSymbol.IsUnion` as the discriminator: Roslyn defines it as the
  union declaration or the presence of the union attribute, over any type whose kind is class or struct, so a source
  type written as a struct or a class and marked with that attribute reports `IsUnion` as true while its parts are
  declared with the struct or the class keyword; emitting a union part for such a type would produce the same CS0261
  in the opposite direction. The syntax factory for a union declaration is absent from Roslyn 5.0.0 and is
  experimental in the consumed build, so build the part from text with `SyntaxFactory.ParseMemberDeclaration` using
  the parse options of the compilation, which the generator already reuses and which are required because the parser
  gates the union keyword on the language feature, and then apply the modifiers, the type parameter list, the base
  list, the braces and the members through the `TypeDeclarationSyntax` base class, whose `With` accessors are in the
  Roslyn 5.0 shipped public API, so that the union syntax type is never named. The alternative is to route through
  the generated syntax factory of `eng/src/GenerateMetaSyntaxRewriter` once the stable grammar no longer strips the
  node, which is DT-3. The generated part must omit the case-type list: Roslyn accepts a parameter list on exactly one
  part, reports CS8863 for a second one, reads the case types from that single part, and reports CS9370 only when no
  part carries one. The code model therefore does not need to expose the case types, and the member
  `ITypeSymbol.UnionCaseTypes`, which does not exist in the consumed build, is not required for this fix. Extend
  `AddHeader` to `TypeDeclarationSyntax`. Add a design-time scenario aspect test whose expected generated document
  shows the union part.
- Size: medium.
- Status: new work. The defect is present as written on the working branch, no open pull request touches the
  generator, and no issue tracks it. The related issues are #1881, which introduced the grammar file whose union node
  is stripped as experimental and made Roslyn 5.0.0 the lower variant; #1039 and #1034, the C# 14 precedent for
  splitting a language feature into a code model story and a design-time story; and the open #869, type introduction
  of a struct, whose story should state whether introducing a union is in scope. The implementation is gated by the
  variant decision CM-10 and by the move to the stable Roslyn, because the experimental markers are removed only in
  the release that PB-2027.0 targets.
- Verification: the code pass confirmed the four arms of the factory, the absence of a union arm, the header helper,
  the containment as `LAMA0049` and the dependency on DT-1, and it refuted the alternative discriminator proposed by
  the original report. The semantics pass confirmed on `dotnet/roslyn` and `dotnet/csharplang` that a union has
  `TypeKind.Struct` and `IsRecord` false, that the merge identity includes the declaration kind, that the resulting
  error is CS0261, and it answered the open question of the original report by establishing that exactly one part may
  carry the case-type list. The scope pass confirmed that no source file in either repository names the union kind
  and that no pull request or issue covers the change.
- Open questions: none. The open question of the original report, whether a generated part may omit the case-type
  list, is answered affirmatively by the compiler sources cited above.

### DT-3. The design-time diff hashers do not see the C# 15 nodes until the grammar is refreshed from the stable Roslyn 5.12

- Where:
  - `eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs:19`, `:35-43` (`RemoveExperimentalDeclarations`) and
    `:57`; `eng/src/GenerateMetaSyntaxRewriter/Model/TreeType.cs:37` and
    `eng/src/GenerateMetaSyntaxRewriter/Model/Field.cs:51` (the experimental predicates)
  - `eng/src/GenerateMetaSyntaxRewriter/Generator.cs:614-712` (`GenerateHasher`, which emits one visit method per
    surviving grammar node) and `:714-735` (the field-content and trivial-token rules)
  - `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs:18` (the version list) and `:46-47` (the two
    generated hashers)
  - `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.10.0.xml:1296` and `:1307` (the experimental `Name` field of the
    break statement and of the continue statement) and `:1954` (the experimental union node);
    `eng/src/GenerateMetaSyntaxRewriter/Syntax-5.0.0.xml` (no union node at all)
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/Metalama.Framework.DesignTime.csproj:3` and `:33-35` (the
    variant properties and the inclusion of the generated files of that variant)
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/BaseCodeHasher.cs:19` and `:27`, and
    `Metalama.Framework/src/Metalama.Framework.Sdk/Utilities/Roslyn/SafeSyntaxWalker.cs:39` (the walker depth is
    `SyntaxWalkerDepth.Node`)
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/DiffStrategy.cs:73-93`, `:80-85` (an equal
    hash means no change) and `:97-150` (the partial-type invalidation)
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/CompilationChanges.cs:206-226` (a change
    entry is recorded only when the hash differs)
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/PartialTypesVisitor.cs:18-42` and
    `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/PartialTypesHasher.cs:21-47` (only class,
    struct and record route to the shared helper), and
    `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Diff/SyntaxTreeChange.cs:111`
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/Pipeline/Dependencies/DependencyCollector.cs:63-64` and
    `:83-89` (which do register a union, because they test `BaseTypeDeclarationSyntax`)
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/DesignTime/Pipeline/CompilationChangesTests.IsDifferent.cs:13-30`
    (the existing tests use the default parse options)
  - `eng/RoslynVersions/Roslyn.5.10.0.props` and `eng/RoslynVersions/Latest.props` (the variant constant and its
    import), and [`updating-roslyn.md`](../updating-roslyn.md):11-13 and `:36`
- What happens today: the two hashers are generated with one visit method per grammar node, and the generator strips
  every node and every field that carries an experimental URL before generation. The stripping pass has two
  consequences. First, the union node has no visit method, so the Roslyn default visit runs; the walker is constructed at
  `SyntaxWalkerDepth.Node`, which visits child nodes and skips tokens, so the union identifier, its modifiers and the
  `union` keyword contribute nothing to the hash, while the node-typed fields that the grammar declares, namely the
  attribute lists, the type parameter list, the parameter list, the base list, the constraint clauses and the
  members, still do. Second, the generated visit methods for the break and continue statements hash only the
  attribute lists, the keyword and the semicolon, because the `Name` field was removed before generation, so the
  label of a labeled `break` or `continue` is never hashed even by the compile-time hasher, which otherwise hashes
  bodies. An equal hash is treated as no change and no change entry is recorded, so the pipeline is not re-run.
  Renaming a union, or adding `partial` to it in response to `LAMA0048`, in a file with no other change does not
  trigger re-analysis, so the stale result and the stale `LAMA0048` persist; and changing the target label of a
  `break` or a `continue` inside a template does not rebuild the compile-time project, so the template keeps its
  previous behaviour at design time. Separately, the partial-type visitor and the partial-type hasher route only
  class, struct and record declarations to the shared helper, so a partial union is never listed among the partial
  types of a syntax tree version and the partial-type invalidation does not cover unions, whereas the dependency
  collector does register them because it tests `BaseTypeDeclarationSyntax`. That omission is not specific to unions:
  the interface declaration is not routed either, so a partial interface is already treated the same way today. The
  whole mechanism lives in `Metalama.Framework.DesignTime`, so a batch compilation is not affected, and the defect is
  latent until C# 15 is reachable in a Metalama project.
- Consequence: silent wrong output, in the form of design-time staleness. Nothing throws, nothing asserts and no
  diagnostic is produced; the editor keeps showing the previous analysis.
- Proposed change: perform the correction as part of the move to the next stable Roslyn, which is 5.12 and not 5.10.
  Following [`updating-roslyn.md`](../updating-roslyn.md), add the stable grammar as a new file named after the new
  version and register it in the version list of the generator; the document explicitly forbids overwriting or
  renaming the grammar file of the previous version. The union node and the `Name` field of the break and continue
  statements lose their experimental markers when the features ship in C# 15, so the generator will then emit the
  missing visit methods and hash the missing field. Add union visit methods to the partial-type visitor and the
  partial-type hasher, and decide at the same time whether the interface declaration should be added as well, because
  a partial interface has the same gap today. These two classes are compiled for both variants and the union syntax
  type does not exist in Roslyn 5.0.0, so the additions must be guarded; no new file and no policy exception are
  required, because the latest variant already defines a constant that the design-time project imports, and the
  procedure document restricts adding a new constant rather than using the existing one. Add a diff unit test that
  renames a union and changes a `break` label and asserts that the hash changes. That test must set explicit parse
  options, because the existing tests use the default options, and it cannot be gated on parsing the string `15.0`,
  because the corresponding language version does not exist in the Roslyn consumed today and the test would be
  skipped unconditionally; gate it on the variant constant instead, and use the preview language version until the
  move to Roslyn 5.12.
- Size: small for the regeneration; medium with the tests, the variant guard and the partial-type visitors.
- Status: new work. The stripping pass, the generator and the two visitors are unchanged on the working branch, no
  open pull request touches them, and two issue searches returned nothing that scopes the work. The related issues
  are #1881, whose pull request introduced both the grammar file and the stripping pass that create the gap; #1896,
  which tied the template language version to the lower Roslyn variant and is the same constraint that decides
  whether a union visit method may be written in shared source; and the open #985, because the same regeneration also
  refreshes the meta-syntax rewriter and the Roslyn version syntax verifier, so the story must say which of the newly
  generated nodes it verifies and which remain for the template compiler catch-all.
- Verification: the code pass confirmed the stripping pass, the per-node generation, the walker depth, the absence of
  a change entry on an equal hash and the partial-type gap, and it corrected four details, namely the version number,
  the grammar-file procedure, the availability of the variant constant and the proposed test gate. The semantics pass
  confirmed against `dotnet/roslyn` that the experimental markers are present in the consumed window and absent on
  main, that the default visit reaches tokens only at a greater walker depth, that a union may be partial, and that
  the label of a labeled `break` is stored only in the optional `Name` field, and it corrected the release number
  from 5.10 to 5.12. The scope pass confirmed that no C# source in the repository names the union node and that the
  generated hashers are per-variant, so the generated half is corrected by regeneration alone.
- Open questions: the open question of the original report, whether the stable grammar still marks these nodes
  experimental, is answered for `dotnet/roslyn` main, where only the unsafe expression keeps the marker. It cannot be
  answered against a published package, because no stable package above 5.9.0 exists yet, so the statement is
  plausible rather than established for the release that will actually ship.

### DT-4. The design-time pipeline verifies no language version, and correctly so

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:62-93`
    (`VerifyLanguageVersion`, whose comment at `:64-65` states that Roslyn does not set the language version properly
    at design time), `:70-81` (the preview opt-in) and `:177` (the sole call site)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Options/MSBuildProjectOptions.cs:108`
    (`AllowPreviewLanguageFeatures`) and `:167-181` (the language version getter, whose fallback at `:178` returns the
    latest supported version whenever the property does not parse)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/LanguageVersionProvider.cs:45-72` (the .NET SDK major
    is mapped to a language version, and every major of 10 or more yields C# 14 at `:56`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-43`, `:50` and
    `:149-155`; `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/AllLanguageVersions.cs:14-18`;
    `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/LanguageVersionExtensions.cs:33-39` (the
    display mapping, which throws for an unknown numeric value)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplateCompiler.cs:56-79` (the one language version
    check that is reachable at design time, for the separate template language version property, reported as
    `LAMA0052`), reached through
    `Metalama.Framework/src/Metalama.Framework.Engine/Templating/TemplatingCodeValidator.Visitor.cs:549` and
    `Metalama.Framework/src/Metalama.Framework.DesignTime/DiagnosticAnalysis/TheDiagnosticAnalyzer.cs:181`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/DesignTime/DesignTimeSyntaxTreeGenerator.cs:54-57`
    (the generated trees reuse the parse options of the compilation)
  - `Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:115-121` (the rewrite of an
    implicitly set `LangVersion`) and `:243-246` (the accompanying warning)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/SourceTransformer.cs:145-158` (an exception thrown out
    of the pipeline is caught and reported through the compile-time exception handler)
  - The pinned baselines `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/Misc/LanguageVersion.t.cs:2`
    and `.../Tests/Aspects/LanguageVersion/LanguageVersionPreview.t.cs:2`, both of which embed the list of supported
    versions
- What happens today: the language version verification exists only in the compile-time pipeline and is called from
  one place. No file under `Metalama.Framework.DesignTime` or under the design-time part of the engine reads the
  language version, the preview opt-in or the supported-versions table. One check is nevertheless reachable at design
  time, although it does not cover the project `LangVersion`: the template compiler validates the separate
  `MetalamaTemplateLanguageVersion` property and reports `LAMA0052`, and it runs both from the compile-time
  compilation builder and from the design-time analyzer. The compile-time project is built with the language version
  provider in every scenario, and that provider caps the project version at C# 14 for any .NET SDK major of 10 or
  more. Three qualifications bound the description of a C# 15 project. First, the implicit language version is
  computed by the MSBuild targets of Roslyn itself as nine plus the target framework major, capped by a maximum that
  reads 14.0 in Roslyn 5.0, in the stable 5.9.0 and throughout the consumed window, and 15.0 only from the 5.11 window
  onward, so a `net11.0` project receives an implicit language version of 14.0 under the compiler consumed today.
  Second, when the language version is implicitly set, `Metalama.Framework.targets` rewrites any value outside its
  list down to 12.0 and emits a warning, and the rewrite is a property evaluation, so design time and build agree.
  Third, with the consumed compiler an explicit `LangVersion` of 15.0 is rejected by the command line parser with
  CS1617 before Metalama runs. The case that is genuinely silent at design time today is therefore the preview
  language version: the project analyses without any Metalama diagnostic in the editor, templates that use C# 15
  syntax fail in the compile-time compilation with CS8652 because the six C# 15 features are gated on preview in the
  consumed build, and the build reports `LAMA0051` unless the preview opt-in is set. The design-time generated syntax
  trees reuse the parse options of the compilation, so they follow the project language version and need no change.
- Consequence: no design-time impact today, and a narrow inconsistency. After the move to a Roslyn that defines the
  C# 15 language version, and before the supported-versions tables are extended, the compile-time pipeline no longer
  reports the intended diagnostic but throws an argument exception in the display mapping; that exception is caught
  by the source transformer and reported through the compile-time exception handler, so the build fails with an
  internal error diagnostic rather than crashing the compiler.
- Proposed change: no design-time change. Keeping the design-time pipeline free of the version check remains correct,
  because the design-time host may present a Roslyn that maps the default language version differently from the
  build: the Roslyn 5.0 variant that serves Rider maps default, latest and latest-major to C# 14 where Roslyn 5.12
  maps them to C# 15, and that variant also fails to parse the string `15.0`, so a check there would report against
  the wrong version. That rationale is recorded today only in a comment of the compile-time pipeline and should be
  carried into this document or into a comment at the design-time site. The implementation work belongs to the
  language version theme and consists of adding the C# 15 constant as a numeric cast, extending the supported-versions
  table and its display mapping, extending the maximum-version mapping with the boundary at Roslyn 5.11 or 5.12
  rather than 5.10, extending the .NET SDK mapping for major 11, updating the two aspect test baselines that embed the
  list of supported versions, and revising the value list of the `LangVersion` rewrite in
  `Metalama.Framework.targets`, which does not contain 15.0 and therefore lowers an implicitly set C# 15 project to
  C# 12.
- Size: small.
- Status: new work owned elsewhere. The design-time part is a deliberate non-change with a recorded rationale rather
  than an implementation task. Nothing in the repository names the C# 15 language version, no open pull request
  touches the tables, and no issue tracks them. The related issues are the open meta-issue #1921, under which a
  language version story belongs and none of whose sub-issues covers it; #1881, whose body states that C# 15 needed no
  change because the consumed preview stops at C# 14, which is the statement a C# 15 story reverses; #1896, which
  pins the template language version to the lower Roslyn variant; the open #985; and #1898, which already routes a
  host at or above the latest variant to the latest payload.
- Verification: the code pass confirmed that the verification method has exactly one declaration and one call site
  and that no design-time file reads the language version, and it corrected three supporting details, namely the
  existence of the template language version check at design time, the lowering of an implicitly set C# 15 project to
  C# 12 by the targets, and the unreachability today of both the diagnostic and the argument exception for the value
  1500. The semantics pass confirmed against `dotnet/roslyn` that the C# 15 language version has the value 1500 on
  main and is absent from the consumed build, that the implicit language version comes from the MSBuild targets of
  Roslyn and moves to 15.0 only in the 5.11 window, and that the Roslyn 5.0 variant maps the default language version
  to C# 14, and it corrected the error identity from a not-available message to CS8652. The scope pass confirmed that
  none of the tables has been extended and that no issue tracks the work.
- Open questions: none.

### DT-5. The attribute-adding helper of code actions returns `null` for unions, and already does so for records and extension blocks

- Where:
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/Refactoring/CSharpAttributeHelper.cs:74-191` (the switch)
    and `:189-190` (the default arm that returns `null`), with `:33-38` propagating that result out of the
    asynchronous entry point
  - In the `Metalama.Premium` repository,
    `src/Metalama.Extensions.CodeFixes.DesignTime/AddAspectAttributeCodeActionModel.cs:94-99` (the call to the helper
    and the null check that follows, with the empty result returned at `:98`, and the two preceding failure branches
    at `:70-75` and `:81-86`, which do log a warning)
  - In the `Metalama.Premium` repository, `src/Metalama.Extensions.CodeFixes.DesignTime/CodeFixService.cs:200-253`
    (the refactoring is offered for any declared symbol, with no filter on the syntax kind)
  - `Metalama.Framework/src/Metalama.Framework.Engine/DesignTime/CodeFixes/CodeActionResult.cs:26`, `:49` and
    `:51-90` (the empty result is a successful result that applies no change)
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/DesignTime/CSharpAttributeHelperTests.cs` (the
    covered kinds, and the trivia tests at `:658-760`)
  - `Metalama.Framework/src/Metalama.Framework.Engine/SyntaxGeneration/ContextualSyntaxGenerator.cs:780-816` (a
    parallel switch over the same kinds that throws instead of returning `null`, serving the other code fix path)
- What happens today: the helper switches on the syntax kind and casts to the concrete syntax type for the method,
  the destructor, the constructor, the interface, the delegate, the enum, the class, the struct, the parameter, the
  property, the event, the four accessors, the operator, the conversion operator, the indexer, the field, the event
  field and the compilation unit. The record, the record struct, the extension block and the union fall to the
  default arm, which returns `null`, and that result is propagated out of the asynchronous entry point. The Premium
  code action that computes the "add aspect" refactoring offers the item for any declared symbol without filtering
  the syntax kind, so it is offered on a record. When the helper returns `null` the code action returns the empty
  result without logging a warning, unlike the two preceding failure branches. The empty result is a successful
  result carrying no change, so applying it returns the solution unchanged and the user sees neither a modification
  nor a message. The unit tests cover the method, the class, the interface, the delegate, the enum, the property
  accessor, the event, the field, the variable declarator, the parameter and the assembly target; none of them covers
  a record, an extension block or a union.
- Consequence: silent wrong output. The code action is offered, reports success and does nothing.
- Proposed change: replace the per-kind cases for the type and member declarations by a call to the
  `AddAttributeLists` method of the abstract member declaration base type, which the Roslyn generator emits for every
  abstract node that has a list field and which covers records, extension blocks and unions without naming a syntax
  kind that does not exist in the Roslyn 5.0 variant. Keep the special cases for the parameter, the accessors and the
  compilation unit, none of which derives from that base type. Narrow the fallback so that the behaviour is not
  widened: a test on the member declaration base type alone would also match namespace declarations, file-scoped
  namespace declarations, enum members, global statements and incomplete members, which return `null` today and for
  which an added attribute list would be invalid code, whereas a test on the base type declaration for the type
  declarations, keeping the existing cases for the other members, covers every type declaration kind including the
  union and adds nothing else. The helper is a public type, so the set of callers cannot be assumed to be limited to
  the Premium code action. Consider the same treatment for the parallel switch in the contextual syntax generator,
  which throws on the same kinds and serves the other code fix path. Add unit tests for a record and a record struct;
  the record and extension-block half of the work is independent of C# 15 and can be delivered today. A union test
  must be guarded by the variant constant, because the same sources are compiled by the Roslyn 5.0.0 variant of the
  unit test project, must set parse options with the preview language version, because the consumed Roslyn has no
  C# 15 language version, and must suppress the experimental diagnostic that marks the union syntax API. Whether an
  attribute on an extension block is accepted by the binder was not verified, so an extension block test carries that
  assumption; the parser does accept attributes on the node.
- Size: small.
- Status: new work. The file has not been modified in the 2027.0 line, no open pull request touches it, and no issue
  scopes the change. The related issues are the open #735, about the placement of the attribute added by the same
  helper in Visual Studio Code, which is adjacent work in the same file rather than a duplicate; the closed #779,
  whose trivia-preserving tests the proposed refactoring must not regress, because the helper restores the leading
  trivia of the old node at `:193`; and the closed #692, which added the delegate case to this switch and is the
  precedent showing that unsupported declaration kinds have been fixed one kind at a time, which is the reason to
  replace the enumeration rather than add three more cases.
- Verification: the code pass confirmed the switch, the default arm, the propagation, the single production consumer,
  the successful-but-empty result and the absence of a record, extension block or union test, and it corrected the
  described test coverage, one line number, the proposed fallback base type and the cost of the proposed union test.
  The semantics pass did not run, because the finding rests on no external premise. The scope pass confirmed that the
  change is not implemented, not in progress and not tracked, and that only the union test, not the record and
  extension-block half, depends on the Roslyn variant.
- Open questions: none.

### DT-6. Design-time suppressions are not applied to diagnostics located on a union header

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxExtensions.cs:19-46`
    (`FindMemberDeclaration` and the private `FindMemberDeclarationOrNull`, whose kind list is at `:29-36`), `:51-75`
    (`FindSymbolDeclaringNode`, whose kind list is at `:57-64`) and `:113-120` (`GetDeclaringType`)
  - `Metalama.Framework/src/Metalama.Framework.DesignTime/DiagnosticSuppressing/TheDiagnosticSuppressor.cs:190-197`
    (the node lookup and the silent skip when nothing is found) and `:202-215` (the upward walk that matches the
    suppression)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Diagnostics/ScopedSuppression.cs:60-72` (the compile-time
    suppression path, which uses the same helper)
  - The other consumers of the same helpers:
    `Metalama.Framework/src/Metalama.Framework.Engine/CodeModel/Helpers/CodeModelExtensions.cs:72` and `:87` (the
    insert position) and `Metalama.Framework/src/Metalama.Framework.Engine/Linking/LexicalScopeFactory.cs:121` and
    `:186`
  - `Metalama.Framework/src/Metalama.Framework.Engine.Analyzers/KindCheckOptimizationAnalyzer.cs:25-31` and `:836-840`
    (the analyzer exempts abstract syntax classes, so a type test against an abstract base is permitted)
  - `Metalama.Framework/src/Metalama.Framework.Engine.5.0.0/Metalama.Framework.Engine.5.0.0.csproj:5-8` (the same
    sources are compiled against Roslyn 5.0.0, whose syntax kind enumeration has no union member)
  - `Metalama.Framework/src/tests/Metalama.Framework.Tests.UnitTests/DesignTime/EndToEnd/DiagnosticSuppressorTests.cs:190-220`
    (the existing outer-scope suppression test)
- What happens today: the suppressor locates the declaring member of each reported diagnostic with
  `FindSymbolDeclaringNode`, whose kind list names class, struct, interface, record, record struct, enum and delegate
  but not union, and not extension block either. A union declaration carries its identifier, its modifiers and its
  case types on the union node itself, because the case types are parsed into the parameter list of that node, so a
  diagnostic located on any of them has no member declaration ancestor below the union node and the upward walk
  passes over it. When the union is declared inside a namespace declaration the walk stops at the namespace and the
  matching loop then resolves symbols from the namespace upwards; when the union is nested in a type the walk stops
  at the enclosing type; when the union is declared at file scope with no namespace declaration the walk returns
  nothing and the diagnostic is skipped. In all three cases a suppression registered on the union type is never
  matched, and the failure is silent, because no assertion and no diagnostic is produced and the serializable
  identifier of a namespace symbol is computed without error. Diagnostics located inside a member of the union still
  match, because the member is found and the walk up from it reaches the union node, on which the general symbol
  lookup succeeds through the base type declaration case. The same helper serves the compile-time scoped suppression
  matcher, so the gap is not confined to design time. `FindMemberDeclaration` and `GetDeclaringType` have the same
  omission, and their failure mode is not silent: the first throws when the walk finds nothing, and both call sites of
  the second assert that the result is not null.
- Consequence: a diagnostic reported that should have been suppressed. The suppression itself fails silently.
- Proposed change: replace the enumerated kinds by type tests, that is a member declaration test in the two
  node-finding helpers and a type declaration test in `GetDeclaringType`, whose declared return type is the type
  declaration and therefore cannot be based on the wider base type declaration. Adding the union kind to the lists is
  not an option, because the Roslyn 5.0.0 variant compiles the same sources against a syntax kind enumeration that has
  no union member and production source carries no conditional compilation for Roslyn versions. The type tests
  compile in both variants and are not reported by the kind-check analyzer, which exempts abstract syntax classes.
  The type tests are not exactly equivalent to the present lists: the member declaration base type also matches enum
  members and extension blocks, and the type declaration base type also matches extension blocks, so the effect on
  the insert-position helper and on the lexical scope factory has to be reviewed, and the change is best accompanied
  by a review of the current enum member behaviour. Add a suppression test on a union in the design-time suppression
  test suite, beside the existing outer-scope test, and one in the compile-time suppression tests; those tests depend
  on the ability of a test to request C# 15, which is DT-7, while the production change does not.
- Size: small.
- Status: new work. The three helpers are unchanged on the working branch, no open pull request touches them, and two
  issue searches returned no issue that scopes the change; the suppression issues that exist concern other subjects.
  No related issue is recorded for this finding.
- Verification: the code pass confirmed the three kind lists, the upward walk, the silent skip, the shape of the
  union header, the compile-time consumer and the two non-silent consumers, and it corrected a method name, several
  line ranges, the description of the three walk outcomes and one of the two implementation options, which would not
  have compiled. The semantics pass did not run, because the finding rests on no external premise beyond the shape of
  the union header, which the code pass verified against the grammar file. The scope pass confirmed that the change is
  not implemented, not in progress and not tracked, and identified the second consumer in the compile-time scoped
  suppressions, which widens the finding beyond its title.
- Open questions: none.

### DT-7. C# 15 cannot be requested by a test today, and a test that requests it is skipped without any failure

- Where:
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestOptions.cs:53` (a skip reason marks the test skipped),
    `:681-700` (the language version directive, whose failure branch sets the skip reason), `:702-721` (the
    dependency language version directive), `:545-548` (the include directive) and `:857-871` (the options mapped to
    the test context)
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/XunitFramework/TestExecutor.cs:309-311` (the skip is
    reported to the test framework with its reason) and
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/XunitFramework/TestDiscoverer.cs:139` (a leading underscore
    excludes a file from discovery)
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/BaseTestRunner.cs:218-223` (the parse options of the main
    project), `:297-306` (an included document is added with the parse options of the test), `:418-423` (the
    dependency project) and `:943-946` (the failure when the HTML writer is absent)
  - `Metalama.Framework/src/Metalama.Testing.UnitTesting/TestLanguageVersionProvider.cs:12` and
    `Metalama.Framework/src/Metalama.Testing.UnitTesting/TestContext.CreateRoslynCompilation.cs:155-156` (unit tests
    receive the default parse options with no way to pass a version), and
    `Metalama.Framework/src/Metalama.Testing.UnitTesting/TestContextOptions.cs:167`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:31-32`, `:38-43`, `:50`,
    `:52-62` and `:149-159`, and
    `Metalama.Framework/src/Metalama.Framework.Engine/Options/DefaultProjectOptions.cs:77`
  - `Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/CompileTime/CompileTimeAspectPipeline.cs:70-80` and
    `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeCompilationBuilder.cs:279`
  - `Metalama.Framework/Directory.Build.props:45-46` (the repository sets the maximum language version to 14.0 for
    every project) and
    `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Metalama.Framework.Tests.AspectTests.csproj:14`
    (the test project multi-targets `net48` and `net10.0`), `:22` (the language version override hook), `:53-55` (the
    exclusion of companion files) and `:66-70` (the HTML writer constant)
  - `eng/RoslynVersions/Roslyn.5.10.0.props:8-10` (the sole definition of the latest-variant constant) and
    `Metalama.Testing.AspectTesting.targets:58-61` (the constants are exported as assembly metadata)
  - The pinned baselines
    `Metalama.Framework/src/tests/Metalama.Framework.Tests.AspectTests/Tests/Aspects/LanguageVersion/LanguageVersionPreview.cs:6`
    and `LanguageVersionPreview.t.cs:2`, and the two existing uses of the variant constant at
    `Tests/Aspects/DesignTimeInvalidCode/UnknownAccessorInTemplate.cs:7` and its Roslyn 5.0 counterpart
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/DesignTimeTestRunner.cs:47-67` and
    `Metalama.Framework/src/Metalama.Testing.AspectTesting/TestContextExtensions.cs:19`
- What happens today: the language version directive is parsed with the Roslyn parsing helper; when that fails for an
  integral value of 10 or more, the test is marked skipped with the reason that the version is not recognized by the
  current version of Roslyn, rather than failed, and the skip is reported to the test framework with that reason, so
  the run reports success. The absence of the C# 15 language version from the consumed build is established directly
  rather than by dating: the stable Roslyn 5.9.0 assemblies declare the enumeration member for C# 14 and declare no
  member for C# 15, the only occurrence of the name `CSharp15` in them being the name of a test assembly in an
  `InternalsVisibleTo` attribute, and the consumed build exposes the same public API. Consequently a test that requests C# 15 is skipped on
  both variants and the suite reports success with zero C# 15 tests executed. A test that requests the preview version
  parses, but the default scenario runs the compile-time pipeline, which rejects preview unless the preview opt-in is
  set, and neither the test options nor the test project options expose that option; that behaviour is pinned by an
  existing test with a committed baseline. Independently of the directive, the compile-time project of every test is
  compiled at the latest supported version, which is C# 14, so a template using C# 15 syntax cannot compile; the
  separate template language version property does not help, because it only lowers the declared version and is not
  mapped from an aspect test. Unit tests receive the default parse options with no way to pass a version. Every aspect
  test file is also compiled into the test project itself at the project language version, which the repository sets
  to 14.0 for every project rather than inheriting it from the .NET SDK, unless the file name starts with two
  underscores, in which case it is excluded from compilation, excluded from discovery and pulled in by an include
  directive of another test, which adds it with the parse options of that test. The required-constant and
  forbidden-constant directives work as documented, and the latest-variant constant is defined by one properties file
  and used by exactly two aspect tests.
- Consequence: the suite reports success with no C# 15 test executed. The loss is not entirely invisible, because the
  skip is recorded with its reason and appears in the skipped count; only the exit code is unaffected. Nothing throws
  and nothing asserts.
- Proposed change, as the plan for C# 15 tests.
  1. Gate: consume the stable Roslyn 5.12 first. Until then the string `15.0` cannot be requested on any variant. The
     latest-variant constant is written literally in its properties file and keeps its name through the renumbering.
  2. Engine: add the C# 15 constant as a numeric cast, which compiles in both variants, and add the corresponding
     cases to the maximum-version mapping and to the Roslyn-to-language-version mapping. Do not raise the latest
     supported version and the supported-versions set unconditionally: that file is compiled into both variant
     assemblies, and the Roslyn 5.0.0 variant that serves Rider and the C# Dev Kit would then build its compile-time
     compilation at a language version its Roslyn does not know. Make the latest version variant-aware by deriving it
     from the current Roslyn API version, and make the set the versions up to that value; the default parse options
     and the test language version provider then follow with no further change. Verify every comparison against C# 14
     that this moves, in particular the compile-time compilation constant and the .NET SDK mapping, which needs a case
     for major 11.
  3. Aspect tests: write each C# 15 test with the language version directive and, where the expected output differs
     between variants, with the required-constant directive naming the latest variant, so that the lower variant skips
     it with a named reason. Because every test file is also compiled into the test project, either set the language
     version override property for the aspect test project or put the C# 15 source in a companion file included with
     the include directive. The first route depends on the .NET SDK rather than on the Roslyn variant, because the
     aspect test project sets no property that injects `Metalama.Compiler` and the aspect testing properties disable
     Metalama for it, so it is compiled by the compiler of the pinned SDK, which rejects the value 15.0 with CS1617
     until the .NET 11 SDK is pinned. The companion route has no such dependency and is therefore the safer of the
     two. The design-time scenario runner produces the generated documents from the parse options of the first source
     tree, so union partial parts are testable there; commit the expected transformed and generated files after
     reading them.
  4. Guard against silent skipping: add a unit test that asserts that the string `15.0` parses and that the parsed
     value equals the latest supported version, so that a future Roslyn move that loses the version fails visibly. Do
     not key that test on a Roslyn assembly version of 5.10 or above: official Roslyn builds carry an assembly version
     of the form major.minor.0.0, so the consumed prerelease already reports 5.10.0.0 while lacking the value, and
     such a guard would fail today. Key it on the variant constant, or assert it unconditionally once step 1 is done.
  5. Unit tests: add a language version parameter to the test compilation factory, or a language version option next
     to the existing template language version option.
  6. Preview: add a preview opt-in directive mapped to the corresponding test project option only if tests must run
     against a preview feature. On Roslyn 5.12 the six C# 15 features are gated on C# 15 and only the unsafe
     expression stays at preview, so C# 15 itself does not need the option after step 1; before step 1, the option is
     the only way to reach any C# 15 syntax at all. Leave the existing preview baseline as it is.
  7. HTML: the input and output HTML directives need the HTML writer plug-in and fail with a named message when it is
     absent; the two directories that use them gate themselves with the corresponding required constant, which is
     defined for the latest variant only, and the HTML writer contains no syntax kind switch, so no change is needed
     there.
- Size: medium for the framework changes, plus the tests themselves. The variant-aware latest version of step 2 is
  the part whose effect reaches beyond the test framework, because every consumer of that value and of the default
  parse options then differs between the two variant assemblies. The gate of step 1 is a separate and larger piece of
  work that also depends on `Metalama.Compiler`.
- Status: new work, gated by the move to the stable Roslyn. Nothing of the plan is implemented, no open pull request
  touches the harness, and four issue searches returned no issue about C# 15 in tests and no C# 15 umbrella issue at
  all. The related issues are #1896, which raised the template language version to C# 14 and recorded the rule that
  the pin cannot exceed what the lower Roslyn variant supports; #1881, which added the latest variant and its
  constant; the open #985; and #1898, which is the model for the named-skip behaviour that step 3 asks for.
- Verification: the code pass confirmed the parsing and skipping path, the parse options, the preview rejection, the
  compile-time language version of tests, the compilation of test files into the project and the constant mechanism,
  it replaced the dating argument by a direct reading of the shipped assemblies, and it corrected three defects in
  the plan, namely the unconditional raise that would break the lower variant, the version number and the claim that
  the language version override is valid on both variants. The semantics pass confirmed the date decoding of the
  consumed build, the Roslyn commit that added the C# 15 language version, the absence of that value from the window
  that contains the consumed build, the frozen assembly version scheme that defeats the proposed guard, and the
  grammar rule that permits a partial union, and it corrected the target release from 5.10 to 5.12. The scope pass
  confirmed that the silent-skip path, the C# 14 language version of unit tests and the disabled preview option are
  all present as described and that the required-constant machinery the plan relies on already exists.
- Open questions: the open question of the original report, whether the stable Roslyn maps the default language
  version to C# 15, is answered affirmatively for `dotnet/roslyn` main. It does not by itself decide what a test
  without a language version directive sees, because the default parse options apply the Metalama latest-version
  constant rather than the Roslyn latest value; that is decided by step 2.

### DT-8. Workspaces, AspectTesting and the LinqPad driver reference a Roslyn version that nuget.org does not serve

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Workspaces/Metalama.Framework.Workspaces.csproj:51-54` (four public
    Roslyn dependencies at the maximum version), `:79` (the private MSBuild workspaces dependency) and `:100-113`
    (its repacked assets)
  - `Metalama.Framework/src/Metalama.Testing.AspectTesting/Metalama.Testing.AspectTesting.csproj:17-18` and
    `Metalama.Framework/src/Metalama.Testing.AspectTesting.5.0.0/Metalama.Testing.AspectTesting.5.0.0.csproj:3` (the
    lower variant is not packable, so the only packed variant carries the prerelease dependency)
  - `Metalama.Framework/src/Metalama.Testing.UnitTesting/Metalama.Testing.UnitTesting.csproj:33` (a public project
    reference that carries the prerelease Roslyn dependency transitively)
  - `Metalama.LinqPad/src/Metalama.LinqPad/Metalama.LinqPad.csproj:23` and `:34-37` (the package reference and the
    assembly metadata that records the MSBuild workspaces package version), and
    `Metalama.LinqPad/src/tests/Metalama.LinqPad.Tests/Metalama.LinqPad.Tests.csproj:29`
  - `Directory.Packages.props:25-30` (the two version properties and the comment naming the private feed) and
    `nuget.base.config:4-19` (the private feed and the package source mapping)
  - `eng/RoslynVersions/Latest.props:2` and `eng/RoslynVersions/Roslyn.5.10.0.props:3` (the latest variant resolves to
    the maximum API version), and `eng/src/Program.cs:63` and `:117-132` (the generated configuration and the public
    artifacts)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/SupportedCSharpVersions.cs:77-132` (the version string
    of the consumed build, from which the prerelease package source is derived)
  - `Metalama.Framework/src/Metalama.Framework.Engine/Metalama.Framework.Engine.csproj:53-55` and
    `Metalama.Framework/src/Metalama.Framework.Implementation.Package/Metalama.Framework.Implementation.Package.csproj:46-48`
    (the same references, owned by the packaging theme)
  - [`updating-roslyn.md`](../updating-roslyn.md):19-31 and `:38-52`, and
    [`platform-support.md`](../platform-support.md):274-276
- What happens today: the four principal Roslyn packages are public dependencies of the Workspaces package at the
  maximum Roslyn version, which is the prerelease `5.10.0-1.26365.3`; the MSBuild workspaces package is private and
  repacked. The latest AspectTesting variant depends on two of them at the same version, and the unit testing package
  carries the same dependency transitively through a public project reference. The LinqPad driver records the MSBuild
  workspaces package version as assembly metadata and also references the Workspaces package. The nuget.org flat
  container index of each of these packages, fetched on 2026-09-04, lists the versions 5.0.0-2.final, 5.0.0,
  5.3.0-2.final, 5.3.0, 5.6.0 and 5.9.0, and no 5.10, 5.11 or 5.12 version of any kind; the repository configuration
  states that the prerelease is served by a private feed and maps every Roslyn package to it. A consumer restoring any
  of these packages from nuget.org therefore gets NU1102, because the package identifier exists there but no published
  version satisfies the minimum. The failure is invisible inside the repository, because the private feed and the
  package source mapping are declared in the generated configuration that the repository build consumes. One partial
  mitigation exists and does not apply here: the prerelease package source is declared in the configuration generated
  for the reference project restored on a user machine, not in the dependency graph of the published packages.
- Consequence: a restore error for every external consumer who restores from nuget.org alone. It affects
  `Metalama.Framework.Workspaces`, `Metalama.Testing.AspectTesting`, `Metalama.Testing.UnitTesting`, the
  implementation package of the latest variant and, transitively, `Metalama.LinqPad`. The main user package
  `Metalama.Framework` is not affected, because it embeds the engine through the compiler extensions rather than
  referencing the implementation package.
- Proposed change: treat the move off the prerelease Roslyn as a release gate for 2027.0 and for any preview
  published on nuget.org. The move is not the edit of two properties: the procedure document makes it a renumbering of
  the latest variant onto the stable version, which renames the variant properties file, changes the variant-derived
  properties, and updates the resource extractor, the Roslyn API version enumeration, the supported-versions table
  including the version string from which the prerelease package source is derived, the serialization binder, the
  compiler extensions resources project and the entries derived from the variant name. Name the target version 5.12.0
  rather than 5.10.0: no stable 5.10 or 5.11 exists or is expected, because Roslyn publishes a stable every third
  minor version in step with the quarterly Visual Studio releases and `dotnet/roslyn` main is already at 5.12. Record
  that the prerequisite is `Metalama.Compiler` moving to that Roslyn version, and that this was not verified, because
  `Metalama.Compiler` is not cloned. Independently, add a build check that fails the pack or the publish of a public
  artifact when either version property carries a prerelease label; the prerelease condition is already a single
  textual test in the supported-versions table, so the check is cheap to express, and the story must say whether it
  lives in the build orchestration or in a target, because the second option also has to cover `Metalama.Premium`,
  whose variant properties file does not define the same property.
- Size: small for the version properties alone; medium for the variant renumbering that the procedure document
  requires. The date is set by the stable Roslyn expected in November 2026 with Visual Studio 2027 and the .NET 11
  SDK; that date is an inference from the quarterly release cadence and from the version numbers on `dotnet/roslyn`
  main, and not a published commitment by Microsoft.
- Status: decision required. The decision is whether to renumber the latest variant now onto the stable 5.9.0, which
  exposes the same public API as the consumed prerelease and is available today, or to wait for the expected stable
  5.12.0. The first option removes the restore failure immediately and loses no API that the code uses, but provides
  nothing for C# 15; the second is the target of PB-2027.0 but has an unverified date and an external prerequisite.
  Neither the gate nor the build check exists today, and no issue tracks either. The related issues are #1106, in
  which the same failure class already reached a user through a published preview pinned to a Roslyn version that
  nuget.org did not serve; #1885, the nearest prior art, which declared the private source only in the configuration
  generated on the user machine and therefore does not affect the published dependency graph; #1881, which introduced
  the prerelease pin; #1747, about the package source mapping that hides the problem inside the repository; the open
  meta-issue #1921, under which a story belongs; and #1913, because the Premium alignment mirrors the same pin into
  the second repository, so the gate has to cover both.
- Verification: the code pass confirmed every reference, the private feed, the public artifact list and the absence
  of any equivalent check in this repository, and it corrected the set of affected packages in both directions, adding
  the unit testing package and removing the main user package. The semantics pass re-fetched the nuget.org indexes of all five
  packages, confirmed the restore failure, refuted the target version 5.10 and identified the stable 5.9.0 fallback.
  The scope pass confirmed that the pin is still the prerelease, that the documentation states the obligation without
  making it a gate, that the pre-release verification checklist does not contain it, and that no pull request or issue
  covers it.
- Open questions: what LINQPad does with the package assembly metadata. The attribute predates the public history of
  the repository and the LINQPad website is unreachable from this session, so if LINQPad restores the named package a
  prerelease version fails in the same way. The question is not load-bearing, because the package dependencies fail on
  nuget.org regardless.

### DT-9. A .NET 10 host of Workspaces or LinqPad cannot use a machine that has only the .NET 11 SDK

- Where:
  - `Metalama.Framework/src/Metalama.Framework.Workspaces/MSBuildInitializer.cs:83-87` (the major-version filter and
    the selection of the highest matching SDK) and `:89-95` (the exception, including the architecture flag at `:94`)
  - `Metalama.Framework/src/Metalama.Framework.Workspaces/MSBuildInitializationException.cs:24-28` (the documented
    meaning of that flag)
  - `Metalama.Framework/src/Metalama.Framework.Workspaces/Workspace.cs:226` and `:280-283` (the initializer runs
    before the workspace is created), and
    `Metalama.Framework/src/Metalama.Framework.Workspaces/WorkspaceCollection.cs:117-118` with
    `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Threading/TaskRunner.cs:50-58` (the exception reaches
    the caller wrapped in an aggregate exception)
  - `Metalama.LinqPad/src/Metalama.LinqPad/MetalamaWorkspaceDataContext.cs:29-50` (the consumer that replaces the
    accurate message, with the substitution at `:38-45`)
  - `Metalama.Framework/src/tests/Metalama.DesignTime.HostSimulator/MSBuildEnvironment.cs:47-59` (a second copy of the
    same filter, with an accurate message)
  - `Metalama.Framework/src/Metalama.Framework.Workspaces/Metalama.Framework.Workspaces.csproj:5-8` and `:18`, and
    `Metalama.LinqPad/src/Metalama.LinqPad/Metalama.LinqPad.csproj:6`, and
    `Metalama.LinqPad/src/Metalama.LinqPad/linqpad-samples/CLAUDE.md:3`
  - `Directory.Packages.props:35-50` and [`Directory.Packages.md`](../../../Directory.Packages.md):63-69 (the MSBuild
    pin and its rationale), and [`platform-support.md`](../platform-support.md):195-197
  - `dotnet/roslyn` main, `src/Workspaces/MSBuild/Core/Microsoft.CodeAnalysis.Workspaces.MSBuild.csproj` (the library
    assets are the current .NET target and `net472`) and
    `src/Workspaces/MSBuild/BuildHost/Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost.csproj` with
    `eng/targets/TargetFrameworks.props` (the build host targets `net8.0` and `net472`); `dotnet/msbuild` main,
    `eng/Versions.props` (the version prefix is 18.12.0 while the assembly version is pinned at 15.1.0.0)
- What happens today: the initializer lists the SDKs with the .NET command line interface and keeps only those whose
  major version is at most the major version of the host runtime, then registers the highest one whose version file
  names the current runtime identifier. The filter is correct for the in-process registration, because the MSBuild
  assemblies of an SDK target the runtime of that SDK and cannot load into an older runtime; the assets of the
  consumed packages bind to the .NET 10 runtime. The Workspaces package targets .NET 10 only and the LinqPad driver
  targets .NET 10 for Windows. Under PB-2027.0 the .NET 11 SDK is in the supported set, and a developer who installs
  Visual Studio 2027 alone may have the .NET 11 SDK and not the .NET 10 SDK, which is plausible but unverifiable
  while Visual Studio 2027 is unreleased. The scenario also requires the .NET 10 runtime to be installed, because the
  default roll-forward policy does not cross a major version and a .NET 10 host would otherwise not start at all. On
  such a machine the initializer selects nothing and throws its own exception before any project is opened. The
  message names the cause and the remedy, but a LINQPad user does not see it: the architecture flag is set whenever at
  least one SDK was parsed, including one rejected only by the major-version filter, although the exception documents
  that flag as meaning that an SDK was found for another processor architecture, and the LinqPad data context replaces
  the accurate message with a processor-architecture message that does not apply and directs the user to a remedy that
  cannot work. The failure belongs to Metalama alone: the Roslyn build host that would load the projects is a separate
  process targeting `net8.0` and `net472` whose runtime configuration rolls forward across major versions, so Roslyn
  itself would select a compatible SDK. A .NET 11 host of the same asset selects the .NET 11 SDK and loads its MSBuild
  against the compile-time MSBuild reference, which works because MSBuild freezes its binding identity at 15.1.0.0
  across its 18.x line; the pin must not rise, because the .NET 10 SDK remains the lowest supported host.
- Consequence: a deliberate exception of a public type at workspace load, not an assertion and not an unhandled crash
  in an unexpected place. In the Workspaces package the message names the cause and the remedy. In LINQPad the message
  is replaced by an incorrect processor-architecture diagnostic, so the user is misdirected. The environment is
  plausible but unverified.
- Proposed change: correct the architecture flag so that it reports what its documentation says, for example by
  computing it from the SDKs that pass the major-version filter and fail only the architecture test rather than from
  the count of parsed SDKs. That restores the accurate message in LINQPad and is a one-line change in a file compiled
  once for every Roslyn variant, so it raises no variant question. Then document in the Workspaces and LinqPad
  readme files, which today carry no prerequisites section, that the host runtime major version decides which SDK the
  in-process MSBuild registration can use and that a .NET 10 host needs a .NET 10 SDK. When recording this, name the
  Roslyn variant rather than a version number, because the latest variant is expected to be renumbered. Consider a
  .NET 11 target for the LinqPad driver only if a LINQPad release hosts .NET 11, which the available sources do not
  establish. No change is proposed for the filter itself, which encodes a real MSBuild constraint, and none for the
  second copy in the host simulator, whose message is already accurate. A separate question, outside this finding, is
  whether the in-process registration is still required at all, given that Roslyn performs the project evaluation in a
  build host process that selects its own SDK.
- Size: small.
- Status: new work. Neither readme mentions the host runtime or the .NET SDK, the nearest existing statement is a
  comment in the Workspaces project file addressed to a maintainer, no open pull request touches either project, and
  two issue searches returned nothing that scopes the work. The related issues are the open meta-issue #1921; #1881,
  whose pull request moved both hosts to .NET 10 because the consumed Roslyn ships no lower asset; #1884, which
  introduced the platform requirement item for user projects and which the story must distinguish, because that
  mechanism constrains a project that references `Metalama.Framework` and not the SDK that the host process itself
  needs; #1887, which proposed exactly a platform requirement declaration for the LinqPad package and was closed as
  not planned, which is a further reason the remedy is documentation; and the open #1217, which already records that
  samples may fail because of a Roslyn version mismatch with LINQPad.
- Verification: the code pass confirmed the filter, the exception, the two hosts and the absence of any test pinning
  the behaviour, and it found the misreporting architecture flag, the LinqPad substitution of the message and the
  second copy of the filter in the host simulator, and it made explicit the requirement that the .NET 10 runtime be
  installed. The semantics pass confirmed the package layout on `dotnet/roslyn` main and in the shipped 5.9.0 package,
  corrected the target frameworks of the build host and answered the open question about roll-forward affirmatively,
  and confirmed the frozen MSBuild assembly version across 18.0.2, 18.9.6 and main. The scope pass confirmed that no
  readme, no baseline document and no package documentation states the requirement, and that no pull request or issue
  covers it.
- Open questions: the runtime of LINQPad 9 and of any later LINQPad release. The LINQPad website and its forum are
  blocked by the egress proxy, so the only evidence available is a web search summary reporting that LINQPad 9 starts
  on .NET 8 or later and that queries may target .NET 6 through .NET 10; that is weak evidence. The default SDK set of
  a machine carrying Visual Studio 2027 alone is likewise unverifiable while that product is unreleased.

## Withdrawn findings

No finding of the original report was withdrawn. All nine findings survived the verification passes, and none was
refuted. Six of them changed materially and are recorded above rather than withdrawn, because their central claim
held while a supporting statement did not.

DT-1 claimed that the code fix defect follows from the union kind; the code pass established that the code fix never
inspects the existing modifier list at all, so the duplicate-modifier defect is independent of unions and is worth
correcting on its own account, and that the same generator runs during a build with a discarded diagnostic sink, so
the consequence is not purely a design-time one. DT-2 proposed the Roslyn union predicate as the discriminator for
the generated part; both passes refuted that, because the predicate is also true for a hand-written class or struct
carrying the union attribute, and emitting a union part for such a type would produce the same compiler error in the
opposite direction. DT-3, DT-7 and DT-8 all named a stable Roslyn 5.10 as the release that removes the constraint;
that release does not exist and is not expected, and the target is 5.12, which changes the version numbers of three
proposals without changing their mechanism. DT-4 claimed that a C# 15 project is processed at C# 14 at design time
while the build reports an error; the code pass established that an implicitly set C# 15 language version is lowered
to C# 12 by the package targets at both design time and build, that the diagnostic in question cannot be raised with
the Roslyn versions consumed today, and that the case genuinely silent at design time is the preview language
version. DT-9 claimed that the thrown message already names the cause; the code pass established that this is true of
the Workspaces path and false of the LinqPad path, which converts the change from documentation alone into
documentation plus one line of code.

Two proposals were also refuted in detail without removing the finding that carried them. The fallback proposed by
DT-5, a type test against the member declaration base type, is not behaviour-preserving, because it also matches
namespace declarations, enum members, global statements and incomplete members. The guard proposed by DT-7, keyed on
a Roslyn assembly version of 5.10 or above, would fail today, because official Roslyn builds carry an assembly version
of the form major.minor.0.0 and the consumed prerelease already reports 5.10.0.0 while lacking the language version.

## Non-findings

The following were checked in the original report and found unaffected. They were not re-checked by the verification
passes unless a finding depends on them, so they are recorded as read on 2026-09-03.

- Analyzer shim and variant selection. `RoslynVariantPolicy.TryGetVariantName`
  (`Metalama.Framework/src/Metalama.Framework.CompilerExtensions/RoslynVariantPolicy.cs:30-54`) returns the latest
  variant for every host at Roslyn 5.10 or above, so a Visual Studio 2027 update presenting Roslyn 5.11 or 6.0 is
  served by that variant, and `RoslynVariantPolicyTests.cs:36-45` pins 5.10, 5.10.0, 5.11.0 and 6.0.0. The host
  version is read from the assembly version of the Roslyn assembly, with a special case for the JetBrains build
  (`ResourceExtractor.cs:633-656`), and the assembly resolution accepts a same-or-higher version for non-embedded
  assemblies (`AssemblyResolutionPolicy.cs:61-80`). A host below Roslyn 5.0 gets no variant and a report file
  (`ResourceExtractor.cs:157-211`), and the compiler entry point reports `LAMA0087`
  (`MetalamaSourceTransformer.cs:23-31` and `:48-63`); every other entry point routes through the same factory and
  holds no version literal. The embedded resource lists (`Metalama.Framework.CompilerExtensions.csproj:53-70`) name
  the `net472` and `net10.0` outputs, and [`platform-support.md`](../platform-support.md):141-160 records that the
  Visual Studio private runtime is .NET 10 on that Roslyn branch, so no flavour changes. One residual risk remains:
  the latest variant is compiled against the prerelease and would load into the stable Roslyn of Visual Studio 2027
  with no check other than the major and minor version, so an API that changed between the two would surface as a load
  or missing-member exception written to the crash-report directory (`ResourceExtractor.cs:259-307`) and as nothing in
  the editor. DT-8 closes that risk by consuming a stable build before release.
- Remote procedure call and cross-version contracts. The only kind on the wire is the aspect explorer declaration
  kind, with two values
  (`Metalama.Framework/src/Metalama.Framework.DesignTime.Contracts/AspectExplorer/AspectExplorerAspectInstance.cs:60-66`).
  No file under the remote procedure call, the contracts or the Visual Studio design-time directories mentions a type
  kind, a syntax kind or a language version. The project key carries the assembly name, a preprocessor symbol hash and
  a Metalama flag (`ProjectKey.cs:21-25`), declarations cross the pipe as serializable identifiers
  (`SerializableDeclarationIdProvider.ToSymbol.cs:21-39`), for which a union is an ordinary named type, and the
  message pack options constrain typeless deserialization to the contract types and the symbol key
  (`RpcContractMessagePackOptions.cs:37-78`). Nothing there depends on the language version, and no C# 15 feature
  crosses a Metalama version boundary.
- Preview and code action transport. The serializable syntax tree carries text and annotations, and the conversion
  back re-parses with the options of the caller
  (`Metalama.Framework/src/Metalama.Framework.Engine/DesignTime/JsonSerializationHelper.cs:41-49`, used by
  `PreviewTestRunner.cs:83` with the original tree options), while the node conversion uses the default parse options
  (`:52`) when a code action is applied in the user process (`CodeActionResult.cs:75`). With a stable Roslyn whose
  default is C# 15, C# 15 text parses; under the Roslyn 5.0 variant a project cannot be at C# 15 in the first place.
  The omission of the preprocessor symbols of the project in that call is pre-existing and unrelated to this release.
- Code lens and refactorings. The code lens service reasons on symbols and prints the symbol kind
  (`CodeLensServiceImpl.cs:189`), the code refactoring provider resolves the declared symbol of whatever node is under
  the caret (`TheCodeRefactoringProvider.cs:104-132`), and the live template test runner locates its target by
  attribute (`LiveTemplateTestRunner.cs:51-56`). None of them switches on a declaration kind.
- The design-time diff for collection expression arguments. The with-element node is stripped from the grammar and
  therefore has no hasher method, but the default visit still descends into its argument list, whose arguments are
  hashed; only the `with` keyword token is lost, and that token cannot change without the argument list changing
  shape.
- Dependency collection. The dependency collector tests the base type declaration and the `partial` token
  (`DependencyCollector.cs:63-64`), so partial unions do register partial-type dependencies. This is the asymmetry
  that DT-3 records on the other side.
- Generated syntax trees at design time. The generator reuses the parse options of the compilation
  (`DesignTimeSyntaxTreeGenerator.cs:54-57`), so generated documents share the language version and the preprocessor
  symbols of the project, and the design-time test runner adds them to the input compilation unchanged
  (`DesignTimeTestRunner.cs:58-66`).
- Workspaces packaging under PB-2027.0. The single .NET 10 target
  (`Metalama.Framework.Workspaces.csproj:18`) is what the baseline requires, and a .NET 11 consumer resolves that
  asset. The fallback to a .NET 9 asset (`:96-98`) is never selected once the consumed package contains a .NET 10
  folder, and the
  error at `:117-118` guards the packing. The file `build/Metalama.Framework.Workspaces.targets` imports a path that
  does not exist, because the file name in the import is singular where the real file is plural; this has been so
  since the first public commit and has no effect, because NuGet drops from the build asset list every file whose name
  also appears under the transitive build directory, so only the transitive file is ever imported. The file may be
  deleted or corrected in any later change.
- Workspaces MSBuild versions. The MSBuild pin is 18.0.2 with its rationale at `Directory.Packages.props:35-52`, and
  the lowest supported host stays the .NET 10 SDK under PB-2027.0
  ([`Directory.Packages.md`](../../../Directory.Packages.md):18-20), so the pin is unchanged by .NET 11. The MSBuild
  locator and the solution persistence package (`Directory.Packages.props:151-152`) have no dependency on the SDK
  major version.
- LinqPad literals. The only framework literal is the target framework of the driver
  (`Metalama.LinqPad.csproj:6`); the other target framework strings are project properties read at run time
  (`Permalink.cs:49`, `SchemaFactory.cs:52-55`). The driver compiles its typed data context with the Roslyn of
  LINQPad (`MetalamaScratchpadDriver.cs:167-180`), which is independent of the Metalama language version, and the
  LINQPad reference package ships assets that constrain nothing.
- Test directives. The required-constant and forbidden-constant directives are evaluated against the preprocessor
  symbol metadata of the test assembly (`TestInput.cs:74-95`, `Metalama.Testing.AspectTesting.targets:58-61`), the
  language feature directive maps to the Roslyn parse option features (`TestOptions.cs:720-737`,
  `BaseTestRunner.cs:226-229`), and the dependency language version directive shapes the dependency project
  (`BaseTestRunner.cs:421-424`). All three behave as documented in [`testing.md`](../testing.md):147-154.
- One observation outside the .NET 11 scope, recorded so that it is not lost. The workspace loader accepts project,
  solution and solution filter files only (`Workspace.cs:311-340`), and no file under Workspaces or LinqPad mentions
  the newer XML solution format, although the solution persistence package is referenced. Visual Studio 2026 and 2027
  create solutions in that format, so this deserves a separate issue.

## Related themes

The findings of this theme cross-reference the following work owned elsewhere. The prefix of a finding identifies its
theme: LV for the language version and the hosts, TP for the syntax generator and the templates, CM for the code
model, LK for the linker and the advice, DT for this theme, UT for the user target frameworks, the tests and the
documentation, and PR for `Metalama.Premium`.

- Hand-written type-declaration kind lists (cluster CL-01, owned by the code model theme). DT-1 and DT-6 are the
  design-time half of one edit that CM-2, CM-6 and LK-3 also report. The predicate at
  `Metalama.Framework/src/Metalama.Framework.Engine/Utilities/Roslyn/SyntaxKindExtensions.cs:33-35` and `:41` is the
  single place at which the kind is tested, and the shape of the edit, a type test or an added kind, is one shared
  decision. It is one work item and not five.
- The Roslyn variant gating strategy (cluster CL-09, owned by the code model theme as CM-10). DT-1, DT-2, DT-3, DT-5
  and DT-6 each state that their implementation depends on it. The engine is compiled twice from the same sources,
  once against a Roslyn that has none of the C# 15 API, production source carries no conditional compilation today,
  and the API that does exist in the consumed build is marked experimental, which the compiler reports as an error.
  The three candidate mechanisms have different reach: a numeric syntax kind names no absent member but cannot
  override a visitor method or call a syntax factory, a preprocessor block can but must be renamed with every variant
  renumbering, and a per-variant service repeats a reflection pattern that #1215 deliberately removed.
- The design-time generated partial part (cluster CL-04, owned by this theme). DT-2 is the same arm of the same
  factory that CM-3 and LK-4 report. LK-4 contributes the verified statement that the `closed` modifier needs no
  counterpart in the generated part, because the compiler merges the modifiers of partial parts.
- Renumbering the latest Roslyn variant to the stable 5.12 (cluster CL-05, owned by the language version theme). DT-3
  and DT-8 are the design-time and packaging views of one release step that LV-12, LV-13, LV-14, TP-1, TP-9 and PR-1
  also report. DT-3 is the grammar refresh and the regeneration that stop stripping the experimental nodes; DT-8 is
  the same edit seen as a release gate for the packages published on nuget.org. The mirror edit in
  `Metalama.Premium` is delivered as a separate pull request, because a pull request cannot span two repositories.
- The C# language version tables (cluster CL-06, owned by the language version theme). DT-4 is the design-time member
  of a cluster that also contains LV-2, LV-3, LV-6, LV-7, TP-2 and TP-8. Its contribution is a deliberate non-change
  with a recorded rationale; the implementation is in the supported-versions table, the display mapping, the .NET SDK
  mapping and the MSBuild `LangVersion` rewrite.
- The test harness and C# 15 (cluster CL-08, owned by the tests and conventions theme). DT-7 is the plan; LV-8 records
  that the tolerant skipping is adequate while C# 15 does not exist, and UT-19 defines the directory and constant
  conventions that a C# 15 suite follows. One change to a shared harness plus one convention note.
- Switches over declaration kinds that fall through (cluster CL-17, owned by this theme). DT-5 is one of four
  instances of the same shape, with PR-10, TP-7 and PR-11. None of the four is caused by C# 15 and all are reachable
  today, and in every case the remedy is to test an abstract syntax base type or to add the missing arm, which admits
  unions later without naming an experimental member.
- Documentation that states the previous baseline (cluster CL-20, owned by the tests and documentation theme). DT-9 is
  the host runtime statement for the introspection packages, delivered with UT-18, LV-10, PR-9 and PR-14 in one pull
  request, because four of the five edit the same two documents.
- The public code model of a union (CM-1). DT-1 and DT-2 both consume it, and both are deliberately written so that
  they do not require it: the discriminator is the kind of the primary declaration syntax, not a code model member.
- Injection and linking of members introduced into a union (cluster CL-13, owned by the linker and advice theme). A
  correction of the design-time generator alone leaves the members dropped by the injection rewriter, so the editor
  and the produced assembly would disagree in the opposite direction from today. The two clusters have to ship in the
  same release, although not necessarily in the same pull request.
