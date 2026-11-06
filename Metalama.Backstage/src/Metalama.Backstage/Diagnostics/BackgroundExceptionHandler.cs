// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using System;

namespace Metalama.Backstage.Diagnostics;

/// <summary>
/// Reports an exception caught at the boundary of a thread, a timer callback or an event raised by the operating system,
/// where letting it escape would terminate the process. See https://github.com/metalama/Metalama/issues/2317.
/// </summary>
[PublicAPI]
public static class BackgroundExceptionHandler
{
    /// <summary>
    /// Logs <paramref name="exception"/> and, when it is an error and the backstage services are initialized, writes a
    /// local crash report. Never throws.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="logger">The logger, or <see langword="null"/>.</param>
    /// <param name="caption">A description of the boundary, used in the log.</param>
    public static void Report( Exception exception, ILogger? logger, string caption )
    {
        try
        {
            var classifiedException = ExceptionClassifier.Classify( exception );
            logger.LogException( classifiedException, caption );

            if ( classifiedException.IsError && BackstageServiceFactory.IsInitialized )
            {
                // There is no project context here, so the default policy is used: the local report is written, but no
                // telemetry is sent.
                var telemetryService = BackstageServiceFactory.ServiceProvider.GetBackstageService<ITelemetryService>();
                telemetryService?.OpenContext( telemetryService.GetPolicy( null ) ).ReportException( classifiedException );
            }
        }
        catch ( Exception reportingException )
        {
            // Reporting is best effort. This method runs where an exception would terminate the process.
            System.Diagnostics.Trace.TraceError( $"Metalama: cannot report an exception ({caption}): {reportingException}" );
        }
    }
}
