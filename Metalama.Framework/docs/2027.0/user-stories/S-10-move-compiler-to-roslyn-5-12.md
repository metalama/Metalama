### S-10. Metalama.Compiler: move to Roslyn 5.12

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Compiler`
- Size: L
- Blocked by: nothing in this repository
- Findings: none from the theme documents. The scope below is derived from the tracker of
  `metalama/Metalama.Compiler`, principally the pull requests #210 and #202 and their issues #207 and #201, read on
  2026-09-04. The repository is not cloned in the session that produced this analysis, so no file of it was read.

---

`Metalama.Compiler` on `develop/2027.0` carries Roslyn `5.10.0-1.26365.3`, which is a build of an upstream release
branch and will never have a stable counterpart. Step 1 of [`updating-roslyn.md`](../../updating-roslyn.md) makes every
Roslyn move in `metalama/Metalama` conditional on `Metalama.Compiler` moving first, so the whole C# 15 plan of
2027.0 depends on a date that is decided in that repository. The move is not a rebase. It is a `git merge` of an
upstream Roslyn branch into `develop/2027.0`, and the two most recent such merges, pull requests #210 and #202,
measure what that costs.

#### Context

The two most recent Roslyn moves are the measurement. Pull request #210, merged on 2026-08-31, moved the repository
to Roslyn 5.10: 940 changed files, 245 commits, 39,912 added and 8,300 deleted lines, from a merge of 239 upstream
commits. Pull request #202, merged on 2026-08-12, moved it from Roslyn 5.6 to Roslyn 5.9 across roughly 14,100
files, and reported that 18,174 unit tests pass. Both were completed within one working day of the issue being
filed, so the elapsed time is short; the risk is not in the merge itself but in five places around it.

The first is the choice of the upstream branch. Issue #207 records that the branch table of
`docs-Metalama/Merging.md` was stale, because upstream rotates `release/stable` and `release/insiders` between
versions, and that the table has to be re-derived on every merge rather than trusted. Both merges rewrote that
document.

The second is the set of files that conflict, which is the set of divergences the fork carries. Pull requests #210
and #202 name them: `CommonCompiler.cs`, which holds the transformer order parsing, the
`ResolveAnalyzersFromArguments` overload and the `transformers` argument of `CompileAndEmit`;
`Syntax.xml.Main.Generated.cs`, which holds the Metalama `TreeTracker` hooks; `NullableWalker.cs`;
`DiagnosticHelper.cs` and `IDEDiagnosticIDConfigurationTests.cs`, which hold a `LAMA` guard; `Version.Details.props`
and `Version.Details.xml`; `Metalama.Compiler.slnf`; `Microsoft.CodeAnalysis.LanguageServer.csproj`; and the
external access unit test project that holds a `TypeForwards` reference. Every one of them is a place where a
Metalama change has to be re-applied inside a structure that upstream changed.

The third is package resolution. Pull request #202 reports 990 `NU1109` downgrade errors when the runtime and base
class library packages are not moved to the versions the new Roslyn floors, and #210 reports the same class of
failure for a single hardcoded entry of `eng/Packages.props`. Both merges also depend on the `roslyn-consolidated`
mirror serving the packages that upstream pins; #202 needed a namespace added to that feed, which is an
infrastructure request outside the repository.

The fourth is the build agent. Following upstream in `global.json` moves the .NET SDK band, which moves the agent
declaration in `eng-Metalama/src/Program.cs` and requires the container to be regenerated. Pull request #210 needed
two rounds of continuous integration for this reason alone: the desktop MSBuild of the agent could not host the
adopted SDK band, because that band declares a minimum MSBuild version above the one the installed build tools
provide, and the fix was a scoped `global.json` for the affected scenarios. The same rounds exposed a restore and
build skew between two SDK bands. Pull requests #211, #212 and #213 then removed the `net8.0` and `net9.0` target
frameworks, wrote the platform doctrine of that repository and moved its build image to Visual Studio 2026, so the
agent of the next merge is not the agent of #210.

The fifth is the .NET Framework test leg. Pull request #210 started with twenty failures in it, seventeen of them
caused by the merge, and the leg is not run by the same command as the rest of the suite. Its verification set is
also wider than a build and a test run: both merges required `eng/generate-compiler-code.cmd` to produce no diff,
all 71 project paths of `Metalama.Compiler.slnf` to resolve and to be present in `Roslyn.slnx`, and `Roslyn.slnx` to
be well-formed with all 365 projects present on disk.

One deliverable of this story is not a package. Step 4 of [`updating-roslyn.md`](../../updating-roslyn.md) requires the
grammar file `src/Compilers/CSharp/Portable/Syntax/Syntax.xml` to be copied into `metalama/Metalama` from the
`Metalama.Compiler` branch that targets the consumed Roslyn version, because that is the grammar of the build that
is actually consumed. S-13 regenerates the syntax model from it, so S-13 needs a named branch and commit and not
only a package version.

Two properties of the previous merges must be confirmed rather than assumed for this one. Both were a single hop,
because the merge base was the upstream parent of the previous merge, and both took a preview band deliberately:
Roslyn 5.10 was consumed because it ships in a .NET 11 preview software development kit. Roslyn 5.12 is expected to
be a stable release, and the branch that produces it has to be derived again. No issue for this merge exists in
`metalama/Metalama.Compiler` as of 2026-09-04, so this story files the first one.

#### Scope

- Derive the upstream branch that produces the Roslyn version the November 2026 hosts carry, and correct the branch
  table of `docs-Metalama/Merging.md` in the same change, following issue #207.
- Merge that branch into `develop/2027.0`, report the merge base and the number of upstream commits, and re-apply
  the Metalama divergences in the conflicting files named in the Context section.
- Move the runtime and base class library package versions to the ones the new Roslyn floors, so that no `NU1109`
  downgrade is reported, and confirm that every version upstream pins is served by the `roslyn-consolidated` mirror,
  requesting the missing ones rather than pinning older versions.
- Follow upstream in `global.json`, move the agent declaration of `eng-Metalama/src/Program.cs` with it, regenerate
  the container, and confirm that the desktop MSBuild of the agent can host the adopted software development kit
  band.
- Run the .NET Framework test leg as well as the .NET leg, and account for every failure as caused by the merge or
  pre-existing.
- Set `RoslynVersion` in `eng/Versions.props`, publish the resulting `Metalama.Compiler` version, and record it in
  `eng/AutoUpdatedVersions.props` of `metalama/Metalama`, where `MetalamaCompilerVersion` is currently `2027.0.0`.
- Name the branch and the commit of `Metalama.Compiler` from which `src/Compilers/CSharp/Portable/Syntax/Syntax.xml`
  is to be copied, which is what step 4 of [`updating-roslyn.md`](../../updating-roslyn.md) requires and what S-13
  consumes.
- Report the date at which the merge is expected to be complete, because the schedule of S-13, S-15 and every story
  downstream of them is derived from it.

#### Acceptance criteria

- `Metalama.Compiler` builds and passes its .NET tests and its .NET Framework tests against the stable Roslyn that
  the November 2026 hosts carry, and every remaining failure of the .NET Framework leg is shown to fail identically
  before the merge.
- `eng/generate-compiler-code.cmd` produces no diff.
- Every project path of `Metalama.Compiler.slnf` resolves and is present in `Roslyn.slnx`, and every project of
  `Roslyn.slnx` is present on disk.
- A solution-wide restore reports no NuGet error, and in particular no `NU1109`.
- The continuous integration build is green on the regenerated container.
- The branch table of `docs-Metalama/Merging.md` names the branch that was actually merged, and the date on which
  the table was derived.
- The published `Metalama.Compiler` version pins a Roslyn package that nuget.org serves.
- `metalama/Metalama` can raise `RoslynApiMaxVersion` and `RoslynMaxVersion` to that version without any prerelease
  package source.
- The story names the branch and the commit from which S-13 copies the grammar file.

#### Not in scope

This story does not edit `metalama/Metalama` or `metalama/Metalama.Premium`. Those edits are stories S-13 and S-14.
It does not correct the .NET Framework MSBuild bridge of `build/Metalama.Compiler.props`, which probes the shared
framework directory for `10.*` only; `platform-support.md:347-349` records that drift point and pull request #211
left it out of scope deliberately. It does not change the target frameworks of that repository, which pull request
#211 already reduced to `net10.0` and `net472`.

— Claude for @gfraiteur

— Claude for @gfraiteur
