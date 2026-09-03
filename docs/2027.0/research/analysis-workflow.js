export const meta = {
  name: 'net11-impact-analysis',
  description: 'Derive and adversarially verify the Metalama 2027.0 user stories for .NET 11 and C# 15',
  phases: [
    { title: 'Analyse', detail: 'one agent per theme, deriving candidate user stories from the digest, the terrain and the code' },
    { title: 'Verify', detail: 'three adversarial lenses per theme' },
    { title: 'Consolidate', detail: 'dedupe and organise into an epic tree' },
  ],
}

const NOTES = 'C:/Users/GaelFraiteur/AppData/Local/Temp/claude/C--src-Metalama-2027-0-Metalama/86248111-7c7e-4f30-bf61-ae10afe3e5e4/scratchpad/net11'
const REPO = 'C:/src/Metalama-2027.0/Metalama'
const PREMIUM = 'C:/src/Metalama-2027.0/Metalama.Premium'

const COMMON = `
You are deriving the work items that Metalama 2027.0 needs in order to support .NET 11 and C# 15.
Metalama is a C# meta-programming and aspect-oriented framework built on Roslyn. It replaces the C#
compiler at build time and runs as an analyzer at design time, in Visual Studio, Rider and the
Visual Studio Code C# Dev Kit.

TWO RESEARCH DOCUMENTS ARE ALREADY WRITTEN. Read both, in full, before you do anything else.

  1. ${NOTES}/DIGEST.md   (4548 lines)
     What .NET 11, C# 15 and Roslyn 5.10 to 5.12 actually change. Verified against primary sources on
     2026-09-03. Its sections are: version anchors; 1 C# 15 language features; 2 Roslyn API and grammar
     changes; 3 compiler breaking changes; 4 .NET 11 runtime and BCL changes; 5 runtime async;
     6 SDK, MSBuild and NuGet; 7 design-time hosts; 8 resolved contradictions; 9 open questions.
     Companion detail files in the same directory, read the ones your theme needs:
     csharp15-overview.md, unions-closed.md, collection-args-indexers-labels.md, memory-safety.md,
     roslyn-api.md, compiler-breaking.md, runtime-bcl.md, runtime-async.md, sdk-msbuild-tooling.md,
     critique.md, and gap-2.md through gap-7.md.

  2. ${NOTES}/TERRAIN.md   (3265 lines)
     Where the Metalama source tree is sensitive to the shape of the C# language and to platform
     versions. A hotspot table of 308 rows with exact paths and line numbers; a section per subsystem;
     section 3, which traces how each kind of language addition propagates end to end; section 4, which
     traces how each platform axis propagates; and section 5, which lists every place that fails silently.

REPOSITORIES ON DISK. Read the actual code to confirm anything you assert.
  - ${REPO}          branch topic/2027.0/26-09-03-net11-impact, based on develop/2027.0
  - ${PREMIUM}       the premium repository

WHAT IS ALREADY DONE, do not propose it again:
  - The platform baseline PB-2027.0, in ${REPO}/Metalama.Framework/docs/platform-support.md.
  - net8.0 and net9.0 removed as target frameworks (#1876). Core flavour net10.0, Desktop flavour net472.
  - The Roslyn 4.12 variant dropped; variants are Roslyn 5.0 and the latest (#1881).
  - RoslynApiMaxVersion is 5.10.0-1.26365.3, restored from the roslyn-consolidated prerelease feed (#1885).
  - MetalamaTemplateLanguageVersion raised to 14.0 (#1896).
  - A report when the host Roslyn is below the floor (#1898), and a warning when the target framework,
    the .NET SDK or the Visual Studio version is outside the tested matrix (#1884).
  - The Visual-Studio-shipped package caps re-derived (#1897).

ALREADY TRACKED AS OPEN GITHUB ISSUES, reference them rather than duplicating them:
  #1860 Backstage licensing fails on macOS with .NET 11, because DSA was removed there.
  #1864 Add an elliptic-curve licensing authority beside the DSA one.
  #1903 Re-derive the .NET 8.0 line pin of user-surfacing packages against PB-2027.0.
  #1913 Metalama.Premium: remove net8.0 and net9.0 and align the Roslyn variants with PB-2027.0.
  #1343 Support meta.Proceed() for compiler-synthesized record members.
  #985  Template compiler: later C# features catch-all.
Verify the current state of any issue you cite with the GitHub command line tool, for example
  gh issue view 1860 --repo metalama/Metalama --json number,title,state,body

HOW THE PREVIOUS LANGUAGE WAVE WAS DECOMPOSED. C# 14 was tracked as one meta issue, #1039, and a set of
issues each covering one feature in one layer: #1034 extension members code model, #1035 extension members
advising, #1036 extension members invokers, #1159 introducing extension blocks, #1160 introducing into
existing extension blocks, #1127 contract on a receiver parameter, #1094 field keyword in target code,
#1114 field keyword in templates, #1108 and #1109 null-conditional assignments, #1110 to #1113 partial
constructors and partial events, #1115, #1116 and #1131 user-defined compound assignment operators,
#1041 simple lambda parameters with modifiers, #1105 raise errors when an unsupported feature is used in
a template. Follow that granularity: one story is one feature in one layer, small enough to be one pull
request. Read a few of those issues to match the register.

THE DOCTRINE THAT DECIDES SCOPE. ${REPO}/Metalama.Framework/docs/updating-roslyn.md, step 3, says that
experimental features are ignored and are not supported. A feature that is still gated on
LanguageVersion.Preview at .NET 11 general availability is therefore out of scope for 2027.0, and the
correct work item for it is a diagnostic that reports it as unsupported, not an implementation. A feature
that is LanguageVersion.CSharp15 at general availability is in scope. State which case applies and why.

WHAT A GOOD STORY LOOKS LIKE. It is one deliverable a developer could pick up and finish. It names the
files it touches, taken from TERRAIN.md and confirmed against the code. It states what is deliberately out
of scope. Its acceptance criteria are checkable, and they include the tests that must exist, because a
Metalama feature without an aspect test under Tests/Aspects/ is not finished.

Do not edit any file. Do not run builds. Use Read, Grep, Glob and the GitHub command line tool.
`

const STORY_SCHEMA = {
  type: 'object',
  required: ['theme', 'stories'],
  additionalProperties: false,
  properties: {
    theme: { type: 'string' },
    themeSummary: { type: 'string', description: 'what this theme is, in 3-6 sentences, for the epic page' },
    stories: {
      type: 'array',
      items: {
        type: 'object',
        required: ['title', 'kind', 'rationale', 'scope', 'outOfScope', 'acceptanceCriteria', 'affectedFiles', 'risk', 'size'],
        additionalProperties: false,
        properties: {
          title: { type: 'string', description: 'imperative, like a GitHub issue title, no trailing period' },
          kind: { type: 'string', enum: ['feature', 'breaking-change-response', 'defect', 'infrastructure', 'investigation', 'documentation'] },
          areaLabel: { type: 'string', description: 'the Area-* GitHub label that fits best' },
          rationale: { type: 'string', description: 'why this is needed, naming the .NET 11 or C# 15 change that forces it' },
          scope: { type: 'string', description: 'what is delivered' },
          outOfScope: { type: 'string' },
          acceptanceCriteria: { type: 'array', items: { type: 'string' } },
          affectedFiles: { type: 'array', items: { type: 'string' }, description: 'repo-relative paths, with line numbers where known' },
          dependsOn: { type: 'array', items: { type: 'string' }, description: 'titles of other stories, or issue numbers' },
          existingIssue: { type: 'string', description: 'the number of an existing GitHub issue that already covers this, or empty' },
          risk: { type: 'string', enum: ['low', 'medium', 'high'] },
          size: { type: 'string', enum: ['S', 'M', 'L', 'XL'] },
          silentFailure: { type: 'string', description: 'what goes wrong quietly if this is not done, or empty if it fails loudly' },
        },
      },
    },
    notes: { type: 'string', description: 'anything the theme raised that is not a story' },
  },
}

const THEMES = [
  { key: 'enablement', label: 'Enabling C# 15 at all',
    prompt: `The prerequisite theme. Cover: the grammar generator gate in
eng/src/GenerateMetaSyntaxRewriter/Model/TreeReader.cs, which deletes every element carrying
ExperimentalUrl before code generation, and how it must behave when the C# 15 elements stop being
experimental; refreshing the Syntax-5.10.0.xml snapshot from the Roslyn branch that Metalama.Compiler
consumes, noting that the digest says LanguageVersion.CSharp15 first appears in Roslyn 5.11, not 5.10;
the six version tables in Metalama.Framework.Engine/Utilities (AllLanguageVersions,
SupportedCSharpVersions.Latest, .All, .ToLanguageVersion, .GetMaxLanguageVersion, LanguageVersionProvider)
and LanguageVersionExtensions display strings; the fact that V5_0_0 and V5_10_0 both map to CSharp14 today,
which makes the template version gate silently pass; the implicit LangVersion clamp in
Metalama.Framework.Package/build/Metalama.Framework.targets around line 118; and moving from the prerelease
Roslyn to the stable one, per the "Entering and leaving a prerelease Roslyn" section of updating-roslyn.md.
Note that this theme gates almost every other theme.` },
  { key: 'unions-codemodel', label: 'Union types: code model',
    prompt: `Representing a C# 15 union in the Metalama code model. Use TERRAIN.md section 3.1, which traces
this end to end, and the new Roslyn API ITypeSymbol.IsUnion and ITypeSymbol.UnionCaseTypes. Note from the
digest that a union lowers to a sealed struct with a boxed object Value and one constructor per case type,
and that its case-type list is a ParameterListSyntax. Decide and justify whether a union is an INamedType,
a new IUnionType derived from INamedType (the IExtensionBlock and ITupleType precedent), or something else,
and derive the consequences for TypeKind, DeclarationKind, the declaration factory, the comparers, the
serializable identifiers, the reference graph and introspection. Pay attention to
DeclarationEqualityComparer.Conversions.cs, which has no default arm.` },
  { key: 'unions-templates-linker', label: 'Union types: templates, advising and linker',
    prompt: `Everything about unions beyond the code model. Cover: a union declaration appearing inside a
template and inside target code; the generated MetaSyntaxRewriter, RoslynVersionSyntaxVerifier and hashers;
TemplateAnnotator and TemplatingCodeValidator, which have no unknown-construct branch; whether an aspect may
introduce a union or introduce members into one; the linker's per-type-kind Visit methods in
LinkerLinkingStep.LinkingRewriter and LinkerInjectionStep.Rewriter, where a type kind with no Visit method
means the aspect silently does nothing; pattern matching over a union inside a template, which the digest
records as a moving target because the unwrapping rules changed between previews; and the
SkipConvenienceFactories and optional-brace peculiarities of UnionDeclarationSyntax.` },
  { key: 'closed', label: 'Closed hierarchies',
    prompt: `The closed contextual modifier. Cover: ITypeSymbol.IsClosed and GetClosedDerivedTypeInfo, the
IsClosedTypeAttribute the compiler emits, the CompilerFeatureRequired("ClosedClasses") attribute emitted on
the constructors of a closed class, and DeclarationModifiers.IsClosed in the Workspaces layer. Then the
Metalama consequences: exposing the modifier in the code model, the modifier surface in ModifierHelper and
ModifierCategories, the override transformations whose GetSyntaxModifierList masks are allow-lists that
silently drop an unknown modifier, and above all the semantic hazard that an aspect which introduces a type
derived from a closed class, or introduces a member into one, changes the exhaustiveness of every switch
over that hierarchy in user code and in other assemblies. Decide what Metalama must forbid, what it must
warn about, and what it must support.` },
  { key: 'extension-indexers', label: 'Extension indexers',
    prompt: `Indexers declared inside an extension block, which is C# 15 and adds no grammar node. This is
the closest analogue to the C# 14 extension-member work, so follow #1034, #1035, #1036, #1159 and #1160.
Cover: the code model, where IExtensionBlock already exposes Indexers; lifting the guard at
Metalama.Framework.Engine/Advising/AdviceFactory.cs:1406 which rejects introducing an indexer into an
extension block, and the missing GetImplicitDeclarations override on IntroduceIndexerTransformation that
makes lifting it insufficient; the accessor naming in
AdviceImpl/Introduction/ExtensionImplementationHelper.cs:177, which concatenates get_ or set_ with a
property name and cannot express an indexer; invokers; overriding an extension indexer; and contracts,
where ContractExtensionBlockTransformation.cs:93 already iterates the indexers speculatively. Also cover
the digest's observation that extension Length, Count and Slice members now affect countability and
implicit indexers for code that already compiles.` },
  { key: 'collection-arguments', label: 'Collection expression arguments',
    prompt: `The with(...) element, WithElementSyntax deriving from CollectionElementSyntax, and the new
ICollectionExpressionOperation.ConstructArguments and OperationKind.CollectionExpressionElementsPlaceholder.
The digest records that the parsing of with(...) is unconditional, so the syntax tree changes at every
language version and not only at C# 15; establish whether that is true and what it implies. Cover: a
with(...) element inside a template, where MetaSyntaxRewriter.Transform of a CollectionElementSyntax
currently throws; compile-time and run-time classification of the arguments in TemplateAnnotator; whether
Metalama's own syntax generation should ever emit one; and whether any Metalama-generated collection
expression must now carry arguments to preserve a comparer or a capacity.` },
  { key: 'labeled-jumps', label: 'Labeled break and continue',
    prompt: `The optional Name field on BreakStatementSyntax and ContinueStatementSyntax. The digest calls
this the most likely silent breakage of the whole Roslyn delta, because the child count goes from three to
four with the new child inserted in the middle, so an existing Update call drops the label. Find every such
call in Metalama. Then cover: templates containing a labeled jump, both in compile-time control flow and in
run-time code; the interaction with Metalama's compile-time loops, which are unrolled, so a run-time labeled
jump that targets a compile-time loop must be diagnosed; the linker, where
Linking/LinkerLinkingStep.CountLabelUsesWalker.cs counts only goto statements, so a label referenced only by
a labeled break is deleted while still referenced; and the statement builders and formatting.` },
  { key: 'static-interface-members', label: 'Non-virtual static members in interfaces',
    prompt: `The seventh C# 15 feature, IDS_FeatureStaticMembersInInterfaces, which the What's New page does
not cover and which the digest records as undocumented and unspecified. Establish exactly what it permits,
from the Roslyn sources and the language design meeting notes in the research directory. Then derive the
Metalama consequences: the code model's notion of what an interface may contain, the eligibility rules,
interface implementation and the InterfaceImplementation area, introducing a static member into an
interface, and the linker. Note the digest's point that this becomes legal on netstandard2.0 and .NET
Framework targets, which is exactly where Metalama's own compile-time compilation lives.` },
  { key: 'memory-safety', label: 'Memory safety and the unsafe model',
    prompt: `The unsafe evolution work: UnsafeExpressionSyntax, the SafeKeyword, MemorySafetyRulesAttribute,
ISymbol.RequiresUnsafeContext, IModuleSymbol.MemorySafetyRulesVersion, RequiresUnsafeAttribute, and the
pointer relaxations. The digest states this is LanguageVersion.Preview at general availability, so apply the
doctrine: the story is a diagnostic that reports it as unsupported in a template, plus whatever is needed so
that Metalama does not corrupt code that merely contains it. Verify that reading of the digest before you
build on it. Also consider the Roslyn public API items marked RSEXPERIMENTAL006, which produce a build error
unless suppressed, and whether Metalama's own code will meet them; and the digest's point that await becomes
legal in an unsafe context but illegal in a fixed statement, which touches the async machinery.` },
  { key: 'template-diagnostics', label: 'Diagnostics for unsupported C# 15 constructs',
    prompt: `The analogue of issue #1105 for C# 15. Establish which C# 15 constructs Metalama will not
support in a template in 2027.0, and make sure each is reported rather than silently mistranslated. Cover
the whole reporting chain: RoslynVersionSyntaxVerifier and its generated partial, which never calls
base.Visit; TemplateAnnotator and TemplatingCodeValidator, which have no branch for a construct they do not
recognise; TemplatingDiagnosticDescriptors; and the message text, which must name the construct.
Also cover the case of a C# 15 construct in target code rather than in a template, where the aspect must
either handle it or refuse it, and never rewrite it wrongly. Use the digest's complete inventory of new
diagnostics, CS9354 to CS9400 plus CS9346, to check that Metalama does not itself generate code that trips
one of them.` },
  { key: 'roslyn-version', label: 'Roslyn version, variants and the host matrix',
    prompt: `Cover: leaving the prerelease Roslyn once the version that carries LanguageVersion.CSharp15 is
stable, per the procedure in updating-roslyn.md, and the fact that the digest places CSharp15 in Roslyn 5.11
while the repository references 5.10.0-1.26365.3; whether the Roslyn 5.0 variant is still required, which
depends on the Rider and C# Dev Kit measurement that platform-support.md defers to 2026-11-20; the
verification checklist in platform-support.md and what has to happen at the release candidate; the
RoslynApiVersion enumeration, whose ordinals are the wire form of TemplateSymbolManifest.UsedApiVersion;
ResourceExtractor.GetRoslynVersion and the JetBrains 42.42.42.42 marker; and the Roslyn behavioural changes
between 5.0 and 5.12 that Metalama compiles against, in particular SyntaxFacts.GetTypeDeclarationKind now
returning ExtensionBlockDeclaration for the extension keyword, and SyntaxFactory.Parameter now throwing when
both the type and the identifier are missing.` },
  { key: 'net11-tfm', label: 'net11.0 as a supported user target framework',
    prompt: `Use TERRAIN.md section 4.1. Cover everything that must change for net11.0 to be a first-class
user target framework: the target frameworks of the user-surfacing packages, the Windows-specific assets of
Metalama.Patterns.Wpf, the compile-time compilation which always targets netstandard2.0, the reference
assemblies that CompileTimeAssemblyLocator restores, the support-matrix warning of #1884, the licensing and
telemetry code that reports a framework name, and the test matrix. Distinguish clearly between what a user
targeting net11.0 needs and what Metalama's own projects target.` },
  { key: 'sdk-msbuild', label: '.NET 11 SDK, MSBuild and the build system',
    prompt: `Cover the .NET 11 SDK and the MSBuild it ships: global.json and the SDK the repository builds
with; MicrosoftBuildVersion and the Microsoft.Build.Locator binding, which must not exceed the lowest host;
new and changed SDK defaults and analyzer levels that would fail the continuous integration build, which
runs with ContinuousIntegrationBuild=True and promotes analyzer suggestions to errors; NuGet changes; the
Metalama.Compiler toolset directory selection and its net10.0 asset with rollForward, which is in the
Metalama.Compiler repository but constrains this one; and anything in eng/src that hard-codes an SDK or
MSBuild version.` },
  { key: 'runtime-bcl', label: '.NET 11 runtime and base class library changes',
    prompt: `Go through every .NET 11 runtime and base-class-library breaking change in section 4 of the
digest and decide, for each, whether Metalama is affected, naming the file. Give particular attention to
cryptography, because issues #1860 and #1864 already record that licensing fails on macOS with .NET 11, and
establish whether those two issues are sufficient or whether more is needed; to serialization; to assembly
loading and AssemblyLoadContext, because Metalama loads user compile-time assemblies into the compiler
process; to System.Text.Json, which the manifest serialization uses; and to anything that affects a
long-lived analyzer process. Also report the new APIs that are worth adopting, and say plainly when the
answer for a change is that nothing is needed.` },
  { key: 'runtime-async', label: 'Runtime async',
    prompt: `Read section 5 of the digest in full. Establish whether runtime async is enabled by default in
.NET 11, what the compiler emits instead of a state machine, and whether anything is observable above the
intermediate language. Then answer the question that matters for Metalama: does a framework that rewrites C#
source before the compiler sees it have to change anything. Cover the async template machinery,
TemplateExpansionContext and its ConfigureAwait and Proceed implementations, the linker's inlining of async
methods and iterators, the AsyncHelpers well-known type and the SYSLIB5007 diagnostic, and the
Area-Framework-AsyncAndIterators surface. If the honest answer is that nothing is required, say so and
propose the verification that would establish it, rather than inventing work.` },
  { key: 'new-roslyn-apis', label: 'New Roslyn APIs worth adopting',
    prompt: `Roslyn 5.10 to 5.12 adds public API that Metalama could use even where no C# 15 feature forces
it. Assess each, and propose a story only where the benefit is real: the pre-compilation source output,
IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput and
IncrementalGeneratorOutputKind.PreCompilation, marked RSEXPERIMENTAL007, against how Metalama integrates with
the compiler and with the design-time source generator; INamedTypeSymbol.TypeLayout and
ExtendedLayoutAttribute; SourceHashAlgorithm.Sha384 and Sha512 against the hashing Metalama does for its
compile-time project identity; CSharpExtensions.GetValueConversion on ICoalesceOperation; and the new
WellKnownMemberNames. Be sceptical: reject the ones that buy nothing.` },
  { key: 'design-time', label: 'Design time and the integrated development environments',
    prompt: `Read section 7 of the digest. Cover the design-time consequences: Visual Studio 2027, expected
November 2026, and the Visual Studio 2026 long-term servicing channel baseline, whose private runtime and
Roslyn version the platform-support.md checklist requires to be measured; the second Rider and C# Dev Kit
measurement due 2026-11-20; ResourceExtractor and the extension loader target-framework literals; the
contract versioning between Metalama.Framework.DesignTime.Contracts and the Visual Studio extension, which
lives in the Metalama.Vsx repository; syntax highlighting and classification of the new keywords union,
closed and safe; code lens, the aspect explorer and the preview, all of which must handle a union and a
closed type; and the design-time memory rules when a new declaration kind is added.` },
  { key: 'patterns', label: 'Metalama.Patterns under C# 15 and .NET 11',
    prompt: `Go through each package under Metalama.Patterns/src and decide what C# 15 and .NET 11 require:
Contracts, whose aspects apply to parameters, fields and properties and would now meet extension indexers
and unions; Observability, which already deals with the field keyword and semi-automatic properties and
would meet closed hierarchies and unions in a view model; Caching and its backends, including the .NET 11
base-class-library changes; Memoization; Immutability, which is interesting because a closed hierarchy and a
union are both immutability-relevant; Wpf, whose windows-specific asset moves with the target framework; and
Flashtrace. Be concrete: name the aspect and the construct it would meet.` },
  { key: 'premium', label: 'Metalama.Premium',
    prompt: `Everything in ${PREMIUM}. Cover: issue #1913, removing net8.0 and net9.0 and aligning the
Roslyn variants, and whether it is complete as written; Metalama.Extensions.Architecture, whose predicates
and validators enumerate C# constructs and would meet unions, closed hierarchies and extension indexers;
Metalama.Extensions.Validation and its engine, including the reference kinds a union introduces;
Metalama.Extensions.CodeFixes and its design-time part; Metalama.Licensing and Metalama.Licensing.BuildTasks
against the .NET 11 cryptography changes; and the Azure and Redis caching backends against .NET 11.
Read the repository rather than assuming symmetry with the open-source one.` },
  { key: 'testing', label: 'Testing infrastructure and matrices',
    prompt: `Cover what must exist so that C# 15 and .NET 11 are actually tested: a Tests/Aspects/CSharp15
suite organised by feature, following the CSharp14 layout; the @LanguageVersion directive and whether it
accepts C# 15, including the skip path when the running Roslyn does not recognise the version; the
@RequiredConstant and @ForbiddenConstant directives and the ROSLYN_5_10_0_OR_GREATER constant; the standalone
and design-time standalone tests; the docker tests; the target frameworks of the test matrix and whether
net11.0 must be added; and the fact that Build.ps1 build does not build test projects, so a new test project
must be added to the right place. Also cover the continuous integration consequence that new test code is
the most likely place for a diagnostic that only fails under ContinuousIntegrationBuild=True.` },
  { key: 'docs', label: 'Documentation and the public surface',
    prompt: `Cover the documentation and public-surface work: the requirements page on doc.metalama.net and
the supported platform matrix; the conceptual documentation in the sibling Metalama.Documentation
repository, which needs articles or updates for every C# 15 feature Metalama supports and a statement for
every one it does not; the migration notes for users on net8.0 and net9.0, whose target framework is no
longer supported; the XML documentation of any public API added by the other themes; and
platform-support.md itself, whose verification checklist has three items due before the general availability
date. Propose stories, not a wish list: each one must name what is written and where.` },
]

phase('Analyse')

const analysed = (await parallel(THEMES.map(t => () =>
  agent(`${COMMON}\nYOUR THEME: ${t.label}\n\n${t.prompt}\n\n` +
    `Return between 1 and 8 stories. Fewer, well-founded stories beat many speculative ones. If the honest ` +
    `conclusion for part of your theme is that no work is needed, put that in notes and do not invent a story.`,
    { label: `analyse:${t.key}`, phase: 'Analyse', schema: STORY_SCHEMA, effort: 'high' })
))).map((r, i) => r ? { ...r, key: THEMES[i].key, label: THEMES[i].label } : null).filter(Boolean)

const flat = analysed.flatMap(a => a.stories.map(s => ({ ...s, themeKey: a.key, themeLabel: a.label })))
log(`${analysed.length}/${THEMES.length} themes analysed; ${flat.length} candidate stories`)

phase('Verify')

const LENSES = [
  { key: 'factual', ask: `Is each story's premise factually correct about .NET 11, C# 15 or Roslyn? Re-check it against ${NOTES}/DIGEST.md and the companion research files, and against primary sources on the web if the digest is silent. A story whose rationale misstates what the language or the runtime does is refuted.` },
  { key: 'already-done', ask: `Is each story already implemented, or already tracked? Read the actual code at the paths the story names. Check the GitHub issues with: gh issue list --repo metalama/Metalama --search "<terms>" --state all --limit 30 --json number,title,state. A story that restates work already merged into develop/2027.0, or already covered by an open issue, is refuted; say which issue.` },
  { key: 'scope-and-shape', ask: `Is each story in scope for 2027.0 and correctly shaped? Apply the doctrine of updating-roslyn.md step 3: a feature still gated on LanguageVersion.Preview at general availability is out of scope, and only a diagnostic is owed. Apply platform-support.md for anything about a platform floor. Also judge the shape: is it one deliverable, or two stories fused, or a vague wish? Are the acceptance criteria checkable? Does it name real files that exist?` },
]

const VERDICT = {
  type: 'object',
  required: ['verdicts'],
  additionalProperties: false,
  properties: {
    verdicts: {
      type: 'array',
      description: 'exactly one entry per candidate story you were given, in the same order',
      items: {
        type: 'object',
        required: ['storyTitle', 'refuted', 'confidence', 'reason'],
        additionalProperties: false,
        properties: {
          storyTitle: { type: 'string' },
          refuted: { type: 'boolean' },
          confidence: { type: 'string', enum: ['low', 'medium', 'high'] },
          reason: { type: 'string', description: 'with a file path, a line number, an issue number or a source URL' },
          correction: { type: 'string', description: 'if not refuted but something is wrong, the correction to apply' },
          duplicateOfIssue: { type: 'string' },
        },
      },
    },
    missingStories: {
      type: 'array',
      items: { type: 'string' },
      description: 'work this theme needs that the candidates do not cover, seen through your lens',
    },
  },
}

const verifiedByTheme = await pipeline(
  analysed,
  (theme) => parallel(LENSES.map(lens => () =>
    agent(
      `${COMMON}\nYou are verifying candidate work items, adversarially. Default to refuting when you are ` +
      `not convinced. Do not be agreeable, and do not wave a story through because it sounds plausible.\n\n` +
      `LENS: ${lens.ask}\n\n` +
      `THEME: ${theme.label}\n\nCANDIDATE STORIES (${theme.stories.length}):\n` +
      `${JSON.stringify(theme.stories, null, 2)}\n\n` +
      `Investigate each one, in the actual code and the actual sources, then return one verdict per story ` +
      `in the same order. Also report, in missingStories, any work this theme needs that these candidates ` +
      `do not cover and that your lens makes visible.`,
      { label: `verify:${lens.key}:${theme.key}`, phase: 'Verify', schema: VERDICT, effort: 'high' })
  )).then(votes => ({ theme, votes: votes.filter(Boolean) }))
)

const byTitle = new Map()
for (const s of flat) {
  byTitle.set(s.themeKey + ' ' + s.title, { story: s, refutations: [], corrections: [], duplicateOfIssue: '' })
}
const missingByTheme = []
for (const entry of verifiedByTheme.filter(Boolean)) {
  for (const vote of entry.votes) {
    for (const v of vote.verdicts || []) {
      const rec = byTitle.get(entry.theme.key + ' ' + v.storyTitle)
      if (!rec) { continue }
      if (v.refuted) { rec.refutations.push(v.reason) }
      if (v.correction) { rec.corrections.push(v.correction) }
      if (v.duplicateOfIssue && !rec.duplicateOfIssue) { rec.duplicateOfIssue = v.duplicateOfIssue }
    }
    for (const m of vote.missingStories || []) { missingByTheme.push({ theme: entry.theme.label, gap: m }) }
  }
}

const all = [...byTitle.values()]
const kept = all.filter(r => r.refutations.length < 2)
const dropped = all.filter(r => r.refutations.length >= 2)
log(`${kept.length} stories survived; ${dropped.length} refuted; ${missingByTheme.length} gaps reported`)

phase('Consolidate')

const consolidated = await agent(
  `You are consolidating the verified work items for Metalama 2027.0 under .NET 11 and C# 15 into one ` +
  `backlog document.\n\n` +
  `Write it to ${NOTES}/BACKLOG.md. Then return the structured summary.\n\n` +
  `SURVIVING STORIES, each with its adversarial verdicts and any corrections the verifiers asked for. ` +
  `APPLY EVERY CORRECTION before you write the story out:\n${JSON.stringify(kept, null, 2)}\n\n` +
  `REFUTED CANDIDATES, with the reasons. Record them in an appendix called "Considered and rejected", ` +
  `with the reason, because the reader needs to know these were examined:\n` +
  `${JSON.stringify(dropped.map(d => ({ title: d.story.title, theme: d.story.themeLabel, reasons: d.refutations })), null, 2)}\n\n` +
  `GAPS THAT THE VERIFIERS REPORTED, that is, work a lens made visible and that no candidate covers. ` +
  `Turn each into a story where it is genuine, and drop it where it duplicates one:\n` +
  `${JSON.stringify(missingByTheme, null, 2)}\n\n` +
  `THEME SUMMARIES, for the epic pages:\n${JSON.stringify(analysed.map(a => ({ key: a.key, label: a.label, summary: a.themeSummary, notes: a.notes })), null, 2)}\n\n` +
  `THE DOCUMENT MUST:\n` +
  `  1. Open with a short statement of what .NET 11 and C# 15 change for Metalama, and of what is already ` +
  `     done on develop/2027.0.\n` +
  `  2. Group the stories into epics. Propose the epic structure yourself from the material; the C# 14 wave ` +
  `     used one meta issue plus one issue per feature per layer.\n` +
  `  3. Merge duplicates across themes. Two stories that touch the same files for the same reason are one ` +
  `     story. Say in a "Merged" line which candidates were merged.\n` +
  `  4. Order the stories by dependency, and make the dependency edges explicit. The enablement theme gates ` +
  `     most of the language themes.\n` +
  `  5. Mark each story that an existing GitHub issue already covers, with its number.\n` +
  `  6. Keep the appendix of rejected candidates.\n` +
  `PROSE RULES, which are the house rules of this repository and are not negotiable: be accurate; use ` +
  `precise software engineering language and no analogies or slang; state the subject in the first clause ` +
  `and never open with a rhetorical construct; expand any acronym that is not standard in this codebase; ` +
  `assume the reader is not a native speaker of English, so prefer short sentences and one idea per ` +
  `sentence; do not use bold for emphasis inside a paragraph and do not use italics to stress a word; ` +
  `never use an em dash.`,
  {
    label: 'consolidate', phase: 'Consolidate', effort: 'high',
    schema: {
      type: 'object',
      required: ['backlogFile', 'epics'],
      additionalProperties: false,
      properties: {
        backlogFile: { type: 'string' },
        epics: {
          type: 'array',
          items: {
            type: 'object',
            required: ['title', 'summary', 'storyTitles'],
            additionalProperties: false,
            properties: {
              title: { type: 'string' },
              summary: { type: 'string' },
              storyTitles: { type: 'array', items: { type: 'string' } },
            },
          },
        },
        totalStories: { type: 'number' },
        mergedCount: { type: 'number' },
        rejectedCount: { type: 'number' },
      },
    },
  })

return { consolidated, candidateCount: flat.length, keptCount: kept.length, droppedCount: dropped.length }
