// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization;

/// <summary>
/// Verifies that a durable reference of a batch compilation, which stores the reference it was created from instead of
/// an identifier, is written to the compile-time stream as an identifier and is read back as an identifier-based
/// reference.
/// </summary>
/// <remarks>
/// Retention and serialization are two distinct requirements, and these tests cover the boundary between them. A
/// reference written with an incorrect identifier is still deserialized without error, and the defect appears only in
/// the consuming project, which resolves the reference to a different declaration. See issue #1811.
/// </remarks>
public sealed class DurableRefSerializationTests : SerializationTestsBase
{
    private const string _code = """
                                 class Plain
                                 {
                                     public int M( string p ) => 0;
                                 }

                                 class Generic<T> { }

                                 class Container
                                 {
                                     public Generic<int> ConstructedField = null!;
                                 }
                                 """;

    protected override TestContext CreateTestContextCore( TestContextOptions contextOptions, IAdditionalServiceCollection services )
        => base.CreateTestContextCore( contextOptions with { DurableRefKind = DurableRefKind.Live }, services );

    /// <summary>
    /// Verifies that a durable reference of a batch compilation writes the identifier that the identifier-based
    /// reference to the same declaration would have carried.
    /// </summary>
    [Theory]
    [InlineData( "Type" )]
    [InlineData( "Method" )]
    [InlineData( "Parameter" )]
    [InlineData( "ConstructedType" )]
    public void ALiveDurableRefWritesTheIdentifierOfItsIdentifierEquivalent( string kind )
    {
        using var testContext = this.CreateTestContextWithCode( _code );

        var reference = GetReference( testContext, kind );
        var durableRef = (IDurableRefImpl) reference.ToDurable();

        Assert.True( durableRef.ReachesCompilation, "The project is a batch compilation, so its durable references hold the compilation." );

        var expected = (IDurableRefImpl) SerializableDurableRefFactory.Instance.FromFullRef( (IFullRef<ICompilationElement>) reference );

        Assert.Equal( expected.Id, durableRef.Id );
    }

    /// <summary>
    /// Verifies that a deserialized durable reference is identifier-based, whichever project wrote it, and that it
    /// resolves to the same declaration.
    /// </summary>
    /// <remarks>
    /// A reference is read in a compilation other than the one that wrote it. A reference that stored the compilation
    /// of the writing project would therefore be unusable, and would keep that compilation in memory for as long as
    /// the reading project holds it.
    /// </remarks>
    [Theory]
    [InlineData( "Type" )]
    [InlineData( "Method" )]
    [InlineData( "Parameter" )]
    [InlineData( "ConstructedType" )]
    public void ADeserializedDurableRefIsIdentifierBased( string kind )
    {
        using var testContext = this.CreateTestContextWithCode( _code );

        var original = GetReference( testContext, kind ).ToDurable();
        var expectedTarget = original.GetTargetInterface( testContext.Compilation, null, null, true );

        var deserialized = (IDurableRefImpl) SerializeDeserialize( (IRef) original, testContext );

        Assert.False( deserialized.ReachesCompilation );
        Assert.Equal( ((IDurableRefImpl) original).Id, deserialized.Id );

        Assert.Same( expectedTarget, deserialized.GetTargetInterface( testContext.Compilation, null, null, true ) );
    }

    /// <summary>
    /// Verifies that a constructed generic type keeps its type arguments across the stream.
    /// </summary>
    /// <remarks>
    /// This is the serialization counterpart of the defect reported as issue #1797. A declaration identifier names the
    /// generic definition, so writing one would turn <c>Generic&lt;int&gt;</c> into <c>Generic&lt;T&gt;</c> silently,
    /// and the reference would still resolve to a usable type.
    /// </remarks>
    [Fact]
    public void AConstructedGenericTypeKeepsItsTypeArgumentsAcrossTheStream()
    {
        using var testContext = this.CreateTestContextWithCode( _code );

        var reference = (IRef<INamedType>) GetReference( testContext, "ConstructedType" );
        Assert.Equal( "Generic<int>", reference.GetTarget( testContext.Compilation ).ToDisplayString() );

        var deserialized = SerializeDeserialize( (IRef<INamedType>) reference.ToDurable(), testContext );

        Assert.IsType<TypeIdRef<INamedType>>( deserialized );
        Assert.Equal( "Generic<int>", deserialized.GetTarget( testContext.Compilation ).ToDisplayString() );
    }

    /// <summary>
    /// Returns the reference denoted by a kind name in a compilation of <see cref="_code"/>.
    /// </summary>
    private static IRef GetReference( SerializationTestContext testContext, string kind )
    {
        var compilation = testContext.Compilation;
        var type = compilation.Types.OfName( "Plain" ).Single();
        var method = type.Methods.OfName( "M" ).Single();

        return kind switch
        {
            "Type" => type.ToRef(),
            "Method" => method.ToRef(),
            "Parameter" => method.Parameters[0].ToRef(),
            "ConstructedType" => compilation.Types.OfName( "Container" ).Single().Fields.OfName( "ConstructedField" ).Single().Type.ToRef(),
            _ => throw new AssertionFailedException( $"Unknown kind '{kind}'." )
        };
    }
}
