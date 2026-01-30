# Standalone Tests

When creating standalone tests with multiple projects:

1. **Study existing examples first** - Look at `CompileTimeContract` or `TestWeaver` before designing a new structure. They show the correct patterns for `MetalamaExtensionAssembly`, `MetalamaCompileTimeAssembly`, and project references.

2. **Understand the MSBuild items**:
   - `MetalamaExtensionAssembly`: Loads extension assemblies at runtime (must have `ExportExtensionAttribute`)
   - `MetalamaCompileTimeAssembly`: Adds assemblies to compile-time project references
   - These serve different purposes - don't conflate them

3. **Project structure for SDK extensions**:
   - Contracts project: Contains `[CompileTime]` interfaces, `MetalamaEnabled=false`
   - Extension project: Contains `IProjectServiceFactory` impl, `MetalamaEnabled=false`, references Contracts
   - Consumer project: References Contracts via `ProjectReference`, loads Extension via `MetalamaExtensionAssembly`, adds Contracts via `MetalamaCompileTimeAssembly`

4. **C# limitations with `in` parameters**: Cannot use `yield return` in methods with `in` parameters. Use array initialization instead: `return new[] { ... }`.


## Attention

- Tests under this directory should only use `PackageReference` to reference Metalama. `ProjectReference` should only be used within the same solution, in the same test.