// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Templating.MetaModel;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.AspectTesting;
using Metalama.Testing.AspectTesting.Utilities;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;
using RefKind = Metalama.Framework.Code.RefKind;
#if NET5_0_OR_GREATER
using System.Runtime.Loader;
#endif

namespace Metalama.Framework.Tests.TemplateTests.Runner
{
    /// <summary>
    /// Executes template integration tests by compiling and expanding a template method in the input source file.
    /// </summary>
    internal sealed class TemplatingTestRunner : BaseTestRunner
    {
        private readonly IEnumerable<CSharpSyntaxVisitor> _testAnalyzers;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatingTestRunner"/> class.
        /// </summary>
        public TemplatingTestRunner(
            GlobalServiceProvider serviceProvider,
            string? projectDirectory,
            TestProjectReferences references,
            ITestOutputHelper? logger ) : this(
            serviceProvider,
            projectDirectory,
            references,
            Array.Empty<CSharpSyntaxVisitor>(),
            logger ) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatingTestRunner"/> class.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="projectDirectory"></param>
        /// <param name="metadataReferences"></param>
        /// <param name="implicitUsings"></param>
        /// <param name="testAnalyzers">A list of analyzers to invoke on the test source.</param>
        /// <param name="logger"></param>
        private TemplatingTestRunner(
            GlobalServiceProvider serviceProvider,
            string? projectDirectory,
            TestProjectReferences references,
            IEnumerable<CSharpSyntaxVisitor> testAnalyzers,
            ITestOutputHelper? logger )
            : base(
                serviceProvider,
                projectDirectory,
                references,
                logger )
        {
            this._testAnalyzers = testAnalyzers;
        }

        /// <summary>
        /// Runs the template test with name and source provided in the <paramref name="testInput"/>.
        /// </summary>
        /// <param name="testInput">Specifies the input test parameters such as the name and the source.</param>
        /// <param name="testResult"></param>
        /// <param name="testContext"></param>
        /// <returns>The result of the test execution.</returns>
        protected override async Task RunAsync(
            TestInput testInput,
            TestResult testResult,
            TestContext testContext )
        {
            await base.RunAsync( testInput, testResult, testContext );

            if ( !testResult.Success )
            {
                return;
            }

            var testSyntaxTree = testResult.SyntaxTrees.Single();
            var templateDocument = testSyntaxTree.InputDocument;
            var templateSyntaxRoot = await templateDocument.GetSyntaxRootAsync();
            var templateSemanticModel = await templateDocument.GetSemanticModelAsync();

            foreach ( var testAnalyzer in this._testAnalyzers )
            {
                testAnalyzer.Visit( templateSyntaxRoot );
            }

            var serviceProvider = testContext.ServiceProvider
                .WithService( SyntaxGenerationOptions.Formatted )
                .WithService( new TestTemplateClassMemberBuilder() )
                .WithService( UserDiagnosticRegistry.Empty )
                .WithService( ConstantDiagnosticExtensionPolicy.None );

            var assemblyLocator = serviceProvider.GetReferenceAssemblyLocator();

            // Create an empty compilation (just with references) for the compile-time project.
            var compileTimeCompilation = CSharpCompilation.Create(
                    "assemblyName",
                    Array.Empty<SyntaxTree>(),
                    assemblyLocator.MetadataReferences,
                    (CSharpCompilationOptions) testResult.InputProject!.CompilationOptions! )
                .AddReferences( MetadataReference.CreateFromFile( typeof(TestTemplateAttribute).Assembly.Location ) );

            var templateCompiler = new TestTemplateCompiler( templateSemanticModel, testResult.PipelineDiagnostics, serviceProvider );

            var templateCompilerSuccess = templateCompiler.TryCompile(
                compileTimeCompilation,
                templateSyntaxRoot!,
                out var annotatedTemplateSyntax,
                out var transformedTemplateSyntax );

            // Annotation shouldn't do any code transformations.
            // Otherwise, highlighted spans don't match the actual code.
            if ( templateCompilerSuccess && templateSyntaxRoot != null && annotatedTemplateSyntax != null )
            {
                Assert.Equal( templateSyntaxRoot.ToString(), annotatedTemplateSyntax.ToString() );
            }

            testSyntaxTree.AnnotatedSyntaxRoot = annotatedTemplateSyntax;

            // Write the transformed code to disk.
            var generatedDirectoryPath = Path.Combine( testInput.ProjectDirectory, "obj", testInput.ProjectProperties.TargetFramework, "generated" );
            var transformedTemplatePath = Path.Combine( generatedDirectoryPath, Path.ChangeExtension( testInput.TestName, ".cs" ) );
            var transformedTemplateText = await transformedTemplateSyntax!.SyntaxTree.GetTextAsync();
            Directory.CreateDirectory( Path.GetDirectoryName( transformedTemplatePath )! );

            var textWriter = new StreamWriter( transformedTemplatePath, false, Encoding.UTF8 );

            using ( textWriter.IgnoreAsyncDisposable() )
            {
                transformedTemplateText.Write( textWriter );
            }

            // Report the result to the test.
            testSyntaxTree.SetCompileTimeCode( transformedTemplateSyntax, transformedTemplatePath );

            if ( !templateCompilerSuccess )
            {
                testResult.SetFailed( "TestTemplateCompiler.TryCompile failed." );

                return;
            }

            testResult.HasOutputCode = true;

            // Create a SyntaxTree that maps to the file we have just written.
            var oldTransformedTemplateSyntaxTree = transformedTemplateSyntax.SyntaxTree;

            var newTransformedTemplateSyntaxTree = CSharpSyntaxTree.Create(
                (CSharpSyntaxNode) await oldTransformedTemplateSyntaxTree.GetRootAsync(),
                (CSharpParseOptions?) oldTransformedTemplateSyntaxTree.Options,
                transformedTemplatePath,
                Encoding.UTF8 );

            // Compile the template. This would eventually need to be done by Metalama itself and not this test program.
            compileTimeCompilation = compileTimeCompilation.AddSyntaxTrees( newTransformedTemplateSyntaxTree );

            var buildTimeAssemblyStream = new MemoryStream();
            var buildTimeDebugStream = new MemoryStream();

            // SyntaxTreeStructureVerifier.Verify( compileTimeCompilation );

            var emitResult = compileTimeCompilation.Emit(
                buildTimeAssemblyStream,
                buildTimeDebugStream,
                options: new EmitOptions(
                    defaultSourceFileEncoding: Encoding.UTF8,
                    fallbackSourceFileEncoding: Encoding.UTF8,
                    debugInformationFormat: DebugInformationFormat.PortablePdb ) );

            if ( !emitResult.Success )
            {
                testResult.PipelineDiagnostics.Report( emitResult.Diagnostics );
                testResult.SetFailed( "The final template compilation failed." );

                return;
            }

            if ( !this.VerifyBinaryStream( testInput, testResult, buildTimeAssemblyStream ) )
            {
                return;
            }

            buildTimeAssemblyStream.Seek( 0, SeekOrigin.Begin );
            buildTimeDebugStream.Seek( 0, SeekOrigin.Begin );
#if NET5_0_OR_GREATER
            var assemblyLoadContext = new AssemblyLoadContext( null, true );

            var assembly = assemblyLoadContext.LoadFromStream( buildTimeAssemblyStream, buildTimeDebugStream );
#else
            var assembly = Assembly.Load( buildTimeAssemblyStream.GetBuffer(), buildTimeDebugStream.GetBuffer() );
#endif

            try
            {
                var templateMethod = testResult.InputCompilation!.Assembly.GetTypes()
                    .Single( t => string.Equals( t.Name, "Aspect", StringComparison.Ordinal ) )
                    .GetMembers( "Template" )
                    .OfType<IMethodSymbol>()
                    .Single();

                var compiledAspectType = assembly.GetTypes().Single( t => t.Name.Equals( "Aspect", StringComparison.Ordinal ) );

                var aspectInstance = Activator.CreateInstance( compiledAspectType );

                // In templating tests, test classes do not implement ITemplateProvider so we use FromInstanceUnsafe.
                var templateProvider = TemplateProvider.FromInstanceUnsafe( aspectInstance );

                var compiledTemplateMethodName = TemplateNameHelper.GetCompiledTemplateName( templateMethod );
                var compiledTemplateMethod = compiledAspectType.GetAnyMethod( compiledTemplateMethodName );

                Invariant.Assert( compiledTemplateMethod != null );
                var driver = new TemplateDriver( serviceProvider, compiledTemplateMethod );

                var compilationModel = CompilationModel.CreateInitialInstance(
                    new ProjectModel( testResult.InputCompilation, serviceProvider ),
                    (CSharpCompilation) testResult.InputCompilation );

                var fakeTemplateClass = new TestTemplateClass(
                    serviceProvider,
                    testResult.InputCompilation.GetCompilationContext(),
                    templateMethod.ContainingType,
                    compiledAspectType );

                var fakeTemplateClassMember = new TemplateClassMember(
                    "Template",
                    "Template",
                    fakeTemplateClass,
                    NullTemplateInfo.Instance,
                    new TestTemplateAttribute(),
                    default,
                    ImmutableArray<TemplateClassMemberParameter>.Empty,
                    ImmutableArray<TemplateClassMemberParameter>.Empty,
                    ImmutableDictionary<MethodKind, TemplateClassMember>.Empty );

                var templateMethodDeclaration = compilationModel.Factory.GetMethod( templateMethod );
                var template = TemplateMemberFactory.Create( templateMethodDeclaration, fakeTemplateClassMember, templateProvider, ObjectReader.Empty );

                var (expansionContext, targetMethod) = CreateTemplateExpansionContext(
                    serviceProvider,
                    assembly,
                    compilationModel,
                    template.ForIntroduction( templateMethodDeclaration ) );

                var expandSuccessful = driver.TryExpandDeclaration( expansionContext, [], out var output );

                testResult.PipelineDiagnostics.Report( expansionContext.DiagnosticSink.ToImmutable().ReportedDiagnostics );

                if ( !expandSuccessful )
                {
                    testResult.SetFailed( "The compiled template failed." );

                    return;
                }

                await testSyntaxTree.SetRunTimeCodeAsync( targetMethod.WithBody( output! ) );
            }
            catch ( Exception e )
            {
                testResult.SetFailed( "Exception during template expansion: " + e.Message, e );
            }
#if NET5_0_OR_GREATER
            finally
            {
                assemblyLoadContext.Unload();
            }
#endif
        }

        private static (TemplateExpansionContext Context, MethodDeclarationSyntax TargetMethod) CreateTemplateExpansionContext(
            ProjectServiceProvider serviceProvider,
            Assembly assembly,
            CompilationModel compilation,
            BoundTemplateMethod template )
        {
            var roslynCompilation = compilation.RoslynCompilation;
            var compilationServices = roslynCompilation.GetCompilationContext();

            var targetType = assembly.GetTypes().Single( t => t.Name.Equals( "TargetCode", StringComparison.Ordinal ) );
            var targetMetalamaType = compilation.Factory.GetTypeByReflectionName( targetType.FullName! );
            var targetMethod = targetMetalamaType.Methods.Single( m => string.Equals( m.Name, "Method", StringComparison.Ordinal ) );

            var diagnostics = new UserDiagnosticSink( serviceProvider );

            var roslynTargetType = roslynCompilation.Assembly.GetTypes().Single( t => t.Name.Equals( "TargetCode", StringComparison.Ordinal ) );

            var roslynTargetMethod = (MethodDeclarationSyntax) roslynTargetType.GetMembers()
                .Single( m => string.Equals( m.Name, "Method", StringComparison.Ordinal ) )
                .DeclaringSyntaxReferences.Select( r => (CSharpSyntaxNode) r.GetSyntax() )
                .Single();

            var semanticModel = compilation.RoslynCompilation.GetSemanticModel( compilation.RoslynCompilation.SyntaxTrees.First() );
            var roslynTargetMethodSymbol = semanticModel.GetDeclaredSymbol( roslynTargetMethod );

            if ( roslynTargetMethodSymbol == null )
            {
                throw new InvalidOperationException( "The symbol of the target method was not found." );
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            var lexicalScopeFactory = new LexicalScopeFactory( compilation );
            var lexicalScope = lexicalScopeFactory.GetLexicalScope( targetMethod.ToFullRef() );
            var syntaxGenerationContext = compilationServices.GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted );

            var proceedExpression =
                new SyntaxUserExpression(
                    GetProceedInvocation( targetMethod ),
                    targetMethod.ReturnType );

            serviceProvider = serviceProvider.WithServices(
                ExecutionScenario.CompileTime.WithTest(),
                new TemplateClassProvider( ImmutableDictionary<string, TemplateClass>.Empty ) );

            var metaApi = MetaApi.ForMethod(
                targetMethod,
                new MetaApiProperties(
                    compilation,
                    diagnostics,
                    template.TemplateMember.AsMemberOrNamedType(),
                    default,
                    syntaxGenerationContext,
                    null!,
                    serviceProvider,
                    MetaApiStaticity.Default ) );

            return (new TemplateExpansionContext(
                        serviceProvider,
                        metaApi,
                        lexicalScope,
                        syntaxGenerationContext,
                        template,
                        template.TemplateProvider,
                        _ => proceedExpression,
                        default ),
                    roslynTargetMethod);

            static ExpressionSyntax GetProceedInvocation( IMethod targetMethod )
            {
                return
                    SyntaxFactory.InvocationExpression(
                        targetMethod.IsStatic
                            ? SyntaxFactory.IdentifierName( targetMethod.Name )
                            : SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ThisExpression(),
                                SyntaxFactory.IdentifierName( targetMethod.Name ) ),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                targetMethod.Parameters.SelectAsImmutableArray(
                                    p =>
                                        SyntaxFactory.Argument(
                                            null,
                                            p.RefKind switch
                                            {
                                                RefKind.None => default,
                                                RefKind.In => default,
                                                RefKind.Out => SyntaxFactory.Token( SyntaxKind.OutKeyword ),
                                                RefKind.Ref => SyntaxFactory.Token( SyntaxKind.RefKeyword ),
                                                _ => throw new AssertionFailedException( $"Unexpected value for RefKind in {p}: {p.RefKind}." )
                                            },
                                            SyntaxFactory.IdentifierName( p.Name ) ) ) ) ) );
            }
        }

        private sealed class TestTemplateClassMemberBuilder : ITemplateClassMemberBuilder
        {
            public bool TryGetMembers(
                TemplateClass templateClass,
                INamedTypeSymbol type,
                CompilationContext compilationContext,
                IDiagnosticAdder diagnosticAdder,
                out ImmutableDictionary<string, TemplateClassMember>? members )
            {
                members = ImmutableDictionary<string, TemplateClassMember>.Empty;

                return true;
            }
        }

        private sealed class TestTemplateClass : TemplateClass
        {
            internal TestTemplateClass(
                ProjectServiceProvider serviceProvider,
                ITemplateReflectionContext templateReflectionContext,
                INamedTypeSymbol templateTypeSymbol,
                Type templateType )
                : base( serviceProvider, templateReflectionContext, templateTypeSymbol, ThrowingDiagnosticAdder.Instance, null, templateType.Name )
            {
                this.Type = templateType;
            }

            internal override Type Type { get; }

            public override string FullName => this.Type.FullName!;
        }
    }
}