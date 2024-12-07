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
using Metalama.Framework.Engine.DesignTime.CodeFixes;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Fabrics;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Licensing;
using Metalama.Framework.Engine.Metrics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Engine.Validation;
using Metalama.Framework.Project;
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

    private readonly bool _ownsDomain;

    protected IProjectOptions ProjectOptions { get; }

    protected CompileTimeDomain Domain { get; }

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
        ServiceProvider<IProjectService> serviceProvider,
        ExecutionScenario executionScenario,
        CompileTimeDomain? domain )
    {
        this.Logger = serviceProvider.GetLoggerFactory().GetLogger( "AspectPipeline" );

        this.ProjectOptions = serviceProvider.GetRequiredService<IProjectOptions>();

        // Set the execution scenario. In cases where we re-use the design-time pipeline for preview or introspection,
        // we replace the execution scenario for future services in the current pipeline.
        this.ServiceProvider = serviceProvider.WithService( executionScenario, true );

        // Setup the domain.
        if ( domain != null )
        {
            this.Domain = domain;
        }
        else
        {
            // Coverage: Ignore (tests always provide a domain).
            this.Domain = this.ServiceProvider.Global.GetRequiredService<ICompileTimeDomainFactory>().CreateDomain();
            this._ownsDomain = true;
        }
    }

    internal int PipelineInitializationCount { get; private set; }

    protected abstract SyntaxGenerationOptions GetSyntaxGenerationOptions();

    protected virtual bool TryInitialize(
        IDiagnosticAdder diagnosticAdder,
        Compilation compilation,
        ProjectLicenseInfo? projectLicenseInfo,
        IReadOnlyList<SyntaxTree>? compileTimeTreesHint,
        CancellationToken cancellationToken,
        [NotNullWhen( true )] out AspectPipelineConfiguration? configuration )
    {
        this.PipelineInitializationCount++;

        // Check that we have the system library.
        var objectType = compilation.GetSpecialType( SpecialType.System_Object );

        if ( objectType.Kind == SymbolKind.ErrorType ) { }

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

        // Prepare the compile-time assembly.
        var compileTimeProjectRepository = CompileTimeProjectRepository.Create(
            this.Domain,
            this.ServiceProvider,
            compilation,
            diagnosticAdder,
            false,
            projectLicenseInfo,
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
        var projectServiceProviderWithoutPlugins = this.ServiceProvider.WithCompileTimeProjectServices( compileTimeProjectRepository );
        var projectServiceProviderWithProject = projectServiceProviderWithoutPlugins;

        // Create compiler plug-ins found in compile-time code.

        projectServiceProviderWithProject = projectServiceProviderWithProject.WithService( compileTimeProject );

        var invoker = this.ServiceProvider.GetRequiredService<UserCodeInvoker>();

        var plugIns = compileTimeProject.ClosureProjects
            .SelectMany( p => p.PlugInTypes.SelectAsReadOnlyList( t => (Project: p, TypeName: t) ) )
            .Select(
                t =>
                {
                    var type = t.Project.GetType( t.TypeName );
                    var constructor = type.GetConstructor( Type.EmptyTypes );

                    if ( constructor == null )
                    {
                        diagnosticAdder.Report( GeneralDiagnosticDescriptors.TypeMustHavePublicDefaultConstructor.CreateRoslynDiagnostic( null, type ) );

                        return null;
                    }

                    var executionContext = new UserCodeExecutionContext(
                        projectServiceProviderWithoutPlugins,
                        diagnosticAdder,
                        UserCodeDescription.Create( "instantiating the plug-in {0}", type ) );

                    if ( !invoker.TryInvoke( () => Activator.CreateInstance( type ), executionContext, out var instance ) )
                    {
                        return null;
                    }
                    else
                    {
                        return instance;
                    }
                } )
            .WhereNotNull()
            .ToImmutableArray();

        projectServiceProviderWithProject = projectServiceProviderWithProject
            .WithServices( plugIns.OfType<IProjectService>() );

        // Initialize the licensing service with redistribution licenses.
        // Add the license verifier.
        var licenseConsumptionManager = projectServiceProviderWithProject.GetService<ProjectLicenseConsumer>();

        if ( licenseConsumptionManager != null )
        {
            var licenseVerifier = new LicenseVerifier( projectServiceProviderWithProject );

            if ( !licenseVerifier.TryInitialize( compileTimeProject, diagnosticAdder ) )
            {
                configuration = null;

                return false;
            }

            projectServiceProviderWithProject = projectServiceProviderWithProject.WithService( licenseVerifier );
        }

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
            new AttributeAspectOrderingSource( projectServiceProviderWithProject, compilation ),
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

        var fabricTopLevelAspectClass = new FabricTopLevelAspectClass( projectServiceProviderWithProject, compilationModel, compileTimeProject );
        var fabricAspectLayer = new OrderedAspectLayer( -1, -1, fabricTopLevelAspectClass.Layer );

        var allOrderedAspectLayers = orderedAspectLayers.Insert( 0, fabricAspectLayer );
        var allAspectClasses = new BoundAspectClassCollection( aspectClasses.As<IBoundAspectClass>().Add( fabricTopLevelAspectClass ) );

        // Execute fabrics.
        var fabricManager = new FabricManager( allAspectClasses, projectServiceProviderWithProject, compileTimeProject );
        var (fabricsConfiguration, fabricTypes) = fabricManager.ExecuteFabrics( compileTimeProject, compilationModel, projectModel, diagnosticAdder );

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

        // Return.
        configuration = new AspectPipelineConfiguration(
            this.Domain,
            stages,
            allAspectClasses,
            allOrderedAspectLayers,
            compileTimeProject,
            compileTimeProjectRepository,
            fabricsConfiguration,
            fabricTypes,
            projectModel,
            projectServiceProviderWithProject.WithService( eligibilityService ),
            this.CodeFixFilter );

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

    private static IEnumerable<Version> GetMetalamaVersions( Compilation compilation )
        => compilation.SourceModule.ReferencedAssemblies
            .Where( identity => identity.Name == "Metalama.Framework" )
            .Select( x => x.Version );

    private bool IsMetalamaEnabled( Compilation compilation )
        => this.ServiceProvider.Global.GetRequiredService<IMetalamaProjectClassifier>().TryGetMetalamaVersion( compilation, out _ );

    // It's simpler to analyze memory leaks when CodeFixFilter does not reference the AspectPipeline.
    private protected virtual CodeFixFilter CodeFixFilter => ( _, _ ) => false;

    // ReSharper disable UnusedParameter.Global
    private protected virtual PipelineContributorSources CreatePipelineContributorSources(
        AspectPipelineConfiguration configuration,
        CompilationContext compilationContext,
        CancellationToken cancellationToken )
    {
        var aspectClasses = configuration.BoundAspectClasses.ToImmutableArray<IAspectClass>();

        var transitiveAspectSource = new TransitivePipelineContributorSource( compilationContext, aspectClasses, configuration.ServiceProvider );

        var aspectSources = ImmutableArray.Create<IAspectSource>(
            new CompilationAspectSource( configuration.ServiceProvider, aspectClasses ),
            transitiveAspectSource );

        var validatorSources = ImmutableArray.Create<IValidatorSource>( transitiveAspectSource );

        var optionsSources = ImmutableArray.Create<IHierarchicalOptionsSource>( new CompilationHierarchicalOptionsSource( configuration.ServiceProvider ) );

        var allSources = new PipelineContributorSources( aspectSources, validatorSources, optionsSources, transitiveAspectSource, transitiveAspectSource );

        if ( configuration.FabricsConfiguration != null )
        {
            allSources = allSources.Add( configuration.FabricsConfiguration );
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
            if ( !this.TryInitialize( diagnosticAdder, compilation.Compilation, null, null, cancellationToken, out pipelineConfiguration ) )
            {
                return default;
            }
        }

        // We need to overridde execution scenario in this service provider as well.
        var executionScenario = this.ServiceProvider.GetRequiredService<ExecutionScenario>();

        pipelineConfiguration =
            pipelineConfiguration.WithServiceProvider( pipelineConfiguration.ServiceProvider.WithService( executionScenario, allowOverride: true ) );

        // When we reuse a pipeline configuration created from a different pipeline (e.g. design-time to code fix),
        // we need to substitute the code fix filter.
        pipelineConfiguration = pipelineConfiguration.WithCodeFixFilter( this.CodeFixFilter );

        if ( pipelineConfiguration.CompileTimeProject == null || pipelineConfiguration.BoundAspectClasses.Count == 0 )
        {
            // If there is no aspect in the compilation, don't execute the pipeline.
            return new AspectPipelineResult(
                compilation,
                pipelineConfiguration.ProjectModel,
                pipelineConfiguration );
        }

        var contributorSources = this.CreatePipelineContributorSources( pipelineConfiguration, compilation.CompilationContext, cancellationToken );

        var additionalCompilationOutputFiles = GetAdditionalCompilationOutputFiles( pipelineConfiguration.ServiceProvider );

        // Set up the options manager and the compilation model.
        var hierarchicalOptionsManager = new HierarchicalOptionsManager( pipelineConfiguration.ServiceProvider );

        var compilationModel = CompilationModel.CreateInitialInstance(
            pipelineConfiguration.ProjectModel,
            compilation,
            hierarchicalOptionsManager: hierarchicalOptionsManager,
            externalAnnotationProvider: contributorSources.ExternalAnnotationProvider );

        var diagnosticSink = new UserDiagnosticSink( pipelineConfiguration.CompileTimeProject );

        await hierarchicalOptionsManager.InitializeAsync(
            pipelineConfiguration.CompileTimeProject,
            contributorSources.OptionsSources,
            contributorSources.ExternalOptionsProvider,
            compilationModel,
            diagnosticSink,
            cancellationToken );

        diagnosticAdder.Report( diagnosticSink.ToImmutable().ReportedDiagnostics );

        // Execute the pipeline stages.
        var pipelineStageResult = new AspectPipelineResult(
            compilation,
            pipelineConfiguration.ProjectModel,
            pipelineConfiguration.AspectLayers,
            compilationModel,
            compilationModel,
            pipelineConfiguration,
            null,
            contributorSources,
            additionalCompilationOutputFiles: additionalCompilationOutputFiles );

        var allAspects = Enumerable.Empty<AspectInstanceResult>();
        var hasValidator = false;

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
                hasValidator |= stageResult.Value.HasDeclarationValidator || stageResult.Value.ReferenceValidators.Any();
            }
        }

        // Enforce licensing. Design-time licensing is handled elsewhere. (See usages of LicenseVerifier's methods.)
        if ( !executionScenario.IsDesignTime )
        {
            var licenseVerifier = pipelineConfiguration.ServiceProvider.GetService<LicenseVerifier>();

            if ( licenseVerifier != null )
            {
                var compileTimeProject = pipelineConfiguration.ServiceProvider.GetRequiredService<CompileTimeProject>();
                var licensingDiagnostics = new UserDiagnosticSink( compileTimeProject );
                licenseVerifier.VerifyCompilationResult( compileTimeProject, allAspects, hasValidator, licensingDiagnostics );
                pipelineStageResult = pipelineStageResult.WithAdditionalDiagnostics( licensingDiagnostics.ToImmutable() );
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
        => new NullPipelineStage( compileTimeProject, configuration.AspectLayers );

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

    protected virtual void Dispose( bool disposing )
    {
        if ( this._ownsDomain )
        {
            this.Domain.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose() => this.Dispose( true );
}