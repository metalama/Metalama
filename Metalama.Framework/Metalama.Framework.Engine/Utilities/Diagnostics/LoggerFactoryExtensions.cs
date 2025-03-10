// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;

namespace Metalama.Framework.Engine.Utilities.Diagnostics;

internal static class LoggerFactoryExtensions
{
    public static ILogger Remoting( this ILoggerFactory loggerFactory ) => loggerFactory.GetLogger( "Remoting" );

    public static ILogger DesignTime( this ILoggerFactory loggerFactory ) => loggerFactory.GetLogger( "DesignTime" );

    public static ILogger CompileTime( this ILoggerFactory loggerFactory ) => loggerFactory.GetLogger( "CompileTime" );
}