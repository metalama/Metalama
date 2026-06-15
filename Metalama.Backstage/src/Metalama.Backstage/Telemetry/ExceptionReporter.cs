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

    // The full, unscrubbed local rendering is stored next to the scrubbed report with this suffix, so the review page
    // can show both side by side. It is local-only and must NEVER be uploaded; the scrubbed '.xml' is the only file
    // ever sent. See #1674.
    private const string _localRenderingSuffix = ".local.xml";

    private static string GetLocalRenderingFileName( string scrubbedReportFileName )
        => Path.GetFileNameWithoutExtension( scrubbedReportFileName ) + _localRenderingSuffix;

    /// <summary>
    /// Validates the bare file name of a scrubbed report, guarding against path traversal and against ever addressing
    /// the full local rendering. <paramref name="reportFileName"/> must be a non-empty bare file name (no directory
    /// component) and must not be the full local rendering ('.local.xml'), which is never uploadable.
    /// </summary>
    private static bool IsValidScrubbedReportFileName( string reportFileName )
    {
        if ( string.IsNullOrEmpty( reportFileName ) || Path.GetFileName( reportFileName ) != reportFileName )
        {
            return false;
        }

        // The full, unscrubbed local rendering must never be read as an upload payload nor enqueued for upload.
        if ( reportFileName.EndsWith( _localRenderingSuffix, StringComparison.OrdinalIgnoreCase ) )
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Locates a scrubbed report by its bare file name, whether it is still awaiting review under
    /// <see cref="IStandardDirectories.TelemetryExceptionsDirectory"/> or has already been moved to
    /// <see cref="IStandardDirectories.TelemetryUploadQueueDirectory"/> (auto-sent, or sent on demand). Returns
    /// <c>false</c> if the name is invalid or the file is not found in either location.
    /// </summary>
    private bool TryResolveReportPath( string reportFileName, [NotNullWhen( true )] out string? fullPath, out bool isQueued )
    {
        fullPath = null;
        isQueued = false;

        if ( !IsValidScrubbedReportFileName( reportFileName ) )
        {
            this._logger.Warning?.Log( $"Rejecting an invalid exception report file name '{reportFileName}'." );

            return false;
        }

        var localPath = Path.Combine( this._directories.TelemetryExceptionsDirectory, reportFileName );

        if ( this._fileSystem.FileExists( localPath ) )
        {
            fullPath = localPath;

            return true;
        }

        var queuedPath = Path.Combine( this._directories.TelemetryUploadQueueDirectory, reportFileName );

        if ( this._fileSystem.FileExists( queuedPath ) )
        {
            fullPath = queuedPath;
            isQueued = true;

            return true;
        }

        return false;
    }

    public bool TryGetReport( string reportFileName, [NotNullWhen( true )] out CapturedExceptionReport? report )
    {
        report = null;

        if ( !this.TryResolveReportPath( reportFileName, out var fullPath, out var isQueued ) )
        {
            return false;
        }

        var scrubbedContent = this._fileSystem.ReadAllText( fullPath );

        // The full local rendering always lives next to the captured report under the exceptions directory (it is never
        // moved to the upload queue). It is absent for reports captured through a custom exception adapter, which cannot
        // be re-rendered unscrubbed.
        string? localContent = null;
        var localRenderingPath = Path.Combine( this._directories.TelemetryExceptionsDirectory, GetLocalRenderingFileName( reportFileName ) );

        if ( this._fileSystem.FileExists( localRenderingPath ) )
        {
            localContent = this._fileSystem.ReadAllText( localRenderingPath );
        }

        // A report found in the upload queue has already been enqueued (auto-sent, or sent on a previous review), so the
        // review page shows it as already sent rather than offering the Report button again. See #1674.
        report = new CapturedExceptionReport( scrubbedContent, localContent, ParseCategory( scrubbedContent ), isQueued );

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
        if ( !this.TryResolveReportPath( reportFileName, out var fullPath, out var isQueued ) )
        {
            this._logger.Warning?.Log( $"Cannot send the exception report '{reportFileName}' because it does not exist." );

            return false;
        }

        if ( isQueued )
        {
            // The report has already been enqueued (auto-sent, or sent on a previous click). Sending again is a no-op
            // success so the review page's Report button stays reliable. See #1674.
            this._logger.Trace?.Log( $"The exception report '{reportFileName}' is already queued for upload." );

            return true;
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
        // purely informational (clicking it still opens the page, which shows what was reported).
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

    // An assembly is safe to disclose (name + version) when its name starts with a known framework / first-party /
    // bundled-OSS prefix; any other assembly is user/third-party code whose name (even a single token), version and
    // file version can identify the user's product or build, so it is redacted to "#user". The prefix list and boundary
    // rule are shared with the namespace scrubber (ExceptionSensitiveDataHelper.KnownSafePrefixes) so the two never
    // diverge. See #1680.
    internal static bool IsKnownSafeAssemblyName( string? name ) => ExceptionSensitiveDataHelper.IsKnownSafePrefix( name );

    internal static void WriteAssemblyElement( XmlWriter xmlWriter, string? name, Version? version, string? fileVersion )
        => WriteAssemblyElement( xmlWriter, name, version, fileVersion, ExceptionSensitiveDataHelper.Instance );

    internal static void WriteAssemblyElement(
        XmlWriter xmlWriter,
        string? name,
        Version? version,
        string? fileVersion,
        ExceptionSensitiveDataHelper scrubber )
    {
        xmlWriter.WriteStartElement( "Assembly" );

        // The full local rendering (scrubber disabled) discloses every assembly so the user can see exactly what is
        // withheld from the upload payload. The upload payload discloses only framework/runtime assemblies; any other
        // assembly is user/third-party code whose name, version and file version can identify the user's product or
        // build, so it is redacted. See #1674, #1680.
        if ( !scrubber.IsEnabled || IsKnownSafeAssemblyName( name ) )
        {
            xmlWriter.WriteElementString( "Name", name );
            xmlWriter.WriteElementString( "Version", version?.ToString() ?? "<unknown>" );

            if ( !string.IsNullOrEmpty( fileVersion ) )
            {
                xmlWriter.WriteElementString( "FileVersion", scrubber.RemoveSensitiveData( fileVersion ) );
            }
        }
        else
        {
            // User / third-party assembly: redact the name, version and file version, which can identify the
            // user's product or build. The scrubber would not catch a single-token name on its own. See #1680.
            xmlWriter.WriteElementString( "Name", "#user" );
        }

        xmlWriter.WriteEndElement();
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

            // Capture is decoupled from sending (#1674). The report is captured locally (and a toast shown) whenever
            // telemetry is globally enabled, regardless of the per-category reporting action — the category only decides
            // whether the report is additionally auto-sent (see below). A process-level opt-out (the application does not
            // support telemetry, the process is unattended, or the opt-out environment variable is set) still suppresses
            // capture entirely.
            if ( !this._telemetryConfigurationService.IsGloballyEnabled )
            {
                this._logger.Trace?.Log( $"The exception will not be captured because telemetry is disabled for this process." );

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

            // Capture the full, unscrubbed rendering of the same report next to it (with a '.local.xml' extension), so
            // the review page can always show both side by side and the user can see exactly what the scrubber removes —
            // whether the report is auto-sent or awaiting review. The '.local.xml' file is never uploaded (it is not
            // enqueued and the upload queue only ever receives the scrubbed '.xml'). We can only re-render unscrubbed
            // through the default adapter; a custom adapter (cross-process exceptions) scrubs internally. See #1674.
            if ( adapter is DefaultExceptionAdapter )
            {
                this._fileSystem.WriteAllText(
                    Path.Combine( directory, GetLocalRenderingFileName( baseName + ".xml" ) ),
                    this.BuildReport( hash, scenario, classifiedException, adapter, ExceptionSensitiveDataHelper.Disabled ) );
            }

            // Capture is decoupled from sending (#1674). The scrubbed report has now been captured locally under the
            // Telemetry\Exceptions directory. We only auto-send it (move it to the upload queue) when the user has
            // explicitly opted the category in (ReportingAction.Yes). Otherwise (review-first: ReportingAction.Default,
            // or an explicit opt-out: ReportingAction.No) the report stays local until the user reviews and sends it
            // from the worker page / CLI.
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
            }

            // Notify the user that a report was captured. Clicking the toast opens the worker review page, which shows
            // the report renderings with a Report button and a per-category auto-report checkbox. We reference the report
            // by the bare file name of its scrubbed payload; the page resolves it whether it is still under
            // Telemetry\Exceptions (review-first) or has already been moved to Telemetry\UploadQueue (auto-sent). See #1674.
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

            string? fileVersion = null;

            try
            {
                if ( !assembly.IsDynamic && !string.IsNullOrEmpty( assembly.Location ) )
                {
                    fileVersion = FileVersionInfo.GetVersionInfo( assembly.Location ).FileVersion;
                }
            }
            catch ( NotSupportedException ) { }

            WriteAssemblyElement( xmlWriter, assemblyName.Name, assemblyName.Version, fileVersion, scrubber );
        }

        xmlWriter.WriteEndElement();
        xmlWriter.Close();

        return stringWriter.ToString();
    }
}