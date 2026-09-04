### S-09. Build: .NET 11 software development kit in the build container

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
test can exercise a C# 15 construct in compile-time code. The .NET 11 software development kit is installed and
becomes the main kit of the product, which `global.json` names.

#### Context

The mechanism is the compile-time compilation rather than the target framework, which section 9 of
[`DECISIONS.md`](../DECISIONS.md) records.

`LanguageVersionProvider.GetLanguageVersionFromDotNetSdk` reads the `NETCoreSdkVersion` property that MSBuild makes
visible to the compiler, maps its major version to a maximum language version, and applies that maximum whatever
the project requests. A major of 10 or more maps to C# 14 today. The compile-time half of an aspect test is
therefore pinned to C# 14 while the container carries only the .NET 10 kit, and a test that uses a C# 15 construct
in compile-time code cannot build. Story S-15 adds the arm that maps a major of 11 to C# 15, and this story
provides the kit that makes the arm reachable.

The `net11.0` target framework remains out of scope, and this story does not add one. Section 9 records that no
.NET 11 application programming interface justifies a `net11.0` asset or a leg in the test matrix. What this story
provides is the toolchain, not a new target.

The risk is known and recorded in the comment at `eng/src/Program.cs:19-25`. Two feature bands under one
installation already produced a restore failure, because a stale `MSBuildExtensionsPath` made a solution restore
import the NuGet targets of one band into the MSBuild of another and fail with `MSB4062`. The mitigation is
complete upstream: pull request #1919 removes that variable in `DotNetTool.cs:61` and in `MSBuildTool.cs:55`, and
the matching change is in PostSharp.Engineering 2023.2.421. This repository still pins 2023.2.420 at
`Directory.Packages.props:12`, so the remaining work is to take the dependency rather than to write the fix.

The container also installs the .NET software development kit that Visual Studio ships through its own component,
which is why `dotNetSdkVersion` was pinned to the version Visual Studio installs rather than to the preferred
version of PostSharp.Engineering. Moving the pin to the .NET 11 kit therefore leaves two feature bands installed,
which is the configuration that the restore failure above comes from.

#### Scope

- Add the .NET 11 software development kit to the container component list in `eng/src/Program.cs`, beside the
  existing one rather than in place of it, and regenerate the container files with the script generation step.
- Move the `dotNetSdkVersion` pin to the .NET 11 kit, so that the generated `global.json` names it as the main kit
  of the product. Section 9 of [`DECISIONS.md`](../DECISIONS.md) takes that decision, because it is what lifts the
  compile-time language cap everywhere rather than only in the scenarios that ask for the newer kit. Record beside
  the constant that the product is therefore built with a kit that no supported Visual Studio installs, which the
  desktop MSBuild path of the container still exercises.
- Move the `PostSharpEngineeringVersion` pin of `Directory.Packages.props` to 2023.2.421 or above, which is the
  release that blocks `MSBuildExtensionsPath` in both the tool invocation options and the MSBuild tool. Without it
  the two feature bands are expected to fail a restore.
- Mirror the container change in `metalama/Metalama.Premium`, whose container definition is a separate copy of the
  same component list.
- Verify that a build with the .NET 11 kit produces the same packages as a build with the .NET 10 kit, so that the
  move of the pin is not a behavioural change.

#### Acceptance criteria

- The container carries both software development kits, and the generated container files name both.
- A build and a test run are green on the continuous integration server with `global.json` naming the .NET 11 kit,
  with zero warnings.
- An aspect test that requests C# 15 in compile-time code compiles once story S-15 has landed, which is the
  outcome this story exists for.
- No restore fails with `MSB4062`, and the engineering version that blocks the environment variable causing it is
  the one the repository pins.
- `Metalama.Premium` builds against the same container.

#### Not in scope

The `net11.0` target framework, for the product or for the tests, which section 9 puts out of scope. The arm of
the language version provider that maps a major of 11 to C# 15, which belongs to story S-15.

— Claude for @gfraiteur
