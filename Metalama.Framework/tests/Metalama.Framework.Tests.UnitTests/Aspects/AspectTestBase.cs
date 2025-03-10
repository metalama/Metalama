// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

public class AspectTestBase : UnitTestClass
{
    protected static async Task<FallibleResult<CompileTimeAspectPipelineResult>> CompileAsync( TestContext testContext, string code, bool throwOnError = true )
    {
        var compilation = testContext.CreateCSharpCompilation( code );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();

        var result = await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        if ( !result.IsSuccessful && throwOnError )
        {
            throw new DiagnosticException( "The Metalama pipeline failed.", diagnostics.ToImmutableArray() );
        }

        return result;
    }

    protected static async Task<FallibleResult<CompileTimeAspectPipelineResult>> CompileAsync(
        TestContext testContext,
        IReadOnlyDictionary<string, string> code,
        bool throwOnError = true )
    {
        var compilation = testContext.CreateCSharpCompilation( code );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();

        var result = await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        if ( !result.IsSuccessful && throwOnError )
        {
            throw new DiagnosticException( "The Metalama pipeline failed.", diagnostics.ToImmutableArray() );
        }

        return result;
    }
}