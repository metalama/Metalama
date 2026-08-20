// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Framework.Tests.UnitTestHelpers.TestClasses;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Tests that an aspect source that returns an aspect instance whose target declaration belongs to another
/// compilation does not crash the design-time pipeline.
/// </summary>
/// <remarks>
/// Regression test for https://github.com/metalama/Metalama/issues/1856. The design-time pipeline reuses the
/// pipeline configuration across the versions of the project, so an aspect source that belongs to the
/// configuration outlives the compilation in which it was created. When such a source keeps a declaration of an
/// earlier version and returns an aspect instance for it, resolving that instance in the current compilation
/// fails, because the symbol of a deleted declaration cannot be translated.
/// </remarks>
public sealed class AspectSourceOutsidePartialCompilationTests : DesignTimePipelineTestsBase
{
    public AspectSourceOutsidePartialCompilationTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void FabricKeepsDeclarationOfPreviousCompilationVersion()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>
        {
            ["aspect.cs"] =
                """
                using System;
                using Metalama.Framework.Aspects;

                public class LogAspect : OverrideMethodAspect
                {
                    public override dynamic? OverrideMethod()
                    {
                        Console.WriteLine("logged");

                        return meta.Proceed();
                    }
                }
                """,

            // The fabric stores the target type in a field of the fabric. The fabric belongs to the pipeline
            // configuration, which the design-time pipeline reuses across the versions of the project, so the
            // field keeps a declaration of the version in which the query first ran.
            ["fabric.cs"] =
                """
                using System.Linq;
                using Metalama.Framework.Aspects;
                using Metalama.Framework.Code;
                using Metalama.Framework.Fabrics;

                internal class Fabric : ProjectFabric
                {
                    private INamedType? _capturedType;

                    public override void AmendProject( IProjectAmender amender )
                    {
                        amender.SelectMany( c => c.Types )
                            .Where( t => t.Name == "OtherClass" )
                            .Select( t => this._capturedType ??= t.Compilation.Types.Single( x => x.Name == "TargetClass" ) )
                            .SelectMany( t => t.Methods )
                            .AddAspect<LogAspect>();
                    }
                }
                """,
            ["target.cs"] = "public class TargetClass { public void Method1() { } }",
            ["other.cs"] = "public class OtherClass { public void DoWork() { } }"
        };

        var compilation = testContext.CreateCSharpCompilation( code );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out _ ) );

        // Remove the type the fabric captured and modify the file that drives the query, so that the query runs
        // again and returns the captured declaration, which no longer exists in the new compilation.
        var compilation2 = ReplaceFile( compilation, "target.cs", "public class SomethingElse { public void Method1() { } }" );
        compilation2 = ReplaceFile( compilation2, "other.cs", "public class OtherClass { public void DoWork() { } public int More() => 42; }" );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation2, default, out _ ) );
    }

    [Fact]
    public void OnlyTheUnresolvedAspectInstanceIsSkipped()
    {
        using var testContext = this.CreateTestContext();

        var code = new Dictionary<string, string>
        {
            ["aspect.cs"] =
                """
                using Metalama.Framework.Aspects;

                public class MyAspect : TypeAspect
                {
                    [Introduce]
                    public void IntroducedMethod() { }
                }
                """,

            // The query returns both the type it selected in the current compilation and the type the fabric
            // captured in the compilation of the first run.
            ["fabric.cs"] =
                """
                using System.Linq;
                using Metalama.Framework.Aspects;
                using Metalama.Framework.Code;
                using Metalama.Framework.Fabrics;

                internal class Fabric : ProjectFabric
                {
                    private INamedType? _capturedType;

                    public override void AmendProject( IProjectAmender amender )
                    {
                        amender.SelectMany( c => c.Types )
                            .Where( t => t.Name == "OtherClass" )
                            .SelectMany(
                                t =>
                                {
                                    this._capturedType ??= t.Compilation.Types.Single( x => x.Name == "TargetClass" );

                                    return new[] { t, this._capturedType };
                                } )
                            .AddAspect<MyAspect>();
                    }
                }
                """,
            ["target.cs"] = "public partial class TargetClass { }",
            ["other.cs"] = "public partial class OtherClass { }"
        };

        var compilation = testContext.CreateCSharpCompilation( code );

        using TestDesignTimeAspectPipelineFactory factory = new( testContext );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var results1 ) );
        Assert.Contains( "IntroducedMethod", DumpResults( results1 ), System.StringComparison.Ordinal );

        var compilation2 = ReplaceFile( compilation, "target.cs", "public partial class SomethingElse { }" );
        compilation2 = ReplaceFile( compilation2, "other.cs", "public partial class OtherClass { public int More() => 42; }" );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation2, default, out var results2 ) );

        // The aspect instance of the captured type is skipped, but the aspect instance of the type that resolves
        // is still applied.
        Assert.Contains( "IntroducedMethod", DumpResults( results2 ), System.StringComparison.Ordinal );
    }

    private static CSharpCompilation ReplaceFile( CSharpCompilation compilation, string path, string newCode )
    {
        var originalTree = compilation.SyntaxTrees.Single( t => t.FilePath == path );
        var parseOptions = (CSharpParseOptions) originalTree.Options;
        var newTree = CSharpSyntaxTree.ParseText( newCode, parseOptions, path: path );

        return compilation.ReplaceSyntaxTree( originalTree, newTree );
    }
}
