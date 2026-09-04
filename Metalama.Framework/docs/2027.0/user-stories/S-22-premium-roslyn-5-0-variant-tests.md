### S-22. Metalama.Premium: execute the Roslyn 5.0.0 variant in tests

- Issue type: User Story
- Labels: `enhancement`, `Area-Extensions`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama.Premium`
- Size: L
- Blocked by: nothing
- Findings: [PR-2](../07-premium.md), [PR-13](../07-premium.md)

---

The three Roslyn 5.0.0 variant projects of the Premium validation and code fix engines are compiled by the solution
build and are executed by no test. That variant is the one that serves Rider and the Visual Studio Code C# Dev Kit,
which nobody exercises by hand now that Visual Studio 2022 is dropped, so the risk of this gap rose this release
rather than staying constant.

#### Context

The lower variant that these tests would execute was created by metalama/Metalama.Premium#85, which closed the issue
#1913 and renamed the Roslyn 4.12.0 variant projects to 5.0.0; that pull request added no test for them, which is the
gap this story closes. The gap is behavioural rather than an interface gap: the variant projects are referenced by
the package resource projects, so a use of an application programming interface that Roslyn 5.0 does not have fails
the build. What is not covered is behaviour that differs when the engines bind against the older Roslyn, and a defect
of that kind is not detected before a Rider user reports it. The order inside the story matters: the existing aspect
test projects reference the unsuffixed engine by a hardcoded path, so a variant shim added before they are made
variant-aware would compile the test sources under one property set and load the other engine.

#### Scope

- Make `Metalama.Extensions.Validation.AspectTests` and `Metalama.Extensions.CodeFixes.AspectTests` variant-aware, in
  the manner of the core aspect test project, by resolving the engine project reference through the variant suffix.
- Add the aspect test shims for the Roslyn 5.0.0 variant, and confirm that the extension assembly item names the file
  that is actually in the output directory.
- Decide separately whether the unit test shims are added, since they additionally need a Roslyn 5.0.0 build of the
  core unit test helper package, which is a change in the other repository; they belong in a second pull request or
  are dropped in favour of the aspect test shims alone.
- Correct the comment in those project files that still describes a variant constant which no longer exists, in the
  change that removes its cause.

#### Acceptance criteria

- The validation and code fix engines are executed by tests in both Roslyn variants, and both are green.
- No test project resolves the engine by a hardcoded path.
- No project file describes a preprocessor constant that no configuration defines.

— Claude for @gfraiteur
