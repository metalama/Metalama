// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Telemetry;

[PublicAPI]
public static class TelemetryServiceProviderExtensions
{
    /// <summary>
    /// Reports an exception about the tooling itself (the CLI, the worker, the compiler outer fallback) — telemetry with
    /// no explicit project context, routed through the tooling policy (see <see cref="ITelemetryService.GetToolingPolicy"/>).
    /// A no-op when no <see cref="ITelemetryService"/> is registered. See #1701.
    /// </summary>
    public static void ReportToolingException( this IServiceProvider serviceProvider, Exception exception )
    {
        var telemetryService = serviceProvider.GetBackstageService<ITelemetryService>();
        telemetryService?.OpenContext( telemetryService.GetToolingPolicy() ).ReportException( exception );
    }

    /// <inheritdoc cref="ReportToolingException(System.IServiceProvider,System.Exception)"/>
    public static void ReportToolingException( this IServiceProvider serviceProvider, ClassifiedException classifiedException )
    {
        var telemetryService = serviceProvider.GetBackstageService<ITelemetryService>();
        telemetryService?.OpenContext( telemetryService.GetToolingPolicy() ).ReportException( classifiedException );
    }
}
