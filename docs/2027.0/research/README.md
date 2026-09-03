# Research corpus for the .NET 11 and C# 15 analysis

These files are generated reference material, not deliverables. They were produced on 2026-09-03 by an
automated research pass and preserved here because the working directory that held them is under `%TEMP%`
and is not durable. Delete the directory once the user stories derived from it have been filed.

## The two consolidated documents

| File | Contents |
| --- | --- |
| `DIGEST.md` | What .NET 11, C# 15 and Roslyn 5.10 to 5.12 change. Every claim is verified against a primary source, chiefly the `dotnet/roslyn`, `dotnet/runtime`, `dotnet/sdk` and `dotnet/csharplang` repositories and `learn.microsoft.com`. Section 8 records the contradictions found between sources and how each was resolved. Section 9 records 40 questions that the published sources do not answer. |
| `TERRAIN.md` | Where the Metalama source tree is sensitive to the shape of the C# language and to platform versions. A table of 308 hotspots with file paths and line numbers, a section for each of fourteen subsystems, a trace of how each kind of language addition propagates from the grammar to the tests, a trace of how each platform axis propagates, and a list of the places that fail silently. |

`critique.md` is the completeness critique that drove the second research round. The `gap-*.md` files are that
round. The remaining files are the per-topic notes that `DIGEST.md` and `TERRAIN.md` consolidate; read one
only when the consolidated document is not specific enough.

## The finding that orders the work

The Roslyn fork that `Metalama.Compiler` has merged, and that this repository references through
`RoslynApiMaxVersion`, does not declare `LanguageVersion.CSharp15`.

| Branch | Roslyn version | Declares `CSharp15` |
| --- | --- | --- |
| `dotnet/roslyn` `main` | 5.12 | Yes, `CSharp15 = 1500` |
| `dotnet/roslyn` `release/stable` | 5.10 | No |
| `dotnet/roslyn` `release/dev18.3` | 5.3 | No |
| `metalama/Metalama.Compiler` `topic/2027.0/207-merge-roslyn-5.10` | 5.10 | No |

No C# 15 feature can be supported until `Metalama.Compiler` merges a Roslyn build that declares it. That
merge is therefore the root dependency of the whole language wave, and it belongs to a third repository.

The consequence of not doing it is not that C# 15 is unavailable. The .NET 11 software development kit
makes C# 15 the default language version for a `net11.0` project, so a user who upgrades meets C# 15
constructs whether or not Metalama understands them. `DIGEST.md` records this as RES-13.
