// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;

namespace Metalama.Framework.Engine.Utilities.Diagnostics;

internal static class LoggerFactoryExtensions
{
    public static ILogger Remoting( this ILoggerFactory loggerFactory ) => loggerFactory.GetLogger( "Remoting" );

    public static ILogger DesignTime( this ILoggerFactory loggerFactory ) => loggerFactory.GetLogger( "DesignTime" );

    public static ILogger CompileTime( this ILoggerFactory loggerFactory ) => loggerFactory.GetLogger( "CompileTime" );
}