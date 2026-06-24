// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

// Covers the CompileTimeExceptionHandler: it writes a diagnostic and routes telemetry through the per-project context
// (when it has project options) or the tooling policy (the global fallback used by SourceTransformer's outer layer). See #1701.
public sealed class CompileTimeExceptionHandlerTests : UnitTestClass
{
    [Fact]
    public void ReportException_WithProject_EmitsDiagnosticAndRoutesThroughRepository()
    {
        using var testContext = this.CreateTestContext();
        testContext.ExpectsReportedExceptions = true;

        var projectPath = Path.Combine( Path.GetTempPath(), "repo", "proj.csproj" );
        var serviceProvider = testContext.ServiceProvider.Underlying.WithService( new ProjectOptionsStub( projectPath ), allowOverride: true );

        var handler = new CompileTimeExceptionHandler( serviceProvider );
        var diagnostics = new List<Diagnostic>();
        var exception = new InvalidOperationException( "boom" );

        handler.ReportException( exception, diagnostics.Add, canIgnoreException: false, out var isHandled );

        Assert.True( isHandled );
        var diagnostic = Assert.Single( diagnostics );
        Assert.Equal( GeneralDiagnosticDescriptors.UnhandledException.Id, diagnostic.Id );

        var report = Assert.Single( testContext.ReportedTelemetryExceptions );
        Assert.Same( exception, report.Exception );
        Assert.Equal( TelemetryRouting.Repository, report.Routing );
        Assert.Equal( Path.GetDirectoryName( projectPath ), report.Directory );
    }

    [Fact]
    public void ReportException_Ignorable_EmitsIgnorableDiagnostic()
    {
        using var testContext = this.CreateTestContext();
        testContext.ExpectsReportedExceptions = true;

        var serviceProvider = testContext.ServiceProvider.Underlying.WithService(
            new ProjectOptionsStub( Path.Combine( Path.GetTempPath(), "repo", "proj.csproj" ) ),
            allowOverride: true );

        var handler = new CompileTimeExceptionHandler( serviceProvider );
        var diagnostics = new List<Diagnostic>();

        handler.ReportException( new InvalidOperationException(), diagnostics.Add, canIgnoreException: true, out _ );

        var diagnostic = Assert.Single( diagnostics );
        Assert.Equal( GeneralDiagnosticDescriptors.IgnorableUnhandledException.Id, diagnostic.Id );
    }

    [Fact]
    public void ReportException_WithoutProject_RoutesThroughToolingPolicy()
    {
        // A handler built from the global services (no project options) is the global fallback used by SourceTransformer's
        // outer catch: it reports as a tooling exception.
        using var testContext = this.CreateTestContext();
        testContext.ExpectsReportedExceptions = true;

        var handler = new CompileTimeExceptionHandler( testContext.ServiceProvider.Global.Underlying );
        var diagnostics = new List<Diagnostic>();

        handler.ReportException( new InvalidOperationException(), diagnostics.Add, canIgnoreException: false, out var isHandled );

        Assert.True( isHandled );
        Assert.Single( diagnostics );
        var report = Assert.Single( testContext.ReportedTelemetryExceptions );
        Assert.Equal( TelemetryRouting.Tooling, report.Routing );
    }

    private sealed class ProjectOptionsStub : DefaultProjectOptions
    {
        public ProjectOptionsStub( string? projectPath )
        {
            this.ProjectPath = projectPath;
        }

        public override string? ProjectPath { get; }
    }
}
