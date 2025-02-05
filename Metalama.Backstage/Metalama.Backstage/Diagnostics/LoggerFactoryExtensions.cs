// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Diagnostics;

public static class LoggerFactoryExtensions
{
    internal static ILogger Licensing( this ILoggerFactory loggerFactory ) => loggerFactory.GetLogger( "Licensing" );

    internal static ILogger Telemetry( this ILoggerFactory loggerFactory ) => loggerFactory.GetLogger( "Telemetry" );
}