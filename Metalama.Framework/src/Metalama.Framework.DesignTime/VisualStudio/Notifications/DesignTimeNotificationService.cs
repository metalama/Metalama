// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.DesignTime.Contracts.Notifications;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.DesignTime.VisualStudio.ServiceHub;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.Notifications;

/// <summary>
/// Implementation of the cross-version-safe <see cref="IDesignTimeNotificationService"/>. Subscribes to
/// in-process events on <see cref="ServiceHubServerEndpoint"/>, translates them to <c>[Guid]</c>-marked
/// Contracts DTOs, and fans them out to registered observers.
/// </summary>
internal sealed partial class DesignTimeNotificationService : IDesignTimeNotificationService
{
    private readonly ILogger _logger;
    private readonly object _sync = new();
    private readonly Dictionary<string, List<IDesignTimeNotificationObserver>> _observersByEventType = new( StringComparer.Ordinal );

    public DesignTimeNotificationService( GlobalServiceProvider serviceProvider )
        : this( serviceProvider.GetLoggerFactory().GetLogger( nameof(DesignTimeNotificationService) ) )
    {
        var endpoint = serviceProvider.GetRequiredService<ServiceHubServerEndpoint>();
        endpoint.InProcessEventReceived += this.OnRpcEventReceived;
    }

    // For tests: bypass the endpoint hookup so the service can be exercised in isolation by calling Publish directly.
    internal DesignTimeNotificationService( ILogger logger )
    {
        this._logger = logger;
    }

    // For tests: simulates the arrival of an event from the analyzer process.
    internal void Publish( RpcEventData eventData ) => this.OnRpcEventReceived( eventData );

    public IDisposable Subscribe( IDesignTimeNotificationObserver observer, string[] eventTypeNames )
    {
        if ( observer == null! )
        {
            throw new ArgumentNullException( nameof(observer) );
        }

        if ( eventTypeNames == null! )
        {
            throw new ArgumentNullException( nameof(eventTypeNames) );
        }

        lock ( this._sync )
        {
            foreach ( var name in eventTypeNames )
            {
                if ( !this._observersByEventType.TryGetValue( name, out var list ) )
                {
                    list = new List<IDesignTimeNotificationObserver>();
                    this._observersByEventType[name] = list;
                }

                list.Add( observer );
            }
        }

        this._logger.Trace?.Log( $"Subscribed observer to [{string.Join( ", ", eventTypeNames )}]." );

        return new Subscription( this, observer, eventTypeNames );
    }

    private void OnRpcEventReceived( RpcEventData eventData )
    {
        var translated = Translate( eventData );

        if ( translated == null )
        {
            return;
        }

        IDesignTimeNotificationObserver[] snapshot;

        lock ( this._sync )
        {
            if ( !this._observersByEventType.TryGetValue( translated.EventTypeName, out var list ) || list.Count == 0 )
            {
                return;
            }

            snapshot = list.ToArray();
        }

        foreach ( var observer in snapshot )
        {
            // Fire-and-forget per observer so a slow or throwing observer cannot stall the analyzer's event loop.
            _ = Task.Run(
                () =>
                {
                    try
                    {
                        observer.OnEvent( translated );
                    }
                    catch ( Exception e )
                    {
                        this._logger.Warning?.Log( $"Observer threw from OnEvent({translated.EventTypeName}): {e}" );
                    }
                } );
        }
    }

    private static IDesignTimeNotificationEvent? Translate( RpcEventData eventData )
        => eventData switch
        {
            CompilationResultChangedEventData d => new CompilationResultChangedEvent(
                d.ProjectKey.ToString(),
                d.IsPartialCompilation,
                d.SyntaxTreePaths.IsDefault ? Array.Empty<string>() : d.SyntaxTreePaths.ToArray() ),
            EndpointChangedEventData d => new EndpointChangedEvent( d.ProjectGuid ),
            _ => null
        };
}
