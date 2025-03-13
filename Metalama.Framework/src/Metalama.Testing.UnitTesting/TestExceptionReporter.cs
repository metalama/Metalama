// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Telemetry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Metalama.Testing.UnitTesting;

internal sealed class TestExceptionReporter : IExceptionReporter
{
    private readonly ConcurrentBag<Exception> _reportedExceptions = new();

    public IReadOnlyCollection<Exception> ReportedExceptions => this._reportedExceptions;

    public void ReportException(
        Exception reportedException,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
        string? localReportPath = null,
        IExceptionAdapter? exceptionAdapter = null )
    {
        this._reportedExceptions.Add( reportedException );
    }
}