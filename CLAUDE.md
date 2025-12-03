# Claude Instructions

## Repository

This repo contains mostly C# projects.

This repo is composed of different parts called "solutions", but one "solution" can contain different sln files. Each "solution" is a different first-level directory. E.g. `Metalama` and `Metalama.Extensions` are different solutions.

Solutions are defined in `eng/src/Program.cs`. Solutions are layered and ordered. A solution N can depend on a solution N-1 onlyu through PackageReference, never through ProjectReference.

Solutions are build by `Build.ps1 build`, which is a front end to `eng/src/Program.cs`. `Build.ps1 build` is the only valid way to build the packages of a solution. However, `Build.ps1 build` is slow and should only be executed when truly needed.

When changes are done _within_ a solution, it's ok to do builds using `dotnet build` or to run tests with `dotnet test`. When any change is done in a _lower_ solution (N-1), then `Build.ps1 build` is required.


## When writing XML documentation

- Use `<see>` tags where possible.
- Use consistent lexicon, style and structure among classes and files that have the same suffix (belong to the same family).
- Do not write long code examples.
- Read the related conceptual documentation in project `../Metalama.Documentation/content` mentioned by DocFx uid in the `<seealso href="@..."/>` tags.
- Complete the API doc with the conceptual documentation where relevant.

## Pre-PR checks and enhancements

- Documentation
    - Check and complete the documentation of all new or modified APIs.
    - Look for relevant conceptual articles in `../Metalama.Documentation/content` using keyword search.
    - Suggest changes in affected conceptual articles

## Aspect tests

- In aspect tests, Foo.t.cs is the result file of Foo.cs


## Incremental learning

When you learn something important that can make save you time the next time, update CLAUDE.md.

## Key learnings

### Cross-solution builds
- When changes span multiple solutions (e.g., Framework + Extensions + LinqPad), use `Build.ps1 build` - `dotnet build` will fail because packages from lower solutions aren't published yet.
- When adding new package references, also add the `PackageVersion` to `Directory.Packages.props` (Central Package Management).


### Documentation updates
- When adding public APIs, also update:
  - XML documentation with `<see>` tags and usage examples
  - Conceptual documentation in `../Metalama.Documentation/content`
  - Sample code in `../Metalama.Documentation/code`
- Two build.ps1 builds can never run in parallel. the previous one must always complete
- Do not run `Build.ps1 build` yourself, but ask the user to do it, because the timeout is too low and you will then retry the build
## Git branches

- Branch naming convention: `topic/YYYY.N/XXXX-short-description` where `XXXX` is the issue number
- For a branch named `topic/YYYY.N/*`, the merge branch is always `develop/YYYY.N` - do not use the default merge branch

## Commits

- Commit messages must include the issue number, e.g. `(#1212)`
- Do not sign commits with "Generated with Claude Code"

- It is never needed to clear global packages.