// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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