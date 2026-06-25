// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// Captures an exception report for telemetry. This is the internal seam invoked by <see cref="ITelemetryContext"/> once
/// it has resolved the effective <see cref="ReportingAction"/> from its <see cref="ITelemetryPolicy"/>. Reporting an
/// exception is public only on <see cref="ITelemetryContext.ReportException(System.Exception,ExceptionReportingKind,bool,IExceptionAdapter?)"/>,
/// so that every report passes through the policy; this interface carries the already-resolved action and must not be
/// called directly to bypass the context. See #1701.
/// </summary>
internal interface IExceptionCapturer : IBackstageService
{
    /// <summary>
    /// Captures the <paramref name="classifiedException"/> for the given <paramref name="reportingAction"/> (already
    /// resolved by the context from its policy; never <see cref="ReportingAction.No"/> in practice). When
    /// <paramref name="writeLocalReport"/> is <c>true</c> and this is an exception (not a performance report), the local
    /// crash report is also written; the caller passes <c>false</c> when it has already written its own.
    /// </summary>
    void Capture(
        ClassifiedException classifiedException,
        ExceptionReportingKind exceptionReportingKind,
        ReportingAction reportingAction,
        bool writeLocalReport,
        IExceptionAdapter? adapter );
}
