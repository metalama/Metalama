# Metalama.DesignTime.Hosting

A library that hosts the design-time pipeline of Metalama in a test process, the way an IDE hosts it. It targets
`net472` and `net10.0` and is published as the internal package `Metalama.DesignTime.Hosting`.

This library does not reference Metalama. It loads Metalama from the analyzer references of the hosted projects, so it
exercises the Metalama version that each project references.

It has two consumers:

- `Metalama.DesignTime.HostSimulator`, the command line that runs the `DesignTimeStandalone` scenarios.
- The tests of the Metalama extension of Visual Studio (`Metalama.Vsx`), which call the Visual Studio services of
  Metalama without Visual Studio.

## Two host modes

`DesignTimeHost` analyzes the projects of any Roslyn `Solution` or `Workspace`, for instance an `MSBuildWorkspace` or an
`AdhocWorkspace`.

| Method | What it simulates |
|---|---|
| `RunAsync` | An in-process host such as Rider or the Visual Studio Code language server. The analyzers are loaded by a `HostAnalyzerAssemblyLoader` and run in the current process. |
| `RunVisualStudioInProcessAsync` | Visual Studio. Metalama hosts both processes of the Visual Studio architecture in the current process: the user process, which serves the services of the Metalama extension, and the analysis process, which runs the pipeline. The two communicate through named pipes, as in Visual Studio. |

`HostAnalyzerAssemblyLoader.Create()` returns the loader of the current runtime. On .NET, it isolates the analyzers of
each directory in an `AssemblyLoadContext`, as Roslyn does. On .NET Framework, it loads them into the application domain,
as Visual Studio does. Both resolve the dependencies of an analyzer from the other analyzer references whose path was
passed to `AddDependencyLocation`.

## The Visual Studio in-process mode is a supported test contract

The mode is defined in `Metalama.Framework.DesignTime` and is off unless a test enables it. It does not change the
behavior of Metalama in Visual Studio, in Rider, or in the compiler. Its contract has three parts:

| Part | Value |
|---|---|
| Environment variable | `METALAMA_DESIGN_TIME_HOST=VisualStudioInProcess`, read once per process when the Metalama assemblies are loaded. |
| `AppDomain` data slot | `Metalama.Framework.DesignTime.VisualStudioInProcess.WaitForAnalysis`: a `Func<CancellationToken, Task>` that completes when Metalama has analyzed the current solution of its workspace. |
| `AppDomain` data slot | `Metalama.Framework.DesignTime.VisualStudioInProcess.SetWorkspace`: an `Action<Workspace>` that has the effect of `IWorkspaceReceiver.SetWorkspace`. |

`VisualStudioInProcessHost` repeats these names, and the tests of this repository exercise them on both runtimes. The
Metalama side is `VsInProcessAnalysisProcess` and `DesignTimeHostOverride`. Do not change one side without the other.

In this mode, Metalama:

- creates the Visual Studio user-process entry points whatever the name of the process is;
- registers the service provider of the user process in `DesignTimeEntryPointManager.Instance`, when the source
  generator of `Metalama.Framework.CompilerExtensions.dll` is created, as in Visual Studio;
- creates the service provider of the analysis process with separate global services and its own
  `DesignTimeEntryPointManager`, so that only the user provider is registered;
- serves `IWorkspaceReceiver` from the user provider (Visual Studio does not), and gives the workspace to both
  processes;
- runs the source generator of the analysis process for each C# project when it receives the workspace and when the
  workspace changes, as Roslyn does in the analysis process of Visual Studio;
- shows no Backstage user interface, does not collect telemetry (it sets `METALAMA_TELEMETRY_OPT_OUT=1` unless it is
  set), and does not use licensing, as design time already does.

## Steps for a test of the Visual Studio extension

1. **Enable the mode** before any Metalama assembly is loaded:

   ```csharp
   VisualStudioInProcessHost.Enable( tempDirectory );
   ```

   `tempDirectory` becomes `METALAMA_TEMP`, where Metalama extracts its assemblies and writes its caches and logs. Keep
   it short: Metalama limits the length of the paths below it to 207 characters, and a deep directory fails with
   `PathTooLongException`. On .NET Framework, `Enable` also calls `VerifyBindingRedirects` (step 5).

2. **Create the workspace** with the analyzer references that a project referencing `Metalama.Framework` gets. Both are
   required, because the generator of the first one depends on the second one:

   - `Metalama.Framework.CompilerExtensions.dll`, from `analyzers/dotnet/cs` of the `Metalama.Framework` package;
   - `Metalama.Compiler.Interface.dll`, from `analyzers/dotnet/cs` of the `Metalama.Compiler` package.

   `MetalamaAnalyzerPaths.FromPackagesFolder( packagesFolder, metalamaVersion )` returns all the analyzer assemblies of
   both packages from a global packages folder. Load them with one `HostAnalyzerAssemblyLoader`, and call
   `AddDependencyLocation` for each path.

   The project must define `METALAMA`, reference `Metalama.Framework.dll`, and have a `.globalconfig` analyzer config
   document with the build properties that `Metalama.Framework.targets` makes visible to the compiler. At least:

   ```ini
   is_global = true
   build_property.MetalamaEnabled = true
   build_property.AssemblyName = MyProject
   build_property.TargetFramework = netstandard2.0
   build_property.MSBuildProjectFullPath = C:\...\MyProject.csproj
   build_property.NETCoreSdkVersion = <the output of dotnet --version in the directory of the project>
   ```

   `NETCoreSdkVersion` is required, and it must be the version of a .NET SDK that is installed, typically the output of
   `dotnet --version` in the directory of the project. Metalama derives the language version of the compile-time code
   from it, and it writes it into the `global.json` file of the temporary project that resolves the compile-time
   reference assemblies. With an SDK that is not installed, the pipeline fails with `LAMA0082`.

3. **Create the Metalama source generators**, which registers the services:

   ```csharp
   await VisualStudioInProcessHost.CreateGeneratorsAsync( project );
   ```

   This is what the extension does in `MetalamaServiceProvider.EnsureUserProcessGeneratorCreatedAsync`, so a test can
   also go through that code instead.

4. **Get the services and give them the workspace**, as the extension does:

   ```csharp
   var consumer = DesignTimeEntryPointManager.Instance.GetConsumer( CurrentContractVersions.All );
   var provider = await consumer.GetServiceProviderAsync( metalamaVersion, cancellationToken );
   ((IWorkspaceReceiver) provider.GetService( typeof(IWorkspaceReceiver) )).SetWorkspace( workspace );
   ```

   A test that does not reference the contracts calls `VisualStudioInProcessHost.SetWorkspace( workspace )` instead.
   Metalama accepts one workspace per process, so the tests of one process share it.

5. **Wait for the analysis** before calling a service, and again after each change of the workspace:

   ```csharp
   await VisualStudioInProcessHost.WaitForAnalysisAsync( cancellationToken );
   ```

   The task compares the current solution of the workspace with the last analyzed one, so it does not depend on the
   order of the workspace events. It fails when the analysis fails, and when the aspect pipeline of a project cannot be
   configured because of an error that has no location in the source code, such as `LAMA0082` for an SDK that is not
   installed. An error in the compile-time code of the project, such as a C# error in an aspect, is a state that a test
   may observe, so it does not make the task fail. A change to a document that contains compile-time
   code, such as an aspect, pauses the pipeline until the user resumes it, as in Visual Studio. A test that expects the
   services to follow a change must change a document that contains run-time code only.

6. **On .NET Framework, provide the binding redirects** that Visual Studio gives to `devenv`. Metalama embeds
   `System.Text.Json` 10.0.0.10, some of its assemblies are compiled against 9.0.0.10, and it provides an embedded assembly
   only for the exact requested version. Add this fragment under `configuration/runtime` in the `App.config` of the test
   project, and reference the `System.Text.Json` package of version 10.0.10 or higher. The fragment is also the value of
   `VisualStudioInProcessHost.BindingRedirectsAppConfigFragment`.

   ```xml
   <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
     <dependentAssembly>
       <assemblyIdentity name="System.Text.Json" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
       <bindingRedirect oldVersion="0.0.0.0-10.0.0.10" newVersion="10.0.0.10" />
     </dependentAssembly>
     <dependentAssembly>
       <assemblyIdentity name="System.Text.Encodings.Web" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
       <bindingRedirect oldVersion="0.0.0.0-10.0.0.10" newVersion="10.0.0.10" />
     </dependentAssembly>
   </assemblyBinding>
   ```

   The named pipes need `Microsoft.Bcl.AsyncInterfaces` as well, which the processes of Visual Studio provide.
   `Metalama.Framework.DesignTime.Rpc` is compiled against 8.0.0.0, and Metalama embeds 10.0.0.10. Reference the
   `Microsoft.Bcl.AsyncInterfaces` package of version 10.0 or higher, and keep a binding redirect to its version, which
   `AutoGenerateBindingRedirects` generates when another reference requests a lower version. Without it, the analysis
   process never registers its projects, and the calls to the services wait forever.

   Without these redirects, `VerifyBindingRedirects` throws an `InvalidOperationException` that says what is missing.

7. **On .NET, provide `Microsoft.Bcl.AsyncInterfaces`** in the output of the host, for the same reason. The shared
   framework does not contain it. Without it, the analysis process fails with a `FileNotFoundException` for
   `Microsoft.Bcl.AsyncInterfaces, Version=8.0.0.0` and never registers its projects.

The tests under `Metalama.Framework/src/tests/Metalama.DesignTime.Hosting.Tests` follow these steps on both runtimes,
and are the reference implementation of a test.

## Timing

Measured on a developer machine, on .NET Framework, with an empty `METALAMA_TEMP`: the services are registered about
1.2 seconds after the generator is created, which includes the extraction of the Metalama assemblies, and the first
analysis is complete after about 7 seconds, which includes the restore of the compile-time reference assemblies. With a
warm `METALAMA_TEMP`, the first analysis takes about 4 seconds, and the services then answer in tens of milliseconds.
