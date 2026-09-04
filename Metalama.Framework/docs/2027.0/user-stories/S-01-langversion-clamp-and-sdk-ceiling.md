### S-01. Build: a `net11.0` project is silently compiled as C# 12, and every .NET 11 software development kit is reported as unsupported

- Issue type: Bug
- Labels: `bug`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: nothing
- Findings: [LV-1](../01-language-version-and-hosts.md), [UT-2](../06-user-tfm-patterns-tests-docs.md)

---

`Metalama.Framework/src/Metalama.Framework.Package/build/Metalama.Framework.targets:118-121` rewrites an implicitly
set `LangVersion` that is not one of `12.0`, `13.0`, `14.0`, `default`, `latest`, `latestMajor` or `preview` down to
`12.0`, and the warning at `:243-248` carries no `Code` attribute and explains the rewrite falsely. Separately,
`_MetalamaSdkVersion` at `:399` strips only the prerelease label and is then compared with `VersionGreaterThan`
against `MaximumSdkVersion` `11.0` at `:412`, so every .NET 11 SDK, including `11.0.100`, reports `LAMA0601`
although `Metalama.Framework.props:33` declares .NET 11 supported and `:39` names 10.0 and 11.0 as the supported
versions. The second defect is live today.

#### Context

Section 6b of [`DECISIONS.md`](../DECISIONS.md) removes the build container work from the release and names these two
defects as what remains: both are properties of a comparison and are verified without an installed .NET 11 SDK. The
clamp is not yet reachable, because the compiler toolset caps the implied version of a `net11.0` project at `14.0`
until the Roslyn of S-13; it becomes reachable on the day that cap moves to `15.0`, and it then costs a project three
language versions at once, because a project that implied `15.0` drops to `12.0`. The ceiling defect is reachable
now: `MaximumSdkVersion` is documented as the last supported major and minor line, and comparing it against a full
version makes every feature band of that line exceed it. `MinimumSdkVersion` legitimately keeps feature-band
precision, because a contributing package may require `10.0.200`, so the two rules cannot share one property.

Two issues already cover part of this work. The open issue #714 asks for three things: a warning when Metalama is
used with an unsupported target framework, no silent downgrade of `LangVersion` without a warning, and the same
language version at design time and at build time. The first was delivered by the closed issue #1884, which also
introduced the `MaximumSdkVersion` comparison that this story corrects, and the second is what the diagnostic code
and the rewritten warning text deliver here. This story therefore references #714, and #714 is closed or reduced to
its third point when this story is done. The closed issue #1894 is the neighbour that removed the temporary
`MetalamaCheckSupportedPlatform` property of this repository, so a build of this repository now exercises the same
check.

#### Scope

- Add a second property holding the first two components of `$(NETCoreSdkVersion)` and use it in the maximum rule at
  `Metalama.Framework.targets:412` only, leaving the minimum rule at `:406-408` on the full version, with a comment
  stating why the two differ.
- Give the `MetalamaCheckLangVersion` warning a `Code` attribute, allocated from the `LAMA06xx` platform range beside
  `LAMA0600` to `LAMA0602`, and rewrite its text so that it describes the rewrite that actually happened.
- State the suppression mechanism correctly: an MSBuild task warning is suppressed by `MSBuildWarningsAsMessages` and
  not by `NoWarn`.
- Extend the accepted value list of the clamp condition when `LangMaxVersion` moves, which is S-15, and reference
  that story from the comment so the two lists do not drift.
- Add unit tests or a standalone scenario that exercises both comparisons without an installed .NET 11 SDK, following
  section 6b of [`DECISIONS.md`](../DECISIONS.md).

#### Acceptance criteria

- A build with a .NET SDK of the `11.0` line reports no `LAMA0601`, and a build with a `12.0` SDK still reports it.
- A build with a .NET SDK of `10.0.100` still reports no warning, and one with `9.0.100` still reports `LAMA0601`.
- The language version warning carries a code, and adding that code to `MSBuildWarningsAsMessages` suppresses it.
- The warning text states the version the project had and the version it was given, and no sentence of it is false.

#### Not in scope

This story does not install a .NET 11 SDK in the build container and does not add a `net11.0` scenario. Sections 6b
and 6c of [`DECISIONS.md`](../DECISIONS.md) exclude both.

— Claude for @gfraiteur
