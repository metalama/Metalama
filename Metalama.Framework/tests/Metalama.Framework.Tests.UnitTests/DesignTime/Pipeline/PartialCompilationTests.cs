// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Services;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable VSTHRD200 // Warning VSTHRD200 : Use "Async" suffix in names of methods that return an awaitable type.

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline
{
    public sealed class PartialCompilationTests : UnitTestClass
    {
        private readonly ITestOutputHelper _logger;

        public PartialCompilationTests( ITestOutputHelper logger )
        {
            this._logger = logger;
        }

        [Fact]
        public void Bug28733()
        {
            using var testContext = this.CreateTestContext();

            var code = new Dictionary<string, string> { ["Class1.cs"] = "class Class1 { class Nested {} }" };

            var compilation = testContext.CreateCSharpCompilation( code );

            var syntaxTree1 = compilation.SyntaxTrees.Single();
            var partialCompilation = PartialCompilation.CreatePartial( compilation, syntaxTree1 );

            // Under bug 28733, the following line would throw
            // `AssertionFailedException: The item Class1.Nested of type NonErrorNamedTypeSymbol has been visited twice.`
            _ = CompilationModel.CreateInitialInstance( new ProjectModel( compilation, testContext.ServiceProvider ), partialCompilation );
        }

        [Fact]
        public void TypeClosure()
        {
            using var testContext = this.CreateTestContext();

            var code = new Dictionary<string, string>
            {
                ["Class1.cs"] = "public class Class1 { }",
                ["Class2.cs"] = "public class Class2 { }",
                ["Class3.cs"] = "public class Class3 : Class2 { }",
                ["Interface1.cs"] = "public interface Interface1 { }",
                ["Interface2.cs"] = "public interface Interface2 : Interface1 { }",
                ["Interface3.cs"] = "public interface Interface3 : Interface2 { }",
                ["Class4.cs"] = "public class Class4 : Class3, Interface3 { }"
            };

            var compilation = testContext.CreateCSharpCompilation( code );
            var nullProject = new ProjectModel( testContext.ServiceProvider );

            // Tests for Class1.
            var syntaxTree1 = compilation.SyntaxTrees.Single( t => t.FilePath == "Class1.cs" );
            var compilationModel1 = CompilationModel.CreateInitialInstance( nullProject, PartialCompilation.CreatePartial( compilation, syntaxTree1 ) );
            Assert.Single( compilationModel1.Types.SelectAsReadOnlyCollection( t => t.Name ), "Class1" );

            // Tests for Class3. The Types collection must contain the base class.
            var syntaxTree3 = compilation.SyntaxTrees.Single( t => t.FilePath == "Class3.cs" );
            var compilationModel3 = CompilationModel.CreateInitialInstance( nullProject, PartialCompilation.CreatePartial( compilation, syntaxTree3 ) );
            Assert.Equal( new[] { "Class2", "Class3" }, compilationModel3.Types.SelectAsReadOnlyCollection( t => t.Name ).OrderBy( t => t ) );

            // Tests for Class4: the Types collection must contain the base class and the interfaces.
            var semanticModel4 = compilation.SyntaxTrees.Single( t => t.FilePath == "Class4.cs" );
            var compilationModel4 = CompilationModel.CreateInitialInstance( nullProject, PartialCompilation.CreatePartial( compilation, semanticModel4 ) );

            Assert.Equal(
                new[] { "Class2", "Class3", "Class4", "Interface1", "Interface2", "Interface3" },
                compilationModel4.Types.SelectAsReadOnlyCollection( t => t.Name ).OrderBy( t => t ) );
        }

        [Fact]
        public void Dependencies()
        {
            using var testContext = this.CreateTestContext();

            var code = new Dictionary<string, string>
            {
                ["Class1.cs"] = "public class Class1 { }",
                ["Class2.cs"] = "public class Class2 { }",
                ["Class3.cs"] = "public class Class3 : Class2 { }",
                ["Interface1.cs"] = "public interface Interface1 { }",
                ["Interface2.cs"] = "public interface Interface2 : Interface1 { }",
                ["Interface3.cs"] = "public interface Interface3 : Interface2 { }",
                ["Class4.cs"] = "public class Class4 : Class3, Interface3 { }"
            };

            var compilation = PartialCompilation.CreateComplete( testContext.CreateCSharpCompilation( code ) );
            var collector = new TestDependencyCollector();

            compilation.DerivedTypes.PopulateDependencies( collector );

            var dependencies = string.Join( ",", collector.Dependencies.OrderBy( x => x ) );

            this._logger.WriteLine( dependencies );

            Assert.Equal(
                "Class3->Class2,Class4->Class2,Class4->Class3,Class4->Interface1,Class4->Interface2,Class4->Interface3,Interface2->Interface1,Interface3->Interface1,Interface3->Interface2",
                dependencies );
        }

        [Fact]
        public void Namespaces()
        {
            using var testContext = this.CreateTestContext();

            var code = new Dictionary<string, string>
            {
                ["Class1.cs"] = "namespace Ns1 { public class Class1 { } }",
                ["Class2.cs"] = "namespace Ns1 { public class Class2 : Class1 { } }",
                ["Class3.cs"] = "namespace Ns2 { public class Class3 { } }",
                ["Class4.cs"] = "namespace Ns1 { public class Class4 { } }"
            };

            var compilation = testContext.CreateCSharpCompilation( code );
            var nullProject = new ProjectModel( testContext.ServiceProvider );

            var syntaxTree1 = compilation.SyntaxTrees.Single( t => t.FilePath == "Class2.cs" );
            var compilationModel1 = CompilationModel.CreateInitialInstance( nullProject, PartialCompilation.CreatePartial( compilation, syntaxTree1 ) );

            var ns1 = compilationModel1.GlobalNamespace.Namespaces.Single();

            Assert.Equal( new[] { "Class1", "Class2" }, ns1.Types.SelectAsReadOnlyCollection( t => t.Name ).OrderBy( t => t ) );
        }

        [Fact]
        public void SyntaxTreeWithoutType()
        {
            using var testContext = this.CreateTestContext();

            var code = new Dictionary<string, string> { ["Class1.cs"] = "/* Intentionally empty */" };

            var compilation = testContext.CreateCSharpCompilation( code );
            var partialCompilation = PartialCompilation.CreatePartial( compilation, compilation.SyntaxTrees[0] );
            Assert.Single( partialCompilation.SyntaxTrees );
        }

        [Fact]
        public async Task SeveralModifications_Partial()
        {
            using var testContext = this.CreateTestContext();
            var code = new Dictionary<string, string> { ["Class1.cs"] = "/* Intentionally empty */" };

            var compilation = testContext.CreateCSharpCompilation( code );

            await ApplySeveralModifications( PartialCompilation.CreatePartial( compilation, compilation.SyntaxTrees[0] ) );
        }

        private static async Task ApplySeveralModifications( PartialCompilation partialCompilation1 )
        {
            var initialCompilation = partialCompilation1.InitialCompilation;

            // Test the initial compilation.
            const string path1 = "1.cs";
            Assert.Single( partialCompilation1.SyntaxTrees );
            Assert.Empty( partialCompilation1.ModifiedSyntaxTrees );

            // Add a syntax tree.
            var partialCompilation2 = (PartialCompilation) partialCompilation1.AddSyntaxTrees(
                CSharpSyntaxTree.ParseText( "", path: path1, options: SupportedCSharpVersions.DefaultParseOptions ) );

            Assert.Equal( 2, partialCompilation2.SyntaxTrees.Count );
            Assert.Single( partialCompilation2.ModifiedSyntaxTrees );
            Assert.Same( initialCompilation, partialCompilation2.InitialCompilation );
            Assert.Null( partialCompilation2.ModifiedSyntaxTrees[path1].OldTree );

            // Add a second syntax tree.
            var partialCompilation3 = (PartialCompilation) partialCompilation2.AddSyntaxTrees(
                CSharpSyntaxTree.ParseText( "", path: "2.cs", options: SupportedCSharpVersions.DefaultParseOptions ) );

            Assert.Equal( 3, partialCompilation3.SyntaxTrees.Count );
            Assert.Equal( 2, partialCompilation3.ModifiedSyntaxTrees.Count );
            Assert.Null( partialCompilation3.ModifiedSyntaxTrees[path1].OldTree );
            Assert.Same( initialCompilation, partialCompilation3.InitialCompilation );

            // Modify syntax trees.
            var partialCompilation4 = (PartialCompilation) await partialCompilation3.RewriteSyntaxTreesAsync(
                _ => new Rewriter(),
                ServiceProvider<IProjectService>.Empty.WithService( new SingleThreadedTaskRunner() ) );

            Assert.Equal( 3, partialCompilation4.SyntaxTrees.Count );
            Assert.Equal( 3, partialCompilation4.ModifiedSyntaxTrees.Count );
            Assert.Null( partialCompilation4.ModifiedSyntaxTrees[path1].OldTree );
            Assert.Same( initialCompilation, partialCompilation4.InitialCompilation );
        }

        [Fact]
        public async Task SeveralModifications_Complete()
        {
            using var testContext = this.CreateTestContext();
            var code = new Dictionary<string, string> { ["Class1.cs"] = "/* Intentionally empty */" };

            var compilation = testContext.CreateCSharpCompilation( code );

            await ApplySeveralModifications( PartialCompilation.CreateComplete( compilation ) );
        }

        [Fact]
        public void SyntaxTreeForCompilationLevelAttributes_WithAssemblyInfo()
        {
            using var testContext = this.CreateTestContext();

            var code = new Dictionary<string, string>()
            {
                ["AssemblyInfo.cs"] = "[assembly: System.Reflection.AssemblyCompanyAttribute(\"Foo\")]", ["AAA.cs"] = ""
            };

            var compilation = PartialCompilation.CreateComplete( testContext.CreateCSharpCompilation( code ) );

            Assert.Equal( "MetalamaAssemblyAttributes.cs", compilation.SyntaxTreeForCompilationLevelAttributes.FilePath );
        }

        [Fact]
        public void SyntaxTreeForCompilationLevelAttributes_WithTwoAssemblyInfo()
        {
            using var testContext = this.CreateTestContext();

            var code = new Dictionary<string, string>()
            {
                ["AssemblyInfo1.cs"] = "[assembly: System.Reflection.AssemblyCompanyAttribute(\"Foo\")]",
                ["AssemblyInfo2.cs"] = "[assembly: System.Reflection.AssemblyConfigurationAttribute(\"Debug\")]"
            };

            var compilation = PartialCompilation.CreateComplete( testContext.CreateCSharpCompilation( code ) );

            Assert.Equal( "MetalamaAssemblyAttributes.cs", compilation.SyntaxTreeForCompilationLevelAttributes.FilePath );
        }

        [Fact]
        public void SyntaxTreeForCompilationLevelAttributes_WithoutAssemblyInfo()
        {
            using var testContext = this.CreateTestContext();
            var code = new Dictionary<string, string>() { ["AAA.cs"] = "", ["AA.cs"] = "" };
            var compilation = PartialCompilation.CreateComplete( testContext.CreateCSharpCompilation( code ) );

            Assert.Equal( "MetalamaAssemblyAttributes.cs", compilation.SyntaxTreeForCompilationLevelAttributes.FilePath );
        }

        [Fact]
        public void SyntaxTreeForCompilationLevelAttributes_WithoutAssemblyInfo_SameLength()
        {
            using var testContext = this.CreateTestContext();
            var code = new Dictionary<string, string>() { ["BB.cs"] = "", ["AA.cs"] = "" };
            var compilation = PartialCompilation.CreateComplete( testContext.CreateCSharpCompilation( code ) );

            Assert.Equal( "MetalamaAssemblyAttributes.cs", compilation.SyntaxTreeForCompilationLevelAttributes.FilePath );
        }

        [Fact]
        public void NestedTypedInPartialCompilations()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCSharpCompilation( "class C { class D {} }" );
            var compilationModel = PartialCompilation.CreatePartial( compilation, compilation.SyntaxTrees );

            // #30800 In partial compilations, ICompilation.Types include nested types
            Assert.Single( compilationModel.Types );
        }

        private sealed class Rewriter : SafeSyntaxRewriter
        {
            // Apply some arbitrary transformation.
            protected override SyntaxNode VisitCore( SyntaxNode? node ) => base.VisitCore( node )!.WithTrailingTrivia( SyntaxFactory.Space );
        }
    }
}