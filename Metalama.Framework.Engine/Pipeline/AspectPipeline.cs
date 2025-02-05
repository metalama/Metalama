// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.AdditionalOutputs;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Fabrics;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Metrics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Queries.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline;

/// <summary>
/// The base class for the main process of Metalama.
/// </summary>
public abstract class AspectPipeline : IDisposable
{
    private const string _highLevelStageGroupingKey = nameof(_highLevelStageGroupingKey);

    protected IProjectOptions ProjectOptions { get; }

    private CompileTimeDomain Domain { get; }

    // This member is intentionally protected because there can be one ServiceProvider per project,
    // but the pipeline can be used by many projects.
    public ProjectServiceProvider ServiceProvider { get; }

    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AspectPipeline"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="executionScenario"></param>
    /// <param name="domain">If <c>null</c>, the instance is created from the <see cref="ICompileTimeDomainFactory"/> service.</param>
    protected AspectPipeline(
        ProjectServiceProvider serviceProvider,
        ExecutionScenario executionScenario )
    {
        this.Logger = serviceProvider.GetLoggerFactory().GetLogger( "AspectPipeline" );

        this.ProjectOptions = serviceProvider.GetRequiredService<IProjectOptions>();

        // Set the execution scenario. In cases where we re-use the design-time pipeline for preview or introspection,
        // we replace the execution scenario for future services in the current pipeline.
        this.ServiceProvider = serviceProvider
            .WithService( executionScenario, true );

        this.Domain = serviceProvider.Global.GetRequiredService<CompileTimeDomain>();
    }

    internal int PipelineInitializationCount { get; private set; }

    protected abstract SyntaxGenerationOptions GetSyntaxGenerationOptions();

    protected virtual bool TryInitialize(
        IDiagnosticAdder diagnosticAdder,
        Compilation compilation,
        IReadOnlyList<SyntaxTree>? compileTimeTreesHint,
        CancellationToken cancellationToken,
        [NotNullWhen( true )] out AspectPipelineConfiguration? configuration )
    {
        this.PipelineInitializationCount++;

        // Check that Metalama is enabled for the project.            
        if ( !this.IsMetalamaEnabled( compilation ) || !this.ProjectOptions.IsFrameworkEnabled )
        {
            // Metalama not installed.

            diagnosticAdder.Report( GeneralDiagnosticDescriptors.MetalamaNotInstalled.CreateRoslynDiagnostic( null, default ) );

            configuration = null;

            return false;
        }

        // Check the Metalama version.
        var referencedMetalamaVersions = GetMetalamaVersions( compilation ).ToReadOnlyList();

        if ( referencedMetalamaVersions.Count > 1 || referencedMetalamaVersions[0] > EngineAssemblyMetadataReader.Instance.AssemblyVersion )
        {
            // Metalama version mismatch.

            diagnosticAdder.Report(
                GeneralDiagnosticDescriptors.MetalamaVersionNotSupported.CreateRoslynDiagnostic(
                    null,
                    (referencedMetalamaVersions.SelectAsArray( x => x.ToString() ),
                     EngineAssemblyMetadataReader.Instance.AssemblyVersion.ToString()) ) );

            configuration = null;

            return false;
        }

        // Extension assemblies have to be loaded before the compile-time project is created,
        // because the compile-time project can reference their types.
        var extensions = this.LoadExtensions( diagnosticAdder, compilation, this.ServiceProvider );

        // Prepare the compile-time assembly.
        var compileTimeProjectRepository = CompileTimeProjectRepository.Create(
            this.Domain,
            this.ServiceProvider,
            compilation,
            diagnosticAdder,
            false,
            compileTimeTreesHint,
            cancellationToken );

        if ( compileTimeProjectRepository == null )
        {
            this.Logger.Warning?.Log( $"TryInitialize('{this.ProjectOptions.AssemblyName}') failed: cannot get the compile-time compilation." );

            configuration = null;

            return false;
        }

        var compileTimeProject = compileTimeProjectRepository.RootProject;

        // Create a project-level service provider.
        var projectServiceProviderWithoutPlugins =
            this.ServiceProvider
                .WithCompileTimeProjectServices( compileTimeProjectRepository )
                .WithService( this.GetDiagnosticExtensionPolicy() );

        var projectServiceProviderWithProject = projectServiceProviderWithoutPlugins;

        // Create compiler plug-ins found in compile-time code and add them to the service provider.
        // We disable caching of the type-interface mapping in the service provider to makes sure the AssemblyLoadContext can be unloaded.
        projectServiceProviderWithProject = projectServiceProviderWithProject.WithService( compileTimeProject );

        var plugIns = this.LoadPlugIns( diagnosticAdder, compilation, compileTimeProject, projectServiceProviderWithoutPlugins );

        projectServiceProviderWithProject = projectServiceProviderWithProject
            .WithServices( plugIns.OfType<IProjectService>(), true );

        // Add extensions
        projectServiceProviderWithProject = projectServiceProviderWithProject.WithService( new PipelineExtensionProvider( extensions ) );

        var extensionInitializationContext =
            new PipelineExtensionContext( this.ProjectOptions, diagnosticAdder );

        foreach ( var extension in extensions )
        {
            this.Domain.AddAssembly( extension.GetType().Assembly );

            if ( !extension.Initialize( extensionInitializationContext ) )
            {
                this.Logger.Warning?.Log( $"The extension '{extension}' failed to initialize." );

                configuration = null;

                return false;
            }
        }

        projectServiceProviderWithProject = extensionInitializationContext.Services.Build( projectServiceProviderWithProject );

        // Set NormalizeWhitespace setting for the compilation.
        projectServiceProviderWithProject =
            projectServiceProviderWithProject.WithService( this.GetSyntaxGenerationOptions(), true );

        // Add MetricsManager.
        projectServiceProviderWithProject = projectServiceProviderWithProject.WithService( new MetricManager( projectServiceProviderWithProject ) );

        // Creates a project model that includes the final service provider.
        var projectModel = new ProjectModel( compilation, projectServiceProviderWithProject );

        // Create a compilation model for the aspect initialization.
        var compilationModel = CompilationModel.CreateInitialInstance( projectModel, compilation );

        // Create aspect types.
        var driverFactory = new AspectDriverFactory( compilationModel, plugIns, projectServiceProviderWithProject );
        var aspectTypeFactory = new AspectClassFactory( driverFactory, compilationModel.CompilationContext );

        var aspectClasses = aspectTypeFactory.GetClasses(
                projectServiceProviderWithProject,
                compileTimeProject,
                diagnosticAdder )
            .ToImmutableArray();

        // Get aspect parts and sort them.
        var aspectOrderSources = new IAspectOrderingSource[]
        {
            new AttributeAspectOrderingSource( projectServiceProviderWithProject, compilationModel.CompilationContext ),
            new AspectLayerOrderingSource( aspectClasses ),
            new FrameworkAspectOrderingSource( aspectClasses )
        };

        if ( !AspectLayerSorter.TrySort( aspectClasses, aspectOrderSources, diagnosticAdder, out var orderedAspectLayers ) )
        {
            this.Logger.Warning?.Log( $"TryInitialize('{this.ProjectOptions.AssemblyName}') failed: cannot sort aspect layers." );

            configuration = null;

            return false;
        }

        // Create other template classes.
        var otherTemplateClassFactory = new OtherTemplateClassFactory( compilationModel.CompilationContext );

        var otherTemplateClasses = otherTemplateClassFactory.GetClasses(
            projectServiceProviderWithProject,
            compileTimeProject,
            diagnosticAdder );

        projectServiceProviderWithProject = projectServiceProviderWithProject.WithService(
            new TemplateClassProvider( otherTemplateClasses.Concat<TemplateClass>( aspectClasses ).ToImmutableDictionary( x => x.FullName, x => x ) ) );

        // Add fabrics.

        var fabricTopLevelAspectClass = new FabricTopLevelAspectClass( projectServiceProviderWithProject, compilationModel );
        var fabricAspectLayer = new OrderedAspectLayer( -1, -1, fabricTopLevelAspectClass.Layer );

        var allOrderedAspectLayers = orderedAspectLayers.Insert( 0, fabricAspectLayer );
        var allAspectClasses = new AspectClassCollection( aspectClasses.As<IBoundAspectClass>().Add( fabricTopLevelAspectClass ) );

        // Execute fabrics.
        var fabricManager = new FabricManager( allAspectClasses, projectServiceProviderWithProject );
        var (fabricContributors, fabricTypes) = fabricManager.ExecuteFabrics( compileTimeProject, compilationModel, projectModel, diagnosticAdder );

        // Freeze the project model to prevent any further modification of configuration.
        projectModel.Freeze();

        var stages = allOrderedAspectLayers
            .GroupAdjacent( x => GetGroupingKey( x.AspectClass.AspectDriver ) )
            .Select(
                g =>
                    g.Key is _highLevelStageGroupingKey
                        ? new PipelineStageConfiguration( PipelineStageKind.HighLevel, g.ToImmutableArray(), null )
                        : new PipelineStageConfiguration( PipelineStageKind.LowLevel, g.ToImmutableArray(), (IAspectWeaver) g.Key ) )
            .ToImmutableArray();

        var eligibilityService = new EligibilityService( allAspectClasses );

        // Create the configuration.
        configuration = new AspectPipelineConfiguration(
            this.Domain,
            stages,
            allAspectClasses,
            allOrderedAspectLayers,
            compileTimeProject,
            compileTimeProjectRepository,
            fabricContributors,
            fabricTypes,
            projectModel,
            projectServiceProviderWithProject.WithService( eligibilityService ),
            extensions );

        return true;

        static object GetGroupingKey( IAspectDriver driver )
        {
            return driver switch
            {
                // weavers are not grouped together
                // Note: this requires that every AspectType has its own instance of IAspectWeaver
                IAspectWeaver weaver => weaver,

                // AspectDrivers are grouped together
                AspectDriver => _highLevelStageGroupingKey,

                _ => throw new AssertionFailedException( $"Invalid aspect driver type: {driver.GetType()}." )
            };
        }
    }

    private ImmutableArray<PipelineExtension> LoadExtensions(
        IDiagnosticAdder diagnosticAdder,
        Compilation compilation,
        ServiceProvider<IProjectService> serviceProvider )
    {
        var invoker = this.ServiceProvider.GetRequiredService<UserCodeInvoker>();

        // Load extensions.
        var extensionLoader = this.ServiceProvider.Global.GetService<IExtensionLoader>();

        ImmutableArray<PipelineExtension> extensions;

        if ( extensionLoader != null )
        {
            var extensionTypes = extensionLoader.GetExtensionTypes( this.ProjectOptions, this.Domain, ExtensionKind.Default );

            extensions = extensionTypes
                .Select( type => CreateInstanceOfType( type, diagnosticAdder, compilation, serviceProvider, invoker ) )
                .WhereNotNull()
                .Cast<PipelineExtension>()
                .ToImmutableArray();
        }
        else
        {
            extensions = ImmutableArray<PipelineExtension>.Empty;
        }

        // Add built-in extensions.
        extensions = extensions.Add( new DiagnosticQueryPipelineExtension() );

        return extensions;
    }

    private ImmutableArray<object> LoadPlugIns(
        IDiagnosticAdder diagnosticAdder,
        Compilation compilation,
        CompileTimeProject compileTimeProject,
        ServiceProvider<IProjectService> serviceProvider )
    {
        var invoker = this.ServiceProvider.GetRequiredService<UserCodeInvoker>();

        // Load plug-ins.
        var plugInTypes = compileTimeProject.ClosureProjects
            .SelectMany( p => p.PlugInTypes.SelectAsReadOnlyList( t => (Project: p, TypeName: t) ) )
            .Select( t => t.Project.GetType( t.TypeName ) );

        var plugIns =
            plugInTypes
                .Select( type => CreateInstanceOfType( type, diagnosticAdder, compilation, serviceProvider, invoker ) )
                .WhereNotNull()
                .ToImmutableArray();

        return plugIns;
    }

    private static object? CreateInstanceOfType(
        Type type,
        IDiagnosticAdder diagnosticAdder,
        Compilation compilation,
        ServiceProvider<IProjectService> serviceProvider,
        UserCodeInvoker invoker )
    {
        var constructor = type.GetConstructor( Type.EmptyTypes );

        if ( constructor == null )
        {
            diagnosticAdder.Report( GeneralDiagnosticDescriptors.TypeMustHavePublicDefaultConstructor.CreateRoslynDiagnostic( null, type ) );

            return null;
        }

        var executionContext = UserCodeExecutionContext.CreateInstance(
            serviceProvider,
            UserCodeDescription.Create( "instantiating '{0}'", type ),
            compilation.GetCompilationContext(),
            diagnostics: diagnosticAdder );

        if ( !invoker.TryInvoke( () => Activator.CreateInstance( type ), executionContext, out var instance ) )
        {
            return null;
        }
        else
        {
            return instance;
        }
    }

    private static IEnumerable<Version> GetMetalamaVersions( Compilation compilation )
        => compilation.SourceModule.ReferencedAssemblies
            .Where( identity => identity.Name == "Metalama.Framework" )
            .Select( x => x.Version );

    private bool IsMetalamaEnabled( Compilation compilation )
        => this.ServiceProvider.Global.GetRequiredService<IMetalamaProjectClassifier>().TryGetMetalamaVersion( compilation, out _ );

    // ReSharper disable UnusedParameter.Global
    private protected virtual PipelineContributorSources CreatePipelineContributorSources(
        AspectPipelineConfiguration configuration,
        CompilationContext compilationContext,
        CancellationToken cancellationToken )
    {
        var aspectClasses = configuration.BoundAspectClasses.ToImmutableArray<IAspectClass>();

        var contributors = ImmutableArray.CreateBuilder<IPipelineContributor>();

        var transitivePipelineContributorSource = new TransitivePipelineContributorSource( compilationContext, aspectClasses, configuration.ServiceProvider );
        contributors.Add( transitivePipelineContributorSource );
        contributors.AddRange( transitivePipelineContributorSource.ExtensionContributors );
        contributors.Add( new CompilationAspectSource( configuration.ServiceProvider, aspectClasses ) );
        contributors.Add( new CompilationHierarchicalOptionsSource( configuration.ServiceProvider ) );

        var allSources = new PipelineContributorSources( contributors.ToImmutable(), transitivePipelineContributorSource, transitivePipelineContributorSource );

        if ( configuration.FabricsContributors != null )
        {
            allSources = allSources.Add( configuration.FabricsContributors );
        }

        return allSources;
    }

    private static ImmutableArray<AdditionalCompilationOutputFile> GetAdditionalCompilationOutputFiles( in ProjectServiceProvider serviceProvider )
    {
        var provider = serviceProvider.GetService<IAdditionalOutputFileProvider>();

        if ( provider == null )
        {
            return ImmutableArray<AdditionalCompilationOutputFile>.Empty;
        }

        return provider.GetAdditionalCompilationOutputFiles();
    }

    protected virtual IDiagnosticExtensionPolicy GetDiagnosticExtensionPolicy() => ConstantDiagnosticExtensionPolicy.None;

    /// <summary>
    /// Executes the all stages of the current pipeline, report diagnostics, and returns the last <see cref="AspectPipelineResult"/>.
    /// </summary>
    /// <returns><c>true</c> if there was no error, <c>false</c> otherwise.</returns>
    protected async Task<FallibleResult<AspectPipelineResult>> ExecuteAsync(
        PartialCompilation compilation,
        IDiagnosticAdder diagnosticAdder,
        AspectPipelineConfiguration? pipelineConfiguration,
        TestableCancellationToken cancellationToken )
    {
        if ( pipelineConfiguration == null )
        {
            if ( !this.TryInitialize( diagnosticAdder, compilation.Compilation, null, cancellationToken, out pipelineConfiguration ) )
            {
                return default;
            }
        }

        var serviceProvider = pipelineConfiguration.ServiceProvider;

        // We need to overridde execution scenario in this service provider as well.
        var executionScenario = this.ServiceProvider.GetRequiredService<ExecutionScenario>();
        serviceProvider = serviceProvider.WithService( executionScenario, allowOverride: true );

        // Update the pipeline configuration.
        pipelineConfiguration = pipelineConfiguration.WithServiceProvider( serviceProvider );

        if ( pipelineConfiguration.CompileTimeProject == null || pipelineConfiguration.BoundAspectClasses.Count == 0 )
        {
            // If there is no aspect in the compilation, don't execute the pipeline.
            return new AspectPipelineResult(
                compilation,
                pipelineConfiguration.ProjectModel,
                pipelineConfiguration );
        }

        var contributorSources = this.CreatePipelineContributorSources( pipelineConfiguration, compilation.CompilationContext, cancellationToken );

        var additionalCompilationOutputFiles = GetAdditionalCompilationOutputFiles( serviceProvider );

        // Set up the options manager and the compilation model.
        var hierarchicalOptionsManager = new HierarchicalOptionsManager( serviceProvider );

        var compilationModel = CompilationModel.CreateInitialInstance(
            pipelineConfiguration.ProjectModel,
            compilation,
            hierarchicalOptionsManager: hierarchicalOptionsManager,
            externalAnnotationProvider: contributorSources.ExternalAnnotationProvider );

        var initializationDiagnosticSink = new UserDiagnosticSink( serviceProvider );

        await hierarchicalOptionsManager.InitializeAsync(
            pipelineConfiguration.CompileTimeProject,
            contributorSources.Contributors.OfType<IHierarchicalOptionsSource>(),
            contributorSources.ExternalOptionsProvider,
            compilationModel,
            initializationDiagnosticSink,
            cancellationToken );

        // Execute the extensions provided by fabrics.
        var fabricContributors = pipelineConfiguration.FabricsContributors?.Contributors ?? default;

        if ( !fabricContributors.IsDefaultOrEmpty )
        {
            foreach ( var extension in pipelineConfiguration.Extensions )
            {
                await extension.ExecuteContributorsAsync(
                    pipelineConfiguration,
                    compilationModel,
                    initializationDiagnosticSink,
                    fabricContributors,
                    cancellationToken );
            }
        }

        // We pass the initialization diagnostics to the pipeline so they are merged with the pipeline results and
        // we don't need to handle diagnostics, suppressions or code fixes separately.
        var initializationDiagnostics = initializationDiagnosticSink.ToImmutable();

        // Execute the pipeline stages.
        var pipelineStageResult = new AspectPipelineResult(
            compilation,
            pipelineConfiguration.ProjectModel,
            pipelineConfiguration.AspectLayers,
            compilationModel,
            compilationModel,
            pipelineConfiguration,
            initializationDiagnostics,
            contributorSources,
            additionalCompilationOutputFiles: additionalCompilationOutputFiles );

        var allAspects = Enumerable.Empty<AspectInstanceResult>();

        foreach ( var stageConfiguration in pipelineConfiguration.Stages )
        {
            var stage = this.CreateStage( stageConfiguration, pipelineConfiguration.CompileTimeProject );

            if ( stage == null )
            {
                // This stage is skipped in the current pipeline (e.g. design-time).

                continue;
            }

            var stageResult = await stage.ExecuteAsync( pipelineConfiguration, pipelineStageResult, diagnosticAdder, cancellationToken );

            if ( !stageResult.IsSuccessful )
            {
                return default;
            }
            else
            {
                pipelineStageResult = stageResult.Value;
                allAspects = allAspects.Union( stageResult.Value.AspectInstanceResults );
            }
        }

        // Report diagnostics
        foreach ( var diagnostic in pipelineStageResult.Diagnostics.ReportedDiagnostics )
        {
            cancellationToken.ThrowIfCancellationRequested();
            diagnosticAdder.Report( diagnostic );
        }

        return FallibleResult<AspectPipelineResult>.Succeeded( pipelineStageResult );
    }

    /// <summary>
    /// Creates an instance of <see cref="HighLevelPipelineStage"/>.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="compileTimeProject"></param>
    /// <returns></returns>
    private protected virtual HighLevelPipelineStage CreateHighLevelStage(
        PipelineStageConfiguration configuration,
        CompileTimeProject compileTimeProject )
        => new NullPipelineStage( configuration.AspectLayers );

    private protected virtual LowLevelPipelineStage? CreateLowLevelStage( PipelineStageConfiguration configuration ) => null;

    private PipelineStage? CreateStage( PipelineStageConfiguration configuration, CompileTimeProject project )
    {
        switch ( configuration.Kind )
        {
            case PipelineStageKind.LowLevel:
                return this.CreateLowLevelStage( configuration );

            case PipelineStageKind.HighLevel:

                return this.CreateHighLevelStage( configuration, project );

            default:

                throw new NotSupportedException();
        }
    }

    protected virtual void Dispose( bool disposing ) { }

    /// <inheritdoc/>
    public void Dispose() => this.Dispose( true );
}