// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

    protected CancellationToken ApplicationExitingToken { get; }

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
        this.ApplicationExitingToken = serviceProvider.GetRequiredService<ApplicationExitManager>().Token;
    }

    public abstract SourceGeneratorResult GenerateSources( Compilation compilation, TestableCancellationToken cancellationToken );

    protected virtual void Dispose( bool disposing )
    {
        if ( disposing && !this.ApplicationExitingToken.IsCancellationRequested )
        {
            this._taskRunner.RunSynchronously(
                () => this.PendingTasks.WaitAllAsync( this.ApplicationExitingToken ),
                cancellationToken: this.ApplicationExitingToken );
        }
    }

    public void Dispose() => this.Dispose( true );

    public TaskBag PendingTasks { get; }
}