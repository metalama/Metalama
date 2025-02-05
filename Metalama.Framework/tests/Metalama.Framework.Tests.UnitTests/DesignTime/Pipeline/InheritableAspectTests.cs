// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline
{
    public sealed class InheritableAspectTests : UnitTestClass
    {
        [Fact]
        public void InheritableAspectsMakeItToCompilationResult()
        {
            using var testContext = this.CreateTestContext();

            // Initial compilation.
            const string code1 = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[Inheritable]
public class Aspect : TypeAspect {}

[Aspect]
public interface I {}
";

            var compilation1 = testContext.CreateCompilationModel( code1 );

            using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );
            var pipeline = pipelineFactory.CreatePipeline( compilation1.RoslynCompilation );

            Assert.True( pipeline.TryExecute( compilation1.RoslynCompilation, default, out var compilationResult1 ) );

            Assert.Equal( new[] { "Aspect" }, compilationResult1.Result.InheritableAspectTypes.ToArray() );

            Assert.Equal(
                new[] { "T:I" },
                compilationResult1.Result.GetInheritableAspects( "Aspect" ).Select( i => i.TargetDeclaration.ToSerializableId().Id ).ToArray() );
        }

        [Fact]
        public void IncrementalCompilationWorks()
        {
            using var testContext = this.CreateTestContext();

            const string aspectCode = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[Inheritable]
public class Aspect : TypeAspect { }
";

            // Initial compilation.
            const string targetCode1 = "[Aspect] interface I {}";

            var compilation1 = testContext.CreateCompilationModel( new Dictionary<string, string> { ["aspect.cs"] = aspectCode, ["target.cs"] = targetCode1 } );

            var targetTree1 = compilation1.RoslynCompilation.SyntaxTrees.Single( t => t.FilePath == "target.cs" );

            using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );
            var pipeline = pipelineFactory.CreatePipeline( compilation1.RoslynCompilation );

            Assert.True( pipeline.TryExecute( compilation1.RoslynCompilation, default, out var compilationResult1 ) );

            Assert.Equal(
                new[] { "T:I" },
                compilationResult1.Result.GetInheritableAspects( "Aspect" ).Select( i => i.TargetDeclaration.ToSerializableId().Id ).ToArray() );

            // Add a target class.
            var targetTree2 = CSharpSyntaxTree.ParseText(
                "[Aspect] interface I {} [Aspect] class C {}",
                path: "target.cs",
                options: SupportedCSharpVersions.DefaultParseOptions );

            var compilation2 = testContext.CreateCompilationModel( compilation1.RoslynCompilation.ReplaceSyntaxTree( targetTree1, targetTree2 ) );
            Assert.True( pipeline.TryExecute( compilation2.RoslynCompilation, default, out var compilationResult2 ) );

            Assert.Equal(
                new[] { "T:C", "T:I" },
                compilationResult2.Result.GetInheritableAspects( "Aspect" )
                    .Select( i => i.TargetDeclaration.ToSerializableId().Id )
                    .OrderBy( a => a )
                    .ToArray() );

            // Remove a target
            var targetTree3 = CSharpSyntaxTree.ParseText( "[Aspect] class C {}", path: "target.cs", options: SupportedCSharpVersions.DefaultParseOptions );
            var compilation3 = testContext.CreateCompilationModel( compilation2.RoslynCompilation.ReplaceSyntaxTree( targetTree2, targetTree3 ) );
            Assert.True( pipeline.TryExecute( compilation3.RoslynCompilation, default, out var compilationResult3 ) );

            Assert.Equal(
                new[] { "T:C" },
                compilationResult3.Result.GetInheritableAspects( "Aspect" )
                    .Select( i => i.TargetDeclaration.ToSerializableId().Id )
                    .OrderBy( a => a )
                    .ToArray() );
        }

#if NET5_0_OR_GREATER
        [Fact( Skip = "CLR internal error when unloading the domain" )]
#else
        [Fact]
#endif
        public void CrossProjectIntegration()
        {
            using var testContext = this.CreateTestContext();

            const string code1 = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[Inheritable]
public class Aspect : TypeAspect
{
    [Introduce]
    public void IntroducedMethod() {}
}

[Aspect]
public interface I {}
";

            const string code2 = @"partial class C : I {}";

            using var testContext1 = this.CreateTestContext();

            var compilation1 = testContext.CreateCSharpCompilation( code1 );

            using var testContext2 = this.CreateTestContext();

            var compilation2 = testContext.CreateCSharpCompilation( code2, additionalReferences: new[] { compilation1.ToMetadataReference() } );

            // We have to execute the pipeline on compilation1 first and explicitly because implicit running is not currently possible
            // because of missing project options.

            using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );

            Assert.True( pipelineFactory.TryExecute( testContext1.ProjectOptions, compilation1, default, out _ ) );
            Assert.True( pipelineFactory.TryExecute( testContext2.ProjectOptions, compilation2, default, out var compilationResult2 ) );

            Assert.Single( compilationResult2.Result.IntroducedSyntaxTrees );
        }
    }
}