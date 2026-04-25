// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Tests.UnitTests.Aspects;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Diagnostics;

#pragma warning disable VSTHRD200

public sealed class DiagnosticDescriptionTests : AspectTestBase
{
    [Fact]
    public async Task ExceptionDiagnosticDescriptionContainsExceptionDetails()
    {
        // Arrange: an aspect whose template throws a DivideByZeroException.
        const string code = """
                            using Metalama.Framework.Aspects;

                            class ThrowingAspect : OverrideMethodAspect
                            {
                                public override dynamic? OverrideMethod()
                                {
                                    var a = meta.CompileTime( 0 );
                                    var b = 1 / a;
                                    return meta.Proceed();
                                }
                            }

                            class TargetCode
                            {
                                [ThrowingAspect]
                                int Method( int x ) => x;
                            }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCSharpCompilation( code );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();

        await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        // Find the LAMA0041 diagnostic (ExceptionInUserCode).
        var exceptionDiagnostic = diagnostics.ToImmutableArray().FirstOrDefault( d => d.Id == "LAMA0041" );

        Assert.NotNull( exceptionDiagnostic );

        // The diagnostic Description should contain the exception details so that
        // VS users can see them directly in the Error List panel (issue #1467).
        var description = exceptionDiagnostic.Descriptor.Description.ToString( CultureInfo.InvariantCulture );

        Assert.Contains( "DivideByZeroException", description, StringComparison.Ordinal );
    }

    [Fact]
    public async Task UnhandledExceptionDiagnosticDescriptionContainsExceptionDetails()
    {
        // Arrange: an aspect whose BuildAspect throws an exception.
        const string code = """
                            using Metalama.Framework.Aspects;
                            using Metalama.Framework.Code;
                            using Metalama.Framework.Advising;

                            class ThrowingAspect : TypeAspect
                            {
                                public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                {
                                    throw new System.InvalidOperationException( "Intentional test exception" );
                                }
                            }

                            [ThrowingAspect]
                            class TargetClass { }
                            """;

        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCSharpCompilation( code );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();

        await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        // Find the LAMA0041 diagnostic (ExceptionInUserCode) — BuildAspect exceptions are also reported as LAMA0041.
        var exceptionDiagnostic = diagnostics.ToImmutableArray().FirstOrDefault( d => d.Id is "LAMA0041" or "LAMA0001" );

        Assert.NotNull( exceptionDiagnostic );

        // The diagnostic Description should contain the exception details so that
        // VS users can see them directly in the Error List panel (issue #1467).
        var description = exceptionDiagnostic.Descriptor.Description.ToString( CultureInfo.InvariantCulture );

        Assert.Contains( "InvalidOperationException", description, StringComparison.Ordinal );
    }
}