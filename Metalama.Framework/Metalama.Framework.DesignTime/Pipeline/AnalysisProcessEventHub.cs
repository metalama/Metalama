// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using System.Collections.Concurrent;

namespace Metalama.Framework.DesignTime.Pipeline;

[PublicAPI]
public sealed class AnalysisProcessEventHub : IGlobalService
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<ProjectKey, ProjectKey> _projectsWithPausedPipeline = new();
    private readonly WeakEvent<CompilationResultChangedEventData> _compilationResultChangedEvent = new();
    private readonly AsyncWeakEvent<DesignTimePipelineStatusChangedEventArgs> _pipelineStatusChangedEvent = new();
    private readonly AsyncEvent<ProjectKey> _externalBuildCompletedEvent = new();
    private bool _isEditingCompileTimeCode;

    public AnalysisProcessEventHub( GlobalServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "EventHub" );
    }

    internal bool IsEditingCompileTimeCode
    {
        get => this._isEditingCompileTimeCode;
        private set
        {
            if ( value != this._isEditingCompileTimeCode )
            {
                // Raising duplicate events in case of races should not matter. The clients should be design to accept this. 
                this._isEditingCompileTimeCode = value;
                this.IsEditingCompileTimeCodeChanged?.Invoke( value );
            }
        }
    }

    public event Action<bool>? IsEditingCompileTimeCodeChanged;

    public event Action? EditingCompileTimeCodeCompleted;

    public WeakEventBase<Action<CompilationResultChangedEventData>, CompilationResultChangedEventData>.Accessors CompilationResultChangedEvent
        => this._compilationResultChangedEvent.GetAccessors();

    public void OnCompileTimeCodeCompletedEditing() => this.EditingCompileTimeCodeCompleted?.Invoke();

    internal bool IsUserInterfaceAttached { get; private set; }

    public void OnUserInterfaceAttached() => this.IsUserInterfaceAttached = true;

    internal void PublishCompilationResultChangedNotification( CompilationResultChangedEventData notification )
    {
        if ( !this._compilationResultChangedEvent.HasHandlers() )
        {
            this._logger.Warning?.Log( "Do not publish a change notification because there is no listener." );
        }

        this._compilationResultChangedEvent.Invoke( notification );
    }

    public event Action<ProjectKey>? DirtyProject;

    internal void OnProjectDirty( ProjectKey projectKey ) => this.DirtyProject?.Invoke( projectKey );

    internal AsyncEvent<ProjectKey>.Accessors ExternalBuildCompletedEvent => this._externalBuildCompletedEvent.GetAccessors();

    /// <summary>
    /// Gets an event raised when the pipeline result has changed because of an external cause, i.e.
    /// not a change in the source code of the project of the pipeline itself.
    /// </summary>
    internal WeakEventBase<Func<DesignTimePipelineStatusChangedEventArgs, Task>, DesignTimePipelineStatusChangedEventArgs>.Accessors PipelineStatusChangedEvent
        => this._pipelineStatusChangedEvent.GetAccessors();

    internal Task OnPipelineStatusChangedEventAsync( DesignTimePipelineStatusChangedEventArgs args )
    {
        if ( args.IsResuming )
        {
            if ( this._projectsWithPausedPipeline.TryRemove( args.Pipeline.ProjectKey, out _ ) && this._projectsWithPausedPipeline.IsEmpty )
            {
                this.IsEditingCompileTimeCode = false;
            }
        }
        else if ( args.IsPausing )
        {
            if ( this._projectsWithPausedPipeline.TryAdd( args.Pipeline.ProjectKey, args.Pipeline.ProjectKey ) )
            {
                this.IsEditingCompileTimeCode = true;
            }
        }

        return this._pipelineStatusChangedEvent.InvokeAsync( args );
    }

    internal void ResetIsEditingCompileTimeCode()
    {
        if ( !this._projectsWithPausedPipeline.IsEmpty )
        {
            this._logger.Error?.Log( $"The following projects were not expected to be paused: {string.Join( ",", this._projectsWithPausedPipeline.Keys )}" );
        }

        this._projectsWithPausedPipeline.Clear();
        this.IsEditingCompileTimeCode = false;
    }

    internal Task OnExternalBuildCompletedEventAsync( ProjectKey projectKey )
    {
        return this._externalBuildCompletedEvent.InvokeAsync( projectKey );
    }

    public event Action<ProjectKey, IReadOnlyCollection<DiagnosticData>>? CompileTimeErrorsChanged;

    internal void PublishCompileTimeErrors( ProjectKey projectKey, IReadOnlyCollection<DiagnosticData> errors )
    {
        this.CompileTimeErrorsChanged?.Invoke( projectKey, errors );
    }

    public event Action<ProjectKey>? AspectClassesChanged;

    internal void OnAspectClassesChanged( ProjectKey projectKey ) => this.AspectClassesChanged?.Invoke( projectKey );

    public event Action<ProjectKey>? AspectInstancesChanged;

    internal void OnAspectInstancesChanged( ProjectKey projectKey ) => this.AspectInstancesChanged?.Invoke( projectKey );
}