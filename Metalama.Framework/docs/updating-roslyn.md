# Updating Roslyn

1. Update Metalama.Compiler first. 
2. Update `RoslynMaxVersion` and `RoslynApiMaxVersion` in `Directory.packages.props` and possibly `ThisRoslynVersion` in `eng/RoslynVersions/Roslyn.<LAST_VERSION>.props` (when updating between pre-release versions of Roslyn).
3. Study the new C# syntax features. We IGNORE any experimental feature. They are not supported. If the new Roslyn only has new experimental features, there is nothing to do in this repo.
4. Add the `Syntax.xml` file of the new Roslyn version to `eng/src/GenerateMetaSyntaxRewriter`, under the name `Syntax-<NEW_VERSION>.xml`. Copy it from `src/Compilers/CSharp/Portable/Syntax/Syntax.xml` in the `Metalama.Compiler` branch that targets that Roslyn version, because that is the grammar of the Roslyn build we consume. Do not rename the previous version's file instead: the grammar is specific to the Roslyn version, and the generated code and the version checker both derive from it. Keep the experimental nodes that the file declares. The generator only needs them to be present in the referenced `Microsoft.CodeAnalysis.CSharp` assembly, which they are; whether a node is reachable from a supported `LanguageVersion` is a separate question, decided in step 3.
5. Edit `eng/src/GenerateMetaSyntaxRewriter/GenerateMetaSyntaxRewriter.cs` to include this file.
6. Run `build.ps1 prepare`.
7. Decide whether the new Roslyn version needs a variant of its own, or whether the latest variant is renumbered to it.
    * Renumber the latest variant when the previous latest variant no longer serves any supported host. Renaming `Roslyn.<PREVIOUS>.props` to `Roslyn.<NEW>.props` and setting `ThisRoslynVersionNoPreview` to the new version is then the whole change, because the latest variant has an empty `ThisRoslynVersionProjectSuffix`.
    * Add a variant when supported hosts remain on the previous Roslyn version. A host loads a variant only when its own Roslyn is at least the version the variant binds against, because `Microsoft.CodeAnalysis` carries `AssemblyVersion` `major.minor.0.0` and assembly binding never rolls back. See `Directory.Packages.md`.
8. Inside `../eng/RoslynVersions`:
    1. Create a `.props` file for the new version. Copy from the previous latest version and just change the version number.
    2. Update the `Latest.props` to point to the new version.
    3. In the `Roslyn.*.props` file of the _previous_ version, set the `ThisRoslynVersionProjectSuffix` property to something like `.4.0.1` and _mind the leading period_, it is necessary.
    4. Do not add a `DefineConstants` entry unless the source has to branch on a distinction that no existing constant expresses. The variant props files currently define one constant, `ROSLYN_5_0_0_OR_GREATER`.
9. Look at all projects named e.g. `Metalama.*.<LAST_VERSION>.csproj` and duplicate them, but import the _previous last version_.
10. Update Metalama.Framework.sln to include the new project.
11. Do a find-in-files for the _previous_ latest version and see where things need to be changed or added. This includes:
    1. Many `InternalsVisibleTo`
    2. `ResourceExtractor.GetRoslynVersion`
    3. `JsonSerializationBinder`
12. Update `Metalama.Framework.CompilerExtensions.Resources.csproj` to include the new assemblies.

The testing should include:
* normal compile-time testing,
* basic design-time testing with the new VS version,
* basic design-time testing with the _previous_ VS version, or at least with the previous LTS version.
