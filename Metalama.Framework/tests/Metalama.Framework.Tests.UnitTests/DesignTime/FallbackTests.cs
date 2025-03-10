// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AdditionalOutputs;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

// ReSharper disable UseAwaitUsing

namespace Metalama.Framework.Tests.UnitTests.DesignTime
{
    public sealed class FallbackTests : UnitTestClass
    {
        [Fact]
        public async Task DeclarativeAsync()
        {
            const string code = @"
using Metalama.Framework.Aspects; 

public class IntroductionAspectAttribute : TypeAspect
{
    [Introduce]
    public int Foo()
    {
        return 42;
    }
}
    
[IntroductionAspect]
public partial class TargetClass
{
    public void Bar()
    {
        _ = this.Foo();
    }
}

// <remove>
public partial class TargetClass
{
    public int Foo()
    {
        return 0;
    }
}
";

            await this.RunTestAsync( code );
        }

        [Fact]
        public async Task ProgrammaticAsync()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;

public class IntroductionAspectAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        builder.IntroduceMethod( nameof(Foo));
    }

    [Template]
    public int Foo()
    {
        return 42;
    }
}
    
[IntroductionAspect]
public partial class TargetClass
{
    public void Bar()
    {
        _ = this.Foo();
    }
}

// <remove>
public partial class TargetClass
{
    public int Foo()
    {
        return 0;
    }
}
";

            await this.RunTestAsync( code );
        }

        [Fact]
        public async Task NonPartialAsync()
        {
            const string code = @"
using Metalama.Framework.Aspects; 

public class IntroductionAspectAttribute : TypeAspect
{
    [Introduce]
    public int Foo()
    {
        return 42;
    }
}
    
[IntroductionAspect]
public class TargetClass
{
    public void Bar()
    {
    }
}
";

            await this.RunTestAsync( code );
        }

        private async Task RunTestAsync( string code )
        {
            using var testContext = this.CreateTestContext();

            var inputCompilation = testContext.CreateCSharpCompilation( code );

            // Create a compilation from the input compilation while removing nodes marked for removal.
            foreach ( var inputSyntaxTree in inputCompilation.SyntaxTrees )
            {
                inputCompilation = inputCompilation.ReplaceSyntaxTree(
                    inputSyntaxTree,
                    inputSyntaxTree.WithRootAndOptions( new RemovingRewriter().Visit( await inputSyntaxTree.GetRootAsync() )!, inputSyntaxTree.Options ) );
            }

            // Replace the project options to enable design time fallback.
            var designTimeFallbackServiceProvider =
                testContext.ServiceProvider.WithService( new DesignTimeFallbackProjectOptions( testContext.ProjectOptions ), true );

            using var compileTimePipeline = new CompileTimeAspectPipeline(
                designTimeFallbackServiceProvider,
                ExecutionScenario.CompileTime );

            var diagnosticAdder = new ThrowingDiagnosticAdder();

            var compileTimeResult = await compileTimePipeline.ExecuteAsync( diagnosticAdder.Report, null, inputCompilation, default );

            if ( !compileTimeResult.IsSuccessful )
            {
                throw new AssertionFailedException( "CompileTimeAspectPipeline.ExecuteAsync failed." );
            }

            // Create a compilation from the input compilation with removed nodes plus auxiliary files.
            var resultingCompilation = inputCompilation;

            foreach ( var file in compileTimeResult.Value.AdditionalCompilationOutputFiles.Where(
                         f => f.Kind == AdditionalCompilationOutputFileKind.DesignTimeGeneratedCode ) )
            {
                using var outputStream = new MemoryStream();
                file.WriteToStream( outputStream );
                using var inputStream = new MemoryStream( outputStream.ToArray() );
                var syntaxTree = CSharpSyntaxTree.ParseText( SourceText.From( inputStream ), SupportedCSharpVersions.DefaultParseOptions );
                resultingCompilation = resultingCompilation.AddSyntaxTrees( syntaxTree );
            }

            var compilationDiagnostics = resultingCompilation.GetDiagnostics();

            Assert.Empty( compilationDiagnostics );

            var emitResult = resultingCompilation.Emit( Stream.Null );

            Assert.Empty( emitResult.Diagnostics.Where( d => d.Severity != DiagnosticSeverity.Hidden ) );
        }

        private sealed class DesignTimeFallbackProjectOptions : ProjectOptionsWrapper
        {
            public override bool IsDesignTimeEnabled => false;

            public override bool IsTest => true;

            public DesignTimeFallbackProjectOptions( IProjectOptions underlying ) : base( underlying ) { }
        }

        private sealed class RemovingRewriter : SafeSyntaxRewriter
        {
            public override SyntaxNode? VisitClassDeclaration( ClassDeclarationSyntax node )
            {
                if ( HasRemoveTrivia( node ) )
                {
                    return null;
                }

                return base.VisitClassDeclaration( node );
            }

            public override SyntaxNode? VisitStructDeclaration( StructDeclarationSyntax node )
            {
                if ( HasRemoveTrivia( node ) )
                {
                    return null;
                }

                return base.VisitStructDeclaration( node );
            }

            public override SyntaxNode? VisitRecordDeclaration( RecordDeclarationSyntax node )
            {
                if ( HasRemoveTrivia( node ) )
                {
                    return null;
                }

                return base.VisitRecordDeclaration( node );
            }

            public override SyntaxNode? VisitEnumDeclaration( EnumDeclarationSyntax node )
            {
                if ( HasRemoveTrivia( node ) )
                {
                    return null;
                }

                return base.VisitEnumDeclaration( node );
            }

            public override SyntaxNode? VisitInterfaceDeclaration( InterfaceDeclarationSyntax node )
            {
                if ( HasRemoveTrivia( node ) )
                {
                    return null;
                }

                return base.VisitInterfaceDeclaration( node );
            }

            private static bool HasRemoveTrivia( SyntaxNode typeDeclaration )
            {
                if ( typeDeclaration.HasLeadingTrivia )
                {
                    var leadingTrivia = typeDeclaration.GetLeadingTrivia();

                    if ( leadingTrivia.ToString().ContainsOrdinal( "<remove>" ) )
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}