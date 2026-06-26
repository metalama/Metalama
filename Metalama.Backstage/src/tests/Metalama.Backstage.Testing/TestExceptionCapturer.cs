// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Telemetry;

namespace Metalama.Backstage.Testing;

public class TestExceptionCapturer : IExceptionCapturer
{
    public void Capture(
        ClassifiedException classifiedException,
        ExceptionReportingKind exceptionReportingKind,
        TelemetryConsent telemetryConsent,
        bool writeLocalReport,
        IExceptionAdapter? adapter ) { }
}