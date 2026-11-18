// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Advising;

/// <summary>
/// Tests of <see cref="DurableArgumentResolver"/>. Each test creates a durable value in one compilation and resolves it in a second
/// compilation of the same code, which is what happens when an extension keeps template arguments across compilations.
/// </summary>
public sealed class DurableArgumentResolverTests : UnitTestClass
{
    private const string _code = """
                                 class C
                                 {
                                     void M( int p ) { }
                                 }
                                 """;

    /// <summary>
    /// Verifies that a durable reference to a type resolves to the type of the second compilation.
    /// </summary>
    [Fact]
    public void Resolve_DurableTypeReference_ReturnsTypeOfNewCompilation()
    {
        using var testContext = this.CreateTestContext();
        var compilation1 = testContext.CreateCompilationModel( _code );
        var compilation2 = testContext.CreateCompilationModel( _code );

        var durableRef = compilation1.Types.OfName( "C" ).Single().ToDurableRef();

        var resolved = Assert.IsAssignableFrom<INamedType>( DurableArgumentResolver.Resolve( durableRef, compilation2 ) );

        Assert.Same( compilation2.Types.OfName( "C" ).Single(), resolved );
    }

    /// <summary>
    /// Verifies that a durable reference to a parameter resolves to the parameter of the second compilation, which is also an
    /// <see cref="IExpression"/>.
    /// </summary>
    [Fact]
    public void Resolve_DurableParameterReference_ReturnsParameterOfNewCompilation()
    {
        using var testContext = this.CreateTestContext();
        var compilation1 = testContext.CreateCompilationModel( _code );
        var compilation2 = testContext.CreateCompilationModel( _code );

        var durableRef = GetParameter( compilation1 ).ToDurableRef();

        var resolved = Assert.IsAssignableFrom<IParameter>( DurableArgumentResolver.Resolve( durableRef, compilation2 ) );

        Assert.Same( GetParameter( compilation2 ), resolved );
    }

    /// <summary>
    /// Verifies that a durable expression resolves to an <see cref="IExpression"/> whose type belongs to the second compilation.
    /// </summary>
    [Fact]
    public void Resolve_DurableExpression_ReturnsExpressionOfNewCompilation()
    {
        using var testContext = this.CreateTestContext();
        var compilation1 = testContext.CreateCompilationModel( _code );
        var compilation2 = testContext.CreateCompilationModel( _code );

        IDurableExpression durableExpression;

        using ( testContext.WithExecutionContext( compilation1 ) )
        {
            durableExpression = ExpressionFactory.Parse( "1 + 2", compilation1.Factory.GetTypeByReflectionType( typeof(int) ) ).ToDurable();
        }

        var resolved = Assert.IsAssignableFrom<IExpression>( DurableArgumentResolver.Resolve( durableExpression, compilation2 ) );

        Assert.Same( compilation2, resolved.Type.Compilation );
        Assert.Equal( "int", resolved.Type.ToDisplayString() );
    }

    /// <summary>
    /// Verifies that a value that is not a durable code model value is returned unchanged.
    /// </summary>
    [Fact]
    public void Resolve_OtherValues_ReturnedUnchanged()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var array = new[] { 1, 2 };

        Assert.Null( DurableArgumentResolver.Resolve( null, compilation ) );
        Assert.Equal( 5, DurableArgumentResolver.Resolve( 5, compilation ) );
        Assert.Equal( "text", DurableArgumentResolver.Resolve( "text", compilation ) );
        Assert.Same( typeof(Guid), DurableArgumentResolver.Resolve( typeof(Guid), compilation ) );
        Assert.Same( array, DurableArgumentResolver.Resolve( array, compilation ) );
    }

    /// <summary>
    /// Verifies that a durable reference to a declaration that does not exist in the second compilation causes an exception.
    /// </summary>
    [Fact]
    public void Resolve_MissingDeclaration_Throws()
    {
        using var testContext = this.CreateTestContext();
        var compilation1 = testContext.CreateCompilationModel( _code );
        var compilation2 = testContext.CreateCompilationModel( "class D { }" );

        var durableRef = compilation1.Types.OfName( "C" ).Single().ToDurableRef();

        Assert.Throws<SymbolNotFoundException>( () => DurableArgumentResolver.Resolve( durableRef, compilation2 ) );
    }

    /// <summary>
    /// Verifies that <see cref="DurableArgumentResolver.Wrap"/> resolves the values on every read path of <see cref="IObjectReader"/>.
    /// </summary>
    [Fact]
    public void Wrap_ResolvesValuesOnRead()
    {
        using var testContext = this.CreateTestContext();
        var compilation1 = testContext.CreateCompilationModel( _code );
        var compilation2 = testContext.CreateCompilationModel( _code );

        var type2 = compilation2.Types.OfName( "C" ).Single();
        var reader = CreateReader( compilation1.Types.OfName( "C" ).Single().ToDurableRef(), 5 );

        var wrapped = DurableArgumentResolver.Wrap( reader, compilation2 );

        Assert.True( wrapped.TryGetValue( "T", out var value ) );
        Assert.Same( type2, value );
        Assert.Same( type2, wrapped["T"] );
        Assert.Equal( 5, wrapped["N"] );
        Assert.Same( type2, wrapped.Single( p => p.Key == "T" ).Value );
        Assert.Contains( type2, wrapped.Values );
        Assert.Equal( 2, wrapped.Count );
        Assert.False( wrapped.TryGetValue( "X", out _ ) );
        Assert.Same( reader.Source, wrapped.Source );
    }

    /// <summary>
    /// Verifies that wrapping an already wrapped reader resolves the values in the compilation of the last call.
    /// </summary>
    [Fact]
    public void Wrap_AlreadyWrapped_ResolvesInLastCompilation()
    {
        using var testContext = this.CreateTestContext();
        var compilation1 = testContext.CreateCompilationModel( _code );
        var compilation2 = testContext.CreateCompilationModel( _code );
        var compilation3 = testContext.CreateCompilationModel( _code );

        var reader = CreateReader( compilation1.Types.OfName( "C" ).Single().ToDurableRef(), 5 );

        var wrapped = DurableArgumentResolver.Wrap( DurableArgumentResolver.Wrap( reader, compilation2 ), compilation3 );

        Assert.Same( compilation3.Types.OfName( "C" ).Single(), wrapped["T"] );
    }

    /// <summary>
    /// Verifies that wrapping the empty reader returns the empty reader.
    /// </summary>
    [Fact]
    public void Wrap_Empty_ReturnsEmpty()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        Assert.Same( ObjectReader.Empty, DurableArgumentResolver.Wrap( ObjectReader.Empty, compilation ) );
    }

    private static IParameter GetParameter( ICompilation compilation ) => compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single().Parameters[0];

    private static IObjectReader CreateReader( object type, int number )
        => new ObjectReaderDictionaryWrapper( new Dictionary<string, object?> { ["T"] = type, ["N"] = number } );
}
