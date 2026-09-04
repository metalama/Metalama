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
