// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.SerializableIds;

/// <summary>
/// Tests the resolution of a <see cref="SerializableDeclarationId"/> that designates a parameter of a constructor which
/// an aspect extended with pulled parameters.
/// </summary>
/// <remarks>
/// <para>
/// The identifier of such a constructor is deliberately written from its pre-transformation signature, as documented on
/// <c>AspectGeneratedAttribute</c>, so that a consumer can resolve it whether or not the transformation is visible. The
/// parser implements that by excluding the parameters that carry the attribute from the count it compares, therefore
/// one identifier matches both the source constructor and the extended one.
/// </para>
/// <para>
/// The ordinal that follows the identifier, however, indexes the full parameter list of the declaration the identifier
/// was made from. An ordinal beyond the source parameter count is satisfied only by the extended constructor, but an
/// ordinal within it is satisfied by both, so the resolution has to define which one it returns. See the review of
/// pull request #1784.
/// </para>
/// </remarks>
public sealed class ConstructorParameterIdResolutionTests : UnitTestClass
{
    public ConstructorParameterIdResolutionTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// A type whose constructor exists in the two shapes that coexist at design time: the one written by the user, and
    /// the overload that the pull of a constructor parameter adds beside it rather than in its place.
    /// </summary>
    private const string _code = """
                                 using Metalama.Framework.RunTime;

                                 public class C
                                 {
                                     public C( string s ) { }

                                     public C( string s, [AspectGenerated] int p1 = 20 ) : this( s ) { }
                                 }
                                 """;

    /// <summary>
    /// Returns the two constructors of <c>C</c>, the source one first.
    /// </summary>
    private static (IConstructor Source, IConstructor Extended) GetConstructors( ICompilation compilation )
    {
        var constructors = compilation.Types.OfName( "C" ).Single().Constructors.OrderBy( c => c.Parameters.Count ).ToArray();

        Assert.Equal( 2, constructors.Length );

        return (constructors[0], constructors[1]);
    }

    /// <summary>
    /// Verifies that the identifier written from the source signature matches both constructors.
    /// </summary>
    /// <remarks>
    /// This is the parser rule that makes the ordinal the only thing that can tell the two apart. The identifier is
    /// built here from the source constructor, which is what the writer produces for a constructor whose extra
    /// parameters were introduced by an aspect: the writer omits introduced parameters, and the parser excludes the
    /// parameters that carry <c>AspectGeneratedAttribute</c> from the count it compares.
    /// </remarks>
    [Fact]
    public void TheSourceIdentifierMatchesBothConstructors()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var (source, _) = GetConstructors( compilation );

        var candidates = DocumentationIdHelper.GetDeclarationsForDeclarationId( source.ToSerializableId().Id, compilation );

        Assert.Equal( 2, candidates.Count );
    }

    /// <summary>
    /// Verifies that an ordinal which only the extended constructor declares resolves to the pulled parameter, rather
    /// than to nothing because the source constructor happened to be examined first.
    /// </summary>
    /// <remarks>
    /// This is the regression guard for the second defect fixed by pull request #1784, which threw
    /// <c>ArgumentOutOfRangeException</c> here.
    /// </remarks>
    [Fact]
    public void OrdinalDeclaredOnlyByTheExtendedConstructorResolvesToThePulledParameter()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var (source, _) = GetConstructors( compilation );

        var id = new SerializableDeclarationId( $"{source.ToSerializableId().Id};Parameter;1" );

        var parameter = Assert.IsAssignableFrom<IParameter>( id.ResolveToDeclaration( compilation ) );
        Assert.Equal( "p1", parameter.Name );
        Assert.Equal( 2, parameter.DeclaringMember.Parameters.Count );
    }

    /// <summary>
    /// Verifies that an ordinal declared by neither constructor resolves to <c>null</c> instead of throwing.
    /// </summary>
    /// <remarks>
    /// This is the regression guard for the <c>GetAtOrNull</c> change of pull request #1784.
    /// </remarks>
    [Fact]
    public void OrdinalDeclaredByNoConstructorResolvesToNull()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var (source, _) = GetConstructors( compilation );

        var id = new SerializableDeclarationId( $"{source.ToSerializableId().Id};Parameter;7" );

        Assert.Null( id.ResolveToDeclaration( compilation ) );
    }

    /// <summary>
    /// Verifies that an ordinal which both constructors declare resolves to the source constructor, which is the
    /// declaration the identifier describes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The identifier is documented as the pre-transformation identifier of the member, so the declaration it names is
    /// the one without aspect-generated parameters. Resolution currently returns the first candidate that declares the
    /// ordinal, in an order that <c>DocumentationIdHelper.GetDeclarationsForDeclarationId</c> documents as undefined,
    /// so which of the two constructors is returned is not specified.
    /// </para>
    /// <para>
    /// The distinction is observable: the two parameters have the same name and type but different declaring members,
    /// so a caller that navigates from the resolved parameter to its constructor reaches a different declaration
    /// depending on an ordering that is not defined.
    /// </para>
    /// </remarks>
    [Fact]
    public void OrdinalDeclaredByBothConstructorsResolvesToTheSourceConstructor()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var (source, _) = GetConstructors( compilation );

        var resolved = source.Parameters[0].ToSerializableId().ResolveToDeclaration( compilation );

        var parameter = Assert.IsAssignableFrom<IParameter>( resolved );
        Assert.Equal( "s", parameter.Name );

        Assert.Single( parameter.DeclaringMember.Parameters );
    }

    /// <summary>
    /// Verifies that resolving one identifier twice returns the same declaration.
    /// </summary>
    /// <remarks>
    /// Determinism is the weaker property that holds whichever candidate the resolution selects, and it is asserted
    /// separately so that a failure distinguishes an unstable result from a stable but unintended one.
    /// </remarks>
    [Fact]
    public void ResolutionOfAnAmbiguousIdentifierIsStable()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        var (source, _) = GetConstructors( compilation );
        var id = source.Parameters[0].ToSerializableId();

        var first = Assert.IsAssignableFrom<IParameter>( id.ResolveToDeclaration( compilation ) );
        var second = Assert.IsAssignableFrom<IParameter>( id.ResolveToDeclaration( compilation ) );

        Assert.Equal( first.DeclaringMember.Parameters.Count, second.DeclaringMember.Parameters.Count );
    }
}
