# Metalama.DesignTime.HostSimulator

A command-line program that simulates what an IDE does at design time over a real solution, so that defects that
only appear at design time can be reproduced outside an IDE.

**This project deliberately does not reference Metalama.** It hosts Metalama the way an editor does: through the
analyzer references declared by the simulated solution, loaded from disk. It therefore exercises whichever Metalama
version each project actually references, including two different versions in one solution.

## Why it exists

The compile-time tests under `Standalone` run `dotnet build`, which gives one compiler process per project and no
shared state between them. Several classes of defect are invisible there:

- A design-time pipeline caches its configuration and can serve it to the pipeline of a dependent project, so a
  defect can depend on **which project is analyzed first**.
- Analyzers from two different package directories load into two different `AssemblyLoadContext`s, so a solution
  referencing two Metalama versions really does load two copies of the engine, which a single `dotnet build` never
  does.
- Editors ask for diagnostics **per document**, not per compilation, so the number and order of requests differ
  from a batch build.

## What it does

1. Registers the .NET SDK whose MSBuild opens the solution (`MSBuildEnvironment`), the same way
   `Metalama.Framework.Workspaces` does.
2. Opens the solution with `MSBuildWorkspace`, passing `DesignTimeBuild=true` and `BuildingInsideVisualStudio=true`
   so that projects evaluate as they do in an IDE.
3. For each project, in the requested order (`ProjectDesignTimeSession`):
   - re-creates the analyzer references with `IsolatedAnalyzerAssemblyLoader`, which allocates one
     `AssemblyLoadContext` per analyzer directory, reproducing
     `Microsoft.CodeAnalysis.AnalyzerAssemblyLoader.DirectoryLoadContext` (that type is internal, so it cannot be
     reused);
   - runs the source generators with `CSharpGeneratorDriver` and folds their output into the compilation, because
     analyzers must see generated code;
   - issues, for every document, the syntax and semantic analyzer requests an editor issues when the document is
     open.
4. Reports every diagnostic, and fails when it sees an infrastructure failure: an analyzer that threw (`AD0001`),
   an unhandled Metalama exception (`LAMA0001`), a Metalama error, or an analyzer assembly that failed to load.

## Usage

```
Metalama.DesignTime.HostSimulator <solution> [options]

  --traversal <ORDER> Solution (default), Graph, or Reverse. Graph analyzes a project after the ones it
                      references; Reverse analyzes it before them, which is what an editor routinely does.
  --order <a,b,c>     Analyze the named projects in this order. Mutually exclusive with --traversal.
  --permutations      Simulate every order of the projects, each in a fresh process.
  --property, -p      An MSBuild property, as Name=Value. Can be repeated.
  --msbuild-locator   Register an MSBuild instance with Microsoft.Build.Locator. Off by default: the workspace
                      uses an out-of-process build host that locates MSBuild itself, and registering the locator
                      prevents that host from starting, which hangs the workspace.
  --timeout <SECONDS> Abandon the simulation and report a failure after this long. Defaults to 600. Zero disables.
  --verbose, -v       Print every diagnostic, not only the ones that indicate a failure.
```

The timeout is enforced by racing the work against a delay and then killing the process, not by a cancellation
token, because a deadlocked call never observes the token. A design-time defect can be a deadlock as easily as an
exception, and without this a scenario that deadlocks would hang CI instead of failing it.

## How it is run by the build

Scenarios live under `Metalama.Framework/src/tests/DesignTimeStandalone` and are run by `ManyDesignTimeSolutions`,
declared in `eng/src`. It is the design-time counterpart of `ManyDotNetSolutions`: discovery, scheduling and
reporting are inherited from `ManySolutions`, and only the engine differs. Each scenario is restored but never
built, because a scenario is allowed to be one that does not compile.

Diagnostics are written to standard output in the canonical MSBuild format, one per line, which is what lets a
scenario assert on them with the same `test.json` syntax as a compile-time scenario.

The command line is built on Spectre.Console.Cli, so `--help` lists the options with their descriptions.

`--permutations` runs each order in a **child process**. This is not a convenience: analyzer assemblies load into
non-collectible load contexts, as they do in an IDE, so a second permutation in the same process would inherit the
assemblies and caches of the first one and would no longer be the scenario it claims to be. It refuses to run for
more than seven projects, since the number of orders is factorial.

The exit code is `0` when no infrastructure failure was seen, `1` when one was, and `2` on a usage error.

## First result

Run against `Standalone/Issue1749`, which `dotnet build` fails on with a `FileLoadException` from
`CompileTimeDomain`, the simulator gets further and reports a different, design-time-only failure:

```
FAIL Consumer: CS8785: Generator 'MetalamaSourceGenerator' failed to generate source.
  System.ArgumentException: An element with the same key but a different value already exists.
  Key: 'Contract, 2c261a018ff9f98d'
     at ImmutableDictionary`2.Builder.Add(TKey key, TValue value)
     at ProjectVersionProvider.Implementation.GetProjectReferencesAsync(...) line 181
```

Two distinct assemblies both named `Contract` collapse to a single `ProjectKey`, and
`ProjectVersionProvider.Implementation` builds an `ImmutableDictionary` keyed on it with a plain `Add`. This site
is unreachable from a batch build, and it is a further instance of the family in #1743 and #1749.

## What it does not simulate

Stated explicitly, because the gap matters when interpreting a green run:

- **Incremental editing.** It analyzes each document once. It does not type into a document, re-request
  diagnostics, or exercise the dependency-invalidation paths that a real editing session does.
- **The two-process split.** Visual Studio runs Metalama's analysis in a separate process and talks to it over RPC.
  This host loads analyzers in-process, which is what Rider and OmniSharp do, and what Visual Studio does for the
  parts that run in the analyzer host.
- **Roslyn's own scheduling.** A real IDE analyzes documents concurrently, on its own priority order, and cancels
  requests. This host is deterministic and sequential, which is what makes a failing order reproducible, but it
  means a race that needs concurrency will not appear.
- **Code actions, completion, and other language features.** Only diagnostics and source generation are requested.
