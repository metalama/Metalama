// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Utilities.ObjectGraph;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Fabrics;

/// <summary>
/// Tests the three decisions of <c>UserCodeRetentionPolicy</c>, and the collection of findings built on them, against
/// object graphs constructed for the purpose and holding genuine code-model objects.
/// </summary>
/// <remarks>
/// The value of the diagnostic rests entirely on these decisions. Reporting one type too many makes it fire on every
/// project that has a fabric; reporting one too few makes a real leak invisible. Each is therefore pinned down here,
/// which is far cheaper than compiling a fabric for every case.
/// </remarks>
public sealed class UserCodeRetentionPolicyTests : UnitTestClass
{
    public UserCodeRetentionPolicyTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }

    /// <summary>
    /// A policy that treats the assembly of the current test class as compile-time user code, so that the holder types
    /// declared below play the part of a user fabric.
    /// </summary>
    private static UserCodeRetentionPolicy CreatePolicy()
        => new( ImmutableHashSet.Create( StringComparer.OrdinalIgnoreCase, typeof(UserCodeRetentionPolicyTests).Assembly.GetName().Name! ) );

    private static UserCodeRetentionPolicy CreateEmptyPolicy() => new( ImmutableHashSet<string>.Empty );

#pragma warning disable SA1401

    private sealed class Holder
    {
        public object? Value;
        public object? Other;
    }

    private sealed class Indirection
    {
        public Holder Holder = new();
    }

#pragma warning restore SA1401

    private static IReadOnlyList<UserCodeRetentionAnalyzer.Finding> FindRetentions( object root, UserCodeRetentionPolicy? policy = null )
    {
        UserCodeRetentionAnalyzer.FindRetentions(
            [("root", root)],
            new HashSet<object>(),
            policy ?? CreatePolicy(),
            out var findings );

        return findings;
    }

    [Theory]
    [InlineData( "compilation" )]
    [InlineData( "syntaxTree" )]
    [InlineData( "semanticModel" )]
    [InlineData( "symbol" )]
    [InlineData( "syntaxNode" )]
    [InlineData( "compilationModel" )]
    [InlineData( "compilationContext" )]
    [InlineData( "namedType" )]
    [InlineData( "method" )]
    [InlineData( "fullRef" )]
    [InlineData( "boundDurableRef" )]
    public void PinningObject_IsReported( string kind )
    {
        using var testContext = this.CreateTestContext(
            new TestContextOptions { DurableRefKind = kind == "boundDurableRef" ? DurableRefKind.Bound : DurableRefKind.Default } );

        var compilationModel = testContext.CreateCompilationModel( "class C { void M() { } }" );
        var type = compilationModel.Types.OfName( "C" ).Single();

        object pinning = kind switch
        {
            "compilation" => compilationModel.RoslynCompilation,
            "syntaxTree" => compilationModel.RoslynCompilation.SyntaxTrees.First(),
            "semanticModel" => compilationModel.RoslynCompilation.GetSemanticModel( compilationModel.RoslynCompilation.SyntaxTrees.First() ),
            "symbol" => type.GetSymbol()!,
            "syntaxNode" => compilationModel.RoslynCompilation.SyntaxTrees.First().GetRoot(),
            "compilationModel" => compilationModel,
            "compilationContext" => compilationModel.CompilationContext,
            "namedType" => type,
            "method" => type.Methods.OfName( "M" ).Single(),
            "fullRef" => type.ToRef(),

            // During a batch compilation, a durable reference stores the reference it was created from, because the
            // compilation lives until the build ends. This analysis reproduces the design-time object graph during a
            // build, so it must report such a reference as holding a compilation. See issue #1811.
            "boundDurableRef" => type.ToRef().ToDurable(),
            _ => throw new ArgumentOutOfRangeException( nameof(kind) )
        };

        Assert.True( UserCodeRetentionPolicy.IsPinning( pinning ), $"'{kind}' should have been reported as pinning." );
    }

    /// <summary>
    /// Verifies that an identifier-based durable reference is not reported, which is the whole point of making a
    /// reference durable.
    /// </summary>
    /// <remarks>
    /// This test is the negative counterpart of the <c>boundDurableRef</c> case of
    /// <see cref="PinningObject_IsReported"/>. Both are required: an analysis that reported every durable reference
    /// would be as inaccurate as one that reported none, and a single property distinguishes the two representations.
    /// </remarks>
    [Theory]
    [InlineData( DurableRefKind.Serializable )]
    [InlineData( DurableRefKind.SerializableWithoutCache )]
    public void SerializableDurableRef_IsNotReported( DurableRefKind kind )
    {
        using var testContext = this.CreateTestContext( new TestContextOptions { DurableRefKind = kind } );
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );

        var durableRef = compilationModel.Types.OfName( "C" ).Single().ToRef().ToDurable();

        Assert.False( UserCodeRetentionPolicy.IsPinning( durableRef ) );
    }

    [Fact]
    public void MetadataSymbol_IsNotReported()
    {
        // The symbols of a referenced assembly hang off a reference manager that Roslyn shares between compilations.
        // They have no declaring compilation and keep nothing alive. This matters more than it looks: the template
        // members of every aspect that comes from a package hold the parameter types of their templates, so reporting
        // every symbol buries the few findings that matter under dozens that cannot be acted upon.
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );

        var metadataSymbol = compilationModel.RoslynCompilation.GetTypeByMetadataName( "System.String" )!;

        Assert.False( UserCodeRetentionPolicy.IsPinning( metadataSymbol ) );
        Assert.False( UserCodeRetentionPolicy.IsPinning( compilationModel.RoslynCompilation.DynamicType ) );
    }

    [Fact]
    public void SourceSymbol_IsReported()
    {
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );

        var sourceSymbol = compilationModel.RoslynCompilation.GetTypeByMetadataName( "C" )!;

        Assert.True( UserCodeRetentionPolicy.IsPinning( sourceSymbol ) );
    }

    [Fact]
    public void MetadataSymbolConstructedOverASourceType_IsReported()
    {
        // The type is declared in metadata but reaches a source symbol through its type arguments, so stopping at the
        // declaring assembly alone would miss it.
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );
        var roslynCompilation = compilationModel.RoslynCompilation;

        var listOfSource = roslynCompilation.GetTypeByMetadataName( "System.Collections.Generic.List`1" )!
            .Construct( roslynCompilation.GetTypeByMetadataName( "C" )! );

        var listOfString = roslynCompilation.GetTypeByMetadataName( "System.Collections.Generic.List`1" )!
            .Construct( roslynCompilation.GetTypeByMetadataName( "System.String" )! );

        Assert.True( UserCodeRetentionPolicy.IsPinning( listOfSource ) );
        Assert.False( UserCodeRetentionPolicy.IsPinning( listOfString ) );
    }

    [Fact]
    public void LoaderAllocatorAndRuntimeInternals_AreBoundaries()
    {
        // A delegate to a dynamic method holds a LoaderAllocator in its _methodBase field, and that object references
        // everything allocated in its load context. Following it turns any delegate into a route to arbitrary unrelated
        // objects and produces a chain naming a dozen types the user has never heard of.
        var method = new DynamicMethod( "Test", typeof(int), [] );
        var il = method.GetILGenerator();
        il.Emit( OpCodes.Ldc_I4_1 );
        il.Emit( OpCodes.Ret );
        var dynamicDelegate = (Func<int>) method.CreateDelegate( typeof(Func<int>) );

        var holder = new Holder { Value = dynamicDelegate };
        var visited = new List<string>();

        new ObjectGraphWalker().Walk(
            [("root", holder)],
            node =>
            {
                visited.Add( node.Object.GetType().FullName ?? "" );

                return UserCodeRetentionPolicy.IsBoundary( node.Object ) ? ObjectGraphAction.Skip : ObjectGraphAction.Traverse;
            } );

        Assert.Equal( 1, dynamicDelegate() );

        // The delegate itself is a handful of objects. Escaping through the load context is not a matter of a few extra
        // hops: it reaches everything the context ever allocated.
        Assert.True(
            visited.Count < 100,
            $"The walk visited {visited.Count} objects from a single delegate, which means it escaped through a runtime internal." );
    }

    [Fact]
    public void TemplateReflectionContext_IsABoundary()
    {
        // The cacheable template reflection context owns the compilation against which the templates of a referenced
        // assembly are reflected. That compilation has no syntax tree and only portable executable references, so the
        // framework keeps it deliberately, for the whole session. A fabric reaches it through the template class of its
        // own aspect, therefore a walk that descended into it would report a compilation the user did not create and
        // cannot release, and would name the fabric as the cause.
        using var testContext = this.CreateTestContext();

        var provider = new CacheableTemplateDiscoveryContextProvider(
            testContext.CreateCSharpCompilation( "class C { }" ),
            testContext.ServiceProvider );

        // Without this call the provider concludes that no reference contains compile-time code and creates no context.
        provider.OnPortableExecutableReferenceDiscovered();

        var context = provider.GetTemplateDiscoveryContext()!;

        Assert.True( UserCodeRetentionPolicy.IsBoundary( context ) );
        Assert.Empty( FindRetentions( new Holder { Value = context } ) );
    }

    [Fact]
    public void SourceCompilationContext_IsStillReported()
    {
        // The pair of the case above, and the reason why the boundary is safe: the compilation context of the source
        // compilation also implements the template reflection context interface, but it pins a compilation the user
        // does edit, so it must keep being reported.
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );
        var compilationContext = compilationModel.CompilationContext;

        Assert.IsAssignableFrom<ITemplateReflectionContext>( compilationContext );
        Assert.True( UserCodeRetentionPolicy.IsPinning( compilationContext ) );

        var finding = Assert.Single( FindRetentions( new Holder { Value = compilationContext } ) );

        Assert.Same( compilationContext, finding.Node.Object );
    }

    [Fact]
    public void DurableReference_IsNotReported()
    {
        // This is the pair of the case above, and the one that makes the recommended fix verifiable: a reference that
        // has been made durable must stop being reported, otherwise the advice in the diagnostic would be useless.
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );

        var durableRef = compilationModel.Types.OfName( "C" ).Single().ToRef().ToDurable();

        Assert.False( UserCodeRetentionPolicy.IsPinning( durableRef ) );
    }

    [Fact]
    public void SerializableIdentifiersAndStrings_AreNotReported()
    {
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );
        var type = compilationModel.Types.OfName( "C" ).Single();

        Assert.False( UserCodeRetentionPolicy.IsPinning( type.ToSerializableId() ) );
        Assert.False( UserCodeRetentionPolicy.IsPinning( type.ToSerializableId().Id ) );
        Assert.False( UserCodeRetentionPolicy.IsPinning( type.FullName ) );
        Assert.False( UserCodeRetentionPolicy.IsPinning( 42 ) );
    }

    [Fact]
    public void LocationAndDiagnostic_AreNotClassifiedByTheirType()
    {
        // Whether a location or a diagnostic pins anything depends on what it holds, so neither is classified as
        // pinning by its type. The tests below assert what the walk finds inside them instead.
        Assert.False( UserCodeRetentionPolicy.IsPinning( Location.None ) );
        Assert.False( UserCodeRetentionPolicy.IsPinning( Location.Create( "file.cs", default, default ) ) );
    }

    [Fact]
    public void SourceLocation_ReportsTheSyntaxTreeItHolds()
    {
        // Reporting the location itself would name the container rather than the object that is retained, and would
        // stop the walk before the tree, which is what actually holds the text and the green nodes.
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );
        var syntaxTree = compilationModel.RoslynCompilation.SyntaxTrees.First();
        var holder = new Holder { Value = syntaxTree.GetRoot().GetLocation() };

        var finding = Assert.Single( FindRetentions( holder ) );

        Assert.Same( syntaxTree, finding.Node.Object );
        Assert.Equal( "root -> Value -> _syntaxTree", string.Join( " -> ", finding.Node.GetPath().SelectAsArray( n => n.Label ) ) );
    }

    [Fact]
    public void LocationWithoutSource_IsNotReported()
    {
        var holder = new Holder { Value = Location.None, Other = Location.Create( "file.cs", default, default ) };

        Assert.Empty( FindRetentions( holder ) );
    }

    [Fact]
    public void DiagnosticWithoutSourceOrArguments_IsNotReported()
    {
        // A diagnostic reported without a location and with no compilation-bound argument reaches nothing. Classifying
        // every diagnostic as pinning would make this a false positive.
        var holder = new Holder { Value = CreateDiagnostic( Location.None ) };

        Assert.Empty( FindRetentions( holder ) );
    }

    [Fact]
    public void DiagnosticOnASourceLocation_ReportsTheSyntaxTree()
    {
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );
        var syntaxTree = compilationModel.RoslynCompilation.SyntaxTrees.First();
        var holder = new Holder { Value = CreateDiagnostic( syntaxTree.GetRoot().GetLocation() ) };

        var finding = Assert.Single( FindRetentions( holder ) );

        Assert.Same( syntaxTree, finding.Node.Object );
    }

    [Fact]
    public void DiagnosticWithADeclarationArgument_ReportsTheDeclaration()
    {
        // A diagnostic whose message argument was not materialized holds the declaration it was formatted from. The
        // walk finds it inside the argument array, which is the part that has to be fixed.
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );
        var type = compilationModel.Types.OfName( "C" ).Single();
        var holder = new Holder { Value = CreateDiagnostic( Location.None, type ) };

        var finding = Assert.Single( FindRetentions( holder ) );

        Assert.Same( type, finding.Node.Object );
    }

    private static Diagnostic CreateDiagnostic( Location location, params object[] arguments )
        => Diagnostic.Create(
            new DiagnosticDescriptor( "TEST0001", "Test", "message {0}", "Test", DiagnosticSeverity.Warning, true ),
            location,
            arguments );

    [Fact]
    public void ServiceProviderAndCompileTimeProject_AreBoundaries()
    {
        using var testContext = this.CreateTestContext();

        Assert.True( UserCodeRetentionPolicy.IsBoundary( testContext.ServiceProvider.Underlying ) );
        Assert.True( UserCodeRetentionPolicy.IsBoundary( "a string" ) );
        Assert.True( UserCodeRetentionPolicy.IsBoundary( typeof(int) ) );
        Assert.True( UserCodeRetentionPolicy.IsBoundary( typeof(int).Assembly ) );
        Assert.False( UserCodeRetentionPolicy.IsBoundary( new Holder() ) );
    }

    [Fact]
    public void CompilationReachedOnlyThroughAServiceProvider_IsNotReported()
    {
        // The service provider is a project-scoped object shared by every component. A chain through it explains
        // nothing about the fabric, and following it would make the walk explore the whole engine.
        using var testContext = this.CreateTestContext();
        var holder = new Holder { Value = testContext.ServiceProvider.Underlying };

        Assert.Empty( FindRetentions( holder ) );
    }

    [Fact]
    public void DeclarationHeldByUserType_IsAttributedToThatType()
    {
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );
        var holder = new Holder { Value = compilationModel.Types.OfName( "C" ).Single() };

        var findings = FindRetentions( holder );

        var finding = Assert.Single( findings );
        Assert.NotNull( finding.UserType );
        Assert.Contains( nameof(Holder), finding.UserType! );
        Assert.Equal( "Value", finding.Node.Label );
    }

    [Fact]
    public void DeclarationHeldByFrameworkTypeOnly_IsAttributedToTheFramework()
    {
        // The same graph analysed with a policy that knows of no user assembly must classify the finding as belonging
        // to Metalama, which is what keeps a clean project free of warnings.
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );
        var holder = new Holder { Value = compilationModel.Types.OfName( "C" ).Single() };

        var finding = Assert.Single( FindRetentions( holder, CreateEmptyPolicy() ) );

        Assert.Null( finding.UserType );
    }

    [Fact]
    public void SameDeclarationHeldTwice_ProducesOneFinding()
    {
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );
        var type = compilationModel.Types.OfName( "C" ).Single();
        var holder = new Holder { Value = type, Other = type };

        Assert.Single( FindRetentions( holder ) );
    }

    [Fact]
    public void DeclarationReachableByTwoPaths_ReportsTheShortestOne()
    {
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );
        var type = compilationModel.Types.OfName( "C" ).Single();

        var indirection = new Indirection();
        indirection.Holder.Value = type;

        var root = new Holder { Value = type, Other = indirection };

        var finding = Assert.Single( FindRetentions( root ) );

        Assert.Equal( 1, finding.Node.Depth );
        Assert.Equal( "root -> Value", string.Join( " -> ", finding.Node.GetPath().SelectAsArray( n => n.Label ) ) );
    }

    [Fact]
    public void DeclarationNestedInUserObjects_ReportsTheWholeChain()
    {
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );

        var indirection = new Indirection();
        indirection.Holder.Value = compilationModel.Types.OfName( "C" ).Single();

        var finding = Assert.Single( FindRetentions( indirection ) );

        Assert.Equal( "root -> Holder -> Value", string.Join( " -> ", finding.Node.GetPath().SelectAsArray( n => n.Label ) ) );
        Assert.Contains( nameof(Holder), finding.UserType! );
    }

    [Fact]
    public void UserCodeRoot_AttributesAFindingWithoutAUserTypeOnItsChain()
    {
        // This is the shape of a static field of a compile-time type whose value is a code-model object: no hop of the
        // chain is a user type, yet the retention is the user's.
        using var testContext = this.CreateTestContext();
        var compilationModel = testContext.CreateCompilationModel( "class C { }" );
        var type = compilationModel.Types.OfName( "C" ).Single();

        UserCodeRetentionAnalyzer.FindRetentions(
            [("static field 'MyCache.Type'", type)],
            new HashSet<object> { type },
            CreateEmptyPolicy(),
            out var findings );

        var finding = Assert.Single( findings );
        Assert.Equal( "static field 'MyCache.Type'", finding.UserType );
    }

    [Fact]
    public void ClosureType_IsAttributedToItsDeclaringType()
    {
        // A lambda declared in a user type compiles to a nested type with a compiler-generated name. Reporting that
        // name would tell the user nothing, so the outermost declaring type is reported instead.
        var policy = CreatePolicy();

        Func<object> closure = () => new Holder();
        var closureType = closure.Target!.GetType();

        Assert.Contains( '<', closureType.Name );
        Assert.DoesNotContain( '<', policy.GetUserCodeTypeName( closureType )! );
        Assert.Contains( nameof(UserCodeRetentionPolicyTests), policy.GetUserCodeTypeName( closureType )! );
    }

    [Fact]
    public void TypeOfAnUnknownAssembly_IsNotUserCode()
    {
        Assert.Null( CreatePolicy().GetUserCodeTypeName( typeof(CSharpCompilation) ) );
        Assert.Null( CreatePolicy().GetUserCodeTypeName( typeof(CompilationModel) ) );
    }
}
