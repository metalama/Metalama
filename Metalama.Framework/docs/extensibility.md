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
    └── net10.0/
        └── MyExtension.dll        # Extension assembly for .NET 10
```

**Important:** Always target both frameworks: `net472` and `net10.0`. This ensures compatibility with all supported runtime environments.

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
    <TargetFrameworks>net472;net10.0</TargetFrameworks>
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
MyExtension.Engine.5.0.0/             # 5.0.0-specific build
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
    <Import Project="../../eng/RoslynVersions/Roslyn.5.0.0.props" />

    <!-- Import main project for other settings -->
    <Import Project="../MyExtension.Engine/MyExtension.Engine.csproj" />
</Project>
```
**Roslyn version props file** (`eng/RoslynVersions/Roslyn.X.X.X.props`): define a symbol only for a distinction that the source actually branches on. Metalama defines only `ROSLYN_5_10_0_OR_GREATER`, and only its aspect tests use it: both variants are Roslyn 5 and the engine source treats them alike. Match the existing `eng/RoslynVersions/Roslyn.<v>.props` files for the exact set:
```xml
<Project>
    <PropertyGroup>
        <ThisRoslynVersion>5.0.0</ThisRoslynVersion>
        <ThisRoslynVersionProjectSuffix>.5.0.0</ThisRoslynVersionProjectSuffix>
        <!-- No DefineConstants: the source does not branch on the variant. -->
    </PropertyGroup>
</Project>
```

**Props file registration** (use `TargetRoslynVersion` metadata):
```xml
<MetalamaExtensionAssembly
    Include="...MyExtension.Engine.5.10.0.dll"
    TargetFramework="net472"
    TargetRoslynVersion="5.10.0"/>
<MetalamaExtensionAssembly
    Include="...MyExtension.Engine.5.0.0.dll"
    TargetFramework="net472"
    TargetRoslynVersion="5.0.0"/>
```

**Conditional compilation in code:** define a constant in the props file of the variant that provides the newer API, and branch on it. Metalama itself needs no such constant today.

**Example:** `Metalama.Extensions.Validation` in Metalama.Premium uses this pattern with builds for Roslyn 5.0.0 and 5.10.0. The Roslyn 4.8.0 variant was retired with Metalama 2026.1 and the Roslyn 4.12.0 variant with Metalama 2027.0 (see `Directory.Packages.md`); historical references to either in extension repos should be migrated to `Roslyn.5.0.0.props`.

### Simple Extension Pattern (HtmlWriter)

For extensions with bundled dependencies, use this `.csproj` pattern:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFrameworks>net472;net10.0</TargetFrameworks>
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
            Include="$(MSBuildThisFileDirectory)../metalama/net10.0/SomeDependency.dll"
            TargetFramework="net10.0" />

        <!-- Then load the extension -->
        <MetalamaExtensionAssembly
            Include="$(MSBuildThisFileDirectory)../metalama/net472/MyExtension.dll"
            TargetFramework="net472" />
        <MetalamaExtensionAssembly
            Include="$(MSBuildThisFileDirectory)../metalama/net10.0/MyExtension.dll"
            TargetFramework="net10.0" />
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

### Contributions made through an adviser

An extension method of `IAdviser<T>` that registers a contribution gets the engine state behind the adviser with `AdviserExtensibility.GetExtensionContext` (namespace `Metalama.Framework.Engine.Advising`). The returned `AdviserExtensionContext` gives:

- `Owner`: the `IQueryOwner` to which the contributor is added. It is the aspect builder for an aspect, including the advisers that `With` and the introduction advice return, and the amender for a type fabric.
- `AspectTarget` and `TemplateProvider`. The template provider takes `WithTemplateProvider` into account.
- `ThrowIfDisposed()`, which throws when the aspect or the fabric has finished executing.
- `CreateQuery( declaration )`, a query of one declaration owned by `Owner`.
- `CaptureOrigin()`, which returns an `ExtensionContributionOrigin`: the predecessor, the description and the default template provider of the contribution, and the aspect layer to which the code that it produces is attributed.

A contribution made through a query captures its origin with `ExtensionContributionOrigin.Capture( queryImpl.Owner )`. The origin of a project or namespace fabric holds no aspect instance, because the fabric amender belongs to the long-lived pipeline configuration.

`AspectBuilderState.AddContributor` throws after `BuildAspect` has completed, because a contributor added later would not be part of the result of the aspect.

### Transforming hook

`PipelineExtension.ExecuteTransformingContributorsAsync( ExtensionTransformationContext, CancellationToken )` runs at the end of every high-level stage that runs the linker, after `ExecutePipelineContributorsAsync` and before the linker. It runs at compile time and in the preview, live-template and introspection scenarios, and not at design time. The context gives the contributors of the stage (`Contributors`) and those added by the aspects of the stage (`ContributorsAddedInStage`), the source compilation, the initial and final compilations of the stage, `HighLevelStageIndex`, `IsSourceStage` and a diagnostic sink. Only the first high-level stage has `IsSourceStage`; a low-level weaver starts a new stage.

### Source expressions for compile-time code

An extension can give compile-time code an expression of the source code without letting it be emitted. `SourceExpressionFactory.CreateInspectionOnly( expression, type )` (namespace `Metalama.Framework.Engine.Templating`) returns an `ISourceExpression` whose `AsSyntaxNode`, `AsString`, `AsFullString`, `AsTypedConstant` and `Type` behave as for any source expression, and which is not assignable. Emitting it in generated code reports LAMA0297, during a template expansion and through the textual conversion of expressions, because the expression is already evaluated at its original location: a second evaluation can have side effects, and it can reference local variables and parameters that do not exist in the generated code.

In the other direction, `SourceExpressionExtensions.GetSourceSyntax()` (SDK, namespace `Metalama.Framework.Engine.CodeModel`) returns the source `ExpressionSyntax` of an expression that wraps source syntax, for instance the initializer of a source field, or `null` for a generated expression, a parameter or a `TypedConstant`.

### Design-time hook

`PipelineExtension.ExecuteDesignTimePipelineContributorsAsync( DesignTimeContributorsContext, CancellationToken )` runs at the end of every high-level stage of the design-time pipeline that has extension contributors. The context gives `Contributors`, `ContributorsAddedInStage`, `NewContributors`, the initial and final compilations of the stage and `HighLevelStageIndex`.

The design-time pipeline accumulates the transitive contributors that the extensions return across the high-level stages, and the design-time result is read from the last stage. The contributors replayed from the contributor sources of the pipeline, for instance those of fabrics and of referenced projects, are part of `Contributors` in every stage. An extension that returns transitive contributors must therefore build them from `NewContributors`, which is `Contributors` in the first stage and `ContributorsAddedInStage` in the later stages. Otherwise, a pipeline split by a low-level weaver returns the transitive contributors of the replayed contributors once per stage. The inheritable aspects of every stage are accumulated in the same way.

### Project-local design-time results

By default, the design-time form of a transitive contributor (`ITransitivePipelineContributor.ToDesignTime`) is exported to the projects that reference the project: it is written to the design-time transitive manifest, and its presence makes the pipeline produce the manifest. A kind declared with `ContributorKind.IsProjectLocal` keeps its results in the project that produced them. Only `PipelineExtension.AnalyzeSemanticModel` of that project sees them, `ToTransitiveAspectManifestExtension` is never called for them, and they do not count in `DesignTimeAspectPipelineResultExtensionCollection.HasExportedContent`, which decides whether the manifest is produced. A kind cannot be both project-local and `IsDesignTimeValidator`; the `init` accessors throw `InvalidOperationException` for this combination.

### Shared index of source references

An extension that needs references of the source compilation returns its requirements from `PipelineExtension.GetSourceIndexRequirements( SourceIndexRequirementsContext )` instead of walking the syntax trees itself. `SourceReferenceIndexService.BeginStage` merges the requirements of all extensions into one `SourceReferenceIndexStage` per high-level stage, and the extension reads the index with `ExtensionTransformationContext.SourceReferenceIndex.GetIndexAsync()`. The index is built once, on the first read, so the extensions share the walk and the binding of member bodies.

- The names of the requirements are merged per reference kind, so a name that one extension requests for a kind does not admit references of another kind.
- When every extension that returned requirements also returned `DeclarationRoots`, the stage walks only those declarations. Otherwise it walks every syntax tree.
- `SourceIndexRequirementsContext.HighLevelStageIndex` lets an extension return `SourceIndexRequirements.None` in the stages where it reads nothing.
- At design time, `SourceReferenceIndexService.GetDesignTimeIndex( serviceProvider, semanticModel, extensions, cancellationToken )` returns one index per `SemanticModel`, built with `DesignTimeAspectPipelineResultExtensionCollection.IndexOptions`. These options include the requirements of the extensions of the project that implement `IDesignTimeReferenceIndexRequirementsProvider`, which are not exported to referencing projects.
- `SourceReferenceIndexService` is a static class and holds no state of a pipeline execution, because several pipelines can use one configuration at the same time. It is not a project service: a project service that stored the service provider of an execution would retain the compilation of that execution. The validators of Metalama.Premium still build their own index; their migration to the shared index is tracked as item F20 of the interceptor design.

### Redirection of call sites

The transforming hook can replace source call sites through `ExtensionTransformationContext.TransformationFactory` (namespace `Metalama.Framework.Engine.Extensibility.Transformations`). The engine creates one `ExtensionTransformationFactory` per high-level stage, shares it between the extensions, and completes it after the last extension. A request made after completion throws `InvalidOperationException`.

- `RedirectInvocation( origin, InvocationRedirectionRequest )` replaces an invocation of an ordinary or extension method by an invocation of a static method. `CallSiteReceiverMode` defines how the receiver of the source call is passed: it is dropped, passed as the first argument (by value, by `ref` or by `in`), or kept as the receiver of an extension method (`ExtensionReceiver`). Only `ExtensionReceiver` can be used inside a conditional access, because it keeps the short-circuit of `?.`.
- `InvocationRedirectionRequest.Arguments` gives the complete argument list of the new call with `RedirectedArgument.SourceReceiver`, `RedirectedArgument.SourceArgument( parameterOrdinal )` and `RedirectedArgument.Value( expression )`. The linker writes every argument with its parameter name and in the order of the source call, so the arguments are evaluated in their original order without temporary variables. A source argument that the new list omits must not be passed by reference. When it can have a side effect, the linker evaluates it into a discard in its source order, as the governing expression of a switch expression whose value is the next argument (`D switch { _ => next }`), or after the previous argument (`previous switch { var t => D switch { _ => t } }`) when the next one is passed by reference or does not exist. The factory refuses the request when no adjacent argument is passed by value, and below C# 9. `RedirectedArgument.SourceArgument( i ).WithCast( type )` writes a source argument passed by value as `(T)(argument)`, which keeps the conversion of the source call site when the parameter of the new target has another type. When the call site passes a `params` argument in expanded form, `RedirectedArgument.SourceArgument` of its parameter packs the elements into one collection, `[e1, e2]`, or `new T[] { e1, e2 }` before C# 12.
- The factory binds the rewritten call speculatively at the call site, exactly as the linker writes it, and refuses the request when the call does not bind to the target. This happens for instance when an overload of the target without its optional parameters exists, or when an instance method of the receiver hides an extension method.
- `ExtraArguments` appends named arguments, `TypeArguments` writes the type arguments explicitly, and `ResultCast` casts the result of the new call. A request can name the method that the call site already calls, to add named arguments to it.
- `RedirectMethodReference( origin, MethodReferenceRedirectionRequest )` replaces a method group that is converted to a delegate or to a function pointer, including an event subscription. The source method and the target must be static.
- `IsRedirected( node )` tells whether a request already exists for a node. A second request for the same node throws `InvalidOperationException`.
- `ExtensionTemplateServices.MethodTemplateExists( serviceProvider, templateProvider, name )` tells whether a template provider declares a method template of a given name, without throwing for a type that is not a template class of the project.

The factory validates each request against the semantic model of the final compilation of the stage and throws `ArgumentException` for a request that it cannot honor. The node of a request must belong to a syntax tree of that compilation. The origin must be attributed to an ordered aspect layer of the pipeline, and the generated syntax receives the annotation of the aspect class of the origin.

The factory does not change the code model. The injection step of the linker (`LinkerInjectionStep.Rewriter`) applies the redirections while it visits the source syntax trees, including the initializers of fields and events, constructor initializers and the base arguments of primary constructors. A redirection that the injection step does not reach, for example a call in the initializer of a field that an aspect promoted to a property, is reported with the warning LAMA0660 and the call site is kept unchanged.

The in-repository proof of concept in `src/tests/Metalama.Framework.Tests.ExtensionPoints.*` and `src/tests/Metalama.Framework.Tests.AspectTests.ExtensionPoints` uses these extension points with the public API only. None of these assemblies is in an `InternalsVisibleTo` list, so its tests fail to compile if an extension point needs internal API. The design of the extension points is in `docs/future/interceptors/`, sections 10.2 to 10.5 and 10.7.5.

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
- Multiple Roslyn version builds (5.0.0, 5.10.0)
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
        <TargetFrameworks>net472;net10.0</TargetFrameworks>
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

**Multi-targeting:** Standalone tests should target all supported frameworks (`net472;net10.0`) to validate extension loading across all runtime environments.

### Debugging Extension Loading

When tests fail with assembly loading errors:

1. Check `%TEMP%\Metalama\CompileTimeTroubleshooting\...\errors.txt` for detailed error messages
2. Verify `MetalamaExtensionAssembly` items have correct `TargetFramework` metadata
3. Ensure dependencies are loaded before dependent assemblies
4. Check that all transitive dependencies are bundled in the package

## ProjectReference vs PackageReference

When consuming extension packages, there's a critical difference between `ProjectReference` and `PackageReference` that affects how props files are imported.

### PackageReference (Automatic Import)

When using `PackageReference`, NuGet automatically imports props files from the `build/` and `buildTransitive/` folders. This is the standard pattern for end users consuming published packages:

```xml
<ItemGroup>
    <PackageReference Include="Metalama.Extensions.HtmlWriter" />
    <PackageReference Include="Metalama.Extensions.DiffEngine" />
</ItemGroup>
```

The `MetalamaExtensionAssembly` and `MetalamaTestPlugIn` items are automatically registered.

### ProjectReference (Direct MetalamaExtensionAssembly Items)

When using `ProjectReference` (common in internal test projects during development), MSBuild does **NOT** automatically import the props files from `build/` or `buildTransitive/` folders.

**Important:** Do NOT simply import the props files. The props files reference `metalama/` folder paths that only exist in NuGet packages, not in build output. Instead, add `MetalamaExtensionAssembly` items that point directly to the build output:

```xml
<ItemGroup>
    <ProjectReference Include="../../Metalama.Extensions.DiffEngine/Metalama.Extensions.DiffEngine.csproj" />
    <ProjectReference Include="../../Metalama.Extensions.HtmlWriter/Metalama.Extensions.HtmlWriter.csproj" />
</ItemGroup>

<!-- Register extension assemblies from build output (for ProjectReference) -->
<ItemGroup>
    <!-- HtmlWriter extension and its dependency -->
    <MetalamaExtensionAssembly Include="../../Metalama.Extensions.HtmlWriter/bin/$(Configuration)/net472/DiffPlex.dll" TargetFramework="net472" />
    <MetalamaExtensionAssembly Include="../../Metalama.Extensions.HtmlWriter/bin/$(Configuration)/net472/Metalama.Extensions.HtmlWriter.dll" TargetFramework="net472" />
    <!-- DiffEngine extension and its dependencies -->
    <MetalamaExtensionAssembly Include="../../Metalama.Extensions.DiffEngine/bin/$(Configuration)/net472/EmptyFiles.dll" TargetFramework="net472" />
    <MetalamaExtensionAssembly Include="../../Metalama.Extensions.DiffEngine/bin/$(Configuration)/net472/DiffEngine.dll" TargetFramework="net472" />
    <MetalamaExtensionAssembly Include="../../Metalama.Extensions.DiffEngine/bin/$(Configuration)/net472/Metalama.Extensions.DiffEngine.dll" TargetFramework="net472" />
    <!-- DiffEngine test plugin registration -->
    <MetalamaTestPlugIn Include="Metalama.Extensions.DiffEngine.DiffEngineRunner, Metalama.Extensions.DiffEngine" />
</ItemGroup>
```

**Key points:**
- Use `bin/$(Configuration)/$(TargetFramework)/` paths, not `metalama/` paths
- Include transitive dependencies (DiffPlex for HtmlWriter, EmptyFiles for DiffEngine)
- Register test plugins via `MetalamaTestPlugIn` if needed
- Always specify `TargetFramework` metadata on each assembly

**Without proper registration:**
- Extensions fail to load with `FileNotFoundException`
- Errors like "Could not find a part of the path '...metalama/net10.0/DiffPlex.dll'"

### Test Projects in Metalama.Framework

The following test projects use `ProjectReference` and require direct `MetalamaExtensionAssembly` registration:

| Project | Extensions Used |
|---------|-----------------|
| `Metalama.Framework.Tests.AspectTests` | HtmlWriter, DiffEngine |
| `Metalama.Framework.Tests.LinkerTests` | DiffEngine |
| `Metalama.Framework.Tests.TemplateTests` | DiffEngine |
| `Metalama.Framework.Tests.UnitTests` | DiffEngine |
| `Metalama.AspectWorkbench` | DiffEngine |

When adding new test projects that reference extension packages via `ProjectReference`, add the corresponding `MetalamaExtensionAssembly` items pointing to build output paths.

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
<MetalamaExtensionAssembly Include="...net10.0/MyExtension.dll" TargetFramework="net10.0" />
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
