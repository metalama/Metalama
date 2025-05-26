// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using StreamJsonRpc;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// A base class for RPC clients and servers.
/// </summary>
public abstract class BaseEndpoint : IDisposable
{
    private readonly JsonSerializationBinder _binder;
    private readonly CancellationTokenSource _disposeCancellationSource = new();
    private readonly ConcurrentDictionary<int, (Task Task, string Description)> _backgroundTasks = new();

    protected IEndpointObserver? Observer { get; }

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

        var binderProvider = (IJsonSerializationBinderProvider?) serviceProvider.GetService( typeof(IJsonSerializationBinderProvider) )
                             ?? throw new InvalidOperationException( "Cannot get the IJsonSerializationBinderProvider" );

        this._binder = binderProvider.Binder;
        this.Observer = serviceProvider.GetService( typeof(IEndpointObserver) ) as IEndpointObserver;
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

    protected JsonRpc CreateRpc( Stream stream )
    {
        // MessagePackFormatter does not work in the devenv process, probably because devenv sets it up with some global effect.

        /*
        var formatter = new MessagePackFormatter();
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create( BuiltinResolver.Instance, DynamicObjectResolverAllowPrivate.Instance ) );
            formatter.SetMessagePackSerializerOptions( options );
        */

        var formatter = new JsonMessageFormatter();
        formatter.JsonSerializer.TypeNameHandling = TypeNameHandling.All;

        // We have to specify the full assembly name otherwise there are conflicts when several versions of Metalama are loaded in the AppDomain (see #31075).
        // However, we need to remove the version number for non-Metalama assemblies because different versions of these libraries may run on both ends
        // of the pipe. The solution is to specify TypeNameAssemblyFormatHandling.Full but implement our JsonSerializationBinder.
        formatter.JsonSerializer.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
        formatter.JsonSerializer.SerializationBinder = this._binder;

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
                    this.Logger.Error?.Log( e.ToString() );
                    this.ExceptionHandler?.OnException( e, this.Logger, this.DisposeCancellationToken.IsCancellationRequested );
                }
                finally
                {
                    if ( registerTask )
                    {
                        lock ( this._backgroundTasks )
                        {
                            this._backgroundTasks.TryRemove( taskId, out _ );
                        }
                    }
                }
            },
            this.DisposeCancellationToken );

        if ( registerTask )
        {
            lock ( this._backgroundTasks )
            {
                if ( task is { IsCompleted: false, IsCanceled: false, IsFaulted: false } )
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
            this.Logger.Error?.Log( e.ToString() );
        }

        GC.SuppressFinalize( this );
    }

    // ReSharper disable InconsistentlySynchronizedField
    public Task WhenBackgroundTasksCompletedAsync( CancellationToken cancellationToken )
        => Task.WhenAll( this._backgroundTasks.Values.Select( t => t.Task ) )
            .WithCancellation( cancellationToken )
            .WarnIfLongAsync(
                this.Logger,
                $"Awaiting for background task(s):{string.Join( ", ", this._backgroundTasks.Values.Select( x => x.Description ) )}",
                cancellationToken );

    // ReSharper restore once InconsistentlySynchronizedField

    public void Dispose() => this.Dispose( true );

    ~BaseEndpoint()
    {
        this.Dispose( false );
    }
}