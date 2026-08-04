// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class RefTests : UnitTestClass
{
    [Fact]
    public void CompilationRef()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var compilationRef = compilation.ToRef();
        var resolved = compilationRef.GetTarget( compilation );

        Assert.Same( compilation, resolved );
    }

    [Fact]
    public void CompilationSymbolId()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );
        var symbolId = SymbolId.Create( compilation.Symbol );
        var resolvedSymbol = symbolId.Resolve( compilation.RoslynCompilation ).AssertNotNull();
        var resolvedDeclaration = compilation.Factory.GetCompilationElement( resolvedSymbol );

        Assert.Same( compilation, resolvedDeclaration );
    }

    [Fact]
    public void ReferencedAssemblySymbol()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var assemblyRefSymbol = compilation.Factory.GetTypeByReflectionType( typeof(string) ).GetSymbol();
        var assemblyRefRef = SymbolId.Create( assemblyRefSymbol );
        _ = assemblyRefRef.Resolve( compilation.RoslynCompilation );
    }

    /// <summary>
    /// A durable reference is not bound to a compilation, but it still has to answer
    /// <see cref="Engine.CodeModel.References.RefExtensions.GetPrimarySyntaxTree(IRef, CompilationContext)"/> with the same tree the equivalent
    /// full reference gives (issue #1748).
    /// </summary>
    [Fact]
    public void GetPrimarySyntaxTreeOfDurableRefInSource()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class C { }" );

        var fullRef = compilation.Types.OfName( "C" ).Single().ToRef();
        var durableRef = fullRef.ToDurable();

        Assert.False( durableRef is IFullRef, "The reference is expected not to be bound to a compilation." );

        var expected = fullRef.GetPrimarySyntaxTree( compilation.CompilationContext );
        Assert.NotNull( expected );

        Assert.Same( expected, durableRef.GetPrimarySyntaxTree( compilation.CompilationContext ) );
    }

    /// <summary>
    /// A durable reference to a declaration of a referenced assembly has no syntax tree in the current compilation, so
    /// <see cref="Engine.CodeModel.References.RefExtensions.GetPrimarySyntaxTree(IRef, CompilationContext)"/> returns <c>null</c> rather than
    /// throwing (issue #1748).
    /// </summary>
    [Fact]
    public void GetPrimarySyntaxTreeOfDurableRefInMetadata()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var durableRef = compilation.Factory.GetTypeByReflectionType( typeof(string) ).ToRef().ToDurable();

        Assert.Null( durableRef.GetPrimarySyntaxTree( compilation.CompilationContext ) );
    }

    /// <summary>
    /// A durable reference whose id resolves to nothing in the current compilation, which happens when the referenced
    /// project changed since its manifest was written, must not throw either (issue #1748).
    /// </summary>
    [Fact]
    public void GetPrimarySyntaxTreeOfUnresolvableDurableRef()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "/* nothing */" );

        var durableRef = new DeclarationIdRef<INamedType>( new SerializableDeclarationId( "T:ThereIsNoSuchType" ) );

        Assert.Null( durableRef.GetPrimarySyntaxTree( compilation.CompilationContext ) );
    }

    private const string _genericTypesCode = """
                                             class Plain { }

                                             class Generic<T>
                                             {
                                                 public class Nested { }
                                             }

                                             class Container
                                             {
                                                 public Generic<int> ConstructedField = null!;
                                                 public Generic<string> OtherConstructedField = null!;
                                             }
                                             """;

    private static INamedType GetTestType( CompilationModel compilation, string kind )
        => kind switch
        {
            "Plain" => compilation.Types.OfName( "Plain" ).Single(),
            "Generic" => compilation.Types.OfName( "Generic" ).Single(),
            "Nested" => compilation.Types.OfName( "Generic" ).Single().Types.OfName( "Nested" ).Single(),
            "Constructed" => (INamedType) compilation.Types.OfName( "Container" ).Single().Fields.OfName( "ConstructedField" ).Single().Type,
            "External" => compilation.Factory.GetTypeByReflectionType( typeof(string) ).AssertCast<INamedType>(),
            _ => throw new AssertionFailedException( $"Unknown kind '{kind}'." )
        };

    /// <summary>
    /// Verifies that converting an <see cref="INamedType"/> to a durable reference with <c>ToDurable</c> and resolving
    /// it again yields an equivalent type, for every shape of type except a constructed generic one, which
    /// <see cref="DurableRefToConstructedGenericTypeLosesTheTypeArguments"/> covers.
    /// </summary>
    [Theory]
    [InlineData( "Plain" )]
    [InlineData( "Generic" )]
    [InlineData( "Nested" )]
    [InlineData( "External" )]
    public void DurableRefToNamedTypeResolvesToAnEquivalentType( string kind )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _genericTypesCode );

        var type = GetTestType( compilation, kind );

        var resolved = type.ToRef().ToDurable().GetTarget( compilation );

        Assert.Equal( type.ToDisplayString(), resolved.ToDisplayString() );
    }

    /// <summary>
    /// Records that <c>ToDurable</c> does not preserve the type arguments of a constructed generic type, and that a
    /// <see cref="SerializableTypeId"/> does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ToDurable</c> is backed by a <c>SerializableDeclarationId</c>, which names a declaration and therefore names
    /// the generic definition. Resolving it returns <c>Generic&lt;T&gt;</c> where <c>Generic&lt;int&gt;</c> was
    /// converted, silently and with no diagnostic. This is a trap for any caller that converts a type coming from user
    /// code, because the result is a usable type rather than an error, and the widening only shows in what the caller
    /// subsequently matches.
    /// </para>
    /// <para>
    /// <c>Query.CreateBaseTypeResolver</c> is such a caller: <c>SelectTypesDerivedFrom( INamedType )</c> accepts a
    /// constructed generic type, and going through a declaration identifier would have made the query match the types
    /// derived from every construction of the generic type. It uses a <see cref="SerializableTypeId"/> for that reason.
    /// </para>
    /// </remarks>
    [Fact]
    public void DurableRefToConstructedGenericTypeLosesTheTypeArguments()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _genericTypesCode );

        var type = GetTestType( compilation, "Constructed" );
        Assert.Equal( "Generic<int>", type.ToDisplayString() );

        var throughDeclarationId = type.ToRef().ToDurable().GetTarget( compilation );
        Assert.Equal( "Generic<T>", throughDeclarationId.ToDisplayString() );

        var throughTypeId = DurableRefFactory.FromTypeId<INamedType>( type.GetSerializableTypeId() ).GetTarget( compilation );
        Assert.Equal( "Generic<int>", throughTypeId.ToDisplayString() );
    }

    /// <summary>
    /// Verifies that the durable form used by <c>Query.CreateBaseTypeResolver</c> round-trips every shape of type that
    /// <c>SelectTypesDerivedFrom( INamedType )</c> accepts.
    /// </summary>
    /// <remarks>
    /// That method converts the type it is given to a durable reference so that the query, which may outlive the
    /// compilation by an entire editing session, does not pin it (issue #1799). The type comes from user code, so the
    /// conversion has to survive every shape the signature accepts, not only the plain named type that the first
    /// version of the change was written against.
    /// </remarks>
    [Theory]
    [InlineData( "Plain" )]
    [InlineData( "Generic" )]
    [InlineData( "Nested" )]
    [InlineData( "Constructed" )]
    [InlineData( "External" )]
    public void DurableTypeIdRefResolvesToAnEquivalentType( string kind )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _genericTypesCode );

        var type = GetTestType( compilation, kind );

        var resolved = DurableRefFactory.FromTypeId<INamedType>( type.GetSerializableTypeId() ).GetTarget( compilation );

        Assert.Equal( type.ToDisplayString(), resolved.ToDisplayString() );
    }

    /// <summary>
    /// Records that a type introduced by an aspect has a <see cref="SerializableTypeId"/> but that the identifier does
    /// not resolve, and that the failure is a resolution failure rather than a failure to produce the identifier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A type identifier is syntactic, so one is produced for an introduced type as readily as for any other. Resolving
    /// it, however, goes through the symbol table of the Roslyn compilation, in which an introduced type does not
    /// exist. The two halves failing at different moments is what matters to a caller: the conversion succeeds, and the
    /// exception surfaces later, wherever the reference happens to be resolved.
    /// </para>
    /// <para>
    /// <c>Query.CreateBaseTypeResolver</c> is such a caller, and this is why it verifies the conversion instead of
    /// assuming it. Without that check, passing an introduced type to <c>SelectTypesDerivedFrom</c> builds a query that
    /// throws when it is executed, which
    /// <c>Metalama.Framework.Tests.AspectTests/Tests/Fabrics/SelectTypesDerivedFromIntroducedType.cs</c> demonstrates
    /// end to end.
    /// </para>
    /// </remarks>
    [Fact]
    public void DurableTypeIdRefToIntroducedTypeDoesNotResolve()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "class Outer;" ).CreateMutableClone();

        var typeBuilder = new NamedTypeBuilder( null!, compilation.GlobalNamespace, "Introduced", TypeKind.Class );
        typeBuilder.Freeze();
        compilation.AddTransformation( typeBuilder.CreateTransformation() );

        var introducedType = compilation.Types.OfName( "Introduced" ).Single();

        var durableRef = DurableRefFactory.FromTypeId<INamedType>( introducedType.GetSerializableTypeId() );

        Assert.Null( durableRef.GetTargetOrNull( compilation ) );
        Assert.Throws<SymbolNotFoundException>( () => durableRef.GetTarget( compilation ) );
    }
}