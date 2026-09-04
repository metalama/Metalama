# Decisions taken by the product owner on 2026-09-04

These answers are settled, except where a section says the question is open. What is not settled is listed in
[`OPEN-QUESTIONS.md`](OPEN-QUESTIONS.md), which is the companion of this document: a question leaves that file
when it is answered here. The user stories are written against them, and a document that still presents one of
them as an open question is out of date.

## 1. C# 15 support ships in Metalama 2027.0

The C# 15 feature axis is in scope for the release. Every story that depends on Roslyn 5.12 is therefore in scope,
and the schedule risk of the window between the November 2026 Roslyn and the general availability date of
2027-01-01 is accepted rather than avoided.

## 2. The C# 15 Roslyn API is reached through the existing variant mechanism

The engine keeps using preprocessor blocks and one implementation assembly per Roslyn version, which is the
mechanism already in place. A symbol in the manner of `ROSLYN_5_12_0_OR_GREATER` is defined by the latest variant
props file, and the sources that name `UnionDeclarationSyntax`, `SyntaxKind.UnionDeclaration`,
`SyntaxKind.ClosedKeyword`, `ITypeSymbol.IsUnion`, `ITypeSymbol.UnionCaseTypes` and `ITypeSymbol.IsClosed` are
compiled only in that variant. The note in `eng/RoslynVersions/Roslyn.5.0.0.props` and in
`eng/RoslynVersions/Roslyn.5.10.0.props` that no production source branches on the variant, and the corresponding
paragraph of `Directory.Packages.md`, are superseded and must be rewritten. The alternatives of numeric syntax
kinds and of a reflection shim are rejected.

The consequence for the Roslyn 5.0 variant, which serves Rider and the Visual Studio Code C# Dev Kit, is that
`IsUnion` and `IsClosed` report false there. Whether that variant reports a diagnostic instead of staying silent
is a design question of the union stories, not of this decision.

## 3. Unions are supported as aspect targets

The answer is full support, not a blanket refusal. The code model exposes the union, the compile-time path handles
it, the linker injects and links advice applied to it, and the design-time pipeline emits a union partial part.
Advice that a union cannot carry, such as an instance field, an auto-property, a field-like event, a public
single-parameter constructor or a constructor that does not chain, is refused with a clear diagnostic rather than
producing code that the compiler rejects. Both halves are in scope.

## 4. The template language stays at C# 14, and labels are forbidden in templates

`MetalamaTemplateLanguageVersion` stays at `14.0`. No C# 15 language feature is worth having in a template, and the
pin remains bounded by the lowest supported Roslyn variant in any case.

A labeled `break` or `continue` in a template is forbidden and must be reported with a diagnostic. The reason is
that the label of such a statement cannot be classified as run-time or compile-time by the template annotator: the
label belongs to a loop whose scope may differ from the scope of the statement that names it. This replaces the
earlier proposal to support labeled break and continue in templates and to rename labels when inlining. The
diagnostic belongs to the template compiler, beside the other rejections of syntax that the annotator cannot
classify.

Run-time code that an aspect transforms, and that uses a labeled break or continue outside a template, is not
affected by this decision and must keep working once the syntax model is regenerated from the stable grammar.

## 5. Introducing a closed class or a union: to be analyzed

Whether an aspect may introduce a closed class, and whether it may introduce a union, is not decided. It needs a
separate analysis, which must take into account that introduced type support covers neither structs, records, enums
nor delegates today (issues #869, #867, #866 and #865 are open). Until that analysis is done, the reading half is
in scope and the writing half is out of scope for the stories.

## 6. No `net11.0` leg is added to the full test matrix without a reason

Adding `net11.0` beside `net10.0` in every test project is not justified unless there is a .NET 11 application
programming interface that Metalama wants to use. The question of whether such an interface exists must be answered
before any test matrix story is written. What remains necessary regardless is that the .NET 11 SDK works as a build
host, because it is in the supported set: the language version clamp and the supported-toolchain check must be
correct under it, and a scenario that exercises the .NET 11 SDK as a build host is worth having even when the
target framework of the test project stays `net10.0`.

## 6b. The .NET 11 SDK in the build container is not a priority

Added on 2026-09-04, refining decision 6. Installing the .NET 11 SDK in the build container and settling which
version `global.json` pins is not important for 2027.0 and is a distraction. The container work follows from a
`net11.0` test leg, and decision 6 does not ask for one.

Two things remain in scope and do not depend on the container.

The supported-toolchain check must not report `LAMA0601` for a supported .NET SDK. The ceiling `MaximumSdkVersion`
is `11.0` and the comparison uses `VersionGreaterThan` against the full version, so an .NET 11 SDK of `11.0.100`
compares as greater than the ceiling and every build with it reports that the SDK is unsupported. This is a defect
of the version comparison and it is verified by a test of that comparison, not by installing an SDK in the image.

The language version clamp in `Metalama.Framework.targets` must not rewrite the language version that a `net11.0`
project implies. That defect is likewise a property of the condition and is verified without the SDK.

## 6c. The answer to decision 6: no .NET 11 interface is wanted

Established on 2026-09-04 by the analysis in
[`analysis-reports/09-net11-api-value.md`](analysis-reports/09-net11-api-value.md), which is the answer that the
open question of decision 6 asked for.

No .NET 11 application programming interface justifies a `net11.0` asset or a `net11.0` test leg. The .NET 11
additions are numeric types, domain name resolution, compression, process management, text, streams and vector
intrinsics, and none of them is on a path that Metalama uses. The internal evidence is decisive on its own: neither
repository contains a polyfill file, no production source branches above `NET8_0_OR_GREATER`, and every shim is
written to serve the `netstandard2.0` and `net472` assets, which a `net11.0` asset would not remove.

The two adjacent candidates are rejected with evidence. The `UnionAttribute` and `IUnion` types of the union
lowering are not needed as references, because the symbol classifier keys well-known types by name and namespace
strings, and because the compile-time compilation always targets `netstandard2.0`. The assembly location override
of `AssemblyLoadContext` solves a problem that Metalama does not have, because Metalama loads compile-time
assemblies from a file path.

The licensing issues #1860 and #1864 imply a .NET 11 runtime as a macOS test host and explicitly not a .NET 11
target framework, because the elliptic curve members they need exist on every target framework of
`Metalama.Backstage` already.

This confirms decision 6b: with no interface wanted, the build container change has no justification either.

One documentation correction follows from the same analysis. The statement in `platform-support.md` that the
`net10.0` toolset rolls forward to .NET 11 overstates `RollForward=Major`, which selects .NET 11 only when no .NET
10 runtime is installed.

## 5b. The answer to decision 5: both writers are out of scope for 2027.0

Established on 2026-09-04 by the analysis in
[`analysis-reports/10-introducing-closed-and-unions.md`](analysis-reports/10-introducing-closed-and-unions.md).

An aspect may read a union and a closed class, through `INamedType.IsUnion` and `INamedType.IsClosed`, and may not
introduce either. The reading half is in scope for 2027.0 and the writing half is not.

The closed writer is size M and every part of it is identified and cheap: the property on `INamedTypeBuilder`, its
validation, the storage in the builder data, the exposure on the introduced type, and the token emission in
`ModifierHelper`. It is deferred for the reason in the next section rather than for its cost. An aspect can already
introduce a class together with its subtypes in one run, which the existing tests `Recursive.cs`,
`BaseType_Abstract.cs` and `IntroducedDerivedType.cs` prove, so the scenario is expressible.

The union writer is size L and is blocked by two things that are not the absence of struct introduction. The
grammar makes the case list mandatory and the type builder has no model for it, the primary constructor of a
builder being an open item in `INamedTypeBuilder`. The compiler-synthesized members must be materialized as
builders, because the introduction pipeline never re-reads the final model from Roslyn. That is the same blocker
that keeps record introduction open in issue #867.

Issues #865, #866, #867 and #869, which ask for the introduction of enums, delegates, records and structs, are all
open user stories with no milestone, no assignee and no body, imported from the previous tracker. Union
introduction belongs with them rather than with the C# 15 work.

The one thing that would change this recommendation is a customer scenario that needs an aspect-generated closed
hierarchy. None is known.

## 2b. A consequence of decision 2 that needs a further decision

The analysis of the writers surfaced a property of the variant mechanism that decision 2 does not settle, and that
applies to the reading half as much as to the writing half.

`Metalama.Framework`, the public application programming interface assembly, is not built per Roslyn version:
only `Metalama.Framework.Engine`, `Metalama.Framework.DesignTime` and `Metalama.Framework.Implementation` carry
the variant suffix. A member such as `INamedType.IsUnion` therefore exists in every host, while the engine code
that answers it is compiled only in the latest variant.

The consequence is a silent divergence between design time and build time on the hosts that the Roslyn 5.0 variant
serves, which are Rider and the Visual Studio Code C# Dev Kit. There, `IsUnion` and `IsClosed` report false, an
aspect sees a union as an ordinary struct, and an aspect that emits a `closed` modifier at build time emits nothing
at design time. The editor and the command line then disagree, and nothing reports it. This is the same class of
failure that the platform baseline document describes as the reason for deriving the Roslyn floor deliberately.

The open question is what the lower variant does when it meets a C# 15 type that it cannot see: stay silent, or
report a diagnostic that the design-time result is incomplete because the host Roslyn predates C# 15. A diagnostic
is the safer answer and costs one descriptor and one call site, but it fires in an editor whose user cannot act on
it other than by changing the integrated development environment.

## 5c. Decision 5 is overridden: introducing unions and union legs is required

Taken by the product owner on 2026-09-04, superseding section 5b. Metalama 2027.0 must support the introduction of
a union type and the introduction of a union leg, that is a case of a union. The recommendation of the analysis to
ship only the reading half is not accepted.

This is the largest single piece of C# 15 work in the release, and it has two halves that differ in kind.

Introducing a whole union means that the type builder acquires a model for the case list, which the grammar makes
mandatory, and that the introduction pipeline materializes the members that the compiler synthesizes, namely one
public constructor per case and the `Value` property. The introduction pipeline never re-reads the final model from
Roslyn, so a synthesized member that an aspect must be able to see or override has to exist as a builder. The
machinery for exactly this problem was written for records in pull request #1879, which materializes the body of a
compiler-synthesized record member from its symbol, and it is the natural thing to extend rather than to duplicate.

Introducing a leg into a union that already exists in source is a different operation. It changes the case list of
a declaration that the user wrote, which is a signature change of an existing declaration. The precedent in the
code base is the introduction of a parameter into a partial constructor, delivered for C# 14 in issue #1143, and
the same two constraints apply. The linker has to rewrite the case list of the existing declaration, and the
design-time pipeline cannot change the signature of an existing declaration, so the design-time result has to be
expressed the way partial constructors express theirs.

One point of the C# 15 grammar has to be settled before either half can be designed, and it was already recorded as
an open question of the code model analysis: whether every part of a partial union must repeat the case list, or
whether one part may carry it. The answer decides whether a leg can be added from a generated partial part at all,
or whether the linker must rewrite the part that the user wrote.

The four introduction issues #865, #866, #867 and #869 stay out of scope. Union introduction is no longer grouped
with them.

## 7. The user stories do not carry the application programming interface design

Stated by the product owner on 2026-09-04, and it applies to every story of this release.

A user story states the capability, the scope, the files and mechanisms it touches, and the acceptance criteria. It
does not specify the shape of the public application programming interface. The name and the parameters of a new
advice method, the members added to a builder, and the way a new concept is exposed on an interface are decided
when the story is implemented, not when it is written.

The analyses under `analysis-reports` may go further, because they exist to establish feasibility and size, and a
proposed shape is often the shortest way to show that something is possible. A proposed shape in an analysis is
evidence for the estimate. It is not a specification, and a story must not quote it as one.

This applies in particular to the union work of section 5c, where the natural temptation is to fix the signature of
an introduction method in the story text.

### 7b. Refinement: the interfaces are drafted, as material to think with

Stated on 2026-09-04, refining section 7 rather than reversing it. The application programming interfaces are to be
drafted now, because the concepts stay too abstract without them and a shape on the page is what makes a trade-off
arguable.

The distinction that section 7 draws is kept, and it is a distinction of authority rather than of content. A draft
is written to be criticised and replaced. It is illustrative material, it lives in the analyses under
`analysis-reports`, and a user story does not become a specification by citing one. What a story states remains the
capability, the scope and the acceptance criteria.

A draft is most useful when it is written the way an aspect author would meet it, which means the code an author
writes, the code that the author's users write, and the code that Metalama produces, rather than an interface
declaration on its own.

## 8. Roslyn 5.12 replaces the 5.10 variant, it is not added beside it

Question asked on 2026-09-04: is Roslyn 5.12 implemented in addition to Roslyn 5.10, or in replacement of it, that
is, is there a supported Visual Studio version sitting on Roslyn 5.10?

The answer is replacement. No supported Visual Studio presents Roslyn 5.10 or 5.11, and none ever will, because
those minor versions are not published.

### Why no host can present Roslyn 5.10

Roslyn publishes a stable package every third minor version. The evidence is the flat container index of
`Microsoft.CodeAnalysis.CSharp` on nuget.org, read on 2026-09-04, which serves exactly four versions in the 5
generation: 5.0.0 published 2025-11-18, 5.3.0 published 2026-03-10, 5.6.0 published 2026-07-02 and 5.9.0 published
2026-08-17. There is no 5.10 and no 5.11, and `eng/Versions.props` on the `main` branch of `dotnet/roslyn` reads
minor version 12, so the next published version is 5.12.

Roslyn minor versions track Visual Studio minor versions, which `platform-support.md` already states and which the
release branches corroborate: `release/dev18.0` and `release/dev18.3` are the only release branches, matching the
stable 5.0 and 5.3. The Visual Studio 2026 quarterly versions are therefore 18.0, 18.3, 18.6, 18.9 and 18.12, and
they carry Roslyn 5.0, 5.3, 5.6, 5.9 and 5.12. A version between two of those exists only as a build of `main`,
which is what the consumed `5.10.0-1.26365.3` is, and no Visual Studio ships one.

### The consequence for the variant set

Rule 8 of the doctrine says that a variant may exist only if it serves a host in the supported set. A variant whose
identity is 5.10.0 is loadable only by a host presenting Roslyn 5.10 or later, and once the latest variant is
renumbered to 5.12.0, the 5.10.0 identity serves no host that the 5.12.0 identity does not serve. Keeping it would
add a payload for an empty set.

The variant set of 2027.0 therefore stays at two: the `Roslyn.5.0.0` variant, which serves Rider and the Visual
Studio Code C# Dev Kit, and the latest variant, renumbered from 5.10.0 to 5.12.0, which serves the Visual Studio
2026 long-term servicing channel baseline, Visual Studio 2027 and `Metalama.Compiler`.

### Two corrections that follow

The Roslyn API section of `platform-support.md` says that the November 2026 long-term servicing channel baseline
carries "Roslyn 5.11 or thereabouts". Given the publication cadence above, the value is 5.12, and the sentence
should name it. Checklist item 1 of that document, which requires the Roslyn version of the baseline to be
measured after 2026-11-10, stands unchanged and remains the thing that settles it.

The variant table of the same section offers a row "5.10 or above" for the measured Rider and C# Dev Kit version.
Since 5.10 and 5.11 are not published, that row should read 5.12 or above.

### The caveat

This derivation is about Visual Studio. Rider does not take a published Roslyn package: it builds its own, reports
assembly version `42.42.42.42`, and carries the real version in an informational attribute, measured at 5.0.0 on
2026-09-01. A future Rider could in principle report a version that no Visual Studio presents. That would change
the lower bound of the variant set, which is what the release candidate measurement of checklist item 2 exists to
catch, and it would not change the conclusion above about the upper bound.

### 5d. How the requirement of section 5c is met, and one choice it exposes

The design analysis is in
[`analysis-reports/11-introducing-unions-design.md`](analysis-reports/11-introducing-unions-design.md). It settles
the grammar question and finds that the requirement is met in full for one form of union and only at build time for
the other. The choice between them is the product owner's.

#### The grammar question is settled

Exactly one part of a partial union carries the case list. A part without one parses and binds. A second part that
carries one is an error, CS8863, reported by the same code path that rejects two parameter lists on a partial
record. A union where no part carries one is an error, CS9370. No diagnostic compares case lists between parts,
and none can exist.

The consequence is that a generated partial part can never add a case to a union that the user declared with the
`union` keyword.

#### There are two kinds of union, and the distinction decides the design

A `union` declaration is one form. The other is any class or struct that carries
`System.Runtime.CompilerServices.UnionAttribute`, which the proposal describes as adapting an existing type to the
union patterns. Roslyn treats both as unions: `ITypeSymbol.IsUnion` is true for either, and the case set of the
second form is derived from the public single-parameter constructors, or from the static factory methods of a
nested member provider interface, rather than from a case list.

For a type carrying the attribute, adding a leg is the introduction of a constructor. That is ordinary member
introduction, it is expressible in a generated partial part, and the editor and the build therefore agree. None of
the restrictions of a union declaration applies, because Roslyn guards all of them behind a test of whether the
type is a union declaration.

For a `union` declaration, adding a leg means rewriting the case list of the part the user wrote. The build-time
half is small, because the linker already rewrites a type parameter list in the same field and the same method for
partial constructor parameter introduction. The design-time half is impossible: every route from a generated part
is closed by a compiler rule, so the editor cannot show the added case, and the editor and the build disagree about
conversions and about switch exhaustiveness.

#### The choice

The analysis recommends shipping both, the attribute form first because it is small and its design-time result is
correct, and the declaration form second with an explicit design-time diagnostic. If only one fits the release, it
recommends the attribute form, and it states plainly that the declaration form buys a capability that works at
build time only.

Estimated at six to nine days in total, of which about four can proceed before Roslyn 5.12 exists.

#### One consequence for section 5b

The earlier analysis said that union introduction was blocked by the mandatory case list and by the materialization
of the synthesized members. The first is confirmed for the declaration form and does not apply to the attribute
form. The second is answered differently than expected: the machinery of pull request #1879 does not generalise,
because a user may not declare the `Value` property or a case constructor at all, so there is no override to serve
and no body to reproduce. The precedent that does apply is the introduction of a namespace, which registers a
builder without an injection.

## 9. The drafted interfaces for the rest of C# 15

The drafts are in [`analysis-reports/12-csharp15-api-drafts.md`](analysis-reports/12-csharp15-api-drafts.md), and
they are material for discussion under section 7b, not a specification. Four of the five subjects came back smaller
than expected, and the fifth needs a decision.

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

The forbidden labeled break and continue of section 4 is one error descriptor reported on the label token from the
two visit methods of the template annotator. Run-time code that uses a label is unaffected, because the annotator
runs only under the guard that tests whether the code is inside a template.

The divergence of section 2b is drafted both ways, and the analysis recommends reporting rather than staying
silent: a design-time warning reported once per project, with an opt-out, from the diagnostic analyzer. The
decision is still open, and one measurement could change it, which is the Roslyn version that a current Rider
presents.

### 5e. Is the attribute form an implementation detail?

Question asked on 2026-09-04. The answer is no, and the proposal is explicit about it, but the question points at a
real usability problem that the recommendation of section 5d understates.

The unions proposal separates the two on purpose. It states that "any class or struct type with a
`System.Runtime.CompilerServices.UnionAttribute` attribute is considered a union type", and it gives the reason:
"the separation between union types and union declarations allows C# to have a succinct union declaration syntax
with opinionated semantics, while also allowing existing types or types with other implementation choices to opt
into union behaviors". The attribute is therefore a supported authoring form and an opt-in mechanism, not a marker
that the compiler reserves for its own lowering. It happens to be what a `union` declaration lowers to as well.

The usability problem is a different matter. A type carrying the attribute has no case list, so its cases are its
public single-parameter constructors, which the author writes by hand, along with the `Value` property. Compared
with `union Pet(Cat, Dog)`, that is a great deal of text. Telling a user that an aspect can add a leg only if they
abandon the concise syntax is a poor answer, and it is the same class of demand as requiring a type to be declared
`partial`, but much heavier.

The distinction that resolves it is who writes the type.

When the aspect introduces the whole union, nothing is imposed on anyone: the aspect emits the attribute form, the
user never writes it, and the design-time result is correct. This is the case where the attribute form is simply
the right choice.

When the user wrote `union Pet(Cat, Dog)` and an aspect adds a leg, the attribute form is not available without
asking the user to rewrite their declaration. Here the honest options are to support the operation at build time
only, with a design-time diagnostic saying that the editor cannot show the added case, or not to support it.

One risk that section 5d records bears on this and is worth repeating. The public Roslyn interface conflates the
two forms: `ITypeSymbol.IsUnion` is true for both, and the restrictions of a union declaration apply to only one of
them. An eligibility rule keyed on `IsUnion` alone would therefore reject advice that is legal on a type carrying
the attribute.
