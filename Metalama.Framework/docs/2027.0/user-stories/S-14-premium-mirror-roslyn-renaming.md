### S-14. Metalama.Premium: mirror the Roslyn 5.12 renaming

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Premium`
- Size: M
- Blocked by: S-13
- Findings: [PR-1](../07-premium.md)

---

metalama/Metalama.Premium#85, which closed the issue #1913, left Premium with a latest variant named 5.10.0, two
prerelease fallback literals in `Directory.Packages.props` and a `nuget.base.config` declaring the
`roslyn-consolidated` feed. The version string
appears in eleven tracked files, including the `InternalsVisibleTo` entries, the packaging paths and the
`MetalamaExtensionAssembly` items with their `TargetRoslynVersion` metadata.

#### Context

If the two repositories do not move in the same release, one of three things happens: Premium resolves a preview from
a feed that the core has removed, or Premium removes the feed while the exported `RoslynApiMaxVersion` still carries a
prerelease label, or Premium ships variant assemblies whose names no longer match the variant that the core loads,
which the extension loader drops without a diagnostic. This story is separate from S-13 only because a pull request
cannot span two repositories.

#### Scope

- Rename `eng/RoslynVersions/Roslyn.5.10.0.props` to the new version, set `ThisRoslynVersionNoPreview` accordingly
  and update `eng/RoslynVersions/Latest.props`.
- Change the two fallback literals `RoslynVersion` and `RoslynMaxVersion` in `Directory.Packages.props`.
- Update every remaining occurrence of the version string, which `git grep` finds in eleven tracked files: the
  `InternalsVisibleTo` entries of the CodeFixes and Validation projects, the `TfmSpecificPackageFile` paths of the
  two package projects, and the `MetalamaExtensionAssembly` and `MetalamaDesignTimeExtensionAssembly` items with
  their `TargetRoslynVersion` metadata in the four property files.
- Remove `nuget.base.config`, or keep it with its comment rewritten, according to whether the core still needs the
  prerelease source after S-13.

#### Acceptance criteria

- No file in `Metalama.Premium` names the retired variant version.
- Premium restores with no prerelease Roslyn package source.
- The variant assembly names and their `TargetRoslynVersion` metadata match the variant that the core payload loads,
  verified by a design-time run rather than by inspection.

— Claude for @gfraiteur
