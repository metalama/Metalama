// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
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
/// decline to run instead of failing while it resolves a well-known type of the core library. See issue #1832.
/// </remarks>
public sealed class UnresolvedSdkTests : UnitTestClass
{
    public UnresolvedSdkTests( ITestOutputHelper logger ) : base( logger ) { }

    private const string _code = "public class C {}";

    /// <summary>
    /// Creates a compilation that references the Metalama assemblies but no assembly of the .NET SDK.
    /// </summary>
    private static CSharpCompilation CreateCompilationWithoutCoreLibrary( TestContext testContext )
    {
        var references = testContext.GetMetadataReferences()
            .Where( r => Path.GetFileNameWithoutExtension( r.FilePath )!.StartsWith( "Metalama", StringComparison.OrdinalIgnoreCase ) )
            .ToArray();

        return testContext.CreateEmptyCSharpCompilation( "MetalamaCurrent", references )
            .AddSyntaxTrees( CSharpSyntaxTree.ParseText( _code, testContext.GetCompilationParseOptions(), "Class1.cs" ) );
    }

    [Fact]
    public void PipelineDoesNotCrashWhenCoreLibraryIsNotReferenced()
    {
        using var testContext = this.CreateTestContext();

        var compilation = CreateCompilationWithoutCoreLibrary( testContext );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        // The pipeline is allowed to fail on such a compilation, but it must fail gracefully instead of throwing.
        Assert.False( pipelineFactory.TryExecute( testContext.ProjectOptions, compilation, default, out _, out var diagnostics ) );

        // No diagnostic is reported, because the C# compiler already reports the missing core library itself.
        Assert.Empty( diagnostics );
    }

    [Fact]
    public void PipelineExecutesWhenCoreLibraryIsReferenced()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCSharpCompilation( new Dictionary<string, string> { ["Class1.cs"] = _code } );

        using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

        // The guard against the missing core library must not affect a compilation that has one.
        Assert.True( pipelineFactory.TryExecute( testContext.ProjectOptions, compilation, default, out _ ) );
    }
}
