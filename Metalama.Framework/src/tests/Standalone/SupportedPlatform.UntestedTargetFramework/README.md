# The supported platform check

End-to-end tests of the diagnostics that report a target framework, a .NET SDK or a Visual Studio version outside
the configuration matrix that Metalama is tested with (issue
[#1884](https://github.com/metalama/Metalama/issues/1884)).

The check is a set of MSBuild targets in `Metalama.Framework.Package/build/Metalama.Framework.targets`. Nothing
about it is visible to a unit test, because it reads MSBuild properties that only a real build sets, and it reports
through the `Warning` task rather than through the Metalama pipeline. Only a scenario that runs MSBuild can cover
it.

## The scenarios of this family

| Scenario | What it asserts |
| --- | --- |
| `SupportedPlatform.UntestedTargetFramework` | A target framework older than the tested range reports `LAMA0600` and the build succeeds. |
| `SupportedPlatform.TestedTargetFrameworks` | Every target framework of the tested matrix, including `net10.0-windows`, reports nothing. |
| `SupportedPlatform.ContributedRequirements` | A requirement contributed by another package is evaluated on its own and names its own package. |
| `SupportedPlatform.CheckDisabled` | `MetalamaCheckSupportedPlatform` set to `False` suppresses every diagnostic. |
| `SupportedPlatform.NoWarn` | A code in `NoWarn` suppresses one dimension and leaves the others in place. |
| `SupportedPlatform.MetalamaDisabled` | `MetalamaEnabled` set to `False` suppresses every diagnostic. |
| `SupportedPlatform.Exclusion` | `MetalamaSupportedPlatformExclusion` skips one requirement and leaves the others in place. |

## What these scenarios do not cover

The Visual Studio dimension cannot be covered here. `dotnet build` runs MSBuild on .NET, so `$(MSBuildRuntimeType)`
is `Core` and the check does not apply. The scenarios assert the other half of that rule, that no `LAMA0602` is
reported under `dotnet build`, and the positive case belongs to the manual verification protocol of
`Directory.Packages.md`, which already requires a pass in every supported Visual Studio version.

The .NET SDK dimension is covered only through a contributed requirement, because the build agent has one SDK.
Varying the SDK belongs to the matrix of `metalama/Metalama.Tests.DotNetSdk`.

## How to run

`Build.ps1 test`, which builds every scenario under `Standalone`. To reproduce one by hand:

```
dotnet build SupportedPlatform.UntestedTargetFramework.csproj
```
