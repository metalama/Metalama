# Metalama Framework Extensibility

This document describes how to create extension packages for the Metalama Framework. Extensions can provide additional services, pipeline functionality, or test framework plugins.

## Extension Package Structure

Extension packages use a specific directory layout to integrate with MSBuild and the Metalama pipeline.

### Directory Layout

```
MyExtension/
├── MyExtension.csproj
├── build/
│   └── MyExtension.props          # MSBuild props for direct references
├── buildTransitive/
│   └── MyExtension.props          # MSBuild props for transitive references
└── metalama/
    ├── net472/
    │   └── MyExtension.dll        # Extension assembly for .NET Framework
    ├── net8.0/
    │   └── MyExtension.dll        # Extension assembly for .NET 8
    └── net9.0/
        └── MyExtension.dll        # Extension assembly for .NET 9
```

**Important:** Always target all three frameworks: `net472`, `net8.0`, and `net9.0`. This ensures compatibility with all supported runtime environments.

### Target Framework Selection

Extensions can use different target framework strategies depending on their complexity:

#### netstandard2.0 Extensions (Simple Cases)

For extensions that:
- Don't have native dependencies
- Don't use framework-specific APIs
- Have dependencies that all support netstandard2.0

You can target `netstandard2.0` for simpler packaging:

```xml
<PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
</PropertyGroup>
```

Props file (single entry, no `TargetFramework` metadata needed):
```xml
<Project>
    <ItemGroup>
        <MetalamaExtensionAssembly
            Include="$(MSBuildThisFileDirectory)../metalama/netstandard2.0/MyExtension.dll" />
    </ItemGroup>
</Project>
```

**Advantages:**
- Single assembly to build and package
- Simpler props files
- Works across all .NET runtimes that support netstandard2.0

**When NOT to use netstandard2.0:**
- Dependencies don't support netstandard2.0 (e.g., `DiffEngine` requires net462+/net6.0+)
- You need framework-specific APIs
- Dependencies have different versions per framework

#### Multi-Target Extensions (Complex Cases)

For extensions with dependencies that don't support netstandard2.0, or that need framework-specific code, use multi-targeting:

```xml
<PropertyGroup>
    <TargetFrameworks>net472;net8.0;net9.0</TargetFrameworks>
</PropertyGroup>
```

This requires separate `MetalamaExtensionAssembly` entries with `TargetFramework` metadata as shown in the Props File Pattern section below.

**Example:** `Metalama.Extensions.DiffEngine` must multi-target because `DiffEngine` package doesn't support netstandard2.0.

#### Multi-Roslyn-Version Extensions (Advanced Cases)

For extensions that use Roslyn internals which differ between versions, you need version-specific builds. This is orthogonal to .NET framework targeting—you may need both.

**When required:**
- Using internal Roslyn APIs that changed between versions
- Supporting older Visual Studio versions with different Roslyn versions
- Extensions in `Metalama.Premium` that access Roslyn internals

**Project structure:**
```
MyExtension.Engine/                    # Main engine project (latest Roslyn)
MyExtension.Engine.4.8.0/             # 4.8.0-specific build
MyExtension.Engine.4.12.0/            # 4.12.0-specific build
```

**Version-specific project pattern:**
```xml
<Project ToolsVersion="Current">
    <PropertyGroup>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <!-- Include source from main project -->
        <Compile Include="../MyExtension.Engine/**/*.cs"
                 Exclude="../MyExtension.Engine/bin/**/*.cs;
                          ../MyExtension.Engine/obj/**/*.cs" />
    </ItemGroup>

    <!-- Import Roslyn version configuration -->
    <Import Project="../../eng/RoslynVersions/Roslyn.4.8.0.props" />

    <!-- Import main project for other settings -->
    <Import Project="../MyExtension.Engine/MyExtension.Engine.csproj" />
</Project>
```

**Roslyn version props file** (`eng/RoslynVersions/Roslyn.X.X.X.props`):
```xml
<Project>
    <PropertyGroup>
        <ThisRoslynVersion>4.8.0</ThisRoslynVersion>
        <ThisRoslynVersionProjectSuffix>.4.8.0</ThisRoslynVersionProjectSuffix>
        <DefineConstants>$(DefineConstants);ROSLYN_4_8_0;ROSLYN_4_8_0_OR_GREATER</DefineConstants>
    </PropertyGroup>
</Project>
```

**Props file registration** (use `TargetRoslynVersion` metadata):
```xml
<MetalamaExtensionAssembly
    Include="...MyExtension.Engine.5.0.0.dll"
    TargetFramework="net472"
    TargetRoslynVersion="5.0.0"/>
<MetalamaExtensionAssembly
    Include="...MyExtension.Engine.4.8.0.dll"
    TargetFramework="net472"
    TargetRoslynVersion="4.8.0"/>
```

**Conditional compilation in code:**
```csharp
#if ROSLYN_4_12_0_OR_GREATER
    // Use new API
#else
    // Use old API
#endif
```

**Example:** `Metalama.Extensions.Validation` in Metalama.Premium uses this pattern with builds for Roslyn 4.8.0, 4.12.0, and 5.0.0.

### Simple Extension Pattern (HtmlWriter)

For extensions with bundled dependencies, use this `.csproj` pattern:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFrameworks>net472;net8.0;net9.0</TargetFrameworks>
        <IncludeBuildOutput>false</IncludeBuildOutput>
        <TargetsForTfmSpecificContentInPackage>
            $(TargetsForTfmSpecificContentInPackage);_AddAssembliesToOutput
        </TargetsForTfmSpecificContentInPackage>

        <!-- Disable warnings about empty lib folder -->
        <NoWarn>$(NoWarn);NU5128;NU5100</NoWarn>

        <!-- Disable Metalama.Compiler.Sdk customization -->
        <MetalamaCompilerDisablePackCustomization>True</MetalamaCompilerDisablePackCustomization>

        <!-- Don't create symbol packages -->
        <IncludeSymbols>false</IncludeSymbols>

        <!-- Copy dependencies to output for bundling -->
        <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
    </PropertyGroup>

    <ItemGroup>
        <!-- Bundle dependency privately -->
        <PackageReference Include="SomeDependency" PrivateAssets="all" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="../Metalama.Framework.Sdk/Metalama.Framework.Sdk.csproj" />
    </ItemGroup>

    <ItemGroup>
        <None Include="build\**\*" Pack="true" PackagePath="build" />
        <None Include="buildTransitive\**\*" Pack="true" PackagePath="buildTransitive" />
    </ItemGroup>

    <Target Name="_AddAssembliesToOutput">
        <ItemGroup>
            <!-- Extension assembly -->
            <TfmSpecificPackageFile Include="$(OutDir)MyExtension.dll"
                                    PackagePath="metalama/$(TargetFramework)" />
            <!-- Bundled dependency -->
            <TfmSpecificPackageFile Include="$(OutDir)SomeDependency.dll"
                                    PackagePath="metalama/$(TargetFramework)" />
        </ItemGroup>
    </Target>

    <Target Name="SetPackageContent" AfterTargets="MetalamaCompilerSetLibAssembliesInPackage">
        <ItemGroup>
            <!-- Remove everything from lib folder -->
            <BuildOutputInPackage Remove="@(BuildOutputInPackage)" />
        </ItemGroup>
    </Target>
</Project>
```

### Complex Extension Pattern (Premium/Validation)

For extensions requiring Roslyn-version-specific builds, use a three-tier structure:

1. **API Project** (`Metalama.Extensions.Validation`): Public API, targets `netstandard2.0`
2. **Engine Projects** (`Metalama.Extensions.Validation.Engine.X.X.X`): Version-specific implementations
3. **Package Project** (`Metalama.Extensions.Validation.Package`): Aggregates everything into one NuGet package

## MetalamaExtensionAssembly Registration

Extensions are registered via MSBuild props files that declare `MetalamaExtensionAssembly` items.

### Props File Pattern

Create `build/MyExtension.props`:

```xml
<Project>
    <ItemGroup>
        <!-- Load dependencies first -->
        <MetalamaExtensionAssembly
            Include="$(MSBuildThisFileDirectory)../metalama/net472/SomeDependency.dll"
            TargetFramework="net472" />
        <MetalamaExtensionAssembly
            Include="$(MSBuildThisFileDirectory)../metalama/net8.0/SomeDependency.dll"
            TargetFramework="net8.0" />
        <MetalamaExtensionAssembly
            Include="$(MSBuildThisFileDirectory)../metalama/net9.0/SomeDependency.dll"
            TargetFramework="net9.0" />

        <!-- Then load the extension -->
        <MetalamaExtensionAssembly
            Include="$(MSBuildThisFileDirectory)../metalama/net472/MyExtension.dll"
            TargetFramework="net472" />
        <MetalamaExtensionAssembly
            Include="$(MSBuildThisFileDirectory)../metalama/net8.0/MyExtension.dll"
            TargetFramework="net8.0" />
        <MetalamaExtensionAssembly
            Include="$(MSBuildThisFileDirectory)../metalama/net9.0/MyExtension.dll"
            TargetFramework="net9.0" />
    </ItemGroup>
</Project>
```

**Critical:** Always specify the `TargetFramework` metadata on `MetalamaExtensionAssembly` items. If omitted, the assembly will be loaded for **all** target frameworks, which can cause assembly version conflicts or runtime errors.

### buildTransitive Pattern

The `buildTransitive/MyExtension.props` registers the extension assemblies for transitive consumers. You have two options:

**Option 1:** Import from `build/` (single source of truth):
```xml
<Project>
    <Import Project="../build/MyExtension.props"/>
</Project>
```

**Option 2:** Duplicate the content (common in practice):
Both `build/` and `buildTransitive/` contain identical `MetalamaExtensionAssembly` items. This avoids path resolution issues in some build scenarios.

The `build/` folder is used when a project directly references the package, while `buildTransitive/` is used when a project transitively references it through another package.

### Assembly Loading Order

Dependencies must be listed before the assemblies that use them. The Metalama pipeline loads assemblies in the order they appear in the `MetalamaExtensionAssembly` items.

### Roslyn-Version-Specific Assemblies

For extensions with multiple Roslyn version builds, use the `TargetRoslynVersion` metadata. See the **Multi-Roslyn-Version Extensions** section under Target Framework Selection for full details on project structure and conditional compilation.

## Service Registration

### IProjectServiceFactory (Simple Pattern)

For extensions that provide project services, implement `IProjectServiceFactory`:

```csharp
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;

[assembly: ExportExtension(typeof(MyServiceFactory), ExtensionKinds.ServiceFactory)]

namespace MyExtension;

public sealed class MyServiceFactory : IProjectServiceFactory
{
    public IEnumerable<IProjectService> CreateServices(in ProjectServiceProvider serviceProvider)
    {
        // Resolve dependencies from the service provider
        var dependency = serviceProvider.GetRequiredService<ISomeDependency>();

        return [new MyService(dependency)];
    }
}
```

The service will be available to aspects via `ProjectServiceProvider.GetService<IMyService>()`.

### PipelineExtension (Complex Pattern)

For extensions that need to hook into the pipeline execution, derive from `PipelineExtension`:

```csharp
using Metalama.Framework.Engine.Extensibility;

[assembly: ExportExtension(typeof(MyPipelineExtension), ExtensionKinds.Default)]

namespace MyExtension;

public class MyPipelineExtension : PipelineExtension
{
    public override bool Initialize(PipelineExtensionInitializationContext context)
    {
        // Register services via ServiceBuilder
        context.ServiceBuilder.Add(_ => new MyQueryService());

        // Register diagnostic definitions
        var diagnosticDiscovery = context.ServiceProvider
            .GetRequiredService<DiagnosticDefinitionDiscoveryService>();
        context.AddDiagnosticDefinitions(
            diagnosticDiscovery.GetDiagnosticDefinitions(typeof(MyDiagnostics)));

        return true; // Return false to disable the extension
    }

    public override async Task ExecuteContributorsAsync(
        AspectPipelineConfiguration pipelineConfiguration,
        CompilationModel initialCompilation,
        UserDiagnosticSink diagnosticSink,
        ImmutableArray<IPipelineContributor> contributors,
        CancellationToken cancellationToken)
    {
        // Execute pipeline contributors as they are collected
    }

    public override Task<ExtensionPipelineContributorsResult> ExecutePipelineContributorsAsync(
        AspectPipelineConfiguration pipelineConfiguration,
        IEnumerable<IPipelineContributor> contributors,
        CompilationModel initialCompilation,
        CompilationModel finalCompilation,
        CancellationToken cancellationToken)
    {
        // Execute at end of pipeline with both initial and final compilations
        return Task.FromResult(ExtensionPipelineContributorsResult.Empty);
    }
}
```

## Test Framework Plugins

The test framework supports plugins for optional functionality like diff tools.

### MetalamaTestPlugIn Item

Register test plugins via MSBuild in a props file:

```xml
<Project>
    <ItemGroup>
        <MetalamaTestPlugIn Include="MyNamespace.MyPlugIn, MyAssembly" />
    </ItemGroup>
</Project>
```

### Plugin Interface Pattern

Define an interface in the core test framework:

```csharp
public interface ISnapshotDiffToolRunner
{
    bool IsDisabled { get; }
    void SetMaxInstances(int count);
    void Launch(string actualPath, string expectedPath);
    void Kill(string actualPath, string expectedPath);
}
```

Implement it in the optional plugin package:

```csharp
public sealed class DiffEngineRunner : ISnapshotDiffToolRunner
{
    public bool IsDisabled => DiffRunner.Disabled;
    public void SetMaxInstances(int count) => DiffRunner.MaxInstancesToLaunch(count);
    public void Launch(string actualPath, string expectedPath)
        => DiffRunner.Launch(actualPath, expectedPath);
    public void Kill(string actualPath, string expectedPath)
        => DiffRunner.Kill(actualPath, expectedPath);
}
```

### Graceful Degradation

Discover plugins via `PlugIns.OfType<T>()` and handle missing plugins gracefully:

```csharp
// Get the diff tool runner from plugins (may be null if package is not referenced)
var diffToolRunner = testContext.PlugIns.OfType<ISnapshotDiffToolRunner>().SingleOrDefault();

// Only use if available
diffToolRunner?.SetMaxInstances(maxInstances);
```

## SDK Interfaces

### IHtmlCodeWriter

Provides HTML code formatting with syntax highlighting and diff support:

```csharp
public interface IHtmlCodeWriter : IProjectService
{
    Task WriteAsync(
        Document document,
        TextWriter textWriter,
        HtmlCodeWriterOptions options,
        IEnumerable<Diagnostic>? diagnostics = null,
        CancellationToken cancellationToken = default);

    Task WriteDiffAsync(
        Document inputDocument,
        Document outputDocument,
        TextWriter inputTextWriter,
        TextWriter outputTextWriter,
        HtmlCodeWriterOptions options,
        IEnumerable<Diagnostic>? inputDiagnostics,
        IEnumerable<Diagnostic>? outputDiagnostics,
        CancellationToken cancellationToken);
}
```

### IFormattedCodeWriter

Provides classified text spans for code formatting:

```csharp
public interface IFormattedCodeWriter : IProjectService
{
    Task<IEnumerable<IClassifiedTextSpan>> GetClassifiedTextSpansAsync(
        Document document,
        bool areNodesAnnotated = false,
        IEnumerable<Diagnostic>? diagnostics = null,
        bool addTitles = false,
        CancellationToken cancellationToken = default);
}
```

### IClassifiedTextSpan

Represents a classified text span with semantic properties:

```csharp
public interface IClassifiedTextSpan
{
    TextSpan Span { get; }
    TextSpanClassification Classification { get; }
    Diagnostic? Diagnostic { get; }
    string? CSharpClassification { get; }
    string? Title { get; }
    string? GeneratingAspect { get; }
}
```

## Dependency Management

### Bundling Dependencies

For simple extensions, bundle dependencies privately:

```xml
<PropertyGroup>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>

<ItemGroup>
    <PackageReference Include="SomeDependency" PrivateAssets="all" />
</ItemGroup>
```

Include them in the package via `_AddAssembliesToOutput` target.

### Transitive Dependencies

**Critical:** You must bundle **all** transitive dependencies, not just direct dependencies. For example, if your extension uses `DiffEngine`, you must also bundle:
- `DiffEngine.dll` (direct dependency)
- `EmptyFiles.dll` (transitive dependency of DiffEngine)
- `System.Management.dll` (undeclared runtime dependency for process cleanup on .NET Core/.NET 5+)

**Note:** Some packages have undeclared dependencies that are loaded dynamically at runtime. These won't appear in the NuGet dependency graph but will cause `FileNotFoundException` at runtime. Test your extension thoroughly to discover these.

Use `dotnet publish` or inspect the build output to identify all required assemblies. Missing transitive dependencies cause `FileNotFoundException` at runtime with errors like:
```
System.IO.FileNotFoundException: Could not load file or assembly 'EmptyFiles, Version=...'
```

Register transitive dependencies in `MetalamaExtensionAssembly` items **before** the assemblies that depend on them.

### Separate Assembly Loading

For complex extensions, load each assembly separately via `MetalamaExtensionAssembly`.
This is required when:
- Different Roslyn version builds need the same dependency
- The dependency has its own complex initialization

### Why No ILMerge

Do NOT use ILMerge or similar tools. Instead:
- Use separate assemblies in the `metalama/` folder
- Register each via `MetalamaExtensionAssembly`
- Rely on the assembly loader to resolve dependencies

## Examples

### HtmlWriter (Simple Extension)

Location: `Metalama.Framework/src/Metalama.Extensions.HtmlWriter/`

- Single extension assembly with bundled DiffPlex dependency
- Implements `IProjectServiceFactory` to provide `IHtmlCodeWriter`
- Props file loads DiffPlex before HtmlWriter

### DiffEngine (Test Plugin)

Location: `Metalama.Framework/src/Metalama.Extensions.DiffEngine/`

- Test framework plugin for diff tools integration
- Uses **both** `MetalamaExtensionAssembly` (to load assemblies) and `MetalamaTestPlugIn` (to register plugin)
- Bundles transitive dependencies: `EmptyFiles.dll`, `DiffEngine.dll`
- Gracefully degrades when not installed

**Props file pattern for test plugins:**
```xml
<Project>
    <ItemGroup>
        <!-- Load dependencies first (in dependency order) -->
        <MetalamaExtensionAssembly Include="...EmptyFiles.dll" TargetFramework="net472" />
        <MetalamaExtensionAssembly Include="...DiffEngine.dll" TargetFramework="net472" />
        <!-- Load the extension assembly -->
        <MetalamaExtensionAssembly Include="...Metalama.Extensions.DiffEngine.dll" TargetFramework="net472" />
        <!-- Register the test plugin -->
        <MetalamaTestPlugIn Include="Metalama.Extensions.DiffEngine.DiffEngineRunner, Metalama.Extensions.DiffEngine" />
    </ItemGroup>
</Project>
```

**Important:** Test plugins that need runtime dependencies must register those dependencies via `MetalamaExtensionAssembly` items *before* the `MetalamaTestPlugIn` item. The test framework loads plugins by type name, but the assembly must already be available.

### Validation (Complex Extension - Premium)

Location: `Metalama.Premium/src/Metalama.Extensions.Validation*/`

- Three-tier structure: API + Engine + Package
- Multiple Roslyn version builds (4.8.0, 4.12.0, 5.0.0)
- Uses `PipelineExtension` for pipeline integration
- Registers services via `context.ServiceBuilder.Add()`

## Standalone Tests

Standalone tests validate extension packages by consuming them as end users would.

### Location and Structure

Location: `Metalama.Framework/src/tests/Standalone/`

Standalone tests **must use `PackageReference`** to reference Metalama packages (not `ProjectReference`). This ensures the test validates real package consumption, including:
- MSBuild props/targets integration
- `MetalamaExtensionAssembly` loading
- `MetalamaTestPlugIn` registration
- Dependency bundling

### Test Project Pattern

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFrameworks>net472;net8.0;net9.0</TargetFrameworks>
        <Nullable>enable</Nullable>
        <OutputType>Library</OutputType>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="xunit" />
        <PackageReference Include="xunit.runner.visualstudio" />
        <PackageReference Include="Metalama.Testing.AspectTesting" />
        <PackageReference Include="Metalama.Extensions.HtmlWriter" />
        <PackageReference Include="Metalama.Framework" />
    </ItemGroup>
</Project>
```

**Multi-targeting:** Standalone tests should target all supported frameworks (`net472;net8.0;net9.0`) to validate extension loading across all runtime environments.

### Debugging Extension Loading

When tests fail with assembly loading errors:

1. Check `%TEMP%\Metalama\CompileTimeTroubleshooting\...\errors.txt` for detailed error messages
2. Verify `MetalamaExtensionAssembly` items have correct `TargetFramework` metadata
3. Ensure dependencies are loaded before dependent assemblies
4. Check that all transitive dependencies are bundled in the package

## Common Issues and Troubleshooting

### Assembly Not Found (Leading/Trailing Whitespace)

**Symptom:** `Cannot find the assembly ' MyExtension'` (note the leading space)

**Cause:** Whitespace in `MetalamaTestPlugIn` type specification.

**Fix:** The test framework trims plugin type names, but verify your props file has no extra whitespace:
```xml
<!-- Correct -->
<MetalamaTestPlugIn Include="MyNamespace.MyPlugin, MyAssembly" />
<!-- Incorrect (trailing space) -->
<MetalamaTestPlugIn Include="MyNamespace.MyPlugin, MyAssembly " />
```

### Extension Assembly Loads for Wrong Framework

**Symptom:** Assembly version conflicts or type load exceptions.

**Cause:** Missing `TargetFramework` metadata on `MetalamaExtensionAssembly`.

**Fix:** Always specify `TargetFramework`:
```xml
<MetalamaExtensionAssembly Include="...net472/MyExtension.dll" TargetFramework="net472" />
<MetalamaExtensionAssembly Include="...net8.0/MyExtension.dll" TargetFramework="net8.0" />
<MetalamaExtensionAssembly Include="...net9.0/MyExtension.dll" TargetFramework="net9.0" />
```

### Service Not Resolved

**Symptom:** `GetService<IMyService>()` returns null.

**Cause:** Extension assembly not loaded or service factory not registered.

**Fix:**
1. Verify the package is referenced (check build output for props file import)
2. Ensure `[assembly: ExportExtension(typeof(MyServiceFactory), ExtensionKinds.ServiceFactory)]` is present
3. Check that `MetalamaExtensionAssembly` items are correctly specified in props files

### Sequence Contains No Matching Element

**Symptom:** `InvalidOperationException: Sequence contains no matching element` during test execution.

**Cause:** Extension code using `.Single()` or `.First()` on collections that may be empty in certain scenarios.

**Fix:** Use `.SingleOrDefault()` or `.FirstOrDefault()` with null checks:
```csharp
var result = collection.SingleOrDefault(x => x.Matches);
if (result == null)
{
    return; // Handle gracefully
}
```
