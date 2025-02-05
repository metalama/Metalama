// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#if !WORKER_PROCESS && (NETFRAMEWORK || NET6_0_OR_GREATER)
#define PROFILING_ENABLED
#endif

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using System;

#if PROFILING_ENABLED
using JetBrains.Profiler.Api;
using JetBrains.Profiler.SelfApi;
using Metalama.Backstage.Maintenance;
using System.Runtime.CompilerServices;
using System.Threading;
#endif

namespace Metalama.Backstage.Diagnostics;

internal class ProfilingService : IProfilingService
{
#if PROFILING_ENABLED
    private static int _isProfiling;
#endif

    private readonly ProcessKind _processKind;
    private readonly DiagnosticsConfiguration _configuration;
    private readonly ILogger _logger;

#if PROFILING_ENABLED
    private readonly ITempFileManager _tempFileManager;
#endif

    public ProfilingService( IServiceProvider serviceProvider )
    {
        this._processKind = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication.ProcessKind;
        this._configuration = serviceProvider.GetRequiredBackstageService<IConfigurationManager>().Get<DiagnosticsConfiguration>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "Profiling" );

#if PROFILING_ENABLED
        this._tempFileManager = serviceProvider.GetRequiredBackstageService<ITempFileManager>();
#endif
    }

    public void Initialize()
    {
        if ( this._configuration.Profiling.Processes.TryGetValue( this._processKind.ToString(), out var enabled ) && enabled )
        {
            // ReSharper disable once RedundantBlankLines
#if PROFILING_ENABLED
            
            // The implementation is intentionally in a different type to avoid the loading of JetBrains assemblies if profiling
            // is not requested.
            ProfilerContainer.StartProfiler( this._tempFileManager, this._configuration, this._logger );
#endif
        }
    }

    /// <summary>
    /// If memory profiling is active, captures a snapshot, or does nothing otherwise.
    /// </summary>
    public void CreateMemorySnapshot( string? snapshotName = null )
    {
        if ( this._configuration.Profiling.Processes.TryGetValue( this._processKind.ToString(), out var enabled ) && enabled )
        {
            // ReSharper disable once RedundantBlankLines
#if PROFILING_ENABLED
            
            // The implementation is intentionally in a different type to avoid the loading of JetBrains assemblies if profiling
            // is not requested.
            ProfilerContainer.CreateMemorySnapshot( this._configuration, this._logger, snapshotName );
#else
            this._logger.Warning?.Log( $"The profiler was not started because this is the .NET Standard 2.0 build of Metalama.Backstage'." );
#endif
        }
    }

#if PROFILING_ENABLED
    private static class ProfilerContainer
    {
        [MethodImpl( MethodImplOptions.NoInlining )]
        public static void CreateMemorySnapshot( DiagnosticsConfiguration configuration, ILogger logger, string? snapshotName )
        {
            if ( _isProfiling != 0 )
            {
                switch ( configuration.Profiling.Kind?.ToLowerInvariant() )
                {
                    case "memory" or "memory-allocation":
                        logger.Error?.Log( $"Getting snapshot \"{snapshotName}\"." );

                        try
                        {
                            DotMemory.GetSnapshot( snapshotName );
                        }
                        catch ( Exception e )
                        {
                            logger.Error?.Log( $"Cannot get a snapshot: {e}" );
                        }

                        break;
                }
            }
        }

        [MethodImpl( MethodImplOptions.NoInlining )]
        public static void StartProfiler( ITempFileManager tempFileManager, DiagnosticsConfiguration configuration, ILogger logger )
        {
            if ( Interlocked.CompareExchange( ref _isProfiling, 1, 0 ) != 0 )
            {
                logger.Trace?.Log( $"Profiling is already in progress." );

                return;
            }

            var directory = tempFileManager.GetTempDirectory( "Profiling", CleanUpStrategy.WhenUnused );

            logger.Warning?.Log( $"Starting the profiler. Data will be stored in '{directory}'." );

            var kind = configuration.Profiling.Kind?.ToLowerInvariant();

            switch ( kind )
            {
                case null or "performance" or "performance-tracing" or "performance-timeline":
                    try
                    {
                        DotTrace.Init();

                        var config = new DotTrace.Config().SaveToDir( directory );

                        if ( kind is "performance-timeline" )
                        {
                            config = config.UseTimelineProfilingType( true );
                        }

                        DotTrace.Attach( config );
                        DotTrace.StartCollectingData();
                    }
                    catch ( Exception e )
                    {
                        logger.Error?.Log( $"Cannot start the profiler: {e}" );
                    }

                    break;

                case "memory" or "memory-allocation":
                    try
                    {
                        DotMemory.Init();

                        var config = new DotMemory.Config().SaveToDir( directory );

                        DotMemory.Attach( config );

                        if ( kind is "memory-allocation" )
                        {
                            MemoryProfiler.CollectAllocations( true );
                        }

                        DotMemory.GetSnapshot( "initial" );
                    }
                    catch ( Exception e )
                    {
                        logger.Error?.Log( $"Cannot start the profiler: {e}" );
                    }

                    break;

                default:
                    logger.Error?.Log( $"Unknown kind of profiling was specified start the profiler: {configuration.Profiling.Kind}" );

                    break;
            }
        }
    }
#endif
}