// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#define TRACE // Because TraceEvent has [Conditional("TRACE")]

using Flashtrace.Records;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Flashtrace.Loggers;

internal sealed class TraceSourceFlashtraceLogger : SimpleFlashtraceLogger
{
    private readonly TraceSource _traceSource;
    private static readonly ConcurrentDictionary<string, TraceSource> _traceSources = new( StringComparer.OrdinalIgnoreCase );

    internal TraceSourceFlashtraceLogger( IFlashtraceRoleLoggerFactory factory, FlashtraceRole role, string name ) : base( role, name )
    {
        this.Factory = factory;
        this._traceSource = GetTraceSource( role );
    }

    public override IFlashtraceRoleLoggerFactory Factory { get; }

    private static string GetSourceName( FlashtraceRole role ) => role.Name ?? "Logging";

    public static TraceSource GetTraceSource( FlashtraceRole? role = null )
    {
        var sourceName = GetSourceName( role ?? FlashtraceRole.Default );

        return _traceSources.GetOrAdd( sourceName, n => new TraceSource( n ) );
    }

    public override bool IsEnabled( FlashtraceLevel level )
    {
        if ( level == FlashtraceLevel.None )
        {
            return false;
        }

        return this._traceSource.Switch.ShouldTrace( GetTraceEventType( level ) );
    }

    protected override void Write( FlashtraceLevel level, LogRecordKind recordKind, string text, Exception? exception )
    {
        var eventType = GetTraceEventType( level );
        this._traceSource.TraceEvent( eventType, 0, text );
    }

    private static TraceEventType GetTraceEventType( FlashtraceLevel level )
    {
        switch ( level )
        {
            case FlashtraceLevel.Trace:
                return TraceEventType.Verbose;

            case FlashtraceLevel.Debug:
                return TraceEventType.Verbose;

            case FlashtraceLevel.Info:
                return TraceEventType.Information;

            case FlashtraceLevel.Warning:
                return TraceEventType.Warning;

            case FlashtraceLevel.Error:
                return TraceEventType.Error;

            case FlashtraceLevel.Critical:
                return TraceEventType.Critical;

            default:
                throw new ArgumentOutOfRangeException( nameof(level), level, null );
        }
    }
}