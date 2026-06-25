// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Telemetry;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine.Options;
using Metalama.Testing.UnitTesting;
using System;
using System.IO;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime;

// Covers the routing that DesignTimeExceptionHandler chooses when reporting through the telemetry capture path: a known
// project routes through its repository context (honoring metalama.json), an unknown project disables telemetry, and an
// OperationCanceledException must never be reported (it must flow to the caller, else VS caches incomplete results). See #1701.
public sealed class DesignTimeExceptionHandlerTests : UnitTestClass
{
    private static DesignTimeExceptionHandler GetHandler( TestContext testContext )
        => testContext.ServiceProvider.Global.GetRequiredService<DesignTimeExceptionHandler>();

    [Fact]
    public void ReportException_WithProject_RoutesThroughRepositoryContext()
    {
        using var testContext = this.CreateTestContext();
        testContext.ExpectsReportedExceptions = true;
        var handler = GetHandler( testContext );

        var projectPath = Path.Combine( Path.GetTempPath(), "repo", "src", "MyProject.csproj" );
        var exception = new InvalidOperationException( "boom" );

        handler.ReportException( exception, new ProjectOptionsStub( projectPath ) );

        var report = Assert.Single( testContext.ReportedTelemetryExceptions );
        Assert.Same( exception, report.Exception );
        Assert.Equal( TelemetryScenario.Exception, report.Scenario );
        Assert.Equal( TelemetryRouting.Repository, report.Routing );
        Assert.Equal( Path.GetDirectoryName( projectPath ), report.Directory );
    }

    [Fact]
    public void ReportException_WithoutProject_DisablesTelemetry()
    {
        using var testContext = this.CreateTestContext();
        testContext.ExpectsReportedExceptions = true;
        var handler = GetHandler( testContext );

        handler.ReportException( new InvalidOperationException() );

        var report = Assert.Single( testContext.ReportedTelemetryExceptions );
        Assert.Equal( TelemetryRouting.Disabled, report.Routing );
        Assert.Null( report.Directory );
    }

    [Fact]
    public void ReportException_ProjectWithoutPath_DisablesTelemetry()
    {
        using var testContext = this.CreateTestContext();
        testContext.ExpectsReportedExceptions = true;
        var handler = GetHandler( testContext );

        handler.ReportException( new InvalidOperationException(), new ProjectOptionsStub( null ) );

        var report = Assert.Single( testContext.ReportedTelemetryExceptions );
        Assert.Equal( TelemetryRouting.Disabled, report.Routing );
    }

    [Fact]
    public void ReportException_OperationCanceledException_IsNotReported()
    {
        using var testContext = this.CreateTestContext();
        var handler = GetHandler( testContext );

        // OperationCanceledException is not an error: it must never be captured (and must flow to the caller). The default
        // end-of-test safety net also confirms nothing was reported.
        handler.ReportException( new OperationCanceledException(), new ProjectOptionsStub( @"C:\repo\proj.csproj" ) );

        Assert.Empty( testContext.ReportedTelemetryExceptions );
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
