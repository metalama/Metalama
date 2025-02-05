// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Utilities;
using System;
using System.Globalization;
using System.IO;

namespace Metalama.Backstage.Licensing.Audit;

/// <summary>
/// An additional logger for the telemetry component, creating short but permanent logs.
/// </summary>
internal class TelemetryLogger : IBackstageService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _logsDirectory;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly string _source;

    public TelemetryLogger( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._logsDirectory = serviceProvider.GetRequiredBackstageService<IStandardDirectories>().TelemetryLogsDirectory;
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(TelemetryLogger) );
        var applicationInfo = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>().CurrentApplication;
        this._source = $"{applicationInfo.Name} {applicationInfo.PackageVersion}";
    }

    public void WriteLine( string line )
    {
        try
        {
            var fileName = Path.Combine( this._logsDirectory, $"Telemetry-{DateTime.Now.Year}-{DateTime.Now.Month:00}.log" );

            using ( MutexHelper.WithGlobalLock( fileName ) )
            {
                if ( !this._fileSystem.DirectoryExists( this._logsDirectory ) )
                {
                    this._fileSystem.CreateDirectory( this._logsDirectory );
                }

                var timestamp = DateTime.Now.ToString( CultureInfo.InvariantCulture );

                RetryHelper.RetryWithLockDetection(
                    fileName,
                    f => this._fileSystem.AppendAllLines( f, new[] { $"[{timestamp}, {this._source}] {line}" } ),
                    this._serviceProvider );
            }
        }
        catch ( Exception e )
        {
            this._logger.Error?.Log( e.ToString() );
        }
    }
}