// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Extensibility;
using Metalama.Framework.DesignTime.Pipeline.Diff;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Services;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Project;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable InconsistentlySynchronizedField
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace Metalama.Framework.DesignTime.Pipeline;

/// <summary>
/// Caches the <see cref="DesignTimeAspectPipeline"/> (so they can be reused between projects) and the
/// returns produced by <see cref="DesignTimeAspectPipeline"/>. This class is also responsible for invoking
/// cache invalidation methods as appropriate.
/// </summary>
public class DesignTimeAspectPipelineFactory : IDisposable, IAspectPipelineConfigurationProvider
{
    private readonly ConcurrentDictionary<ProjectKey, DesignTimeAspectPipeline> _pipelinesByProjectKey = new();
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<TaskCompletionSource<DesignTimeAspectPipeline>> _newPipelineListeners = new();
    private readonly CancellationToken _globalCancellationToken;
    private readonly IMetalamaProjectClassifier _projectClassifier;
    private readonly AnalysisProcessEventHub? _eventHub;
    private readonly IProjectOptionsFactory _projectOptionsFactory;
    private readonly ITaskRunner _taskRunner;
    private readonly DesignTimeExceptionHandler _exceptionHandler;
    private readonly DesignTimeExtensionManager? _extensionManager;
    private readonly CompileTimeDomain _domain;

    internal ServiceProvider<IGlobalService> ServiceProvider { get; }

    public DesignTimeAspectPipelineFactory( ServiceProvider<IGlobalService> serviceProvider )
    {
        serviceProvider = serviceProvider.WithServiceConditional( _ => this );

        this._exceptionHandler = serviceProvider.GetRequiredService<DesignTimeExceptionHandler>();
        this._projectClassifier = serviceProvider.GetRequiredService<IMetalamaProjectClassifier>();
        this._projectOptionsFactory = serviceProvider.GetRequiredService<IProjectOptionsFactory>();
        this._taskRunner = serviceProvider.GetRequiredService<ITaskRunner>();
        this._eventHub = serviceProvider.GetService<AnalysisProcessEventHub>();
        this._extensionManager = serviceProvider.GetService<DesignTimeExtensionManager>();
        this._globalCancellationToken = serviceProvider.GetRequiredService<ApplicationExitManager>().Token;

        if ( this._eventHub != null )
        {
            this._eventHub.EditingCompileTimeCodeCompleted += this.OnEditingCompileTimeCodeCompleted;
            this._eventHub.ExternalBuildCompletedEvent.RegisterHandler( this.OnExternalBuildCompletedAsync );
        }

        serviceProvider = serviceProvider.WithService( new ProjectVersionProvider( serviceProvider ) );

        this._domain = serviceProvider.GetRequiredService<CompileTimeDomain>();
        this.ServiceProvider = serviceProvider;
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "DesignTime" );
    }

#pragma warning disable VSTHRD100
    private async void OnEditingCompileTimeCodeCompleted()
    {
        try
        {
            await this.ResumePipelinesAsync( AsyncExecutionContext.Get(), true, this._globalCancellationToken );
        }
        catch ( Exception e )
        {
            this._exceptionHandler.ReportException( e, this._logger );
        }
    }
#pragma warning restore VSTHRD100

    /// <summary>
    /// Gets the pipeline for a given project, and creates it if necessary.
    /// </summary>
    public DesignTimeAspectPipeline? GetOrCreatePipeline(
        Microsoft.CodeAnalysis.Project project,
        TestableCancellationToken cancellationToken = default )
    {
        var projectKey = ProjectKeyFactory.FromProject( project );

        if ( projectKey == null )
        {
            return null;
        }

        var options = this._projectOptionsFactory.GetProjectOptions( project );

        return this.GetOrCreatePipeline( options, projectKey, project.MetadataReferences.OfType<PortableExecutableReference>(), cancellationToken );
    }

    /// <summary>
    /// Gets the pipeline for a given compilation, and creates it if necessary.
    /// </summary>
    public DesignTimeAspectPipeline? GetOrCreatePipeline(
        IProjectOptions projectOptions,
        Compilation compilation,
        TestableCancellationToken cancellationToken = default )
        => this.GetOrCreatePipeline(
            projectOptions,
            ProjectKeyFactory.FromCompilation( compilation ),
            compilation.References.OfType<PortableExecutableReference>(),
            cancellationToken );

    private DesignTimeAspectPipeline? GetOrCreatePipeline(
        IProjectOptions projectOptions,
        ProjectKey projectKey,
        IEnumerable<PortableExecutableReference> references,
        TestableCancellationToken cancellationToken = default )
    {
        if ( !projectOptions.IsFrameworkEnabled )
        {
            this._logger.Trace?.Log( $"Cannot get a pipeline for project '{projectOptions.AssemblyName}': Metalama is disabled for this project." );

            return null;
        }

        this._extensionManager?.OnProjectDiscovered( projectOptions );

        var referencesArray = references.ToImmutableArray();

        if ( this.TryGetPipeline( projectKey, projectOptions, referencesArray, out var pipeline ) )
        {
            return pipeline;
        }

        // We lock the dictionary because the ConcurrentDictionary does not guarantee that the creation delegate
        // is called only once, and we prefer a single instance for the simplicity of debugging.
        lock ( this._pipelinesByProjectKey )
        {
            if ( this.TryGetPipeline( projectKey, projectOptions, referencesArray, out pipeline ) )
            {
                cancellationToken.ThrowIfCancellationRequested();

                return pipeline;
            }

            pipeline = new DesignTimeAspectPipeline( this, projectOptions, projectKey, referencesArray );

            if ( !this._pipelinesByProjectKey.TryAdd( projectKey, pipeline ) )
            {
                throw new AssertionFailedException( $"The pipeline '{projectKey}' has already been created." );
            }

            foreach ( var listener in this._newPipelineListeners )
            {
                listener.TrySetResult( pipeline );
            }

            return pipeline;
        }
    }

    private bool TryGetPipeline(
        ProjectKey projectKey,
        IProjectOptions projectOptions,
        ImmutableArray<PortableExecutableReference> references,
        out DesignTimeAspectPipeline? pipeline )
    {
        if ( this._pipelinesByProjectKey.TryGetValue( projectKey, out pipeline ) )
        {
            var pipelineOptions = pipeline.ServiceProvider.GetRequiredService<IProjectOptions>();

            static bool ReferencesAreEqual( ImmutableArray<PortableExecutableReference> x, ImmutableArray<PortableExecutableReference> y )
            {
                if ( x.Equals( y ) )
                {
                    return true;
                }

                if ( x.Length != y.Length )
                {
                    return false;
                }

                // Also ensure none of the paths are null, because we cannot reliably compare those.
                return x.Zip( y, ( x, y ) => (x, y) ).All( pair => pair.x.FilePath == pair.y.FilePath && pair.x.FilePath != null );
            }

            if ( projectOptions.Equals( pipelineOptions )
                 && ReferencesAreEqual( references, pipeline.MetadataReferences ) )
            {
                return true;
            }
            else
            {
                if ( this._logger.Trace is { } trace )
                {
                    // Recreating the pipeline over and over is a performance issue, so we log the reason.
                    if ( !projectOptions.Equals( pipelineOptions ) )
                    {
                        trace.Log( $"Recreating pipeline because project options were not equal." );
                    }
                    else
                    {
                        static string Format( ImmutableArray<PortableExecutableReference> references )
                            => string.Join( ", ", references.Select( r => r.FilePath ) );

                        trace.Log(
                            $"Recreating pipeline because references were not equal. Old: {Format( pipeline.MetadataReferences )}. New: {Format( references )}" );
                    }
                }

                this._pipelinesByProjectKey.TryRemove( projectKey, out _ );
                pipeline = null;

                return false;
            }
        }

        return false;
    }

    internal virtual async ValueTask<FallibleResultWithDiagnostics<DesignTimeAspectPipeline>> GetOrCreatePipelineAsync(
        IProjectVersion projectVersion,
        TestableCancellationToken cancellationToken )
    {
        var projectKey = projectVersion.ProjectKey;

        var workspaceProvider = this.ServiceProvider.GetRequiredService<WorkspaceProvider>();
        var referencedProject = await workspaceProvider.GetProjectAsync( projectKey, cancellationToken );

        if ( referencedProject == null )
        {
            this._logger.Warning?.Log( $"Failed to process the reference to '{projectKey}': cannot get the project from the workspace." );

            return FallibleResultWithDiagnostics<DesignTimeAspectPipeline>.Failed(
                ImmutableArray<Diagnostic>.Empty,
                $"Cannot get the project '{projectKey}' from the workspace" );
        }

        var pipeline = this.GetOrCreatePipeline( referencedProject, cancellationToken );

        if ( pipeline == null )
        {
            this._logger.Warning?.Log( $"Failed to process the reference to '{projectKey}': cannot get a pipeline." );

            return FallibleResultWithDiagnostics<DesignTimeAspectPipeline>.Failed(
                ImmutableArray<Diagnostic>.Empty,
                $"Cannot get the pipeline for project '{projectKey}'." );
        }

        return pipeline;
    }

    internal async ValueTask ResumePipelinesAsync( AsyncExecutionContext executionContext, bool executePipelineNow, CancellationToken cancellationToken )
    {
        Logger.DesignTime.Trace?.Log( "Received ICompileTimeCodeEditingStatusService.OnEditingCompileTimeCodeCompleted." );

        // Resuming all pipelines.
        var tasks = new List<Task>();

        foreach ( var pipeline in this._pipelinesByProjectKey.Values )
        {
            tasks.Add( pipeline.ResumeAsync( executionContext.Fork(), executePipelineNow, cancellationToken ) );
        }

        await Task.WhenAll( tasks );
    }

    private async Task OnExternalBuildCompletedAsync( ProjectKey projectKey )
    {
        // If a build has started, we have to invalidate the whole cache because we have allowed
        // our cache to become inconsistent when we started to have an outdated pipeline configuration.
        foreach ( var pipeline in this._pipelinesByProjectKey.Values )
        {
            // We don't do it concurrently because ResetCacheAsync is most likely synchronous.

            await pipeline.ResetCacheAsync( AsyncExecutionContext.Get(), this._globalCancellationToken );
        }

        // In case the event hub got out of sync (which should not happen), we reset its status.
        this._eventHub?.ResetIsEditingCompileTimeCode();
    }

    public bool TryExecute(
        IProjectOptions options,
        Compilation compilation,
        TestableCancellationToken cancellationToken,
        [NotNullWhen( true )] out DesignTimeAspectPipelineResultAndState? compilationResult )
        => this.TryExecute( options, compilation, cancellationToken, out compilationResult, out _ );

    internal bool TryExecute(
        IProjectOptions options,
        Compilation compilation,
        TestableCancellationToken cancellationToken,
        [NotNullWhen( true )] out DesignTimeAspectPipelineResultAndState? compilationResult,
        out ImmutableArray<Diagnostic> diagnostics )
    {
        var result = this._taskRunner.RunSynchronously(
            () => this.ExecuteAsync( options, compilation, AsyncExecutionContext.Get(), cancellationToken ),
            cancellationToken );

        if ( result.IsSuccessful )
        {
            compilationResult = result.Value;
            diagnostics = result.Diagnostics;

            return true;
        }
        else
        {
            compilationResult = null;
            diagnostics = result.Diagnostics;

            return false;
        }
    }

    private Task<FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>> ExecuteAsync(
        IProjectOptions projectOptions,
        Compilation compilation,
        AsyncExecutionContext executionContext,
        TestableCancellationToken cancellationToken )
    {
        // Force to create the pipeline.
        var designTimePipeline = this.GetOrCreatePipeline( projectOptions, compilation, cancellationToken );

        if ( designTimePipeline == null )
        {
            return Task.FromResult( FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>.Failed( ImmutableArray<Diagnostic>.Empty ) );
        }

        // Call the execution method that assumes that the pipeline exists or waits for it.
        return this.ExecuteAsync( compilation, executionContext, cancellationToken );
    }

    internal virtual bool TryGetMetalamaVersion( Compilation compilation, [NotNullWhen( true )] out Version? version )
        => this._projectClassifier.TryGetMetalamaVersion( compilation, out version );

    internal Task<FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>> ExecuteAsync(
        Compilation compilation,
        AsyncExecutionContext executionContext,
        TestableCancellationToken cancellationToken = default )
        => this.ExecuteAsync( compilation, false, executionContext, cancellationToken );

    private async Task<FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>> ExecuteAsync(
        Compilation compilation,
        bool autoResumePipeline,
        AsyncExecutionContext executionContext,
        TestableCancellationToken cancellationToken = default )
    {
        var pipeline = await this.GetPipelineAndWaitAsync( compilation, cancellationToken );

        if ( pipeline == null )
        {
            return FallibleResultWithDiagnostics<DesignTimeAspectPipelineResultAndState>.Failed( "Cannot get the pipeline." );
        }

        return await pipeline.ExecuteAsync( compilation, autoResumePipeline, executionContext, cancellationToken );
    }

    public virtual void Dispose()
    {
        foreach ( var designTimeAspectPipeline in this._pipelinesByProjectKey.Values )
        {
            designTimeAspectPipeline.Dispose();
        }

        this._eventHub?.ExternalBuildCompletedEvent.UnregisterHandler( this.OnExternalBuildCompletedAsync );
        this._pipelinesByProjectKey.Clear();
        this._domain.Dispose();
    }

    internal virtual async ValueTask<DesignTimeAspectPipeline?> GetPipelineAndWaitAsync( Compilation compilation, CancellationToken cancellationToken )
    {
        var projectKey = compilation.GetProjectKey();

        DesignTimeAspectPipeline? pipeline;

        while ( !this._pipelinesByProjectKey.TryGetValue( projectKey, out pipeline ) )
        {
            if ( !this.TryGetMetalamaVersion( compilation, out _ ) )
            {
                this._logger.Trace?.Log( "Metalama is not enabled for this project." );

                return null;
            }

            this._logger.Trace?.Log( $"Awaiting for the pipeline '{projectKey}'." );

            var taskCompletionSource = new TaskCompletionSource<DesignTimeAspectPipeline>();

#if NET6_0_OR_GREATER
            await using ( cancellationToken.Register( () => taskCompletionSource.SetCanceled( cancellationToken ) ) )
#else
            using ( cancellationToken.Register( () => taskCompletionSource.SetCanceled() ) )
#endif
            {
                this._newPipelineListeners.Enqueue( taskCompletionSource );
            }

            await taskCompletionSource.Task;
        }

        return pipeline;
    }

    public bool TryGetPipeline( ProjectKey projectKey, [NotNullWhen( true )] out DesignTimeAspectPipeline? pipeline )
    {
        if ( !this._pipelinesByProjectKey.TryGetValue( projectKey, out pipeline ) )
        {
            this._logger.Trace?.Log( $"Cannot get the pipeline for project '{projectKey}': it has not been created yet." );

            return false;
        }

        return true;
    }

    async ValueTask<FallibleResultWithDiagnostics<AspectPipelineConfiguration>> IAspectPipelineConfigurationProvider.GetConfigurationAsync(
        PartialCompilation compilation,
        AsyncExecutionContext executionContext,
        TestableCancellationToken cancellationToken )
    {
        var pipeline = await this.GetPipelineAndWaitAsync( compilation.Compilation, cancellationToken );

        if ( pipeline == null )
        {
            return FallibleResultWithDiagnostics<AspectPipelineConfiguration>.Failed( ImmutableArray<Diagnostic>.Empty );
        }

        var configuration = await pipeline.GetConfigurationAsync( compilation, true, executionContext, cancellationToken );

        if ( configuration.IsSuccessful )
        {
            var transitiveAspectManifestProvider = await pipeline.GetDesignTimeProjectVersionAsync(
                compilation.Compilation,
                false,
                executionContext,
                cancellationToken );

            if ( !transitiveAspectManifestProvider.IsSuccessful )
            {
                return transitiveAspectManifestProvider.CastFailure<AspectPipelineConfiguration>();
            }

            configuration = configuration.Value.WithServiceProvider(
                configuration.Value.ServiceProvider.WithService( transitiveAspectManifestProvider.Value ) );
        }

        return configuration;
    }
}