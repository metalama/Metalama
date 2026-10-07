// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Tests of <see cref="SignatureTypeComparer"/>.
/// </summary>
public sealed class SignatureTypeComparerTests : UnitTestClass
{
    private const string _code = """
                                 class C
                                 {
                                     void M( int[] a, int[] b, int[,] c, int[][] d, string[] e ) { }
                                 }
                                 """;

    /// <summary>
    /// Verifies that the hash code of an array type is computed from the element type and the rank, and that it terminates for both overloads.
    /// </summary>
    [Fact]
    public void GetHashCode_ArrayType_Terminates()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var parameters = compilation.Types.OfName( "C" ).Single().Methods.OfName( "M" ).Single().Parameters;
        var comparer = SignatureTypeComparer.Instance;

        var types = parameters.SelectAsArray( p => p.Type );
        var symbols = types.SelectAsArray( t => t.GetSymbol() );

        // Equal types have equal hash codes, through both overloads.
        Assert.True( comparer.Equals( types[0], types[1] ) );
        Assert.Equal( comparer.GetHashCode( types[0] ), comparer.GetHashCode( types[1] ) );
        Assert.Equal( comparer.GetHashCode( symbols[0] ), comparer.GetHashCode( symbols[1] ) );
        Assert.Equal( comparer.GetHashCode( symbols[0] ), comparer.GetHashCode( types[0] ) );

        // The rank, the element type and the nesting of arrays are part of the hash code.
        Assert.False( comparer.Equals( types[0], types[2] ) );
        Assert.NotEqual( comparer.GetHashCode( symbols[0] ), comparer.GetHashCode( symbols[2] ) );
        Assert.NotEqual( comparer.GetHashCode( symbols[0] ), comparer.GetHashCode( symbols[3] ) );
        Assert.NotEqual( comparer.GetHashCode( symbols[0] ), comparer.GetHashCode( symbols[4] ) );
    }

    /// <summary>
    /// Verifies that a named type has the same hash code whether it is given as a symbol or as an <see cref="IType"/>.
    /// </summary>
    [Fact]
    public void GetHashCode_NamedType_SameForSymbolAndType()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );
        var type = compilation.Types.OfName( "C" ).Single();

        Assert.Equal( SignatureTypeComparer.Instance.GetHashCode( type.GetSymbol() ), SignatureTypeComparer.Instance.GetHashCode( type ) );
    }
}
