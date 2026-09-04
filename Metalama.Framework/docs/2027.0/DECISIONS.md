# Decisions taken by the product owner on 2026-09-04

These answers are settled, except where a section says the question is open. The user stories are written against them, and a document that still presents one of
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
