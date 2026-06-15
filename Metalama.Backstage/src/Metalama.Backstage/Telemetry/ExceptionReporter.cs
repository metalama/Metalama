// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.UserInterface.Toasts;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Metalama.Backstage.Telemetry;

internal sealed class ExceptionReporter : IExceptionReporter
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
    private readonly IServiceProvider _serviceProvider;
    private readonly IToastNotificationService? _toastNotificationService;

    public ExceptionReporter( TelemetryQueue uploadManager, IServiceProvider serviceProvider )
    {
        this._uploadManager = uploadManager;
        this._serviceProvider = serviceProvider;
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
        this._time = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._applicationInfoProvider = serviceProvider.GetRequiredBackstageService<IApplicationInfoProvider>();
        this._directories = serviceProvider.GetRequiredBackstageService<IStandardDirectories>();
        this._logger = serviceProvider.GetLoggerFactory().Telemetry();
        this._fileSystem = serviceProvider.GetRequiredBackstageService<IFileSystem>();
        this._canIgnoreRecoverableExceptions = serviceProvider.GetRequiredBackstageService<IRecoverableExceptionService>().CanIgnore;
        this._localExceptionReporter = serviceProvider.GetBackstageService<LocalExceptionReporter>();
        this._toastNotificationService = serviceProvider.GetBackstageService<IToastNotificationService>();
    }

    /// <summary>
    /// Resolves and validates a bare report file name against the local exceptions directory, guarding against path
    /// traversal: <paramref name="reportFileName"/> must be a non-empty bare file name (no directory component).
    /// </summary>
    private bool TryResolveReportPath( string reportFileName, [NotNullWhen( true )] out string? fullPath )
    {
        fullPath = null;

        if ( string.IsNullOrEmpty( reportFileName ) || Path.GetFileName( reportFileName ) != reportFileName )
        {
            this._logger.Warning?.Log( $"Rejecting an invalid exception report file name '{reportFileName}'." );

            return false;
        }

        fullPath = Path.Combine( this._directories.TelemetryExceptionsDirectory, reportFileName );

        return true;
    }

    // The full, unscrubbed local rendering is stored next to the scrubbed report with a '.local.xml' extension, so the
    // review page can show both side by side. The scrubbed '.xml' is the only file ever uploaded. See #1674.
    private static string GetLocalReportFileName( string scrubbedReportFileName )
        => Path.GetFileNameWithoutExtension( scrubbedReportFileName ) + ".local.xml";

    public bool TryGetReport( string reportFileName, [NotNullWhen( true )] out LocalExceptionReport? report )
    {
        report = null;

        if ( !this.TryResolveReportPath( reportFileName, out var fullPath ) || !this._fileSystem.FileExists( fullPath ) )
        {
            return false;
        }

        var scrubbedContent = this._fileSystem.ReadAllText( fullPath );

        // Read the full local rendering if it exists (it is absent for reports captured through a custom exception
        // adapter, which cannot be re-rendered unscrubbed).
        string? localContent = null;
        var localFullPath = Path.Combine( this._directories.TelemetryExceptionsDirectory, GetLocalReportFileName( reportFileName ) );

        if ( this._fileSystem.FileExists( localFullPath ) )
        {
            localContent = this._fileSystem.ReadAllText( localFullPath );
        }

        report = new LocalExceptionReport( scrubbedContent, localContent, ParseCategory( scrubbedContent ) );

        return true;
    }

    // Reads the self-contained <Category> element from a captured report. Defaults to Exception if absent or unparsable.
    private static TelemetryScenario ParseCategory( string reportContent )
    {
        try
        {
            var category = XDocument.Parse( reportContent ).Root?.Element( "Category" )?.Value;

            if ( !string.IsNullOrEmpty( category ) && Enum.TryParse<TelemetryScenario>( category, out var scenario ) )
            {
                return scenario;
            }
        }
        catch ( XmlException )
        {
            // Fall through to the default.
        }

        return TelemetryScenario.Exception;
    }

    public bool SendReport( string reportFileName )
    {
        if ( !this.TryResolveReportPath( reportFileName, out var fullPath ) || !this._fileSystem.FileExists( fullPath ) )
        {
            this._logger.Warning?.Log( $"Cannot send the exception report '{reportFileName}' because it does not exist." );

            return false;
        }

        this._logger.Trace?.Log( $"Sending the exception report '{reportFileName}' upon user request." );

        this._uploadManager.EnqueueFile( fullPath );

        // Force the upload now: the user explicitly asked to send this report, so we bypass the once-per-day throttling.
        this._serviceProvider.GetBackstageService<ITelemetryUploader>()?.StartUpload( force: true );

        return true;
    }

    private void ShowToastNotification( string reportFileName, TelemetryScenario scenario, string applicationName, bool autoSent )
    {
        if ( this._toastNotificationService == null )
        {
            return;
        }

        var category = scenario == TelemetryScenario.Performance ? "performance problem" : "exception";

        // For a review-first report the call to action is to review and send; for an auto-sent report the toast is
        // purely informational (clicking it still opens the page, which shows the report as already sent).
        var callToAction = autoSent ? "Click to review what was reported." : "Click to review and report it.";

        // The Uri carries only the bare report file name ('exception-<hash>-<guid>.xml'), not a page path, so it
        // stays specifically an exception-report reference rather than an arbitrary URL. It is token-safe (no spaces),
        // so the desktop toast can pass it as a single command argument; the desktop command builds the review-page
        // path itself. The category is stored inside the report, so it is not passed here. See #1674.
        this._toastNotificationService.Show(
            new ToastNotification(
                ToastNotificationKinds.Exception,
                Text: $"The process {applicationName} encountered an unexpected {category}. {callToAction}",
                Uri: reportFileName ) );
    }

    private IEnumerable<string?> CleanStackTrace( string stackTrace )
    {
        foreach ( Match? match in this._stackFrameRegex.Matches( stackTrace ) )
        {
            yield return match?.Value;
        }
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

        return HashUtilities.HashToString( signature.ToString().Normalize() );
    }

    public bool ShouldReportIssue( string hash )
    {
        if ( !this._applicationInfoProvider.CurrentApplication.ShouldCreateLocalCrashReports )
        {
            this._logger.Trace?.Log(
                $"The issue {hash} should not be reported because the errors in the application '{this._applicationInfoProvider.CurrentApplication}' should never be reported." );

            return false;
        }

        if ( this._configurationManager.Get<TelemetryConfiguration>().Issues.TryGetValue( hash, out var currentStatus )
             && currentStatus is ReportingStatus.Ignored or ReportingStatus.Reported )
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
        Exception exception,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
        string? localReportPath = null,
        IExceptionAdapter? adapter = null )
        => this.ReportException( ExceptionClassifier.Classify( exception ), exceptionReportingKind, localReportPath, adapter );

    public void ReportException(
        ClassifiedException classifiedException,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
        string? localReportPath = null,
        IExceptionAdapter? adapter = null )
    {
        try
        {
            if ( !classifiedException.IsError )
            {
                return;
            }

            this._logger.Trace?.Log( $"Attempting to report an exception of type '{classifiedException.GetType().Name}' of kind '{exceptionReportingKind}'." );

            if ( exceptionReportingKind == ExceptionReportingKind.Exception )
            {
                this._logger.Trace?.Log( $"Reporting the exception locally." );

                this._localExceptionReporter?.ReportException( classifiedException.Exception, localReportPath );
            }

            var scenario = exceptionReportingKind == ExceptionReportingKind.Exception ? TelemetryScenario.Exception : TelemetryScenario.Performance;

            if ( !this._telemetryConfigurationService.IsEnabled( scenario ) )
            {
                this._logger.Trace?.Log( $"The exception will not be captured because the telemetry is disabled." );

                return;
            }

            this._logger.Trace?.Log( $"Capturing the exception report." );

            adapter ??= DefaultExceptionAdapter.Instance;
            var applicationInfo = this._applicationInfoProvider.CurrentApplication;

            // Get stack traces.
            var stackTraces = new List<string?>();
            PopulateStackTraces( stackTraces, classifiedException.Exception, adapter );

            // Compute a signature for this exception.
            var hash = this.ComputeExceptionHash(
                applicationInfo.PackageVersion,
                adapter.GetTypeFullName( classifiedException.Exception )!,
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

            var baseName = "exception-" + hash + "-" + Guid.NewGuid();
            var fileName = Path.Combine( directory, baseName + ".xml" );

            // The scrubbed report is the exact payload that would be uploaded.
            this._fileSystem.WriteAllText( fileName, this.BuildReport( hash, scenario, classifiedException, adapter, ExceptionSensitiveDataHelper.Instance ) );

            // Capture is decoupled from sending (#1674). The scrubbed report has now been captured locally under the
            // Telemetry\Exceptions directory. We only auto-send it (move it to the upload queue) when the user has
            // explicitly opted the category in (ReportingAction.Yes). For a review-first category (ReportingAction.Default)
            // the report stays local until the user reviews and sends it from the worker page / CLI.
            var reportingAction = this._configurationManager.Get<TelemetryConfiguration>().GetReportingAction( scenario );

            if ( reportingAction == ReportingAction.Yes )
            {
                this._logger.Trace?.Log( $"Auto-sending the exception report because the category is set to '{ReportingAction.Yes}'." );

                this._uploadManager.EnqueueFile( fileName );
            }
            else
            {
                this._logger.Trace?.Log(
                    $"The exception report was captured locally for review but not enqueued for upload because the category is review-first ('{reportingAction}')." );

                // Capture the full, unscrubbed rendering of the same report next to it (with a '.local.xml' extension),
                // so the review page can show both side by side and the user can see exactly what the scrubber removes
                // before anything leaves the machine. The '.local.xml' file is never uploaded. We only need it for the
                // review-first flow (an auto-sent report is uploaded immediately, with nothing to review). We can only
                // re-render unscrubbed through the default adapter; a custom adapter (cross-process exceptions) scrubs
                // internally. See #1674.
                if ( adapter is DefaultExceptionAdapter )
                {
                    this._fileSystem.WriteAllText(
                        Path.Combine( directory, GetLocalReportFileName( baseName + ".xml" ) ),
                        this.BuildReport( hash, scenario, classifiedException, adapter, ExceptionSensitiveDataHelper.Disabled ) );
                }
            }

            // Notify the user that a report was captured. Clicking the toast opens the worker review page, which shows
            // the exact scrubbed payload with a Report button and a per-category auto-report checkbox. The page reads
            // the report from the local exceptions directory, so we reference it by its bare file name. For an
            // auto-sent (Yes) report the file has been moved to the upload queue; the page then reports it as already
            // sent. See #1674.
            this.ShowToastNotification( Path.GetFileName( fileName ), scenario, applicationInfo.Name, autoSent: reportingAction == ReportingAction.Yes );
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

    // Renders the error report to an XML string. The same report is rendered twice: once with the real scrubber (the
    // upload payload) and once with ExceptionSensitiveDataHelper.Disabled (the full local rendering), so the review page
    // can show both side by side. The category is written into the report so it is self-contained. See #1674.
    private string BuildReport(
        string hash,
        TelemetryScenario scenario,
        ClassifiedException classifiedException,
        IExceptionAdapter adapter,
        ExceptionSensitiveDataHelper scrubber )
    {
        var applicationInfo = this._applicationInfoProvider.CurrentApplication;

        var stringWriter = new StringWriter();

        var xmlWriter = new XmlTextWriter( stringWriter ) { Formatting = Formatting.Indented };
        xmlWriter.WriteStartDocument();
        xmlWriter.WriteStartElement( "ErrorReport" );
        xmlWriter.WriteElementString( "InvariantHash", hash );

        // Store the category in the report itself so it is self-contained: the toast, command and review page do not
        // need to pass it as a parameter. See #1674.
        xmlWriter.WriteElementString( "Category", scenario.ToString() );
        xmlWriter.WriteElementString( "Time", XmlConvert.ToString( this._time.UtcNow, XmlDateTimeSerializationMode.RoundtripKind ) );

        // Emit an anonymized device hash keyed by the first-party-only ExceptionReportingSalt, never the raw
        // DeviceId GUID (the rotation seed from which the Matomo hash is derivable). The dedicated salt keeps
        // this exception-reporting pseudonym unjoinable to both the Matomo and the usage-tracking data. See #1668.
        var exceptionReportingDeviceHash = HashUtilities.ComputeInt64Hmac(
            this._telemetryConfigurationService.DeviceId.ToString(),
            this._telemetryConfigurationService.ExceptionReportingSalt );

        xmlWriter.WriteElementString( "ClientId", exceptionReportingDeviceHash.ToString( "x", CultureInfo.InvariantCulture ) );
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

        // The default adapter can render the exception with any scrubber, so the full local rendering is genuinely
        // unscrubbed. A custom adapter (cross-process exceptions) scrubs internally, so we fall back to it.
        if ( adapter is DefaultExceptionAdapter )
        {
            ExceptionXmlFormatter.WriteException( xmlWriter, classifiedException.Exception, scrubber );
        }
        else
        {
            adapter.WriteException( xmlWriter, classifiedException.Exception );
        }

        xmlWriter.WriteEndElement();

        // Include the call stack at the point where the exception is being reported.
        // This is essential for async exceptions where the exception's own StackTrace
        // only shows the throw-to-catch chain, but not the broader context of the entry
        // point that caught the exception (e.g., CodeLens, Preview, AspectExplorer).
        try
        {
            var reportingCallStack =
                Environment.NewLine
                + scrubber.RemoveSensitiveData( Environment.StackTrace )
                + Environment.NewLine;

            xmlWriter.WriteElementString( "ReportingCallStack", reportingCallStack );
        }
        catch
        {
            // Best-effort: ignore failures when capturing or sanitizing the reporting call stack
            // so that exception reporting remains resilient.
        }

        xmlWriter.WriteStartElement( "Assemblies" );

        foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
        {
            var assemblyName = assembly.GetName();
            xmlWriter.WriteStartElement( "Assembly" );
            xmlWriter.WriteElementString( "Name", scrubber.RemoveSensitiveData( assemblyName.Name ) );
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

        return stringWriter.ToString();
    }
}