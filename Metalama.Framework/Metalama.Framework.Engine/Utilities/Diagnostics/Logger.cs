// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Utilities;
using System;
using System.Diagnostics;

namespace Metalama.Framework.Engine.Utilities.Diagnostics
{
    public static class Logger
    {
        private static readonly object _initializeSync = new();
        private static ILoggerFactory? _loggerFactory;

        internal static ILoggerFactory LoggerFactory => _loggerFactory ?? NullLogger.Instance;

        /// <summary>
        /// Initializes all loggers from the support services.
        /// </summary>
        /// <remarks>
        /// This method should only be called from the MetalamaDiagnosticsService.Initialize method.
        /// </remarks>
        internal static void Initialize()
        {
            lock ( _initializeSync )
            {
                if ( _loggerFactory != null )
                {
                    return;
                }

                _loggerFactory = BackstageServiceFactory.ServiceProvider.GetLoggerFactory();
                var processInfo = _loggerFactory.GetLogger( "ProcessInfo" );

                processInfo.Info?.Log( $"Command line: {Environment.CommandLine}" );
                processInfo.Info?.Log( $"Process kind: {ProcessUtilities.ProcessKind}" );
                processInfo.Info?.Log( $"Process name: {Process.GetCurrentProcess().ProcessName.ToLowerInvariant()}" );
                processInfo.Info?.Log( $"Version: {AssemblyMetadataReader.GetInstance( typeof(Logger).Assembly ).BuildId}" );

                DesignTime = _loggerFactory.DesignTime();
                Remoting = _loggerFactory.Remoting();
                DesignTimeEntryPointManager = _loggerFactory.GetLogger( "DesignTimeEntryPointManager" );
                Domain = _loggerFactory.GetLogger( "Domain" );
            }
        }

        // The DesignTime logger is used before the service container is initialized, therefore we use the global instance.
        public static ILogger DesignTime { get; private set; } = NullLogger.Instance;

        public static ILogger Remoting { get; private set; } = NullLogger.Instance;

        public static ILogger DesignTimeEntryPointManager { get; private set; } = NullLogger.Instance;

        internal static ILogger Domain { get; private set; } = NullLogger.Instance;
    }
}