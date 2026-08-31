// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests that the code model represents a type that Roslyn could not bind, instead of throwing (issue #1867).
/// Roslyn produces an error type symbol for every reference it cannot resolve, which happens continuously at
/// design time while a <c>using</c> directive is missing, a package is restoring, or an identifier is half typed.
/// </summary>
/// <remarks>
/// <para>
/// A declaration whose signature directly names an unresolved type is excluded from the code model, so the
/// unresolved type reaches the code model as the type argument of a type that binds, as in
/// <c>List&lt;Unresolved&gt;</c>. That is the shape reported in issue #1867.
/// </para>
/// <para>
/// The code model substitutes an unresolved type by <c>object</c>. That substitution is the pre-existing
/// behaviour of <c>DeclarationFactory.GetNamedType</c>; what issue #1867 adds is that
/// <c>DeclarationFactory.GetIType</c> reaches it instead of throwing.
/// </para>
/// </remarks>
public sealed class ErrorTypeTests : UnitTestClass
{
    private const string _code = """
                                 using System.Collections.Generic;

                                 class C
                                 {
                                     public List<Unresolved> Field = null!;
                                     public Dictionary<string, Unresolved> NestedField = null!;
                                     public List<Unresolved> Method( List<Unresolved> parameter ) => parameter;
                                 }
                                 """;

    [Fact]
    public void TypeArgumentIsSubstituted()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code, ignoreErrors: true );

        var fieldType = (INamedType) compilation.Types.OfName( "C" ).Single().Fields.OfName( "Field" ).Single().Type;

        var typeArgument = Assert.Single( fieldType.TypeArguments );

        Assert.Equal( SpecialType.Object, typeArgument.SpecialType );
    }

    [Fact]
    public void SecondTypeArgumentIsSubstituted()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code, ignoreErrors: true );

        var fieldType = (INamedType) compilation.Types.OfName( "C" ).Single().Fields.OfName( "NestedField" ).Single().Type;

        Assert.Equal( SpecialType.String, fieldType.TypeArguments[0].SpecialType );
        Assert.Equal( SpecialType.Object, fieldType.TypeArguments[1].SpecialType );
    }

    [Fact]
    public void ReturnTypeAndParameterTypeAreRead()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code, ignoreErrors: true );

        var method = compilation.Types.OfName( "C" ).Single().Methods.OfName( "Method" ).Single();

        Assert.Equal( SpecialType.Object, ((INamedType) method.ReturnType).TypeArguments.Single().SpecialType );
        Assert.Equal( SpecialType.Object, ((INamedType) method.Parameters.Single().Type).TypeArguments.Single().SpecialType );
    }

    /// <summary>
    /// Verifies the call path reported in issue #1867: the display string formatter reads the type arguments of a
    /// named type, and one of them is an error type.
    /// </summary>
    [Theory]
    [InlineData( "Field", "List<object>" )]
    [InlineData( "NestedField", "Dictionary<string, object>" )]
    public void UnresolvedTypeArgumentIsFormatted( string fieldName, string expectedDisplayString )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code, ignoreErrors: true );

        var fieldType = compilation.Types.OfName( "C" ).Single().Fields.OfName( fieldName ).Single().Type;

        Assert.Equal( expectedDisplayString, fieldType.ToDisplayString() );
    }

    /// <summary>
    /// Verifies the variant of the call path reported as problem 29597, where the formatter renders a parameter
    /// list instead of a type.
    /// </summary>
    [Fact]
    public void UnresolvedTypeArgumentIsFormattedInParameterList()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code, ignoreErrors: true );

        var method = compilation.Types.OfName( "C" ).Single().Methods.OfName( "Method" ).Single();

        Assert.Equal( "C.Method(List<object>)", method.ToDisplayString() );
    }
}
