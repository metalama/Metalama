// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using System.Diagnostics;
using System.Globalization;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Forwards <see cref="TraceSource"/> events to a Metalama <see cref="ILogger"/>. Used to surface
/// StreamJsonRpc's internal events (request/response dispatch, argument-deserialization errors, connection
/// lifecycle) in the same logging stream as the rest of the RPC layer, so production interactive-VS sessions
/// can capture the actual failure point of issue #1606-class symptoms.
/// </summary>
internal sealed class LoggerTraceListener : TraceListener
{
    private readonly ILogger _logger;

    public LoggerTraceListener( ILogger logger )
    {
        this._logger = logger;
    }

    public override void Write( string? message ) => this._logger.Trace?.Log( message ?? string.Empty );

    public override void WriteLine( string? message ) => this._logger.Trace?.Log( message ?? string.Empty );

    public override void TraceEvent( TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message )
    {
        var formatted = $"[{source}#{id}] {message}";

        switch ( eventType )
        {
            case TraceEventType.Critical:
            case TraceEventType.Error:
                this._logger.Error?.Log( formatted );

                break;

            case TraceEventType.Warning:
                this._logger.Warning?.Log( formatted );

                break;

            case TraceEventType.Information:
                this._logger.Info?.Log( formatted );

                break;

            default:
                this._logger.Trace?.Log( formatted );

                break;
        }
    }

    public override void TraceEvent(
        TraceEventCache? eventCache,
        string source,
        TraceEventType eventType,
        int id,
        string? format,
        params object?[]? args )
    {
        var message = format != null
            ? string.Format( CultureInfo.InvariantCulture, format, args ?? Array.Empty<object?>() )
            : string.Empty;

        this.TraceEvent( eventCache, source, eventType, id, message );
    }
}
