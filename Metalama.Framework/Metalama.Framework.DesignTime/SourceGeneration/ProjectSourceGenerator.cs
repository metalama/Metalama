// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime.SourceGeneration;

/// <summary>
/// Handles the compiler requests (currently only from source generators) for a specific project.
/// </summary>
internal abstract class ProjectSourceGenerator : IDisposable
{
    private readonly ITaskRunner _taskRunner;

    protected GlobalServiceProvider ServiceProvider { get; }

    protected IProjectOptions ProjectOptions { get; }

    protected ProjectKey ProjectKey { get; }

    protected ILogger Logger { get; }

    /// <summary>
    /// Gets the latest touch ID for the current project, which should usually be equivalent to the contents of the touch file.
    /// Returns <see langword="null" /> when the touch ID was not yet set by the current process.
    /// </summary>
    internal string? LastTouchId { get; private protected set; }

    protected ProjectSourceGenerator( GlobalServiceProvider serviceProvider, IProjectOptions projectOptions, ProjectKey projectKey )
    {
        this.ServiceProvider = serviceProvider;
        this.ProjectOptions = projectOptions;
        this.ProjectKey = projectKey;
        this.Logger = this.ServiceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
        this.PendingTasks = new TaskBag( this.Logger, serviceProvider );
        this._taskRunner = this.ServiceProvider.GetRequiredService<ITaskRunner>();
    }

    public abstract SourceGeneratorResult GenerateSources( Compilation compilation, TestableCancellationToken cancellationToken );

    protected virtual void Dispose( bool disposing )
    {
        if ( disposing )
        {
            this._taskRunner.RunSynchronously( () => this.PendingTasks.WaitAllAsync() );
        }
    }

    public void Dispose() => this.Dispose( true );

    public TaskBag PendingTasks { get; }
}