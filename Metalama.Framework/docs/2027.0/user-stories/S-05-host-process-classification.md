### S-05. Design time: host process classification

- Issue type: Bug
- Labels: `bug`, `Area-Framework`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: nothing
- Findings: none. No theme document of this analysis examined host process detection. This story closes the second
  paragraph of the completeness review, which reported that the two copies of
  the detection have diverged on the Visual Studio Code C# Dev Kit.

---

Two files classify the current process by its name.
`Metalama.Backstage/src/Metalama.Backstage/Utilities/ProcessUtilities.cs:34-139` matches fifteen process names in a
switch statement and a sixteenth by prefix, and returns one of the seventeen members of
`Metalama.Backstage.Diagnostics.ProcessKind`.
`Metalama.Framework/src/Metalama.Framework.CompilerExtensions/ProcessKindHelper.cs:14-59` matches seven process
names and returns one of the six members of `Metalama.Framework.CompilerExtensions.ProcessKind`, declared at
`:62-70`. Each file carries a comment telling the reader that the other copy exists and that every change must be
made in both:
`ProcessKindHelper.cs:16-17` and `ProcessUtilities.cs:36-37`. The two tables no longer agree. The first classifies
`microsoft.codeanalysis.languageserver` at `:78-80`, which is the language server of the Visual Studio Code C# Dev
Kit; the second has no such case and no such enumeration member, so that process reaches the default arm at
`ProcessKindHelper.cs:56-57` and is reported as `ProcessKind.Other`. The C# Dev Kit is one of the design-time host
axes of PB-2027.0.

#### Context

Eleven classifications exist in the Backstage copy and in no other. They are `servicehub.host` carrying
`$codelensservice$` in its command line at `ProcessUtilities.cs:51-65`, `visualstudio` at `:67-68`,
`resharpertestrunner` and `resharpertestrunner64` at `:74-76`, `microsoft.codeanalysis.languageserver` and
`microsoft.visualstudio.code.languageserver` at `:78-80`, `msbuild` at `:82-83`, `testhost` at `:85-86`, the four
command line tests under the `dotnet` process name at `:102-117`, and a process name beginning with `linqpad` at
`:130-133`. The two copies agree on `devenv`, on the three names of the Roslyn analysis process, on `csc` and
`vbcscompiler`, and on the three command line tests that both perform under `dotnet`.

The consequence at run time is smaller than the divergence suggests, and stating it precisely is what keeps this
story at size S. `ProcessKindHelper.CurrentProcessKind` is read at six places. Five of them select the design-time
entry point that is instantiated: `MetalamaSourceGenerator.cs:25-59`, `MetalamaDiagnosticAnalyzer.cs:24-57`,
`MetalamaDiagnosticSuppressor.cs:21-50`, `MetalamaGeneratedCodeAnalyzer.cs:22-34`, and, in the assembly
`Metalama.Framework.EditorExtensions`, `MetalamaCodeFixProvider.cs:24-57` and
`MetalamaCodeRefactoringProvider.cs:22-55`. In every one of them the C# Dev Kit falls to the default arm, and the
default arm selects the same entry point that an explicit arm for the language server would select today.
`ResourceExtractor.cs:580-581` is the only place that takes a decision rather than writes a message from this
classification, and it tests `ProcessKind.DevEnv or ProcessKind.Rider`, which the language server does not match
under either table. No entry point is therefore wrong today.

Two consequences are real. The first is that the troubleshooting report and the exception text of
`ResourceExtractor.cs:202`, `:275` and `:402` name the host as `Other` for the C# Dev Kit, for Visual Studio Code
with OmniSharp, for LinqPad, for the test host, for an MSBuild node, for a ReSharper test runner and for the Code
Lens service, while the Backstage log of the same process names each of them correctly. A support report from a C#
Dev Kit user therefore does not say which host produced it. The second is that no arm that must treat the C# Dev
Kit differently can be written in `ProcessKindHelper` at all, because the enumeration has no member for it. Section 6 of [`DECISIONS.md`](../DECISIONS.md) and question [Q2](../OPEN-QUESTIONS.md) name Rider and the C# Dev Kit as exactly
the two hosts on which the design-time result and the build-time result diverge under the Roslyn 5.0 variant, and
`Metalama.Framework.EditorExtensions` is compiled once for every Roslyn variant against `RoslynApiMinVersion`. That
assembly can distinguish Rider today, at `MetalamaCodeFixProvider.cs:42-48`, and cannot distinguish the C# Dev Kit.

The failure shape is on record. Issue #1463 reports that Visual Studio 2026 runs the Roslyn analysis service in a
process named `DevHub.exe` instead of `ServiceHub.RoslynCodeAnalysisService.exe`, that the name was missing from
both copies at once, and that the fix had to add it to both. The pull request that closed it is #1465. The two
halves of this story, reconciling the tables and classifying the C# Dev Kit, are the same edit for the same reason,
which is why they are one story and not two.

Two further lists of process names exist, and one of them has the same omission. `ProcessManagerBase.cs:18-30`
declares the processes that the process manager may stop. It names `servicehub.roslyncodeanalysisservice`, the two
JetBrains workers and `omnisharp`, and it names neither `devhub` nor `microsoft.codeanalysis.languageserver`, so
the Visual Studio 2026 analysis process and the C# Dev Kit language server are not stopped even when they hold a
Metalama assembly. `ServiceHubClientEndpoint.cs:59-72` matches parent process names in order to find the Visual
Studio user process, with one arm for Visual Studio 2022 at `:59-64` and one for Visual Studio 2026 at `:67-70`;
`platform-support.md:134` states that 2027.0 does not support Visual Studio 2022, so the first arm serves no
supported host.

The duplication is removed rather than pinned by a test, and the mechanism for removing it is already in the
repository. The comment at `Metalama.Framework.CompilerExtensions.csproj:23-25` states that the project can have no
reference at all, because `Metalama.Backstage` is one of the assemblies that it embeds and extracts. The obstacle
is therefore the assembly reference and not the source. `Metalama.Framework/Directory.Build.props:21` defines
`MetalamaSharedThreadingSourceDirectory`, and `Metalama.Framework.CompilerExtensions.csproj:26-33` and
`Metalama.Framework.DesignTime.Contracts.csproj:20-27` already compile eight source files of `Metalama.Backstage`
through linked `Compile` items, for exactly this reason. A test that compares the two switch statements would
report the next divergence after it happened; a shared source file prevents it.

#### Scope

- Move the process name table out of `ProcessUtilities.GetProcessKind` and `ProcessKindHelper.GetProcessKind` into
  one source file that both assemblies compile, following the linked `Compile` item pattern of
  `Metalama.Framework.CompilerExtensions.csproj:26-33` and the `MetalamaSharedThreadingSourceDirectory` property of
  `Metalama.Framework/Directory.Build.props:21`.
- Keep the two assemblies from declaring a type of the same full name, because `ResourceExtractor` extracts and
  loads `Metalama.Backstage` into the same process that already contains `Metalama.Framework.CompilerExtensions`.
- Make the process name and the command line parameters of the classification, so that the table can be exercised
  by a test without the process existing, and keep the caching of the result for the current process, which
  `ProcessUtilities.cs:22-29` explains is required because a parent process may end first.
- Give `Metalama.Framework.CompilerExtensions.ProcessKind` the members that the shared table returns, and give the
  entry point switches of `MetalamaSourceGenerator.cs`, `MetalamaDiagnosticAnalyzer.cs`,
  `MetalamaDiagnosticSuppressor.cs`, `MetalamaGeneratedCodeAnalyzer.cs`, `MetalamaCodeFixProvider.cs` and
  `MetalamaCodeRefactoringProvider.cs` an explicit arm for the C# Dev Kit language server, rather than leaving it in
  the default arm.
- Add `devhub` and `microsoft.codeanalysis.languageserver` to `ProcessManagerBase._processesToKill` at
  `ProcessManagerBase.cs:18-30`, with the display names that the neighbouring entries use.
- Record, for each arm of the shared table, which host of PB-2027.0 it serves, and remove the Visual Studio 2022
  arm of `ServiceHubClientEndpoint.cs:59-64` and the `visualstudio` arm that returns `ProcessKind.VisualStudioMac`,
  or state in a comment why each is kept.
- Add the process name of the Roslyn analysis process and of the C# Dev Kit language server to what checklist items
  1 and 2 of [`platform-support.md`](../../platform-support.md) require to be recorded, so that a renamed process in
  Visual Studio 2027 is found by the measurement rather than by a bug report.
- Add a test that classifies every name of the table, and a test that fails when either assembly stops compiling the
  shared file.

#### Acceptance criteria

- One source file holds the process name table, and adding a name to it takes effect in `Metalama.Backstage` and in
  `Metalama.Framework.CompilerExtensions` without a second edit.
- No source file carries a comment instructing the reader to repeat a change to this classification in another file.
- The troubleshooting report of `ResourceExtractor` names the C# Dev Kit language server, an MSBuild node, a test
  host and the Code Lens service by the same names that the Backstage log gives them.
- Every design-time entry point selects the same implementation for the C# Dev Kit as before this story, chosen by
  an arm that names the C# Dev Kit rather than by the default arm.
- The process manager stops the Visual Studio 2026 analysis process and the C# Dev Kit language server when they
  hold a Metalama assembly.
- A test classifies every process name of the table, and it runs without the corresponding process existing.
- Every arm of the table names a host that PB-2027.0 supports, or carries the reason it is kept.

#### Not in scope

This story does not perform the November 2026 measurement, which is S-11; it only states what that measurement must
record. It does not decide whether the lower Roslyn variant reports the divergence of question Q2, which is a
separate decision and a separate story, and it does not add any diagnostic. It does not change the behaviour that
any entry point has today on any host.

— Claude for @gfraiteur
