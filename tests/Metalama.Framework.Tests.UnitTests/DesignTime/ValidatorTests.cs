// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Tests.UnitTests.DesignTime.Mocks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable VSTHRD200

namespace Metalama.Framework.Tests.UnitTests.DesignTime
{
    public sealed class ValidatorTests : FrameworkBaseTestClass
    {
        [Fact]
        public void ReferenceValidatorsMakeItToCompilationResult()
        {
            using var testContext = this.CreateTestContext();

            // Initial compilation.
            const string code1 = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Validation;
using Metalama.Framework.Code;

public class Aspect : TypeAspect 
{
   public override void BuildAspect( IAspectBuilder<INamedType> builder )
   {
        builder.Outbound.ValidateReferences( this.Validate, ReferenceKinds.All );
   }

    private void Validate( in ReferenceValidationContext context ) {}
}

[Aspect]
public class C {}
";

            var compilation1 = testContext.CreateCompilationModel( code1 );

            using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );
            var pipeline = pipelineFactory.CreatePipeline( compilation1.RoslynCompilation );

            Assert.True( pipeline.TryExecute( compilation1.RoslynCompilation, default, out var compilationResult1 ) );

            Assert.False( compilationResult1.Result.ReferenceValidators.IsEmpty );

            Assert.Single( compilationResult1.Result.ReferenceValidators.GetValidatorsForSymbol( compilation1.Types.OfName( "C" ).Single().GetSymbol() ) );
        }

        [Fact]
        public void IncrementalCompilationWorks()
        {
            using var testContext = this.CreateTestContext();

            const string aspectCode = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Validation;
using Metalama.Framework.Code;

public class Aspect1 : TypeAspect 
{
   public override void BuildAspect( IAspectBuilder<INamedType> builder )
   {
        builder.Outbound.ValidateReferences( this.Validate, ReferenceKinds.All );
   }

    private void Validate( in ReferenceValidationContext context ) {}
}

public class Aspect2 : TypeAspect 
{
   public override void BuildAspect( IAspectBuilder<INamedType> builder )
   {
        builder.Outbound.ValidateReferences( this.Validate, ReferenceKinds.All );
   }

    private void Validate( in ReferenceValidationContext context ) {}
}

";

            // Initial compilation.
            const string targetCode1 = "[Aspect1] class C {}";

            var compilation1 = testContext.CreateCompilationModel( new Dictionary<string, string> { ["aspect.cs"] = aspectCode, ["target.cs"] = targetCode1 } );
            var classC = compilation1.Types.OfName( "C" ).Single().GetSymbol().AssertNotNull();

            var targetTree1 = compilation1.RoslynCompilation.SyntaxTrees.Single( t => t.FilePath == "target.cs" );

            using var pipelineFactory = new TestDesignTimeAspectPipelineFactory( testContext );
            var pipeline = pipelineFactory.CreatePipeline( compilation1.RoslynCompilation );

            Assert.True( pipeline.TryExecute( compilation1.RoslynCompilation, default, out var compilationResult1 ) );

            Assert.False( compilationResult1.Result.ReferenceValidators.IsEmpty );

            Assert.Equal(
                new[] { "Aspect1" },
                compilationResult1.Result.ReferenceValidators.GetValidatorsForSymbol( classC )
                    .SelectAsImmutableArray( v => v.Implementation.Implementation.GetType().Name ) );

            // Add a constraint.
            var targetTree2 = CSharpSyntaxTree.ParseText(
                "[Aspect1, Aspect2] class C {}",
                path: "target.cs",
                options: SupportedCSharpVersions.DefaultParseOptions );

            var compilation2 = testContext.CreateCompilationModel( compilation1.RoslynCompilation.ReplaceSyntaxTree( targetTree1, targetTree2 ) );
            Assert.True( pipeline.TryExecute( compilation2.RoslynCompilation, default, out var compilationResult2 ) );
            Assert.False( compilationResult2.Result.ReferenceValidators.IsEmpty );

            Assert.Equal(
                new[] { "Aspect1", "Aspect2" },
                compilationResult2.Result.ReferenceValidators.GetValidatorsForSymbol( classC )
                    .SelectAsReadOnlyCollection( v => v.Implementation.Implementation.GetType().Name )
                    .OrderBy( n => n )
                    .ToArray() );

            // Remove a constraint
            var targetTree3 = CSharpSyntaxTree.ParseText( "[Aspect2] class C {}", path: "target.cs", options: SupportedCSharpVersions.DefaultParseOptions );
            var compilation3 = testContext.CreateCompilationModel( compilation2.RoslynCompilation.ReplaceSyntaxTree( targetTree2, targetTree3 ) );
            Assert.True( pipeline.TryExecute( compilation3.RoslynCompilation, default, out var compilationResult3 ) );
            Assert.False( compilationResult3.Result.ReferenceValidators.IsEmpty );

            Assert.Equal(
                new[] { "Aspect2" },
                compilationResult3.Result.ReferenceValidators.GetValidatorsForSymbol( classC )
                    .SelectAsImmutableArray( v => v.Implementation.Implementation.GetType().Name ) );
        }

#if false
#if NET5_0_OR_GREATER
        [Fact( Skip = "CLR internal error when unloading the domain" )]
#else
        [Fact]
#endif
        public void CrossProjectIntegration()
        {
            using var domain = testContext.CreateDomain();
            using var options = new TestProjectOptions();
            using var factory = new TestDesignTimeAspectPipelineFactory( domain, options );

            var code1 = @"
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 

public class Aspect : TypeAspect, IInheritedAspect
{
    [Introduce]
    public void IntroducedMethod() {}
}

[Aspect]
public interface I {}
";

            var code2 = @"partial class C : I {}";

            using var testContext1 = this.CreateTestContext();

            var compilation1 = CreateCSharpCompilation( code1 );

            using var testContext2 = this.CreateTestContext();

            var compilation2 = CreateCSharpCompilation( code2, additionalReferences: new[] { compilation1.ToMetadataReference() } );

            // We have to execute the pipeline on compilation1 first and explicitly because implicit running is not currently possible
            // because of missing project options.

            Assert.True( factory.TryExecute( testContext1.ProjectOptions, compilation1, default) );
            Assert.True( factory.TryExecute( testContext2.ProjectOptions, compilation2, default, out var compilationResult2 ) );

            Assert.Single( compilationResult2!.IntroducedSyntaxTrees );
        }
#endif

        [Fact]
        public async Task ValidationDoesNotLeakCompilation()
        {
            var output = await this.ValidationDoesNotLeakCompilationCore();

            GC.Collect();

            if ( output.Compilation.TryGetTarget( out var compilation ) )
            {
                DiagnosticsHelper.CaptureDotMemoryDumpAndThrow( $"There is still a dangling reference to '{compilation}'." );
            }

            GC.KeepAlive( output.Pipeline );
        }

        private async Task<(WeakReference<Compilation> Compilation, DesignTimeAspectPipeline Pipeline)>
            ValidationDoesNotLeakCompilationCore()
        {
            using var testContext = this.CreateTestContext();

            using TestDesignTimeAspectPipelineFactory factory = new( testContext, testContext.ServiceProvider );

            var compilation1 = TestCompilationFactory.CreateCSharpCompilation(
                GetAspectRepositoryValidatorCode( "1" ),
                name: "ValidationDoesNotLeakCompilation1" );

            var pipeline = factory.GetOrCreatePipeline( testContext.ProjectOptions, compilation1 )!;

            await pipeline.ExecuteAsync( compilation1, true, AsyncExecutionContext.Get() );

            var compilation2 = TestCompilationFactory.CreateCSharpCompilation(
                GetAspectRepositoryValidatorCode( "2" ),
                name: "ValidationDoesNotLeakCompilation2" );

            // This is to make sure that the first compilation is not the last one, because it's ok to hold a reference to the last-seen compilation.
            await pipeline.ExecuteAsync( compilation2, true, AsyncExecutionContext.Get() );

            return (new WeakReference<Compilation>( compilation1 ), pipeline);
        }

        [Fact]
        public async Task ValidationCanAccessAspectRepository()
        {
            using var testContext = this.CreateTestContext();

            using TestDesignTimeAspectPipelineFactory factory = new( testContext, testContext.ServiceProvider );

            var compilation = TestCompilationFactory.CreateCSharpCompilation( GetAspectRepositoryValidatorCode( "" ) );

            var pipeline = factory.GetOrCreatePipeline( testContext.ProjectOptions, compilation )!;

            // If AspectRepository didn't work, this would throw wrapped InvalidOperationException.
            await pipeline.ExecuteAsync( compilation, true, AsyncExecutionContext.Get() );
        }

        private static string GetAspectRepositoryValidatorCode( string suffix )
            => $$"""
                 using System;
                 using Metalama.Framework.Advising;
                 using Metalama.Framework.Aspects; 
                 using Metalama.Framework.Code;
                 using Metalama.Framework.Validation;

                 public class TheAspect : TypeAspect
                 {
                     public override void BuildAspect( IAspectBuilder<INamedType> builder )
                     {
                         builder.Outbound.ValidateReferences( ValidateReference, ReferenceKinds.All );
                     }
                 
                     private void ValidateReference( in ReferenceValidationContext context )
                     {
                         if ( !( (INamedType)context.ReferencedDeclaration ).Enhancements().HasAspect<TheAspect>() )
                         {
                             throw new InvalidOperationException();
                         }
                     }
                 }

                 [TheAspect]
                 internal class C{{suffix}}
                 {
                     private C{{suffix}}? _f;
                 }
                 """;

        [Fact]
        public async Task SourceCodeModificationsWithFabric()
        {
            using var testContext = this.CreateTestContext();
            using TestDesignTimeAspectPipelineFactory factory = new( testContext, testContext.ServiceProvider );

            const string aspectCode = """
                                      using System;
                                      using Metalama.Framework.Fabrics;
                                      using Metalama.Framework.Code;
                                      using Metalama.Framework.Validation;
                                      using Metalama.Framework.Diagnostics;

                                      public class Fabric : ProjectFabric
                                      {
                                          static DiagnosticDefinition<IDeclaration> _warning = new( "MY001", Severity.Warning, "Reference to {0}" );
                                          public override void AmendProject( IProjectAmender amender )
                                          {
                                              amender.SelectMany( p => p.Types ).ValidateReferences( ValidateReference, ReferenceKinds.All );
                                          }
                                      
                                          private void ValidateReference( in ReferenceValidationContext context )
                                          {
                                              context.Diagnostics.Report( _warning.WithArguments( context.ReferencedDeclaration ) );
                                          }
                                      }

                                      """;

            const string validatedCode = """

                                         class A {}

                                         class B {}
                                         """;

            const string changingCodeFilename = "changingCode.cs";

            var code1 = ImmutableDictionary.Create<string, string>()
                .Add( "aspectCode.cs", aspectCode )
                .Add( "validatedCode.cs", validatedCode )
                .Add(
                    changingCodeFilename,
                    """
                    class C
                    {
                       void M()
                       {
                         A a;
                       }
                    }
                    """ );

            // Step 1.
            var result1 = await ValidateCompilationAsync( code1 );
            Assert.Single( result1 );

            // Step 2. Adding a reference inside a method body.
            var code2 = code1.SetItem(
                changingCodeFilename,
                """
                class C
                {
                   void M()
                   {
                     A a;
                     B b;
                   }
                }

                """ );

            var result2 = await ValidateCompilationAsync( code2 );
            Assert.Equal( 2, result2.Count );

            // Step 3. Adding a new class and a reference to this class.
            // The difficulty is that the fabric should "detect" the new class.
            var code3 = code1
                .Add( "D.cs", "class D {}" )
                .SetItem(
                    changingCodeFilename,
                    """
                    class C
                    {
                       void M()
                       {
                         A a;
                         B b;
                         D d;
                       }
                    }

                    """ );

            var result3 = await ValidateCompilationAsync( code3 );
            Assert.Equal( 3, result3.Count );

            // Local function that executes the pipeline and the validators.
            async Task<IReadOnlyList<Diagnostic>> ValidateCompilationAsync( ImmutableDictionary<string, string> code )
            {
                var compilation = TestCompilationFactory.CreateCSharpCompilation( code, name: "test" );

                // ReSharper disable AccessToDisposedClosure
                var pipeline = factory.GetOrCreatePipeline( testContext.ProjectOptions, compilation )!;

                // ReSharper restore AccessToDisposedClosure

                var compilationResult = await pipeline.ExecuteAsync( compilation, true, AsyncExecutionContext.Get() );

                return compilation.SyntaxTrees
                    .SelectMany(
                        t => DesignTimeReferenceValidatorRunner.Validate(
                                compilationResult.Value.Configuration.ServiceProvider,
                                compilation.GetSemanticModel( t ),
                                compilationResult.Value )
                            .ReportedDiagnostics )
                    .ToReadOnlyList();
            }
        }

        [Fact]
        public async Task ModificationInAttributeConstruction()
        {
            using var testContext = this.CreateTestContext();
            using TestDesignTimeAspectPipelineFactory factory = new( testContext, testContext.ServiceProvider );

            const string aspectCode = """
                                      using System;
                                      using Metalama.Framework.Advising;
                                      using Metalama.Framework.Aspects; 
                                      using Metalama.Framework.Code;
                                      using Metalama.Framework.Validation;
                                      using Metalama.Framework.Diagnostics;

                                      public class TheAspect : TypeAspect
                                      {
                                          string _name;
                                      
                                          public TheAspect( string name )
                                          {
                                              this._name = name;
                                          }
                                      
                                          static DiagnosticDefinition<string> _warning = new( "MY001", Severity.Warning, "<<{0}>>" );
                                      
                                          public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                          {
                                              builder.Outbound.ValidateReferences( ValidateReference, ReferenceKinds.All );
                                          }
                                      
                                          private void ValidateReference( in ReferenceValidationContext context )
                                          {
                                              context.Diagnostics.Report( _warning.WithArguments( this._name ) );
                                          }
                                      }

                                      """;

            const string validatedCodeTemplate = """
                                                 [TheAspect("<<name>>")]
                                                 class A {}
                                                 """;

            var baseCode = ImmutableDictionary.Create<string, string>()
                .Add( "aspectCode.cs", aspectCode )
                .Add( "userCode.cs", "class B : A {}" );

            // Step 1.
            var code1 = baseCode
                .Add( "validatedCode.cs", validatedCodeTemplate.ReplaceOrdinal( "<<name>>", "VERSION1" ) );

            var result1 = await ValidateCompilationAsync( code1 );
            Assert.Single( result1 );
            Assert.Contains( "VERSION1", result1[0].ToString(), StringComparison.Ordinal );

            // Step 2. Changing the custom attribute.
            var code2 = baseCode
                .Add( "validatedCode.cs", validatedCodeTemplate.ReplaceOrdinal( "<<name>>", "VERSION2" ) );

            var result2 = await ValidateCompilationAsync( code2 );
            Assert.Single( result2 );
            Assert.Contains( "VERSION2", result2[0].ToString(), StringComparison.Ordinal );

            // Local function that executes the pipeline and the validators.
            async Task<IReadOnlyList<Diagnostic>> ValidateCompilationAsync( ImmutableDictionary<string, string> code )
            {
                var compilation = TestCompilationFactory.CreateCSharpCompilation( code, name: "test" );

                // ReSharper disable AccessToDisposedClosure
                var pipeline = factory.GetOrCreatePipeline( testContext.ProjectOptions, compilation )!;

                // ReSharper restore AccessToDisposedClosure

                var compilationResult = await pipeline.ExecuteAsync( compilation, true, AsyncExecutionContext.Get() );

                return compilation.SyntaxTrees
                    .SelectMany(
                        t => DesignTimeReferenceValidatorRunner.Validate(
                                compilationResult.Value.Configuration.ServiceProvider,
                                compilation.GetSemanticModel( t ),
                                compilationResult.Value )
                            .ReportedDiagnostics )
                    .ToReadOnlyList();
            }
        }

        [Fact]
        public async Task CrossProject_ModificationInAttributeConstruction()
        {
            using var testContext = this.CreateTestContext();
            using TestDesignTimeAspectPipelineFactory factory = new( testContext, testContext.ServiceProvider );

            const string aspectCode = """
                                      using System;
                                      using Metalama.Framework.Advising;
                                      using Metalama.Framework.Aspects; 
                                      using Metalama.Framework.Code;
                                      using Metalama.Framework.Validation;
                                      using Metalama.Framework.Diagnostics;

                                      public class TheAspect : TypeAspect
                                      {
                                          string _name;
                                      
                                          public TheAspect( string name )
                                          {
                                              this._name = name;
                                          }
                                      
                                          static DiagnosticDefinition<string> _warning = new( "MY001", Severity.Warning, "<<{0}>>" );
                                      
                                          public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                          {
                                              builder.Outbound.ValidateReferences( ValidateReference, ReferenceKinds.All );
                                          }
                                      
                                          private void ValidateReference( in ReferenceValidationContext context )
                                          {
                                              context.Diagnostics.Report( _warning.WithArguments( this._name ) );
                                          }
                                      }

                                      """;

            const string validatedCodeTemplate = """
                                                 [TheAspect("<<name>>")]
                                                 public class A {}
                                                 """;

            // Step 1.
            var dependentCode1 = ImmutableDictionary.Create<string, string>()
                .Add( "aspectCode.cs", aspectCode )
                .Add( "validatedCode.cs", validatedCodeTemplate.ReplaceOrdinal( "<<name>>", "VERSION1" ) );

            var result1 = await ValidateCompilationAsync( dependentCode1 );
            Assert.Single( result1 );
            Assert.Contains( "VERSION1", result1[0].ToString(), StringComparison.Ordinal );

            // Step 2. Changing the custom attribute.
            var code2 = dependentCode1
                .SetItem( "validatedCode.cs", validatedCodeTemplate.ReplaceOrdinal( "<<name>>", "VERSION2" ) );

            var result2 = await ValidateCompilationAsync( code2 );
            Assert.Single( result2 );
            Assert.Contains( "VERSION2", result2[0].ToString(), StringComparison.Ordinal );

            // Local function that executes the pipeline and the validators.
            async Task<IReadOnlyList<Diagnostic>> ValidateCompilationAsync( ImmutableDictionary<string, string> dependentCode )
            {
                var mainCode = ImmutableDictionary.Create<string, string>().Add( "code.cs", "class B : A {}" );

                var compilation = TestCompilationFactory.CreateCSharpCompilation( mainCode, dependentCode: dependentCode, name: "test" );

                // ReSharper disable AccessToDisposedClosure
                var pipeline = factory.GetOrCreatePipeline( testContext.ProjectOptions, compilation )!;

                // ReSharper restore AccessToDisposedClosure

                var compilationResult = await pipeline.ExecuteAsync( compilation, true, AsyncExecutionContext.Get() );

                return compilation.SyntaxTrees
                    .SelectMany(
                        t => DesignTimeReferenceValidatorRunner.Validate(
                                compilationResult.Value.Configuration.ServiceProvider,
                                compilation.GetSemanticModel( t ),
                                compilationResult.Value )
                            .ReportedDiagnostics )
                    .ToReadOnlyList();
            }
        }

        [Fact]
        public async Task CrossProject()
        {
            using var testContext = this.CreateTestContext();
            using TestDesignTimeAspectPipelineFactory factory = new( testContext, testContext.ServiceProvider );

            const string dependentCode = """
                                         using System;
                                         using Metalama.Framework.Fabrics;
                                         using Metalama.Framework.Code;
                                         using Metalama.Framework.Validation;
                                         using Metalama.Framework.Diagnostics;

                                         public class Fabric : ProjectFabric
                                         {
                                             static DiagnosticDefinition<IDeclaration> _warning = new( "MY001", Severity.Warning, "Reference to {0}" );
                                             public override void AmendProject( IProjectAmender amender )
                                             {
                                                 amender.SelectMany( p => p.Types ).ValidateReferences( ValidateReference, ReferenceKinds.All );
                                             }
                                         
                                             private void ValidateReference( in ReferenceValidationContext context )
                                             {
                                                 context.Diagnostics.Report( _warning.WithArguments( context.ReferencedDeclaration ) );
                                             }
                                         }

                                         public class A {}

                                         public class B {}

                                         """;

            const string referencingCode = """
                                           class C
                                           {
                                              void M()
                                              {
                                                A a;
                                                B b;
                                              }
                                           }

                                           """;

            var compilation = TestCompilationFactory.CreateCSharpCompilation( referencingCode, dependentCode: dependentCode );

            var pipeline = factory.GetOrCreatePipeline( testContext.ProjectOptions, compilation )!;

            // ReSharper restore AccessToDisposedClosure

            var compilationResult = await pipeline.ExecuteAsync( compilation, true, AsyncExecutionContext.Get() );

            var diagnostics = compilation.SyntaxTrees
                .SelectMany(
                    t => DesignTimeReferenceValidatorRunner.Validate(
                            compilationResult.Value.Configuration.ServiceProvider,
                            compilation.GetSemanticModel( t ),
                            compilationResult.Value )
                        .ReportedDiagnostics )
                .ToReadOnlyList();

            Assert.Equal( 2, diagnostics.Count );
        }

        [Fact]
        public async Task CrossProject_Diamond()
        {
            // This tests that we do not get duplicate validators when a project indirectly references the same project
            // several times.

            using var testContext = this.CreateTestContext();
            using TestDesignTimeAspectPipelineFactory factory = new( testContext, testContext.ServiceProvider );

            const string dependentCode = """
                                         using System;
                                         using Metalama.Framework.Fabrics;
                                         using Metalama.Framework.Code;
                                         using Metalama.Framework.Validation;
                                         using Metalama.Framework.Diagnostics;

                                         public class Fabric : ProjectFabric
                                         {
                                             static DiagnosticDefinition<IDeclaration> _warning = new( "MY001", Severity.Warning, "Reference to {0}" );
                                             public override void AmendProject( IProjectAmender amender )
                                             {
                                                 amender.SelectMany( p => p.Types ).ValidateReferences( ValidateReference, ReferenceKinds.All );
                                             }
                                         
                                             private void ValidateReference( in ReferenceValidationContext context )
                                             {
                                                 context.Diagnostics.Report( _warning.WithArguments( context.ReferencedDeclaration ) );
                                             }
                                         }

                                         public class A {}

                                         public class B {}

                                         """;

            const string referencingCode = """
                                           class C
                                           {
                                              void M()
                                              {
                                                A a;
                                                B b;
                                              }
                                           }

                                           """;

            var dependentCompilation = TestCompilationFactory.CreateCSharpCompilation( dependentCode );

            var intermediateCompilation1 = TestCompilationFactory.CreateCSharpCompilation(
                referencingCode,
                additionalReferences: new[] { dependentCompilation.ToMetadataReference() } );

            var intermediateCompilation2 = TestCompilationFactory.CreateCSharpCompilation(
                referencingCode,
                additionalReferences: new[] { dependentCompilation.ToMetadataReference() } );

            var compilation = TestCompilationFactory.CreateCSharpCompilation(
                referencingCode,
                additionalReferences: new[]
                {
                    intermediateCompilation1.ToMetadataReference(),
                    intermediateCompilation2.ToMetadataReference(),
                    dependentCompilation.ToMetadataReference()
                } );

            var pipeline = factory.GetOrCreatePipeline( testContext.ProjectOptions, compilation )!;

            // ReSharper restore AccessToDisposedClosure

            var compilationResult = await pipeline.ExecuteAsync( compilation, true, AsyncExecutionContext.Get() );

            var diagnostics = compilation.SyntaxTrees
                .SelectMany(
                    t => DesignTimeReferenceValidatorRunner.Validate(
                            compilationResult.Value.Configuration.ServiceProvider,
                            compilation.GetSemanticModel( t ),
                            compilationResult.Value )
                        .ReportedDiagnostics )
                .ToReadOnlyList();

            Assert.Equal( 2, diagnostics.Count );
        }

        [Fact]
        public void ConstructorReference()
        {
            const string forTestOnlyAttribute = """
                                                using System;
                                                using Metalama.Framework.Advising;
                                                using Metalama.Framework.Aspects;
                                                using Metalama.Framework.Code;
                                                using Metalama.Framework.Diagnostics;
                                                using Metalama.Framework.Eligibility;
                                                using Metalama.Framework.Validation;

                                                internal class ForTestOnlyAttribute : Aspect, IAspect<IDeclaration>
                                                {
                                                    private static readonly DiagnosticDefinition<IDeclaration> _warning = new(
                                                        "TEST01",
                                                        Severity.Warning,
                                                        "'{0}' can be used only in a namespace whose name ends with '.Tests'");
                                                
                                                    public void BuildAspect(IAspectBuilder<IDeclaration> builder)
                                                    {
                                                        builder.Outbound.ValidateInboundReferences(ValidateReference, ReferenceGranularity.ParameterOrAttribute, ReferenceKinds.All);
                                                    }
                                                
                                                    public void BuildEligibility(IEligibilityBuilder<IDeclaration> builder) { }
                                                
                                                    private void ValidateReference(ReferenceValidationContext context)
                                                    {
                                                        if (!context.Origin.Type.IsConvertibleTo(context.Destination.Type!) && !context.Origin.Namespace.FullName.EndsWith(".Tests"))
                                                        {
                                                            context.Diagnostics.Report(_warning.WithArguments(context.Destination.Declaration));
                                                        }
                                                    }
                                                }
                                                """;

            const string target = """
                                  class Target
                                  {
                                      [ForTestOnly]
                                      public Target() { }
                                  }
                                  """;

            const string regularUsage = """
                                        namespace ReferenceValidation;

                                        class Foo
                                        {
                                            void M()
                                            {
                                                // These calls are FORBIDDEN because we are not in a test namespace.
                                                Target target = new();
                                                _ = new Target();
                                            }
                                        }
                                        """;

            const string testUsage = """
                                     namespace ReferenceValidation.Tests;

                                     class Bar
                                     {
                                         void M()
                                         {
                                             // This call is ALLOWED because we are in a test namespace.
                                             _ = new Target();
                                         }
                                     }
                                     """;

            using var testContext = this.CreateTestContext();

            var code = new Dictionary<string, string>
            {
                ["forTestOnlyAttribute.cs"] = forTestOnlyAttribute, ["target.cs"] = target, ["regularUsage.cs"] = regularUsage, ["testUsage.cs"] = testUsage
            };

            var compilation = TestCompilationFactory.CreateCSharpCompilation( code );

            using TestDesignTimeAspectPipelineFactory factory = new( testContext );

            Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var result ) );

            var diagnostics = compilation.SyntaxTrees
                .SelectMany(
                    t => DesignTimeReferenceValidatorRunner.Validate(
                            result.Configuration.ServiceProvider,
                            compilation.GetSemanticModel( t ),
                            result )
                        .ReportedDiagnostics )
                .ToReadOnlyList();

            Assert.Equal( 2, diagnostics.Count );

            Assert.All( diagnostics, diag => Assert.Equal( "regularUsage.cs", diag.Location.SourceTree.FilePath ) );
        }
    }
}