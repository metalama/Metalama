// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Templating;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating
{
    public sealed class SymbolClassifierTests : UnitTestClass
    {
        private void AssertScope(
            IDeclaration declaration,
            TemplatingScope expectedScope,
            SymbolClassificationContext context = SymbolClassificationContext.Default,
            IDiagnosticAdder? diagnosticAdder = null )
        {
            this.AssertScope( declaration.GetCompilationModel().RoslynCompilation, declaration.GetSymbol()!, expectedScope, context, diagnosticAdder );
        }

        private void AssertScope(
            INamedType declaration,
            TemplatingScope expectedScope,
            SymbolClassificationContext context = SymbolClassificationContext.Default,
            IDiagnosticAdder? diagnosticAdder = null )
        {
            this.AssertScope(
                declaration.GetCompilationModel().RoslynCompilation,
                declaration.GetSymbol().AssertSymbolNotNull(),
                expectedScope,
                context,
                diagnosticAdder );
        }

        private void AssertScope(
            IType type,
            TemplatingScope expectedScope,
            SymbolClassificationContext context = SymbolClassificationContext.Default,
            IDiagnosticAdder? diagnosticAdder = null )
        {
            this.AssertScope( type.GetCompilationModel().RoslynCompilation, type.GetSymbol().AssertSymbolNotNull(), expectedScope, context, diagnosticAdder );
        }

        private void AssertScope(
            Compilation compilation,
            ISymbol symbol,
            TemplatingScope expectedScope,
            SymbolClassificationContext context = SymbolClassificationContext.Default,
            IDiagnosticAdder? diagnosticAdder = null,
            TestContextOptions? contextOptions = null )
        {
            using var testContext = this.CreateTestContext( contextOptions );

            var classifier = testContext.ServiceProvider.GetRequiredService<ClassifyingCompilationContextFactory>().GetInstance( compilation ).SymbolClassifier;

            var actualScope = classifier.GetTemplatingScope( symbol, context );
            Assert.Equal( expectedScope, actualScope );

            if ( diagnosticAdder != null )
            {
                classifier.ReportScopeError( symbol.DeclaringSyntaxReferences.First().GetSyntax(), symbol, diagnosticAdder );
            }
        }

        [Fact]
        public void AspectType()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                using Metalama.Framework.Advising;
                                using Metalama.Framework.Aspects;
                                using Metalama.Framework.Diagnostics;

                                class C : TypeAspect
                                {
                                  void M() { }
                                  int F; // System type.
                                  DiagnosticDefinition F2; // Metalama type.


                                  [TemplateAttribute]
                                  void Template() { }
                                }
                                """;

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.OfName( "C" ).Single();
            this.AssertScope( (IDeclaration) type, TemplatingScope.RunTimeOrCompileTime );
            this.AssertScope( type.Fields.OfName( "F" ).Single(), TemplatingScope.RunTimeOrCompileTime );
            this.AssertScope( type.Fields.OfName( "F2" ).Single(), TemplatingScope.CompileTimeOnly );
            this.AssertScope( type.Methods.OfName( "M" ).Single(), TemplatingScope.RunTimeOrCompileTime );
            this.AssertScope( type.Methods.OfName( "Template" ).Single(), TemplatingScope.CompileTimeOnly );
        }

        [Fact]
        public void DirectAspectType()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                using Metalama.Framework.Aspects;
                                using Metalama.Framework.Code;
                                using Metalama.Framework.Eligibility;

                                class C : IAspect<INamedType>
                                {
                                    public void BuildAspect( IAspectBuilder<INamedType> builder ) { }

                                    public void BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }
                                }
                                """;

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.OfName( "C" ).Single();
            this.AssertScope( (IDeclaration) type, TemplatingScope.RunTimeOrCompileTime );
            this.AssertScope( type.Methods.OfName( "BuildAspect" ).Single(), TemplatingScope.CompileTimeOnly );
            this.AssertScope( type.Methods.OfName( "BuildEligibility" ).Single(), TemplatingScope.CompileTimeOnly );
        }

        [Fact]
        public void ErrorTypes()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
class C : ErrorType { }

class D : C { }

class E { ErrorType X; }
";

            var compilation = testContext.CreateCompilationModel( code, ignoreErrors: true );
            this.AssertScope( compilation.Types.OfName( "C" ).Single(), TemplatingScope.RunTimeOnly, SymbolClassificationContext.RunTimeOnly );
            this.AssertScope( compilation.Types.OfName( "D" ).Single(), TemplatingScope.RunTimeOnly, SymbolClassificationContext.RunTimeOnly );
        }

        [Fact]
        public void DefaultCode()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

class C 
{
  void M() {}
  int F;
}

class D : System.IDisposable 
{
   public void Dispose() {} 
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.OfName( "C" ).Single();
            this.AssertScope( type, TemplatingScope.RunTimeOnly, SymbolClassificationContext.RunTimeOnly );
            this.AssertScope( type.Fields.OfName( "F" ).Single(), TemplatingScope.RunTimeOnly, SymbolClassificationContext.RunTimeOnly );
            this.AssertScope( type.Methods.OfName( "M" ).Single(), TemplatingScope.RunTimeOnly, SymbolClassificationContext.RunTimeOnly );

            this.AssertScope( compilation.Types.OfName( "D" ).Single(), TemplatingScope.RunTimeOnly, SymbolClassificationContext.RunTimeOnly );
        }

        [Fact]
        public void AssemblyAttribute()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
[assembly: CompileTime]
class C 
{
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.OfName( "C" ).Single();
            this.AssertScope( type, TemplatingScope.CompileTimeOnly );
        }

        [Fact]
        public void MarkedAsCompileTimeOnly()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[CompileTime]
class C 
{
  void M() {}
  int F;

  class Nested {}
}
";

            var compilation = testContext.CreateCSharpCompilation( code );
            var type = (ITypeSymbol) compilation.GetSymbolsWithName( "C" ).Single();
            this.AssertScope( compilation, type, TemplatingScope.CompileTimeOnly );
            this.AssertScope( compilation, type.GetMembers( "F" ).Single(), TemplatingScope.CompileTimeOnlyReturningBoth );
            this.AssertScope( compilation, type.GetMembers( "M" ).Single(), TemplatingScope.CompileTimeOnly );
            this.AssertScope( compilation, type.GetMembers( "Nested" ).Single(), TemplatingScope.CompileTimeOnly );
        }

        [Fact]
        public void MarkedAsCompileTime()
        {
            using var testContext = this.CreateTestContext();

            // We cannot use CompilationModel for this test because CompileTimeOnly are hidden from the model.

            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

[RunTimeOrCompileTime]
class C 
{
  void M() {}
  int F;
}
";

            var compilation = testContext.CreateCompilationModel( code );
            this.AssertScope( compilation.Types.OfName( "C" ).Single(), TemplatingScope.RunTimeOrCompileTime );
        }

        [Fact]
        public void NoMetalamaReference()
        {
            using var testContext = this.CreateTestContext();

            const string code = "class C {}";
            var compilation = testContext.CreateCompilationModel( code, addMetalamaReferences: false );
            this.AssertScope( compilation.Factory.GetNamedTypeByReflectionType( typeof(int) ), TemplatingScope.RunTimeOrCompileTime );
            this.AssertScope( compilation.Factory.GetNamedTypeByReflectionType( typeof(Console) ), TemplatingScope.RunTimeOnly );
            this.AssertScope( compilation.Types.Single(), TemplatingScope.RunTimeOnly );
        }

        [Fact]
        public void UnmarkedMethodInAspect()
        {
            using var testContext = this.CreateTestContext();

            // The main purpose of these tests is to check that there is no infinite recursion.

            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using System.Collections.Generic;

internal class C : TypeAspect
{
    public int M() => 0;
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var m1 = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single();
            this.AssertScope( m1, TemplatingScope.RunTimeOrCompileTime );
            this.AssertScope( m1.ReturnType, TemplatingScope.RunTimeOrCompileTime );
        }

        [Fact]
        public void UnmarkedGenericMethodInAspect()
        {
            using var testContext = this.CreateTestContext();

            // The main purpose of these tests is to check that there is no infinite recursion.

            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using System.Collections.Generic;

internal class C : TypeAspect
{
    public T M1<T>() => default!;
    public T[] M2<T>() => default!;
    public T M3<T>(List<T> l) => default!;
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var m1 = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M1" ).Single();
            this.AssertScope( m1, TemplatingScope.RunTimeOrCompileTime );
            this.AssertScope( m1.ReturnType, TemplatingScope.RunTimeOrCompileTime );

            var m2 = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M2" ).Single();
            this.AssertScope( m2, TemplatingScope.RunTimeOrCompileTime );
            this.AssertScope( m2.ReturnType, TemplatingScope.RunTimeOrCompileTime );

            var m3 = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M3" ).Single();
            this.AssertScope( m3, TemplatingScope.RunTimeOrCompileTime );
            this.AssertScope( m3.ReturnType, TemplatingScope.RunTimeOrCompileTime );
        }

        [Fact]
        public void GenericTemplate()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using System.Collections.Generic;

internal class C : TypeAspect
{
    [Template]
    public T M1<[CompileTime] T>() => default!;

    [Template]
    public T[] M2<[CompileTime] T>() => default!;

    [Template]
    public T M3<[CompileTime] T>(List<T> p1, T[] p2, List<T[]> p3) => default!;
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var m1 = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M1" ).Single();
            this.AssertScope( m1.ReturnType, TemplatingScope.CompileTimeOnlyReturningRuntimeOnly );

            var m2 = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M2" ).Single();
            this.AssertScope( m2.ReturnType, TemplatingScope.RunTimeOnly );

            var m3 = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M3" ).Single();
            this.AssertScope( m3.Parameters[0].Type, TemplatingScope.RunTimeOnly );
            this.AssertScope( m3.Parameters[1].Type, TemplatingScope.RunTimeOnly );
            this.AssertScope( m3.Parameters[2].Type, TemplatingScope.RunTimeOnly );
        }

        [Fact]
        public void TypeArgumentBug()
        {
            const string code = @"
using System.Collections.Immutable;

class C 
{
   void M() 
   {
      var ids = ImmutableArray.CreateBuilder<int>();
   }
}
";

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );

            var classifier = testContext.ServiceProvider.GetRequiredService<ClassifyingCompilationContextFactory>()
                .GetInstance( compilation.RoslynCompilation )
                .SymbolClassifier;

            var syntaxTree = compilation.RoslynCompilation.SyntaxTrees.First();
            var semanticModel = compilation.RoslynCompilation.GetSemanticModel( syntaxTree );
            var nodes = syntaxTree.GetRoot().DescendantNodes();

            foreach ( var node in nodes )
            {
                var symbol = semanticModel.GetSymbolInfo( node ).Symbol;

                if ( symbol != null )
                {
                    classifier.GetTemplatingScope( symbol, SymbolClassificationContext.RunTimeOnly );
                }
            }
        }

        [Fact]
        public void RecordStruct()
        {
            const string code = @"record struct S ( int X );
";

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();

            this.AssertScope( type, TemplatingScope.RunTimeOnly, SymbolClassificationContext.RunTimeOnly );
        }

        [Fact]
        public void NestedClassCompileTimeByInheritance()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

class C : TypeAspect 
{
  class S : IAspectState {}
}
";

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.Single();

            this.AssertScope( type, TemplatingScope.RunTimeOrCompileTime );
        }

        [Fact]
        public void ConflictDiagnostic()
        {
            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;

class RunTimeClass { } 

[RunTimeOrCompileTime]
class C  {
   void M( RunTimeClass c, IAspectBuilder<IDeclaration> a ) {}
}

";

            DiagnosticBag diagnosticBag = new();

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.OfName( "C" ).Single();
            var method = type.Methods.Single();

            this.AssertScope( method, TemplatingScope.Conflict, diagnosticAdder: diagnosticBag );

            var diagnostic = Assert.Single( diagnosticBag );

            Assert.Equal( TemplatingDiagnosticDescriptors.TemplatingScopeConflict.Id, diagnostic.Id );
        }

        [Fact]
        public void SystemTypes()
        {
            const string code = """
                                using System;

                                class C
                                {
                                    void M()
                                    {
                                        Console.WriteLine();

                                        _ = DateTime.Now;

                                        Math.Abs(0);
                                    }
                                }
                                """;

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );

            var syntaxTree = compilation.RoslynCompilation.SyntaxTrees.First();
            var semanticModel = compilation.RoslynCompilation.GetSemanticModel( syntaxTree );
            var nodes = syntaxTree.GetRoot().DescendantNodes().ToArray();

            AssertScope( "Console", TemplatingScope.RunTimeOnly );   // Hardcoded.
            AssertScope( "WriteLine", TemplatingScope.RunTimeOnly ); // Hardcoded.
            AssertScope( "Now", TemplatingScope.RunTimeOnly );       // Hardcoded.

            AssertScope( "DateTime", TemplatingScope.NotCompileTimeOnly );
            AssertScope( "Math", TemplatingScope.NotCompileTimeOnly );
            AssertScope( "Abs", TemplatingScope.NotCompileTimeOnly );

            // Resharper disable once LocalFunctionHidesMethod
            void AssertScope( string text, TemplatingScope scope )
            {
                var node = nodes.Single( n => n.ToString() == text );
                var symbol = semanticModel.GetSymbolInfo( node ).Symbol!;

                this.AssertScope( compilation.RoslynCompilation, symbol, scope, SymbolClassificationContext.RunTimeOnly );
            }
        }

        [Fact]
        public void GenericTypeWithNonCompileTimeOnlyArgument()
        {
            const string code = """
                                using System;
                                using System.Threading.Tasks;
                                using System.Linq;
                                using System.Linq.Expressions;

                                namespace System;

                                class C
                                {
                                    public static Task<float?> AverageAsync<TSource>( IQueryable<TSource> source,
                                       Expression<Func<TSource, float?>> predicate)
                                        => null!;
                                }
                                """;

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );

            var syntaxTree = compilation.RoslynCompilation.SyntaxTrees.First();
            var semanticModel = compilation.RoslynCompilation.GetSemanticModel( syntaxTree );
            var nodes = syntaxTree.GetRoot().DescendantNodes().ToArray();

            var node = nodes.OfType<MethodDeclarationSyntax>().Single();
            var symbol = semanticModel.GetDeclaredSymbol( node ).AssertNotNull();

            this.AssertScope( compilation.RoslynCompilation, symbol, TemplatingScope.RunTimeOnly, SymbolClassificationContext.RunTimeOnly );
        }

        [Fact]
        public void RoslynTypesDefaultNotCompileTimeOnly()
        {
            // Verifies that Roslyn types are NOT classified as compile-time-only by default.
            // This is the expected behavior for projects that reference Metalama.Framework
            // but not Metalama.Framework.Sdk. See #722.

            const string code = """
                                using Microsoft.CodeAnalysis;
                                using Microsoft.CodeAnalysis.CSharp;

                                class C
                                {
                                    void M()
                                    {
                                        ISymbol symbol;
                                        CSharpSyntaxNode node;
                                    }
                                }
                                """;

            // Use default options — no explicit RoslynIsCompileTimeOnly setting.
            using var testContext = this.CreateTestContext();

            var additionalReferences =
                new[] { typeof(ISymbol), typeof(CSharpSyntaxNode) }.SelectAsReadOnlyList( type => MetadataReference.CreateFromFile( type.Assembly.Location ) );

            var compilation = testContext.CreateCompilationModel( code, additionalReferences: additionalReferences );

            var syntaxTree = compilation.RoslynCompilation.SyntaxTrees.First();
            var semanticModel = compilation.RoslynCompilation.GetSemanticModel( syntaxTree );
            var nodes = syntaxTree.GetRoot().DescendantNodes().ToArray();

            // With the default (false), Roslyn types should NOT be compile-time-only.
            AssertScope( "ISymbol", TemplatingScope.RunTimeOrCompileTime );
            AssertScope( "CSharpSyntaxNode", TemplatingScope.RunTimeOrCompileTime );

            // Resharper disable once LocalFunctionHidesMethod
            void AssertScope( string text, TemplatingScope scope )
            {
                var node = nodes.Single( n => n.ToString() == text );
                var symbol = semanticModel.GetSymbolInfo( node ).Symbol!;

                this.AssertScope( compilation.RoslynCompilation, symbol, scope, SymbolClassificationContext.RunTimeOnly );
            }
        }

        [Theory]
        [InlineData( true )]
        [InlineData( false )]
        public void RoslynTypes( bool roslynIsCompileTime )
        {
            const string code = """
                                using Microsoft.CodeAnalysis;
                                using Microsoft.CodeAnalysis.CSharp;

                                class C
                                {
                                    void M()
                                    {
                                        ISymbol symbol;
                                        CSharpSyntaxNode node;
                                    }
                                }
                                """;

            var options = new TestContextOptions() { RoslynIsCompileTimeOnly = roslynIsCompileTime };
            using var testContext = this.CreateTestContext( options );

            var additionalReferences =
                new[] { typeof(ISymbol), typeof(CSharpSyntaxNode) }.SelectAsReadOnlyList( type => MetadataReference.CreateFromFile( type.Assembly.Location ) );

            var compilation = testContext.CreateCompilationModel( code, additionalReferences: additionalReferences );

            var syntaxTree = compilation.RoslynCompilation.SyntaxTrees.First();
            var semanticModel = compilation.RoslynCompilation.GetSemanticModel( syntaxTree );
            var nodes = syntaxTree.GetRoot().DescendantNodes().ToArray();

            var expectedScope = roslynIsCompileTime ? TemplatingScope.CompileTimeOnly : TemplatingScope.RunTimeOrCompileTime;

            AssertScope( "ISymbol", expectedScope );
            AssertScope( "CSharpSyntaxNode", expectedScope );

            // Resharper disable once LocalFunctionHidesMethod
            void AssertScope( string text, TemplatingScope scope )
            {
                var node = nodes.Single( n => n.ToString() == text );
                var symbol = semanticModel.GetSymbolInfo( node ).Symbol!;

                this.AssertScope( compilation.RoslynCompilation, symbol, scope, SymbolClassificationContext.RunTimeOnly, contextOptions: options );
            }
        }

        [Fact]
        public void AnonymousTypeWithNotCompileTimeOnlyAndRunTimeOnlyProperties()
        {
            // Reproduces a bug where combining NotCompileTimeOnly with RunTimeOnly throws an exception.
            // This can occur in Razor-generated code with anonymous objects containing mixed property types.
            // See: https://github.com/metalama/Metalama/issues/1261

            const string code = """
                                using System;
                                using System.Linq;
                                using System.Collections.Generic;

                                class Download { public Guid DownloadGuid { get; set; } }

                                class C
                                {
                                    void M( List<Download> downloads )
                                    {
                                        // Anonymous type with:
                                        // - Guid property (NotCompileTimeOnly - it's a System struct)
                                        // - string property (RunTimeOnly in run-time context)
                                        var result = downloads.Select( d => new { id = d.DownloadGuid, name = "test" } );
                                    }
                                }
                                """;

            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( code );

            var classifier = testContext.ServiceProvider.GetRequiredService<ClassifyingCompilationContextFactory>()
                .GetInstance( compilation.RoslynCompilation )
                .SymbolClassifier;

            var syntaxTree = compilation.RoslynCompilation.SyntaxTrees.First();
            var semanticModel = compilation.RoslynCompilation.GetSemanticModel( syntaxTree );
            var nodes = syntaxTree.GetRoot().DescendantNodes();

            // This should not throw - it previously threw "Invalid combination: (NotCompileTimeOnly, RunTimeOnly)"
            foreach ( var node in nodes )
            {
                var symbol = semanticModel.GetSymbolInfo( node ).Symbol;

                if ( symbol != null )
                {
                    classifier.GetTemplatingScope( symbol, SymbolClassificationContext.RunTimeOnly );
                }

                if ( semanticModel.GetTypeInfo( node ).Type is { } type )
                {
                    classifier.GetTemplatingScope( type, SymbolClassificationContext.RunTimeOnly );
                }
            }
        }

#if NET7_0_OR_GREATER
        [Fact]
        public void NonStandardApiInStandardType()
        {
            using var testContext = this.CreateTestContext();

            // The main purpose of these tests is to check that there is no infinite recursion.

            const string code = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using System.Collections.Generic;

internal class C : TypeAspect
{
    public int F => int.Clamp( 1, 2, 3 );
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var field = (PropertyDeclarationSyntax) compilation.Types.Single().Properties.Single().GetPrimaryDeclarationSyntax().AssertNotNull();
            var invocation = field.DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            var semanticModel = compilation.RoslynCompilation.GetSemanticModel( field.SyntaxTree );
            var invokedMethod = semanticModel.GetSymbolInfo( invocation.Expression ).Symbol.AssertSymbolNotNull();

            this.AssertScope( compilation.RoslynCompilation, invokedMethod, TemplatingScope.RunTimeOnly );
        }
#endif

        [Fact]
        public void EnumNestedInRunTimeOrCompileTimeType()
        {
            // Reproduces #627: A nested enum inside a [RunTimeOrCompileTime] type should be usable by members of that type.
            using var testContext = this.CreateTestContext();

            const string code = @"
using Metalama.Framework.Aspects;

[RunTimeOrCompileTime]
class C
{
    enum E
    {
        A
    }

    void M( E e ) { }
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.OfName( "C" ).Single();

            // The nested enum should NOT be run-time-only — it should inherit RunTimeOrCompileTime from the parent.
            this.AssertScope( type.Types.OfName( "E" ).Single(), TemplatingScope.RunTimeOrCompileTime );

            // The method using the enum in its signature should also be RunTimeOrCompileTime.
            this.AssertScope( type.Methods.OfName( "M" ).Single(), TemplatingScope.RunTimeOrCompileTime );
        }

        [Fact]
        public void ExplicitCompileTimeNestedInRunTimeOrCompileTimeType()
        {
            // A nested type explicitly marked [CompileTime] inside a [RunTimeOrCompileTime] type
            // should retain its explicit CompileTimeOnly scope.
            using var testContext = this.CreateTestContext();

            const string code = @"
using Metalama.Framework.Aspects;

[RunTimeOrCompileTime]
class C
{
    [CompileTime]
    class Nested { }
}
";

            var compilation = testContext.CreateCSharpCompilation( code );
            var parentType = (ITypeSymbol) compilation.GetSymbolsWithName( "C" ).Single();
            var nestedType = parentType.GetMembers( "Nested" ).Single();

            this.AssertScope( compilation, nestedType, TemplatingScope.CompileTimeOnly );
        }

        [Fact]
        public void ExplicitRunTimeNestedInRunTimeOrCompileTimeType()
        {
            // A nested type explicitly marked [RunTime] inside a [RunTimeOrCompileTime] type
            // should retain its explicit RunTimeOnly scope.
            using var testContext = this.CreateTestContext();

            const string code = @"
using Metalama.Framework.Aspects;

[RunTimeOrCompileTime]
class C
{
    [RunTime]
    class Nested { }
}
";

            var compilation = testContext.CreateCSharpCompilation( code );
            var parentType = (ITypeSymbol) compilation.GetSymbolsWithName( "C" ).Single();
            var nestedType = parentType.GetMembers( "Nested" ).Single();

            this.AssertScope( compilation, nestedType, TemplatingScope.RunTimeOnly );
        }

        [Fact]
        public void ImplicitCompileTimeNestedInRunTimeOrCompileTimeType()
        {
            // A nested type that inherits from a compile-time base (TypeFabric) inside a [RunTimeOrCompileTime] type
            // should be classified as CompileTimeOnly through inheritance.
            using var testContext = this.CreateTestContext();

            const string code = @"
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

[RunTimeOrCompileTime]
class C
{
    class Nested : TypeFabric
    {
        public override void AmendType( ITypeAmender amender ) { }
    }
}
";

            var compilation = testContext.CreateCSharpCompilation( code );
            var parentType = (ITypeSymbol) compilation.GetSymbolsWithName( "C" ).Single();
            var nestedType = parentType.GetMembers( "Nested" ).Single();

            this.AssertScope( compilation, nestedType, TemplatingScope.CompileTimeOnly );
        }

        [Fact]
        public void PlainClassNestedInRunTimeOrCompileTimeType()
        {
            // A plain nested class (no attributes, no special base type) inside a [RunTimeOrCompileTime] type
            // should inherit RunTimeOrCompileTime from its declaring type.
            using var testContext = this.CreateTestContext();

            const string code = @"
using Metalama.Framework.Aspects;

[RunTimeOrCompileTime]
class C
{
    class Nested
    {
        void M() { }
    }
}
";

            var compilation = testContext.CreateCompilationModel( code );
            var type = compilation.Types.OfName( "C" ).Single();

            this.AssertScope( type.Types.OfName( "Nested" ).Single(), TemplatingScope.RunTimeOrCompileTime );
        }
    }
}