// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.ReferenceIndex;

/// <summary>
/// Tests of <see cref="SourceReferenceIndexService"/>, <see cref="SourceReferenceIndexStage"/>, the walk of declaration roots and the merge of the
/// options of several consumers.
/// </summary>
public sealed class SourceReferenceIndexServiceTests : UnitTestClass
{
    private static readonly Dictionary<string, string> _code = new()
    {
        ["A.cs"] = "class A { public static int F() => 0; public static int G() => 0; }",
        ["B.cs"] = """
                   class B
                   {
                       int _field = A.F();
                       int Property => A.G();
                       void M() { A.F(); System.Func<int> f = () => A.G(); }
                       class Nested { void N() => A.F(); }
                   }
                   """,
        ["C.cs"] = "class C { void M() => A.F(); }"
    };

    [Fact]
    public async Task SharedIndex_BuiltOncePerStage()
    {
        using var context = this.CreateContext( _code );

        using var stage = SourceReferenceIndexService.BeginStage(
            context.ServiceProvider,
            context.Compilation,
            [Requirements( context.Compilation, "F" ), Requirements( context.Compilation, "G" )] );

        var index1 = await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );
        var index2 = await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        Assert.Same( index1, index2 );
        Assert.Equal( ["B.cs", "C.cs"], context.Observer.ResolvedSemanticModelNames );
    }

    [Fact]
    public async Task MergedRequirements_UnionPerKind()
    {
        using var context = this.CreateContext( _code );

        using var stage = SourceReferenceIndexService.BeginStage(
            context.ServiceProvider,
            context.Compilation,
            [Requirements( context.Compilation, "F" ), Requirements( context.Compilation, "G" )] );

        var index = await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        Assert.Equal( ["F", "G"], GetReferencedNames( index ) );
    }

    /// <summary>
    /// Verifies that a name requested for one reference kind does not admit references of other kinds. Before the per-kind merge, the names of all
    /// consumers were one set shared by all kinds.
    /// </summary>
    [Fact]
    public void MergedRequirements_NameOfOneKindDoesNotAdmitAnotherKind()
    {
        var options = new ReferenceIndexerOptions(
        [
            new ReferenceIndexerRequirements( ReferenceKinds.Invocation, false, DeclarationKind.Method, "F" ),
            new ReferenceIndexerRequirements( ReferenceKinds.NameOf, false, DeclarationKind.Method, "G" )
        ] );

        var tokenF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Identifier( "F" );
        var tokenG = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Identifier( "G" );

        Assert.True( options.MustIndexReference( ReferenceKinds.Invocation, tokenF ) );
        Assert.False( options.MustIndexReference( ReferenceKinds.Invocation, tokenG ) );
        Assert.True( options.MustIndexReference( ReferenceKinds.NameOf, tokenG ) );
        Assert.False( options.MustIndexReference( ReferenceKinds.NameOf, tokenF ) );
    }

    /// <summary>
    /// Verifies that a consumer whose requirement cannot be filtered by identifier for one kind does not disable the filtering of other kinds.
    /// </summary>
    [Fact]
    public void MergedRequirements_UnconstrainedKindDisablesOnlyThatKind()
    {
        var options = new ReferenceIndexerOptions(
        [
            new ReferenceIndexerRequirements( ReferenceKinds.Invocation, false, DeclarationKind.Method, "F" ),
            new ReferenceIndexerRequirements( ReferenceKinds.TypeOf, false, DeclarationKind.Namespace, null )
        ] );

        var tokenG = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Identifier( "G" );

        Assert.False( options.MustIndexReference( ReferenceKinds.Invocation, tokenG ) );
        Assert.True( options.MustIndexReference( ReferenceKinds.TypeOf, tokenG ) );
    }

    [Fact]
    public async Task ScopeRestricted_WhenEveryConsumerReturnsRoots()
    {
        using var context = this.CreateContext( _code );
        var methodOfC = GetMethodSyntax( context.Compilation, "C", "M" );

        using var stage = SourceReferenceIndexService.BeginStage(
            context.ServiceProvider,
            context.Compilation,
            [Requirements( context.Compilation, "F" ) with { DeclarationRoots = [methodOfC] }] );

        Assert.True( stage.IsRestrictedToDeclarationRoots );

        var index = await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        Assert.Equal( ["C.M()"], GetReferencingNames( index ) );
        Assert.Equal( ["C.cs"], context.Observer.ResolvedSemanticModelNames );
    }

    [Fact]
    public async Task OneConsumerWithoutRoots_WalksEveryTree()
    {
        using var context = this.CreateContext( _code );
        var methodOfC = GetMethodSyntax( context.Compilation, "C", "M" );

        using var stage = SourceReferenceIndexService.BeginStage(
            context.ServiceProvider,
            context.Compilation,
            [Requirements( context.Compilation, "F" ) with { DeclarationRoots = [methodOfC] }, Requirements( context.Compilation, "G" )] );

        Assert.False( stage.IsRestrictedToDeclarationRoots );

        var index = await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        Assert.Contains( "B.M()", GetReferencingNames( index ) );
    }

    [Fact]
    public void NestedRoots_WalkedOnce()
    {
        using var context = this.CreateContext( _code );
        var typeB = GetTypeSyntax( context.Compilation, "B" );
        var methodOfB = GetMethodSyntax( context.Compilation, "B", "M" );

        var merged = SourceReferenceIndexStage.MergeRoots( [methodOfB, typeB, typeB] );

        Assert.Same( typeB, Assert.Single( Assert.Single( merged ).Value ) );
    }

    [Fact]
    public async Task NoRequirement_IndexNotBuilt()
    {
        using var context = this.CreateContext( _code );

        using var stage = SourceReferenceIndexService.BeginStage( context.ServiceProvider, context.Compilation, [SourceIndexRequirements.None] );

        Assert.False( stage.HasRequirements );

        var index = await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        Assert.Empty( index.ReferencedSymbols );
        Assert.Empty( context.Observer.ResolvedSemanticModelNames );
    }

    /// <summary>
    /// Verifies that requirements whose reference kinds are all <see cref="ReferenceKinds.None"/>, which <see cref="ReferenceIndexerRequirements.Create"/>
    /// returns for a request that the index cannot serve, do not make the stage walk the source compilation.
    /// </summary>
    [Fact]
    public async Task NoneKindRequirements_IndexNotBuilt()
    {
        using var context = this.CreateContext( _code );

        var requirements = new SourceIndexRequirements( [new ReferenceIndexerRequirements( ReferenceKinds.None, false, DeclarationKind.Method, "F" )] );

        Assert.True( requirements.IsEmpty );

        using var stage = SourceReferenceIndexService.BeginStage( context.ServiceProvider, context.Compilation, [requirements] );

        Assert.False( stage.HasRequirements );

        var index = await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        Assert.Empty( index.ReferencedSymbols );
        Assert.Empty( context.Observer.ResolvedSemanticModelNames );
    }

    /// <summary>
    /// Verifies that the cache of the design-time indexes does not retain the semantic model and the compilation of an index, although the index
    /// references their symbols and syntax.
    /// </summary>
    [Fact]
    public void DesignTimeIndex_DoesNotRetainCompilation()
    {
        using var context = this.CreateContext( _code );

        var compilation = IndexNewCompilation( context );

        MemoryLeakAssert.Collected( compilation, "The compilation of a design-time index" );
    }

    /// <summary>
    /// Creates a compilation that only this method references, builds its design-time index, and returns a weak reference to the compilation.
    /// </summary>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static WeakReference IndexNewCompilation( TestContext context )
    {
        var compilation = context.Compilation.RoslynCompilation.WithAssemblyName( "DesignTimeIndex_DoesNotRetainCompilation" );
        var semanticModel = compilation.GetSemanticModel( compilation.SyntaxTrees.First() );

        var index = SourceReferenceIndexService.GetDesignTimeIndex(
            context.ServiceProvider,
            semanticModel,
            DesignTimeAspectPipelineResultExtensionCollection.Empty,
            Xunit.TestContext.Current.CancellationToken );

        Assert.Same(
            index,
            SourceReferenceIndexService.GetDesignTimeIndex(
                context.ServiceProvider,
                semanticModel,
                DesignTimeAspectPipelineResultExtensionCollection.Empty,
                Xunit.TestContext.Current.CancellationToken ) );

        return new WeakReference( compilation );
    }

    [Fact]
    public async Task Stage_DisposedThrows()
    {
        using var context = this.CreateContext( _code );

        var stage = SourceReferenceIndexService.BeginStage( context.ServiceProvider, context.Compilation, [Requirements( context.Compilation, "F" )] );
        stage.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>( () => stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken ) );
    }

    [Fact]
    public void DesignTimeIndex_OnePerSemanticModel()
    {
        using var context = this.CreateContext( _code );
        var semanticModel = context.Compilation.RoslynCompilation.GetSemanticModel( context.Compilation.RoslynCompilation.SyntaxTrees.First() );

        var index1 = SourceReferenceIndexService.GetDesignTimeIndex( context.ServiceProvider, semanticModel, DesignTimeAspectPipelineResultExtensionCollection.Empty, Xunit.TestContext.Current.CancellationToken );
        var index2 = SourceReferenceIndexService.GetDesignTimeIndex( context.ServiceProvider, semanticModel, DesignTimeAspectPipelineResultExtensionCollection.Empty, Xunit.TestContext.Current.CancellationToken );

        Assert.Same( index1, index2 );
    }

    [Theory]
    [InlineData( "B", "_field", "B._field" )]
    [InlineData( "B", "Property", "B.Property.get" )]
    public void Root_MemberWithoutBody_EntersDeclaration( string typeName, string memberName, string expectedReferencingSymbol )
    {
        using var context = this.CreateContext( _code );
        var type = GetTypeSyntax( context.Compilation, typeName );

        SyntaxNode root = memberName == "Property"
            ? type.Members.OfType<PropertyDeclarationSyntax>().Single().ExpressionBody!
            : type.Members.OfType<FieldDeclarationSyntax>().Single().Declaration.Variables.Single();

        var index = IndexRoots( context, root, "F", "G" );

        Assert.Equal( [expectedReferencingSymbol], GetReferencingNames( index ) );
    }

    [Fact]
    public void Root_TypeIncludesNestedTypesAndLambdas()
    {
        using var context = this.CreateContext( _code );

        var index = IndexRoots( context, GetTypeSyntax( context.Compilation, "B" ), "F", "G" );

        Assert.Equal( ["B.M()", "B.Nested.N()", "B.Property.get", "B._field"], GetReferencingNames( index ) );
    }

    [Fact]
    public void Root_CompilationUnitWithTopLevelStatements()
    {
        using var context = this.CreateContext(
            new Dictionary<string, string> { ["A.cs"] = "class A { public static int F() => 0; }", ["Program.cs"] = "A.F();" },
            OutputKind.ConsoleApplication );

        var programTree = context.Compilation.RoslynCompilation.SyntaxTrees.Single( t => t.FilePath == "Program.cs" );

        var index = IndexRoots( context, programTree.GetRoot( Xunit.TestContext.Current.CancellationToken ), "F" );

        Assert.Single( GetReferencingNames( index ) );
    }

    [Fact]
    public void Root_Method()
    {
        using var context = this.CreateContext( _code );

        var index = IndexRoots( context, GetMethodSyntax( context.Compilation, "B", "M" ), "F", "G" );

        Assert.Equal( ["B.M()"], GetReferencingNames( index ) );
    }

    /// <summary>
    /// Verifies that a reference in a lambda is attributed to the member that contains the lambda, which is the member that a consumer selects.
    /// </summary>
    [Fact]
    public void Root_LambdaAttributedToEnclosingMember()
    {
        using var context = this.CreateContext( _code );

        var index = IndexRoots( context, GetMethodSyntax( context.Compilation, "B", "M" ), "G" );

        Assert.Equal( ["B.M()"], GetReferencingNames( index ) );
    }

    [Fact]
    public void Root_PrimaryConstructorBaseArgumentAttributedToType()
    {
        using var context = this.CreateContext(
            new Dictionary<string, string>
            {
                ["A.cs"] = "class A { public static int F() => 0; }", ["D.cs"] = "class Base( int x ) { } class D() : Base( A.F() ) { }"
            } );

        var index = IndexRoots( context, GetTypeSyntax( context.Compilation, "D" ), "F" );

        Assert.Equal( ["D"], GetReferencingNames( index ) );
    }

    [Fact]
    public void Root_Namespace()
    {
        using var context = this.CreateContext(
            new Dictionary<string, string>
            {
                ["A.cs"] = "class A { public static int F() => 0; }",
                ["N.cs"] = "namespace N { class X { void M() => A.F(); } namespace Inner { class Y { void M() => A.F(); } } } class Z { void M() => A.F(); }"
            } );

        var namespaceSyntax = context.Compilation.RoslynCompilation.SyntaxTrees.Single( t => t.FilePath == "N.cs" )
            .GetRoot( Xunit.TestContext.Current.CancellationToken )
            .DescendantNodes()
            .OfType<NamespaceDeclarationSyntax>()
            .First();

        var index = IndexRoots( context, namespaceSyntax, "F" );

        Assert.Equal( ["X.M()", "Y.M()"], GetReferencingNames( index ) );
    }

    /// <summary>
    /// Verifies that the stage walks every part of a partial type when the consumer gives every part as a root, including the parts in other
    /// syntax trees.
    /// </summary>
    [Fact]
    public async Task Root_TypeAllPartialParts()
    {
        using var context = this.CreateContext(
            new Dictionary<string, string>
            {
                ["A.cs"] = "class A { public static int F() => 0; }",
                ["P1.cs"] = "partial class P { void M1() => A.F(); }",
                ["P2.cs"] = "partial class P { void M2() => A.F(); }",
                ["Q.cs"] = "class Q { void M() => A.F(); }"
            } );

        var parts = context.Compilation.RoslynCompilation.SyntaxTrees.SelectMany( t => t.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>() )
            .Where( t => t.Identifier.Text == "P" )
            .ToArray();

        Assert.Equal( 2, parts.Length );

        using var stage = SourceReferenceIndexService.BeginStage(
            context.ServiceProvider,
            context.Compilation,
            [Requirements( context.Compilation, "F" ) with { DeclarationRoots = [..parts] }] );

        var index = await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        Assert.Equal( ["P.M1()", "P.M2()"], GetReferencingNames( index ) );
        Assert.Equal( ["P1.cs", "P2.cs"], context.Observer.ResolvedSemanticModelNames );
    }

    /// <summary>
    /// Verifies that a root that contains no identifier of the requirements does not cause the semantic model of its syntax tree to be created.
    /// </summary>
    [Fact]
    public async Task Root_NoSemanticModelWithoutNameMatch()
    {
        using var context = this.CreateContext(
            new Dictionary<string, string> { ["A.cs"] = "class A { public static int F() => 0; }", ["E.cs"] = "class E { int M() => 1 + 2; }" } );

        using var stage = SourceReferenceIndexService.BeginStage(
            context.ServiceProvider,
            context.Compilation,
            [Requirements( context.Compilation, "F" ) with { DeclarationRoots = [GetTypeSyntax( context.Compilation, "E" )] }] );

        var index = await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        Assert.Empty( GetReferencingNames( index ) );
        Assert.Empty( context.Observer.ResolvedSemanticModelNames );
    }

    /// <summary>
    /// Verifies that the roots of several syntax trees, which the stage indexes concurrently, give the same index as the walk of each root alone.
    /// </summary>
    [Fact]
    public async Task Root_ConcurrentRoots()
    {
        using var context = this.CreateContext( _code );

        SyntaxNode[] roots =
        [
            GetMethodSyntax( context.Compilation, "B", "M" ), GetTypeSyntax( context.Compilation, "Nested" ), GetMethodSyntax( context.Compilation, "C", "M" )
        ];

        using var stage = SourceReferenceIndexService.BeginStage(
            context.ServiceProvider,
            context.Compilation,
            [Requirements( context.Compilation, "F", "G" ) with { DeclarationRoots = [..roots] }] );

        var index = await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        var expected = roots.SelectMany( r => GetReferencingNames( IndexRoots( context, r, "F", "G" ) ) ).Distinct().ToOrderedList( x => x, StringComparer.Ordinal );

        Assert.Equal( ["B.M()", "B.Nested.N()", "C.M()"], expected );
        Assert.Equal( expected, GetReferencingNames( index ) );
    }

    /// <summary>
    /// Verifies that a requirement on the compilation, which admits references to any declaration of the compilation, disables the filtering by
    /// identifier of its reference kind, even when another consumer filters the same kind by identifier.
    /// </summary>
    [Fact]
    public void CompilationKind_DisablesIdentifierFilteringOfInvocations()
    {
        var options = new ReferenceIndexerOptions(
        [
            new ReferenceIndexerRequirements( ReferenceKinds.Invocation, false, DeclarationKind.Method, "F" ),
            new ReferenceIndexerRequirements( ReferenceKinds.Invocation, false, DeclarationKind.Compilation, null )
        ] );

        var tokenG = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Identifier( "G" );

        Assert.True( options.MustIndexReference( ReferenceKinds.Invocation, tokenG ) );
    }

    /// <summary>
    /// Verifies that the requirements of a design-time result of a project apply to the design-time index of that project, but not to the index
    /// of a project that references it.
    /// </summary>
    [Fact]
    public void DesignTimeIndex_ProjectLocalRequirementsNotMergedIntoReferencingProject()
    {
        var builder = DesignTimeAspectPipelineResultExtensionCollection.Empty.ToBuilder();
        builder.Add( new RequirementsProvider( "F" ) );
        var referencedProject = builder.ToImmutable( [] );

        var referencingProject = DesignTimeAspectPipelineResultExtensionCollection.Empty.WithChildCollections( [referencedProject] );

        var tokenF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Identifier( "F" );

        Assert.True( referencedProject.IndexOptions.MustIndexReference( ReferenceKinds.Invocation, tokenF ) );
        Assert.False( referencingProject.IndexOptions.MustIndexReference( ReferenceKinds.Invocation, tokenF ) );
    }

    /// <summary>
    /// A design-time result that requests the invocations of one method name.
    /// </summary>
    private sealed class RequirementsProvider : IDesignTimePipelineResultExtension, IDesignTimeReferenceIndexRequirementsProvider
    {
        private static readonly ContributorKind<RequirementsProvider> _kind = new( nameof(RequirementsProvider) );

        public RequirementsProvider( string methodName )
        {
            this.ReferenceIndexerRequirements = [new ReferenceIndexerRequirements( ReferenceKinds.Invocation, false, DeclarationKind.Method, methodName )];
        }

        public ContributorKind ContributorKind => _kind;

        public IEnumerable<ReferenceIndexerRequirements> ReferenceIndexerRequirements { get; }

        public ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension()
            => throw new InvalidOperationException( "The test does not export the result." );
    }

    private static InboundReferenceIndex IndexRoots( TestContext context, SyntaxNode root, params string[] methodNames )
    {
        var options = new ReferenceIndexerOptions( Requirements( context.Compilation, methodNames ).Requirements );
        var builder = new InboundReferenceIndexBuilder( context.ServiceProvider, options, SymbolEqualityComparer.Default );
        builder.IndexDeclarationRoots( root.SyntaxTree, [root], context.Compilation.CompilationContext.SemanticModelProvider, Xunit.TestContext.Current.CancellationToken );

        return builder.ToReadOnly();
    }

    private static SourceIndexRequirements Requirements( CompilationModel compilation, params string[] methodNames )
        => new(
            compilation.Types.OfName( "A" )
                .SelectMany( t => t.Methods )
                .Where( m => methodNames.Contains( m.Name ) )
                .Select( m => ReferenceIndexerRequirements.Create( m, ReferenceKinds.Invocation, false ) )
                .ToImmutableArray() );

    private static TypeDeclarationSyntax GetTypeSyntax( CompilationModel compilation, string typeName )
        => compilation.RoslynCompilation.SyntaxTrees.SelectMany( t => t.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>() )
            .Single( t => t.Identifier.Text == typeName );

    private static MethodDeclarationSyntax GetMethodSyntax( CompilationModel compilation, string typeName, string methodName )
        => GetTypeSyntax( compilation, typeName ).Members.OfType<MethodDeclarationSyntax>().Single( m => m.Identifier.Text == methodName );

    private static IReadOnlyList<string> GetReferencedNames( InboundReferenceIndex index )
        => index.ReferencedSymbols.Where( s => s.References.Any() ).Select( s => s.ReferencedSymbol.Name ).ToOrderedList( x => x, StringComparer.Ordinal );

    private static IReadOnlyList<string> GetReferencingNames( InboundReferenceIndex index )
        => index.ReferencedSymbols.SelectMany( s => s.References ).Select( r => r.ReferencingSymbol.ToTestName() ).Distinct().ToOrderedList( x => x, StringComparer.Ordinal );

    private TestContext CreateContext( Dictionary<string, string> code, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary )
    {
        var observer = new ReferenceIndexObserver();
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( observer );
        additionalServices.AddProjectService( SymbolClassificationService.CreateTestInstance() );
        var testContext = this.CreateTestContext( additionalServices );
        var compilation = testContext.CreateCompilationModel( testContext.CreateCSharpCompilation( code, outputKind: outputKind ) );

        return new TestContext( testContext, compilation, observer );
    }

    private sealed class TestContext : IDisposable
    {
        private readonly MetalamaTestContext _testContext;

        public TestContext( MetalamaTestContext testContext, CompilationModel compilation, ReferenceIndexObserver observer )
        {
            this._testContext = testContext;
            this.Compilation = compilation;
            this.Observer = observer;
        }

        public CompilationModel Compilation { get; }

        public ReferenceIndexObserver Observer { get; }

        public ProjectServiceProvider ServiceProvider => this._testContext.ServiceProvider;

        public void Dispose() => this._testContext.Dispose();
    }
}
