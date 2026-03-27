// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

public sealed class EmptyProjectTests : UnitTestClass
{
    [Fact]
    public async Task NoSourceFiles_ShouldNotReportLAMA0053()
    {
        using var testContext = this.CreateTestContext();

        // Create a compilation with no syntax trees (simulates a project with no source files).
        var compilation = testContext.CreateEmptyCSharpCompilation( "EmptyProject" );

        Assert.Empty( compilation.SyntaxTrees );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();

        await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        // LAMA0053 should not be reported for a project with no source files.
        var lama0053Diagnostics = diagnostics.ToImmutableArray().Where( d => d.Id == "LAMA0053" );

        Assert.Empty( lama0053Diagnostics );
    }
}
