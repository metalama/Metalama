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

- `QueryOwner`: the `IQueryOwner` to which the contributor is added. It is the aspect builder for an aspect, including the advisers that `With` and the introduction advice return, and the amender for a type fabric.
- `AspectTarget` and `TemplateProvider`. The template provider takes `WithTemplateProvider` into account.
- `ThrowIfDisposed()`, which throws when the aspect or the fabric has finished executing.
- `CreateQuery( declaration )`, a query of one declaration owned by `QueryOwner`.
- `CaptureOrigin()`, which returns an `ExtensionContributionOrigin`: the predecessor, the description and the default template provider of the contribution, and the aspect layer to which the code that it produces is attributed.

A contribution made through a query captures its origin with `ExtensionContributionOrigin.Capture( queryImpl.Owner )`. The origin of a project or namespace fabric holds no aspect instance, because the fabric amender belongs to the long-lived pipeline configuration.

`AspectBuilderState.AddContributor` throws after `BuildAspect` has completed, because a contributor added later would not be part of the result of the aspect.

### Transforming hook

`PipelineExtension.ExecuteTransformingContributorsAsync( ExtensionTransformationContext, CancellationToken )` runs once per pipeline execution, on the source compilation, after `ExecutePipelineContributorsAsync` and before the linker. It runs at compile time and in the preview, live-template and introspection scenarios, and not at design time. The context gives the contributors (`Contributors`), the source compilation (`SourceCompilation`), the compilation that results from the aspects (`FinalCompilation`), `SourceCompilationWithFinalAspects`, which binds the source compilation to the aspects, and a diagnostic sink.

The extension hooks do not support low-level aspect weavers. They run before any weaver, and a contribution made by an aspect that executes after a weaver is not processed. The error LAMA0091 is reported for such a contribution.

### Source expressions for compile-time code

An extension can give compile-time code an expression of the source code without letting it be emitted. `SourceExpressionFactory.CreateInspectionOnly( expression, type )` (namespace `Metalama.Framework.Engine.Templating`) returns an `ISourceExpression` whose `AsSyntaxNode`, `AsString`, `AsFullString`, `AsTypedConstant` and `Type` behave as for any source expression, and which is not assignable. Emitting it in generated code reports LAMA0297, during a template expansion and through the textual conversion of expressions, because the expression is already evaluated at its original location: a second evaluation can have side effects, and it can reference local variables and parameters that do not exist in the generated code.

In the other direction, `SourceExpressionExtensions.GetSourceSyntax()` (SDK, namespace `Metalama.Framework.Engine.CodeModel`) returns the source `ExpressionSyntax` of an expression that wraps source syntax, for instance the initializer of a source field, or `null` for a generated expression, a parameter or a `TypedConstant`.

### Design-time hook

`PipelineExtension.ExecuteDesignTimePipelineContributorsAsync( DesignTimeContributorsContext, CancellationToken )` runs once per execution of the design-time pipeline, on the source compilation, when there are extension contributors. The context gives the contributors (`Contributors`), the source compilation (`SourceCompilation`) and the compilation that results from the aspects (`FinalCompilation`).

As the transforming hook at compile time, the hook runs before any low-level aspect weaver, and a contributor added by an aspect that executes after a weaver is not processed. The transitive contributors that the hook returns are kept in the design-time result.

### Project-local design-time results

By default, the design-time form of a transitive contributor (`ITransitivePipelineContributor.ToDesignTime`) is exported to the projects that reference the project: it is written to the design-time transitive manifest, and its presence makes the pipeline produce the manifest. A kind declared with `ContributorKind.IsProjectTransitive = false`, which is a project-local kind, keeps its results in the project that produced them. Only `PipelineExtension.AnalyzeSemanticModel` of that project sees them, `ToTransitiveAspectManifestExtension` is never called for them, and they do not count in `DesignTimeAspectPipelineResultExtensionCollection.HasExportedContent`, which decides whether the manifest is produced. A kind that is `IsDesignTimeValidator` must be project-transitive; the `init` accessors throw `InvalidOperationException` for a project-local validator kind.

### Shared index of source references

An extension that needs references of the source compilation returns its requirements from `PipelineExtension.GetSourceIndexRequirements( SourceIndexRequirementsContext )` instead of walking the syntax trees itself. `SourceReferenceIndexService.Create` merges the requirements of all extensions into one `SourceReferenceIndex` per pipeline execution, and the extension reads the index with `ExtensionTransformationContext.SourceReferenceIndex.GetIndexAsync()`. `GetSourceIndexRequirements` is called once per pipeline execution. The index is built once, on the first read, so the extensions share the walk and the binding of member bodies. The pipeline disposes the index when the linker has completed.

- The names of the requirements are merged per reference kind, so a name that one extension requests for a kind does not admit references of another kind.
- When every extension that returned requirements also returned `DeclarationRoots`, the index covers only those declarations. Otherwise it covers every syntax tree. A default `DeclarationRoots` array means every syntax tree.
- At design time, `SourceReferenceIndexService.GetDesignTimeIndex( serviceProvider, semanticModel, extensions, cancellationToken )` returns one index per `SemanticModel`, built with `DesignTimeAspectPipelineResultExtensionCollection.IndexOptions`. These options add the requirements of the design-time results of the project that implement `IDesignTimeReferenceIndexRequirementsProvider` to `Options`, which are the options that referencing projects merge. A result that implements this interface should have a project-local kind (`ContributorKind.IsProjectTransitive = false`), so that the result itself is not exported to referencing projects either.
- `SourceReferenceIndexService` is a static class and holds no state of a pipeline execution, because several pipelines can use one configuration at the same time. It is not a project service: a project service that stored the service provider of an execution would retain the compilation of that execution. The validators of Metalama.Premium still build their own index; their migration to the shared index is tracked as item F20 of the interceptor design.

### Arguments in the forms of advice arguments

An extension that accepts arguments in the forms of the `args` and `tags` of advice, which are an anonymous object, an `IReadOnlyDictionary<string, object?>` or an `IObjectReader`, reads them with the project service `IObjectReaderFactory` (namespace `Metalama.Framework.Services`), which `Metalama.Framework` declares and the engine implements. Compile-time code that does not reference the engine gets it from `declaration.Compilation.Project.ServiceProvider`.

### Redirection of call sites

The transforming hook can replace source call sites through `ExtensionTransformationContext.TransformationFactory` (namespace `Metalama.Framework.Engine.Extensibility.CallSites`, with `ExtensionTemplateServices` in `Metalama.Framework.Engine.Extensibility.Transformations`). The engine creates one `ExtensionTransformationFactory` per pipeline execution, shares it between the extensions, and completes it after the last extension. A request made after completion throws `InvalidOperationException`.

- `RedirectInvocation( origin, InvocationRedirectionRequest )` replaces an invocation of an ordinary or extension method by an invocation of a static method, or of an instance method declared with `DeclareMethod`. `CallSiteReceiverMode` defines how the receiver of the source call is passed: it is dropped, or passed as the first argument, by value, by `ref` or by `in`. An ordinary call site is written in the fully qualified static form. A call site in a conditional access, `a?.M( x )`, accepts only `FirstArgument`: the factory writes it as a call of a forwarder, `a?.F( x )`, so that the receiver stays in the conditional access and the compiler keeps its short-circuit. The forwarder is an extension method with the parameters of the target, whose body calls the target with its fully qualified name. The linker generates the forwarders in one internal static class of the global namespace per project, `__MetalamaCallSites_<assembly name>`, in the syntax tree `MetalamaCallSites.cs`, so no directive is added to the file of the call site. The factory refuses the request when the receiver converts to the first parameter by another conversion than an identity, reference or boxing conversion, when a top-level class cannot access the target, when the target is introduced by an aspect, and when the name of the forwarder already designates a member on the receiver at the call site.
- `InvocationRedirectionRequest.Arguments` gives the complete argument list of the new call with `RedirectedArgument.SourceReceiver`, `RedirectedArgument.SourceArgument( parameterOrdinal )` and `RedirectedArgument.Value( expression )`. `Value` accepts an `ExpressionSyntax`, or an `IExpression` of the code model, such as a `TypedConstant` or a parameter of the calling member, whose syntax the factory generates in the context of the call site. A `TypedConstant` whose value is a type is written as `typeof`. A parameter of the calling member is written as its name. Since C# 8, a parameter of a lambda or local function, or a local variable declared in it, can have the same name and hide the parameter at the call site. The factory then asks the linker to rename each variable that hides it between the call site and the member, with its declaration and its references, to the first name of the form `name_1`, `name_2` and so on that the member does not contain. A member of an anonymous object or an element of a tuple that takes its name from a renamed variable receives this name explicitly. The factory refuses the request when such a variable is used in a `nameof` expression, or when the name designates another kind of symbol. The linker reports the warning LAMA0661 for each renamed variable, because the compiled code no longer matches the source code that the debugger shows. An inspection-only source expression cannot be passed this way: the source argument or the source receiver is passed instead. A source argument can be passed more than once only when it is passed by value and its evaluation has no side effect, for instance a constant, a local or a parameter. The linker writes every argument with its parameter name and in the order of the source call, so the arguments are evaluated in their original order without temporary variables. A source argument that the new list omits must not be passed by reference. When it can have a side effect, the linker evaluates it in its source order with the run-time helper `Metalama.Framework.RunTime.CallSiteHelper`: `DropBefore( D, next )` before the next argument, or `DropAfter( previous, D )` after the previous one when the next one cannot hold it. Several consecutive dropped values are passed as one tuple, and the type arguments are written when the kept value has no natural type. The factory refuses the request when no adjacent argument is passed by value, or when a dropped or kept value has a ref struct or pointer type. `RedirectedArgument.SourceArgument( i ).WithCast( type )` writes a source argument passed by value as `(T)(argument)`, which keeps the conversion of the source call site when the parameter of the new target has another type. When the call site passes a `params` argument in expanded form, `RedirectedArgument.SourceArgument` of its parameter packs the elements into one collection, `[e1, e2]`, or `new T[] { e1, e2 }` before C# 12.
- The factory binds the rewritten call speculatively at the call site, exactly as the linker writes it, and refuses the request when the call does not bind to the target. This happens for instance when an overload of the target without its optional parameters exists, or when an instance method of the receiver hides an extension method.
- `ExtraArguments` appends named arguments, `TypeArguments` writes the type arguments explicitly, and `ResultCast` casts the result of the new call. A request can name the method that the call site already calls, to add named arguments to it.
- `RedirectMethodReference( origin, MethodReferenceRedirectionRequest )` replaces a method group that is converted to a delegate or to a function pointer, including an event subscription. The source method and the target must be static.
- `IsRedirected( node )` tells whether a request already exists for a node. A second request for the same node throws `InvalidOperationException`.
- `ExtensionTemplateServices.MethodTemplateExists( serviceProvider, templateProvider, name )` tells whether a template provider declares a method template of a given name, without throwing for a type that is not a template class of the project. `TryGetMethodTemplateParameters` returns the parameters of such a template, with their types in a given compilation, and tells the compile-time parameters from the run-time parameters.

The factory validates each request against the semantic model of the final compilation and throws `ArgumentException` for a request that it cannot honor. The node of a request must belong to a syntax tree of that compilation. The origin must be attributed to an ordered aspect layer of the pipeline, and the generated syntax receives the annotation of the aspect class of the origin.

The factory does not change the code model. The injection step of the linker (`LinkerInjectionStep.Rewriter`) applies the redirections while it visits the source syntax trees, including the initializers of fields and events, constructor initializers and the base arguments of primary constructors. A redirection that the injection step does not reach, for example a call in the initializer of a field that an aspect promoted to a property, is reported with the warning LAMA0660 and the call site is kept unchanged.

### Methods declared from templates

The factory can also declare methods whose body is generated from a template, and redirect call sites to them (namespace `Metalama.Framework.Engine.Extensibility.Synthesis`). Template interceptors are built on these primitives.

- `DeclareStaticClass( origin, SynthesizedStaticClassRequest )` declares an internal static class in a namespace given by its full name. The parts of the namespace that do not exist are declared. A numeric suffix is added to the name when a type of that name exists.
- `DeclareMethod( origin, SynthesizedMethodRequest )` declares a method in a class or struct of the compilation, including a type introduced by an aspect, or in a declared static class. The request gives a name hint, a delegate that sets the signature on an `IMethodBuilder` (accessibility, static or instance, return type, parameters, type parameters and their constraints), a `SynthesizedMethodTemplate`, and a delegate that returns the `ProceedBinding` of `meta.Proceed()`. The factory gives the method the first name of the form `hint`, `hint1`, `hint2` that no member of the type or of its base types has. A caller that needs a deterministic output therefore declares its methods in a deterministic order.
- `CreateMethodBuilder( origin, placement, nameHint, initialize, restrictions )` creates the builder of a method that `DeclareMethod` declares later, so that an extension can give a pre-filled builder to other code, for instance the `configure` delegate of an interceptor. The `initialize` delegate sets the initial signature. Then the `restrictions` apply. They are an instance of a class derived from `MethodBuilderRestrictions` (namespace `Metalama.Framework.Engine.CodeModel.Introductions.Builders`), which the caller defines. The builder calls one of its `Validate*` methods before each change of the method, of a parameter, of a type parameter or of an attribute, and only when the value changes. The method refuses the change by throwing an exception, normally `InvalidOperationException`, so the code that makes the change receives the error at the statement that makes it. The default implementation of each method accepts the change. `LockedSignatureRestrictions` is the built-in implementation: it locks the reference kinds and the positions of the `LockedLeadingParameterCount` first parameters, the return type and the name. The `LockedTypeParameterCount` first type parameters cannot be renamed, and their constraints can be added but not removed or relaxed. It can refuse new type parameters. The builders expose their type parameters as an `ITypeParameterBuilderList`, through the `TypeParameters` property of `IMethodBuilder`, `INamedTypeBuilder` and `IDelegateBuilder`. Type parameters can be added after the locked ones, so a method whose template is not generic can still be made generic by the code that receives the builder. Parameters cannot be removed through `IMethodBuilder`, and a parameter cannot be inserted before a locked one, so the locked parameters are always present. `new SynthesizedMethodRequest( builder, template, createProceedBinding )` declares the method from such a builder. `DeclareMethod` refuses a builder that the factory did not create, or that was already declared, and uses the name of the builder as the name hint. The request that takes an `Action<IMethodBuilder>` creates a builder in the same way.
- The template is selected by `MethodTemplateSelection`, as for an override, and bound by `TemplateBindingHelper.ForSynthesizedMethod`. A run-time parameter of the template binds by name, or else by its position among the parameters that are neither among the `HiddenLeadingParameterCount` first parameters nor among the `NameOnlyTrailingParameterCount` last ones. A run-time template parameter cannot have a default value or be `params`. A run-time type parameter of the template binds to the type parameter of the declared method at the same position among the run-time type parameters, as for an override, and its constraints must be compatible. An error is thrown as `ArgumentException`.
- `ProceedBinding.InvokeStatic` and `InvokeOnParameter` describe the call of `meta.Proceed()`: a static method, or an instance method on a parameter, with the parameters of the declared method as arguments and optional casts. The call keeps virtual dispatch. A `void` method is never awaited, because the invoked method can be an `async void` method of another assembly.
- `SynthesizedMethodTemplate.MetaExtensions` gives the objects that the template reads through `meta.GetExtension<T>()`. `SynthesizedMethodRequest.DiagnosticLocation` relocates the diagnostics of the template, for instance to a call site, because the declared method has no source code.
- `CallSiteRedirectionTarget` is abstract. `CallSiteRedirectionTarget.Existing( method )` creates an `ExistingCallSiteRedirectionTarget`, and `CallSiteRedirectionTarget.Synthesized( handle )` creates a `SynthesizedCallSiteRedirectionTarget`, whose `Handle` is the handle of the declared method. `Kind` tells them apart. The redirection passes `TypeArguments` to a generic target, including the type arguments of the type parameters that were added to the builder.
- `CallSiteRedirectionTarget.Synthesized( handle )` redirects a call site to a declared method. An instance method is called on `this`. It is accepted only in an instance member of its declaring type or of a derived type, outside a static lambda or local function, outside a lambda or local function of a struct, and outside a field initializer, a constructor initializer and an attribute. A call site in a conditional access is redirected through a forwarder, which requires a static method that a top-level class can access, in a type that is neither generic nor nested.

#### Visibility of declared types and methods

The types and methods that `DeclareStaticClass`, `DeclareMethod` and `CreateMethodBuilder` produce are not visible in the code model to aspects, in any stage of the pipeline. The factory runs after all aspects, and it adds the declarations only to the compilation that the linker sees. The design-time pipeline does not run the transforming hook, so the code that the IDE generates does not contain them either. A builder created by `CreateMethodBuilder` is detached: its declaring type does not list it, and a declared static class is not in `ICompilation.Types`. A template that implements a declared method reads it through `meta.Target`, but it cannot find it through the members of the compilation. An extension that exposes these declarations to its users, such as the interceptors, documents this restriction in its own API.

The factory adds the declarations to the final compilation that only the linker sees. A declared method is an `IntroduceMethodTransformation` with a default body and a `SynthesizedMethodBodyTransformation` that overrides it, so the linker processes it like a method that an aspect introduces and inlines the override. The template is expanded by the linker. When the expansion fails, the template has reported an error, and the call sites redirected to the method are left unchanged without LAMA0660. A project or namespace fabric has no aspect instance, so the transformations of its requests are attributed to an aspect instance of the top-level fabric aspect class, whose predecessor is the fabric. The lexical scope of a type that has no syntax, such as a declared static class or a type introduced by an aspect, is built from the code model, so that the locals of a template do not collide with the parameters of the method.

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
