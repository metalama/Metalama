// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Tests the behavior of the design-time pipeline when the compilation has no reference to the core library.
/// </summary>
/// <remarks>
/// This is the state of a project whose .NET SDK cannot be resolved: the references contributed by NuGet packages are
/// still present, but the ones contributed by the SDK, including the core library, are missing. The pipeline must then
/// decline to run instead of failing while it resolves a well-known type of the core library.
/// </remarks>
public sealed class UnresolvedSdkTests : UnitTestClass
{
    public UnresolvedSdkTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Creates a compilation that references the Metalama assemblies but no assembly of the .NET SDK.
    /// </summary>
    private static CSharpCompilation CreateCompilationWithoutCoreLibrary( TestContext testContext )
    {
        var references = testContext.GetMetadataReferences()
            .Where( r => Path.GetFileNameWithoutExtension( r.FilePath )!.StartsWith( "Metalama", StringComparison.OrdinalIgnoreCase ) )
            .ToArray();

        return testContext.CreateEmptyCSharpCompilation( "MetalamaCurrent", references )
            .AddSyntaxTrees( CSharpSyntaxTree.ParseText( "public class C {}", testContext.GetCompilationParseOptions(), "Class1.cs" ) );
    }

    [Fact]
    public void PipelineDoesNotCrashWhenCoreLibraryIsNotReferenced()
    {
        using var testContext = this.CreateTestContext();

        var compilation = CreateCompilationWithoutCoreLibrary( testContext );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );
        var pipeline = pipelineFactory.CreatePipeline( compilation );

        // The pipeline is allowed to fail on such a compilation, but it must fail gracefully instead of throwing.
        Assert.False( pipeline.TryExecute( compilation, default, out _ ) );
    }
}
