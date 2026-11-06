// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using System;

namespace Metalama.Backstage.Diagnostics;

/// <summary>
/// The implementation of <see cref="IBackgroundExceptionReporter"/>.
/// </summary>
internal sealed class BackgroundExceptionReporter : IBackgroundExceptionReporter
{
    /// <summary>
    /// Set while the current thread reports, so that an exception thrown by the reporting itself, for instance by the file
    /// system while the report is written, is logged instead of being reported again.
    /// </summary>
    [ThreadStatic]
    private static bool _isReporting;

    private readonly IServiceProvider _serviceProvider;

    // Resolved on the first report, not in the constructor: the logger factory depends on services that depend on this one.
    private ILogger? _fallbackLogger;

    // The constructor must not resolve any service: see the remarks of IBackgroundExceptionReporter.
    public BackgroundExceptionReporter( IServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
    }

    public void Report( Exception exception, ILogger? logger, string boundary )
    {
        try
        {
            var classifiedException = ExceptionClassifier.Classify( exception );

            // A caller at the bottom of the dependency graph has no logger of its own. Without one, an exception that is not
            // an error, such as an IOException, would leave no trace at all.
            logger ??= this._fallbackLogger ??= this._serviceProvider.GetLoggerFactory().GetLogger( "BackgroundExceptionReporter" );
            logger.LogException( classifiedException, boundary );

            if ( !classifiedException.IsError || _isReporting )
            {
                return;
            }

            _isReporting = true;

            try
            {
                // There is no project context here, so the default policy is used.
                var telemetryService = this._serviceProvider.GetBackstageService<ITelemetryService>();
                telemetryService?.OpenContext( telemetryService.GetPolicy( null ) ).ReportException( classifiedException );
            }
            finally
            {
                _isReporting = false;
            }
        }
        catch ( Exception reportingException )
        {
            // Reporting is best effort. This method runs where an exception would terminate the process.
            System.Diagnostics.Trace.TraceError( $"Metalama: cannot report an exception ({boundary}): {reportingException}" );
        }
    }
}
