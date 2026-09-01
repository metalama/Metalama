# Updating Roslyn

1. Update Metalama.Compiler first. 
2. Update `RoslynMaxVersion` and `RoslynApiMaxVersion` in `Directory.packages.props` and possibly `ThisRoslynVersion` in `eng/RoslynVersions/Roslyn.<LAST_VERSION>.props` (when updating between pre-release versions of Roslyn).
3. Study the new C# syntax features. We IGNORE any experimental feature. They are not supported. If the new Roslyn only has new experimental features, there is nothing to do in this repo.
4. Add the `Syntax.xml` file of the new Roslyn version to `eng/src/GenerateMetaSyntaxRewriter`, under the name `Syntax-<NEW_VERSION>.xml`. Copy it from `src/Compilers/CSharp/Portable/Syntax/Syntax.xml` in the `Metalama.Compiler` branch that targets that Roslyn version, because that is the grammar of the Roslyn build we consume. Do not rename the previous version's file instead: the grammar is specific to the Roslyn version, and the generated code and the version checker both derive from it. Keep the experimental nodes that the file declares. The generator only needs them to be present in the referenced `Microsoft.CodeAnalysis.CSharp` assembly, which they are; whether a node is reachable from a supported `LanguageVersion` is a separate question, decided in step 3.
5. Edit `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs` to include this file.
6. Run `build.ps1 prepare`.
7. Decide whether the new Roslyn version needs a variant of its own, or whether the latest variant is renumbered to it. A host loads a variant only when its own Roslyn is at least the version the variant binds against, because `Microsoft.CodeAnalysis` carries `AssemblyVersion` `major.minor.0.0` and assembly binding never rolls back. See `Directory.Packages.md`.
    * Renumber the latest variant when the previous latest variant no longer serves any supported host. Follow step 8, then step 10.
    * Add a variant when supported hosts remain on the previous Roslyn version. Follow step 9, then step 10.
8. To renumber the latest variant, inside `../eng/RoslynVersions`:
    1. Rename `Roslyn.<PREVIOUS>.props` to `Roslyn.<NEW>.props` and set `ThisRoslynVersionNoPreview` to the new version.
    2. Update `Latest.props` to point to the new file.

    No project directory is renamed, because the latest variant has an empty `ThisRoslynVersionProjectSuffix`. Do not perform step 9: it would add a variant instead of renumbering one.
9. To add a variant:
    1. Inside `../eng/RoslynVersions`, create a `.props` file for the new version. Copy from the previous latest version and just change the version number.
    2. Update the `Latest.props` to point to the new version.
    3. In the `Roslyn.*.props` file of the _previous_ version, set the `ThisRoslynVersionProjectSuffix` property to something like `.4.0.1` and _mind the leading period_, it is necessary.
    4. Look at all projects named e.g. `Metalama.*.<LAST_VERSION>.csproj` and duplicate them, but import the _previous last version_.
    5. Update Metalama.Framework.sln to include the new project.
10. In both cases, do a find-in-files for the _previous_ latest version and see where things need to be changed or added. Both paths rename the assemblies, the packages and the generated-code directory that derive from `ThisRoslynVersionNoPreview`. This includes:
    1. Many `InternalsVisibleTo`
    2. `ResourceExtractor.GetRoslynVersion`
    3. `RoslynApiVersion` and `SupportedCSharpVersions`
    4. `JsonSerializationBinder`
    5. `Metalama.Framework.CompilerExtensions.Resources.csproj`, which must list the new assemblies
11. Drop a variant when no host in the supported platform baseline still needs it. Delete its props file and its shim projects, remove them from `Metalama.Framework.sln`, and raise `RoslynApiMinVersion` to the identity of the lowest remaining variant. Then check every constant the remaining variants define: a constant that all of them define, or that none of them defines, is no longer a distinction, and it must be removed together with its `#if` sites and its `@RequiredConstant`, `@ForbiddenConstant`, `RequiredConstants` and `ForbiddenConstants` test directives. A test that exists only for the dropped variant goes with it.
12. Do not add a `DefineConstants` entry to a variant props file unless the source has to branch on a distinction that no existing constant expresses. The variant props files currently define none.

The testing should include:
* normal compile-time testing,
* basic design-time testing with the new VS version,
* basic design-time testing with the _previous_ VS version, or at least with the previous LTS version.
