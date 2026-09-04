# Decisions for the 2027.0 C# 15 and .NET 11 work

This document states the decisions that govern the release. It is written as the position that holds today, not as
a record of how that position was reached, so a decision that was taken, revised and settled appears once, in its
final form.

What is not settled is listed in [`OPEN-QUESTIONS.md`](OPEN-QUESTIONS.md), which is the companion of this document.
A question leaves that file when it is answered here. The user stories are written against these decisions, and a
document that still presents one of them as an open question is out of date.

## 1. C# 15 support ships in Metalama 2027.0

The C# 15 feature axis is in scope for the release. Every story that depends on a Roslyn version carrying C# 15 is
therefore in scope, and the schedule risk of the window between the November 2026 Roslyn and the general
availability date of 2027-01-01 is accepted rather than avoided.

## 2. What support must contain, and what is discretionary

Metalama must be able to advise any user code. Whatever a user writes, and whatever language feature it uses, an
aspect applied to it must work: the code model must describe it, the pipeline must transform it, the linker must
emit it, and the design-time result must match the build.

Metalama is not required to expose every new language feature in the advising and introduction interfaces. That an
aspect can introduce a construct is a separate product decision, taken feature by feature on its merits, and the
absence of it is a limitation rather than a defect. The four open issues #865, #866, #867 and #869, which ask for
the introduction of enums, delegates, records and structs, are the standing evidence that the product has always
worked this way.

This is a general rule of the product rather than a decision about this release. A search of
`Metalama.Framework/docs` and of `CLAUDE.md` finds it written nowhere, which is why it is recorded here.

### What the rule requires for 2027.0

Required, because a user will write this code and apply an aspect to it: the union in the code model, the
compile-time and design-time dispatch, the injection and linking of advice applied to a union, the design-time
partial part, the comparer repairs, the correctness of the pattern and extension libraries on a union, the reading
of a closed hierarchy, and the handling of labeled statements, collection expression arguments and extension
indexers in code that an aspect transforms. None of this is deferrable while C# 15 ships. Deferring it does not
stop the code from existing. It only leaves Metalama crashing or silently wrong when it meets it.

Discretionary, because it adds surface rather than protecting code: the introduction of a union, the introduction
of a case into a union, and the introduction of a closed class. Section 4 puts all three in scope for 2027.0 and
marks them as nice to have. They are the first thing to move to 2027.1 if the release runs short, and they are the
only thing that may move without breaking the promise that Metalama can advise any user code.

Moving the union work as a whole to 2027.1 is therefore not available, because most of that work is the advising
path rather than the interface surface. What may move is the introduction interface, which is one large story and
two medium ones.

### What Metalama does not guarantee

Metalama makes no guarantee that it generates valid C# code, and it does not verify every precondition of the
language. That responsibility belongs to the aspect author. An eligibility rule is written where it protects the
pipeline or where it turns a compiler error into a message the aspect author can act on, not as a systematic
restatement of the language rules.

## 3. Unions are supported as aspect targets

The answer is full support, not a blanket refusal. The code model exposes the union, the compile-time path handles
it, the linker injects and links advice applied to it, and the design-time pipeline emits a union partial part.
Advice that a union cannot carry, such as an instance field, an auto-property, a field-like event, a public
single-parameter constructor or a constructor that does not chain, is refused with a clear diagnostic rather than
producing code that the compiler rejects. Both halves are in scope.

## 4. Introducing unions, union cases and closed classes

An aspect may introduce a closed class, a union, and a case into a union. All three are in scope for 2027.0 and all
three are discretionary under section 2, in the order given: the closed class is the smallest and therefore the
likeliest to survive a cut, and the two union stories go together, because the second is meaningless without the
first.

The four open introduction issues #865, #866, #867 and #869, which ask for enums, delegates, records and structs,
stay out of scope. Union introduction is not grouped with them.

Two documentation stories name a discretionary story as a blocker. The internal architecture documents wait on all
three, and the conceptual documentation waits on the union introduction. They document what ships, so the sections
that describe an introduction interface slip with the story that delivers it, and the rest of each document does
not wait for it.

### Introducing a closed class

A closed class is an ordinary class with one more modifier, which is why this is the smallest of the three. The
parts are the settable property on `INamedTypeBuilder`, its validation, the storage in the builder data, the
exposure on the introduced type, and the token emission in `ModifierHelper`. Three details of the emission are not
free and must not be lost in the implementation.

The `closed` token replaces `abstract` rather than joining it. Roslyn sets the abstract flag on a closed class
implicitly and rejects a declaration that says both, so the modifier list must emit `closed` and suppress
`abstract` when the type is closed, while `IsAbstract` keeps reporting true, which is what Roslyn reports.

The token goes before `partial`, because `partial` must sit immediately before the type keyword. The order is
`closed partial class`.

The validation rejects a closed type that is not a class, and one that is sealed or static.

On the hosts that the lower Roslyn variant serves, an aspect that introduces a closed class emits the modifier at
build time and nothing at design time. Section 6 records why that needs no diagnostic: such a host cannot compile
C# 15, so it reports the closed hierarchy as an error of its own before the divergence can matter.

An aspect can already introduce a class together with its subtypes in one run, which the existing tests
`Recursive.cs`, `BaseType_Abstract.cs` and `IntroducedDerivedType.cs` prove, so the scenario is expressible.

### There are two forms of union, and the distinction decides the design

A `union` declaration is one form. The other is any class or struct that carries
`System.Runtime.CompilerServices.UnionAttribute`, which the proposal describes as adapting an existing type to the
union patterns. Roslyn treats both as unions: `ITypeSymbol.IsUnion` is true for either, and the case set of the
second form is derived from the public single-parameter constructors, or from the static factory methods of a
nested member provider interface, rather than from a case list.

The attribute form is a supported authoring form and an opt-in mechanism, not a marker that the compiler reserves
for its own lowering. The proposal states that "any class or struct type with a
`System.Runtime.CompilerServices.UnionAttribute` attribute is considered a union type", and gives the reason: "the
separation between union types and union declarations allows C# to have a succinct union declaration syntax with
opinionated semantics, while also allowing existing types or types with other implementation choices to opt into
union behaviors". It happens to be what a `union` declaration lowers to as well.

One point of the grammar decides the design, and it is settled. Exactly one part of a partial union carries the
case list. A part without one parses and binds. A second part that carries one is an error, CS8863, reported by the
same code path that rejects two parameter lists on a partial record. A union where no part carries one is an error,
CS9370. No diagnostic compares case lists between parts, and none can exist. A generated partial part can therefore
never add a case to a union that the user declared with the `union` keyword.

### Introducing a whole union

The type builder acquires a model for the case list, which the grammar makes mandatory, and the introduction
pipeline materializes the members that the compiler synthesizes, namely one public constructor per case and the
`Value` property. The introduction pipeline never re-reads the final model from Roslyn, so a synthesized member
that an aspect must be able to see or override has to exist as a builder.

The precedent that applies is the introduction of a namespace, which registers a builder without an injection. The
machinery written for records in pull request #1879 does not generalise, because a user may not declare the `Value`
property or a case constructor at all, so there is no override to serve and no body to reproduce.

Nothing is imposed on the user in this case: the aspect emits the attribute form, the user never writes it, and the
design-time result is correct.

### Introducing a case into an existing union

The two forms differ in kind here, and the difference is what question Q1 of
[`OPEN-QUESTIONS.md`](OPEN-QUESTIONS.md) has to settle.

For a type carrying the attribute, adding a case is the introduction of a constructor. That is ordinary member
introduction, it is expressible in a generated partial part, and the editor and the build therefore agree. None of
the restrictions of a union declaration applies, because Roslyn guards all of them behind a test of whether the
type is a union declaration.

For a `union` declaration, adding a case means rewriting the case list of the part the user wrote, which is a
signature change of an existing declaration. The precedent in the code base is the introduction of a parameter into
a partial constructor, delivered for C# 14 in issue #1143. The build-time half is small, because the linker already
rewrites a type parameter list in the same field and the same method. The design-time half cannot be expressed at
all: every route from a generated part is closed by a compiler rule, so the editor cannot show the added case, and
the editor and the build disagree about conversions and about switch exhaustiveness.

Telling a user that an aspect can add a case only if they abandon `union Pet(Cat, Dog)` for the attribute form is a
poor answer, because the attribute form requires the author to write the case constructors and the `Value` property
by hand. That is the usability cost that question Q1 weighs against a build-time-only capability.

The design analysis is in
[`analysis-reports/11-introducing-unions-design.md`](analysis-reports/11-introducing-unions-design.md), which
estimates six to nine days in total, of which about four can proceed before the new Roslyn exists.

## 5. The template language stays at C# 14, and labels are forbidden in templates

`MetalamaTemplateLanguageVersion` stays at `14.0`. No C# 15 language feature is worth having in a template, and the
pin remains bounded by the lowest supported Roslyn variant in any case.

A label in a template is unsupported and must be reported, and so is a labeled `break` or `continue`. The reason is
that the template annotator cannot decide whether a label is run-time or compile-time: the label belongs to a loop
whose scope may differ from the scope of the statement that names it. There is also a consistency argument. Before
C# 15 the only way to use a label was `goto`, which the annotator already reports as an unsupported language
feature, so a label in a template was already useless in practice. C# 15 gives labels a second use, and rejecting
the jumps while accepting the labels would leave the language feature half supported for no benefit.

The mechanism exists and is the same one. `TemplateAnnotator.VisitGotoStatement` calls
`ReportUnsupportedLanguageFeature` for the `goto` keyword. The work is to do the same in a `VisitLabeledStatement`
override, and in the visits of the two jump statements when they carry a name.

One distinction must be preserved in the implementation. The rejection belongs to the annotator, which reads the
template that the aspect author wrote. The template compiler rewriter generates a labeled statement of its own in
the code it produces, and that generation is not affected, because it does not pass through the annotator. A
rejection placed in the rewriter rather than in the annotator would break the generated output.

Run-time code that an aspect transforms is not affected, because the annotator runs only inside a template. Such
code must keep working once the syntax model is regenerated from the stable grammar.

## 6. The C# 15 Roslyn interfaces are reached through the payload variant mechanism

The engine keeps using preprocessor blocks and one implementation assembly per Roslyn version, which is the
mechanism already in place. A symbol in the manner of `ROSLYN_5_12_0_OR_GREATER` is defined by the latest variant
props file, and the sources that name `UnionDeclarationSyntax`, `SyntaxKind.UnionDeclaration`,
`SyntaxKind.ClosedKeyword`, `ITypeSymbol.IsUnion`, `ITypeSymbol.UnionCaseTypes` and `ITypeSymbol.IsClosed` are
compiled only in that variant. The alternatives of numeric syntax kinds and of a reflection shim are rejected.

The note in `eng/RoslynVersions/Roslyn.5.0.0.props` and in `eng/RoslynVersions/Roslyn.5.10.0.props` that no
production source branches on the variant, and the corresponding paragraph of `Directory.Packages.md`, are
superseded and must be rewritten.

### The divergence that the mechanism creates

`Metalama.Framework`, the public application programming interface assembly, is not built per Roslyn version: only
`Metalama.Framework.Engine`, `Metalama.Framework.DesignTime` and `Metalama.Framework.Implementation` carry the
variant suffix. A member such as `INamedType.IsUnion` therefore exists in every host, while the engine code that
answers it is compiled only in the latest variant.

On the hosts that the lower variant serves, `IsUnion` and `IsClosed` report false and an aspect sees a union as an
ordinary struct. The lower variant answers false and reports nothing. No diagnostic is added for this.

The reason is that a host presenting an older Roslyn cannot compile C# 15 at all, so the situation the diagnostic
would describe cannot arise in a project that builds. Metalama is loaded by the host, and a host whose Roslyn
predates C# 15 has no `LanguageVersion.CSharp15` and reaches the features only under `LanguageVersion.Preview`.
Metalama refuses the preview language version: `CompileTimeAspectPipeline.VerifyLanguageVersion` reports
`PreviewCSharpVersionNotSupported` unless the project sets `MetalamaAllowPreviewLanguageFeatures`. A user who writes
a union in such an editor therefore already sees the error, reported by the host for syntax it cannot parse or for a
language feature it does not offer. A Metalama diagnostic would restate what the editor already shows, in a place
where the user can act on it only by changing the integrated development environment.

## 7. The Roslyn variant set of 2027.0

The variant set stays at two. The lower variant is `Roslyn.5.0.0`, and the latest variant is renumbered from 5.10.0
to the version that the November 2026 measurement names, which is expected to be 5.12.0. Roslyn 5.12 replaces the
5.10 variant rather than being added beside it.

### The criterion

A variant is needed for a Roslyn version only if a Visual Studio version that presents it is still in support on
2027-01-31, and that Roslyn exposes a C# 15 feature as a supported, non-experimental language feature whose use
requires a Roslyn interface that an older Roslyn does not have. A version that reaches the features only under
`LanguageVersion.Preview` imposes nothing, because a user cannot rely on them there.

The interface half of the criterion is the operative one, and a new token kind counts. The `closed` modifier
illustrates it: the feature adds no syntax node, and `SyntaxKind.ClosedKeyword` is still a new enumeration member,
so reading or emitting the modifier requires a build against a Roslyn that declares it. `Metalama.Framework` does
not care about the Roslyn implementation, but the engine that reads and writes the syntax does.

### Where the C# 15 boundary sits

The criterion is decidable from the Roslyn sources, and it was applied to the branches by reading
`LanguageVersion.cs`, `Errors/MessageID.cs` and `Syntax/Syntax.xml` on each. The three branches carry a train one
minor apart: `release/stable` at 5.10, `release/insiders` at 5.11 and `main` at 5.12.

| | `release/stable`, Roslyn 5.10 | `release/insiders`, Roslyn 5.11 |
| --- | --- | --- |
| `LanguageVersion.CSharp15` | absent | present, value 1500 |
| `Latest` maps to | `CSharp14` | `CSharp15` |
| Unions, closed classes, collection expression arguments, extension indexers, labeled break and continue, static members in interfaces | all `Preview` | all `CSharp15` |
| Unsafe evolution | `Preview` | `Preview` |
| Experimental nodes in the grammar | five, including the union declaration and the with element | one, the unsafe expression alone |

The boundary is therefore between 5.10 and 5.11. Roslyn 5.10 exposes no supported C# 15 feature, so a Visual Studio
that presents it cannot offer C# 15 to a user and imposes no C# 15 requirement on Metalama. Roslyn 5.11 is the
first version where C# 15 is a supported language version and where the union and with element syntax is no longer
experimental.

The latest variant must therefore bind against a version no higher than the lowest Roslyn that offers C# 15 among
the hosts in support, which is 5.11 if such a host exists and 5.12 otherwise.

### The lower variant is required

Roslyn 5.0 probably serves a supported Visual Studio rather than only Rider and the Visual Studio Code C# Dev Kit.
The branch `release/dev18.0` of `dotnet/roslyn` exists, carries major version 5 and minor version 0, and was still
receiving commits on 2026-08-25, which is nine months after Visual Studio 18.0 shipped in November 2025. Two
dependency-flow branches for the same release were updated on 2026-08-26 and 2026-08-31. A release branch is not
serviced for a channel that nobody can install.

The statement in `platform-support.md` that the Visual Studio 2026 long-term servicing channel opens in November
2026, and is therefore the first pinnable Visual Studio 2026 version, is doubtful for that reason. If a long-term
servicing channel baseline exists at 18.0 and follows the eighteen-month pattern of the Visual Studio 2022
baselines, it is supported well past 2027-01-31 and it presents Roslyn 5.0.

The consequence is that the `Roslyn.5.0.0` variant is required by a Visual Studio host, which makes it harder to
drop and makes the divergence of section 6 a Visual Studio problem as well.

### Why the latest variant is renumbered

Rule 8 of the doctrine says that a variant may exist only if it serves a host in the supported set. A variant whose
identity is 5.10.0 is loadable only by a host presenting Roslyn 5.10 or later, and once the latest variant is
renumbered, the 5.10.0 identity serves no host that the new identity does not serve. Keeping it would add a payload
for an empty set.

Roslyn publishes a stable package every third minor version. The flat container index of
`Microsoft.CodeAnalysis.CSharp` on nuget.org, read on 2026-09-04, serves exactly four versions in the 5 generation:
5.0.0 published 2025-11-18, 5.3.0 published 2026-03-10, 5.6.0 published 2026-07-02 and 5.9.0 published 2026-08-17.
Roslyn minor versions track Visual Studio minor versions, which `platform-support.md` already states, so the Visual
Studio 2026 quarterly versions are 18.0, 18.3, 18.6, 18.9 and 18.12, carrying Roslyn 5.0, 5.3, 5.6, 5.9 and 5.12.

That derivation is an inference from a cadence and it is not sufficient on its own. The package index is equally
consistent with a monthly Visual Studio whose intermediate versions do not publish packages to nuget.org, and
Roslyn may rotate a minor version that no Visual Studio release takes. The rotations observed on `main` were on
2026-07-28 and 2026-08-25, which is roughly monthly, and that places Roslyn 5.12 on `release/stable` around
November 2026.

### The measurement that settles it

Checklist item 1 of [`platform-support.md`](../platform-support.md) requires the Roslyn version of the November
2026 baseline to be read from a real installation after 2026-11-10. It is enough to know the Roslyn version of
every Visual Studio in support on 2027-01-31, and to check whether any of them is 5.11 or above while being below
the version that the latest variant binds against. Until then the renumbering is a decision that depends on that
measurement rather than a settled fact.

Two statements of `platform-support.md` follow from the inference above and are recorded rather than applied,
because that document is the doctrine and because the measurement has not been made. The Roslyn API section says
that the November 2026 baseline carries "Roslyn 5.11 or thereabouts", and the variant table offers a row "5.10 or
above" for the measured Rider and C# Dev Kit version. Neither 5.10 nor 5.11 is published.

Rider is a separate caveat. It does not take a published Roslyn package: it builds its own, reports assembly
version `42.42.42.42`, and carries the real version in an informational attribute, measured at 5.0.0 on 2026-09-01.
A future Rider could report a version that no Visual Studio presents, which is what the release candidate
measurement of checklist item 2 exists to catch.

## 8. How Metalama.Compiler moves to the new Roslyn

`Metalama.Compiler` moves to a preview Roslyn 5.12 first, because otherwise no progress is possible during the
preview. Moving to the stable Roslyn branch is a release candidate requirement rather than a starting condition, so
the C# 15 work does not wait for a stable Roslyn to begin.

The practice is to follow the .NET software development kit release candidate builds and to update the software
development kit and Roslyn at each one, which means merging many times rather than once. After the software
development kit reaches general availability, the wait is for Microsoft to publish the Roslyn public packages,
which they are usually late in doing. The work is therefore a sequence of merges from an upstream branch, paced by
the release candidates, rather than a single move.

There is no point in updating to a Roslyn version that no software development kit has shipped. This retires the
question of whether to chase 5.10 or 5.11 for their own sake.

Roslyn 5.12 does not become the stable branch at the software development kit release candidate. The train rotates
by one minor at a time, so at release candidate time the software development kit carries a 5.12 build from `main`
or from `release/insiders`, and the stable branch is still one or two minors behind.

## 9. The .NET 11 software development kit, and no `net11.0` target framework

The .NET 11 software development kit is installed in the build container and named by `global.json` as the main
software development kit of the product. Without it, the aspect tests that use C# 15 cannot compile.

The mechanism is the compile-time compilation rather than the target framework.
`LanguageVersionProvider.GetLanguageVersionFromDotNetSdk` reads the `NETCoreSdkVersion` property that MSBuild makes
visible to the compiler, and caps the language version of the compile-time compilation by the major version of the
software development kit: a major of 10 or more maps to C# 14 today, and the cap is applied whatever the project
requests. With only the .NET 10 software development kit installed, the compile-time half of an aspect test is
pinned to C# 14, and a test that exercises a C# 15 construct in compile-time code cannot build.

The container work carries a known risk. The comment at `eng/src/Program.cs:19-25` records that two feature bands
under one installation already produced a restore failure through a stale `MSBuildExtensionsPath`. The mitigation
exists upstream: pull request #1919 removes that variable in `DotNetTool.cs:61` and in `MSBuildTool.cs:55`, and the
matching change is in PostSharp.Engineering 2023.2.421, while `Directory.Packages.props:12` still pins 2023.2.420.
The pinned software development kit constant is `dotNetSdkVersion`, currently `10.0.400`, which feeds the container
component, the `global.json` that the preparation step generates, and `DotNetSdkVersion`.

### The `net11.0` target framework is not wanted

No .NET 11 application programming interface justifies a `net11.0` asset or a `net11.0` leg in the test matrix,
which the analysis in [`analysis-reports/09-net11-api-value.md`](analysis-reports/09-net11-api-value.md)
established. The .NET 11 additions are numeric types, domain name resolution, compression, process management,
text, streams and vector intrinsics, and none of them is on a path that Metalama uses. The internal evidence is
decisive on its own: neither repository contains a polyfill file, no production source branches above
`NET8_0_OR_GREATER`, and every shim is written to serve the `netstandard2.0` and `net472` assets, which a `net11.0`
asset would not remove.

The two adjacent candidates are rejected with evidence. The `UnionAttribute` and `IUnion` types of the union
lowering are not needed as references, because the symbol classifier keys well-known types by name and namespace
strings, and because the compile-time compilation always targets `netstandard2.0`. The assembly location override
of `AssemblyLoadContext` solves a problem that Metalama does not have, because Metalama loads compile-time
assemblies from a file path.

The licensing issues #1860 and #1864 imply a .NET 11 runtime as a macOS test host and explicitly not a .NET 11
target framework, because the elliptic curve members they need exist on every target framework of
`Metalama.Backstage` already.

The .NET 11 software development kit is exercised as a build host through the external `Metalama.Tests.DotNetSdk`
matrix rather than through a leg of this repository's own suite.

What remains necessary regardless of the container is that the .NET 11 software development kit works as a build
host, because it is in the supported set. The supported-toolchain check must not report `LAMA0601` for it, and the
language version clamp in `Metalama.Framework.targets` must not rewrite the language version that a `net11.0`
project implies. Both defects are properties of a version comparison and are verified without an installed
software development kit.

One documentation correction follows from the same analysis. The statement in `platform-support.md` that the
`net10.0` toolset rolls forward to .NET 11 overstates `RollForward=Major`, which selects .NET 11 only when no .NET
10 runtime is installed.

## 10. The comparer impact of unions

The analysis is in [`analysis-reports/13-union-comparers.md`](analysis-reports/13-union-comparers.md).

Most of the hazards do not exist, which is the useful part of the answer.
`ConstructorSignatureEqualityComparer` compares parameter types through `SignatureTypeComparer`, so it does not
collide two case constructors that differ only in their single parameter, and no comparer in the code base keys a
constructor on its name and parameter count alone. The missing declaring syntax of a synthesized constructor
reaches no comparer of members, because `DeclarationEqualityComparer` keys on a reference and
`StructuralSymbolComparer` on symbol properties. `InjectedMemberComparer` never receives a synthesized union
member, because the design registers those as builders without an injection, and `DeclarationOrderingComparer` does
not order generated members at all. The determinism fix that pull request #1879 had to make for records is
therefore not needed a second time.

Two genuine defects were found, and neither is in the work breakdown of the design analysis.

### The aspect instance ordering throws when two targets share a span

`AspectInstanceComparer.Compare` in
`Metalama.Framework/src/Metalama.Framework.Engine/Pipeline/ExecuteAspectLayerPipelineStep.cs:198-269` orders aspect
instances by the position of the primary declaration syntax of their target. When two targets have the same span it
has one escape hatch, at `:250-265`: both targets are methods, they have the same declaring type, that type
`IsRecord`, and both are implicitly declared, in which case it compares them by signature. Anything else reaches
the `AssertionFailedException` at `:267`.

A union misses that hatch three ways. The synthesized `Value` is a property rather than a method. The synthesized
case constructors are constructors rather than methods, and there may be several of them, all carrying the span of
the union declaration. The `Invariant.Assert` at `:255` requires `IsRecord`, which is false for a union.

An aspect that targets more than one synthesized member of a union therefore crashes. This affects the reading
half, which ships whatever the open questions decide, so it is not conditional on them. The fix is to generalise
the record special case to any implicitly declared members that share a span, rather than to add a union arm beside
the record one. Size S.

### The conversion reimplementation does not know the union conversions

`DeclarationEqualityComparer` reimplements the conversion rules and enumerates `op_Implicit` methods only, so an
introduced union does not accept the implicit conversion from its case types that Roslyn grants a union declared in
source. Size M, and it is a prerequisite of the introduction work rather than a follow-up.

`ComparerAgreesWithRoslynTests` is the test that would have caught this, and it and `DeclarationComparerTests` are
the two tests that must gain a union case.

### The equality of the union type itself

No impact. The caching pattern builds its cache key from `ToString` rather than from `Equals` or `GetHashCode`, so
the value equality of a union struct does not reach it.

## 11. The application programming interfaces are drafted, and a story does not specify them

A user story states the capability, the scope, the files and mechanisms it touches, and the acceptance criteria. It
does not specify the shape of the public application programming interface. The name and the parameters of a new
advice method, the members added to a builder, and the way a new concept is exposed on an interface are decided
when the story is implemented, not when it is written.

The interfaces are nevertheless drafted now, because the concepts stay too abstract without them and a shape on the
page is what makes a trade-off arguable. The distinction is one of authority rather than of content. A draft is
written to be criticised and replaced. It is illustrative material, it lives in the analyses under
`analysis-reports`, and a user story does not become a specification by citing one. A draft is most useful when it
is written the way an aspect author would meet it, which means the code an author writes, the code that the
author's users write, and the code that Metalama produces, rather than an interface declaration on its own.

### What the drafts found

The drafts are in [`analysis-reports/12-csharp15-api-drafts.md`](analysis-reports/12-csharp15-api-drafts.md). Four
of the five subjects came back smaller than expected.

Reading a union needs two members on `INamedType`, `IsUnion` and `UnionCaseTypes`, following the precedent of
`IsRecord`. The synthesized `Value` property and the per-case constructors are reached by name through the members
that already exist.

Reading a closed hierarchy needs no new way to enumerate derived types. The derived type index already restricts
itself to the current compilation, and the language requires every subtype of a closed type to be in the same
module, so the existing `DerivedTypesOptions.DirectOnly` is already the complete set for a closed type of the
current compilation. Only the flag and its documentation are new. The one genuine hole is a closed type that comes
from a referenced assembly.

Extension indexers need no application programming interface change at all in order to be overridden. Introducing
one needs the removal of a single validation call in the advice factory, plus an eligibility rule requiring the
named receiver that an extension block with an indexer must declare.

The label rejection of section 5 is one error descriptor reported on the label token from the visit methods of the
template annotator. Run-time code that uses a label is unaffected, because the annotator runs only under the guard
that tests whether the code is inside a template.

The fifth subject is the divergence of section 6. The draft argues it both ways and recommends reporting a
diagnostic. Section 6 decides otherwise, for a reason the draft did not weigh: the host that would receive the
diagnostic cannot compile C# 15 and reports the code as an error already.

## 12. The stories are grouped under a new meta issue, "C# 15 Support"

The stories of this analysis become sub-issues of a new issue titled "C# 15", which is created when the stories are
approved. It is a child of the existing meta issue #1921, ".NET 11 Support", which groups the platform work of the
release and already carries sixteen sub-issues of which thirteen are closed.

The hierarchy repeats what the previous release did, which was found by checking the tracker rather than by
assuming. Issue #1039, titled "C# 14", is of type Feature, its parent is issue #1045, ".NET 10 Support", and it
carries twenty sub-issues, all closed. A language feature grouping under the release platform meta issue keeps one
root per release.

The division between the two follows the subject rather than the schedule. A story about the language, that is the
C# 15 language version, unions, closed hierarchies, extension indexers, the template restrictions and the syntax
model, belongs to the new meta issue. A story about the platform, that is a target framework, a software
development kit, a Visual Studio version, a package version or a Roslyn variant, belongs to #1921.

Two stories sit on the boundary and are assigned deliberately. The move of the latest Roslyn variant to the stable
5.12 belongs to #1921, because it is a platform move whose motivation happens to be a language feature. The
enabling of C# 15 as a supported language version belongs to the new meta issue, because it is the language change
that the platform move makes possible.

No issue is created until the stories are approved, which is the standing rule of this analysis.
