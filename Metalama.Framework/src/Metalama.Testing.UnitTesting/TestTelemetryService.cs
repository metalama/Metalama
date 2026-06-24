// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Telemetry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Testing.UnitTesting;

/// <summary>
/// How an exception was routed to telemetry, as recorded by <see cref="TestTelemetryService"/>: through a repository
/// context (a project directory was known), through the tooling policy (no project context), or disabled (no directory).
/// </summary>
[PublicAPI]
public enum TelemetryRouting
{
    Repository,
    Tooling,
    Disabled
}

/// <summary>
/// An exception reported through the test telemetry capture path, with the routing the reporter chose.
/// </summary>
[PublicAPI]
public sealed record ReportedTelemetryException( Exception Exception, TelemetryScenario Scenario, TelemetryRouting Routing, string? Directory );

// A test ITelemetryService that records every exception reported through ITelemetryContext, together with the routing
// the reporter chose (per-project directory / tooling / disabled). Reporting always flows through ITelemetryContext, so
// we intercept at that seam using only public types — no telemetry is ever captured or sent, and no filesystem is
// touched. The routing is captured by having GetPolicy/GetToolingPolicy return marker policies that OpenContext threads
// into the recording context. See #1701.
internal sealed class TestTelemetryService : ITelemetryService
{
    private readonly ConcurrentBag<ReportedTelemetryException> _reported = new();

    public IReadOnlyCollection<ReportedTelemetryException> ReportedExceptions => this._reported;

    // Mirrors the real service: an unknown directory disables telemetry, a known directory routes to its repository.
    public ITelemetryPolicy GetPolicy( string? directory )
        => string.IsNullOrEmpty( directory )
            ? new TestTelemetryPolicy( TelemetryRouting.Disabled, null )
            : new TestTelemetryPolicy( TelemetryRouting.Repository, directory );

    public ITelemetryPolicy GetToolingPolicy() => new TestTelemetryPolicy( TelemetryRouting.Tooling, null );

    public ITelemetryContext NullContext => new TestTelemetryContext( this._reported, new TestTelemetryPolicy( TelemetryRouting.Disabled, null ) );

    public ITelemetryContext OpenContext( ITelemetryPolicy policy )
        => new TestTelemetryContext( this._reported, policy as TestTelemetryPolicy ?? new TestTelemetryPolicy( TelemetryRouting.Disabled, null ) );

    private sealed record TestTelemetryPolicy( TelemetryRouting Routing, string? Directory ) : ITelemetryPolicy
    {
        public ReportingAction GetReportingAction( TelemetryScenario scenario ) => ReportingAction.No;

        public ImmutableArray<TelemetryContextWarning> Warnings => ImmutableArray<TelemetryContextWarning>.Empty;
    }

    private sealed class TestTelemetryContext : ITelemetryContext
    {
        private readonly ConcurrentBag<ReportedTelemetryException> _reported;
        private readonly TestTelemetryPolicy _policy;

        public TestTelemetryContext( ConcurrentBag<ReportedTelemetryException> reported, TestTelemetryPolicy policy )
        {
            this._reported = reported;
            this._policy = policy;
        }

        public ImmutableArray<TelemetryContextWarning> Warnings => ImmutableArray<TelemetryContextWarning>.Empty;

        public bool IsTelemetryEnabled( TelemetryScenario scenario ) => false;

        public IUsageSession StartUsageSession( string kind, string? projectName = null ) => NullTestUsageSession.Instance;

        public void ReportException(
            Exception exception,
            ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
            bool writeLocalReport = true,
            IExceptionAdapter? exceptionAdapter = null )
            => this.Record( exception, exceptionReportingKind );

        public void ReportException(
            ClassifiedException classifiedException,
            ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
            bool writeLocalReport = true,
            IExceptionAdapter? exceptionAdapter = null )
            => this.Record( classifiedException.Exception, exceptionReportingKind );

        private void Record( Exception exception, ExceptionReportingKind kind )
        {
            var scenario = kind == ExceptionReportingKind.Exception ? TelemetryScenario.Exception : TelemetryScenario.Performance;
            this._reported.Add( new ReportedTelemetryException( exception, scenario, this._policy.Routing, this._policy.Directory ) );
        }
    }

    private sealed class NullTestUsageSession : IUsageSession
    {
        public static NullTestUsageSession Instance { get; } = new();

        public bool ShouldCollectMetrics => false;

        public MetricCollection Metrics { get; } = new();

        public void Dispose() { }
    }
}
