// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TRACE_JSON
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;

namespace Metalama.Backstage.Diagnostics;

internal sealed class JsonTraceWriter : ITraceWriter
{
    private readonly string _filePath;
    private readonly ILogger _logger;

    public JsonTraceWriter( string filePath, ILogger logger )
    {
        this._filePath = filePath;
        this._logger = logger;
    }

    public void Trace( TraceLevel level, string message, Exception? ex )
    {
        switch ( level )
        {
            case TraceLevel.Error:
                this._logger.Error?.Log( message );

                break;

            case TraceLevel.Warning:
                this._logger.Warning?.Log( message );

                break;

            case TraceLevel.Info:
            case TraceLevel.Verbose:
                if ( message.StartsWith( "Unable to", StringComparison.OrdinalIgnoreCase )
                     || message.StartsWith( "Could not find", StringComparison.OrdinalIgnoreCase ) )
                {
                    this._logger.Warning?.Log( $"Recoverable error in '{this._filePath}': {message}" );
                }
                else
                {
                    this._logger.Trace?.Log( message );
                }

                break;
        }
    }

    // Some warnings are reported as Verbose so we need to capture all messages.
    public TraceLevel LevelFilter => TraceLevel.Verbose;
}
#endif