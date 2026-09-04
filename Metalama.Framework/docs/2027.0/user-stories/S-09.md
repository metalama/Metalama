### S-09. Renumber the latest Roslyn variant to the stable 5.12 and regenerate the syntax model

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`, `Area-Framework`, `breaking`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: L
- Blocked by: S-01, and the Roslyn version measured by S-08
- Findings: [LV-12](../01-language-version-and-hosts.md), [LV-13](../01-language-version-and-hosts.md),
  [LV-14](../01-language-version-and-hosts.md), [TP-1](../02-syntax-generator-and-templates.md),
  [TP-9](../02-syntax-generator-and-templates.md), [DT-3](../05-design-time-workspaces-linqpad.md),
  [DT-8](../05-design-time-workspaces-linqpad.md)

---

`RoslynApiMaxVersion` and `RoslynMaxVersion` are `5.10.0-1.26365.3`, a build of the `main` branch of `dotnet/roslyn`
of 2026-07-15 restored from the `roslyn-consolidated` feed, and no stable 5.10 or 5.11 package exists or will exist.
Leaving the prerelease is therefore a renumbering of the latest variant to 5.12.0 and not the edit of a version
label. This story is one pull request because the pieces cannot be merged separately without breaking the build, and
it is the gate of the whole release.

#### Context

Section 8 of [`DECISIONS.md`](../DECISIONS.md) records that 5.12 replaces the 5.10 variant rather than being added
beside it, because no supported host presents Roslyn 5.10 or 5.11 and rule 8 of the doctrine forbids a variant that
serves an empty set. A version mismatch has two silent failure modes: `TargetedAssemblyReference` compares the
declared Roslyn version by equality, and `ExtensionLoaderBase` drops a non-matching extension assembly with no
diagnostic, which removes a pipeline stage rather than reporting an error. The regeneration is the second half:
`TreeReader.RemoveExperimentalDeclarations` strips every node carrying `ExperimentalUrl`, which is why no generated
visitor, version verifier or design-time hasher knows the union declaration, the with-element or the `Name` field of
`break` and `continue`. Three published packages depend on this story as a release gate, because they currently
declare a dependency on a Roslyn package that nuget.org does not serve, which already failed for a user in #1106.

The variant that this story renumbers was created by the closed issue #1881, and the `roslyn-consolidated` package
source that it removes was declared by the closed issue #1885, so this story supersedes both rather than merely
standing beside them. The failure reported in #1106 was the nested reference-assembly restore, which reported
`NU1102` for `Microsoft.CodeAnalysis.CSharp`, and not the declared dependency of a published package. The two share
one cause, which is a Roslyn version that nuget.org does not serve, and the second is what the pack-time check below
prevents.

The value 5.12 is derived from the publication cadence and not measured. Checklist item 1 of
[`platform-support.md`](../../platform-support.md), which S-08 performs after 2026-11-10, settles it, and this story
takes the measured value.

#### Scope

- Set the version strings of `Directory.Packages.props`, of the variant property file whose
  `ThisRoslynVersionNoPreview` and `DefineConstants` are written literally, and of
  `SupportedCSharpVersions.ToNuGetVersionString`, following steps 7 and 8 of
  [`updating-roslyn.md`](../../updating-roslyn.md).
- Insert the retired version name into the version list of `eng/src/GenerateMetaSyntaxRewriter` in version order and
  not at the end, because the enumeration values are positional indices and a compile-time project manifest already
  on disk carries the name.
- Rename the variant projects, the variant preprocessor symbol and every literal that names the variant, including
  `RoslynVariantPolicy` and its tests.
- Add the stable grammar as a new `Syntax-5.12.0.xml` rather than overwriting the previous file, and regenerate, so
  that the rewriters, the version verifier arms and the design-time hashers gain the union declaration, the
  with-element and the labeled `break` and `continue`.
- Add a guard that compares the local grammar file with the grammar of the exact `Microsoft.CodeAnalysis.CSharp`
  package that is consumed, rather than one keyed on a prerelease label, because the unsafe expression keeps its
  experimental marker and a label-based check would fail permanently.
- Verify that removing the prerelease label removes the `roslyn-consolidated` package source, which
  `SupportedCSharpVersions.ToNuGetVersionString` derives from the hyphen, and re-derive the per-variant
  `System.Text.Json` version from the stable package.
- Rewrite step 10 of [`updating-roslyn.md`](../../updating-roslyn.md), which names members that #1911 renamed, and split
  the add-a-variant list from the renumbering list.
- Add a pack-time check that refuses to publish a package pinning a prerelease Roslyn, subject to decision D-11.
- Add two design-time diff test cases for the newly generated hashers, which are a union rename and a change to the
  label of a `break` statement.

#### Acceptance criteria

- No file names the retired variant version, and both variants build and pass their tests.
- A compile-time project manifest written by the previous release still deserializes.
- An extension assembly declared for the previous variant version is either loaded or refused with a diagnostic, and
  never dropped in silence.
- `Metalama.Framework.Workspaces`, `Metalama.Testing.AspectTesting` and `Metalama.LinqPad` restore from nuget.org
  alone.
- The generated syntax rewriters, the version verifier and the design-time hashers cover the union declaration, the
  with-element and the `Name` field of `break` and `continue`.

#### Not in scope

This story does not add C# 15 to the supported language versions, which is S-11, and it does not mirror the
renumbering in `Metalama.Premium`, which is S-10. The open issue #875, which asks for a move to Roslyn 4.9, names a
version far below the current floor and is superseded by this story. It is closed as superseded when this story is
filed, rather than left to look like a duplicate.

— Claude for @gfraiteur
