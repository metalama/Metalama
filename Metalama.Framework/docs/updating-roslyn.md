# Updating Roslyn

Which Roslyn versions we must support, and therefore which variants we ship, is decided by the platform baseline
in [`platform-support.md`](platform-support.md). Rule 7 of that doctrine requires a new stable Roslyn version to
be supported within three weeks of its stable release. This document is the procedure for doing so; the decision
of whether a variant is still needed, and which Visual Studio versions the testing below must cover, belongs
there.

1. Update Metalama.Compiler first. 
2. Update `RoslynMaxVersion` and `RoslynApiMaxVersion` in `Directory.packages.props` and possibly `ThisRoslynVersion` in `eng/RoslynVersions/Roslyn.<LAST_VERSION>.props` (when updating between pre-release versions of Roslyn). When the new version is a prerelease, also apply the switch described in [Entering and leaving a prerelease Roslyn](#entering-and-leaving-a-prerelease-roslyn) below.
3. Study the new C# syntax features. We IGNORE any experimental feature. They are not supported. If the new Roslyn only has new experimental features, there is nothing to do in this repo.
4. Add the `Syntax.xml` file from Roslyn to `eng/src/GenerateMetaSyntaxRewriter`
5. Edit `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs` to include this file.
6. Run `build.ps1 prepare`.
7. Inside `../eng/RoslynVersions`:
    1. Create a `.props` file for the new version. Copy from the previous latest version and just change the version number.
    2. Update the `Latest.props` to point to the new version.
    3. In the `Roslyn.*.props` file of the _previous_ version, set the `ThisRoslynVersionProjectSuffix` property to something like `.4.0.1` and _mind the leading period_, it is necessary.
8. Look at all projects named e.g. `Metalama.*.<LAST_VERSION>.csproj` and duplicate them, but import the _previous last version_.
9. Update Metalama.Framework.sln to include the new project.
10. Do a find-in-files for the _previous_ latest version and see where things need to be changed or added. This includes:
    1. Many `InternalsVisibleTo`
    2. `ResourceExtractor.GetRoslynVersion`
    3. `JsonSerializationBinder`
11. Update `Metalama.Framework.CompilerExtensions.Resources.csproj` to include the new assemblies.

## Entering and leaving a prerelease Roslyn

A prerelease Roslyn package is published on the `roslyn-consolidated` feed and not on nuget.org, and the project that
`CompileTimeAssemblyLocator` restores on a user machine references `Microsoft.CodeAnalysis.CSharp` at that version. The
generated `nuget.config` therefore has to declare that feed, and it does so on its own, because a user of a prerelease
Metalama has no reason to declare it.

The whole switch is `SupportedCSharpVersions.cs`, in the two methods `ToNuGetVersionString` and
`ToPrereleasePackageSourceUrl`, which are edited together:

1. To enter a prerelease Roslyn, give the version its prerelease version string in `ToNuGetVersionString`, and move the
   version out of the group that returns `null` in `ToPrereleasePackageSourceUrl`, so that it returns
   `RoslynPrereleaseSourceUrl`.
2. To leave it, apply the reverse edit: give the version its released version string, and move it back into the group
   that returns `null`.

The unit test `NuGetHelperTests.CurrentRoslynVersionHasNoPrereleasePackageSource` fails while a branch is on a
prerelease Roslyn. It is the reminder that the switch has to be reversed before the branch is released, and it is
updated together with the two methods.

See issue #1885.

The testing should include:
* normal compile-time testing,
* basic design-time testing with the new VS version,
* basic design-time testing with the _previous_ VS version, or at least with the previous LTS version.