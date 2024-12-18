// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using StreamJsonRpc;
using System.Diagnostics;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// A base class for RPC clients and servers.
/// </summary>
public abstract class ServiceEndpoint
{
    private readonly JsonSerializationBinder _binder;

    protected TaskCompletionSource<bool> InitializedTask { get; } = new();

    protected IRpcExceptionHandler? ExceptionHandler { get; }

    protected ILogger Logger { get; }

    public string PipeName { get; }

    protected ServiceEndpoint( IServiceProvider serviceProvider, string pipeName, JsonSerializationBinder? binder = null )
    {
        this.Logger = serviceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
        this.PipeName = pipeName;
        this.ExceptionHandler = (IRpcExceptionHandler?) serviceProvider.GetService( typeof(IRpcExceptionHandler) );
        this._binder = binder ?? JsonSerializationBinder.Default;
    }

    public async ValueTask WaitUntilInitializedAsync( string callerName, CancellationToken cancellationToken = default )
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
                $"Waiting for the endpoint '{this.PipeName}' to be initialized because of {callerName} is taking a long time " + Environment.NewLine
                + stackTrace );
        }

        try
        {
            await this.InitializedTask.Task.WithCancellation( cancellationToken );

            this.Logger.Trace?.Log( $"Endpoint '{this.PipeName}' is now initialized." );
        }
        catch ( OperationCanceledException )
        {
            this.Logger.Warning?.Log( $"Waiting for the endpoint '{this.PipeName}' to be initialized because of {callerName}: wait cancelled." );

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
}