// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.DesignTime.Contracts.EntryPoint;
using Metalama.Framework.DesignTime.Contracts.Pipeline;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Pipeline.Dependencies;
using Metalama.Framework.DesignTime.Pipeline.Diff;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Pipeline.LiveTemplates;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Caching;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Pipeline;

/// <summary>
/// The design-time implementation of <see cref="AspectPipeline"/>.
/// </summary>
/// Must be public because of testing.
public sealed partial class DesignTimeAspectPipeline : BaseDesignTimeAspectPipeline
{
    private static readonly string _sourceGeneratorAssemblyName = typeof(DesignTimeAspectPipelineFactory).Assembly.GetName().Name.AssertNotNull();

    private readonly WeakCache<Compilation, FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>> _compilationResultCache = new();
    private readonly RecursiveFileSystemWatcher? _fileSystemWatcher;
    private readonly ConcurrentQueue<Func<AsyncExecutionContext, ValueTask>> _jobQueue = new();
    private readonly IDesignTimeAspectPipelineObserver? _observer;
    private readonly SemaphoreSlim _sync = new( 1, 1 );
    private readonly IDesignTimeEntryPointConsumer? _entryPointConsumer;
    private readonly AnalysisProcessEventHub? _eventHub;
    private readonly DesignTimeAspectPipelineFactory _pipelineFactory;
    private readonly ITaskRunner _taskRunner;
    private readonly ProjectVersionProvider _projectVersionProvider;
    private readonly IUserDiagnosticRegistrationService? _userDiagnosticsRegistrationService;
    private readonly DesignTimeExceptionHandler _exceptionHandler;

    private bool _mustProcessQueue;

    // This field should not be changed directly, but only through the SetState method.
    private PipelineState _currentState;

    private ulong _aspectClassesHashCode;
    private ulong _aspectInstancesHashCode;

    private int _pipelineExecutionCount;

    internal ProjectKey ProjectKey { get; }

    /// <summary>
    /// Gets the number of times the pipeline has been executed. Useful for testing purposes.
    /// </summary>
    internal int PipelineExecutionCount => this._pipelineExecutionCount;

    // ReSharper disable once InconsistentlySynchronizedField
    internal DesignTimeAspectPipelineStatus Status => this._currentState.Status;

    internal DependencyGraph Dependencies => this._currentState.Dependencies;

    internal ImmutableArray<PortableExecutableReference> MetadataReferences { get; }

    internal DesignTimeAspectPipeline(
        DesignTimeAspectPipelineFactory pipelineFactory,
        IProjectOptions projectOptions,
        ProjectKey projectKey,
        ImmutableArray<PortableExecutableReference> metadataReferences )
        : base( GetServiceProvider( pipelineFactory.ServiceProvider, projectOptions, metadataReferences ) )
    {
        this.ProjectKey = projectKey;
        this.MetadataReferences = metadataReferences;
        this._pipelineFactory = pipelineFactory;
        this._entryPointConsumer = (IDesignTimeEntryPointConsumer?) this.ServiceProvider.Global.Underlying.GetService( typeof(IDesignTimeEntryPointConsumer) );
        this._projectVersionProvider = this.ServiceProvider.Global.GetRequiredService<ProjectVersionProvider>();
        this._observer = this.ServiceProvider.GetService<IDesignTimeAspectPipelineObserver>();
        this._eventHub = this.ServiceProvider.Global.GetService<AnalysisProcessEventHub>();
        this._exceptionHandler = this.ServiceProvider.Global.GetRequiredService<DesignTimeExceptionHandler>();

        if ( this._eventHub != null )
        {
            this._eventHub.CompilationResultChangedEvent.RegisterHandler( this.OnOtherPipelineCompilationResultChanged );
            this._eventHub.PipelineStatusChangedEvent.RegisterHandler( this.OnOtherPipelineStatusChangedAsync );
        }

        this._taskRunner = this.ServiceProvider.Global.GetRequiredService<ITaskRunner>();
        this._userDiagnosticsRegistrationService = this.ServiceProvider.Global.GetService<IUserDiagnosticRegistrationService>();

        if ( this._userDiagnosticsRegistrationService == null )
        {
            this.Logger.Warning?.Log( "Cannot get a UserDiagnosticRegistrationService." );
        }

        this._currentState = new PipelineState( this );

        // The design-time pipeline contains project-scoped services for performance reasons: the pipeline may be called several
        // times with the same compilation.

        if ( string.IsNullOrEmpty( this.ProjectOptions.BuildTouchFile ) )
        {
            return;
        }

        this.Logger.Trace?.Log( $"BuildTouchFile={this.ProjectOptions.BuildTouchFile}" );

        // Initialize FileSystemWatcher.
        var watchedFilter = Path.GetFileName( this.ProjectOptions.BuildTouchFile ).AssertNotNull();
        var watchedDirectory = Path.GetDirectoryName( this.ProjectOptions.BuildTouchFile );

        if ( watchedDirectory != null )
        {
            this._fileSystemWatcher = new RecursiveFileSystemWatcher( watchedDirectory, watchedFilter );

            this._fileSystemWatcher.Changed += this.OnTouchFileChanged;
            this._fileSystemWatcher.EnableRaisingEvents = true;
        }
    }

    private void OnOtherPipelineCompilationResultChanged( CompilationResultChangedEventData data )
    {
        if ( this.Status != DesignTimeAspectPipelineStatus.Ready )
        {
            return;
        }

        var dependencies = this._currentState.Dependencies;

        if ( dependencies.IsUninitialized )
        {
            return;
        }

        if ( !dependencies.DependenciesByMasterProject.TryGetValue( data.ProjectKey, out var dependenciesInThisProject ) )
        {
            return;
        }

        this.Logger.Trace?.Log( $"Processing change notification from dependent project '{data.ProjectKey}'." );

        if ( !data.IsPartialCompilation || data.SyntaxTreePaths.Any( p => dependenciesInThisProject.DependenciesByMasterFilePath.ContainsKey( p ) ) )
        {
            this.Logger.Trace?.Log( $"Processing change notification from dependent project '{data.ProjectKey}': the current project may be affected." );
            this._eventHub?.OnProjectDirty( this.ProjectKey );
        }
        else
        {
            this.Logger.Trace?.Log( $"Processing change notification from dependent project '{data.ProjectKey}': the current project is not affected." );
        }
    }

    private Task OnOtherPipelineStatusChangedAsync( DesignTimePipelineStatusChangedEventArgs arg )
        => this.OnOtherPipelineStatusChangedAsync( AsyncExecutionContext.Get( $"{this.ProjectKey}:PipelineStatusChangedEvent" ), arg );

    private async Task OnOtherPipelineStatusChangedAsync( AsyncExecutionContext executionContext, DesignTimePipelineStatusChangedEventArgs args )
    {
        if ( this._currentState.ProjectVersion?.ReferencedProjectVersions.ContainsKey( args.Pipeline.ProjectKey ) == true )
        {
            if ( args.IsPausing && this.Status != DesignTimeAspectPipelineStatus.Paused )
            {
                this.Logger.Trace?.Log( $"Pausing '{this.ProjectKey}' because the dependent project '{args.Pipeline.ProjectKey}' has paused." );

                await this.ExecuteIfLockAvailableOrEnqueueAsync( context => this.SetStateAsync( this._currentState.Pause(), context ), executionContext );
            }
            else if ( args.IsResuming && this.Status != DesignTimeAspectPipelineStatus.Default )
            {
                this.Logger.Trace?.Log( $"Resuming '{this.ProjectKey}' because the dependent project '{args.Pipeline.ProjectKey}' has resumed." );
                await this.ExecuteIfLockAvailableOrEnqueueAsync( this.ResumeCoreAsync, executionContext );
            }
        }
    }

    private static ServiceProvider<IProjectService> GetServiceProvider(
        ServiceProvider<IGlobalService> serviceProvider,
        IProjectOptions projectOptions,
        ImmutableArray<PortableExecutableReference> metadataReferences )
    {
        var projectServiceProvider = serviceProvider.WithProjectScopedServices( projectOptions, metadataReferences );

        return projectServiceProvider;
    }

    private async ValueTask SetStateAsync( PipelineState state, AsyncExecutionContext executionContext )
    {
#if DEBUG
        executionContext.RequireObject( this );
#endif

        var oldStatus = this._currentState.Status;

        this._currentState = state;

        if ( oldStatus != state.Status && this._eventHub != null )
        {
            await this._eventHub.OnPipelineStatusChangedEventAsync( new DesignTimePipelineStatusChangedEventArgs( this, oldStatus, state.Status ) );
        }

        // Recomputing configuration when compile-time code changes goes old -> invalid -> new.
        // We want to ignore the intermediate invalid state here.
        var newAspectClassesHashCode = state.Configuration is { IsSuccessful: true } configuration
            ? configuration.Value.AspectClassesHashCode ^ HashUtilities.HashStrings( configuration.Value.FabricTypeNames )
            : 0;

        if ( newAspectClassesHashCode != 0 && this._aspectClassesHashCode != newAspectClassesHashCode )
        {
            this._aspectClassesHashCode = newAspectClassesHashCode;
            this._eventHub?.OnAspectClassesChanged( this.ProjectKey );
        }

        var newAspectInstancesHashCode = state.PipelineResult.AspectInstancesHashCode;

        if ( newAspectInstancesHashCode != 0 && this._aspectInstancesHashCode != newAspectInstancesHashCode )
        {
            this._aspectInstancesHashCode = newAspectInstancesHashCode;
            this._eventHub?.OnAspectInstancesChanged( this.ProjectKey );
        }
    }

    // It's ok if we return an obsolete project in the use cases of this property.
    internal IReadOnlyCollection<IAspectClass>? AspectClasses
    {
        get
        {
            if ( this._currentState.Configuration is not { IsSuccessful: true } )
            {
                return null;
            }

            return this._currentState.Configuration.Value.Value.AspectClasses;
        }
    }

    internal ImmutableArray<string>? Fabrics
    {
        get
        {
            if ( this._currentState.Configuration is not { IsSuccessful: true } )
            {
                return null;
            }

            return this._currentState.Configuration.Value.Value.FabricTypeNames;
        }
    }

#pragma warning disable VSTHRD100
    private async void OnTouchFileChanged( object sender, FileSystemEventArgs e )
    {
        try
        {
            if ( this.Status != DesignTimeAspectPipelineStatus.Paused )
            {
                return;
            }

            // There was an external build. Touch the files to re-run the analyzer.
            this.Logger.Trace?.Log( $"Detected an external build for project '{this.ProjectKey}'." );

            await this.ResumeAsync( AsyncExecutionContext.Get(), false, CancellationToken.None );

            // Raise the event.
            if ( this._eventHub != null )
            {
                await this._eventHub.OnExternalBuildCompletedEventAsync( this.ProjectKey );
            }
        }
        catch ( Exception exception )
        {
            this._exceptionHandler.ReportException( exception );
        }
    }
#pragma warning restore VSTHRD100

    internal async Task ResumeAsync( AsyncExecutionContext executionContext, bool executePipelineNow, CancellationToken cancellationToken = default )
    {
        this.Logger.Trace?.Log( $"Resuming the pipeline for project '{this.ProjectKey}'." );

        using ( await this.WithLockAsync( executionContext, cancellationToken ) )
        {
            try
            {
                if ( this.Status != DesignTimeAspectPipelineStatus.Paused )
                {
                    this.Logger.Trace?.Log( $"A Resume request was requested for project '{this.ProjectKey}', but the pipeline was not paused." );

                    return;
                }

                await this.ResumeCoreAsync( executionContext );

                if ( executePipelineNow && this.LastProjectVersion != null )
                {
                    await this.ExecuteAsync( this.LastProjectVersion.Compilation, executionContext, cancellationToken.ToTestable() );
                }
            }
            finally
            {
                await this.ProcessJobQueueAsync( executionContext );
            }
        }
    }

    private async ValueTask ResumeCoreAsync( AsyncExecutionContext executionContext )
    {
        // Notify the IDE that errors have been removed on compile-time syntax trees.
        if ( this.MustReportPausedPipelineAsErrors )
        {
            if ( this._currentState.CompileTimeSyntaxTrees != null )
            {
                var notification = new CompilationResultChangedEventData(
                    this.ProjectKey,
                    true,
                    this._currentState.CompileTimeSyntaxTrees.Keys.ToImmutableArray() );

                this._eventHub?.PublishCompilationResultChangedNotification( notification );
            }
        }

        // Reset the pipeline.
        await this.SetStateAsync( this._currentState.Reset(), executionContext );

        // Notify that the pipeline must be executed again.
        this._jobQueue.Enqueue(
            _ =>
            {
                this._eventHub?.OnProjectDirty( this.ProjectKey );

                return default;
            } );
    }

    public async ValueTask<FallibleResultWithDiagnostics<AspectPipelineConfiguration>> GetConfigurationAsync(
        PartialCompilation compilation,
        bool ignoreStatus,
        AsyncExecutionContext executionContext,
        TestableCancellationToken cancellationToken )
    {
        using ( await this.WithLockAsync( executionContext, cancellationToken ) )
        {
            try
            {
                if ( ignoreStatus )
                {
                    await this.InvalidateCacheAsync( compilation.Compilation, executionContext, cancellationToken );
                }

                var state = this._currentState;

                var getConfigurationResult = PipelineState.GetConfiguration(
                    ref state,
                    compilation.Compilation,
                    ignoreStatus,
                    cancellationToken );

                await this.SetStateAsync( state, executionContext );

                return getConfigurationResult;
            }
            finally
            {
                await this.ProcessJobQueueAsync( executionContext );
            }
        }
    }

    internal bool MustReportPausedPipelineAsErrors => this._eventHub is not { IsUserInterfaceAttached: true };

    protected override void Dispose( bool disposing )
    {
        base.Dispose( disposing );
        this._fileSystemWatcher?.Dispose();
        this._sync.Dispose();

        if ( this._eventHub != null )
        {
            this._eventHub.PipelineStatusChangedEvent.UnregisterHandler( this.OnOtherPipelineStatusChangedAsync );
            this._eventHub.CompilationResultChangedEvent.UnregisterHandler( this.OnOtherPipelineCompilationResultChanged );
        }
    }

    private async ValueTask<ProjectVersion> InvalidateCacheAsync(
        Compilation compilation,
        AsyncExecutionContext executionContext,
        TestableCancellationToken cancellationToken )
    {
        var newState = await this._currentState.InvalidateCacheForNewCompilationAsync(
            compilation,
            true,
            cancellationToken );

        await this.SetStateAsync( newState, executionContext );

        return newState.ProjectVersion.AssertNotNull();
    }

    internal async ValueTask ResetCacheAsync( AsyncExecutionContext executionContext, CancellationToken cancellationToken )
    {
        using ( await this.WithLockAsync( executionContext, cancellationToken ) )
        {
            try
            {
                await this.SetStateAsync( this._currentState.Reset(), executionContext );

                this._eventHub?.OnProjectDirty( this.ProjectKey );
            }
            finally
            {
                await this.ProcessJobQueueAsync( executionContext );
            }
        }
    }

    private async Task<FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>> ExecutePartialAsync(
        PartialCompilation partialCompilation,
        DesignTimeProjectVersion projectVersion,
        AsyncExecutionContext executionContext,
        TestableCancellationToken cancellationToken )
    {
        var result = await PipelineState.ExecuteAsync( this._currentState, partialCompilation, projectVersion, cancellationToken );

        // Intentionally updating the state atomically after execution of the method, so the state is
        // not affected by a cancellation.
        await this.SetStateAsync( result.NewState, executionContext );

        if ( !result.CompilationResult.IsSuccessful )
        {
            return FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>.Failed( result.CompilationResult.Diagnostics );
        }
        else
        {
            return FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>.Succeeded(
                result.CompilationResult.Value,
                result.CompilationResult.Diagnostics );
        }
    }

    internal FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState> Execute(
        Compilation compilation,
        TestableCancellationToken cancellationToken = default )
        => this._taskRunner.RunSynchronously( () => this.ExecuteAsync( compilation, AsyncExecutionContext.Get(), cancellationToken ), cancellationToken );

    // This method is for testing only.
    public bool TryExecute(
        Compilation compilation,
        TestableCancellationToken cancellationToken,
        [NotNullWhen( true )] out DesignTimeAspectPipelineResultAndState? compilationResult )
    {
        var result = this._taskRunner.RunSynchronously(
            () => this.ExecuteAsync( compilation, AsyncExecutionContext.Get(), cancellationToken ),
            cancellationToken );

        if ( !result.IsSuccessful )
        {
            compilationResult = null;

            return false;
        }
        else
        {
            compilationResult = result.Value;

            return true;
        }
    }

    internal async ValueTask<FallibleResultWithDiagnostics<DesignTimeProjectVersion>> GetDesignTimeProjectVersionAsync(
        Compilation compilation,
        bool autoResumePipeline,
        AsyncExecutionContext executionContext,
        TestableCancellationToken cancellationToken )
    {
        var pipelineStatus = this.Status;

        var compilationVersion = await this._projectVersionProvider.GetCompilationVersionAsync(
            this._currentState.ProjectVersion?.Compilation,
            compilation,
            cancellationToken );

        List<DesignTimeProjectReference> compilationReferences = new();

        foreach ( var reference in compilationVersion.ReferencedProjectVersions.Values )
        {
            if ( this._pipelineFactory.TryGetMetalamaVersion( reference.Compilation, out var metalamaVersion ) )
            {
                if ( metalamaVersion == EngineAssemblyMetadataReader.Instance.AssemblyVersion )
                {
                    // This is a Metalama reference of the current version. We need to compile the dependency.

                    if ( !this._pipelineFactory.TryGetPipeline( reference.ProjectKey, out var pipeline ) )
                    {
                        // There is currently no pipeline for this project.
                        // Calling _pipelineFactory.ExecuteAsync would wait for it to be created,
                        // which only happens when some other code calls GetOrCreatePipelineAsync.
                        // This is not guanteed to happen, so we have to create the pipeline ourselves here.

                        var pipelineResult = await this._pipelineFactory.GetOrCreatePipelineAsync( reference, cancellationToken );

                        if ( !pipelineResult.IsSuccessful )
                        {
                            return pipelineResult.CastFailure<DesignTimeProjectVersion>();
                        }

                        pipeline = pipelineResult.Value;
                    }

                    var referenceResult = await pipeline.ExecuteAsync(
                        reference.Compilation,
                        autoResumePipeline,
                        executionContext,
                        cancellationToken );

                    if ( !referenceResult.IsSuccessful )
                    {
                        this.Logger.Warning?.Log(
                            $"GetDesignTimeProjectVersionAsync('{this.ProjectKey}'): the pipeline for the reference '{reference.ProjectKey}' failed." );

                        return FallibleResultWithDiagnostics<DesignTimeProjectVersion>.Failed(
                            referenceResult.Diagnostics,
                            $"The pipeline for the reference '{reference.ProjectKey}' failed: {referenceResult.DebugReason}" );
                    }

                    compilationReferences.Add(
                        new DesignTimeProjectReference(
                            referenceResult.Value.ProjectVersion.ProjectKey,
                            referenceResult.Value.Result ) );

                    if ( referenceResult.Value.Status == DesignTimeAspectPipelineStatus.Paused )
                    {
                        pipelineStatus = DesignTimeAspectPipelineStatus.Paused;
                    }
                }
                else
                {
                    // We have a reference to a different version of Metalama.

                    var entryPointConsumer = this._entryPointConsumer.AssertNotNull();
                    var serviceProvider = (await entryPointConsumer.GetServiceProviderAsync( metalamaVersion, cancellationToken )).AssertNotNull();

                    var transitiveCompilationService =
                        (ITransitiveCompilationService) serviceProvider.GetService( typeof(ITransitiveCompilationService) ).AssertNotNull();

                    var resultArray = new ITransitiveCompilationResult?[1];
                    await transitiveCompilationService.GetTransitiveAspectManifestAsync( reference.Compilation, resultArray, cancellationToken );

                    var result = resultArray[0].AssertNotNull();

                    if ( result.IsSuccessful != true )
                    {
                        this.Logger.Warning?.Log( $"Failed to process the reference to '{reference.ProjectKey}': cannot get the transitive aspect manifest." );

                        return FallibleResultWithDiagnostics<DesignTimeProjectVersion>.Failed(
                            result.Diagnostics.ToImmutableArray(),
                            $"GetTransitiveAspectManifest failed for '{reference.ProjectKey}'" );
                    }
                    else
                    {
                        // To deserialize the manifest, we need a service provider with the CompileTimeProject of the referenced project, compiled
                        // for the current Metalama version.

                        var pipelineResult = await this._pipelineFactory.GetOrCreatePipelineAsync( reference, cancellationToken );

                        if ( !pipelineResult.IsSuccessful )
                        {
                            return pipelineResult.CastFailure<DesignTimeProjectVersion>();
                        }

                        var pipeline = pipelineResult.Value;

                        var configuration = await pipeline.GetConfigurationAsync(
                            PartialCompilation.CreateComplete( reference.Compilation ),
                            false,
                            executionContext,
                            cancellationToken );

                        if ( !configuration.IsSuccessful )
                        {
                            return FallibleResultWithDiagnostics<DesignTimeProjectVersion>.Failed(
                                configuration.Diagnostics,
                                $"Cannot get configuration: {configuration.DebugReason}" );
                        }

                        var manifest = TransitiveAspectsManifest.Deserialize(
                            new MemoryStream( result.Manifest! ),
                            configuration.Value.ServiceProvider,
                            compilation.GetCompilationContext(),
                            reference.Compilation.AssemblyName );

                        compilationReferences.Add(
                            new DesignTimeProjectReference(
                                reference.ProjectKey,
                                manifest ) );

                        if ( result.IsPipelinePaused )
                        {
                            pipelineStatus = DesignTimeAspectPipelineStatus.Paused;
                        }
                    }
                }
            }
            else
            {
                // It is a non-Metalama reference.
                var projectKey = reference.Compilation.GetProjectKey();

                var projectReference = new DesignTimeProjectReference( projectKey );
                compilationReferences.Add( projectReference );
            }
        }

        return new DesignTimeProjectVersion( compilationVersion, compilationReferences, pipelineStatus );
    }

    public ValueTask<FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>> ExecuteAsync(
        Compilation compilation,
        AsyncExecutionContext executionContext,
        TestableCancellationToken cancellationToken = default )
        => this.ExecuteAsync( compilation, false, executionContext, cancellationToken );

    public async ValueTask<FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>> ExecuteAsync(
        Compilation compilation,
        bool autoResumePipeline,
        AsyncExecutionContext executionContext,
        TestableCancellationToken cancellationToken = default )
    {
        async Task AutoResumeAsync()
        {
            if ( this.Status == DesignTimeAspectPipelineStatus.Paused && autoResumePipeline )
            {
                await this.ResumeCoreAsync( executionContext );
            }
        }

        if ( this._compilationResultCache.TryGetValue( compilation, out var compilationResult ) )
        {
            if ( !compilationResult.IsSuccessful )
            {
                this.Logger.Trace?.Log( "Returning a cached but failed compilation." );
            }

            return compilationResult;
        }

        try
        {
            using ( await this.WithLockAsync( executionContext, cancellationToken ) )
            {
                try
                {
                    if ( this._compilationResultCache.TryGetValue( compilation, out compilationResult ) )
                    {
                        return compilationResult;
                    }

                    var projectVersion = await this.GetDesignTimeProjectVersionAsync( compilation, autoResumePipeline, executionContext, cancellationToken );

                    if ( !projectVersion.IsSuccessful )
                    {
                        // A dependency could not be compiled.
                        this.Logger.Warning?.Log( $"ExecuteAsync('{this.ProjectKey}'): cannot compile a referenced project." );

                        return FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>.Failed(
                            projectVersion.Diagnostics,
                            $"Cannot compile a referenced project: {projectVersion.DebugReason}" );
                    }

                    // If a dependency project was paused, we must pause too.
                    if ( this.Status != DesignTimeAspectPipelineStatus.Paused
                         && projectVersion.Value.PipelineStatus == DesignTimeAspectPipelineStatus.Paused )
                    {
                        await this.SetStateAsync( this._currentState.Pause(), executionContext );
                    }

                    await AutoResumeAsync();

                    Compilation? compilationToAnalyze = null;

                    if ( this.Status != DesignTimeAspectPipelineStatus.Paused )
                    {
                        // Invalidate the cache for the new compilation.
                        var compilationVersion = await this.InvalidateCacheAsync(
                            compilation,
                            executionContext,
                            cancellationToken );

                        await AutoResumeAsync();

                        compilationToAnalyze = compilationVersion.CompilationToAnalyze;

                        if ( this.Logger.Trace != null )
                        {
                            if ( compilationToAnalyze != compilation )
                            {
                                this.Logger.Trace?.Log(
                                    $"Cache hit: the original compilation is {DebuggingHelper.GetObjectId( compilation )}, but we will analyze the cached compilation {DebuggingHelper.GetObjectId( compilationToAnalyze )}" );
                            }
                        }
                    }
                    else
                    {
                        // If the pipeline is paused, there is no need to track changes because the pipeline will be fully invalidated anyway
                        // when it will be resumed.
                    }

                    if ( this.Status != DesignTimeAspectPipelineStatus.Paused )
                    {
                        PartialCompilation partialCompilation;

                        if ( this.Status == DesignTimeAspectPipelineStatus.Default )
                        {
                            partialCompilation = PartialCompilation.CreateComplete( compilationToAnalyze! );
                        }
                        else
                        {
                            var dirtySyntaxTrees = this.GetDirtySyntaxTrees( compilationToAnalyze! );

                            // Even if there is no dirty syntax tree, we may need to update the cache, e.g. because of removed trees.
                            partialCompilation = PartialCompilation.CreatePartial( compilationToAnalyze!, dirtySyntaxTrees );
                        }

                        // Execute the pipeline, and update the cache.
                        Interlocked.Increment( ref this._pipelineExecutionCount );

                        var executionResult = await this.ExecutePartialAsync(
                            partialCompilation,
                            projectVersion.Value,
                            executionContext,
                            cancellationToken );

                        if ( !executionResult.IsSuccessful )
                        {
                            compilationResult = FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>.Failed( executionResult.Diagnostics );

                            if ( !this._compilationResultCache.TryAdd( compilation, compilationResult ) )
                            {
                                // TODO: there seems to be some race which I cannot solve, but it is better not to fail in this case.
                                this.Logger.Warning?.Log( $"Results of compilation '{this.ProjectKey}' were already in the cache." );
                            }

                            return compilationResult;
                        }

                        // Publish a change notification.
                        var notification = new CompilationResultChangedEventData(
                            this.ProjectKey,
                            partialCompilation.IsPartial,
                            partialCompilation.IsPartial ? partialCompilation.SyntaxTrees.SelectAsImmutableArray( t => t.Key ) : default );

                        this._eventHub?.PublishCompilationResultChangedNotification( notification );

                        // Return the result from the cache.
                        compilationResult = new DesignTimeAspectPipelineResultAndState(
                            this._currentState.ProjectVersion.AssertNotNull(),
                            this._currentState.PipelineResult,
                            this._currentState.Status,
                            this._currentState.Configuration!.Value.Value );

                        if ( !this._compilationResultCache.TryAdd( compilation, compilationResult ) )
                        {
                            // TODO: there seems to be some race which I cannot solve, but it is better not to fail in this case.
                            this.Logger.Warning?.Log( $"Results of compilation '{this.ProjectKey}' were already in the cache." );
                        }

                        return compilationResult;
                    }
                    else
                    {
                        this.Logger.Trace?.Log(
                            $"DesignTimeAspectPipeline.ExecuteAsync('{compilation.AssemblyName}', CompilationId = {DebuggingHelper.GetObjectId( compilation )}): "
                            + $"the pipeline is paused, returning from cache only." );

                        // If the pipeline is paused, we only serve pipeline results from the cache.

                        if ( this._currentState.ProjectVersion != null )
                        {
                            if ( this._currentState.PipelineResult.Configuration == null )
                            {
                                compilationResult = FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>.Failed(
                                    ImmutableArray<Diagnostic>.Empty,
                                    "The pipeline was paused while there were compile-time errors." );
                            }
                            else
                            {
                                compilationResult = new DesignTimeAspectPipelineResultAndState(
                                    this._currentState.ProjectVersion,
                                    this._currentState.PipelineResult,
                                    this._currentState.Status,
                                    this._currentState.PipelineResult.Configuration );
                            }
                        }
                        else
                        {
                            // The pipeline was paused before being first executed.
                            compilationResult = FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>.Failed(
                                ImmutableArray<Diagnostic>.Empty,
                                "The pipeline was paused in the middle of execution." );
                        }

                        return compilationResult;
                    }
                }
                finally
                {
                    await this.ProcessJobQueueAsync( executionContext );
                }
            }
        }
        catch ( OperationCanceledException )
        {
            this.Logger.Warning?.Log(
                $"DesignTimeAspectPipeline.ExecuteAsync('{compilation.AssemblyName}', CompilationId = {DebuggingHelper.GetObjectId( compilation )}): cancelled." );

            throw;
        }
    }

    private List<SyntaxTree> GetDirtySyntaxTrees( Compilation compilation )
    {
        // Computes the set of semantic models that need to be processed.

        List<SyntaxTree> uncachedSyntaxTrees = new();

        foreach ( var syntaxTree in compilation.SyntaxTrees )
        {
            if ( syntaxTree.FilePath.StartsWith( _sourceGeneratorAssemblyName, StringComparison.Ordinal ) )
            {
                // This is our own generated file. Don't include.
                continue;
            }

            if ( this._currentState.PipelineResult.IsSyntaxTreeDirty( syntaxTree ) )
            {
                uncachedSyntaxTrees.Add( syntaxTree );
            }
        }

        return uncachedSyntaxTrees;
    }

    /// <summary>
    /// Determines whether a compile-time syntax tree is outdated. This happens when the syntax
    /// tree has changed compared to the cached configuration of this pipeline. This method is used to
    /// determine whether an error must displayed in the editor.  
    /// </summary>
    internal bool IsCompileTimeSyntaxTreeOutdated( string name )
        => this._currentState.CompileTimeSyntaxTrees is { } compileTimeSyntaxTrees
           && compileTimeSyntaxTrees.TryGetValue( name, out var isValid )
           && !isValid;

    private IReadOnlyList<DesignTimeAspectInstance>? GetAspectInstancesOnSymbol( ISymbol symbol )
    {
        // Check the aspects already on the declaration.
        var filePath = symbol.GetPrimaryDeclarationSyntax()?.SyntaxTree.FilePath;

        if ( filePath == null )
        {
            return null;
        }

        var symbolId = symbol.GetSerializableId();

        if ( !this._currentState.PipelineResult.SyntaxTreeResults.TryGetValue( filePath, out var result ) )
        {
            return null;
        }

        return result.AspectInstances.Where( i => i.TargetDeclarationId == symbolId ).ToReadOnlyList();
    }

    public IReadOnlyList<AspectClass> GetEligibleAspects( Compilation compilation, ISymbol symbol, TestableCancellationToken cancellationToken )
    {
        var classes = this.AspectClasses;

        if ( classes == null )
        {
            return Array.Empty<AspectClass>();
        }

        // We are not implementing this method as an enumerator for the ease of debugging.
        var result = new List<AspectClass>();

        var compilationContext = compilation.GetCompilationContext();

        var currentAspectInstances = this.GetAspectInstancesOnSymbol( symbol )
                                     ?? Array.Empty<DesignTimeAspectInstance>();

        IDeclaration? declaration = null;

        foreach ( var aspectClass in classes.OfType<AspectClass>() )
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.Logger.Trace?.Log( $"Considering the eligibility of aspect '{aspectClass.ShortName}' on '{symbol}'." );

            // Check if there is already an instance of this aspect class on the target.
            if ( currentAspectInstances.Any( i => i.AspectClassFullName == aspectClass.FullName ) )
            {
                this.Logger.Trace?.Log( "The aspect is not eligible because it has already been added to the symbol." );

                continue;
            }

            // Check if the aspect class is accessible from the symbol.

            if ( !compilationContext.SerializableTypeIdResolver.TryResolveId( aspectClass.TypeId, out var aspectClassSymbol ) )
            {
                // This situation may happen during edits (see #34704).
                this.Logger.Trace?.Log( $"The aspect is not eligible because '{aspectClass.TypeId}' cannot be resolved." );

                continue;
            }

            var enclosingSymbol = (ISymbol?) symbol.GetClosestContainingType() ?? symbol.ContainingAssembly ?? symbol as IAssemblySymbol;

            if ( enclosingSymbol == null )
            {
                this.Logger.Trace?.Log( $"The aspect is not eligible because we cannot get the enclosing type or assembly of '{symbol}'." );

                continue;
            }

            if ( !compilation.IsSymbolAccessibleWithin( aspectClassSymbol, enclosingSymbol ) )
            {
                this.Logger.Trace?.Log( "The aspect is not eligible because it is not accessible from the symbol." );

                continue;
            }

            if ( !aspectClass.IsAbstract && aspectClass.IsEligibleFast( symbol ) )
            {
                // We have a candidate. Create an IDeclaration if we haven't done it yet.
                if ( declaration == null )
                {
                    var projectModel = new ProjectModel( compilation, this.ServiceProvider );

                    var compilationModel = CompilationModel.CreateInitialInstance(
                        projectModel,
                        PartialCompilation.CreatePartial( compilation, Array.Empty<SyntaxTree>() ),
                        new PipelineResultBasedAspectRepository( this._currentState.PipelineResult ) );

                    declaration = compilationModel.Factory.GetDeclaration( symbol );
                }

                // Filter with eligibility.
                var eligibleScenarios = aspectClass.GetEligibility( declaration );

                if ( eligibleScenarios.IncludesAny( EligibleScenarios.All ) )
                {
                    result.Add( aspectClass );
                }
            }
        }

        return result;
    }

    private async ValueTask ExecuteIfLockAvailableOrEnqueueAsync( Func<AsyncExecutionContext, ValueTask> action, AsyncExecutionContext executionContext )
    {
        using ( var @lock = await this.WithLockAsync( executionContext, 0, CancellationToken.None ) )
        {
            if ( @lock.IsAcquired )
            {
                await action( executionContext );

                await this.ProcessJobQueueAsync( executionContext );
            }
            else
            {
                this._jobQueue.Enqueue( action );

                // Enqueue a task to process the action when the lock will be available.
                _ = this.ProcessJobQueueWhenLockAvailableAsync();
            }
        }
    }

    internal async Task ProcessJobQueueWhenLockAvailableAsync()
    {
        var executionContext = AsyncExecutionContext.Get();

        using ( await this.WithLockAsync( executionContext, CancellationToken.None ) )
        {
            await this.ProcessJobQueueAsync( executionContext );
        }
    }

    private async ValueTask ProcessJobQueueAsync( AsyncExecutionContext executionContext )
    {
        while ( this._mustProcessQueue )
        {
            this._mustProcessQueue = false;

            while ( this._jobQueue.TryDequeue( out var job ) )
            {
                await job( executionContext );
            }
        }
    }

    private ValueTask<Lock> WithLockAsync( AsyncExecutionContext executionContext, CancellationToken cancellationToken )
        => this.WithLockAsync( executionContext, -1, cancellationToken );

    private async ValueTask<Lock> WithLockAsync( AsyncExecutionContext executionContext, int timeout, CancellationToken cancellationToken )
    {
        if ( this._sync.CurrentCount < 1 )
        {
            this.Logger.Trace?.Log( $"Waiting for lock on '{this.ProjectKey}'." );
        }

#if DEBUG
        executionContext.DetectCycle( this );
#endif

        // First wait for a limited amount of time, so we can display a warning in case of large delay.
        var realTimeout = timeout < 0 ? int.MaxValue : timeout;
        var acquired = await this._sync.WaitAsync( Math.Min( 5000, realTimeout ), cancellationToken );

        // Now wait more if needed.
        if ( !acquired )
        {
            this.Logger.Warning?.Log( $"Acquiring the lock on '{this.ProjectKey}' is taking a long time." + Environment.NewLine + new StackTrace() );

            if ( realTimeout > 5000 )
            {
                acquired = await this._sync.WaitAsync( timeout, cancellationToken );
            }
        }

        Action? lockDisposeAction = null;

        if ( acquired )
        {
            this._mustProcessQueue = true;
            this.Logger.Trace?.Log( $"Lock on '{this.ProjectKey}' acquired." );

#if DEBUG

            executionContext.Push( this );

            if ( this.Logger.Warning != null )
            {
                var callStack = new StackTrace();
                var isDisposed = new TaskCompletionSource<bool>();
                lockDisposeAction = () => isDisposed.SetResult( true );

                _ = WarnIfNotDisposedAsync( new WeakReference<DesignTimeAspectPipeline>( this ), isDisposed.Task, callStack );
            }
#endif
        }
        else
        {
            this.Logger.Trace?.Log( $"Lock on '{this.ProjectKey}' not acquired because of a timeout." );
        }

        return new Lock( this, acquired, lockDisposeAction, executionContext );
    }

    private static async Task WarnIfNotDisposedAsync( WeakReference<DesignTimeAspectPipeline> pipelineRef, Task disposedTask, StackTrace stackTrace )
    {
#pragma warning disable VSTHRD003
        await Task.WhenAny( Task.Delay( TimeSpan.FromSeconds( 1 ) ), disposedTask );
#pragma warning restore VSTHRD003

        if ( !disposedTask.IsCompleted && pipelineRef.TryGetTarget( out var pipeline ) )
        {
            pipeline.Logger.Warning?.Log( $"The following call stack has been holding the lock for '{pipeline.ProjectKey}' for a long time:" );
            pipeline.Logger.Warning?.Log( stackTrace.ToString() );
        }
    }

    private readonly struct Lock : IDisposable
    {
        private readonly DesignTimeAspectPipeline _parent;
        private readonly Action? _disposeAction;
        private readonly AsyncExecutionContext _executionContext;

        public Lock( DesignTimeAspectPipeline sync, bool isAcquired, Action? disposeAction, AsyncExecutionContext executionContext )
        {
            this._parent = sync;
            this.IsAcquired = isAcquired;
            this._disposeAction = disposeAction;
            this._executionContext = executionContext;
        }

        public bool IsAcquired { get; }

        public void Dispose()
        {
            if ( this.IsAcquired )
            {
                if ( this._parent._mustProcessQueue )
                {
                    throw new AssertionFailedException( "Queue not empty." );
                }

                this._parent.Logger.Trace?.Log( $"Releasing lock on '{this._parent.ProjectKey}'." );
                this._parent._sync.Release();

#if DEBUG
                this._executionContext.Pop( this._parent );
#endif
            }

            this._disposeAction?.Invoke();
        }
    }

    internal void ValidateTemplatingCode( SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, Action<ScopedSuppression>? reportSuppression )
        => TemplatingCodeValidator.Validate(
            this.ServiceProvider,
            semanticModel,
            reportDiagnostic,
            reportSuppression,
            this.IsCompileTimeSyntaxTreeOutdated( semanticModel.SyntaxTree.FilePath ),
            true,
            CancellationToken.None );

    public async Task<(bool Success, PartialCompilation? Compilation, ImmutableArray<Diagnostic> Diagnostics)> ApplyAspectToCodeAsync(
        string aspectTypeName,
        Compilation inputCompilation,
        ISymbol targetSymbol,
        TestableCancellationToken cancellationToken )
    {
        // Get a compilation _without_ generated code, and map the target symbol.
        var generatedFiles = inputCompilation.SyntaxTrees.Where( SourceGeneratorHelper.IsGeneratedFile );
        var sourceCompilation = inputCompilation.RemoveSyntaxTrees( generatedFiles );

        var sourceSymbol = DocumentationCommentId
            .GetFirstSymbolForDeclarationId( targetSymbol.GetDocumentationCommentId().AssertNotNull(), sourceCompilation )
            .AssertNotNull();

        // TODO: use partial compilation (it does not seem to work).
        var partialCompilation = PartialCompilation.CreateComplete( sourceCompilation );

        DiagnosticBag diagnosticBag = new();

        var getConfigurationResult = await this.GetConfigurationAsync( partialCompilation, true, AsyncExecutionContext.Get(), cancellationToken );

        if ( !getConfigurationResult.IsSuccessful )
        {
            return (false, null, diagnosticBag.ToImmutableArray());
        }

        var configuration = getConfigurationResult.Value;

        var result = await LiveTemplateAspectPipeline.ExecuteAsync(
            configuration.ServiceProvider,
            configuration,
            x => x.AspectClasses.Single( c => c.FullName == aspectTypeName ),
            partialCompilation,
            sourceSymbol,
            diagnosticBag,
            cancellationToken );

        if ( !result.IsSuccessful )
        {
            return (false, null, diagnosticBag.ToImmutableArray());
        }
        else
        {
            return (true, result.Value, diagnosticBag.ToImmutableArray());
        }
    }

    internal DesignTimeAspectPipelineResult AspectPipelineResult => this._currentState.PipelineResult;

    protected override bool TryInitialize(
        IDiagnosticAdder diagnosticAdder,
        Compilation compilation,
        IReadOnlyList<SyntaxTree>? compileTimeTreesHint,
        CancellationToken cancellationToken,
        [NotNullWhen( true )] out AspectPipelineConfiguration? configuration )
    {
        var tryInitialize = base.TryInitialize( diagnosticAdder, compilation, compileTimeTreesHint, cancellationToken, out configuration );

        if ( tryInitialize )
        {
            // Register user diagnostics.
            // If we have a new compile-time project, we need to register user diagnostics.
            if ( configuration!.ClosureDiagnosticManifest is { DiagnosticDefinitions.IsEmpty: false } or { SuppressionDefinitions.IsEmpty: false } )
            {
                this._userDiagnosticsRegistrationService?.RegisterDescriptors( configuration.ClosureDiagnosticManifest );
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    protected override IDiagnosticExtensionPolicy GetDiagnosticExtensionPolicy() => ConstantDiagnosticExtensionPolicy.PropertiesOnly;

    public override string ToString() => $"{this.GetType().Name}, Project='{this.ProjectKey}'";

    internal ProjectVersion? LastProjectVersion => this._currentState.ProjectVersion;
}