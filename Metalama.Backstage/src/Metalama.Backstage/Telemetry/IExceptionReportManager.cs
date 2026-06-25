// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// Retrieves and sends locally-captured exception reports. Capturing an exception is not part of this interface: it
/// happens through <see cref="ITelemetryContext.ReportException(System.Exception,ExceptionReportingKind,bool,IExceptionAdapter?)"/>
/// (internally <see cref="IExceptionCapturer"/>), so that capture always passes through the telemetry policy. This
/// interface manages the already-captured reports — it backs the worker review page and the CLI. See #1674, #1701.
/// </summary>
public interface IExceptionReportManager : IBackstageService
{
    /// <summary>
    /// Gets a locally-captured exception report so that it can be reviewed before sending, including both the exact
    /// scrubbed payload that would be uploaded and the full unscrubbed local rendering, plus the report category.
    /// <paramref name="reportFileName"/> is the bare file name (no directory component) of the scrubbed report, which is
    /// resolved whether it is still awaiting review or has already been moved to the upload queue (auto-sent or sent on
    /// demand). The full local rendering (<c>.local.xml</c>) is never a valid argument because it must never be uploaded.
    /// Returns <c>false</c> if the name is invalid or the report does not exist. See #1674.
    /// </summary>
    bool TryGetReport( string reportFileName, [NotNullWhen( true )] out CapturedExceptionReport? report );

    /// <summary>
    /// Sends a single locally-captured exception report identified by the bare file name of its scrubbed payload: the
    /// file is enqueued for upload and an upload is started. The operation is idempotent — a report that is already in
    /// the upload queue returns <c>true</c> without being re-enqueued. Returns <c>false</c> only if the name is invalid
    /// (including the full <c>.local.xml</c> rendering, which must never be uploaded) or the report does not exist. See #1674.
    /// </summary>
    bool SendReport( string reportFileName );
}