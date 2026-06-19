// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler.Services;
using System;

namespace Metalama.Framework.Engine.Services;

internal sealed class ExceptionReporterAdapter : IExceptionReporter
{
    private readonly Backstage.Telemetry.ITelemetryContext _telemetryContext;

    public ExceptionReporterAdapter( Backstage.Telemetry.ITelemetryContext telemetryContext )
    {
        this._telemetryContext = telemetryContext;
    }

    // Routes engine exceptions through the telemetry context so that the repository-scoped opt-out (metalama.json) is
    // honored: a disabled context writes the local crash report but sends no telemetry. See #1701.
    public void ReportException( Exception reportedException ) => this._telemetryContext.ReportException( reportedException );
}