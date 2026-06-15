// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Telemetry;

public interface IExceptionReporter : IBackstageService
{
    void ReportException(
        Exception exception,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
        string? localReportPath = null,
        IExceptionAdapter? exceptionAdapter = null );

    void ReportException(
        ClassifiedException classifiedException,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
        string? localReportPath = null,
        IExceptionAdapter? exceptionAdapter = null );

    /// <summary>
    /// Gets the content of a locally-captured exception report (the exact scrubbed payload that would be uploaded), so
    /// that it can be reviewed before sending. <paramref name="reportFileName"/> is a bare file name (no directory
    /// component) of a file in the local exceptions directory. Returns <c>null</c> if the name is invalid or the file
    /// does not exist (e.g. it was already sent). See #1674.
    /// </summary>
    string? TryGetReportContent( string reportFileName );

    /// <summary>
    /// Sends a single locally-captured exception report identified by its bare <paramref name="reportFileName"/>: the
    /// file is enqueued for upload and an upload is started. Returns <c>false</c> if the name is invalid or the file
    /// does not exist. See #1674.
    /// </summary>
    bool SendReport( string reportFileName );
}