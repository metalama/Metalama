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
        // Resolve the target writer first: when the corresponding ILogger writer is null (the level isn't
        // enabled in the Metalama logger configuration), skip building the formatted string entirely.
        var writer = this.GetWriter( eventType );

        if ( writer == null )
        {
            return;
        }

        writer.Log( $"[{source}#{id}] {message}" );
    }

    public override void TraceEvent(
        TraceEventCache? eventCache,
        string source,
        TraceEventType eventType,
        int id,
        string? format,
        params object?[]? args )
    {
        // Same early-return discipline as the message overload — don't pay for string.Format when the writer
        // is null. StreamJsonRpc emits some events (e.g., argument-payload dumps in Verbose mode) with format
        // strings, so skipping the format call when nobody is listening matters.
        var writer = this.GetWriter( eventType );

        if ( writer == null )
        {
            return;
        }

        var message = format != null
            ? string.Format( CultureInfo.InvariantCulture, format, args ?? Array.Empty<object?>() )
            : string.Empty;

        writer.Log( $"[{source}#{id}] {message}" );
    }

    private ILogWriter? GetWriter( TraceEventType eventType )
        => eventType switch
        {
            TraceEventType.Critical or TraceEventType.Error => this._logger.Error,
            TraceEventType.Warning => this._logger.Warning,
            TraceEventType.Information => this._logger.Info,
            _ => this._logger.Trace
        };
}
