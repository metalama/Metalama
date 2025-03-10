// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Diagnostics;

public sealed class DiagnosticRegistrationTests : UnitTestClass
{
    [Fact]
    public async Task ExtensionDiagnosticsAreRegistered()
    {
        using var testContext = this.CreateTestContext(
            this.CreateDefaultTestContextOptions() with { ExtensionTypes = ImmutableArray.Create( typeof(TestExtension) ) } );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();
        var compilation = testContext.CreateCSharpCompilation( "" );

        var result = await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        Assert.True( result.IsSuccessful );

        Assert.Single( result.Value.Configuration.DiagnosticManifest.DiagnosticDefinitions.Keys, "TEST01" );
        Assert.Single( result.Value.Configuration.DiagnosticManifest.SuppressionDefinitions.Keys, "TEST01" );
    }

    [Fact]
    public async Task UserCodeDiagnosticsAreRegistered()
    {
        using var testContext = this.CreateTestContext();

        const string code =
            """
            using System;
            using Metalama.Framework.Aspects;
            using Metalama.Framework.Code;
            using Metalama.Framework.Diagnostics;
            using Metalama.Framework.Fabrics;


            [CompileTime]
            internal class SomeType
            {
                private static readonly DiagnosticDefinition<string> _warning1 = new( "MY001", Severity.Warning, "Warning 1: {0}." );
                private static readonly SuppressionDefinition _suppression1 = new( "CS0169" );
            }
            """;

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();
        var compilation = testContext.CreateCSharpCompilation( code );

        var result = await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        Assert.True( result.IsSuccessful );

        Assert.Single( result.Value.Configuration.DiagnosticManifest.DiagnosticDefinitions.Keys, "MY001" );
        Assert.Single( result.Value.Configuration.DiagnosticManifest.SuppressionDefinitions.Keys, "CS0169" );
    }

    [Fact]
    public async Task DuplicateRegistrationsDontCrash()
    {
        using var testContext = this.CreateTestContext();

        const string code =
            """
            using System;
            using Metalama.Framework.Aspects;
            using Metalama.Framework.Code;
            using Metalama.Framework.Diagnostics;
            using Metalama.Framework.Fabrics;


            [CompileTime]
            internal class SomeType
            {
                private static readonly DiagnosticDefinition<string> _warning1 = new( "MY001", Severity.Warning, "Warning 1: {0}." );
                private static readonly DiagnosticDefinition<string> _warning2 = new( "MY001", Severity.Warning, "Warning 1: {0}." );
            }
            """;

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();
        var compilation = testContext.CreateCSharpCompilation( code );

        var result = await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        Assert.True( result.IsSuccessful );

        Assert.Single( result.Value.Configuration.DiagnosticManifest.DiagnosticDefinitions.Keys, "MY001" );
    }

    private sealed class TestExtension : PipelineExtension
    {
        public override bool Initialize( PipelineExtensionInitializationContext context )
        {
            // Assert that the service is present.
            _ = context.ServiceProvider.GetRequiredService<DiagnosticDefinitionDiscoveryService>();
            
            context.AddDiagnosticDefinitions( [new DiagnosticDefinition( "TEST01", Severity.Error, "Test" )] );
            context.AddSuppressionDefinitions( [new SuppressionDefinition( "TEST01" )] );

            return true;
        }
    }
}