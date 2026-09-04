### S-05. Remove the residue of the previous platform baseline from the engine defaults and the test gates

- Issue type: Bug
- Labels: `bug`, `Area-Framework`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`
- Size: S
- Blocked by: nothing
- Findings: [UT-3](../06-user-tfm-patterns-tests-docs.md), [UT-4](../06-user-tfm-patterns-tests-docs.md),
  [UT-9](../06-user-tfm-patterns-tests-docs.md), [UT-10](../06-user-tfm-patterns-tests-docs.md). This story completes the
  closed issue #1876.

---

Four residual items of #1876, the issue that removed explicit support for .NET 8 and .NET 9, and of its pull request
#1877, share one property: each is invisible in a build that succeeds, and none produces a failure.
`CompileTimeAssemblyLocator.cs:43` still names `net8.0` in the default target frameworks of
the nested compile-time reference project, `DefaultProjectOptions.cs:56` reports the target framework `net8.0` to
every test whatever the test assembly targets, two facts of the Contracts unit tests are excluded on every leg
because their guard names `NET6_0` rather than `NET6_0_OR_GREATER`, and two aspect tests never run because their
guard names `ROSLYN4_4_OR_GREATER`, a symbol that no variant defines.

#### Context

An additional compile-time package resolves its `netstandard2.0` asset instead of its `net10.0` asset because of the
first value, and an out-of-support target framework is restored on every build. The two guards are dead in different
ways: the Contracts guard excludes code that genuinely does not compile on .NET Framework, so it keeps a target
framework condition and only the symbol name changes; the aspect test guard survived the cleanup of #1881 because the
name does not follow the underscore convention of the variant symbols, and each run already reports the two tests as
skipped with that reason. This story is scheduled before the C# 15 test suite is written, because that suite adds new
constant gates of exactly this shape.

#### Scope

- Change `_defaultCompileTimeTargetFrameworks` in
  `Metalama.Framework/src/Metalama.Framework.Engine/CompileTime/CompileTimeAssemblyLocator.cs:43` to name `net10.0`
  instead of `net8.0`, and edit the fixtures that pin it, which are the `Issue1789` standalone scenario, its
  `README.md`, the unit test data and the comments in `IProjectOptions.cs` and `test.ps1`.
- Use `net10.0` and not `net11.0` for that value, because PB-2027.0 keeps a .NET 10 SDK as the build-time SDK.
- Raise the default of `DefaultProjectOptions.TargetFramework` from `net8.0` to `net10.0`, and re-accept the one
  aspect test snapshot that prints the value.
- Replace `NET6_0` by `NET6_0_OR_GREATER` in `Metalama.Patterns.Contracts.UnitTests/DoubleTests.cs`, keeping the
  target framework condition, then run the two facts and adopt the result.
- Remove the `ROSLYN4_4_OR_GREATER` gate from the two `InterfaceImplementation` aspect tests, keep their
  `NET6_0_OR_GREATER` requirement so that the directive and the conditional name the same symbol, run them and adopt
  their output.

#### Acceptance criteria

- No `net8.0` literal remains in the engine defaults or in the fixtures that pin them.
- A test reads the target framework of the leg it runs on, and the aspect test that prints it is re-accepted.
- The four previously excluded tests execute, and the pull request description states, for each of them, what the
  newly produced output proves.
- No test directive names a preprocessor symbol that no configuration defines.

#### Not in scope

This story adds no `net11.0` test leg. Sections 6 and 6c of [`DECISIONS.md`](../DECISIONS.md) exclude it.

— Claude for @gfraiteur
