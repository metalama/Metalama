// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Testing;
using Metalama.Framework.Tests.UnitTests.TestFramework;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

// End-to-end coverage of the two-layer exception handling in SourceTransformer.Execute, driven through the ITransformerContext
// seam with a fault injected via ITestFaultInjector. The inner (project-scoped) layer must capture repository telemetry; the
// outer (global fallback) layer must capture tooling telemetry. Both layers must write a LAMA0001 diagnostic and swallow the
// exception so the compiler does not crash. See #1701.
public sealed class SourceTransformerExceptionHandlingTests : UnitTestClass
{
    private const string _code = "class C {}";

    [Fact]
    public void Execute_FaultInProjectScope_EmitsDiagnosticAndCapturesRepositoryTelemetry()
    {
        using var testContext = this.CreateTestContext();
        testContext.ExpectsReportedExceptions = true;

        var faultInjector = new TestFaultInjector();
        faultInjector.ArmFault( FaultInjectionPoints.CompileTimePipeline );

        var serviceProvider = testContext.ServiceProvider.Global.WithService( faultInjector );
        var compilation = testContext.CreateCSharpCompilation( _code );
        var projectPath = Path.Combine( Path.GetTempPath(), "repo", "proj.csproj" );

        var context = new TestTransformerContext( serviceProvider, compilation, new ProjectOptionsStub( projectPath ) );

        // Must not throw: the inner handler swallows the fault.
        SourceTransformer.Execute( context );

        var diagnostic = Assert.Single( context.ReportedDiagnostics );
        Assert.Equal( GeneralDiagnosticDescriptors.UnhandledException.Id, diagnostic.Id );

        var report = Assert.Single( testContext.ReportedTelemetryExceptions );
        Assert.Equal( TelemetryRouting.Repository, report.Routing );
        Assert.Equal( Path.GetDirectoryName( projectPath ), report.Directory );
    }

    [Fact]
    public void Execute_FaultBeforeProjectScope_EmitsDiagnosticAndCapturesToolingTelemetry()
    {
        using var testContext = this.CreateTestContext();
        testContext.ExpectsReportedExceptions = true;

        var faultInjector = new TestFaultInjector();
        faultInjector.ArmFault( FaultInjectionPoints.SourceTransformerEntry );

        var serviceProvider = testContext.ServiceProvider.Global.WithService( faultInjector );
        var compilation = testContext.CreateCSharpCompilation( _code );
        var projectPath = Path.Combine( Path.GetTempPath(), "repo", "proj.csproj" );

        var context = new TestTransformerContext( serviceProvider, compilation, new ProjectOptionsStub( projectPath ) );

        // Must not throw: the outer (global) handler swallows the fault.
        SourceTransformer.Execute( context );

        var diagnostic = Assert.Single( context.ReportedDiagnostics );
        Assert.Equal( GeneralDiagnosticDescriptors.UnhandledException.Id, diagnostic.Id );

        // The project scope was never established, so reporting routes through the tooling policy.
        var report = Assert.Single( testContext.ReportedTelemetryExceptions );
        Assert.Equal( TelemetryRouting.Tooling, report.Routing );
    }

    private sealed class TestTransformerContext : ITransformerContext
    {
        private readonly List<Diagnostic> _diagnostics = new();

        public TestTransformerContext( GlobalServiceProvider serviceProvider, Compilation compilation, IProjectOptions projectOptions )
        {
            this.ServiceProvider = serviceProvider;
            this.Compilation = compilation;
            this.ProjectOptions = projectOptions;
        }

        public IReadOnlyList<Diagnostic> ReportedDiagnostics => this._diagnostics;

        public GlobalServiceProvider ServiceProvider { get; }

        public Compilation Compilation { get; }

        public IProjectOptions ProjectOptions { get; }

        public ImmutableArray<ManagedResource> Resources => ImmutableArray<ManagedResource>.Empty;

        public void ReportDiagnostic( Diagnostic diagnostic ) => this._diagnostics.Add( diagnostic );

        // The remaining members are only reached on the success path, which these fault tests never take.
        public void AddSyntaxTreeTransformations( IEnumerable<SyntaxTreeTransformation> transformations )
            => throw new InvalidOperationException( "Not expected on the fault path." );

        public void AddResources( IEnumerable<ManagedResource> resources )
            => throw new InvalidOperationException( "Not expected on the fault path." );

        public void RegisterDiagnosticFilter( in DiagnosticFilter filter )
            => throw new InvalidOperationException( "Not expected on the fault path." );
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
