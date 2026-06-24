// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Telemetry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Testing.UnitTesting;

// A test ITelemetryService that records every reported exception unconditionally (regardless of the telemetry policy),
// so that a test can assert which exceptions the engine reported. Reporting now always flows through ITelemetryContext,
// so we intercept at that seam using only public types — no telemetry is ever captured or sent in tests. See #1701.
internal sealed class TestTelemetryService : ITelemetryService
{
    private readonly TestTelemetryContext _context = new();

    public IReadOnlyCollection<Exception> ReportedExceptions => this._context.ReportedExceptions;

    public ITelemetryPolicy GetPolicy( string? directory ) => TestTelemetryPolicy.Instance;

    public ITelemetryPolicy GetToolingPolicy() => TestTelemetryPolicy.Instance;

    public ITelemetryContext NullContext => this._context;

    public ITelemetryContext OpenContext( ITelemetryPolicy policy ) => this._context;

    private sealed class TestTelemetryPolicy : ITelemetryPolicy
    {
        public static TestTelemetryPolicy Instance { get; } = new();

        public ReportingAction GetReportingAction( TelemetryScenario scenario ) => ReportingAction.No;

        public ImmutableArray<TelemetryContextWarning> Warnings => ImmutableArray<TelemetryContextWarning>.Empty;
    }

    private sealed class TestTelemetryContext : ITelemetryContext
    {
        private readonly ConcurrentBag<Exception> _reportedExceptions = new();

        public IReadOnlyCollection<Exception> ReportedExceptions => this._reportedExceptions;

        public ImmutableArray<TelemetryContextWarning> Warnings => ImmutableArray<TelemetryContextWarning>.Empty;

        public bool IsTelemetryEnabled( TelemetryScenario scenario ) => false;

        public IUsageSession StartUsageSession( string kind, string? projectName = null ) => NullTestUsageSession.Instance;

        public void ReportException(
            Exception exception,
            ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
            bool writeLocalReport = true,
            IExceptionAdapter? exceptionAdapter = null )
            => this._reportedExceptions.Add( exception );

        public void ReportException(
            ClassifiedException classifiedException,
            ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
            bool writeLocalReport = true,
            IExceptionAdapter? exceptionAdapter = null )
            => this._reportedExceptions.Add( classifiedException.Exception );
    }

    private sealed class NullTestUsageSession : IUsageSession
    {
        public static NullTestUsageSession Instance { get; } = new();

        public bool ShouldCollectMetrics => false;

        public MetricCollection Metrics { get; } = new();

        public void Dispose() { }
    }
}
