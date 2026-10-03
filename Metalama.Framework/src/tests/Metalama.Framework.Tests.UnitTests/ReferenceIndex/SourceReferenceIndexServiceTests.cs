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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.ReferenceIndex;

/// <summary>
/// Tests of <see cref="SourceReferenceIndexService"/>, <see cref="SourceReferenceIndexStage"/>, the walk of declaration roots and the merge of the
/// options of several consumers.
/// </summary>
public sealed class SourceReferenceIndexServiceTests : UnitTestClass
{
    /// <summary>
    /// The code of most tests, indexed by file path: the methods <c>A.F</c> and <c>A.G</c>, references to them in a field initializer, a property,
    /// a method, a lambda and a nested type of <c>B</c>, and a reference in a method of <c>C</c>.
    /// </summary>
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

    /// <summary>
    /// Verifies that two calls of <see cref="SourceReferenceIndexStage.GetIndexAsync"/> return the same index, and that the semantic models of
    /// <c>B.cs</c> and <c>C.cs</c> are resolved once.
    /// </summary>
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

    /// <summary>
    /// Verifies that the index of a stage that receives the requirements of two consumers contains the references requested by both, all of the kind
    /// <see cref="ReferenceKinds.Invocation"/>.
    /// </summary>
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

        Assert.All(
            index.ReferencedSymbols.SelectMany( s => s.References ).SelectMany( r => r.Nodes ),
            n => Assert.Equal( ReferenceKinds.Invocation, n.ReferenceKind ) );
    }

    /// <summary>
    /// Verifies that the constructor that merges the options of several consumers merges the identifiers per reference kind.
    /// </summary>
    [Fact]
    public void MergedOptions_UnionPerKind()
    {
        var invocationOptions = new ReferenceIndexerOptions( [new ReferenceIndexerRequirements( ReferenceKinds.Invocation, false, DeclarationKind.Method, "F" )] );
        var nameOfOptions = new ReferenceIndexerOptions( [new ReferenceIndexerRequirements( ReferenceKinds.NameOf, false, DeclarationKind.Method, "G" )] );

        var merged = new ReferenceIndexerOptions( new[] { invocationOptions, nameOfOptions } );

        var tokenF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Identifier( "F" );
        var tokenG = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Identifier( "G" );

        Assert.True( merged.MustIndexReference( ReferenceKinds.Invocation, tokenF ) );
        Assert.False( merged.MustIndexReference( ReferenceKinds.Invocation, tokenG ) );
        Assert.True( merged.MustIndexReference( ReferenceKinds.NameOf, tokenG ) );
        Assert.False( merged.MustIndexReference( ReferenceKinds.NameOf, tokenF ) );
    }

    /// <summary>
    /// Verifies that two concurrent calls of <see cref="SourceReferenceIndexStage.GetIndexAsync"/> share one build of the index.
    /// </summary>
    [Fact]
    public async Task GetIndex_ConcurrentCalls_ShareOneBuild()
    {
        using var context = this.CreateContext( _code );

        using var stage = SourceReferenceIndexService.BeginStage(
            context.ServiceProvider,
            context.Compilation,
            [Requirements( context.Compilation, "F", "G" )] );

        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        var indexes = await Task.WhenAll(
            Task.Run( () => stage.GetIndexAsync( cancellationToken ) ),
            Task.Run( () => stage.GetIndexAsync( cancellationToken ) ) );

        Assert.Same( indexes[0], indexes[1] );
        Assert.Equal( ["B.cs", "C.cs"], context.Observer.ResolvedSemanticModelNames );
    }

    /// <summary>
    /// Verifies that a build that was canceled is not cached, so that a later call builds the index.
    /// </summary>
    [Fact]
    public async Task GetIndex_AfterCanceledBuild_BuildsAgain()
    {
        using var context = this.CreateContext( _code );

        using var stage = SourceReferenceIndexService.BeginStage( context.ServiceProvider, context.Compilation, [Requirements( context.Compilation, "F" )] );

        using var canceledSource = new CancellationTokenSource();
#pragma warning disable VSTHRD103 // CancelAsync does not exist on .NET Framework.
        canceledSource.Cancel();
#pragma warning restore VSTHRD103

        await Assert.ThrowsAnyAsync<OperationCanceledException>( () => stage.GetIndexAsync( canceledSource.Token ) );

        var index = await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        Assert.Equal( ["F"], GetReferencedNames( index ) );
    }

    /// <summary>
    /// Verifies that a default array of declaration roots is equivalent to no roots, instead of failing when the roots are merged.
    /// </summary>
    [Fact]
    public async Task DeclarationRoots_DefaultArray_EquivalentToNull()
    {
        using var context = this.CreateContext( _code );

        using var stage = SourceReferenceIndexService.BeginStage(
            context.ServiceProvider,
            context.Compilation,
            [Requirements( context.Compilation, "F" ) with { DeclarationRoots = default(ImmutableArray<SyntaxNode>) }] );

        Assert.False( stage.IsRestrictedToDeclarationRoots );

        var index = await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        Assert.Equal( ["F"], GetReferencedNames( index ) );
    }

    /// <summary>
    /// Verifies that a variable declarator of a field, as a root, also indexes the attributes of the field declaration, as the walk of the whole
    /// syntax tree does.
    /// </summary>
    [Fact]
    public void Root_FieldDeclarator_IndexesAttributes()
    {
        using var context = this.CreateContext(
            new Dictionary<string, string>
            {
                ["Mark.cs"] = "class MarkAttribute : System.Attribute { }", ["D.cs"] = "class D { [MarkAttribute] int _x = 0; }"
            } );

        var declarator = context.Compilation.RoslynCompilation.SyntaxTrees.Single( t => t.FilePath == "D.cs" )
            .GetRoot( Xunit.TestContext.Current.CancellationToken )
            .DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Single();

        var options = new ReferenceIndexerOptions( [new ReferenceIndexerRequirements( ReferenceKinds.AttributeType, false, DeclarationKind.NamedType, "MarkAttribute" )] );
        var builder = new InboundReferenceIndexBuilder( context.ServiceProvider, options, SymbolEqualityComparer.Default );

        builder.IndexDeclarationRoots(
            declarator.SyntaxTree,
            [declarator],
            context.Compilation.CompilationContext.SemanticModelProvider,
            Xunit.TestContext.Current.CancellationToken );

        // The name of an attribute binds to the constructor of the attribute type, so the reference is keyed by the constructor.
        var referencedSymbol = Assert.Single( builder.ToReadOnly().ReferencedSymbols, s => s.References.Any() ).ReferencedSymbol;

        Assert.Equal( "MarkAttribute", referencedSymbol.ContainingType.Name );
        Assert.Equal( Microsoft.CodeAnalysis.MethodKind.Constructor, ( (IMethodSymbol) referencedSymbol ).MethodKind );
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

    /// <summary>
    /// Verifies that the stage is restricted to the declaration roots when every consumer returns roots, and that only the syntax tree of the roots
    /// is bound.
    /// </summary>
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

    /// <summary>
    /// Verifies that the stage is not restricted to the declaration roots when one consumer returns no root, so that the index contains references
    /// outside of the roots of the other consumer.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="SourceReferenceIndexStage.MergeRoots"/> removes a duplicate root and a root that is contained in another root.
    /// </summary>
    [Fact]
    public void NestedRoots_WalkedOnce()
    {
        using var context = this.CreateContext( _code );
        var typeB = GetTypeSyntax( context.Compilation, "B" );
        var methodOfB = GetMethodSyntax( context.Compilation, "B", "M" );

        var merged = SourceReferenceIndexStage.MergeRoots( [methodOfB, typeB, typeB] );

        Assert.Same( typeB, Assert.Single( Assert.Single( merged ).Value ) );
    }

    /// <summary>
    /// Verifies that a stage without requirements returns an empty index and resolves no semantic model.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="SourceReferenceIndexStage.GetIndexAsync"/> throws an <see cref="ObjectDisposedException"/> after the stage is
    /// disposed.
    /// </summary>
    [Fact]
    public async Task Stage_DisposedThrows()
    {
        using var context = this.CreateContext( _code );

        var stage = SourceReferenceIndexService.BeginStage( context.ServiceProvider, context.Compilation, [Requirements( context.Compilation, "F" )] );
        stage.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>( () => stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken ) );
    }

    /// <summary>
    /// Verifies that <see cref="SourceReferenceIndexService.GetDesignTimeIndex"/> returns the same index for two calls with the same semantic model.
    /// </summary>
    [Fact]
    public void DesignTimeIndex_OnePerSemanticModel()
    {
        using var context = this.CreateContext( _code );
        var semanticModel = context.Compilation.RoslynCompilation.GetSemanticModel( context.Compilation.RoslynCompilation.SyntaxTrees.First() );

        var index1 = SourceReferenceIndexService.GetDesignTimeIndex( context.ServiceProvider, semanticModel, DesignTimeAspectPipelineResultExtensionCollection.Empty, Xunit.TestContext.Current.CancellationToken );
        var index2 = SourceReferenceIndexService.GetDesignTimeIndex( context.ServiceProvider, semanticModel, DesignTimeAspectPipelineResultExtensionCollection.Empty, Xunit.TestContext.Current.CancellationToken );

        Assert.Same( index1, index2 );
    }

    /// <summary>
    /// Verifies that a root that is the declarator of a field or the expression body of a property is walked, and that its reference is attributed
    /// to the field or to the getter of the property.
    /// </summary>
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

    /// <summary>
    /// Verifies that a type root includes the members, the nested types and the lambdas of the type.
    /// </summary>
    [Fact]
    public void Root_TypeIncludesNestedTypesAndLambdas()
    {
        using var context = this.CreateContext( _code );

        var index = IndexRoots( context, GetTypeSyntax( context.Compilation, "B" ), "F", "G" );

        Assert.Equal( ["B.M()", "B.Nested.N()", "B.Property.get", "B._field"], GetReferencingNames( index ) );
    }

    /// <summary>
    /// Verifies that the reference in a compilation unit that contains top-level statements is indexed when the compilation unit is the root.
    /// </summary>
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

    /// <summary>
    /// Verifies that the index is not built while the stage holds its lock. A build of a single syntax tree runs synchronously in the call that
    /// starts it, so a build under the lock would block the other extensions and <see cref="SourceReferenceIndexStage.Dispose"/>.
    /// </summary>
    [Fact]
    public async Task GetIndex_BuildsOutsideOfLock()
    {
        var observer = new LockObserver();
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( observer );
        additionalServices.AddProjectService( SymbolClassificationService.CreateTestInstance() );
        using var testContext = this.CreateTestContext( additionalServices );
        var compilation = testContext.CreateCompilationModel( testContext.CreateCSharpCompilation( _code ) );

        using var stage = SourceReferenceIndexService.BeginStage(
            testContext.ServiceProvider,
            compilation,
            [Requirements( compilation, "F" ) with { DeclarationRoots = [GetMethodSyntax( compilation, "C", "M" )] }] );

        observer.Lock = typeof(SourceReferenceIndexStage).GetField( "_sync", BindingFlags.Instance | BindingFlags.NonPublic )!.GetValue( stage );

        await stage.GetIndexAsync( Xunit.TestContext.Current.CancellationToken );

        Assert.True( observer.WasCalled );
        Assert.False( observer.WasLockHeld );
    }

    /// <summary>
    /// Verifies that a declaration root of a syntax tree that is not part of the source compilation is refused when the stage begins, and not
    /// when a task of the build binds it.
    /// </summary>
    [Fact]
    public void BeginStage_RootOfForeignTree_Throws()
    {
        using var context = this.CreateContext( _code );
        var foreignRoot = CSharpSyntaxTree.ParseText( "class D { void M() { } }", cancellationToken: Xunit.TestContext.Current.CancellationToken )
            .GetRoot( Xunit.TestContext.Current.CancellationToken );

        Assert.Throws<ArgumentException>(
            () => SourceReferenceIndexService.BeginStage(
                context.ServiceProvider,
                context.Compilation,
                [Requirements( context.Compilation, "F" ) with { DeclarationRoots = [foreignRoot] }] ) );
    }

    /// <summary>
    /// Verifies that a method root includes only the references in that method.
    /// </summary>
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

    /// <summary>
    /// Verifies that a reference in an argument of the base type of a type with a primary constructor is attributed to the type.
    /// </summary>
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

    /// <summary>
    /// Verifies that a namespace root includes the types of the namespace and of its nested namespaces, and not the types outside of the namespace.
    /// </summary>
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
    /// An observer that records whether the thread that resolves a semantic model holds the lock of the stage.
    /// </summary>
    private sealed class LockObserver : IReferenceIndexObserver
    {
        /// <summary>
        /// Gets or sets the lock of the stage.
        /// </summary>
        public object? Lock { get; set; }

        /// <summary>
        /// Gets a value indicating whether a semantic model was resolved.
        /// </summary>
        public bool WasCalled { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a thread resolved a semantic model while it held <see cref="Lock"/>.
        /// </summary>
        public bool WasLockHeld { get; private set; }

        /// <inheritdoc />
        public void OnSymbolResolved( ISymbol symbol ) { }

        /// <summary>
        /// Records that a semantic model was resolved, and whether the current thread holds <see cref="Lock"/>.
        /// </summary>
        public void OnSemanticModelResolved( SemanticModel semanticModel )
        {
            this.WasCalled = true;
            this.WasLockHeld |= this.Lock != null && Monitor.IsEntered( this.Lock );
        }
    }

    /// <summary>
    /// A design-time result that requests the invocations of one method name.
    /// </summary>
    private sealed class RequirementsProvider : IDesignTimePipelineResultExtension, IDesignTimeReferenceIndexRequirementsProvider
    {
        /// <summary>
        /// The kind of <see cref="RequirementsProvider"/>.
        /// </summary>
        private static readonly ContributorKind<RequirementsProvider> _kind = new( nameof(RequirementsProvider) );

        /// <summary>
        /// Initializes a new instance of the <see cref="RequirementsProvider"/> class that requests the invocations of the methods of the given name.
        /// </summary>
        public RequirementsProvider( string methodName )
        {
            this.ReferenceIndexerRequirements = [new ReferenceIndexerRequirements( ReferenceKinds.Invocation, false, DeclarationKind.Method, methodName )];
        }

        /// <inheritdoc />
        public ContributorKind ContributorKind => _kind;

        /// <inheritdoc />
        public IEnumerable<ReferenceIndexerRequirements> ReferenceIndexerRequirements { get; }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/>, because the test does not export the result.
        /// </summary>
        public ITransitiveAspectsManifestExtension ToTransitiveAspectManifestExtension()
            => throw new InvalidOperationException( "The test does not export the result." );
    }

    /// <summary>
    /// Builds an index of the invocations of the given methods of the type <c>A</c> by walking a single declaration root.
    /// </summary>
    private static InboundReferenceIndex IndexRoots( TestContext context, SyntaxNode root, params string[] methodNames )
    {
        var options = new ReferenceIndexerOptions( Requirements( context.Compilation, methodNames ).Requirements );
        var builder = new InboundReferenceIndexBuilder( context.ServiceProvider, options, SymbolEqualityComparer.Default );
        builder.IndexDeclarationRoots( root.SyntaxTree, [root], context.Compilation.CompilationContext.SemanticModelProvider, Xunit.TestContext.Current.CancellationToken );

        return builder.ToReadOnly();
    }

    /// <summary>
    /// Returns the requirements of the invocations of the given methods of the type <c>A</c>.
    /// </summary>
    private static SourceIndexRequirements Requirements( CompilationModel compilation, params string[] methodNames )
        => new(
            compilation.Types.OfName( "A" )
                .SelectMany( t => t.Methods )
                .Where( m => methodNames.Contains( m.Name ) )
                .Select( m => ReferenceIndexerRequirements.Create( m, ReferenceKinds.Invocation, false ) )
                .ToImmutableArray() );

    /// <summary>
    /// Returns the single type declaration of the given name in the compilation.
    /// </summary>
    private static TypeDeclarationSyntax GetTypeSyntax( CompilationModel compilation, string typeName )
        => compilation.RoslynCompilation.SyntaxTrees.SelectMany( t => t.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>() )
            .Single( t => t.Identifier.Text == typeName );

    /// <summary>
    /// Returns the declaration of a method of a type, both given by name.
    /// </summary>
    private static MethodDeclarationSyntax GetMethodSyntax( CompilationModel compilation, string typeName, string methodName )
        => GetTypeSyntax( compilation, typeName ).Members.OfType<MethodDeclarationSyntax>().Single( m => m.Identifier.Text == methodName );

    /// <summary>
    /// Returns the sorted names of the symbols that have at least one reference in the index.
    /// </summary>
    private static IReadOnlyList<string> GetReferencedNames( InboundReferenceIndex index )
        => index.ReferencedSymbols.Where( s => s.References.Any() ).Select( s => s.ReferencedSymbol.Name ).ToOrderedList( x => x, StringComparer.Ordinal );

    /// <summary>
    /// Returns the sorted and distinct test names of the symbols that contain a reference in the index.
    /// </summary>
    private static IReadOnlyList<string> GetReferencingNames( InboundReferenceIndex index )
        => index.ReferencedSymbols.SelectMany( s => s.References ).Select( r => r.ReferencingSymbol.ToTestName() ).Distinct().ToOrderedList( x => x, StringComparer.Ordinal );

    /// <summary>
    /// Creates a test context that has a <see cref="ReferenceIndexObserver"/>, and the compilation model of the given code.
    /// </summary>
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
        /// <summary>
        /// The Metalama test context, which <see cref="Dispose"/> disposes.
        /// </summary>
        private readonly MetalamaTestContext _testContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestContext"/> class.
        /// </summary>
        public TestContext( MetalamaTestContext testContext, CompilationModel compilation, ReferenceIndexObserver observer )
        {
            this._testContext = testContext;
            this.Compilation = compilation;
            this.Observer = observer;
        }

        /// <summary>
        /// Gets the compilation model of the code of the test.
        /// </summary>
        public CompilationModel Compilation { get; }

        /// <summary>
        /// Gets the observer of the reference indexer.
        /// </summary>
        public ReferenceIndexObserver Observer { get; }

        /// <summary>
        /// Gets the service provider of the project.
        /// </summary>
        public ProjectServiceProvider ServiceProvider => this._testContext.ServiceProvider;

        /// <summary>
        /// Disposes the Metalama test context.
        /// </summary>
        public void Dispose() => this._testContext.Dispose();
    }
}
