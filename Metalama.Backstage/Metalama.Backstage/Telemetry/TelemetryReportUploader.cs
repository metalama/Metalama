// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Audit;
using System;
using System.IO;
using System.Text;

namespace Metalama.Backstage.Telemetry;

internal sealed class TelemetryReportUploader : IBackstageService
{
    private readonly IStandardDirectories _directories;
    private readonly IFileSystem _fileSystem;
    private readonly ITelemetryUploader _uploader;
    private readonly ILogger _logger;
    private readonly TelemetryLogger _telemetryLogger;

    public TelemetryReportUploader( IServiceProvider serviceProvider )
    {
        this._directories = serviceProvider.GetRequiredBackstageService<IStandardDirectories>();
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        this._uploader = serviceProvider.GetRequiredBackstageService<ITelemetryUploader>();
        this._telemetryLogger = serviceProvider.GetRequiredBackstageService<TelemetryLogger>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( "Metrics" );
    }

    public void Upload( TelemetryReport report )
    {
        // If no filename was provided, we have to write metrics to the standard reporting directory.

        this.CreateUploadDirectory();

        try
        {
            // TODO: Write multiple reports to the same file.
            var fileName = Path.Combine( this._directories.TelemetryUploadQueueDirectory, $"{report.Kind}-{Guid.NewGuid()}.log" );
            this.Write( report, fileName );
        }
        catch ( Exception e )
        {
            this._logger.Error?.Log( e.ToString() );

            return;
        }

        // Start the upload periodically.
        this._uploader.StartUpload();
    }

    private void Write( TelemetryReport report, string fileName )
    {
        this._logger.Trace?.Log( $"Writing telemetry report to '{fileName}' file." );

        var directory = Path.GetDirectoryName( fileName );

        if ( directory != null )
        {
            this._fileSystem.CreateDirectory( directory );
        }

        var formattedReport = FormatReport( report );

        using ( var stream = this._fileSystem.Open( fileName, FileMode.Append, FileAccess.Write, FileShare.None ) )
        using ( var writer = new StreamWriter( stream, Encoding.UTF8 ) )
        {
            writer.WriteLine( formattedReport );
        }

        this._logger.Trace?.Log( $"Appended to '{fileName}': '{formattedReport}'." );
        this._telemetryLogger.WriteLine( $"Appended to '{fileName}': '{formattedReport}'." );
    }

    private static string FormatReport( TelemetryReport report )
    {
        using var stringWriter = new StringWriter();
        var first = true;

        foreach ( var metric in report.Metrics )
        {
            if ( first )
            {
                first = false;
            }
            else
            {
                stringWriter.Write( ';' );
            }

            stringWriter.Write( metric.Name );
            stringWriter.Write( '=' );
            metric.WriteValue( stringWriter );
        }

        return stringWriter.ToString();
    }

    private void CreateUploadDirectory()
    {
        if ( !this._fileSystem.DirectoryExists( this._directories.TelemetryUploadQueueDirectory ) )
        {
            this._logger.Trace?.Log( $"Creating '{this._directories.TelemetryUploadQueueDirectory}' directory." );
            this._fileSystem.CreateDirectory( this._directories.TelemetryUploadQueueDirectory );
        }
    }
}