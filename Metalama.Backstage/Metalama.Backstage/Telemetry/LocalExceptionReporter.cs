// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.UserInterface;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Metalama.Backstage.Telemetry;

internal class LocalExceptionReporter : IBackstageService
{
    private readonly IStandardDirectories _standardDirectories;
    private readonly IApplicationInfoProvider _applicationInfoProvider;
    private readonly IToastNotificationService? _toastNotificationService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    public LocalExceptionReporter( IServiceProvider serviceProvider )
    {
        this._standardDirectories = serviceProvider.GetRequiredBackstageService<IStandardDirectories>();
        this._applicationInfoProvider = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>();
        this._toastNotificationService = serviceProvider.GetBackstageService<IToastNotificationService>();
        this._loggerFactory = serviceProvider.GetLoggerFactory();
        this._logger = this._loggerFactory.GetLogger( this.GetType().Name );
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
    }

    public void ReportException( Exception exception, string? localReportPath )
    {
        this._logger.Error?.Log( exception.ToString() );
        
        // The app may crash after reporting the exception, so we flush the logs first.
        this._loggerFactory.Flush();

        try
        {
            if ( localReportPath == null )
            {
                // If the caller did not create the report file, create it ourselves.
                localReportPath = Path.Combine( this._standardDirectories.CrashReportsDirectory, $"exception-{Guid.NewGuid()}.txt" );

                var exceptionText = new StringBuilder();

#pragma warning disable CA1305
                exceptionText.AppendLine( $"Metalama Application: {this._applicationInfoProvider.CurrentApplication.Name}" );
                exceptionText.AppendLine( $"Metalama Version: {this._applicationInfoProvider.CurrentApplication.PackageVersion}" );
                exceptionText.AppendLine( $"Runtime: {RuntimeInformation.FrameworkDescription}" );
                exceptionText.AppendLine( $"Processor Architecture: {RuntimeInformation.ProcessArchitecture}" );
                exceptionText.AppendLine( $"OS Description: {RuntimeInformation.OSDescription}" );
                exceptionText.AppendLine( $"OS Architecture: {RuntimeInformation.OSArchitecture}" );
                exceptionText.AppendLine( $"Exception type: {exception.GetType()}" );
                exceptionText.AppendLine( $"Exception message: {exception.Message}" );

                try
                {
                    // The next line may fail.
                    var exceptionToString = exception.ToString();
                    exceptionText.AppendLine( "===== Exception ===== " );
                    exceptionText.AppendLine( exceptionToString );
                }

                // ReSharper disable once EmptyGeneralCatchClause
                catch { }
#pragma warning restore CA1305

                this._fileSystem.WriteAllText( localReportPath, exceptionText.ToString() );

                this._logger.Info?.Log( $"Creating an exception report in '{localReportPath}'." );
            }

            this._toastNotificationService?.Show(
                new ToastNotification(
                    ToastNotificationKinds.Exception,
                    Text: $"The process {this._applicationInfoProvider.CurrentApplication.Name} encountered "
                          + $"an unexpected exception: {exception.Message}",
                    Uri: "file:///" + localReportPath ) );
        }
        catch ( Exception e )
        {
            this._logger.Error?.Log( e.ToString() );
        }
    }
}