# Standalone Tests

Standalone tests are full `dotnet`/MSBuild builds of real projects. How scenarios are discovered and run, and the
full `test.json` schema (including how expected/forbidden diagnostics are matched), are documented in
`Metalama.Framework/docs/testing.md` (sections "Standalone tests" and "Design-time standalone tests"). This file
covers only the conventions specific to authoring a scenario here.

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

To assert an outcome other than "builds and runs cleanly" (for example, a build expected to fail with a specific
diagnostic), add a `test.json` next to the scenario entry point, and a `README.md` explaining why (see `Issue1741`,
`Issue1743`, `Issue1749`). The `test.json` schema and diagnostic-matching rules are in
`Metalama.Framework/docs/testing.md`.

## Attention

- Tests under this directory should only use `PackageReference` to reference Metalama. `ProjectReference` should only be used within the same solution, in the same test.