// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.SerializableIds;

/// <summary>
/// Tests the properties of an <see cref="INamespace"/> resolved from a <see cref="SerializableDeclarationId"/>.
/// The resolved namespace must behave like the namespace reached through <see cref="INamedType.ContainingNamespace"/>.
/// See issue #2354.
/// </summary>
public sealed class NamespaceIdResolutionTests : UnitTestClass
{
    public NamespaceIdResolutionTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// A source file that declares the namespace <c>MyCompany.MyProject</c>.
    /// </summary>
    private const string _code = """
                                 namespace MyCompany.MyProject
                                 {
                                     public class MyClass { }
                                 }
                                 """;

    /// <summary>
    /// Verifies that a source namespace resolved from its identifier is equal to the namespace reached from a type.
    /// </summary>
    [Fact]
    public void SourceNamespaceResolvedFromIdBelongsToCurrentProject()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var viaType = compilation.Types.OfName( "MyClass" ).Single().ContainingNamespace;
        var viaId = Assert.IsAssignableFrom<INamespace>( new SerializableDeclarationId( "N:MyCompany.MyProject" ).ResolveToDeclaration( compilation ) );

        Assert.True( viaType.BelongsToCurrentProject );
        Assert.True( viaId.BelongsToCurrentProject );
        Assert.Same( compilation, viaId.DeclaringAssembly );
        Assert.Equal( viaType.Depth, viaId.Depth );
        Assert.Equal( viaType, viaId );
    }

    /// <summary>
    /// Verifies the resolution of a source namespace that a referenced assembly also declares.
    /// </summary>
    /// <remarks>
    /// Roslyn represents such a namespace in the compilation by a merged namespace symbol, which has no containing assembly.
    /// </remarks>
    [Fact]
    public void SourceNamespaceAlsoDeclaredByReferenceResolvedFromIdBelongsToCurrentProject()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel(
            _code,
            dependentCode: """
                           namespace MyCompany.MyProject
                           {
                               public class OtherClass { }
                           }
                           """ );

        var viaType = compilation.Types.OfName( "MyClass" ).Single().ContainingNamespace;
        var viaId = Assert.IsAssignableFrom<INamespace>( new SerializableDeclarationId( "N:MyCompany.MyProject" ).ResolveToDeclaration( compilation ) );

        Assert.True( viaType.BelongsToCurrentProject );
        Assert.True( viaId.BelongsToCurrentProject );
        Assert.Same( compilation, viaId.DeclaringAssembly );
        Assert.Equal( viaType.Depth, viaId.Depth );
        Assert.Equal( viaType, viaId );
    }

    /// <summary>
    /// Verifies the resolution of a source namespace through <see cref="IDeclarationFactory.GetDeclarationFromId"/>.
    /// </summary>
    [Fact]
    public void SourceNamespaceResolvedFromIdThroughFactoryBelongsToCurrentProject()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var viaId = Assert.IsAssignableFrom<INamespace>(
            compilation.Factory.GetDeclarationFromId( new SerializableDeclarationId( "N:MyCompany.MyProject" ) ) );

        Assert.True( viaId.BelongsToCurrentProject );
        Assert.Same( compilation, viaId.DeclaringAssembly );
    }

    /// <summary>
    /// Verifies that a namespace of a referenced assembly resolved from its identifier does not belong to the current project.
    /// </summary>
    [Fact]
    public void ReferencedNamespaceResolvedFromIdDoesNotBelongToCurrentProject()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var viaType = ((INamedType) compilation.Factory.GetTypeByReflectionType( typeof(object) )).ContainingNamespace;
        var viaId = Assert.IsAssignableFrom<INamespace>( new SerializableDeclarationId( "N:System" ).ResolveToDeclaration( compilation ) );

        Assert.False( viaType.BelongsToCurrentProject );
        Assert.False( viaId.BelongsToCurrentProject );
        Assert.NotSame( compilation, viaId.DeclaringAssembly );
    }
}
