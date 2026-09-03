# Issue 1757 — warnings when a solution mixes Metalama versions

## What this solution builds

| Project | Metalama | Referenced by `Consumer` as | Expected diagnostic |
|---|---|---|---|
| `OldAspects` | 2025.1.18, from nuget.org | `ProjectReference` | **LAMA0078** |
| `MidAspects` | 2026.1.20, from nuget.org | `ProjectReference` | **LAMA0081** |
| `Consumer` | the version built by this repository | — | — |

`LAMA0078` is the specific case: 2025.1.18 belongs to the previous generation of the design-time contracts, the one
that #1605 broke, so the IDE cannot consume the reference at all. `LAMA0081` is the general case: 2026.1.20 is the
same generation as the current version but not the same version, which works but is slower and invites trouble. The
specific warning replaces the general one when both apply.

Both are warnings, never errors, because **the build genuinely is not affected**: the compile-time pipeline reads
the compile-time project embedded in the reference and applies its aspects, whatever version produced it. Only the
IDE experience degrades.

## Why both references are ProjectReferences

The warnings are reported for a project reference and never for a package. Consuming a package built with another
version of Metalama is normal and works identically at build time and in the IDE, because a package is consumed
statically through its embedded compile-time project. Two projects of one solution on two versions is a different
matter: it is a configuration the user controls and can fix.

`ProjectReference` is a design-time concept, so by the time the compiler runs every reference is a
`PortableExecutableReference` and the distinction is lost. MSBuild supplies it through the
`MetalamaProjectReferenceNames` property, built from `%(ReferenceSourceTarget)`.

`DesignTimeStandalone/Issue1749.FrameworkVersions` is the negative control: it wires an assembly built by Metalama
2025.1.18 as a **file** reference, and must therefore produce neither warning.

## The trap this scenario exists to catch

**The reference must be matched by assembly name, not by path.** A path comparison fails for every project
reference and therefore reports nothing at all: when the referenced project produces a reference assembly, which is
the default, the compiler is given the assembly under `obj/<config>/<tfm>/ref/` while `@(ReferencePath)` still holds
the one under `bin/<config>/<tfm>/`. The first implementation compared paths, built and passed, and silently warned
about nothing; only this scenario caught it.

Names are also immune to casing, normalization, `..` segments and link resolution, and cannot contain the `,` that
separates the list items, so nothing has to be escaped.

## How it is asserted

`test.json` requires both warnings to appear, so a regression in either the MSBuild export or the reporting fails
the build:

```json
{
    "BuildOnly": true,
    "ExpectedDiagnosticsRegexes": [
        "warning LAMA0078.*OldAspects.*2025\\.1\\.18",
        "warning LAMA0081.*MidAspects.*2026\\.1\\.20"
    ]
}
```

`nuget.config` clears the repository's package source mapping, which otherwise routes `Metalama.*` to the local
feed and makes the publicly released versions unresolvable. `OldAspects` pins `LangVersion` because the
`Metalama.Compiler` shipping with 2025.1.18 predates the language version the current SDK defaults to.
It pins `MetalamaTemplateLanguageVersion` for the same kind of reason: 2025.1.18 supports C# 13 at most, so it
rejects with LAMA0052 the value that the repository sets in its own `Directory.Build.props`.

## Reading Metalama's trace while investigating this

All three of these are required; omitting any one produces no output at all.

```powershell
$env:METALAMA_CONSOLE_TRACE="*"
dotnet build Consumer\Consumer.csproj -t:rebuild --disable-build-servers -v:detailed
```
