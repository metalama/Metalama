// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// A base class for RPC clients and servers.
/// </summary>
public abstract class BaseEndpoint : IDisposable
{
    private readonly CancellationTokenSource _disposeCancellationSource = new();
    private readonly ConcurrentDictionary<int, (Task Task, string Description)> _backgroundTasks = new();

    protected ITestSynchronizationProvider? TestSyncProvider { get; }

    private int _nextBackgroundTaskId;

    protected TaskCompletionSource<bool> InitializedTask { get; } = new();

    protected IRpcExceptionHandler? ExceptionHandler { get; }

    protected ILogger Logger { get; }

    internal ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Gets the name of the principal pipe. Server endpoints can have several pipes, each with a different set of services (when added dynamically).
    /// In this case, the name of the additional pipes is formed by adding a suffix to the <see cref="PipeName"/> property.
    /// </summary>
    public string PipeName { get; }

    protected BaseEndpoint(
        IServiceProvider serviceProvider,
        string pipeName )
    {
        this.LoggerFactory = serviceProvider.GetLoggerFactory();
        this.Logger = serviceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
        this.PipeName = pipeName;
        this.ExceptionHandler = (IRpcExceptionHandler?) serviceProvider.GetService( typeof(IRpcExceptionHandler) );
        this.TestSyncProvider = serviceProvider.GetService( typeof(ITestSynchronizationProvider) ) as ITestSynchronizationProvider;
    }

    public async ValueTask WaitUntilInitializedAsync( CancellationToken cancellationToken = default )
    {
        if ( this.InitializedTask.Task.Status == TaskStatus.RanToCompletion )
        {
            return;
        }

        // Take the stack trace before any yield.
        var delayTask = Task.Delay( 1000, cancellationToken );

        if ( await Task.WhenAny( delayTask, this.InitializedTask.Task ) == delayTask )
        {
            string stackTrace;

            try
            {
                stackTrace = new StackTrace().ToString();
            }
            catch
            {
                stackTrace = "(cannot get a stack trace)";
            }

            this.Logger.Warning?.Log(
                $"Waiting for the endpoint '{this.PipeName}' to be initialized is taking a long time " + Environment.NewLine
                                                                                                       + stackTrace );
        }

        try
        {
            await this.InitializedTask.Task.WithCancellation( cancellationToken );

            this.Logger.Trace?.Log( $"Endpoint '{this.PipeName}' is now initialized." );
        }
        catch ( OperationCanceledException )
        {
            this.Logger.Warning?.Log( $"Waiting for the endpoint '{this.PipeName}' to be initialized: wait cancelled." );

            throw;
        }
    }

    protected static JsonRpc CreateRpc( Stream stream )
    {
        var formatter = new MessagePackFormatter();

        // Use TypelessObjectResolver with CompositeResolver to handle both polymorphic types
        // and immutable collections. StandardResolver includes formatters for ImmutableArray<T>, etc.
        var resolver = CompositeResolver.Create(
            TypelessObjectResolver.Instance,
            StandardResolver.Instance );

        var options = MessagePackSerializerOptions.Standard.WithResolver( resolver );
        formatter.SetMessagePackSerializerOptions( options );

        var handler = new LengthHeaderMessageHandler( stream, stream, formatter );

        return new JsonRpc( handler );
    }

    public override string ToString() => $"{this.GetType().Name}({this.PipeName})";

    /// <summary>
    /// Executes a background type without awaiting for the result. Reports exceptions to the logger and the <see cref="IRpcExceptionHandler"/>.
    /// </summary>
    protected void ExecuteBackgroundTask( Func<CancellationToken, Task> action, string description, bool registerTask = true )
    {
        var taskId = Interlocked.Increment( ref this._nextBackgroundTaskId );

        this.Logger.Trace?.Log( $"Scheduling background task {taskId}: '{description}'." );

        var backgroundTaskWillBeRemoved = true;

        var task = Task.Run(
            async () =>
            {
                try
                {
                    await action( this.DisposeCancellationToken );
                    this.Logger.Trace?.Log( $"Background task {taskId} has completed." );
                }
                catch ( Exception e ) when ( this.ExceptionHandler != null )
                {
                    this.Logger.LogException( e );
                    this.ExceptionHandler?.OnException( e, this.Logger, this.DisposeCancellationToken.IsCancellationRequested );
                }
                finally
                {
                    if ( registerTask )
                    {
                        lock ( this._backgroundTasks )
                        {
                            this._backgroundTasks.TryRemove( taskId, out _ );
                            backgroundTaskWillBeRemoved = false;
                        }
                    }
                }
            },
            this.DisposeCancellationToken );

        if ( registerTask )
        {
            // Always add the task first. The finally block will remove it when complete.
            // We must not check IsCompleted here because the task may complete between
            // Task.Run and this point, causing WhenBackgroundTasksCompletedAsync to miss it.
            lock ( this._backgroundTasks )
            {
                if ( backgroundTaskWillBeRemoved )
                {
                    this._backgroundTasks.TryAdd( taskId, (task, description) );
                }
            }
        }
    }

    protected CancellationToken DisposeCancellationToken => this._disposeCancellationSource.Token;

    protected virtual void Dispose( bool disposing )
    {
        this.Logger.Trace?.Log( $"Disposing endpoint '{this.PipeName}'." );

        try
        {
            this._disposeCancellationSource.Cancel();
        }
        catch ( Exception e )
        {
            this.Logger.LogException( e );
        }

        GC.SuppressFinalize( this );
    }

    // ReSharper disable InconsistentlySynchronizedField
    /// <summary>
    /// Waits until all background tasks have completed. This method loops until no tasks remain,
    /// ensuring that tasks scheduled during the completion of other tasks are also awaited.
    /// </summary>
    public async Task WhenBackgroundTasksCompletedAsync( CancellationToken cancellationToken )
    {
        while ( true )
        {
            var tasks = this._backgroundTasks.Values.ToArray();

            if ( tasks.Length == 0 )
            {
                return;
            }

            await Task.WhenAll( tasks.Select( t => t.Task ) )
                .WithCancellation( cancellationToken )
                .WarnIfLongAsync(
                    this.Logger,
                    $"Awaiting for background task(s):{string.Join( ", ", tasks.Select( x => x.Description ) )}",
                    cancellationToken );
        }
    }

    // ReSharper restore once InconsistentlySynchronizedField

    public void Dispose() => this.Dispose( true );

    ~BaseEndpoint()
    {
        this.Dispose( false );
    }
}