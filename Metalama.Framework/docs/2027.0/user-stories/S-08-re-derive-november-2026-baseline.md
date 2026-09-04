### S-08. Re-derive the November 2026 baseline: Visual Studio build tools, MSBuild and the host-capped pins

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: M
- Blocked by: the November 2026 releases, that is 2026-11-10
- Findings: [LV-11](../01-language-version-and-hosts.md), [UT-11](../06-user-tfm-patterns-tests-docs.md),
  [UT-12](../06-user-tfm-patterns-tests-docs.md), [UT-13](../06-user-tfm-patterns-tests-docs.md)

---

`eng/src/Program.cs:40` pins `VisualStudioBuildToolsComponentVersion.v18_9_2`, which is a quarterly release below the
November 2026 long-term servicing floor that PB-2027.0 names, and `MSBuildVersion` at `:63` must keep matching it.
`Metalama.Framework.props:34-37` and [`Directory.Packages.md`](../../../../Directory.Packages.md) both schedule a
re-reading of the Visual Studio floor, the feature band and the host-capped package pins after 2026-11-10. Four
findings from three themes wait on that one measurement, and grouping them avoids four separate reopenings of the
same two documents.

#### Context

The build tools pin has a history in two issues and one pull request. #1902 declined an earlier attempt to move it,
on the grounds that regenerating the Visual Studio base images is expensive, and it recorded that the component
version must exist in PostSharp.Engineering before the change can be made at all. The merged pull request #1919 then
made that move, pinning the build tools to 18.9.2 and `MSBuildVersion` to 18.9, so this story raises values that
#1919 set rather than values that have never moved. The obligation to re-read the host-capped package pins after
2026-11-10 was recorded by the closed issue #1897, whose Premium mirror is the open pull request
metalama/Metalama.Premium#84. The measurement itself is checklist item 1 of
[`platform-support.md`](../../platform-support.md). Two of the four items are comment corrections rather than version
changes: the `Microsoft.NET.Test.Sdk` pin carries a comment that still names Visual Studio 2022 as the lowest
supported host, and `Microsoft.Build` is pinned at the lowest supported host by doctrine and needs verification
rather than a bump.

#### Scope

- Measure the Visual Studio version, the .NET SDK feature band, the Roslyn version and the private runtime of the
  November 2026 long-term servicing channel and of Visual Studio 2027, per checklist item 1 of
  [`platform-support.md`](../../platform-support.md).
- Raise `VisualStudioBuildToolsComponentVersion` and `MSBuildVersion` in `eng/src/Program.cs` together, once
  PostSharp.Engineering exposes the newer component version, which is an external prerequisite.
- Re-derive the `Microsoft.NET.Test.Sdk` pin against the measured floor and rewrite its comment, which states the
  right rule and the wrong value.
- Verify, rather than raise, the `Microsoft.Build` pin, which doctrine keeps at the lowest supported host, and
  correct the parenthetical of [`Directory.Packages.md`](../../../../Directory.Packages.md) that states the frozen
  assembly version for the 17 line only.
- Re-run the vulnerability audit, remove the audit suppressions whose cause the Roslyn floor move removed, and
  correct the package version comments that name a dropped target framework.
- Restate the audit rule correctly where it is described: `NuGetAuditMode` defaults to `direct` except for .NET 10
  and later target frameworks, where it defaults to `all`.

#### Acceptance criteria

- The build container names a Visual Studio version that PB-2027.0 lists as supported, and `MSBuildVersion` matches
  the installed build tools.
- Every package pin whose comment names a measured host names the host that was actually measured.
- No audit suppression remains whose cause has been removed, and the audit reports nothing new.
- [`platform-support.md`](../../platform-support.md) records the measurement and the date it was taken.

#### Not in scope

This story does not rewrite the audience paragraph of [`Directory.Packages.md`](../../../../Directory.Packages.md),
which #1903 owns and which is referenced rather than rewritten. It does not change the .NET SDK component of the
container, which section 6b of [`DECISIONS.md`](../DECISIONS.md) excludes.

— Claude for @gfraiteur
