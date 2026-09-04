### S-13. Build: rename the latest Roslyn variant to 5.12 and regenerate the syntax model

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`, `Area-Framework`, `breaking`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: L
- Blocked by: S-10, and the Roslyn version measured by S-11
- Findings: [LV-12](../01-language-version-and-hosts.md), [LV-13](../01-language-version-and-hosts.md),
  [LV-14](../01-language-version-and-hosts.md), [TP-1](../02-syntax-generator-and-templates.md),
  [TP-9](../02-syntax-generator-and-templates.md), [DT-3](../05-design-time-workspaces-linqpad.md),
  [DT-8](../05-design-time-workspaces-linqpad.md), [CM-10](../03-code-model-unions-closed.md)

---

The latest Roslyn variant is renamed from 5.10 to 5.12 in the source code, and the C# 15 Roslyn members are reached
through conditional compilation in that variant. Those are the two halves of this story. The rename touches the
variant property file, the variant project names, the variant preprocessor symbol and every literal that spells the
version, and it replaces the prerelease pin `5.10.0-1.26365.3` of `RoslynApiMaxVersion` and `RoslynMaxVersion` by
the stable package that the November 2026 hosts carry. The conditional compilation is the mechanism that section 2
of [`DECISIONS.md`](../DECISIONS.md) settles: the sources that name the C# 15 Roslyn members are compiled in the
latest variant alone. The two halves are one story because they cannot be merged separately without breaking the
build: the variant symbol that guards the C# 15 sources is named after the variant, so it is renamed by the same
change that introduces it. This story is the gate of the whole release.

#### Context

The version that this story leaves is a real one. Section 8b of [`DECISIONS.md`](../DECISIONS.md) records that
`5.10.0-1.26365.3` is built from the `release/stable` branch of `dotnet/roslyn`, whose `PreReleaseVersionLabel` is 1,
so Roslyn 5.10 is the current stable-track version and not a discarded intermediate state. What the repository
consumes is a prerelease build of it restored from the `roslyn-consolidated` feed, and what this story adopts is the
stable package that nuget.org serves.

Which version the variant binds against is decided by a criterion and by a measurement, not by the publication
cadence. Section 8c of [`DECISIONS.md`](../DECISIONS.md) states the criterion: the latest variant must bind against a
version no higher than the lowest Roslyn that offers C# 15 among the hosts in support on 2027-01-31, which is 5.11 if
a supported host presents 5.11 and 5.12 otherwise. Roslyn 5.10 offers every C# 15 feature under
`LanguageVersion.Preview` alone and still marks the union declaration and the with element experimental in the
grammar, so a host on 5.10 imposes no C# 15 requirement. Section 14.1 sharpens the criterion further: a new
enumeration member such as `SyntaxKind.ClosedKeyword` is a new Roslyn application programming interface, so a feature
that adds no syntax node may still require a build against a newer Roslyn. The measurement that settles the value is
checklist item 1 of [`platform-support.md`](../../platform-support.md), which S-11 performs after 2026-11-10, and this
story takes the measured value. The title names 5.12 because that is the expected outcome.

Section 8 of [`DECISIONS.md`](../DECISIONS.md) records that the new version replaces the 5.10 variant rather than
being added beside it, so the variant set stays at two. A version mismatch has two silent failure modes:
`TargetedAssemblyReference` compares the declared Roslyn version by equality, and `ExtensionLoaderBase` drops a
non-matching extension assembly with no diagnostic, which removes a pipeline stage rather than reporting an error.
The regeneration is the second half of the rename: `TreeReader.RemoveExperimentalDeclarations` strips every node
carrying `ExperimentalUrl`, which is why no generated visitor, version verifier or design-time hasher knows the union
declaration, the with-element or the `Name` field of `break` and `continue`. Three published packages depend on this
story as a release gate, because they currently declare a dependency on a Roslyn package that nuget.org does not
serve, which already failed for a user in #1106.

The variant that this story renames was created by the closed issue #1881, and the `roslyn-consolidated` package
source that it removes was declared by the closed issue #1885, so this story supersedes both rather than merely
standing beside them. The failure reported in #1106 was the nested reference-assembly restore, which reported
`NU1102` for `Microsoft.CodeAnalysis.CSharp`, and not the declared dependency of a published package. The two share
one cause, which is a Roslyn version that nuget.org does not serve, and the second is what the pack-time check below
prevents.

The conditional compilation half has its own history. Issue #1881 removed 177 `#if ROSLYN_*` blocks from 152
production files and wrote the note, in both variant property files, that no production source branches on the
variant. Section 2 of [`DECISIONS.md`](../DECISIONS.md) supersedes that note for the C# 15 members, and it rejects the
two alternatives that were considered, which are numeric syntax kind values with a run-time guard and a per-variant
service that reads the members by reflection; the second repeats what #1215 deliberately removed. The decision is
narrow: it covers the members that Roslyn 5.0 does not have, and it does not reopen the general policy for anything
else. On the lower variant each gated site reports the value that an ordinary type would report, and whether it also
reports a diagnostic is decision D-3.

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
  the add-a-variant list from the renaming list.
- Add a pack-time check that refuses to publish a package pinning a prerelease Roslyn, subject to decision D-11.
- Add two design-time diff test cases for the newly generated hashers, which are a union rename and a change to the
  label of a `break` statement.
- Rewrite the note in `eng/RoslynVersions/Roslyn.5.0.0.props` and in the latest variant property file, and the
  corresponding paragraph of [`Directory.Packages.md`](../../../../Directory.Packages.md), so that they state the
  policy that is in force after this story: production source may branch on the latest variant symbol, and only for
  members that the lower variant does not expose.
- State in the same place which members are covered, namely `UnionDeclarationSyntax`, `SyntaxKind.UnionDeclaration`,
  `SyntaxKind.ClosedKeyword`, `ITypeSymbol.IsUnion`, `ITypeSymbol.UnionCaseTypes`, `ITypeSymbol.IsClosed` and the
  `Name` field of `BreakStatementSyntax` and `ContinueStatementSyntax`, and that the list is closed rather than a
  precedent for new branches.
- Deliver one worked example in the smallest consumer, so that the pattern is visible in the code rather than only in
  a document.
- State whether the public `Metalama.Framework.Sdk` kind helpers, which are part of the extensibility surface, may
  name the new kinds at all, since a public surface cannot easily be narrowed later.
- Settle the suppression of `RSEXPERIMENTAL006`: it is required while the latest variant is built against a Roslyn
  that still marks the union and closed members experimental, and it disappears when the variant binds against the
  version that this story adopts.
- Reference the open issue #1217, which asks for `Metalama.Extensions.Metrics` to support several Roslyn versions,
  and state whether the policy written here applies to that package or leaves it outside the closed list.

#### Acceptance criteria

- No file names the retired variant version, and both variants build and pass their tests.
- A compile-time project manifest written by the previous release still deserializes.
- An extension assembly declared for the previous variant version is either loaded or refused with a diagnostic, and
  never dropped in silence.
- `Metalama.Framework.Workspaces`, `Metalama.Testing.AspectTesting` and `Metalama.LinqPad` restore from nuget.org
  alone.
- The generated syntax rewriters, the version verifier and the design-time hashers cover the union declaration, the
  with-element and the `Name` field of `break` and `continue`.
- Both variant property files and [`Directory.Packages.md`](../../../../Directory.Packages.md) describe the policy that
  is actually in force, and no document still states that production source carries no variant branch.
- One production source file compiles a C# 15 Roslyn member behind the variant symbol, and both variants build.
- The list of members that may be gated is written down, and the rule for adding to it is written down.

#### Not in scope

This story does not add C# 15 to the supported language versions, which is S-15, and it does not mirror the
renaming in `Metalama.Premium`, which is S-14. It does not deliver the union and closed features themselves: it
delivers the conditional compilation mechanism and one worked example, and the features are S-16, S-18 and S-28. The open issue #875, which asks for a move to Roslyn 4.9, names a
version far below the current floor and is superseded by this story. It is closed as superseded when this story is
filed, rather than left to look like a duplicate.

— Claude for @gfraiteur
