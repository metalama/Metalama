// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Metalama.Backstage.Telemetry;

internal class ExceptionReporter : IExceptionReporter, IDisposable
{
    private readonly TelemetryQueue _uploadManager;
    private readonly IDateTimeProvider _time;
    private readonly IApplicationInfoProvider _applicationInfoProvider;
    private readonly IStandardDirectories _directories;
    private readonly ILogger _logger;
    private readonly Regex _stackFrameRegex = new( @"\S+\([^\)]*\)" );
    private readonly IConfigurationManager _configurationManager;
    private readonly IFileSystem _fileSystem;
    private readonly bool _canIgnoreRecoverableExceptions;
    private readonly LocalExceptionReporter? _localExceptionReporter;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private TelemetryConfiguration _configuration;

    public ExceptionReporter( TelemetryQueue uploadManager, IServiceProvider serviceProvider )
    {
        this._uploadManager = uploadManager;
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._configuration = this._configurationManager.Get<TelemetryConfiguration>();
        this._configurationManager.ConfigurationFileChanged += this.OnConfigurationChanged;
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
        this._time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._applicationInfoProvider = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>();
        this._directories = serviceProvider.GetRequiredBackstageService<IStandardDirectories>();
        this._logger = serviceProvider.GetLoggerFactory().Telemetry();
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        this._canIgnoreRecoverableExceptions = serviceProvider.GetRequiredBackstageService<IRecoverableExceptionService>().CanIgnore;
        this._localExceptionReporter = serviceProvider.GetBackstageService<LocalExceptionReporter>();
    }

    private void OnConfigurationChanged( ConfigurationFile configuration )
    {
        if ( configuration is TelemetryConfiguration telemetryConfiguration )
        {
            this._configuration = telemetryConfiguration;
        }
    }

    private IEnumerable<string?> CleanStackTrace( string stackTrace )
    {
        foreach ( Match? match in this._stackFrameRegex.Matches( stackTrace ) )
        {
            yield return match?.Value;
        }
    }

    public bool ShouldReportException( Exception exception )
    {
        switch ( exception )
        {
            case IOException _:
            case SecurityException _:
            case UnauthorizedAccessException _:
            case WebException _:
            case OperationCanceledException _:
                this._logger.Trace?.Log( $"The exception '{exception.GetType().Name}' should not be reported because the exception type shows a user reason." );

                return false;
        }

        if ( exception.InnerException != null && !this.ShouldReportException( exception.InnerException ) )
        {
            this._logger.Trace?.Log( $"The exception '{exception.GetType().Name}' should not be reported because the inner exception should not be reported." );

            return false;
        }

        if ( exception is AggregateException aggregateException && aggregateException.InnerExceptions.Any( e => !this.ShouldReportException( e ) ) )
        {
            this._logger.Trace?.Log(
                $"The exception '{exception.GetType().Name}' should not be reported because some inner exception should not be reported." );

            return false;
        }

        return true;
    }

    private string ComputeExceptionHash( string? version, string exceptionTypeName, IEnumerable<string?> stackTraces )
    {
        var signature = new StringBuilder( 1024 );
        signature.Append( version ?? "?" );
        signature.Append( ':' );
        signature.Append( exceptionTypeName );
        signature.Append( ':' );

        var firstFrame = true;
        var lastFrameIsUser = false;

        foreach ( var stackTrace in stackTraces )
        {
            if ( stackTrace == null )
            {
                continue;
            }

            foreach ( var stackFrame in this.CleanStackTrace( stackTrace ) )
            {
                var writeStackFrame = stackFrame ?? "<null>";

#pragma warning disable CA1307
                if ( writeStackFrame.Contains( "#user" ) )
#pragma warning restore CA1307
                {
                    if ( lastFrameIsUser )
                    {
                        continue;
                    }
                    else
                    {
                        writeStackFrame = "#user";
                        lastFrameIsUser = true;
                    }
                }
                else
                {
                    lastFrameIsUser = false;
                }

                if ( firstFrame )
                {
                    firstFrame = false;
                }
                else
                {
                    signature.Append( ',' );
                }

                signature.Append( writeStackFrame );
            }
        }

        byte[] hashBytes;

        using ( var md5 = new MD5Managed() )
        {
            hashBytes = md5.ComputeHash( Encoding.UTF8.GetBytes( signature.ToString().Normalize() ) );
        }

        var hash = new StringBuilder( hashBytes.Length * 2 );

        foreach ( var t in hashBytes )
        {
            hash.AppendFormat( CultureInfo.InvariantCulture, "{0:x2}", t );
        }

        return hash.ToString();
    }

    public bool ShouldReportIssue( string hash )
    {
        if ( !this._applicationInfoProvider.CurrentApplication.ShouldCreateLocalCrashReports )
        {
            this._logger.Trace?.Log(
                $"The issue {hash} should not be reported because the errors in the application '{this._applicationInfoProvider.CurrentApplication}' should never be reported." );

            return false;
        }

        if ( this._configuration.Issues.TryGetValue( hash, out var currentStatus ) && currentStatus is ReportingStatus.Ignored or ReportingStatus.Reported )
        {
            this._logger.Trace?.Log( $"The issue {hash} should not be reported because its status is {currentStatus}." );

            return false;
        }

        return this._configurationManager.UpdateIf<TelemetryConfiguration>(
            c =>
            {
                if ( c.Issues.TryGetValue( hash, out var raceStatus ) && raceStatus is ReportingStatus.Ignored or ReportingStatus.Reported )
                {
                    this._logger.Trace?.Log( $"The issue {hash} should not be reported because another process is reporting it." );

                    return false;
                }

                return true;
            },
            c =>
            {
                this._logger.Trace?.Log( $"The issue {hash} should be reported." );

                return c with { Issues = c.Issues.SetItem( hash, ReportingStatus.Reported ) };
            } );
    }

    private static void PopulateStackTraces( List<string?> stackTraces, Exception exception, IExceptionAdapter adapter )
    {
        var stackTrace = adapter.GetStackTrace( exception );

        if ( stackTrace != null )
        {
            stackTraces.Add( stackTrace );
        }

        if ( exception is AggregateException aggregateException )
        {
            foreach ( var child in aggregateException.InnerExceptions )
            {
                PopulateStackTraces( stackTraces, child, adapter );
            }
        }
        else if ( exception.InnerException != null )
        {
            PopulateStackTraces( stackTraces, exception.InnerException, adapter );
        }
    }

    public void ReportException(
        Exception reportedException,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
        string? localReportPath = null,
        IExceptionAdapter? adapter = null )
    {
        try
        {
            if ( !this.ShouldReportException( reportedException ) )
            {
                return;
            }

            this._logger.Trace?.Log( $"Attempting to report an exception of type '{reportedException.GetType().Name}' of kind '{exceptionReportingKind}'." );

            if ( exceptionReportingKind == ExceptionReportingKind.Exception )
            {
                this._logger.Trace?.Log( $"Reporting the exception locally." );
                
                this._localExceptionReporter?.ReportException( reportedException, localReportPath );
            }

            if ( !this._telemetryConfigurationService.IsEnabled )
            {
                this._logger.Trace?.Log( $"The exception will not be reported remotely because the telemetry is disabled." );
                
                return;
            }

            var reportingAction = exceptionReportingKind == ExceptionReportingKind.Exception
                ? this._configuration.ExceptionReportingAction
                : this._configuration.PerformanceProblemReportingAction;

            if ( reportingAction != ReportingAction.Yes )
            {
                this._logger.Trace?.Log( $"The issue will not be reported because the reporting action in the user profile is set to {reportingAction}." );

                return;
            }
            
            this._logger.Trace?.Log( $"Reporting the exception remotely." );

            adapter ??= DefaultExceptionAdapter.Instance;
            var applicationInfo = this._applicationInfoProvider.CurrentApplication;

            // Get stack traces.
            var stackTraces = new List<string?>();
            PopulateStackTraces( stackTraces, reportedException, adapter );

            // Compute a signature for this exception.
            var hash = this.ComputeExceptionHash(
                applicationInfo.PackageVersion,
                adapter.GetTypeFullName( reportedException )!,
                stackTraces );

            // Check if this exception has already been reported.

            if ( !this.ShouldReportIssue( hash ) )
            {
                return;
            }

            // Create the exception report file.
            var directory = this._directories.TelemetryExceptionsDirectory;

            if ( !this._fileSystem.DirectoryExists( directory ) )
            {
                this._fileSystem.CreateDirectory( directory );
            }

            var fileName = Path.Combine( directory, "exception-" + hash + "-" + Guid.NewGuid().ToString() + ".xml" );

            var stringWriter = new StringWriter();

            var xmlWriter = new XmlTextWriter( stringWriter ) { Formatting = Formatting.Indented };
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement( "ErrorReport" );
            xmlWriter.WriteElementString( "InvariantHash", hash );
            xmlWriter.WriteElementString( "Time", XmlConvert.ToString( this._time.UtcNow, XmlDateTimeSerializationMode.RoundtripKind ) );
            xmlWriter.WriteElementString( "ClientId", this._telemetryConfigurationService.DeviceId.ToString() );
            xmlWriter.WriteStartElement( "Application" );
            xmlWriter.WriteElementString( "Name", applicationInfo.Name );
            xmlWriter.WriteElementString( "Version", applicationInfo.PackageVersion );
            xmlWriter.WriteEndElement();

            var currentProcess = Process.GetCurrentProcess();
            xmlWriter.WriteStartElement( "Process" );
            xmlWriter.WriteElementString( "Name", currentProcess.ProcessName );
            xmlWriter.WriteElementString( "ProcessorArchitecture", XmlConvert.ToString( IntPtr.Size * 8 ) );
            xmlWriter.WriteElementString( "SessionId", XmlConvert.ToString( currentProcess.SessionId ) );
            xmlWriter.WriteElementString( "TotalProcessorTime", XmlConvert.ToString( currentProcess.TotalProcessorTime ) );
            xmlWriter.WriteElementString( "WorkingSet", XmlConvert.ToString( currentProcess.WorkingSet64 ) );
            xmlWriter.WriteElementString( "PeakWorkingSet", XmlConvert.ToString( currentProcess.PeakWorkingSet64 ) );
            xmlWriter.WriteElementString( "ManagedHeap", XmlConvert.ToString( GC.GetTotalMemory( false ) ) );
            xmlWriter.WriteEndElement();
            xmlWriter.WriteStartElement( "Environment" );
            xmlWriter.WriteElementString( "OSVersion", Environment.OSVersion.Version.ToString() );
            xmlWriter.WriteElementString( "ProcessorCount", XmlConvert.ToString( Environment.ProcessorCount ) );
            xmlWriter.WriteElementString( "Version", Environment.Version.ToString() );
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement( "Exception" );

            adapter.WriteException( xmlWriter, reportedException );

            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement( "Assemblies" );

            foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
            {
                var assemblyName = assembly.GetName();
                xmlWriter.WriteStartElement( "Assembly" );
                xmlWriter.WriteElementString( "Name", ExceptionSensitiveDataHelper.Instance.RemoveSensitiveData( assemblyName.Name ) );
                xmlWriter.WriteElementString( "Version", assemblyName.Version?.ToString() ?? "<unknown>" );

                try
                {
                    if ( !assembly.IsDynamic && !string.IsNullOrEmpty( assembly.Location ) )
                    {
                        xmlWriter.WriteElementString( "FileVersion", FileVersionInfo.GetVersionInfo( assembly.Location ).FileVersion );
                    }
                }
                catch ( NotSupportedException ) { }

                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
            xmlWriter.Close();

            this._fileSystem.WriteAllText( fileName, stringWriter.ToString() );

            this._uploadManager.EnqueueFile( fileName );
        }
        catch ( Exception e )
        {
            try
            {
                this._logger.Error?.Log( "Cannot report the exception: " + e );
            }
            catch when ( this._canIgnoreRecoverableExceptions ) { }

            if ( !this._canIgnoreRecoverableExceptions )
            {
                throw;
            }
        }
    }

    public void Dispose() => this._configurationManager.ConfigurationFileChanged -= this.OnConfigurationChanged;
}