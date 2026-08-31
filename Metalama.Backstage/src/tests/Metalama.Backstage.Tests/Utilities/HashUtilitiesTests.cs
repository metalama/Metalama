// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using Xunit;

namespace Metalama.Backstage.Tests.Utilities;

/// <summary>
/// Tests of <see cref="HashUtilities"/>.
/// </summary>
public sealed class HashUtilitiesTests
{
    /// <summary>
    /// Verifies <see cref="HashUtilities.ComputeStringHash64"/> against values pinned from the specification of the
    /// <c>CryptoUtilities.ComputeStringHash64</c> method of PostSharp.
    /// </summary>
    /// <remarks>
    /// These values were computed independently of the implementation under test. They must never change, because
    /// the license audit of Metalama and the license audit of PostSharp are compared on them. See issue #1873.
    /// </remarks>
    [Theory]
    [InlineData( "gfraiteur", -2995304574211768698L )]
    [InlineData( "a", -6289574019528802036L )]
    [InlineData( "", 338333539836370388L )]
    public void ComputeStringHash64ReturnsThePinnedValue( string s, long expected )
        => Assert.Equal( expected, HashUtilities.ComputeStringHash64( s ) );

    /// <summary>
    /// Verifies that <see cref="HashUtilities.ComputeStringHash64"/> ignores the surrounding white space and the
    /// case of the input.
    /// </summary>
    [Theory]
    [InlineData( "gfraiteur" )]
    [InlineData( "GFRAITEUR" )]
    [InlineData( "  GFraiteur  " )]
    [InlineData( "\tGfraiteur\r\n" )]
    public void ComputeStringHash64NormalizesWhiteSpaceAndCase( string s )
        => Assert.Equal( -2995304574211768698L, HashUtilities.ComputeStringHash64( s ) );

    /// <summary>
    /// Verifies that <see cref="HashUtilities.ComputeStringHash64"/> normalizes the input, so that the composed and
    /// the decomposed forms of the same account name give the same value.
    /// </summary>
    [Theory]
    [InlineData( "Élodie" )]  // Composed form.
    [InlineData( "Élodie" )] // Decomposed form.
    public void ComputeStringHash64NormalizesTheInput( string s )
        => Assert.Equal( -3286989950528976640L, HashUtilities.ComputeStringHash64( s ) );

    /// <summary>
    /// Verifies that <see cref="HashUtilities.ComputeStringHash64"/> returns zero for a null input, and never
    /// returns zero for a non-null input.
    /// </summary>
    [Fact]
    public void ComputeStringHash64ReservesZeroForNull()
    {
        Assert.Equal( 0L, HashUtilities.ComputeStringHash64( null ) );

        Assert.NotEqual( 0L, HashUtilities.ComputeStringHash64( "" ) );
        Assert.NotEqual( 0L, HashUtilities.ComputeStringHash64( "gfraiteur" ) );
    }

    /// <summary>
    /// Verifies that <see cref="HashUtilities.ComputeStringHash64"/> is unkeyed, so that its value does not depend on
    /// any per-installation state, unlike <see cref="HashUtilities.ComputeInt64Hmac"/>.
    /// </summary>
    /// <remarks>
    /// This is the property that makes one developer who uses several machines a single user in the license audit.
    /// See issue #1873.
    /// </remarks>
    [Fact]
    public void ComputeStringHash64IsNotTheSaltedHash()
    {
        var unkeyed = HashUtilities.ComputeStringHash64( "gfraiteur" );

        Assert.NotEqual( unkeyed, HashUtilities.ComputeInt64Hmac( "gfraiteur", 0x0123456789ABCDEF ) );
        Assert.NotEqual( unkeyed, HashUtilities.ComputeInt64Hmac( "gfraiteur", 0x7EDCBA9876543210 ) );
    }
}
