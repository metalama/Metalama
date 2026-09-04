### S-37. Install the .NET 11 software development kit in the build container and settle what `global.json` pins

- Issue type: User Story
- Labels: `enhancement`, `Area-Build-Engineering`
- Milestone: `2027.0`
- Repositories: `metalama/Metalama`, `metalama/Metalama.Premium`
- Size: M
- Blocked by: nothing
- Findings: [LV-9](../01-language-version-and-hosts.md), [UT-1](../06-user-tfm-patterns-tests-docs.md)

---

The build container installs one .NET software development kit, named by the `dotNetSdkVersion` constant of
`eng/src/Program.cs`, which is `10.0.400`. That constant feeds the container component, the `global.json` that the
preparation step generates, and the `DotNetSdkVersion` of the build tool. With only that kit installed, no aspect
test can exercise a C# 15 construct in compile-time code.

#### Context

The mechanism is the compile-time compilation rather than the target framework, which is why section 6b of
[`DECISIONS.md`](../DECISIONS.md) first judged this work a distraction and section 6d corrects it.

`LanguageVersionProvider.GetLanguageVersionFromDotNetSdk` reads the `NETCoreSdkVersion` property that MSBuild makes
visible to the compiler, maps its major version to a maximum language version, and applies that maximum whatever
the project requests. A major of 10 or more maps to C# 14 today. The compile-time half of an aspect test is
therefore pinned to C# 14 while the container carries only the .NET 10 kit, and a test that uses a C# 15 construct
in compile-time code cannot build. Story S-11 adds the arm that maps a major of 11 to C# 15, and this story
provides the kit that makes the arm reachable.

The `net11.0` target framework remains out of scope, and this story does not add one. Section 6c records that no
.NET 11 application programming interface justifies a `net11.0` asset or a leg in the test matrix. What this story
provides is the toolchain, not a new target.

The risk is known and recorded in the comment at `eng/src/Program.cs:19-25`. Two feature bands under one
installation already produced a restore failure, because a stale `MSBuildExtensionsPath` made a solution restore
import the NuGet targets of one band into the MSBuild of another and fail with `MSB4062`. The mitigation exists in
PostSharp.Engineering, in the blocked-environment-variable list of the tool invocation options and in the
equivalent list of the MSBuild tool, and both remove `MSBUILD_EXE_PATH` and `MSBuildSDKsPath` while neither
removes `MSBuildExtensionsPath`. That is the one entry the mitigation lacks.

The container also installs the .NET software development kit that Visual Studio ships through its own component,
which is why `dotNetSdkVersion` is pinned to the version Visual Studio installs rather than to the preferred
version of PostSharp.Engineering.

#### Scope

- Add the .NET 11 software development kit to the container component list in `eng/src/Program.cs`, beside the
  existing one rather than in place of it, and regenerate the container files with the script generation step.
- Decide what `global.json` pins, and record the decision beside the constant. Pinning the .NET 10 kit keeps the
  product build on the version Visual Studio ships and lets only the scenarios that ask for it use the newer kit.
  Pinning the .NET 11 kit makes the compile-time cap disappear everywhere at the cost of building the product with
  a kit that no supported Visual Studio installs.
- Add `MSBuildExtensionsPath` to the blocked environment variables of PostSharp.Engineering, in both the tool
  invocation options and the MSBuild tool, and take the dependency on the release that carries it. Without this the
  two feature bands are expected to fail a restore.
- Mirror the container change in `metalama/Metalama.Premium`, whose container definition is a separate copy of the
  same component list.
- Verify that a build with each kit produces the same packages, so that the pin is a choice rather than a
  behavioural change.

#### Acceptance criteria

- The container carries both software development kits, and the generated container files name both.
- A build and a test run are green on the continuous integration server with the pin as decided, with zero
  warnings.
- An aspect test that requests C# 15 in compile-time code compiles once story S-11 has landed, which is the
  outcome this story exists for.
- No restore fails with `MSB4062`, and the environment variable that causes it is blocked.
- `Metalama.Premium` builds against the same container.

#### Not in scope

The `net11.0` target framework, for the product or for the tests, which section 6c puts out of scope. The arm of
the language version provider that maps a major of 11 to C# 15, which belongs to story S-11.

— Claude for @gfraiteur
